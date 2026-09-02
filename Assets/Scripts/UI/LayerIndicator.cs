using SousLaVille.Core;
using UnityEngine;
using UnityEngine.UI;

namespace SousLaVille.UI
{
    /// <summary>
    /// Le repere de couche, en haut a gauche : un soleil en surface, une echelle en dessous.
    /// Aucun texte, aucune legende, conformement a CLAUDE.md.
    ///
    /// Pas de mini-carte : elle serait vide de sens tant que le reseau n'existe pas, et
    /// demanderait une legende.
    /// </summary>
    public class LayerIndicator : MonoBehaviour
    {
        [SerializeField] private Image icon;
        [SerializeField] private Sprite surfaceIcon;
        [SerializeField] private Sprite undergroundIcon;

        private SceneRouter router;

        // Start et non Awake : le GameManager s'enregistre dans son propre Awake.
        private void Start()
        {
            if (GameManager.Instance == null || GameManager.Instance.Router == null)
            {
                Debug.LogError("[Sous la Ville] Repere de couche sans GameManager.");
                return;
            }

            router = GameManager.Instance.Router;
            router.LayerChanged += OnLayerChanged;
            OnLayerChanged(router.CurrentLayer);
        }

        private void OnDestroy()
        {
            if (router != null)
            {
                router.LayerChanged -= OnLayerChanged;
            }
        }

        private void OnLayerChanged(GameLayer layer)
        {
            if (icon == null)
            {
                return;
            }

            icon.sprite = layer == GameLayer.Underground ? undergroundIcon : surfaceIcon;
        }
    }
}
