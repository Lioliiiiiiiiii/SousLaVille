using SousLaVille.Buildings;
using UnityEngine;

namespace SousLaVille.World
{
    /// <summary>
    /// La plaque que porte une bouche d'egout. Elle demande a l'atelier ce qu'elle doit
    /// afficher, et retombe sur son allure d'usine tant qu'on ne lui a rien pose.
    ///
    /// Elle s'abonne dans OnEnable et se desabonne dans OnDisable, et se reapplique a chaque
    /// rallumage : c'est un composant d'une couche de jeu, et le SceneRouter eteint la racine
    /// de la couche inactive. Le piege a coute une correction en phase 1 et une en phase 4 ;
    /// c'est le meme remede que SeasonAmbience.
    /// </summary>
    public class ManholeCover : MonoBehaviour
    {
        [Tooltip("La case de cette bouche, telle que le plan du village la donne.")]
        [SerializeField] private Vector2Int cell;

        [SerializeField] private SpriteRenderer view;

        [Tooltip("L'allure d'usine, tant que le joueur n'a rien pose.")]
        [SerializeField] private Sprite defaultCover;

        private ManholeFactory factory;

        public Vector2Int Cell => cell;

        private void OnEnable()
        {
            Subscribe();
        }

        private void OnDisable()
        {
            if (factory != null)
            {
                factory.Changed -= Apply;
                factory = null;
            }
        }

        /// <summary>
        /// Resolution paresseuse : l'atelier vit dans la meme scene, mais l'ordre des OnEnable
        /// n'est garanti par rien. Tant qu'il manque, on retente.
        /// </summary>
        private void Update()
        {
            if (factory == null)
            {
                Subscribe();
            }
        }

        private void Subscribe()
        {
            if (factory != null)
            {
                return;
            }

            factory = FindAnyObjectByType<ManholeFactory>(FindObjectsInactive.Include);
            if (factory == null)
            {
                return;
            }

            factory.Changed += Apply;

            // Une plaque a pu etre posee pendant que la couche etait eteinte.
            Apply();
        }

        private void Apply()
        {
            if (view == null)
            {
                return;
            }

            ManholeCoverDefinition definition = factory != null ? factory.CoverFor(cell) : null;
            view.sprite = definition != null ? definition.Cover : defaultCover;
        }
    }
}
