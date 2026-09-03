using UnityEngine;

namespace SousLaVille.Seasons
{
    /// <summary>
    /// Une saison decrite en donnees, pas en code. Ajouter une saison ou changer un effet
    /// ne demande pas de recompiler.
    ///
    /// Les quatre assets sont produits par le menu « Sous La Ville/Creer les
    /// ScriptableObjects » : reproductible et versionnable, comme les scenes.
    /// </summary>
    [CreateAssetMenu(fileName = "Season_", menuName = "Sous la Ville/Saison")]
    public class SeasonDefinition : ScriptableObject
    {
        [Tooltip("Nom interne. Jamais affiche au joueur : le jeu parle par pictogrammes.")]
        [SerializeField] private string displayName = "Saison";

        [Tooltip("Pictogramme affiche au HUD. Le seul mot de la saison.")]
        [SerializeField] private Sprite picto;

        [Tooltip("Couleur de la lumiere globale de la surface pendant cette saison.")]
        [SerializeField] private Color lightColor = Color.white;

        [Tooltip("Profondeur jusqu'a laquelle les tuyaux gelent. 0 : aucun gel.")]
        [Range(0, 3)]
        [SerializeField] private int freezeMaxDepth;

        [Tooltip("Probabilite qu'un tuyau peu profond se bouche. 0 : aucun bouchon.")]
        [Range(0f, 1f)]
        [SerializeField] private float clogChance;

        [Tooltip("Usure de la saison, multipliee par PipeType.WearPerSeason.")]
        [Range(0f, 3f)]
        [SerializeField] private float wearMultiplier = 1f;

        [Tooltip("Cette saison degele tout ce qui etait gele.")]
        [SerializeField] private bool thaws;

        [Tooltip("Eau de pluie apportee au reseau par la saison, en unites. L'automne en apporte le plus.")]
        [Min(0)]
        [SerializeField] private int rainVolume;

        public string DisplayName => displayName;
        public Sprite Picto => picto;
        public Color LightColor => lightColor;

        /// <summary>1 gele les tuyaux peu profonds, 0 n'en gele aucun.</summary>
        public int FreezeMaxDepth => freezeMaxDepth;

        public float ClogChance => clogChance;
        public float WearMultiplier => wearMultiplier;

        /// <summary>Le printemps degele : rien n'est jamais perdu pour de bon.</summary>
        public bool Thaws => thaws;

        /// <summary>
        /// La pluie de la saison, en unites d'eau. Une donnee de saison comme le gel et
        /// l'usure : changer l'equilibre ne demande pas de recompiler.
        /// </summary>
        public int RainVolume => rainVolume;
    }
}
