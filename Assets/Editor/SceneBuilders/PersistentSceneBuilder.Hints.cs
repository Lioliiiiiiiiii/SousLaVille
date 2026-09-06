using SousLaVille.Minigames;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

namespace SousLaVille.EditorTools
{
    /// <summary>
    /// LA BANDE D'AIDE DES MINI-JEUX, phase 22. En bas de chacun des trois : ce que font les
    /// flèches, ce que fait Espace, et qu'Échap sort.
    ///
    /// Trois groupes, chacun un picto de touche suivi d'un mot. Les deux premiers mots sont
    /// posés par `MiniGameScreen.RefreshHints` à chaque geste — c'est tout l'objet de la bande,
    /// la même touche ne faisant pas la même chose selon l'état. Le troisième ne bouge jamais :
    /// Échap sort, toujours.
    /// </summary>
    public static partial class PersistentSceneBuilder
    {
        /// <summary>Côté d'un picto de touche. Un indicateur, pas une cible : le plancher de 32 ne s'applique pas.</summary>
        private const float HintKeySize = 16f;

        /// <summary>
        /// Hauteur totale que la bande occupe en bas de l'ecran. Les mini-jeux la reservent :
        /// sans cela, leur contenu s'ecrit par-dessus.
        /// </summary>
        private const float HintBarHeight = 16f;

        /// <summary>Blanc entre la touche et son mot.</summary>
        private const float HintKeyGap = 3f;

        /// <summary>Abscisse du début de chaque groupe. Trois colonnes régulières.</summary>
        private static readonly float[] HintGroupX = { -140f, -30f, 70f };

        /// <summary>De combien le cadre rouge d'une rangée écartée dépasse, de chaque côté.</summary>
        private const float QuizOutlineBleed = 2f;

        /// <summary>Le rouge du cadre : celui des panneaux d'interdiction, déjà dans la palette.</summary>
        private static readonly Color QuizOutlineColor =
            new Color(0xC8 / 255f, 0x2F / 255f, 0x2F / 255f, 1f);

        /// <summary>
        /// Construit la bande et câble les deux mots variables sur le composant.
        ///
        /// `serialized` est celui du mini-jeu, encore ouvert : le câblage se fait dedans, et
        /// l'appelant l'applique avec le reste.
        /// </summary>
        private static void CreateHintBar(Transform panel, SerializedObject serialized)
        {
            ValidateHintWords();

            float y = -ReferenceHeight * 0.5f + ScreenMargin + HintKeySize * 0.5f;

            // UN FOND OPAQUE, PLEINE LARGEUR, phase 23.
            //
            // Le Stock et La Fabrique posent la bande sur leur fond uni : elle s'y voit sans
            // rien. LE PLAN, lui, occupe 176 px sur 180 — cinq rangees de 32 plus les 16 px
            // dont un panneau deborde au-dessus de sa case — et deux de ses quinze plans
            // garnissent la rangee la plus basse comme la plus haute. Il n'y a donc AUCUNE
            // bande libre, ni en haut ni en bas : decaler le plateau couperait des panneaux.
            //
            // La bande se pose donc PAR-DESSUS le bas de la carte, et son fond la rend lisible
            // au lieu de melanger ses mots a l'herbe et aux poteaux. Les panneaux eux-memes
            // restent visibles : c'est le pied des poteaux et le sol qu'elle couvre.
            Image fond = CreateCenteredImage(panel, "Hint_Background", new Vector2(0f, y),
                new Vector2(ReferenceWidth, HintBarHeight + ScreenMargin * 2f));
            fond.color = MiniGameBackground;

            Image arrowsWord = CreateHintGroup(panel, "Hint_Arrows", HintGroupX[0], y,
                PlaceholderArtGenerator.PictoKeyArrows, null);

            Image spaceWord = CreateHintGroup(panel, "Hint_Space", HintGroupX[1], y,
                PlaceholderArtGenerator.PictoKeySpace, null);

            // Échap ne change jamais : son mot est posé ici, une fois, et le composant l'ignore.
            CreateHintGroup(panel, "Hint_Escape", HintGroupX[2], y,
                PlaceholderArtGenerator.PictoKeyEscape,
                LoadSprite(PlaceholderArtGenerator.HintWordTexture(0)));

            serialized.FindProperty("arrowsHint").objectReferenceValue = arrowsWord;
            serialized.FindProperty("spaceHint").objectReferenceValue = spaceWord;

            SerializedProperty words = serialized.FindProperty("hintWords");
            words.arraySize = PlaceholderArtGenerator.HintWords.Length;

            for (int i = 0; i < PlaceholderArtGenerator.HintWords.Length; i++)
            {
                words.GetArrayElementAtIndex(i).objectReferenceValue =
                    LoadSprite(PlaceholderArtGenerator.HintWordTexture(i));
            }
        }

