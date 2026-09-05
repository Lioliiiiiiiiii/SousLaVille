using System.Collections.Generic;
using UnityEngine;

namespace SousLaVille.EditorTools
{
    /// <summary>
    /// LES QUINZE PLANS DU MINI-JEU LE PLAN, phase 16. Ecrits a la main dans le style de
    /// VillageLayout, et JAMAIS engendres : chacun est relu, et la difficulte progresse dans un
    /// ordre choisi. Decision du 3 septembre 2026.
    ///
    /// Legende
    ///   .  herbe            #  rue            =  rue PRIORITAIRE          A  maison (bloquant)
    ///
    /// Une rue qui touche le bord CONTINUE hors du plan : ce n'est pas une impasse. Une rue qui
    /// finit devant une maison en est une, si elle fait plus de deux cases — sinon c'est un
    /// acces, et le Code n'y met rien. La rue prioritaire se dessine avec « = » ; ses coins sont
    /// deduits, et elle doit etre d'un seul trait.
    ///
    /// Les POSTES — ou un panneau manque — et le panneau attendu a chacun ne sont PAS ecrits ici :
    /// ils sont DERIVES par RoadSignRules, les memes regles que les 32 panneaux du village. C'est
    /// ce qui rend « verifie solvable par le calcul » vrai : une solution ecrite a la main
    /// pourrait etre fausse, une solution deduite du Code ne peut pas l'etre.
    ///
    /// Neuf cases sur cinq au plus : une case fait 32 px a l'ecran, le plancher de CLAUDE.md, et
    /// 9 x 32 = 288 sur 320, 5 x 32 = 160 sur 180.
    /// </summary>
    public static class PlanLayout
    {
        public const int MaxWidth = 9;
        public const int MaxHeight = 5;

        public const char Grass = '.';
        public const char Road = '#';
        public const char MainRoad = '=';
        public const char House = 'A';

        /// <summary>Un plan : son nom, pour les messages, et ses lignes, du haut vers le bas.</summary>
        public readonly struct Plan
        {
            public Plan(string name, params string[] rows)
            {
                Name = name;
                Rows = rows;
            }

            public string Name { get; }
            public string[] Rows { get; }

            public int Width => Rows.Length > 0 ? Rows[0].Length : 0;
            public int Height => Rows.Length;

            /// <summary>La case (x, y), y vers le HAUT comme dans VillageLayout. Herbe hors du plan.</summary>
            public char At(int x, int y)
            {
                if (x < 0 || x >= Width || y < 0 || y >= Height)
                {
                    return Grass;
                }

                return Rows[Height - 1 - y][x];
            }
        }

