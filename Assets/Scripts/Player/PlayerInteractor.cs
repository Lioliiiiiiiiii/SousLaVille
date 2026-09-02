using SousLaVille.Core;
using SousLaVille.Network;
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
            Dig,
            PlacePipe,
            RemovePipe
        }

        [Tooltip("Picto affiche au-dessus de la tete. Annonce toujours ce que fera Espace.")]
        [SerializeField] private SpriteRenderer prompt;

        [SerializeField] private Sprite promptDown;
        [SerializeField] private Sprite promptUp;
        [SerializeField] private Sprite promptDig;
        [SerializeField] private Sprite promptPipe;
        [SerializeField] private Sprite promptRemove;

        private SousLaVilleInputActions input;
        private PlayerController controller;
        private PipeNetwork network;
        private bool isTravelling;

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
            if (isTravelling || router == null || !controller.HasMap)
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

            // 2. De la terre pleine devant soi : creuser.
            if (!underground.IsWalkable(target))
            {
                return InteractionKind.Dig;
            }

            PipeNetwork pipes = ResolveNetwork();
            if (pipes == null)
            {
                return InteractionKind.None;
            }

            // 3 et 4. Une galerie devant soi : poser, ou enlever ce qui y est deja. Un noeud
            // pose par le monde, la station, ne s'annonce pas : il ne s'enleve pas.
            PipeNode node = pipes.NodeAt(target);
            if (node == null)
            {
                return InteractionKind.PlacePipe;
            }

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

                case InteractionKind.Dig:
                    ((UndergroundMap)controller.Map).Dig(controller.FacingCell);
                    return;

                case InteractionKind.PlacePipe:
                    ResolveNetwork().PlacePipe(controller.FacingCell);
                    return;

                case InteractionKind.RemovePipe:
                    ResolveNetwork().RemovePipe(controller.FacingCell);
                    return;
            }
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
            switch (kind)
            {
                case InteractionKind.Descend: return promptDown;
                case InteractionKind.Ascend: return promptUp;
                case InteractionKind.Dig: return promptDig;
                case InteractionKind.PlacePipe: return promptPipe;
                case InteractionKind.RemovePipe: return promptRemove;
                default: return null;
            }
        }
    }
}
