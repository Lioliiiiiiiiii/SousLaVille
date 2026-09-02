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
        public const string PictoPipe = PictosFolder + "/picto_pipe.png";
        public const string PictoRemove = PictosFolder + "/picto_remove.png";
        public const string CursorTarget = PictosFolder + "/cursor_target.png";
        public const string PictoDropFull = PictosFolder + "/picto_drop_full.png";
        public const string PictoDropEmpty = PictosFolder + "/picto_drop_empty.png";

        /// <summary>Nombre de nuances de profondeur : 1 peu profond, 3 profond.</summary>
        public const int DepthCount = 3;

        /// <summary>Nombre d'images de canalisation, une par masque de raccords.</summary>
        public const int PipeMaskCount = 16;

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

        // Fichiers de la phase 2 remplaces en phase 3.
        private static readonly string[] ObsoleteAssets =
        {
            TilesFolder + "/tile_earth.png",
            TilesFolder + "/tile_tunnel.png",
            TilesFolder + "/Tile_Earth.asset",
            TilesFolder + "/Tile_Tunnel.asset",
            SpritesFolder + "/player.png"
        };

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

        /// <summary>Asset Tile de canalisation pour un masque de raccords, 0 a 15.</summary>
        public static string TilePipe(int mask)
        {
            return $"{TilesFolder}/Tile_Pipe_{Mathf.Clamp(mask, 0, PipeMaskCount - 1):00}.asset";
        }

        private static string TunnelTexture(int depth)
        {
            return $"{TilesFolder}/tile_tunnel_{depth}.png";
        }

        private static string EarthTexture(int depth)
        {
            return $"{TilesFolder}/tile_earth_{depth}.png";
        }

        private static string PipeTexture(int mask)
        {
            return $"{TilesFolder}/pipe_{mask:00}.png";
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

                for (int mask = 0; mask < PipeMaskCount; mask++)
                {
                    WriteTexture(PipeTexture(mask), BuildPipe(mask));
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
                WriteTexture(PictoPipe, BuildPipePicto());
                WriteTexture(PictoRemove, BuildRemovePicto());
                WriteTexture(CursorTarget, BuildCursor());
                WriteTexture(PictoDropFull, BuildDrop(full: true));
                WriteTexture(PictoDropEmpty, BuildDrop(full: false));
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

            for (int mask = 0; mask < PipeMaskCount; mask++)
            {
                ConfigureImporter(PipeTexture(mask), null);
            }

            ConfigureImporter(ManholeTexture, null);
            ConfigureImporter(LadderTexture, null);
            ConfigureImporter(HouseInletTexture, null);

            // Meme pivot que le personnage : la maison se pose sur sa case et son toit deborde.
            ConfigureImporter(HouseTexture, PlayerPivot);

            foreach (string path in new[] { PlayerDown, PlayerUp, PlayerLeft, PlayerRight })
            {
                ConfigureImporter(path, PlayerPivot);
            }

            foreach (string path in new[] { PictoSurface, PictoUnderground, PictoDown, PictoUp,
                         PictoDig, PictoPipe, PictoRemove, CursorTarget, PictoDropFull,
                         PictoDropEmpty })
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

            for (int depth = 1; depth <= DepthCount; depth++)
            {
                CreateTileAsset(TileEarth(depth), EarthTexture(depth));
                CreateTileAsset(TileTunnel(depth), TunnelTexture(depth));
            }

            for (int mask = 0; mask < PipeMaskCount; mask++)
            {
                CreateTileAsset(TilePipe(mask), PipeTexture(mask));
            }

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            Debug.Log("[Sous la Ville] Art placeholder généré : 42 textures, 29 tuiles.");
        }

        /// <summary>Vrai si toutes les tuiles et tous les sprites attendus sont sur le disque.</summary>
        public static bool AreAssetsPresent()
        {
            string[] tiles =
            {
                TileGrass, TilePath, TilePark, TilePlantFloor, TileHedge, TilePlantWall, TileHouse
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

            for (int mask = 0; mask < PipeMaskCount; mask++)
            {
                if (AssetDatabase.LoadAssetAtPath<Tile>(TilePipe(mask)) == null)
                {
                    return false;
                }
            }

            string[] sprites =
            {
                ManholeTexture, LadderTexture, HouseTexture, HouseInletTexture, PlayerDown,
                PlayerUp, PlayerLeft, PlayerRight, PictoSurface, PictoUnderground, PictoDown,
                PictoUp, PictoDig, PictoPipe, PictoRemove, CursorTarget, PictoDropFull,
                PictoDropEmpty
            };

            foreach (string path in sprites)
            {
                if (AssetDatabase.LoadAssetAtPath<Sprite>(path) == null)
                {
                    return false;
                }
            }

            return true;
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
        private static Color32[] BuildPipe(int mask)
        {
            Color32 body = new Color32(0x9F, 0xB3, 0xC2, 0xFF);
            Color32 outline = new Color32(0x46, 0x58, 0x6A, 0xFF);

            Color32[] pixels = NewTransparent(TileSize * TileSize);

            DrawPipe(pixels, mask, grow: 1, color: outline);
            DrawPipe(pixels, mask, grow: 0, color: body);

            return pixels;
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

        /// <summary>« Ici on pose » : un tronçon de canalisation avec ses deux collerettes.</summary>
        private static Color32[] BuildPipePicto()
        {
            Color32 body = new Color32(0x9F, 0xB3, 0xC2, 0xFF);
            Color32 outline = new Color32(0x2B, 0x1B, 0x14, 0xFF);

            Color32[] pixels = NewTransparent(TileSize * TileSize);

            Fill(pixels, TileSize, 1, 14, 4, 11, outline);
            Fill(pixels, TileSize, 2, 13, 6, 9, body);
            Fill(pixels, TileSize, 2, 3, 5, 10, body);
            Fill(pixels, TileSize, 12, 13, 5, 10, body);

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

        private static void ConfigureImporter(string assetPath, Vector2? customPivot)
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
            importer.maxTextureSize = 32;

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
