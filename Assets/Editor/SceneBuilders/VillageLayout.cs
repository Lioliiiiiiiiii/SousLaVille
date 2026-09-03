using System.Collections.Generic;
using UnityEngine;

namespace SousLaVille.EditorTools
{
    /// <summary>
    /// Le plan du village, ecrit a la main. Trente lignes de quarante caracteres.
    ///
    /// Ce fichier vit dans l'assembly Editor et rien de plus : le runtime ne lit jamais la
    /// carte ASCII, il interroge les tilemaps. Le procedural est reserve au labyrinthe de
    /// la phase 11.
    ///
    /// Legende
    ///   .  herbe              #  chemin             P  dalle du parc
    ///   S  sol station        H  haie (bloquant)    B  batiment station (bloquant)
    ///   M  bouche d'egout     X  depart du joueur   T  entree de la station
    ///   A  maison (bloquant)  W  pave de l'atelier  C  plaque exposee
    ///
    /// M, X, T, A et C sont des marqueurs : le builder peint le sol correspondant dessous et
    /// pose un GameObject par-dessus. Une maison est en plus bloquante : on passe devant,
    /// pas dedans. Une plaque exposee ne bloque pas : on marche dessus pour la choisir.
    ///
    /// La cour de l'atelier occupe seize cases sur six, en haut a droite, dans la zone
    /// d'herbe restee libre. Ses huit plaques sont espacees de quatre cases : le nom
    /// AMSTERDAM mesure 3,4 cases de large, et deux cartels voisins se chevaucheraient a
    /// moins.
    /// </summary>
    public static class VillageLayout
    {
        public const int Width = 40;
        public const int Height = 30;

        public const char Grass = '.';
        public const char Road = '#';
        public const char Park = 'P';
        public const char PlantFloor = 'S';
        public const char Hedge = 'H';
        public const char PlantWall = 'B';
        public const char Manhole = 'M';
        public const char PlayerStart = 'X';
        public const char PlantInlet = 'T';
        public const char House = 'A';
        public const char Workshop = 'W';
        public const char Cover = 'C';

        /// <summary>Ligne 0 en haut, comme on lit la carte. La conversion en case se fait dans At.</summary>
        private static readonly string[] Rows =
        {
            "HHHHHHHHHHHHHHHHHHHHHHHHHHHHHHHHHHHHHHHH",
            "H......................................H",
            "H.BBBBBBBBB..........WWWWWWWWWWWWWWWW..H",
            "H.BSSSSSSSB..........WCWWWCWWWCWWWCWW..H",
            "H.BSSSTSSSB....HHH...WWWWWWWWWWWWWWWW..H",
            "H.BSSSSSSSB....HHH...WWWWWWWWWWWWWWWW..H",
            "H.BSSSSSSSB..........WCWWWCWWWCWWWCWW..H",
            "H.BBBBSBBBB..........WWWWWWWWWWWWWWWW..H",
            "H.....#................................H",
            "H....A#................................H",
            "H.....##M######################........H",
            "H...........#.......#.......#..........H",
            "H...........#A..PPPPPPPPP..A#..........H",
            "H...........#...PPPPPPPPP...#....HHH...H",
            "H...........##X#PPPPPPPPP####....HHH...H",
            "H...........#...PPPPPPPPP...#..........H",
            "H...........#...PPPPPPPPP...#..........H",
            "H...........#...PPPPPPPPP...#..........H",
            "H...........#.......#.......#..........H",
            "H...........########M########A.........H",
            "H...........................#..........H",
            "H...........................#..........H",
            "H.....HHH...................#..........H",
            "H.....HHH...................#..........H",
            "H...........................#####M#....H",
            "H.................................A....H",
            "H......................................H",
            "H......................................H",
            "H......................................H",
            "HHHHHHHHHHHHHHHHHHHHHHHHHHHHHHHHHHHHHHHH",
        };

        /// <summary>
        /// Caractere de la case (x, y). y compte du bas vers le haut, comme les tilemaps
        /// d'Unity, alors que la carte se lit du haut vers le bas.
        /// </summary>
        public static char At(int x, int y)
        {
            if (x < 0 || x >= Width || y < 0 || y >= Height)
            {
                return Hedge;
            }

            return Rows[Height - 1 - y][x];
        }

        /// <summary>Sol a peindre sous un marqueur. Les marqueurs ne sont pas des tuiles.</summary>
        public static char GroundAt(int x, int y)
        {
            char cell = At(x, y);

            switch (cell)
            {
                case Manhole:
                case PlayerStart:
                    return Road;
                case Cover:
                    // Une plaque exposee est un marqueur : le pave de l'atelier passe dessous.
                    return Workshop;
                case House:
                    // De l'herbe sous la maison : la tuile bloquante se pose par-dessus.
                    return Grass;
                case PlantInlet:
                    return PlantFloor;
                default:
                    return cell;
            }
        }

        /// <summary>Toutes les cases portant un marqueur donne, balayees du bas vers le haut.</summary>
        public static List<Vector2Int> FindAll(char marker)
        {
            List<Vector2Int> cells = new List<Vector2Int>();

            for (int y = 0; y < Height; y++)
            {
                for (int x = 0; x < Width; x++)
                {
                    if (At(x, y) == marker)
                    {
                        cells.Add(new Vector2Int(x, y));
                    }
                }
            }

            return cells;
        }

        /// <summary>Marqueur attendu en un seul exemplaire. Rend (0, 0) et signale s'il manque.</summary>
        public static Vector2Int FindSingle(char marker)
        {
            List<Vector2Int> cells = FindAll(marker);

            if (cells.Count != 1)
            {
                Debug.LogError($"[Sous la Ville] Le plan du village porte {cells.Count} marqueur(s) " +
                               $"« {marker} », il en faut exactement un.");
                return cells.Count > 0 ? cells[0] : Vector2Int.zero;
            }

            return cells[0];
        }

        /// <summary>Vrai si la carte fait bien 30 lignes de 40 caracteres.</summary>
        public static bool IsWellFormed()
        {
            if (Rows.Length != Height)
            {
                Debug.LogError($"[Sous la Ville] Le plan du village fait {Rows.Length} lignes, " +
                               $"il en faut {Height}.");
                return false;
            }

            for (int row = 0; row < Rows.Length; row++)
            {
                if (Rows[row].Length != Width)
                {
                    Debug.LogError($"[Sous la Ville] Ligne {row} du plan : {Rows[row].Length} " +
                                   $"caractères au lieu de {Width}.");
                    return false;
                }
            }

            return true;
        }
    }
}
