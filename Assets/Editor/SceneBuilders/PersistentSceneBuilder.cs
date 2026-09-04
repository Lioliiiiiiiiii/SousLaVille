using System.Collections.Generic;
using SousLaVille.Core;
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

        [MenuItem("Sous La Ville/Construire la scène Persistent")]
        public static void Build()
        {
            Scene scene = SceneBuilderUtility.BeginScene();
            if (!scene.IsValid())
            {
                return;
            }

            SceneRouter router = CreateGameManager();

            // Un seul personnage pour tout le jeu, dans la scene jamais dechargee. Surface
            // et Underground partagent le meme repere : descendre par une bouche est un
            // simple echange de decor.
            GameObject player = CreatePlayer();
            CreateCamera(player.transform);
            CreateHud(router);

            SceneBuilderUtility.EndScene(scene, SceneName);
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
