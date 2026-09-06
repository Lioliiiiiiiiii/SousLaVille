using UnityEngine;
using UnityEngine.UI;

namespace SousLaVille.Minigames
{
    /// <summary>
    /// LE PLAN, phase 16. Le troisieme mini-jeu de l'usine a panneaux : « IL MANQUE DES
    /// PANNEAUX ICI · POSE-LES SUR MON PLAN ». Une petite carte de rues, des poteaux vides,
    /// et les cinq panneaux de rue du village : cedez le passage, stop, impasse, route
    /// prioritaire, fin de route prioritaire.
    ///
    /// LA REGLE EST LE CODE DE LA ROUTE, tel que RoadSignRules l'applique au village. Les
    /// postes et le panneau attendu a chacun ne sont ecrits nulle part : ils sont DERIVES a la
    /// construction, par les memes regles que les 32 panneaux des rues. Decision du
    /// 5 septembre 2026 : une solution ecrite a la main pourrait etre fausse, une solution
    /// deduite du Code ne peut pas l'etre.
    ///
    /// UNE SEULE TOUCHE. Les fleches vont de poteau en poteau ; Espace FAIT DEFILER le panneau
    /// du poteau vise — vide, cedez, stop, impasse, prioritaire, fin, vide. Aucun second
    /// niveau de choix, aucune palette a ouvrir : c'est la contrainte de CLAUDE.md, et c'est
    /// aussi une lecon, chaque panneau passe sous les yeux a son tour.
    ///
    /// LE PLAN VERIFIE QUAND TOUS LES POTEAUX SONT GARNIS, au geste suivant : les justes se
    /// fixent, les faux se vident et on les repose. Il faut penser chaque poste avant de
    /// savoir. Aucun echec : on recommence les faux, autant de fois qu'il faut.
    ///
    /// Le rang du plan avance a chaque lancement, et revient au premier a la fermeture du jeu :
    /// un champ, rien sur le disque, comme le memory et le quiz.
    /// </summary>
    public class SignPlan : MiniGameScreen
    {
        /// <summary>Un plan cuit par le builder : ses lignes, ses postes, et le panneau du a chacun.</summary>
        [System.Serializable]
        public class PlanData
        {
            public string name;
            public int width;
            public int height;
            public string[] rows;
            public Vector2Int[] postCells;
            public int[] postKinds;
        }

        public const char Grass = '.';
        public const char Road = '#';
        public const char MainRoad = '=';
        public const char House = 'A';

        [Tooltip("Les quinze plans, du plus simple au plus lourd.")]
        [SerializeField] private PlanData[] plans;

        [Tooltip("Le sol de chaque case, par rang de case, la plus grande grille possible.")]
        [SerializeField] private Image[] grounds;

        [Tooltip("Ce qui se dresse sur chaque case : maison, poteau, panneau. Meme ordre.")]
        [SerializeField] private Image[] entities;

        [Tooltip("Le cadre pose sur le poteau vise.")]
        [SerializeField] private Image cursor;

        [Tooltip("Le picto de sortie, allume quand le plan est juste.")]
        [SerializeField] private Image exitPrompt;

        [SerializeField] private Sprite grass;
        [SerializeField] private Sprite[] roads;
        [SerializeField] private Sprite house;
        [SerializeField] private Sprite emptyPost;

        [Tooltip("Les cinq panneaux de rue, dans l'ordre de RoadSignKind.")]
        [SerializeField] private Sprite[] signs;

        [Tooltip("Colonnes et rangees de la plus grande grille.")]
        [SerializeField] private int maxWidth = 9;

        [SerializeField] private int maxHeight = 5;

        [SerializeField] private float cellSize = 32f;

        [Tooltip("Le plateau descend d'autant : un panneau deborde de sa case vers le haut, et la rangee haute touchait le bord.")]
        [SerializeField] private float boardOffsetY = -8f;

        [Tooltip("La route prioritaire se teinte : c'est ce qui la distingue d'une rue.")]
        [SerializeField] private Color mainRoadTint = new Color(1f, 0.82f, 0.45f, 1f);

        [Tooltip("Le sol d'un poste dont le panneau est juste.")]
        [SerializeField] private Color fixedTint = new Color(0.55f, 0.85f, 0.55f, 1f);

        private int planIndex = -1;
        private int[] placed = new int[0];
        private bool[] fixedPost = new bool[0];
        private int cursorPost;
        private int roundsPlayed;

        /// <summary>Nombre de plans. Sert aux verifications.</summary>
        public int PlanCount => plans != null ? plans.Length : 0;

