using SousLaVille.Core;
using UnityEngine;
using UnityEngine.Rendering.Universal;

namespace SousLaVille.Seasons
{
    /// <summary>
    /// La couleur du ciel. Pose sur la racine de la scene Surface, ce composant teinte la
    /// lumiere globale de sa propre couche a chaque changement de saison.
    ///
    /// C'est le signal le plus fort du jeu et il ne coute rien : la Light2D globale existe
    /// depuis la phase 0. Le sous-sol garde sa lumiere : les saisons se voient dessus, se
    /// subissent dessous.
    ///
    /// Il vit dans une couche de jeu, donc il s'abonne dans OnEnable et se desabonne dans
    /// OnDisable : le SceneRouter eteint la racine de la couche inactive, et Unity rappelle
    /// OnEnable sans repasser par Awake apres un rechargement de domaine.
    /// </summary>
    public class SeasonAmbience : MonoBehaviour
    {
        [Tooltip("La lumiere globale de cette couche. Cablee par le generateur de scene.")]
        [SerializeField] private Light2D globalLight;

        private SeasonSystem seasons;

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

        /// <summary>
        /// Resolution paresseuse : Persistent est chargee avant Surface, mais un jour ce
        /// pourrait etre l'inverse. Tant que le systeme manque, on retente ; des qu'il est
        /// la, on s'abonne et Update n'a plus rien a faire.
        /// </summary>
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

            // La saison a pu changer pendant que la couche etait eteinte : on se remet a
            // la bonne couleur des le rallumage, sans attendre le prochain tick.
            Apply(seasons.Current);
        }

        private void Apply(SeasonDefinition season)
        {
            if (globalLight == null || season == null)
            {
                return;
            }

            globalLight.color = season.LightColor;
        }
    }
}
