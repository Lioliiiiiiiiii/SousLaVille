using UnityEngine;
using UnityEngine.UI;

namespace SousLaVille.Minigames
{
    /// <summary>
    /// LA FABRIQUE, phase 15. Le deuxieme mini-jeu de l'usine a panneaux : « JE DESSINE LES
    /// PANNEAUX · SAURAS-TU LES NOMMER ? ». Un panneau, trois noms ecrits, on choisit.
    ///
    /// Victorien sait lire, decision du 3 septembre 2026, et les noms des panneaux sont
    /// courts et en majuscules : c'est le seul endroit du jeu ou le TEXTE est la matiere
    /// meme du jeu, et il reprend les vingt-quatre images de noms de la phase 13 sans en
    /// dessiner une de plus.
    ///
    /// LES LEURRES SE RESSERRENT d'un lancement a l'autre. Au premier, les deux faux noms
    /// viennent d'autres familles : la grammaire des formes suffit — un disque bleu n'est pas
    /// un « SENS INTERDIT ». Au deuxieme, l'un des deux est de la meme famille. Au troisieme
    /// et ensuite, les deux : il faut lire le pictogramme. Le compteur est un champ du
    /// composant, comme celui du memory ; rien n'est ecrit sur le disque.
    ///
    /// Un nom faux S'ETEINT et on rechoisit : au plus deux erreurs par question, jamais de
    /// revelation, jamais d'echec. Un nom juste passe au vert, les deux autres s'eteignent, et
    /// le GESTE SUIVANT, quel qu'il soit, passe a la question d'apres — aucune minuterie,
    /// comme en 14 : l'enfant regarde le panneau et son nom aussi longtemps qu'il veut.
    /// </summary>
    public class SignQuiz : MiniGameScreen
    {
        /// <summary>Une question : le panneau, les trois noms proposes, et lequel est le bon.</summary>
        public struct Question
        {
            public int Target;
            public int[] Options;
            public int Answer;
        }

        [Tooltip("Le panneau a nommer, agrandi par le Canvas.")]
        [SerializeField] private Image sign;

        [Tooltip("Les fonds des trois rangees de choix.")]
        [SerializeField] private Image[] rows;

        [Tooltip("Le nom ecrit dans chaque rangee, dans le meme ordre.")]
        [SerializeField] private Image[] rowNames;

        [Tooltip("La jauge : un carre par question, allume quand elle est faite.")]
        [SerializeField] private Image[] progress;

        [Tooltip("Le picto de sortie, allume quand la manche est finie.")]
        [SerializeField] private Image exitPrompt;

        [Tooltip("Les vingt-quatre panneaux de la planche, dans l'ordre de lecture.")]
        [SerializeField] private Sprite[] signs;

        [Tooltip("Leurs vingt-quatre noms, dans le meme ordre.")]
        [SerializeField] private Sprite[] names;

        [Tooltip("Nombre de familles de la planche.")]
        [SerializeField] private int families = 4;

        [Tooltip("Questions par manche. Divisible par le nombre de familles.")]
        [SerializeField] private int questionsPerRound = 8;

        [Tooltip("Leurres de la MEME famille a chaque lancement. La derniere valeur tient ensuite.")]
        [SerializeField] private int[] sameFamilyLuresPerRound = { 0, 1, 2 };

        // Phase 18f : les rangees sont la boite de papier du HUD, teintee — gris acier au repos,
        // blanc sous le curseur, vert feuille pour la bonne reponse, gris sombre pour l'autre.
        [SerializeField] private Color idleColor = new Color(0.76f, 0.78f, 0.82f, 1f);
        [SerializeField] private Color selectedColor = Color.white;
        [SerializeField] private Color rightColor = new Color(0.63f, 0.88f, 0.44f, 1f);
        [SerializeField] private Color wrongColor = new Color(0.55f, 0.57f, 0.61f, 1f);
        [SerializeField] private Color progressIdleColor = new Color(0.76f, 0.78f, 0.82f, 1f);
        [SerializeField] private Color progressDoneColor = new Color(0.35f, 0.72f, 0.28f, 1f);

        private Question[] questions = new Question[0];
        private int current;
        private int selection;
        private bool[] ruledOut = new bool[0];
        private bool answered;
        private int roundsPlayed;

        /// <summary>Questions de la manche. Sert aux verifications et au pilote.</summary>
        public int QuestionCount => questions.Length;

