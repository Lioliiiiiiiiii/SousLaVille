using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEngine;
using UnityEngine.Tilemaps;

namespace SousLaVille.EditorTools
{
    /// <summary>
    /// Ecrit les placeholders graphiques : de vrais fichiers PNG, puis les assets Tile
    /// correspondants. Des carres de couleurs franches, le pixel art viendra a la toute fin,
    /// une fois le gameplay valide.
    ///
    /// Chaque tuile porte un lisere 1 px plus sombre : deux carres de meme couleur poses cote
    /// a cote restent distincts, ce qui aide a lire la grille.
    /// </summary>
    public static partial class PlaceholderArtGenerator
    {
        public const string TilesFolder = "Assets/Art/Tiles";
        public const string SpritesFolder = "Assets/Art/Sprites";
        public const string PictosFolder = "Assets/Art/Pictos";

        // Assets Tile consommes par SurfaceSceneBuilder.
        public const string TileGrass = TilesFolder + "/Tile_Grass.asset";
        public const string TilePath = TilesFolder + "/Tile_Path.asset";
        public const string TilePark = TilesFolder + "/Tile_Park.asset";
        public const string TilePlantFloor = TilesFolder + "/Tile_PlantFloor.asset";
        public const string TileHedge = TilesFolder + "/Tile_Hedge.asset";
        public const string TilePlantWall = TilesFolder + "/Tile_PlantWall.asset";

        // Sprites poses sur des GameObjects, pas peints dans une tilemap.
        public const string ManholeTexture = SpritesFolder + "/manhole.png";
        public const string PlantWallTexture = TilesFolder + "/tile_plant_wall.png";
        public const string LadderTexture = SpritesFolder + "/ladder.png";

        // Les maisons, phase 4.
        public const string HouseTexture = SpritesFolder + "/house.png";
        public const string HouseInletTexture = SpritesFolder + "/house_inlet.png";
        public const string TileHouse = TilesFolder + "/Tile_House.asset";

        // Le personnage, un sprite par direction. Decide le 2 septembre 2026.
        public const string PlayerDown = SpritesFolder + "/player_down.png";
        public const string PlayerUp = SpritesFolder + "/player_up.png";
        public const string PlayerLeft = SpritesFolder + "/player_left.png";
        public const string PlayerRight = SpritesFolder + "/player_right.png";

        // Pictogrammes : le jeu parle par panneaux, pas par phrases.
        public const string PictoSurface = PictosFolder + "/picto_surface.png";
        public const string PictoUnderground = PictosFolder + "/picto_underground.png";
        public const string PictoDown = PictosFolder + "/picto_down.png";
        public const string PictoUp = PictosFolder + "/picto_up.png";
        public const string PictoDig = PictosFolder + "/picto_dig.png";
        public const string PictoRemove = PictosFolder + "/picto_remove.png";
        public const string CursorTarget = PictosFolder + "/cursor_target.png";
        public const string PictoDropFull = PictosFolder + "/picto_drop_full.png";
        public const string PictoDropEmpty = PictosFolder + "/picto_drop_empty.png";
        public const string PictoRepair = PictosFolder + "/picto_repair.png";

        // Les quatre saisons, phase 5. Chacune dit sa couleur avant de dire son motif :
        // le fond suffit a reconnaitre la saison du coin de l'oeil.
        public const string PictoSpring = PictosFolder + "/picto_season_spring.png";
        public const string PictoSummer = PictosFolder + "/picto_season_summer.png";
        public const string PictoAutumn = PictosFolder + "/picto_season_autumn.png";
        public const string PictoWinter = PictosFolder + "/picto_season_winter.png";

        // La plaque d'egout du joueur, phase 7. Huit motifs inspires de styles regionaux,
        // dessines ici : la geometrie est originale, jamais l'embleme d'une ville reelle.
        public static readonly string[] CoverNames =
        {
            "PARIS", "TOKYO", "BERLIN", "NEW YORK",
            "AMSTERDAM", "LONDRES", "ROME", "LISBONNE"
        };

        public const int CoverCount = 8;

        public const string TileWorkshop = TilesFolder + "/Tile_Workshop.asset";
        public const string VillageMapTexture = SpritesFolder + "/village_map.png";

        // Phase 9a, les batiments. Le pave de l'atelier de la phase 7 sert desormais de sol
        // aux pieces : c'est la meme matiere, elle est simplement passee a l'interieur.
        /// <summary>L'eau du village, phase 10. Semi-transparente : on voit le sol dessous.</summary>
        public const string TileWater = TilesFolder + "/Tile_Water.asset";

        /// <summary>
        /// La fontaine du parc, phase 11, SEPAREE EN DEUX EN PHASE 12C sur le patron exact de
        /// house.png / tile_house.png. La tuile bloquante et le sprite sortaient du meme
        /// fichier de seize sur seize, ce qui clouait la fontaine a la taille d'une case : elle
        /// ne pouvait pas grandir sans que le mur du parc grandisse avec elle.
        /// </summary>
        public const string FountainTexture = TilesFolder + "/tile_fountain.png";
        public const string TileFountain = TilesFolder + "/Tile_Fountain.asset";
        public const string FountainSprite = SpritesFolder + "/fountain.png";

        // ---- PHASE 12C, LE DECOR -------------------------------------------------------
        //
        // Seize tuiles par famille, masque de raccord, sur le patron EXACT de BuildPipe :
        // bit 0 nord, 1 est, 2 sud, 3 ouest. Sans elles un labyrinthe de trois cents cases est
        // trois cents carres verts identiques, et deux cent quarante-cinq cases de route sont
        // une nappe beige sans direction.
        public const int DecorMaskCount = 16;

        /// <summary>L'arbre, phase 12c. Bloquant comme une maison, et de la meme hauteur.</summary>
        public const string TreeTexture = SpritesFolder + "/tree.png";
        public const string TreeTileTexture = TilesFolder + "/tile_tree.png";
        public const string TileTree = TilesFolder + "/Tile_Tree.asset";

        /// <summary>
        /// Le catalogue de panneaux, phase 12c. Quatre panneaux de rue et quatre panneaux de
        /// direction, un par point cardinal. L'USINE A PANNEAUX DE LA PHASE 13 REPREND CE
        /// CATALOGUE au lieu d'en creer un second : c'est le premier dessin, pas le troisieme,
        /// et la regle 4 de CLAUDE.md tient.
        /// </summary>
        public const int SignCount = 29;

        /// <summary>Nombre de panonceaux de jalonnement : nord, est, sud, ouest.</summary>
        public const int SignArrowCount = 4;

        /// <summary>
        /// LA PLANCHE DE L'USINE A PANNEAUX, phase 13. Vingt-quatre rangs, QUATRE FAMILLES DE
        /// SIX, dans l'ordre de lecture de la piece : une famille par rangee.
        ///
        /// Chaque rang existe dans le Code de la route francais et porte son numero. Les
        /// quatre deja dessines en phase 12c gardent leur rang, les vingt neufs prennent les
        /// rangs 9 a 28.
        ///
        /// L'impasse C13a (rang 2) et les quatre panonceaux de jalonnement (rangs 5 a 8) ne
        /// sont d'aucune de ces quatre familles : le village et les galeries les posent, mais
        /// la planche ne les expose pas. Une cinquieme rangee depareillee de cinq casserait
        /// la lecon, qui est justement que la FORME dit la famille.
        /// </summary>
        public static readonly int[] SignBoard =
        {
            // INTERSECTION ET PRIORITE
            9, 10, 0, 1, 3, 4,
            // DANGER
            11, 12, 13, 14, 15, 16,
            // INTERDICTION
            17, 18, 19, 20, 21, 22,
            // OBLIGATION
            23, 24, 25, 26, 27, 28
        };

        /// <summary>
        /// Le nom de chaque rang de la planche, dans l'ordre de SignBoard. Majuscules,
        /// francais, moins de six mots : CLAUDE.md. Victorien sait lire, decision du
        /// 3 septembre, et le nom s'affiche au HUD quand on foule le panneau — comme le nom
        /// d'une plaque ou d'un type de tuyau depuis la phase 9.
        ///
        /// Le plus long, ARRET ET STATIONNEMENT INTERDITS, fait 32 caracteres, soit 193 px
        /// sur les 320 de l'ecran de reference. Le plafond dur de PixelFont est 42.
        /// </summary>
        public static readonly string[] SignBoardNames =
        {
            "PRIORITÉ À DROITE",            // AB1
            "GIRATOIRE",                    // AB25
            "CÉDEZ LE PASSAGE",             // AB3a
            "ARRÊT OBLIGATOIRE",            // AB4
            "ROUTE PRIORITAIRE",            // AB2
            "FIN DE ROUTE PRIORITAIRE",     // AB6

            "VIRAGE À GAUCHE",              // A1b
            "SUCCESSION DE VIRAGES",        // A1c
            "CHAUSSÉE GLISSANTE",           // A4
            "PASSAGE POUR PIÉTONS",         // A13b
            "DANGER",                       // A14
            "DÉBOUCHÉ DE CYCLISTES",        // A21

            "CIRCULATION INTERDITE",        // B0
            "SENS INTERDIT",                // B1
            "INTERDIT DE TOURNER À DROITE", // B2b
            "INTERDIT AUX PIÉTONS",         // B9a
            "STATIONNEMENT INTERDIT",       // B6a1
            "ARRÊT ET STATIONNEMENT INTERDITS", // B6d

            "TOUT DROIT OBLIGATOIRE",       // B21b
            "À DROITE OBLIGATOIRE",         // B21c1
            "À DROITE OU À GAUCHE",         // B21e
            "CONTOURNE PAR LA DROITE",      // B21a1
            "PISTE CYCLABLE",               // B22a
            "CHEMIN POUR PIÉTONS"           // B22b
        };

        /// <summary>Image du nom d'un panneau. Nommee par le RANG, pas par la place sur la planche.</summary>
        public static string SignNameTexture(int kind)
        {
            return $"{SpritesFolder}/sign_name_{Mathf.Clamp(kind, 0, SignCount - 1):00}.png";
        }

        /// <summary>
        /// Les trois personnages de l'usine a panneaux, phase 13, un par mini-jeu a venir :
        /// Le Stock (14), La Fabrique (15), Le Plan (16). Chacun explique son probleme et
        /// invite ; Espace le fait parler, et son mini-jeu se branchera ici.
        /// </summary>
        public static readonly string[] SignFactoryVillagers =
        {
            SpritesFolder + "/villager_stock.png",
            SpritesFolder + "/villager_maker.png",
            SpritesFolder + "/villager_planner.png"
        };

        /// <summary>Ce que disent les trois, dans le meme ordre.</summary>
        public static readonly string[][] SignFactoryLines =
        {
            new[] { "MES PANNEAUX SONT EN DÉSORDRE", "RETROUVE-LES DEUX PAR DEUX" },
            new[] { "JE DESSINE LES PANNEAUX", "SAURAS-TU LES NOMMER ?" },
            new[] { "IL MANQUE DES PANNEAUX ICI", "POSE-LES SUR MON PLAN" }
        };

        /// <summary>Image d'une phrase d'un personnage de l'usine a panneaux.</summary>
        public static string SignFactoryLineTexture(int who, int line)
        {
            return $"{SpritesFolder}/line_signworks_{who:00}_{line:00}.png";
        }

        /// <summary>
        /// LES HUIT LECONS, phase 12e. Un guide par lecon, et rien de plus : le jeu entier
        /// comptait sept phrases avant celle-ci, toutes derriere les portes de deux boutiques,
        /// et toutes sur le choix des plaques et des tuyaux. Rien ne disait le but, rien ne
        /// disait qu'on creuse, et rien ne disait LA REGLE DE PROFONDEUR, qui EST le puzzle
        /// selon CLAUDE.md : `grep "Depth"` dans toute l'UI et tout le code du joueur ne rendait
        /// rien.
        ///
        /// Moins de six mots par phrase, en francais, majuscules : CLAUDE.md. Victorien sait
        /// lire, decision du 3 septembre, et le pictogramme reste le premier choix partout
        /// ailleurs.
        ///
        /// L'ordre est celui des postes, et il ne change pas : GuideSceneOrder l'appareille.
        /// </summary>
        public static readonly string[][] GuideLines =
        {
            // 1. LE BUT. Au depart du village.
            new[] { "RELIE LES MAISONS", "LA STATION LES ATTEND" },
            // 2. LA BOUCHE. Pres d'une bouche.
            new[] { "ON DESCEND PAR LA BOUCHE" },
            // 3. LES SAISONS. En surface.
            new[] { "L'AUTOMNE BOUCHE LES TUYAUX", "L'HIVER GELE LES PEU PROFONDS" },
            // 4. REPARER. Pres d'une flaque.
            new[] { "UNE FLAQUE ? UN TUYAU FUIT", "DESCENDS LE REPARER" },
            // 5. CREUSER. Sous terre, devant la terre pleine.
            new[] { "APPUIE POUR CREUSER LA TERRE" },
            // 6. POSER. Sous terre, devant une galerie.
            new[] { "PUIS POSE UN TUYAU" },
            // 7. LA PROFONDEUR. La lecon centrale, et la seule que rien n'affichait.
            new[] { "L'EAU NE REMONTE JAMAIS", "CREUSE TOUJOURS PLUS PROFOND", "CHERCHE LE PASSAGE" },
            // 8. LE BASSIN. Pres de sa chambre.
            new[] { "L'ORAGE REMPLIT LE BASSIN", "RELIE-LE AUSSI" }
        };

        /// <summary>Image d'une phrase d'un guide.</summary>
        public static string GuideLineTexture(int guide, int line)
        {
            return $"{SpritesFolder}/line_guide_{guide:00}_{line:00}.png";
        }

        /// <summary>
        /// Le signal d'attention d'un guide, phase 12e : un triangle de danger du vocabulaire
        /// routier, la passion de Victorien. Il est pilote par le GUIDE et non par
        /// PlayerInteractor, qui eteint sa bulle des que le joueur regarde ailleurs : il faut
        /// que le signal se voie DE LOIN, sinon il n'appelle personne.
        /// </summary>
        public const string GuideAttention = PictosFolder + "/picto_attention.png";

        /// <summary>
        /// Phase 12d. Le picto d'agrandissement de la station, et le bassin de traitement qui
        /// se pose sur son sol : la station GROSSIT A L'ECRAN d'un bassin a chaque
        /// agrandissement, sinon le progres ne se verrait nulle part.
        /// </summary>
        public const string PictoGrow = PictosFolder + "/picto_grow.png";
        public const string PlantBasinTexture = SpritesFolder + "/plant_basin.png";

        /// <summary>Rang du premier panonceau de direction. Les quatre suivants : nord, est, sud, ouest.</summary>
        public const int SignFirstArrow = 5;

        public static string SignTexture(int kind)
        {
            return $"{SpritesFolder}/sign_{Mathf.Clamp(kind, 0, SignCount - 1):00}.png";
        }

        /// <summary>
        /// LE DOS D'UN PANNEAU, phase 14. C'est la face cachee d'une carte du memory : une
        /// plaque grise, son lisere et la bride qui la tient au poteau.
        ///
        /// Un carre de couleur aurait fait l'affaire, mais un panneau A un dos, et Victorien
        /// le sait : une carte retournee montre donc ce qu'on voit d'un panneau par derriere.
        /// Meme gabarit que les vingt-neuf autres, 16 sur 24, poteau compris.
        /// </summary>
        public const string SignBackTexture = SpritesFolder + "/sign_back.png";

        /// <summary>
        /// Le cadre de choix du memory, 32 px de cote. cursor_target en fait 16 et flotterait
        /// au milieu d'une carte : la carte fait 32, le plancher de CLAUDE.md.
        /// </summary>
        public const string PictoCardCursor = PictosFolder + "/picto_card_cursor.png";

        /// <summary>
        /// LE POTEAU VIDE, phase 16 : un poste du mini-jeu Le Plan ou le panneau manque. Le
        /// poteau des vingt-neuf autres, et a la place de la plaque un pointille : quelque chose
        /// devrait etre la.
        /// </summary>
        public const string SignPostTexture = SpritesFolder + "/sign_post.png";

        /// <summary>L'image de la tuile d'herbe, pour qui la dessine hors tilemap : le plan du mini-jeu.</summary>
        public const string GrassTexture = TilesFolder + "/tile_grass.png";

        public static string HedgeTexture(int mask)
        {
            return $"{TilesFolder}/tile_hedge_{Mathf.Clamp(mask, 0, DecorMaskCount - 1):00}.png";
        }

        public static string TileHedgeMasked(int mask)
        {
            return $"{TilesFolder}/Tile_Hedge_{Mathf.Clamp(mask, 0, DecorMaskCount - 1):00}.asset";
        }

        public static string RoadTexture(int mask)
        {
            return $"{TilesFolder}/tile_path_{Mathf.Clamp(mask, 0, DecorMaskCount - 1):00}.png";
        }

        public static string TileRoadMasked(int mask)
        {
            return $"{TilesFolder}/Tile_Path_{Mathf.Clamp(mask, 0, DecorMaskCount - 1):00}.asset";
        }

        /// <summary>
        /// LES SEIZE FACADES, phase 17b, sur le patron des routes et des haies. Une facade fait
        /// quatre cases sur deux : les cases SANS VOISIN AU NORD sont la rangee du haut, donc le
        /// TOIT, et les autres le mur. Le masque le dit tout seul, sans que le builder ait a
        /// savoir ou commence un batiment.
        /// </summary>
        public static string FacadeTexture(int mask)
        {
            return $"{TilesFolder}/tile_facade_{Mathf.Clamp(mask, 0, DecorMaskCount - 1):00}.png";
        }

        public static string TileFacadeMasked(int mask)
        {
            return $"{TilesFolder}/Tile_Facade_{Mathf.Clamp(mask, 0, DecorMaskCount - 1):00}.asset";
        }

        /// <summary>
        /// LES TROIS ENSEIGNES, phase 17b. Rien ne disait de l'exterieur lequel des trois
        /// batiments etait l'atelier des plaques : trois facades ocres identiques sur l'herbe.
        /// Chacune porte desormais au-dessus de sa porte le SIGNE DE SON METIER — une plaque,
        /// un tuyau, un panneau —, dans l'ordre des pieces d'InteriorsLayout.
        ///
        /// C'est la regle du projet depuis la phase 0 : on reconnait sans un mot.
        /// </summary>
        public static readonly string[] SignboardTextures =
        {
            SpritesFolder + "/signboard_covers.png",
            SpritesFolder + "/signboard_pipes.png",
            SpritesFolder + "/signboard_signs.png"
        };

        /// <summary>
        /// LE MOBILIER DES PIECES, phase 17g. Trois meubles adosses au mur du fond : un etabli,
        /// un rateaux d'outils, une pile de caisses. Les pieces etaient propres et VIDES —
        /// « trois echantillons et deux personnages dans une salle de vingt sur dix : c'est
        /// propre, mais ce n'est pas encore un lieu ».
        ///
        /// Ils se posent SUR LE MUR, jamais sur le sol : une case de mur ne se traverse deja
        /// pas, donc rien de ce que le joueur peut faire ne change, et aucun validateur de
        /// piece n'a a apprendre un marqueur de plus.
        /// </summary>
        public static readonly string[] FurnitureTextures =
        {
            SpritesFolder + "/furniture_bench.png",
            SpritesFolder + "/furniture_rack.png",
            SpritesFolder + "/furniture_crates.png"
        };

        public const string TileFacade = TilesFolder + "/Tile_Facade.asset";
        public const string TileWall = TilesFolder + "/Tile_Wall.asset";
        public const string DoorTexture = SpritesFolder + "/door.png";
        public const string VillagerCraftsman = SpritesFolder + "/villager_craftsman.png";
        public const string PictoEnter = PictosFolder + "/picto_enter.png";
        public const string PictoExit = PictosFolder + "/picto_exit.png";
        public const string PictoTalk = PictosFolder + "/picto_talk.png";

        /// <summary>
        /// Ce que dit l'artisan des plaques. Trois phrases de cinq mots ou moins, en
        /// francais, relues a voix haute pour six ans, conformement a CLAUDE.md.
        ///
        /// Majuscules : c'est la seule casse que PixelFont connaisse. La troisieme phrase
        /// dit que le choix se refait, ce qui est la promesse du jeu.
        /// </summary>
        public static readonly string[] CraftsmanLines =
        {
            "CHOISIS UNE PLAQUE",
            "PUIS CHOISIS UNE BOUCHE",
            "TU PEUX EN CHANGER"
        };

        /// <summary>Image d'une phrase de l'artisan.</summary>
        public static string CraftsmanLineTexture(int index)
        {
            return $"{SpritesFolder}/line_craftsman_{index:00}.png";
        }

        /// <summary>Image 16x16 d'une plaque, celle-la meme qui se pose sur la bouche.</summary>
        public static string CoverTexture(int index)
        {
            return $"{SpritesFolder}/cover_{index:00}.png";
        }

        /// <summary>Le nom de la ville, ecrit en pixels, affiche sous la plaque dans l'atelier.</summary>
        public static string CoverNameTexture(int index)
        {
            return $"{PictosFolder}/cover_name_{index:00}.png";
        }

        // Le bassin d'orage, phase 8. Cinq images pour une cuve : vide, un quart, la moitie,
        // trois quarts, pleine. Le niveau se lit dessus, dans le sous-sol, pas au HUD.
        public const int ReserveLevelCount = 5;

        /// <summary>Image 16x16 de la cuve a un niveau donne, 0 vide a 4 pleine.</summary>
        public static string ReserveTexture(int level)
        {
            return $"{SpritesFolder}/reserve_{Mathf.Clamp(level, 0, ReserveLevelCount - 1):00}.png";
        }

        /// <summary>Nombre de nuances de profondeur : 1 peu profond, 3 profond.</summary>
        public const int DepthCount = 3;

        /// <summary>Nombre d'images de canalisation, une par masque de raccords.</summary>
        public const int PipeMaskCount = 16;

        /// <summary>Nombre de motifs de canalisation : un par type de tuyau.</summary>
        public const int PipePatternCount = 3;

        /// <summary>
        /// Le masque de l'echantillon expose : est plus ouest, un tuyau bien droit. C'est la
        /// meme image que celle posee en jeu, donc le picto de pose ne peut pas mentir sur ce
        /// qu'il va poser.
        /// </summary>
        public const int PipeSampleMask = 10;

        /// <summary>Les trois noms de tuyaux, dans l'ordre du catalogue.</summary>
        public static readonly string[] PipeNames = { "NORMAL", "ISOLÉ", "GRILLÉ" };

