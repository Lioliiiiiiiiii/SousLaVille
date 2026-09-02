using System.Collections.Generic;
using UnityEngine;

namespace SousLaVille.EditorTools
{
    /// <summary>
    /// Le plan du sous-sol, ecrit a la main. Trente lignes de quarante caracteres, exactement
    /// alignees case pour case sur celles du village : la case (8, 19) du sous-sol est
    /// directement sous la case (8, 19) de la surface.
    ///
    /// Au depart tout est de la terre pleine, sauf une salle sous chaque bouche et sous la
    /// station, et les galeries qui les relient. Soixante-quatorze cases praticables sur mille
    /// deux cents : de quoi marcher des la phase 2, et tout le reste a ouvrir en phase 3.
    ///
    /// Legende
    ///   #  terre pleine (bloquant)   .  galerie creusee
    ///   E  echelle vers la surface   T  arrivee sous la station
    ///
    /// E et T sont des marqueurs : le builder peint du sol de galerie dessous et pose un
    /// GameObject par-dessus.
    /// </summary>
    public static class UndergroundLayout
    {
        public const int Width = VillageLayout.Width;
        public const int Height = VillageLayout.Height;

        public const char Earth = '#';
        public const char Tunnel = '.';
        public const char Ladder = 'E';
        public const char PlantOutlet = 'T';

        /// <summary>Ligne 0 en haut, comme on lit la carte. La conversion en case se fait dans At.</summary>
        private static readonly string[] Rows =
        {
            "########################################",
            "########################################",
            "########################################",
            "#####...################################",
            "#####.T.################################",
            "#####...################################",
            "######.#################################",
            "######.#################################",
            "######.#################################",
            "######....##############################",
            "######..E.##############################",
            "#######...##############################",
            "########.###############################",
            "########.###############################",
            "########.###############################",
            "########.###############################",
            "########.###############################",
            "########.###############################",
            "########.##########...##################",
            "########............E.##################",
            "###################...##################",
            "####################.###################",
            "####################.###################",
            "####################.###########...#####",
            "####################.............E.#####",
            "################################...#####",
            "########################################",
            "########################################",
            "########################################",
            "########################################",
        };

        /// <summary>
        /// Caractere de la case (x, y). y compte du bas vers le haut, comme les tilemaps
        /// d'Unity, alors que la carte se lit du haut vers le bas.
        /// </summary>
        public static char At(int x, int y)
        {
            if (x < 0 || x >= Width || y < 0 || y >= Height)
            {
                return Earth;
            }

            return Rows[Height - 1 - y][x];
        }

        /// <summary>Vrai si la case se parcourt au depart. Les marqueurs sont creuses.</summary>
        public static bool IsOpen(int x, int y)
        {
            return At(x, y) != Earth;
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

        /// <summary>Vrai si la carte fait bien 30 lignes de 40 caracteres.</summary>
        public static bool IsWellFormed()
        {
            if (Rows.Length != Height)
            {
                Debug.LogError($"[Sous la Ville] Le plan du sous-sol fait {Rows.Length} lignes, " +
                               $"il en faut {Height}.");
                return false;
            }

            for (int row = 0; row < Rows.Length; row++)
            {
                if (Rows[row].Length != Width)
                {
                    Debug.LogError($"[Sous la Ville] Ligne {row} du plan du sous-sol : " +
                                   $"{Rows[row].Length} caractères au lieu de {Width}.");
                    return false;
                }
            }

            return true;
        }

        /// <summary>
        /// Verifie l'alignement des deux cartes : une echelle sous chaque bouche, une arrivee
        /// sous la station, et rien d'orphelin. C'est le seul vrai risque de deux plans ecrits
        /// a la main : un portail mal place ne planterait pas, il serait juste mort.
        /// </summary>
        public static bool ValidateAgainstVillage()
        {
            bool ok = true;

            List<Vector2Int> manholes = VillageLayout.FindAll(VillageLayout.Manhole);
            List<Vector2Int> ladders = FindAll(Ladder);

            if (manholes.Count != ladders.Count)
            {
                Debug.LogError($"[Sous la Ville] {manholes.Count} bouche(s) en surface mais " +
                               $"{ladders.Count} échelle(s) au sous-sol.");
                ok = false;
            }

            foreach (Vector2Int manhole in manholes)
            {
                if (At(manhole.x, manhole.y) != Ladder)
                {
                    Debug.LogError($"[Sous la Ville] Aucune échelle sous la bouche {manhole}.");
                    ok = false;
                }
            }

            foreach (Vector2Int ladder in ladders)
            {
                if (VillageLayout.At(ladder.x, ladder.y) != VillageLayout.Manhole)
                {
                    Debug.LogError($"[Sous la Ville] Échelle orpheline en {ladder} : " +
                                   "aucune bouche au-dessus.");
                    ok = false;
                }
            }

            Vector2Int plant = VillageLayout.FindSingle(VillageLayout.PlantInlet);
            if (At(plant.x, plant.y) != PlantOutlet)
            {
                Debug.LogError($"[Sous la Ville] Aucune arrivée de station sous {plant}.");
                ok = false;
            }

            return ok;
        }
    }
}
