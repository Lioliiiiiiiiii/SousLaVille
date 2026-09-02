using System.Collections.Generic;
using SousLaVille.Core;
using SousLaVille.World;
using UnityEditor;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.Tilemaps;

namespace SousLaVille.EditorTools
{
    /// <summary>
    /// Scene Underground : le reseau. La carte est peinte a partir du plan ASCII de
    /// UndergroundLayout, jamais a la main dans l'editeur.
    ///
    /// Meme structure que la surface, deux tilemaps : le sol, decoratif, et la couche
    /// bloquante qui porte la terre pleine et sert de carte de collision. Creuser, en
    /// phase 3, sera retirer une tuile de la couche bloquante.
    /// </summary>
    public static class UndergroundSceneBuilder
    {
        public const string SceneName = "Underground";

        // Le sous-sol est plus sombre que le village, sans gener la lecture des couleurs.
        private const float GlobalLightIntensity = 0.8f;

        [MenuItem("Sous La Ville/Construire la scène Underground")]
        public static void Build()
        {
            if (!UndergroundLayout.IsWellFormed() || !VillageLayout.IsWellFormed())
            {
                return;
            }

            // Un portail mal aligne ne planterait pas, il serait juste mort : on refuse de
            // construire plutot que de livrer une bouche qui ne mene nulle part.
            if (!UndergroundLayout.ValidateAgainstVillage())
            {
                return;
            }

            if (!PlaceholderArtGenerator.AreAssetsPresent())
            {
                Debug.LogError("[Sous la Ville] Art placeholder absent. Lance d'abord " +
                               "« Sous La Ville/Générer l'art placeholder ».");
                return;
            }

            Scene scene = SceneBuilderUtility.BeginScene();
            if (!scene.IsValid())
            {
                return;
            }

            GameObject root = LayerRootBuilder.CreateRoot(SceneName, GlobalLightIntensity,
                GameSortingLayers.Underground);

            Tilemap ground;
            Tilemap blocking;
            Grid grid = CreateGrid(root, out ground, out blocking);

            PaintUnderground(ground, blocking);
            CreateLadders(root);
            AttachUndergroundMap(root, grid, ground, blocking);

            SceneBuilderUtility.EndScene(scene, SceneName);
        }

        private static Grid CreateGrid(GameObject root, out Tilemap ground, out Tilemap blocking)
        {
            GameObject gridObject = new GameObject("Grid");
            gridObject.transform.SetParent(root.transform, false);

            Grid grid = gridObject.AddComponent<Grid>();
            grid.cellSize = new Vector3(1f, 1f, 0f);
            grid.cellLayout = GridLayout.CellLayout.Rectangle;

            ground = CreateTilemap(gridObject, "Tilemap_Ground", order: 0);
            blocking = CreateTilemap(gridObject, "Tilemap_Blocking", order: 1);

            return grid;
        }

        private static Tilemap CreateTilemap(GameObject gridObject, string name, int order)
        {
            GameObject tilemapObject = new GameObject(name);
            tilemapObject.transform.SetParent(gridObject.transform, false);

            Tilemap tilemap = tilemapObject.AddComponent<Tilemap>();
            TilemapRenderer renderer = tilemapObject.AddComponent<TilemapRenderer>();
            SceneBuilderUtility.ApplySortingLayer(renderer, GameSortingLayers.UndergroundGround, order);

            return tilemap;
        }

        /// <summary>
        /// Peint les deux tilemaps d'un coup, comme en surface. La terre pleine est peinte
        /// dans les deux : sur le sol pour le decor, sur la couche bloquante pour la
        /// collision. Creuser reviendra a retirer la seconde et a repeindre la premiere.
        /// </summary>
        private static void PaintUnderground(Tilemap ground, Tilemap blocking)
        {
            Tile earth = LoadTile(PlaceholderArtGenerator.TileEarth);
            Tile tunnel = LoadTile(PlaceholderArtGenerator.TileTunnel);

            int width = UndergroundLayout.Width;
            int height = UndergroundLayout.Height;

            TileBase[] groundTiles = new TileBase[width * height];
            TileBase[] blockingTiles = new TileBase[width * height];

            for (int y = 0; y < height; y++)
            {
                for (int x = 0; x < width; x++)
                {
                    int index = x + y * width;

                    if (UndergroundLayout.IsOpen(x, y))
                    {
                        groundTiles[index] = tunnel;
                    }
                    else
                    {
                        groundTiles[index] = earth;
                        blockingTiles[index] = earth;
                    }
                }
            }

            BoundsInt bounds = new BoundsInt(0, 0, 0, width, height, 1);
            ground.SetTilesBlock(bounds, groundTiles);
            blocking.SetTilesBlock(bounds, blockingTiles);
            ground.CompressBounds();
            blocking.CompressBounds();
        }

        /// <summary>
        /// Une echelle sous chaque bouche, plus l'arrivee sous la station. Chacune porte un
        /// ManholePortal qui remonte a la meme case : les deux cartes sont alignees.
        /// </summary>
        private static void CreateLadders(GameObject root)
        {
            Sprite sprite = LoadSprite(PlaceholderArtGenerator.LadderTexture);

            GameObject parent = new GameObject("Ladders");
            parent.transform.SetParent(root.transform, false);

            List<Vector2Int> cells = UndergroundLayout.FindAll(UndergroundLayout.Ladder);
            for (int i = 0; i < cells.Count; i++)
            {
                CreateExit(parent, $"Ladder_{i + 1:00}", cells[i], sprite);
            }

            foreach (Vector2Int cell in UndergroundLayout.FindAll(UndergroundLayout.PlantOutlet))
            {
                CreateExit(root, "PlantOutlet", cell, sprite);
            }
        }

        private static void CreateExit(GameObject parent, string name, Vector2Int cell, Sprite sprite)
        {
            GameObject exit = new GameObject(name);
            exit.transform.SetParent(parent.transform, false);
            exit.transform.position = SurfaceSceneBuilder.CellCenter(cell);

            SpriteRenderer renderer = exit.AddComponent<SpriteRenderer>();
            renderer.sprite = sprite;
            SceneBuilderUtility.ApplySortingLayer(renderer, GameSortingLayers.UndergroundEntities, 0);

            PortalBuilder.Attach(exit, cell, GameLayer.Underground, GameLayer.Surface);
        }

        private static void AttachUndergroundMap(GameObject root, Grid grid, Tilemap ground,
            Tilemap blocking)
        {
            UndergroundMap map = root.AddComponent<UndergroundMap>();

            SerializedObject serialized = new SerializedObject(map);
            serialized.FindProperty("grid").objectReferenceValue = grid;
            serialized.FindProperty("mapSize").vector2IntValue =
                new Vector2Int(UndergroundLayout.Width, UndergroundLayout.Height);
            serialized.FindProperty("ground").objectReferenceValue = ground;
            serialized.FindProperty("blocking").objectReferenceValue = blocking;
            serialized.ApplyModifiedPropertiesWithoutUndo();
        }

        private static Tile LoadTile(string path)
        {
            Tile tile = AssetDatabase.LoadAssetAtPath<Tile>(path);
            if (tile == null)
            {
                Debug.LogError($"[Sous la Ville] Tuile introuvable : {path}");
            }

            return tile;
        }

        private static Sprite LoadSprite(string path)
        {
            Sprite sprite = AssetDatabase.LoadAssetAtPath<Sprite>(path);
            if (sprite == null)
            {
                Debug.LogError($"[Sous la Ville] Sprite introuvable : {path}");
            }

            return sprite;
        }
    }
}
