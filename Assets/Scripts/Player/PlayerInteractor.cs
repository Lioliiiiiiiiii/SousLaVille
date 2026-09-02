using SousLaVille.Core;
using SousLaVille.World;
using UnityEngine;

namespace SousLaVille.Player
{
    /// <summary>
    /// La touche unique. En phase 2, Espace ne fait qu'une chose : emprunter le passage sur
    /// lequel on se tient, dans un sens comme dans l'autre.
    ///
    /// L'interaction porte sur la case OCCUPEE, pas sur la case regardee : marcher sur la
    /// bouche puis appuyer, c'est le geste le plus simple a six ans. La case regardee est
    /// reservee au creusement de la phase 3, qui viendra dans ce meme fichier ; les deux
    /// gestes cohabiteront sans ambiguite, on ne creuse pas la case ou l'on se tient.
    /// </summary>
    [RequireComponent(typeof(PlayerController))]
    public class PlayerInteractor : MonoBehaviour
    {
        [Tooltip("Picto affiche au-dessus de la tete quand la case porte un passage.")]
        [SerializeField] private SpriteRenderer prompt;

        [Tooltip("Picto « ici on descend ».")]
        [SerializeField] private Sprite promptDown;

        [Tooltip("Picto « ici on remonte ».")]
        [SerializeField] private Sprite promptUp;

        private SousLaVilleInputActions input;
        private PlayerController controller;
        private bool isTravelling;

        private void Awake()
        {
            controller = GetComponent<PlayerController>();
            input = new SousLaVilleInputActions();
        }

        private void OnEnable()
        {
            input.Gameplay.Enable();
        }

        private void OnDisable()
        {
            input.Gameplay.Disable();
        }

        private void OnDestroy()
        {
            input?.Dispose();
        }

        private void Update()
        {
            SceneRouter router = GameManager.Instance != null ? GameManager.Instance.Router : null;
            if (router == null || !controller.HasMap)
            {
                ShowPrompt(null);
                return;
            }

            ManholePortal portal = isTravelling
                ? null
                : ManholePortal.Find(router.CurrentLayer, controller.Cell);

            ShowPrompt(portal);

            // WasPressedThisFrame et non ReadValue : un appui, jamais un maintien. Espace
            // garde enfonce ne fait pas descendre et remonter en boucle.
            if (portal != null && input.Gameplay.Interact.WasPressedThisFrame())
            {
                UsePortal(portal, router);
            }
        }

        /// <summary>
        /// Le voyage : fondu au noir, bascule de couche, replacement du personnage pendant
        /// que l'ecran est noir, fondu inverse.
        /// </summary>
        private async void UsePortal(ManholePortal portal, SceneRouter router)
        {
            GameLayer destinationLayer = portal.DestinationLayer;
            Vector2Int destinationCell = portal.DestinationCell;

            isTravelling = true;

            // Une fleche maintenue pendant le fondu ne doit pas faire partir le personnage
            // de travers a l'arrivee.
            controller.enabled = false;
            ShowPrompt(null);

            await router.TravelAsync(destinationLayer, () => controller.Teleport(destinationCell));

            if (this == null)
            {
                return;
            }

            controller.enabled = true;
            isTravelling = false;
        }

        private void ShowPrompt(ManholePortal portal)
        {
            if (prompt == null)
            {
                return;
            }

            if (portal == null)
            {
                prompt.enabled = false;
                return;
            }

            prompt.sprite = portal.DestinationLayer == GameLayer.Underground ? promptDown : promptUp;
            prompt.enabled = prompt.sprite != null;
        }
    }
}
