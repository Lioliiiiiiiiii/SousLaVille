using UnityEngine;

namespace SousLaVille.Buildings
{
    /// <summary>
    /// La planche de l'usine a panneaux, phase 13. Elle connait les cases ou les panneaux du
    /// catalogue sont exposes, et le NOM de chacun.
    ///
    /// Patron exact de PipeFactory, moins le choix : un panneau NE SE PREND PAS, il se lit.
    /// Rien ne sort de ce batiment — l'usine est une pure recreation, sans lien avec le
    /// reseau, decision du 3 septembre 2026 — donc aucun etat, aucun evenement, aucune
    /// sauvegarde. On marche sur un panneau, son nom s'affiche au HUD, on s'en va.
    ///
    /// Elle vit dans la scene Interiors, que le SceneRouter eteint quand on ressort. Qui la
    /// cherche depuis une autre couche doit donc l'inclure en FindObjectsInactive.Include ;
    /// mais on ne s'en sert que dedans, la couche allumee.
    /// </summary>
    public class SignCatalogue : MonoBehaviour
    {
        [Tooltip("Les cases ou les panneaux sont exposes, dans l'ordre de la planche.")]
        [SerializeField] private Vector2Int[] signCells;

        [Tooltip("Le nom de chaque panneau expose, dans le meme ordre.")]
        [SerializeField] private Sprite[] names;

        /// <summary>Nombre de panneaux exposes. Sert aux verifications.</summary>
        public int Count => signCells != null ? signCells.Length : 0;

        /// <summary>Le rang du panneau expose sur cette case, ou -1.</summary>
        public int IndexAt(Vector2Int cell)
        {
            if (signCells == null)
            {
                return -1;
            }

            for (int i = 0; i < signCells.Length; i++)
            {
                if (signCells[i] == cell)
                {
                    return i;
                }
            }

            return -1;
        }

        /// <summary>
        /// Le nom d'un rang, ou null si le rang n'existe pas. En silence : un cartel absent
        /// n'efface rien et ne casse pas une partie, il ne s'affiche simplement pas.
        /// </summary>
        public Sprite NameAt(int index)
        {
            if (names == null || index < 0 || index >= names.Length)
            {
                return null;
            }

            return names[index];
        }

        /// <summary>Le nom du panneau foule, ou null si l'on n'en foule aucun.</summary>
        public Sprite NameOn(Vector2Int cell)
        {
            return NameAt(IndexAt(cell));
        }
    }
}
