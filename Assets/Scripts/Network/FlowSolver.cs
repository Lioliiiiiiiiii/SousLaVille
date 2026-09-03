using System;
using System.Collections.Generic;
using UnityEngine;

namespace SousLaVille.Network
{
    /// <summary>
    /// Le parcours de l'eau, tel que CLAUDE.md le decrit : pour chaque maison, un parcours en
    /// largeur jusqu'a la station. Aucune simulation de fluide, un graphe et une regle.
    ///
    /// Une arete n'est franchissable que si la profondeur ne diminue pas dans le sens de
    /// l'ecoulement, si le segment tient encore, s'il n'est pas gele et s'il n'est pas bouche.
    /// La regle de profondeur croissante remplace toute gravite.
    ///
    /// Ne tourne que sur changement de reseau, et plus tard aux ticks de saison. Jamais par
    /// frame.
    ///
    /// Vit dans la scene Persistent et non dans l'Underground : quand le joueur remonte, la
    /// racine du sous-sol s'eteint, et un solveur eteint ne pourrait plus repondre aux maisons
    /// de la surface, qui sont justement allumees a ce moment-la.
    /// </summary>
    public class FlowSolver : MonoBehaviour
    {
        /// <summary>
        /// En dessous, le tuyau est trop abime pour transporter. Publique : la vue peint en
        /// rouge terne a partir du meme seuil, il ne doit exister qu'a un seul endroit.
        /// </summary>
        public const float MinimumCondition = 0.3f;

        private PipeNetwork network;

        private readonly HashSet<PipeSegment> carrying = new HashSet<PipeSegment>();
        private readonly HashSet<Vector2Int> served = new HashSet<Vector2Int>();
        private readonly List<PipeNode> houses = new List<PipeNode>();

        /// <summary>Leve apres chaque resolution. Les vues s'y accrochent.</summary>
        public event Action Solved;

        /// <summary>Nombre de resolutions depuis le demarrage. Sert a verifier qu'on ne tourne pas par frame.</summary>
        public int SolveCount { get; private set; }

        /// <summary>Maisons raccordees a la station, et nombre total de maisons.</summary>
        public int ServedCount => served.Count;

        public int HouseCount => houses.Count;

        public bool IsServed(Vector2Int houseCell) => served.Contains(houseCell);

        public bool IsCarrying(PipeSegment segment) => carrying.Contains(segment);

        /// <summary>Vrai si au moins un segment pose sur cette case porte de l'eau.</summary>
        public bool IsCarryingAt(Vector2Int cell)
        {
            if (network == null)
            {
                return false;
            }

            foreach (PipeSegment segment in network.SegmentsAt(cell))
            {
                if (carrying.Contains(segment))
                {
                    return true;
                }
            }

            return false;
        }

        /// <summary>
        /// Resolution paresseuse, comme pour les cartes depuis la phase 1 : le solveur vit
        /// dans Persistent, qui est chargee AVANT l'Underground. Au Start le reseau n'existe
        /// pas encore, et resoudre une seule fois laisserait le solveur muet pour toujours.
        ///
        /// Include : une fois trouve, le reseau reste valable meme quand la couche s'eteint.
        /// </summary>
        private void Update()
        {
            if (network != null)
            {
                return;
            }

            network = FindAnyObjectByType<PipeNetwork>(FindObjectsInactive.Include);
            if (network == null)
            {
                return;
            }

            network.Changed += Solve;
            Solve();
        }

        private void OnDestroy()
        {
            if (network != null)
            {
                network.Changed -= Solve;
            }
        }

        /// <summary>
        /// Recalcule tout. Cinq maisons sur quelques centaines de noeuds : le cout est
        /// invisible, et c'est plus simple qu'un calcul incremental a maintenir juste.
        /// </summary>
        public void Solve()
        {
            carrying.Clear();
            served.Clear();
            houses.Clear();

            if (network == null)
            {
                return;
            }

            foreach (PipeNode node in network.Nodes)
            {
                if (node.Type == NodeType.HouseConnection)
                {
                    houses.Add(node);
                }
            }

            foreach (PipeNode house in houses)
            {
                if (TraceToPlant(house))
                {
                    served.Add(house.GridPos);
                }
            }

            SolveCount++;
            Solved?.Invoke();
        }

        /// <summary>
        /// Parcours en largeur depuis une maison. Si la station est atteinte, on remonte le
        /// chemin et on marque ses segments comme porteurs.
        /// </summary>
        private bool TraceToPlant(PipeNode house)
        {
            Dictionary<Vector2Int, PipeSegment> cameFrom = new Dictionary<Vector2Int, PipeSegment>();
            HashSet<Vector2Int> visited = new HashSet<Vector2Int> { house.GridPos };
            Queue<PipeNode> queue = new Queue<PipeNode>();
            queue.Enqueue(house);

            while (queue.Count > 0)
            {
                PipeNode current = queue.Dequeue();

                if (current.Type == NodeType.PlantInlet)
                {
                    MarkPath(cameFrom, current, house);
                    return true;
                }

                foreach (PipeSegment segment in network.SegmentsAt(current.GridPos))
                {
                    if (!CanCarry(segment))
                    {
                        continue;
                    }

                    PipeNode next = segment.Other(current);

                    // La regle : l'eau ne remonte pas.
                    if (next.Depth < current.Depth || !visited.Add(next.GridPos))
                    {
                        continue;
                    }

                    cameFrom[next.GridPos] = segment;
                    queue.Enqueue(next);
                }
            }

            return false;
        }

        private static bool CanCarry(PipeSegment segment)
        {
            return segment.Condition > MinimumCondition && !segment.IsFrozen && !segment.IsClogged;
        }

        private void MarkPath(Dictionary<Vector2Int, PipeSegment> cameFrom, PipeNode plant,
            PipeNode house)
        {
            PipeNode current = plant;

            while (current != house)
            {
                PipeSegment segment;
                if (!cameFrom.TryGetValue(current.GridPos, out segment))
                {
                    return;
                }

                carrying.Add(segment);
                current = segment.Other(current);
            }
        }
    }
}
