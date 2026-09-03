using System.Collections.Generic;
using UnityEngine;

namespace SousLaVille.EditorTools
{
    /// <summary>
    /// Le plan des interieurs, ecrit a la main comme celui du village et celui du sous-sol.
    ///
    /// Une difference de forme, et une seule : ce plan n'est pas une grande grille de trente
    /// lignes, mais UNE GRILLE PAR PIECE, posee dans un creneau de la carte. Ajouter un
    /// batiment, c'est ajouter un bloc et un creneau, sans toucher aux pieces existantes.
    /// Une grande grille aurait oblige a rouvrir les lignes des voisins a chaque batiment.
    ///
    /// La carte fait 40x30 comme les deux autres couches, decoupee en six creneaux de 20 sur
    /// 10. Un creneau est exactement la vue de la camera, 320x180 a PPU 16 : la piece tient
    /// donc a l'ecran d'un seul tenant, sans rien faire defiler.
    ///
    /// Legende
    ///   #  mur (bloquant)     .  sol      D  porte, vers le village
    ///   C  plaque exposee     P  echantillon de tuyau      V  personnage
    ///
    /// C, P et V sont des marqueurs : le builder peint du sol dessous et pose un GameObject
    /// par-dessus. Les echantillons ne bloquent pas, on marche dessus pour les choisir ; le
    /// personnage, lui, bloque : on ne traverse pas quelqu'un, et debout sur lui on ne
    /// pourrait plus lui parler.
    ///
    /// Tout ce qui n'appartient a aucune piece est infranchissable : voir InteriorMap, qui
    /// refuse toute case hors piece plutot que de peindre six cents tuiles de mur qu'on ne
    /// verra jamais.
    /// </summary>
    public static class InteriorsLayout
    {
        public const int Width = 40;
        public const int Height = 30;

        public const int RoomWidth = 20;
        public const int RoomHeight = 10;

        public const char Wall = '#';
        public const char Floor = '.';
        public const char Door = 'D';
        public const char Cover = 'C';
        public const char PipeSample = 'P';
        public const char Villager = 'V';

        /// <summary>Une piece : son bloc dessine et le coin bas gauche de son creneau.</summary>
        public readonly struct Room
        {
            public Room(string name, Vector2Int origin, string[] rows)
            {
                Name = name;
                Origin = origin;
                Rows = rows;
            }

            /// <summary>Nom de la piece, pour l'objet de scene et les messages d'erreur.</summary>
            public string Name { get; }

            /// <summary>Coin bas gauche du creneau, en cases de la carte.</summary>
            public Vector2Int Origin { get; }

            /// <summary>Ligne 0 en haut, comme on lit une carte.</summary>
            public string[] Rows { get; }

            /// <summary>Bornes de la piece en cases, murs compris.</summary>
            public RectInt Bounds => new RectInt(Origin.x, Origin.y, RoomWidth, RoomHeight);
        }

        /// <summary>
        /// L'atelier des plaques. Huit plaques en deux rangees de quatre, espacees de quatre
        /// cases : le nom AMSTERDAM mesure 3,4 cases de large, et deux cartels voisins se
        /// chevaucheraient a moins. Le meme espacement qu'en phase 7, dans la cour.
        ///
        /// Les plaques sont en x = 3, 7, 11 et 15 plutot qu'en x = 2, 6, 10 et 14 : un cartel
        /// centre sur x = 2 deborderait sur le mur de gauche.
        ///
        /// L'artisan se tient au milieu, entre les plaques et la porte : on lui parle en
        /// entrant, sans avoir a le chercher, et sans qu'il barre le chemin.
        /// </summary>
        private static readonly Room CoverWorkshop = new Room(
            "Atelier des plaques",
            new Vector2Int(0, 20),
            new[]
            {
                "####################",
                "#..................#",
                "#..C...C...C...C...#",
                "#..................#",
                "#..................#",
                "#..C...C...C...C...#",
                "#..................#",
                "#........V.........#",
                "#..................#",
                "#########D##########",
            });

        /// <summary>
        /// L'usine a tuyaux. Trois echantillons seulement, largement espaces : au-dessus de
        /// chacun, le picto de la saison qu'il vainc, flocon ou feuille, et sous chacun son
        /// nom ecrit. Le standard ne vainc rien et n'a donc pas de picto, ce qui se voit.
        ///
        /// L'ouvrier se tient au meme endroit que l'artisan chez le voisin : les deux
        /// batiments s'apprennent une seule fois.
        /// </summary>
        private static readonly Room PipeWorks = new Room(
            "Usine a tuyaux",
            new Vector2Int(20, 20),
            new[]
            {
                "####################",
                "#..................#",
                "#..................#",
                "#..................#",
                "#....P....P....P...#",
                "#..................#",
                "#..................#",
                "#........V.........#",
                "#..................#",
                "#########D##########",
            });

