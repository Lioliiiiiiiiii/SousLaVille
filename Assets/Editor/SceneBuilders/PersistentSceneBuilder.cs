using SousLaVille.Core;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering.Universal;
using UnityEngine.SceneManagement;

namespace SousLaVille.EditorTools
{
    /// <summary>
    /// Scene Persistent : le GameManager, le SceneRouter et la camera unique.
    /// Jamais dechargee, elle survit a toutes les bascules surface / sous-sol.
    /// </summary>
    public static class PersistentSceneBuilder
    {
        public const string SceneName = "Persistent";

        // Resolution de reference imposee par CLAUDE.md.
        private const int ReferenceWidth = 320;
        private const int ReferenceHeight = 180;
        private const int PixelsPerUnit = 16;

        [MenuItem("Sous La Ville/Construire la scène Persistent")]
        public static void Build()
        {
            Scene scene = SceneBuilderUtility.BeginScene();
            if (!scene.IsValid())
            {
                return;
            }

            CreateGameManager();
            CreateCamera();

            SceneBuilderUtility.EndScene(scene, SceneName);
        }

        private static void CreateGameManager()
        {
            GameObject managerObject = new GameObject("GameManager");

            // RequireComponent ajoute le SceneRouter automatiquement.
            GameManager manager = managerObject.AddComponent<GameManager>();
            SceneRouter router = managerObject.GetComponent<SceneRouter>();

            // Cablage explicite du champ serialise : visible dans l'inspecteur.
            SerializedObject serialized = new SerializedObject(manager);
            serialized.FindProperty("router").objectReferenceValue = router;
            serialized.ApplyModifiedPropertiesWithoutUndo();
        }

        private static void CreateCamera()
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
        }
    }
}
