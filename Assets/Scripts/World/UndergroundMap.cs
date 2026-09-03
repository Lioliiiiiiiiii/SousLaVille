using System;
using System.Collections.Generic;
using SousLaVille.Core;
using UnityEngine;
using UnityEngine.Tilemaps;

namespace SousLaVille.World
{
    /// <summary>
    /// La carte du sous-sol. Meme repere et meme taille que le village : la case (8, 19) du
    /// sous-sol est exactement sous la case (8, 19) de la surface. C'est ce qui rend le
    /// portail comprehensible sans un mot d'explication.
    ///
    /// Au depart, presque tout est de la terre pleine. Seules les galeries deja creusees se
    /// parcourent ; la phase 3 ouvrira le reste.
    ///
    /// Le code n'est volontairement pas partage avec SurfaceMap : des la phase 3 le sous-sol
    /// devient mutable et la surface non. Trois lignes dupliquees valent mieux qu'un couplage
    /// premature.
    /// </summary>
    public class UndergroundMap : GridMap
    {
        [Tooltip("Terre et galeries. Purement decoratif.")]
        [SerializeField] private Tilemap ground;

        [Tooltip("Terre pleine. Une tuile ici veut dire : on ne passe pas, il faudra creuser.")]
        [SerializeField] private Tilemap blocking;

        [Tooltip("Profondeur de chaque case, 1 a 3, indexee x + y * largeur. Remplie par le builder.")]
        [SerializeField] private int[] depths;

        [Tooltip("Sol de galerie, une tuile par profondeur. Peinte au moment de creuser.")]
        [SerializeField] private TileBase[] tunnelTilesByDepth;

        /// <summary>
        /// Les cases ouvertes EN JEU. Les galeries deja creusees par le plan n'y sont pas :
        /// elles viennent de la scene, pas du joueur, et c'est exactement ce que la
        /// sauvegarde doit distinguer.
        /// </summary>
        private readonly HashSet<Vector2Int> dugCells = new HashSet<Vector2Int>();

        /// <summary>Leve a chaque coup de pelle reussi. La sauvegarde s'y accroche.</summary>
        public event Action<Vector2Int> Dug;

        public override GameLayer Layer => GameLayer.Underground;

        /// <summary>Les cases ouvertes en jeu, dans l'ordre ou elles ont ete creusees.</summary>
        public IReadOnlyCollection<Vector2Int> DugCells => dugCells;

        /// <summary>Tilemap du sol. La phase 3 y peindra le sol des galeries creusees.</summary>
        public Tilemap Ground => ground;

        /// <summary>Tilemap de la terre pleine. Creuser, c'est retirer une tuile d'ici.</summary>
        public Tilemap Blocking => blocking;

        /// <summary>
        /// Profondeur de la case : 1 peu profond, 2 moyen, 3 profond. Elle est peinte dans le
        /// plan, jamais choisie par le joueur. C'est elle qui porte le puzzle.
        ///
        /// Tableau serialise plutot que relecture de tuile : c'est une donnee de carte
        /// statique, la tuile n'en est que l'affichage.
        /// </summary>
        public int DepthAt(Vector2Int cell)
        {
            if (!Contains(cell) || depths == null)
            {
                return 1;
            }

            int index = cell.x + cell.y * CellBounds.size.x;
            if (index < 0 || index >= depths.Length)
            {
                return 1;
            }

            return Mathf.Clamp(depths[index], 1, 3);
        }

        /// <summary>
        /// Ouvre une case de terre pleine. Rend false si la case n'etait pas pleine : il ne
        /// se passe rien, aucun echec n'est puni.
        /// </summary>
        public bool Dig(Vector2Int cell)
        {
            if (!Contains(cell))
            {
                return false;
            }

            Vector3Int position = new Vector3Int(cell.x, cell.y, 0);
            if (!blocking.HasTile(position))
            {
                return false;
            }

            blocking.SetTile(position, null);

            // Le sol prend la nuance de sa profondeur : la galerie fraiche se lit comme les
            // autres.
            if (tunnelTilesByDepth != null && tunnelTilesByDepth.Length >= 3)
            {
                ground.SetTile(position, tunnelTilesByDepth[DepthAt(cell) - 1]);
            }

            dugCells.Add(cell);
            Dug?.Invoke(cell);

            return true;
        }

        public override bool IsWalkable(Vector2Int cell)
        {
            if (!Contains(cell))
            {
                return false;
            }

            return !blocking.HasTile(new Vector3Int(cell.x, cell.y, 0));
        }
    }
}
