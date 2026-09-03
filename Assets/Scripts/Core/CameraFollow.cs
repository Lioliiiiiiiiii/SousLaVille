using SousLaVille.World;
using UnityEngine;

namespace SousLaVille.Core
{
    /// <summary>
    /// Suivi de camera, borne aux limites de la carte active : le joueur ne voit jamais le
    /// vide autour du village.
    ///
    /// Aucun lissage. Un lissage se bat avec le pixel snapping de la Pixel Perfect Camera
    /// et produit du tremblement. A rediscuter une fois le rendu vu.
    ///
    /// Fichier hors liste CLAUDE.md, comme Bootstrapper en phase 0 : il rejoint Core avec
    /// les autres services globaux.
    /// </summary>
    public class CameraFollow : MonoBehaviour
    {
        [Tooltip("Le personnage. Cable par PersistentSceneBuilder.")]
        [SerializeField] private Transform target;

        [Tooltip("Demi-vue en unites monde : 320 x 180 a PPU 16 donnent 20 x 11,25 unites.")]
        [SerializeField] private Vector2 halfView = new Vector2(10f, 5.625f);

        [Tooltip("Distance de la camera au plan de jeu.")]
        [SerializeField] private float depth = -10f;

        private GridMap map;

        private void LateUpdate()
        {
            if (target == null || !ResolveMap())
            {
                return;
            }

            // Les bornes autour de la cible, et non celles de la carte : une carte decoupee
            // en pieces closes, comme celle des interieurs, borne la vue a la piece ou se
            // tient le personnage. Sur la surface et sous terre, c'est la carte entiere.
            Bounds bounds = map.WorldBoundsAround(map.WorldToCell(target.position));

            transform.position = new Vector3(
                ClampAxis(target.position.x, bounds.min.x, bounds.max.x, halfView.x),
                ClampAxis(target.position.y, bounds.min.y, bounds.max.y, halfView.y),
                depth);
        }

        /// <summary>
        /// Borne un axe a l'interieur de la carte. Si la carte est plus etroite que la vue,
        /// la camera se centre : mieux vaut du vide symetrique qu'un bord colle.
        /// </summary>
        private static float ClampAxis(float value, float min, float max, float half)
        {
            float low = min + half;
            float high = max - half;

            if (low > high)
            {
                return (min + max) * 0.5f;
            }

            return Mathf.Clamp(value, low, high);
        }

        /// <summary>Meme resolution que PlayerController : la couche eteinte ne repond pas.</summary>
        private bool ResolveMap()
        {
            if (map != null && map.isActiveAndEnabled)
            {
                return true;
            }

            map = FindAnyObjectByType<GridMap>();
            return map != null;
        }
    }
}
