using System;
using SousLaVille.Core;
using SousLaVille.Network;
using UnityEngine;

namespace SousLaVille.Seasons
{
    /// <summary>
    /// La boucle de l'annee. A chaque Tick de l'horloge : on avance d'une saison, on
    /// applique ses effets aux segments, puis on resout l'ecoulement. Dans cet ordre, une
    /// seule fois par saison, jamais par frame.
    ///
    /// C'est le coeur de la rejouabilite : l'hiver gele les tuyaux peu profonds, donc
    /// l'hiver recompense ceux qui ont creuse profond. La regle de profondeur cesse d'etre
    /// une contrainte abstraite, elle devient une lecon qui revient chaque annee.
    ///
    /// Rien n'est jamais perdu : le gel et le bouchon se defont tout seuls ou d'un geste,
    /// l'usure se repare. Aucun echec puni.
    ///
    /// Vit dans Persistent avec les autres services globaux.
    /// </summary>
    public class SeasonSystem : MonoBehaviour
    {
        /// <summary>
        /// « Peu profond », pour les feuilles de l'automne : la profondeur 1, celle que
        /// l'hiver gele. Constante et non champ de saison : c'est la meme frontiere que
        /// celle du gel, et le plan ne decrit qu'un seul reglage de bouchon.
        /// </summary>
        private const int ShallowDepth = 1;

        [Tooltip("Les saisons, dans l'ordre du cycle. La premiere est celle du demarrage.")]
        [SerializeField] private SeasonDefinition[] seasons;

        [Tooltip("L'horloge qui donne le tempo. Cablee par PersistentSceneBuilder.")]
        [SerializeField] private GameClock clock;

        [Tooltip("Le solveur, relance apres chaque changement de saison.")]
        [SerializeField] private FlowSolver flow;

        private PipeNetwork network;
        private int index;

        /// <summary>Leve a chaque changement de saison, et une fois au demarrage.</summary>
        public event Action<SeasonDefinition> SeasonChanged;

        /// <summary>La saison en cours. Nulle si aucune saison n'est cablee.</summary>
        public SeasonDefinition Current
        {
            get
            {
                return seasons != null && seasons.Length > 0 ? seasons[index] : null;
            }
        }

        /// <summary>Rang de la saison dans le cycle. La phase 6 le sauvegardera.</summary>
        public int CurrentIndex => index;

        public int SeasonCount => seasons != null ? seasons.Length : 0;

        /// <summary>
        /// Nombre de segments geles a cet instant. Recalcule a la lecture plutot que mis en
        /// cache : une reparation du joueur doit s'y voir tout de suite. Sert aux
        /// verifications, jamais au jeu, donc jamais par frame.
        /// </summary>
        public int FrozenCount => CountSegments(true);

        /// <summary>Nombre de segments bouches a cet instant. Meme regle que FrozenCount.</summary>
        public int CloggedCount => CountSegments(false);

        private void Awake()
        {
            if (clock == null)
            {
                clock = GetComponent<GameClock>();
            }

            if (flow == null)
            {
                flow = GetComponent<FlowSolver>();
            }
        }

        // Le systeme vit dans Persistent, jamais eteinte : s'abonner ici et se desabonner
        // dans OnDisable suffit, et survit a un rechargement de domaine.
        private void OnEnable()
        {
            if (clock != null)
            {
                clock.Tick += Advance;
            }
        }

        private void OnDisable()
        {
            if (clock != null)
            {
                clock.Tick -= Advance;
            }
        }

        private void Start()
        {
            // La premiere saison n'a pas d'effet a appliquer : elle est deja la au reveil.
            // On previent quand meme les vues, pour qu'elles se mettent a la bonne couleur.
            SeasonChanged?.Invoke(Current);
        }

        /// <summary>
        /// Resolution paresseuse, comme le solveur et les cartes : le systeme vit dans
        /// Persistent, chargee AVANT l'Underground. Chercher une seule fois au Start le
        /// laisserait sans reseau pour toujours. Include : une fois trouve, le reseau reste
        /// valable meme quand sa couche s'eteint.
        /// </summary>
        private void Update()
        {
            if (network == null)
            {
                network = FindAnyObjectByType<PipeNetwork>(FindObjectsInactive.Include);
            }
        }

        /// <summary>
        /// Une saison de plus. Publique : les tests la declenchent sans attendre l'horloge,
        /// et la phase 6 rejouera peut-etre les saisons manquees.
        /// </summary>
        public void Advance()
        {
            if (seasons == null || seasons.Length == 0)
            {
                return;
            }

            index = (index + 1) % seasons.Length;

            ApplySeason(seasons[index]);

            SeasonChanged?.Invoke(seasons[index]);
        }

        /// <summary>
        /// Les effets de la saison sur les segments, puis une resolution. Le solveur tourne
        /// donc quatre fois par cycle, plus une fois par action du joueur.
        /// </summary>
        private void ApplySeason(SeasonDefinition season)
        {
            if (network == null)
            {
                network = FindAnyObjectByType<PipeNetwork>(FindObjectsInactive.Include);
            }

            if (network != null && season != null)
            {
                foreach (PipeSegment segment in network.Segments)
                {
                    ApplyToSegment(season, segment);
                }
            }

            if (flow != null)
            {
                flow.Solve();
            }
        }

        private static void ApplyToSegment(SeasonDefinition season, PipeSegment segment)
        {
            // Un segment est aussi expose que son extremite la moins profonde : c'est par
            // la que le froid et les feuilles entrent.
            int depth = Mathf.Min(segment.NodeA.Depth, segment.NodeB.Depth);

            // 1. Le degel d'abord : le printemps efface l'hiver, sans un geste du joueur.
            if (season.Thaws)
            {
                segment.IsFrozen = false;
            }

            // 2. Le gel. La resistance du type est une probabilite de tenir : 0 gele des le
            // premier hiver, 1 ne gele jamais. C'est ce qui donnera un sens aux types de
            // canalisation, en phase 9.
            if (season.FreezeMaxDepth > 0 && depth <= season.FreezeMaxDepth)
            {
                float resistance = segment.PipeType != null ? segment.PipeType.FrostResistance : 0f;
                if (UnityEngine.Random.value >= resistance)
                {
                    segment.IsFrozen = true;
                }
            }

            // 3. Les feuilles de l'automne, sur les tuyaux peu profonds seulement.
            if (season.ClogChance > 0f && depth <= ShallowDepth
                && UnityEngine.Random.value < season.ClogChance)
            {
                segment.IsClogged = true;
            }

            // 4. L'usure, sur tous les tuyaux. Elle seule demande un geste pour se defaire.
            float wear = segment.PipeType != null ? segment.PipeType.WearPerSeason : 0f;
            segment.Condition -= wear * season.WearMultiplier;
        }

        private int CountSegments(bool frozen)
        {
            if (network == null)
            {
                return 0;
            }

            int count = 0;

            foreach (PipeSegment segment in network.Segments)
            {
                if (frozen ? segment.IsFrozen : segment.IsClogged)
                {
                    count++;
                }
            }

            return count;
        }
    }
}
