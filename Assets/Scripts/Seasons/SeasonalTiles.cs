using SousLaVille.Core;
using UnityEngine;
using UnityEngine.Tilemaps;

namespace SousLaVille.Seasons
{
    /// <summary>
    /// LE SOL QUI CHANGE AVEC LA SAISON, phase 18g. Chaque famille de tuiles — la pelouse, la case
    /// de decor, le chemin par masque, l'eau, le pied d'arbre, la haie par masque, le sol de la
    /// maison — a une tuile par saison, et le passage d'une saison a l'autre est UN SwapTile par
    /// famille et par tilemap : Unity remplace toutes les occurrences d'un coup. Rien ne
    /// s'instancie, rien ne boucle par image.
    ///
    /// Les quatre tableaux sont paralleles : la case i de chacun est la meme famille. Deux
    /// saisons peuvent partager une tuile, le swap est alors saute. La scene est peinte dans
    /// l'etat du PRINTEMPS — les fleurs — et c'est de la que part le premier echange.
    ///
    /// Pose sur la racine de la scene Surface ; s'abonne dans OnEnable et se desabonne dans
    /// OnDisable, comme SeasonAmbience et pour la meme raison.
    /// </summary>
    public class SeasonalTiles : MonoBehaviour
    {
        [Tooltip("Les tilemaps a echanger : le sol, la couche bloquante, l'eau.")]
        [SerializeField] private Tilemap[] tilemaps;

        [Tooltip("Une tuile par famille, au printemps. Meme ordre dans les quatre tableaux.")]
        [SerializeField] private TileBase[] spring;
        [SerializeField] private TileBase[] summer;
        [SerializeField] private TileBase[] autumn;
        [SerializeField] private TileBase[] winter;

        /// <summary>La saison dont les tuiles sont posees. La scene est peinte au printemps, rang 0.</summary>
        private int applied;

        private SeasonSystem seasons;

        private void OnEnable()
        {
            Subscribe();
        }

        private void OnDisable()
        {
            if (seasons != null)
            {
                seasons.SeasonChanged -= Apply;
                seasons = null;
            }
        }

        private void Update()
        {
            if (seasons == null)
            {
                Subscribe();
            }
        }

        private void Subscribe()
        {
            if (seasons != null || GameManager.Instance == null)
            {
                return;
            }

            seasons = GameManager.Instance.Seasons;
            if (seasons == null)
            {
                return;
            }

            seasons.SeasonChanged += Apply;
            Apply(seasons.Current);
        }

        private TileBase[] SetFor(int season)
        {
            switch (season)
            {
                case 1: return summer;
                case 2: return autumn;
                case 3: return winter;
                default: return spring;
            }
        }

        private void Apply(SeasonDefinition season)
        {
            if (seasons == null || tilemaps == null)
            {
                return;
            }

            int target = seasons.CurrentIndex;
            if (target == applied)
            {
                return;
            }

            TileBase[] from = SetFor(applied);
            TileBase[] to = SetFor(target);
            int families = Mathf.Min(from.Length, to.Length);

            for (int family = 0; family < families; family++)
            {
                if (from[family] == null || to[family] == null || from[family] == to[family])
                {
                    continue;
                }

                foreach (Tilemap tilemap in tilemaps)
                {
                    if (tilemap != null)
                    {
                        tilemap.SwapTile(from[family], to[family]);
                    }
                }
            }

            applied = target;
        }
    }
}
