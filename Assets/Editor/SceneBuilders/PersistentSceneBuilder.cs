using System.Collections.Generic;
using SousLaVille.Core;
using SousLaVille.Minigames;
using SousLaVille.Network;
using SousLaVille.Player;
using SousLaVille.Seasons;
using SousLaVille.UI;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering.Universal;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace SousLaVille.EditorTools
{
    /// <summary>
    /// Scene Persistent : le GameManager, le SceneRouter, la camera unique, le personnage et
    /// le HUD. Jamais dechargee, elle survit a toutes les bascules surface / sous-sol.
    /// </summary>
    public static class PersistentSceneBuilder
    {
        public const string SceneName = "Persistent";

        // Resolution de reference imposee par CLAUDE.md.
        private const int ReferenceWidth = 320;
        private const int ReferenceHeight = 180;
        private const int PixelsPerUnit = 16;

        // Le picto d'action se pose au-dessus de la tete : le sprite du personnage monte a
        // une unite au-dessus de son pivot.
        private static readonly Vector3 PromptOffset = new Vector3(0f, 1.25f, 0f);

        // ------------------------------------------------- Le Stock, le memory de la phase 14
        //
        // Toute la geometrie du plateau tient dans ces six nombres, et ValidateMemoryBoard les
        // rejoue par le calcul a chaque construction. Rien ici n'est libre : voir PLAN-PHASE-14.

        /// <summary>Rangees du plateau. Cinq ne tiennent pas : 5x32 + 4x4 = 176 px pour 156.</summary>
        private const int MemoryRows = 4;

        /// <summary>
        /// Cote d'une carte. C'est le plancher de zone cliquable de CLAUDE.md.
        ///
        /// static readonly et non const : le compilateur replie « 32f &lt; 32f » et signale le
        /// filet comme du code mort, ce qui reviendrait a supprimer la garde pour faire taire
        /// l'avertissement. C'est un reglage, pas une constante de compilation.
        /// </summary>
        private static readonly float MemoryCardSize = 32f;

        /// <summary>Le plancher lui-meme, celui de CLAUDE.md.</summary>
        private const float MinimumTouchSize = 32f;

        private const float MemoryGutter = 4f;

        /// <summary>Le plateau remonte de ceci pour degager la bande du nom, en bas.</summary>
        private const float MemoryBoardOffsetY = 8f;

        /// <summary>Hauteur reservee en bas au nom de la derniere paire trouvee.</summary>
        private const float MemoryBandHeight = 16f;

        /// <summary>Marge minimale autour de ce qu'un ecran modal affiche, de chaque cote.</summary>
        private const float ScreenMargin = 4f;

        /// <summary>Les quatre familles de la planche : intersection, danger, interdiction, obligation.</summary>
        private const int MemoryFamilies = 4;

        /// <summary>
        /// Paires de chaque manche, la derniere tenant ensuite. 8, 12 et 16 sont toutes
        /// divisibles par quatre : chaque manche tire donc 2, 3 puis 4 panneaux PAR FAMILLE, et
        /// les quatre formes sont toujours a l'ecran.
        /// </summary>
        private static readonly int[] MemoryRoundPairs = { 8, 12, 16 };

        /// <summary>Cartes creees d'avance : la plus grande manche. Aucune allocation en jeu.</summary>
        private static int MemoryCardCount
        {
            get
            {
                int most = 0;
                foreach (int pairs in MemoryRoundPairs)
                {
                    most = Mathf.Max(most, pairs);
                }

                return most * 2;
            }
        }

        /// <summary>
        /// LE PLATEAU TIENT-IL DEBOUT ? Prouve par le calcul, avant qu'une seule carte ne soit
        /// creee, et rejoue a chaque construction.
        ///
        /// Un validateur qui dit toujours oui ne vaut rien : celui-ci refuse une carte sous le
        /// plancher de 32, un plateau qui deborde des 320x180, une manche impaire, une manche
        /// qui ne se partage pas entre les quatre familles, et une manche qui demanderait a une
        /// famille plus de panneaux qu'elle n'en porte.
        /// </summary>
        private static bool ValidateMemoryBoard()
        {
            bool ok = true;

            if (MemoryCardSize < MinimumTouchSize)
            {
                Debug.LogError($"[Sous la Ville] Une carte du memory fait {MemoryCardSize} px de " +
                               $"côté : CLAUDE.md impose au moins {MinimumTouchSize} px à la " +
                               "résolution de référence.");
                ok = false;
            }

            float height = MemoryRows * MemoryCardSize + (MemoryRows - 1) * MemoryGutter;
            float roomForHeight = ReferenceHeight - MemoryBandHeight - 2 * ScreenMargin;

            if (height > roomForHeight)
            {
                Debug.LogError($"[Sous la Ville] Le plateau du memory fait {height} px de haut " +
                               $"pour {roomForHeight} disponibles : {MemoryRows} rangées de " +
                               $"{MemoryCardSize} px ne tiennent pas sous la bande du nom.");
                ok = false;
            }

            int slots = PlaceholderArtGenerator.SignBoard.Length;

            if (slots % MemoryFamilies != 0)
            {
                Debug.LogError($"[Sous la Ville] La planche porte {slots} panneau(x) pour " +
                               $"{MemoryFamilies} familles : elles ne sont pas de même taille, " +
                               "et le tirage par famille n'a plus de sens.");
                ok = false;
            }

            foreach (int pairs in MemoryRoundPairs)
            {
                int cards = pairs * 2;

                if (pairs <= 0 || cards % MemoryRows != 0)
                {
                    Debug.LogError($"[Sous la Ville] Une manche de {pairs} paire(s) fait " +
                                   $"{cards} carte(s), qui ne se rangent pas en " +
                                   $"{MemoryRows} rangées pleines.");
                    ok = false;
                    continue;
                }

                int columns = cards / MemoryRows;
                float width = columns * MemoryCardSize + (columns - 1) * MemoryGutter;
                float roomForWidth = ReferenceWidth - 2 * ScreenMargin;

                if (width > roomForWidth)
                {
                    Debug.LogError($"[Sous la Ville] Une manche de {pairs} paire(s) demande " +
                                   $"{columns} colonnes, soit {width} px pour {roomForWidth} " +
                                   "disponibles : le plateau déborde de l'écran.");
                    ok = false;
                }

                if (pairs % MemoryFamilies != 0)
                {
                    Debug.LogError($"[Sous la Ville] Une manche de {pairs} paire(s) ne se " +
                                   $"partage pas entre les {MemoryFamilies} familles : le " +
                                   "tirage en favoriserait une, et la grammaire des formes " +
                                   "n'aurait plus de sens.");
                    ok = false;
                }
                else if (slots % MemoryFamilies == 0 && pairs / MemoryFamilies > slots / MemoryFamilies)
                {
                    Debug.LogError($"[Sous la Ville] Une manche de {pairs} paire(s) demande " +
                                   $"{pairs / MemoryFamilies} panneaux par famille, alors " +
                                   $"qu'une famille n'en porte que {slots / MemoryFamilies}.");
                    ok = false;
                }
            }

            // Le nom de la paire trouvee s'affiche en entier ou il ment.
            foreach (string name in PlaceholderArtGenerator.SignBoardNames)
            {
                int width = PixelFont.WidthOf(name);
                if (width > ReferenceWidth - 2 * ScreenMargin)
                {
                    Debug.LogError($"[Sous la Ville] Le nom « {name} » fait {width} px de large : " +
                                   $"il ne tient pas dans les {ReferenceWidth} px de l'écran.");
                    ok = false;
                }
            }

            return ok;
        }

        // ------------------------------------------------ La Fabrique, le quiz de la phase 15
        //
        // Le panneau agrandi a gauche, trois noms a droite sur des rangees de 32 px. Meme
        // discipline que le memory : six nombres, et ValidateQuizBoard les rejoue par le calcul.

        /// <summary>Hauteur d'une rangee de choix. Le plancher de zone cliquable de CLAUDE.md.</summary>
        private static readonly float QuizRowHeight = 32f;

        /// <summary>Largeur d'une rangee. Le plus long nom fait 193 px ; il y tient avec ses marges.</summary>
        private static readonly float QuizRowWidth = 204f;

        /// <summary>Blanc entre deux rangees.</summary>
        private const float QuizRowGutter = 4f;

        /// <summary>Le panneau est agrandi d'autant par le Canvas, a filtre point : 16x24 devient 48x72.</summary>
        private const int QuizSignScale = 3;

        /// <summary>Blanc entre le panneau et les rangees.</summary>
        private const float QuizGap = 12f;

        /// <summary>Questions par manche. Divisible par les quatre familles : deux par famille.</summary>
        private const int QuizQuestions = 8;

        /// <summary>Cote d'un carre de la jauge, et blanc entre deux.</summary>
        private const float QuizProgressSize = 8f;

        private const float QuizProgressGap = 2f;

        /// <summary>
        /// Leurres de la MEME famille a chaque lancement, la derniere valeur tenant ensuite.
        /// 0 : la grammaire des formes suffit. 1 : un des deux faux noms ressemble. 2 : il faut
        /// lire le pictogramme.
        /// </summary>
        private static readonly int[] QuizSameFamilyLures = { 0, 1, 2 };

        /// <summary>
        /// LE QUIZ TIENT-IL DEBOUT ? Rangee sous le plancher, largeur qui deborde, nom qui ne
        /// tient pas dans sa rangee, questions non partageables entre les familles, plus de
        /// leurres de meme famille qu'il n'y a de choix ou qu'une famille n'en offre : refuse.
        /// </summary>
        private static bool ValidateQuizBoard()
        {
            bool ok = true;

            if (QuizRowHeight < MinimumTouchSize)
            {
                Debug.LogError($"[Sous la Ville] Une rangée du quiz fait {QuizRowHeight} px de haut : " +
                               $"CLAUDE.md impose au moins {MinimumTouchSize} px.");
                ok = false;
            }

            float signWidth = 16f * QuizSignScale;
            float signHeight = 24f * QuizSignScale;
            float width = signWidth + QuizGap + QuizRowWidth;
            float roomForWidth = ReferenceWidth - 2 * ScreenMargin;

            if (width > roomForWidth)
            {
                Debug.LogError($"[Sous la Ville] Le quiz fait {width} px de large pour {roomForWidth} " +
                               "disponibles : panneau, blanc et rangées débordent de l'écran.");
                ok = false;
            }

            float rowsHeight = 3 * QuizRowHeight + 2 * QuizRowGutter;
            float roomForHeight = ReferenceHeight - 2 * ScreenMargin
                                  - 2 * (QuizProgressSize + 2 * ScreenMargin);

            if (rowsHeight > roomForHeight || signHeight > roomForHeight)
            {
                Debug.LogError($"[Sous la Ville] Le quiz fait {Mathf.Max(rowsHeight, signHeight)} px " +
                               $"de haut pour {roomForHeight} disponibles entre la jauge et le bas.");
                ok = false;
            }

            foreach (string name in PlaceholderArtGenerator.SignBoardNames)
            {
                int nameWidth = PixelFont.WidthOf(name);
                if (nameWidth > QuizRowWidth - 2 * ScreenMargin)
                {
                    Debug.LogError($"[Sous la Ville] Le nom « {name} » fait {nameWidth} px : il ne " +
                                   $"tient pas dans une rangée de {QuizRowWidth} px.");
                    ok = false;
                }
            }

            int slots = PlaceholderArtGenerator.SignBoard.Length;
            int perFamily = MemoryFamilies > 0 ? slots / MemoryFamilies : 0;

            if (QuizQuestions % MemoryFamilies != 0 || perFamily == 0
                || QuizQuestions / MemoryFamilies > perFamily)
            {
                Debug.LogError($"[Sous la Ville] {QuizQuestions} question(s) ne se partagent pas " +
                               $"entre {MemoryFamilies} familles de {perFamily} panneaux.");
                ok = false;
            }

            foreach (int lures in QuizSameFamilyLures)
            {
                if (lures >= 0 && lures <= 2 && lures <= perFamily - 1)
                {
                    continue;
                }

                Debug.LogError($"[Sous la Ville] {lures} leurre(s) de la même famille : il n'y a que " +
                               $"deux faux noms par question, et {perFamily - 1} autres panneaux " +
                               "dans une famille.");
                ok = false;
            }

            return ok;
        }

        [MenuItem("Sous La Ville/Construire la scène Persistent")]
        public static bool Build()
        {
            if (!ValidateMemoryBoard() || !ValidateQuizBoard())
            {
                return false;
            }

            Scene scene = SceneBuilderUtility.BeginScene();
            if (!scene.IsValid())
            {
                return false;
            }

            SceneRouter router = CreateGameManager();

            // Un seul personnage pour tout le jeu, dans la scene jamais dechargee. Surface
            // et Underground partagent le meme repere : descendre par une bouche est un
            // simple echange de decor.
            GameObject player = CreatePlayer();
            CreateCamera(player.transform);
            CreateHud(router);

            SceneBuilderUtility.EndScene(scene, SceneName);
            return true;
        }

        private static SceneRouter CreateGameManager()
        {
            GameObject managerObject = new GameObject("GameManager");

            // RequireComponent ajoute le SceneRouter automatiquement.
            GameManager manager = managerObject.AddComponent<GameManager>();
            SceneRouter router = managerObject.GetComponent<SceneRouter>();

            // Le solveur vit ici et non dans l'Underground : la couche eteinte ne pourrait
            // plus repondre aux maisons de la surface.
            FlowSolver flow = managerObject.AddComponent<FlowSolver>();

            // L'horloge et les saisons vivent ici pour la meme raison : le temps passe des
            // deux cotes de la bouche d'egout, et une couche eteinte ne repondrait plus.
            GameClock clock = managerObject.AddComponent<GameClock>();
            SeasonSystem seasons = CreateSeasonSystem(managerObject, clock, flow);

            // La sauvegarde ecoute les gestes du joueur et ecrit toute seule. Elle vit ici
            // parce qu'elle doit survivre aux bascules de couche.
            SaveSystem save = managerObject.AddComponent<SaveSystem>();
            SerializedObject serializedSave = new SerializedObject(save);
            serializedSave.FindProperty("clock").objectReferenceValue = clock;
            serializedSave.FindProperty("seasons").objectReferenceValue = seasons;
            serializedSave.ApplyModifiedPropertiesWithoutUndo();

            // Cablage explicite des champs serialises : visibles dans l'inspecteur.
            SerializedObject serialized = new SerializedObject(manager);
            serialized.FindProperty("router").objectReferenceValue = router;
            serialized.FindProperty("flow").objectReferenceValue = flow;
            serialized.FindProperty("clock").objectReferenceValue = clock;
            serialized.FindProperty("seasons").objectReferenceValue = seasons;
            serialized.FindProperty("save").objectReferenceValue = save;
            serialized.ApplyModifiedPropertiesWithoutUndo();

            return router;
        }

        /// <summary>
        /// Les quatre saisons dans l'ordre du cycle. Elles sont des donnees : changer un
        /// effet ou en ajouter une ne demande pas de toucher au code.
        /// </summary>
        private static SeasonSystem CreateSeasonSystem(GameObject managerObject, GameClock clock,
            FlowSolver flow)
        {
            SeasonSystem seasons = managerObject.AddComponent<SeasonSystem>();

            SerializedObject serialized = new SerializedObject(seasons);
            serialized.FindProperty("clock").objectReferenceValue = clock;
            serialized.FindProperty("flow").objectReferenceValue = flow;

            SerializedProperty cycle = serialized.FindProperty("seasons");
            cycle.arraySize = ScriptableObjectSetup.SeasonCycle.Length;

            for (int i = 0; i < ScriptableObjectSetup.SeasonCycle.Length; i++)
            {
                string path = ScriptableObjectSetup.SeasonCycle[i];
                SeasonDefinition definition = AssetDatabase.LoadAssetAtPath<SeasonDefinition>(path);

                if (definition == null)
                {
                    Debug.LogError($"[Sous la Ville] Saison introuvable : {path}. Lance d'abord " +
                                   "« Sous La Ville/Créer les ScriptableObjects ».");
                }

                cycle.GetArrayElementAtIndex(i).objectReferenceValue = definition;
            }

            serialized.ApplyModifiedPropertiesWithoutUndo();

            return seasons;
        }

        private static GameObject CreatePlayer()
        {
            GameObject player = new GameObject("Player");
            player.transform.position = SurfaceSceneBuilder.CellCenter(
                VillageLayout.FindSingle(VillageLayout.PlayerStart));

            SpriteRenderer renderer = player.AddComponent<SpriteRenderer>();
            renderer.sprite = LoadSprite(PlaceholderArtGenerator.PlayerDown);

            // Le personnage vit dans Persistent alors que son Sorting Layer appartient a la
            // famille Surface. C'est le layer de depart : PlayerController le bascule sur
            // Underground_Entities des qu'il change de couche, sinon le personnage serait
            // noir sous terre.
            SceneBuilderUtility.ApplySortingLayer(renderer, GameSortingLayers.SurfaceEntities, 10);

            PlayerController controller = player.AddComponent<PlayerController>();

            // Quatre sprites, un par direction. Le corps est cable explicitement : le
            // personnage porte aussi le picto et le curseur, GetComponent prendrait le
            // premier venu.
            SerializedObject serialized = new SerializedObject(controller);
            serialized.FindProperty("body").objectReferenceValue = renderer;
            serialized.FindProperty("spriteDown").objectReferenceValue =
                LoadSprite(PlaceholderArtGenerator.PlayerDown);
            serialized.FindProperty("spriteUp").objectReferenceValue =
                LoadSprite(PlaceholderArtGenerator.PlayerUp);
            serialized.FindProperty("spriteLeft").objectReferenceValue =
                LoadSprite(PlaceholderArtGenerator.PlayerLeft);
            serialized.FindProperty("spriteRight").objectReferenceValue =
                LoadSprite(PlaceholderArtGenerator.PlayerRight);
            serialized.ApplyModifiedPropertiesWithoutUndo();

            CreateInteractor(player);
            CreateCursor(player, controller);

            return player;
        }

        /// <summary>Espace, plus le picto d'action affiche au-dessus de la tete.</summary>
        private static void CreateInteractor(GameObject player)
        {
            GameObject promptObject = new GameObject("Prompt");
            promptObject.transform.SetParent(player.transform, false);
            promptObject.transform.localPosition = PromptOffset;

            SpriteRenderer prompt = promptObject.AddComponent<SpriteRenderer>();
            prompt.enabled = false;
            SceneBuilderUtility.ApplySortingLayer(prompt, GameSortingLayers.SurfaceEntities, 11);

            PlayerInteractor interactor = player.AddComponent<PlayerInteractor>();

            SerializedObject serialized = new SerializedObject(interactor);
            serialized.FindProperty("prompt").objectReferenceValue = prompt;
            serialized.FindProperty("promptDown").objectReferenceValue =
                LoadSprite(PlaceholderArtGenerator.PictoDown);
            serialized.FindProperty("promptUp").objectReferenceValue =
                LoadSprite(PlaceholderArtGenerator.PictoUp);
            serialized.FindProperty("promptEnter").objectReferenceValue =
                LoadSprite(PlaceholderArtGenerator.PictoEnter);
            serialized.FindProperty("promptExit").objectReferenceValue =
                LoadSprite(PlaceholderArtGenerator.PictoExit);
            serialized.FindProperty("promptDig").objectReferenceValue =
                LoadSprite(PlaceholderArtGenerator.PictoDig);
            serialized.FindProperty("promptRepair").objectReferenceValue =
                LoadSprite(PlaceholderArtGenerator.PictoRepair);
            serialized.FindProperty("promptRemove").objectReferenceValue =
                LoadSprite(PlaceholderArtGenerator.PictoRemove);
            serialized.FindProperty("promptGrow").objectReferenceValue =
                LoadSprite(PlaceholderArtGenerator.PictoGrow);
            serialized.ApplyModifiedPropertiesWithoutUndo();
        }

        /// <summary>
        /// Le cadre pose sur la case regardee. Sous le personnage dans l'ordre de tri : c'est
        /// une marque au sol, pas un objet.
        /// </summary>
        private static void CreateCursor(GameObject player, PlayerController controller)
        {
            GameObject cursorObject = new GameObject("Cursor");
            cursorObject.transform.SetParent(player.transform, false);

            SpriteRenderer view = cursorObject.AddComponent<SpriteRenderer>();
            view.sprite = LoadSprite(PlaceholderArtGenerator.CursorTarget);
            view.enabled = false;
            SceneBuilderUtility.ApplySortingLayer(view, GameSortingLayers.SurfaceEntities, 9);

            TargetCursor cursor = cursorObject.AddComponent<TargetCursor>();

            SerializedObject serialized = new SerializedObject(cursor);
            serialized.FindProperty("player").objectReferenceValue = controller;
            serialized.FindProperty("view").objectReferenceValue = view;
            serialized.ApplyModifiedPropertiesWithoutUndo();
        }

        private static void CreateCamera(Transform followTarget)
        {
            GameObject cameraObject = new GameObject("Main Camera");
            cameraObject.tag = "MainCamera";
            cameraObject.transform.position = new Vector3(0f, 0f, -10f);

            Camera camera = cameraObject.AddComponent<Camera>();
            camera.orthographic = true;
            camera.clearFlags = CameraClearFlags.SolidColor;
            camera.backgroundColor = new Color32(24, 20, 37, 255);
            camera.nearClipPlane = -100f;
            camera.farClipPlane = 100f;

            // Donnees de camera URP, requises par le Renderer2D.
            cameraObject.AddComponent<UniversalAdditionalCameraData>();

            PixelPerfectCamera pixelPerfect = cameraObject.AddComponent<PixelPerfectCamera>();
            pixelPerfect.assetsPPU = PixelsPerUnit;
            pixelPerfect.refResolutionX = ReferenceWidth;
            pixelPerfect.refResolutionY = ReferenceHeight;
            pixelPerfect.gridSnapping = PixelPerfectCamera.GridSnapping.UpscaleRenderTexture;
            pixelPerfect.cropFrame = PixelPerfectCamera.CropFrame.None;

            cameraObject.AddComponent<AudioListener>();

            CameraFollow follow = cameraObject.AddComponent<CameraFollow>();
            SerializedObject serialized = new SerializedObject(follow);
            serialized.FindProperty("target").objectReferenceValue = followTarget;
            serialized.ApplyModifiedPropertiesWithoutUndo();
        }

        /// <summary>
        /// Le HUD : le voile du fondu et le repere de couche. En Screen Space, donc a l'abri
        /// des Sorting Layers et des lumieres 2D, contrairement a un sprite enfant de la
        /// camera qui devrait changer de famille a chaque bascule.
        ///
        /// Pas d'EventSystem : rien n'est cliquable, le HUD est purement informatif.
        /// </summary>
        private static void CreateHud(SceneRouter router)
        {
            GameObject canvasObject = new GameObject("HUD_Canvas");

            Canvas canvas = canvasObject.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.pixelPerfect = true;
            canvas.sortingOrder = 100;

            CanvasScaler scaler = canvasObject.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(ReferenceWidth, ReferenceHeight);
            scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.Expand;

            Image veil = CreateFullScreenVeil(canvasObject.transform);
            Image icon = CreateLayerIcon(canvasObject.transform);
            Image seasonIcon = CreateSeasonIcon(canvasObject.transform);
            List<Image> drops = CreateHouseDrops(canvasObject.transform);
            CreateVillageMap(canvasObject);
            CreateSignMemory(canvasObject);
            CreateSignQuiz(canvasObject);
            CreateSpeechBox(canvasObject);
            CreateItemLabel(canvasObject);

            ScreenFader fader = canvasObject.AddComponent<ScreenFader>();
            SerializedObject serializedFader = new SerializedObject(fader);
            serializedFader.FindProperty("veil").objectReferenceValue = veil;
            serializedFader.ApplyModifiedPropertiesWithoutUndo();

            LayerIndicator indicator = canvasObject.AddComponent<LayerIndicator>();
            SerializedObject serializedIndicator = new SerializedObject(indicator);
            serializedIndicator.FindProperty("icon").objectReferenceValue = icon;
            serializedIndicator.FindProperty("surfaceIcon").objectReferenceValue =
                LoadSprite(PlaceholderArtGenerator.PictoSurface);
            serializedIndicator.FindProperty("undergroundIcon").objectReferenceValue =
                LoadSprite(PlaceholderArtGenerator.PictoUnderground);
            serializedIndicator.ApplyModifiedPropertiesWithoutUndo();

            SerializedObject serializedRouter = new SerializedObject(router);
            serializedRouter.FindProperty("fader").objectReferenceValue = fader;
            serializedRouter.ApplyModifiedPropertiesWithoutUndo();

            SeasonIndicator seasonIndicator = canvasObject.AddComponent<SeasonIndicator>();
            SerializedObject serializedSeason = new SerializedObject(seasonIndicator);
            serializedSeason.FindProperty("icon").objectReferenceValue = seasonIcon;
            serializedSeason.ApplyModifiedPropertiesWithoutUndo();

            HouseCounter counter = canvasObject.AddComponent<HouseCounter>();

            SerializedObject serializedCounter = new SerializedObject(counter);
            SerializedProperty dropProperty = serializedCounter.FindProperty("drops");
            dropProperty.arraySize = drops.Count;
            for (int i = 0; i < drops.Count; i++)
            {
                dropProperty.GetArrayElementAtIndex(i).objectReferenceValue = drops[i];
            }

            serializedCounter.FindProperty("dropServed").objectReferenceValue =
                LoadSprite(PlaceholderArtGenerator.PictoDropFull);
            serializedCounter.FindProperty("dropIdle").objectReferenceValue =
                LoadSprite(PlaceholderArtGenerator.PictoDropEmpty);
            serializedCounter.ApplyModifiedPropertiesWithoutUndo();
        }

        /// <summary>
        /// Une goutte par maison, en haut a droite. Le seul but affiche du jeu, et il tient
        /// sans un mot.
        /// </summary>
        private static List<Image> CreateHouseDrops(Transform parent)
        {
            const float size = 16f;
            const float margin = 4f;
            const float spacing = 2f;

            // Une goutte par DESTINATION : les cinq maisons plus la fontaine du parc depuis
            // la phase 11. Le solveur les compte ensemble dans ServedCount.
            int count = VillageLayout.FindAll(VillageLayout.House).Count
                      + VillageLayout.FindAll(VillageLayout.Fountain).Count;
            Sprite idle = LoadSprite(PlaceholderArtGenerator.PictoDropEmpty);

            List<Image> drops = new List<Image>(count);

            for (int i = 0; i < count; i++)
            {
                GameObject dropObject = new GameObject($"Drop_{i + 1:00}");
                dropObject.transform.SetParent(parent, false);

                Image drop = dropObject.AddComponent<Image>();
                drop.raycastTarget = false;
                drop.sprite = idle;

                RectTransform rect = drop.rectTransform;
                rect.anchorMin = new Vector2(1f, 1f);
                rect.anchorMax = new Vector2(1f, 1f);
                rect.pivot = new Vector2(1f, 1f);
                rect.sizeDelta = new Vector2(size, size);

                // La rangee est calee sur le coin droit, mais elle se remplit de la gauche
                // vers la droite : la premiere goutte allumee est la plus a gauche, comme on
                // compte sur ses doigts.
                int fromRight = count - 1 - i;
                rect.anchoredPosition = new Vector2(-(margin + fromRight * (size + spacing)), -margin);

                drops.Add(drop);
            }

            return drops;
        }

        private static Image CreateFullScreenVeil(Transform parent)
        {
            GameObject veilObject = new GameObject("Fader");
            veilObject.transform.SetParent(parent, false);

            Image veil = veilObject.AddComponent<Image>();
            veil.color = new Color(0f, 0f, 0f, 0f);
            veil.raycastTarget = false;

            // Eteint tant qu'il n'y a pas de transition : un voile transparent se dessinerait
            // pour rien a chaque image.
            veil.enabled = false;

            RectTransform rect = veil.rectTransform;
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;

            return veil;
        }

        private static Image CreateLayerIcon(Transform parent)
        {
            GameObject iconObject = new GameObject("LayerIndicator");
            iconObject.transform.SetParent(parent, false);

            Image icon = iconObject.AddComponent<Image>();
            icon.raycastTarget = false;
            icon.sprite = LoadSprite(PlaceholderArtGenerator.PictoSurface);

            // Coin haut gauche, marge de 4 px a la resolution de reference. Trente-deux
            // pixels de cote : lisible a 320x180.
            RectTransform rect = icon.rectTransform;
            rect.anchorMin = new Vector2(0f, 1f);
            rect.anchorMax = new Vector2(0f, 1f);
            rect.pivot = new Vector2(0f, 1f);
            rect.sizeDelta = new Vector2(32f, 32f);
            rect.anchoredPosition = new Vector2(4f, -4f);

            return icon;
        }

        /// <summary>
        /// Le picto de saison, juste a droite du repere de couche. Meme taille, meme marge :
        /// deux panneaux cote a cote, ou l'on est et quand on est.
        /// </summary>
        private static Image CreateSeasonIcon(Transform parent)
        {
            GameObject iconObject = new GameObject("SeasonIndicator");
            iconObject.transform.SetParent(parent, false);

            Image icon = iconObject.AddComponent<Image>();
            icon.raycastTarget = false;
            icon.sprite = LoadSprite(PlaceholderArtGenerator.PictoSpring);

            RectTransform rect = icon.rectTransform;
            rect.anchorMin = new Vector2(0f, 1f);
            rect.anchorMax = new Vector2(0f, 1f);
            rect.pivot = new Vector2(0f, 1f);
            rect.sizeDelta = new Vector2(32f, 32f);
            rect.anchoredPosition = new Vector2(40f, -4f);

            return icon;
        }

        /// <summary>
        /// Le plan du village, ouvert le temps de choisir une bouche d'egout. Eteint le reste
        /// du temps : un panneau plein ecran ne se dessine pas pour rien a chaque image.
        ///
        /// Le fond du plan est engendre depuis VillageLayout : il ne peut pas mentir sur le
        /// village, puisqu'il en sort.
        /// </summary>
        private static void CreateVillageMap(GameObject canvasObject)
        {
            const float scale = 4f;

            GameObject panel = new GameObject("VillageMap");
            panel.transform.SetParent(canvasObject.transform, false);

            RectTransform panelRect = panel.AddComponent<RectTransform>();
            panelRect.anchorMin = Vector2.zero;
            panelRect.anchorMax = Vector2.one;
            panelRect.offsetMin = Vector2.zero;
            panelRect.offsetMax = Vector2.zero;

            // Un voile sombre derriere le plan : le village continue d'exister dessous, mais
            // il ne doit pas concurrencer le choix en cours.
            Image veil = panel.AddComponent<Image>();
            veil.color = new Color(0f, 0f, 0f, 0.6f);
            veil.raycastTarget = false;

            GameObject mapObject = new GameObject("Map");
            mapObject.transform.SetParent(panel.transform, false);

            Image mapImage = mapObject.AddComponent<Image>();
            mapImage.raycastTarget = false;
            mapImage.sprite = LoadSprite(PlaceholderArtGenerator.VillageMapTexture);

            RectTransform mapRect = mapImage.rectTransform;
            mapRect.anchorMin = new Vector2(0.5f, 0.5f);
            mapRect.anchorMax = new Vector2(0.5f, 0.5f);
            mapRect.pivot = new Vector2(0.5f, 0.5f);
            mapRect.anchoredPosition = Vector2.zero;
            mapRect.sizeDelta = new Vector2(VillageLayout.Width * scale, VillageLayout.Height * scale);

            // Le cadre du choix, sous les marqueurs dans l'ordre de dessin.
            Image cursor = CreateMapMark(mapObject.transform, "Cursor", 16f);
            cursor.sprite = LoadSprite(PlaceholderArtGenerator.CursorTarget);

            List<Vector2Int> manholes = VillageLayout.FindAll(VillageLayout.Manhole);
            List<Image> markers = new List<Image>(manholes.Count);

            for (int i = 0; i < manholes.Count; i++)
            {
                Image marker = CreateMapMark(mapObject.transform, $"Manhole_{i + 1:00}", 12f);
                marker.sprite = LoadSprite(PlaceholderArtGenerator.ManholeTexture);
                markers.Add(marker);
            }

            VillageMapScreen screen = canvasObject.AddComponent<VillageMapScreen>();

            SerializedObject serialized = new SerializedObject(screen);
            serialized.FindProperty("panel").objectReferenceValue = panel;
            serialized.FindProperty("cursor").objectReferenceValue = cursor;
            serialized.FindProperty("defaultCover").objectReferenceValue =
                LoadSprite(PlaceholderArtGenerator.ManholeTexture);
            serialized.FindProperty("mapSize").vector2IntValue =
                new Vector2Int(VillageLayout.Width, VillageLayout.Height);
            serialized.FindProperty("mapScale").floatValue = scale;

            SerializedProperty markerProperty = serialized.FindProperty("markers");
            markerProperty.arraySize = markers.Count;
            for (int i = 0; i < markers.Count; i++)
            {
                markerProperty.GetArrayElementAtIndex(i).objectReferenceValue = markers[i];
            }

            serialized.ApplyModifiedPropertiesWithoutUndo();

            // Eteint au depart : VillageMapScreen le rallume le temps du choix.
            panel.SetActive(false);
        }

        /// <summary>
        /// LE STOCK, le memory de la phase 14. Il vit dans le HUD comme le plan du village et
        /// la boite de dialogue, cree JUSTE APRES le plan pour passer devant les gouttes et les
        /// indicateurs.
        ///
        /// SON FOND EST OPAQUE, et ce n'est pas de l'esthetique. La rangee de gouttes du HUD
        /// occupe le coin haut-droit de y = 160 a 176, exactement la ou passe la rangee haute du
        /// plateau. Le voile a 0,6 du plan du village y laisserait quatorze gouttes transparaitre
        /// au travers des cartes. C'est le piege de la phase 13 a l'identique, ou la premiere
        /// famille de la planche etait passee derriere cette meme rangee.
        ///
        /// Les trente-deux cartes sont creees D'AVANCE, la plus grande manche : le mini-jeu en
        /// allume ce qu'il lui faut et n'alloue rien en cours de partie.
        /// </summary>
        private static void CreateSignMemory(GameObject canvasObject)
        {
            GameObject panel = new GameObject("SignMemory");
            panel.transform.SetParent(canvasObject.transform, false);

            RectTransform panelRect = panel.AddComponent<RectTransform>();
            panelRect.anchorMin = Vector2.zero;
            panelRect.anchorMax = Vector2.one;
            panelRect.offsetMin = Vector2.zero;
            panelRect.offsetMax = Vector2.zero;

            Image background = panel.AddComponent<Image>();
            background.color = new Color(0.10f, 0.11f, 0.13f, 1f);
            background.raycastTarget = false;

            GameObject boardObject = new GameObject("Board");
            boardObject.transform.SetParent(panel.transform, false);

            RectTransform boardRect = boardObject.AddComponent<RectTransform>();
            boardRect.anchorMin = new Vector2(0.5f, 0.5f);
            boardRect.anchorMax = new Vector2(0.5f, 0.5f);
            boardRect.pivot = new Vector2(0.5f, 0.5f);
            boardRect.anchoredPosition = Vector2.zero;
            boardRect.sizeDelta = Vector2.zero;

            Sprite back = LoadSprite(PlaceholderArtGenerator.SignBackTexture);

            int count = MemoryCardCount;
            List<Image> cards = new List<Image>(count);
            List<Image> faces = new List<Image>(count);

            for (int i = 0; i < count; i++)
            {
                GameObject cardObject = new GameObject($"Card_{i + 1:00}");
                cardObject.transform.SetParent(boardObject.transform, false);

                // Le fond de carte n'a AUCUN sprite : une Image sans sprite est un carre teinte,
                // comme le voile du fondu depuis la phase 2. Le mini-jeu en change la couleur
                // pour dire face cachee, face visible ou paire trouvee.
                Image card = cardObject.AddComponent<Image>();
                card.raycastTarget = false;
                card.rectTransform.anchorMin = new Vector2(0.5f, 0.5f);
                card.rectTransform.anchorMax = new Vector2(0.5f, 0.5f);
                card.rectTransform.pivot = new Vector2(0.5f, 0.5f);
                card.rectTransform.sizeDelta = new Vector2(MemoryCardSize, MemoryCardSize);

                GameObject faceObject = new GameObject("Face");
                faceObject.transform.SetParent(cardObject.transform, false);

                // Le panneau a sa taille exacte, 16 sur 24 : celle qu'il a dans le village et
                // dans la piece. Jamais SetNativeSize, qui multiplierait par 100 / 16.
                Image face = faceObject.AddComponent<Image>();
                face.raycastTarget = false;
                face.sprite = back;
                face.rectTransform.anchorMin = new Vector2(0.5f, 0.5f);
                face.rectTransform.anchorMax = new Vector2(0.5f, 0.5f);
                face.rectTransform.pivot = new Vector2(0.5f, 0.5f);
                face.rectTransform.anchoredPosition = Vector2.zero;
                face.rectTransform.sizeDelta = new Vector2(back.rect.width, back.rect.height);

                cards.Add(card);
                faces.Add(face);
            }

            // Le cadre EN DERNIER, donc dessine par-dessus les cartes : il les cerne sans
            // qu'aucune ne lui passe devant.
            GameObject cursorObject = new GameObject("Cursor");
            cursorObject.transform.SetParent(boardObject.transform, false);

            Image cursor = cursorObject.AddComponent<Image>();
            cursor.raycastTarget = false;
            cursor.sprite = LoadSprite(PlaceholderArtGenerator.PictoCardCursor);
            cursor.rectTransform.anchorMin = new Vector2(0.5f, 0.5f);
            cursor.rectTransform.anchorMax = new Vector2(0.5f, 0.5f);
            cursor.rectTransform.pivot = new Vector2(0.5f, 0.5f);
            cursor.rectTransform.sizeDelta = new Vector2(MemoryCardSize, MemoryCardSize);

            // Le nom de la derniere paire trouvee. Il reprend les vingt-quatre images de la
            // phase 13 : aucun texte neuf n'est dessine pour ce mini-jeu.
            GameObject nameObject = new GameObject("PairName");
            nameObject.transform.SetParent(panel.transform, false);

            Image nameBand = nameObject.AddComponent<Image>();
            nameBand.raycastTarget = false;
            nameBand.enabled = false;
            nameBand.rectTransform.anchorMin = new Vector2(0.5f, 0f);
            nameBand.rectTransform.anchorMax = new Vector2(0.5f, 0f);
            nameBand.rectTransform.pivot = new Vector2(0.5f, 0f);
            nameBand.rectTransform.anchoredPosition = new Vector2(0f, ScreenMargin);

            // LE PICTO DE SORTIE EST CELUI DES BATIMENTS, pas un dessin de plus : refermer un
            // ecran fini et sortir d'une porte sont le meme geste, et il n'y a rien a apprendre.
            GameObject exitObject = new GameObject("ExitPrompt");
            exitObject.transform.SetParent(panel.transform, false);

            Image exitPrompt = exitObject.AddComponent<Image>();
            exitPrompt.raycastTarget = false;
            exitPrompt.enabled = false;
            exitPrompt.sprite = LoadSprite(PlaceholderArtGenerator.PictoExit);
            exitPrompt.rectTransform.anchorMin = new Vector2(1f, 0f);
            exitPrompt.rectTransform.anchorMax = new Vector2(1f, 0f);
            exitPrompt.rectTransform.pivot = new Vector2(1f, 0f);
            exitPrompt.rectTransform.anchoredPosition = new Vector2(-ScreenMargin, ScreenMargin);
            exitPrompt.rectTransform.sizeDelta =
                new Vector2(exitPrompt.sprite.rect.width, exitPrompt.sprite.rect.height);

            SignMemory memory = canvasObject.AddComponent<SignMemory>();

            SerializedObject serialized = new SerializedObject(memory);
            serialized.FindProperty("panel").objectReferenceValue = panel;
            serialized.FindProperty("kind").enumValueIndex = (int)MiniGameKind.Stock;
            serialized.FindProperty("cursor").objectReferenceValue = cursor;
            serialized.FindProperty("nameBand").objectReferenceValue = nameBand;
            serialized.FindProperty("exitPrompt").objectReferenceValue = exitPrompt;
            serialized.FindProperty("back").objectReferenceValue = back;
            serialized.FindProperty("families").intValue = MemoryFamilies;
            serialized.FindProperty("rows").intValue = MemoryRows;
            serialized.FindProperty("cardSize").floatValue = MemoryCardSize;
            serialized.FindProperty("gutter").floatValue = MemoryGutter;
            serialized.FindProperty("boardOffsetY").floatValue = MemoryBoardOffsetY;

            FillArray(serialized.FindProperty("cards"), cards);
            FillArray(serialized.FindProperty("faces"), faces);

            SerializedProperty rounds = serialized.FindProperty("roundPairs");
            rounds.arraySize = MemoryRoundPairs.Length;
            for (int i = 0; i < MemoryRoundPairs.Length; i++)
            {
                rounds.GetArrayElementAtIndex(i).intValue = MemoryRoundPairs[i];
            }

            // LES VINGT-QUATRE PANNEAUX ET LEURS VINGT-QUATRE NOMS, DANS L'ORDRE DE LA PLANCHE,
            // et non dans celui des rangs. Le mini-jeu ne connait que des places de planche : il
            // en deduit la famille par tranches de six, ce qui n'est vrai que dans cet ordre-la.
            int[] board = PlaceholderArtGenerator.SignBoard;
            SerializedProperty signs = serialized.FindProperty("signs");
            SerializedProperty names = serialized.FindProperty("names");
            signs.arraySize = board.Length;
            names.arraySize = board.Length;

            for (int slot = 0; slot < board.Length; slot++)
            {
                signs.GetArrayElementAtIndex(slot).objectReferenceValue =
                    LoadSprite(PlaceholderArtGenerator.SignTexture(board[slot]));
                names.GetArrayElementAtIndex(slot).objectReferenceValue =
                    LoadSprite(PlaceholderArtGenerator.SignNameTexture(board[slot]));
            }

            serialized.ApplyModifiedPropertiesWithoutUndo();

            // Eteint au depart : c'est Le Stock qui le rallume, apres ses deux phrases.
            panel.SetActive(false);
        }

        /// <summary>
        /// LA FABRIQUE, le quiz de la phase 15. Meme HUD, meme fond opaque, meme raison que le
        /// memory. Le panneau a gauche, agrandi trois fois par le Canvas — a filtre point, un
        /// pixel en fait neuf, rien n'est lisse — et trois rangees de choix a droite.
        ///
        /// AUCUNE IMAGE NEUVE : les rangees et la jauge sont des Image teintees sans sprite,
        /// et les noms sont les vingt-quatre de la phase 13.
        /// </summary>
        private static void CreateSignQuiz(GameObject canvasObject)
        {
            GameObject panel = new GameObject("SignQuiz");
            panel.transform.SetParent(canvasObject.transform, false);

            RectTransform panelRect = panel.AddComponent<RectTransform>();
            panelRect.anchorMin = Vector2.zero;
            panelRect.anchorMax = Vector2.one;
            panelRect.offsetMin = Vector2.zero;
            panelRect.offsetMax = Vector2.zero;

            Image background = panel.AddComponent<Image>();
            background.color = new Color(0.10f, 0.11f, 0.13f, 1f);
            background.raycastTarget = false;

            float signWidth = 16f * QuizSignScale;
            float signHeight = 24f * QuizSignScale;
            float contentWidth = signWidth + QuizGap + QuizRowWidth;
            float left = -contentWidth * 0.5f;

            // Le panneau, centre verticalement, a gauche.
            Image sign = CreateCenteredImage(panel.transform, "Sign",
                new Vector2(left + signWidth * 0.5f, 0f), new Vector2(signWidth, signHeight));
            sign.sprite = LoadSprite(PlaceholderArtGenerator.SignTexture(
                PlaceholderArtGenerator.SignBoard[0]));

            // Les trois rangees, de haut en bas, et le nom centre dans chacune.
            float rowsCenterX = left + signWidth + QuizGap + QuizRowWidth * 0.5f;
            float pitch = QuizRowHeight + QuizRowGutter;
            List<Image> rows = new List<Image>(3);
            List<Image> rowNames = new List<Image>(3);

            for (int i = 0; i < 3; i++)
            {
                Image row = CreateCenteredImage(panel.transform, $"Row_{i + 1}",
                    new Vector2(rowsCenterX, (1 - i) * pitch), new Vector2(QuizRowWidth, QuizRowHeight));
                Image name = CreateCenteredImage(row.transform, "Name", Vector2.zero, Vector2.one);
                rows.Add(row);
                rowNames.Add(name);
            }

            // La jauge, en haut : un carre par question.
            float progressWidth = QuizQuestions * QuizProgressSize + (QuizQuestions - 1) * QuizProgressGap;
            float progressY = ReferenceHeight * 0.5f - ScreenMargin - QuizProgressSize * 0.5f;
            List<Image> progress = new List<Image>(QuizQuestions);

            for (int i = 0; i < QuizQuestions; i++)
            {
                float x = -progressWidth * 0.5f + QuizProgressSize * 0.5f
                          + i * (QuizProgressSize + QuizProgressGap);
                progress.Add(CreateCenteredImage(panel.transform, $"Progress_{i + 1}",
                    new Vector2(x, progressY), new Vector2(QuizProgressSize, QuizProgressSize)));
            }

            // Le picto de sortie, celui des batiments, en bas a droite comme au memory.
            GameObject exitObject = new GameObject("ExitPrompt");
            exitObject.transform.SetParent(panel.transform, false);

            Image exitPrompt = exitObject.AddComponent<Image>();
            exitPrompt.raycastTarget = false;
            exitPrompt.enabled = false;
            exitPrompt.sprite = LoadSprite(PlaceholderArtGenerator.PictoExit);
            exitPrompt.rectTransform.anchorMin = new Vector2(1f, 0f);
            exitPrompt.rectTransform.anchorMax = new Vector2(1f, 0f);
            exitPrompt.rectTransform.pivot = new Vector2(1f, 0f);
            exitPrompt.rectTransform.anchoredPosition = new Vector2(-ScreenMargin, ScreenMargin);
            exitPrompt.rectTransform.sizeDelta =
                new Vector2(exitPrompt.sprite.rect.width, exitPrompt.sprite.rect.height);

            SignQuiz quiz = canvasObject.AddComponent<SignQuiz>();

            SerializedObject serialized = new SerializedObject(quiz);
            serialized.FindProperty("panel").objectReferenceValue = panel;
            serialized.FindProperty("kind").enumValueIndex = (int)MiniGameKind.Fabrique;
            serialized.FindProperty("sign").objectReferenceValue = sign;
            serialized.FindProperty("exitPrompt").objectReferenceValue = exitPrompt;
            serialized.FindProperty("families").intValue = MemoryFamilies;
            serialized.FindProperty("questionsPerRound").intValue = QuizQuestions;

            FillArray(serialized.FindProperty("rows"), rows);
            FillArray(serialized.FindProperty("rowNames"), rowNames);
            FillArray(serialized.FindProperty("progress"), progress);

            SerializedProperty lures = serialized.FindProperty("sameFamilyLuresPerRound");
            lures.arraySize = QuizSameFamilyLures.Length;
            for (int i = 0; i < QuizSameFamilyLures.Length; i++)
            {
                lures.GetArrayElementAtIndex(i).intValue = QuizSameFamilyLures[i];
            }

            // Les memes vingt-quatre panneaux et noms que le memory, dans l'ordre de la planche.
            int[] board = PlaceholderArtGenerator.SignBoard;
            SerializedProperty signs = serialized.FindProperty("signs");
            SerializedProperty names = serialized.FindProperty("names");
            signs.arraySize = board.Length;
            names.arraySize = board.Length;

            for (int slot = 0; slot < board.Length; slot++)
            {
                signs.GetArrayElementAtIndex(slot).objectReferenceValue =
                    LoadSprite(PlaceholderArtGenerator.SignTexture(board[slot]));
                names.GetArrayElementAtIndex(slot).objectReferenceValue =
                    LoadSprite(PlaceholderArtGenerator.SignNameTexture(board[slot]));
            }

            serialized.ApplyModifiedPropertiesWithoutUndo();

            // Eteint au depart : c'est La Fabrique qui le rallume, apres ses deux phrases.
            panel.SetActive(false);
        }

        /// <summary>Une Image sans sprite, ancree au centre de son parent, a une position et une taille donnees.</summary>
        private static Image CreateCenteredImage(Transform parent, string name, Vector2 position, Vector2 size)
        {
            GameObject imageObject = new GameObject(name);
            imageObject.transform.SetParent(parent, false);

            Image image = imageObject.AddComponent<Image>();
            image.raycastTarget = false;
            image.rectTransform.anchorMin = new Vector2(0.5f, 0.5f);
            image.rectTransform.anchorMax = new Vector2(0.5f, 0.5f);
            image.rectTransform.pivot = new Vector2(0.5f, 0.5f);
            image.rectTransform.anchoredPosition = position;
            image.rectTransform.sizeDelta = size;
            return image;
        }

        private static void FillArray(SerializedProperty property, List<Image> images)
        {
            property.arraySize = images.Count;
            for (int i = 0; i < images.Count; i++)
            {
                property.GetArrayElementAtIndex(i).objectReferenceValue = images[i];
            }
        }

        /// <summary>
        /// Ce que dit un personnage, en bas de l'ecran. Eteint tant que personne ne parle,
        /// comme le voile du fondu depuis la phase 2 : le HUD ne gagne aucun indicateur
        /// permanent, et un panneau eteint ne se dessine pas.
        ///
        /// Les phrases sont des images dessinees par PixelFont a la generation. Le fond est
        /// une Image sans sprite, simplement teintee, comme le voile : aucune image de plus
        /// a dessiner.
        /// </summary>
        private static void CreateSpeechBox(GameObject canvasObject)
        {
            const float boxHeight = 34f;
            const float margin = 6f;

            GameObject panel = new GameObject("SpeechBox");
            panel.transform.SetParent(canvasObject.transform, false);

            Image background = panel.AddComponent<Image>();
            background.color = new Color(0f, 0f, 0f, 0.78f);
            background.raycastTarget = false;

            RectTransform panelRect = background.rectTransform;
            panelRect.anchorMin = new Vector2(0f, 0f);
            panelRect.anchorMax = new Vector2(1f, 0f);
            panelRect.pivot = new Vector2(0.5f, 0f);
            panelRect.offsetMin = new Vector2(margin, margin);
            panelRect.offsetMax = new Vector2(-margin, margin + boxHeight);

            GameObject lineObject = new GameObject("Line");
            lineObject.transform.SetParent(panel.transform, false);

            Image line = lineObject.AddComponent<Image>();
            line.raycastTarget = false;

            // Une seule phrase a la fois, centree. SpeechBox pose la taille du rectangle en
            // pixels de la resolution de reference : la phrase s'affiche a sa taille exacte,
            // jamais etiree.
            RectTransform lineRect = line.rectTransform;
            lineRect.anchorMin = new Vector2(0.5f, 0.5f);
            lineRect.anchorMax = new Vector2(0.5f, 0.5f);
            lineRect.pivot = new Vector2(0.5f, 0.5f);
            lineRect.anchoredPosition = Vector2.zero;

            SpeechBox box = canvasObject.AddComponent<SpeechBox>();

            SerializedObject serialized = new SerializedObject(box);
            serialized.FindProperty("panel").objectReferenceValue = panel;
            serialized.FindProperty("line").objectReferenceValue = line;
            serialized.ApplyModifiedPropertiesWithoutUndo();

            // Eteint au depart : c'est le personnage qui le rallume, le temps qu'il parle.
            panel.SetActive(false);
        }

        /// <summary>
        /// Le cartel de l'objet foule : son nom, et le picto de la saison qu'il vainc s'il en
        /// a un. Juste au-dessus de la boite de dialogue, pour qu'ils ne se recouvrent jamais.
        ///
        /// Il remplace les noms qui etaient ecrits dans le decor jusqu'au 4 septembre 2026 :
        /// huit noms de cinq sur sept pixels poses sur du pave, tous en meme temps, ne se
        /// lisaient pas. Un seul a la fois, sur un fond uni, se lit.
        /// </summary>
        private static void CreateItemLabel(GameObject canvasObject)
        {
            const float boxHeight = 24f;
            const float margin = 6f;
            const float speechHeight = 34f;

            GameObject panel = new GameObject("ItemLabel");
            panel.transform.SetParent(canvasObject.transform, false);

            Image background = panel.AddComponent<Image>();
            background.color = new Color(0f, 0f, 0f, 0.78f);
            background.raycastTarget = false;

            RectTransform panelRect = background.rectTransform;
            panelRect.anchorMin = new Vector2(0.5f, 0f);
            panelRect.anchorMax = new Vector2(0.5f, 0f);
            panelRect.pivot = new Vector2(0.5f, 0f);
            // La largeur est posee par ItemLabel : la boite epouse son contenu.
            panelRect.sizeDelta = new Vector2(0f, boxHeight);
            panelRect.anchoredPosition = new Vector2(0f, margin * 2f + speechHeight);

            GameObject iconObject = new GameObject("Season");
            iconObject.transform.SetParent(panel.transform, false);

            Image icon = iconObject.AddComponent<Image>();
            icon.raycastTarget = false;
            icon.enabled = false;

            // Picto et nom sont ancres au CENTRE de la boite : ItemLabel les decale lui-meme,
            // puisque lui seul connait la largeur du nom qu'il affiche.
            RectTransform iconRect = icon.rectTransform;
            iconRect.anchorMin = new Vector2(0.5f, 0.5f);
            iconRect.anchorMax = new Vector2(0.5f, 0.5f);
            iconRect.pivot = new Vector2(0.5f, 0.5f);

            GameObject nameObject = new GameObject("Name");
            nameObject.transform.SetParent(panel.transform, false);

            Image name = nameObject.AddComponent<Image>();
            name.raycastTarget = false;

            RectTransform nameRect = name.rectTransform;
            nameRect.anchorMin = new Vector2(0.5f, 0.5f);
            nameRect.anchorMax = new Vector2(0.5f, 0.5f);
            nameRect.pivot = new Vector2(0.5f, 0.5f);

            ItemLabel label = canvasObject.AddComponent<ItemLabel>();

            SerializedObject serialized = new SerializedObject(label);
            serialized.FindProperty("panel").objectReferenceValue = panel;
            serialized.FindProperty("label").objectReferenceValue = name;
            serialized.FindProperty("icon").objectReferenceValue = icon;
            serialized.ApplyModifiedPropertiesWithoutUndo();

            // Eteint au depart : il ne s'allume que sur un objet foule.
            panel.SetActive(false);
        }

        /// <summary>Une marque posee sur le plan, centree sur sa case par le script.</summary>
        private static Image CreateMapMark(Transform parent, string name, float size)
        {
            GameObject markObject = new GameObject(name);
            markObject.transform.SetParent(parent, false);

            Image mark = markObject.AddComponent<Image>();
            mark.raycastTarget = false;

            RectTransform rect = mark.rectTransform;
            rect.anchorMin = new Vector2(0.5f, 0.5f);
            rect.anchorMax = new Vector2(0.5f, 0.5f);
            rect.pivot = new Vector2(0.5f, 0.5f);
            rect.sizeDelta = new Vector2(size, size);

            return mark;
        }

        private static Sprite LoadSprite(string path)
        {
            Sprite sprite = AssetDatabase.LoadAssetAtPath<Sprite>(path);
            if (sprite == null)
            {
                Debug.LogError($"[Sous la Ville] Sprite introuvable : {path}. Lance d'abord " +
                               "« Sous La Ville/Générer l'art placeholder ».");
            }

            return sprite;
        }
    }
}
