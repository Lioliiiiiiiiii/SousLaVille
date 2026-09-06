using System.Collections.Generic;
using SousLaVille.Core;
using SousLaVille.Seasons;
using UnityEngine;

namespace SousLaVille.World
{
    /// <summary>
    /// Pose les maisons du village au reveil, a partir de la liste de cases cuite par le
    /// builder. Cinq objets crees par code plutot que figes dans la scene : c'est ce que le
    /// nom promet, et la scene reste legere.
    ///
    /// Le spawner tient aussi le rafraichissement : quand le joueur remonte du sous-sol, la
    /// surface se rallume et les gouttes doivent dire la verite du moment.
    /// </summary>
    public class HouseSpawner : MonoBehaviour
    {
        [Tooltip("Cases de raccordement des maisons, cuites depuis le plan du village.")]
        [SerializeField] private Vector2Int[] cells;

        [Tooltip("Coin bas-gauche de l'empreinte de deux cases sur deux de chaque maison, meme ordre.")]
        [SerializeField] private Vector2Int[] anchors;

        [SerializeField] private GridMap map;
        [SerializeField] private Sprite houseSprite;

        [Tooltip("La maison selon la saison, phase 18g : printemps, ete, automne, hiver.")]
        [SerializeField] private Sprite[] houseSeasons;
        [SerializeField] private Sprite dropServed;
        [SerializeField] private Sprite dropIdle;

        [Tooltip("Hauteur de la goutte au-dessus de la rangee du bas de la maison, en unites. Le toit monte a 2.")]
        [SerializeField] private float dropHeight = 2.6f;

        private readonly List<House> houses = new List<House>();

        public IReadOnlyList<House> Houses => houses;

        private void Awake()
        {
            Spawn();
        }

        private void OnEnable()
        {
            SubscribeToFlow();
            RefreshAll();
        }

        private void OnDisable()
        {
            if (GameManager.Instance != null && GameManager.Instance.Flow != null)
            {
                GameManager.Instance.Flow.Solved -= RefreshAll;
            }
        }

        /// <summary>Abonnement au solveur, refait a chaque rallumage de la couche.</summary>
        private void SubscribeToFlow()
        {
            if (GameManager.Instance == null || GameManager.Instance.Flow == null)
            {
                return;
            }

            GameManager.Instance.Flow.Solved -= RefreshAll;
            GameManager.Instance.Flow.Solved += RefreshAll;
        }

        private void RefreshAll()
        {
            foreach (House house in houses)
            {
                house.Refresh();
            }
        }

        private void Spawn()
        {
            if (cells == null || map == null)
            {
                return;
            }

            for (int i = 0; i < cells.Length; i++)
            {
                GameObject houseObject = new GameObject($"House_{i + 1:00}");
                houseObject.transform.SetParent(transform, false);

                // PHASE 18D : la maison couvre deux cases sur deux. Son pivot est au milieu de sa
                // largeur, donc elle se pose a un demi-carreau a l'est du centre de la case
                // d'ancrage, le coin bas-gauche de l'empreinte. Sans ancre cuite, elle retombe
                // sur sa case de raccordement et le dit.
                Vector2Int anchor = anchors != null && i < anchors.Length ? anchors[i] : cells[i];
                if (anchors == null || i >= anchors.Length)
                {
                    Debug.LogError($"[Sous la Ville] La maison {cells[i]} n'a pas d'ancre : reconstruis la scène Surface.");
                }

                houseObject.transform.position = map.CellToWorld(anchor) + new Vector3(0.5f, 0f, 0f);

                SpriteRenderer body = houseObject.AddComponent<SpriteRenderer>();
                body.sprite = houseSprite;
                body.sortingLayerName = GameSortingLayers.SurfaceEntities;
                body.sortingOrder = 0;
                // Phase 18b : le tri par Y se fait au PIVOT, pose au sol, et non au centre du
                // sprite, qui monte avec le toit. Meme regle que SceneBuilderUtility.ApplyStandingSort.
                body.spriteSortPoint = SpriteSortPoint.Pivot;

                // Phase 18g : le toit se couvre de neige l'hiver.
                if (houseSeasons != null && houseSeasons.Length > 0)
                {
                    houseObject.AddComponent<SeasonalSprite>().Initialize(body, houseSeasons);
                }

                GameObject dropObject = new GameObject("Drop");
                dropObject.transform.SetParent(houseObject.transform, false);
                dropObject.transform.localPosition = new Vector3(0f, dropHeight, 0f);

                SpriteRenderer drop = dropObject.AddComponent<SpriteRenderer>();
                drop.sortingLayerName = GameSortingLayers.SurfaceOverlay;
                drop.sortingOrder = 0;

                House house = houseObject.AddComponent<House>();
                house.Initialize(cells[i], drop, dropServed, dropIdle);

                houses.Add(house);
            }
        }
    }
}
