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
    /// a SurfaceMap, d'agrandir le village au-dela de sa derniere colonne donc de deplacer ses
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
        public static bool Build()
        {
            if (!ValidateRooms())
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
            CreateSignFactory(root);

            SceneBuilderUtility.EndScene(scene, SceneName);
            return true;
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
        /// Un personnage d'une piece : SA CASE, son sprite et ce qu'il dit.
        ///
        /// APPARIE CASE PAR CASE, jamais par l'ordre d'un balayage ni par le rang de la
        /// piece. Jusqu'a la phase 13, le sprite et les phrases se choisissaient sur
        /// l'indice de la piece : cela ne pouvait marcher que tant qu'il n'y avait qu'un
        /// personnage par piece. Et un appariement par l'ordre ment en silence — c'est la
        /// lecon des huit guides de la phase 12e, ou un poste deplace d'une case aurait
        /// change de lecon sans un mot.
        /// </summary>
        private readonly struct VillagerSpec
        {
            public VillagerSpec(int room, Vector2Int cell, string sprite, string[] lines)
            {
                Room = room;
                Cell = cell;
                Sprite = sprite;
                Lines = lines;
            }

            public int Room { get; }
            public Vector2Int Cell { get; }
            public string Sprite { get; }
            public string[] Lines { get; }
        }

        /// <summary>Rang de l'usine a panneaux dans InteriorsLayout.Rooms.</summary>
        private const int SignFactoryRoom = 2;

        /// <summary>Tous les personnages du jeu, case par case.</summary>
        private static readonly VillagerSpec[] VillagerSpecs = BuildVillagerSpecs();

        private static VillagerSpec[] BuildVillagerSpecs()
        {
            List<VillagerSpec> specs = new List<VillagerSpec>
            {
                new VillagerSpec(0, new Vector2Int(9, 25),
                    PlaceholderArtGenerator.VillagerCraftsman,
                    LinePaths(PlaceholderArtGenerator.CraftsmanLines.Length,
                        PlaceholderArtGenerator.CraftsmanLineTexture)),

                new VillagerSpec(1, new Vector2Int(29, 25),
                    PlaceholderArtGenerator.VillagerWorker,
                    LinePaths(PlaceholderArtGenerator.WorkerLines.Length,
                        PlaceholderArtGenerator.WorkerLineTexture))
            };

            // Les trois de l'usine a panneaux, de gauche a droite : Le Stock, La Fabrique,
            // Le Plan — l'ordre des mini-jeux, du plus simple au plus lourd.
            Vector2Int origin = InteriorsLayout.Rooms[SignFactoryRoom].Origin;
            Vector2Int[] cells =
            {
                origin + new Vector2Int(3, 4),
                origin + new Vector2Int(9, 4),
                origin + new Vector2Int(15, 4)
            };

            for (int who = 0; who < cells.Length; who++)
            {
                int index = who;
                specs.Add(new VillagerSpec(SignFactoryRoom, cells[who],
                    PlaceholderArtGenerator.SignFactoryVillagers[who],
                    LinePaths(PlaceholderArtGenerator.SignFactoryLines[who].Length,
                        line => PlaceholderArtGenerator.SignFactoryLineTexture(index, line))));
            }

            return specs.ToArray();
        }

        private static string[] LinePaths(int count, System.Func<int, string> texture)
        {
            string[] paths = new string[count];
            for (int i = 0; i < count; i++)
            {
                paths[i] = texture(i);
            }

            return paths;
        }

        /// <summary>
        /// Les cases de la planche, DE HAUT EN BAS et de gauche a droite : l'ordre de lecture
        /// de la piece, et celui de PlaceholderArtGenerator.SignBoard.
        ///
        /// Ecrites ici et non lues de FindAll, qui balaye du BAS vers le haut : s'y fier
        /// apparierait la rangee OBLIGATION avec la famille INTERSECTION ET PRIORITE, et
        /// vingt-quatre panneaux sortiraient sous le mauvais nom SANS UN MOT.
        /// </summary>
        private static readonly Vector2Int[] BoardCells = BuildBoardCells();

        private static Vector2Int[] BuildBoardCells()
        {
            int[] rows = { 7, 5, 3, 1 };
            int[] columns = { 4, 6, 8, 10, 12, 14 };
            Vector2Int origin = InteriorsLayout.Rooms[SignFactoryRoom].Origin;

            List<Vector2Int> cells = new List<Vector2Int>();
            foreach (int y in rows)
            {
                foreach (int x in columns)
                {
                    cells.Add(origin + new Vector2Int(x, y));
                }
            }

            return cells.ToArray();
        }

        /// <summary>
        /// Les pieces tiennent-elles debout ? Le plan d'abord — dix lignes de vingt, une
        /// porte, et le nombre de personnages que chaque piece declare —, puis les DEUX
        /// APPARIEMENTS, dans les deux sens.
        ///
        /// Compter ne suffit pas. Un personnage deplace d'une case laisse le compte juste et
        /// changerait de metier ; un panneau deplace d'une case sortirait sous le nom de son
        /// voisin. Ce qui prouve, c'est que l'ensemble des cases du PLAN et l'ensemble des
        /// cases de la TABLE soient exactement le meme, et le validateur le dit dans les deux
        /// sens : une case sans table, une table sans case.
        /// </summary>
        private static bool ValidateRooms()
        {
            if (!InteriorsLayout.IsWellFormed())
            {
                return false;
            }

            bool ok = true;

            // 1. Chaque personnage du plan est dans la table.
            for (int i = 0; i < InteriorsLayout.Rooms.Length; i++)
            {
                InteriorsLayout.Room room = InteriorsLayout.Rooms[i];

                foreach (Vector2Int cell in InteriorsLayout.FindAll(room, InteriorsLayout.Villager))
                {
                    if (SpecIndex(i, cell) >= 0)
                    {
                        continue;
                    }

                    Debug.LogError($"[Sous la Ville] Le personnage {cell} de la pièce " +
                                   $"« {room.Name} » n'est dans aucune table : je ne sais ni à " +
                                   "quoi le faire ressembler, ni ce qu'il doit dire. Un " +
                                   "appariement par l'ordre d'un balayage mentirait en silence.");
                    ok = false;
                }
            }

            // 2. Et chaque entree de la table tombe sur un personnage du plan.
            foreach (VillagerSpec spec in VillagerSpecs)
            {
                if (InteriorsLayout.At(spec.Cell.x, spec.Cell.y) == InteriorsLayout.Villager)
                {
                    continue;
                }

                Debug.LogError($"[Sous la Ville] La table annonce un personnage en {spec.Cell}, " +
                               "mais le plan des intérieurs n'y met aucun « V ».");
                ok = false;
            }

            return ValidateSignBoard() && ok;
        }

        /// <summary>
        /// La planche de l'usine : autant de cases que de rangs, aucun rang deux fois, et les
        /// memes cases des deux cotes.
        ///
        /// « Aucun rang deux fois » n'est pas du zele : au milieu de vingt-quatre panneaux,
        /// un rang recopie passerait inapercu a l'oeil, et le memory de la phase 14 aurait
        /// deux paires identiques sans que rien ne l'ait dit.
        /// </summary>
        private static bool ValidateSignBoard()
        {
            bool ok = true;

            int[] board = PlaceholderArtGenerator.SignBoard;
            string[] names = PlaceholderArtGenerator.SignBoardNames;

            if (board.Length != names.Length)
            {
                Debug.LogError($"[Sous la Ville] La planche porte {board.Length} rang(s) pour " +
                               $"{names.Length} nom(s) : il en faut autant.");
                ok = false;
            }

            HashSet<int> seen = new HashSet<int>();
            foreach (int kind in board)
            {
                if (kind < 0 || kind >= PlaceholderArtGenerator.SignCount)
                {
                    Debug.LogError($"[Sous la Ville] La planche expose le rang de panneau " +
                                   $"{kind}, qui n'existe pas : SignCount vaut " +
                                   $"{PlaceholderArtGenerator.SignCount}.");
                    ok = false;
                }
                else if (!seen.Add(kind))
                {
                    Debug.LogError($"[Sous la Ville] Le rang de panneau {kind} est exposé deux " +
                                   "fois sur la planche : chaque panneau du Code n'y figure " +
                                   "qu'une fois.");
                    ok = false;
                }
            }

            InteriorsLayout.Room room = InteriorsLayout.Rooms[SignFactoryRoom];
            List<Vector2Int> exposed =
                InteriorsLayout.FindAll(room, InteriorsLayout.SignSample);

            if (exposed.Count != board.Length)
            {
                Debug.LogError($"[Sous la Ville] La pièce « {room.Name} » expose " +
                               $"{exposed.Count} panneau(x), il en faut {board.Length}.");
                ok = false;
            }

            HashSet<Vector2Int> planned = new HashSet<Vector2Int>(BoardCells);

            foreach (Vector2Int cell in exposed)
            {
                if (planned.Contains(cell))
                {
                    continue;
                }

                Debug.LogError($"[Sous la Ville] Le panneau {cell} de « {room.Name} » n'est pas " +
                               "une case de la planche : je ne saurais pas lequel y poser.");
                ok = false;
            }

            HashSet<Vector2Int> drawn = new HashSet<Vector2Int>(exposed);

            foreach (Vector2Int cell in BoardCells)
            {
                if (drawn.Contains(cell))
                {
                    continue;
                }

                Debug.LogError($"[Sous la Ville] La planche attend un panneau en {cell}, mais le " +
                               "plan des intérieurs n'y met aucun « S ».");
                ok = false;
            }

            return ok;
        }

        /// <summary>Le rang du personnage attendu sur cette case de cette piece, ou -1.</summary>
        private static int SpecIndex(int room, Vector2Int cell)
        {
            for (int i = 0; i < VillagerSpecs.Length; i++)
            {
                if (VillagerSpecs[i].Room == room && VillagerSpecs[i].Cell == cell)
                {
                    return i;
                }
            }

            return -1;
        }

        /// <summary>
        /// Les personnages, un par entree de la table. Il dit ce qu'on peut faire ici, en
        /// phrases de moins de six mots dessinees par PixelFont a la generation de l'art.
        ///
        /// ValidateRooms a deja prouve que le plan et la table portent exactement les memes
        /// cases : on peut donc creer sans re-verifier.
        /// </summary>
        private static void CreateVillagers(GameObject root)
        {
            GameObject parent = new GameObject("Villagers");
            parent.transform.SetParent(root.transform, false);

            for (int i = 0; i < VillagerSpecs.Length; i++)
            {
                VillagerSpec spec = VillagerSpecs[i];
                InteriorsLayout.Room room = InteriorsLayout.Rooms[spec.Room];

                GameObject villager = new GameObject($"Villager_{i + 1:00}_{room.Name}");
                villager.transform.SetParent(parent.transform, false);
                villager.transform.position = CellCenter(spec.Cell);

                SpriteRenderer renderer = villager.AddComponent<SpriteRenderer>();
                renderer.sprite = LoadSprite(spec.Sprite);

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
                serialized.FindProperty("cell").vector2IntValue = spec.Cell;
                serialized.FindProperty("prompt").objectReferenceValue = prompt;

                SerializedProperty lines = serialized.FindProperty("lines");
                lines.arraySize = spec.Lines.Length;
                for (int line = 0; line < spec.Lines.Length; line++)
                {
                    lines.GetArrayElementAtIndex(line).objectReferenceValue =
                        LoadSprite(spec.Lines[line]);
                }

                serialized.ApplyModifiedPropertiesWithoutUndo();
            }
        }

        /// <summary>
        /// L'USINE A PANNEAUX, phase 13 : vingt-quatre panneaux du Code de la route exposes
        /// en quatre rangees de six, une famille par rangee.
        ///
        /// Comme les plaques et les echantillons de tuyau, ils ne bloquent pas : on marche
        /// dessus, et le nom s'affiche au HUD. Mais contrairement a eux, ON N'EN PREND
        /// AUCUN : rien ne sort de ce batiment. L'usine est une pure recreation, sans lien
        /// avec le reseau, ce qui garde intact le « aucun echec puni » de CLAUDE.md — aucun
        /// mini-jeu ne pourra jamais bloquer la progression.
        ///
        /// Le nom, lui, n'est pas du decor : c'est ce qui fait la difference entre une salle
        /// decoree et un catalogue, et c'est sur lui que La Fabrique s'appuiera en phase 15.
        /// </summary>
        private static void CreateSignFactory(GameObject root)
        {
            GameObject parent = new GameObject("SignFactory");
            parent.transform.SetParent(root.transform, false);

            int[] board = PlaceholderArtGenerator.SignBoard;
            Sprite[] names = new Sprite[board.Length];

            for (int slot = 0; slot < board.Length; slot++)
            {
                int kind = board[slot];
                Vector2Int cell = BoardCells[slot];

                GameObject sign = new GameObject($"Sign_{slot + 1:00}_{kind:00}");
                sign.transform.SetParent(parent.transform, false);
                sign.transform.position = CellCenter(cell);

                SpriteRenderer view = sign.AddComponent<SpriteRenderer>();
                view.sprite = LoadSprite(PlaceholderArtGenerator.SignTexture(kind));
                SceneBuilderUtility.ApplySortingLayer(view, EntitiesSortingLayer, 0);

                names[slot] = LoadSprite(PlaceholderArtGenerator.SignNameTexture(kind));
            }

            SignCatalogue catalogue = root.AddComponent<SignCatalogue>();

            SerializedObject serialized = new SerializedObject(catalogue);
            SetCells(serialized.FindProperty("signCells"), new List<Vector2Int>(BoardCells));

            SerializedProperty nameProperty = serialized.FindProperty("names");
            nameProperty.arraySize = names.Length;
            for (int i = 0; i < names.Length; i++)
            {
                nameProperty.GetArrayElementAtIndex(i).objectReferenceValue = names[i];
            }

            serialized.ApplyModifiedPropertiesWithoutUndo();
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
