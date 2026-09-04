using SousLaVille.Core;
using UnityEditor;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace SousLaVille.EditorTools
{
    /// <summary>
    /// Scene Boot : un seul objet, le Bootstrapper. Elle se decharge d'elle-meme au demarrage.
    /// </summary>
    public static class BootSceneBuilder
    {
        public const string SceneName = "Boot";

        [MenuItem("Sous La Ville/Construire la scène Boot")]
        public static bool Build()
        {
            Scene scene = SceneBuilderUtility.BeginScene();
            if (!scene.IsValid())
            {
                return false;
            }

            GameObject bootstrapper = new GameObject("Bootstrapper");
            bootstrapper.AddComponent<Bootstrapper>();

            SceneBuilderUtility.EndScene(scene, SceneName);
            return true;
        }
    }
}
