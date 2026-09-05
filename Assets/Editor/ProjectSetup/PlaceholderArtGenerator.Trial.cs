using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace SousLaVille.EditorTools
{
    /// <summary>
    /// LA PLANCHE D'ESSAI DE LA PHASE 18 : un bout de village compose avec les dessins de la
    /// feuille de style, ecrit dans Captures/ a quatre fois sa taille, SANS TOUCHER A UNE SEULE
    /// IMAGE DU JEU. C'est elle qu'on valide avant de redessiner les deux cent soixante-six autres :
    /// un style se juge sur un ecran, pas sur un texte, et la phase 17 l'a appris a ses depens —
    /// sept sous-phases validees sur leurs planches, et un rendu qui ne convenait pas.
    ///
    /// Onze cases sur huit, dessinees en espace texture, y montant. La composition suit le tri du
    /// jeu : les sols d'abord, puis ce qui se dresse, de la rangee du fond a celle du devant.
    /// </summary>
    public static partial class PlaceholderArtGenerator
    {
        private const int TrialColumns = 11;
        private const int TrialRows = 8;
        private const int TrialScale = 4;
        private const int TrialStripHeight = 24;

        [MenuItem("Sous La Ville/Planche d'essai 18")]
        public static void BuildStyleTrialSheet()
        {
            int width = TrialColumns * TileSize;
            int sceneHeight = TrialRows * TileSize;
            int height = sceneHeight + TrialStripHeight;
            Color32[] canvas = new Color32[width * height];

            for (int i = 0; i < canvas.Length; i++)
            {
                canvas[i] = Palette.Charcoal;
            }

            // RIEN SOUS LES DEUX BOITES DU HUD, qui couvrent les deux rangees du haut : la
            // premiere composition avait mis le bouquet d'arbres sous la boite de gauche, et la
            // frondaison — ce que la planche devait montrer d'abord — ne se voyait pas. Le piege
            // de la rangee du haut, dans une planche composee a la main.

            // Les chemins : une rue horizontale, une impasse qui monte, un acces qui descend.
            HashSet<Vector2Int> path = new HashSet<Vector2Int>();
            for (int x = 0; x < TrialColumns; x++) path.Add(new Vector2Int(x, 2));
            for (int y = 3; y <= 5; y++) path.Add(new Vector2Int(1, y));
            path.Add(new Vector2Int(8, 1));
            path.Add(new Vector2Int(8, 0));

            // Les arbres : un bouquet de trois sur deux, a droite, en pleine vue.
            List<Vector2Int> trees = new List<Vector2Int>();
            for (int x = 6; x <= 8; x++)
            {
                trees.Add(new Vector2Int(x, 4));
                trees.Add(new Vector2Int(x, 3));
            }

            // La maison : deux cases sur deux, la porte au sud sur la rue.
            Vector2Int house = new Vector2Int(3, 3);

            // La haie : une ligne de buissons contre le bord droit.
            HashSet<Vector2Int> bushes = new HashSet<Vector2Int>();
            for (int y = 3; y <= 7; y++) bushes.Add(new Vector2Int(10, y));

            Vector2Int water0 = new Vector2Int(5, 0);
            Vector2Int water1 = new Vector2Int(6, 0);

            // ---- les sols
            Color32[] lawn = BuildLawnTile();
            Color32[] treeBase = BuildTreeBaseV2();
            Color32[] flowers = BuildFlowers();
            Color32[] water = BuildWaterV2();

            for (int y = 0; y < TrialRows; y++)
            {
                for (int x = 0; x < TrialColumns; x++)
                {
                    Vector2Int cell = new Vector2Int(x, y);
                    Color32[] tile = lawn;

                    if (path.Contains(cell))
                    {
                        tile = BuildSandTile(NeighbourMask(path, cell));
                    }
                    else if (trees.Contains(cell))
                    {
                        tile = treeBase;
                    }

                    BlitTile(canvas, width, tile, TileSize, TileSize, x, y, TrialStripHeight);
                }
            }

            BlitTile(canvas, width, water, TileSize, TileSize, water0.x, water0.y, TrialStripHeight);
            BlitTile(canvas, width, water, TileSize, TileSize, water1.x, water1.y, TrialStripHeight);
            BlitTile(canvas, width, flowers, TileSize, TileSize, 0, 4, TrialStripHeight);
            BlitTile(canvas, width, flowers, TileSize, TileSize, 4, 6, TrialStripHeight);

            foreach (Vector2Int cell in bushes)
            {
                BlitTile(canvas, width, BuildBushTile(NeighbourMask(bushes, cell)), TileSize, TileSize,
                    cell.x, cell.y, TrialStripHeight);
            }

            // ---- ce qui se dresse, du fond vers le devant
            Color32[] tree = BuildTreeV2();
            foreach (int row in new[] { 4, 3 })
            {
                foreach (Vector2Int cell in trees)
                {
                    if (cell.y == row)
                    {
                        BlitSprite(canvas, width, tree, PlayerWidth, TreeHeight,
                            cell.x * TileSize, cell.y * TileSize + TrialStripHeight);
                    }
                }
            }

            BlitSprite(canvas, width, BuildHouseV2(), HouseWidth, HouseHeight,
                house.x * TileSize, house.y * TileSize + TrialStripHeight);

            // Un habitant sur l'herbe, et un panneau sur son poteau neuf.
            Color32[] villager = NewTransparent(PlayerWidth * PlayerHeight);
            DrawFigure(villager, Palette.Teal, Palette.Charcoal, Palette.Charcoal, Palette.SteelDark,
                Vector2Int.down);
            Fill(villager, PlayerWidth, 1, 8, 18, 18, Palette.Charcoal);   // la visiere de la casquette
            Outline(villager, PlayerWidth, Palette.Ink);
            BlitSprite(canvas, width, villager, PlayerWidth, PlayerHeight,
                2 * TileSize, 4 * TileSize + TrialStripHeight);

            Color32[] sign = BuildSign(0);
            BlitSprite(canvas, width, sign, PlayerWidth, PlayerHeight,
                6 * TileSize, 1 * TileSize + TrialStripHeight);
            BlitSprite(canvas, width, BuildSignPostV2(), PlayerWidth, PlayerHeight,
                6 * TileSize, 1 * TileSize + TrialStripHeight);

            BlitSprite(canvas, width, BuildPlayerV2(Vector2Int.down), PlayerWidth, PlayerHeight,
                4 * TileSize, 2 * TileSize + TrialStripHeight);

            // ---- le HUD : deux boites, en haut a gauche et en haut a droite
            int top = sceneHeight + TrialStripHeight;
            Color32[] leftBox = BuildBanner(60, 32);
            BlitSprite(canvas, width, leftBox, 60, 32, 4, top - 4 - 32);
            BlitSprite(canvas, width, BuildSunPictoV2(), HudPictoSize, HudPictoSize, 8, top - 8 - HudPictoSize);
            BlitSprite(canvas, width, BuildSpringPictoV2(), HudPictoSize, HudPictoSize, 36, top - 8 - HudPictoSize);

            const int dropCount = 6;
            int dropBoxWidth = dropCount * 14 + 6;
            Color32[] rightBox = BuildBanner(dropBoxWidth, 22);
            int dropBoxX = width - 4 - dropBoxWidth;
            BlitSprite(canvas, width, rightBox, dropBoxWidth, 22, dropBoxX, top - 4 - 22);
            for (int i = 0; i < dropCount; i++)
            {
                BlitSprite(canvas, width, BuildDropV2(i < 3), TileSize, TileSize,
                    dropBoxX + 4 + i * 14, top - 4 - 22 + 3);
            }

            // ---- la bande des seize couleurs neuves, en bas
            int firstNew = System.Array.IndexOf(Palette.Names, "LawnLight");
            int swatch = width / (Palette.All.Length - firstNew);
            for (int i = firstNew; i < Palette.All.Length; i++)
            {
                int x0 = (i - firstNew) * swatch;
                for (int y = 2; y < TrialStripHeight - 2; y++)
                {
                    for (int x = x0 + 1; x < x0 + swatch - 1; x++)
                    {
                        canvas[y * width + x] = Palette.All[i];
                    }
                }
            }

            System.IO.Directory.CreateDirectory("Captures");
            Texture2D sheet = new Texture2D(width * TrialScale, height * TrialScale, TextureFormat.RGBA32, false);
            sheet.SetPixels32(Upscale(canvas, width, height, TrialScale));
            WriteSheet("Captures/planche_essai_18.png", sheet);

            Debug.Log("[Sous la Ville] Planche d'essai écrite : Captures/planche_essai_18.png. À REGARDER.");
        }

        /// <summary>Le masque de raccord d'une case dans un ensemble : bit 0 nord, 1 est, 2 sud, 3 ouest.</summary>
        private static int NeighbourMask(HashSet<Vector2Int> cells, Vector2Int cell)
        {
            int mask = 0;
            if (cells.Contains(cell + Vector2Int.up)) mask |= 1;
            if (cells.Contains(cell + Vector2Int.right)) mask |= 2;
            if (cells.Contains(cell + Vector2Int.down)) mask |= 4;
            if (cells.Contains(cell + Vector2Int.left)) mask |= 8;
            return mask;
        }

        /// <summary>Pose une tuile sur la case (cx, cy) de la planche, le bas de la scene etant decale de la bande.</summary>
        private static void BlitTile(Color32[] canvas, int canvasWidth, Color32[] tile, int tileWidth,
            int tileHeight, int cx, int cy, int offsetY)
        {
            BlitSprite(canvas, canvasWidth, tile, tileWidth, tileHeight, cx * tileWidth, cy * tileHeight + offsetY);
        }

        /// <summary>
        /// Pose un sprite par son coin bas gauche, pixel transparent ignore, pixel translucide
        /// melange : l'eau se voit au travers sur la planche comme en jeu.
        /// </summary>
        private static void BlitSprite(Color32[] canvas, int canvasWidth, Color32[] sprite, int spriteWidth,
            int spriteHeight, int x0, int y0)
        {
            int canvasHeight = canvas.Length / canvasWidth;

            for (int y = 0; y < spriteHeight; y++)
            {
                for (int x = 0; x < spriteWidth; x++)
                {
                    Color32 source = sprite[y * spriteWidth + x];
                    if (source.a == 0)
                    {
                        continue;
                    }

                    int tx = x0 + x;
                    int ty = y0 + y;
                    if (tx < 0 || ty < 0 || tx >= canvasWidth || ty >= canvasHeight)
                    {
                        continue;
                    }

                    int index = ty * canvasWidth + tx;
                    canvas[index] = source.a == 255 ? source : Color32.Lerp(canvas[index], source, source.a / 255f);
                }
            }
        }

        private static Color32[] Upscale(Color32[] pixels, int width, int height, int scale)
        {
            Color32[] result = new Color32[width * scale * height * scale];
            int resultWidth = width * scale;

            for (int y = 0; y < height * scale; y++)
            {
                for (int x = 0; x < resultWidth; x++)
                {
                    result[y * resultWidth + x] = pixels[(y / scale) * width + (x / scale)];
                }
            }

            return result;
        }
    }
}
