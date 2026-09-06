using UnityEngine;
using UnityEngine.UI;

namespace SousLaVille.Minigames
{
    /// <summary>
    /// LE STOCK, phase 14. Le memory de l'usine a panneaux, et le premier des trois
    /// mini-jeux : « MES PANNEAUX SONT EN DÉSORDRE · RETROUVE-LES DEUX PAR DEUX ».
    ///
    /// Les cartes apparient un panneau AVEC LE MEME PANNEAU, et le NOM de la paire s'affiche
    /// des qu'elle est trouvee. Le panneau contre son nom avait ete mesure impossible avant
    /// d'ecrire : la police est a chasse fixe, ARRÊT ET STATIONNEMENT INTERDITS fait 193 px
    /// de large sur les 320 de l'ecran, et des cartes assez larges pour le porter donnent
    /// deux colonnes et quatre paires. Le nom devient donc la RECOMPENSE de la trouvaille au
    /// lieu d'etre l'enigme — et c'est ce qui prepare La Fabrique, dont le sujet est de nommer.
    ///
    /// AUCUNE MINUTERIE, nulle part. Deux cartes qui ne vont pas ensemble restent visibles
    /// jusqu'a la prochaine action du joueur, quelle qu'elle soit : l'enfant regarde aussi
    /// longtemps qu'il veut, et rien ne lui est demande dans un delai. C'est la seule facon
    /// de tenir « aucun timing serre » sans lui reprendre l'information qu'il vient de voir.
    ///
    /// AUCUN SCORE, aucun compte de coups, aucun chrono, aucune sauvegarde. Un memory rate
    /// n'existe pas : il n'y a que des memory pas encore finis.
    /// </summary>
    public class SignMemory : MiniGameScreen
    {
        [Tooltip("Le fond de chaque carte. Trente-deux, la plus grande manche.")]
        [SerializeField] private Image[] cards;

        [Tooltip("La face de chaque carte, dans le meme ordre : le dos, ou le panneau.")]
        [SerializeField] private Image[] faces;

        [Tooltip("Le cadre de choix, pose sur la carte visee.")]
        [SerializeField] private Image cursor;

        [Tooltip("Le nom de la derniere paire trouvee, en bas de l'ecran.")]
        [SerializeField] private Image nameBand;

        [Tooltip("Le picto de sortie, allume quand la manche est finie.")]
        [SerializeField] private Image exitPrompt;

        [Tooltip("Le dos d'un panneau : la face cachee d'une carte.")]
        [SerializeField] private Sprite back;

        [Tooltip("Les vingt-quatre panneaux de la planche, dans l'ordre de lecture.")]
        [SerializeField] private Sprite[] signs;

        [Tooltip("Leurs vingt-quatre noms, dans le meme ordre.")]
        [SerializeField] private Sprite[] names;

        [Tooltip("Nombre de familles de la planche. Chaque manche en tire autant de chacune.")]
        [SerializeField] private int families = 4;

        [Tooltip("Rangees du plateau. Cinq ne tiennent pas dans 180 px : voir PLAN-PHASE-14.")]
        [SerializeField] private int rows = 4;

        [Tooltip("Cote d'une carte en pixels de reference. Plancher de CLAUDE.md : 32.")]
        [SerializeField] private float cardSize = 32f;

        [Tooltip("Blanc entre deux cartes.")]
        [SerializeField] private float gutter = 4f;

        [Tooltip("Le plateau monte de ceci pour laisser la bande du nom en bas.")]
        [SerializeField] private float boardOffsetY = 8f;

        [Tooltip("Paires de chaque manche. La derniere valeur tient ensuite.")]
        [SerializeField] private int[] roundPairs = { 8, 12, 16 };

        // Phase 18f : les cartes sont la boite de papier du HUD, teintee — gris acier de dos,
        // blanc de face, vert feuille une fois la paire trouvee.
        [SerializeField] private Color faceDownColor = new Color(0.76f, 0.78f, 0.82f, 1f);
        [SerializeField] private Color faceUpColor = Color.white;
        [SerializeField] private Color matchedColor = new Color(0.63f, 0.88f, 0.44f, 1f);

        private int[] deck = new int[0];
        private bool[] matched = new bool[0];
        private bool[] faceUp = new bool[0];
        private int columns;
        private int cursorIndex;
        private int firstPick = -1;
        private int secondPick = -1;
        private int roundsPlayed;