        /// <summary>Le plan en cours, ou -1.</summary>
        public int PlanIndex => planIndex;

        /// <summary>Postes du plan en cours.</summary>
        public int PostCount => placed.Length;

        /// <summary>Le poteau vise.</summary>
        public int CursorPost => cursorPost;

        /// <summary>Le panneau pose sur un poste, -1 si vide.</summary>
        public int PlacedAt(int post) => post >= 0 && post < placed.Length ? placed[post] : -1;

        /// <summary>Vrai si le poste porte son panneau juste et n'y touche plus.</summary>
        public bool IsFixed(int post) => post >= 0 && post < fixedPost.Length && fixedPost[post];

        /// <summary>La case d'un poste.</summary>
        public Vector2Int PostCellAt(int post) =>
            planIndex >= 0 && post >= 0 && post < plans[planIndex].postCells.Length
                ? plans[planIndex].postCells[post]
                : new Vector2Int(-1, -1);

        /// <summary>
        /// Le panneau que le Code attend a un poste. Sert aux VERIFICATIONS et au pilote de
        /// test ; le jeu lui-meme ne l'affiche jamais.
        /// </summary>
        public int ExpectedKindAt(int post) =>
            planIndex >= 0 && post >= 0 && post < plans[planIndex].postKinds.Length
                ? plans[planIndex].postKinds[post]
                : -1;

        /// <summary>Vrai si chaque poste porte un panneau, juste ou non.</summary>
        public bool AllFilled
        {
            get
            {
                foreach (int kind in placed)
                {
                    if (kind < 0)
                    {
                        return false;
                    }
                }

                return placed.Length > 0;
            }
        }

        /// <summary>Vrai quand chaque poste porte son panneau juste. Espace referme alors.</summary>
        public bool IsFinished
        {
            get
            {
                foreach (bool done in fixedPost)
                {
                    if (!done)
                    {
                        return false;
                    }
                }

                return fixedPost.Length > 0;
            }
        }

        /// <summary>
        /// Un plan par lancement, le suivant a chaque fois, et l'on reprend au premier apres le
        /// dernier. Le compteur est un CHAMP, jamais un octet de partie.json.
        /// </summary>
        protected override bool Begin()
        {
            if (plans == null || plans.Length == 0)
            {
                return false;
            }

            if (!StartPlan(roundsPlayed % plans.Length))
            {
                return false;
            }

            roundsPlayed++;
            return true;
        }

        /// <summary>Pose un plan precis. Publique : la verification en ouvre chacun sans clavier.</summary>
        public bool StartPlan(int index)
        {
            if (plans == null || index < 0 || index >= plans.Length || grounds == null || entities == null)
            {
                return false;
            }

            PlanData plan = plans[index];
            if (plan.rows == null || plan.rows.Length != plan.height || plan.postCells == null
                || plan.postKinds == null || plan.postCells.Length != plan.postKinds.Length
                || plan.postCells.Length == 0 || plan.width > maxWidth || plan.height > maxHeight)
            {
                return false;
            }

            planIndex = index;
            placed = new int[plan.postCells.Length];
            fixedPost = new bool[plan.postCells.Length];
            for (int i = 0; i < placed.Length; i++)
            {
                placed[i] = -1;
            }

            cursorPost = 0;

            if (exitPrompt != null)
            {
                exitPrompt.enabled = false;
            }

            if (cursor != null)
            {
                cursor.enabled = true;
                cursor.rectTransform.sizeDelta = new Vector2(cellSize, cellSize);
            }

            Layout(plan);
            RefreshPosts();
            PlaceCursor();
            return true;
        }

        /// <summary>
        /// Une fleche : le poteau le plus proche dans cette direction, parmi ceux qui ne sont
        /// pas encore fixes. Mais d'abord, si tous les poteaux sont garnis, LE PLAN VERIFIE :
        /// c'est le geste qui dit « j'ai fini de poser ».
        /// </summary>
        /// <summary>
        /// LE POINT QUI MANQUAIT, phase 22. Une fleche DEPLACE tant qu'un poteau est vide, et
        /// VERIFIE des que tous sont garnis. La meme touche, deux actions, et rien ne le disait :
        /// « je ne comprenais pas qu'il fallait appuyer sur la fleche du bas pour valider ».
        /// </summary>
        protected override int ArrowsHintIndex
        {
            get
            {
                if (IsFinished)
                {
                    return -1;
                }

                return AllFilled ? HintCheck : HintChoose;
            }
        }

