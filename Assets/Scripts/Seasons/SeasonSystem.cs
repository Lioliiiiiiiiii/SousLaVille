using System;
using SousLaVille.Buildings;
using SousLaVille.Core;
using SousLaVille.Network;
using UnityEngine;

namespace SousLaVille.Seasons
{
    /// <summary>
    /// La boucle de l'annee. A chaque Tick de l'horloge : on avance d'une saison, on
    /// applique ses effets aux segments, on resout l'ecoulement, puis on fait le bilan de
    /// l'eau. Dans cet ordre, une seule fois par saison, jamais par frame.
    ///
    /// Le bilan de l'eau est quatre additions par saison, pas une simulation de fluide :
    /// ce qui arrive, ce que la station traite, ce que le bassin encaisse ou relache, ce
    /// qui se perd. Il s'accroche juste apres la resolution, parce qu'il a besoin de
    /// savoir quelles maisons sont desservies et si le bassin a une route.
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

        /// <summary>Ce qu'une maison desservie envoie au reseau par saison : ses eaux usees.</summary>
        public const int HouseVolumePerSeason = 1;

        /// <summary>
        /// Le bilan d'une saison, en unites d'eau. Cinq entiers, poses une fois par tick.
        /// Surplus et marge ne sont jamais tous les deux non nuls : une saison remplit ou
        /// vide le bassin, jamais les deux.
        /// </summary>
        public struct WaterBudget
        {
            /// <summary>Maisons desservies plus pluie de la saison.</summary>
            public int Inflow;

            /// <summary>Ce que la station a traite : au plus sa capacite.</summary>
            public int Treated;

            /// <summary>Ce que le bassin a encaisse du surplus.</summary>
            public int Absorbed;

            /// <summary>Ce que le bassin a relache dans la marge de la station.</summary>
            public int Released;

            /// <summary>Le surplus que rien n'a retenu. Il disparait sans un mot : phase 10.</summary>
            public int Lost;
        }

        [Tooltip("Les saisons, dans l'ordre du cycle. La premiere est celle du demarrage.")]
        [SerializeField] private SeasonDefinition[] seasons;

        [Tooltip("L'horloge qui donne le tempo. Cablee par PersistentSceneBuilder.")]
        [SerializeField] private GameClock clock;

        [Tooltip("Le solveur, relance apres chaque changement de saison.")]
        [SerializeField] private FlowSolver flow;

        private PipeNetwork network;
        private WaterReserve reserve;
        private TreatmentPlant plant;
        private int index;

        /// <summary>Le dernier bilan de l'eau. Sert aux verifications et, en phase 10, au debordement.</summary>
        public WaterBudget LastBudget { get; private set; }

        /// <summary>Nombre de bilans depuis le demarrage. Doit suivre exactement le nombre de saisons.</summary>
        public int BudgetCount { get; private set; }

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
            ResolveSceneObjects();
        }

        /// <summary>Retente tant que ca manque, puis plus jamais. Le bassin et la station vivent avec le reseau.</summary>
        private void ResolveSceneObjects()
        {
            if (network == null)
            {
                network = FindAnyObjectByType<PipeNetwork>(FindObjectsInactive.Include);
            }

            if (reserve == null)
            {
                reserve = FindAnyObjectByType<WaterReserve>(FindObjectsInactive.Include);
            }

            if (plant == null)
            {
                plant = FindAnyObjectByType<TreatmentPlant>(FindObjectsInactive.Include);
            }
        }

        /// <summary>
        /// Pose la saison lue dans une sauvegarde, SANS appliquer ses effets : charger une
        /// partie en hiver ne doit pas regeler le reseau une deuxieme fois, il l'est deja.
        /// Les vues sont prevenues pour se mettre a la bonne couleur.
        /// </summary>
        public void Restore(int seasonIndex)
        {
            if (seasons == null || seasons.Length == 0)
            {
                return;
            }

            index = Mathf.Clamp(seasonIndex, 0, seasons.Length - 1);
            SeasonChanged?.Invoke(Current);
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
        /// Les effets de la saison sur les segments, une resolution, puis le bilan de
        /// l'eau. Le solveur tourne donc quatre fois par cycle, plus une fois par action du
        /// joueur ; le bilan, lui, ne tourne qu'ici.
        /// </summary>
        private void ApplySeason(SeasonDefinition season)
        {
            ResolveSceneObjects();

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

            ApplyWaterBudget(season);
        }

        /// <summary>
        /// Quatre additions, une fois par saison, juste apres la resolution :
        ///
        ///   arrivant = maisons desservies + pluie
        ///   traite   = min(arrivant, capacite de la station)
        ///   surplus  = arrivant - traite
        ///   marge    = capacite - traite
        ///
        /// Si le bassin a une route valide jusqu'a la station, il absorbe le surplus dans
        /// la limite de sa place, puis relache dans la marge ce qu'il retient. Sinon, ou
        /// s'il est plein, le surplus est perdu, sans un mot : le rendre visible est le
        /// sujet entier de la phase 10.
        /// </summary>
        private void ApplyWaterBudget(SeasonDefinition season)
        {
            WaterBudget budget = new WaterBudget();

            int served = flow != null ? flow.ServedCount : 0;
            int rain = season != null ? season.RainVolume : 0;
            int capacity = plant != null ? plant.CapacityPerSeason : 0;

            budget.Inflow = served * HouseVolumePerSeason + rain;
            budget.Treated = Mathf.Min(budget.Inflow, capacity);

            int surplus = budget.Inflow - budget.Treated;
            int margin = capacity - budget.Treated;

            // Le bassin obeit exactement a la regle du puzzle : il ne sert que si le solveur
            // lui a trouve une route, avec les memes exigences que pour une maison.
            if (reserve != null && flow != null && flow.IsReserveConnectedAt(reserve.Cell))
            {
                budget.Absorbed = reserve.Absorb(surplus);
                budget.Released = reserve.Release(margin);
            }

            budget.Lost = surplus - budget.Absorbed;

            LastBudget = budget;
            BudgetCount++;
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

            // 2. Le gel. La resistance est une probabilite de tenir : 0 gele des le premier
            // hiver, 1 ne gele jamais. Elle est celle du bout le plus faible du segment,
            // depuis la phase 9b : une route isolee l'est de bout en bout, ou elle gele.
            if (season.FreezeMaxDepth > 0 && depth <= season.FreezeMaxDepth
                && UnityEngine.Random.value >= segment.FrostResistance)
            {
                segment.IsFrozen = true;
            }

            // 3. Les feuilles de l'automne, sur les tuyaux peu profonds seulement. La
            // resistance aux feuilles se lit exactement comme celle au gel : c'est sa
            // jumelle, et le grillage est a l'automne ce que l'isole est a l'hiver.
            if (season.ClogChance > 0f && depth <= ShallowDepth
                && UnityEngine.Random.value < season.ClogChance
                && UnityEngine.Random.value >= segment.LeafResistance)
            {
                segment.IsClogged = true;
            }

            // 4. L'usure, sur tous les tuyaux. Elle seule demande un geste pour se defaire,
            // et elle est la meme pour les trois types : l'entretien reste la boucle.
            segment.Condition -= segment.WearPerSeason * season.WearMultiplier;
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
