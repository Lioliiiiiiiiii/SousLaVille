using System;
using System.Collections.Generic;
using UnityEngine;

namespace SousLaVille.Buildings
{
    /// <summary>
    /// L'atelier des plaques. Il connait le catalogue, ou sont exposees ses propres plaques,
    /// ou sont les bouches du village, et laquelle porte quoi.
    ///
    /// Il vit dans la scene Surface, comme PipeNetwork vit dans l'Underground : une couche
    /// eteinte n'est pas dechargee, son etat survit, et SaveSystem sait aller le chercher
    /// avec FindObjectsInactive.Include.
    ///
    /// Rien ici ne touche au reseau : la plaque est purement decorative.
    /// </summary>
    public class ManholeFactory : MonoBehaviour
    {
        [Tooltip("Les huit plaques, dans l'ordre du catalogue.")]
        [SerializeField] private ManholeCoverDefinition[] catalogue;

        [Tooltip("Les cases ou ces plaques sont exposees, dans le meme ordre.")]
        [SerializeField] private Vector2Int[] sampleCells;

        [Tooltip("Les bouches d'egout du village, telles que le plan les donne.")]
        [SerializeField] private Vector2Int[] manholeCells;

        /// <summary>Ce que porte chaque bouche. Une bouche absente garde son allure d'usine.</summary>
        private readonly Dictionary<Vector2Int, int> coverByManhole =
            new Dictionary<Vector2Int, int>();

        /// <summary>Leve a chaque pose. Les bouches s'y accrochent, la sauvegarde aussi.</summary>
        public event Action Changed;

        public int CatalogueCount => catalogue != null ? catalogue.Length : 0;

        public IReadOnlyList<Vector2Int> ManholeCells =>
            manholeCells ?? Array.Empty<Vector2Int>();

        public ManholeCoverDefinition CoverAt(int index)
        {
            if (catalogue == null || index < 0 || index >= catalogue.Length)
            {
                return null;
            }

            return catalogue[index];
        }

        /// <summary>Le rang de la plaque exposee sur cette case, ou -1 si la case n'en porte pas.</summary>
        public int SampleIndexAt(Vector2Int cell)
        {
            if (sampleCells == null)
            {
                return -1;
            }

            for (int i = 0; i < sampleCells.Length; i++)
            {
                if (sampleCells[i] == cell)
                {
                    return i;
                }
            }

            return -1;
        }

        /// <summary>Le rang de la plaque posee sur une bouche, ou -1 si elle est d'origine.</summary>
        public int IndexFor(Vector2Int manholeCell)
        {
            int index;
            return coverByManhole.TryGetValue(manholeCell, out index) ? index : -1;
        }

        /// <summary>La plaque posee sur une bouche, ou null si elle est restee d'origine.</summary>
        public ManholeCoverDefinition CoverFor(Vector2Int manholeCell)
        {
            return CoverAt(IndexFor(manholeCell));
        }

        /// <summary>
        /// Pose une plaque sur une bouche. Reposer autre chose remplace : il n'y a jamais
        /// d'erreur a corriger, seulement un choix a refaire.
        /// </summary>
        public void SetCover(Vector2Int manholeCell, int index)
        {
            if (CoverAt(index) == null)
            {
                return;
            }

            coverByManhole[manholeCell] = index;
            Changed?.Invoke();
        }

        /// <summary>Ce qu'il faut ecrire dans la sauvegarde.</summary>
        public IReadOnlyDictionary<Vector2Int, int> Assignments => coverByManhole;

        /// <summary>
        /// Repose les plaques lues dans une sauvegarde, et ne previent qu'une fois. Une plaque
        /// dont le rang n'existe plus dans le catalogue est ignoree en silence, et la bouche
        /// garde son allure d'usine.
        /// </summary>
        public void Restore(IEnumerable<KeyValuePair<Vector2Int, int>> assignments)
        {
            coverByManhole.Clear();

            if (assignments != null)
            {
                foreach (KeyValuePair<Vector2Int, int> entry in assignments)
                {
                    if (CoverAt(entry.Value) != null)
                    {
                        coverByManhole[entry.Key] = entry.Value;
                    }
                }
            }

            Changed?.Invoke();
        }
    }
}
