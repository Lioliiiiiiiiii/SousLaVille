using System.Collections.Generic;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace SousLaVille.EditorTools
{
    /// <summary>
    /// Helpers partages par les generateurs de scenes.
    /// Les scenes sont toujours produites par code : reproductible et versionnable.
    /// </summary>
    public static class SceneBuilderUtility
    {
        public const string ScenesFolder = "Assets/Scenes";

        /// <summary>
        /// Ouvre une scene vide, sans camera ni lumiere par defaut. Rend une scene invalide
        /// si l'utilisateur refuse d'enregistrer son travail en cours.
        /// </summary>
        public static Scene BeginScene()
        {
            if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo())
            {
                return default;
            }

            return EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
        }

        /// <summary>Enregistre la scene sous Assets/Scenes et l'inscrit aux Build Settings.</summary>
        public static void EndScene(Scene scene, string sceneName)
        {
            if (!AssetDatabase.IsValidFolder(ScenesFolder))
            {
                AssetDatabase.CreateFolder("Assets", "Scenes");
            }

            string path = $"{ScenesFolder}/{sceneName}.unity";
            EditorSceneManager.SaveScene(scene, path);
            EnsureInBuildSettings(path);
            Debug.Log($"[Sous la Ville] Scene generee : {path}");
        }

        /// <summary>Remplace la liste des scenes du build par celles fournies, dans l'ordre.</summary>
        public static void SetBuildScenes(params string[] sceneNames)
        {
            List<EditorBuildSettingsScene> scenes = new List<EditorBuildSettingsScene>();

            foreach (string sceneName in sceneNames)
            {
                string path = $"{ScenesFolder}/{sceneName}.unity";
                if (AssetDatabase.LoadAssetAtPath<SceneAsset>(path) == null)
                {
                    Debug.LogWarning($"[Sous la Ville] Scene introuvable, ignoree : {path}");
                    continue;
                }

                scenes.Add(new EditorBuildSettingsScene(path, true));
            }

            EditorBuildSettings.scenes = scenes.ToArray();
        }

        /// <summary>Ajoute une scene aux Build Settings si elle n'y est pas deja.</summary>
        public static void EnsureInBuildSettings(string scenePath)
        {
            foreach (EditorBuildSettingsScene existing in EditorBuildSettings.scenes)
            {
                if (existing.path == scenePath)
                {
                    return;
                }
            }

            List<EditorBuildSettingsScene> scenes = new List<EditorBuildSettingsScene>(EditorBuildSettings.scenes);
            scenes.Add(new EditorBuildSettingsScene(scenePath, true));
            EditorBuildSettings.scenes = scenes.ToArray();
        }
    }
}