        /// <summary>
        /// Du plus simple au plus lourd. Le cedez seul, puis l'impasse, puis la route prioritaire
        /// et le stop, puis les combinaisons. Un commentaire par plan dit ce qu'il apprend.
        /// </summary>
        public static readonly Plan[] Plans =
        {
            // 1. Un T : la rue du bas debouche, elle cede.
            new Plan("Le premier cedez",
                ".......",
                "#######",
                "...#..."),

            // 2. Le meme, retourne : le panneau change de cote avec le sens du conducteur.
            new Plan("Le cedez d'en haut",
                "...#...",
                "#######",
                "......."),

            // 3. Une croix sans route prioritaire : l'est-ouest passe, le nord-sud cede.
            new Plan("La croix",
                "....#....",
                "#########",
                "....#...."),

            // 4. Une rue qui finit devant une maison : elle cede, et elle est une impasse.
            new Plan("L'impasse",
                "....#....",
                "....#....",
                "....####A",
                "....#....",
                "....#...."),

            // 5. Deux rues debouchent, une de chaque cote : deux cedez, pas du meme bord.
            new Plan("Deux rues",
                "..#......",
                "..#......",
                "#########",
                "......#..",
                "......#.."),

            // 6. Une vraie rue et une entree de garage : la seconde n'a droit a rien.
            new Plan("L'entree de garage",
                "....#....",
                "....#....",
                "#########",
                "......#..",
                "......A.."),

            // 7. La route prioritaire seule : annoncee a l'entree, close a la sortie, deux fois.
            new Plan("La route prioritaire",
                ".........",
                ".........",
                "=========",
                ".........",
                "........."),

            // 8. Une rue debouche sur la route prioritaire : ce n'est plus un cedez, c'est un stop.
            new Plan("Le premier stop",
                ".........",
                ".........",
                "=========",
                "....#....",
                "....#...."),

            // 9. Une croix dont un bras est une impasse : le cedez prend la place, l'impasse recule.
            new Plan("L'impasse qui recule",
                "....#....",
                "....#....",
                "########A",
                "....#....",
                "....#...."),

            // 10. Une croix sur la route prioritaire : deux stops.
            new Plan("La croix prioritaire",
                "....#....",
                "....#....",
                "=========",
                "....#....",
                "....#...."),

            // 11. La route prioritaire tourne, et une rue y debouche.
            new Plan("Le virage prioritaire",
                "....=....",
                "....=....",
                "=====....",
                "..#......",
                "..#......"),

            // 12. Une rue debouche sur la prioritaire, et une impasse debouche sur cette rue.
            new Plan("L'impasse de la rue",
                "....#....",
                "....####A",
                "....#....",
                "=========",
                "........."),

            // 13. Une croix et une rue sur la route prioritaire : trois stops.
            new Plan("Trois stops",
                "...#..#..",
                "...#..#..",
                "=========",
                "...#.....",
                "...#....."),

            // 14. Le virage prioritaire avec une rue de chaque cote, et une fin de route qui recule.
            new Plan("Le virage a deux rues",
                "....=....",
                "....=####",
                "=====....",
                "..#......",
                "..#......"),

            // 15. Une croix et une rue, des deux cotes de la route prioritaire.
            new Plan("Le grand carrefour",
                "...#.....",
                "...#.....",
                "=========",
                "...#.#...",
                "...#.#...")
        };

        /// <summary>Un plan vu par le Code de la route.</summary>
        public sealed class PlanRoadGrid : IRoadGrid
        {
            private readonly Plan plan;
            private readonly List<Vector2Int> corners;

            public PlanRoadGrid(Plan plan)
            {
                this.plan = plan;
                corners = MainRoadCornersOf(plan);
            }

            public int Width => plan.Width;
            public int Height => plan.Height;

            public bool IsRoad(Vector2Int cell)
            {
                char c = plan.At(cell.x, cell.y);
                return Inside(cell) && (c == Road || c == MainRoad);
            }

            public bool IsFreeGrass(Vector2Int cell) => Inside(cell) && plan.At(cell.x, cell.y) == Grass;

            /// <summary>Une rue qui touche le bord continue hors du plan.</summary>
            public bool IsExit(Vector2Int cell) =>
                IsRoad(cell) && (cell.x == 0 || cell.y == 0 || cell.x == Width - 1 || cell.y == Height - 1);

            public IList<Vector2Int> MainRoadCorners => corners;

            private bool Inside(Vector2Int cell) =>
                cell.x >= 0 && cell.x < Width && cell.y >= 0 && cell.y < Height;
        }

        /// <summary>
        /// Les coins de la route prioritaire, deduits des cases « = » : un bout, puis chaque
        /// changement de direction, puis l'autre bout. Vide s'il n'y a pas de « = ». Null si
        /// les « = » ne font pas un seul trait simple — le validateur le dit.
        /// </summary>
        public static List<Vector2Int> MainRoadCornersOf(Plan plan)
        {
            List<Vector2Int> cells = new List<Vector2Int>();
            for (int y = 0; y < plan.Height; y++)
            {
                for (int x = 0; x < plan.Width; x++)
                {
                    if (plan.At(x, y) == MainRoad)
                    {
                        cells.Add(new Vector2Int(x, y));
                    }
                }
            }

            if (cells.Count == 0)
            {
                return new List<Vector2Int>();
            }

            HashSet<Vector2Int> set = new HashSet<Vector2Int>(cells);
            List<Vector2Int> ends = new List<Vector2Int>();

            foreach (Vector2Int cell in cells)
            {
                int degree = 0;
                foreach (Vector2Int step in RoadSignRules.Steps)
                {
                    if (set.Contains(cell + step))
                    {
                        degree++;
                    }
                }

                if (degree > 2 || degree == 0 && cells.Count > 1)
                {
                    return null;
                }

                if (degree <= 1)
                {
                    ends.Add(cell);
                }
            }

            if (cells.Count < 2 || ends.Count != 2)
            {
                return null;
            }

            // On suit le trait d'un bout a l'autre, et on note chaque coude.
            List<Vector2Int> corners = new List<Vector2Int> { ends[0] };
            Vector2Int previous = ends[0];
            Vector2Int current = ends[0];
            Vector2Int heading = Vector2Int.zero;
            int walked = 1;

            while (current != ends[1])
            {
                Vector2Int next = Vector2Int.zero;
                bool found = false;

                foreach (Vector2Int step in RoadSignRules.Steps)
                {
                    Vector2Int candidate = current + step;
                    if (candidate != previous && set.Contains(candidate))
                    {
                        next = candidate;
                        found = true;
                        break;
                    }
                }

                if (!found)
                {
                    return null;
                }

                Vector2Int direction = next - current;
                if (heading != Vector2Int.zero && direction != heading)
                {
                    corners.Add(current);
                }

                heading = direction;
                previous = current;
                current = next;
                walked++;
            }

            if (walked != cells.Count)
            {
                return null;                            // un trait, mais pas tout le trait
            }

            corners.Add(ends[1]);
            return corners;
        }