        /// <summary>Rang de la question en cours, ou QuestionCount une fois la manche finie.</summary>
        public int Current => current;

        /// <summary>La rangee visee.</summary>
        public int Selection => selection;

        /// <summary>Vrai entre une bonne reponse et le geste qui passe a la suivante.</summary>
        public bool IsAnswered => answered;

        /// <summary>Vrai quand les huit questions sont faites. Espace referme alors.</summary>
        public bool IsFinished => questions.Length > 0 && current >= questions.Length;

        /// <summary>Le rang de planche propose dans une rangee de la question en cours, ou -1.</summary>
        public int OptionAt(int row)
        {
            if (IsFinished || row < 0 || row >= questions[current].Options.Length)
            {
                return -1;
            }

            return questions[current].Options[row];
        }

        /// <summary>La rangee qui porte le bon nom pour la question en cours, ou -1.</summary>
        public int AnswerRow => IsFinished ? -1 : questions[current].Answer;

        /// <summary>Vrai si cette rangee a ete essayee et eteinte.</summary>
        public bool IsRuledOut(int row)
        {
            return row >= 0 && row < ruledOut.Length && ruledOut[row];
        }

        /// <summary>
        /// LES QUESTIONS, pures et deterministes. Les cibles se tirent par famille comme les
        /// paires du memory ; les leurres se tirent pour chacune, sameFamilyLures dans la
        /// famille de la cible et le reste ailleurs ; puis les trois noms sont melanges.
        ///
        /// Rend null plutot qu'une manche bancale : questions non partageables entre les
        /// familles, ou plus de leurres de meme famille qu'une famille n'en offre.
        /// </summary>
        public static Question[] Build(int questionCount, int slots, int families,
            int sameFamilyLures, System.Random random)
        {
            const int optionCount = 3;

            if (sameFamilyLures < 0 || sameFamilyLures > optionCount - 1)
            {
                return null;
            }

            int[] targets = SignDraw.PerFamily(questionCount, slots, families, random);
            if (targets == null)
            {
                return null;
            }

            SignDraw.Shuffle(targets, random);

            Question[] built = new Question[questionCount];

            for (int q = 0; q < questionCount; q++)
            {
                int target = targets[q];
                int[] same = SignDraw.SameFamily(target, sameFamilyLures, slots, families, random);
                int[] other = SignDraw.OtherFamilies(target, optionCount - 1 - sameFamilyLures,
                    slots, families, random);

                if (same == null || other == null)
                {
                    return null;
                }

                int[] options = new int[optionCount];
                options[0] = target;
                System.Array.Copy(same, 0, options, 1, same.Length);
                System.Array.Copy(other, 0, options, 1 + same.Length, other.Length);
                SignDraw.Shuffle(options, random);

                built[q].Target = target;
                built[q].Options = options;
                built[q].Answer = System.Array.IndexOf(options, target);
            }

            return built;
        }

        /// <summary>
        /// Prepare une manche precise. Publique et deterministe : la verification rejoue une
        /// graine et retrouve les memes questions, sans clavier et sans hasard.
        /// </summary>
        public bool StartRound(int questionCount, int sameFamilyLures, int seed)
        {
            if (sign == null || rows == null || rowNames == null || signs == null || names == null)
            {
                return false;
            }

            if (rows.Length != 3 || rowNames.Length != 3 || signs.Length != names.Length)
            {
                return false;
            }

            if (progress != null && progress.Length != questionCount)
            {
                return false;
            }

            Question[] built = Build(questionCount, signs.Length, families, sameFamilyLures,
                new System.Random(seed));
            if (built == null)
            {
                return false;
            }

            questions = built;
            current = 0;
            ruledOut = new bool[3];

            if (exitPrompt != null)
            {
                exitPrompt.enabled = false;
            }

            ShowQuestion();
            return true;
        }

        /// <summary>
        /// Une manche par lancement, les leurres se resserrant a chaque fois jusqu'a la
        /// derniere valeur. Le compteur est un CHAMP, jamais un octet de partie.json.
        /// </summary>
        protected override bool Begin()
        {
            if (sameFamilyLuresPerRound == null || sameFamilyLuresPerRound.Length == 0)
            {
                return false;
            }

            int step = Mathf.Min(roundsPlayed, sameFamilyLuresPerRound.Length - 1);
            if (!StartRound(questionsPerRound, sameFamilyLuresPerRound[step],
                    Random.Range(int.MinValue, int.MaxValue)))
            {
                return false;
            }

            roundsPlayed++;
            return true;
        }