        /// <summary>
        /// Ce que dit l'ouvrier des tuyaux. Quatre phrases de cinq mots ou moins.
        ///
        /// Les deux du milieu sont symetriques a dessein : meme verbe, ARRETE, mot que
        /// Victorien connait et qui est deja sur les panneaux qu'il aime, et la menace qui
        /// change. Chacune repond au picto affiche au-dessus de son echantillon.
        ///
        /// « Le grillage ne se bouche pas » a ete ecartee : six mots.
        /// </summary>
        public static readonly string[] WorkerLines =
        {
            "CHOISIS UN TUYAU",
            "L'ISOLÉ ARRÊTE LE FROID",
            "LE GRILLÉ ARRÊTE LES FEUILLES",
            "REVIENS QUAND TU VEUX"
        };

        public const string VillagerWorker = SpritesFolder + "/villager_worker.png";

        /// <summary>
        /// LE GUIDE, phase 17d. Les huit postes de la phase 12e portaient le sprite de l'artisan
        /// des plaques : un guide croise dans la rue ressemblait TRAIT POUR TRAIT au boutiquier
        /// qu'on va voir dans son atelier. Il a desormais son gilet de chantier, le vocabulaire
        /// de celui qui previent — et c'est celui des panneaux, que Victorien connait.
        /// </summary>
        public const string VillagerGuide = SpritesFolder + "/villager_guide.png";

        /// <summary>
        /// Les couleurs des trois personnages de l'usine a panneaux. Franches et distinctes
        /// du vert de l'artisan et du bleu de l'ouvrier : on les reconnait de loin, sans un
        /// mot, ce qui est la regle du projet depuis la phase 0.
        /// </summary>
        /// <summary>
        /// LES SIX COULEURS DE CORPS DU JEU, dans un seul endroit : le personnage joueur, les
        /// deux artisans et les trois de l'usine a panneaux. Elles doivent etre SIX COULEURS
        /// DIFFERENTES — c'est la regle du projet depuis la phase 0, on reconnait quelqu'un de
        /// loin sans un mot — et ValidateDistinct l'exige.
        ///
        /// Elles vivent ici et non dans les fonctions de dessin, parce qu'une contrainte qui
        /// porte sur un ENSEMBLE ne se verifie pas en regardant ses membres un par un.
        /// </summary>
        public static readonly Color32[] CharacterColors =
        {
            Palette.Orange,     // le personnage joueur
            Palette.Teal,       // l'artisan des plaques
            Palette.SignBlue,   // l'ouvrier des tuyaux
            Palette.Brick,      // Le Stock
            Palette.Violet,     // La Fabrique
            Palette.Gold,       // Le Plan
            Palette.Grass       // les huit guides
        };

        /// <summary>Le metier de chacun des trois, dans l'ordre de SignFactoryVillagers.</summary>
        private static readonly VillagerTrade[] SignFactoryTrades =
        {
            VillagerTrade.Stock,
            VillagerTrade.Maker,
            VillagerTrade.Planner
        };

        private static readonly Color32[] SignFactoryColors =
        {
            // BRIQUE ET NON ORANGE, phase 17a. La phase 13 avait choisi un orange (#D07A2E)
            // voisin de celui du joueur (#E05A2B) : deux teintes distinctes a l'oeil, mais la
            // palette de trente-quatre les a fondues sur la meme. Le Stock et le personnage
            // joueur se seraient reconnus l'un pour l'autre, et RIEN ne l'aurait dit — ni la
            // compilation, ni le controle de palette, ni la comparaison des images, les sprites
            // differant par le repere de direction. Vu a l'ecran, puis reglé ici.
            Palette.Brick,    // Le Stock
            Palette.Violet,   // La Fabrique, violet
            Palette.Gold    // Le Plan, ocre
        };

        /// <summary>Image du nom d'un type de tuyau.</summary>
        public static string PipeNameTexture(int index)
        {
            return $"{SpritesFolder}/pipe_name_{index:00}.png";
        }

        /// <summary>Image d'une phrase de l'ouvrier.</summary>
        public static string WorkerLineTexture(int index)
        {
            return $"{SpritesFolder}/line_worker_{index:00}.png";
        }

        private const int TileSize = 16;
        private const int PictoSize = 32;
        private const int PixelsPerUnit = 16;
        private const int PlayerWidth = 16;
        private const int PlayerHeight = 24;

        // Le pivot tombe au centre des 16 px du bas : le transform se pose exactement au
        // centre de la case et la tete du personnage deborde vers le haut.
        private static readonly Vector2 PlayerPivot = new Vector2(0.5f, 1f / 3f);

        // Le brun s'assombrit avec la profondeur, et la galerie vire au gris froid au plus
        // profond : la nuance se lit sans legende.
        private static readonly Color32[] EarthColors =
        {
            Palette.Earth,
            Palette.EarthMid,
            Palette.EarthDeep
        };

        private static readonly Color32[] TunnelColors =
        {
            Palette.Tunnel,
            Palette.TunnelMid,
            Palette.TunnelDeep
        };

        // Fichiers remplaces par une phase ulterieure. La phase 3 avait retire ceux de la
        // phase 2 ; la phase 9b retire les seize tuyaux d'un seul motif, remplaces par
        // quarante-huit, et picto_pipe, remplace par l'echantillon du type en main.
        private static readonly string[] ObsoleteAssets = BuildObsoleteList();

        private static string[] BuildObsoleteList()
        {
            List<string> obsolete = new List<string>
            {
                TilesFolder + "/tile_earth.png",
                TilesFolder + "/tile_tunnel.png",
                TilesFolder + "/Tile_Earth.asset",
                TilesFolder + "/Tile_Tunnel.asset",
                SpritesFolder + "/player.png",
                PictosFolder + "/picto_pipe.png"
            };

            for (int mask = 0; mask < PipeMaskCount; mask++)
            {
                obsolete.Add($"{TilesFolder}/pipe_{mask:00}.png");
                obsolete.Add($"{TilesFolder}/Tile_Pipe_{mask:00}.asset");
            }

            return obsolete.ToArray();
        }

        /// <summary>Asset Tile du sol de galerie d'une profondeur, 1 a 3.</summary>
        public static string TileTunnel(int depth)
        {
            return $"{TilesFolder}/Tile_Tunnel_{Mathf.Clamp(depth, 1, DepthCount)}.asset";
        }

        /// <summary>Asset Tile de la terre pleine d'une profondeur, 1 a 3.</summary>
        public static string TileEarth(int depth)
        {
            return $"{TilesFolder}/Tile_Earth_{Mathf.Clamp(depth, 1, DepthCount)}.asset";
        }

        /// <summary>
        /// Asset Tile de canalisation, pour un motif et un masque de raccords.
        ///
        /// Les seize tuiles nommees Tile_Pipe_00 a 15 de la phase 3 sont remplacees par
        /// quarante-huit, motif compris dans le nom : un schema mixte aurait ete une verrue
        /// dont la phase 12 aurait herite.
        /// </summary>
        public static string TilePipe(int pattern, int mask)
        {
            return $"{TilesFolder}/Tile_Pipe_{Mathf.Clamp(pattern, 0, PipePatternCount - 1)}"
                 + $"_{Mathf.Clamp(mask, 0, PipeMaskCount - 1):00}.asset";
        }

        /// <summary>Texture de canalisation, pour un motif et un masque. Sert d'echantillon.</summary>
        public static string PipeTexture(int pattern, int mask)
        {
            return $"{TilesFolder}/pipe_{Mathf.Clamp(pattern, 0, PipePatternCount - 1)}"
                 + $"_{Mathf.Clamp(mask, 0, PipeMaskCount - 1):00}.png";
        }

        public static string TunnelTexture(int depth)
        {
            return $"{TilesFolder}/tile_tunnel_{depth}.png";
        }

        public static string EarthTexture(int depth)
        {
            return $"{TilesFolder}/tile_earth_{depth}.png";
        }


        [MenuItem("Sous La Ville/Générer l'art placeholder")]
        public static void Generate()
        {
            EnsureFolder(TilesFolder);
            EnsureFolder(SpritesFolder);
            EnsureFolder(PictosFolder);

            foreach (string obsolete in ObsoleteAssets)
            {
                if (AssetDatabase.LoadAssetAtPath<Object>(obsolete) != null)
                {
                    AssetDatabase.DeleteAsset(obsolete);
                }
            }

            AssetDatabase.StartAssetEditing();
            try
            {
                // PHASE 17B : les sols ne sont plus des aplats bordes d'un lisere. Un lisere
                // sur chaque case dessine une GRILLE, et le village entier se lisait comme du
                // papier millimetre ; une pelouse n'a pas de bord tous les seize pixels.
                WriteTexture($"{TilesFolder}/tile_grass.png", BuildGrassTile());
                WriteTexture($"{TilesFolder}/tile_path.png", BuildDirtTile());
                WriteTexture($"{TilesFolder}/tile_park.png", BuildPavingTile(Palette.SteelLight));
                WriteTexture($"{TilesFolder}/tile_plant_floor.png", BuildConcreteTile());
                WriteTileTexture("tile_hedge", Palette.GrassDeep);
                WriteTexture($"{TilesFolder}/tile_plant_wall.png", BuildPlantWallTile());
                WriteTileTexture("tile_house", Palette.Brick);

                for (int depth = 1; depth <= DepthCount; depth++)
                {
                    WriteTexture(EarthTexture(depth), BuildEarthTile(depth));
                    WriteTexture(TunnelTexture(depth), BuildTunnelTile(depth));
                }

                for (int pattern = 0; pattern < PipePatternCount; pattern++)
                {
                    for (int mask = 0; mask < PipeMaskCount; mask++)
                    {
                        WriteTexture(PipeTexture(pattern, mask), BuildPipe(pattern, mask));
                    }
                }

                WriteTexture(ManholeTexture, BuildManhole());
                WriteTexture(LadderTexture, BuildLadder(), PlayerWidth);
                WriteTexture(HouseTexture, BuildHouse(), PlayerWidth);
                WriteTexture(HouseInletTexture, BuildHouseInlet());

                WriteTexture(PlayerDown, BuildPlayer(Vector2Int.down), PlayerWidth);
                WriteTexture(PlayerUp, BuildPlayer(Vector2Int.up), PlayerWidth);
                WriteTexture(PlayerLeft, BuildPlayer(Vector2Int.left), PlayerWidth);
                WriteTexture(PlayerRight, BuildPlayer(Vector2Int.right), PlayerWidth);

                WriteTexture(PictoSurface, BuildSunPicto(), PictoSize);
                WriteTexture(PictoUnderground, BuildLadderPicto(), PictoSize);
                WriteTexture(PictoDown, BuildArrow(pointingDown: true));
                WriteTexture(PictoUp, BuildArrow(pointingDown: false));
                WriteTexture(PictoDig, BuildDigPicto());
                WriteTexture(PictoRemove, BuildRemovePicto());
                WriteTexture(CursorTarget, BuildCursor(TileSize));
                WriteTexture(PictoDropFull, BuildDrop(full: true));
                WriteTexture(PictoDropEmpty, BuildDrop(full: false));
                WriteTexture(PictoRepair, BuildRepairPicto());
                WriteTexture(PictoGrow, BuildGrowPicto());
                WriteTexture(PlantBasinTexture, BuildPlantBasin());

                WriteTexture(PictoSpring, BuildSpringPicto(), PictoSize);
                WriteTexture(PictoSummer, BuildSummerPicto(), PictoSize);
                WriteTexture(PictoAutumn, BuildAutumnPicto(), PictoSize);
                WriteTexture(PictoWinter, BuildWinterPicto(), PictoSize);

                WriteTexture($"{TilesFolder}/tile_workshop.png", BuildPavingTile(Palette.Steel));

                for (int mask = 0; mask < DecorMaskCount; mask++)
                {
                    WriteTexture(FacadeTexture(mask), BuildFacade(mask));
                }

                for (int who = 0; who < SignboardTextures.Length; who++)
                {
                    WriteTexture(SignboardTextures[who], BuildSignboard(who));
                }
                WriteTexture($"{TilesFolder}/tile_water.png", BuildWater());
                WriteTexture(FountainTexture, BuildFountainBase());
                WriteTexture(FountainSprite, BuildFountainSprite(), PlayerWidth);

                // Le decor de la phase 12c.
                for (int mask = 0; mask < DecorMaskCount; mask++)
                {
                    WriteTexture(HedgeTexture(mask), BuildHedge(mask));
                    WriteTexture(RoadTexture(mask), BuildRoad(mask));
                }

                WriteTexture(TreeTexture, BuildTree(), PlayerWidth);
                WriteTexture(TreeTileTexture, BuildTreeBase());

                for (int kind = 0; kind < SignCount; kind++)
                {
                    WriteTexture(SignTexture(kind), BuildSign(kind), PlayerWidth);
                }

                // Phase 14 : le dos d'une carte du memory, et son cadre de choix.
                WriteTexture(SignBackTexture, BuildSignBack(), PlayerWidth);
                WriteTexture(PictoCardCursor, BuildCursor(PictoSize), PictoSize);

                // Phase 16 : le poteau vide d'un poste du plan.
                WriteTexture(SignPostTexture, BuildSignPost(), PlayerWidth);

                // Le nom de chaque panneau de la planche, phase 13. Nomme par le RANG : la
                // place sur la planche peut changer, le rang non.
                for (int slot = 0; slot < SignBoard.Length; slot++)
                {
                    WriteWord(SignNameTexture(SignBoard[slot]), SignBoardNames[slot]);
                }
                WriteTileTexture("tile_facade", Palette.Bark);
                WriteTexture($"{TilesFolder}/tile_wall.png", BuildRoomWallTile());

                for (int piece = 0; piece < FurnitureTextures.Length; piece++)
                {
                    WriteTexture(FurnitureTextures[piece], BuildFurniture(piece), PlayerWidth);
                }

                WriteTexture(DoorTexture, BuildDoor());
                WriteTexture(VillagerGuide,
                    BuildVillager(CharacterColors[6], VillagerTrade.Guide), PlayerWidth);
                WriteTexture(VillagerCraftsman,
                    BuildVillager(CharacterColors[1], VillagerTrade.Covers), PlayerWidth);
                WriteTexture(VillagerWorker,
                    BuildVillager(CharacterColors[2], VillagerTrade.Pipes), PlayerWidth);
                WriteTexture(PictoEnter, BuildDoorPicto(entering: true));
                WriteTexture(PictoExit, BuildDoorPicto(entering: false));
                WriteTexture(PictoTalk, BuildTalkPicto());

                for (int index = 0; index < CraftsmanLines.Length; index++)
                {
                    WriteWord(CraftsmanLineTexture(index), CraftsmanLines[index]);
                }

                for (int index = 0; index < WorkerLines.Length; index++)
                {
                    WriteWord(WorkerLineTexture(index), WorkerLines[index]);
                }

                for (int guide = 0; guide < GuideLines.Length; guide++)
                {
                    for (int line = 0; line < GuideLines[guide].Length; line++)
                    {
                        WriteWord(GuideLineTexture(guide, line), GuideLines[guide][line]);
                    }
                }

                // Les trois personnages de l'usine a panneaux et ce qu'ils disent, phase 13.
                for (int who = 0; who < SignFactoryVillagers.Length; who++)
                {
                    WriteTexture(SignFactoryVillagers[who],
                        BuildVillager(SignFactoryColors[who], SignFactoryTrades[who]), PlayerWidth);

                    for (int line = 0; line < SignFactoryLines[who].Length; line++)
                    {
                        WriteWord(SignFactoryLineTexture(who, line), SignFactoryLines[who][line]);
                    }
                }

                WriteTexture(GuideAttention, BuildAttentionPicto());

                for (int index = 0; index < PipeNames.Length; index++)
                {
                    WriteWord(PipeNameTexture(index), PipeNames[index]);
                }

                for (int index = 0; index < CoverCount; index++)
                {
                    WriteTexture(CoverTexture(index), BuildCover(index));
                    WriteWord(CoverNameTexture(index), CoverNames[index]);
                }

                WriteTexture(VillageMapTexture, BuildVillageMap(), VillageLayout.Width);

                for (int level = 0; level < ReserveLevelCount; level++)
                {
                    WriteTexture(ReserveTexture(level), BuildReserve(level));
                }
            }
            finally
            {
                AssetDatabase.StopAssetEditing();
            }

            // Les importeurs se reglent apres l'ecriture : ils ont besoin de l'asset importe.
            foreach (string name in new[] { "tile_grass", "tile_path", "tile_park",
                         "tile_plant_floor", "tile_hedge", "tile_plant_wall", "tile_house" })
            {
                ConfigureImporter($"{TilesFolder}/{name}.png", null);
            }

            for (int depth = 1; depth <= DepthCount; depth++)
            {
                ConfigureImporter(EarthTexture(depth), null);
                ConfigureImporter(TunnelTexture(depth), null);
            }

            for (int pattern = 0; pattern < PipePatternCount; pattern++)
            {
                for (int mask = 0; mask < PipeMaskCount; mask++)
                {
                    ConfigureImporter(PipeTexture(pattern, mask), null);
                }
            }

            ConfigureImporter($"{TilesFolder}/tile_workshop.png", null);
            ConfigureImporter($"{TilesFolder}/tile_water.png", null);
            ConfigureImporter(FountainTexture, null);
            ConfigureImporter(VillagerGuide, PlayerPivot);

            foreach (string furniture in FurnitureTextures)
            {
                ConfigureImporter(furniture, PlayerPivot);
            }

            ConfigureImporter(FountainSprite, PlayerPivot);
            ConfigureImporter(TreeTexture, PlayerPivot);
            ConfigureImporter(TreeTileTexture, null);

            for (int mask = 0; mask < DecorMaskCount; mask++)
            {
                ConfigureImporter(HedgeTexture(mask), null);
                ConfigureImporter(RoadTexture(mask), null);
            }

            for (int kind = 0; kind < SignCount; kind++)
            {
                ConfigureImporter(SignTexture(kind), PlayerPivot);
            }

            for (int mask = 0; mask < DecorMaskCount; mask++)
            {
                ConfigureImporter(FacadeTexture(mask), null);
            }

            foreach (string signboard in SignboardTextures)
            {
                ConfigureImporter(signboard, null);
            }

            ConfigureImporter(SignBackTexture, PlayerPivot);
            ConfigureImporter(PictoCardCursor, null);
            ConfigureImporter(SignPostTexture, PlayerPivot);

            foreach (int kind in SignBoard)
            {
                ConfigureImporter(SignNameTexture(kind), null);
            }

            for (int who = 0; who < SignFactoryVillagers.Length; who++)
            {
                ConfigureImporter(SignFactoryVillagers[who], PlayerPivot);

                for (int line = 0; line < SignFactoryLines[who].Length; line++)
                {
                    ConfigureImporter(SignFactoryLineTexture(who, line), null);
                }
            }
            ConfigureImporter($"{TilesFolder}/tile_facade.png", null);
            ConfigureImporter($"{TilesFolder}/tile_wall.png", null);
            ConfigureImporter(VillageMapTexture, null);
            ConfigureImporter(DoorTexture, null);
            ConfigureImporter(PictoEnter, null);
            ConfigureImporter(PictoExit, null);
            ConfigureImporter(PictoTalk, null);

            // Meme pivot que le personnage joueur : l'artisan se pose sur sa case et sa tete
            // deborde vers le haut.
            ConfigureImporter(VillagerCraftsman, PlayerPivot);
            ConfigureImporter(VillagerWorker, PlayerPivot);

            // Une phrase fait environ 170 pixels de large. Le plafond de 64 pose en phase 7
            // la reduirait EN SILENCE, exactement le piege que cette phase-la avait evite de
            // justesse sur le nom AMSTERDAM.
            for (int index = 0; index < CraftsmanLines.Length; index++)
            {
                ConfigureImporter(CraftsmanLineTexture(index), null);
            }

            for (int index = 0; index < WorkerLines.Length; index++)
            {
                ConfigureImporter(WorkerLineTexture(index), null);
            }

            for (int guide = 0; guide < GuideLines.Length; guide++)
            {
                for (int line = 0; line < GuideLines[guide].Length; line++)
                {
                    ConfigureImporter(GuideLineTexture(guide, line), null);
                }
            }

            ConfigureImporter(GuideAttention, null);

            for (int index = 0; index < PipeNames.Length; index++)
            {
                ConfigureImporter(PipeNameTexture(index), null);
            }

            for (int index = 0; index < CoverCount; index++)
            {
                ConfigureImporter(CoverTexture(index), null);
                ConfigureImporter(CoverNameTexture(index), null);
            }

            ConfigureImporter(ManholeTexture, null);
            ConfigureImporter(LadderTexture, PlayerPivot);
            ConfigureImporter(HouseInletTexture, null);

            for (int level = 0; level < ReserveLevelCount; level++)
            {
                ConfigureImporter(ReserveTexture(level), null);
            }

            // Meme pivot que le personnage : la maison se pose sur sa case et son toit deborde.
            ConfigureImporter(HouseTexture, PlayerPivot);

            foreach (string path in new[] { PlayerDown, PlayerUp, PlayerLeft, PlayerRight })
            {
                ConfigureImporter(path, PlayerPivot);
            }

            foreach (string path in new[] { PictoSurface, PictoUnderground, PictoDown, PictoUp,
                         PictoDig, PictoRemove, CursorTarget, PictoDropFull,
                         PictoDropEmpty, PictoRepair, PictoGrow, PlantBasinTexture,
                         PictoSpring, PictoSummer, PictoAutumn, PictoWinter })
            {
                ConfigureImporter(path, null);
            }

            for (int mask = 0; mask < DecorMaskCount; mask++)
            {
                CreateTileAsset(TileFacadeMasked(mask), FacadeTexture(mask));
            }

            CreateTileAsset(TileGrass, $"{TilesFolder}/tile_grass.png");
            CreateTileAsset(TilePath, $"{TilesFolder}/tile_path.png");
            CreateTileAsset(TilePark, $"{TilesFolder}/tile_park.png");
            CreateTileAsset(TilePlantFloor, $"{TilesFolder}/tile_plant_floor.png");
            CreateTileAsset(TileHedge, $"{TilesFolder}/tile_hedge.png");
            CreateTileAsset(TilePlantWall, PlantWallTexture);
            CreateTileAsset(TileHouse, $"{TilesFolder}/tile_house.png");
            CreateTileAsset(TileWorkshop, $"{TilesFolder}/tile_workshop.png");
            CreateTileAsset(TileWater, $"{TilesFolder}/tile_water.png");
            CreateTileAsset(TileFountain, FountainTexture);
            CreateTileAsset(TileTree, TreeTileTexture);

            for (int mask = 0; mask < DecorMaskCount; mask++)
            {
                CreateTileAsset(TileHedgeMasked(mask), HedgeTexture(mask));
                CreateTileAsset(TileRoadMasked(mask), RoadTexture(mask));
            }
            CreateTileAsset(TileFacade, $"{TilesFolder}/tile_facade.png");
            CreateTileAsset(TileWall, $"{TilesFolder}/tile_wall.png");

            for (int depth = 1; depth <= DepthCount; depth++)
            {
                CreateTileAsset(TileEarth(depth), EarthTexture(depth));
                CreateTileAsset(TileTunnel(depth), TunnelTexture(depth));
            }

            for (int pattern = 0; pattern < PipePatternCount; pattern++)
            {
                for (int mask = 0; mask < PipeMaskCount; mask++)
                {
                    CreateTileAsset(TilePipe(pattern, mask), PipeTexture(pattern, mask));
                }
            }

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            // Le compte est MESURE, plus ecrit a la main. Il annoncait « 123 textures, 66
            // tuiles » quelle que soit la realite : la phase 12c en a ajoute quarante-deux
            // sans que le nombre bouge d'une unite. Un journal qui ne peut pas dire non ne
            // vaut pas mieux qu'un validateur qui dit toujours oui.
            int textures = CountAssets(TilesFolder, ".png") + CountAssets(SpritesFolder, ".png")
                         + CountAssets(PictosFolder, ".png");
            int tiles = CountAssets(TilesFolder, ".asset");

            Debug.Log($"[Sous la Ville] Art placeholder généré : {textures} textures, " +
                      $"{tiles} tuiles.");

            // LE CONTROLE DE PALETTE SUIT LA GENERATION, phase 17a : une image hors palette
            // s'apprend dans la foulee, jamais trois phases plus tard.
            ValidatePalette();
            ValidateDistinct();
        }

