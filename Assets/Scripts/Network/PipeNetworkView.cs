using UnityEngine;
using UnityEngine.Tilemaps;

namespace SousLaVille.Network
{
    /// <summary>
    /// Le rendu du reseau. Chaque noeud recoit la tuile qui correspond a son masque de
    /// raccords : un tuyau isole, un coude, un T et un croisement se distinguent donc sans
    /// code de dessin particulier.
    ///
    /// La couleur dit l'etat, dans un ordre de priorite fixe : gele, bouche, trop abime,
    /// porteur d'eau, sain. Aucune image supplementaire : une couleur par case suffit a
    /// montrer tout le reseau d'un coup d'oeil, et aucune ne demande de legende.
    ///
    /// Tout est redessine a chaque changement. Quelques centaines de cases, et seulement sur
    /// action du joueur ou apres une resolution : le calcul incremental viendra s'il se voit
    /// un jour.
    /// </summary>
    public class PipeNetworkView : MonoBehaviour
    {
        [SerializeField] private PipeNetwork network;
        [SerializeField] private Tilemap pipes;

        [Tooltip("Seize tuiles, indexees par le masque de raccords.")]
        [SerializeField] private TileBase[] tilesByMask;

        [Tooltip("Teinte d'un tuyau qui porte de l'eau.")]
        [SerializeField] private Color waterTint = new Color(0.36f, 0.66f, 0.94f, 1f);

        [Tooltip("Tuyau gele : blanc bleute.")]
        [SerializeField] private Color frozenTint = new Color(0.85f, 0.93f, 1f, 1f);

        [Tooltip("Tuyau bouche : brun.")]
        [SerializeField] private Color cloggedTint = new Color(0.55f, 0.40f, 0.24f, 1f);

        [Tooltip("Tuyau trop abime pour porter : rouge terne.")]
        [SerializeField] private Color brokenTint = new Color(0.72f, 0.35f, 0.32f, 1f);

        [Tooltip("Tuyau sain, sans eau. Blanc : la tuile garde son gris d'origine.")]
        [SerializeField] private Color idleTint = Color.white;

        private SousLaVille.Core.GameManager Manager => SousLaVille.Core.GameManager.Instance;
        private FlowSolver flow;

        private void OnEnable()
        {
            if (network == null)
            {
                return;
            }

            network.Changed += Redraw;

            // Le solveur vit dans Persistent : il survit a l'extinction de cette couche, mais
            // il faut se rabonner a chaque rallumage.
            flow = Manager != null ? Manager.Flow : null;
            if (flow != null)
            {
                flow.Solved += Redraw;
            }

            Redraw();
        }

        private void OnDisable()
        {
            if (network != null)
            {
                network.Changed -= Redraw;
            }

            if (flow != null)
            {
                flow.Solved -= Redraw;
            }
        }

        private void Redraw()
        {
            if (pipes == null || tilesByMask == null || tilesByMask.Length < 16)
            {
                return;
            }

            pipes.ClearAllTiles();

            if (flow == null && Manager != null)
            {
                flow = Manager.Flow;
            }

            foreach (PipeNode node in network.Nodes)
            {
                Vector3Int position = new Vector3Int(node.GridPos.x, node.GridPos.y, 0);
                int mask = network.NeighbourMask(node.GridPos);

                pipes.SetTile(position, tilesByMask[mask]);

                // LockColor est pose par defaut sur une tuile : sans ce reglage, SetColor
                // serait ignore en silence.
                pipes.SetTileFlags(position, TileFlags.None);
                pipes.SetColor(position, TintFor(node.GridPos));
            }
        }

        /// <summary>
        /// L'etat d'une case, dans l'ordre de priorite du plan : ce qui empeche l'eau de
        /// passer se voit avant ce qui la laisse passer. Une case peut porter plusieurs
        /// segments : le pire l'emporte, c'est celui que le joueur doit aller reparer.
        /// </summary>
        private Color TintFor(Vector2Int cell)
        {
            bool broken = false;

            foreach (PipeSegment segment in network.SegmentsAt(cell))
            {
                if (segment.IsFrozen)
                {
                    return frozenTint;
                }

                if (segment.IsClogged)
                {
                    return cloggedTint;
                }

                broken |= segment.Condition <= FlowSolver.MinimumCondition;
            }

            if (broken)
            {
                return brokenTint;
            }

            return flow != null && flow.IsCarryingAt(cell) ? waterTint : idleTint;
        }
    }
}
