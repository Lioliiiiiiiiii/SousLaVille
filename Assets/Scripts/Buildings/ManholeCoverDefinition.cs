using UnityEngine;

namespace SousLaVille.Buildings
{
    /// <summary>
    /// Une plaque d'egout du catalogue : son image, et l'image de son nom.
    ///
    /// Meme patron que SeasonDefinition : une famille d'objets decrite en donnees, produite
    /// par le menu « Creer les ScriptableObjects ». C'est ce patron que le catalogue de
    /// panneaux de la phase 12 reprendra.
    ///
    /// Le nom est une image et non une chaine affichee : une police TTF sortirait de la
    /// grille de pixels. Voir PixelFont, cote Editor.
    /// </summary>
    [CreateAssetMenu(fileName = "Cover_", menuName = "Sous la Ville/Plaque d'égout")]
    public class ManholeCoverDefinition : ScriptableObject
    {
        [Tooltip("Nom de la ville. Sert a nommer l'asset ; c'est l'image qui s'affiche.")]
        [SerializeField] private string displayName = "Plaque";

        [Tooltip("L'image 16x16 qui se pose sur la bouche. Le catalogue l'affiche agrandie.")]
        [SerializeField] private Sprite cover;

        [Tooltip("Le nom, dessine en pixels, affiche sous la plaque dans l'atelier.")]
        [SerializeField] private Sprite nameImage;

        public string DisplayName => displayName;
        public Sprite Cover => cover;
        public Sprite NameImage => nameImage;
    }
}