        /// <summary>
        /// CHAQUE PIXEL DE CHAQUE IMAGE EST UNE COULEUR DE LA PALETTE, OU TRANSPARENT.
        /// Phase 17a. Relu sur les fichiers reellement ecrits sur le disque, jamais sur ce que
        /// le code croit avoir dessine — c'est la seule lecture qui ne puisse pas mentir.
        ///
        /// Sans ce filet, une palette n'est qu'une intention : rien n'empeche un `new Color32`
        /// de se glisser dans un dessin, et personne ne le verrait — quatre-vingt-huit couleurs
        /// s'etaient accumulees ainsi en seize phases, dont trois gris qu'aucun oeil ne separait.
        ///
        /// L'ALPHA NE COMPTE PAS : l'eau et le voile sont des couleurs de la palette qu'on voit
        /// au travers. Un pixel totalement transparent passe quelle que soit sa couleur, les
        /// canaux d'un pixel invisible n'etant regardes par personne.
        /// </summary>
        public static bool ValidatePalette()
        {
            string[] folders = { TilesFolder, SpritesFolder, PictosFolder };
            int checkedFiles = 0;
            int faults = 0;

            foreach (string folder in folders)
            {
                string absolute = System.IO.Path.Combine(
                    System.IO.Path.GetDirectoryName(Application.dataPath), folder);

                if (!System.IO.Directory.Exists(absolute))
                {
                    continue;
                }

                foreach (string file in System.IO.Directory.GetFiles(absolute, "*.png",
                             System.IO.SearchOption.TopDirectoryOnly))
                {
                    checkedFiles++;

                    // Les textures importees ne sont PAS lisibles : on relit le PNG du disque.
                    Texture2D texture = new Texture2D(2, 2, TextureFormat.RGBA32, false);
                    if (!texture.LoadImage(System.IO.File.ReadAllBytes(file)))
                    {
                        Debug.LogError($"[Sous la Ville] Image illisible : {file}");
                        Object.DestroyImmediate(texture);
                        faults++;
                        continue;
                    }

                    Color32[] pixels = texture.GetPixels32();
                    int width = texture.width;
                    Object.DestroyImmediate(texture);

                    for (int i = 0; i < pixels.Length; i++)
                    {
                        Color32 pixel = pixels[i];
                        if (pixel.a == 0 || Palette.Contains(pixel))
                        {
                            continue;
                        }

                        faults++;

                        // Un seul pixel suffit a dire la faute : on nomme le fichier, la case et
                        // la couleur, et on passe a l'image suivante plutot que de noyer la
                        // console sous mille lignes identiques.
                        Debug.LogError($"[Sous la Ville] {System.IO.Path.GetFileName(file)} " +
                                       $"porte en ({i % width}, {i / width}) la couleur " +
                                       $"#{pixel.r:X2}{pixel.g:X2}{pixel.b:X2}, qui n'est pas de " +
                                       "la palette. Ajoute-la à Palette, ou dessine avec une " +
                                       "couleur qui y est.");
                        break;
                    }
                }
            }

            if (faults > 0)
            {
                Debug.LogError($"[Sous la Ville] {faults} image(s) hors palette sur " +
                               $"{checkedFiles} : l'art ne tient pas ses {Palette.All.Length} couleurs.");
                return false;
            }

            Debug.Log($"[Sous la Ville] Palette tenue : {checkedFiles} image(s), " +
                      $"{Palette.All.Length} couleurs et pas une de plus.");
            return true;
        }

        /// <summary>
        /// CE QUE LE JEU DISTINGUE NE DOIT PAS ETRE DEUX FOIS LA MEME IMAGE, phase 17a.
        ///
        /// Une palette limitee fond des couleurs ensemble, et l'habillage redessine tout : deux
        /// plaques d'egout, deux motifs de tuyau, deux personnages ou deux panneaux du Code
        /// peuvent devenir identiques au pixel pres SANS QU'AUCUN AUTRE FILET NE LE DISE. Le
        /// contrôle de palette les accepterait — ce sont de bonnes couleurs — et la compilation
        /// aussi. Seule la comparaison des images entre elles l'attrape.
        ///
        /// Ce n'est pas une precaution en l'air : la phase 13 a sorti AB1 et AB25 indiscernables,
        /// puis A1b et A1c, et il a fallu une planche agrandie huit fois pour s'en apercevoir.
        /// Ici, c'est refuse en nommant les deux fichiers.
        /// </summary>
        public static bool ValidateDistinct()
        {
            bool ok = true;

            string[] covers = new string[CoverCount];
            for (int i = 0; i < CoverCount; i++)
            {
                covers[i] = CoverTexture(i);
            }

            // Le motif est-ouest : celui que la vitrine expose et que le joueur reconnait.
            string[] pipes = new string[PipePatternCount];
            for (int pattern = 0; pattern < PipePatternCount; pattern++)
            {
                pipes[pattern] = PipeTexture(pattern, 10);
            }

            string[] people = new string[3 + SignFactoryVillagers.Length];
            people[0] = VillagerCraftsman;
            people[1] = VillagerWorker;
            people[2 + SignFactoryVillagers.Length] = VillagerGuide;
            for (int who = 0; who < SignFactoryVillagers.Length; who++)
            {
                people[2 + who] = SignFactoryVillagers[who];
            }

            string[] signs = new string[SignCount];
            for (int kind = 0; kind < SignCount; kind++)
            {
                signs[kind] = SignTexture(kind);
            }

            string[] grounds = new string[DepthCount * 2];
            for (int depth = 1; depth <= DepthCount; depth++)
            {
                grounds[depth - 1] = EarthTexture(depth);
                grounds[DepthCount + depth - 1] = TunnelTexture(depth);
            }

            string[] seasons = { PictoSpring, PictoSummer, PictoAutumn, PictoWinter };

            ok &= AllDistinct("les plaques d'égout", covers);
            ok &= AllDistinct("les motifs de canalisation", pipes);
            ok &= AllDistinct("les personnages", people);
            ok &= AllDistinct("les panneaux du Code", signs);
            ok &= AllDistinct("les profondeurs de terre et de galerie", grounds);
            ok &= AllDistinct("les pictogrammes de saison", seasons);

            // ET LES SIX COULEURS DE CORPS, qui ne sont pas des images mais un ensemble : deux
            // personnages de la meme couleur portent des sprites differents — le joueur a son
            // repere de direction — donc AllDistinct les laisserait passer, alors qu'a l'ecran
            // on ne les distinguerait pas. Tombé en phase 17a.
            for (int i = 0; i < CharacterColors.Length; i++)
            {
                for (int j = i + 1; j < CharacterColors.Length; j++)
                {
                    if (!Palette.Same(CharacterColors[i], CharacterColors[j]))
                    {
                        continue;
                    }

                    Debug.LogError($"[Sous la Ville] Les personnages {i} et {j} portent tous " +
                                   $"deux la couleur « {Palette.NameOf(CharacterColors[i])} » : " +
                                   "on ne les distinguerait pas de loin.");
                    ok = false;
                }
            }

            if (ok)
            {
                Debug.Log($"[Sous la Ville] Familles distinctes : {covers.Length} plaques, " +
                          $"{pipes.Length} motifs, {people.Length} personnages, {signs.Length} " +
                          $"panneaux, {grounds.Length} sols, {seasons.Length} saisons, " +
                          $"{CharacterColors.Length} couleurs de corps — aucune paire identique.");
            }

            return ok;
        }

        private static bool AllDistinct(string family, string[] paths)
        {
            bool ok = true;
            Dictionary<string, string> seen = new Dictionary<string, string>();

            foreach (string path in paths)
            {
                string signature = Signature(path);
                if (signature == null)
                {
                    Debug.LogError($"[Sous la Ville] Image illisible dans {family} : {path}");
                    ok = false;
                    continue;
                }

                if (seen.TryGetValue(signature, out string twin))
                {
                    Debug.LogError($"[Sous la Ville] Dans {family}, « " +
                                   $"{System.IO.Path.GetFileName(path)} » est identique au pixel " +
                                   $"près à « {System.IO.Path.GetFileName(twin)} » : rien ne les " +
                                   "distinguerait à l'écran.");
                    ok = false;
                    continue;
                }

                seen[signature] = path;
            }

            return ok;
        }

        /// <summary>Les pixels d'une image, relus du disque, en une chaine comparable.</summary>
        private static string Signature(string assetPath)
        {
            string absolute = System.IO.Path.Combine(
                System.IO.Path.GetDirectoryName(Application.dataPath), assetPath);

            if (!System.IO.File.Exists(absolute))
            {
                return null;
            }

            Texture2D texture = new Texture2D(2, 2, TextureFormat.RGBA32, false);
            if (!texture.LoadImage(System.IO.File.ReadAllBytes(absolute)))
            {
                Object.DestroyImmediate(texture);
                return null;
            }

            Color32[] pixels = texture.GetPixels32();
            int width = texture.width;
            Object.DestroyImmediate(texture);

            System.Text.StringBuilder builder = new System.Text.StringBuilder(pixels.Length * 4 + 8);
            builder.Append(width).Append(':');

            foreach (Color32 pixel in pixels)
            {
                // Un pixel transparent ne se voit pas : ses canaux ne comptent pas.
                if (pixel.a == 0)
                {
                    builder.Append('.');
                    continue;
                }

                builder.Append((char)('a' + pixel.r % 26))
                       .Append((char)('a' + pixel.g % 26))
                       .Append((char)('a' + pixel.b % 26))
                       .Append((char)('a' + pixel.a % 26));
            }

            return builder.ToString();
        }

        /// <summary>
        /// LA PLANCHE DE L'ART, phase 17a. Toutes les images du jeu agrandies sur une feuille
        /// par dossier, plus la palette elle-meme.
        ///
        /// Rien ne se declare fini sans l'avoir regardee. C'est la lecon des phases 13, 14 et
        /// 16 : trois fois le code compilait, les validateurs passaient, et c'est une image
        /// agrandie qui a montre six dessins rates, puis une rangee cachee derriere le HUD,
        /// puis un panneau coupe par le bord de l'ecran. Aucun validateur ne pouvait le dire.
        /// </summary>
        [MenuItem("Sous La Ville/Planche de l'art")]
        public static void BuildArtSheets()
        {
            System.IO.Directory.CreateDirectory("Captures");

            WriteSheet("Captures/planche_palette.png", PaletteSheet());

            string[][] folders =
            {
                new[] { TilesFolder, "Captures/planche_tuiles.png" },
                new[] { SpritesFolder, "Captures/planche_sprites.png" },
                new[] { PictosFolder, "Captures/planche_pictos.png" }
            };

            foreach (string[] entry in folders)
            {
                Texture2D sheet = FolderSheet(entry[0]);
                if (sheet != null)
                {
                    WriteSheet(entry[1], sheet);
                }
            }

            Debug.Log("[Sous la Ville] Planches écrites dans Captures/ : palette, tuiles, " +
                      "sprites, pictos. À REGARDER, pas seulement à produire.");
        }

        /// <summary>La palette, une bande par couleur, dans l'ordre de la declaration.</summary>
        private static Texture2D PaletteSheet()
        {
            const int swatch = 48;
            const int columns = 6;

            int rows = (Palette.All.Length + columns - 1) / columns;
            Texture2D sheet = NewSheet(columns * swatch, rows * swatch);

            for (int i = 0; i < Palette.All.Length; i++)
            {
                int column = i % columns;
                int row = i / columns;

                for (int y = 1; y < swatch - 1; y++)
                {
                    for (int x = 1; x < swatch - 1; x++)
                    {
                        sheet.SetPixel(column * swatch + x,
                            sheet.height - 1 - (row * swatch + y), Palette.All[i]);
                    }
                }
            }

            sheet.Apply();
            return sheet;
        }

        /// <summary>
        /// Toutes les images d'un dossier, agrandies trois fois, posees en lignes qui se
        /// replient. Les tailles sont libres : les noms de villes font 55 px de large, les
        /// phrases jusqu'a 193, les tuiles 16.
        /// </summary>
        private static Texture2D FolderSheet(string folder)
        {
            const int scale = 3;
            const int gap = 3;
            const int maxWidth = 1500;

            string absolute = System.IO.Path.Combine(
                System.IO.Path.GetDirectoryName(Application.dataPath), folder);

            if (!System.IO.Directory.Exists(absolute))
            {
                return null;
            }

            string[] files = System.IO.Directory.GetFiles(absolute, "*.png",
                System.IO.SearchOption.TopDirectoryOnly);
            System.Array.Sort(files);

            List<Texture2D> images = new List<Texture2D>();
            foreach (string file in files)
            {
                Texture2D texture = new Texture2D(2, 2, TextureFormat.RGBA32, false);
                if (texture.LoadImage(System.IO.File.ReadAllBytes(file)))
                {
                    images.Add(texture);
                }
                else
                {
                    Object.DestroyImmediate(texture);
                }
            }

            if (images.Count == 0)
            {
                return null;
            }

            // Premiere passe : ou tombe chaque image, et quelle taille fait la feuille.
            List<Vector2Int> places = new List<Vector2Int>(images.Count);
            int penX = gap;
            int penY = gap;
            int lineHeight = 0;
            int sheetWidth = 0;

            foreach (Texture2D image in images)
            {
                int width = image.width * scale;
                int height = image.height * scale;

                if (penX + width + gap > maxWidth && penX > gap)
                {
                    penX = gap;
                    penY += lineHeight + gap;
                    lineHeight = 0;
                }

                places.Add(new Vector2Int(penX, penY));
                penX += width + gap;
                lineHeight = Mathf.Max(lineHeight, height);
                sheetWidth = Mathf.Max(sheetWidth, penX);
            }

            Texture2D sheet = NewSheet(sheetWidth + gap, penY + lineHeight + gap);

            for (int i = 0; i < images.Count; i++)
            {
                Texture2D image = images[i];
                Color32[] pixels = image.GetPixels32();

                for (int y = 0; y < image.height; y++)
                {
                    for (int x = 0; x < image.width; x++)
                    {
                        Color32 pixel = pixels[y * image.width + x];
                        if (pixel.a == 0)
                        {
                            continue;
                        }

                        for (int sy = 0; sy < scale; sy++)
                        {
                            for (int sx = 0; sx < scale; sx++)
                            {
                                // L'origine d'une texture est en bas ; la feuille se remplit du
                                // haut vers le bas, d'ou le retournement.
                                int px = places[i].x + x * scale + sx;
                                int py = sheet.height - 1 - places[i].y
                                         - (image.height - 1 - y) * scale - sy;
                                sheet.SetPixel(px, py, pixel);
                            }
                        }
                    }
                }

                Object.DestroyImmediate(image);
            }

            sheet.Apply();
            return sheet;
        }

        private static Texture2D NewSheet(int width, int height)
        {
            Texture2D sheet = new Texture2D(width, height, TextureFormat.RGBA32, false);
            Color32 ground = Palette.Charcoal;

            Color32[] pixels = new Color32[width * height];
            for (int i = 0; i < pixels.Length; i++)
            {
                pixels[i] = ground;
            }

            sheet.SetPixels32(pixels);
            return sheet;
        }

        private static void WriteSheet(string path, Texture2D sheet)
        {
            System.IO.File.WriteAllBytes(path, sheet.EncodeToPNG());
            Object.DestroyImmediate(sheet);
        }

        /// <summary>Combien de fichiers d'une extension donnee vivent dans un dossier d'assets.</summary>
        private static int CountAssets(string folder, string extension)
        {
            string absolute = System.IO.Path.Combine(
                System.IO.Path.GetDirectoryName(Application.dataPath), folder);

            if (!System.IO.Directory.Exists(absolute))
            {
                return 0;
            }

            return System.IO.Directory.GetFiles(absolute, "*" + extension,
                System.IO.SearchOption.TopDirectoryOnly).Length;
        }

        /// <summary>Vrai si toutes les tuiles et tous les sprites attendus sont sur le disque.</summary>
        public static bool AreAssetsPresent()
        {
            string[] tiles =
            {
                TileGrass, TilePath, TilePark, TilePlantFloor, TileHedge, TilePlantWall, TileHouse,
                TileWorkshop, TileFacade, TileWall, TileWater, TileFountain, TileTree
            };

            foreach (string path in tiles)
            {
                if (AssetDatabase.LoadAssetAtPath<Tile>(path) == null)
                {
                    return false;
                }
            }

            for (int depth = 1; depth <= DepthCount; depth++)
            {
                if (AssetDatabase.LoadAssetAtPath<Tile>(TileEarth(depth)) == null
                    || AssetDatabase.LoadAssetAtPath<Tile>(TileTunnel(depth)) == null)
                {
                    return false;
                }
            }

            for (int pattern = 0; pattern < PipePatternCount; pattern++)
            {
                for (int mask = 0; mask < PipeMaskCount; mask++)
                {
                    if (AssetDatabase.LoadAssetAtPath<Tile>(TilePipe(pattern, mask)) == null)
                    {
                        return false;
                    }
                }
            }

            // Le decor de la phase 12c. AreAssetsPresent barre TOUTE construction de scene :
            // une famille de tuiles oubliee ici se verrait donc a la construction, et non a
            // l'ecran par des carres manquants.
            for (int mask = 0; mask < DecorMaskCount; mask++)
            {
                if (AssetDatabase.LoadAssetAtPath<Tile>(TileHedgeMasked(mask)) == null
                    || AssetDatabase.LoadAssetAtPath<Tile>(TileRoadMasked(mask)) == null
                    || AssetDatabase.LoadAssetAtPath<Tile>(TileFacadeMasked(mask)) == null)
                {
                    return false;
                }
            }

            foreach (string signboard in SignboardTextures)
            {
                if (AssetDatabase.LoadAssetAtPath<Sprite>(signboard) == null)
                {
                    return false;
                }
            }

            foreach (string furniture in FurnitureTextures)
            {
                if (AssetDatabase.LoadAssetAtPath<Sprite>(furniture) == null)
                {
                    return false;
                }
            }

            for (int kind = 0; kind < SignCount; kind++)
            {
                if (AssetDatabase.LoadAssetAtPath<Sprite>(SignTexture(kind)) == null)
                {
                    return false;
                }
            }

            // La planche de l'usine a panneaux et ses trois personnages, phase 13. Comme
            // tout le reste ici, une image manquante barre TOUTE construction de scene :
            // elle se voit a la construction, et non a l'ecran par un carre absent.
            foreach (int kind in SignBoard)
            {
                if (AssetDatabase.LoadAssetAtPath<Sprite>(SignNameTexture(kind)) == null)
                {
                    return false;
                }
            }

            for (int who = 0; who < SignFactoryVillagers.Length; who++)
            {
                if (AssetDatabase.LoadAssetAtPath<Sprite>(SignFactoryVillagers[who]) == null)
                {
                    return false;
                }

                for (int line = 0; line < SignFactoryLines[who].Length; line++)
                {
                    if (AssetDatabase.LoadAssetAtPath<Sprite>(
                            SignFactoryLineTexture(who, line)) == null)
                    {
                        return false;
                    }
                }
            }

            string[] sprites =
            {
                ManholeTexture, LadderTexture, HouseTexture, HouseInletTexture, PlayerDown,
                PlayerUp, PlayerLeft, PlayerRight, PictoSurface, PictoUnderground, PictoDown,
                PictoUp, PictoDig, PictoRemove, CursorTarget, PictoDropFull,
                PictoDropEmpty, PictoRepair, PictoSpring, PictoSummer, PictoAutumn, PictoWinter,
                DoorTexture, VillagerCraftsman, VillagerWorker, VillagerGuide, PictoEnter, PictoExit, PictoTalk,
                FountainSprite, TreeTexture, PictoGrow, PlantBasinTexture, GuideAttention,
                SignBackTexture, PictoCardCursor, SignPostTexture
            };

            foreach (string path in sprites)
            {
                if (AssetDatabase.LoadAssetAtPath<Sprite>(path) == null)
                {
                    return false;
                }
            }

            for (int index = 0; index < CoverCount; index++)
            {
                if (AssetDatabase.LoadAssetAtPath<Sprite>(CoverTexture(index)) == null
                    || AssetDatabase.LoadAssetAtPath<Sprite>(CoverNameTexture(index)) == null)
                {
                    return false;
                }
            }

            for (int level = 0; level < ReserveLevelCount; level++)
            {
                if (AssetDatabase.LoadAssetAtPath<Sprite>(ReserveTexture(level)) == null)
                {
                    return false;
                }
            }

            for (int index = 0; index < CraftsmanLines.Length; index++)
            {
                if (AssetDatabase.LoadAssetAtPath<Sprite>(CraftsmanLineTexture(index)) == null)
                {
                    return false;
                }
            }

            for (int index = 0; index < WorkerLines.Length; index++)
            {
                if (AssetDatabase.LoadAssetAtPath<Sprite>(WorkerLineTexture(index)) == null)
                {
                    return false;
                }
            }

            for (int index = 0; index < PipeNames.Length; index++)
            {
                if (AssetDatabase.LoadAssetAtPath<Sprite>(PipeNameTexture(index)) == null)
                {
                    return false;
                }
            }

            return AssetDatabase.LoadAssetAtPath<Sprite>(VillageMapTexture) != null;
        }

        // ---------------------------------------------------------------- dessin

