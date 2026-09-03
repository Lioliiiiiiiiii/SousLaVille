using System;
using SousLaVille.Core;
using UnityEngine;
using UnityEngine.Tilemaps;

namespace SousLaVille.World
{
    /// <summary>
    /// La carte des interieurs. Une seule carte porte toutes les pieces, cote a cote dans des
    /// creneaux, et chaque piece est close par ses murs : on ne passe pas de l'une a l'autre
    /// a pied, on ressort par la porte.
    ///
    /// Comme la surface, elle ne duplique aucune donnee de collision : un mur, c'est une tuile
    /// dans la tilemap bloquante.
    ///
    /// La seule chose qu'elle sait de plus, c'est ou commence et ou finit chaque piece, pour
    /// que la camera se borne a celle ou l'on se tient plutot qu'a la carte entiere. Sans
    /// cela, on verrait la piece voisine par-dessus le mur.
    /// </summary>
    public class InteriorMap : GridMap
    {
        [Tooltip("Sol et murs dessines. Purement decoratif.")]
        [SerializeField] private Tilemap ground;

        [Tooltip("Les murs. Une tuile ici veut dire : on ne passe pas.")]
        [SerializeField] private Tilemap blocking;

        [Tooltip("Les pieces, en cases. Cuites par le generateur depuis InteriorsLayout.")]
        [SerializeField] private RectInt[] rooms = Array.Empty<RectInt>();

        public override GameLayer Layer => GameLayer.Interior;

        /// <summary>Tilemap du sol, symetrique de celle de la surface.</summary>
        public Tilemap Ground => ground;

        /// <summary>Les pieces telles que le plan les donne. Sert aux verifications.</summary>
        public RectInt[] Rooms => rooms;

        /// <summary>
        /// Praticable si la case est dans une piece et ne porte pas de mur.
        ///
        /// Le test de piece n'est pas une precaution : hors des pieces, rien n'est peint du
        /// tout. Peindre six cents tuiles de mur qu'on ne verra jamais, seulement pour que
        /// « pas de tuile bloquante » suffise, serait du decor pour personne.
        /// </summary>
        public override bool IsWalkable(Vector2Int cell)
        {
            if (!Contains(cell) || RoomIndexAt(cell) < 0)
            {
                return false;
            }

            return !blocking.HasTile(new Vector3Int(cell.x, cell.y, 0));
        }

        /// <summary>Rang de la piece contenant cette case, ou -1.</summary>
        public int RoomIndexAt(Vector2Int cell)
        {
            for (int i = 0; i < rooms.Length; i++)
            {
                if (rooms[i].Contains(cell))
                {
                    return i;
                }
            }

            return -1;
        }

        /// <summary>
        /// Bornes de la piece contenant cette case. Une piece fait au plus la taille de la
        /// vue, donc la camera s'y centre sans rien faire defiler.
        ///
        /// Hors de toute piece, on retombe sur la carte entiere plutot que de rendre des
        /// bornes vides : le cas ne devrait pas arriver, et s'il arrive la camera montre
        /// quelque chose au lieu de partir a l'origine.
        /// </summary>
        public override Bounds WorldBoundsAround(Vector2Int cell)
        {
            int index = RoomIndexAt(cell);
            if (index < 0)
            {
                return WorldBounds;
            }

            RectInt room = rooms[index];
            Vector3 min = Grid.CellToWorld(new Vector3Int(room.xMin, room.yMin, 0));
            Vector3 max = Grid.CellToWorld(new Vector3Int(room.xMax, room.yMax, 0));

            Bounds bounds = new Bounds();
            bounds.SetMinMax(new Vector3(min.x, min.y, 0f), new Vector3(max.x, max.y, 0f));
            return bounds;
        }
    }
}
