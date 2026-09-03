using UnityEngine;

namespace SousLaVille.Network
{
    /// <summary>
    /// Une famille de canalisation. Trois depuis la phase 9b, une par menace de saison :
    /// Standard, Isole qui ne gele pas, Grillage que les feuilles ne bouchent pas.
    ///
    /// Chaque type repond a une saison, et une seule. L'hiver reste un probleme pour le
    /// grillage, l'automne pour l'isole : choisir, c'est d'abord comprendre ce qui coupe la
    /// maison. Un type qui resisterait a tout supprimerait le choix.
    ///
    /// L'usure est la meme pour les trois : un tuyau qui ne s'use pas rendrait la cle de
    /// reparation inutile, et c'est ce geste qui garde le bassin bas et les maisons
    /// desservies.
    ///
    /// C'est un catalogue, comme ManholeCoverDefinition et SeasonDefinition : le type porte
    /// son image d'echantillon, son nom ecrit et le picto de la saison qu'il vainc.
    /// </summary>
    [CreateAssetMenu(fileName = "PipeType_", menuName = "Sous la Ville/Type de canalisation")]
    public class PipeType : ScriptableObject
    {
        [Tooltip("Nom interne. Le nom AFFICHE est une image, dessinee par PixelFont.")]
        [SerializeField] private string displayName = "Standard";

        [Tooltip("Teinte du tuyau a l'ecran. Inutilise : la couleur est au seul service de l'etat.")]
        [SerializeField] private Color tint = Color.white;

        [Tooltip("0 : gele des le premier hiver. 1 : ne gele jamais.")]
        [Range(0f, 1f)]
        [SerializeField] private float frostResistance;

        [Tooltip("0 : se bouche a la premiere feuille. 1 : jamais.")]
        [Range(0f, 1f)]
        [SerializeField] private float leafResistance;

        [Tooltip("Usure retiree a la condition a chaque saison, avant le facteur de saison.")]
        [SerializeField] private float wearPerSeason = 0.1f;

        [Tooltip("Rang du motif dans la famille de tuiles. Le motif dit le type, jamais la couleur.")]
        [SerializeField] private int patternIndex;

        [Tooltip("L'echantillon expose a l'usine. C'est aussi le picto de pose.")]
        [SerializeField] private Sprite sample;

        [Tooltip("Le nom ecrit sous l'echantillon, dessine par PixelFont.")]
        [SerializeField] private Sprite nameImage;

        [Tooltip("Le picto de la saison que ce type vainc. Aucun pour le standard.")]
        [SerializeField] private Sprite defeatedSeasonIcon;

        public string DisplayName => displayName;
        public Color Tint => tint;

        /// <summary>Probabilite de TENIR au gel : 0 gele toujours, 1 jamais.</summary>
        public float FrostResistance => frostResistance;

        /// <summary>Probabilite de TENIR aux feuilles, jumelle exacte de la resistance au gel.</summary>
        public float LeafResistance => leafResistance;

        public float WearPerSeason => wearPerSeason;

        /// <summary>
        /// Le motif, pas la teinte. La couleur dit deja l'etat depuis la phase 5 : gele,
        /// bouche, trop abime, porteur d'eau, sain. Un type qui prendrait une couleur
        /// entrerait en concurrence avec ces cinq-la.
        /// </summary>
        public int PatternIndex => patternIndex;

        public Sprite Sample => sample;
        public Sprite NameImage => nameImage;
        public Sprite DefeatedSeasonIcon => defeatedSeasonIcon;
    }
}
