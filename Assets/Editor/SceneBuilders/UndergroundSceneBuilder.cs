using System.Collections.Generic;
using SousLaVille.Buildings;
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

        // Les nombres du bilan de l'eau, phase 8. Cinq maisons et huit de pluie font treize
        // a l'automne : la station en traite huit, le bassin encaisse cinq, et l'ete le
        // ramene a zero. Le cycle est stable.
        // PHASE 11 : 9 et non 8. La fontaine est une sixieme destination, et l'annee passe de
        // 31 a 35 unites d'arrivant. A huit par saison, la station n'en traite que 32 : le
        // bassin gagnerait trois unites par an, saturerait vers la troisieme annee, et le
        // village deborderait a chaque automne. A neuf, le bilan annuel redevient -1, comme
        // avant la fontaine, et la suite du bassin retombe exactement sur celle de la phase 8,
        // 5 / 3 / 2 / 0.
        private const int PlantCapacityPerSeason = 9;
        private const int ReserveCapacity = 10;

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
            CreateHouseInlets(root);
            CreateReserve(root);

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
                GameObject outlet = CreateExit(root, "PlantOutlet", cell, sprite);

                // La station devient un objet, phase 8 : elle porte ce qu'elle traite par
                // saison. Sur l'arrivee, a cote de son noeud.
                TreatmentPlant plant = outlet.AddComponent<TreatmentPlant>();
                SerializedObject serialized = new SerializedObject(plant);
                serialized.FindProperty("capacityPerSeason").intValue = PlantCapacityPerSeason;
                serialized.ApplyModifiedPropertiesWithoutUndo();
            }
        }

        /// <summary>
        /// Le bassin d'orage, dans sa chambre deja creusee. La cuve porte son niveau : cinq
        /// images, de vide a pleine, et rien au HUD.
        /// </summary>
        private static void CreateReserve(GameObject root)
        {
            List<Vector2Int> cells = UndergroundLayout.FindAll(UndergroundLayout.Reserve);
            if (cells.Count == 0)
            {
                return;
            }

            GameObject reserveObject = new GameObject("WaterReserve");
            reserveObject.transform.SetParent(root.transform, false);
            reserveObject.transform.position = SurfaceSceneBuilder.CellCenter(cells[0]);

            SpriteRenderer renderer = reserveObject.AddComponent<SpriteRenderer>();
            renderer.sprite = LoadSprite(PlaceholderArtGenerator.ReserveTexture(0));
            SceneBuilderUtility.ApplySortingLayer(renderer, GameSortingLayers.UndergroundEntities, 0);

            WaterReserve reserve = reserveObject.AddComponent<WaterReserve>();

            SerializedObject serialized = new SerializedObject(reserve);
            serialized.FindProperty("cell").vector2IntValue = cells[0];
            serialized.FindProperty("capacity").intValue = ReserveCapacity;
            serialized.FindProperty("view").objectReferenceValue = renderer;

            SerializedProperty sprites = serialized.FindProperty("levelSprites");
            sprites.arraySize = PlaceholderArtGenerator.ReserveLevelCount;
            for (int level = 0; level < PlaceholderArtGenerator.ReserveLevelCount; level++)
            {
                sprites.GetArrayElementAtIndex(level).objectReferenceValue =
                    LoadSprite(PlaceholderArtGenerator.ReserveTexture(level));
            }

            serialized.ApplyModifiedPropertiesWithoutUndo();
        }

        /// <summary>
        /// Le repere d'arrivee d'une maison, dans son alcove. Aucun portail : on ne monte pas
        /// dans les maisons, on y raccorde un tuyau.
        /// </summary>
        private static void CreateHouseInlets(GameObject root)
        {
            List<Vector2Int> cells = UndergroundLayout.FindAll(UndergroundLayout.HouseOutlet);
            if (cells.Count == 0)
            {
                return;
            }

            Sprite sprite = LoadSprite(PlaceholderArtGenerator.HouseInletTexture);

            GameObject parent = new GameObject("HouseInlets");
            parent.transform.SetParent(root.transform, false);

            for (int i = 0; i < cells.Count; i++)
            {
                GameObject inlet = new GameObject($"HouseInlet_{i + 1:00}");
                inlet.transform.SetParent(parent.transform, false);
                inlet.transform.position = SurfaceSceneBuilder.CellCenter(cells[i]);

                SpriteRenderer renderer = inlet.AddComponent<SpriteRenderer>();
                renderer.sprite = sprite;
                SceneBuilderUtility.ApplySortingLayer(renderer,
                    GameSortingLayers.UndergroundEntities, 0);
            }
        }

        private static GameObject CreateExit(GameObject parent, string name, Vector2Int cell,
            Sprite sprite)
        {
            GameObject exit = new GameObject(name);
            exit.transform.SetParent(parent.transform, false);
            exit.transform.position = SurfaceSceneBuilder.CellCenter(cell);

            SpriteRenderer renderer = exit.AddComponent<SpriteRenderer>();
            renderer.sprite = sprite;
            SceneBuilderUtility.ApplySortingLayer(renderer, GameSortingLayers.UndergroundEntities, 0);

            PortalBuilder.Attach(exit, cell, GameLayer.Underground, GameLayer.Surface);

            return exit;
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
        /// Le graphe et son rendu. Les noeuds imposes par le monde sont la station, les
        /// maisons et le bassin : les echelles restent des cases ordinaires tant que le type
        /// Manhole n'a pas d'usage.
        /// </summary>
        private static void AttachNetwork(GameObject root, UndergroundMap map, Tilemap pipes)
        {
            PipeNetwork network = root.AddComponent<PipeNetwork>();

            SerializedObject serializedNetwork = new SerializedObject(network);
            serializedNetwork.FindProperty("map").objectReferenceValue = map;
            serializedNetwork.FindProperty("defaultPipeType").objectReferenceValue =
                AssetDatabase.LoadAssetAtPath<PipeType>(ScriptableObjectSetup.PipeTypeStandard);

            List<Vector2Int> outlets = UndergroundLayout.FindAll(UndergroundLayout.PlantOutlet);
            List<Vector2Int> houses = UndergroundLayout.FindAll(UndergroundLayout.HouseOutlet);
            List<Vector2Int> reserves = UndergroundLayout.FindAll(UndergroundLayout.Reserve);
            List<Vector2Int> fountains = UndergroundLayout.FindAll(UndergroundLayout.Fountain);

            SerializedProperty fixedNodes = serializedNetwork.FindProperty("fixedNodes");
            fixedNodes.arraySize =
                outlets.Count + houses.Count + reserves.Count + fountains.Count;

            int index = 0;
            foreach (Vector2Int cell in outlets)
            {
                SetFixedNode(fixedNodes.GetArrayElementAtIndex(index++), cell, NodeType.PlantInlet);
            }

            // Les maisons sont des noeuds permanents comme la station : le joueur ne les pose
            // pas et ne peut pas les retirer, il vient s'y raccorder.
            foreach (Vector2Int cell in houses)
            {
                SetFixedNode(fixedNodes.GetArrayElementAtIndex(index++), cell,
                    NodeType.HouseConnection);
            }

            // Le bassin, phase 8 : un noeud comme un autre pour le solveur, permanent comme
            // les maisons. Le joueur vient s'y raccorder.
            foreach (Vector2Int cell in reserves)
            {
                SetFixedNode(fixedNodes.GetArrayElementAtIndex(index++), cell, NodeType.ReserveInlet);
            }

            // La fontaine, phase 11 : une destination comme une maison, permanente comme
            // elles. C'est le premier usage de FountainInlet, le seul type du modele de
            // CLAUDE.md qui n'en avait aucun depuis la phase 3.
            foreach (Vector2Int cell in fountains)
            {
                SetFixedNode(fixedNodes.GetArrayElementAtIndex(index++), cell,
                    NodeType.FountainInlet);
            }

            serializedNetwork.ApplyModifiedPropertiesWithoutUndo();

            PipeNetworkView view = root.AddComponent<PipeNetworkView>();

            SerializedObject serializedView = new SerializedObject(view);
            serializedView.FindProperty("network").objectReferenceValue = network;
            serializedView.FindProperty("pipes").objectReferenceValue = pipes;

            // Quarante-huit tuiles : seize masques par motif, les motifs bout a bout. Le
            // rang vaut motif * 16 + masque, ce que TileFor recalcule cote rendu.
            int patterns = PlaceholderArtGenerator.PipePatternCount;
            int masks = PlaceholderArtGenerator.PipeMaskCount;

            SerializedProperty tiles = serializedView.FindProperty("tilesByPatternAndMask");
            tiles.arraySize = patterns * masks;

            for (int pattern = 0; pattern < patterns; pattern++)
            {
                for (int mask = 0; mask < masks; mask++)
                {
                    tiles.GetArrayElementAtIndex(pattern * masks + mask).objectReferenceValue =
                        LoadTile(PlaceholderArtGenerator.TilePipe(pattern, mask));
                }
            }

            serializedView.FindProperty("patternCount").intValue = patterns;

            serializedView.ApplyModifiedPropertiesWithoutUndo();
        }

        private static void SetFixedNode(SerializedProperty element, Vector2Int cell, NodeType type)
        {
            element.FindPropertyRelative("cell").vector2IntValue = cell;
            element.FindPropertyRelative("type").enumValueIndex = (int)type;
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