        /// <summary>
        /// UNE CASE DE FACADE. Le masque dit ou l'on se trouve dans le batiment : pas de voisin
        /// au NORD veut dire rangee du haut, donc le TOIT ; sinon le mur, avec sa fenetre.
        ///
        /// Le bord se cerne d'un trait sombre du cote ou il n'y a pas de voisin, exactement
        /// comme une haie ou une route : le batiment a donc une silhouette nette au lieu d'etre
        /// un aplat ocre pose sur l'herbe.
        /// </summary>
        private static Color32[] BuildFacade(int mask)
        {
            bool north = (mask & 1) != 0;
            bool east = (mask & 2) != 0;
            bool south = (mask & 4) != 0;
            bool west = (mask & 8) != 0;

            Color32[] pixels = new Color32[TileSize * TileSize];

            if (!north)
            {
                // LE TOIT. Des rangees de tuiles decalees, et le faite en arete claire tout en
                // haut : c'est ce qui manquait le plus, une facade sans toit ne se lit pas
                // comme un batiment.
                for (int y = 0; y < TileSize; y++)
                {
                    for (int x = 0; x < TileSize; x++)
                    {
                        pixels[y * TileSize + x] = Palette.Brick;
                    }
                }

                for (int row = 2; row < TileSize - 2; row += 4)
                {
                    Fill(pixels, TileSize, 0, TileSize - 1, row, row, Palette.Shade(Palette.Brick));
                }

                // Le decalage d'une rangee sur deux : les tuiles ne s'alignent pas en colonnes.
                for (int y = 0; y < TileSize; y++)
                {
                    int band = y / 4;
                    for (int x = (band % 2) * 4; x < TileSize; x += 8)
                    {
                        pixels[y * TileSize + x] = Palette.Shade(Palette.Brick);
                    }
                }

                Fill(pixels, TileSize, 0, TileSize - 1, TileSize - 2, TileSize - 1, Palette.Orange);
            }
            else
            {
                // LE MUR, et une fenetre au milieu.
                for (int y = 0; y < TileSize; y++)
                {
                    for (int x = 0; x < TileSize; x++)
                    {
                        pixels[y * TileSize + x] = Palette.Bark;
                    }
                }

                for (int y = 0; y < TileSize; y++)
                {
                    for (int x = 0; x < TileSize; x++)
                    {
                        if (Speckle(x, y, 41) % 20 == 0)
                        {
                            pixels[y * TileSize + x] = Palette.Wood;
                        }
                    }
                }

                Fill(pixels, TileSize, 5, 10, 5, 10, Palette.WoodDark);
                Fill(pixels, TileSize, 6, 9, 6, 9, Palette.Ice);
                Fill(pixels, TileSize, 6, 7, 8, 9, Palette.Paper);
            }

            // Le contour, du cote ou le batiment s'arrete.
            Color32 edge = Palette.Ink;
            if (!north) Fill(pixels, TileSize, 0, TileSize - 1, TileSize - 1, TileSize - 1, edge);
            if (!south) Fill(pixels, TileSize, 0, TileSize - 1, 0, 0, edge);
            if (!east) Fill(pixels, TileSize, TileSize - 1, TileSize - 1, 0, TileSize - 1, edge);
            if (!west) Fill(pixels, TileSize, 0, 0, 0, TileSize - 1, edge);

            return pixels;
        }

        /// <summary>
        /// UNE ENSEIGNE : un panonceau de bois porte au mur, et dessus le signe du metier.
        /// Zero pour les plaques, un pour les tuyaux, deux pour les panneaux.
        /// </summary>
        private static Color32[] BuildSignboard(int trade)
        {
            Color32[] pixels = NewTransparent(TileSize * TileSize);

            // Le panonceau, avec son ombre portee sur le mur.
            Fill(pixels, TileSize, 2, 13, 2, 13, Palette.WoodDark);
            Fill(pixels, TileSize, 3, 12, 3, 12, Palette.Wood);

            switch (trade)
            {
                case 0:
                    // Une plaque d'egout : un disque et ses deux barres.
                    DrawDisc(pixels, TileSize, 8, 8, 4.2f, Palette.Steel);
                    Fill(pixels, TileSize, 5, 10, 8, 8, Palette.Charcoal);
                    Fill(pixels, TileSize, 5, 10, 6, 6, Palette.Charcoal);
                    break;

                case 1:
                    // Un tuyau vu en bout : un anneau clair et son trou sombre.
                    DrawDisc(pixels, TileSize, 8, 8, 4.2f, Palette.SteelLight);
                    DrawDisc(pixels, TileSize, 8, 8, 2.2f, Palette.Charcoal);
                    break;

                default:
                    // Un panneau : le triangle borde de rouge, la forme que Victorien lit
                    // en premier.
                    for (int row = 0; row < 7; row++)
                    {
                        int half = 6 - row;
                        Fill(pixels, TileSize, 8 - half, 7 + half, 4 + row, 4 + row, Palette.SignRed);
                    }

                    for (int row = 0; row < 4; row++)
                    {
                        int half = 3 - row;
                        if (half > 0)
                        {
                            Fill(pixels, TileSize, 8 - half, 7 + half, 6 + row, 6 + row, Palette.Paper);
                        }
                    }

                    break;
            }

            return pixels;
        }

        /// <summary>
        /// UN BRUIT STABLE, phase 17b. Le meme (x, y) rend toujours la meme valeur : la texture
        /// d'un sol est donc reproductible d'une generation a l'autre, et un diff d'image ne
        /// bouge pas sans raison. Ce n'est pas du hasard, c'est un motif qu'on ne lit pas comme
        /// un motif.
        /// </summary>
        private static int Speckle(int x, int y, int salt)
        {
            int hash = x * 374761393 + y * 668265263 + salt * 2147483647;
            hash = (hash ^ (hash >> 13)) * 1274126177;
            return (hash ^ (hash >> 16)) & 0x7FFFFFFF;
        }

        /// <summary>
        /// LA PELOUSE. Le fond, des touffes plus sombres et quelques brins clairs, SANS AUCUN
        /// BORD : une tuile d'herbe se raccorde a la suivante sans couture, et le village cesse
        /// de se lire comme un quadrillage.
        ///
        /// Le motif est le meme sur toutes les cases — une seule tuile est peinte partout — mais
        /// il est assez irregulier pour qu'on ne compte pas les cases a l'oeil.
        /// </summary>
        private static Color32[] BuildGrassTile()
        {
            Color32[] pixels = new Color32[TileSize * TileSize];

            for (int y = 0; y < TileSize; y++)
            {
                for (int x = 0; x < TileSize; x++)
                {
                    pixels[y * TileSize + x] = Palette.Grass;
                }
            }

            for (int y = 0; y < TileSize; y++)
            {
                for (int x = 0; x < TileSize; x++)
                {
                    int noise = Speckle(x, y, 7) % 32;

                    // Une touffe : deux pixels sombres l'un au-dessus de l'autre.
                    if (noise == 0 && y + 1 < TileSize)
                    {
                        pixels[y * TileSize + x] = Palette.GrassDark;
                        pixels[(y + 1) * TileSize + x] = Palette.GrassDark;
                    }
                    else if (noise == 5)
                    {
                        pixels[y * TileSize + x] = Palette.GrassDark;
                    }
                }
            }

            return pixels;
        }

        /// <summary>
        /// LA TERRE BATTUE du chemin de base, sans bord elle aussi : des grains plus sombres et
        /// plus clairs dans le sable.
        /// </summary>
        private static Color32[] BuildDirtTile()
        {
            Color32[] pixels = new Color32[TileSize * TileSize];

            for (int i = 0; i < pixels.Length; i++)
            {
                pixels[i] = Palette.Stone;
            }

            for (int y = 0; y < TileSize; y++)
            {
                for (int x = 0; x < TileSize; x++)
                {
                    int noise = Speckle(x, y, 19) % 24;

                    if (noise == 0)
                    {
                        pixels[y * TileSize + x] = Palette.StoneDark;
                    }
                    else if (noise == 3)
                    {
                        pixels[y * TileSize + x] = Palette.StoneLight;
                    }
                }
            }

            return pixels;
        }

        /// <summary>
        /// LE PAVAGE : quatre dalles de huit pixels, un joint sombre entre elles, et un eclat
        /// clair en haut a gauche de chaque dalle. Le joint tombe sur le bord de la tuile, donc
        /// les dalles se poursuivent d'une case a l'autre sans decalage.
        /// </summary>
        private static Color32[] BuildPavingTile(Color32 stone)
        {
            Color32 joint = Palette.Shade(stone);

            // L'eclat est la nuance CLAIRE de la dalle. Ecrit d'abord « = stone », il ne faisait
            // rien du tout : la dalle etait uniforme et personne ne l'aurait su sans la regarder.
            Color32 shine = Palette.Tint(stone);

            Color32[] pixels = new Color32[TileSize * TileSize];

            for (int y = 0; y < TileSize; y++)
            {
                for (int x = 0; x < TileSize; x++)
                {
                    bool onJoint = x % 8 == 0 || y % 8 == 0;
                    pixels[y * TileSize + x] = onJoint ? joint : stone;
                }
            }

            // L'eclat : le coin haut-gauche de chaque dalle prend la nuance claire du dessus.
            for (int slab = 0; slab < 4; slab++)
            {
                int ox = (slab % 2) * 8 + 1;
                int oy = (slab / 2) * 8 + 6;

                Fill(pixels, TileSize, ox, ox + 4, oy, oy, shine);
            }

            return pixels;
        }

        /// <summary>
        /// LA TERRE PLEINE, phase 17c. Un aplat borde d'un lisere dessinait sous terre la meme
        /// grille qu'en surface. Ici, des cailloux et des veines, sans aucun bord.
        ///
        /// LA PROFONDEUR SE COMPTE, elle ne se devine plus a la nuance. Chaque case porte autant
        /// de CAILLOUX CLAIRS que sa profondeur : un a la profondeur 1, deux a la 2, trois a la 3.
        /// L'ecart entre les profondeurs 1 et 2 etait une question ouverte depuis la phase 3 —
        /// « distinct sur les captures, mais l'ecart est faible » —, et une nuance de brun ne se
        /// compare qu'en voyant les deux cote a cote. Un nombre se compte sur une seule case, et
        /// la regle de profondeur croissante EST le puzzle selon CLAUDE.md.
        /// </summary>
        private static Color32[] BuildEarthTile(int depth)
        {
            Color32 body = EarthColors[Mathf.Clamp(depth - 1, 0, EarthColors.Length - 1)];
            Color32 grain = Palette.Shade(body);

            Color32[] pixels = new Color32[TileSize * TileSize];

            for (int i = 0; i < pixels.Length; i++)
            {
                pixels[i] = body;
            }

            for (int y = 0; y < TileSize; y++)
            {
                for (int x = 0; x < TileSize; x++)
                {
                    if (Speckle(x, y, 53 + depth) % 14 == 0)
                    {
                        pixels[y * TileSize + x] = grain;
                    }
                }
            }

            DrawDepthPebbles(pixels, depth, Palette.StoneDark);
            return pixels;
        }

        /// <summary>
        /// LE SOL D'UNE GALERIE. Meme lecture de la profondeur, meme absence de bord : la
        /// galerie se creuse dans la terre, elle ne se pose pas dessus en carreaux.
        /// </summary>
        private static Color32[] BuildTunnelTile(int depth)
        {
            Color32 body = TunnelColors[Mathf.Clamp(depth - 1, 0, TunnelColors.Length - 1)];
            Color32 grain = Palette.Shade(body);

            Color32[] pixels = new Color32[TileSize * TileSize];

            for (int i = 0; i < pixels.Length; i++)
            {
                pixels[i] = body;
            }

            for (int y = 0; y < TileSize; y++)
            {
                for (int x = 0; x < TileSize; x++)
                {
                    if (Speckle(x, y, 71 + depth) % 22 == 0)
                    {
                        pixels[y * TileSize + x] = grain;
                    }
                }
            }

            DrawDepthPebbles(pixels, depth, grain);
            return pixels;
        }

        /// <summary>
        /// Autant de cailloux que la profondeur, aux memes places d'une tuile a l'autre : on les
        /// compte d'un coup d'oeil au lieu de comparer deux bruns.
        /// </summary>
        private static void DrawDepthPebbles(Color32[] pixels, int depth, Color32 pebble)
        {
            Vector2Int[] places =
            {
                new Vector2Int(3, 11),
                new Vector2Int(11, 6),
                new Vector2Int(7, 2)
            };

            for (int i = 0; i < depth && i < places.Length; i++)
            {
                Vector2Int place = places[i];
                Fill(pixels, TileSize, place.x, place.x + 1, place.y, place.y + 1, pebble);
            }
        }

        /// <summary>
        /// LE MUR D'UNE PIECE : des planches verticales et deux lisses horizontales, plutot
        /// qu'un aplat de bois borde d'un lisere. C'est un mur d'atelier, on doit y accrocher
        /// des choses.
        /// </summary>
        private static Color32[] BuildRoomWallTile()
        {
            Color32[] pixels = new Color32[TileSize * TileSize];

            for (int i = 0; i < pixels.Length; i++)
            {
                pixels[i] = Palette.Wood;
            }

            Color32 joint = Palette.Shade(Palette.Wood);

            // Les planches, quatre par case, et leur grain.
            for (int x = 0; x < TileSize; x += 4)
            {
                Fill(pixels, TileSize, x, x, 0, TileSize - 1, joint);
            }

            for (int y = 0; y < TileSize; y++)
            {
                for (int x = 0; x < TileSize; x++)
                {
                    if (Speckle(x, y, 97) % 17 == 0)
                    {
                        pixels[y * TileSize + x] = Palette.Bark;
                    }
                }
            }

            // Les deux lisses : elles courent d'une case a l'autre sans se couper.
            Fill(pixels, TileSize, 0, TileSize - 1, 3, 4, Palette.Bark);
            Fill(pixels, TileSize, 0, TileSize - 1, 11, 12, Palette.Bark);
            Fill(pixels, TileSize, 0, TileSize - 1, 2, 2, joint);
            Fill(pixels, TileSize, 0, TileSize - 1, 10, 10, joint);

            return pixels;
        }

        /// <summary>
        /// UN MEUBLE adosse au mur du fond : un etabli, un rateau d'outils, une pile de caisses.
        /// Meme gabarit qu'un personnage, 16 sur 24, pour deborder vers le haut comme tout ce
        /// qui se dresse dans ce jeu depuis la phase 1.
        /// </summary>
        private static Color32[] BuildFurniture(int piece)
        {
            const int width = PlayerWidth;
            const int height = PlayerHeight;

            Color32[] pixels = NewTransparent(width * height);

            switch (piece)
            {
                case 0:
                    // L'ETABLI : un plateau epais, deux pieds, et un tiroir.
                    Fill(pixels, width, 1, 14, 10, 13, Palette.Bark);
                    Fill(pixels, width, 1, 14, 13, 13, Palette.Wood);
                    Fill(pixels, width, 2, 4, 1, 9, Palette.Wood);
                    Fill(pixels, width, 11, 13, 1, 9, Palette.Wood);
                    Fill(pixels, width, 5, 10, 5, 9, Palette.WoodDark);
                    Fill(pixels, width, 6, 9, 7, 7, Palette.Steel);
                    break;

                case 1:
                    // LE RATEAU D'OUTILS : une planche murale et trois outils pendus.
                    Fill(pixels, width, 0, 15, 14, 16, Palette.WoodDark);
                    Fill(pixels, width, 0, 15, 16, 16, Palette.Bark);
                    Fill(pixels, width, 2, 3, 6, 14, Palette.SteelLight);
                    Fill(pixels, width, 1, 4, 5, 6, Palette.Steel);
                    Fill(pixels, width, 7, 8, 8, 14, Palette.SteelLight);
                    Fill(pixels, width, 6, 9, 7, 8, Palette.Steel);
                    Fill(pixels, width, 12, 13, 7, 14, Palette.Gold);
                    Fill(pixels, width, 11, 14, 6, 7, Palette.Sun);
                    break;

                default:
                    // LA PILE DE CAISSES : deux en bas, une posee de travers dessus.
                    Fill(pixels, width, 0, 7, 1, 8, Palette.Wood);
                    Fill(pixels, width, 8, 15, 1, 8, Palette.Wood);
                    Fill(pixels, width, 3, 12, 9, 16, Palette.Bark);
                    Fill(pixels, width, 0, 7, 1, 1, Palette.WoodDark);
                    Fill(pixels, width, 8, 15, 1, 1, Palette.WoodDark);
                    Fill(pixels, width, 0, 15, 8, 8, Palette.WoodDark);
                    Fill(pixels, width, 3, 12, 16, 16, Palette.WoodDark);
                    Fill(pixels, width, 3, 12, 12, 12, Palette.WoodDark);
                    Fill(pixels, width, 7, 7, 1, 8, Palette.WoodDark);
                    Fill(pixels, width, 7, 8, 9, 16, Palette.WoodDark);
                    break;
            }

            return pixels;
        }

        /// <summary>
        /// LE MUR DE L'ENCEINTE de la station : des blocs de beton peint, decales d'une assise a
        /// l'autre, et l'arete du haut eclairee. C'etait un aplat bleu borde d'un lisere, donc
        /// une boite de plus sur l'herbe texturee.
        /// </summary>
        private static Color32[] BuildPlantWallTile()
        {
            Color32[] pixels = new Color32[TileSize * TileSize];

            for (int i = 0; i < pixels.Length; i++)
            {
                pixels[i] = Palette.SignBlue;
            }

            Color32 joint = Palette.Shade(Palette.SignBlue);

            // Quatre assises de quatre pixels, les joints verticaux decales d'une sur deux.
            for (int y = 0; y < TileSize; y++)
            {
                if (y % 4 == 0)
                {
                    Fill(pixels, TileSize, 0, TileSize - 1, y, y, joint);
                    continue;
                }

                int course = y / 4;
                for (int x = (course % 2) * 4; x < TileSize; x += 8)
                {
                    pixels[y * TileSize + x] = joint;
                }
            }

            // L'arete du haut, eclairee : c'est elle qui donne l'epaisseur au mur.
            Fill(pixels, TileSize, 0, TileSize - 1, TileSize - 1, TileSize - 1, Palette.Water);

            return pixels;
        }

        /// <summary>
        /// LE BETON de la station : uni, un peu granuleux, avec le joint d'une dalle de seize.
        /// </summary>
        private static Color32[] BuildConcreteTile()
        {
            Color32[] pixels = new Color32[TileSize * TileSize];

            for (int i = 0; i < pixels.Length; i++)
            {
                pixels[i] = Palette.SteelDark;
            }

            for (int y = 0; y < TileSize; y++)
            {
                for (int x = 0; x < TileSize; x++)
                {
                    if (Speckle(x, y, 31) % 18 == 0)
                    {
                        pixels[y * TileSize + x] = Palette.Steel;
                    }
                }
            }

            // PAS DE JOINT. Premier jet, deux bords en Charcoal : vu a l'ecran, la cour de la
            // station se lisait comme un quadrillage noir, exactement le defaut qu'on venait de
            // retirer a l'herbe. Le beton est uni ; ce sont les murs de l'enceinte qui le cadrent.
            return pixels;
        }

        /// <summary>Carre plein borde d'un lisere 1 px assombri.</summary>
        private static Color32[] BuildTile(Color32 fill)
        {
            Color32 border = Palette.Shade(fill);
            Color32[] pixels = new Color32[TileSize * TileSize];

            for (int y = 0; y < TileSize; y++)
            {
                for (int x = 0; x < TileSize; x++)
                {
                    bool onEdge = x == 0 || y == 0 || x == TileSize - 1 || y == TileSize - 1;
                    pixels[y * TileSize + x] = onEdge ? border : fill;
                }
            }

            return pixels;
        }

        private static void WriteTileTexture(string fileName, Color32 fill)
        {
            WriteTexture($"{TilesFolder}/{fileName}.png", BuildTile(fill));
        }

        /// <summary>
        /// Une canalisation vue de dessus : un corps central et un bras par raccord.
        /// Le masque suit PipeNetwork : bit 0 nord, 1 est, 2 sud, 3 ouest. Seize images
        /// dessinees par une seule fonction, pas seize dessins a la main.
        /// </summary>
        /// <summary>
        /// Une canalisation : la silhouette de la phase 3, plus un motif qui dit son type.
        ///
        /// La silhouette est RIGOUREUSEMENT LA MEME pour les trois motifs : seul le
        /// remplissage change. Deux tuyaux de types differents se raccordent donc a l'oeil
        /// comme ils se raccordent dans le graphe.
        ///
        /// Le motif est une nuance plus sombre du corps, et non une couleur a lui. C'est ce
        /// qui le fait survivre aux cinq teintes d'etat de la phase 5 : teinter multiplie
        /// toute la tuile, donc le contraste entre le corps et son motif est preserve, gele
        /// comme bouche comme porteur d'eau.
        /// </summary>
        private static Color32[] BuildPipe(int pattern, int mask)
        {
            Color32 body = Palette.SteelLight;
            Color32 outline = Palette.SteelDark;
            Color32 motif = Palette.Shade(body);

            Color32[] pixels = NewTransparent(TileSize * TileSize);

            DrawPipe(pixels, mask, grow: 1, color: outline);
            DrawPipe(pixels, mask, grow: 0, color: body);

            ApplyPipePattern(pixels, pattern, body, motif);

            return pixels;
        }

        /// <summary>
        /// Le motif, applique sur le seul corps du tuyau : le lisere n'est jamais touche,
        /// donc la silhouette reste identique d'un type a l'autre.
        ///
        /// 0 : corps uni, le tuyau d'aujourd'hui.
        /// 1 : raye en travers, comme la gaine d'un tuyau isole.
        /// 2 : pointille, comme la grille qui arrete les feuilles.
        /// </summary>
        private static void ApplyPipePattern(Color32[] pixels, int pattern, Color32 body,
            Color32 motif)
        {
            if (pattern == 0)
            {
                return;
            }

            for (int y = 0; y < TileSize; y++)
            {
                for (int x = 0; x < TileSize; x++)
                {
                    int index = y * TileSize + x;
                    Color32 pixel = pixels[index];

                    // Seul le corps recoit le motif. Comparaison champ par champ : Color32
                    // n'a pas d'operateur d'egalite.
                    if (pixel.r != body.r || pixel.g != body.g || pixel.b != body.b
                        || pixel.a != body.a)
                    {
                        continue;
                    }

                    bool marked = pattern == 1
                        ? (x + y) % 4 < 2       // bandes en diagonale, larges de deux pixels
                        : x % 3 == 0 && y % 3 == 0;  // points espaces de trois, une grille

                    if (marked)
                    {
                        pixels[index] = motif;
                    }
                }
            }
        }

