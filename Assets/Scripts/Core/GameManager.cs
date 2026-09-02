using SousLaVille.Network;
using UnityEngine;

namespace SousLaVille.Core
{
    /// <summary>
    /// Point d'entree unique du jeu. Vit dans la scene Persistent, qui n'est jamais dechargee.
    /// Tout le reste passe par lui pour atteindre les systemes globaux.
    /// </summary>
    [DefaultExecutionOrder(-100)]
    [RequireComponent(typeof(SceneRouter))]
    public class GameManager : MonoBehaviour
    {
        /// <summary>Instance unique, disponible des le premier Awake de la scene Persistent.</summary>
        public static GameManager Instance { get; private set; }

        [SerializeField] private SceneRouter router;

        [SerializeField] private FlowSolver flow;

        /// <summary>Routeur de scenes : chargement additif et bascule surface / sous-sol.</summary>
        public SceneRouter Router => router;

        /// <summary>
        /// Le solveur d'ecoulement. Il vit ici et non dans l'Underground : la couche eteinte
        /// ne repondrait plus aux maisons de la surface.
        /// </summary>
        public FlowSolver Flow => flow;

        private void Awake()
        {
            // Garde anti-doublon : si une instance existe deja, ce clone disparait.
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }

            Instance = this;

            // Filet de securite : la scene Persistent n'est jamais dechargee, mais un
            // chargement en mode Single ailleurs dans le code ne doit pas tuer le manager.
            DontDestroyOnLoad(gameObject);

            // 60 fps vises sur un portable modeste, sans dependre de la synchro verticale.
            Application.targetFrameRate = 60;
            QualitySettings.vSyncCount = 0;

            if (router == null)
            {
                router = GetComponent<SceneRouter>();
            }

            if (flow == null)
            {
                flow = GetComponent<FlowSolver>();
            }
        }

        private void OnDestroy()
        {
            if (Instance == this)
            {
                Instance = null;
            }
        }
    }
}
