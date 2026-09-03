using System;
using SousLaVille.Buildings;
using SousLaVille.Core;
using UnityEngine;
using UnityEngine.UI;

namespace SousLaVille.UI
{
    /// <summary>
    /// Le plan du village, ouvert le temps de choisir une bouche d'egout.
    ///
    /// La phase 2 avait ecarte la mini-carte : « elle serait vide de sens tant que le reseau
    /// n'existe pas, et demanderait une legende ». Elle a maintenant un objet, et elle ne
    /// demande toujours pas de legende : chaque bouche y porte la plaque qu'elle a deja.
    ///
    /// Les fleches passent d'une bouche a l'autre, Espace pose et referme. Aucune annulation
    /// n'est necessaire : reposer autre chose se fait du meme geste, et Espace sort toujours.
    /// </summary>
    public class VillageMapScreen : MonoBehaviour
    {
        [Tooltip("Le panneau entier, eteint tant qu'on ne choisit pas.")]
        [SerializeField] private GameObject panel;

        [Tooltip("Un marqueur par bouche, dans l'ordre du plan du village.")]
        [SerializeField] private Image[] markers;

        [Tooltip("Le cadre pose sur la bouche choisie.")]
        [SerializeField] private Image cursor;

        [Tooltip("L'allure d'usine, pour une bouche qui n'a rien recu.")]
        [SerializeField] private Sprite defaultCover;

        [Tooltip("Taille du plan en cases, et facteur d'agrandissement a l'ecran.")]
        [SerializeField] private Vector2Int mapSize = new Vector2Int(40, 30);

        [SerializeField] private float mapScale = 4f;

        private SousLaVilleInputActions input;
        private ManholeFactory factory;
        private int carriedCover = -1;
        private int selection;
        private int openedFrame = -1;
        private Vector2Int lastDirection;

        /// <summary>Vrai tant que le plan est ouvert. L'interacteur s'en sert pour se taire.</summary>
        public bool IsOpen { get; private set; }

        /// <summary>Leve a la fermeture. Le personnage se rallume la-dessus.</summary>
        public event Action Closed;

        private void Awake()
        {
            EnsureInput();
            Hide();
        }

        private void OnEnable()
        {
            // Recompiler pendant le play recharge le domaine : Unity rappelle OnEnable sans
            // repasser par Awake. Meme garde-fou que dans PlayerController.
            EnsureInput();
            input.Gameplay.Enable();
        }

        private void OnDisable()
        {
            input?.Gameplay.Disable();
        }

        private void OnDestroy()
        {
            input?.Dispose();
        }

        private void EnsureInput()
        {
            if (input == null)
            {
                input = new SousLaVilleInputActions();
            }
        }

        /// <summary>
        /// Ouvre le plan, une plaque en main. Rend false et ne fait rien s'il n'y a pas
        /// d'atelier ou pas de bouche : il ne se passe simplement rien.
        /// </summary>
        public bool Open(int coverIndex)
        {
            factory = FindAnyObjectByType<ManholeFactory>(FindObjectsInactive.Include);
            if (factory == null || factory.ManholeCells.Count == 0 || markers == null)
            {
                return false;
            }

            carriedCover = coverIndex;
            selection = 0;
            lastDirection = Vector2Int.zero;

            // L'appui qui ouvre le plan ne doit pas poser dans la foulee : l'interacteur et
            // cet ecran lisent la meme touche, et le meme Espace serait vu deux fois.
            openedFrame = Time.frameCount;

            IsOpen = true;
            if (panel != null)
            {
                panel.SetActive(true);
            }

            Refresh();
            return true;
        }

        private void Update()
        {
            if (!IsOpen)
            {
                return;
            }

            if (Time.frameCount == openedFrame)
            {
                return;
            }

            ReadDirection();

            if (input.Gameplay.Interact.WasPressedThisFrame())
            {
                Place();
            }
        }

