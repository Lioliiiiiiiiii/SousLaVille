using System;
using System.Collections.Generic;
using UnityEngine;

namespace SousLaVille.Network
{
    /// <summary>
    /// Le parcours de l'eau, tel que CLAUDE.md le decrit : pour chaque maison, un parcours en
    /// largeur jusqu'a la station. Aucune simulation de fluide, un graphe et une regle.
    ///
    /// Depuis la phase 8, le bassin d'orage est trace de la meme facon, sur un noeud de
    /// plus : il obeit au gel, aux bouchons et a l'usure sans une ligne de code
    /// particuliere. C'est le meme parcours.
    ///
    /// Une arete n'est franchissable que si le segment tient encore, s'il n'est pas gele et
    /// s'il n'est pas bouche. RIEN D'AUTRE.
    ///
    /// LA REGLE DE PROFONDEUR CROISSANTE A ETE RETIREE EN PHASE 21. Elle a tenu des phases 4 a
    /// 20, et CLAUDE.md en faisait le puzzle du jeu. Ce qui l'a emportee n'est pas une opinion
    /// mais une mesure, prise sur les quatorze destinations de la carte : elle ne rendait
    /// aucune maison impossible, et elle multipliait la longueur de la route par 2 a 3,6.
    /// Trois fois plus de creusements et de poses, sur un trace contre-intuitif, pour un
    /// enfant de six ans : le puzzle etait devenu un peage.
    ///
    /// Ce qui reste comme difficulte : le labyrinthe et la distance. C'est ce que Victorien
    /// aime, et c'est deja dans la carte.
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
        private readonly List<PipeNode> reserves = new List<PipeNode>();
        private readonly HashSet<Vector2Int> connectedReserves = new HashSet<Vector2Int>();
        private readonly List<PipeNode> fountains = new List<PipeNode>();

        /// <summary>
        /// Les cases que l'eau atteint depuis une source SANS parvenir a la station. C'est la
        /// frontiere : l'endroit exact ou la regle de profondeur casse.
        /// </summary>
        private readonly HashSet<Vector2Int> stranded = new HashSet<Vector2Int>();

        /// <summary>Leve apres chaque resolution. Les vues s'y accrochent.</summary>
        public event Action Solved;

        /// <summary>Nombre de resolutions depuis le demarrage. Sert a verifier qu'on ne tourne pas par frame.</summary>
        public int SolveCount { get; private set; }

        /// <summary>Maisons raccordees a la station, et nombre total de maisons.</summary>
        public int ServedCount => served.Count;

        public int HouseCount => houses.Count;

        /// <summary>Nombre de fontaines du monde. Une seule aujourd'hui.</summary>
        public int FountainCount => fountains.Count;

        /// <summary>
        /// Nombre de DESTINATIONS : maisons plus fontaines. C'est ce que le HUD compte en
        /// gouttes, et ce que le bilan de l'eau multiplie par le volume d'une saison.
        /// </summary>
        public int DestinationCount => houses.Count + fountains.Count;

        public bool IsServed(Vector2Int houseCell) => served.Contains(houseCell);

        /// <summary>Nombre de bassins que le plan du monde impose. Un seul pour l'instant.</summary>
        public int ReserveCount => reserves.Count;

        /// <summary>Vrai si au moins un bassin a une route valide jusqu'a la station.</summary>
        public bool IsReserveConnected => connectedReserves.Count > 0;

        /// <summary>Vrai si le bassin de cette case a une route valide jusqu'a la station.</summary>
        public bool IsReserveConnectedAt(Vector2Int reserveCell) => connectedReserves.Contains(reserveCell);

        public bool IsCarrying(PipeSegment segment) => carrying.Contains(segment);

        /// <summary>
        /// Vrai si l'eau monte jusqu'a cette case sans aller plus loin. Elle appartient donc a
        /// une route COMMENCEE mais qui n'aboutit pas.
        ///
        /// Ajoute en phase 12a, et c'est le retour qui manquait le plus. Jusque-la, une route de
        /// cinquante-sept segments fausse d'UNE SEULE case etait rendue entierement blanche,
        /// exactement comme un tuyau qu'on vient de poser : le parcours jetait son ensemble de
        /// cases atteintes des qu'il echouait. Le joueur apprenait « ça ne marche pas » apres
        /// soixante-treize appuis sur Espace, et rien ne lui disait ou.
        ///
        /// C'est aussi la seule chose que l'agrandissement de la carte degradait LINEAIREMENT
        /// avec la longueur des routes, et c'est ce qu'un personnage-guide doit pouvoir montrer :
        /// on ne peut pas expliquer ou ça casse tant que le jeu l'ignore.
        /// </summary>
        public bool IsStrandedAt(Vector2Int cell) => stranded.Contains(cell);