        private static void DrawPipe(Color32[] pixels, int mask, int grow, Color32 color)
        {
            Fill(pixels, TileSize, 5 - grow, 10 + grow, 5 - grow, 10 + grow, color);

            if ((mask & 1) != 0)
            {
                Fill(pixels, TileSize, 6 - grow, 9 + grow, 10, 15, color);
            }

            if ((mask & 2) != 0)
            {
                Fill(pixels, TileSize, 10, 15, 6 - grow, 9 + grow, color);
            }

            if ((mask & 4) != 0)
            {
                Fill(pixels, TileSize, 6 - grow, 9 + grow, 0, 5, color);
            }

            if ((mask & 8) != 0)
            {
                Fill(pixels, TileSize, 0, 5, 6 - grow, 9 + grow, color);
            }
        }

        /// <summary>
        /// Bouche d'egout : disque sombre, lisere clair, deux fentes. Ronde plutot que
        /// carree pour qu'elle se distingue au premier coup d'oeil des tuiles du chemin.
        /// </summary>
        private static Color32[] BuildManhole()
        {
            Color32 cover = Palette.Charcoal;
            Color32 rim = Palette.Steel;
            Color32 slot = Palette.Charcoal;
            Color32 clear = new Color32(0, 0, 0, 0);

            Color32[] pixels = new Color32[TileSize * TileSize];
            const float center = (TileSize - 1) * 0.5f;

            for (int y = 0; y < TileSize; y++)
            {
                for (int x = 0; x < TileSize; x++)
                {
                    float dx = x - center;
                    float dy = y - center;
                    float distance = Mathf.Sqrt(dx * dx + dy * dy);

                    Color32 pixel;
                    if (distance > 7.4f)
                    {
                        pixel = clear;
                    }
                    else if (distance > 6.2f)
                    {
                        pixel = rim;
                    }
                    else if ((y == 6 || y == 9) && Mathf.Abs(dx) < 4f)
                    {
                        pixel = slot;
                    }
                    else
                    {
                        pixel = cover;
                    }

                    pixels[y * TileSize + x] = pixel;
                }
            }

            return pixels;
        }

        /// <summary>
        /// Personnage 16x24, un sprite par direction. Le corps ne change pas ; c'est le
        /// visage qui dit ou l'on regarde, et le dos de la tete qui dit qu'on s'eloigne.
        /// </summary>
        private static Color32[] BuildPlayer(Vector2Int facing)
        {
            const int width = PlayerWidth;
            const int height = PlayerHeight;

            Color32 body = CharacterColors[0];
            Color32 head = Palette.Skin;
            Color32 legs = Palette.Shade(body);
            Color32 hair = Palette.WoodDark;
            Color32 eye = Palette.Ink;

            Color32[] pixels = NewTransparent(width * height);

            // Origine en bas a gauche : y = 0 est la ligne des pieds.
            Fill(pixels, width, 4, 6, 0, 3, legs);      // jambe gauche
            Fill(pixels, width, 9, 11, 0, 3, legs);     // jambe droite
            Fill(pixels, width, 3, 12, 4, 14, body);    // torse
            Fill(pixels, width, 3, 12, 15, 22, head);   // tete
            Fill(pixels, width, 4, 11, 23, 23, head);   // sommet du crane, coins ronges

            if (facing == Vector2Int.up)
            {
                // De dos : pas de visage, une nuque de cheveux.
                Fill(pixels, width, 3, 12, 18, 23, hair);
            }
            else if (facing == Vector2Int.left)
            {
                Fill(pixels, width, 3, 12, 21, 23, hair);
                Fill(pixels, width, 4, 5, 18, 19, eye);
            }
            else if (facing == Vector2Int.right)
            {
                Fill(pixels, width, 3, 12, 21, 23, hair);
                Fill(pixels, width, 10, 11, 18, 19, eye);
            }
            else
            {
                Fill(pixels, width, 3, 12, 21, 23, hair);
                Fill(pixels, width, 5, 6, 18, 19, eye);
                Fill(pixels, width, 9, 10, 18, 19, eye);
            }

            return pixels;
        }

        /// <summary>
        /// Une maison 16x24 : quatre murs, un toit et une porte. Meme pivot que le
        /// personnage, elle se pose sur sa case et son toit deborde vers le haut.
        /// </summary>
        private static Color32[] BuildHouse()
        {
            const int width = PlayerWidth;
            const int height = PlayerHeight;

            Color32 wall = Palette.StoneLight;
            Color32 roof = Palette.Brick;
            Color32 door = Palette.WoodDark;
            Color32 window = Palette.Ice;

            Color32[] pixels = NewTransparent(width * height);

            Fill(pixels, width, 2, 13, 0, 14, wall);
            Fill(pixels, width, 6, 9, 0, 6, door);
            Fill(pixels, width, 3, 5, 9, 12, window);
            Fill(pixels, width, 10, 12, 9, 12, window);

            // Toit en pente : chaque ligne se resserre vers le faite.
            for (int row = 0; row <= 7; row++)
            {
                int half = 8 - row;
                Fill(pixels, width, 8 - half, 7 + half, 15 + row, 15 + row, roof);
            }

            return pixels;
        }

        /// <summary>
        /// L'arrivee d'une maison, vue du sous-sol : une collerette de raccordement, pour
        /// qu'on la distingue d'un tuyau ordinaire.
        /// </summary>
        private static Color32[] BuildHouseInlet()
        {
            Color32 flange = Palette.StoneLight;
            Color32 mouth = Palette.WoodDark;

            Color32[] pixels = NewTransparent(TileSize * TileSize);

            Fill(pixels, TileSize, 2, 13, 2, 13, flange);
            Fill(pixels, TileSize, 5, 10, 5, 10, mouth);

            return pixels;
        }

        /// <summary>
        /// La cuve du bassin d'orage, vue de dessus : un cadre de beton, un interieur
        /// sombre, et l'eau qui monte du bas. Douze pixels d'interieur, trois par palier :
        /// les cinq niveaux se distinguent a leur taille reelle. Trois traits clairs sur le
        /// bord gauche marquent les quarts, comme les graduations d'un verre doseur.
        /// </summary>
        private static Color32[] BuildReserve(int level)
        {
            Color32 outline = Palette.Charcoal;
            Color32 rim = Palette.SteelDark;
            Color32 inside = Palette.Charcoal;
            Color32 water = Palette.Water;
            Color32 waterTop = Palette.Ice;
            Color32 tick = Palette.SteelLight;

            const int innerBottom = 2;
            const int innerTop = 13;
            const int pixelsPerLevel = 3;

            Color32[] pixels = new Color32[TileSize * TileSize];

            Fill(pixels, TileSize, 0, 15, 0, 15, outline);
            Fill(pixels, TileSize, 1, 14, 1, 14, rim);
            Fill(pixels, TileSize, innerBottom, innerTop, innerBottom, innerTop, inside);

            int height = Mathf.Clamp(level, 0, ReserveLevelCount - 1) * pixelsPerLevel;
            if (height > 0)
            {
                int top = innerBottom + height - 1;
                Fill(pixels, TileSize, innerBottom, innerTop, innerBottom, top, water);
                Fill(pixels, TileSize, innerBottom, innerTop, top, top, waterTop);
            }

            // Les graduations passent par-dessus l'eau : elles se lisent cuve vide ou pleine.
            for (int mark = 1; mark < ReserveLevelCount - 1; mark++)
            {
                int y = innerBottom + mark * pixelsPerLevel - 1;
                Fill(pixels, TileSize, innerBottom, innerBottom + 1, y, y, tick);
            }

            return pixels;
        }

        /// <summary>
        /// La goutte : pleine quand la maison est raccordee, vide sinon. Aucune croix, aucun
        /// rouge : ne pas etre reliee n'est pas une faute.
        /// </summary>
        private static Color32[] BuildDrop(bool full)
        {
            Color32 fill = full
                ? Palette.Water
                : Palette.Steel;
            Color32 outline = Palette.Ink;

            Color32[] pixels = NewTransparent(TileSize * TileSize);

            DrawDrop(pixels, grow: 1, color: outline);
            DrawDrop(pixels, grow: 0, color: fill);

            return pixels;
        }

        /// <summary>
        /// Une goutte : rond en bas, pointe en haut. Dessinee deux fois, la premiere elargie
        /// d'un pixel, ce qui donne le contour.
        /// </summary>
        private static void DrawDrop(Color32[] pixels, int grow, Color32 color)
        {
            // Demi-largeur de chaque ligne, du bas vers le haut.
            int[] halves = { 2, 3, 4, 5, 5, 5, 5, 4, 3, 2, 2, 1, 1, 1 };

            for (int row = 0; row < halves.Length; row++)
            {
                int half = Mathf.Min(halves[row] + grow, 7);
                int y = 1 + row;
                Fill(pixels, TileSize, 8 - half, 7 + half, y, y, color);
            }
        }

        /// <summary>Echelle de remontee : deux montants et trois barreaux, fond transparent.</summary>
        /// <summary>
        /// L'ECHELLE, redessinee en phase 17c AU GABARIT DU PERSONNAGE, 16 sur 24.
        ///
        /// Elle tenait dans une seule case, donc debout dessus le joueur la RECOUVRAIT ENTIEREMENT
        /// et le seul chemin vers la surface disparaissait sous ses pieds — releve en phase 2 et
        /// laisse a l'habillage. Elle monte desormais huit pixels plus haut que sa case, comme la
        /// tete du personnage : elle reste visible derriere lui, et l'on voit ou l'on remonte.
        ///
        /// Le pivot est celui du joueur, au tiers : le transform se pose au centre de la case et
        /// le haut deborde.
        /// </summary>
        private static Color32[] BuildLadder()
        {
            const int width = PlayerWidth;
            const int height = PlayerHeight;

            Color32 rail = Palette.Gold;
            Color32 rung = Palette.Shade(rail);
            Color32 shadow = Palette.Ink;

            Color32[] pixels = NewTransparent(width * height);

            // LES MONTANTS SONT AUX BORDS, et c'est tout l'interet. Les mettre a 3 et 10 les
            // plaçait sous le corps du joueur, qui occupe le milieu : au meme gabarit que lui,
            // l'echelle etait EXACTEMENT recouverte et le premier essai n'a rien change. Aux
            // colonnes 1 et 14 elle depasse de chaque cote, et l'on voit ou l'on remonte meme
            // debout dessus.
            Fill(pixels, width, 1, 3, 1, height - 2, rail);
            Fill(pixels, width, 12, 14, 1, height - 2, rail);

            // Leur cote sombre : l'echelle a une epaisseur, elle n'est pas peinte au mur.
            Fill(pixels, width, 3, 3, 1, height - 2, shadow);
            Fill(pixels, width, 14, 14, 1, height - 2, shadow);

            // Les barreaux, tous les quatre pixels, sur toute la hauteur.
            for (int y = 3; y < height - 2; y += 4)
            {
                Fill(pixels, width, 3, 12, y, y + 1, rung);
                Fill(pixels, width, 3, 12, y + 1, y + 1, shadow);
            }

            return pixels;
        }

        /// <summary>Repere « je suis en haut » : un soleil et ses quatre rayons sur fond de ciel.</summary>
        private static Color32[] BuildSunPicto()
        {
            Color32 sky = Palette.BlueDeep;
            Color32 sun = Palette.Sun;

            Color32[] pixels = new Color32[PictoSize * PictoSize];
            const float center = (PictoSize - 1) * 0.5f;

            for (int y = 0; y < PictoSize; y++)
            {
                for (int x = 0; x < PictoSize; x++)
                {
                    float dx = x - center;
                    float dy = y - center;
                    pixels[y * PictoSize + x] = Mathf.Sqrt(dx * dx + dy * dy) <= 8f ? sun : sky;
                }
            }

            // Quatre rayons, un par point cardinal.
            Fill(pixels, PictoSize, 15, 16, 2, 5, sun);
            Fill(pixels, PictoSize, 15, 16, 26, 29, sun);
            Fill(pixels, PictoSize, 2, 5, 15, 16, sun);
            Fill(pixels, PictoSize, 26, 29, 15, 16, sun);

            return pixels;
        }

        /// <summary>Repere « je suis en bas » : une echelle sur fond de terre.</summary>
        private static Color32[] BuildLadderPicto()
        {
            Color32 earth = Palette.WoodDark;
            Color32 rail = Palette.Gold;
            Color32 rung = Palette.Shade(rail);

            Color32[] pixels = new Color32[PictoSize * PictoSize];
            for (int i = 0; i < pixels.Length; i++)
            {
                pixels[i] = earth;
            }

            Fill(pixels, PictoSize, 9, 11, 3, 28, rail);
            Fill(pixels, PictoSize, 20, 22, 3, 28, rail);

            foreach (int y in new[] { 6, 12, 18, 24 })
            {
                Fill(pixels, PictoSize, 12, 19, y, y + 1, rung);
            }

            return pixels;
        }

        /// <summary>
        /// Picto d'action affiche au-dessus de la tete : une fleche vers le bas sur une
        /// bouche, vers le haut sur une echelle. Cerne d'un contour sombre pour rester
        /// lisible sur n'importe quel sol.
        /// </summary>
        private static Color32[] BuildArrow(bool pointingDown)
        {
            Color32 fill = Palette.Paper;
            Color32 outline = Palette.Ink;

            Color32[] pixels = NewTransparent(TileSize * TileSize);

            DrawArrow(pixels, pointingDown, grow: 1, color: outline);
            DrawArrow(pixels, pointingDown, grow: 0, color: fill);

            return pixels;
        }

        /// <summary>
        /// Une fleche : tete triangulaire de sept lignes, puis une tige. Dessinee deux fois,
        /// la premiere elargie d'un pixel, ce qui donne le contour.
        /// </summary>
        private static void DrawArrow(Color32[] pixels, bool pointingDown, int grow, Color32 color)
        {
            if (grow > 0)
            {
                int tip = pointingDown ? 0 : TileSize - 1;
                Fill(pixels, TileSize, 7, 8, tip, tip, color);
            }

            for (int step = 1; step <= 7; step++)
            {
                int half = Mathf.Min(step + grow, 7);
                int row = pointingDown ? step : TileSize - 1 - step;
                Fill(pixels, TileSize, 8 - half, 7 + half, row, row, color);
            }

            for (int step = 8; step <= 14; step++)
            {
                int row = pointingDown ? step : TileSize - 1 - step;
                Fill(pixels, TileSize, 6 - grow, 9 + grow, row, row, color);
            }
        }

        /// <summary>« Ici on creuse » : une pelle, manche et fer.</summary>
        private static Color32[] BuildDigPicto()
        {
            Color32 handle = Palette.Bark;
            Color32 blade = Palette.SteelLight;
            Color32 outline = Palette.Ink;

            Color32[] pixels = NewTransparent(TileSize * TileSize);

            // Contour d'abord, motif ensuite : le picto reste lisible sur la terre comme sur
            // la galerie.
            Fill(pixels, TileSize, 5, 10, 7, 15, outline);
            Fill(pixels, TileSize, 3, 12, 1, 7, outline);

            Fill(pixels, TileSize, 6, 9, 8, 14, handle);
            Fill(pixels, TileSize, 4, 11, 2, 6, blade);

            return pixels;
        }

        /// <summary>« Ici on enleve » : un disque barre, comme un panneau.</summary>
        /// <summary>
        /// ENLEVER UN TUYAU, redessine en phase 17e. C'etait un DISQUE BLANC BARRE DE ROUGE,
        /// c'est-a-dire le vocabulaire de l'INTERDICTION — un sens interdit, presque. Or on
        /// n'interdit rien : on retire ce qu'on avait pose.
        ///
        /// Desormais un tuyau vu en bout, et une fleche qui l'en sort par le haut. C'est le
        /// geste, pas une defense. Releve dans les placeholders depuis la phase 3 : « le picto
        /// enlever est un disque barre, vocabulaire d'interdiction plutot que de retrait ».
        /// </summary>
        private static Color32[] BuildRemovePicto()
        {
            Color32 pipe = Palette.SteelLight;
            Color32 rim = Palette.SteelDark;
            Color32 hole = Palette.Charcoal;
            Color32 arrow = Palette.Sun;
            Color32 edge = Palette.Ink;

            Color32[] pixels = NewTransparent(TileSize * TileSize);

            // Le tuyau, couche en bas : deux brides et son ouverture sombre.
            Fill(pixels, TileSize, 1, 14, 1, 6, rim);
            Fill(pixels, TileSize, 2, 13, 2, 5, pipe);
            Fill(pixels, TileSize, 6, 9, 2, 5, hole);

            // La fleche qui l'en sort, cernee pour se lire sur la terre comme sur le pave.
            Fill(pixels, TileSize, 6, 9, 7, 11, arrow);
            for (int row = 0; row < 4; row++)
            {
                int half = 4 - row;
                Fill(pixels, TileSize, 8 - half, 7 + half, 11 + row, 11 + row, arrow);
            }

            Fill(pixels, TileSize, 5, 5, 7, 11, edge);
            Fill(pixels, TileSize, 10, 10, 7, 11, edge);

            return pixels;
        }

        /// <summary>
        /// « Ici on repare » : une cle plate, machoire ouverte vers le haut. Le vocabulaire
        /// de l'atelier plutot que celui de l'interdiction.
        /// </summary>
        private static Color32[] BuildRepairPicto()
        {
            Color32 metal = Palette.SteelLight;
            Color32 outline = Palette.Ink;
            Color32 clear = new Color32(0, 0, 0, 0);

            Color32[] pixels = NewTransparent(TileSize * TileSize);

            // Contours d'abord, corps ensuite : le picto reste lisible sur la terre comme
            // sur un tuyau.
            Fill(pixels, TileSize, 5, 10, 0, 11, outline);
            Fill(pixels, TileSize, 3, 12, 9, 15, outline);

            Fill(pixels, TileSize, 6, 9, 1, 10, metal);
            Fill(pixels, TileSize, 4, 11, 10, 14, metal);

            // L'entaille de la machoire, puis son fond : deux dents et un creux.
            Fill(pixels, TileSize, 6, 9, 13, 15, clear);
            Fill(pixels, TileSize, 6, 9, 12, 12, outline);

            return pixels;
        }

        /// <summary>Printemps : une pousse qui sort de terre, sur un vert tendre.</summary>
        private static Color32[] BuildSpringPicto()
        {
            Color32 sky = Palette.Grass;
            Color32 soil = Palette.Wood;
            Color32 plant = Palette.GrassDark;

            Color32[] pixels = FilledPicto(sky);

            Fill(pixels, PictoSize, 0, PictoSize - 1, 0, 4, soil);
            Fill(pixels, PictoSize, 15, 16, 4, 24, plant);

            // Deux feuilles, l'une plus haute que l'autre : une pousse n'est pas symetrique.
            FillEllipse(pixels, PictoSize, 10f, 20f, 5f, 3f, plant);
            FillEllipse(pixels, PictoSize, 21f, 14f, 5f, 3f, plant);

            return pixels;
        }

        /// <summary>Ete : un soleil haut et plein, huit rayons, sur un or pale.</summary>
        private static Color32[] BuildSummerPicto()
        {
            Color32 sky = Palette.Bone;
            Color32 sun = Palette.Orange;

            Color32[] pixels = FilledPicto(sky);

            FillEllipse(pixels, PictoSize, 15.5f, 15.5f, 9f, 9f, sun);

            // Quatre rayons cardinaux, quatre en diagonale : le soleil au zenith.
            Fill(pixels, PictoSize, 15, 16, 27, 30, sun);
            Fill(pixels, PictoSize, 15, 16, 1, 4, sun);
            Fill(pixels, PictoSize, 1, 4, 15, 16, sun);
            Fill(pixels, PictoSize, 27, 30, 15, 16, sun);

            Fill(pixels, PictoSize, 5, 7, 24, 26, sun);
            Fill(pixels, PictoSize, 24, 26, 24, 26, sun);
            Fill(pixels, PictoSize, 5, 7, 5, 7, sun);
            Fill(pixels, PictoSize, 24, 26, 5, 7, sun);

            return pixels;
        }

        /// <summary>Automne : une feuille et sa nervure, sur un orange de feuillage.</summary>
        private static Color32[] BuildAutumnPicto()
        {
            Color32 sky = Palette.Skin;
            Color32 leaf = Palette.Brick;
            Color32 vein = Palette.Orange;

            Color32[] pixels = FilledPicto(sky);

            // Un losange allonge : large au milieu, pointu aux deux bouts.
            for (int y = 6; y <= 27; y++)
            {
                int half = Mathf.RoundToInt(9f - Mathf.Abs(y - 17f) * 0.85f);
                if (half <= 0)
                {
                    continue;
                }

                Fill(pixels, PictoSize, 15 - half, 16 + half, y, y, leaf);
            }

            Fill(pixels, PictoSize, 15, 16, 3, 24, vein);

            return pixels;
        }

        /// <summary>Hiver : un flocon a six branches, sur un bleu de givre.</summary>
        private static Color32[] BuildWinterPicto()
        {
            Color32 sky = Palette.Ice;
            Color32 flake = Palette.Paper;

            Color32[] pixels = FilledPicto(sky);

            Fill(pixels, PictoSize, 15, 16, 3, 28, flake);
            Fill(pixels, PictoSize, 3, 28, 15, 16, flake);

            // Les deux diagonales, tracees pixel par pixel plutot qu'en rectangles.
            for (int step = -12; step <= 12; step++)
            {
                PlotThick(pixels, 15 + step, 15 + step, flake);
                PlotThick(pixels, 15 + step, 16 - step, flake);
            }

            // Les pointes des quatre branches droites, comme sur un vrai flocon.
            Fill(pixels, PictoSize, 12, 19, 25, 26, flake);
            Fill(pixels, PictoSize, 12, 19, 5, 6, flake);
            Fill(pixels, PictoSize, 5, 6, 12, 19, flake);
            Fill(pixels, PictoSize, 25, 26, 12, 19, flake);

            return pixels;
        }

        /// <summary>Un picto 32x32 rempli d'une couleur de fond.</summary>
        private static Color32[] FilledPicto(Color32 background)
        {
            Color32[] pixels = new Color32[PictoSize * PictoSize];

            for (int i = 0; i < pixels.Length; i++)
            {
                pixels[i] = background;
            }

            return pixels;
        }

        /// <summary>Un point epais de 2x2, borne au picto. Sert aux traits en diagonale.</summary>
        private static void PlotThick(Color32[] pixels, int x, int y, Color32 color)
        {
            for (int dy = 0; dy <= 1; dy++)
            {
                for (int dx = 0; dx <= 1; dx++)
                {
                    int px = x + dx;
                    int py = y + dy;

                    if (px >= 0 && px < PictoSize && py >= 0 && py < PictoSize)
                    {
                        pixels[py * PictoSize + px] = color;
                    }
                }
            }
        }

        /// <summary>Une ellipse pleine, bornee a l'image. Feuilles et soleils.</summary>
        private static void FillEllipse(Color32[] pixels, int width, float cx, float cy,
            float rx, float ry, Color32 color)
        {
            int height = pixels.Length / width;

            for (int y = 0; y < height; y++)
            {
                for (int x = 0; x < width; x++)
                {
                    float dx = (x - cx) / rx;
                    float dy = (y - cy) / ry;

                    if (dx * dx + dy * dy <= 1f)
                    {
                        pixels[y * width + x] = color;
                    }
                }
            }
        }

