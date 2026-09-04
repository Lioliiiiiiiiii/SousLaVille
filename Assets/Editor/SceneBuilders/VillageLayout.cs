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
    ///   O  fontaine du parc (bloquant)   Y  arbre (bloquant)
    ///   I  panneau de signalisation (NE bloque PAS : c'est un repere)
    ///   V  poste de guide (bloquant : on ne traverse pas quelqu'un)
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
    /// structurelle, et ValidateVillage la reverifie a chaque construction.
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
        public const char Tree = 'Y';
        public const char Sign = 'I';

        /// <summary>
        /// Le poste d'un personnage-guide, phase 12e. BLOQUANT, et c'est voulu : on ne traverse
        /// pas quelqu'un, et surtout, debout SUR lui on ne pourrait plus lui parler, puisque
        /// l'interacteur cherche un personnage sur la case REGARDEE. Un guide qu'on efface en
        /// marchant dessus est pire qu'un guide un peu mal place.
        /// </summary>
        public const char GuidePost = 'V';

        /// <summary>Les facades des batiments, dans l'ordre des pieces d'InteriorsLayout.</summary>
        public static readonly char[] Facades = { Facade, PipeFacade };

        /// <summary>Les portes des batiments, dans le meme ordre.</summary>
        public static readonly char[] Doors = { Door, PipeDoor };

        /// <summary>Ligne 0 en haut, comme on lit la carte. La conversion en case se fait dans At.</summary>
        private static readonly string[] Rows =
        {
            "HHHHHHHHHHHHHHHHHHHHHHHHHHHHHHHHHHHHHHHHHHHHHHHHHHHHHHHHHHHHHYYH",
            "H...........YYY....YYY...........YYYY.......Y.....Y.....YYY....H",
            "H.....Y............YYY...........YYY....................YYY....H",
            "H.........................................................A....H",
            "H.................##########I#######################I######....H",
            "H.YYY.............#....#...........YYY.......##................H",
            "H.YYY...........Y.#....#......Y....YYY.......A#.....Y..........H",
            "H.................#....#......................#................H",
            "H.................#....#................Y.....#...............YH",
            "H.........####I####....#...A..................##I#M............H",
            "H..Y......A.......#....#...#..................#................H",
            "H.......#M######I##########M####...........Y..#................H",
            "H.......#.........#....#...#V..#......Y.......#................H",
            "H.......#.........#....I...#...#..............#.............YY.H",
            "H.......#.........#....#...#...#.Y............##I#######A...YY.H",
            "H.YY....#.........A....#...#...#..............#................H",
            "H.YY....#..............#...#...#..............#................H",
            "H.BBBBBBBBB...........FFFF.#..GGGG..YYY.......#.....YYY........H",
            "H.BSSSSSSSB......YY...FFYFV#..GGGG..YYY.......#.....YYY.Y......H",
            "H.BSSSTSSSB......YY....D.......E...........AI##.........I......H",
            "H.BSSSSSSSB...................................#................H",
            "H.YSSSSSSSB....HHHHHHHHHHHHHPHHHHHHHHHHHHH....#................H",
            "H.BBBBSBBBB....HPPPPPPPPPPPPPPPPPPPPPPPPPH....##I#########M....H",
            "H....##.#......HPHHHHHHHPHHHHHHHHHPHHHHHPH....#................H",
            "H....A#.#......HPYPPPPPPPPPPPPPPPPPPPHPHPH....#................H",
            "H.....#.#......HPHPHPHHHHHPHHHPHHHPHPHPHPH....#................H",
            "H.YY..#.##I###.HPHPHPHPPPPPPPPPPPHPHPHPHPH..YY#.............YYYH",
            "H.YY..###....A.HPHPHPHPHPHHHPHHHPHPHPHPHPH..YY#I####AY......YYYH",
            "H.....I.I...V.XPPHPHPHPHPHHHPHHHPHPHPHPHPH....#................H",
            "H.....#.#...Y..HPHPHPHPHPHHHHHHHPHPHPHPHPH....#................H",
            "H.....#.#......HPHPHPHPHPHPHOPPPPHPHPHPHPP....#................H",
            "H.....#.#......HPHPHPHPHHHPHHHHHHHHHPHPHPH....#............Y...H",
            "H.....#.#......HPHPHPHPPPPPPPPPPPPPPPHPHPH....#................H",
            "H.....#.#MV....HPHPHHHHHPHHHHHPHHHHHHHPHPH....#................H",
            "H.Y...#........HPHPPPPPPPPPPPPPPPPPPPPPPPH....####M###########.H",
            "H.....#........HPHHHHHPHHHPHHHHHPHHHPHHHPH...................#.H",
            "H.....#........HPPPPPPPPPPPPPPPPPPPPPPPPPH...........YYY.....AYH",
            "H.YYY.#.....YY.HHHHHHHHHHHHHPHHHHHHHHHHHHH..Y........YYY.......H",
            "H.YYY.#.....YY.................................................H",
            "H.....##############I#####I########............................H",
            "H..........................#......A..........................Y.H",
            "H..........Y..YYY..........M...............YYY..Y..............H",
            "H.............YYY...Y.................Y....YYY..........Y......H",
            "H.............................Y................................H",
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
                case Tree:
                    // De l'herbe sous l'arbre : sa tuile bloquante se pose par-dessus.
                    return Grass;
                case GuidePost:
                    // De l'herbe sous le guide : il est un GameObject, pas une tuile, et c'est
                    // lui-meme qui bloque en occupant sa case.
                    return Grass;
                case Sign:
                    // Un socle de chemin sous le panneau. Les panneaux de rue sont poses sur
                    // des cases de route, donc le masque de raccord de la route reste continu :
                    // c'est GroundAt que le peintre interroge, jamais le marqueur.
                    return Road;
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
        /// Le village tient-il debout ? Une seule fontaine, RIEN d'enferme nulle part, la
        /// fontaine atteignable, aucun panneau en travers d'un chemin, et aucun arbre assez
        /// pres d'une bouche pour manger une flaque.
        ///
        /// REECRIT EN PHASE 12A, ELARGI EN PHASE 12C. La version de la phase 11 partait de
        /// quatre entrees ECRITES A LA MAIN et lancait quatre parcours qui, par inondation,
        /// exploraient tous le meme et unique ensemble : elle prouvait quatre fois la meme
        /// chose, et jamais celle que son resume promettait.
        ///
        /// La phase 12c y ajoute les ARBRES, premiers obstacles poses hors du parc et hors des
        /// batiments. Un seul parcours depuis le DEPART du joueur prouve tout : sa composante
        /// connexe doit contenir TOUTE case praticable de la carte, parc compris. Un arbre qui
        /// enferme une maison, un panneau, ou un coin de plaine est donc refuse par
        /// construction, et rien n'est ecrit a la main.
        ///
        /// S'appelait ValidatePark jusqu'a la phase 12c : elle ne prouve plus seulement le parc.
        /// </summary>
        public static bool ValidateVillage()
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

            // 1. Rien ne piege et rien ne s'enferme, NULLE PART. C'est plus fort que « aucune
            // case de parc enfermee » : les arbres de la phase 12c peuvent detacher un coin de
            // plaine aussi bien qu'un couloir du labyrinthe.
            int orphans = 0;
            for (int y = 0; y < Height; y++)
            {
                for (int x = 0; x < Width; x++)
                {
                    Vector2Int cell = new Vector2Int(x, y);
                    if (!IsWalkable(cell) || reachable.Contains(cell))
                    {
                        continue;
                    }

                    if (orphans < 5)
                    {
                        Debug.LogError($"[Sous la Ville] La case {cell}, « {At(x, y)} », est " +
                                       "enfermée : aucun chemin ne l'atteint depuis le départ.");
                    }

                    orphans++;
                    ok = false;
                }
            }

            if (orphans >= 5)
            {
                Debug.LogError($"[Sous la Ville] {orphans} cases enfermées au total.");
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

            ok &= ValidateDecor(reachable);

            Debug.Log($"[Sous la Ville] Village : {reachable.Count} case(s) praticable(s), " +
                      $"toutes reliées ; {FindAll(Tree).Count} arbre(s), " +
                      $"{FindAll(Sign).Count} panneau(x).");

            return ok;
        }

        /// <summary>
        /// Le decor de la phase 12c. Deux regles, et elles sont du gameplay, pas de l'ornement.
        ///
        /// UN PANNEAU SE VOIT. Verifier qu'il ne bloque pas ne servirait a rien : « I » n'est
        /// pas dans la liste de blocage, donc la reponse serait oui quoi qu'il arrive, et un
        /// controle qui ne peut pas dire non ne vaut rien. Ce qu'on verifie est qu'il est
        /// ATTEIGNABLE : un panneau muré dans un bosquet ou au coeur d'une haie est un repere
        /// que personne ne lira jamais.
        ///
        /// UN ARBRE SE TIENT A PLUS DE QUATRE PAS D'UNE BOUCHE. Le rayon de la flaque vaut
        /// `Lost - 1` et `Lost` plafonne a 5, donc quatre : une case bloquante ne prend pas
        /// l'eau, et un arbre plante plus pres retirerait des cases au debordement SANS QUE
        /// RIEN NE LE DISE. Le debordement est le seul retour permanent du jeu ; on ne le rogne
        /// pas pour un arbre.
        /// </summary>
        private static bool ValidateDecor(HashSet<Vector2Int> reachable)
        {
            bool ok = true;

            foreach (Vector2Int sign in FindAll(Sign))
            {
                if (reachable.Contains(sign))
                {
                    continue;
                }

                Debug.LogError($"[Sous la Ville] Le panneau {sign} n'est atteignable depuis " +
                               "aucune case accessible : personne ne le lira jamais.");
                ok = false;
            }

            List<Vector2Int> manholes = FindAll(Manhole);

            foreach (Vector2Int tree in FindAll(Tree))
            {
                foreach (Vector2Int manhole in manholes)
                {
                    int distance = Mathf.Abs(tree.x - manhole.x) + Mathf.Abs(tree.y - manhole.y);
                    if (distance > MaxFloodRadius)
                    {
                        continue;
                    }

                    Debug.LogError($"[Sous la Ville] L'arbre {tree} est à {distance} pas de la " +
                                   $"bouche {manhole} : il mange une case de flaque. Le rayon du " +
                                   $"débordement plafonne à {MaxFloodRadius}.");
                    ok = false;
                }
            }

            return ok;
        }

        /// <summary>
        /// Rayon maximal d'une flaque de debordement : `Lost` plafonne a 5 et le rayon vaut
        /// `Lost - 1`. Voir la table d'etalement dans PROGRESS.md, qui se recalcule a chaque
        /// phase touchant a la carte.
        /// </summary>
        public const int MaxFloodRadius = 4;

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
        /// haies, les maisons, les murs de la station, les facades, la fontaine, les ARBRES et
        /// les POSTES DE GUIDE bloquent. Un PANNEAU ne bloque pas : c'est un repere, pas un obstacle, et le mettre
        /// en travers d'un chemin serait un echec puni au sens de CLAUDE.md.
        /// </summary>
        public static bool IsWalkable(Vector2Int cell)
        {
            if (cell.x < 0 || cell.x >= Width || cell.y < 0 || cell.y >= Height)
            {
                return false;
            }

            char marker = At(cell.x, cell.y);
            return marker != Hedge && marker != House && marker != PlantWall
                && marker != Facade && marker != PipeFacade && marker != Fountain
                && marker != Tree && marker != GuidePost;
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
