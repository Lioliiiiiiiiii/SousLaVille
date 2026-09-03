using System;
using System.Collections.Generic;
using UnityEngine;

namespace SousLaVille.Core
{
    /// <summary>
    /// Une case, en deux entiers explicites.
    ///
    /// Volontairement pas un Vector2Int : Newtonsoft ecrirait aussi ses proprietes calculees
    /// magnitude et sqrMagnitude, qu'il ne saurait pas relire. Le fichier ne doit pas dependre
    /// de la facon dont Unity serialise ses types.
    /// </summary>
    [Serializable]
    public class SaveCell
    {
        public int x;
        public int y;

        /// <summary>Constructeur sans argument : Newtonsoft en a besoin pour relire.</summary>
        public SaveCell()
        {
        }

        public SaveCell(Vector2Int cell)
        {
            x = cell.x;
            y = cell.y;
        }

        public Vector2Int ToCell()
        {
            return new Vector2Int(x, y);
        }
    }

    /// <summary>
    /// L'etat d'un tuyau entre deux cases. Seuls les segments abimes sont ecrits : reposer un
    /// tuyau le recree neuf, donc ecrire un segment intact ne changerait rien au chargement.
    /// Chaque ligne du fichier est ainsi une cicatrice, et le fichier reste petit.
    /// </summary>
    [Serializable]
    public class SaveSegment
    {
        public SaveCell a;
        public SaveCell b;
        public float condition = 1f;
        public bool isFrozen;
        public bool isClogged;
    }

    /// <summary>
    /// La forme du fichier sur le disque, et rien d'autre. Aucune logique ici : cette classe
    /// doit se lire d'un coup d'oeil, parce qu'elle decrit ce qui survit a l'extinction du jeu.
    ///
    /// On sauvegarde des gestes et non un etat : la liste des cases creusees et des cases
    /// posees suffit, puisque Dig et PlacePipe sont deja les seules facons de modifier le
    /// monde. Consequence : tout etat charge est un etat atteignable en jouant.
    /// </summary>
    [Serializable]
    public class SaveData
    {
        /// <summary>
        /// A monter des que la forme du fichier change. Un champ ajoute se relit sans rien
        /// casser ; un champ dont le sens change, non.
        /// </summary>
        public const int CurrentVersion = 1;

        public int version = CurrentVersion;

        /// <summary>Rang de la saison dans le cycle.</summary>
        public int seasonIndex;

        /// <summary>Avancement dans la saison en cours, de 0 a 1.</summary>
        public float seasonProgress;

        /// <summary>Les cases ouvertes par le joueur. Les galeries du plan n'y sont pas.</summary>
        public List<SaveCell> dugCells = new List<SaveCell>();

        /// <summary>Les cases ou le joueur a pose un tuyau. Station et maisons exclues.</summary>
        public List<SaveCell> pipeCells = new List<SaveCell>();

        /// <summary>Les segments qui ne sont plus neufs.</summary>
        public List<SaveSegment> segments = new List<SaveSegment>();
    }
}
