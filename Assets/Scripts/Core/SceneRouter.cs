using System;
using SousLaVille.UI;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace SousLaVille.Core
{
    /// <summary>
    /// Charge les scenes de jeu en additif et bascule d'une couche a l'autre : la surface,
    /// le sous-sol, et depuis la phase 9a les interieurs des batiments.
    ///
    /// Les trois couches restent chargees en memoire : la bascule est instantanee et l'etat
    /// du reseau souterrain n'est jamais recharge. Aucune couche n'est citee en dur ici :
    /// ajouter une couche, c'est ajouter une valeur a GameLayer et une scene.
    /// </summary>
    public class SceneRouter : MonoBehaviour
    {
        public const string PersistentSceneName = "Persistent";
        public const string SurfaceSceneName = "Surface";
        public const string UndergroundSceneName = "Underground";
        public const string InteriorsSceneName = "Interiors";

        /// <summary>Les couches de jeu, dans l'ordre de chargement.</summary>
        public static readonly GameLayer[] GameLayers =
        {
            GameLayer.Surface,
            GameLayer.Underground,
            GameLayer.Interior
        };

        [Tooltip("Fondu au noir des transitions. Cable par PersistentSceneBuilder.")]
        [SerializeField] private ScreenFader fader;

        /// <summary>Couche actuellement visible et jouable.</summary>
        public GameLayer CurrentLayer { get; private set; } = GameLayer.Surface;

        /// <summary>Vrai pendant un chargement : empeche deux transitions simultanees.</summary>
        public bool IsBusy { get; private set; }

        /// <summary>Leve a chaque bascule. La phase 2 y branchera le fondu au noir.</summary>
        public event Action<GameLayer> LayerChanged;

        /// <summary>Nom de la scene qui porte une couche donnee.</summary>
        public static string SceneNameFor(GameLayer layer)
        {
            switch (layer)
            {
                case GameLayer.Underground: return UndergroundSceneName;
                case GameLayer.Interior: return InteriorsSceneName;
                default: return SurfaceSceneName;
            }
        }

        /// <summary>
        /// Charge les trois couches en additif, et active la surface.
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
                foreach (GameLayer layer in GameLayers)
                {
                    await LoadSceneIfNeededAsync(SceneNameFor(layer));
                }

                SetActiveLayer(GameLayer.Surface);
            }
            finally
            {
                IsBusy = false;
            }
        }

        /// <summary>
        /// Voyage d'une couche a l'autre : fondu au noir, bascule, puis fondu inverse.
        /// L'action <paramref name="whileBlack"/> est jouee pendant que l'ecran est noir,
        /// une fois la couche cible allumee : c'est la que l'appelant replace son
        /// personnage. Le routeur n'a ainsi jamais besoin de connaitre le joueur.
        /// </summary>
        public async Awaitable TravelAsync(GameLayer target, Action whileBlack = null)
        {
            // Espace martele pendant le fondu ne doit pas empiler deux transitions.
            if (IsBusy)
            {
                return;
            }

            IsBusy = true;
            try
            {
                if (fader != null)
                {
                    await fader.FadeToBlackAsync();
                }

                SetActiveLayer(target);
                whileBlack?.Invoke();

                if (fader != null)
                {
                    await fader.FadeFromBlackAsync();
                }
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
            foreach (GameLayer candidate in GameLayers)
            {
                SetLayerEnabled(candidate, candidate == layer);
            }

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
        /// Indispensable : deux Light2D globales allumees en meme temps cumuleraient leur
        /// eclairage. C'est aussi ce qui garde les interieurs a l'abri des saisons : la
        /// SeasonAmbience de la surface est eteinte pendant qu'on est dans un batiment.
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
