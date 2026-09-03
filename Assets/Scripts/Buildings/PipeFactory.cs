using System;
using System.Collections.Generic;
using SousLaVille.Network;
using UnityEngine;

namespace SousLaVille.Buildings
{
    /// <summary>
    /// L'usine a tuyaux. Elle connait le catalogue des trois types, ou ses echantillons sont
    /// exposes, et LE TYPE EN MAIN.
    ///
    /// Pas de stock, pas de compte : les tuyaux sont illimites, et le choix se fait ici. On
    /// descend avec un type en main et on remonte pour en changer. Un enfant de six ans ne
    /// doit jamais tomber en panne au fond d'une galerie.
    ///
    /// Meme patron que ManholeFactory, et elle vit dans la meme scene, Interiors : une couche
    /// eteinte n'est pas dechargee, son etat survit, et qui la cherche depuis une autre couche
    /// la trouve avec FindObjectsInactive.Include. C'est indispensable ici : on pose des
    /// tuyaux SOUS TERRE, donc l'usine est toujours eteinte au moment ou l'on s'en sert.
    /// </summary>
    public class PipeFactory : MonoBehaviour
    {
        [Tooltip("Les trois types, dans l'ordre du catalogue. Le rang 0 est le standard.")]
        [SerializeField] private PipeType[] catalogue;

        [Tooltip("Les cases ou ces echantillons sont exposes, dans le meme ordre.")]
        [SerializeField] private Vector2Int[] sampleCells;

        [Tooltip("Rang du type en main. Zero au demarrage : on part avec le standard.")]
        [SerializeField] private int currentIndex;

        /// <summary>Leve a chaque changement de type en main. La sauvegarde s'y accroche.</summary>
        public event Action Changed;

        public int CatalogueCount => catalogue != null ? catalogue.Length : 0;

        /// <summary>Rang du type en main. Sauvegarde, parce qu'il se relit.</summary>
        public int CurrentIndex => currentIndex;

        /// <summary>Le type en main. Jamais null tant que le catalogue n'est pas vide.</summary>
        public PipeType Current => TypeAt(currentIndex);

        public IReadOnlyList<Vector2Int> SampleCells =>
            sampleCells ?? Array.Empty<Vector2Int>();

        /// <summary>
        /// Le type d'un rang, ou le standard si le rang n'existe pas. En silence : une
        /// sauvegarde ecrite avec un catalogue plus long ne doit pas casser la partie.
        /// </summary>
        public PipeType TypeAt(int index)
        {
            if (catalogue == null || catalogue.Length == 0)
            {
                return null;
            }

            if (index < 0 || index >= catalogue.Length)
            {
                return catalogue[0];
            }

            return catalogue[index];
        }

        /// <summary>Rang d'un type dans le catalogue, ou 0 s'il n'y est pas.</summary>
        public int IndexOf(PipeType type)
        {
            if (catalogue == null || type == null)
            {
                return 0;
            }

            for (int i = 0; i < catalogue.Length; i++)
            {
                if (catalogue[i] == type)
                {
                    return i;
                }
            }

            return 0;
        }

        /// <summary>Le rang de l'echantillon expose sur cette case, ou -1.</summary>
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

        /// <summary>
        /// Prend un type en main. Reprendre celui qu'on a deja ne previent personne : rien
        /// n'a change, et une sauvegarde de plus ne servirait a rien.
        /// </summary>
        public void SetCurrent(int index)
        {
            int clamped = catalogue != null && catalogue.Length > 0
                ? Mathf.Clamp(index, 0, catalogue.Length - 1)
                : 0;

            if (clamped == currentIndex)
            {
                return;
            }

            currentIndex = clamped;
            Changed?.Invoke();
        }

        /// <summary>
        /// Repose le type en main lu dans une sauvegarde. Un rang qui n'existe plus retombe
        /// sur le standard, en silence, comme une plaque disparue du catalogue.
        /// </summary>
        public void Restore(int index)
        {
            currentIndex = catalogue != null && catalogue.Length > 0
                ? Mathf.Clamp(index, 0, catalogue.Length - 1)
                : 0;

            Changed?.Invoke();
        }
    }
}
