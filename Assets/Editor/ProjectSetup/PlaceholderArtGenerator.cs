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
    public static class PlaceholderArtGenerator
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
        /// Les couleurs des trois personnages de l'usine a panneaux. Franches et distinctes
        /// du vert de l'artisan et du bleu de l'ouvrier : on les reconnait de loin, sans un
        /// mot, ce qui est la regle du projet depuis la phase 0.
        /// </summary>
        private static readonly Color32[] SignFactoryColors =
        {
            new Color32(0xD0, 0x7A, 0x2E, 0xFF),   // Le Stock, orange
            new Color32(0x7A, 0x4E, 0xA8, 0xFF),   // La Fabrique, violet
            new Color32(0xC0, 0xA8, 0x30, 0xFF)    // Le Plan, ocre
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
            new Color32(0x6B, 0x4F, 0x38, 0xFF),
            new Color32(0x55, 0x40, 0x2D, 0xFF),
            new Color32(0x3E, 0x32, 0x26, 0xFF)
        };

        private static readonly Color32[] TunnelColors =
        {
            new Color32(0xC2, 0xB3, 0x93, 0xFF),
            new Color32(0x9C, 0x91, 0x79, 0xFF),
            new Color32(0x77, 0x80, 0x8A, 0xFF)
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

        private static string TunnelTexture(int depth)
        {
            return $"{TilesFolder}/tile_tunnel_{depth}.png";
        }

        private static string EarthTexture(int depth)
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
                WriteTileTexture("tile_grass", new Color32(0x4E, 0x9A, 0x3E, 0xFF));
                WriteTileTexture("tile_path", new Color32(0xC8, 0xA9, 0x6E, 0xFF));
                WriteTileTexture("tile_park", new Color32(0xB8, 0xB8, 0xB0, 0xFF));
                WriteTileTexture("tile_plant_floor", new Color32(0x6E, 0x7B, 0x8B, 0xFF));
                WriteTileTexture("tile_hedge", new Color32(0x1F, 0x5C, 0x2E, 0xFF));
                WriteTileTexture("tile_plant_wall", new Color32(0x3A, 0x6E, 0xA5, 0xFF));
                WriteTileTexture("tile_house", new Color32(0xA0, 0x44, 0x2B, 0xFF));

                for (int depth = 1; depth <= DepthCount; depth++)
                {
                    WriteTexture(EarthTexture(depth), BuildTile(EarthColors[depth - 1]));
                    WriteTexture(TunnelTexture(depth), BuildTile(TunnelColors[depth - 1]));
                }

                for (int pattern = 0; pattern < PipePatternCount; pattern++)
                {
                    for (int mask = 0; mask < PipeMaskCount; mask++)
                    {
                        WriteTexture(PipeTexture(pattern, mask), BuildPipe(pattern, mask));
                    }
                }

                WriteTexture(ManholeTexture, BuildManhole());
                WriteTexture(LadderTexture, BuildLadder());
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

                WriteTileTexture("tile_workshop", new Color32(0x8E, 0x87, 0x78, 0xFF));
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

                // Le nom de chaque panneau de la planche, phase 13. Nomme par le RANG : la
                // place sur la planche peut changer, le rang non.
                for (int slot = 0; slot < SignBoard.Length; slot++)
                {
                    WriteWord(SignNameTexture(SignBoard[slot]), SignBoardNames[slot]);
                }
                WriteTileTexture("tile_facade", new Color32(0xB0, 0x7A, 0x3C, 0xFF));
                WriteTileTexture("tile_wall", new Color32(0x6A, 0x5B, 0x49, 0xFF));

                WriteTexture(DoorTexture, BuildDoor());
                WriteTexture(VillagerCraftsman,
                    BuildVillager(new Color32(0x3E, 0x8E, 0x7A, 0xFF)), PlayerWidth);
                WriteTexture(VillagerWorker,
                    BuildVillager(new Color32(0x2E, 0x5F, 0xA8, 0xFF)), PlayerWidth);
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
                        BuildVillager(SignFactoryColors[who]), PlayerWidth);

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

            ConfigureImporter(SignBackTexture, PlayerPivot);
            ConfigureImporter(PictoCardCursor, null);

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
            ConfigureImporter(LadderTexture, null);
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
                    || AssetDatabase.LoadAssetAtPath<Tile>(TileRoadMasked(mask)) == null)
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
                DoorTexture, VillagerCraftsman, VillagerWorker, PictoEnter, PictoExit, PictoTalk,
                FountainSprite, TreeTexture, PictoGrow, PlantBasinTexture, GuideAttention,
                SignBackTexture, PictoCardCursor
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

        /// <summary>Carre plein borde d'un lisere 1 px assombri.</summary>
        private static Color32[] BuildTile(Color32 fill)
        {
            Color32 border = Darken(fill, 0.72f);
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
            Color32 body = new Color32(0x9F, 0xB3, 0xC2, 0xFF);
            Color32 outline = new Color32(0x46, 0x58, 0x6A, 0xFF);
            Color32 motif = Darken(body, 0.68f);

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
            Color32 cover = new Color32(0x3C, 0x3C, 0x3C, 0xFF);
            Color32 rim = new Color32(0x8A, 0x8A, 0x8A, 0xFF);
            Color32 slot = new Color32(0x1E, 0x1E, 0x1E, 0xFF);
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

            Color32 body = new Color32(0xE0, 0x5A, 0x2B, 0xFF);
            Color32 head = new Color32(0xF2, 0xA0, 0x7B, 0xFF);
            Color32 legs = Darken(body, 0.65f);
            Color32 hair = new Color32(0x4A, 0x2E, 0x1E, 0xFF);
            Color32 eye = new Color32(0x2B, 0x1B, 0x14, 0xFF);

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

            Color32 wall = new Color32(0xD9, 0xC7, 0xA0, 0xFF);
            Color32 roof = new Color32(0xA0, 0x44, 0x2B, 0xFF);
            Color32 door = new Color32(0x5A, 0x3A, 0x22, 0xFF);
            Color32 window = new Color32(0x6E, 0x9E, 0xC4, 0xFF);

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
            Color32 flange = new Color32(0xD9, 0xC7, 0xA0, 0xFF);
            Color32 mouth = new Color32(0x5A, 0x3A, 0x22, 0xFF);

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
            Color32 outline = new Color32(0x2E, 0x36, 0x3E, 0xFF);
            Color32 rim = new Color32(0x7A, 0x88, 0x96, 0xFF);
            Color32 inside = new Color32(0x1C, 0x21, 0x28, 0xFF);
            Color32 water = new Color32(0x4F, 0xA9, 0xEF, 0xFF);
            Color32 waterTop = new Color32(0xA8, 0xDA, 0xFB, 0xFF);
            Color32 tick = new Color32(0xB0, 0xB8, 0xC0, 0xFF);

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
                ? new Color32(0x4F, 0xA9, 0xEF, 0xFF)
                : new Color32(0x9A, 0xA0, 0xA6, 0xFF);
            Color32 outline = new Color32(0x2B, 0x1B, 0x14, 0xFF);

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
        private static Color32[] BuildLadder()
        {
            Color32 rail = new Color32(0xC9, 0xA2, 0x27, 0xFF);
            Color32 rung = Darken(rail, 0.6f);

            Color32[] pixels = NewTransparent(TileSize * TileSize);

            Fill(pixels, TileSize, 3, 4, 1, 14, rail);
            Fill(pixels, TileSize, 11, 12, 1, 14, rail);

            foreach (int y in new[] { 3, 7, 11 })
            {
                Fill(pixels, TileSize, 5, 10, y, y + 1, rung);
            }

            return pixels;
        }

        /// <summary>Repere « je suis en haut » : un soleil et ses quatre rayons sur fond de ciel.</summary>
        private static Color32[] BuildSunPicto()
        {
            Color32 sky = new Color32(0x2A, 0x4C, 0x7D, 0xFF);
            Color32 sun = new Color32(0xF2, 0xC1, 0x4E, 0xFF);

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
            Color32 earth = new Color32(0x3A, 0x2C, 0x20, 0xFF);
            Color32 rail = new Color32(0xC9, 0xA2, 0x27, 0xFF);
            Color32 rung = Darken(rail, 0.6f);

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
            Color32 fill = new Color32(0xFF, 0xFF, 0xFF, 0xFF);
            Color32 outline = new Color32(0x2B, 0x1B, 0x14, 0xFF);

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
            Color32 handle = new Color32(0x9A, 0x6E, 0x3A, 0xFF);
            Color32 blade = new Color32(0xC8, 0xCE, 0xD4, 0xFF);
            Color32 outline = new Color32(0x2B, 0x1B, 0x14, 0xFF);

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
        private static Color32[] BuildRemovePicto()
        {
            Color32 disc = new Color32(0xFF, 0xFF, 0xFF, 0xFF);
            Color32 bar = new Color32(0x2B, 0x1B, 0x14, 0xFF);

            Color32[] pixels = NewTransparent(TileSize * TileSize);
            const float center = (TileSize - 1) * 0.5f;

            for (int y = 0; y < TileSize; y++)
            {
                for (int x = 0; x < TileSize; x++)
                {
                    float dx = x - center;
                    float dy = y - center;
                    float distance = Mathf.Sqrt(dx * dx + dy * dy);

                    if (distance <= 7f)
                    {
                        pixels[y * TileSize + x] = distance > 5.5f ? bar : disc;
                    }
                }
            }

            Fill(pixels, TileSize, 4, 11, 7, 8, bar);

            return pixels;
        }

        /// <summary>
        /// « Ici on repare » : une cle plate, machoire ouverte vers le haut. Le vocabulaire
        /// de l'atelier plutot que celui de l'interdiction.
        /// </summary>
        private static Color32[] BuildRepairPicto()
        {
            Color32 metal = new Color32(0xC8, 0xCE, 0xD4, 0xFF);
            Color32 outline = new Color32(0x2B, 0x1B, 0x14, 0xFF);
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
            Color32 sky = new Color32(0xC8, 0xE6, 0xA0, 0xFF);
            Color32 soil = new Color32(0x7A, 0x55, 0x33, 0xFF);
            Color32 plant = new Color32(0x2E, 0x7D, 0x32, 0xFF);

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
            Color32 sky = new Color32(0xF7, 0xDE, 0x8B, 0xFF);
            Color32 sun = new Color32(0xE8, 0x87, 0x1E, 0xFF);

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
            Color32 sky = new Color32(0xE8, 0xA4, 0x5C, 0xFF);
            Color32 leaf = new Color32(0x8C, 0x3A, 0x17, 0xFF);
            Color32 vein = new Color32(0xC9, 0x6B, 0x2E, 0xFF);

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
            Color32 sky = new Color32(0x8F, 0xB4, 0xD9, 0xFF);
            Color32 flake = new Color32(0xFF, 0xFF, 0xFF, 0xFF);

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
            Color32 rim = new Color32(0x3A, 0x3F, 0x44, 0xFF);
            Color32 body = new Color32(0x8A, 0x91, 0x98, 0xFF);
            Color32 groove = new Color32(0x51, 0x58, 0x5E, 0xFF);
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
            Color32 water = new Color32(0x3A, 0x7C, 0xC8, 0xB4);
            Color32 ripple = new Color32(0x9C, 0xD4, 0xF0, 0xC8);

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
            Color32 red = new Color32(0xC8, 0x2F, 0x2F, 0xFF);
            Color32 white = new Color32(0xF2, 0xF0, 0xEA, 0xFF);

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
            Color32 tank = new Color32(0x6E, 0x7B, 0x8B, 0xFF);
            Color32 water = new Color32(0x3A, 0x7C, 0xC8, 0xFF);
            Color32 plus = new Color32(0xF2, 0xF0, 0xEA, 0xFF);

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
            Color32 wall = new Color32(0x8C, 0x99, 0xA8, 0xFF);
            Color32 water = new Color32(0x3A, 0x7C, 0xC8, 0xFF);
            Color32 glint = new Color32(0x7C, 0xB8, 0xE8, 0xFF);

            Color32[] pixels = NewTransparent(TileSize * TileSize);

            Fill(pixels, TileSize, 1, 14, 1, 14, wall);
            Fill(pixels, TileSize, 3, 12, 3, 12, water);
            Fill(pixels, TileSize, 4, 7, 9, 10, glint);

            return pixels;
        }

        private static Color32[] BuildHedge(int mask)
        {
            Color32 leaf = new Color32(0x1F, 0x5C, 0x2E, 0xFF);
            Color32 crown = new Color32(0x2F, 0x7A, 0x3E, 0xFF);
            Color32 shade = Darken(leaf, 0.62f);

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
            Color32 verge = new Color32(0xC8, 0xA9, 0x6E, 0xFF);
            Color32 asphalt = new Color32(0xA6, 0x8B, 0x59, 0xFF);
            Color32 paint = new Color32(0xE2, 0xD4, 0xB0, 0xFF);

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

            Color32 trunk = new Color32(0x6B, 0x4A, 0x2E, 0xFF);
            Color32 bark = Darken(trunk, 0.72f);
            Color32 leaf = new Color32(0x2C, 0x6E, 0x35, 0xFF);
            Color32 light = new Color32(0x46, 0x92, 0x48, 0xFF);

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
            Color32 trunk = new Color32(0x6B, 0x4A, 0x2E, 0xFF);
            Color32 root = Darken(trunk, 0.7f);

            Color32[] pixels = BuildTile(new Color32(0x3A, 0x5E, 0x2C, 0xFF));

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

            Color32 post = new Color32(0x9A, 0x9A, 0x9A, 0xFF);
            Color32 red = new Color32(0xC8, 0x2F, 0x2F, 0xFF);
            Color32 blue = new Color32(0x2E, 0x5F, 0xA8, 0xFF);
            Color32 white = new Color32(0xF2, 0xF0, 0xEA, 0xFF);
            Color32 yellow = new Color32(0xF2, 0xC8, 0x2A, 0xFF);
            Color32 black = new Color32(0x1A, 0x1A, 0x1A, 0xFF);

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

                    Fill(pixels, width, cx - 3, cx + 2, cy - 1, cy, white);
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

            Color32 stone = new Color32(0x8C, 0x86, 0x7A, 0xFF);
            Color32 rim = new Color32(0xB8, 0xB2, 0xA4, 0xFF);
            Color32 water = new Color32(0x3A, 0x7C, 0xC8, 0xFF);
            Color32 jet = new Color32(0x9C, 0xD4, 0xF0, 0xFF);

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
            Color32 rim = new Color32(0xB8, 0xB2, 0xA4, 0xFF);
            Color32 stone = new Color32(0x8C, 0x86, 0x7A, 0xFF);
            Color32 basin = new Color32(0x3A, 0x7C, 0xC8, 0xFF);
            Color32 jet = new Color32(0x9C, 0xD4, 0xF0, 0xFF);

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
            Color32 frame = new Color32(0x6A, 0x4A, 0x2A, 0xFF);
            Color32 opening = new Color32(0x24, 0x1C, 0x18, 0xFF);
            Color32 handle = new Color32(0xE8, 0xC8, 0x60, 0xFF);

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
        private static Color32[] BuildVillager(Color32 body)
        {
            const int width = PlayerWidth;
            const int height = PlayerHeight;

            Color32 head = new Color32(0xE8, 0xC0, 0x96, 0xFF);
            Color32 legs = Darken(body, 0.65f);
            Color32 hair = new Color32(0x33, 0x33, 0x38, 0xFF);
            Color32 eye = new Color32(0x2B, 0x1B, 0x14, 0xFF);

            Color32[] pixels = NewTransparent(width * height);

            Fill(pixels, width, 4, 6, 0, 3, legs);
            Fill(pixels, width, 9, 11, 0, 3, legs);
            Fill(pixels, width, 3, 12, 4, 14, body);
            Fill(pixels, width, 3, 12, 15, 22, head);
            Fill(pixels, width, 4, 11, 23, 23, head);
            Fill(pixels, width, 3, 12, 21, 23, hair);
            Fill(pixels, width, 5, 6, 18, 19, eye);
            Fill(pixels, width, 9, 10, 18, 19, eye);

            return pixels;
        }

        /// <summary>
        /// « Ici on entre » et « ici on sort » : un chambranle, et une fleche qui va dedans ou
        /// qui en vient. Le vocabulaire des panneaux, que Victorien aime, plutot que deux
        /// fleches nues qui se confondraient avec descendre et remonter.
        /// </summary>
        private static Color32[] BuildDoorPicto(bool entering)
        {
            Color32 frame = new Color32(0xFF, 0xFF, 0xFF, 0xFF);
            Color32 opening = new Color32(0x2B, 0x1B, 0x14, 0xFF);
            Color32 arrow = new Color32(0xF2, 0xC0, 0x40, 0xFF);

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
            Color32 bubble = new Color32(0xFF, 0xFF, 0xFF, 0xFF);
            Color32 outline = new Color32(0x2B, 0x1B, 0x14, 0xFF);

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
                new Color32(0xFF, 0xFF, 0xFF, 0xFF),
                new Color32(0x2B, 0x1B, 0x14, 0xFF));
        }

        /// <summary>
        /// Le plan du village, une case par pixel, engendre depuis VillageLayout. Deux dessins
        /// d'un meme village finiraient par diverger ; celui-ci en sort, donc il ne peut pas
        /// mentir.
        /// </summary>
        private static Color32[] BuildVillageMap()
        {
            Color32 grass = new Color32(0x4E, 0x9A, 0x3E, 0xFF);
            Color32 road = new Color32(0xC8, 0xA9, 0x6E, 0xFF);
            Color32 park = new Color32(0xB8, 0xB8, 0xB0, 0xFF);
            Color32 plantFloor = new Color32(0x6E, 0x7B, 0x8B, 0xFF);
            Color32 hedge = new Color32(0x1F, 0x5C, 0x2E, 0xFF);
            Color32 plantWall = new Color32(0x3A, 0x6E, 0xA5, 0xFF);
            Color32 house = new Color32(0xA0, 0x44, 0x2B, 0xFF);
            Color32 facade = new Color32(0xB0, 0x7A, 0x3C, 0xFF);
            Color32 tree = new Color32(0x2C, 0x6E, 0x35, 0xFF);

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
            Color32 mark = new Color32(0xFF, 0xF4, 0xC2, 0xFF);
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
        private static Color32[] BuildSignBack()
        {
            const int width = PlayerWidth;
            const int height = PlayerHeight;

            Color32 post = new Color32(0x9A, 0x9A, 0x9A, 0xFF);
            Color32 plate = new Color32(0xB4, 0xB2, 0xAC, 0xFF);
            Color32 edge = new Color32(0x6E, 0x6C, 0x68, 0xFF);
            Color32 clamp = new Color32(0x88, 0x86, 0x82, 0xFF);

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

        private static Color32 Darken(Color32 color, float factor)
        {
            return new Color32(
                (byte)(color.r * factor),
                (byte)(color.g * factor),
                (byte)(color.b * factor),
                color.a);
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
