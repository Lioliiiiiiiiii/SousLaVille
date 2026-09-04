using System.Collections.Generic;
using SousLaVille.UI;
using UnityEngine;

namespace SousLaVille.Buildings
{
    /// <summary>
    /// Le personnage qui tient un batiment et dit ce qu'on peut y faire. L'artisan des
    /// plaques en phase 9a, l'ouvrier des tuyaux en 9b, et les trois personnages de l'usine
    /// a panneaux en phase 12 sur le meme patron.
    ///
    /// Il ne bloque rien : on peut prendre un echantillon sans lui avoir parle. Il ne pose
    /// aucune question et n'attend aucune reponse ; Espace face a lui le fait parler, Espace
    /// fait defiler puis referme.
    ///
    /// Ses phrases sont des images dessinees a la generation de l'art, de moins de six mots,
    /// en francais, conformement a CLAUDE.md.
    ///
    /// Il vit dans une couche de jeu, que le SceneRouter eteint : il resout donc la boite de
    /// dialogue paresseusement, et en FindObjectsInactive.Include puisque cette boite est
    /// eteinte tant que personne ne parle. Il s'inscrit dans OnEnable et se retire dans
    /// OnDisable, comme ManholePortal : l'interacteur cherche un personnage a chaque image,
    /// et un FindObjectsByType par image allouerait un tableau soixante fois par seconde.
    /// </summary>
    public class Villager : MonoBehaviour
    {
        [Tooltip("La case ou il se tient. On lui parle en le regardant depuis une case voisine.")]
        [SerializeField] private Vector2Int cell;

        [Tooltip("Ce qu'il dit, une image par phrase, dans l'ordre.")]
        [SerializeField] private Sprite[] lines;

        [Tooltip("La bulle affichee au-dessus de SA tete quand le joueur le regarde.")]
        [SerializeField] private SpriteRenderer prompt;

        // La couche qui dort est desactivee : ses personnages se desinscrivent tout seuls,
        // et la recherche ne voit donc que celui de la piece ou l'on se tient.
        private static readonly List<Villager> Active = new List<Villager>();

        private SpeechBox box;

        public Vector2Int Cell => cell;

        private void OnEnable()
        {
            Active.Add(this);
        }

        private void OnDisable()
        {
            Active.Remove(this);
            ShowPrompt(false);
        }

        /// <summary>
        /// Allume ou eteint la bulle au-dessus de sa tete. C'est l'interacteur qui l'appelle,
        /// puisque c'est lui qui sait ce que le joueur regarde.
        ///
        /// La bulle est au-dessus de SA tete et non de celle du joueur, contrairement a tous
        /// les autres pictos d'action. Decide le 3 septembre 2026, apres l'avoir vu en jeu :
        /// la place habituelle, une unite et quart au-dessus du joueur, tombe exactement sur
        /// le visage de qui se tient une case plus haut, et le picto effacait l'artisan a qui
        /// l'on venait parler. Un picto qui cache ce qu'il designe ne designe rien.
        /// </summary>
        public void ShowPrompt(bool visible)
        {
            if (prompt != null && prompt.enabled != visible)
            {
                prompt.enabled = visible;
            }
        }

        /// <summary>
        /// Change ce qu'il dit. Phase 12e : c'est ainsi qu'un guide SE TAIT SANS DISPARAITRE
        /// quand sa lecon est acquise. Vider ses lignes suffit — `CanSpeak` vaut deja
        /// `LineCount > 0`, et `PlayerInteractor.Evaluate` ne propose `Talk` que si `CanSpeak`.
        /// Le personnage reste donc visible, sur sa case, et Espace ne fait plus rien devant
        /// lui : aucune disparition, aucun echec puni.
        ///
        /// La boite en cours n'est PAS refermee : SpeechBox.Open garde sa propre reference sur
        /// les phrases, donc une phrase commencee se termine. Couper quelqu'un au milieu d'un
        /// mot serait la seule chose plus deroutante que de le laisser finir.
        /// </summary>
        public void SetLines(Sprite[] sentences)
        {
            lines = sentences;
        }

        /// <summary>Nombre de phrases. Sert aux verifications.</summary>
        public int LineCount => lines != null ? lines.Length : 0;

        /// <summary>Vrai s'il a quelque chose a dire.</summary>
        public bool CanSpeak => LineCount > 0 && ResolveBox() != null;

        /// <summary>
        /// Le fait parler. Rend la boite ouverte, ou null s'il n'a rien a dire : l'appelant
        /// s'y abonne pour rallumer le personnage joueur a la fermeture.
        /// </summary>
        public SpeechBox Speak()
        {
            SpeechBox speech = ResolveBox();
            if (speech == null || !speech.Open(lines))
            {
                return null;
            }

            return speech;
        }

        /// <summary>
        /// La boite vit dans Persistent, jamais dechargee, mais elle est ETEINTE tant que
        /// personne ne parle : il faut donc l'inclure explicitement dans la recherche.
        /// </summary>
        private SpeechBox ResolveBox()
        {
            if (box == null)
            {
                box = FindAnyObjectByType<SpeechBox>(FindObjectsInactive.Include);
            }

            return box;
        }

        /// <summary>Le personnage pose sur cette case, ou null. Sert a l'interacteur.</summary>
        public static Villager At(Vector2Int cell)
        {
            foreach (Villager villager in Active)
            {
                if (villager.cell == cell)
                {
                    return villager;
                }
            }

            return null;
        }
    }
}
