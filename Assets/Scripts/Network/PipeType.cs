using UnityEngine;

namespace SousLaVille.Network
{
    /// <summary>
    /// Une famille de canalisation. Un seul type en phase 3 : les variantes n'auront
    /// d'interet qu'avec le gel, en phase 5.
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

        [Tooltip("Usure retiree a la condition a chaque saison. Phase 5.")]
        [SerializeField] private float wearPerSeason = 0.05f;

        public string DisplayName => displayName;
        public Color Tint => tint;
        public float FrostResistance => frostResistance;
        public float WearPerSeason => wearPerSeason;
    }
}