        protected override int SpaceHintIndex
        {
            get
            {
                if (IsFinished)
                {
                    return HintExit;
                }

                // Sur un poteau deja fixe, Espace ne fait rien : le mot s'eteint plutot que
                // de promettre une action qui n'arrivera pas.
                return IsFixed(CursorPost) ? -1 : HintPlace;
            }
        }

        public override void Move(Vector2Int direction)
        {
            if (planIndex < 0 || IsFinished)
            {
                return;
            }

            if (AllFilled)
            {
                Check();
                if (IsFinished)
                {
                    return;
                }
            }

            int next = Nearest(direction);
            if (next >= 0)
            {
                cursorPost = next;
                PlaceCursor();
            }
        }

        /// <summary>
        /// Espace : le panneau suivant sur le poteau vise — vide, puis les cinq, puis vide.
        /// Sur un poste fixe, rien. Le plan juste, il referme.
        /// </summary>
        public override void Validate()
        {
            if (planIndex < 0)
            {
                return;
            }

            if (IsFinished)
            {
                Close();
                return;
            }

            if (fixedPost[cursorPost])
            {
                return;
            }

            int count = signs != null ? signs.Length : 0;
            placed[cursorPost] = placed[cursorPost] + 1 >= count ? -1 : placed[cursorPost] + 1;
            RefreshPost(cursorPost);
        }

        /// <summary>
        /// Le Code juge : les justes se fixent, les faux se vident. Aucun echec puni, on repose.
        /// Publique pour se verifier sans clavier.
        /// </summary>
        public void Check()
        {
            PlanData plan = plans[planIndex];

            for (int i = 0; i < placed.Length; i++)
            {
                if (fixedPost[i])
                {
                    continue;
                }

                if (placed[i] == plan.postKinds[i])
                {
                    fixedPost[i] = true;
                }
                else
                {
                    placed[i] = -1;
                }
            }

            RefreshPosts();

            if (IsFinished)
            {
                Finish();
                return;
            }

            if (fixedPost[cursorPost])
            {
                cursorPost = NearestOpen(cursorPost);
                PlaceCursor();
            }
        }

        /// <summary>Le poteau non fixe le plus proche dans une direction, comme le plan du village choisit une bouche.</summary>
        private int Nearest(Vector2Int direction)
        {
            PlanData plan = plans[planIndex];
            Vector2 from = plan.postCells[cursorPost];
            int best = -1;
            float bestDistance = float.MaxValue;

            for (int i = 0; i < plan.postCells.Length; i++)
            {
                if (i == cursorPost || fixedPost[i])
                {
                    continue;
                }

                Vector2 delta = (Vector2)plan.postCells[i] - from;
                if (Vector2.Dot(delta, direction) <= 0f)
                {
                    continue;
                }

                float distance = delta.sqrMagnitude;
                if (distance < bestDistance)
                {
                    bestDistance = distance;
                    best = i;
                }
            }

            return best;
        }

        private int NearestOpen(int from)
        {
            PlanData plan = plans[planIndex];
            Vector2Int origin = plan.postCells[from];
            int best = from;
            int bestDistance = int.MaxValue;

            for (int i = 0; i < placed.Length; i++)
            {
                if (fixedPost[i])
                {
                    continue;
                }

                Vector2Int delta = plan.postCells[i] - origin;
                int distance = Mathf.Abs(delta.x) + Mathf.Abs(delta.y);
                if (distance < bestDistance)
                {
                    bestDistance = distance;
                    best = i;
                }
            }

            return best;
        }

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
        /// Pose le plan : une case fait cellSize, la grille est centree. Les cases hors du plan
        /// s'eteignent. La route prioritaire est teintee : c'est ce qui la distingue d'une rue,
        /// et c'est la seule chose que le joueur doit voir pour choisir un stop plutot qu'un cedez.
        /// </summary>
        private void Layout(PlanData plan)
        {
            for (int index = 0; index < grounds.Length; index++)
            {
                int x = index % maxWidth;
                int y = maxHeight - 1 - index / maxWidth;
                bool used = x < plan.width && y < plan.height;

                if (grounds[index] != null)
                {
                    grounds[index].gameObject.SetActive(used);
                }

                if (entities != null && index < entities.Length && entities[index] != null)
                {
                    entities[index].gameObject.SetActive(used);
                }

                if (!used)
                {
                    continue;
                }

                Vector2 position = CellPosition(plan, x, y);
                char c = CharAt(plan, x, y);

                if (grounds[index] != null)
                {
                    grounds[index].rectTransform.sizeDelta = new Vector2(cellSize, cellSize);
                    grounds[index].rectTransform.anchoredPosition = position;
                    grounds[index].color = c == MainRoad ? mainRoadTint : Color.white;
                    grounds[index].sprite = c == Road || c == MainRoad ? roads[RoadMask(plan, x, y)] : grass;
                }

                if (entities == null || index >= entities.Length || entities[index] == null)
                {
                    continue;
                }

                Image entity = entities[index];
                Sprite standing = c == House ? house : null;
                entity.sprite = standing;
                entity.enabled = standing != null;

                if (standing != null)
                {
                    // Le sprite fait 16 sur 24 au pivot du joueur : ses seize pixels du bas
                    // couvrent la case, la tete deborde vers le haut. Agrandi de cellSize / 16.
                    float scale = cellSize / 16f;
                    entity.rectTransform.sizeDelta = new Vector2(standing.rect.width * scale, standing.rect.height * scale);
                    entity.rectTransform.anchoredPosition = position + new Vector2(0f, (standing.rect.height - 16f) * scale * 0.5f);
                }
            }
        }

