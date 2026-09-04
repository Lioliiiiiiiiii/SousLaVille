using UnityEngine;

namespace SousLaVille.Buildings
{
    /// <summary>
    /// La station qui GROSSIT A L'ECRAN, phase 12d. Un bassin de traitement apparait sur son
    /// sol a chaque agrandissement. Sans cela, le seul retour d'un appui sur Espace devant
    /// l'arrivee serait un nombre invisible : le progres du joueur doit se voir.
    ///
    /// Les cuves sont posees une fois pour toutes a la construction de la scene, et seules
    /// leur visibilite change. Rien ne s'instancie en jeu, rien ne se detruit.
    ///
    /// Vit dans la scene SURFACE, sur la station, alors que le geste se fait SOUS TERRE. La
    /// couche est donc ETEINTE au moment ou le bassin s'ajoute : ce composant s'abonne dans
    /// OnEnable, se desabonne dans OnDisable, et SE REAPPLIQUE au rallumage. C'est la regle du
    /// projet depuis la phase 1, et c'est ici qu'elle compte le plus : on remonte a la surface
    /// exactement pour voir ce qu'on vient de construire.
    /// </summary>
    public class PlantBasinsView : MonoBehaviour
    {
        [Tooltip("Une cuve par agrandissement possible, dans l'ordre où elles s'allument.")]
        [SerializeField] private SpriteRenderer[] basins;

        private TreatmentPlant plant;

        private void OnEnable()
        {
            Subscribe();
        }

        private void OnDisable()
        {
            if (plant != null)
            {
                plant.Changed -= Redraw;
            }
        }

        private void Update()
        {
            // La station vit dans une AUTRE scene, chargee dans un ordre qui n'est garanti par
            // rien. On retente tant qu'on ne l'a pas : resoudre une seule fois au Start est le
            // piege tombe en phase 1, en phase 4 et en phase 5.
            if (plant == null)
            {
                Subscribe();
            }
        }

        private void Subscribe()
        {
            if (plant != null)
            {
                Redraw();
                return;
            }

            plant = FindAnyObjectByType<TreatmentPlant>(FindObjectsInactive.Include);
            if (plant == null)
            {
                return;
            }

            plant.Changed += Redraw;
            Redraw();
        }

        /// <summary>Autant de cuves allumees que de bassins construits.</summary>
        private void Redraw()
        {
            if (basins == null || plant == null)
            {
                return;
            }

            for (int i = 0; i < basins.Length; i++)
            {
                if (basins[i] != null)
                {
                    basins[i].enabled = i < plant.Basins;
                }
            }
        }
    }
}
