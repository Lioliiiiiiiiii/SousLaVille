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
    /// La plaque posee sur une bouche d'egout : sa case, et le rang dans le catalogue.
    /// Purement decoratif ; une bouche absente de la liste garde son allure d'usine.
    /// </summary>
    [Serializable]
    public class SaveCover
    {
        public SaveCell cell;
        public int cover;
    }

    /// <summary>
    /// Le type de tuyau pose sur une case : sa case, et le rang dans le catalogue.
    ///
    /// SEULES LES CASES NON STANDARD SONT ECRITES, comme seuls les segments abimes le sont :
    /// le standard est le rang 0 et c'est ce que PlacePipe pose sans qu'on lui demande rien.
    /// Chaque ligne du fichier est donc un choix, pas un etat.
    /// </summary>
    [Serializable]
    public class SavePipeType
    {
        public SaveCell cell;
        public int type;
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
        ///
        /// PASSEE A 2 EN PHASE 12A, et c'est la premiere fois depuis la phase 6. Le critere est
        /// celui que la phase 6 avait ecrit : c'est le SENS des cases qui change. La carte passe
        /// de 40x30 a 64x45, et une case d'une partie de la phase 11 ne designe plus le meme
        /// endroit du monde. Rejouer ses creusements et ses poses produirait un reseau absurde
        /// sans une erreur, puisque Dig et PlacePipe refusent proprement. Une vieille partie est
        /// donc mise de cote avec un horodatage, bruyamment, et le jeu repart neuf.
        /// </summary>
        public const int CurrentVersion = 2;

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

        /// <summary>
        /// Les plaques posees sur les bouches. Champ ajoute en phase 7 : une partie ecrite
        /// avant lui se relit sans erreur, la liste arrive simplement vide. C'est pourquoi
        /// CurrentVersion reste a 1.
        /// </summary>
        public List<SaveCover> covers = new List<SaveCover>();

        /// <summary>
        /// Le niveau du bassin d'orage, en unites. Champ ajoute en phase 8, meme regle que
        /// covers : une partie ecrite avant lui se relit avec un bassin vide, et
        /// CurrentVersion reste a 1.
        /// </summary>
        public int reserveLevel;

        /// <summary>
        /// Les cases qui portent autre chose qu'un tuyau standard. Champ ajoute en phase 9b,
        /// meme regle que covers et reserveLevel : une partie ecrite avant lui se relit avec
        /// un reseau tout standard, ce qu'il etait, et CurrentVersion reste a 1.
        /// </summary>
        public List<SavePipeType> pipeTypes = new List<SavePipeType>();

        /// <summary>
        /// Le rang du type en main. Zero, le standard, pour une partie d'avant la phase 9b.
        ///
        /// Exception assumee a « on ne sauvegarde pas ce qu'on ne relit pas » : redescendre
        /// pour decouvrir qu'on a repris le standard serait une surprise, et le jeu ne fait
        /// pas de surprises.
        /// </summary>
        public int pipeInHand;
    }
}
