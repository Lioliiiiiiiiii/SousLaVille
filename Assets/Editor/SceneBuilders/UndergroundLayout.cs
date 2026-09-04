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
    /// Un second plan, superpose au premier, donne la profondeur de chaque case : 1 peu
    /// profond, 2 moyen, 3 profond. Trois zones concentriques autour de la station, qui seule
    /// est au fond. C'est cette carte qui porte tout le puzzle, la regle de CLAUDE.md etant
    /// qu'un segment ne transporte que si la profondeur ne diminue pas vers la station.
    ///
    /// Deux plans superposes plutot qu'un alphabet a neuf lettres : chacun reste lisible.
    ///
    /// Legende
    ///   #  terre pleine (bloquant)   .  galerie creusee
    ///   E  echelle vers la surface   T  arrivee sous la station
    ///   A  alcove d'une maison, deja creusee
    ///   R  le bassin d'orage, au centre d'une chambre de trois sur trois deja creusee
    ///   O  l'arrivee de la fontaine du parc, une alcove d'une case comme celle d'une maison
    ///
    /// E, T, R et O sont des marqueurs : le builder peint du sol de galerie dessous et pose un
    /// GameObject par-dessus.
    ///
    /// PHASE 11. L'alcove de la fontaine est en (20, 15), a la PROFONDEUR 1 : sa route gele
    /// donc en hiver et se bouche en automne, comme celle des trois maisons peu profondes. Le
    /// plan des profondeurs n'a pas bouge d'un caractere, et les cinq routes de maison gardent
    /// leurs longueurs, 57 / 46 / 31 / 45 / 6 segments. Sa propre route fait 36 segments, et
    /// elle a ete verifiee par calcul avant que l'alcove soit creusee.
    /// </summary>
    public static class UndergroundLayout
    {
        public const int Width = VillageLayout.Width;
        public const int Height = VillageLayout.Height;

        public const char Earth = '#';
        public const char Tunnel = '.';
        public const char Ladder = 'E';
        public const char PlantOutlet = 'T';
        public const char HouseOutlet = 'A';
        public const char Reserve = 'R';
        public const char Fountain = 'O';

        /// <summary>La chambre du bassin : trois cases sur trois autour du marqueur.</summary>
        public const int ReserveChamberRadius = 1;

        /// <summary>Profondeur maximale de la carte. La station y est, et elle seule.</summary>
        public const int MaxDepth = 3;

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
            "#####A....##############################",
            "######..E.##############################",
            "#######...##############################",
            "########.####A#############A############",
            "########.###############################",
            "########.###########O###################",
            "########....############################",
            "########..R.############################",
            "########....############################",
            "########.##########...##################",
            "########............E.#######A##########",
            "###################...##################",
            "####################.###################",
            "####################.###################",
            "####################.###########...#####",
            "####################.............E.#####",
            "################################..A#####",
            "########################################",
            "########################################",
            "########################################",
            "########################################",
        };

        /// <summary>
        /// Profondeur de chaque case, meme orientation que Rows. Zones concentriques autour
        /// de la station : 3 jusqu'a cinq cases de distance, 2 jusqu'a vingt, 1 au-dela.
        ///
        /// Deux cretes peu profondes traversent la couronne, a quatorze et a huit cases de la
        /// station, chacune percee d'une seule porte, les deux portes opposees. Une crete a
        /// profondeur 1 posee au milieu de la profondeur 2 est un mur pour l'eau : un trajet
        /// deja descendu a 2 ne peut pas y remonter. Il faut trouver la porte.
        ///
        /// Solvabilite verifiee avant ecriture : chaque maison garde un chemin a profondeur
        /// non decroissante jusqu'a la station, avec un detour de zero a seize cases.
        /// </summary>
        private static readonly string[] DepthRows =
        {
            "2212233322122222122222211111111111111111",
            "2122333332212222212222221111111111111111",
            "1223333333222222221222222111111111111111",
            "2233333333322222222122222211111111111111",
            "2333333333332222222212222221111111111111",
            "2233333333322222222122222211111111111111",
            "1223333333222222221222222111111111111111",
            "2122333332212222212222221111111111111111",
            "2212233322122222122222211111111111111111",
            "2221223221222221222222111111111111111111",
            "2222122212222212222221111111111111111111",
            "2222212122222122222211111111111111111111",
            "1222221222221222222111111111111111111111",
            "2122222222212222221111111111111111111111",
            "2212222222122222211111111111111111111111",
            "2221222221222222111111111111111111111111",
            "2222122222222221111111111111111111111111",
            "2222222222222211111111111111111111111111",
            "2222222222222111111111111111111111111111",
            "1222222222221111111111111111111111111111",
            "1122222222211111111111111111111111111111",
            "1112222222111111111111111111111111111111",
            "1111222221111111111111111111111111111111",
            "1111122211111111111111111111111111111111",
            "1111112111111111111111111111111111111111",
            "1111111111111111111111111111111111111111",
            "1111111111111111111111111111111111111111",
            "1111111111111111111111111111111111111111",
            "1111111111111111111111111111111111111111",
            "1111111111111111111111111111111111111111",
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

        /// <summary>Profondeur de la case (x, y) : 1, 2 ou 3. Hors carte, la plus faible.</summary>
        public static int DepthAt(int x, int y)
        {
            if (x < 0 || x >= Width || y < 0 || y >= Height)
            {
                return 1;
            }

            return DepthRows[Height - 1 - y][x] - '0';
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

            if (DepthRows.Length != Height)
            {
                Debug.LogError($"[Sous la Ville] Le plan des profondeurs fait {DepthRows.Length} " +
                               $"lignes, il en faut {Height}.");
                return false;
            }

            for (int row = 0; row < DepthRows.Length; row++)
            {
                if (DepthRows[row].Length != Width)
                {
                    Debug.LogError($"[Sous la Ville] Ligne {row} des profondeurs : " +
                                   $"{DepthRows[row].Length} caractères au lieu de {Width}.");
                    return false;
                }

                foreach (char c in DepthRows[row])
                {
                    if (c < '1' || c > '0' + MaxDepth)
                    {
                        Debug.LogError($"[Sous la Ville] Profondeur « {c} » ligne {row} : " +
                                       $"attendu 1 à {MaxDepth}.");
                        return false;
                    }
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

            // Chaque maison doit avoir son alcove juste dessous, sinon elle ne pourra jamais
            // etre raccordee et le joueur chercherait pour rien.
            List<Vector2Int> villageHouses = VillageLayout.FindAll(VillageLayout.House);
            List<Vector2Int> outlets = FindAll(HouseOutlet);

            if (villageHouses.Count != outlets.Count)
            {
                Debug.LogError($"[Sous la Ville] {villageHouses.Count} maison(s) en surface mais " +
                               $"{outlets.Count} alcôve(s) au sous-sol.");
                ok = false;
            }

            foreach (Vector2Int house in villageHouses)
            {
                if (At(house.x, house.y) != HouseOutlet)
                {
                    Debug.LogError($"[Sous la Ville] Aucune alcôve sous la maison {house}.");
                    ok = false;
                }
            }

            foreach (Vector2Int outlet in outlets)
            {
                if (VillageLayout.At(outlet.x, outlet.y) != VillageLayout.House)
                {
                    Debug.LogError($"[Sous la Ville] Alcôve orpheline en {outlet} : " +
                                   "aucune maison au-dessus.");
                    ok = false;
                }
            }

            ok &= ValidateReserve();

            Vector2Int plant = VillageLayout.FindSingle(VillageLayout.PlantInlet);
            if (At(plant.x, plant.y) != PlantOutlet)
            {
                Debug.LogError($"[Sous la Ville] Aucune arrivée de station sous {plant}.");
                ok = false;
            }

            // L'eau doit pouvoir descendre jusqu'a la station : elle est le point le plus
            // profond de la carte, sinon aucun reseau ne peut aboutir.
            if (DepthAt(plant.x, plant.y) != MaxDepth)
            {
                Debug.LogError($"[Sous la Ville] La station en {plant} est à la profondeur " +
                               $"{DepthAt(plant.x, plant.y)}, il lui faut {MaxDepth}.");
                ok = false;
            }

            return ok;
        }

        /// <summary>
        /// Le bassin d'orage, phase 8 : un seul, au centre d'une chambre deja creusee,
        /// entierement sous de l'herbe, ni maison, ni atelier, ni station au-dessus. Il
        /// doit aussi pouvoir descendre vers la station, donc ne pas etre deja au fond.
        /// </summary>
        private static bool ValidateReserve()
        {
            List<Vector2Int> reserves = FindAll(Reserve);

            if (reserves.Count != 1)
            {
                Debug.LogError($"[Sous la Ville] {reserves.Count} bassin(s) au sous-sol, il en faut " +
                               "exactement un.");
                return false;
            }

            Vector2Int reserve = reserves[0];
            bool ok = true;

            for (int dy = -ReserveChamberRadius; dy <= ReserveChamberRadius; dy++)
            {
                for (int dx = -ReserveChamberRadius; dx <= ReserveChamberRadius; dx++)
                {
                    int x = reserve.x + dx;
                    int y = reserve.y + dy;

                    if (!IsOpen(x, y))
                    {
                        Debug.LogError($"[Sous la Ville] La chambre du bassin n'est pas creusée en " +
                                       $"({x}, {y}).");
                        ok = false;
                    }

                    if (VillageLayout.At(x, y) != VillageLayout.Grass)
                    {
                        Debug.LogError($"[Sous la Ville] Le bassin en {reserve} n'est pas sous de " +
                                       $"l'herbe : « {VillageLayout.At(x, y)} » en ({x}, {y}).");
                        ok = false;
                    }
                }
            }

            if (DepthAt(reserve.x, reserve.y) >= MaxDepth)
            {
                Debug.LogError($"[Sous la Ville] Le bassin en {reserve} est déjà au fond : il ne " +
                               "pourrait plus descendre vers la station.");
                ok = false;
            }

            return ok;
        }
    }
}
