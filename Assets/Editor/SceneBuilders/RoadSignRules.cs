using System.Collections.Generic;
using UnityEngine;

namespace SousLaVille.EditorTools
{
    /// <summary>
    /// Les panneaux du Code de la route francais qu'une carte de rues peut porter, et rien
    /// d'autre. Pas de sens interdit : il n'y a aucune rue a sens unique, et un B1 sans sens
    /// unique serait un panneau imaginaire. Pas de triangle de danger generique : le A14
    /// s'accompagne toujours d'un panonceau qui dit LEQUEL, et il n'y en a pas ici.
    ///
    /// Les valeurs sont les RANGS des cinq premiers panneaux du catalogue de
    /// PlaceholderArtGenerator : SignTexture((int)kind) est l'image du panneau.
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

    /// <summary>
    /// Ce qu'une carte de rues doit savoir dire d'elle-meme pour que le Code s'y applique. Le
    /// village depuis la phase 12c, les quinze plans du mini-jeu depuis la phase 16.
    /// </summary>
    public interface IRoadGrid
    {
        int Width { get; }
        int Height { get; }

        /// <summary>Vrai si la case porte de la chaussee. Faux hors de la carte.</summary>
        bool IsRoad(Vector2Int cell);

        /// <summary>Vrai si la case est de l'herbe nue, ou un panneau peut se dresser.</summary>
        bool IsFreeGrass(Vector2Int cell);

        /// <summary>
        /// Vrai si cette case de chaussee est une SORTIE : la rue continue hors de la carte.
        /// Le village n'en a aucune ; un plan du mini-jeu en a a chaque bord ou une rue le
        /// touche, sinon chaque rue qui sort du cadre serait une impasse.
        /// </summary>
        bool IsExit(Vector2Int cell);

        /// <summary>
        /// La route prioritaire, en coins de polyligne, ou vide s'il n'y en a pas. Une seule,
        /// ecrite a la main : c'est une decision d'urbanisme, pas une propriete du graphe.
        /// </summary>
        IList<Vector2Int> MainRoadCorners { get; }
    }

    /// <summary>
    /// LES PANNEAUX DE RUE, DERIVES DU GRAPHE DES ROUTES selon le Code de la route francais.
    /// Rien n'est ecrit a la main : un panneau ne peut donc etre ni mal place, ni a l'envers,
    /// ni imaginaire — il est la parce que la route l'exige.
    ///
    /// Sorties de VillageLayout en phase 16 pour servir aussi au mini-jeu Le Plan : c'est ce
    /// qui rend « verifie solvable par le calcul » vrai pour ses quinze plans — la solution
    /// n'est pas ecrite, elle est deduite, par les memes regles que les 32 panneaux du village.
    /// Ces 32 sont ressortis identiques au refactor, compares case par case.
    ///
    /// Les regles, et elles sont celles du Code :
    ///
    /// 1. A chaque carrefour, l'axe qui TRAVERSE est prioritaire ; un bras en T qui y
    ///    debouche est secondaire. A une croix, l'axe de la route prioritaire s'il y passe,
    ///    sinon l'axe est-ouest. Un bras secondaire recoit un CEDEZ LE PASSAGE, ou un STOP s'il
    ///    debouche sur la route prioritaire.
    /// 2. Le panneau se pose A DROITE de la chaussee dans le sens de la marche, UNE CASE
    ///    AVANT la ligne du carrefour. Si l'herbe manque a droite, une case plus loin ;
    ///    sinon rien, et l'appelant le dit.
    /// 3. Un bras qui est une impasse recoit un panneau IMPASSE a son entree, a droite du
    ///    conducteur qui s'y engage.
    /// 4. La route prioritaire s'annonce a chacune de ses deux entrees, ROUTE PRIORITAIRE,
    ///    et se clot a chacune de ses deux sorties, FIN DE ROUTE PRIORITAIRE.
    /// 5. Un bras d'au plus DEUX cases qui finit sur une maison est un PASSAGE D'ACCES :
    ///    ni cedez, ni impasse. Le Code ne signale pas les entrees de garage.
    ///
    /// Deux panneaux peuvent revendiquer la meme case — l'impasse d'un bras et le cedez du
    /// bras voisin partagent la diagonale. Les panneaux de PRIORITE se posent en premier :
    /// c'est l'impasse qui recule d'une case, jamais le cedez.
    /// </summary>
    public static class RoadSignRules
    {
        /// <summary>
        /// Un bras d'au plus deux cases qui finit sur une maison : un acces, pas une rue. Le
        /// Code ne signale ni les entrees de garage, ni un cul-de-sac qui ne dessert qu'une
        /// maison a deux pas du carrefour.
        /// </summary>
        public const int DrivewayLength = 2;

