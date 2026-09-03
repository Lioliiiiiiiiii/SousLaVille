using System;
using UnityEngine;

namespace SousLaVille.Core
{
    /// <summary>
    /// L'horloge du jeu. Elle ne fait qu'une chose : compter, et lever un Tick a la fin de
    /// chaque saison. Elle ne sait pas ce qu'est une saison ; c'est le SeasonSystem qui
    /// decide de ce qui arrive au reseau.
    ///
    /// Elle vit dans Persistent, jamais dechargee : le temps passe aussi bien en surface
    /// que sous terre.
    ///
    /// Aucun compte a rebours affiche, aucune urgence : le temps est un decor qui tourne,
    /// pas une menace.
    /// </summary>
    public class GameClock : MonoBehaviour
    {
        /// <summary>Dix minutes. Le cycle des quatre saisons fait donc quarante minutes.</summary>
        public const float DefaultSeasonDuration = 600f;

        [Tooltip("Duree d'une saison, en secondes. Champ serialise : on l'abaisse pour tester.")]
        [SerializeField] private float seasonDuration = DefaultSeasonDuration;

        private float elapsed;

        /// <summary>Leve a la fin de chaque saison. Jamais par frame.</summary>
        public event Action Tick;

        /// <summary>Nombre de saisons ecoulees depuis le demarrage.</summary>
        public int TickCount { get; private set; }

        /// <summary>
        /// Duree d'une saison. Reglable a chaud : la phase 6 la relira depuis la sauvegarde,
        /// et les tests l'abaissent le temps de verifier un cycle complet.
        /// </summary>
        public float SeasonDuration
        {
            get { return seasonDuration; }
            set { seasonDuration = Mathf.Max(0.01f, value); }
        }

        /// <summary>
        /// Progression dans la saison en cours, de 0 a 1. La phase 6 en aura besoin pour
        /// sauvegarder l'instant exact plutot que le seul numero de saison.
        /// </summary>
        public float SeasonProgress
        {
            get { return seasonDuration > 0f ? Mathf.Clamp01(elapsed / seasonDuration) : 0f; }
            set { elapsed = Mathf.Clamp01(value) * seasonDuration; }
        }

        private void Update()
        {
            if (seasonDuration <= 0f)
            {
                return;
            }

            elapsed += Time.deltaTime;

            // Une boucle et non un simple test : une horloge tres acceleree peut franchir
            // plusieurs saisons dans la meme image, et aucune ne doit etre sautee.
            while (elapsed >= seasonDuration)
            {
                elapsed -= seasonDuration;
                TickCount++;
                Tick?.Invoke();
            }
        }
    }
}
