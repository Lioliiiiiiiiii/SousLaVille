using SousLaVille.Core;
using UnityEngine;

namespace SousLaVille.World
{
    /// <summary>
    /// Base commune aux cartes des deux couches. Elle porte la grille, ses bornes et les
    /// conversions case vers monde ; les sous-classes disent seulement ce qui est
    /// franchissable.
    ///
    /// Fichier hors liste CLAUDE.md : il donne au joueur et a la camera un type unique a
    /// interroger, ce qui evite de reecrire leur resolution de carte quand UndergroundMap
    /// arrivera en phase 2.
    /// </summary>
    public abstract class GridMap : MonoBehaviour
    {
        [Tooltip("Grille de la couche. Taille de case 1x1, posee a l'origine.")]
        [SerializeField] private Grid grid;

        [Tooltip("Taille de la carte en cases. L'origine est toujours la case (0, 0).")]
        [SerializeField] private Vector2Int mapSize = new Vector2Int(40, 30);

        /// <summary>Grille Unity de la couche.</summary>
        public Grid Grid => grid;

        /// <summary>Bornes de la carte en cases. Origine (0, 0), exclusive en haut.</summary>
        public BoundsInt CellBounds => new BoundsInt(0, 0, 0, mapSize.x, mapSize.y, 1);

        /// <summary>Bornes de la carte en unites monde, coins de la grille compris.</summary>
        public Bounds WorldBounds
        {
            get
            {
                BoundsInt cells = CellBounds;
                Vector3 min = grid.CellToWorld(cells.min);
                Vector3 max = grid.CellToWorld(new Vector3Int(cells.xMax, cells.yMax, 0));

                Bounds bounds = new Bounds();
                bounds.SetMinMax(new Vector3(min.x, min.y, 0f), new Vector3(max.x, max.y, 0f));
                return bounds;
            }
        }

        /// <summary>
        /// Couche portee par cette carte. Le personnage s'en sert pour choisir sa famille
        /// de Sorting Layers : sans cela, descendre le laisserait sur la famille Surface,
        /// que la lumiere globale du sous-sol n'eclaire pas, et il rendrait tout noir.
        /// </summary>
        public abstract GameLayer Layer { get; }

        /// <summary>
        /// Bornes auxquelles la camera doit se tenir quand le personnage est sur cette case.
        /// Par defaut, la carte entiere ; une carte decoupee en pieces closes, comme celle
        /// des interieurs, rend les bornes de la piece courante.
        ///
        /// C'est le seul endroit ou la notion de piece sort d'InteriorMap : la camera demande
        /// des bornes autour de sa cible et n'a rien a savoir de plus.
        /// </summary>
        public virtual Bounds WorldBoundsAround(Vector2Int cell)
        {
            return WorldBounds;
        }

        /// <summary>Vrai si le personnage peut se tenir sur cette case.</summary>
        public abstract bool IsWalkable(Vector2Int cell);

        /// <summary>Centre monde d'une case. C'est la que se pose un transform.</summary>
        public Vector3 CellToWorld(Vector2Int cell)
        {
            Vector3 center = grid.GetCellCenterWorld(new Vector3Int(cell.x, cell.y, 0));
            return new Vector3(center.x, center.y, 0f);
        }

        /// <summary>Case contenant un point du monde.</summary>
        public Vector2Int WorldToCell(Vector3 world)
        {
            Vector3Int cell = grid.WorldToCell(world);
            return new Vector2Int(cell.x, cell.y);
        }

        /// <summary>Vrai si la case tombe dans les bornes de la carte.</summary>
        public bool Contains(Vector2Int cell)
        {
            return CellBounds.Contains(new Vector3Int(cell.x, cell.y, 0));
        }
    }
}
