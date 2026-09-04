using System.Collections.Generic;
using SousLaVille.Buildings;
using SousLaVille.Core;
using SousLaVille.Seasons;
using SousLaVille.World;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering.Universal;
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

        private const string GroundSortingLayer = GameSortingLayers.SurfaceGround;
        private const string WaterSortingLayer = GameSortingLayers.SurfaceWater;
        private const string EntitiesSortingLayer = GameSortingLayers.SurfaceEntities;

        [MenuItem("Sous La Ville/Construire la scène Surface")]
        public static bool Build()
        {
            if (!VillageLayout.IsWellFormed() || !VillageLayout.ValidatePark())
            {
                return false;
            }

            if (!PlaceholderArtGenerator.AreAssetsPresent())
            {
                Debug.LogError("[Sous la Ville] Art placeholder absent. Lance d'abord " +
                               "« Sous La Ville/Générer l'art placeholder ».");
                return false;
            }

            Scene scene = SceneBuilderUtility.BeginScene();
            if (!scene.IsValid())
            {
                return false;
            }

            GameObject root = LayerRootBuilder.CreateRoot(SceneName, globalLightIntensity: 1f,
                GameSortingLayers.Surface);

            Tilemap ground;
            Tilemap blocking;
            Tilemap water;
            Grid grid = CreateGrid(root, out ground, out blocking, out water);

            PaintVillage(ground, blocking);
            CreateManholes(root);
            CreateTreatmentPlant(root);
            CreateFountain(root);

            SurfaceMap map = AttachSurfaceMap(root, grid, ground, blocking);
            AttachFloodView(root, map, water);
            AttachHouseSpawner(root, map);
            AttachSeasonAmbience(root);
            CreateBuildingDoors(root);

            SceneBuilderUtility.EndScene(scene, SceneName);
            return true;
        }

        private static Grid CreateGrid(GameObject root, out Tilemap ground, out Tilemap blocking,
            out Tilemap water)
        {
            GameObject gridObject = new GameObject("Grid");
            gridObject.transform.SetParent(root.transform, false);

            Grid grid = gridObject.AddComponent<Grid>();
            grid.cellSize = new Vector3(1f, 1f, 0f);
            grid.cellLayout = GridLayout.CellLayout.Rectangle;

            // Le sol et la couche bloquante partagent le meme Sorting Layer : c'est l'ordre
            // qui les separe, les haies se posent par-dessus l'herbe.
            ground = CreateTilemap(gridObject, "Tilemap_Ground", GroundSortingLayer, order: 0);
            blocking = CreateTilemap(gridObject, "Tilemap_Blocking", GroundSortingLayer, order: 1);

            // L'eau a sa propre famille, Surface_Water, creee en phase 0 pour « flaques,
            // fontaine » et restee vide jusqu'ici. Elle se dessine au-dessus du decor et
            // SOUS les entites : le personnage traverse l'eau, il ne passe pas dessous.
            water = CreateTilemap(gridObject, "Tilemap_Water", WaterSortingLayer, order: 0);

            return grid;
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
        /// Peint les deux tilemaps d'un coup. SetTilesBlock ecrit les 2880 cases en un
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
            Tile house = LoadTile(PlaceholderArtGenerator.TileHouse);
            Tile facade = LoadTile(PlaceholderArtGenerator.TileFacade);
            Tile fountainTile = LoadTile(PlaceholderArtGenerator.TileFountain);

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
                            break;
                        case VillageLayout.Hedge:
                            groundTiles[index] = grass;
                            break;
                        default:
                            groundTiles[index] = grass;
                            break;
                    }

                    // Les marqueurs bloquants : GroundAt a deja peint le sol dessous, la
                    // tuile bloquante se pose par-dessus.
                    //
                    // UNE SEULE LISTE DE BLOCAGE depuis la phase 12a. Ce switch et
                    // VillageLayout.IsWalkable etaient deux listes maintenues a la main : un
                    // marqueur ajoute a l'une et oublie a l'autre donnait une case franchissable
                    // qui ne se peint pas, ou l'inverse, sans un mot. C'est desormais le plan
                    // qui tranche, et lui seul.
                    char marker = VillageLayout.At(x, y);

                    if (!VillageLayout.IsWalkable(new Vector2Int(x, y)))
                    {
                        blockingTiles[index] =
                            marker == VillageLayout.House ? house
                            : marker == VillageLayout.Fountain ? fountainTile
                            : marker == VillageLayout.Facade
                              || marker == VillageLayout.PipeFacade ? facade
                            : marker == VillageLayout.PlantWall ? plantWall
                            : hedge;
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
                // Chaque bouche est un passage vers le sous-sol, sur sa propre case.
                GameObject manhole = new GameObject($"Manhole_{i + 1:00}");
                manhole.transform.SetParent(parent.transform, false);
                manhole.transform.position = CellCenter(cells[i]);

                SpriteRenderer renderer = manhole.AddComponent<SpriteRenderer>();
                renderer.sprite = sprite;
                SceneBuilderUtility.ApplySortingLayer(renderer, EntitiesSortingLayer, 0);

                PortalBuilder.Attach(manhole, cells[i], GameLayer.Surface, GameLayer.Underground);

                // La plaque que porte cette bouche. Elle demande a l'atelier ce qu'elle doit
                // afficher, et retombe sur son allure d'usine tant qu'on ne lui a rien pose.
                ManholeCover cover = manhole.AddComponent<ManholeCover>();
                SerializedObject serializedCover = new SerializedObject(cover);
                serializedCover.FindProperty("cell").vector2IntValue = cells[i];
                serializedCover.FindProperty("view").objectReferenceValue = renderer;
                serializedCover.FindProperty("defaultCover").objectReferenceValue = sprite;
                serializedCover.ApplyModifiedPropertiesWithoutUndo();
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

            // La station est un passage comme les autres : on y descend et on en remonte.
            PortalBuilder.Attach(plant, cell, GameLayer.Surface, GameLayer.Underground);
        }

        private static SurfaceMap AttachSurfaceMap(GameObject root, Grid grid, Tilemap ground,
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

            return map;
        }

        /// <summary>
        /// Les maisons ne sont pas posees ici : le spawner les cree au reveil a partir de
        /// cette liste de cases. La scene reste legere et le nom du composant dit vrai.
        /// </summary>
        private static void AttachHouseSpawner(GameObject root, SurfaceMap map)
        {
            List<Vector2Int> cells = VillageLayout.FindAll(VillageLayout.House);
            if (cells.Count == 0)
            {
                Debug.LogWarning("[Sous la Ville] Aucune maison dans le plan du village.");
            }

            HouseSpawner spawner = root.AddComponent<HouseSpawner>();

            SerializedObject serialized = new SerializedObject(spawner);
            SerializedProperty cellsProperty = serialized.FindProperty("cells");
            cellsProperty.arraySize = cells.Count;
            for (int i = 0; i < cells.Count; i++)
            {
                cellsProperty.GetArrayElementAtIndex(i).vector2IntValue = cells[i];
            }

            serialized.FindProperty("map").objectReferenceValue = map;
            serialized.FindProperty("houseSprite").objectReferenceValue =
                LoadSprite(PlaceholderArtGenerator.HouseTexture);
            serialized.FindProperty("dropServed").objectReferenceValue =
                LoadSprite(PlaceholderArtGenerator.PictoDropFull);
            serialized.FindProperty("dropIdle").objectReferenceValue =
                LoadSprite(PlaceholderArtGenerator.PictoDropEmpty);
            serialized.ApplyModifiedPropertiesWithoutUndo();
        }

        /// <summary>
        /// Les portes des batiments du village. Une porte est un ManholePortal comme une
        /// bouche d'egout : meme composant, meme geste, meme fondu au noir. Elle mene a la
        /// piece de son batiment, dans la scene Interiors.
        ///
        /// PHASE 9A : l'atelier des plaques etait une cour a ciel ouvert avec ses huit
        /// echantillons au sol. Il est devenu un batiment, et tout son contenu est passe dans
        /// InteriorsSceneBuilder. Il ne reste ici que la porte, devant sa facade.
        /// </summary>
        private static void CreateBuildingDoors(GameObject root)
        {
            if (VillageLayout.Doors.Length != InteriorsLayout.Rooms.Length)
            {
                Debug.LogError($"[Sous la Ville] Le village porte {VillageLayout.Doors.Length} " +
                               $"porte(s) pour {InteriorsLayout.Rooms.Length} pièce(s) : il en " +
                               "faut autant.");
                return;
            }

            Sprite sprite = LoadSprite(PlaceholderArtGenerator.DoorTexture);

            GameObject parent = new GameObject("Doors");
            parent.transform.SetParent(root.transform, false);

            for (int i = 0; i < VillageLayout.Doors.Length; i++)
            {
                InteriorsLayout.Room room = InteriorsLayout.Rooms[i];
                Vector2Int outside = VillageLayout.FindSingle(VillageLayout.Doors[i]);
                Vector2Int inside = InteriorsLayout.FindSingle(room, InteriorsLayout.Door);

                GameObject door = new GameObject($"Door_{i + 1:00}_{room.Name}");
                door.transform.SetParent(parent.transform, false);
                door.transform.position = CellCenter(outside);

                SpriteRenderer renderer = door.AddComponent<SpriteRenderer>();
                renderer.sprite = sprite;
                SceneBuilderUtility.ApplySortingLayer(renderer, EntitiesSortingLayer, 0);

                // Le pendant interieur de ce passage est pose par InteriorsSceneBuilder, sur
                // la meme paire de cases lue dans les deux plans.
                PortalBuilder.Attach(door, outside, GameLayer.Surface, GameLayer.Interior,
                    inside);
            }
        }

        /// <summary>
        /// La fontaine, au centre du labyrinthe de haies. Elle bloque le passage comme une
        /// maison : on l'atteint, on n'entre pas dedans.
        ///
        /// Elle n'a aucun etat propre : c'est le solveur qui dit si elle est desservie, et
        /// FloodView qui pose son eau. Le composant ne porte que sa case.
        /// </summary>
        private static void CreateFountain(GameObject root)
        {
            Vector2Int cell = VillageLayout.FindSingle(VillageLayout.Fountain);

            GameObject fountain = new GameObject("Fountain");
            fountain.transform.SetParent(root.transform, false);
            fountain.transform.position = CellCenter(cell);

            SpriteRenderer renderer = fountain.AddComponent<SpriteRenderer>();
            renderer.sprite = LoadSprite(PlaceholderArtGenerator.FountainTexture);
            SceneBuilderUtility.ApplySortingLayer(renderer, EntitiesSortingLayer, 0);

            Fountain component = fountain.AddComponent<Fountain>();
            SerializedObject serialized = new SerializedObject(component);
            serialized.FindProperty("cell").vector2IntValue = cell;
            serialized.ApplyModifiedPropertiesWithoutUndo();
        }

        /// <summary>
        /// L'eau du village : le debordement des bouches d'egout et les flaques des tuyaux
        /// creves. Il vit ici, dans la couche de jeu, et non dans le systeme de saisons : une
        /// couche eteinte ne repondrait pas, alors qu'un composant local se repeint a chaque
        /// rallumage. Meme raison que SeasonAmbience depuis la phase 5.
        ///
        /// Les cases des bouches sont cuites depuis le plan du village plutot que lues sur
        /// ManholeFactory : l'atelier vit dans la scene Interiors depuis la phase 9a, et
        /// l'emplacement des bouches est une propriete de la carte, pas du catalogue.
        /// </summary>
        private static void AttachFloodView(GameObject root, SurfaceMap map, Tilemap water)
        {
            List<Vector2Int> manholes = VillageLayout.FindAll(VillageLayout.Manhole);

            FloodView flood = root.AddComponent<FloodView>();

            SerializedObject serialized = new SerializedObject(flood);
            serialized.FindProperty("map").objectReferenceValue = map;
            serialized.FindProperty("water").objectReferenceValue = water;
            serialized.FindProperty("waterTile").objectReferenceValue =
                LoadTile(PlaceholderArtGenerator.TileWater);

            SerializedProperty cells = serialized.FindProperty("manholeCells");
            cells.arraySize = manholes.Count;
            for (int i = 0; i < manholes.Count; i++)
            {
                cells.GetArrayElementAtIndex(i).vector2IntValue = manholes[i];
            }

            serialized.ApplyModifiedPropertiesWithoutUndo();
        }

        /// <summary>
        /// La couleur du ciel selon la saison. Le composant vit ici, dans la couche de jeu,
        /// et non dans le systeme de saisons : une couche eteinte ne repondrait pas, alors
        /// qu'un composant local se rabonne a chaque rallumage.
        ///
        /// Le sous-sol n'en recoit pas : les saisons se voient dessus, se subissent dessous.
        /// </summary>
        private static void AttachSeasonAmbience(GameObject root)
        {
            Light2D globalLight = root.GetComponentInChildren<Light2D>(true);
            if (globalLight == null)
            {
                Debug.LogError("[Sous la Ville] Lumière globale introuvable sur la surface.");
                return;
            }

            SeasonAmbience ambience = root.AddComponent<SeasonAmbience>();

            SerializedObject serialized = new SerializedObject(ambience);
            serialized.FindProperty("globalLight").objectReferenceValue = globalLight;
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
