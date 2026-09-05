using System.Collections.Generic;
using UnityEngine;

namespace SousLaVille.EditorTools
{
    /// <summary>
    /// Le plan des interieurs, ecrit a la main comme celui du village et celui du sous-sol.
    ///
    /// Une difference de forme, et une seule : ce plan n'est pas une grande grille de trente
    /// lignes, mais UNE GRILLE PAR PIECE, posee dans un creneau de la carte. Ajouter un
    /// batiment, c'est ajouter un bloc et un creneau, sans toucher aux pieces existantes.
    /// Une grande grille aurait oblige a rouvrir les lignes des voisins a chaque batiment.
    ///
    /// La carte fait 40x30, decoupee en six creneaux de 20 sur 10. Elle porte ses PROPRES
    /// dimensions et ne suit pas celles du village : la phase 12b a porte les deux autres
    /// couches a 64x45 sans que celle-ci bouge, les pieces n'ayant aucune raison de grandir.
    ///
    /// Un creneau est exactement la vue de la camera, 320x180 a PPU 16 : la piece tient
    /// donc a l'ecran d'un seul tenant, sans rien faire defiler.
    ///
    /// Legende
    ///   #  mur (bloquant)     .  sol      D  porte, vers le village
    ///   C  plaque exposee     P  echantillon de tuyau      V  personnage
    ///   S  panneau expose du catalogue (NE bloque PAS : on marche dessus pour le lire)
    ///
    /// C, P, S et V sont des marqueurs : le builder peint du sol dessous et pose un GameObject
    /// par-dessus. Les echantillons ne bloquent pas, on marche dessus pour les choisir ; le
    /// personnage, lui, bloque : on ne traverse pas quelqu'un, et debout sur lui on ne
    /// pourrait plus lui parler.
    ///
    /// Tout ce qui n'appartient a aucune piece est infranchissable : voir InteriorMap, qui
    /// refuse toute case hors piece plutot que de peindre six cents tuiles de mur qu'on ne
    /// verra jamais.
    /// </summary>
    public static class InteriorsLayout
    {
        public const int Width = 40;
        public const int Height = 30;

        public const int RoomWidth = 20;
        public const int RoomHeight = 10;

        public const char Wall = '#';
        public const char Floor = '.';
        public const char Door = 'D';
        public const char Cover = 'C';
        public const char PipeSample = 'P';
        public const char Villager = 'V';

        /// <summary>
        /// Un panneau du catalogue expose, phase 13. Il ne bloque pas : on marche dessus et
        /// son nom s'affiche au HUD, exactement comme une plaque ou un echantillon de tuyau
        /// depuis la phase 9. Un panneau ne se prend pas, il se lit.
        /// </summary>
        public const char SignSample = 'S';

        /// <summary>Une piece : son bloc dessine et le coin bas gauche de son creneau.</summary>
        public readonly struct Room
        {
            public Room(string name, Vector2Int origin, int villagers, string[] rows)
            {
                Name = name;
                Origin = origin;
                Villagers = villagers;
                Rows = rows;
            }

            /// <summary>Nom de la piece, pour l'objet de scene et les messages d'erreur.</summary>
            public string Name { get; }

            /// <summary>Coin bas gauche du creneau, en cases de la carte.</summary>
            public Vector2Int Origin { get; }

            /// <summary>
            /// Combien de personnages cette piece doit porter, ni plus ni moins. Le nombre
            /// est ECRIT A COTE DU PLAN QU'IL DECRIT : IsWellFormed s'y compare, si bien
            /// qu'un V efface ou un V de trop est refuse a la construction.
            ///
            /// Jusqu'a la phase 13 le validateur exigeait EXACTEMENT UN personnage par
            /// piece, et le commentaire de InteriorsSceneBuilder qui promettait le contraire
            /// etait faux : l'usine a panneaux en veut trois, un par mini-jeu.
            /// </summary>
            public int Villagers { get; }

            /// <summary>Ligne 0 en haut, comme on lit une carte.</summary>
            public string[] Rows { get; }

            /// <summary>Bornes de la piece en cases, murs compris.</summary>
            public RectInt Bounds => new RectInt(Origin.x, Origin.y, RoomWidth, RoomHeight);
        }

