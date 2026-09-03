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
                WriteTexture(CursorTarget, BuildCursor());
                WriteTexture(PictoDropFull, BuildDrop(full: true));
                WriteTexture(PictoDropEmpty, BuildDrop(full: false));
                WriteTexture(PictoRepair, BuildRepairPicto());

                WriteTexture(PictoSpring, BuildSpringPicto(), PictoSize);
                WriteTexture(PictoSummer, BuildSummerPicto(), PictoSize);
                WriteTexture(PictoAutumn, BuildAutumnPicto(), PictoSize);
                WriteTexture(PictoWinter, BuildWinterPicto(), PictoSize);

                WriteTileTexture("tile_workshop", new Color32(0x8E, 0x87, 0x78, 0xFF));
                WriteTexture($"{TilesFolder}/tile_water.png", BuildWater());
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
                    WriteTexture(CraftsmanLineTexture(index),
                        BuildSentence(CraftsmanLines[index]),
                        PixelFont.WidthOf(CraftsmanLines[index]));
                }

                for (int index = 0; index < WorkerLines.Length; index++)
                {
                    WriteTexture(WorkerLineTexture(index),
                        BuildSentence(WorkerLines[index]),
                        PixelFont.WidthOf(WorkerLines[index]));
                }

                for (int index = 0; index < PipeNames.Length; index++)
                {
                    WriteTexture(PipeNameTexture(index),
                        BuildSentence(PipeNames[index]),
                        PixelFont.WidthOf(PipeNames[index]));
                }

                for (int index = 0; index < CoverCount; index++)
                {
                    WriteTexture(CoverTexture(index), BuildCover(index));
                    WriteTexture(CoverNameTexture(index), BuildCoverName(index),
                        PixelFont.WidthOf(CoverNames[index]));
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
                ConfigureImporter(CraftsmanLineTexture(index), null, maxSize: 256);
            }

            for (int index = 0; index < WorkerLines.Length; index++)
            {
                ConfigureImporter(WorkerLineTexture(index), null, maxSize: 256);
            }

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
                         PictoDropEmpty, PictoRepair, PictoSpring, PictoSummer, PictoAutumn,
                         PictoWinter })
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
            Debug.Log("[Sous la Ville] Art placeholder généré : 122 textures, 65 tuiles.");
        }

        /// <summary>Vrai si toutes les tuiles et tous les sprites attendus sont sur le disque.</summary>
        public static bool AreAssetsPresent()
        {
            string[] tiles =
            {
                TileGrass, TilePath, TilePark, TilePlantFloor, TileHedge, TilePlantWall, TileHouse,
                TileWorkshop, TileFacade, TileWall, TileWater
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

            string[] sprites =
            {
                ManholeTexture, LadderTexture, HouseTexture, HouseInletTexture, PlayerDown,
                PlayerUp, PlayerLeft, PlayerRight, PictoSurface, PictoUnderground, PictoDown,
                PictoUp, PictoDig, PictoRemove, CursorTarget, PictoDropFull,
                PictoDropEmpty, PictoRepair, PictoSpring, PictoSummer, PictoAutumn, PictoWinter,
                DoorTexture, VillagerCraftsman, VillagerWorker, PictoEnter, PictoExit, PictoTalk
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

        /// <summary>Le nom d'une ville, en blanc cerne de sombre pour tenir sur le pave.</summary>
        private static Color32[] BuildCoverName(int index)
        {
            return PixelFont.Render(CoverNames[index],
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
                        case VillageLayout.PipeFacade: pixel = facade; break;
                        case VillageLayout.Hedge: pixel = hedge; break;
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
        private static Color32[] BuildCursor()
        {
            Color32 mark = new Color32(0xFF, 0xF4, 0xC2, 0xFF);
            const int arm = 4;

            Color32[] pixels = NewTransparent(TileSize * TileSize);

            for (int i = 0; i < arm; i++)
            {
                int far = TileSize - 1 - i;

                // Quatre coins, deux traits chacun.
                Fill(pixels, TileSize, i, i, 0, 0, mark);
                Fill(pixels, TileSize, 0, 0, i, i, mark);
                Fill(pixels, TileSize, far, far, 0, 0, mark);
                Fill(pixels, TileSize, TileSize - 1, TileSize - 1, i, i, mark);
                Fill(pixels, TileSize, i, i, TileSize - 1, TileSize - 1, mark);
                Fill(pixels, TileSize, 0, 0, far, far, mark);
                Fill(pixels, TileSize, far, far, TileSize - 1, TileSize - 1, mark);
                Fill(pixels, TileSize, TileSize - 1, TileSize - 1, far, far, mark);
            }

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

        private static void ConfigureImporter(string assetPath, Vector2? customPivot,
            int maxSize = 64)
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
            // Les phrases des personnages font environ 170 px et demandent 256, d'ou le
            // parametre. Tout le reste garde 64.
            importer.maxTextureSize = maxSize;

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