        /// <summary>Nombre de cases ou l'eau s'arrete en chemin. Sert aux verifications.</summary>
        public int StrandedCount => stranded.Count;

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
            fountains.Clear();
            stranded.Clear();
            reserves.Clear();
            connectedReserves.Clear();

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
                else if (node.Type == NodeType.FountainInlet)
                {
                    fountains.Add(node);
                }
                else if (node.Type == NodeType.ReserveInlet)
                {
                    reserves.Add(node);
                }
            }

            foreach (PipeNode house in houses)
            {
                if (TraceToPlant(house))
                {
                    served.Add(house.GridPos);
                }
            }

            // La fontaine est une destination comme une maison : meme parcours, memes
            // regles, et elle rejoint le meme ensemble des desservies. Le HUD et le bilan de
            // l'eau la comptent donc sans une ligne de plus, chacun lisant ServedCount.
            foreach (PipeNode fountain in fountains)
            {
                if (TraceToPlant(fountain))
                {
                    served.Add(fountain.GridPos);
                }
            }

            // Le bassin est une source comme une autre : meme parcours, memes regles. Sa
            // route porte la teinte de l'eau comme celle d'une maison, c'est ce qui dit au
            // joueur qu'il est relie.
            foreach (PipeNode reserve in reserves)
            {
                if (TraceToPlant(reserve))
                {
                    connectedReserves.Add(reserve.GridPos);
                }
            }

            SolveCount++;
            Solved?.Invoke();
        }

        /// <summary>
        /// Parcours en largeur depuis une source, maison ou bassin. Si la station est
        /// atteinte, on remonte le chemin et on marque ses segments comme porteurs.
        /// </summary>
        /// <summary>
        /// Parcours en largeur depuis une source. Si la station est atteinte, on remonte le
        /// chemin et on marque ses segments comme porteurs.
        ///
        /// SI ELLE NE L'EST PAS, on garde l'ensemble atteint : c'est jusque-la que l'eau monte,
        /// et pas plus loin. Voir IsStrandedAt.
        /// </summary>
        private bool TraceToPlant(PipeNode source)
        {
            Dictionary<Vector2Int, PipeSegment> cameFrom = new Dictionary<Vector2Int, PipeSegment>();
            HashSet<Vector2Int> visited = new HashSet<Vector2Int> { source.GridPos };
            Queue<PipeNode> queue = new Queue<PipeNode>();
            queue.Enqueue(source);

            while (queue.Count > 0)
            {
                PipeNode current = queue.Dequeue();

                if (current.Type == NodeType.PlantInlet)
                {
                    MarkPath(cameFrom, current, source);
                    return true;
                }

                foreach (PipeSegment segment in network.SegmentsAt(current.GridPos))
                {
                    if (!CanCarry(segment))
                    {
                        continue;
                    }

                    PipeNode next = segment.Other(current);

                    // PHASE 21 : PLUS DE REGLE DE PROFONDEUR ICI. Un tuyau qui tient, qui n'est
                    // ni gele ni bouche, transporte — quelle que soit la profondeur de ses deux
                    // bouts. La profondeur reste peinte dans la carte, elle ne decide plus rien.
                    //
                    // Mesure du 6 septembre 2026, sur les quatorze destinations de la carte :
                    // la regle ne rendait AUCUNE maison impossible, mais elle multipliait la
                    // longueur de la route par 2 a 3,6. La maison (27,35) demandait 113 cases
                    // au lieu de 31, soit trois fois plus de creusements ET de poses, sur un
                    // trace dont la forme n'a rien d'intuitif. C'est ce qui a coute la regle.
                    if (!visited.Add(next.GridPos))
                    {
                        continue;
                    }

                    cameFrom[next.GridPos] = segment;
                    queue.Enqueue(next);
                }
            }

            // La station n'a pas ete atteinte. On retient ou l'eau s'est arretee : ces cases
            // portent une route commencee qui n'aboutit pas, et c'est la seule chose que le
            // joueur puisse regarder pour comprendre.
            foreach (Vector2Int cell in visited)
            {
                stranded.Add(cell);
            }

            return false;
        }

        private static bool CanCarry(PipeSegment segment)
        {
            return segment.Condition > MinimumCondition && !segment.IsFrozen && !segment.IsClogged;
        }

        private void MarkPath(Dictionary<Vector2Int, PipeSegment> cameFrom, PipeNode plant,
            PipeNode source)
        {
            PipeNode current = plant;

            while (current != source)
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
