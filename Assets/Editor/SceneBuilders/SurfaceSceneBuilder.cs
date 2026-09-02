using System.Collections.Generic;
using SousLaVille.World;
using UnityEditor;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.Tilemaps;

namespace SousLaVille.EditorTools
{
    /// <summary>
    /// Scene Surface : le village. La carte est peinte a partir du plan ASCII de
    /// VillageLayout, jamais a la main dans l'editeur.
    ///
    /// Deux tilemaps seulement : le sol, purement decoratif, et la couche bloquante qui
    /// sert aussi de carte de collision. Aucune donnee dupliquee.
    /// </summary>
    public static class SurfaceSceneBuilder
    {
        public const string SceneName = "Surface";

        private const string GroundSortingLayer = "Surface_Ground";
        private const string EntitiesSortingLayer = "Surface_Entities";

        [MenuItem("Sous La Ville/Construire la scène Surface")]
        public static void Build()
        {
            if (!VillageLayout.IsWellFormed())
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

            GameObject root = LayerRootBuilder.CreateRoot(SceneName, globalLightIntensity: 1f,
                SortingLayerSetup.SurfaceLayers);

            Tilemap ground;
            Tilemap blocking;
            Grid grid = CreateGrid(root, out ground, out blocking);

            PaintVillage(ground, blocking);
            CreateManholes(root);
            CreateTreatmentPlant(root);
            AttachSurfaceMap(root, grid, ground, blocking);

            SceneBuilderUtility.EndScene(scene, SceneName);
        }

        private static Grid CreateGrid(GameObject root, out Tilemap ground, out Tilemap blocking)
        {
            GameObject gridObject = new GameObject("Grid");
            gridObject.transform.SetParent(root.transform, false);

            Grid grid = gridObject.AddComponent<Grid>();
            grid.cellSize = new Vector3(1f, 1f, 0f);
            grid.cellLayout = GridLayout.CellLayout.Rectangle;

            // Le sol et la couche bloquante partagent le meme Sorting Layer : c'est l'ordre
            // qui les separe, les haies se posent par-dessus l'herbe.
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
            SceneBuilderUtility.ApplySortingLayer(renderer, GroundSortingLayer, order);

            return tilemap;
        }

        /// <summary>
        /// Peint les deux tilemaps d'un coup. SetTilesBlock ecrit les 1200 cases en un
        /// appel, la ou SetTile case par case declencherait autant de mises a jour.
        /// </summary>
        private static void PaintVillage(Tilemap ground, Tilemap blocking)
        {
            Tile grass = LoadTile(PlaceholderArtGenerator.TileGrass);
            Tile road = LoadTile(PlaceholderArtGenerator.TilePath);
            Tile park = LoadTile(PlaceholderArtGenerator.TilePark);
            Tile plantFloor = LoadTile(PlaceholderArtGenerator.TilePlantFloor);
            Tile hedge = LoadTile(PlaceholderArtGenerator.TileHedge);
            Tile plantWall = LoadTile(PlaceholderArtGenerator.TilePlantWall);

            int width = VillageLayout.Width;
            int height = VillageLayout.Height;

            TileBase[] groundTiles = new TileBase[width * height];
            TileBase[] blockingTiles = new TileBase[width * height];

            for (int y = 0; y < height; y++)
            {
                for (int x = 0; x < width; x++)
                {
                    int index = x + y * width;
                    char cell = VillageLayout.GroundAt(x, y);

                    switch (cell)
                    {
                        case VillageLayout.Road:
                            groundTiles[index] = road;
                            break;
                        case VillageLayout.Park:
                            groundTiles[index] = park;
                            break;
                        case VillageLayout.PlantFloor:
                            groundTiles[index] = plantFloor;
                            break;
                        case VillageLayout.PlantWall:
                            // Du sol de station sous le batiment : le mur se pose dessus.
                            groundTiles[index] = plantFloor;
                            blockingTiles[index] = plantWall;
                            break;
                        case VillageLayout.Hedge:
                            groundTiles[index] = grass;
                            blockingTiles[index] = hedge;
                            break;
                        default:
                            groundTiles[index] = grass;
                            break;
                    }
                }
            }

            BoundsInt bounds = new BoundsInt(0, 0, 0, width, height, 1);
            ground.SetTilesBlock(bounds, groundTiles);
            blocking.SetTilesBlock(bounds, blockingTiles);
            ground.CompressBounds();
            blocking.CompressBounds();
        }

        private static void CreateManholes(GameObject root)
        {
            List<Vector2Int> cells = VillageLayout.FindAll(VillageLayout.Manhole);
            if (cells.Count == 0)
            {
                Debug.LogWarning("[Sous la Ville] Aucune bouche d'égout dans le plan du village.");
                return;
            }

            Sprite sprite = LoadSprite(PlaceholderArtGenerator.ManholeTexture);

            GameObject parent = new GameObject("Manholes");
            parent.transform.SetParent(root.transform, false);

            for (int i = 0; i < cells.Count; i++)
            {
                // La phase 2 n'aura plus qu'a ajouter ManholePortal sur ces objets, sans
                // retoucher la carte.
                GameObject manhole = new GameObject($"Manhole_{i + 1:00}");
                manhole.transform.SetParent(parent.transform, false);
                manhole.transform.position = CellCenter(cells[i]);

                SpriteRenderer renderer = manhole.AddComponent<SpriteRenderer>();
                renderer.sprite = sprite;
                SceneBuilderUtility.ApplySortingLayer(renderer, EntitiesSortingLayer, 0);
            }
        }

        private static void CreateTreatmentPlant(GameObject root)
        {
            Vector2Int cell = VillageLayout.FindSingle(VillageLayout.PlantInlet);

            GameObject plant = new GameObject("TreatmentPlant");
            plant.transform.SetParent(root.transform, false);
            plant.transform.position = CellCenter(cell);

            SpriteRenderer renderer = plant.AddComponent<SpriteRenderer>();
            renderer.sprite = LoadSprite(PlaceholderArtGenerator.PlantWallTexture);
            SceneBuilderUtility.ApplySortingLayer(renderer, EntitiesSortingLayer, 0);
        }

        private static void AttachSurfaceMap(GameObject root, Grid grid, Tilemap ground,
            Tilemap blocking)
        {
            SurfaceMap map = root.AddComponent<SurfaceMap>();

            SerializedObject serialized = new SerializedObject(map);
            serialized.FindProperty("grid").objectReferenceValue = grid;
            serialized.FindProperty("mapSize").vector2IntValue =
                new Vector2Int(VillageLayout.Width, VillageLayout.Height);
            serialized.FindProperty("ground").objectReferenceValue = ground;
            serialized.FindProperty("blocking").objectReferenceValue = blocking;
            serialized.ApplyModifiedPropertiesWithoutUndo();
        }

        /// <summary>Centre monde d'une case. La grille est a l'origine, taille de case 1x1.</summary>
        public static Vector3 CellCenter(Vector2Int cell)
        {
            return new Vector3(cell.x + 0.5f, cell.y + 0.5f, 0f);
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
