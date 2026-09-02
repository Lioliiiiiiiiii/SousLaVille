using UnityEditor;
using UnityEngine.SceneManagement;

namespace SousLaVille.EditorTools
{
    /// <summary>
    /// Scene Underground : le reseau. Vide en phase 0, la phase 2 y baissera la lumiere.
    /// </summary>
    public static class UndergroundSceneBuilder
    {
        public const string SceneName = "Underground";

        [MenuItem("Sous La Ville/Construire la scène Underground")]
        public static void Build()
        {
            Scene scene = SceneBuilderUtility.BeginScene();
            if (!scene.IsValid())
            {
                return;
            }

            LayerRootBuilder.CreateRoot(SceneName, globalLightIntensity: 1f,
                SortingLayerSetup.UndergroundLayers);

            SceneBuilderUtility.EndScene(scene, SceneName);
        }
    }
}
