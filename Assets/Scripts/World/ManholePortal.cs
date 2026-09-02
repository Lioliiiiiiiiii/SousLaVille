using System.Collections.Generic;
using SousLaVille.Core;
using UnityEngine;

namespace SousLaVille.World
{
    /// <summary>
    /// Un passage entre les deux couches. Le meme composant sert a la bouche d'egout vue de
    /// la surface et a l'echelle vue d'en dessous : seules les couches de depart et d'arrivee
    /// changent.
    ///
    /// Aucun collider, aucun declencheur physique : le joueur est sur le portail quand sa
    /// case est celle du portail. Un enfant ne peut pas descendre par accident en frolant une
    /// bouche.
    /// </summary>
    public class ManholePortal : MonoBehaviour
    {
        [Tooltip("Case sur laquelle il faut se tenir pour emprunter ce passage.")]
        [SerializeField] private Vector2Int cell;

        [Tooltip("Couche ou vit ce portail.")]
        [SerializeField] private GameLayer layer;

        [Tooltip("Couche atteinte en empruntant ce passage.")]
        [SerializeField] private GameLayer destinationLayer = GameLayer.Underground;

        [Tooltip("Case d'arrivee. Identique a la case de depart : les deux cartes sont alignees.")]
        [SerializeField] private Vector2Int destinationCell;

        // Les deux couches restent chargees, mais celle qui dort est desactivee : ses
        // portails se desinscrivent tout seuls. La recherche ne voit donc que la couche
        // active, ce que la couche demandee confirme de toute facon.
        private static readonly List<ManholePortal> Active = new List<ManholePortal>();

        public Vector2Int Cell => cell;
        public GameLayer Layer => layer;
        public GameLayer DestinationLayer => destinationLayer;
        public Vector2Int DestinationCell => destinationCell;

        private void OnEnable()
        {
            Active.Add(this);
        }

        private void OnDisable()
        {
            Active.Remove(this);
        }

        /// <summary>
        /// Portail d'une couche donnee pose sur une case donnee, ou null. La couche fait
        /// partie de la recherche : une bouche et son echelle occupent la meme case.
        /// </summary>
        public static ManholePortal Find(GameLayer layer, Vector2Int cell)
        {
            foreach (ManholePortal portal in Active)
            {
                if (portal.layer == layer && portal.cell == cell)
                {
                    return portal;
                }
            }

            return null;
        }
    }
}