        /// <summary>
        /// L'atelier des plaques. Huit plaques en deux rangees de quatre, espacees de quatre
        /// cases : le nom AMSTERDAM mesure 3,4 cases de large, et deux cartels voisins se
        /// chevaucheraient a moins. Le meme espacement qu'en phase 7, dans la cour.
        ///
        /// Les plaques sont en x = 3, 7, 11 et 15 plutot qu'en x = 2, 6, 10 et 14 : un cartel
        /// centre sur x = 2 deborderait sur le mur de gauche.
        ///
        /// L'artisan se tient AU MILIEU DE LA PIECE, entre les deux rangees de plaques, et
        /// non devant la porte. Corrige le 4 septembre 2026 apres l'avoir vu : plante a deux
        /// cases de l'entree, il avait l'air d'attendre dans le couloir. Au centre, il tient
        /// sa boutique, et la camera etant centree sur la piece, il est aussi au centre de
        /// l'ecran quand on entre.
        /// </summary>
        private static readonly Room CoverWorkshop = new Room(
            "Atelier des plaques",
            new Vector2Int(0, 20),
            1,
            new[]
            {
                "####################",
                "#..................#",
                "#..C...C...C...C...#",
                "#..................#",
                "#........V.........#",
                "#..................#",
                "#..C...C...C...C...#",
                "#..................#",
                "#..................#",
                "#########D##########",
            });

        /// <summary>
        /// L'usine a tuyaux. Trois echantillons seulement, largement espaces, exposes
        /// derriere l'ouvrier comme derriere un comptoir.
        ///
        /// Ni nom ni picto de saison dans le decor depuis le 4 septembre 2026 : les deux
        /// s'affichent au HUD quand on marche sur un echantillon. Voir ItemLabel.
        ///
        /// L'ouvrier se tient au meme endroit que l'artisan chez le voisin, au milieu de sa
        /// piece : les deux batiments s'apprennent une seule fois.
        /// </summary>
        private static readonly Room PipeWorks = new Room(
            "Usine a tuyaux",
            new Vector2Int(20, 20),
            1,
            new[]
            {
                "####################",
                "#..................#",
                "#....P....P....P...#",
                "#..................#",
                "#........V.........#",
                "#..................#",
                "#..................#",
                "#..................#",
                "#..................#",
                "#########D##########",
            });

        /// <summary>
        /// L'USINE A PANNEAUX, phase 13. Une galerie : vingt-quatre panneaux du Code de la
        /// route francais en QUATRE RANGEES DE SIX, UNE FAMILLE PAR RANGEE, dans l'ordre de
        /// la planche — intersection et priorite, danger, interdiction, obligation.
        ///
        /// C'est la grammaire qui s'apprend d'abord : la forme et la bordure disent la
        /// FAMILLE avant que le dessin dise le detail. Un triangle borde de rouge previent,
        /// un disque borde de rouge interdit, un disque bleu plein oblige.
        ///
        /// La geometrie n'est pas libre. Un panneau fait 16x24 au pivot du joueur : il
        /// occupe donc de y - 0,5 a y + 1,0. Deux rangees ecartees de DEUX laissent une
        /// demi-case de blanc entre la plaque du bas et le poteau du haut ; a une case elles
        /// se chevaucheraient. Et les personnages se tiennent en COLONNES IMPAIRES, ou
        /// aucun panneau ne se dresse : sinon un panneau leur passerait devant le visage.
        ///
        /// On entre en (9, 0) et le personnage du milieu est droit devant. (9, 1) reste libre :
        /// aucun personnage ne bouche l'entree.
        ///
        /// LA RANGEE DU HAUT EST LAISSEE VIDE, et ce n'est pas de l'esthetique. Un creneau
        /// fait dix lignes quand la camera en montre 11,25 : la piece est donc centree, et la
        /// rangee y = 8 tombe DERRIERE la rangee de gouttes du HUD. Vu a l'ecran : la premiere
        /// famille etait a moitie cachee. Tout le tableau est descendu d'une rangee.
        ///
        /// CRENEAU (0, 0), et ce choix n'est pas indifferent. Un creneau fait dix lignes
        /// quand la camera en montre 11,25 : on voit donc 0,625 ligne du voisin du dessus et
        /// du dessous. En (0, 10) ce serait la rangee de mur de l'atelier, suspendue en
        /// l'air. Ici le voisin du dessus est vide et le dessous est hors carte, exactement
        /// ce que montrent deja les deux pieces existantes.
        /// </summary>
        private static readonly Room SignFactory = new Room(
            "Usine a panneaux",
            new Vector2Int(0, 0),
            3,
            new[]
            {
                "####################",
                "#..................#",   // y = 8  LAISSEE VIDE : la rangee de gouttes du HUD
                "#...S.S.S.S.S.S....#",   // y = 7  INTERSECTION ET PRIORITE
                "#..................#",
                "#...S.S.S.S.S.S....#",   // y = 5  DANGER
                "#..V.....V.....V...#",   // y = 4  Le Stock, La Fabrique, Le Plan
                "#...S.S.S.S.S.S....#",   // y = 3  INTERDICTION
                "#..................#",
                "#...S.S.S.S.S.S....#",   // y = 1  OBLIGATION
                "#########D##########",
            });

        /// <summary>
        /// Les pieces du jeu. L'ORDRE EST CELUI DE VillageLayout.Facades ET .Doors : la
        /// troisieme piece a la troisieme facade et la troisieme porte, des deux cotes du
        /// passage. Trois creneaux restent libres.
        /// </summary>
        public static readonly Room[] Rooms = { CoverWorkshop, PipeWorks, SignFactory };

