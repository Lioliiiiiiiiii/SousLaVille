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
    /// Scene Interiors : les pieces des batiments, cote a cote dans une seule carte.
    ///
    /// CINQUIEME SCENE, ecart explicite aux quatre scenes de CLAUDE.md, accepte le
    /// 3 septembre 2026. La peser dans la scene Surface aurait demande d'apprendre les pieces
    /// a SurfaceMap, d'agrandir le village au-dela de la colonne 40 donc de deplacer ses
    /// bornes de camera, et surtout n'aurait pas empeche les saisons de teinter les pieces :
    /// une Light2D globale porte sur des Sorting Layers, pas sur une zone.
    ///
    /// Comme les deux autres couches : une seule racine, que le SceneRouter allume et eteint,
    /// et une lumiere globale cantonnee a la famille Interior_*. C'est cet extinction qui
    /// garde les interieurs hors des saisons, sans une ligne de code : la SeasonAmbience du
    /// village est eteinte pendant qu'on est dedans.
    /// </summary>
    public static class InteriorsSceneBuilder
    {
        public const string SceneName = "Interiors";

        private const string GroundSortingLayer = GameSortingLayers.InteriorGround;
        private const string EntitiesSortingLayer = GameSortingLayers.InteriorEntities;

        // Meme decalage que le picto d'action du joueur, dans PersistentSceneBuilder : les
        // deux bulles se posent a la meme hauteur au-dessus d'une tete.
        private static readonly Vector3 PromptOffset = new Vector3(0f, 1.25f, 0f);

        [MenuItem("Sous La Ville/Construire la scène Interiors")]
        public static void Build()
        {
            if (!InteriorsLayout.IsWellFormed())
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

            // Meme intensite que le village : une piece est eclairee, et elle ne change pas
            // de lumiere avec les saisons.
            GameObject root = LayerRootBuilder.CreateRoot(SceneName, globalLightIntensity: 1f,
                GameSortingLayers.Interior);

            Tilemap ground;
            Tilemap blocking;
            Grid grid = CreateGrid(root, out ground, out blocking);

            PaintRooms(ground, blocking);
            AttachInteriorMap(root, grid, ground, blocking);
            CreateDoors(root);
            CreateVillagers(root);
            CreateCoverWorkshop(root);
            CreatePipeWorks(root);

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
            SceneBuilderUtility.ApplySortingLayer(renderer, GroundSortingLayer, order);

            return tilemap;
        }

        /// <summary>
        /// Peint les pieces, et rien d'autre : hors des pieces, aucune tuile. InteriorMap
        /// refuse toute case hors piece, ce qui evite de peindre six cents murs qu'on ne
        /// verra jamais.
        ///
        /// Le sol est le pave de l'atelier de la phase 7 : c'est la meme matiere, elle est
        /// simplement passee a l'interieur.
        ///
        /// Les plaques exposees ne bloquent pas, on marche dessus pour les choisir ; le
        /// personnage, lui, bloque.
        /// </summary>
        private static void PaintRooms(Tilemap ground, Tilemap blocking)
        {
            Tile floor = LoadTile(PlaceholderArtGenerator.TileWorkshop);
            Tile wall = LoadTile(PlaceholderArtGenerator.TileWall);

            int width = InteriorsLayout.Width;
            int height = InteriorsLayout.Height;

            TileBase[] groundTiles = new TileBase[width * height];
            TileBase[] blockingTiles = new TileBase[width * height];

            for (int y = 0; y < height; y++)
            {
                for (int x = 0; x < width; x++)
                {
                    if (!InteriorsLayout.IsInsideARoom(x, y))
                    {
                        continue;
                    }

                    int index = x + y * width;

                    // Du sol sous tout, y compris sous les murs : la tuile bloquante se pose
                    // par-dessus, comme l'herbe sous les haies depuis la phase 1.
                    groundTiles[index] = floor;

                    char marker = InteriorsLayout.At(x, y);

                    if (marker == InteriorsLayout.Wall)
                    {
                        blockingTiles[index] = wall;
                    }
                    else if (marker == InteriorsLayout.Villager)
                    {
                        // La case du personnage est bloquante : on ne traverse pas quelqu'un,
                        // et surtout, debout SUR lui on ne pourrait plus lui parler, puisque
                        // l'interacteur cherche un personnage sur la case REGARDEE.
                        //
                        // La tuile bloquante est celle du sol : elle disparait sous le sprite
                        // 16x24 du personnage, comme l'herbe sous la maison depuis la phase 1.
                        blockingTiles[index] = floor;
                    }
                }
            }

            BoundsInt bounds = new BoundsInt(0, 0, 0, width, height, 1);
            ground.SetTilesBlock(bounds, groundTiles);
            blocking.SetTilesBlock(bounds, blockingTiles);
            ground.CompressBounds();
            blocking.CompressBounds();
        }

        private static void AttachInteriorMap(GameObject root, Grid grid, Tilemap ground,
            Tilemap blocking)
        {
            InteriorMap map = root.AddComponent<InteriorMap>();

            SerializedObject serialized = new SerializedObject(map);
            serialized.FindProperty("grid").objectReferenceValue = grid;
            serialized.FindProperty("mapSize").vector2IntValue =
                new Vector2Int(InteriorsLayout.Width, InteriorsLayout.Height);
            serialized.FindProperty("ground").objectReferenceValue = ground;
            serialized.FindProperty("blocking").objectReferenceValue = blocking;

            // Les bornes des pieces, cuites depuis le plan : c'est ce qui borne la camera a
            // la piece ou l'on se tient plutot qu'a la carte entiere.
            SerializedProperty rooms = serialized.FindProperty("rooms");
            rooms.arraySize = InteriorsLayout.Rooms.Length;
            for (int i = 0; i < InteriorsLayout.Rooms.Length; i++)
            {
                rooms.GetArrayElementAtIndex(i).rectIntValue = InteriorsLayout.Rooms[i].Bounds;
            }

            serialized.ApplyModifiedPropertiesWithoutUndo();
        }

        /// <summary>
        /// Le pendant interieur de chaque porte du village. Meme composant, memes deux cases,
        /// lues dans les deux plans : le passage ne peut pas etre depareille.
        /// </summary>
        private static void CreateDoors(GameObject root)
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

            for (int i = 0; i < InteriorsLayout.Rooms.Length; i++)
            {
                InteriorsLayout.Room room = InteriorsLayout.Rooms[i];
                Vector2Int cell = InteriorsLayout.FindSingle(room, InteriorsLayout.Door);

                GameObject door = new GameObject($"Door_{i + 1:00}_{room.Name}");
                door.transform.SetParent(parent.transform, false);
                door.transform.position = CellCenter(cell);

                SpriteRenderer renderer = door.AddComponent<SpriteRenderer>();
                renderer.sprite = sprite;
                SceneBuilderUtility.ApplySortingLayer(renderer, EntitiesSortingLayer, 0);

                PortalBuilder.Attach(door, cell, GameLayer.Interior, GameLayer.Surface,
                    VillageLayout.FindSingle(VillageLayout.Doors[i]));
            }
        }

        /// <summary>
        /// Le personnage de chaque piece. Il dit ce qu'on peut faire ici, en phrases de moins
        /// de six mots dessinees par PixelFont a la generation de l'art.
        ///
        /// Un seul personnage par piece en phase 9a ; le patron en accepte plusieurs, ce dont
        /// l'usine a panneaux aura besoin en phase 12 avec ses trois mini-jeux.
        /// </summary>
        private static void CreateVillagers(GameObject root)
        {
            GameObject parent = new GameObject("Villagers");
            parent.transform.SetParent(root.transform, false);

            for (int i = 0; i < InteriorsLayout.Rooms.Length; i++)
            {
                InteriorsLayout.Room room = InteriorsLayout.Rooms[i];
                Vector2Int cell = InteriorsLayout.FindSingle(room, InteriorsLayout.Villager);

                GameObject villager = new GameObject($"Villager_{i + 1:00}_{room.Name}");
                villager.transform.SetParent(parent.transform, false);
                villager.transform.position = CellCenter(cell);

                SpriteRenderer renderer = villager.AddComponent<SpriteRenderer>();
                renderer.sprite = LoadSprite(SpriteFor(i));

                // Un cran devant le decor, un cran derriere le joueur, qui est en ordre 10.
                SceneBuilderUtility.ApplySortingLayer(renderer, EntitiesSortingLayer, 5);

                // La bulle au-dessus de SA tete, au meme decalage que le picto du joueur.
                // Elle passe devant tout le monde : c'est elle qu'on doit voir.
                GameObject promptObject = new GameObject("Prompt");
                promptObject.transform.SetParent(villager.transform, false);
                promptObject.transform.localPosition = PromptOffset;

                SpriteRenderer prompt = promptObject.AddComponent<SpriteRenderer>();
                prompt.sprite = LoadSprite(PlaceholderArtGenerator.PictoTalk);
                prompt.enabled = false;
                SceneBuilderUtility.ApplySortingLayer(prompt, EntitiesSortingLayer, 12);

                Villager component = villager.AddComponent<Villager>();

                SerializedObject serialized = new SerializedObject(component);
                serialized.FindProperty("cell").vector2IntValue = cell;
                serialized.FindProperty("prompt").objectReferenceValue = prompt;

                string[] paths = LinesFor(i);
                SerializedProperty lines = serialized.FindProperty("lines");
                lines.arraySize = paths.Length;
                for (int line = 0; line < paths.Length; line++)
                {
                    lines.GetArrayElementAtIndex(line).objectReferenceValue = LoadSprite(paths[line]);
                }

                serialized.ApplyModifiedPropertiesWithoutUndo();
            }
        }

        /// <summary>
        /// Le sprite du personnage d'une piece. Une table plutot qu'un champ dans le plan :
        /// InteriorsLayout decrit une geometrie, pas des chemins d'images.
        /// </summary>
        private static string SpriteFor(int roomIndex)
        {
            return roomIndex == 1
                ? PlaceholderArtGenerator.VillagerWorker
                : PlaceholderArtGenerator.VillagerCraftsman;
        }

        /// <summary>Ce que dit le personnage d'une piece, une image par phrase.</summary>
        private static string[] LinesFor(int roomIndex)
        {
            int count = roomIndex == 1
                ? PlaceholderArtGenerator.WorkerLines.Length
                : PlaceholderArtGenerator.CraftsmanLines.Length;

            string[] paths = new string[count];
            for (int i = 0; i < count; i++)
            {
                paths[i] = roomIndex == 1
                    ? PlaceholderArtGenerator.WorkerLineTexture(i)
                    : PlaceholderArtGenerator.CraftsmanLineTexture(i);
            }

            return paths;
        }

        /// <summary>
        /// L'usine a tuyaux : trois echantillons poses au sol, derriere l'ouvrier comme
        /// derriere un comptoir.
        ///
        /// NI NOM NI PICTO DE SAISON DANS LE DECOR depuis le 4 septembre 2026. Le picto de
        /// saison faisait trente-deux pixels de cote a cote d'un echantillon de seize, deux
        /// fois trop gros ; le nom, lui, ne se lisait pas. Les deux s'affichent au HUD quand
        /// on foule l'echantillon, a une taille qui leur va. Voir ItemLabel.
        ///
        /// L'echantillon EST la tuile qui sera posee en jeu : le picto de pose ne peut donc
        /// pas mentir sur ce qu'il va poser. Le standard ne vainc rien et n'a pas de picto,
        /// ce qui se voit d'un coup d'oeil et se comprend sans un mot.
        ///
        /// Comme les plaques, les echantillons ne bloquent pas : on marche dessus, et Espace
        /// prend celui qu'on foule. Pas de stock, pas de compte : on repart avec ce type en
        /// main, pour toujours, jusqu'a ce qu'on revienne en changer.
        /// </summary>
        private static void CreatePipeWorks(GameObject root)
        {
            InteriorsLayout.Room room = InteriorsLayout.Rooms[1];
            List<Vector2Int> samples = InteriorsLayout.FindAll(room, InteriorsLayout.PipeSample);

            if (samples.Count != ScriptableObjectSetup.PipeTypes.Length)
            {
                Debug.LogError($"[Sous la Ville] La pièce « {room.Name} » expose " +
                               $"{samples.Count} échantillon(s), il en faut " +
                               $"{ScriptableObjectSetup.PipeTypes.Length}.");
                return;
            }

            GameObject parent = new GameObject("PipeWorks");
            parent.transform.SetParent(root.transform, false);

            PipeType[] catalogue = new PipeType[ScriptableObjectSetup.PipeTypes.Length];

            for (int i = 0; i < samples.Count; i++)
            {
                catalogue[i] = AssetDatabase.LoadAssetAtPath<PipeType>(
                    ScriptableObjectSetup.PipeTypes[i]);

                if (catalogue[i] == null)
                {
                    Debug.LogError("[Sous la Ville] Type de tuyau introuvable : " +
                                   ScriptableObjectSetup.PipeTypes[i]);
                    continue;
                }

                GameObject sample = new GameObject($"Pipe_{i + 1:00}_{catalogue[i].DisplayName}");
                sample.transform.SetParent(parent.transform, false);
                sample.transform.position = CellCenter(samples[i]);

                SpriteRenderer view = sample.AddComponent<SpriteRenderer>();
                view.sprite = catalogue[i].Sample;
                SceneBuilderUtility.ApplySortingLayer(view, EntitiesSortingLayer, 0);
            }

            PipeFactory factory = root.AddComponent<PipeFactory>();

            SerializedObject serialized = new SerializedObject(factory);
            SetCells(serialized.FindProperty("sampleCells"), samples);

            SerializedProperty catalogueProperty = serialized.FindProperty("catalogue");
            catalogueProperty.arraySize = catalogue.Length;
            for (int i = 0; i < catalogue.Length; i++)
            {
                catalogueProperty.GetArrayElementAtIndex(i).objectReferenceValue = catalogue[i];
            }

            // On part avec le standard en main : c'est le tuyau de la phase 3, et une partie
            // qui n'a jamais visite l'usine se comporte exactement comme avant.
            serialized.FindProperty("currentIndex").intValue = 0;
            serialized.ApplyModifiedPropertiesWithoutUndo();
        }

        /// <summary>
        /// L'atelier des plaques, tel qu'il etait dans la cour de la phase 7 : huit
        /// echantillons poses au sol, chacun avec son nom ecrit juste en dessous, comme les
        /// cartels d'une vitrine.
        ///
        /// Rien n'a change au choix lui-meme ni a la sauvegarde des plaques ; seul le lieu a
        /// change. Les plaques ne bloquent toujours pas le passage : on marche dessus, et
        /// Espace prend celle qu'on foule.
        ///
        /// PLUS DE CARTEL DANS LE DECOR depuis le 4 septembre 2026 : huit noms de cinq sur
        /// sept pixels affiches en meme temps sur du pave ne se lisaient pas. Le nom de la
        /// plaque foulee s'affiche au HUD, un seul a la fois. Voir ItemLabel.
        ///
        /// ManholeFactory vit desormais dans cette scene, comme PipeNetwork vit dans
        /// l'Underground. ManholeCover, reste dans le village, la cherche deja en
        /// FindObjectsInactive.Include : le demenagement ne lui demande rien.
        /// </summary>
        private static void CreateCoverWorkshop(GameObject root)
        {
            InteriorsLayout.Room room = InteriorsLayout.Rooms[0];
            List<Vector2Int> samples = InteriorsLayout.FindAll(room, InteriorsLayout.Cover);
            List<Vector2Int> manholes = VillageLayout.FindAll(VillageLayout.Manhole);

            if (samples.Count != PlaceholderArtGenerator.CoverCount)
            {
                Debug.LogError($"[Sous la Ville] La pièce « {room.Name} » expose " +
                               $"{samples.Count} plaque(s), il en faut " +
                               $"{PlaceholderArtGenerator.CoverCount}.");
                return;
            }

            GameObject parent = new GameObject("Workshop");
            parent.transform.SetParent(root.transform, false);

            ManholeCoverDefinition[] catalogue =
                new ManholeCoverDefinition[PlaceholderArtGenerator.CoverCount];

            for (int i = 0; i < samples.Count; i++)
            {
                catalogue[i] = AssetDatabase.LoadAssetAtPath<ManholeCoverDefinition>(
                    ScriptableObjectSetup.CoverAsset(i));

                if (catalogue[i] == null)
                {
                    Debug.LogError("[Sous la Ville] Plaque introuvable : " +
                                   ScriptableObjectSetup.CoverAsset(i));
                    continue;
                }

                GameObject sample = new GameObject($"Cover_{i + 1:00}_{catalogue[i].DisplayName}");
                sample.transform.SetParent(parent.transform, false);
                sample.transform.position = CellCenter(samples[i]);

                SpriteRenderer view = sample.AddComponent<SpriteRenderer>();
                view.sprite = catalogue[i].Cover;
                SceneBuilderUtility.ApplySortingLayer(view, EntitiesSortingLayer, 0);
            }

            ManholeFactory factory = root.AddComponent<ManholeFactory>();

            SerializedObject serialized = new SerializedObject(factory);
            SetCells(serialized.FindProperty("sampleCells"), samples);
            SetCells(serialized.FindProperty("manholeCells"), manholes);

            SerializedProperty catalogueProperty = serialized.FindProperty("catalogue");
            catalogueProperty.arraySize = catalogue.Length;
            for (int i = 0; i < catalogue.Length; i++)
            {
                catalogueProperty.GetArrayElementAtIndex(i).objectReferenceValue = catalogue[i];
            }

            serialized.ApplyModifiedPropertiesWithoutUndo();
        }

        private static void SetCells(SerializedProperty property, List<Vector2Int> cells)
        {
            property.arraySize = cells.Count;
            for (int i = 0; i < cells.Count; i++)
            {
                property.GetArrayElementAtIndex(i).vector2IntValue = cells[i];
            }
        }

        /// <summary>Centre monde d'une case. La grille est a l'origine, taille de case 1x1.</summary>
        private static Vector3 CellCenter(Vector2Int cell)
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