        /// <summary>Cartes en jeu dans la manche. Sert aux verifications et au pilote.</summary>
        public int CardCount => deck.Length;

        /// <summary>Colonnes du plateau. Les rangees sont fixes.</summary>
        public int Columns => columns;

        /// <summary>Rangees du plateau.</summary>
        public int Rows => rows;

        /// <summary>La carte visee.</summary>
        public int CursorIndex => cursorIndex;

        /// <summary>Le rang de panneau porte par une carte. Le pilote de test s'en sert.</summary>
        public int SlotAt(int card)
        {
            return card >= 0 && card < deck.Length ? deck[card] : -1;
        }

        /// <summary>Vrai si cette carte a trouve sa jumelle.</summary>
        public bool IsMatched(int card)
        {
            return card >= 0 && card < matched.Length && matched[card];
        }

        /// <summary>Vrai si cette carte est retournee face visible.</summary>
        public bool IsFaceUp(int card)
        {
            return card >= 0 && card < faceUp.Length && faceUp[card];
        }

        /// <summary>Paires deja trouvees.</summary>
        public int PairsFound
        {
            get
            {
                int found = 0;
                foreach (bool done in matched)
                {
                    if (done)
                    {
                        found++;
                    }
                }

                return found / 2;
            }
        }

        /// <summary>Vrai quand toutes les paires sont trouvees. Espace referme alors.</summary>
        public bool IsFinished => deck.Length > 0 && PairsFound * 2 == deck.Length;

        /// <summary>
        /// LE TIRAGE, pur et deterministe : aucun MonoBehaviour, aucun ecran, aucune image.
        /// C'est lui qui se verifie par le calcul sur mille graines, et c'est pour cela qu'il
        /// est statique.
        ///
        /// Il tire pairs / families panneaux DANS CHAQUE FAMILLE par SignDraw.PerFamily, jamais
        /// au hasard dans le tas : les quatre formes sont donc toujours presentes, et la lecon
        /// de la piece — la forme dit la famille avant que le dessin dise le detail — tient
        /// dans le mini-jeu au lieu d'y etre contredite par un tirage de six triangles rouges.
        ///
        /// Rend null plutot que de distribuer une manche bancale : une taille impaire, une
        /// taille qui ne se partage pas entre les familles, ou plus de panneaux demandes qu'une
        /// famille n'en porte.
        ///
        /// Phase 15 : le tirage par famille est parti dans SignDraw pour servir aussi a La
        /// Fabrique. L'ordre des appels au generateur est le meme qu'avant : une graine donne
        /// le meme plateau qu'en phase 14, verifie donne pour donne.
        /// </summary>
        public static int[] Deal(int pairs, int slots, int families, System.Random random)
        {
            int[] chosen = SignDraw.PerFamily(pairs, slots, families, random);
            if (chosen == null)
            {
                return null;
            }

            int[] dealt = new int[pairs * 2];
            for (int i = 0; i < pairs; i++)
            {
                dealt[i * 2] = chosen[i];
                dealt[i * 2 + 1] = chosen[i];
            }

            SignDraw.Shuffle(dealt, random);
            return dealt;
        }

        /// <summary>
        /// Distribue une manche precise. Publique et deterministe : la verification rejoue
        /// une graine et retrouve le meme plateau, sans clavier et sans hasard.
        /// </summary>
        public bool StartRound(int pairs, int seed)
        {
            if (cards == null || faces == null || signs == null || names == null)
            {
                return false;
            }

            if (cards.Length != faces.Length || signs.Length != names.Length)
            {
                return false;
            }

            if (pairs * 2 > cards.Length || rows <= 0 || pairs * 2 % rows != 0)
            {
                return false;
            }

            int[] dealt = Deal(pairs, signs.Length, families, new System.Random(seed));
            if (dealt == null)
            {
                return false;
            }

            deck = dealt;
            matched = new bool[deck.Length];
            faceUp = new bool[deck.Length];
            columns = deck.Length / rows;
            cursorIndex = 0;
            firstPick = -1;
            secondPick = -1;

            Layout();
            RefreshAll();
            return true;
        }

