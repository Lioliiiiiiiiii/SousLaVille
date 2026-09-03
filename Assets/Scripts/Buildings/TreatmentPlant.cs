using UnityEngine;

namespace SousLaVille.Buildings
{
    /// <summary>
    /// La station d'epuration. Elle existait comme sprite et comme noeud PlantInlet depuis
    /// la phase 1 ; elle devient un objet le jour ou elle a une propriete a porter : ce
    /// qu'elle traite par saison.
    ///
    /// Cette capacite n'a pas sa place dans le systeme des saisons, c'est une propriete de
    /// la station. C'est elle qui fait deborder l'automne : huit unites traitees, quand cinq
    /// maisons et huit de pluie en apportent treize.
    ///
    /// Vit dans la scene Underground, sur l'arrivee de la station, a cote du noeud.
    /// </summary>
    public class TreatmentPlant : MonoBehaviour
    {
        [Tooltip("Ce que la station traite par saison, en unites d'eau. Le reste deborde ou va au bassin.")]
        [Min(0)]
        [SerializeField] private int capacityPerSeason = 8;

        public int CapacityPerSeason => capacityPerSeason;
    }
}
