using UnityEditor;
using UnityEngine.SceneManagement;

namespace SousLaVille.EditorTools
{
    /// <summary>
    /// Scene Surface : le village. Vide en phase 0, la tilemap arrive en phase 1.
    /// </summary>
    public static class SurfaceSceneBuilder
    {
        public const string SceneName = "Surface";

        [MenuItem("Sous La Ville/Construire la scène Surface")]
        public static void Build()
        {
            Scene scene = SceneBuilderUtility.BeginScene();
            if (!scene.IsValid())
            {
                return;
            }

            LayerRootBuilder.CreateRoot(SceneName, globalLightIntensity: 1f,
                SortingLayerSetup.SurfaceLayers);

            SceneBuilderUtility.EndScene(scene, SceneName);
        }
    }
}
