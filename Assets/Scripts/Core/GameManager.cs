using SousLaVille.Network;
using SousLaVille.Seasons;
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

        [SerializeField] private GameClock clock;

        [SerializeField] private SeasonSystem seasons;

        [SerializeField] private SaveSystem save;

        /// <summary>Routeur de scenes : chargement additif et bascule surface / sous-sol.</summary>
        public SceneRouter Router => router;

        /// <summary>
        /// Le solveur d'ecoulement. Il vit ici et non dans l'Underground : la couche eteinte
        /// ne repondrait plus aux maisons de la surface.
        /// </summary>
        public FlowSolver Flow => flow;

        /// <summary>L'horloge. Elle compte, et leve un tick a la fin de chaque saison.</summary>
        public GameClock Clock => clock;

        /// <summary>
        /// Les saisons. Elles vivent ici et non dans une couche de jeu : la couche eteinte
        /// ne repondrait plus, et le temps passe des deux cotes de la bouche d'egout.
        /// </summary>
        public SeasonSystem Seasons => seasons;

        /// <summary>
        /// La sauvegarde. Elle n'a ni bouton ni menu : elle ecoute les gestes du joueur et
        /// ecrit toute seule.
        /// </summary>
        public SaveSystem Save => save;

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

            if (clock == null)
            {
                clock = GetComponent<GameClock>();
            }

            if (seasons == null)
            {
                seasons = GetComponent<SeasonSystem>();
            }

            if (save == null)
            {
                save = GetComponent<SaveSystem>();
            }
        }

#if !UNITY_EDITOR && !UNITY_WEBGL
        // Echap ferme le jeu. C'est la seule touche hors des fleches et d'Espace, et elle ne
        // sert qu'a sortir : un enfant de six ans doit pouvoir quitter un plein ecran seul,
        // sans Cmd+Q ni Alt+F4, qui sont des combinaisons.
        //
        // Compilee pour les seuls executables de bureau. Dans l'editeur, Application.Quit ne
        // fait rien ; dans le build web, on ferme l'onglet. Le sondage image par image de
        // Keyboard.current ne pese donc sur aucune des deux plateformes de developpement.
        //
        // La sauvegarde suit toute seule : SaveSystem.OnApplicationQuit ecrit avant la sortie.
        private void Update()
        {
            // PHASE 22 : un mini-jeu ouvert prend Echap pour lui. Sans cette garde, un seul
            // appui refermerait le mini-jeu ET quitterait le jeu, dans un ordre que rien ne
            // garantit. Le mini-jeu passe en premier : c'est l'ecran que le joueur regarde.
            if (SousLaVille.Minigames.MiniGameScreen.AnyOpen)
            {
                return;
            }

            UnityEngine.InputSystem.Keyboard clavier = UnityEngine.InputSystem.Keyboard.current;

            if (clavier != null && clavier.escapeKey.wasPressedThisFrame)
            {
                Application.Quit();
            }
        }
#endif

        private void OnDestroy()
        {
            if (Instance == this)
            {
                Instance = null;
            }
        }
    }
}
