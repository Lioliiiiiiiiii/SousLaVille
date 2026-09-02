using SousLaVille.Core;
using SousLaVille.World;
using UnityEngine;

namespace SousLaVille.Player
{
    /// <summary>
    /// Deplacement case par case aux quatre fleches. Aucun Rigidbody2D, aucun collider :
    /// les collisions sont une lecture de tilemap. Plus previsible pour un enfant de six
    /// ans, et cela tient les 60 fps sans effort.
    ///
    /// Une case bloquee ne punit jamais : le personnage se tourne vers elle et reste sur
    /// place. Pas de recul, pas de son d'echec.
    /// </summary>
    public class PlayerController : MonoBehaviour
    {
        [Tooltip("Cases parcourues par seconde. Vitesse constante, aucune acceleration.")]
        [SerializeField] private float tilesPerSecond = 5f;

        [Tooltip("Le sprite du corps. Change avec la direction regardee.")]
        [SerializeField] private SpriteRenderer body;

        [Tooltip("Un sprite par direction, dans l'ordre bas, haut, gauche, droite.")]
        [SerializeField] private Sprite spriteDown;
        [SerializeField] private Sprite spriteUp;
        [SerializeField] private Sprite spriteLeft;
        [SerializeField] private Sprite spriteRight;

        private SousLaVilleInputActions input;
        private GridMap map;
        private SpriteRenderer[] renderers;

        private Vector2Int currentCell;
        private Vector2Int targetCell;
        private float stepProgress;
        private bool isMoving;
        private Vector2Int facing = Vector2Int.down;

        /// <summary>Case occupee. Pendant un pas, c'est encore la case de depart.</summary>
        public Vector2Int Cell => currentCell;

        /// <summary>Direction regardee, l'une des quatre. Jamais nulle.</summary>
        public Vector2Int Facing => facing;

        /// <summary>Case visee par le regard. La phase 3 y fera agir Espace.</summary>
        public Vector2Int FacingCell => currentCell + facing;

        /// <summary>Vrai si la carte de la couche active a ete trouvee.</summary>
        public bool HasMap => map != null;

        /// <summary>Carte de la couche courante. Nulle tant qu'aucune couche n'est allumee.</summary>
        public GridMap Map => map;

        private void Awake()
        {
            EnsureInput();

            // Le corps et le picto d'action : tous deux doivent suivre la famille de
            // Sorting Layers de la couche courante.
            renderers = GetComponentsInChildren<SpriteRenderer>(true);

            ApplyFacingSprite();
        }

        private void OnEnable()
        {
            // Recompiler pendant le play recharge le domaine : Unity rappelle OnEnable sans
            // repasser par Awake, et les champs non serialises sont perdus. On recree donc
            // l'input ici plutot que de laisser une NullReferenceException.
            EnsureInput();
            input.Gameplay.Enable();
        }

        private void EnsureInput()
        {
            if (input == null)
            {
                input = new SousLaVilleInputActions();
            }
        }

        private void OnDisable()
        {
            input?.Gameplay.Disable();
        }

        private void OnDestroy()
        {
            input?.Dispose();
        }

        private void Start()
        {
            ResolveMap();
        }

        private void Update()
        {
            if (!ResolveMap())
            {
                return;
            }

            if (isMoving)
            {
                stepProgress += tilesPerSecond * Time.deltaTime;

                if (stepProgress < 1f)
                {
                    ApplyStepPosition();
                    return;
                }

                // Le surplus est reporte sur le pas suivant : une fleche maintenue avance
                // a vitesse exactement constante, sans micro-pause a chaque case.
                stepProgress -= 1f;
                currentCell = targetCell;
                isMoving = false;
            }

            // A l'arret seulement : deux pas ne peuvent jamais se chevaucher.
            TryStartStep();

            if (isMoving)
            {
                ApplyStepPosition();
            }
            else
            {
                stepProgress = 0f;
                transform.position = map.CellToWorld(currentCell);
            }
        }

