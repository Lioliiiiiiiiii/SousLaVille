using SousLaVille.World;
using UnityEngine;

namespace SousLaVille.Player
{
    /// <summary>
    /// Le cadre pose sur la case regardee. C'est la phase 3 qui le rend necessaire : Espace
    /// agit pour la premiere fois a distance, il faut voir sur quoi.
    ///
    /// Visible sous terre seulement : en surface, la case regardee ne sert a rien et un cadre
    /// qui ne fait rien serait du bruit.
    ///
    /// Fichier hors liste CLAUDE.md, range avec le personnage dont il suit le regard.
    /// </summary>
    public class TargetCursor : MonoBehaviour
    {
        [SerializeField] private PlayerController player;
        [SerializeField] private SpriteRenderer view;

        private void LateUpdate()
        {
            GridMap map = player != null ? player.Map : null;
            bool visible = map is UndergroundMap;

            if (view.enabled != visible)
            {
                view.enabled = visible;
            }

            if (!visible)
            {
                return;
            }

            transform.position = map.CellToWorld(player.FacingCell);
        }
    }
}
