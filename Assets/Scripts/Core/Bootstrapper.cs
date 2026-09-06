using System;
using SousLaVille.UI;
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

            // PHASE 20 : le village se choisit AVANT que les couches de jeu ne se chargent.
            //
            // L'ordre n'est pas une commodite. SaveSystem lit son fichier des que la carte et
            // le reseau repondent : si les couches etaient la pendant que l'ecran est ouvert,
            // le village 1 serait charge sous l'ecran de choix, et le village choisi ensuite
            // arriverait par-dessus une partie deja restauree.
            await ChooseVillageAsync(manager);

            await manager.Router.LoadGameplayScenesAsync();

            // Boot n'a plus de raison d'exister. Le discard dit au compilateur que ne pas
            // attendre est voulu : cet objet meurt avec la scene qu'il decharge.
            _ = SceneManager.UnloadSceneAsync(bootScene);
        }

        /// <summary>
        /// Ouvre l'ecran de choix et attend. En sortie, SaveSystem sait quel village il lira.
        ///
        /// Sans ecran dans la scene — une scene Persistent d'avant la phase 20, non
        /// reconstruite — le jeu ne reste pas bloque : il prend le village 1 et le dit en
        /// console. Un filet qui echoue en silence serait pire que pas de filet.
        /// </summary>
        private static async Awaitable ChooseVillageAsync(GameManager manager)
        {
            if (manager.Save == null)
            {
                Debug.LogError("[Sous la Ville] Aucune sauvegarde sur le GameManager.");
                return;
            }

            VillageSelectScreen screen =
                FindAnyObjectByType<VillageSelectScreen>(FindObjectsInactive.Include);

            if (screen == null)
            {
                Debug.LogWarning("[Sous la Ville] Écran de choix absent de la scène Persistent : " +
                                 "le village 1 est pris d'office.");
                manager.Save.ChooseSlot(1);
                return;
            }

            int chosen = 0;
            Action<int> onChosen = slot => chosen = slot;

            screen.Chosen += onChosen;
            screen.Open();

            try
            {
                while (screen.IsOpen)
                {
                    await Awaitable.NextFrameAsync();
                }
            }
            finally
            {
                screen.Chosen -= onChosen;
            }

            manager.Save.ChooseSlot(chosen);
        }
    }
}
