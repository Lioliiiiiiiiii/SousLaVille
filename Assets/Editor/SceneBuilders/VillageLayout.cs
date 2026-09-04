using System.Collections.Generic;
using UnityEngine;

namespace SousLaVille.EditorTools
{
    /// <summary>
    /// Le plan du village, ecrit a la main. Quarante-cinq lignes de soixante-quatre
    /// caracteres.
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
    /// PHASE 12B. La carte passe de 40x30 a 64x45. Le sens de l'agrandissement n'est pas
    /// neutre : At fait Rows[Height - 1 - y][x], donc AJOUTER LES LIGNES EN TETE et LES
    /// CARACTERES EN FIN DE LIGNE preserve rigoureusement chaque couple (x, y). La station
    /// reste en (6, 25), l'atelier et l'usine a tuyaux sur leurs cases, et le village a
    /// grandi vers le NORD et vers l'EST.
    ///
    /// Le parc porte desormais UN SEUL labyrinthe de haies de 27 sur 17, coin bas-gauche en
    /// (15, 7), fontaine en (28, 14). L'ecran montre 20 sur 11,25 cases : il ne tient donc
    /// plus dans le cadre, la camera y defile, et il ne se resout plus d'un coup d'oeil mais
    /// DE MEMOIRE. C'est le but, pas un effet de bord. Ses quatre entrees sont a 24, 32, 34
    /// et 34 pas de la fontaine, et sa case la plus lointaine a 47 pas.
    ///
    /// Le labyrinthe a ete CREUSE EN POLYLIGNES et non dessine caractere par caractere : un
    /// premier jet dessine a la main s'etait fragmente en seize morceaux sans que rien ne le
    /// dise. Chaque polyligne devait toucher le trace deja pose, ce qui rend la connexite
    /// structurelle, et ValidatePark la reverifie a chaque construction.
    ///
    /// Douze maisons et la fontaine font TREIZE destinations, le plafond de la rangee de
    /// gouttes du HUD (a quatorze elle chevauche le picto de saison). Sept bouches d'egout
    /// tiennent la densite de la phase 1, et l'invariant : jamais plus de 23 pas entre une
    /// case et l'echelle la plus proche.
    ///
    /// Ecrit a la main et non engendre. Le commentaire ci-dessous annoncait l'inverse en
    /// phase 1 ; la pratique du projet a tranche depuis, et un plan engendre ne se verifie
    /// plus une fois pour toutes.
    /// </summary>
    public static class VillageLayout
    {
        public const int Width = 64;
        public const int Height = 45;

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
            "HHHHHHHHHHHHHHHHHHHHHHHHHHHHHHHHHHHHHHHHHHHHHHHHHHHHHHHHHHHHHHHH",
            "H..................HHH..................................HHH....H",
            "H..................HHH..................................HHH....H",
            "H.........................................................A....H",
            "H.................#########################################....H",
            "H.................#....#.....................##................H",
            "H.................#....#.....................A#................H",
            "H.................#....#......................#................H",
            "H.................#....#......................#................H",
            "H.........#########....#...A..................####M............H",
            "H.........A.......#....#...#..................#................H",
            "H.......#M#################M####..............#................H",
            "H.......#.........#....#...#...#..............#................H",
            "H.......#.........#....#...#...#..............#................H",
            "H.......#.........#....#...#...#..............##########A......H",
            "H.......#.........A....#...#...#..............#................H",
            "H.......#..............#...#...#..............#................H",
            "H.BBBBBBBBB...........FFFF.#..GGGG..HHH.......#................H",
            "H.BSSSSSSSB...........FFFF.#..GGGG..HHH.......#................H",
            "H.BSSSTSSSB............D.......E...........A###................H",
            "H.BSSSSSSSB...................................#................H",
            "H.BSSSSSSSB....HHHHHHHHHHHHHPHHHHHHHHHHHHH....#................H",
            "H.BBBBSBBBB....HPPPPPPPPPPPPPPPPPPPPPPPPPH....############M....H",
            "H....##.#......HPHHHHHHHPHHHHHHHHHPHHHHHPH....#................H",
            "H....A#.#......HPHPPPPPPPPPPPPPPPPPPPHPHPH....#................H",
            "H.....#.#......HPHPHPHHHHHPHHHPHHHPHPHPHPH....#................H",
            "H.....#.######.HPHPHPHPPPPPPPPPPPHPHPHPHPH....#................H",
            "H.....###....A.HPHPHPHPHPHHHPHHHPHPHPHPHPH....######A..........H",
            "H.....#.#.....XPPHPHPHPHPHHHPHHHPHPHPHPHPH....#................H",
            "H.....#.#......HPHPHPHPHPHHHHHHHPHPHPHPHPH....#................H",
            "H.....#.#......HPHPHPHPHPHPHOPPPPHPHPHPHPP....#................H",
            "H.....#.#......HPHPHPHPHHHPHHHHHHHHHPHPHPH....#................H",
            "H.....#.#......HPHPHPHPPPPPPPPPPPPPPPHPHPH....#................H",
            "H.....#.#M.....HPHPHHHHHPHHHHHPHHHHHHHPHPH....#................H",
            "H.....#........HPHPPPPPPPPPPPPPPPPPPPPPPPH....####M###########.H",
            "H.....#........HPHHHHHPHHHPHHHHHPHHHPHHHPH...................#.H",
            "H.....#........HPPPPPPPPPPPPPPPPPPPPPPPPPH...........HHH.....A.H",
            "H.HHH.#........HHHHHHHHHHHHHPHHHHHHHHHHHHH...........HHH.......H",
            "H.HHH.#........................................................H",
            "H.....#############################............................H",
            "H..........................#......A............................H",
            "H..........................M...................................H",
            "H..............................................................H",
            "H..............................................................H",
            "HHHHHHHHHHHHHHHHHHHHHHHHHHHHHHHHHHHHHHHHHHHHHHHHHHHHHHHHHHHHHHHH",
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
        /// Vrai si le parc tient : une seule fontaine, aucune case enfermee, et la fontaine
        /// atteignable a pied depuis le depart du joueur.
        ///
        /// REECRIT EN PHASE 12A. La version de la phase 11 partait de quatre entrees ECRITES A
        /// LA MAIN et lancait quatre parcours qui, par inondation, exploraient tous le meme et
        /// unique ensemble : elle prouvait quatre fois la meme chose, et jamais celle que son
        /// resume promettait, « aucune case de parc enfermee ». Elle ne verifiait pas non plus
        /// que ses propres points de depart etaient praticables.
        ///
        /// Un seul parcours suffit, et il prouve les deux : la composante connexe du DEPART du
        /// joueur doit contenir toutes les cases praticables du parc, et une voisine de la
        /// fontaine. Rien n'est ecrit a la main, donc rien ne se perime quand le parc grandit.
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

