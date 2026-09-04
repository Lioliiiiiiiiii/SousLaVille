using System.Collections.Generic;
using UnityEngine;

namespace SousLaVille.EditorTools
{
    /// <summary>
    /// Le plan du village, ecrit a la main. Trente lignes de quarante caracteres.
    ///
    /// Ce fichier vit dans l'assembly Editor et rien de plus : le runtime ne lit jamais la
    /// carte ASCII, il interroge les tilemaps. (La phase 1 annoncait ici du procedural pour
    /// le labyrinthe de la phase 11 : il a finalement ete ecrit a la main comme le reste.)
    ///
    /// Legende
    ///   .  herbe              #  chemin             P  dalle du parc
    ///   S  sol station        H  haie (bloquant)    B  batiment station (bloquant)
    ///   M  bouche d'egout     X  depart du joueur   T  entree de la station
    ///   A  maison (bloquant)  F  facade de l'atelier (bloquant)   D  porte de l'atelier
    ///   G  facade de l'usine a tuyaux (bloquant)                   E  sa porte
    ///   O  fontaine du parc (bloquant)
    ///
    /// M, X, T, A, F, D, G et E sont des marqueurs : le builder peint le sol correspondant dessous
    /// et pose un GameObject par-dessus. Une maison et une facade sont en plus bloquantes :
    /// on passe devant, pas dedans. Une porte ne bloque pas : on marche dessus et Espace
    /// fait entrer, exactement comme sur une bouche d'egout.
    ///
    /// PHASE 9A. La cour pavee de l'atelier, seize cases sur six a ciel ouvert, a disparu :
    /// l'atelier est devenu un batiment dans lequel on entre, et ses huit plaques sont
    /// passees a l'interieur, dans la scene Interiors. Il ne reste ici que sa facade et sa
    /// porte. PHASE 9B : l'usine a tuyaux prend la place laissee libre a sa droite, sur le
    /// meme patron.
    ///
    /// PHASE 11. Le parc, neuf cases sur six qui n'avaient jamais rien porte, devient un
    /// LABYRINTHE DE HAIES de treize sur sept, avec la fontaine en son centre. Il ne mange que
    /// de l'herbe : ni la station, ni les bosquets, ni les maisons, ni les bouches, ni le
    /// depart, ni les facades n'ont bouge d'un caractere depuis la phase 1.
    ///
    /// Le trace est une spirale a deux anneaux, ecrit a la main et VERIFIE SOLVABLE PAR CALCUL
    /// AVANT D'ETRE POSE, la methode des cretes de la phase 4. Ses quatre entrees existaient
    /// deja depuis la phase 1, une au milieu de chaque cote ; elles menent a la fontaine en
    /// 11, 21, 18 et 10 pas. Aucune case du parc n'est orpheline : on ressort toujours.
    ///
    /// Ecrit a la main et non engendre. Le commentaire ci-dessous annoncait l'inverse en
    /// phase 1 ; la pratique du projet a tranche depuis, et un plan engendre ne se verifie
    /// plus une fois pour toutes.
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
        public const char Facade = 'F';
        public const char Door = 'D';
        public const char PipeFacade = 'G';
        public const char PipeDoor = 'E';
        public const char Fountain = 'O';

        /// <summary>Les facades des batiments, dans l'ordre des pieces d'InteriorsLayout.</summary>
        public static readonly char[] Facades = { Facade, PipeFacade };

        /// <summary>Les portes des batiments, dans le meme ordre.</summary>
        public static readonly char[] Doors = { Door, PipeDoor };

