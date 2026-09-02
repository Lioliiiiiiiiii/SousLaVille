using System;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace SousLaVille.Core
{
    /// <summary>
    /// Charge les scenes de jeu en additif et bascule entre la surface et le sous-sol.
    /// Les deux couches restent chargees en memoire : la bascule est instantanee et
    /// l'etat du reseau souterrain n'est jamais recharge.
    /// </summary>
    public class SceneRouter : MonoBehaviour
    {
        public const string PersistentSceneName = "Persistent";
        public const string SurfaceSceneName = "Surface";
        public const string UndergroundSceneName = "Underground";

        /// <summary>Couche actuellement visible et jouable.</summary>
        public GameLayer CurrentLayer { get; private set; } = GameLayer.Surface;

        /// <summary>Vrai pendant un chargement : empeche deux transitions simultanees.</summary>
        public bool IsBusy { get; private set; }

        /// <summary>Leve a chaque bascule. La phase 2 y branchera le fondu au noir.</summary>
        public event Action<GameLayer> LayerChanged;

        /// <summary>Nom de la scene qui porte une couche donnee.</summary>
        public static string SceneNameFor(GameLayer layer)
        {
            return layer == GameLayer.Underground ? UndergroundSceneName : SurfaceSceneName;
        }

        /// <summary>
        /// Charge Surface puis Underground en additif, et active la surface.
        /// Appele une seule fois, au demarrage, par le Bootstrapper.
        /// </summary>
        public async Awaitable LoadGameplayScenesAsync()
        {
            if (IsBusy)
            {
                return;
            }

            IsBusy = true;
            try
            {
                await LoadSceneIfNeededAsync(SurfaceSceneName);
                await LoadSceneIfNeededAsync(UndergroundSceneName);
                SetActiveLayer(GameLayer.Surface);
            }
            finally
            {
                IsBusy = false;
            }
        }

        /// <summary>
        /// Bascule instantanee : la couche cible s'allume, l'autre s'eteint.
        /// </summary>
        public void SetActiveLayer(GameLayer layer)
        {
            SetLayerEnabled(GameLayer.Surface, layer == GameLayer.Surface);
            SetLayerEnabled(GameLayer.Underground, layer == GameLayer.Underground);

            Scene scene = SceneManager.GetSceneByName(SceneNameFor(layer));
            if (scene.isLoaded)
            {
                SceneManager.SetActiveScene(scene);
            }

            CurrentLayer = layer;
            LayerChanged?.Invoke(layer);
        }

        /// <summary>
        /// Decharge une couche. Non utilise pour l'instant : les deux restent residentes.
        /// Disponible si la carte souterraine devient trop lourde.
        /// </summary>
        public async Awaitable UnloadLayerAsync(GameLayer layer)
        {
            Scene scene = SceneManager.GetSceneByName(SceneNameFor(layer));
            if (!scene.isLoaded)
            {
                return;
            }

            AsyncOperation operation = SceneManager.UnloadSceneAsync(scene);
            while (operation != null && !operation.isDone)
            {
                await Awaitable.NextFrameAsync();
            }
        }

        private static async Awaitable LoadSceneIfNeededAsync(string sceneName)
        {
            if (SceneManager.GetSceneByName(sceneName).isLoaded)
            {
                return;
            }

            AsyncOperation operation = SceneManager.LoadSceneAsync(sceneName, LoadSceneMode.Additive);
            if (operation == null)
            {
                Debug.LogError($"[Sous la Ville] Scene absente des Build Settings : {sceneName}");
                return;
            }

            while (!operation.isDone)
            {
                await Awaitable.NextFrameAsync();
            }
        }

        /// <summary>
        /// Allume ou eteint tous les objets racines d'une couche.
        /// Indispensable : deux Light2D globales allumees en meme temps cumuleraient leur eclairage.
        /// </summary>
        private static void SetLayerEnabled(GameLayer layer, bool isEnabled)
        {
            Scene scene = SceneManager.GetSceneByName(SceneNameFor(layer));
            if (!scene.isLoaded)
            {
                return;
            }

            foreach (GameObject root in scene.GetRootGameObjects())
            {
                root.SetActive(isEnabled);
            }
        }
    }
}
