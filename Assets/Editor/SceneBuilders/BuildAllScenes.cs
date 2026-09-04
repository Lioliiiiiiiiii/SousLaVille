using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace SousLaVille.EditorTools
{
    /// <summary>
    /// Regenere les cinq scenes du jeu et remet les Build Settings dans l'ordre.
    ///
    /// Cinq et non quatre depuis la phase 9a : Interiors porte les pieces des batiments.
    /// Ecart explicite aux quatre scenes de CLAUDE.md, accepte le 3 septembre 2026.
    /// </summary>
    public static class BuildAllScenes
    {
        [MenuItem("Sous La Ville/Construire toutes les scènes")]
        public static void BuildAll()
        {
            // L'art doit exister avant tout le reste : Surface reference les assets Tile,
            // Persistent les sprites du personnage, et les saisons leur pictogramme.
            if (!PlaceholderArtGenerator.AreAssetsPresent())
            {
                Debug.LogError("[Sous la Ville] Art placeholder absent. Lance d'abord " +
                               "« Sous La Ville/Générer l'art placeholder ».");
                return;
            }

            // Les ScriptableObjects doivent exister avant les scenes : le reseau du sous-sol
            // reference le type de canalisation pose par le joueur.
            if (!ScriptableObjectSetup.ArePresent())
            {
                Debug.LogError("[Sous la Ville] ScriptableObjects absents. Lance d'abord " +
                               "« Sous La Ville/Créer les ScriptableObjects ».");
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

            // LA CHAINE S'ARRETE AU PREMIER REFUS. Jusqu'a la phase 12a, les cinq Build()
            // etaient void et personne ne lisait leur resultat : un generateur qui refusait
            // laissait les quatre autres se construire, et cette methode annoncait quand meme
            // « les cinq scenes sont construites ». Deux cartes desalignees case pour case,
            // Surface en 64x45 et Underground restee en 40x30, n'auraient rien dit.
            if (!BootSceneBuilder.Build()
                || !PersistentSceneBuilder.Build()
                || !SurfaceSceneBuilder.Build()
                || !UndergroundSceneBuilder.Build()
                || !InteriorsSceneBuilder.Build())
            {
                Debug.LogError("[Sous la Ville] Construction interrompue : une scène a refusé. " +
                               "Les Build Settings n'ont pas été touchés, et le message d'erreur " +
                               "ci-dessus dit laquelle et pourquoi.");
                return;
            }

            SceneBuilderUtility.SetBuildScenes(
                BootSceneBuilder.SceneName,
                PersistentSceneBuilder.SceneName,
                SurfaceSceneBuilder.SceneName,
                UndergroundSceneBuilder.SceneName,
                InteriorsSceneBuilder.SceneName);

            // On repart de Boot : c'est la scene par laquelle on lance le jeu.
            EditorSceneManager.OpenScene($"{SceneBuilderUtility.ScenesFolder}/{BootSceneBuilder.SceneName}.unity");

            Debug.Log("[Sous la Ville] Les cinq scènes sont construites et inscrites au build.");
        }
    }
}
