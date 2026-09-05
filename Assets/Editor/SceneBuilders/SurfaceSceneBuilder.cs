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
            if (!VillageLayout.IsWellFormed() || !VillageLayout.ValidateVillage())
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
            CreateTrees(root);
            CreateSigns(root);
            CreateGuides(root);
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
            Tile park = LoadTile(PlaceholderArtGenerator.TilePark);
            Tile plantFloor = LoadTile(PlaceholderArtGenerator.TilePlantFloor);
            Tile plantWall = LoadTile(PlaceholderArtGenerator.TilePlantWall);
            Tile house = LoadTile(PlaceholderArtGenerator.TileHouse);
            Tile facade = LoadTile(PlaceholderArtGenerator.TileFacade);
            Tile fountainTile = LoadTile(PlaceholderArtGenerator.TileFountain);
            Tile tree = LoadTile(PlaceholderArtGenerator.TileTree);

            // PHASE 12C : seize tuiles par famille, choisies par un masque de raccord, sur le
            // patron exact des canalisations de la phase 3.
            Tile[] roads = new Tile[PlaceholderArtGenerator.DecorMaskCount];
            Tile[] hedges = new Tile[PlaceholderArtGenerator.DecorMaskCount];

            for (int mask = 0; mask < PlaceholderArtGenerator.DecorMaskCount; mask++)
            {
                roads[mask] = LoadTile(PlaceholderArtGenerator.TileRoadMasked(mask));
                hedges[mask] = LoadTile(PlaceholderArtGenerator.TileHedgeMasked(mask));
            }

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
                            groundTiles[index] = roads[GroundMask(x, y, VillageLayout.Road)];
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
                            // De l'herbe sous la haie : sa tuile de raccord se pose dessus.
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
                            : marker == VillageLayout.Tree ? tree
                            : marker == VillageLayout.Facade
                              || marker == VillageLayout.PipeFacade
                              || marker == VillageLayout.SignFacade ? facade
                            : marker == VillageLayout.PlantWall ? plantWall
                            : hedges[BlockingMask(x, y, VillageLayout.Hedge)];
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
        /// Le masque de raccord d'une case de SOL : bit 0 nord, 1 est, 2 sud, 3 ouest, la
        /// convention des canalisations de la phase 3. On interroge GroundAt et non le
        /// marqueur : une bouche d'egout, une porte ou un panneau posent du chemin sous eux,
        /// et la route doit donc les traverser sans se couper.
        ///
        /// Hors carte, on considere qu'il n'y a rien : une route se termine proprement au bord.
        /// </summary>
        private static int GroundMask(int x, int y, char kind)
        {
            int mask = 0;

            if (VillageLayout.GroundAt(x, y + 1) == kind) mask |= 1;
            if (VillageLayout.GroundAt(x + 1, y) == kind) mask |= 2;
            if (VillageLayout.GroundAt(x, y - 1) == kind) mask |= 4;
            if (VillageLayout.GroundAt(x - 1, y) == kind) mask |= 8;

            return mask;
        }

        /// <summary>
        /// Le masque de raccord d'une case BLOQUANTE. Hors carte, At rend Hedge : une haie se
        /// prolonge donc au-dela du bord au lieu de s'y interrompre, ce qui est exactement ce
        /// qu'il faut pour la bordure du village.
        /// </summary>
        private static int BlockingMask(int x, int y, char kind)
        {
            int mask = 0;

            if (VillageLayout.At(x, y + 1) == kind) mask |= 1;
            if (VillageLayout.At(x + 1, y) == kind) mask |= 2;
            if (VillageLayout.At(x, y - 1) == kind) mask |= 4;
            if (VillageLayout.At(x - 1, y) == kind) mask |= 8;

            return mask;
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

        /// <summary>
        /// Les arbres, phase 12c. Ils BLOQUENT : leur tuile est deja posee par PaintVillage,
        /// et l'objet ne porte que le sprite, plus haut que sa case. Rien d'autre : un arbre
        /// n'a aucun etat, aucune action, aucun composant.
        ///
        /// L'ordre de tri suit la ligne : un arbre plante plus bas passe devant celui d'au
        /// dessus, sinon une cime recouvrirait le tronc de son voisin du dessous.
        /// </summary>
        private static void CreateTrees(GameObject root)
        {
            List<Vector2Int> cells = VillageLayout.FindAll(VillageLayout.Tree);
            if (cells.Count == 0)
            {
                return;
            }

            Sprite sprite = LoadSprite(PlaceholderArtGenerator.TreeTexture);

            GameObject parent = new GameObject("Trees");
            parent.transform.SetParent(root.transform, false);

            foreach (Vector2Int cell in cells)
            {
                GameObject tree = new GameObject($"Tree_{cell.x:00}_{cell.y:00}");
                tree.transform.SetParent(parent.transform, false);
                tree.transform.position = CellCenter(cell);

                SpriteRenderer renderer = tree.AddComponent<SpriteRenderer>();
                renderer.sprite = sprite;
                SceneBuilderUtility.ApplySortingLayer(renderer, EntitiesSortingLayer,
                    VillageLayout.Height - cell.y);
            }
        }

        /// <summary>
        /// Les panneaux de rue. Ils ne sont PLUS ecrits dans le plan : VillageLayout.RoadSigns
        /// les DERIVE du graphe des routes selon le Code de la route francais — type, cote de
        /// la chaussee et distance au carrefour compris. Un panneau ne peut donc etre ni mal
        /// place, ni a l'envers, ni imaginaire : il est la parce que la route l'exige.
        ///
        /// Ils ne bloquent pas, et ils se tiennent sur l'herbe au bord de la chaussee, jamais
        /// dessus. Le catalogue vient de PlaceholderArtGenerator et sera REPRIS par l'usine a
        /// panneaux de la phase 13.
        /// </summary>
        private static void CreateSigns(GameObject root)
        {
            List<RoadSign> signs = VillageLayout.RoadSigns();
            if (signs.Count == 0)
            {
                return;
            }

            GameObject parent = new GameObject("Signs");
            parent.transform.SetParent(root.transform, false);

            foreach (RoadSign sign in signs)
            {
                GameObject signObject = new GameObject(
                    $"Sign_{sign.Kind}_{sign.Cell.x:00}_{sign.Cell.y:00}");
                signObject.transform.SetParent(parent.transform, false);
                signObject.transform.position = CellCenter(sign.Cell);

                SpriteRenderer renderer = signObject.AddComponent<SpriteRenderer>();
                renderer.sprite = LoadSprite(PlaceholderArtGenerator.SignTexture((int)sign.Kind));
                SceneBuilderUtility.ApplySortingLayer(renderer, EntitiesSortingLayer,
                    VillageLayout.Height - sign.Cell.y);
            }
        }

        /// <summary>
        /// Quel guide dit quelle lecon, CASE PAR CASE. Un appariement par l'ordre du balayage
        /// de FindAll aurait change de lecon en silence des qu'un poste bouge d'une case ; ici
        /// un poste deplace sans etre reporte ici est REFUSE a la construction.
        /// </summary>
        /// Quatre guides en surface : le but au départ, la bouche, les saisons, la fuite.
        ///
        /// Le guide du but s'est d'abord tenu en (13, 16), DIRECTEMENT SOUS une maison : la
        /// goutte de la maison vit sur Surface_Overlay, un Sorting Layer au-dessus de
        /// Surface_Entities, et recouvrait donc son signal d'attention quel que soit son ordre
        /// de tri. Un signal qu'on ne voit pas n'appelle personne. Il s'est décalé d'une case.
        private static readonly GuideAssignment[] Guides =
        {
            new GuideAssignment(new Vector2Int(12, 16), GuidePost.Lesson.Goal),
            new GuideAssignment(new Vector2Int(10, 11), GuidePost.Lesson.Manhole),
            new GuideAssignment(new Vector2Int(26, 26), GuidePost.Lesson.Seasons),
            new GuideAssignment(new Vector2Int(29, 32), GuidePost.Lesson.Repair)
        };

        /// <summary>Un poste et la lecon qu'il tient.</summary>
        private readonly struct GuideAssignment
        {
            public readonly Vector2Int Cell;
            public readonly GuidePost.Lesson Lesson;

            public GuideAssignment(Vector2Int cell, GuidePost.Lesson lesson)
            {
                Cell = cell;
                Lesson = lesson;
            }
        }


        /// <summary>
        /// Les personnages-guides de la SURFACE, phase 12e. Un par lecon, poste sur une case VALIDEE PAR
        /// CALCUL, qui ne parle que si sa condition est vraie et SE TAIT SANS DISPARAITRE
        /// quand sa lecon est acquise.
        ///
        /// Ils BLOQUENT, et c'est voulu : on ne traverse pas quelqu'un, et surtout, debout SUR
        /// lui on ne pourrait plus lui parler, puisque l'interacteur cherche un personnage sur
        /// la case REGARDEE. Un guide qu'on efface en marchant dessus est pire qu'un guide un
        /// peu mal place.
        ///
        /// Chacun porte un SECOND afficheur, le signal d'attention : un triangle de danger du
        /// vocabulaire routier. Il est pilote par le guide et non par PlayerInteractor, qui
        /// eteint sa bulle des que le joueur regarde ailleurs — or ce signal doit se voir DE
        /// LOIN, sinon il n'appelle personne.
        /// </summary>
        private static void CreateGuides(GameObject root)
        {
            List<Vector2Int> posts = VillageLayout.FindAll(VillageLayout.GuidePost);

            if (posts.Count != Guides.Length)
            {
                Debug.LogError($"[Sous la Ville] Le plan porte {posts.Count} poste(s) de guide " +
                               $"pour {Guides.Length} leçon(s) déclarée(s) : chaque poste doit " +
                               "dire laquelle il tient.");
                return;
            }

            GameObject parent = new GameObject("Guides");
            parent.transform.SetParent(root.transform, false);

            for (int i = 0; i < Guides.Length; i++)
            {
                Vector2Int cell = Guides[i].Cell;
                int lesson = (int)Guides[i].Lesson;

                if (VillageLayout.At(cell.x, cell.y) != VillageLayout.GuidePost)
                {
                    Debug.LogError($"[Sous la Ville] Aucun poste de guide en {cell} : la table " +
                                   "des leçons et le plan ne disent pas la même chose. Un poste " +
                                   "déplacé sans être reporté ici tiendrait une autre leçon, en " +
                                   "silence.");
                    continue;
                }

                GameObject guide = new GameObject($"Guide_{lesson:00}_{cell.x:00}_{cell.y:00}");
                guide.transform.SetParent(parent.transform, false);
                guide.transform.position = SurfaceSceneBuilder.CellCenter(cell);

                SpriteRenderer renderer = guide.AddComponent<SpriteRenderer>();
                renderer.sprite = LoadSprite(PlaceholderArtGenerator.VillagerCraftsman);
                SceneBuilderUtility.ApplySortingLayer(renderer, EntitiesSortingLayer, 5);

                // La bulle « on peut lui parler », au-dessus de SA tete.
                GameObject promptObject = new GameObject("Prompt");
                promptObject.transform.SetParent(guide.transform, false);
                promptObject.transform.localPosition = new Vector3(0f, 1.25f, 0f);

                SpriteRenderer prompt = promptObject.AddComponent<SpriteRenderer>();
                prompt.sprite = LoadSprite(PlaceholderArtGenerator.PictoTalk);
                prompt.enabled = false;
                SceneBuilderUtility.ApplySortingLayer(prompt, EntitiesSortingLayer, 12);

                // LE SECOND AFFICHEUR : le signal d'attention, visible de loin. Ordre 11, entre
                // le personnage et la bulle de l'interacteur.
                GameObject attentionObject = new GameObject("Attention");
                attentionObject.transform.SetParent(guide.transform, false);
                attentionObject.transform.localPosition = new Vector3(0f, 1.9f, 0f);

                SpriteRenderer attention = attentionObject.AddComponent<SpriteRenderer>();
                attention.sprite = LoadSprite(PlaceholderArtGenerator.GuideAttention);
                attention.enabled = false;
                SceneBuilderUtility.ApplySortingLayer(attention, EntitiesSortingLayer, 11);

                Villager villager = guide.AddComponent<Villager>();
                SerializedObject serializedVillager = new SerializedObject(villager);
                serializedVillager.FindProperty("cell").vector2IntValue = cell;
                serializedVillager.FindProperty("prompt").objectReferenceValue = prompt;
                serializedVillager.FindProperty("lines").arraySize = 0;
                serializedVillager.ApplyModifiedPropertiesWithoutUndo();

                GuidePost post = guide.AddComponent<GuidePost>();
                SerializedObject serialized = new SerializedObject(post);
                serialized.FindProperty("lesson").enumValueIndex = lesson;
                serialized.FindProperty("attention").objectReferenceValue = attention;

                string[] sentences = PlaceholderArtGenerator.GuideLines[lesson];
                SerializedProperty lines = serialized.FindProperty("lines");
                lines.arraySize = sentences.Length;

                for (int line = 0; line < sentences.Length; line++)
                {
                    lines.GetArrayElementAtIndex(line).objectReferenceValue =
                        LoadSprite(PlaceholderArtGenerator.GuideLineTexture(lesson, line));
                }

                serialized.ApplyModifiedPropertiesWithoutUndo();
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

            CreatePlantBasins(plant);
        }

        /// <summary>
        /// Les cuves de la station, phase 12d. Une par agrandissement possible, posee une fois
        /// pour toutes sur le sol de l'enceinte et ETEINTE : PlantBasinsView les allume au fur
        /// et a mesure. Rien ne s'instancie en jeu.
        ///
        /// Les emplacements sont les cases de sol de l'INTERIEUR STRICT de l'enceinte, remplies
        /// du fond vers l'avant. Le sol de la station comprend son ouverture, ou un bassin
        /// boucherait visuellement la porte alors qu'il ne bloque rien : on ne pose donc que
        /// sur les cases entierement cernees par la station.
        ///
        /// S'il n'y avait pas assez de place, le builder le DIRAIT au lieu de poser en silence
        /// autant de cuves qu'il peut.
        /// </summary>
        /// <summary>
        /// Les cases ou une cuve peut se poser : le sol de la station dont les QUATRE voisines
        /// appartiennent encore a la station. L'ouverture de l'enceinte en est donc exclue, et
        /// la liste se remplit du fond vers l'avant.
        /// </summary>
        private static List<Vector2Int> PlantBasinSlots()
        {
            List<Vector2Int> slots = new List<Vector2Int>();

            Vector2Int[] steps =
            {
                Vector2Int.up, Vector2Int.right, Vector2Int.down, Vector2Int.left
            };

            foreach (Vector2Int cell in VillageLayout.FindAll(VillageLayout.PlantFloor))
            {
                bool inside = true;

                foreach (Vector2Int step in steps)
                {
                    char neighbour = VillageLayout.At(cell.x + step.x, cell.y + step.y);
                    inside &= neighbour == VillageLayout.PlantFloor
                           || neighbour == VillageLayout.PlantWall
                           || neighbour == VillageLayout.PlantInlet;
                }

                if (inside)
                {
                    slots.Add(cell);
                }
            }

            // Du fond vers l'avant : la station pousse vers le joueur, et non l'inverse.
            slots.Reverse();
            return slots;
        }

        private static void CreatePlantBasins(GameObject plant)
        {
            int wanted = VillageLayout.FindAll(VillageLayout.House).Count
                       + VillageLayout.FindAll(VillageLayout.Fountain).Count;

            List<Vector2Int> floor = PlantBasinSlots();
            if (floor.Count < wanted)
            {
                Debug.LogError($"[Sous la Ville] La station n'a que {floor.Count} case(s) de sol " +
                               $"pour {wanted} bassin(s) : elle ne pourra pas grandir jusqu'au " +
                               "bout. Agrandis son enceinte dans le plan du village.");
                return;
            }

            Sprite sprite = LoadSprite(PlaceholderArtGenerator.PlantBasinTexture);

            GameObject parent = new GameObject("Basins");
            parent.transform.SetParent(plant.transform, false);

            List<SpriteRenderer> renderers = new List<SpriteRenderer>(wanted);

            for (int i = 0; i < wanted; i++)
            {
                GameObject basin = new GameObject($"Basin_{i + 1:00}");
                basin.transform.SetParent(parent.transform, false);
                basin.transform.position = CellCenter(floor[i]);

                SpriteRenderer renderer = basin.AddComponent<SpriteRenderer>();
                renderer.sprite = sprite;
                renderer.enabled = false;          // PlantBasinsView les allume
                SceneBuilderUtility.ApplySortingLayer(renderer, EntitiesSortingLayer, 0);

                renderers.Add(renderer);
            }

            PlantBasinsView view = plant.AddComponent<PlantBasinsView>();
            SerializedObject serialized = new SerializedObject(view);
            SerializedProperty property = serialized.FindProperty("basins");
            property.arraySize = renderers.Count;

            for (int i = 0; i < renderers.Count; i++)
            {
                property.GetArrayElementAtIndex(i).objectReferenceValue = renderers[i];
            }

            serialized.ApplyModifiedPropertiesWithoutUndo();
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
        /// PHASE 12C : son sprite et sa tuile bloquante sont DEUX images. Elles sortaient du
        /// meme fichier de seize sur seize, ce qui la clouait a la taille d'une case ; elle
        /// fait maintenant seize sur vingt-quatre et deborde vers le haut, comme une maison.
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
            renderer.sprite = LoadSprite(PlaceholderArtGenerator.FountainSprite);
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
