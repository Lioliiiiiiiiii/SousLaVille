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
    ///   N  facade de l'usine a panneaux (bloquant)                  J  sa porte
    ///
    /// M, X, T, A, F, D, G, E, N et J sont des marqueurs : le builder peint le sol correspondant dessous
    /// et pose un GameObject par-dessus. Une maison et une facade sont en plus bloquantes :
    /// on passe devant, pas dedans. Une porte ne bloque pas : on marche dessus et Espace
    /// fait entrer, exactement comme sur une bouche d'egout.
    ///
    /// PHASE 13. L'USINE A PANNEAUX prend place au NORD DU PARC, facade en (30..33, 35..36)
    /// et porte en (31, 34), sur la rue y = 33 qui court de la bouche (9, 33) a x = 35. Le
    /// modele est celui des deux autres batiments : la porte ouvre AU SUD sur une rue.
    ///
    /// L'emplacement a ete choisi PAR CALCUL parmi les 128 qui respectent ce modele sans
    /// toucher d'une case au trace des routes. Il ne change RIEN aux 32 panneaux derives,
    /// n'ajoute qu'une seule case de chaussee — la porte — et laisse la table d'etalement
    /// des flaques intacte : sa case de facade la plus proche d'une bouche est a CINQ pas,
    /// un de plus que MaxFloodRadius. Le bras nord du carrefour (31, 33) ne fait qu'une
    /// case : le Code n'y met aucun panneau, c'est un acces et non une rue.
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
        /// L'usine a panneaux, phase 13. Troisieme batiment dans lequel on entre, sur le
        /// patron exact de l'atelier des plaques et de l'usine a tuyaux : une facade
        /// bloquante de quatre cases sur deux, et une porte qui ouvre AU SUD sur une rue.
        ///
        /// N comme panneaux, J pour sa porte. Ni V ni W : V est deja le poste de guide, et
        /// W s'en distingue mal a l'oeil dans un plan monospace de soixante-quatre colonnes.
        /// </summary>
        public const char SignFacade = 'N';

        public const char SignDoor = 'J';

        /// <summary>
        /// Le poste d'un personnage-guide, phase 12e. BLOQUANT, et c'est voulu : on ne traverse
        /// pas quelqu'un, et surtout, debout SUR lui on ne pourrait plus lui parler, puisque
        /// l'interacteur cherche un personnage sur la case REGARDEE. Un guide qu'on efface en
        /// marchant dessus est pire qu'un guide un peu mal place.
        /// </summary>
        public const char GuidePost = 'V';

        /// <summary>Les facades des batiments, dans l'ordre des pieces d'InteriorsLayout.</summary>
        public static readonly char[] Facades = { Facade, PipeFacade, SignFacade };

        /// <summary>Les portes des batiments, dans le meme ordre.</summary>
        public static readonly char[] Doors = { Door, PipeDoor, SignDoor };

        /// <summary>Ligne 0 en haut, comme on lit la carte. La conversion en case se fait dans At.</summary>
        private static readonly string[] Rows =
        {
            "HHHHHHHHHHHHHHHHHHHHHHHHHHHHHHHHHHHHHHHHHHHHHHHHHHHHHHHHHHHHHHHH",
            "H...........YYY....YYY...........YYYY.......Y.....Y.....YYY....H",
            "H.....Y............YYY...........YYY....................YYY....H",
            "H.........................................................A....H",
            "H.................#########################################....H",
            "H.YYY.............#....#...........YYY........#................H",
            "H.YYY...........Y.#....#......Y....YYY.......A#.....Y..........H",
            "H.................#....#......................#................H",
            "H.................#....#......NNNN......Y.....#...............YH",
            "H.........#########....#...A..NNNN............####M............H",
            "H..Y......A.......#....#...#...J..............#................H",
            "H........M#################M########.......Y..#................H",
            "H..........#......#..#.....#.V.....#..Y.......#................H",
            "H..........#......#..#.....#.......#..........#.............YY.H",
            "H..........#......#..#.....#.....Y.#..........##########A...YY.H",
            "H.YY.......#......A..#.....#.......#..........#................H",
            "H.YY.......#.........#.....#.......#..........#................H",
            "H.BBBBBBBBB#.........#FFFF.#..GGGG.#YYY.......#.....YYY........H",
            "H.BSSSSSSSB#.....YY..#FFFFV#..GGGG.#YYY.......#.....YYY.Y......H",
            "H.BSSSTSSSB#.....YY..#.D...#...E...#.......A###................H",
            "H.BSSSSSSSB#.........###############..........#................H",
            "H.BSSSSSSSB#...HHHHHHHHHHHHHPHHHHHHHHHHHHH....#................H",
            "H.BBBBSBBBB#...HPPPPPPPPPPPPPPPPPPPPPPPPPH....############M....H",
            "H....##....#...HPHHHHHHHPHHHHHHHHHPHHHHHPH....#................H",
            "H....A#....#...HPHPPPPPPPPPPPPPPPPPPPHPHPH....#................H",
            "H.....#....#...HPHPHPHHHHHPHHHPHHHPHPHPHPH....#................H",
            "H.YY..#.######.HPHPHPHPPPPPPPPPPPHPHPHPHPH..YY#.............YYYH",
            "H.YY..###....A.HPHPHPHPHPHHHPHHHPHPHPHPHPH..YY######AY......YYYH",
            "H.....#.#...V.XPPHPHPHPHPHHHPHHHPHPHPHPHPH....#................H",
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
            "H.....#############################............................H",
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
                case SignDoor:
                    // Un seuil de chemin sous la porte : on voit ou l'on entre.
                    return Road;
                case Facade:
                case PipeFacade:
                case SignFacade:
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
                    // De l'herbe sous un panneau : il se tient AU BORD de la chaussee, jamais
                    // dessus. En surface, plus aucun panneau n'est ecrit dans le plan : ils
                    // sont DERIVES du graphe des routes par RoadSigns, selon le Code de la route.
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
            ok &= ValidateRoads();

            Debug.Log($"[Sous la Ville] Village : {reachable.Count} case(s) praticable(s), " +
                      $"toutes reliées ; {FindAll(Tree).Count} arbre(s), " +
                      $"{RoadSigns().Count} panneau(x) dérivé(s) des routes.");

            return ok;
        }

        /// <summary>
        /// Le reseau routier est-il D'UN SEUL TENANT ? Une rue coupee par un batiment finit en
        /// deux troncons morts, et rien ne le disait : la phase 12b avait fait passer la rue
        /// x = 8 EN PLEIN TRAVERS de l'enceinte de la station, et c'est la logique des panneaux
        /// qui l'a trouve en signalant une impasse la ou il n'aurait pas du y en avoir.
        ///
        /// La dalle du depart du joueur pose du chemin sous elle mais n'est pas une rue : elle
        /// est ignoree.
        /// </summary>
        private static bool ValidateRoads()
        {
            Vector2Int start = FindSingle(PlayerStart);
            HashSet<Vector2Int> roads = new HashSet<Vector2Int>();

            for (int y = 0; y < Height; y++)
            {
                for (int x = 0; x < Width; x++)
                {
                    Vector2Int cell = new Vector2Int(x, y);
                    if (IsRoad(cell) && cell != start)
                    {
                        roads.Add(cell);
                    }
                }
            }

            if (roads.Count == 0)
            {
                return true;
            }

            Vector2Int seed = default;
            foreach (Vector2Int cell in roads)
            {
                seed = cell;
                break;
            }

            HashSet<Vector2Int> seen = new HashSet<Vector2Int> { seed };
            Queue<Vector2Int> queue = new Queue<Vector2Int>();
            queue.Enqueue(seed);

            while (queue.Count > 0)
            {
                Vector2Int cell = queue.Dequeue();
                foreach (Vector2Int step in Steps)
                {
                    Vector2Int next = cell + step;
                    if (roads.Contains(next) && seen.Add(next))
                    {
                        queue.Enqueue(next);
                    }
                }
            }

            if (seen.Count == roads.Count)
            {
                return true;
            }

            int shown = 0;
            foreach (Vector2Int cell in roads)
            {
                if (seen.Contains(cell) || shown++ >= 4)
                {
                    continue;
                }

                Debug.LogError($"[Sous la Ville] La case de route {cell} est coupée du reste " +
                               "du réseau routier : une rue en morceaux.");
            }

            // Ce qu'on a MESURE, c'est le nombre de cases hors du reseau, pas le nombre de
            // morceaux : un mot juste plutot qu'un nombre qui a l'air precis et qui ment.
            Debug.LogError($"[Sous la Ville] {roads.Count - seen.Count} case(s) de route coupée(s) " +
                           "du réseau : il doit être d'un seul tenant.");
            return false;
        }

        /// <summary>
        /// Le decor de la phase 12c. Deux regles, et elles sont du gameplay, pas de l'ornement.
        ///
        /// UN PANNEAU SE VOIT. Les panneaux de rue sont DERIVES des routes par RoadSigns ; ce
        /// qu'on verifie est que chacun tombe sur de l'herbe ATTEIGNABLE — un panneau mure dans
        /// un bosquet est un repere que personne ne lira — et qu'aucun n'est ecrit a la main
        /// dans le plan, ou il pourrait etre mal place, a l'envers ou imaginaire.
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

            foreach (RoadSign sign in RoadSigns())
            {
                if (reachable.Contains(sign.Cell) && At(sign.Cell.x, sign.Cell.y) == Grass)
                {
                    continue;
                }

                Debug.LogError($"[Sous la Ville] Le panneau {sign.Kind} en {sign.Cell} n'est pas " +
                               "sur de l'herbe atteignable : personne ne le lira jamais.");
                ok = false;
            }

            if (FindAll(Sign).Count > 0)
            {
                Debug.LogError($"[Sous la Ville] Le plan du village porte {FindAll(Sign).Count} " +
                               "marqueur(s) « I » : en surface les panneaux se DERIVENT des routes, " +
                               "ils ne s'ecrivent plus a la main.");
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

        /// <summary>
        /// Les panneaux du Code de la route francais que le village peut porter, et rien
        /// d'autre. Pas de sens interdit : il n'y a aucune rue a sens unique, et un B1 sans
        /// sens unique serait un panneau imaginaire. Pas de triangle de danger generique : le
        /// A14 s'accompagne toujours d'un panonceau qui dit LEQUEL, et il n'y en a pas ici.
        /// </summary>
        public enum RoadSignKind
        {
            /// <summary>AB3a. Triangle pointe en bas, borde de rouge. Bras secondaire d'un carrefour.</summary>
            Yield,

            /// <summary>AB4. Octogone rouge. Bras secondaire debouchant sur la route prioritaire.</summary>
            Stop,

            /// <summary>C13a. Carre bleu, voie en T barree de rouge. A l'entree d'une impasse.</summary>
            DeadEnd,

            /// <summary>AB2. Losange jaune. A l'entree de la route prioritaire.</summary>
            Priority,

            /// <summary>AB6. Losange jaune barre. A la sortie de la route prioritaire.</summary>
            PriorityEnd
        }

        /// <summary>Un panneau derive : sa case au bord de la chaussee, son type, et le sens du conducteur qu'il vise.</summary>
        public struct RoadSign
        {
            public Vector2Int Cell;
            public RoadSignKind Kind;
            public Vector2Int Facing;

            public RoadSign(Vector2Int cell, RoadSignKind kind, Vector2Int facing)
            {
                Cell = cell;
                Kind = kind;
                Facing = facing;
            }
        }

        private static readonly Vector2Int[] Steps =
        {
            Vector2Int.up, Vector2Int.right, Vector2Int.down, Vector2Int.left
        };

        /// <summary>
        /// LA ROUTE PRIORITAIRE : la rocade, du carrefour (23, 40) vers l'est puis vers le sud
        /// jusqu'a (46, 10). Une seule, ecrite a la main, parce que c'est une decision
        /// d'urbanisme et non une propriete du graphe. Tout ce qui y debouche marque un STOP.
        /// </summary>
        private static readonly Vector2Int[] MainRoadCorners =
        {
            new Vector2Int(23, 40), new Vector2Int(46, 40), new Vector2Int(46, 10)
        };

        /// <summary>Vrai si la case porte de la chaussee, marqueurs compris (bouche, porte, depart).</summary>
        public static bool IsRoad(int x, int y)
        {
            return x >= 0 && x < Width && y >= 0 && y < Height && GroundAt(x, y) == Road;
        }

        private static bool IsRoad(Vector2Int cell)
        {
            return IsRoad(cell.x, cell.y);
        }

        private static int RoadDegree(Vector2Int cell)
        {
            int degree = 0;
            foreach (Vector2Int step in Steps)
            {
                if (IsRoad(cell + step))
                {
                    degree++;
                }
            }

            return degree;
        }

        /// <summary>
        /// La DROITE du conducteur qui avance dans la direction donnee. En France on roule a
        /// droite et les panneaux se posent a droite de la chaussee, dans le sens de la marche.
        /// (0,1) nord donne (1,0) est ; (1,0) est donne (0,-1) sud ; et ainsi de suite.
        /// </summary>
        private static Vector2Int RightOf(Vector2Int direction)
        {
            return new Vector2Int(direction.y, -direction.x);
        }

        private static bool OnMainRoad(Vector2Int cell)
        {
            for (int i = 0; i + 1 < MainRoadCorners.Length; i++)
            {
                Vector2Int a = MainRoadCorners[i];
                Vector2Int b = MainRoadCorners[i + 1];

                bool vertical = a.x == b.x;
                if (vertical && cell.x == a.x
                    && cell.y >= Mathf.Min(a.y, b.y) && cell.y <= Mathf.Max(a.y, b.y))
                {
                    return true;
                }

                if (!vertical && cell.y == a.y
                    && cell.x >= Mathf.Min(a.x, b.x) && cell.x <= Mathf.Max(a.x, b.x))
                {
                    return true;
                }
            }

            return false;
        }

        /// <summary>
        /// La longueur du bras qui part du carrefour dans cette direction S'IL EST UNE IMPASSE :
        /// on le suit, virages compris, et il finit sur une case de degre un sans croiser un
        /// autre carrefour. Une rue qui finit sur une maison ou sur une bouche est une impasse.
        /// Rend -1 si le bras rejoint un autre carrefour.
        ///
        /// La longueur compte : un bras d'UNE case qui finit sur une maison est un passage
        /// d'acces, pas une rue, et le Code n'y met aucun panneau.
        /// </summary>
        private static int DeadEndArmLength(Vector2Int junction, Vector2Int direction)
        {
            Vector2Int previous = junction;
            Vector2Int current = junction + direction;
            int length = 1;

            for (int guard = 0; guard < Width * Height; guard++)
            {
                Vector2Int next = Vector2Int.zero;
                int exits = 0;

                foreach (Vector2Int step in Steps)
                {
                    Vector2Int candidate = current + step;
                    if (candidate == previous || !IsRoad(candidate))
                    {
                        continue;
                    }

                    exits++;
                    next = candidate;
                }

                if (exits == 0)
                {
                    return length;
                }

                if (exits > 1)
                {
                    return -1;
                }

                previous = current;
                current = next;
                length++;
            }

            return -1;
        }

        /// <summary>
        /// Un bras d'au plus deux cases qui finit sur une maison : un acces, pas une rue. Le
        /// Code ne signale ni les entrees de garage, ni un cul-de-sac qui ne dessert qu'une
        /// maison a deux pas du carrefour.
        /// </summary>
        private const int DrivewayLength = 2;

        /// <summary>
        /// LES PANNEAUX DE RUE, DERIVES DU GRAPHE DES ROUTES selon le Code de la route
        /// francais. Rien n'est ecrit a la main : un panneau ne peut donc etre ni mal place, ni
        /// a l'envers, ni imaginaire — il est la parce que la route l'exige.
        ///
        /// Les regles, et elles sont celles du Code :
        ///
        /// 1. A chaque carrefour, l'axe qui TRAVERSE est prioritaire ; un bras en T qui y
        ///    debouche est secondaire. A une croix, l'axe de la rocade est prioritaire, sinon
        ///    l'axe est-ouest. Un bras secondaire recoit un CEDEZ LE PASSAGE, ou un STOP s'il
        ///    debouche sur la route prioritaire.
        /// 2. Le panneau se pose A DROITE de la chaussee dans le sens de la marche, UNE CASE
        ///    AVANT la ligne du carrefour. Si l'herbe manque a droite, une case plus loin ;
        ///    sinon rien, et le builder le dit.
        /// 3. Un bras qui est une impasse recoit un panneau IMPASSE a son entree, a droite du
        ///    conducteur qui s'y engage.
        /// 4. La route prioritaire s'annonce a chacune de ses deux entrees, ROUTE PRIORITAIRE,
        ///    et se clot a chacune de ses deux sorties, FIN DE ROUTE PRIORITAIRE.
        ///
        /// 5. Un bras d'au plus DEUX cases qui finit sur une maison est un PASSAGE D'ACCES :
        ///    ni cedez, ni impasse. Le Code ne signale pas les entrees de garage.
        ///
        /// Deux panneaux peuvent revendiquer la meme case — l'impasse d'un bras et le cedez du
        /// bras voisin partagent la diagonale. Les panneaux de PRIORITE se posent en premier :
        /// c'est l'impasse qui recule d'une case, jamais le cedez.
        /// </summary>
        public static List<RoadSign> RoadSigns()
        {
            List<RoadSign> signs = new List<RoadSign>();
            HashSet<Vector2Int> taken = new HashSet<Vector2Int>();
            List<Vector2Int> junctions = new List<Vector2Int>();

            for (int y = 0; y < Height; y++)
            {
                for (int x = 0; x < Width; x++)
                {
                    Vector2Int cell = new Vector2Int(x, y);
                    if (IsRoad(cell) && RoadDegree(cell) >= 3)
                    {
                        junctions.Add(cell);
                    }
                }
            }

            // PREMIERE PASSE, les panneaux de priorite : ce sont eux qui comptent pour la
            // securite, ils prennent leur place en premier. Deux carrefours a deux cases l'un
            // de l'autre se disputent la meme herbe, et c'est l'impasse qui recule.
            foreach (Vector2Int junction in junctions)
            {
                bool verticalPriority = PriorityIsVertical(junction);

                foreach (Vector2Int arm in Steps)
                {
                    if (!IsRoad(junction + arm) || (arm.x == 0) == verticalPriority)
                    {
                        continue;
                    }

                    int deadEnd = DeadEndArmLength(junction, arm);
                    if (deadEnd > 0 && deadEnd <= DrivewayLength)
                    {
                        continue;                       // un acces, pas une rue
                    }

                    Vector2Int travel = -arm;           // le conducteur arrive PAR ce bras
                    RoadSignKind kind = OnMainRoad(junction) ? RoadSignKind.Stop : RoadSignKind.Yield;
                    Place(signs, taken, junction, arm, RightOf(travel), kind, travel);
                }
            }

            // SECONDE PASSE, les impasses : a l'entree du bras, a droite de qui s'y engage.
            foreach (Vector2Int junction in junctions)
            {
                foreach (Vector2Int arm in Steps)
                {
                    int length = IsRoad(junction + arm) ? DeadEndArmLength(junction, arm) : -1;
                    if (length <= DrivewayLength)
                    {
                        continue;
                    }

                    Place(signs, taken, junction, arm, RightOf(arm), RoadSignKind.DeadEnd, arm);
                }
            }

            // TROISIEME PASSE, la route prioritaire : annoncee a l'entree, close a la sortie.
            Vector2Int west = MainRoadCorners[0];
            Vector2Int east = MainRoadCorners[MainRoadCorners.Length - 1];
            Vector2Int intoWest = Direction(MainRoadCorners[0], MainRoadCorners[1]);
            Vector2Int intoEast = Direction(east, MainRoadCorners[MainRoadCorners.Length - 2]);

            Place(signs, taken, west, intoWest, RightOf(intoWest), RoadSignKind.Priority, intoWest);
            Place(signs, taken, west, intoWest, RightOf(-intoWest), RoadSignKind.PriorityEnd, -intoWest);
            Place(signs, taken, east, intoEast, RightOf(intoEast), RoadSignKind.Priority, intoEast);
            Place(signs, taken, east, intoEast, RightOf(-intoEast), RoadSignKind.PriorityEnd, -intoEast);

            return signs;
        }

        private static Vector2Int Direction(Vector2Int from, Vector2Int to)
        {
            Vector2Int delta = to - from;
            return new Vector2Int(System.Math.Sign(delta.x), System.Math.Sign(delta.y));
        }

        /// <summary>
        /// A un carrefour, l'axe prioritaire est-il nord-sud ? En T, c'est l'axe qui a ses
        /// deux bras. En croix, celui de la rocade s'il y passe, sinon l'est-ouest.
        /// </summary>
        private static bool PriorityIsVertical(Vector2Int junction)
        {
            bool north = IsRoad(junction + Vector2Int.up);
            bool south = IsRoad(junction + Vector2Int.down);
            bool east = IsRoad(junction + Vector2Int.right);
            bool west = IsRoad(junction + Vector2Int.left);

            bool verticalThrough = north && south;
            bool horizontalThrough = east && west;

            if (verticalThrough != horizontalThrough)
            {
                return verticalThrough;
            }

            if (OnMainRoad(junction))
            {
                return OnMainRoad(junction + Vector2Int.up) || OnMainRoad(junction + Vector2Int.down);
            }

            return false;
        }

        /// <summary>
        /// Pose un panneau au bord du bras : une case dans le bras depuis le carrefour, puis
        /// un pas de cote. Si la case n'est pas de l'herbe libre, une case plus loin dans le
        /// bras ; sinon on renonce en le disant, jamais en silence.
        /// </summary>
        private static void Place(List<RoadSign> signs, HashSet<Vector2Int> taken,
            Vector2Int junction, Vector2Int arm, Vector2Int side, RoadSignKind kind,
            Vector2Int facing)
        {
            for (int distance = 1; distance <= 2; distance++)
            {
                Vector2Int onRoad = junction + arm * distance;
                if (!IsRoad(onRoad))
                {
                    break;
                }

                Vector2Int cell = onRoad + side;
                if (At(cell.x, cell.y) != Grass || taken.Contains(cell))
                {
                    continue;
                }

                taken.Add(cell);
                signs.Add(new RoadSign(cell, kind, facing));
                return;
            }

            Debug.LogWarning($"[Sous la Ville] Pas d'herbe libre pour un panneau {kind} au bord " +
                             $"du bras {arm} du carrefour {junction} : il n'est pas posé.");
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
                && marker != Facade && marker != PipeFacade && marker != SignFacade
                && marker != Fountain && marker != Tree && marker != GuidePost;
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
