using SousLaVille.Core;
using SousLaVille.Seasons;
using UnityEngine;
using UnityEngine.UI;

namespace SousLaVille.UI
{
    /// <summary>
    /// Le pictogramme de saison, en haut a gauche a cote du repere de couche. Quatre
    /// images, zero texte : une pousse, un soleil, une feuille, un flocon.
    ///
    /// Il vit dans Persistent, jamais eteinte, comme le repere de couche et les gouttes.
    /// </summary>
    public class SeasonIndicator : MonoBehaviour
    {
        [SerializeField] private Image icon;

        private SeasonSystem seasons;

        // Start et non Awake : le GameManager s'enregistre dans son propre Awake.
        private void Start()
        {
            if (GameManager.Instance == null || GameManager.Instance.Seasons == null)
            {
                Debug.LogError("[Sous la Ville] Picto de saison sans systeme de saisons.");
                return;
            }

            seasons = GameManager.Instance.Seasons;
            seasons.SeasonChanged += Apply;

            // L'ordre des Start n'est pas garanti : on lit la saison en cours plutot que
            // d'attendre l'evenement du demarrage, qui a pu passer avant.
            Apply(seasons.Current);
        }

        private void OnDestroy()
        {
            if (seasons != null)
            {
                seasons.SeasonChanged -= Apply;
            }
        }

        private void Apply(SeasonDefinition season)
        {
            if (icon == null)
            {
                return;
            }

            icon.sprite = season != null ? season.Picto : null;
            icon.enabled = icon.sprite != null;
        }
    }
}
