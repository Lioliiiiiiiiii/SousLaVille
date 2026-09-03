using System;
using System.Collections.Generic;
using SousLaVille.World;
using UnityEngine;

namespace SousLaVille.Network
{
    /// <summary>
    /// Le reseau, c'est un graphe et pas une simulation de fluide. Une liste d'adjacence,
    /// des noeuds sur des cases, des segments entre voisins.
    ///
    /// Poser un tuyau raccorde automatiquement le nouveau noeud a ses voisins deja poses :
    /// a six ans, mettre deux tuyaux cote a cote doit suffire, il n'y a pas de geste de
    /// raccordement.
    ///
    /// L'evenement Changed est le seul signal dont la phase 4 aura besoin : le FlowSolver
    /// tournera la-dessus et aux ticks de saison, jamais par frame.
    /// </summary>
    public class PipeNetwork : MonoBehaviour
    {
        /// <summary>Un noeud impose par le monde, decrit dans la scene et recree au reveil.</summary>
        [Serializable]
        private struct FixedNode
        {
            public Vector2Int cell;
            public NodeType type;
        }

        [Tooltip("Carte du sous-sol : elle donne la profondeur et dit ou l'on peut poser.")]
        [SerializeField] private UndergroundMap map;

        [Tooltip("Type pose par le joueur. Un seul en phase 3.")]
        [SerializeField] private PipeType defaultPipeType;

        [Tooltip("Noeuds imposes par le monde. La station pour l'instant, les maisons en phase 4.")]
        [SerializeField] private FixedNode[] fixedNodes;

        private readonly Dictionary<Vector2Int, PipeNode> nodes = new Dictionary<Vector2Int, PipeNode>();
        private readonly Dictionary<Vector2Int, List<PipeSegment>> adjacency =
            new Dictionary<Vector2Int, List<PipeSegment>>();
        private readonly List<PipeSegment> segments = new List<PipeSegment>();

        /// <summary>Les quatre voisins, dans l'ordre des bits du masque : nord, est, sud, ouest.</summary>
        private static readonly Vector2Int[] Neighbours =
        {
            Vector2Int.up, Vector2Int.right, Vector2Int.down, Vector2Int.left
        };

        /// <summary>Leve a chaque pose et a chaque retrait. Jamais par frame.</summary>
        public event Action Changed;

        public int NodeCount => nodes.Count;
        public int SegmentCount => segments.Count;
        public IEnumerable<PipeNode> Nodes => nodes.Values;
        public IReadOnlyList<PipeSegment> Segments => segments;

        private void Awake()
        {
            CreateFixedNodes();
        }

        public bool HasNode(Vector2Int cell)
        {
            return nodes.ContainsKey(cell);
        }

        public PipeNode NodeAt(Vector2Int cell)
        {
            PipeNode node;
            return nodes.TryGetValue(cell, out node) ? node : null;
        }

        /// <summary>Segments attaches a une case. Liste vide si la case ne porte rien.</summary>
        public IReadOnlyList<PipeSegment> SegmentsAt(Vector2Int cell)
        {
            List<PipeSegment> attached;
            return adjacency.TryGetValue(cell, out attached) ? attached : Array.Empty<PipeSegment>();
        }

        /// <summary>
        /// Masque des raccords d'une case : bit 0 nord, 1 est, 2 sud, 3 ouest. C'est ce que
        /// la vue lit pour choisir son image de tuyau.
        /// </summary>
        public int NeighbourMask(Vector2Int cell)
        {
            int mask = 0;

            for (int i = 0; i < Neighbours.Length; i++)
            {
                if (HasSegment(cell, cell + Neighbours[i]))
                {
                    mask |= 1 << i;
                }
            }

            return mask;
        }

        public bool HasSegment(Vector2Int a, Vector2Int b)
        {
            foreach (PipeSegment segment in SegmentsAt(a))
            {
                if (segment.Touches(b))
                {
                    return true;
                }
            }

            return false;
        }