        private void RefreshPosts()
        {
            for (int i = 0; i < placed.Length; i++)
            {
                RefreshPost(i);
            }
        }

        /// <summary>Le poteau d'un poste : vide, ou le panneau pose ; et son sol, vert s'il est juste.</summary>
        private void RefreshPost(int post)
        {
            PlanData plan = plans[planIndex];
            Vector2Int cell = plan.postCells[post];
            int index = (maxHeight - 1 - cell.y) * maxWidth + cell.x;

            if (index < 0 || index >= entities.Length || entities[index] == null)
            {
                return;
            }

            int kind = placed[post];
            Sprite standing = kind >= 0 && kind < signs.Length ? signs[kind] : emptyPost;

            Image entity = entities[index];
            entity.sprite = standing;
            entity.enabled = standing != null;

            if (standing != null)
            {
                float scale = cellSize / 16f;
                entity.rectTransform.sizeDelta = new Vector2(standing.rect.width * scale, standing.rect.height * scale);
                entity.rectTransform.anchoredPosition =
                    CellPosition(plan, cell.x, cell.y) + new Vector2(0f, (standing.rect.height - 16f) * scale * 0.5f);
            }

            if (index < grounds.Length && grounds[index] != null)
            {
                grounds[index].color = fixedPost[post] ? fixedTint : Color.white;
            }
        }

        private void PlaceCursor()
        {
            if (cursor == null || planIndex < 0 || cursorPost < 0 || cursorPost >= placed.Length)
            {
                return;
            }

            Vector2Int cell = plans[planIndex].postCells[cursorPost];
            cursor.rectTransform.anchoredPosition = CellPosition(plans[planIndex], cell.x, cell.y);
        }

        private Vector2 CellPosition(PlanData plan, int x, int y)
        {
            // VU A L'ECRAN, phase 16 : un panneau de 16 sur 24 agrandi deux fois deborde de 16 px
            // au-dessus de sa case, et sur un plan de cinq rangees la rangee haute touchait le
            // bord — le cedez du plan 12 sortait coupe en deux. Le plateau descend de boardOffsetY.
            return new Vector2((x - (plan.width - 1) * 0.5f) * cellSize,
                (y - (plan.height - 1) * 0.5f) * cellSize + boardOffsetY);
        }

        private static char CharAt(PlanData plan, int x, int y)
        {
            if (x < 0 || x >= plan.width || y < 0 || y >= plan.height)
            {
                return Grass;
            }

            return plan.rows[plan.height - 1 - y][x];
        }

        private static bool IsRoadAt(PlanData plan, int x, int y)
        {
            // Hors du plan, la rue continue : la tuile du bord se dessine ouverte vers le vide.
            if (x < 0 || x >= plan.width || y < 0 || y >= plan.height)
            {
                return IsRoadAt(plan, Mathf.Clamp(x, 0, plan.width - 1), Mathf.Clamp(y, 0, plan.height - 1));
            }

            char c = CharAt(plan, x, y);
            return c == Road || c == MainRoad;
        }

        /// <summary>Le masque des seize tuiles de route : 1 nord, 2 est, 4 sud, 8 ouest, comme en surface.</summary>
        private static int RoadMask(PlanData plan, int x, int y)
        {
            int mask = 0;
            if (IsRoadAt(plan, x, y + 1)) mask |= 1;
            if (IsRoadAt(plan, x + 1, y)) mask |= 2;
            if (IsRoadAt(plan, x, y - 1)) mask |= 4;
            if (IsRoadAt(plan, x - 1, y)) mask |= 8;
            return mask;
        }
    }
}