        /// <summary>
        /// Une plaque d'egout : un disque cercle d'un jonc, et un motif creuse dedans.
        ///
        /// Les huit motifs sont des familles de geometrie inspirees de styles regionaux, pas
        /// des emblemes de villes. Un blason municipal est une oeuvre a part entiere, et
        /// CLAUDE.md n'autorise que l'original ou le CC0.
        /// </summary>
        private static Color32[] BuildCover(int index)
        {
            Color32 rim = Palette.SteelDark;
            Color32 body = Palette.Steel;
            Color32 groove = Palette.SteelDark;
            Color32 clear = new Color32(0, 0, 0, 0);

            Color32[] pixels = new Color32[TileSize * TileSize];
            const float center = (TileSize - 1) * 0.5f;

            for (int y = 0; y < TileSize; y++)
            {
                for (int x = 0; x < TileSize; x++)
                {
                    float dx = x - center;
                    float dy = y - center;
                    float radius = Mathf.Sqrt(dx * dx + dy * dy);

                    Color32 pixel;

                    if (radius > 7.4f)
                    {
                        pixel = clear;
                    }
                    else if (radius > 6.3f)
                    {
                        pixel = rim;
                    }
                    else
                    {
                        // Atan2 rend un angle de -PI a PI ; on le ramene dans [0, 2 PI[ pour
                        // que les decoupes angulaires soient continues.
                        float angle = Mathf.Atan2(dy, dx) + Mathf.PI;
                        pixel = HasGroove(index, x, y, dx, dy, radius, angle) ? groove : body;
                    }

                    pixels[y * TileSize + x] = pixel;
                }
            }

            return pixels;
        }

        /// <summary>
        /// Le motif creuse de chaque plaque. Un cas par ville.
        ///
        /// Les huit motifs doivent se distinguer a leur taille reelle, seize pixels de cote.
        /// C'est la seule contrainte qui compte ici, et elle est severe : la premiere version
        /// donnait a New York et a Londres deux quadrillages qu'on ne pouvait pas separer.
        /// Ils ont ete redessines, pas doubles.
        /// </summary>
        private static bool HasGroove(int index, int x, int y, float dx, float dy, float radius,
            float angle)
        {
            const float sector = Mathf.PI / 6f;

            switch (index)
            {
                case 0:  // Paris : gaufrage fin en losanges
                    return (x + y) % 4 == 0 || (x - y + 16) % 4 == 0;

                case 1:  // Tokyo : une fleur, six petales autour d'un coeur
                    return radius < 1.9f || IsPetal(dx, dy);

                case 2:  // Berlin : anneaux concentriques
                    return Mathf.RoundToInt(radius) % 2 == 0;

                case 3:  // New York : gros appareillage de briques, decale d'un rang a l'autre
                    return y % 5 == 0 || (x + (y / 5) * 2) % 5 == 0;

                case 4:  // Amsterdam : losanges en diagonale, largement espaces
                    return (x + y) % 5 == 0 || (x - y + 20) % 5 == 0;

                case 5:  // Londres : une croix epaisse qui partage la plaque en quatre panneaux
                    return Mathf.Abs(dx) < 1.1f || Mathf.Abs(dy) < 1.1f
                        || (radius > 5.3f && radius < 6.2f);

                case 6:  // Rome : rayons partant du centre
                    return radius > 1.8f && Mathf.RoundToInt(angle / sector) % 2 == 0;

                default: // Lisbonne : vague en spirale
                    return Mathf.RoundToInt(radius * 1.6f + angle * 1.4f) % 3 == 0;
            }
        }

        /// <summary>Six petales poses en couronne, pour la plaque japonaise.</summary>
        private static bool IsPetal(float dx, float dy)
        {
            const float ring = 4.1f;
            const float petal = 2.1f;

            for (int i = 0; i < 6; i++)
            {
                float a = i * Mathf.PI / 3f;
                float px = dx - Mathf.Cos(a) * ring;
                float py = dy - Mathf.Sin(a) * ring;

                if (px * px + py * py < petal * petal)
                {
                    return true;
                }
            }

            return false;
        }

        /// <summary>
        /// L'eau du village : un bleu SEMI-TRANSPARENT, avec deux trains de vaguelettes.
        ///
        /// Semi-transparente parce qu'elle se pose sur l'herbe, sur le chemin et sur la dalle
        /// du parc : un bleu opaque ferait un carre plein qui cacherait le village, la ou une
        /// eau qui laisse voir le sol dessous se lit tout de suite comme de l'eau.
        ///
        /// Les vaguelettes sont decoupees pour que la tuile se repete sans couture visible :
        /// chaque train traverse le bord et reprend de l'autre cote.
        /// </summary>
        private static Color32[] BuildWater()
        {
            Color32 water = Palette.WithAlpha(Palette.Water, 0xB4);
            Color32 ripple = Palette.WithAlpha(Palette.Ice, 0xC8);

            Color32[] pixels = new Color32[TileSize * TileSize];
            for (int i = 0; i < pixels.Length; i++)
            {
                pixels[i] = water;
            }

            // Rangee du bas : un train qui repart a droite et reprend a gauche.
            Fill(pixels, TileSize, 12, 15, 4, 4, ripple);
            Fill(pixels, TileSize, 0, 1, 4, 4, ripple);
            Fill(pixels, TileSize, 5, 9, 4, 4, ripple);

            // Rangee du haut, decalee : deux vagues ne se superposent jamais.
            Fill(pixels, TileSize, 2, 6, 11, 11, ripple);
            Fill(pixels, TileSize, 9, 13, 11, 11, ripple);

            return pixels;
        }

        /// <summary>
        /// La fontaine du parc : un bassin rond de pierre, son bord clair, et un jet au
        /// milieu. Ronde plutot que carree pour la meme raison que la bouche d'egout : elle se
        /// distingue au premier coup d'oeil des haies et des dalles qui l'entourent.
        ///
        /// Le jet est dessine sur le bassin, meme a l'arret : c'est une fontaine, pas un
        /// puits. Ce qui dit qu'elle marche, c'est l'eau que FloodView pose autour d'elle.
        /// </summary>
        /// <summary>
        /// Une haie a masque de raccord, phase 12c. Le masque suit BuildPipe : bit 0 nord,
        /// 1 est, 2 sud, 3 ouest. La haie remplit sa case et POUSSE vers ses voisines ; sur un
        /// cote libre elle se retire d'un pixel et montre sa tranche claire.
        ///
        /// Le labyrinthe fait deux cent quarante-deux cases de haie. Sans le masque, ce sont
        /// deux cent quarante-deux carres verts identiques separes par un lisere : on ne lit
        /// plus un mur, on lit un damier, et le labyrinthe cesse d'etre lisible.
        /// </summary>
        /// <summary>
        /// Le picto d'agrandissement de la station, phase 12d : une cuve, et une croix qui
        /// dit « une de plus ». Pas de mot, pas de chiffre : le picto d'abord, comme partout.
        /// </summary>
        /// <summary>
        /// Le signal d'attention d'un guide : un triangle de danger, borde de rouge, avec son
        /// point d'exclamation. Vocabulaire du Code de la route, comme les panneaux de la
        /// phase 12c, et il se lit de loin sans un mot.
        /// </summary>
        private static Color32[] BuildAttentionPicto()
        {
            Color32 red = Palette.SignRed;
            Color32 white = Palette.Paper;

            Color32[] pixels = NewTransparent(TileSize * TileSize);

            // La POINTE EN HAUT : c'est un danger. Un triangle pointe en bas serait un cedez
            // le passage, qui dit tout autre chose. En espace de texture y monte, donc la base
            // large est en bas et la pointe en haut.
            for (int row = 0; row < 14; row++)
            {
                int half = (13 - row) * 8 / 14;
                Fill(pixels, TileSize, 8 - half, 7 + half, 1 + row, 1 + row, red);
            }

            for (int row = 1; row < 11; row++)
            {
                int half = (13 - row) * 8 / 14 - 2;
                if (half <= 0)
                {
                    continue;
                }

                Fill(pixels, TileSize, 8 - half, 7 + half, 1 + row, 1 + row, white);
            }

            // Le point d'exclamation, en creux dans le blanc : la barre puis le point.
            Fill(pixels, TileSize, 7, 8, 4, 9, red);
            Fill(pixels, TileSize, 7, 8, 2, 2, red);

            return pixels;
        }

        private static Color32[] BuildGrowPicto()
        {
            Color32 tank = Palette.SteelDark;
            Color32 water = Palette.Water;
            Color32 plus = Palette.Paper;

            Color32[] pixels = NewTransparent(TileSize * TileSize);

            Fill(pixels, TileSize, 1, 10, 2, 12, tank);
            Fill(pixels, TileSize, 2, 9, 3, 9, water);

            // La croix, en haut a droite : « une cuve de plus ».
            Fill(pixels, TileSize, 11, 15, 11, 12, plus);
            Fill(pixels, TileSize, 12, 13, 9, 14, plus);

            return pixels;
        }

        /// <summary>
        /// Un bassin de traitement pose sur le sol de la station, phase 12d. Un par
        /// agrandissement : la station grossit a l'ecran, et le progres du joueur se voit sans
        /// jauge et sans compteur au HUD.
        /// </summary>
        private static Color32[] BuildPlantBasin()
        {
            Color32 wall = Palette.Steel;
            Color32 water = Palette.Water;
            Color32 glint = Palette.Ice;

            Color32[] pixels = NewTransparent(TileSize * TileSize);

            Fill(pixels, TileSize, 1, 14, 1, 14, wall);
            Fill(pixels, TileSize, 3, 12, 3, 12, water);
            Fill(pixels, TileSize, 4, 7, 9, 10, glint);

            return pixels;
        }

        private static Color32[] BuildHedge(int mask)
        {
            Color32 leaf = Palette.GrassDeep;
            Color32 crown = Palette.GrassDark;
            Color32 shade = Palette.Shade(leaf);

            Color32[] pixels = NewTransparent(TileSize * TileSize);

            // Le corps se retire d'un pixel de chaque cote LIBRE, et touche le bord partout
            // ou une haie continue.
            int x0 = (mask & 8) != 0 ? 0 : 1;
            int x1 = (mask & 2) != 0 ? TileSize - 1 : TileSize - 2;
            int y0 = (mask & 4) != 0 ? 0 : 1;
            int y1 = (mask & 1) != 0 ? TileSize - 1 : TileSize - 2;

            Fill(pixels, TileSize, x0, x1, y0, y1, leaf);

            // Le feuillage : des touffes claires, toujours au meme endroit, pour que deux
            // haies voisines ne se lisent pas comme une seule masse plate.
            for (int y = y0; y <= y1; y++)
            {
                for (int x = x0; x <= x1; x++)
                {
                    if ((x + 2 * y) % 5 == 0)
                    {
                        pixels[y * TileSize + x] = crown;
                    }
                }
            }

            // L'ombre portee sur les cotes libres : c'est elle qui donne l'epaisseur du mur.
            if ((mask & 4) == 0) Fill(pixels, TileSize, x0, x1, y0, y0, shade);
            if ((mask & 8) == 0) Fill(pixels, TileSize, x0, x0, y0, y1, shade);

            return pixels;
        }

        /// <summary>
        /// Une route a masque de raccord, phase 12c. Meme masque que la haie. La chaussee
        /// s'etend vers ses voisines et se borde d'un accotement clair sur ses cotes libres :
        /// une route se lit alors comme une route, avec ses virages et ses carrefours, et non
        /// comme une nappe beige.
        /// </summary>
        private static Color32[] BuildRoad(int mask)
        {
            Color32 verge = Palette.Stone;
            Color32 asphalt = Palette.StoneDark;
            Color32 paint = Palette.Bone;

            Color32[] pixels = new Color32[TileSize * TileSize];
            for (int i = 0; i < pixels.Length; i++)
            {
                pixels[i] = verge;
            }

            // La chaussee : un carre central, plus un bras vers chaque voisine.
            Fill(pixels, TileSize, 3, 12, 3, 12, asphalt);
            if ((mask & 1) != 0) Fill(pixels, TileSize, 3, 12, 12, 15, asphalt);
            if ((mask & 2) != 0) Fill(pixels, TileSize, 12, 15, 3, 12, asphalt);
            if ((mask & 4) != 0) Fill(pixels, TileSize, 3, 12, 0, 3, asphalt);
            if ((mask & 8) != 0) Fill(pixels, TileSize, 0, 3, 3, 12, asphalt);

            // La bande blanche, seulement sur une portion droite : elle dit le sens de la
            // route. Un carrefour ou un virage n'en porte pas, comme en vrai.
            bool straightVertical = mask == 5;
            bool straightHorizontal = mask == 10;

            if (straightVertical)
            {
                for (int y = 1; y < TileSize; y += 4)
                {
                    Fill(pixels, TileSize, 7, 8, y, y + 1, paint);
                }
            }
            else if (straightHorizontal)
            {
                for (int x = 1; x < TileSize; x += 4)
                {
                    Fill(pixels, TileSize, x, x + 1, 7, 8, paint);
                }
            }

            return pixels;
        }

        /// <summary>
        /// Un arbre, phase 12c. Seize sur vingt-quatre comme la maison et le personnage : sa
        /// cime deborde vers le haut et son tronc tient dans sa case. Il BLOQUE, et sa tuile
        /// bloquante est une image separee, BuildTreeBase.
        /// </summary>
        private static Color32[] BuildTree()
        {
            const int width = PlayerWidth;
            const int height = PlayerHeight;

            Color32 trunk = Palette.Wood;
            Color32 bark = Palette.Shade(trunk);
            Color32 leaf = Palette.GrassDark;
            Color32 light = Palette.Grass;

            Color32[] pixels = NewTransparent(width * height);

            Fill(pixels, width, 6, 9, 0, 9, trunk);
            Fill(pixels, width, 6, 6, 0, 9, bark);

            // La cime : trois rangs de plus en plus larges, puis un sommet arrondi.
            Fill(pixels, width, 2, 13, 9, 18, leaf);
            Fill(pixels, width, 3, 12, 18, 20, leaf);
            Fill(pixels, width, 5, 10, 20, 22, leaf);
            Fill(pixels, width, 7, 8, 22, 23, leaf);

            for (int y = 9; y < height; y++)
            {
                for (int x = 0; x < width; x++)
                {
                    int index = y * width + x;
                    if (pixels[index].a != 0 && (x + 3 * y) % 7 == 0)
                    {
                        pixels[index] = light;
                    }
                }
            }

            return pixels;
        }

        /// <summary>
        /// La tuile bloquante de l'arbre : son pied. Une image separee du sprite, patron de
        /// house.png / tile_house.png, sans quoi l'arbre serait cloue a la taille d'une case.
        /// </summary>
        private static Color32[] BuildTreeBase()
        {
            Color32 trunk = Palette.Wood;
            Color32 root = Palette.Shade(trunk);

            // LA MEME PELOUSE QUE PARTOUT, phase 17b. C'etait un carre de vert sombre borde d'un
            // lisere : sur l'herbe texturee, chaque arbre se dressait dans une BOITE NOIRE, et
            // les 126 arbres du village avec lui. Vu a l'ecran, jamais deduit — le validateur de
            // palette acceptait ce carre, et il avait raison, ce sont de bonnes couleurs.
            Color32[] pixels = BuildGrassTile();

            // L'ombre portee au pied, en vert sombre : elle pose l'arbre au sol sans le cadrer.
            for (int y = 2; y <= 7; y++)
            {
                int half = y <= 4 ? 5 : 4;
                Fill(pixels, TileSize, 8 - half, 7 + half, y, y, Palette.GrassDeep);
            }

            Fill(pixels, TileSize, 5, 10, 3, 12, root);
            Fill(pixels, TileSize, 6, 9, 4, 11, trunk);

            return pixels;
        }

