using SousLaVille.Core;
using UnityEngine;

namespace SousLaVille.Seasons
{
    /// <summary>
    /// UN SPRITE QUI CHANGE AVEC LA SAISON, phase 18g : la cime d'un arbre, le toit d'une maison.
    /// Quatre images, une par rang de saison ; une case vide garde l'image en place. Le
    /// changement se fait sur SeasonChanged, jamais par image : cent vingt-six arbres qui
    /// ecoutent un evenement ne coutent rien tant qu'il ne se leve pas.
    ///
    /// S'abonne dans OnEnable et se desabonne dans OnDisable, comme SeasonAmbience.
    /// </summary>
    public class SeasonalSprite : MonoBehaviour
    {
        [Tooltip("Le renderer a habiller.")]
        [SerializeField] private SpriteRenderer target;

        [Tooltip("Une image par saison, dans l'ordre du cycle : printemps, ete, automne, hiver.")]
        [SerializeField] private Sprite[] bySeason;

        private SeasonSystem seasons;

        /// <summary>Cable par code, pour les maisons que le spawner cree au reveil.</summary>
        public void Initialize(SpriteRenderer renderer, Sprite[] sprites)
        {
            target = renderer;
            bySeason = sprites;
            Subscribe();
        }

        private void OnEnable()
        {
            Subscribe();
        }

        private void OnDisable()
        {
            if (seasons != null)
            {
                seasons.SeasonChanged -= Apply;
                seasons = null;
            }
        }

        private void Update()
        {
            if (seasons == null)
            {
                Subscribe();
            }
        }

        private void Subscribe()
        {
            if (seasons != null || GameManager.Instance == null)
            {
                return;
            }

            seasons = GameManager.Instance.Seasons;
            if (seasons == null)
            {
                return;
            }

            seasons.SeasonChanged += Apply;
            Apply(seasons.Current);
        }

        private void Apply(SeasonDefinition season)
        {
            if (target == null || bySeason == null || seasons == null)
            {
                return;
            }

            int index = seasons.CurrentIndex;
            if (index < 0 || index >= bySeason.Length || bySeason[index] == null)
            {
                return;
            }

            target.sprite = bySeason[index];
        }
    }
}
