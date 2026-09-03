using SousLaVille.Buildings;
using SousLaVille.Core;
using SousLaVille.Network;
using SousLaVille.UI;
using SousLaVille.World;
using UnityEngine;

namespace SousLaVille.Player
{
    /// <summary>
    /// La touche unique. Espace est contextuel : il fait la seule chose qui ait un sens la
    /// ou se tient le personnage et sur la case qu'il regarde. Aucun mode, aucune
    /// combinaison, aucune seconde touche.
    ///
    /// L'ordre compte. Le passage se prend sur la case OCCUPEE, tout le reste agit sur la
    /// case REGARDEE : on ne creuse pas le sol sous ses pieds, et se tenir sur une bouche ne
    /// doit jamais empecher d'en descendre.
    ///
    /// Depuis les saisons, Espace repare un tuyau abime, gele ou bouche, et n'enleve que
    /// les tuyaux sains. Le picto au-dessus de la tete dit toujours lequel des deux.
    /// </summary>
    [RequireComponent(typeof(PlayerController))]
    public class PlayerInteractor : MonoBehaviour
    {
        /// <summary>Ce que fera Espace maintenant. Sert au picto autant qu'a l'action.</summary>
        private enum InteractionKind
        {
            None,
            Descend,
            Ascend,
            ChooseCover,
            Dig,
            PlacePipe,
            RepairPipe,
            RemovePipe
        }

        [Tooltip("Picto affiche au-dessus de la tete. Annonce toujours ce que fera Espace.")]
        [SerializeField] private SpriteRenderer prompt;

        [SerializeField] private Sprite promptDown;
        [SerializeField] private Sprite promptUp;
        [SerializeField] private Sprite promptDig;
        [SerializeField] private Sprite promptPipe;
        [SerializeField] private Sprite promptRepair;
        [SerializeField] private Sprite promptRemove;

        private SousLaVilleInputActions input;
        private PlayerController controller;
        private PipeNetwork network;
        private ManholeFactory factory;
        private VillageMapScreen map;
        private bool isTravelling;
        private bool isChoosing;

        /// <summary>
        /// Image ou le plan du village s'est referme. Le meme Espace ne doit pas etre lu deux
        /// fois : celui qui pose une plaque refermerait le plan puis le rouvrirait aussitot,
        /// puisque le personnage est encore debout sur la plaque exposee. C'est la garde
        /// symetrique de celle que VillageMapScreen pose a l'ouverture.
        /// </summary>
        private int mapClosedFrame = -1;

        /// <summary>Rang de la plaque exposee sous les pieds, ou -1. Pose par Evaluate.</summary>
        private int coverUnderfoot = -1;

        private void Awake()
        {
            controller = GetComponent<PlayerController>();
            EnsureInput();
        }

        private void OnEnable()
        {
            // Recompiler pendant le play recharge le domaine : Unity rappelle OnEnable sans
            // repasser par Awake. Voir le meme garde-fou dans PlayerController.
            EnsureInput();
            input.Gameplay.Enable();
        }

        private void OnDisable()
        {
            input?.Gameplay.Disable();
        }

        private void EnsureInput()
        {
            if (input == null)
            {
                input = new SousLaVilleInputActions();
            }
        }

        private void OnDestroy()
        {
            input?.Dispose();
        }

        private void Update()
        {
            if (controller == null)
            {
                controller = GetComponent<PlayerController>();
            }

            ManholePortal portal;
            InteractionKind kind = Evaluate(out portal);

            ShowPrompt(kind);

            // WasPressedThisFrame et non ReadValue : un appui, jamais un maintien. Espace
            // garde enfonce ne creuse pas une galerie entiere.
            if (kind != InteractionKind.None && input.Gameplay.Interact.WasPressedThisFrame())
            {
                Perform(kind, portal);
            }
        }

        /// <summary>Ce que ferait Espace a cet instant, sans rien faire.</summary>
        private InteractionKind Evaluate(out ManholePortal portal)
        {
            portal = null;

            SceneRouter router = Router;
            if (isTravelling || isChoosing || Time.frameCount == mapClosedFrame
                || router == null || !controller.HasMap)
            {
                return InteractionKind.None;
            }

            // 1. Le passage, sur la case occupee.
            portal = ManholePortal.Find(router.CurrentLayer, controller.Cell);
            if (portal != null)
            {
                return portal.DestinationLayer == GameLayer.Underground
                    ? InteractionKind.Descend
                    : InteractionKind.Ascend;
            }

            // 2. Une plaque exposee sous les pieds, dans l'atelier. Meme geste que la bouche :
            // on marche dessus et on appuie.
            coverUnderfoot = -1;
            if (router.CurrentLayer == GameLayer.Surface)
            {
                ManholeFactory workshop = ResolveFactory();
                if (workshop != null)
                {
                    coverUnderfoot = workshop.SampleIndexAt(controller.Cell);
                    if (coverUnderfoot >= 0)
                    {
                        return InteractionKind.ChooseCover;
                    }
                }
            }

            // Creuser et poser n'existent que sous terre.
            UndergroundMap underground = controller.Map as UndergroundMap;
            if (underground == null)
            {
                return InteractionKind.None;
            }

            Vector2Int target = controller.FacingCell;
            if (!underground.Contains(target))
            {
                return InteractionKind.None;
            }

            // 3. De la terre pleine devant soi : creuser.
            if (!underground.IsWalkable(target))
            {
                return InteractionKind.Dig;
            }

            PipeNetwork pipes = ResolveNetwork();
            if (pipes == null)
            {
                return InteractionKind.None;
            }

            // 4. Une galerie vide devant soi : poser.
            PipeNode node = pipes.NodeAt(target);
            if (node == null)
            {
                return InteractionKind.PlacePipe;
            }

            // 5. Un tuyau abime, gele ou bouche : reparer. Avant l'enlevement, et meme sur
            // un noeud pose par le monde : le raccordement d'une maison gele doit pouvoir
            // se degeler a la main, sans attendre le printemps.
            if (pipes.NeedsRepair(target))
            {
                return InteractionKind.RepairPipe;
            }

            // 6. Un tuyau sain : enlever. Un noeud pose par le monde, la station ou une
            // maison, ne s'annonce pas : il ne s'enleve pas.
            return node.IsPermanent ? InteractionKind.None : InteractionKind.RemovePipe;
        }

