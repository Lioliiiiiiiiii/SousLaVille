using SousLaVille.Core;
using SousLaVille.Player;
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

            // Cablage explicite du champ serialise : visible dans l'inspecteur.
            SerializedObject serialized = new SerializedObject(manager);
            serialized.FindProperty("router").objectReferenceValue = router;
            serialized.ApplyModifiedPropertiesWithoutUndo();

            return router;
        }

        private static GameObject CreatePlayer()
        {
            GameObject player = new GameObject("Player");
            player.transform.position = SurfaceSceneBuilder.CellCenter(
                VillageLayout.FindSingle(VillageLayout.PlayerStart));

            SpriteRenderer renderer = player.AddComponent<SpriteRenderer>();
            renderer.sprite = LoadSprite(PlaceholderArtGenerator.PlayerTexture);

            // Le personnage vit dans Persistent alors que son Sorting Layer appartient a la
            // famille Surface. C'est le layer de depart : PlayerController le bascule sur
            // Underground_Entities des qu'il change de couche, sinon le personnage serait
            // noir sous terre.
            SceneBuilderUtility.ApplySortingLayer(renderer, GameSortingLayers.SurfaceEntities, 10);

            player.AddComponent<PlayerController>();
            CreateInteractor(player);

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