        /// <summary>
        /// Une fleche, haut ou bas : la rangee voisine encore allumee. Une question repondue
        /// passe d'abord a la suivante — c'est « le geste suivant, quel qu'il soit ».
        /// </summary>
        public override void Move(Vector2Int direction)
        {
            if (IsFinished)
            {
                return;
            }

            if (answered)
            {
                NextQuestion();
                return;
            }

            if (direction.y == 0)
            {
                return;
            }

            // La rangee 0 est EN HAUT : la fleche du haut fait baisser l'indice.
            int step = -direction.y;
            int row = selection + step;

            while (row >= 0 && row < rows.Length && ruledOut[row])
            {
                row += step;
            }

            if (row < 0 || row >= rows.Length)
            {
                return;
            }

            selection = row;
            RefreshRows();
        }

        /// <summary>Espace : ce nom-la. La manche finie, il referme.</summary>
        public override void Validate()
        {
            if (IsFinished)
            {
                Close();
                return;
            }

            if (answered)
            {
                NextQuestion();
                return;
            }

            if (ruledOut[selection])
            {
                return;
            }

            if (selection == questions[current].Answer)
            {
                answered = true;
                RefreshRows();
                return;
            }

            // Faux : la rangee s'eteint, et le choix se pose sur la plus proche encore allumee.
            ruledOut[selection] = true;
            selection = NearestOpen(selection);
            RefreshRows();
        }

        private void NextQuestion()
        {
            if (progress != null && current < progress.Length && progress[current] != null)
            {
                progress[current].color = progressDoneColor;
            }

            current++;
            answered = false;

            if (IsFinished)
            {
                Finish();
                return;
            }

            ShowQuestion();
        }

        private void ShowQuestion()
        {
            Question question = questions[current];

            selection = 0;
            answered = false;
            for (int i = 0; i < ruledOut.Length; i++)
            {
                ruledOut[i] = false;
            }

            if (sign != null && question.Target >= 0 && question.Target < signs.Length)
            {
                sign.sprite = signs[question.Target];
            }

            for (int row = 0; row < rows.Length; row++)
            {
                int slot = question.Options[row];
                Sprite name = slot >= 0 && slot < names.Length ? names[slot] : null;

                if (rowNames[row] != null)
                {
                    rowNames[row].sprite = name;
                    rowNames[row].enabled = name != null;

                    // Surtout pas SetNativeSize : sizeDelta depuis le rectangle du sprite, comme
                    // SpeechBox depuis la phase 9a.
                    if (name != null)
                    {
                        rowNames[row].rectTransform.sizeDelta =
                            new Vector2(name.rect.width, name.rect.height);
                    }
                }
            }

            if (progress != null)
            {
                for (int i = 0; i < progress.Length; i++)
                {
                    if (progress[i] != null)
                    {
                        progress[i].color = i < current ? progressDoneColor : progressIdleColor;
                    }
                }
            }

            RefreshRows();
        }

        private void RefreshRows()
        {
            for (int row = 0; row < rows.Length; row++)
            {
                if (rows[row] == null)
                {
                    continue;
                }

                Color color;
                if (answered)
                {
                    color = row == questions[current].Answer ? rightColor : wrongColor;
                }
                else if (ruledOut[row])
                {
                    color = wrongColor;
                }
                else
                {
                    color = row == selection ? selectedColor : idleColor;
                }

                rows[row].color = color;
            }
        }

        private int NearestOpen(int from)
        {
            int best = from;
            int bestDistance = int.MaxValue;

            for (int row = 0; row < rows.Length; row++)
            {
                if (ruledOut[row])
                {
                    continue;
                }

                int distance = Mathf.Abs(row - from);
                if (distance < bestDistance)
                {
                    bestDistance = distance;
                    best = row;
                }
            }

            return best;
        }

        /// <summary>
        /// La manche est finie : le picto de sortie s'allume. La derniere question reste a
        /// l'ecran, son bon nom en vert — c'est la derniere chose qu'on regarde avant de sortir.
        /// </summary>
        private void Finish()
        {
            if (exitPrompt != null)
            {
                exitPrompt.enabled = true;
            }
        }
    }
}