        /// <summary>
        /// Les pieces du jeu. La phase 12 y ajoutera l'usine a panneaux, dans un des quatre
        /// creneaux encore libres.
        /// </summary>
        public static readonly Room[] Rooms = { CoverWorkshop, PipeWorks };

        /// <summary>
        /// Caractere de la case (x, y), ou le mur si la case n'appartient a aucune piece.
        /// y compte du bas vers le haut, comme les tilemaps d'Unity.
        /// </summary>
        public static char At(int x, int y)
        {
            foreach (Room room in Rooms)
            {
                int localX = x - room.Origin.x;
                int localY = y - room.Origin.y;

                if (localX < 0 || localX >= RoomWidth || localY < 0 || localY >= RoomHeight)
                {
                    continue;
                }

                return room.Rows[RoomHeight - 1 - localY][localX];
            }

            return Wall;
        }

        /// <summary>Vrai si la case appartient a une piece, murs compris.</summary>
        public static bool IsInsideARoom(int x, int y)
        {
            foreach (Room room in Rooms)
            {
                if (room.Bounds.Contains(new Vector2Int(x, y)))
                {
                    return true;
                }
            }

            return false;
        }

        /// <summary>Toutes les cases d'une piece portant un marqueur, balayees du bas vers le haut.</summary>
        public static List<Vector2Int> FindAll(Room room, char marker)
        {
            List<Vector2Int> cells = new List<Vector2Int>();

            for (int y = room.Origin.y; y < room.Origin.y + RoomHeight; y++)
            {
                for (int x = room.Origin.x; x < room.Origin.x + RoomWidth; x++)
                {
                    if (At(x, y) == marker)
                    {
                        cells.Add(new Vector2Int(x, y));
                    }
                }
            }

            return cells;
        }

        /// <summary>Marqueur attendu en un seul exemplaire dans une piece.</summary>
        public static Vector2Int FindSingle(Room room, char marker)
        {
            List<Vector2Int> cells = FindAll(room, marker);

            if (cells.Count != 1)
            {
                Debug.LogError($"[Sous la Ville] La pièce « {room.Name} » porte {cells.Count} " +
                               $"marqueur(s) « {marker} », il en faut exactement un.");
                return cells.Count > 0 ? cells[0] : Vector2Int.zero;
            }

            return cells[0];
        }

        /// <summary>
        /// Vrai si chaque piece fait bien dix lignes de vingt caracteres, tient dans la
        /// carte, ne chevauche aucune autre, et porte exactement une porte et un personnage.
        /// </summary>
        public static bool IsWellFormed()
        {
            for (int i = 0; i < Rooms.Length; i++)
            {
                Room room = Rooms[i];

                if (room.Rows.Length != RoomHeight)
                {
                    Debug.LogError($"[Sous la Ville] La pièce « {room.Name} » fait " +
                                   $"{room.Rows.Length} lignes, il en faut {RoomHeight}.");
                    return false;
                }

                for (int row = 0; row < room.Rows.Length; row++)
                {
                    if (room.Rows[row].Length != RoomWidth)
                    {
                        Debug.LogError($"[Sous la Ville] Ligne {row} de « {room.Name} » : " +
                                       $"{room.Rows[row].Length} caractères au lieu de {RoomWidth}.");
                        return false;
                    }
                }

                RectInt bounds = room.Bounds;
                if (bounds.xMin < 0 || bounds.yMin < 0 || bounds.xMax > Width || bounds.yMax > Height)
                {
                    Debug.LogError($"[Sous la Ville] La pièce « {room.Name} » sort de la carte.");
                    return false;
                }

                for (int other = 0; other < i; other++)
                {
                    if (bounds.Overlaps(Rooms[other].Bounds))
                    {
                        Debug.LogError($"[Sous la Ville] Les pièces « {room.Name} » et " +
                                       $"« {Rooms[other].Name} » se chevauchent.");
                        return false;
                    }
                }

                if (FindAll(room, Door).Count != 1)
                {
                    Debug.LogError($"[Sous la Ville] La pièce « {room.Name} » doit avoir " +
                                   "exactement une porte.");
                    return false;
                }

                if (FindAll(room, Villager).Count != 1)
                {
                    Debug.LogError($"[Sous la Ville] La pièce « {room.Name} » doit avoir " +
                                   "exactement un personnage.");
                    return false;
                }
            }

            return true;
        }
    }
}
