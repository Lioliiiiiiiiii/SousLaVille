using UnityEngine;
using UnityEngine.Tilemaps;

namespace SousLaVille.Network
{
    /// <summary>
    /// Le rendu du reseau. Chaque noeud recoit la tuile qui correspond a son masque de
    /// raccords : un tuyau isole, un coude, un T et un croisement se distinguent donc sans
    /// code de dessin particulier.
    ///
    /// Un tuyau qui porte de l'eau est teinte en bleu, les autres restent gris. Aucune image
    /// supplementaire : une couleur par case suffit a montrer tout le trajet d'un coup d'oeil.
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
                pipes.SetColor(position,
                    flow != null && flow.IsCarryingAt(node.GridPos) ? waterTint : Color.white);
            }
        }
    }
}
