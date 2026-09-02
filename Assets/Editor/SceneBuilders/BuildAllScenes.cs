using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace SousLaVille.EditorTools
{
    /// <summary>
    /// Regenere les quatre scenes du jeu et remet les Build Settings dans l'ordre.
    /// </summary>
    public static class BuildAllScenes
    {
        [MenuItem("Sous La Ville/Construire toutes les scènes")]
        public static void BuildAll()
        {
            // Les ScriptableObjects doivent exister avant les scenes : le reseau du sous-sol
            // reference le type de canalisation pose par le joueur.
            if (!ScriptableObjectSetup.ArePresent())
            {
                Debug.LogError("[Sous la Ville] ScriptableObjects absents. Lance d'abord " +
                               "« Sous La Ville/Créer les ScriptableObjects ».");
                return;
            }

            // L'art doit exister avant les scenes : Surface reference les assets Tile et
            // Persistent les sprites du personnage.
            if (!PlaceholderArtGenerator.AreAssetsPresent())
            {
                Debug.LogError("[Sous la Ville] Art placeholder absent. Lance d'abord " +
                               "« Sous La Ville/Générer l'art placeholder ».");
                return;
            }

            // Les Sorting Layers doivent exister avant les scenes : les lumieres globales
            // s'y accrochent au moment de leur creation.
            SortingLayerSetup.CreateSortingLayers();
            if (!SortingLayerSetup.AreLayersRegistered())
            {
                Debug.LogError("[Sous la Ville] Sorting Layers non enregistrés. " +
                               "Relance « Sous La Ville/Construire toutes les scènes ».");
                return;
            }

            BootSceneBuilder.Build();
            PersistentSceneBuilder.Build();
            SurfaceSceneBuilder.Build();
            UndergroundSceneBuilder.Build();

            SceneBuilderUtility.SetBuildScenes(
                BootSceneBuilder.SceneName,
                PersistentSceneBuilder.SceneName,
                SurfaceSceneBuilder.SceneName,
                UndergroundSceneBuilder.SceneName);

            // On repart de Boot : c'est la scene par laquelle on lance le jeu.
            EditorSceneManager.OpenScene($"{SceneBuilderUtility.ScenesFolder}/{BootSceneBuilder.SceneName}.unity");

            Debug.Log("[Sous la Ville] Les quatre scènes sont construites et inscrites au build.");
        }
    }
}