        /// <summary>Les panneaux d'un plan, et ce qui n'a pas pu se poser.</summary>
        public static List<RoadSign> Derive(Plan plan, List<string> problems)
        {
            return RoadSignRules.Derive(new PlanRoadGrid(plan), problems);
        }

        /// <summary>
        /// Les quinze plans tiennent-ils debout ? Chacun : au plus 9 x 5, lignes egales,
        /// caracteres connus, rues d'un seul tenant, route prioritaire d'un seul trait, AU
        /// MOINS UN poste, AUCUN panneau sans herbe, et a chaque carrefour de la route
        /// prioritaire les bras qui ne cedent pas sont bien la route prioritaire — sinon une
        /// rue ordinaire passerait pour prioritaire et la route prioritaire marquerait le stop.
        /// </summary>
        public static bool Validate()
        {
            bool ok = true;

            if (Plans.Length != 15)
            {
                Debug.LogError($"[Sous la Ville] {Plans.Length} plan(s) : la décision du 3 septembre en veut quinze.");
                ok = false;
            }

            for (int i = 0; i < Plans.Length; i++)
            {
                ok &= ValidatePlan(i, Plans[i]);
            }

            return ok;
        }

        private static bool ValidatePlan(int index, Plan plan)
        {
            string label = $"Le plan {index + 1} « {plan.Name} »";
            bool ok = true;

            if (plan.Height == 0 || plan.Height > MaxHeight || plan.Width == 0 || plan.Width > MaxWidth)
            {
                Debug.LogError($"[Sous la Ville] {label} fait {plan.Width} x {plan.Height} : au plus " +
                               $"{MaxWidth} x {MaxHeight}, une case faisant 32 px à l'écran.");
                return false;
            }

            foreach (string row in plan.Rows)
            {
                if (row.Length != plan.Width)
                {
                    Debug.LogError($"[Sous la Ville] {label} : une ligne fait {row.Length} caractères " +
                                   $"au lieu de {plan.Width}.");
                    return false;
                }

                foreach (char c in row)
                {
                    if (c != Grass && c != Road && c != MainRoad && c != House)
                    {
                        Debug.LogError($"[Sous la Ville] {label} porte un caractère inconnu « {c} ».");
                        return false;
                    }
                }
            }

            PlanRoadGrid grid = new PlanRoadGrid(plan);

            if (grid.MainRoadCorners == null)
            {
                Debug.LogError($"[Sous la Ville] {label} : les « = » ne font pas un seul trait simple.");
                return false;
            }

            if (!RoadsConnected(grid))
            {
                Debug.LogError($"[Sous la Ville] {label} : ses rues ne sont pas d'un seul tenant.");
                ok = false;
            }

            List<string> problems = new List<string>();
            List<RoadSign> signs = Derive(plan, problems);

            foreach (string problem in problems)
            {
                Debug.LogError($"[Sous la Ville] {label} : {problem} Un poste que le Code exige et " +
                               "que le plan ne peut pas porter est un plan à redessiner.");
                ok = false;
            }

            if (signs.Count == 0)
            {
                Debug.LogError($"[Sous la Ville] {label} n'appelle aucun panneau : il n'y a rien à jouer.");
                ok = false;
            }

            // A un carrefour de la route prioritaire, les bras non signales doivent ETRE la
            // route prioritaire. Un coude de la prioritaire dans l'axe d'une rue ordinaire
            // ferait ceder la prioritaire elle-meme, en silence.
            HashSet<Vector2Int> signalled = new HashSet<Vector2Int>();
            foreach (RoadSign sign in signs)
            {
                if (sign.Kind == RoadSignKind.Stop || sign.Kind == RoadSignKind.Yield)
                {
                    // Le poste est a droite du conducteur qui arrive par le bras : le bras est
                    // la case de route voisine du poste dans le sens du conducteur, a sa gauche.
                    signalled.Add(sign.Cell - RoadSignRules.RightOf(sign.Facing));
                }
            }

            for (int y = 0; y < plan.Height; y++)
            {
                for (int x = 0; x < plan.Width; x++)
                {
                    Vector2Int junction = new Vector2Int(x, y);
                    if (!grid.IsRoad(junction) || !RoadSignRules.OnMainRoad(grid, junction))
                    {
                        continue;
                    }

                    int degree = 0;
                    foreach (Vector2Int step in RoadSignRules.Steps)
                    {
                        if (grid.IsRoad(junction + step))
                        {
                            degree++;
                        }
                    }

                    if (degree < 3)
                    {
                        continue;
                    }

                    foreach (Vector2Int step in RoadSignRules.Steps)
                    {
                        Vector2Int arm = junction + step;
                        if (!grid.IsRoad(arm) || RoadSignRules.OnMainRoad(grid, arm) || signalled.Contains(arm))
                        {
                            continue;
                        }

                        // Un acces de deux cases au plus n'est pas signale non plus, et c'est voulu.
                        if (plan.At(arm.x, arm.y) == Road && IsDriveway(grid, junction, step))
                        {
                            continue;
                        }

                        Debug.LogError($"[Sous la Ville] {label} : au carrefour {junction} de la route " +
                                       $"prioritaire, le bras {arm} n'est ni prioritaire ni signalé. " +
                                       "La route prioritaire y céderait le passage à une rue ordinaire.");
                        ok = false;
                    }
                }
            }

            return ok;
        }