        /// <summary>
        /// Un panneau de signalisation. Seize sur vingt-quatre : un poteau dans la case, la
        /// plaque au-dessus. Le catalogue est celui du CODE DE LA ROUTE FRANCAIS, et rien
        /// d'autre : chaque rang correspond a un panneau qui existe, avec son numero.
        ///
        /// LA PLANCHE DE L'USINE, phase 13 : quatre familles de six. La forme et la bordure
        /// disent la FAMILLE avant que le dessin dise le detail — un triangle borde de rouge
        /// previent, un disque borde de rouge interdit, un disque bleu plein oblige. C'est
        /// cette grammaire-la qui s'apprend d'abord.
        ///
        ///   0 AB3a cedez le passage (triangle POINTE EN BAS, borde de rouge)
        ///   1 AB4  arret obligatoire, STOP (octogone rouge)
        ///   2 C13a impasse (carre bleu, voie en T barree de rouge)  — hors planche
        ///   3 AB2  route prioritaire (losange jaune sur losange blanc)
        ///   4 AB6  fin de route prioritaire (le meme, barre de noir)
        ///   5 a 8  panonceau de jalonnement (rectangle bleu, fleche blanche nord / est /
        ///          sud / ouest), reserve aux carrefours de galeries — hors planche
        ///   9 AB1  intersection avec priorite a droite
        ///  10 AB25 carrefour a sens giratoire
        ///  11 A1b  virage a gauche
        ///  12 A1c  succession de virages
        ///  13 A4   chaussee glissante
        ///  14 A13b passage pour pietons
        ///  15 A14  danger
        ///  16 A21  debouche de cyclistes
        ///  17 B0   circulation interdite a tout vehicule dans les deux sens
        ///  18 B1   sens interdit a tout vehicule
        ///  19 B2b  interdiction de tourner a droite a la prochaine intersection
        ///  20 B9a  acces interdit aux pietons
        ///  21 B6a1 stationnement interdit
        ///  22 B6d  arret et stationnement interdits
        ///  23 B21b direction obligatoire : tout droit
        ///  24 B21c1 direction obligatoire : a droite
        ///  25 B21e directions obligatoires : a droite ou a gauche
        ///  26 B21a1 contournement obligatoire par la droite
        ///  27 B22a piste ou bande obligatoire pour les cycles
        ///  28 B22b chemin obligatoire pour pietons
        ///
        /// EN ESPACE DE TEXTURE Y MONTE : le poteau occupe le bas, la plaque le haut, et une
        /// base large en bas fait une pointe en haut. C'est la seule chose a garder en tete
        /// ici, et c'est elle qui avait mis le cedez le passage a l'envers.
        /// </summary>
        private static Color32[] BuildSign(int kind)
        {
            const int width = PlayerWidth;
            const int height = PlayerHeight;

            Color32 post = Palette.Steel;
            Color32 red = Palette.SignRed;
            Color32 blue = Palette.SignBlue;
            Color32 white = Palette.Paper;
            Color32 yellow = Palette.Sun;
            Color32 black = Palette.Charcoal;

            Color32[] pixels = NewTransparent(width * height);

            Fill(pixels, width, 7, 8, 0, 13, post);

            const int cx = 8;
            const int cy = 18;

            switch (kind)
            {
                case 0:
                    // AB3a : la pointe EN BAS, c'est ce qui le distingue d'un danger. Comme y
                    // monte en espace de texture, la BASE LARGE EST EN HAUT. Ce panneau etait
                    // dessine pointe en haut depuis la phase 12c : les onze cedez le passage
                    // du village etaient des triangles de danger, et rien ne le disait.
                    for (int row = 0; row < 9; row++)
                    {
                        int half = 8 - row;
                        int y = cy + 4 - row;
                        Fill(pixels, width, cx - half, cx + half - 1, y, y, red);
                    }

                    for (int row = 0; row < 6; row++)
                    {
                        int half = 6 - row - 2;
                        if (half > 0)
                        {
                            int y = cy + 3 - row - 1;
                            Fill(pixels, width, cx - half, cx + half - 1, y, y, white);
                        }
                    }
                    break;

                case 1:
                    // AB4 : un carre dont on rabote les quatre coins, et la barre du mot STOP.
                    Fill(pixels, width, cx - 5, cx + 4, cy - 5, cy + 4, red);
                    for (int dy = 0; dy < 2; dy++)
                    {
                        for (int dx = 0; dx < 2 - dy; dx++)
                        {
                            Color32 clear = new Color32(0, 0, 0, 0);
                            pixels[(cy - 5 + dy) * width + cx - 5 + dx] = clear;
                            pixels[(cy - 5 + dy) * width + cx + 4 - dx] = clear;
                            pixels[(cy + 4 - dy) * width + cx - 5 + dx] = clear;
                            pixels[(cy + 4 - dy) * width + cx + 4 - dx] = clear;
                        }
                    }

                    // LE MOT, ET NON UNE BARRE. Phase 17f : la plaque portait un seul trait
                    // blanc horizontal, ce qui donne, dans un octogone rouge de seize pixels,
                    // presque exactement le SENS INTERDIT du rang 18 — un disque rouge a barre
                    // blanche. Or le jeu montre les deux : le stop dans les rues, le sens
                    // interdit sur la planche de l'usine.
                    //
                    // Quatre traits verticaux de trois rangees se lisent comme un MOT, jamais
                    // comme une barre. On ne peut pas ecrire STOP en huit pixels ; on peut
                    // ecrire qu'il y a quelque chose d'ecrit, et c'est ce qui separe les deux.
                    for (int letter = 0; letter < 4; letter++)
                    {
                        int x = cx - 4 + letter * 2;
                        Fill(pixels, width, x, x, cy - 1, cy + 1, white);
                    }
                    break;

                case 2:
                    // C13a : un carre bleu, une voie blanche en T dont la barre du haut est
                    // rouge — la rue s'arrete la.
                    Fill(pixels, width, cx - 5, cx + 4, cy - 5, cy + 4, blue);
                    Fill(pixels, width, cx - 1, cx, cy - 4, cy + 1, white);
                    Fill(pixels, width, cx - 3, cx + 2, cy + 1, cy + 2, red);
                    break;

                case 3:
                case 4:
                    // AB2 : un losange jaune dans un losange blanc. AB6 : le meme, barre.
                    DrawDiamond(pixels, width, cx, cy, 6, white);
                    DrawDiamond(pixels, width, cx, cy, 3, yellow);
                    if (kind == 4)
                    {
                        for (int i = -5; i <= 4; i++)
                        {
                            int x = cx + i;
                            int y = cy - i - 1;
                            if (x >= 0 && x < width && y >= 0 && y < height)
                            {
                                pixels[y * width + x] = black;
                            }
                        }
                    }
                    break;

                case 5:
                case 6:
                case 7:
                case 8:
                    // Panonceau de jalonnement : un rectangle bleu, une fleche blanche. Ce
                    // n'est pas un B21 « direction obligatoire » — un disque —, qui dirait au
                    // conducteur ce qu'il DOIT faire ; il dit ou est la station, rien de plus.
                    Fill(pixels, width, cx - 6, cx + 5, cy - 4, cy + 3, blue);
                    DrawArrow(pixels, width, cx, cy, kind - SignFirstArrow, white);
                    break;

                case 9:
                    // AB1 : la croix de Saint-Andre, l'intersection ou l'on cede a droite.
                    DrawDangerPlate(pixels, width, red, white);
                    Ink(pixels, width, cx - 2, 14, white, black);
                    Ink(pixels, width, cx + 2, 14, white, black);
                    Ink(pixels, width, cx - 1, 15, white, black);
                    Ink(pixels, width, cx + 1, 15, white, black);
                    Ink(pixels, width, cx, 16, white, black);
                    Ink(pixels, width, cx - 1, 17, white, black);
                    Ink(pixels, width, cx + 1, 17, white, black);
                    Ink(pixels, width, cx - 2, 18, white, black);
                    Ink(pixels, width, cx + 2, 18, white, black);
                    break;

                case 10:
                    // AB25 : l'anneau du giratoire. Un anneau plein et non un losange : a
                    // cette taille un losange evide ne se distinguait pas d'une croix.
                    DrawDangerPlate(pixels, width, red, white);
                    InkSpan(pixels, width, cx - 2, cx + 2, 14, 14, white, black);
                    Ink(pixels, width, cx - 2, 15, white, black);
                    Ink(pixels, width, cx + 2, 15, white, black);
                    Ink(pixels, width, cx - 2, 16, white, black);
                    Ink(pixels, width, cx + 2, 16, white, black);
                    InkSpan(pixels, width, cx - 1, cx + 1, 17, 17, white, black);
                    break;

                case 11:
                    // A1b : UN virage. La chaussee monte, puis tourne a gauche.
                    DrawDangerPlate(pixels, width, red, white);
                    InkSpan(pixels, width, cx - 1, cx, 14, 16, white, black);
                    InkSpan(pixels, width, cx - 3, cx, 17, 17, white, black);
                    InkSpan(pixels, width, cx - 3, cx - 2, 18, 18, white, black);
                    break;

                case 12:
                    // A1c : DEUX virages, l'S. Le double changement de sens est tout ce qui
                    // le separe du precedent a cette taille : une seule courbe et ce serait
                    // le meme dessin.
                    DrawDangerPlate(pixels, width, red, white);
                    InkSpan(pixels, width, cx - 4, cx - 3, 14, 14, white, black);
                    InkSpan(pixels, width, cx - 2, cx - 1, 15, 15, white, black);
                    InkSpan(pixels, width, cx, cx + 1, 16, 16, white, black);
                    InkSpan(pixels, width, cx - 1, cx, 17, 17, white, black);
                    InkSpan(pixels, width, cx - 2, cx - 1, 18, 18, white, black);
                    break;

                case 13:
                    // A4 : deux traces de derapage. La voiture a ete retiree : trois pixels
                    // de carrosserie au-dessus des traces ne faisaient qu'un pate noir.
                    DrawDangerPlate(pixels, width, red, white);
                    Ink(pixels, width, cx - 3, 14, white, black);
                    Ink(pixels, width, cx - 2, 15, white, black);
                    Ink(pixels, width, cx - 2, 16, white, black);
                    Ink(pixels, width, cx - 3, 17, white, black);
                    Ink(pixels, width, cx - 3, 18, white, black);
                    Ink(pixels, width, cx + 2, 14, white, black);
                    Ink(pixels, width, cx + 1, 15, white, black);
                    Ink(pixels, width, cx + 1, 16, white, black);
                    Ink(pixels, width, cx + 2, 17, white, black);
                    Ink(pixels, width, cx + 2, 18, white, black);
                    break;

                case 14:
                    // A13b : le pieton et le passage clout sous ses pieds.
                    DrawDangerPlate(pixels, width, red, white);
                    DrawPedestrian(pixels, width, cx, 16,
                        (x0, x1, y0, y1) => InkSpan(pixels, width, x0, x1, y0, y1, white, black));
                    for (int x = cx - 3; x <= cx + 3; x += 2)
                    {
                        Ink(pixels, width, x, 14, white, black);
                    }
                    break;

                case 15:
                    // A14 : le point d'exclamation. La barre en haut, le point en bas.
                    DrawDangerPlate(pixels, width, red, white);
                    InkSpan(pixels, width, cx - 1, cx, 16, 19, white, black);
                    InkSpan(pixels, width, cx - 1, cx, 14, 14, white, black);
                    break;

                case 16:
                    // A21 : le velo qui debouche. Deux roues evidees posees dans les deux
                    // rangees les plus larges du triangle — DrawBicycle, dessine pour le
                    // creux rond d'un disque, sortait rogne d'un cote dans un triangle.
                    DrawDangerPlate(pixels, width, red, white);
                    foreach (int wheel in new[] { cx - 3, cx + 3 })
                    {
                        InkSpan(pixels, width, wheel - 1, wheel + 1, 14, 14, white, black);
                        InkSpan(pixels, width, wheel - 1, wheel + 1, 16, 16, white, black);
                        Ink(pixels, width, wheel - 1, 15, white, black);
                        Ink(pixels, width, wheel + 1, 15, white, black);
                    }

                    InkSpan(pixels, width, cx - 1, cx + 1, 16, 16, white, black);
                    Ink(pixels, width, cx, 17, white, black);
                    break;

                case 17:
                    // B0 : le disque borde de rouge, vide. Rien ne passe, dans aucun sens.
                    DrawProhibitionPlate(pixels, width, red, white);
                    break;

                case 18:
                    // B1 : le disque PLEIN de rouge et sa barre blanche. C'est ce plein qui le
                    // distingue du B0, qui n'a qu'une bordure.
                    DrawDisc(pixels, width, cx, cy, 5.6f, red);
                    Fill(pixels, width, cx - 3, cx + 2, cy - 1, cy, white);
                    break;

                case 19:
                    // B2b : la fleche qui part a droite, barree. Une fleche COUDEE ne tenait
                    // pas dans les sept pixels du creux : elle sortait en gribouillis.
                    DrawProhibitionPlate(pixels, width, red, white);
                    Fill(pixels, width, cx - 3, cx, cy - 1, cy, black);
                    Fill(pixels, width, cx + 1, cx + 1, cy - 2, cy + 1, black);
                    Fill(pixels, width, cx + 2, cx + 2, cy - 1, cy, black);
                    DrawSlash(pixels, width, cx, cy, red);
                    break;

                case 20:
                    // B9a : le pieton, barre.
                    DrawProhibitionPlate(pixels, width, red, white);
                    DrawPedestrian(pixels, width, cx, cy - 1,
                        (x0, x1, y0, y1) => Fill(pixels, width, x0, x1, y0, y1, black));
                    DrawSlash(pixels, width, cx, cy, red);
                    break;

                case 21:
                    // B6a1 : le disque BLEU borde de rouge, une seule barre.
                    DrawProhibitionPlate(pixels, width, red, blue);
                    DrawSlash(pixels, width, cx, cy, red);
                    break;

                case 22:
                    // B6d : le meme, DEUX barres croisees. La barre contre la croix, c'est
                    // exactement la distinction qui s'apprend.
                    DrawProhibitionPlate(pixels, width, red, blue);
                    DrawSlash(pixels, width, cx, cy, red);
                    DrawBackslash(pixels, width, cx, cy, red);
                    break;

                case 23:
                    // B21b : tout droit.
                    DrawDisc(pixels, width, cx, cy, 5.6f, blue);
                    Fill(pixels, width, cx - 1, cx, cy - 4, cy + 1, white);
                    DrawArrow(pixels, width, cx, cy + 1, 0, white);
                    break;

                case 24:
                    // B21c1 : a droite. Le fut monte, puis la fleche part a droite.
                    DrawDisc(pixels, width, cx, cy, 5.6f, blue);
                    Fill(pixels, width, cx - 2, cx - 1, cy - 4, cy, white);
                    Fill(pixels, width, cx - 1, cx + 1, cy - 1, cy, white);
                    DrawArrow(pixels, width, cx + 1, cy, 1, white);
                    break;

                case 25:
                    // B21e : a droite ou a gauche, une fleche a deux tetes.
                    DrawDisc(pixels, width, cx, cy, 5.6f, blue);
                    Fill(pixels, width, cx - 2, cx + 1, cy - 1, cy, white);
                    DrawArrow(pixels, width, cx + 1, cy, 1, white);
                    DrawArrow(pixels, width, cx - 2, cy, 3, white);
                    break;

                case 26:
                    // B21a1 : contourner par la droite, la fleche en biais.
                    DrawDisc(pixels, width, cx, cy, 5.6f, blue);
                    for (int step = 0; step < 4; step++)
                    {
                        Fill(pixels, width, cx - 2 + step, cx - 1 + step,
                            cy - 3 + step, cy - 3 + step, white);
                    }

                    Fill(pixels, width, cx, cx + 2, cy + 1, cy + 1, white);
                    Fill(pixels, width, cx + 2, cx + 2, cy - 1, cy + 1, white);
                    break;

                case 27:
                    // B22a : le velo, en blanc sur le bleu.
                    DrawDisc(pixels, width, cx, cy, 5.6f, blue);
                    DrawBicycle(pixels, width, cx, cy,
                        (x0, x1, y0, y1) => Fill(pixels, width, x0, x1, y0, y1, white));
                    break;

                case 28:
                    // B22b : le pieton, en blanc sur le bleu.
                    DrawDisc(pixels, width, cx, cy, 5.6f, blue);
                    DrawPedestrian(pixels, width, cx, cy - 1,
                        (x0, x1, y0, y1) => Fill(pixels, width, x0, x1, y0, y1, white));
                    break;

                default:
                    // AUCUN DEFAUT MUET. Jusqu'a la phase 13 ce default dessinait une fleche
                    // de jalonnement : porter SignCount sans ajouter le dessin aurait sorti
                    // vingt panonceaux bleus identiques a la place de vingt panneaux, SANS UN
                    // MOT. C'est le meme defaut que le « default: return cell » de GroundAt.
                    Debug.LogError($"[Sous la Ville] Le rang de panneau {kind} n'est dessiné " +
                                   $"nulle part. SignCount vaut {SignCount} : ajoute son cas " +
                                   "dans BuildSign, ou baisse SignCount.");
                    break;
            }

            return pixels;
        }

        /// <summary>Le pinceau d'un pictogramme : plein sur un disque, garde sur un triangle.</summary>
        private delegate void Brush(int x0, int x1, int y0, int y1);

        /// <summary>
        /// La plaque d'un panneau de DANGER : triangle POINTE EN HAUT, borde de rouge, fond
        /// blanc. En espace de texture y monte, donc la base large est en bas.
        ///
        /// Un triangle pointe en bas serait un cedez le passage, qui dit tout autre chose :
        /// c'est la faute qui a ete corrigee sur le rang 0 en phase 13.
        /// </summary>
        private static void DrawDangerPlate(Color32[] pixels, int width, Color32 red,
            Color32 white)
        {
            for (int row = 0; row <= 10; row++)
            {
                int half = Mathf.RoundToInt((10 - row) * 0.8f);
                Fill(pixels, width, 8 - half, 7 + half, 13 + row, 13 + row, red);
            }

            // La bordure fait deux pixels dans le bas du triangle et un seul pres de la
            // pointe. A deux partout, le blanc tombait a quatre pixels de large des la
            // cinquieme rangee : le bras d'une croix n'y tenait plus, et les pictogrammes
            // sortaient tous rognes du meme cote.
            for (int row = 1; row <= 6; row++)
            {
                int half = Mathf.RoundToInt((10 - row) * 0.8f) - (row <= 4 ? 2 : 1);
                if (half > 0)
                {
                    Fill(pixels, width, 8 - half, 7 + half, 13 + row, 13 + row, white);
                }
            }
        }

        /// <summary>
        /// La plaque d'une INTERDICTION : un disque borde de rouge. Le centre est blanc pour
        /// les interdictions de circuler, bleu pour celles de stationner — c'est le Code.
        /// </summary>
        private static void DrawProhibitionPlate(Color32[] pixels, int width, Color32 red,
            Color32 inner)
        {
            DrawDisc(pixels, width, 8, 18, 5.6f, red);
            DrawDisc(pixels, width, 8, 18, 3.8f, inner);
        }

        /// <summary>La barre oblique d'une interdiction, du bas gauche vers le haut droit.</summary>
        private static void DrawSlash(Color32[] pixels, int width, int cx, int cy, Color32 color)
        {
            for (int i = -3; i <= 3; i++)
            {
                Ink(pixels, width, cx + i, cy + i, null, color);
            }
        }

        /// <summary>L'autre barre, celle qui fait la croix du B6d.</summary>
        private static void DrawBackslash(Color32[] pixels, int width, int cx, int cy,
            Color32 color)
        {
            for (int i = -3; i <= 3; i++)
            {
                Ink(pixels, width, cx + i, cy - i, null, color);
            }
        }

        /// <summary>
        /// Un pieton : tete, tronc, bras, deux jambes. Six pixels de haut, trois de large —
        /// il tient dans le blanc d'un triangle comme dans le creux d'un disque.
        /// </summary>
        private static void DrawPedestrian(Color32[] pixels, int width, int cx, int cy,
            Brush brush)
        {
            brush(cx, cx, cy + 3, cy + 3);           // tete
            brush(cx, cx, cy, cy + 2);               // tronc
            brush(cx - 1, cx + 1, cy + 1, cy + 1);   // bras
            brush(cx - 1, cx - 1, cy - 2, cy - 1);   // jambe gauche
            brush(cx + 1, cx + 1, cy - 2, cy - 1);   // jambe droite
        }

        /// <summary>
        /// Un velo : deux roues evidees de trois pixels et un cadre. Sept pixels de large,
        /// la largeur exacte du creux d'un disque d'interdiction.
        /// </summary>
        private static void DrawBicycle(Color32[] pixels, int width, int cx, int cy, Brush brush)
        {
            for (int side = -1; side <= 1; side += 2)
            {
                int wheel = cx + side * 2;
                brush(wheel - 1, wheel + 1, cy - 1, cy - 1);
                brush(wheel - 1, wheel + 1, cy + 1, cy + 1);
                brush(wheel - 1, wheel - 1, cy, cy);
                brush(wheel + 1, wheel + 1, cy, cy);
            }

            brush(cx - 1, cx + 1, cy + 1, cy + 1);   // cadre
            brush(cx, cx, cy + 2, cy + 2);           // guidon
        }

        /// <summary>
        /// Un pixel de pictogramme. Avec un fond donne, il n'est pose QUE sur ce fond : le
        /// blanc d'un triangle est etroit et penche, et sans cette garde un bras de croix
        /// trouerait la bordure rouge. Sans fond (null), il est pose partout.
        /// </summary>
        private static void Ink(Color32[] pixels, int width, int x, int y, Color32? background,
            Color32 color)
        {
            if (x < 0 || x >= width || y < 0 || y * width + x >= pixels.Length)
            {
                return;
            }

            if (background.HasValue)
            {
                Color32 current = pixels[y * width + x];
                Color32 wanted = background.Value;

                if (current.r != wanted.r || current.g != wanted.g || current.b != wanted.b
                    || current.a != wanted.a)
                {
                    return;
                }
            }

            pixels[y * width + x] = color;
        }

        /// <summary>Un pave de pictogramme, pose sous la meme garde qu'Ink.</summary>
        private static void InkSpan(Color32[] pixels, int width, int x0, int x1, int y0, int y1,
            Color32 background, Color32 color)
        {
            for (int y = y0; y <= y1; y++)
            {
                for (int x = x0; x <= x1; x++)
                {
                    Ink(pixels, width, x, y, background, color);
                }
            }
        }

        /// <summary>Un losange plein, centre sur (cx, cy), de demi-diagonale donnee.</summary>
        private static void DrawDiamond(Color32[] pixels, int width, int cx, int cy, int half,
            Color32 color)
        {
            for (int dy = -half; dy <= half; dy++)
            {
                int span = half - Mathf.Abs(dy);
                Fill(pixels, width, cx - span, cx + span - 1, cy + dy, cy + dy, color);
            }
        }

        /// <summary>Un disque plein, centre sur (cx, cy).</summary>
        private static void DrawDisc(Color32[] pixels, int width, int cx, int cy, float radius,
            Color32 color)
        {
            int span = Mathf.CeilToInt(radius);

            for (int dy = -span; dy <= span; dy++)
            {
                for (int dx = -span; dx <= span; dx++)
                {
                    if (dx * dx + dy * dy > radius * radius)
                    {
                        continue;
                    }

                    int x = cx + dx;
                    int y = cy + dy;

                    if (x < 0 || x >= width || y < 0 || y * width + x >= pixels.Length)
                    {
                        continue;
                    }

                    pixels[y * width + x] = color;
                }
            }
        }

        /// <summary>
        /// La fleche d'un panneau de direction. 0 nord, 1 est, 2 sud, 3 ouest : le meme ordre
        /// que les bits du masque de raccord, pour n'avoir qu'une convention dans le projet.
        /// </summary>
        private static void DrawArrow(Color32[] pixels, int width, int cx, int cy, int direction,
            Color32 color)
        {
            for (int step = 0; step < 4; step++)
            {
                int half = 3 - step;

                switch (direction)
                {
                    case 0:
                        Fill(pixels, width, cx - half, cx + half - 1, cy + step, cy + step, color);
                        break;
                    case 2:
                        Fill(pixels, width, cx - half, cx + half - 1, cy - step, cy - step, color);
                        break;
                    case 1:
                        Fill(pixels, width, cx + step, cx + step, cy - half, cy + half - 1, color);
                        break;
                    default:
                        Fill(pixels, width, cx - step, cx - step, cy - half, cy + half - 1, color);
                        break;
                }
            }

            // La hampe, dans l'axe de la pointe.
            if (direction == 0) Fill(pixels, width, cx - 1, cx, cy - 4, cy, color);
            else if (direction == 2) Fill(pixels, width, cx - 1, cx, cy, cy + 4, color);
            else if (direction == 1) Fill(pixels, width, cx - 4, cx, cy - 1, cy, color);
            else Fill(pixels, width, cx, cx + 4, cy - 1, cy, color);
        }

        /// <summary>
        /// La fontaine VUE DE FACE, phase 12c : seize sur vingt-quatre comme la maison. Un
        /// bassin, une colonne, une vasque, un jet. Elle ne pouvait pas grandir tant que la
        /// tuile bloquante et le sprite sortaient du meme fichier.
        /// </summary>
        private static Color32[] BuildFountainSprite()
        {
            const int width = PlayerWidth;
            const int height = PlayerHeight;

            Color32 stone = Palette.Steel;
            Color32 rim = Palette.SteelLight;
            Color32 water = Palette.Water;
            Color32 jet = Palette.Ice;

            Color32[] pixels = NewTransparent(width * height);

            // Le bassin du bas, dans la case.
            Fill(pixels, width, 1, 14, 0, 6, stone);
            Fill(pixels, width, 2, 13, 1, 5, water);
            Fill(pixels, width, 1, 14, 6, 6, rim);

            // La colonne.
            Fill(pixels, width, 6, 9, 7, 14, stone);
            Fill(pixels, width, 7, 8, 7, 14, rim);

            // La vasque haute.
            Fill(pixels, width, 3, 12, 15, 17, stone);
            Fill(pixels, width, 4, 11, 16, 17, water);
            Fill(pixels, width, 3, 12, 17, 17, rim);

            // Le jet, qui retombe de part et d'autre.
            Fill(pixels, width, 7, 8, 18, 23, jet);
            Fill(pixels, width, 5, 5, 19, 21, jet);
            Fill(pixels, width, 10, 10, 19, 21, jet);

            return pixels;
        }

        /// <summary>
        /// La tuile BLOQUANTE de la fontaine : son socle vu de dessus. C'est l'ancienne image
        /// de la phase 11, qui servait aussi de sprite ; depuis la phase 12c elle ne sert plus
        /// qu'a bloquer, et le sprite vit dans fountain.png.
        /// </summary>
        private static Color32[] BuildFountainBase()
        {
            return BuildFountain();
        }

        private static Color32[] BuildFountain()
        {
            Color32 rim = Palette.SteelLight;
            Color32 stone = Palette.Steel;
            Color32 basin = Palette.Water;
            Color32 jet = Palette.Ice;

            Color32[] pixels = NewTransparent(TileSize * TileSize);
            const float center = (TileSize - 1) * 0.5f;

            for (int y = 0; y < TileSize; y++)
            {
                for (int x = 0; x < TileSize; x++)
                {
                    float dx = x - center;
                    float dy = y - center;
                    float radius = Mathf.Sqrt(dx * dx + dy * dy);

                    if (radius > 7.4f)
                    {
                        continue;
                    }

                    Color32 pixel;
                    if (radius > 6.2f)
                    {
                        pixel = stone;
                    }
                    else if (radius > 4.8f)
                    {
                        pixel = rim;
                    }
                    else
                    {
                        pixel = basin;
                    }

                    pixels[y * TileSize + x] = pixel;
                }
            }

            // Le jet, une colonne au milieu du bassin.
            Fill(pixels, TileSize, 7, 8, 6, 11, jet);
            Fill(pixels, TileSize, 6, 9, 10, 11, jet);

            return pixels;
        }

        /// <summary>
        /// La porte d'un batiment, posee sur le seuil devant sa facade. Un encadrement clair,
        /// une ouverture sombre, une poignee : on la reconnait de loin, et Espace dessus fait
        /// entrer comme sur une bouche d'egout.
        /// </summary>
        private static Color32[] BuildDoor()
        {
            Color32 frame = Palette.Wood;
            Color32 opening = Palette.Ink;
            Color32 handle = Palette.Sun;

            Color32[] pixels = NewTransparent(TileSize * TileSize);

            Fill(pixels, TileSize, 2, 13, 0, 14, frame);
            Fill(pixels, TileSize, 4, 11, 0, 12, opening);
            Fill(pixels, TileSize, 9, 10, 6, 7, handle);

            return pixels;
        }

        /// <summary>
        /// Un personnage de batiment. Meme silhouette que le personnage joueur, 16x24 et meme
        /// pivot, dans une autre couleur : on voit du premier coup d'oeil que c'est quelqu'un
        /// d'autre, sans avoir a le comparer.
        ///
        /// Il regarde vers le bas, donc vers la porte : il fait face a qui entre.
        ///
        /// L'artisan est vert, l'ouvrier bleu. Ils ne se distinguent que par la couleur, ce
        /// qui est un placeholder de plus a reprendre a l'habillage.
        /// </summary>
        /// <summary>
        /// LES CINQ METIERS, phase 17d. Chacun se reconnait a SA SILHOUETTE, pas seulement a sa
        /// couleur : ce qu'il porte sur la tete change son contour, et ce qu'il porte sur la
        /// poitrine dit son metier. Une couleur se compare, une silhouette se reconnait — et
        /// c'est la regle du projet depuis la phase 0, on reconnait quelqu'un de loin sans un mot.
        ///
        /// Jusqu'ici les cinq etaient LE MEME CORPS repeint, et la liste des placeholders le
        /// disait deux fois : « l'artisan est le personnage joueur repeint en vert », « les trois
        /// ne se distinguent que par la couleur ».
        /// </summary>
        public enum VillagerTrade
        {
            /// <summary>L'artisan des plaques : casquette plate, une plaque ronde sur la poitrine.</summary>
            Covers,

