using UnityEngine;

namespace SousLaVille.Buildings
{
    /// <summary>
    /// La fontaine du parc, au centre du labyrinthe de haies.
    ///
    /// Elle est une DESTINATION comme une maison : elle consomme une unite par saison, elle
    /// allume une goutte au HUD, et elle n'est desservie que si le solveur lui trouve une
    /// route jusqu'a la station, avec les memes exigences qu'ailleurs. A la profondeur 1, sa
    /// route gele donc en hiver et se bouche en automne comme celle des trois maisons peu
    /// profondes : la fontaine n'est pas un decor, c'est un chantier de plus.
    ///
    /// Elle n'a AUCUN etat propre, et rien a sauvegarder : elle jaillit si sa route est
    /// valide, elle s'arrete sinon. C'est FloodView qui pose son eau, puisqu'il possede deja
    /// la tilemap ; deux composants se disputeraient la meme.
    ///
    /// Le bassin bloque le passage, comme une maison : on l'atteint, on n'entre pas dedans.
    ///
    /// Fichier prevu par l'arborescence de CLAUDE.md depuis la phase 0, ecrit ici : c'est
    /// aussi le premier usage de NodeType.FountainInlet, le seul type du modele qui n'en
    /// avait aucun.
    /// </summary>
    public class Fountain : MonoBehaviour
    {
        [Tooltip("La case de la fontaine, telle que le plan du village la donne.")]
        [SerializeField] private Vector2Int cell;

        public Vector2Int Cell => cell;
    }
}