        private void Perform(InteractionKind kind, ManholePortal portal)
        {
            switch (kind)
            {
                case InteractionKind.Descend:
                case InteractionKind.Ascend:
                    UsePortal(portal);
                    return;

                case InteractionKind.ChooseCover:
                    OpenVillageMap();
                    return;

                case InteractionKind.Dig:
                    ((UndergroundMap)controller.Map).Dig(controller.FacingCell);
                    return;

                case InteractionKind.PlacePipe:
                    ResolveNetwork().PlacePipe(controller.FacingCell);
                    return;

                case InteractionKind.RepairPipe:
                    ResolveNetwork().Repair(controller.FacingCell);
                    return;

                case InteractionKind.RemovePipe:
                    ResolveNetwork().RemovePipe(controller.FacingCell);
                    return;
            }
        }

        /// <summary>
        /// Le plan du village s'ouvre, une plaque en main. Le personnage s'eteint le temps du
        /// choix : une fleche ne doit pas le faire marcher et deplacer le choix en meme temps.
        /// </summary>
        private void OpenVillageMap()
        {
            VillageMapScreen screen = ResolveMap();
            if (screen == null || !screen.Open(coverUnderfoot))
            {
                return;
            }

            isChoosing = true;
            controller.enabled = false;
            ShowPrompt(InteractionKind.None);

            screen.Closed += OnVillageMapClosed;
        }

        private void OnVillageMapClosed()
        {
            VillageMapScreen screen = ResolveMap();
            if (screen != null)
            {
                screen.Closed -= OnVillageMapClosed;
            }

            controller.enabled = true;
            isChoosing = false;
            mapClosedFrame = Time.frameCount;
        }

        /// <summary>
        /// Le voyage : fondu au noir, bascule de couche, replacement du personnage pendant
        /// que l'ecran est noir, fondu inverse.
        /// </summary>
        private async void UsePortal(ManholePortal portal)
        {
            SceneRouter router = Router;
            if (router == null)
            {
                return;
            }

            GameLayer destinationLayer = portal.DestinationLayer;
            Vector2Int destinationCell = portal.DestinationCell;

            isTravelling = true;

            // Une fleche maintenue pendant le fondu ne doit pas faire partir le personnage
            // de travers a l'arrivee.
            controller.enabled = false;
            ShowPrompt(InteractionKind.None);

            await router.TravelAsync(destinationLayer, () => controller.Teleport(destinationCell));

            if (this == null)
            {
                return;
            }

            controller.enabled = true;
            isTravelling = false;
        }

        private static SceneRouter Router
        {
            get { return GameManager.Instance != null ? GameManager.Instance.Router : null; }
        }

        /// <summary>
        /// Le reseau vit dans la scene Underground : il ne repond que quand cette couche est
        /// allumee, comme les cartes.
        /// </summary>
        private PipeNetwork ResolveNetwork()
        {
            if (network != null && network.isActiveAndEnabled)
            {
                return network;
            }

            network = FindAnyObjectByType<PipeNetwork>();
            return network;
        }

        /// <summary>L'atelier vit dans la scene Surface : il ne repond que quand elle est allumee.</summary>
        private ManholeFactory ResolveFactory()
        {
            if (factory != null && factory.isActiveAndEnabled)
            {
                return factory;
            }

            factory = FindAnyObjectByType<ManholeFactory>();
            return factory;
        }

        /// <summary>Le plan vit dans Persistent, jamais eteinte : une fois trouve, il le reste.</summary>
        private VillageMapScreen ResolveMap()
        {
            if (map == null)
            {
                map = FindAnyObjectByType<VillageMapScreen>(FindObjectsInactive.Include);
            }

            return map;
        }

        private void ShowPrompt(InteractionKind kind)
        {
            if (prompt == null)
            {
                return;
            }

            Sprite sprite = SpriteFor(kind);
            prompt.sprite = sprite;
            prompt.enabled = sprite != null;
        }

        private Sprite SpriteFor(InteractionKind kind)
        {
            // Sur une plaque exposee, le picto EST la plaque : « celle-la ». Aucune image de
            // plus a dessiner, et aucun symbole a apprendre.
            if (kind == InteractionKind.ChooseCover)
            {
                ManholeFactory workshop = ResolveFactory();
                ManholeCoverDefinition definition =
                    workshop != null ? workshop.CoverAt(coverUnderfoot) : null;
                return definition != null ? definition.Cover : null;
            }

            switch (kind)
            {
                case InteractionKind.Descend: return promptDown;
                case InteractionKind.Ascend: return promptUp;
                case InteractionKind.Dig: return promptDig;
                case InteractionKind.PlacePipe: return promptPipe;
                case InteractionKind.RepairPipe: return promptRepair;
                case InteractionKind.RemovePipe: return promptRemove;
                default: return null;
            }
        }
    }
}
