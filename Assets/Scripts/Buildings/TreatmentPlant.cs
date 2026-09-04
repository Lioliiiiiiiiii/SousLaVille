using System;
using UnityEngine;

namespace SousLaVille.Buildings
{
    /// <summary>
    /// La station d'epuration. Elle existait comme sprite et comme noeud PlantInlet depuis
    /// la phase 1 ; elle est devenue un objet en phase 8 le jour ou elle a eu une propriete a
    /// porter : ce qu'elle traite par saison.
    ///
    /// PHASE 12D : CETTE CAPACITE CESSE D'ETRE UN NOMBRE ECRIT A LA MAIN. Elle vaut
    /// `baseCapacity + bassins`, et le joueur ajoute un bassin en appuyant sur Espace devant
    /// l'arrivee de la station, sous terre. Trois raisons, toutes tranchees le 4 septembre :
    ///
    /// 1. LE DEBORDEMENT REDEVIENT UN RETOUR PERMANENT. A capacite ecrite a la main et reglee
    ///    sur `D + 3`, une fois le bassin d'orage relie, `Lost` vaut zero POUR TOUJOURS : le
    ///    debordement n'apprend plus rien. Avec une station qui commence trop petite, il
    ///    redevient le signal « la station ne suit plus », a chaque maison de plus.
    /// 2. LA REGLE EST DERIVEE, PAS INVENTEE. Sur l'annee, l'arrivant vaut `4S + R` avec S
    ///    destinations desservies et R = 11 de pluie, et la station traite `4C`. La condition
    ///    `4S + 11 <= 4C` a pour minimum entier `C = S + 3`. Avec `baseCapacity = 3`, cela fait
    ///    EXACTEMENT UN BASSIN PAR DESTINATION RELIEE : la regle se voit en jouant, sans un mot.
    /// 3. RIEN NE SE PAIE. Il n'y a pas de monnaie dans ce jeu et il n'y en aura pas. La
    ///    difficulte est de COMPRENDRE qu'il faut agrandir, pas d'amasser de quoi le faire.
    ///
    /// Le nombre de bassins est du PROGRES DE JOUEUR, pas une donnee derivee : il se sauvegarde,
    /// contrairement aux maisons desservies ou a l'eau du village, qui se recalculent.
    ///
    /// Vit dans la scene Underground, sur l'arrivee de la station, a cote du noeud.
    /// </summary>
    public class TreatmentPlant : MonoBehaviour
    {
        [Tooltip("Ce que la station traite par saison sans aucun bassin ajouté. La pluie seule.")]
        [Min(0)]
        [SerializeField] private int baseCapacity = 3;

        [Tooltip("Capacité maximale, bassins compris. Vaut destinations + 3, écrit par le builder.")]
        [Min(0)]
        [SerializeField] private int maxCapacity = 16;

        [Tooltip("Bassins de traitement déjà construits. Sauvegardé : c'est du progrès de joueur.")]
        [Min(0)]
        [SerializeField] private int basins;

        /// <summary>Leve quand un bassin s'ajoute, ou quand une partie est relue.</summary>
        public event Action Changed;

        /// <summary>Ce que la station traite par saison, bassins compris.</summary>
        public int CapacityPerSeason => Mathf.Min(baseCapacity + basins, maxCapacity);

        /// <summary>Combien de bassins sont construits.</summary>
        public int Basins => basins;

        /// <summary>Combien la station pourrait en porter en tout.</summary>
        public int MaxBasins => Mathf.Max(0, maxCapacity - baseCapacity);

        /// <summary>Vrai s'il reste de la place pour un bassin de plus.</summary>
        public bool CanGrow => basins < MaxBasins;

        /// <summary>
        /// Ajoute un bassin. Rend faux si la station est deja au plafond : le picto ne
        /// s'affiche alors plus, et Espace ne fait rien devant l'arrivee. Aucun echec puni, on
        /// ne peut simplement plus agrandir ce qui est complet.
        /// </summary>
        public bool AddBasin()
        {
            if (!CanGrow)
            {
                return false;
            }

            basins++;
            Changed?.Invoke();
            return true;
        }

        /// <summary>
        /// Repose le nombre de bassins d'une partie relue. Comme partout ailleurs, on relit un
        /// GESTE — « le joueur a construit n bassins » — et la capacite s'en deduit.
        /// </summary>
        public void Restore(int savedBasins)
        {
            int clamped = Mathf.Clamp(savedBasins, 0, MaxBasins);
            if (clamped == basins)
            {
                return;
            }

            basins = clamped;
            Changed?.Invoke();
        }
    }
}
