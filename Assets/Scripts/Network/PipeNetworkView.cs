using UnityEngine;
using UnityEngine.Tilemaps;

namespace SousLaVille.Network
{
    /// <summary>
    /// Le rendu du reseau. Chaque noeud recoit la tuile qui correspond a son masque de
    /// raccords : un tuyau isole, un coude, un T et un croisement se distinguent donc sans
    /// code de dessin particulier.
    ///
    /// Tout est redessine a chaque changement. Quelques centaines de cases, et seulement sur
    /// action du joueur : le calcul incremental viendra s'il se voit un jour.
    /// </summary>
    public class PipeNetworkView : MonoBehaviour
    {
        [SerializeField] private PipeNetwork network;
        [SerializeField] private Tilemap pipes;

        [Tooltip("Seize tuiles, indexees par le masque de raccords.")]
        [SerializeField] private TileBase[] tilesByMask;

        private void OnEnable()
        {
            if (network == null)
            {
                return;
            }

            network.Changed += Redraw;
            Redraw();
        }

        private void OnDisable()
        {
            if (network != null)
            {
                network.Changed -= Redraw;
            }
        }

        private void Redraw()
        {
            if (pipes == null || tilesByMask == null || tilesByMask.Length < 16)
            {
                return;
            }

            pipes.ClearAllTiles();

            foreach (PipeNode node in network.Nodes)
            {
                int mask = network.NeighbourMask(node.GridPos);
                pipes.SetTile(new Vector3Int(node.GridPos.x, node.GridPos.y, 0), tilesByMask[mask]);
            }
        }
    }
}