            Vector2Int start = FindSingle(PlayerStart);
            if (!IsWalkable(start))
            {
                Debug.LogError($"[Sous la Ville] Le départ {start} n'est pas praticable.");
                return false;
            }

            HashSet<Vector2Int> reachable = FloodFrom(start);
            bool ok = true;

            // 1. Rien ne piege : toute case praticable du parc se rejoint depuis le depart,
            // donc s'en ressort.
            foreach (Vector2Int cell in FindAll(Park))
            {
                if (IsWalkable(cell) && !reachable.Contains(cell))
                {
                    Debug.LogError($"[Sous la Ville] La case de parc {cell} est enfermée : " +
                                   "aucun chemin ne l'atteint depuis le départ.");
                    ok = false;
                }
            }

            // 2. La fontaine s'atteint : elle bloque, donc c'est une de ses voisines qu'il faut.
            Vector2Int fountain = fountains[0];
            Vector2Int[] steps =
            {
                Vector2Int.up, Vector2Int.right, Vector2Int.down, Vector2Int.left
            };

            bool touched = false;
            foreach (Vector2Int step in steps)
            {
                touched |= reachable.Contains(fountain + step);
            }

            if (!touched)
            {
                Debug.LogError($"[Sous la Ville] La fontaine {fountain} n'est atteignable depuis " +
                               "aucune case accessible : le labyrinthe l'enferme.");
                ok = false;
            }

            return ok;
        }

        /// <summary>Toutes les cases praticables que l'on peut rejoindre a pied depuis une case.</summary>
        private static HashSet<Vector2Int> FloodFrom(Vector2Int start)
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

                    if (!IsWalkable(next) || !seen.Add(next))
                    {
                        continue;
                    }

                    queue.Enqueue(next);
                }
            }

            return seen;
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

        /// <summary>Vrai si la carte fait bien 45 lignes de 64 caracteres.</summary>
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
