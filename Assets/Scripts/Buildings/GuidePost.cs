using SousLaVille.Core;
using SousLaVille.Network;
using SousLaVille.Seasons;
using SousLaVille.World;
using UnityEngine;

namespace SousLaVille.Buildings
{
    /// <summary>
    /// Un personnage-guide, phase 12e. Il ne dit qu'UNE leçon, et il ne la dit QUE tant qu'elle
    /// n'est pas acquise.
    ///
    /// Pourquoi il en fallait huit. Le jeu comptait sept phrases avant cette phase, toutes
    /// derriere les portes de deux boutiques et toutes sur le choix des plaques et des tuyaux.
    /// Rien ne disait le but, rien ne disait qu'on creuse, et rien ne disait comment lire une
    /// route qui n'aboutit pas.
    ///
    /// SANS MEMOIRE, decision du 4 septembre. Il se tait quand sa condition redevient fausse et
    /// reparle si elle redevient vraie. Reexpliquer a un relancement ne punit rien, ne coute
    /// aucun champ de sauvegarde, et evite d'avoir a donner au Villager un identifiant stable
    /// puis a le faire entrer dans le ET des quatre de SaveSystem.TryLoad — ou il aurait pu
    /// n'etre jamais relu, en silence, exactement comme les plaques en phase 9a.
    ///
    /// SE TAIRE SANS DISPARAITRE est gratuit : `Villager.CanSpeak` vaut deja `LineCount > 0`,
    /// et `Evaluate` ne propose `Talk` que si `CanSpeak`. Vider ses lignes laisse donc le guide
    /// visible, sur sa case, et Espace ne fait plus rien devant lui.
    /// </summary>
    [RequireComponent(typeof(Villager))]
    public class GuidePost : MonoBehaviour
    {
        /// <summary>Ce qu'un guide enseigne. L'ordre est celui de PlaceholderArtGenerator.GuideLines.</summary>
        public enum Lesson
        {
            Goal,
            Manhole,
            Seasons,
            Repair,
            Dig,
            PlacePipe,
            Route,
            Reserve
        }

        [Tooltip("La leçon de ce guide. Elle décide de sa condition et de ses phrases.")]
        [SerializeField] private Lesson lesson;

        [Tooltip("Ses phrases, une image chacune. Vidées quand la leçon est acquise.")]
        [SerializeField] private Sprite[] lines;

        [Tooltip("Le signal d'attention, visible de loin, allumé tant qu'il a quelque chose à dire.")]
        [SerializeField] private SpriteRenderer attention;

        private Villager villager;
        private FlowSolver flow;
        private SeasonSystem seasons;
        private PipeNetwork network;
        private UndergroundMap undergroundMap;

        private void Awake()
        {
            villager = GetComponent<Villager>();
        }

        private void OnEnable()
        {
            Subscribe();
            Reevaluate();
        }

        private void OnDisable()
        {
            if (seasons != null)
            {
                seasons.SeasonChanged -= OnSeasonChanged;
            }

            if (flow != null)
            {
                flow.Solved -= Reevaluate;
            }
        }

        private void Update()
        {
            // Persistent et les couches n'arrivent pas dans un ordre garanti, et le guide vit
            // dans une couche que le SceneRouter eteint. On RETENTE tant qu'il manque quelque
            // chose, jamais une seule fois au Start : c'est le piege tombe en phase 1, en
            // phase 4 et en phase 5.
            if (flow == null || seasons == null)
            {
                Subscribe();
                Reevaluate();
            }
        }

        private void Subscribe()
        {
            if (flow == null)
            {
                flow = FindAnyObjectByType<FlowSolver>(FindObjectsInactive.Include);
                if (flow != null)
                {
                    flow.Solved += Reevaluate;
                }
            }

            if (seasons == null)
            {
                seasons = FindAnyObjectByType<SeasonSystem>(FindObjectsInactive.Include);
                if (seasons != null)
                {
                    seasons.SeasonChanged += OnSeasonChanged;
                }
            }

            if (network == null)
            {
                network = FindAnyObjectByType<PipeNetwork>(FindObjectsInactive.Include);
            }

            if (undergroundMap == null)
            {
                undergroundMap = FindAnyObjectByType<UndergroundMap>(FindObjectsInactive.Include);
            }
        }

        private void OnSeasonChanged(SeasonDefinition season)
        {
            Reevaluate();
        }

        /// <summary>
        /// Il parle, ou il se tait. Tout est RELU depuis l'etat du monde, jamais accumule : une
        /// couche qui se rallume rattrape ainsi tout ce qui s'est passe pendant son extinction.
        /// </summary>
        private void Reevaluate()
        {
            if (villager == null)
            {
                return;
            }

            bool speaks = ShouldSpeak();
            villager.SetLines(speaks ? lines : null);

            if (attention != null)
            {
                attention.enabled = speaks;
            }
        }

        /// <summary>
        /// La condition de chaque lecon. Aucune n'invente d'etat : toutes se lisent sur le
        /// monde, et redeviennent vraies si le joueur defait ce qu'il a fait.
        /// </summary>
        private bool ShouldSpeak()
        {
            switch (lesson)
            {
                // Le but : tant qu'aucune maison n'est desservie.
                case Lesson.Goal:
                    return flow == null || flow.ServedCount == 0;

                // La bouche et le creusement : tant que rien n'a ete creuse.
                case Lesson.Manhole:
                case Lesson.Dig:
                    return undergroundMap == null || undergroundMap.DugCells.Count == 0;

                // Poser : tant qu'aucun tuyau n'a ete pose.
                case Lesson.PlacePipe:
                    return network == null || network.SegmentCount == 0;

                // LA ROUTE. Il parle quand de l'EAU MORTE existe AU-DELA des destinations
                // elles-memes : une route commencee qui n'aboutit pas. C'est la frontiere
                // publiee par le solveur depuis la phase 12a, et c'est ce qui rend cette lecon
                // enseignable — on ne peut pas expliquer OU ca casse tant que le jeu l'ignore.
                //
                // La lecon s'appelait « Profondeur » jusqu'a la phase 21. La condition n'a pas
                // bouge d'une ligne : une route inachevee laisse la meme eau morte qu'une route
                // qui remontait. Seul ce qu'il en dit a change.
                //
                // Le seuil compte. A `> 0` il parlait des le premier lancement, avant qu'une
                // seule case soit creusee : sans tuyau, chaque destination est deja sa propre
                // frontiere, et la lecon serait arrivee avant qu'il y ait de l'eau a expliquer.
                case Lesson.Route:
                    return flow != null
                        && flow.StrandedCount > flow.DestinationCount + flow.ReserveCount;

                // Les saisons : a l'automne et a l'hiver, celles qui abiment.
                case Lesson.Seasons:
                    return seasons != null && seasons.Current != null
                        && (seasons.Current.ClogChance > 0f || seasons.Current.FreezeChance > 0f);

                // Reparer : tant qu'un tuyau demande une reparation.
                case Lesson.Repair:
                    return NeedsRepairSomewhere();

                // Le bassin : tant qu'il n'est pas relie.
                case Lesson.Reserve:
                    return flow == null || !flow.IsReserveConnected;

                default:
                    return false;
            }
        }

        private bool NeedsRepairSomewhere()
        {
            if (network == null)
            {
                return false;
            }

            foreach (PipeSegment segment in network.Segments)
            {
                if (segment.IsFrozen || segment.IsClogged
                    || segment.Condition <= FlowSolver.MinimumCondition)
                {
                    return true;
                }
            }

            return false;
        }
    }
}
