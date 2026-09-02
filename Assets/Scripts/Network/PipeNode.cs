using System;
using UnityEngine;

namespace SousLaVille.Network
{
    /// <summary>Ce qu'un noeud represente dans le reseau.</summary>
    public enum NodeType
    {
        Junction,
        HouseConnection,
        Manhole,
        PlantInlet,
        FountainInlet
    }

    /// <summary>
    /// Un point du reseau, pose sur une case du sous-sol.
    ///
    /// La profondeur ne se choisit pas : elle est lue sur la carte au moment de la pose.
    /// C'est elle qui porte tout le puzzle, la regle de CLAUDE.md etant qu'un segment ne
    /// transporte que si la profondeur ne diminue pas dans le sens de l'ecoulement.
    ///
    /// Classe serialisable et non MonoBehaviour : la sauvegarde de la phase 6 doit pouvoir
    /// l'ecrire telle quelle.
    /// </summary>
    [Serializable]
    public class PipeNode
    {
        [SerializeField] private Vector2Int gridPos;
        [SerializeField] private int depth;
        [SerializeField] private NodeType type;

        public PipeNode(Vector2Int gridPos, int depth, NodeType type)
        {
            this.gridPos = gridPos;
            this.depth = depth;
            this.type = type;
        }

        public Vector2Int GridPos => gridPos;

        /// <summary>1 peu profond, 2 moyen, 3 profond.</summary>
        public int Depth => depth;

        public NodeType Type => type;

        /// <summary>
        /// Un noeud pose par le monde, station ou maison, ne s'enleve pas. Seules les
        /// jonctions posees par le joueur se defont.
        /// </summary>
        public bool IsPermanent => type != NodeType.Junction;
    }
}
