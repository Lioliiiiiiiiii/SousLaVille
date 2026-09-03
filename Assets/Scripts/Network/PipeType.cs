using UnityEngine;

namespace SousLaVille.Network
{
    /// <summary>
    /// Une famille de canalisation. Un seul type pour l'instant : les variantes n'auront
    /// d'interet qu'a l'usine a tuyaux, en phase 9, ou la resistance au gel et l'usure
    /// deviendront un choix.
    /// </summary>
    [CreateAssetMenu(fileName = "PipeType_", menuName = "Sous la Ville/Type de canalisation")]
    public class PipeType : ScriptableObject
    {
        [Tooltip("Nom interne. Jamais affiche au joueur : le jeu parle par pictogrammes.")]
        [SerializeField] private string displayName = "Standard";

        [Tooltip("Teinte du tuyau a l'ecran.")]
        [SerializeField] private Color tint = Color.white;

        [Tooltip("0 : gele des le premier hiver. 1 : ne gele jamais. Phase 5.")]
        [Range(0f, 1f)]
        [SerializeField] private float frostResistance;

        [Tooltip("Usure retiree a la condition a chaque saison, avant le facteur de saison.")]
        [SerializeField] private float wearPerSeason = 0.1f;

        public string DisplayName => displayName;
        public Color Tint => tint;
        public float FrostResistance => frostResistance;
        public float WearPerSeason => wearPerSeason;
    }
}
