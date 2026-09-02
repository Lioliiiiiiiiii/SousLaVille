using UnityEngine;
using UnityEngine.Tilemaps;

namespace SousLaVille.World
{
    /// <summary>
    /// La carte du village. Aucune donnee de collision dupliquee : la carte de collision,
    /// c'est la tilemap bloquante. Une haie posee dans l'editeur arrete le personnage sans
    /// qu'aucun tableau n'ait a etre remis a jour.
    /// </summary>
    public class SurfaceMap : GridMap
    {
        [Tooltip("Herbe, chemins, dalles. Purement decoratif.")]
        [SerializeField] private Tilemap ground;

        [Tooltip("Haies et batiments. Une tuile ici veut dire : on ne passe pas.")]
        [SerializeField] private Tilemap blocking;

        /// <summary>Tilemap du sol, utile aux phases suivantes pour lire le type de terrain.</summary>
        public Tilemap Ground => ground;

        public override bool IsWalkable(Vector2Int cell)
        {
            if (!Contains(cell))
            {
                return false;
            }

            return !blocking.HasTile(new Vector3Int(cell.x, cell.y, 0));
        }
    }
}