        /// <summary>
        /// Une manche par lancement, plus grande a chaque fois, plafonnee a la derniere
        /// taille. Le compteur est un CHAMP, jamais un octet de partie.json : l'usine est une
        /// pure recreation, rien n'y sort et rien ne s'y sauvegarde. Il survit donc a une
        /// sortie du batiment — le SceneRouter eteint la couche sans la detruire — et pas a
        /// une fermeture du jeu.
        /// </summary>
        protected override bool Begin()
        {
            if (roundPairs == null || roundPairs.Length == 0)
            {
                return false;
            }

            int step = Mathf.Min(roundsPlayed, roundPairs.Length - 1);
            if (!StartRound(roundPairs[step], Random.Range(int.MinValue, int.MaxValue)))
            {
                return false;
            }

            roundsPlayed++;
            return true;
        }

        /// <summary>
        /// Une fleche. Deux cartes depareillees encore visibles se retournent d'abord : c'est
        /// « la prochaine action du joueur, quelle qu'elle soit », et c'est ce qui remplace
        /// une minuterie.
        ///
        /// Le curseur SAUTE LES CARTES DEJA APPARIEES : aucun coup perdu, aucun geste sans
        /// effet, et le plateau se retrecit tout seul a mesure qu'on avance.
        /// </summary>
        public override void Move(Vector2Int direction)
        {
            if (deck.Length == 0 || IsFinished)
            {
                return;
            }

            HidePending();

            int next = Step(cursorIndex, direction);
            if (next < 0)
            {
                return;
            }

            cursorIndex = next;
            PlaceCursor();
        }

        /// <summary>
        /// Espace : retourne la carte visee. La manche finie, il referme — le meme geste que
        /// partout ailleurs dans le jeu.
        /// </summary>
        protected override int ArrowsHintIndex => IsFinished ? -1 : HintChoose;

        protected override int SpaceHintIndex
        {
            get
            {
                if (deck.Length == 0 || IsFinished)
                {
                    return HintExit;
                }

                // Une carte deja trouvee ou deja retournee ne se retourne pas : le mot
                // s'eteint plutot que de promettre un geste sans effet.
                return matched[cursorIndex] || faceUp[cursorIndex] ? -1 : HintFlip;
            }
        }

        public override void Validate()
        {
            if (deck.Length == 0 || IsFinished)
            {
                Close();
                return;
            }

            HidePending();

            if (matched[cursorIndex] || faceUp[cursorIndex])
            {
                return;
            }

            faceUp[cursorIndex] = true;
            RefreshCard(cursorIndex);

            if (firstPick < 0)
            {
                firstPick = cursorIndex;
                return;
            }

            if (deck[firstPick] == deck[cursorIndex])
            {
                matched[firstPick] = true;
                matched[cursorIndex] = true;
                ShowName(deck[cursorIndex]);

                RefreshCard(firstPick);
                RefreshCard(cursorIndex);

                firstPick = -1;
                secondPick = -1;

                if (IsFinished)
                {
                    Finish();
                    return;
                }

                // Le curseur se tient sur une carte qui vient d'etre appariee : Espace n'y
                // ferait plus rien. On le pose sur la plus proche carte encore en jeu, sinon
                // l'enfant appuierait dans le vide sans comprendre pourquoi.
                cursorIndex = NearestOpen(cursorIndex);
                PlaceCursor();
                return;
            }

            // Depareillees : les deux restent visibles. Aucune minuterie ne les retournera,
            // c'est le prochain geste du joueur qui le fera.
            secondPick = cursorIndex;
        }

        /// <summary>Retourne les deux cartes depareillees encore visibles, s'il y en a.</summary>
        private void HidePending()
        {
            if (secondPick < 0)
            {
                return;
            }

            faceUp[firstPick] = false;
            faceUp[secondPick] = false;

            RefreshCard(firstPick);
            RefreshCard(secondPick);

            firstPick = -1;
            secondPick = -1;
        }

        /// <summary>
        /// La case suivante dans cette direction, en sautant les cartes appariees. Rend -1 au
        /// bord : pousser vers le vide n'est pas une erreur, c'est juste sans effet.
        /// </summary>
        private int Step(int from, Vector2Int direction)
        {
            int row = from / columns;
            int column = from % columns;

            while (true)
            {
                // La rangee 0 est EN HAUT du plateau : la fleche du haut fait donc baisser
                // l'indice de rangee.
                row -= direction.y;
                column += direction.x;

                if (row < 0 || row >= rows || column < 0 || column >= columns)
                {
                    return -1;
                }

                int index = row * columns + column;
                if (!matched[index])
                {
                    return index;
                }
            }
        }

