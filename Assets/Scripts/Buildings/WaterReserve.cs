using System;
using UnityEngine;

namespace SousLaVille.Buildings
{
    /// <summary>
    /// Le bassin d'orage. L'automne apporte plus d'eau que la station n'en traite ; un
    /// bassin raccorde encaisse la pointe et la relache aux saisons suivantes, quand la
    /// station a de la marge.
    ///
    /// Il est fixe dans le monde, deja creuse, comme les alcoves des maisons : le joueur
    /// creuse et pose jusqu'a lui. Aucun geste nouveau, aucun mode, un probleme de chemin.
    ///
    /// Le niveau est un entier, pas un flottant : cinq images, dix crans, il se lit dans
    /// l'inspecteur, se sauvegarde sans surprise d'arrondi et se verifie exactement. Il ne
    /// change qu'au tick de saison, par SeasonSystem, jamais par frame : le reseau reste un
    /// graphe, pas une simulation de fluide.
    ///
    /// Le niveau se lit sur la cuve elle-meme, dans le sous-sol. Pas de jauge au HUD.
    /// Modele et vue tiennent dans le meme composant : cinq images pour une cuve, separer
    /// les deux serait de la ceremonie.
    ///
    /// Vit dans la scene Underground, avec le reseau. Le SceneRouter eteint cette couche
    /// quand le joueur remonte : OnEnable reapplique l'image, au cas ou le niveau aurait
    /// bouge pendant que la cuve etait eteinte.
    /// </summary>
    public class WaterReserve : MonoBehaviour
    {
        /// <summary>Vide, un quart, la moitie, trois quarts, pleine.</summary>
        public const int LevelSpriteCount = 5;

        [Tooltip("La case du bassin, celle qui porte le noeud ReserveInlet du reseau.")]
        [SerializeField] private Vector2Int cell;

        [Tooltip("Ce que le bassin peut retenir, en unites d'eau.")]
        [Min(1)]
        [SerializeField] private int capacity = 10;

        [Tooltip("La cuve. Le niveau se lit dessus.")]
        [SerializeField] private SpriteRenderer view;

        [Tooltip("Cinq images, de vide a pleine, dans cet ordre.")]
        [SerializeField] private Sprite[] levelSprites;

        private int level;

        /// <summary>Leve a chaque changement de niveau. La sauvegarde s'y accroche.</summary>
        public event Action Changed;

        public Vector2Int Cell => cell;

        public int Capacity => capacity;

        /// <summary>Ce que le bassin retient a cet instant, de 0 a Capacity.</summary>
        public int Level => level;

        public int FreeSpace => capacity - level;

        public bool IsFull => level >= capacity;

        public bool IsEmpty => level <= 0;

        private void OnEnable()
        {
            // Le niveau a pu changer pendant que la couche etait eteinte.
            Apply();
        }

        /// <summary>
        /// Encaisse un surplus. Rend ce qui a vraiment ete pris : le reste deborde, et ce
        /// debordement est le sujet de la phase 10, pas de celle-ci.
        /// </summary>
        public int Absorb(int amount)
        {
            int taken = Mathf.Clamp(amount, 0, FreeSpace);
            if (taken == 0)
            {
                return 0;
            }

            SetLevel(level + taken);
            return taken;
        }

        /// <summary>Relache vers la station, dans la limite de ce qu'il retient. Rend ce qui est parti.</summary>
        public int Release(int amount)
        {
            int given = Mathf.Clamp(amount, 0, level);
            if (given == 0)
            {
                return 0;
            }

            SetLevel(level - given);
            return given;
        }

        /// <summary>Pose le niveau lu dans une sauvegarde. Borne a la capacite, sans bruit.</summary>
        public void Restore(int savedLevel)
        {
            SetLevel(savedLevel);
        }

        /// <summary>
        /// Quelle image pour quel niveau. Publique et statique : elle se verifie sans scene.
        /// Vide et pleine sont exactes ; entre les deux, trois paliers egaux.
        /// </summary>
        public static int SpriteIndexFor(int level, int capacity)
        {
            if (level <= 0)
            {
                return 0;
            }

            if (level >= capacity)
            {
                return LevelSpriteCount - 1;
            }

            // capacity >= 2 ici, puisque 0 < level < capacity.
            int steps = LevelSpriteCount - 2;
            return 1 + (level - 1) * steps / (capacity - 1);
        }

        private void SetLevel(int value)
        {
            level = Mathf.Clamp(value, 0, capacity);
            Apply();
            Changed?.Invoke();
        }

        private void Apply()
        {
            if (view == null || levelSprites == null || levelSprites.Length == 0)
            {
                return;
            }

            int index = Mathf.Min(SpriteIndexFor(level, capacity), levelSprites.Length - 1);
            view.sprite = levelSprites[index];
        }
    }
}
