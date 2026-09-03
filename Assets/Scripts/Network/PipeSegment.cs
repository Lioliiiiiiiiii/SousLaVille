using System;
using UnityEngine;

namespace SousLaVille.Network
{
    /// <summary>
    /// Une canalisation entre deux noeuds voisins.
    ///
    /// condition, isFrozen et isClogged ne servent qu'a partir de la phase 5. Ils sont poses
    /// des maintenant parce que le modele de donnees de CLAUDE.md les impose et que les
    /// ajouter apres coup casserait les sauvegardes de la phase 6.
    ///
    /// DEPUIS LA PHASE 9B, le type ne vit plus ici mais sur le noeud, et le segment en deduit
    /// ses resistances : il est aussi faible que sa plus faible extremite. Le champ pipeType
    /// reste comme type par defaut du monde, pour un segment dont aucun bout ne porte de
    /// type. C'est la meme logique que « un segment est aussi expose que son extremite la
    /// moins profonde », posee en phase 5.
    ///
    /// Consequence lisible en jeu : une route isolee l'est de bout en bout, ou elle ne l'est
    /// pas. Un seul tuyau standard au milieu gele, et sa couleur d'hiver le designe.
    /// </summary>
    [Serializable]
    public class PipeSegment
    {
        [SerializeField] private PipeNode nodeA;
        [SerializeField] private PipeNode nodeB;
        [SerializeField] private PipeType pipeType;
        [SerializeField] private float condition = 1f;
        [SerializeField] private bool isFrozen;
        [SerializeField] private bool isClogged;

        public PipeSegment(PipeNode nodeA, PipeNode nodeB, PipeType pipeType)
        {
            this.nodeA = nodeA;
            this.nodeB = nodeB;
            this.pipeType = pipeType;
        }

        public PipeNode NodeA => nodeA;
        public PipeNode NodeB => nodeB;

        /// <summary>
        /// Le type par defaut du monde, retenu seulement si aucune des deux extremites ne
        /// porte de type. Ce n'est PAS le type du tuyau : celui-la vit sur le noeud.
        /// </summary>
        public PipeType DefaultPipeType => pipeType;

        /// <summary>Resistance au gel retenue : celle du bout le plus faible.</summary>
        public float FrostResistance => Weakest(frost: true);

        /// <summary>Resistance aux feuilles retenue : celle du bout le plus faible.</summary>
        public float LeafResistance => Weakest(frost: false);

        /// <summary>
        /// Usure retenue : la PLUS FORTE des deux bouts, puisque c'est encore le bout le plus
        /// faible qui decide. Les trois types s'usent pareil aujourd'hui ; la regle est ecrite
        /// pour le jour ou ce ne sera plus vrai.
        /// </summary>
        public float WearPerSeason
        {
            get
            {
                PipeType a, b;
                if (!ResolveEnds(out a, out b))
                {
                    return 0f;
                }

                return Mathf.Max(a.WearPerSeason, b.WearPerSeason);
            }
        }

        /// <summary>
        /// La plus basse des deux resistances. Une extremite sans type, station ou maison,
        /// ne compte pas : c'est l'autre bout qui decide seul.
        /// </summary>
        private float Weakest(bool frost)
        {
            PipeType a, b;
            if (!ResolveEnds(out a, out b))
            {
                return 0f;
            }

            float ra = frost ? a.FrostResistance : a.LeafResistance;
            float rb = frost ? b.FrostResistance : b.LeafResistance;
            return Mathf.Min(ra, rb);
        }

        /// <summary>
        /// Les types des deux bouts. Un bout sans type prend celui de l'autre ; si aucun des
        /// deux n'en a, le type par defaut du monde tranche. Rend false s'il n'y a rien du
        /// tout, cas ou le segment ne resiste a rien et ne s'use pas.
        /// </summary>
        private bool ResolveEnds(out PipeType a, out PipeType b)
        {
            a = nodeA != null ? nodeA.PipeType : null;
            b = nodeB != null ? nodeB.PipeType : null;

            if (a == null)
            {
                a = b;
            }

            if (b == null)
            {
                b = a;
            }

            if (a == null)
            {
                a = pipeType;
                b = pipeType;
            }

            return a != null && b != null;
        }

        /// <summary>Etat du tuyau, de 0 a 1. En dessous de 0,3 il ne transporte plus.</summary>
        public float Condition
        {
            get { return condition; }
            set { condition = Mathf.Clamp01(value); }
        }

        public bool IsFrozen
        {
            get { return isFrozen; }
            set { isFrozen = value; }
        }

        public bool IsClogged
        {
            get { return isClogged; }
            set { isClogged = value; }
        }

        /// <summary>L'autre bout du segment, vu depuis un de ses noeuds.</summary>
        public PipeNode Other(PipeNode node)
        {
            return node == nodeA ? nodeB : nodeA;
        }

        /// <summary>Vrai si le segment touche cette case.</summary>
        public bool Touches(Vector2Int cell)
        {
            return nodeA.GridPos == cell || nodeB.GridPos == cell;
        }
    }
}
