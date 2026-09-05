using System.Collections.Generic;
using SousLaVille.Core;
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
        [Tooltip("Cases des maisons, cuites depuis le plan du village.")]
        [SerializeField] private Vector2Int[] cells;

        [SerializeField] private GridMap map;
        [SerializeField] private Sprite houseSprite;
        [SerializeField] private Sprite dropServed;
        [SerializeField] private Sprite dropIdle;

        [Tooltip("Hauteur de la goutte au-dessus du sol de la maison, en unites.")]
        [SerializeField] private float dropHeight = 1.4f;

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
                houseObject.transform.position = map.CellToWorld(cells[i]);

                SpriteRenderer body = houseObject.AddComponent<SpriteRenderer>();
                body.sprite = houseSprite;
                body.sortingLayerName = GameSortingLayers.SurfaceEntities;
                body.sortingOrder = 0;
                // Phase 18b : le tri par Y se fait au PIVOT, pose au sol, et non au centre du
                // sprite, qui monte avec le toit. Meme regle que SceneBuilderUtility.ApplyStandingSort.
                body.spriteSortPoint = SpriteSortPoint.Pivot;

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
