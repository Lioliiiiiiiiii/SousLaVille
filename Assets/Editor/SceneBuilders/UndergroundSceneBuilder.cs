using System.Collections.Generic;
using SousLaVille.Buildings;
using SousLaVille.Core;
using SousLaVille.Network;
using SousLaVille.Seasons;
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

        // Les nombres du bilan de l'eau. La regle, derivee en phase 12 puis confirmee par
        // simulation sur six annees : sur l'annee l'arrivant vaut 4D + R et la station traite
        // 4C, donc C = D + 3 est le minimum entier avec R = 11 (pluies 2/0/8/1). A C = D + 3,
        // Lost vaut zero a toutes les saisons et la suite du bassin retombe sur 5/3/2/0.
        //
        // PHASE 12B : treize destinations, donc SEIZE. Le nombre reste ecrit a la main ici ;
        // c'est la phase 12d qui fera grandir la station en jeu, bassin par bassin, et qui
        // rendra au debordement son role de retour permanent.
        // PHASE 12D : la capacite cesse d'etre un nombre unique. La station part de
        // PlantBaseCapacity et le joueur la porte jusqu'a `destinations + 3` en construisant un
        // bassin par appui sur Espace devant son arrivee. Trois, c'est exactement la pluie
        // annuelle divisee par quatre, arrondie au-dessus : la station livree encaisse la pluie
        // et RIEN d'autre. Chaque maison reliee demande donc son bassin, et la regle se voit en
        // jouant.
        private const int PlantBaseCapacity = 3;
        private const int ReserveCapacity = 10;

        /// <summary>
        /// La capacite que la station peut ATTEINDRE, bassins compris : `D + 3`, la regle
        /// derivee en phase 12 puis confirmee par simulation sur six annees. Elle se deduit du
        /// plan et ne s'ecrit plus a la main : ajouter une maison la releve toute seule.
        /// </summary>
        private static int MaxPlantCapacity()
        {
            int destinations = VillageLayout.FindAll(VillageLayout.House).Count
                             + VillageLayout.FindAll(VillageLayout.Fountain).Count;

            return destinations + 3;
        }

        /// <summary>
        /// L'economie de l'eau tient-elle ? Trois choses peuvent casser, et aucune n'est celle
        /// que cette methode verifiait jusqu'ici.
        ///
        /// LA VERSION DE LA PHASE 12A NE PEUT PLUS DIRE NON. Elle comparait `4D + R` a `4C` ;
        /// depuis la phase 12d, `C` vaut `D + 3` et se DERIVE du plan, donc le traite vaut
        /// `4D + 12` et l'arrivant `4D + 11` : la comparaison est vraie quel que soit le nombre
        /// de maisons. Un validateur qui dit toujours oui ne vaut rien, et celui-ci l'etait
        /// devenu sans que rien ne le dise — c'est le meme piege que le bilan des profondeurs,
        /// qui annoncait « 14 destinations atteignables » sur la ligne suivant neuf refus.
        ///
        /// Ce qui peut reellement casser :
        ///
        /// 1. LA PLUIE PASSE LA MARGE DE LA REGLE. `C = D + 3` laisse `4 x 3 = 12` unites par
        ///    an pour la pluie. Elle en vaut 11 aujourd'hui. Relever une saison d'une seule
        ///    unite rend le village insoutenable POUR TOUJOURS, et c'est un champ serialise sur
        ///    un ScriptableObject, donc modifiable d'un clic.
        /// 2. LE BASSIN N'ENCAISSE PLUS LA POINTE. L'automne apporte `D + pluie d'automne` d'un
        ///    coup ; la station en traite `C`, le bassin doit absorber le reste. Sa capacite est
        ///    ecrite a la main.
        /// 3. LA RANGEE DE GOUTTES DEBORDE DE L'ECRAN au-dela de treize destinations.
        /// </summary>
        private static bool ValidateWaterBudget()
        {
            int destinations = VillageLayout.FindAll(VillageLayout.House).Count
                             + VillageLayout.FindAll(VillageLayout.Fountain).Count;

            int rain = 0;
            int peakRain = 0;

            foreach (string path in ScriptableObjectSetup.SeasonCycle)
            {
                SeasonDefinition season = AssetDatabase.LoadAssetAtPath<SeasonDefinition>(path);
                if (season == null)
                {
                    Debug.LogError($"[Sous la Ville] Saison introuvable : {path}.");
                    return false;
                }

                rain += season.RainVolume;
                peakRain = Mathf.Max(peakRain, season.RainVolume);
            }

            int seasons = ScriptableObjectSetup.SeasonCycle.Length;
            int capacity = MaxPlantCapacity();
            int margin = seasons * (capacity - destinations * SeasonSystem.HouseVolumePerSeason);
            bool ok = true;

            // 1. La marge annuelle que la regle C = D + 3 laisse a la pluie.
            if (rain > margin)
            {
                Debug.LogError($"[Sous la Ville] Pluie INSOUTENABLE : {rain} sur l'année alors " +
                               $"que la règle « capacité = destinations + 3 » n'en laisse que " +
                               $"{margin}. Le bassin dériverait de {rain - margin} par an, " +
                               "saturerait, et le village déborderait pour toujours. Baisse une " +
                               "saison, ou relève la constante 3 de PlantBaseCapacity.");
                ok = false;
            }

            // 2. La pointe d'automne, que le bassin doit pouvoir encaisser d'un seul coup.
            int peak = destinations * SeasonSystem.HouseVolumePerSeason + peakRain - capacity;
            if (peak > ReserveCapacity)
            {
                Debug.LogError($"[Sous la Ville] La pointe de la pire saison vaut {peak} unités " +
                               $"quand le bassin n'en contient que {ReserveCapacity} : il " +
                               "saturerait et le village déborderait même station pleine.");
                ok = false;
            }

            // 3. La rangee de gouttes du HUD : 318 - 18N doit rester au-dessus de 72.
            if (destinations > MaxDrops)
            {
                Debug.LogError($"[Sous la Ville] {destinations} destinations : la rangée de " +
                               $"gouttes en chevauche le picto de saison au-delà de {MaxDrops}.");
                ok = false;
            }

            if (ok)
            {
                Debug.Log($"[Sous la Ville] Bilan de l'eau tenable : {destinations} " +
                          $"destination(s), {rain} de pluie sur l'année pour {margin} de marge, " +
                          $"pointe de {peak} pour un bassin de {ReserveCapacity}. Station de " +
                          $"{PlantBaseCapacity} à {capacity} par saison, soit " +
                          $"{capacity - PlantBaseCapacity} bassin(s) à construire.");
            }

            return ok;
        }

        /// <summary>
        /// Le plafond de la rangee de gouttes du HUD. Une goutte fait 16 px et l'espacement 2,
        /// la marge droite 4 : au-dela de treize, la rangee chevauche le picto de saison.
        /// </summary>
        private const int MaxDrops = 13;

        [MenuItem("Sous La Ville/Construire la scène Underground")]
        public static bool Build()
        {
            if (!UndergroundLayout.IsWellFormed() || !VillageLayout.IsWellFormed())
            {
                return false;
            }

            // Un portail mal aligne ne planterait pas, il serait juste mort : on refuse de
            // construire plutot que de livrer une bouche qui ne mene nulle part.
            if (!UndergroundLayout.ValidateAgainstVillage())
            {
                return false;
            }

            if (!PlaceholderArtGenerator.AreAssetsPresent())
            {
                Debug.LogError("[Sous la Ville] Art placeholder absent. Lance d'abord " +
                               "« Sous La Ville/Générer l'art placeholder ».");
                return false;
            }

            if (!ScriptableObjectSetup.ArePresent())
            {
                Debug.LogError("[Sous la Ville] ScriptableObjects absents. Lance d'abord " +
                               "« Sous La Ville/Créer les ScriptableObjects ».");
                return false;
            }

            if (!ValidateWaterBudget())
            {
                return false;
            }

            Scene scene = SceneBuilderUtility.BeginScene();
            if (!scene.IsValid())
            {
                return false;
            }

            GameObject root = LayerRootBuilder.CreateRoot(SceneName, GlobalLightIntensity,
                GameSortingLayers.Underground);

            Tilemap ground;
            Tilemap blocking;
            Tilemap pipes;
            Grid grid = CreateGrid(root, out ground, out blocking, out pipes);

            PaintUnderground(ground, blocking);
            CreateLadders(root);
            CreateSigns(root);
            CreateGuides(root);
            CreateHouseInlets(root);
            CreateReserve(root);

            UndergroundMap map = AttachUndergroundMap(root, grid, ground, blocking);
            AttachNetwork(root, map, pipes);

            SceneBuilderUtility.EndScene(scene, SceneName);
            return true;
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
                serialized.FindProperty("baseCapacity").intValue = PlantBaseCapacity;
                serialized.FindProperty("maxCapacity").intValue = MaxPlantCapacity();
                serialized.ApplyModifiedPropertiesWithoutUndo();
            }
        }

        /// <summary>
        /// Les panneaux de carrefour, phase 12c. Le sous-sol n'avait AUCUN repere : trois
        /// nuances de brun, des galeries qui se ressemblent toutes, et une carte passee de
        /// 1200 a 2880 cases. LayerIndicator porte encore le commentaire « pas de mini-carte,
        /// elle serait vide de sens tant que le reseau n'existe pas » : c'est vrai d'une carte,
        /// pas d'un panneau.
        ///
        /// La fleche montre LA STATION, et elle est calculee ici, une fois, depuis le plan :
        /// l'axe dominant vers l'arrivee. Un panneau de direction ne dit pas le chemin, il dit
        /// la direction — c'est exactement ce que fait un vrai panneau, et c'est ce qui laisse
        /// le puzzle des profondeurs entier.
        ///
        /// Ils ne bloquent pas : ils sont poses dans des galeries deja creusees, et le sol y
        /// est peint comme partout ailleurs.
        /// </summary>
        private static void CreateSigns(GameObject root)
        {
            List<Vector2Int> cells = UndergroundLayout.FindAll(UndergroundLayout.Sign);
            if (cells.Count == 0)
            {
                return;
            }

            List<Vector2Int> plants = UndergroundLayout.FindAll(UndergroundLayout.PlantOutlet);
            if (plants.Count != 1)
            {
                return;
            }

            Vector2Int plant = plants[0];

            GameObject parent = new GameObject("Signs");
            parent.transform.SetParent(root.transform, false);

            foreach (Vector2Int cell in cells)
            {
                Vector2Int delta = plant - cell;

                // 0 nord, 1 est, 2 sud, 3 ouest : le meme ordre que les bits du masque de
                // raccord, pour n'avoir qu'une convention de direction dans tout le projet.
                int arrow = Mathf.Abs(delta.x) >= Mathf.Abs(delta.y)
                    ? (delta.x >= 0 ? 1 : 3)
                    : (delta.y >= 0 ? 0 : 2);

                GameObject sign = new GameObject($"Sign_{cell.x:00}_{cell.y:00}");
                sign.transform.SetParent(parent.transform, false);
                sign.transform.position = SurfaceSceneBuilder.CellCenter(cell);

                SpriteRenderer renderer = sign.AddComponent<SpriteRenderer>();
                renderer.sprite = LoadSprite(PlaceholderArtGenerator.SignTexture(
                    PlaceholderArtGenerator.SignFirstArrow + arrow));
                SceneBuilderUtility.ApplySortingLayer(renderer,
                    GameSortingLayers.UndergroundEntities, UndergroundLayout.Height - cell.y);
            }
        }

        /// <summary>
        /// Quel guide dit quelle lecon, CASE PAR CASE. Un appariement par l'ordre du balayage
        /// de FindAll aurait change de lecon en silence des qu'un poste bouge d'une case ; ici
        /// un poste deplace sans etre reporte ici est REFUSE a la construction.
        /// </summary>
        /// Quatre guides sous terre : creuser, poser, LA PROFONDEUR, et le bassin.
        private static readonly GuideAssignment[] Guides =
        {
            new GuideAssignment(new Vector2Int(11, 10), GuidePost.Lesson.Dig),
            new GuideAssignment(new Vector2Int(5, 22), GuidePost.Lesson.PlacePipe),
            new GuideAssignment(new Vector2Int(39, 26), GuidePost.Lesson.Depth),
            new GuideAssignment(new Vector2Int(11, 16), GuidePost.Lesson.Reserve)
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
        /// Les personnages-guides du SOUS-SOL, phase 12e. Un par lecon, poste sur une case VALIDEE PAR
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
            List<Vector2Int> posts = UndergroundLayout.FindAll(UndergroundLayout.GuidePost);

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

                if (UndergroundLayout.At(cell.x, cell.y) != UndergroundLayout.GuidePost)
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
                SceneBuilderUtility.ApplySortingLayer(renderer, GameSortingLayers.UndergroundEntities, 5);

                // La bulle « on peut lui parler », au-dessus de SA tete.
                GameObject promptObject = new GameObject("Prompt");
                promptObject.transform.SetParent(guide.transform, false);
                promptObject.transform.localPosition = new Vector3(0f, 1.25f, 0f);

                SpriteRenderer prompt = promptObject.AddComponent<SpriteRenderer>();
                prompt.sprite = LoadSprite(PlaceholderArtGenerator.PictoTalk);
                prompt.enabled = false;
                SceneBuilderUtility.ApplySortingLayer(prompt, GameSortingLayers.UndergroundEntities, 12);

                // LE SECOND AFFICHEUR : le signal d'attention, visible de loin. Ordre 11, entre
                // le personnage et la bulle de l'interacteur.
                GameObject attentionObject = new GameObject("Attention");
                attentionObject.transform.SetParent(guide.transform, false);
                attentionObject.transform.localPosition = new Vector3(0f, 1.9f, 0f);

                SpriteRenderer attention = attentionObject.AddComponent<SpriteRenderer>();
                attention.sprite = LoadSprite(PlaceholderArtGenerator.GuideAttention);
                attention.enabled = false;
                SceneBuilderUtility.ApplySortingLayer(attention, GameSortingLayers.UndergroundEntities, 11);

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