        /// <summary>
        /// Trouve la carte de la couche active. FindAnyObjectByType ignore les objets
        /// eteints : la couche inactive ne repond pas, donc c'est toujours la bonne carte
        /// qui est trouvee, y compris apres la bascule de la phase 2.
        /// </summary>
        private bool ResolveMap()
        {
            if (map != null && map.isActiveAndEnabled)
            {
                return true;
            }

            GridMap found = FindAnyObjectByType<GridMap>();
            if (found == null)
            {
                // La scene Persistent demarre avant Surface : quelques frames sans carte
                // au lancement, c'est normal. Le personnage attend sans bouger.
                map = null;
                return false;
            }

            bool changedLayer = map != found;
            map = found;

            if (changedLayer)
            {
                ApplyLayerVisuals();
                SnapToGrid();
            }

            return true;
        }

        /// <summary>
        /// Repose le personnage sur une case donnee de la couche courante. Appele par
        /// PlayerInteractor pendant que l'ecran est noir, juste apres la bascule de couche.
        /// </summary>
        public void Teleport(Vector2Int cell)
        {
            if (!ResolveMap())
            {
                return;
            }

            currentCell = cell;
            targetCell = cell;
            stepProgress = 0f;
            isMoving = false;
            transform.position = map.CellToWorld(cell);
        }

        /// <summary>
        /// Fait rendre le personnage dans la famille de Sorting Layers de sa couche. Sans
        /// cela, il descend et devient noir : la lumiere globale du sous-sol ne porte que
        /// sur la famille Underground.
        /// </summary>
        private void ApplyLayerVisuals()
        {
            string layerName = GameSortingLayers.EntitiesFor(map.Layer);

            foreach (SpriteRenderer renderer in renderers)
            {
                renderer.sortingLayerName = layerName;
            }
        }

        /// <summary>Recale le personnage au centre de sa case sur la carte fraichement trouvee.</summary>
        private void SnapToGrid()
        {
            currentCell = map.WorldToCell(transform.position);
            targetCell = currentCell;
            stepProgress = 0f;
            isMoving = false;
            transform.position = map.CellToWorld(currentCell);
        }

        private void TryStartStep()
        {
            Vector2Int direction = DominantDirection(input.Gameplay.Move.ReadValue<Vector2>());
            if (direction == Vector2Int.zero)
            {
                return;
            }

            // On tourne meme si la case est bloquee : le regard suit toujours la fleche.
            if (facing != direction)
            {
                facing = direction;
                ApplyFacingSprite();
            }

            Vector2Int next = currentCell + direction;
            if (!map.IsWalkable(next))
            {
                return;
            }

            targetCell = next;
            isMoving = true;
        }

        /// <summary>
        /// Quatre sprites, un par direction. Decide le 2 septembre 2026 : un repere dessine
        /// sur un sprite unique ne sait montrer que le bas.
        /// </summary>
        private void ApplyFacingSprite()
        {
            if (body == null)
            {
                return;
            }

            Sprite sprite;
            if (facing == Vector2Int.up)
            {
                sprite = spriteUp;
            }
            else if (facing == Vector2Int.left)
            {
                sprite = spriteLeft;
            }
            else if (facing == Vector2Int.right)
            {
                sprite = spriteRight;
            }
            else
            {
                sprite = spriteDown;
            }

            if (sprite != null)
            {
                body.sprite = sprite;
            }
        }

        private void ApplyStepPosition()
        {
            Vector3 from = map.CellToWorld(currentCell);
            Vector3 to = map.CellToWorld(targetCell);
            transform.position = Vector3.Lerp(from, to, stepProgress);
        }

        /// <summary>
        /// Ne garde que l'axe dominant : le composite du New Input System laisse encore
        /// passer les diagonales, deux fleches enfoncees ensemble ne doivent ni faire
        /// glisser en biais ni immobiliser. X est prioritaire a egalite, choix arbitraire
        /// mais il fallait trancher.
        /// </summary>
        private static Vector2Int DominantDirection(Vector2 raw)
        {
            const float deadZone = 0.5f;

            float absX = Mathf.Abs(raw.x);
            float absY = Mathf.Abs(raw.y);

            if (absX < deadZone && absY < deadZone)
            {
                return Vector2Int.zero;
            }

            if (absX >= absY)
            {
                return raw.x > 0f ? Vector2Int.right : Vector2Int.left;
            }

            return raw.y > 0f ? Vector2Int.up : Vector2Int.down;
        }
    }
}