        /// <summary>
        /// Pose un tuyau sur une case creusee et le raccorde a ses voisins. Rend false et ne
        /// fait rien si la case est pleine ou porte deja un noeud : aucun echec puni, il ne
        /// se passe simplement rien.
        /// </summary>
        public bool PlacePipe(Vector2Int cell)
        {
            if (map == null || nodes.ContainsKey(cell) || !map.IsWalkable(cell))
            {
                return false;
            }

            PipeNode node = CreateNode(cell, NodeType.Junction);
            ConnectToNeighbours(node);

            Changed?.Invoke();
            return true;
        }

        /// <summary>
        /// Retire le tuyau d'une case et les segments qui y arrivent. Refuse sur un noeud
        /// pose par le monde.
        /// </summary>
        public bool RemovePipe(Vector2Int cell)
        {
            PipeNode node;
            if (!nodes.TryGetValue(cell, out node) || node.IsPermanent)
            {
                return false;
            }

            List<PipeSegment> attached;
            if (adjacency.TryGetValue(cell, out attached))
            {
                // Copie : detacher modifie la liste que l'on parcourt.
                foreach (PipeSegment segment in attached.ToArray())
                {
                    DetachSegment(segment);
                }

                adjacency.Remove(cell);
            }

            nodes.Remove(cell);

            Changed?.Invoke();
            return true;
        }

        /// <summary>
        /// Vrai si au moins un segment de cette case demande une reparation : abime, gele
        /// ou bouche. C'est ce que lit le picto au-dessus de la tete pour annoncer si
        /// Espace va reparer ou enlever.
        /// </summary>
        public bool NeedsRepair(Vector2Int cell)
        {
            foreach (PipeSegment segment in SegmentsAt(cell))
            {
                if (IsDamaged(segment))
                {
                    return true;
                }
            }

            return false;
        }

        /// <summary>
        /// Repare tous les segments de cette case : condition remise a neuf, degel,
        /// debouchage. Rend false et ne fait rien si rien n'etait a reparer.
        ///
        /// Consequence assumee de la decision de phase 5 : enlever un tuyau casse demande
        /// de le reparer d'abord. Un seul geste, jamais deux touches.
        /// </summary>
        public bool Repair(Vector2Int cell)
        {
            bool repaired = false;

            foreach (PipeSegment segment in SegmentsAt(cell))
            {
                if (!IsDamaged(segment))
                {
                    continue;
                }

                segment.Condition = 1f;
                segment.IsFrozen = false;
                segment.IsClogged = false;
                repaired = true;
            }

            if (repaired)
            {
                Changed?.Invoke();
            }

            return repaired;
        }

        /// <summary>Un segment est a reparer des qu'il n'est plus neuf.</summary>
        private static bool IsDamaged(PipeSegment segment)
        {
            return segment.IsFrozen || segment.IsClogged || segment.Condition < 1f;
        }

        private void CreateFixedNodes()
        {
            if (fixedNodes == null)
            {
                return;
            }

            foreach (FixedNode fixedNode in fixedNodes)
            {
                if (nodes.ContainsKey(fixedNode.cell))
                {
                    continue;
                }

                CreateNode(fixedNode.cell, fixedNode.type);
            }
        }

        private PipeNode CreateNode(Vector2Int cell, NodeType type)
        {
            int depth = map != null ? map.DepthAt(cell) : 1;

            PipeNode node = new PipeNode(cell, depth, type);
            nodes.Add(cell, node);
            adjacency[cell] = new List<PipeSegment>();

            return node;
        }

        private void ConnectToNeighbours(PipeNode node)
        {
            foreach (Vector2Int direction in Neighbours)
            {
                PipeNode neighbour = NodeAt(node.GridPos + direction);
                if (neighbour == null)
                {
                    continue;
                }

                PipeSegment segment = new PipeSegment(node, neighbour, defaultPipeType);
                segments.Add(segment);
                adjacency[node.GridPos].Add(segment);
                adjacency[neighbour.GridPos].Add(segment);
            }
        }

        private void DetachSegment(PipeSegment segment)
        {
            segments.Remove(segment);

            RemoveFromAdjacency(segment.NodeA.GridPos, segment);
            RemoveFromAdjacency(segment.NodeB.GridPos, segment);
        }

        private void RemoveFromAdjacency(Vector2Int cell, PipeSegment segment)
        {
            List<PipeSegment> attached;
            if (adjacency.TryGetValue(cell, out attached))
            {
                attached.Remove(segment);
            }
        }
    }
}
