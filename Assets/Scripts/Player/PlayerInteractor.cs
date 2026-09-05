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
    ///
    /// Depuis la phase 9a, Espace entre dans un batiment et en ressort : une porte est un
    /// ManholePortal comme une bouche d'egout, le geste et le fondu sont les memes. Et Espace
    /// face a un personnage le fait parler.
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
            ChoosePipe,
            Enter,
            Exit,
            Talk,
            Dig,
            PlacePipe,
            RepairPipe,
            RemovePipe,

            // AJOUTE A LA FIN, phase 12d. Un rang d'enum se serialise par sa VALEUR : inserer
            // au milieu decalerait tout ce que les scenes ont deja ecrit.
            GrowPlant
        }

        [Tooltip("Picto affiche au-dessus de la tete. Annonce toujours ce que fera Espace.")]
        [SerializeField] private SpriteRenderer prompt;

        [SerializeField] private Sprite promptDown;
        [SerializeField] private Sprite promptUp;
        [SerializeField] private Sprite promptEnter;
        [SerializeField] private Sprite promptExit;
        [SerializeField] private Sprite promptDig;
        [SerializeField] private Sprite promptRepair;
        [SerializeField] private Sprite promptRemove;

        [Tooltip("Picto d'agrandissement de la station, phase 12d : un bassin de plus.")]
        [SerializeField] private Sprite promptGrow;

        private TreatmentPlant plant;
        private SousLaVilleInputActions input;
        private PlayerController controller;
        private PipeNetwork network;
        private ManholeFactory factory;
        private PipeFactory pipeFactory;
        private VillageMapScreen map;
        private SpeechBox speech;
        private ItemLabel itemLabel;
        private SignCatalogue signCatalogue;
        private bool isTravelling;
        private bool isChoosing;
        private bool isTalking;

        /// <summary>
        /// Image ou un ecran s'est referme, plan du village ou boite de dialogue. Le meme
        /// Espace ne doit pas etre lu deux fois : celui qui pose une plaque refermerait le
        /// plan puis le rouvrirait aussitot, puisque le personnage est encore debout sur la
        /// plaque exposee, et celui qui referme un dialogue relancerait le meme personnage,
        /// puisqu'on le regarde toujours. C'est la garde symetrique de celle que
        /// VillageMapScreen et SpeechBox posent a l'ouverture.
        /// </summary>
        private int screenClosedFrame = -1;

        /// <summary>Rang de la plaque exposee sous les pieds, ou -1. Pose par Evaluate.</summary>
        private int coverUnderfoot = -1;

        /// <summary>Rang de l'echantillon de tuyau sous les pieds, ou -1. Pose par Evaluate.</summary>
        private int pipeSampleUnderfoot = -1;

        /// <summary>Le personnage regarde, ou null. Pose par Evaluate.</summary>
        private Villager villagerAhead;

        /// <summary>Celui dont la bulle est allumee, pour l'eteindre quand on se detourne.</summary>
        private Villager promptedVillager;

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
            ShowVillagerPrompt(null);
            ResolveItemLabel()?.Hide();
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

            // La bulle « on peut lui parler » vit au-dessus de la tete du PERSONNAGE, pas de
            // celle du joueur : voir Villager.ShowPrompt.
            ShowVillagerPrompt(kind == InteractionKind.Talk ? villagerAhead : null);

            // Le nom de l'objet foule, au HUD. Il n'etait plus lisible dans le decor.
            ShowItemLabel(kind);

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
            if (isTravelling || isChoosing || isTalking || Time.frameCount == screenClosedFrame
                || router == null || !controller.HasMap)
            {
                return InteractionKind.None;
            }

            // 1. Le passage, sur la case occupee.
            portal = ManholePortal.Find(router.CurrentLayer, controller.Cell);
            if (portal != null)
            {
                return KindFor(router.CurrentLayer, portal.DestinationLayer);
            }

            // 2. Une plaque exposee sous les pieds, dans l'atelier. Meme geste que la bouche :
            // on marche dessus et on appuie.
            //
            // Aucune garde de couche ici : depuis la phase 9a l'atelier est un interieur, et
            // ResolveFactory ne rend l'atelier que si sa couche est allumee, exactement comme
            // ResolveNetwork ne rend le reseau que sous terre. Une couche citee en dur ici
            // aurait cesse d'etre vraie le jour ou l'atelier a demenage.
            coverUnderfoot = -1;
            ManholeFactory workshop = ResolveFactory();
            if (workshop != null)
            {
                coverUnderfoot = workshop.SampleIndexAt(controller.Cell);
                if (coverUnderfoot >= 0)
                {
                    return InteractionKind.ChooseCover;
                }
            }

            // 3. Un echantillon de tuyau sous les pieds, dans l'usine. Meme geste que la
            // plaque : on marche dessus et on appuie, et on repart avec ce type en main.
            pipeSampleUnderfoot = -1;
            PipeFactory pipeWorks = ResolvePipeFactory();
            if (pipeWorks != null && pipeWorks.isActiveAndEnabled)
            {
                pipeSampleUnderfoot = pipeWorks.SampleIndexAt(controller.Cell);
                if (pipeSampleUnderfoot >= 0)
                {
                    return InteractionKind.ChoosePipe;
                }
            }

            // 4. Un personnage devant soi : lui parler. Sur la case REGARDEE, contrairement
            // au passage et a la plaque : on ne se tient pas sur quelqu'un.
            villagerAhead = Villager.At(controller.FacingCell);
            if (villagerAhead != null && villagerAhead.CanSpeak)
            {
                return InteractionKind.Talk;
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

            // 5. De la terre pleine devant soi : creuser.
            if (!underground.IsWalkable(target))
            {
                return InteractionKind.Dig;
            }

            PipeNetwork pipes = ResolveNetwork();
            if (pipes == null)
            {
                return InteractionKind.None;
            }

            // 6. Une galerie vide devant soi : poser.
            PipeNode node = pipes.NodeAt(target);
            if (node == null)
            {
                return InteractionKind.PlacePipe;
            }

            // 7. Un tuyau abime, gele ou bouche : reparer. Avant l'enlevement, et meme sur
            // un noeud pose par le monde : le raccordement d'une maison gele doit pouvoir
            // se degeler a la main, sans attendre le printemps.
            if (pipes.NeedsRepair(target))
            {
                return InteractionKind.RepairPipe;
            }

            // 8. L'ARRIVEE DE LA STATION : l'agrandir d'un bassin, phase 12d. Le noeud
            // PlantInlet n'offrait AUCUNE action tant qu'il n'etait pas abime — le creneau
            // etait libre, et c'est le seul endroit du monde ou ce geste a un sens.
            if (node.Type == NodeType.PlantInlet)
            {
                TreatmentPlant plant = ResolvePlant();
                return plant != null && plant.CanGrow
                    ? InteractionKind.GrowPlant
                    : InteractionKind.None;
            }

            // 9. Un tuyau sain : enlever. Un noeud pose par le monde, la station ou une
            // maison, ne s'annonce pas : il ne s'enleve pas.
            return node.IsPermanent ? InteractionKind.None : InteractionKind.RemovePipe;
        }

        private void Perform(InteractionKind kind, ManholePortal portal)
        {
            switch (kind)
            {
                case InteractionKind.Descend:
                case InteractionKind.Ascend:
                case InteractionKind.Enter:
                case InteractionKind.Exit:
                    UsePortal(portal);
                    return;

                case InteractionKind.Talk:
                    StartTalking();
                    return;

                case InteractionKind.ChooseCover:
                    OpenVillageMap();
                    return;

                case InteractionKind.ChoosePipe:
                    // Aucun ecran, aucune confirmation : on marche sur l'echantillon, on
                    // appuie, et on repart avec ce tuyau en main.
                    ResolvePipeFactory().SetCurrent(pipeSampleUnderfoot);
                    return;

                case InteractionKind.Dig:
                    ((UndergroundMap)controller.Map).Dig(controller.FacingCell);
                    return;

                case InteractionKind.PlacePipe:
                    ResolveNetwork().PlacePipe(controller.FacingCell, PipeInHand);
                    return;

                case InteractionKind.RepairPipe:
                    ResolveNetwork().Repair(controller.FacingCell);
                    return;

                case InteractionKind.RemovePipe:
                    ResolveNetwork().RemovePipe(controller.FacingCell);
                    return;

                case InteractionKind.GrowPlant:
                    ResolvePlant().AddBasin();
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
            screenClosedFrame = Time.frameCount;
        }

        /// <summary>
        /// Le personnage parle. Le personnage joueur s'eteint le temps du dialogue : une
        /// fleche ne doit pas le faire marcher pendant qu'on lit, et s'eloigner en plein
        /// dialogue laisserait une boite ouverte sans interlocuteur.
        /// </summary>
        private void StartTalking()
        {
            if (villagerAhead == null)
            {
                return;
            }

            SpeechBox box = villagerAhead.Speak();
            if (box == null)
            {
                return;
            }

            speech = box;
            isTalking = true;
            controller.enabled = false;
            ShowPrompt(InteractionKind.None);

            speech.Closed += OnSpeechClosed;
        }

        private void OnSpeechClosed()
        {
            if (speech != null)
            {
                speech.Closed -= OnSpeechClosed;
                speech = null;
            }

            controller.enabled = true;
            isTalking = false;
            screenClosedFrame = Time.frameCount;
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

        /// <summary>
        /// Ce que dit le picto d'un passage. Descendre et remonter entre la surface et le
        /// sous-sol, entrer et sortir d'un batiment : quatre pictos, un seul mecanisme.
        /// </summary>
        private static InteractionKind KindFor(GameLayer from, GameLayer destination)
        {
            if (destination == GameLayer.Interior)
            {
                return InteractionKind.Enter;
            }

            if (from == GameLayer.Interior)
            {
                return InteractionKind.Exit;
            }

            return destination == GameLayer.Underground
                ? InteractionKind.Descend
                : InteractionKind.Ascend;
        }

        private static SceneRouter Router
        {
            get { return GameManager.Instance != null ? GameManager.Instance.Router : null; }
        }

        /// <summary>
        /// Le reseau vit dans la scene Underground : il ne repond que quand cette couche est
        /// allumee, comme les cartes.
        /// </summary>
        /// <summary>
        /// La station, phase 12d. Elle vit dans la meme scene que le reseau, et on ne
        /// l'interroge que sous terre : elle est donc allumee quand on la cherche. On la garde
        /// une fois trouvee, comme le reseau.
        /// </summary>
        private TreatmentPlant ResolvePlant()
        {
            if (plant != null && plant.isActiveAndEnabled)
            {
                return plant;
            }

            plant = FindAnyObjectByType<TreatmentPlant>(FindObjectsInactive.Include);
            return plant;
        }

        private PipeNetwork ResolveNetwork()
        {
            if (network != null && network.isActiveAndEnabled)
            {
                return network;
            }

            network = FindAnyObjectByType<PipeNetwork>();
            return network;
        }

        /// <summary>
        /// L'usine a tuyaux vit dans la scene Interiors, ETEINTE des qu'on n'y est pas. Or on
        /// pose des tuyaux sous terre : il faut donc l'inclure explicitement dans la recherche,
        /// et la garder une fois trouvee. C'est le contraire de l'atelier, qu'on ne consulte
        /// que sur place.
        /// </summary>
        private PipeFactory ResolvePipeFactory()
        {
            if (pipeFactory == null)
            {
                pipeFactory = FindAnyObjectByType<PipeFactory>(FindObjectsInactive.Include);
            }

            return pipeFactory;
        }

        /// <summary>Le type de tuyau en main, ou null si l'usine ne repond pas encore.</summary>
        private PipeType PipeInHand
        {
            get
            {
                PipeFactory pipeWorks = ResolvePipeFactory();
                return pipeWorks != null ? pipeWorks.Current : null;
            }
        }

        /// <summary>L'atelier vit dans la scene Interiors : il ne repond que quand elle est allumee.</summary>
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

        /// <summary>
        /// Allume la bulle du personnage regarde, et eteint celle de l'ancien. Le changement
        /// seul est traite : on ne touche a rien tant qu'on regarde le meme.
        /// </summary>
        private void ShowVillagerPrompt(Villager villager)
        {
            if (promptedVillager == villager)
            {
                return;
            }

            if (promptedVillager != null)
            {
                promptedVillager.ShowPrompt(false);
            }

            promptedVillager = villager;

            if (promptedVillager != null)
            {
                promptedVillager.ShowPrompt(true);
            }
        }

        /// <summary>
        /// Le cartel de l'objet sur lequel on se tient. Rien de modal : le personnage marche,
        /// aucune touche n'est consommee, et le cartel s'efface des qu'on quitte la case.
        /// </summary>
        private void ShowItemLabel(InteractionKind kind)
        {
            ItemLabel box = ResolveItemLabel();
            if (box == null)
            {
                return;
            }

            if (kind == InteractionKind.ChooseCover)
            {
                ManholeFactory workshop = ResolveFactory();
                ManholeCoverDefinition definition =
                    workshop != null ? workshop.CoverAt(coverUnderfoot) : null;
                box.Show(definition != null ? definition.NameImage : null, null);
                return;
            }

            if (kind == InteractionKind.ChoosePipe)
            {
                PipeFactory pipeWorks = ResolvePipeFactory();
                PipeType type = pipeWorks != null ? pipeWorks.TypeAt(pipeSampleUnderfoot) : null;
                box.Show(type != null ? type.NameImage : null,
                    type != null ? type.DefeatedSeasonIcon : null);
                return;
            }

            // UN PANNEAU DE L'USINE SOUS LES PIEDS, phase 13. Aucun InteractionKind ici, et
            // c'est voulu : un panneau NE SE PREND PAS, il se lit. Un kind non nul ferait
            // agir Espace, et Espace ne doit rien faire sur un panneau expose.
            SignCatalogue planche = ResolveSignCatalogue();
            if (planche != null && planche.isActiveAndEnabled)
            {
                Sprite signName = planche.NameOn(controller.Cell);
                if (signName != null)
                {
                    box.Show(signName, null);
                    return;
                }
            }

            box.Hide();
        }

        /// <summary>
        /// La planche de l'usine a panneaux. Elle vit dans Interiors, que le SceneRouter
        /// eteint : on l'inclut donc dans la recherche, et on RETENTE tant qu'on ne l'a pas.
        /// La garde isActiveAndEnabled fait le reste — hors de l'usine, elle est eteinte et
        /// ne repond pas, exactement comme l'usine a tuyaux.
        /// </summary>
        private SignCatalogue ResolveSignCatalogue()
        {
            if (signCatalogue == null)
            {
                signCatalogue = FindAnyObjectByType<SignCatalogue>(FindObjectsInactive.Include);
            }

            return signCatalogue;
        }

        /// <summary>Le cartel vit dans Persistent, mais eteint : il faut l'inclure.</summary>
        private ItemLabel ResolveItemLabel()
        {
            if (itemLabel == null)
            {
                itemLabel = FindAnyObjectByType<ItemLabel>(FindObjectsInactive.Include);
            }

            return itemLabel;
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

            // Sur un echantillon de tuyau, meme idee : « celui-la ».
            if (kind == InteractionKind.ChoosePipe)
            {
                PipeFactory pipeWorks = ResolvePipeFactory();
                PipeType type = pipeWorks != null ? pipeWorks.TypeAt(pipeSampleUnderfoot) : null;
                return type != null ? type.Sample : null;
            }

            // ET DEVANT UNE GALERIE VIDE, LE PICTO EST LE TUYAU EN MAIN. Il sait toujours ce
            // qu'il va poser, sans jauge et sans compteur au HUD. picto_pipe a disparu avec
            // cette ligne : un symbole generique ne disait plus rien des trois types.
            if (kind == InteractionKind.PlacePipe)
            {
                PipeType type = PipeInHand;
                return type != null ? type.Sample : null;
            }

            switch (kind)
            {
                case InteractionKind.Descend: return promptDown;
                case InteractionKind.Ascend: return promptUp;
                case InteractionKind.Enter: return promptEnter;
                case InteractionKind.Exit: return promptExit;
                case InteractionKind.Dig: return promptDig;
                case InteractionKind.RepairPipe: return promptRepair;
                case InteractionKind.RemovePipe: return promptRemove;
                case InteractionKind.GrowPlant: return promptGrow;
                default: return null;
            }
        }
    }
}
