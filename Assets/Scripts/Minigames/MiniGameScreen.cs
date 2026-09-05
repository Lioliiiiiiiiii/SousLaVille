using System;
using System.Collections.Generic;
using SousLaVille.Core;
using UnityEngine;

namespace SousLaVille.Minigames
{
    /// <summary>
    /// LE SQUELETTE DES TROIS MINI-JEUX, phase 14. Tout ce que Le Stock, La Fabrique et Le
    /// Plan partagent vit ici, et rien d'autre : chacun n'ecrit que sa propre regle.
    ///
    /// C'est le patron de VillageMapScreen — un curseur dans un ensemble, les fleches qui
    /// deplacent, Espace qui valide — assemble avec celui de SpeechBox — le panneau eteint,
    /// la garde d'ouverture, l'evenement de fermeture et l'arret de l'horloge. Les deux
    /// existent depuis les phases 7 et 9a ; on ne les redouble pas, on les reunit.
    ///
    /// TROIS PROPRIETES QUI NE SE NEGOCIENT PAS, chacune payee une fois dans ce projet :
    ///
    /// 1. La garde openedFrame. L'interacteur et cet ecran lisent LA MEME TOUCHE. Sans la
    ///    garde de CHAQUE cote — celle-ci a l'ouverture, screenClosedFrame a la fermeture —
    ///    le meme Espace est lu deux fois et l'ecran se rouvre a peine referme. L'ordre des
    ///    Update n'est garanti par rien.
    ///
    /// 2. Le compteur statique AnyOpen. GameClock s'y arrete comme il s'arrete sur une boite
    ///    de dialogue : une manche de seize paires dure plusieurs minutes, une saison en dure
    ///    dix, et un tick tombant au milieu d'une partie gelerait le village pendant qu'on
    ///    joue a autre chose.
    ///
    /// 3. Move et Validate sont PUBLIQUES et separees de la lecture du clavier, comme
    ///    VillageMapScreen.Select depuis la phase 7 : la regle du jeu se verifie alors sans
    ///    clavier et sans dependre du focus de l'editeur.
    ///
    /// L'ecran vit dans le HUD, donc dans Persistent, jamais dechargee. Son panneau est
    /// eteint tant qu'on ne joue pas : le HUD ne gagne aucun indicateur permanent.
    /// </summary>
    public abstract class MiniGameScreen : MonoBehaviour
    {
        [Tooltip("Le panneau entier, eteint tant qu'on ne joue pas.")]
        [SerializeField] protected GameObject panel;

        [Tooltip("Quel mini-jeu est-ce. Un Villager le nomme pour le lancer.")]
        [SerializeField] private MiniGameKind kind = MiniGameKind.None;

        private SousLaVilleInputActions input;
        private Vector2Int lastDirection;
        private int openedFrame = -1;

        // Le registre statique, comme celui de Villager et de ManholePortal : le personnage
        // cherche son ecran une fois et le garde, plutot qu'un FindObjectsByType par image.
        private static readonly List<MiniGameScreen> Active = new List<MiniGameScreen>();

        private static int openCount;

        /// <summary>Vrai tant qu'un mini-jeu quelconque est ouvert. GameClock s'y arrete.</summary>
        public static bool AnyOpen => openCount > 0;

        /// <summary>Vrai tant que CET ecran est ouvert. L'interacteur s'en sert pour se taire.</summary>
        public bool IsOpen { get; private set; }

        /// <summary>Quel mini-jeu cet ecran porte.</summary>
        public MiniGameKind Kind => kind;

        /// <summary>Leve a la fermeture. Le personnage joueur se rallume la-dessus.</summary>
        public event Action Closed;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetStatics()
        {
            // Le rechargement de domaine desactive ne remet pas les statiques a zero : une
            // partie laissee ouverte a l'arret figerait l'horloge de la session suivante.
            openCount = 0;
            Active.Clear();
        }

        /// <summary>L'ecran de ce mini-jeu, ou null si personne ne le porte.</summary>
        public static MiniGameScreen Find(MiniGameKind wanted)
        {
            if (wanted == MiniGameKind.None)
            {
                return null;
            }

            foreach (MiniGameScreen screen in Active)
            {
                if (screen.kind == wanted)
                {
                    return screen;
                }
            }

            return null;
        }

        protected virtual void Awake()
        {
            EnsureInput();
            Hide();
        }

        protected virtual void OnEnable()
        {
            // Recompiler pendant le play recharge le domaine : Unity rappelle OnEnable sans
            // repasser par Awake. Meme garde-fou que dans PlayerController.
            EnsureInput();
            input.Gameplay.Enable();
            Active.Add(this);
        }

        protected virtual void OnDisable()
        {
            Active.Remove(this);
            input?.Gameplay.Disable();

            // Un ecran eteint doit relacher son compte, sinon l'horloge resterait figee pour
            // toujours. Meme precaution que SpeechBox.
            if (IsOpen)
            {
                IsOpen = false;
                openCount--;
            }
        }

        protected virtual void OnDestroy()
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
        /// Ouvre le mini-jeu. Rend false et ne fait rien si la partie ne peut pas commencer :
        /// il ne se passe simplement rien, aucune erreur, aucun echec puni.
        /// </summary>
        public bool Open()
        {
            if (panel == null || IsOpen || !Begin())
            {
                return false;
            }

            // L'appui qui ouvre le jeu ne doit pas jouer un coup dans la foulee : l'interacteur
            // et cet ecran lisent la meme touche.
            openedFrame = Time.frameCount;
            lastDirection = Vector2Int.zero;

            openCount++;
            IsOpen = true;
            panel.SetActive(true);
            return true;
        }

        /// <summary>
        /// Referme. Publique : c'est le mini-jeu lui-meme qui decide quand une manche est
        /// finie, et la verification s'en sert sans clavier.
        /// </summary>
        public void Close()
        {
            if (!IsOpen)
            {
                return;
            }

            openCount--;
            IsOpen = false;
            Hide();
            Closed?.Invoke();
        }

        private void Update()
        {
            if (!IsOpen || Time.frameCount == openedFrame)
            {
                return;
            }

            ReadDirection();

            // WasPressedThisFrame et non ReadValue : un appui, jamais un maintien. Espace
            // garde enfonce ne retourne pas tout le plateau.
            if (input.Gameplay.Interact.WasPressedThisFrame())
            {
                Validate();
            }
        }

        /// <summary>
        /// Lit les fleches et n'agit qu'au CHANGEMENT de direction : maintenir une fleche ne
        /// fait pas defiler, et il n'y a aucun timing a attraper. Quatre directions, aucune
        /// diagonale, comme le personnage depuis la phase 1.
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
                Move(direction);
            }
        }

        /// <summary>L'axe dominant seul, comme le personnage depuis la phase 1.</summary>
        protected static Vector2Int Dominant(Vector2 raw)
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

        private void Hide()
        {
            if (panel != null)
            {
                panel.SetActive(false);
            }
        }

        /// <summary>
        /// Prepare une manche. Rend false si elle ne peut pas etre distribuee : l'ecran ne
        /// s'ouvre alors pas du tout, plutot que de s'ouvrir vide.
        /// </summary>
        protected abstract bool Begin();

        /// <summary>Une fleche. Publique : elle se verifie sans clavier.</summary>
        public abstract void Move(Vector2Int direction);

        /// <summary>Espace. Publique, pour la meme raison.</summary>
        public abstract void Validate();
    }
}