        public static readonly Vector2Int[] Steps =
        {
            Vector2Int.up, Vector2Int.right, Vector2Int.down, Vector2Int.left
        };

        /// <summary>
        /// Derive les panneaux d'une carte. Chaque panneau qui n'a trouve aucune herbe libre
        /// est consigne dans problems, jamais tu : le village en fait un avertissement, le
        /// mini-jeu un refus.
        /// </summary>
        public static List<RoadSign> Derive(IRoadGrid grid, List<string> problems)
        {
            List<RoadSign> signs = new List<RoadSign>();
            HashSet<Vector2Int> taken = new HashSet<Vector2Int>();
            List<Vector2Int> junctions = new List<Vector2Int>();

            for (int y = 0; y < grid.Height; y++)
            {
                for (int x = 0; x < grid.Width; x++)
                {
                    Vector2Int cell = new Vector2Int(x, y);
                    if (grid.IsRoad(cell) && RoadDegree(grid, cell) >= 3)
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
                bool verticalPriority = PriorityIsVertical(grid, junction);

                foreach (Vector2Int arm in Steps)
                {
                    if (!grid.IsRoad(junction + arm) || (arm.x == 0) == verticalPriority)
                    {
                        continue;
                    }

                    int deadEnd = DeadEndArmLength(grid, junction, arm);
                    if (deadEnd > 0 && deadEnd <= DrivewayLength)
                    {
                        continue;                       // un acces, pas une rue
                    }

                    Vector2Int travel = -arm;           // le conducteur arrive PAR ce bras
                    RoadSignKind kind = OnMainRoad(grid, junction) ? RoadSignKind.Stop : RoadSignKind.Yield;
                    Place(grid, signs, taken, problems, junction, arm, RightOf(travel), kind, travel);
                }
            }

            // SECONDE PASSE, les impasses : a l'entree du bras, a droite de qui s'y engage.
            foreach (Vector2Int junction in junctions)
            {
                foreach (Vector2Int arm in Steps)
                {
                    int length = grid.IsRoad(junction + arm) ? DeadEndArmLength(grid, junction, arm) : -1;
                    if (length <= DrivewayLength)
                    {
                        continue;
                    }

                    Place(grid, signs, taken, problems, junction, arm, RightOf(arm), RoadSignKind.DeadEnd, arm);
                }
            }

            // TROISIEME PASSE, la route prioritaire : annoncee a l'entree, close a la sortie.
            IList<Vector2Int> corners = grid.MainRoadCorners;
            if (corners != null && corners.Count >= 2)
            {
                Vector2Int west = corners[0];
                Vector2Int east = corners[corners.Count - 1];
                Vector2Int intoWest = Direction(corners[0], corners[1]);
                Vector2Int intoEast = Direction(east, corners[corners.Count - 2]);

                Place(grid, signs, taken, problems, west, intoWest, RightOf(intoWest), RoadSignKind.Priority, intoWest);
                Place(grid, signs, taken, problems, west, intoWest, RightOf(-intoWest), RoadSignKind.PriorityEnd, -intoWest);
                Place(grid, signs, taken, problems, east, intoEast, RightOf(intoEast), RoadSignKind.Priority, intoEast);
                Place(grid, signs, taken, problems, east, intoEast, RightOf(-intoEast), RoadSignKind.PriorityEnd, -intoEast);
            }

            return signs;
        }

        private static int RoadDegree(IRoadGrid grid, Vector2Int cell)
        {
            int degree = 0;
            foreach (Vector2Int step in Steps)
            {
                if (grid.IsRoad(cell + step))
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
        public static Vector2Int RightOf(Vector2Int direction)
        {
            return new Vector2Int(direction.y, -direction.x);
        }

        /// <summary>Vrai si la case est sur la polyligne de la route prioritaire.</summary>
        public static bool OnMainRoad(IRoadGrid grid, Vector2Int cell)
        {
            IList<Vector2Int> corners = grid.MainRoadCorners;
            if (corners == null)
            {
                return false;
            }

            for (int i = 0; i + 1 < corners.Count; i++)
            {
                Vector2Int a = corners[i];
                Vector2Int b = corners[i + 1];

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
        /// Rend -1 si le bras rejoint un autre carrefour — ou s'il SORT DE LA CARTE : une rue
        /// qui quitte le cadre continue ailleurs, elle ne finit pas.
        ///
        /// La longueur compte : un bras d'UNE case qui finit sur une maison est un passage
        /// d'acces, pas une rue, et le Code n'y met aucun panneau.
        /// </summary>
        private static int DeadEndArmLength(IRoadGrid grid, Vector2Int junction, Vector2Int direction)
        {
            Vector2Int previous = junction;
            Vector2Int current = junction + direction;
            int length = 1;

            for (int guard = 0; guard < grid.Width * grid.Height; guard++)
            {
                if (grid.IsExit(current))
                {
                    return -1;
                }

                Vector2Int next = Vector2Int.zero;
                int exits = 0;

                foreach (Vector2Int step in Steps)
                {
                    Vector2Int candidate = current + step;
                    if (candidate == previous || !grid.IsRoad(candidate))
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

        private static Vector2Int Direction(Vector2Int from, Vector2Int to)
        {
            Vector2Int delta = to - from;
            return new Vector2Int(System.Math.Sign(delta.x), System.Math.Sign(delta.y));
        }

        /// <summary>
        /// A un carrefour, l'axe prioritaire est-il nord-sud ? En T, c'est l'axe qui a ses
        /// deux bras. En croix, celui de la route prioritaire s'il y passe, sinon l'est-ouest.
        /// </summary>
        private static bool PriorityIsVertical(IRoadGrid grid, Vector2Int junction)
        {
            bool north = grid.IsRoad(junction + Vector2Int.up);
            bool south = grid.IsRoad(junction + Vector2Int.down);
            bool east = grid.IsRoad(junction + Vector2Int.right);
            bool west = grid.IsRoad(junction + Vector2Int.left);

            bool verticalThrough = north && south;
            bool horizontalThrough = east && west;

            if (verticalThrough != horizontalThrough)
            {
                return verticalThrough;
            }

            if (OnMainRoad(grid, junction))
            {
                return OnMainRoad(grid, junction + Vector2Int.up) || OnMainRoad(grid, junction + Vector2Int.down);
            }

            return false;
        }

        /// <summary>
        /// Pose un panneau au bord du bras : une case dans le bras depuis le carrefour, puis
        /// un pas de cote. Si la case n'est pas de l'herbe libre, une case plus loin dans le
        /// bras ; sinon on renonce en le disant, jamais en silence.
        /// </summary>
        private static void Place(IRoadGrid grid, List<RoadSign> signs, HashSet<Vector2Int> taken,
            List<string> problems, Vector2Int junction, Vector2Int arm, Vector2Int side,
            RoadSignKind kind, Vector2Int facing)
        {
            for (int distance = 1; distance <= 2; distance++)
            {
                Vector2Int onRoad = junction + arm * distance;
                if (!grid.IsRoad(onRoad))
                {
                    break;
                }

                Vector2Int cell = onRoad + side;
                if (!grid.IsFreeGrass(cell) || taken.Contains(cell))
                {
                    continue;
                }

                taken.Add(cell);
                signs.Add(new RoadSign(cell, kind, facing));
                return;
            }

            problems?.Add($"Pas d'herbe libre pour un panneau {kind} au bord du bras {arm} du " +
                          $"carrefour {junction} : il n'est pas posé.");
        }
    }
}
