using System.Collections.Generic;
using SousLaVille.Core;
using SousLaVille.Network;
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

            if (!ScriptableObjectSetup.ArePresent())
            {
                Debug.LogError("[Sous la Ville] ScriptableObjects absents. Lance d'abord " +
                               "« Sous La Ville/Créer les ScriptableObjects ».");
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
            Tilemap pipes;
            Grid grid = CreateGrid(root, out ground, out blocking, out pipes);

            PaintUnderground(ground, blocking);
            CreateLadders(root);

            UndergroundMap map = AttachUndergroundMap(root, grid, ground, blocking);
            AttachNetwork(root, map, pipes);

            SceneBuilderUtility.EndScene(scene, SceneName);
        }

        private static Grid CreateGrid(GameObject root, out Tilemap ground, out Tilemap blocking,
            out Tilemap pipes)
        {
            GameObject gridObject = new GameObject("Grid");
            gridObject.transform.SetParent(root.transform, false);

            Grid grid = gridObject.AddComponent<Grid>();
            grid.cellSize = new Vector3(1f, 1f, 0f);
            grid.cellLayout = GridLayout.CellLayout.Rectangle;

            ground = CreateTilemap(gridObject, "Tilemap_Ground", order: 0);
            blocking = CreateTilemap(gridObject, "Tilemap_Blocking", order: 1);

            // Les canalisations ont leur propre famille de tri : elles se posent par-dessus
            // le sol et passent sous le personnage.
            pipes = CreateTilemap(gridObject, "Tilemap_Pipes",
                GameSortingLayers.UndergroundPipes, order: 0);

            return grid;
        }

        private static Tilemap CreateTilemap(GameObject gridObject, string name, int order)
        {
            return CreateTilemap(gridObject, name, GameSortingLayers.UndergroundGround, order);
        }

        private static Tilemap CreateTilemap(GameObject gridObject, string name,
            string sortingLayer, int order)
        {
            GameObject tilemapObject = new GameObject(name);
            tilemapObject.transform.SetParent(gridObject.transform, false);

            Tilemap tilemap = tilemapObject.AddComponent<Tilemap>();
            TilemapRenderer renderer = tilemapObject.AddComponent<TilemapRenderer>();
            SceneBuilderUtility.ApplySortingLayer(renderer, sortingLayer, order);

            return tilemap;
        }

        /// <summary>
        /// Peint les deux tilemaps d'un coup, comme en surface. La terre pleine est peinte
        /// dans les deux : sur le sol pour le decor, sur la couche bloquante pour la
        /// collision. Creuser reviendra a retirer la seconde et a repeindre la premiere.
        /// </summary>
        private static void PaintUnderground(Tilemap ground, Tilemap blocking)
        {
            Tile[] earth = new Tile[PlaceholderArtGenerator.DepthCount];
            Tile[] tunnel = new Tile[PlaceholderArtGenerator.DepthCount];

            for (int depth = 1; depth <= PlaceholderArtGenerator.DepthCount; depth++)
            {
                earth[depth - 1] = LoadTile(PlaceholderArtGenerator.TileEarth(depth));
                tunnel[depth - 1] = LoadTile(PlaceholderArtGenerator.TileTunnel(depth));
            }

            int width = UndergroundLayout.Width;
            int height = UndergroundLayout.Height;

            TileBase[] groundTiles = new TileBase[width * height];
            TileBase[] blockingTiles = new TileBase[width * height];

            for (int y = 0; y < height; y++)
            {
                for (int x = 0; x < width; x++)
                {
                    int index = x + y * width;
                    int depth = UndergroundLayout.DepthAt(x, y) - 1;

                    if (UndergroundLayout.IsOpen(x, y))
                    {
                        groundTiles[index] = tunnel[depth];
                    }
                    else
                    {
                        groundTiles[index] = earth[depth];
                        blockingTiles[index] = earth[depth];
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

        private static UndergroundMap AttachUndergroundMap(GameObject root, Grid grid,
            Tilemap ground, Tilemap blocking)
        {
            UndergroundMap map = root.AddComponent<UndergroundMap>();

            SerializedObject serialized = new SerializedObject(map);
            serialized.FindProperty("grid").objectReferenceValue = grid;
            serialized.FindProperty("mapSize").vector2IntValue =
                new Vector2Int(UndergroundLayout.Width, UndergroundLayout.Height);
            serialized.FindProperty("ground").objectReferenceValue = ground;
            serialized.FindProperty("blocking").objectReferenceValue = blocking;

            // La profondeur est cuite dans la carte : c'est une donnee statique, la tuile
            // n'en est que l'affichage.
            SerializedProperty depths = serialized.FindProperty("depths");
            depths.arraySize = UndergroundLayout.Width * UndergroundLayout.Height;
            for (int y = 0; y < UndergroundLayout.Height; y++)
            {
                for (int x = 0; x < UndergroundLayout.Width; x++)
                {
                    depths.GetArrayElementAtIndex(x + y * UndergroundLayout.Width).intValue =
                        UndergroundLayout.DepthAt(x, y);
                }
            }

            // Les tuiles que Dig ira peindre quand le joueur ouvrira une galerie.
            SerializedProperty tunnelTiles = serialized.FindProperty("tunnelTilesByDepth");
            tunnelTiles.arraySize = PlaceholderArtGenerator.DepthCount;
            for (int depth = 1; depth <= PlaceholderArtGenerator.DepthCount; depth++)
            {
                tunnelTiles.GetArrayElementAtIndex(depth - 1).objectReferenceValue =
                    LoadTile(PlaceholderArtGenerator.TileTunnel(depth));
            }

            serialized.ApplyModifiedPropertiesWithoutUndo();
            return map;
        }

        /// <summary>
        /// Le graphe et son rendu. Le seul noeud impose par le monde est la station : les
        /// echelles restent des cases ordinaires tant que le type Manhole n'a pas d'usage.
        /// </summary>
        private static void AttachNetwork(GameObject root, UndergroundMap map, Tilemap pipes)
        {
            PipeNetwork network = root.AddComponent<PipeNetwork>();

            SerializedObject serializedNetwork = new SerializedObject(network);
            serializedNetwork.FindProperty("map").objectReferenceValue = map;
            serializedNetwork.FindProperty("defaultPipeType").objectReferenceValue =
                AssetDatabase.LoadAssetAtPath<PipeType>(ScriptableObjectSetup.PipeTypeStandard);

            List<Vector2Int> outlets = UndergroundLayout.FindAll(UndergroundLayout.PlantOutlet);
            SerializedProperty fixedNodes = serializedNetwork.FindProperty("fixedNodes");
            fixedNodes.arraySize = outlets.Count;
            for (int i = 0; i < outlets.Count; i++)
            {
                SerializedProperty element = fixedNodes.GetArrayElementAtIndex(i);
                element.FindPropertyRelative("cell").vector2IntValue = outlets[i];
                element.FindPropertyRelative("type").enumValueIndex = (int)NodeType.PlantInlet;
            }

            serializedNetwork.ApplyModifiedPropertiesWithoutUndo();

            PipeNetworkView view = root.AddComponent<PipeNetworkView>();

            SerializedObject serializedView = new SerializedObject(view);
            serializedView.FindProperty("network").objectReferenceValue = network;
            serializedView.FindProperty("pipes").objectReferenceValue = pipes;

            SerializedProperty tiles = serializedView.FindProperty("tilesByMask");
            tiles.arraySize = PlaceholderArtGenerator.PipeMaskCount;
            for (int mask = 0; mask < PlaceholderArtGenerator.PipeMaskCount; mask++)
            {
                tiles.GetArrayElementAtIndex(mask).objectReferenceValue =
                    LoadTile(PlaceholderArtGenerator.TilePipe(mask));
            }

            serializedView.ApplyModifiedPropertiesWithoutUndo();
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