        /// <summary>La carte encore en jeu la plus proche, en distance de plateau.</summary>
        private int NearestOpen(int from)
        {
            int fromRow = from / columns;
            int fromColumn = from % columns;

            int best = from;
            int bestDistance = int.MaxValue;

            for (int index = 0; index < deck.Length; index++)
            {
                if (matched[index])
                {
                    continue;
                }

                int distance = Mathf.Abs(index / columns - fromRow)
                             + Mathf.Abs(index % columns - fromColumn);

                if (distance < bestDistance)
                {
                    bestDistance = distance;
                    best = index;
                }
            }

            return best;
        }

        /// <summary>La manche est finie : plus de curseur, et le picto de sortie s'allume.</summary>
        private void Finish()
        {
            if (cursor != null)
            {
                cursor.enabled = false;
            }

            if (exitPrompt != null)
            {
                exitPrompt.enabled = true;
            }
        }

        /// <summary>
        /// Pose les cartes en grille. Le plateau est centre horizontalement et remonte de
        /// boardOffsetY pour laisser la bande du nom en bas.
        /// </summary>
        private void Layout()
        {
            float pitch = cardSize + gutter;

            for (int index = 0; index < cards.Length; index++)
            {
                bool used = index < deck.Length;

                if (cards[index] != null)
                {
                    cards[index].gameObject.SetActive(used);
                }

                if (!used || cards[index] == null)
                {
                    continue;
                }

                int row = index / columns;
                int column = index % columns;

                RectTransform rect = cards[index].rectTransform;
                rect.sizeDelta = new Vector2(cardSize, cardSize);
                rect.anchoredPosition = new Vector2(
                    (column - (columns - 1) * 0.5f) * pitch,
                    ((rows - 1) * 0.5f - row) * pitch + boardOffsetY);
            }

            if (cursor != null)
            {
                cursor.enabled = true;
                cursor.rectTransform.sizeDelta = new Vector2(cardSize, cardSize);
            }

            if (exitPrompt != null)
            {
                exitPrompt.enabled = false;
            }

            if (nameBand != null)
            {
                nameBand.enabled = false;
            }

            PlaceCursor();
        }

        private void PlaceCursor()
        {
            if (cursor == null || cursorIndex < 0 || cursorIndex >= deck.Length
                || cards[cursorIndex] == null)
            {
                return;
            }

            cursor.rectTransform.anchoredPosition =
                cards[cursorIndex].rectTransform.anchoredPosition;
        }

        private void RefreshAll()
        {
            for (int index = 0; index < deck.Length; index++)
            {
                RefreshCard(index);
            }
        }

        private void RefreshCard(int index)
        {
            if (index < 0 || index >= deck.Length)
            {
                return;
            }

            bool shown = faceUp[index] || matched[index];

            if (cards[index] != null)
            {
                cards[index].color = matched[index] ? matchedColor
                    : shown ? faceUpColor : faceDownColor;
            }

            if (faces[index] == null)
            {
                return;
            }

            int slot = deck[index];
            faces[index].sprite = shown && slot >= 0 && slot < signs.Length
                ? signs[slot]
                : back;
        }

        /// <summary>
        /// Le nom de la paire trouvee. Surtout pas SetNativeSize : il divise la largeur du
        /// sprite par ses pixels par unite, 16 ici, puis la multiplie par les 100 du Canvas —
        /// une phrase de 109 px sortait a 681. On pose sizeDelta depuis le rectangle du sprite,
        /// comme SpeechBox depuis la phase 9a.
        /// </summary>
        private void ShowName(int slot)
        {
            if (nameBand == null || slot < 0 || slot >= names.Length || names[slot] == null)
            {
                return;
            }

            Sprite sprite = names[slot];
            nameBand.sprite = sprite;
            nameBand.rectTransform.sizeDelta = new Vector2(sprite.rect.width, sprite.rect.height);
            nameBand.enabled = true;
        }
    }
}