        /// <summary>
        /// Lit les fleches et n'agit qu'au changement de direction : maintenir une fleche ne
        /// fait pas defiler, et il n'y a aucun timing a attraper.
        ///
        /// La lecture du clavier s'arrete ici. Le choix lui-meme est dans Select, qui se
        /// verifie sans clavier.
        /// </summary>
        private void ReadDirection()
        {
            Vector2Int direction = Dominant(input.Gameplay.Move.ReadValue<Vector2>());

            if (direction == lastDirection)
            {
                return;
            }

            lastDirection = direction;

            if (direction != Vector2Int.zero)
            {
                Select(direction);
            }
        }

        /// <summary>
        /// Passe a la bouche la plus proche dans cette direction, s'il y en a une. Rien ne se
        /// passe au bord : pousser vers le vide n'est pas une erreur, c'est juste sans effet.
        ///
        /// Publique parce qu'elle se verifie ainsi sans dependre du clavier.
        /// </summary>
        public bool Select(Vector2Int direction)
        {
            if (!IsOpen || direction == Vector2Int.zero)
            {
                return false;
            }

            var cells = factory.ManholeCells;
            Vector2 from = cells[selection];
            int best = -1;
            float bestDistance = float.MaxValue;

            for (int i = 0; i < cells.Count; i++)
            {
                if (i == selection)
                {
                    continue;
                }

                Vector2 delta = (Vector2)cells[i] - from;
                if (Vector2.Dot(delta, direction) <= 0f)
                {
                    continue;
                }

                float distance = delta.sqrMagnitude;
                if (distance < bestDistance)
                {
                    bestDistance = distance;
                    best = i;
                }
            }

            if (best < 0)
            {
                return false;
            }

            selection = best;
            Refresh();
            return true;
        }

        /// <summary>La bouche actuellement visee. Sert aux verifications.</summary>
        public Vector2Int SelectedCell =>
            factory != null && selection < factory.ManholeCells.Count
                ? factory.ManholeCells[selection]
                : Vector2Int.zero;

        /// <summary>L'axe dominant seul, comme le personnage depuis la phase 1.</summary>
        private static Vector2Int Dominant(Vector2 raw)
        {
            if (Mathf.Abs(raw.x) < 0.5f && Mathf.Abs(raw.y) < 0.5f)
            {
                return Vector2Int.zero;
            }

            if (Mathf.Abs(raw.x) >= Mathf.Abs(raw.y))
            {
                return new Vector2Int(raw.x > 0f ? 1 : -1, 0);
            }

            return new Vector2Int(0, raw.y > 0f ? 1 : -1);
        }

        /// <summary>
        /// Pose la plaque en main sur la bouche visee et referme. Publique pour la meme
        /// raison que Select.
        /// </summary>
        public void Place()
        {
            var cells = factory.ManholeCells;
            if (selection >= 0 && selection < cells.Count)
            {
                factory.SetCover(cells[selection], carriedCover);
            }

            Hide();
            IsOpen = false;
            Closed?.Invoke();
        }

        /// <summary>Chaque marqueur montre la plaque que porte deja sa bouche.</summary>
        private void Refresh()
        {
            var cells = factory.ManholeCells;

            for (int i = 0; i < markers.Length; i++)
            {
                Image marker = markers[i];
                if (marker == null)
                {
                    continue;
                }

                bool used = i < cells.Count;
                marker.enabled = used;

                if (!used)
                {
                    continue;
                }

                ManholeCoverDefinition definition = factory.CoverFor(cells[i]);
                marker.sprite = definition != null ? definition.Cover : defaultCover;
                marker.rectTransform.anchoredPosition = PositionOf(cells[i]);
            }

            if (cursor != null && selection < cells.Count)
            {
                cursor.enabled = true;
                cursor.rectTransform.anchoredPosition = PositionOf(cells[selection]);
            }
        }

        /// <summary>
        /// Une case du village vers un point du plan. Le plan est centre, une case vaut
        /// mapScale pixels, et l'origine du village est en bas a gauche.
        /// </summary>
        private Vector2 PositionOf(Vector2Int cell)
        {
            float x = (cell.x + 0.5f - mapSize.x * 0.5f) * mapScale;
            float y = (cell.y + 0.5f - mapSize.y * 0.5f) * mapScale;
            return new Vector2(x, y);
        }

        private void Hide()
        {
            if (panel != null)
            {
                panel.SetActive(false);
            }
        }
    }
}
