using SousLaVille.Core;
using UnityEngine;
using UnityEngine.Tilemaps;

namespace SousLaVille.World
{
    /// <summary>
    /// La carte du sous-sol. Meme repere et meme taille que le village : la case (8, 19) du
    /// sous-sol est exactement sous la case (8, 19) de la surface. C'est ce qui rend le
    /// portail comprehensible sans un mot d'explication.
    ///
    /// Au depart, presque tout est de la terre pleine. Seules les galeries deja creusees se
    /// parcourent ; la phase 3 ouvrira le reste.
    ///
    /// Le code n'est volontairement pas partage avec SurfaceMap : des la phase 3 le sous-sol
    /// devient mutable et la surface non. Trois lignes dupliquees valent mieux qu'un couplage
    /// premature.
    /// </summary>
    public class UndergroundMap : GridMap
    {
        [Tooltip("Terre et galeries. Purement decoratif.")]
        [SerializeField] private Tilemap ground;

        [Tooltip("Terre pleine. Une tuile ici veut dire : on ne passe pas, il faudra creuser.")]
        [SerializeField] private Tilemap blocking;

        public override GameLayer Layer => GameLayer.Underground;

        /// <summary>Tilemap du sol. La phase 3 y peindra le sol des galeries creusees.</summary>
        public Tilemap Ground => ground;

        /// <summary>Tilemap de la terre pleine. Creuser, c'est retirer une tuile d'ici.</summary>
        public Tilemap Blocking => blocking;

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
