using UnityEngine;
using UnityEngine.SceneManagement;

namespace SousLaVille.Core
{
    /// <summary>
    /// Seul composant de la scene Boot. Charge Persistent, lance le chargement des
    /// couches de jeu, puis se decharge lui-meme. Le joueur ne voit jamais cette scene.
    /// </summary>
    public class Bootstrapper : MonoBehaviour
    {
        private async void Start()
        {
            Scene bootScene = gameObject.scene;

            // Persistent porte le GameManager et la camera : elle vient en premier.
            if (!SceneManager.GetSceneByName(SceneRouter.PersistentSceneName).isLoaded)
            {
                AsyncOperation operation = SceneManager.LoadSceneAsync(
                    SceneRouter.PersistentSceneName, LoadSceneMode.Additive);

                if (operation == null)
                {
                    Debug.LogError("[Sous la Ville] Scene Persistent absente des Build Settings.");
                    return;
                }

                while (!operation.isDone)
                {
                    await Awaitable.NextFrameAsync();
                }
            }

            // Le GameManager s'enregistre dans son Awake : il est pret des la scene chargee.
            GameManager manager = GameManager.Instance;
            if (manager == null || manager.Router == null)
            {
                Debug.LogError("[Sous la Ville] GameManager introuvable dans la scene Persistent.");
                return;
            }

            await manager.Router.LoadGameplayScenesAsync();

            // Boot n'a plus de raison d'exister. On ne l'attend pas : cet objet meurt avec elle.
            SceneManager.UnloadSceneAsync(bootScene);
        }
    }
}
