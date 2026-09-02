using UnityEngine;
using UnityEngine.UI;

namespace SousLaVille.UI
{
    /// <summary>
    /// Le fondu au noir des changements de couche. Court, 0,15 s par sens : assez pour dire
    /// « je change d'endroit », pas assez pour faire attendre.
    ///
    /// Temps non mis a l'echelle : un fondu ne doit pas dependre d'un eventuel ralenti.
    /// </summary>
    public class ScreenFader : MonoBehaviour
    {
        [Tooltip("Image noire plein ecran. Eteinte tant qu'il n'y a pas de transition.")]
        [SerializeField] private Image veil;

        [Tooltip("Duree d'un sens, en secondes.")]
        [SerializeField] private float duration = 0.15f;

        /// <summary>Ecran noir a la fin de l'attente.</summary>
        public async Awaitable FadeToBlackAsync()
        {
            await FadeAsync(0f, 1f);
        }

        /// <summary>Ecran rendu au joueur a la fin de l'attente.</summary>
        public async Awaitable FadeFromBlackAsync()
        {
            await FadeAsync(1f, 0f);
        }

        private async Awaitable FadeAsync(float from, float to)
        {
            if (veil == null)
            {
                return;
            }

            veil.enabled = true;
            SetAlpha(from);

            float elapsed = 0f;
            while (elapsed < duration)
            {
                elapsed += Time.unscaledDeltaTime;
                SetAlpha(Mathf.Lerp(from, to, Mathf.Clamp01(elapsed / duration)));
                await Awaitable.NextFrameAsync();
            }

            SetAlpha(to);

            // Un voile transparent continuerait a se dessiner pour rien.
            veil.enabled = to > 0f;
        }

        private void SetAlpha(float alpha)
        {
            Color color = veil.color;
            color.a = alpha;
            veil.color = color;
        }
    }
}