        /// <summary>Un groupe : le picto de la touche, puis son mot calé à droite de lui.</summary>
        private static Image CreateHintGroup(Transform panel, string name, float x, float y,
            string keyTexture, Sprite word)
        {
            Image key = CreateCenteredImage(panel, name + "_Key",
                new Vector2(x + HintKeySize * 0.5f, y), new Vector2(HintKeySize, HintKeySize));
            key.sprite = LoadSprite(keyTexture);

            float width = word != null ? word.rect.width : 0f;
            float height = word != null ? word.rect.height : 0f;

            Image text = CreateCenteredImage(panel, name + "_Word",
                new Vector2(x + HintKeySize + HintKeyGap + width * 0.5f, y),
                new Vector2(width, height));
            text.sprite = word;
            text.enabled = word != null;

            // Le mot est calé à GAUCHE contre sa touche : « CHOISIR » et « RETOURNER » n'ont
            // pas la même largeur, et deux mots centrés danseraient d'un état à l'autre.
            text.rectTransform.pivot = new Vector2(0f, 0.5f);
            text.rectTransform.anchoredPosition =
                new Vector2(x + HintKeySize + HintKeyGap, y);

            return text;
        }

        /// <summary>
        /// LE FILET DE LA BANDE, joué à chaque construction. Les rangs des mots sont écrits
        /// DEUX FOIS — dans `PlaceholderArtGenerator.HintWords` et en constantes dans
        /// `MiniGameScreen` — parce que le runtime ne lit pas l'assembly Editor. Deux listes
        /// qui doivent rester d'accord finissent par diverger : celle-ci les compare.
        /// </summary>
        private static bool ValidateHintWords()
        {
            string[] attendus =
            {
                "SORTIR", "CHOISIR", "POSER", "VERIFIER", "RETOURNER", "VALIDER", "SUITE"
            };

            bool ok = true;

            if (PlaceholderArtGenerator.HintWords.Length != attendus.Length)
            {
                Debug.LogError($"[Sous la Ville] La bande d'aide attend {attendus.Length} mots, " +
                               $"le générateur en donne {PlaceholderArtGenerator.HintWords.Length}.");
                return false;
            }

            for (int i = 0; i < attendus.Length; i++)
            {
                if (PlaceholderArtGenerator.HintWords[i] != attendus[i])
                {
                    Debug.LogError($"[Sous la Ville] Mot d'aide {i} : « " +
                                   $"{PlaceholderArtGenerator.HintWords[i]} » au lieu de " +
                                   $"« {attendus[i]} ». Les rangs de MiniGameScreen ne suivent plus.");
                    ok = false;
                }

                int width = PixelFont.WidthOf(attendus[i]);
                if (width > 100f)
                {
                    Debug.LogError($"[Sous la Ville] Le mot d'aide « {attendus[i]} » fait " +
                                   $"{width} px : il déborde de sa colonne.");
                    ok = false;
                }
            }

            return ok;
        }
    }
}