        /// <summary>Ligne 0 en haut, comme on lit la carte. La conversion en case se fait dans At.</summary>
        private static readonly string[] Rows =
        {
            "HHHHHHHHHHHHHHHHHHHHHHHHHHHHHHHHHHHHHHHH",
            "H......................................H",
            "H.BBBBBBBBB...........FFFF....GGGG.....H",
            "H.BSSSSSSSB...........FFFF....GGGG.....H",
            "H.BSSSTSSSB....HHH.....D.......E.......H",
            "H.BSSSSSSSB....HHH.....................H",
            "H.BSSSSSSSB............................H",
            "H.BBBBSBBBB............................H",
            "H.....#................................H",
            "H....A#................................H",
            "H.....##M######################........H",
            "H...........#.HHHHHHPHHHHHH.#..........H",
            "H...........#AHPPPPPPPPPPPHA#..........H",
            "H...........#.HPHHHHHHHHHPH.#....HHH...H",
            "H...........##XPHPPPOPPPHPP##....HHH...H",
            "H...........#.HPHPHHHHHPHPH.#..........H",
            "H...........#.HPPPPPPPPPHPH.#..........H",
            "H...........#.HHHHHHPHHHHHH.#..........H",
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
                case Door:
                case PipeDoor:
                    // Un seuil de chemin sous la porte : on voit ou l'on entre.
                    return Road;
                case Facade:
                case PipeFacade:
                    // De l'herbe sous la facade : la tuile bloquante se pose par-dessus.
                    return Grass;
                case Fountain:
                    // De la dalle sous la fontaine : son bassin se pose par-dessus.
                    return Park;
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

        /// <summary>
        /// Vrai si le parc tient : une seule fontaine, atteignable depuis ses quatre entrees,
        /// et aucune case de parc enfermee.
        ///
        /// La solvabilite a ete verifiee par calcul avant d'ecrire le trace ; cette methode la
        /// reverifie a chaque construction, pour qu'un coup de crayon dans le labyrinthe ne
        /// puisse pas enfermer la fontaine en silence.
        /// </summary>
        public static bool ValidatePark()
        {
            List<Vector2Int> fountains = FindAll(Fountain);
            if (fountains.Count != 1)
            {
                Debug.LogError($"[Sous la Ville] Le plan porte {fountains.Count} fontaine(s), " +
                               "il en faut exactement une.");
                return false;
            }

            Vector2Int fountain = fountains[0];
            Vector2Int[] entrances =
            {
                new Vector2Int(13, 15), new Vector2Int(27, 15),
                new Vector2Int(20, 19), new Vector2Int(20, 11)
            };

            foreach (Vector2Int entrance in entrances)
            {
                if (Reaches(entrance, fountain))
                {
                    continue;
                }

                Debug.LogError($"[Sous la Ville] La fontaine {fountain} n'est pas atteignable " +
                               $"depuis l'entrée {entrance} : le labyrinthe l'enferme.");
                return false;
            }

            return true;
        }

        /// <summary>
        /// Vrai si l'on peut marcher de depart jusqu'a une case VOISINE de la cible. La
        /// fontaine bloque le passage comme une maison : on l'atteint, on n'entre pas dedans.
        /// </summary>
        private static bool Reaches(Vector2Int start, Vector2Int target)
        {
            Vector2Int[] steps =
            {
                Vector2Int.up, Vector2Int.right, Vector2Int.down, Vector2Int.left
            };

            HashSet<Vector2Int> seen = new HashSet<Vector2Int> { start };
            Queue<Vector2Int> queue = new Queue<Vector2Int>();
            queue.Enqueue(start);

            while (queue.Count > 0)
            {
                Vector2Int cell = queue.Dequeue();

                foreach (Vector2Int step in steps)
                {
                    Vector2Int next = cell + step;

                    if (next == target)
                    {
                        return true;
                    }

                    if (!IsWalkable(next) || !seen.Add(next))
                    {
                        continue;
                    }

                    queue.Enqueue(next);
                }
            }

            return false;
        }

        /// <summary>
        /// Vrai si le personnage peut se tenir sur cette case, d'apres le seul plan. Les
        /// haies, les maisons, les murs de la station, les facades et la fontaine bloquent.
        /// </summary>
        public static bool IsWalkable(Vector2Int cell)
        {
            if (cell.x < 0 || cell.x >= Width || cell.y < 0 || cell.y >= Height)
            {
                return false;
            }

            char marker = At(cell.x, cell.y);
            return marker != Hedge && marker != House && marker != PlantWall
                && marker != Facade && marker != PipeFacade && marker != Fountain;
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
