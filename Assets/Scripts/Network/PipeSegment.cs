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
        public PipeType PipeType => pipeType;

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