            /// <summary>L'ouvrier des tuyaux : casque de chantier, un tuyau en travers.</summary>
            Pipes,

            /// <summary>Le Stock : bonnet et tablier, une caisse dans les bras.</summary>
            Stock,

            /// <summary>La Fabrique : beret d'atelier, un crayon a la main.</summary>
            Maker,

            /// <summary>Le Plan : visiere et lunettes, un rouleau de plans sous le bras.</summary>
            Planner,

            /// <summary>Les huit guides : gilet de chantier et casquette, celui qui previent.</summary>
            Guide
        }

        private static Color32[] BuildVillager(Color32 body, VillagerTrade trade)
        {
            const int width = PlayerWidth;
            const int height = PlayerHeight;

            Color32 head = Palette.Skin;
            Color32 legs = Palette.Shade(body);
            Color32 hair = Palette.Charcoal;
            Color32 eye = Palette.Ink;

            Color32[] pixels = NewTransparent(width * height);

            Fill(pixels, width, 4, 6, 0, 3, legs);
            Fill(pixels, width, 9, 11, 0, 3, legs);
            Fill(pixels, width, 3, 12, 4, 14, body);
            Fill(pixels, width, 3, 12, 15, 22, head);
            Fill(pixels, width, 4, 11, 23, 23, head);
            Fill(pixels, width, 5, 6, 18, 19, eye);
            Fill(pixels, width, 9, 10, 18, 19, eye);

            switch (trade)
            {
                case VillagerTrade.Covers:
                    // Casquette plate a visiere, et la plaque d'egout sur la poitrine.
                    Fill(pixels, width, 3, 12, 21, 22, Palette.Charcoal);
                    Fill(pixels, width, 4, 11, 23, 23, Palette.Charcoal);
                    Fill(pixels, width, 1, 8, 20, 20, Palette.Charcoal);
                    DrawDisc(pixels, width, 8, 9, 2.6f, Palette.Steel);
                    Fill(pixels, width, 6, 10, 9, 9, Palette.Charcoal);
                    break;

                case VillagerTrade.Pipes:
                    // Casque de chantier : une calotte qui deborde, et sa crete.
                    Fill(pixels, width, 2, 13, 21, 22, Palette.Sun);
                    Fill(pixels, width, 3, 12, 23, 23, Palette.Sun);
                    Fill(pixels, width, 7, 8, 22, 23, Palette.Gold);
                    // Un tuyau en travers de la poitrine.
                    Fill(pixels, width, 2, 13, 10, 11, Palette.SteelLight);
                    Fill(pixels, width, 2, 13, 9, 9, Palette.SteelDark);
                    break;

                case VillagerTrade.Stock:
                    // Bonnet SUR TOUTE LA LARGEUR du crane : dessine de 4 a 11, il laissait deux
                    // coins de peau nus et le personnage sortait avec des CORNES.
                    Fill(pixels, width, 3, 12, 21, 23, hair);
                    Fill(pixels, width, 3, 12, 20, 20, hair);
                    Fill(pixels, width, 6, 9, 23, 23, Palette.Steel);

                    // Un tablier de toile CLAIRE, et une caisse portee sous le bras.
                    //
                    // Premier jet : tablier en Shade(body) sur six rangees, caisse en bois par
                    // dessus. Shade(Brick) EST WoodDark — le tablier et la caisse devenaient le
                    // meme brun, et la couleur du corps, qui est justement ce qui distingue les
                    // cinq de loin, ne se voyait plus du tout. Vu en relisant les pixels.
                    Fill(pixels, width, 4, 11, 4, 6, Palette.Bone);
                    Fill(pixels, width, 4, 11, 6, 6, Palette.StoneDark);
                    Fill(pixels, width, 12, 15, 6, 12, Palette.Wood);
                    Fill(pixels, width, 12, 15, 12, 12, Palette.WoodDark);
                    Fill(pixels, width, 12, 15, 6, 6, Palette.WoodDark);
                    Fill(pixels, width, 12, 15, 9, 9, Palette.WoodDark);
                    break;

                case VillagerTrade.Maker:
                    // Beret : une calotte molle qui deborde d'un cote, et un crayon.
                    Fill(pixels, width, 3, 12, 21, 22, Palette.BlueDeep);
                    Fill(pixels, width, 4, 13, 23, 23, Palette.BlueDeep);
                    Fill(pixels, width, 13, 14, 22, 22, Palette.BlueDeep);
                    Fill(pixels, width, 12, 13, 6, 13, Palette.Sun);
                    Fill(pixels, width, 12, 13, 13, 13, Palette.Ink);
                    break;

                case VillagerTrade.Guide:
                    // Casquette et GILET DE CHANTIER : deux bandes claires en croix sur la
                    // poitrine, le vetement de celui qui previent. On le reconnait de loin, et
                    // c'est tout ce qu'on lui demande.
                    Fill(pixels, width, 3, 12, 21, 23, Palette.Charcoal);
                    Fill(pixels, width, 2, 8, 20, 20, Palette.Charcoal);
                    Fill(pixels, width, 3, 12, 4, 14, Palette.Sun);
                    Fill(pixels, width, 6, 9, 4, 14, body);
                    Fill(pixels, width, 3, 12, 8, 9, Palette.Paper);
                    Fill(pixels, width, 3, 12, 12, 13, Palette.Paper);
                    break;

                default:
                    // Visiere claire, DE PETITES lunettes, et un rouleau de plans sous le bras.
                    // Larges de quatre pixels sur quatre, elles couvraient tout le visage et il
                    // sortait en masque de soudeur : deux verres serres autour des yeux suffisent.
                    Fill(pixels, width, 3, 12, 21, 23, Palette.Paper);
                    Fill(pixels, width, 2, 13, 20, 20, Palette.SteelLight);
                    Fill(pixels, width, 4, 11, 17, 20, Palette.Ice);
                    Fill(pixels, width, 7, 8, 18, 19, Palette.Skin);
                    Fill(pixels, width, 5, 6, 18, 19, eye);
                    Fill(pixels, width, 9, 10, 18, 19, eye);
                    Fill(pixels, width, 0, 2, 6, 13, Palette.Paper);
                    Fill(pixels, width, 0, 2, 13, 13, Palette.Steel);
                    Fill(pixels, width, 0, 2, 9, 9, Palette.SignBlue);
                    break;
            }

            return pixels;
        }

        /// <summary>
        /// « Ici on entre » et « ici on sort » : un chambranle, et une fleche qui va dedans ou
        /// qui en vient. Le vocabulaire des panneaux, que Victorien aime, plutot que deux
        /// fleches nues qui se confondraient avec descendre et remonter.
        /// </summary>
        private static Color32[] BuildDoorPicto(bool entering)
        {
            Color32 frame = Palette.Paper;
            Color32 opening = Palette.Ink;
            Color32 arrow = Palette.Sun;

            Color32[] pixels = NewTransparent(TileSize * TileSize);

            // Le chambranle occupe la moitie droite : il reste de la place a gauche pour la
            // fleche, dans les deux sens.
            Fill(pixels, TileSize, 8, 15, 1, 14, frame);
            Fill(pixels, TileSize, 10, 15, 1, 12, opening);

            // Hampe de la fleche, puis sa pointe. Vers la droite on entre, vers la gauche on
            // sort : le chambranle ne bouge pas, seule la fleche se retourne.
            Fill(pixels, TileSize, 1, 8, 6, 8, arrow);

            for (int step = 0; step < 4; step++)
            {
                int x = entering ? 9 + step : 4 - step;
                int half = 3 - step;
                Fill(pixels, TileSize, x, x, 7 - half, 7 + half, arrow);
            }

            return pixels;
        }

        /// <summary>
        /// « On peut lui parler » : une bulle et ses trois points. Aucun mot a lire pour
        /// savoir qu'il y a quelque chose a lire.
        /// </summary>
        private static Color32[] BuildTalkPicto()
        {
            Color32 bubble = Palette.Paper;
            Color32 outline = Palette.Ink;

            Color32[] pixels = NewTransparent(TileSize * TileSize);

            // Contour d'abord, bulle ensuite : lisible sur le sol clair d'une piece comme sur
            // la terre du sous-sol.
            Fill(pixels, TileSize, 1, 14, 4, 15, outline);
            Fill(pixels, TileSize, 4, 7, 1, 4, outline);

            Fill(pixels, TileSize, 2, 13, 5, 14, bubble);
            Fill(pixels, TileSize, 5, 6, 2, 5, bubble);

            // Les trois points, dans le creux de la bulle.
            Fill(pixels, TileSize, 4, 5, 9, 10, outline);
            Fill(pixels, TileSize, 7, 8, 9, 10, outline);
            Fill(pixels, TileSize, 10, 11, 9, 10, outline);

            return pixels;
        }

        /// <summary>
        /// Une phrase de personnage, dessinee par PixelFont comme les noms de villes.
        ///
        /// Les phrases sont fixes et connues ici : les dessiner a la generation garde la
        /// police cote Editor, et donne des images versionnees, relisibles a l'oeil, et
        /// nettes a la grille du pixel, ce qu'une TTF ne serait pas.
        /// </summary>
        private static Color32[] BuildSentence(string sentence)
        {
            return PixelFont.Render(sentence,
                Palette.Paper,
                Palette.Ink);
        }

        /// <summary>
        /// Le plan du village, une case par pixel, engendre depuis VillageLayout. Deux dessins
        /// d'un meme village finiraient par diverger ; celui-ci en sort, donc il ne peut pas
        /// mentir.
        /// </summary>
        private static Color32[] BuildVillageMap()
        {
            Color32 grass = Palette.Grass;
            Color32 road = Palette.Stone;
            Color32 park = Palette.SteelLight;
            Color32 plantFloor = Palette.SteelDark;
            Color32 hedge = Palette.GrassDeep;
            Color32 plantWall = Palette.SignBlue;
            Color32 house = Palette.Brick;
            Color32 facade = Palette.Bark;
            Color32 tree = Palette.GrassDark;

            Color32[] pixels = new Color32[VillageLayout.Width * VillageLayout.Height];

            for (int y = 0; y < VillageLayout.Height; y++)
            {
                for (int x = 0; x < VillageLayout.Width; x++)
                {
                    char cell = VillageLayout.At(x, y);
                    Color32 pixel;

                    switch (cell)
                    {
                        case VillageLayout.House: pixel = house; break;
                        case VillageLayout.Facade:
                        case VillageLayout.PipeFacade:
                        case VillageLayout.SignFacade: pixel = facade; break;
                        case VillageLayout.Hedge: pixel = hedge; break;
                        // Un arbre bloque : le plan doit le montrer, sinon il annonce un
                        // passage la ou il n'y en a pas. Un PANNEAU ne bloque pas, il retombe
                        // donc sur son sol par le cas general : le plan n'affiche que ce qui
                        // change un trajet.
                        case VillageLayout.Tree: pixel = tree; break;
                        case VillageLayout.PlantWall: pixel = plantWall; break;
                        default:
                            switch (VillageLayout.GroundAt(x, y))
                            {
                                case VillageLayout.Road: pixel = road; break;
                                case VillageLayout.Park: pixel = park; break;
                                case VillageLayout.PlantFloor: pixel = plantFloor; break;
                                default: pixel = grass; break;
                            }
                            break;
                    }

                    pixels[y * VillageLayout.Width + x] = pixel;
                }
            }

            return pixels;
        }

        /// <summary>Le cadre de la case regardee : quatre equerres, centre libre.</summary>
        /// <summary>
        /// Le cadre de choix : quatre equerres, jamais un rectangle plein, pour qu'il cerne
        /// sans rien cacher de ce qu'il designe.
        ///
        /// La taille est un parametre depuis la phase 14 : la cible du monde fait 16 px comme
        /// une case, le cadre du memory 32 comme une carte. Les branches valent le quart du
        /// cote dans les deux cas, donc l'image de 16 est au pixel pres celle d'avant.
        /// </summary>
        private static Color32[] BuildCursor(int size)
        {
            Color32 mark = Palette.Paper;
            int arm = size / 4;

            Color32[] pixels = NewTransparent(size * size);

            for (int i = 0; i < arm; i++)
            {
                int far = size - 1 - i;

                // Quatre coins, deux traits chacun.
                Fill(pixels, size, i, i, 0, 0, mark);
                Fill(pixels, size, 0, 0, i, i, mark);
                Fill(pixels, size, far, far, 0, 0, mark);
                Fill(pixels, size, size - 1, size - 1, i, i, mark);
                Fill(pixels, size, i, i, size - 1, size - 1, mark);
                Fill(pixels, size, 0, 0, far, far, mark);
                Fill(pixels, size, far, far, size - 1, size - 1, mark);
                Fill(pixels, size, size - 1, size - 1, far, far, mark);
            }

            return pixels;
        }

        /// <summary>
        /// Le dos d'un panneau : une plaque grise, son lisere sombre, et la bride verticale
        /// qui la tient au poteau avec ses deux boulons.
        ///
        /// Meme poteau et meme emprise de plaque que les vingt-neuf faces — poteau en x 7 a 8
        /// de y 0 a 13, plaque centree sur (8, 18) — pour qu'une carte retournee ne saute pas
        /// d'un pixel a l'endroit.
        /// </summary>
        /// <summary>
        /// Le poteau vide : le poteau seul, et un pointille la ou la plaque devrait etre. Meme
        /// emprise que les plaques, pour que le panneau pose tombe exactement dessus.
        /// </summary>
        private static Color32[] BuildSignPost()
        {
            const int width = PlayerWidth;
            const int height = PlayerHeight;

            Color32 post = Palette.Steel;
            Color32 dots = Palette.SteelLight;

            Color32[] pixels = NewTransparent(width * height);

            // Le pointille : un pixel sur deux, sur le bord de l'emprise de la plaque.
            for (int i = 2; i <= 13; i += 2)
            {
                Fill(pixels, width, i, i, 12, 12, dots);
                Fill(pixels, width, i, i, 23, 23, dots);
            }

            for (int j = 14; j <= 21; j += 2)
            {
                Fill(pixels, width, 2, 2, j, j, dots);
                Fill(pixels, width, 13, 13, j, j, dots);
            }

            // Le poteau EN DERNIER : un point du pointille lui mordait un pixel en (8, 12).
            Fill(pixels, width, 7, 8, 0, 13, post);

            return pixels;
        }

        private static Color32[] BuildSignBack()
        {
            const int width = PlayerWidth;
            const int height = PlayerHeight;

            Color32 post = Palette.Steel;
            Color32 plate = Palette.SteelLight;
            Color32 edge = Palette.SteelDark;
            Color32 clamp = Palette.Steel;

            Color32[] pixels = NewTransparent(width * height);

            Fill(pixels, width, 7, 8, 0, 13, post);

            // La plaque, puis son lisere : douze sur douze, l'emprise commune des faces.
            Fill(pixels, width, 2, 13, 12, 23, plate);
            Fill(pixels, width, 2, 13, 12, 12, edge);
            Fill(pixels, width, 2, 13, 23, 23, edge);
            Fill(pixels, width, 2, 2, 12, 23, edge);
            Fill(pixels, width, 13, 13, 12, 23, edge);

            // La bride et ses deux boulons : ce qui rend un dos reconnaissable comme un dos.
            Fill(pixels, width, 7, 8, 14, 21, clamp);
            Fill(pixels, width, 6, 9, 15, 15, edge);
            Fill(pixels, width, 6, 9, 20, 20, edge);

            return pixels;
        }

        private static Color32[] NewTransparent(int length)
        {
            Color32 clear = new Color32(0, 0, 0, 0);
            Color32[] pixels = new Color32[length];

            for (int i = 0; i < pixels.Length; i++)
            {
                pixels[i] = clear;
            }

            return pixels;
        }

        private static void Fill(Color32[] pixels, int width, int x0, int x1, int y0, int y1,
            Color32 color)
        {
            for (int y = y0; y <= y1; y++)
            {
                for (int x = x0; x <= x1; x++)
                {
                    pixels[y * width + x] = color;
                }
            }
        }

        // ---------------------------------------------------------------- disque

        private static void WriteTexture(string assetPath, Color32[] pixels, int width = TileSize)
        {
            int height = pixels.Length / width;

            Texture2D texture = new Texture2D(width, height, TextureFormat.RGBA32, false);
            texture.SetPixels32(pixels);
            texture.Apply();

            File.WriteAllBytes(assetPath, texture.EncodeToPNG());
            Object.DestroyImmediate(texture);
        }

        /// <summary>
        /// Le plus petit plafond de texture d'Unity qui contienne cette image. Ajoute en
        /// phase 12a : le plafond etait ecrit a la main, et une image plus large que lui etait
        /// divisee par deux EN SILENCE. La phase 7 l'a evite de justesse sur le nom AMSTERDAM,
        /// la phase 9a sur les phrases de l'ouvrier. Le calculer supprime le piege au lieu de
        /// le surveiller.
        /// </summary>
        private static int TextureCapFor(int width, int height)
        {
            int cap = 32;
            int longest = Mathf.Max(width, height);

            while (cap < longest)
            {
                cap *= 2;
            }

            return cap;
        }

        /// <summary>
        /// Le plafond qu'il faut pour le PNG pose sur le disque, lu dans son en-tete : les
        /// octets 16 a 23 portent la largeur puis la hauteur, en gros-boutiste. On lit le
        /// fichier plutot que d'interroger l'importeur, dont l'API de taille source n'est pas
        /// publique et a change d'une version d'Unity a l'autre.
        /// </summary>
        private static int TextureCapForFile(string assetPath)
        {
            try
            {
                byte[] header = new byte[24];
                using (FileStream stream = File.OpenRead(assetPath))
                {
                    if (stream.Read(header, 0, header.Length) < header.Length)
                    {
                        return 64;
                    }
                }

                int width = (header[16] << 24) | (header[17] << 16) | (header[18] << 8) | header[19];
                int height = (header[20] << 24) | (header[21] << 16) | (header[22] << 8) | header[23];

                return TextureCapFor(width, height);
            }
            catch (IOException error)
            {
                Debug.LogError($"[Sous la Ville] Taille illisible pour {assetPath} : {error.Message}");
                return 64;
            }
        }

        /// <summary>
        /// Ecrit l'image d'un mot dessine par PixelFont, avec le plafond de texture qu'il faut
        /// et un refus bruyant si un caractere ne sait pas se dessiner.
        /// </summary>
        private static void WriteWord(string assetPath, string word)
        {
            char missing;
            if (!PixelFont.CanRender(word, out missing))
            {
                Debug.LogError($"[Sous la Ville] PixelFont ne sait pas dessiner « {missing} », " +
                               $"dans « {word} ». Le mot sortirait troué, sans un mot. " +
                               $"Fichier : {assetPath}");
                return;
            }

            WriteTexture(assetPath, BuildSentence(word), PixelFont.WidthOf(word));
        }

        private static void ConfigureImporter(string assetPath, Vector2? customPivot,
            int maxSize = 0)
        {
            AssetDatabase.ImportAsset(assetPath, ImportAssetOptions.ForceUpdate);

            TextureImporter importer = AssetImporter.GetAtPath(assetPath) as TextureImporter;
            if (importer == null)
            {
                Debug.LogError($"[Sous la Ville] Importeur de texture introuvable : {assetPath}");
                return;
            }

            importer.textureType = TextureImporterType.Sprite;
            importer.spriteImportMode = SpriteImportMode.Single;
            importer.spritePixelsPerUnit = PixelsPerUnit;
            importer.filterMode = FilterMode.Point;
            importer.textureCompression = TextureImporterCompression.Uncompressed;
            importer.mipmapEnabled = false;
            importer.wrapMode = TextureWrapMode.Clamp;
            importer.alphaIsTransparency = true;
            importer.npotScale = TextureImporterNPOTScale.None;
            // Le plan du village fait 40 px de large et le nom AMSTERDAM 55 : un plafond a
            // 32 les reduirait en silence et detruirait la police. Ce plafond ne fait que
            // tronquer, il n'agrandit rien : aucune image existante ne change.
            //
            // Depuis la phase 12a, un plafond de zero veut dire « calcule-le » : on prend le
            // plus petit palier qui contienne l'image. Plus aucune texture ne peut etre reduite
            // de moitie en silence, quelle que soit la longueur d'une phrase.
            importer.maxTextureSize = maxSize > 0 ? maxSize : TextureCapForFile(assetPath);

            TextureImporterSettings settings = new TextureImporterSettings();
            importer.ReadTextureSettings(settings);

            // FullRect : un maillage serre rognerait les pixels transparents et casserait
            // l'alignement des tuiles sur la grille.
            settings.spriteMeshType = SpriteMeshType.FullRect;
            settings.spriteExtrude = 0;

            if (customPivot.HasValue)
            {
                settings.spriteAlignment = (int)SpriteAlignment.Custom;
                settings.spritePivot = customPivot.Value;
            }
            else
            {
                settings.spriteAlignment = (int)SpriteAlignment.Center;
            }

            importer.SetTextureSettings(settings);
            importer.SaveAndReimport();
        }

        private static void CreateTileAsset(string tileAssetPath, string texturePath)
        {
            Sprite sprite = AssetDatabase.LoadAssetAtPath<Sprite>(texturePath);
            if (sprite == null)
            {
                Debug.LogError($"[Sous la Ville] Sprite introuvable : {texturePath}");
                return;
            }

            Tile tile = AssetDatabase.LoadAssetAtPath<Tile>(tileAssetPath);
            bool isNew = tile == null;
            if (isNew)
            {
                tile = ScriptableObject.CreateInstance<Tile>();
            }

            tile.sprite = sprite;
            tile.color = Color.white;

            // Les collisions sont logiques, pas physiques : aucune tuile ne porte de collider.
            tile.colliderType = Tile.ColliderType.None;

            if (isNew)
            {
                AssetDatabase.CreateAsset(tile, tileAssetPath);
            }
            else
            {
                EditorUtility.SetDirty(tile);
            }
        }

        private static void EnsureFolder(string folder)
        {
            if (AssetDatabase.IsValidFolder(folder))
            {
                return;
            }

            string parent = Path.GetDirectoryName(folder).Replace('\\', '/');
            string leaf = Path.GetFileName(folder);

            EnsureFolder(parent);
            AssetDatabase.CreateFolder(parent, leaf);
        }
    }
}
