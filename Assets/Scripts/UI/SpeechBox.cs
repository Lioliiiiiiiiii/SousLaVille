using System;
using SousLaVille.Core;
using UnityEngine;
using UnityEngine.UI;

namespace SousLaVille.UI
{
    /// <summary>
    /// Ce que dit un personnage, une phrase a la fois, en bas de l'ecran. Espace passe a la
    /// suivante, et referme apres la derniere.
    ///
    /// Les phrases sont des IMAGES, dessinees par PixelFont a la generation de l'art, comme
    /// les noms de villes de la phase 7 : une police TTF s'afficherait lissee et hors grille
    /// a la resolution 320x180. La police reste donc cote Editor.
    ///
    /// La boite vit dans le HUD, eteinte tant que personne ne parle, comme le voile du fondu
    /// depuis la phase 2 : le HUD ne gagne aucun indicateur permanent.
    ///
    /// Elle lit Espace elle-meme, exactement comme VillageMapScreen, et pour la meme raison :
    /// c'est le composant ouvert qui consomme la touche. D'ou les DEUX gardes symetriques,
    /// l'image d'ouverture ici et l'image de fermeture dans PlayerInteractor. Sans elles, le
    /// meme Espace est lu deux fois et le dialogue se rouvre a peine referme. Le bug a deja
    /// coute une seance en phase 7.
    /// </summary>
    public class SpeechBox : MonoBehaviour
    {
        [Tooltip("Le panneau entier, eteint tant que personne ne parle.")]
        [SerializeField] private GameObject panel;

        [Tooltip("L'image de la phrase affichee.")]
        [SerializeField] private Image line;

        private SousLaVilleInputActions input;
        private Sprite[] lines;
        private int index;
        private int openedFrame = -1;

        /// <summary>Vrai tant qu'un personnage parle. L'interacteur s'en sert pour se taire.</summary>
        public bool IsOpen { get; private set; }

        /// <summary>
        /// Vrai si UNE boite quelconque est ouverte. Phase 12e : GameClock s'en sert pour
        /// s'arreter pendant qu'on lit, et il ne doit pas avoir a chercher la boite — elle vit
        /// dans Persistent, lui aussi, mais un FindAnyObjectByType par image serait absurde.
        /// Un compteur statique remis a zero au chargement de domaine.
        /// </summary>
        public static bool AnyOpen => openCount > 0;

        private static int openCount;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetStatics()
        {
            // Le rechargement de domaine desactive ne remet pas les statiques a zero : une
            // boite laissee ouverte a l'arret figerait l'horloge de la session suivante.
            openCount = 0;
        }

        /// <summary>Leve a la fermeture. Le personnage joueur se rallume la-dessus.</summary>
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

            // Une boite eteinte avec sa couche doit relacher son compte, sinon l'horloge
            // resterait figee pour toujours.
            if (IsOpen)
            {
                IsOpen = false;
                openCount--;
            }
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
        /// Ouvre la boite sur une suite de phrases. Rend false et ne fait rien s'il n'y a
        /// aucune phrase : il ne se passe simplement rien, aucune erreur.
        /// </summary>
        public bool Open(Sprite[] sentences)
        {
            if (sentences == null || sentences.Length == 0 || panel == null || line == null)
            {
                return false;
            }

            lines = sentences;
            index = 0;

            // L'appui qui ouvre le dialogue ne doit pas passer a la phrase suivante dans la
            // foulee : l'interacteur et cette boite lisent la meme touche.
            openedFrame = Time.frameCount;

            if (!IsOpen)
            {
                openCount++;
            }

            IsOpen = true;
            panel.SetActive(true);
            Apply();
            return true;
        }

        private void Update()
        {
            if (!IsOpen || Time.frameCount == openedFrame)
            {
                return;
            }

            if (input.Gameplay.Interact.WasPressedThisFrame())
            {
                Advance();
            }
        }

        /// <summary>
        /// La phrase suivante, ou la fermeture apres la derniere. Separee de la lecture du
        /// clavier pour se verifier sans dependre du focus de l'editeur, comme
        /// VillageMapScreen.Select depuis la phase 7.
        /// </summary>
        public void Advance()
        {
            if (!IsOpen)
            {
                return;
            }

            index++;

            if (lines == null || index >= lines.Length)
            {
                Close();
                return;
            }

            Apply();
        }

        /// <summary>Rang de la phrase affichee. Sert aux verifications.</summary>
        public int LineIndex => index;

        /// <summary>Nombre de phrases du dialogue en cours.</summary>
        public int LineCount => lines != null ? lines.Length : 0;

        /// <summary>
        /// Affiche la phrase courante a sa taille exacte en pixels de la resolution de
        /// reference, 320x180.
        ///
        /// Surtout pas SetNativeSize : il divise la largeur du sprite par ses pixels par
        /// unite, 16 ici, puis la multiplie par les 100 pixels par unite du Canvas. Une
        /// phrase de 109 pixels sortait a 681, soit deux fois la largeur de l'ecran. Le reste
        /// du HUD pose ses tailles en pixels explicites depuis la phase 2 ; celle-ci fait
        /// pareil, en lisant simplement le rectangle du sprite.
        /// </summary>
        private void Apply()
        {
            if (line == null || lines == null || index < 0 || index >= lines.Length)
            {
                return;
            }

            Sprite sprite = lines[index];
            line.sprite = sprite;

            if (sprite != null)
            {
                line.rectTransform.sizeDelta = new Vector2(sprite.rect.width, sprite.rect.height);
            }
        }

        private void Close()
        {
            if (IsOpen)
            {
                openCount--;
            }

            Hide();
            Closed?.Invoke();
        }

        private void Hide()
        {
            IsOpen = false;
            lines = null;
            index = 0;

            if (panel != null)
            {
                panel.SetActive(false);
            }
        }
    }
}