        /// <summary>
        /// Caractere de la case (x, y), ou le mur si la case n'appartient a aucune piece.
        /// y compte du bas vers le haut, comme les tilemaps d'Unity.
        /// </summary>
        public static char At(int x, int y)
        {
            foreach (Room room in Rooms)
            {
                int localX = x - room.Origin.x;
                int localY = y - room.Origin.y;

                if (localX < 0 || localX >= RoomWidth || localY < 0 || localY >= RoomHeight)
                {
                    continue;
                }

                return room.Rows[RoomHeight - 1 - localY][localX];
            }

            return Wall;
        }

        /// <summary>Vrai si la case appartient a une piece, murs compris.</summary>
        public static bool IsInsideARoom(int x, int y)
        {
            foreach (Room room in Rooms)
            {
                if (room.Bounds.Contains(new Vector2Int(x, y)))
                {
                    return true;
                }
            }

            return false;
        }

        /// <summary>Toutes les cases d'une piece portant un marqueur, balayees du bas vers le haut.</summary>
        public static List<Vector2Int> FindAll(Room room, char marker)
        {
            List<Vector2Int> cells = new List<Vector2Int>();

            for (int y = room.Origin.y; y < room.Origin.y + RoomHeight; y++)
            {
                for (int x = room.Origin.x; x < room.Origin.x + RoomWidth; x++)
                {
                    if (At(x, y) == marker)
                    {
                        cells.Add(new Vector2Int(x, y));
                    }
                }
            }

            return cells;
        }

        /// <summary>Marqueur attendu en un seul exemplaire dans une piece.</summary>
        public static Vector2Int FindSingle(Room room, char marker)
        {
            List<Vector2Int> cells = FindAll(room, marker);

            if (cells.Count != 1)
            {
                Debug.LogError($"[Sous la Ville] La pièce « {room.Name} » porte {cells.Count} " +
                               $"marqueur(s) « {marker} », il en faut exactement un.");
                return cells.Count > 0 ? cells[0] : Vector2Int.zero;
            }

            return cells[0];
        }

        /// <summary>
        /// Vrai si chaque piece fait bien dix lignes de vingt caracteres, tient dans la
        /// carte, ne chevauche aucune autre, porte exactement UNE porte et exactement le
        /// nombre de personnages qu'elle declare.
        ///
        /// PHASE 13 : le nombre de personnages n'est plus fige a un, il est celui que la
        /// piece annonce. On reste sur un EXACTEMENT N — une faute de frappe est toujours
        /// refusee — mais l'usine a panneaux peut en porter trois, un par mini-jeu.
        ///
        /// Ce filet compte les personnages ; il ne dit rien de QUI ils sont. C'est
        /// InteriorsSceneBuilder qui les apparie CASE PAR CASE, et qui refuse une case que
        /// sa table ne connait pas.
        /// </summary>
        public static bool IsWellFormed()
        {
            for (int i = 0; i < Rooms.Length; i++)
            {
                Room room = Rooms[i];

                if (room.Rows.Length != RoomHeight)
                {
                    Debug.LogError($"[Sous la Ville] La pièce « {room.Name} » fait " +
                                   $"{room.Rows.Length} lignes, il en faut {RoomHeight}.");
                    return false;
                }

                for (int row = 0; row < room.Rows.Length; row++)
                {
                    if (room.Rows[row].Length != RoomWidth)
                    {
                        Debug.LogError($"[Sous la Ville] Ligne {row} de « {room.Name} » : " +
                                       $"{room.Rows[row].Length} caractères au lieu de {RoomWidth}.");
                        return false;
                    }
                }

                RectInt bounds = room.Bounds;
                if (bounds.xMin < 0 || bounds.yMin < 0 || bounds.xMax > Width || bounds.yMax > Height)
                {
                    Debug.LogError($"[Sous la Ville] La pièce « {room.Name} » sort de la carte.");
                    return false;
                }

                for (int other = 0; other < i; other++)
                {
                    if (bounds.Overlaps(Rooms[other].Bounds))
                    {
                        Debug.LogError($"[Sous la Ville] Les pièces « {room.Name} » et " +
                                       $"« {Rooms[other].Name} » se chevauchent.");
                        return false;
                    }
                }

                if (FindAll(room, Door).Count != 1)
                {
                    Debug.LogError($"[Sous la Ville] La pièce « {room.Name} » doit avoir " +
                                   "exactement une porte.");
                    return false;
                }

                int villagers = FindAll(room, Villager).Count;
                if (villagers != room.Villagers)
                {
                    Debug.LogError($"[Sous la Ville] La pièce « {room.Name} » porte " +
                                   $"{villagers} personnage(s), il en faut exactement " +
                                   $"{room.Villagers}.");
                    return false;
                }
            }

            return true;
        }
    }
}
