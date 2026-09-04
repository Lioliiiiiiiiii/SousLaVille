using System.Collections.Generic;
using UnityEngine;

namespace SousLaVille.EditorTools
{
    /// <summary>
    /// Le plan du sous-sol, ecrit a la main. Quarante-cinq lignes de soixante-quatre
    /// caracteres, exactement
    /// alignees case pour case sur celles du village : la case (8, 19) du sous-sol est
    /// directement sous la case (8, 19) de la surface.
    ///
    /// Au depart tout est de la terre pleine, sauf une chambre sous chaque echelle et sous la
    /// station, et l'ANCIEN COLLECTEUR du village : le reseau qui existait avant que le joueur
    /// arrive, pose sous les routes de la plaine. 217 cases praticables sur 2880, soit 7,5 %,
    /// le ratio de la phase 11 (88 sur 1200). Sans lui la pire destination coute 118 appuis
    /// sur Espace au lieu de 108, et le total passe de 368 a 431 : le pre-creusement est du
    /// CONTENU, pas de la mise a l'echelle.
    ///
    /// Le collecteur s'ARRETE A r = 33, en deca de la crete la plus exterieure : aucune case
    /// de porte n'est jamais livree creusee, et le plan le verifie avant d'etre emis.
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
    /// PHASE 12B. La carte passe a 64x45 et le plan des profondeurs est redessine. Il se
    /// resume a une formule exacte, plus trois listes de portes ecrites a la main :
    ///
    ///   r = distance de MANHATTAN a la station (6, 25)
    ///   r <= 8   -> profondeur 3      r > 31  -> profondeur 1      sinon profondeur 2
    ///   sauf r = 12, 19 et 26, les TROIS CRETES, forcees a 1 hors de leurs portes.
    ///
    /// Une crete est un anneau peu profond pose au milieu de la couronne : un trajet deja
    /// descendu a 2 ne peut pas y remonter a 1, donc elle est un mur pour l'eau et il faut
    /// trouver la porte. Les portes font cinq cases chacune et sont a 90 degres l'une de
    /// l'autre : est pour r = 26, sud pour r = 19, nord pour r = 12. On les traverse donc en
    /// zigzag, et c'est ce zigzag qui fait le puzzle.
    ///
    /// Le rayon exterieur de la couronne decide le RYTHME DE L'HIVER : le nombre de segments
    /// a profondeur 1 d'une route vaut exactement r_destination - 31. freezeMaxDepth vaut 1
    /// et frostResistance(Standard) vaut 0, donc chacun de ces segments gele a CHAQUE hiver,
    /// probabilite 1. La destination la plus lointaine, (61, 8) a r = 72, en compte 41.
    ///
    /// Solvabilite VERIFIEE PAR CALCUL AVANT ECRITURE, la methode des cretes de la phase 4 :
    /// les treize destinations et le bassin gardent un chemin a profondeur non decroissante
    /// jusqu'a la station. ValidateDepthPuzzle le reverifie a chaque construction, et 2844
    /// des 2880 cases restent vivantes.
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
            "################################################################",
            "################################################################",
            "################################################################",
            "##########################################################A#####",
            "##############################################.............#####",
            "#############################################..#################",
            "#############################################A.#################",
            "#############################################..#################",
            "##############################################.##...############",
            "###########################A##################....E.############",
            "########..A###############...#################.##...############",
            "########.E.###############.E.#################.#################",
            "########...###############...#################.#################",
            "###########################.##################.#################",
            "###########################.##################..........A#######",
            "##################A###########################.#########.#######",
            "##################.###########################.#################",
            "##############################################.#################",
            "#####...######################################.#################",
            "#####.T.###############################....A..................##",
            "#####...###################################.##.#####.#####.##.##",
            "######.#######################################.#####.####...#.##",
            "######.#######################################.#####.####.E.#.##",
            "######.#######################################.#####.####...#.##",
            "#####A########################################.#####.########.##",
            "#####.########################################.#####.########.##",
            "##############################################.#####.########.##",
            "##########.##A################################.#####A########.##",
            "##########.##.################################.#####.########.##",
            "##########.###################################.##############.##",
            "#########...################O#################.##############.##",
            "#########.R.################.#################.##############.##",
            "########....##################################.##############.##",
            "########.E.###################################.##...#########.##",
            "########...###################################....E.#########.##",
            "#################################################...#########.##",
            "#############################################################A##",
            "#############################################################.##",
            "################################################################",
            "##################################.#############################",
            "##########################...#####A#############################",
            "##########################.E.......#############################",
            "##########################...###################################",
            "################################################################",
            "################################################################",
        };

        /// <summary>
        /// Profondeur de chaque case, meme orientation que Rows. Anneaux COMPLETS de
        /// Manhattan autour de la station : 3 jusqu'a huit cases, 2 jusqu'a trente et une,
        /// 1 au-dela. Repartition : 1809 / 930 / 141.
        ///
        /// TROIS cretes peu profondes traversent la couronne, a douze, dix-neuf et vingt-six
        /// cases de la station, chacune percee d'une porte de CINQ cases, les trois portes a
        /// 90 degres l'une de l'autre. Voir le resume de la classe pour la formule complete.
        /// </summary>
        private static readonly string[] DepthRows =
        {
            "2222221222222122222111111111111111111111111111111111111111111111",
            "2222212122222212222211111111111111111111111111111111111111111111",
            "2222122212222221222221111111111111111111111111111111111111111111",
            "2221222221222222122222111111111111111111111111111111111111111111",
            "2212222222122222212222211111111111111111111111111111111111111111",
            "2122222222212222221222221111111111111111111111111111111111111111",
            "1222222222221222222122222111111111111111111111111111111111111111",
            "2222222222222122222212222211111111111111111111111111111111111111",
            "2222222222222212222221222221111111111111111111111111111111111111",
            "2222222222222221222222122222111111111111111111111111111111111111",
            "2221222221222222122222212222211111111111111111111111111111111111",
            "2212223222122222212222221222221111111111111111111111111111111111",
            "2122233322212222221222222122222111111111111111111111111111111111",
            "1222333332221222222122222212222211111111111111111111111111111111",
            "2223333333222122222212222221222221111111111111111111111111111111",
            "2233333333322212222221222222122222111111111111111111111111111111",
            "2333333333332221222222122222212222211111111111111111111111111111",
            "3333333333333222122222212222222222221111111111111111111111111111",
            "3333333333333322212222221222222222222111111111111111111111111111",
            "3333333333333332221222222122222222222211111111111111111111111111",
            "3333333333333322212222221222222222222111111111111111111111111111",
            "3333333333333222122222212222222222221111111111111111111111111111",
            "2333333333332221222222122222212222211111111111111111111111111111",
            "2233333333322212222221222222122222111111111111111111111111111111",
            "2223333333222122222212222221222221111111111111111111111111111111",
            "1222333332221222222122222212222211111111111111111111111111111111",
            "2122233322212222221222222122222111111111111111111111111111111111",
            "2212223222122222212222221222221111111111111111111111111111111111",
            "2221222221222222122222212222211111111111111111111111111111111111",
            "2222122212222221222222122222111111111111111111111111111111111111",
            "2222212122222212222221222221111111111111111111111111111111111111",
            "2222221222222122222212222211111111111111111111111111111111111111",
            "1222222222221222222122222111111111111111111111111111111111111111",
            "2122222222212222221222221111111111111111111111111111111111111111",
            "2212222222122222212222211111111111111111111111111111111111111111",
            "2221222221222222122222111111111111111111111111111111111111111111",
            "2222222222222221222221111111111111111111111111111111111111111111",
            "2222222222222212222211111111111111111111111111111111111111111111",
            "2222222222222122222111111111111111111111111111111111111111111111",
            "1222222222221222221111111111111111111111111111111111111111111111",
            "2122222222212222211111111111111111111111111111111111111111111111",
            "2212222222122222111111111111111111111111111111111111111111111111",
            "2221222221222221111111111111111111111111111111111111111111111111",
            "2222122212222211111111111111111111111111111111111111111111111111",
            "2222212122222111111111111111111111111111111111111111111111111111",
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

        /// <summary>Vrai si la carte fait bien 45 lignes de 64 caracteres.</summary>
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

            // LA FONTAINE, appariee depuis la phase 12a. Elle etait le SEUL couple
            // surface/sous-sol que personne ne verifiait : les deux etaient en (20, 15) par la
            // seule discipline de la main. Deplacer l'un sans l'autre eteint la fontaine en
            // silence, FloodView demandant au solveur la case de SURFACE qu'il ne connait plus.
            List<Vector2Int> villageFountains = VillageLayout.FindAll(VillageLayout.Fountain);
            List<Vector2Int> fountainOutlets = FindAll(Fountain);

            if (villageFountains.Count != fountainOutlets.Count)
            {
                Debug.LogError($"[Sous la Ville] {villageFountains.Count} fontaine(s) en surface " +
                               $"mais {fountainOutlets.Count} arrivée(s) au sous-sol.");
                ok = false;
            }

            foreach (Vector2Int fountain in villageFountains)
            {
                if (At(fountain.x, fountain.y) != Fountain)
                {
                    Debug.LogError($"[Sous la Ville] Aucune arrivée sous la fontaine {fountain}.");
                    ok = false;
                }
            }

            foreach (Vector2Int outlet in fountainOutlets)
            {
                if (VillageLayout.At(outlet.x, outlet.y) != VillageLayout.Fountain)
                {
                    Debug.LogError($"[Sous la Ville] Arrivée de fontaine orpheline en {outlet} : " +
                                   "aucune fontaine au-dessus.");
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

            // Le puzzle lui-meme : chaque destination doit etre atteignable a profondeur non
            // decroissante. C'est la seule verification qui regarde le PLAN DES PROFONDEURS, et
            // c'est celle qui manquait depuis la phase 3.
            ok &= ValidateDepthPuzzle();

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
        /// <summary>
        /// Le puzzle est-il resolvable ? Parcours en largeur DEPUIS LA STATION, a profondeur
        /// non croissante en remontant, ce qui est exactement l'inverse de la regle de
        /// CLAUDE.md : l'eau ne va vers la station que si la profondeur ne diminue pas. Une
        /// destination que ce parcours n'atteint pas ne pourra JAMAIS etre desservie, quoi que
        /// le joueur creuse.
        ///
        /// Ajoute en phase 12a. Jusque-la, rien ne verifiait le plan des profondeurs, et la
        /// mesure est brutale : supprimer UNE SEULE porte de crete fait tomber les destinations
        /// desservables de sept a une, et les cases vivantes de 1197 a 125. Le pire mode de
        /// panne n'est pas celui-la : c'est celui ou une seule route sur sept change, qu'aucun
        /// controle par echantillon ne verrait.
        ///
        /// Le parcours ignore ce qui est deja creuse : n'importe quelle case peut l'etre. Seule
        /// la profondeur decide, et elle ne se creuse pas.
        /// </summary>
        public static bool ValidateDepthPuzzle()
        {
            List<Vector2Int> plants = FindAll(PlantOutlet);
            if (plants.Count != 1)
            {
                Debug.LogError($"[Sous la Ville] {plants.Count} station(s) au sous-sol, il en " +
                               "faut exactement une.");
                return false;
            }

            HashSet<Vector2Int> reachable = ReachableFromPlant(plants[0]);

            bool ok = true;
            foreach (Vector2Int destination in Destinations())
            {
                if (reachable.Contains(destination))
                {
                    continue;
                }

                Debug.LogError($"[Sous la Ville] La destination {destination}, profondeur " +
                               $"{DepthAt(destination.x, destination.y)}, ne peut JAMAIS être " +
                               "desservie : aucun chemin à profondeur non décroissante ne la " +
                               "relie à la station. Le plan des profondeurs l'enferme.");
                ok = false;
            }

            // Le compte des ATTEINTES, pas le total : la version de la phase 12a imprimait
            // Destinations().Count, si bien qu'elle annoncait « 14 destinations atteignables »
            // sur la ligne qui suivait neuf refus. Un bilan qui ne peut pas dire non ne vaut
            // pas mieux qu'un validateur qui dit toujours oui.
            List<Vector2Int> all = Destinations();
            int served = 0;
            foreach (Vector2Int destination in all)
            {
                if (reachable.Contains(destination))
                {
                    served++;
                }
            }

            Debug.Log($"[Sous la Ville] Puzzle des profondeurs : {reachable.Count} case(s) " +
                      $"vivante(s) sur {Width * Height}, {served} destination(s) " +
                      $"atteignable(s) sur {all.Count}.");

            return ok;
        }

        /// <summary>
        /// Les cases d'ou l'eau peut atteindre la station. On remonte depuis elle, donc on
        /// n'accepte un voisin que si sa profondeur ne DEPASSE pas celle de la case courante :
        /// c'est la regle de l'ecoulement, lue a l'envers.
        /// </summary>
        private static HashSet<Vector2Int> ReachableFromPlant(Vector2Int plant)
        {
            Vector2Int[] steps =
            {
                Vector2Int.up, Vector2Int.right, Vector2Int.down, Vector2Int.left
            };

            HashSet<Vector2Int> seen = new HashSet<Vector2Int> { plant };
            Queue<Vector2Int> queue = new Queue<Vector2Int>();
            queue.Enqueue(plant);

            while (queue.Count > 0)
            {
                Vector2Int cell = queue.Dequeue();
                int depth = DepthAt(cell.x, cell.y);

                foreach (Vector2Int step in steps)
                {
                    Vector2Int next = cell + step;

                    if (next.x < 0 || next.x >= Width || next.y < 0 || next.y >= Height)
                    {
                        continue;
                    }

                    // En remontant le courant, la profondeur ne peut que diminuer ou tenir.
                    if (DepthAt(next.x, next.y) > depth || !seen.Add(next))
                    {
                        continue;
                    }

                    queue.Enqueue(next);
                }
            }

            return seen;
        }

        /// <summary>Toutes les destinations du monde : maisons, fontaine, bassin.</summary>
        public static List<Vector2Int> Destinations()
        {
            List<Vector2Int> cells = new List<Vector2Int>();
            cells.AddRange(FindAll(HouseOutlet));
            cells.AddRange(FindAll(Fountain));
            cells.AddRange(FindAll(Reserve));
            return cells;
        }

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