        private static bool IsDriveway(PlanRoadGrid grid, Vector2Int junction, Vector2Int step)
        {
            Vector2Int previous = junction;
            Vector2Int current = junction + step;

            for (int length = 1; length <= RoadSignRules.DrivewayLength; length++)
            {
                if (grid.IsExit(current))
                {
                    return false;
                }

                Vector2Int next = Vector2Int.zero;
                int exits = 0;
                foreach (Vector2Int s in RoadSignRules.Steps)
                {
                    Vector2Int candidate = current + s;
                    if (candidate != previous && grid.IsRoad(candidate))
                    {
                        exits++;
                        next = candidate;
                    }
                }

                if (exits == 0)
                {
                    return true;
                }

                if (exits > 1)
                {
                    return false;
                }

                previous = current;
                current = next;
            }

            return false;
        }

        private static bool RoadsConnected(PlanRoadGrid grid)
        {
            Vector2Int start = new Vector2Int(-1, -1);
            int total = 0;

            for (int y = 0; y < grid.Height; y++)
            {
                for (int x = 0; x < grid.Width; x++)
                {
                    Vector2Int cell = new Vector2Int(x, y);
                    if (grid.IsRoad(cell))
                    {
                        total++;
                        if (start.x < 0)
                        {
                            start = cell;
                        }
                    }
                }
            }

            if (total == 0)
            {
                return false;
            }

            HashSet<Vector2Int> seen = new HashSet<Vector2Int> { start };
            Queue<Vector2Int> queue = new Queue<Vector2Int>();
            queue.Enqueue(start);

            while (queue.Count > 0)
            {
                Vector2Int cell = queue.Dequeue();
                foreach (Vector2Int step in RoadSignRules.Steps)
                {
                    Vector2Int next = cell + step;
                    if (grid.IsRoad(next) && seen.Add(next))
                    {
                        queue.Enqueue(next);
                    }
                }
            }

            return seen.Count == total;
        }
    }
}
