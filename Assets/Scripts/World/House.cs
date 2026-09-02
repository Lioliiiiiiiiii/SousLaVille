using SousLaVille.Core;
using SousLaVille.Network;
using UnityEngine;

namespace SousLaVille.World
{
    /// <summary>
    /// Une maison du village. Elle ne fait rien d'autre que montrer si elle est raccordee :
    /// une goutte pleine au-dessus du toit, ou une goutte vide.
    ///
    /// Pas de croix, pas de rouge : ne pas etre reliee n'est pas une faute, c'est un travail
    /// qui reste a faire.
    ///
    /// Fichier hors liste CLAUDE.md : une maison a un etat, il lui faut un composant. Le
    /// spawner ne peut pas porter cinq etats.
    /// </summary>
    public class House : MonoBehaviour
    {
        private SpriteRenderer drop;
        private Sprite servedSprite;
        private Sprite idleSprite;

        /// <summary>Case du village, et donc case de son alcove sous terre : les deux cartes sont alignees.</summary>
        public Vector2Int Cell { get; private set; }

        public void Initialize(Vector2Int cell, SpriteRenderer drop, Sprite servedSprite,
            Sprite idleSprite)
        {
            Cell = cell;
            this.drop = drop;
            this.servedSprite = servedSprite;
            this.idleSprite = idleSprite;

            Refresh();
        }

        /// <summary>Relit son etat aupres du solveur. Appele au reveil et a chaque resolution.</summary>
        public void Refresh()
        {
            if (drop == null)
            {
                return;
            }

            FlowSolver flow = GameManager.Instance != null ? GameManager.Instance.Flow : null;
            bool isServed = flow != null && flow.IsServed(Cell);

            drop.sprite = isServed ? servedSprite : idleSprite;
        }
    }
}
