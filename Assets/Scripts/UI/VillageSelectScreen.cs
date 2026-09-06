using System;
using SousLaVille.Core;
using UnityEngine;
using UnityEngine.UI;

namespace SousLaVille.UI
{
    /// <summary>
    /// L'ECRAN DE CHOIX DU VILLAGE, phase 20. Le premier ecran du jeu, et le seul.
    ///
    /// Une ligne par village existant, puis une ligne « nouveau village » tant qu'il reste de
    /// la place. Les fleches haut et bas changent de ligne, gauche et droite changent d'action
    /// SUR la ligne, Espace fait. Aucune autre touche, aucune combinaison, aucun timing :
    /// exactement les memes gestes que le plan du village de la phase 7.
    ///
    /// POURQUOI IL COUVRE TOUT L'ECRAN. Il s'ouvre avant que les couches de jeu ne soient
    /// chargees, donc devant un monde vide, mais devant un HUD deja construit — la scene
    /// Persistent porte le compteur de gouttes et l'indicateur de saison. Son fond est opaque
    /// et plein cadre : rien du jeu ne doit transparaitre tant qu'aucun village n'est choisi.
    ///
    /// L'EFFACEMENT SE CONFIRME. Espace sur la poubelle n'efface pas : il ouvre une bande de
    /// confirmation ou le curseur se pose sur NON. Il faut donc aller chercher OUI a la fleche,
    /// puis appuyer une seconde fois. Deux appuis separes par un deplacement volontaire ;
    /// aucun geste distrait ne detruit un village.
    /// </summary>
    public class VillageSelectScreen : MonoBehaviour
    {
        /// <summary>Une ligne de village. Le libelle est pose une fois pour toutes par le builder.</summary>
        [Serializable]
        public class VillageRow
        {
            [Tooltip("La ligne entiere, eteinte quand l'emplacement est libre.")]
            public GameObject root;

            [Tooltip("Le cadre de l'action « jouer ».")]
            public RectTransform play;

            [Tooltip("Le cadre de l'action « effacer ».")]
            public RectTransform erase;
        }

        [Tooltip("Le panneau entier, eteint tant que le jeu n'a pas demarre.")]
        [SerializeField] private GameObject panel;

        [Tooltip("Une entree par emplacement de SaveSystem, dans l'ordre des numeros.")]
        [SerializeField] private VillageRow[] rows;

        [Tooltip("La ligne « nouveau village », eteinte quand les emplacements sont tous pris.")]
        [SerializeField] private GameObject createRow;

        [Tooltip("La zone d'action de la ligne « nouveau village ».")]
        [SerializeField] private RectTransform createAction;

        [Tooltip("Le cadre pose sur l'action visee.")]
        [SerializeField] private RectTransform cursor;

        [Tooltip("La bande de confirmation d'effacement, eteinte le reste du temps.")]
        [SerializeField] private GameObject confirmPanel;

        [Tooltip("Le nom du village qu'on s'apprete a effacer.")]
        [SerializeField] private Image confirmLabel;

        [Tooltip("Le nom ecrit de chaque emplacement, dans l'ordre des numeros.")]
        [SerializeField] private Sprite[] nameSprites;

        [Tooltip("Le cadre du NON. C'est la qu'on arrive.")]
        [SerializeField] private RectTransform confirmNo;

        [Tooltip("Le cadre du OUI.")]
        [SerializeField] private RectTransform confirmYes;

        [Tooltip("Le cadre pose sur le NON ou le OUI.")]
        [SerializeField] private RectTransform confirmCursor;

        [Tooltip("Distance verticale entre deux lignes voisines. Posee par le builder.")]
        [SerializeField] private float rowSpacing = 44f;

        /// <summary>Les actions d'une ligne de village, dans l'ordre ou les fleches les parcourent.</summary>
        private const int ActionPlay = 0;
        private const int ActionErase = 1;

        /// <summary>Le rang de la ligne « nouveau village » vaut le nombre de lignes de village.</summary>
        private int CreateRowIndex => rows == null ? 0 : rows.Length;

        private bool confirming;
        private int rowIndex;
        private int actionIndex;
        private bool confirmYesSelected;
        private int openedFrame = -1;
        private Vector2Int lastDirection;
        private SousLaVilleInputActions input;

        /// <summary>Vrai tant que le choix n'est pas fait. Le Bootstrapper attend la-dessus.</summary>
        public bool IsOpen { get; private set; }

        /// <summary>L'emplacement a jouer. Leve une seule fois, a la fermeture.</summary>
        public event Action<int> Chosen;

        private void Awake()
        {
            EnsureInput();
            Hide();
        }

        private void OnEnable()
        {
            // Recompiler pendant le play recharge le domaine : Unity rappelle OnEnable sans
            // repasser par Awake. Meme garde-fou que dans VillageMapScreen.
            EnsureInput();
            input.Gameplay.Enable();
        }

        private void OnDisable()
        {
            input?.Gameplay.Disable();
        }

        private void OnDestroy()
        {
            input?.Dispose();
        }

        private void EnsureInput()
        {
            if (input == null)
            {
                input = new SousLaVilleInputActions();
            }
        }

        private void Hide()
        {
            IsOpen = false;

            if (panel != null)
            {
                panel.SetActive(false);
            }
        }

        /// <summary>
        /// Ouvre l'ecran. La partie d'avant la phase 20 est reprise d'abord : si elle existe,
        /// elle est deja le village 1 quand les lignes se dessinent.
        /// </summary>
        public void Open()
        {
            SaveSystem.MigrateLegacySave();

            confirming = false;
            confirmYesSelected = false;
            actionIndex = ActionPlay;
            lastDirection = Vector2Int.zero;
            openedFrame = Time.frameCount;

            // Le premier village existant, ou la ligne de creation s'il n'y en a aucun.
            rowIndex = CreateRowIndex;
            for (int slot = 1; slot <= SaveSystem.SlotCount; slot++)
            {
                if (SaveSystem.SlotExists(slot))
                {
                    rowIndex = slot - 1;
                    break;
                }
            }

            IsOpen = true;

            if (panel != null)
            {
                panel.SetActive(true);
            }

            Refresh();
        }

        private void Update()
        {
            if (!IsOpen)
            {
                return;
            }

            // L'appui qui ouvre l'ecran ne doit pas choisir dans la foulee.
            if (Time.frameCount == openedFrame)
            {
                return;
            }

            ReadDirection();

            if (input.Gameplay.Interact.WasPressedThisFrame())
            {
                Act();
            }
        }

        /// <summary>
        /// Lit les fleches et n'agit qu'au changement de direction : maintenir une fleche ne
        /// fait pas defiler, et il n'y a aucun timing a attraper.
        /// </summary>
        private void ReadDirection()
        {
            Vector2Int direction = Dominant(input.Gameplay.Move.ReadValue<Vector2>());

            if (direction == lastDirection)
            {
                return;
            }

            lastDirection = direction;

            if (direction != Vector2Int.zero)
            {
                Move(direction);
            }
        }

        /// <summary>
        /// Une seule direction a la fois, jamais de diagonale : c'est la regle de CLAUDE.md,
        /// et c'est celle du deplacement du personnage.
        /// </summary>
        private static Vector2Int Dominant(Vector2 raw)
        {
            if (Mathf.Abs(raw.x) < 0.5f && Mathf.Abs(raw.y) < 0.5f)
            {
                return Vector2Int.zero;
            }

            if (Mathf.Abs(raw.x) >= Mathf.Abs(raw.y))
            {
                return new Vector2Int(raw.x > 0f ? 1 : -1, 0);
            }

            return new Vector2Int(0, raw.y > 0f ? 1 : -1);
        }

        /// <summary>
        /// Deplace le curseur. Publique parce qu'elle se verifie ainsi sans clavier.
        ///
        /// Rien ne boucle : pousser vers le vide au bord de la liste ne fait rien. Un enfant
        /// qui maintient une fleche ne doit pas revenir a son point de depart sans le vouloir.
        /// </summary>
        public void Move(Vector2Int direction)
        {
            if (!IsOpen || direction == Vector2Int.zero)
            {
                return;
            }

            if (confirming)
            {
                // Sur la bande de confirmation, seules gauche et droite comptent.
                if (direction.x != 0)
                {
                    confirmYesSelected = direction.x > 0;
                    Refresh();
                }

                return;
            }

            if (direction.y != 0)
            {
                // A l'ecran, la premiere ligne est en HAUT : la fleche du haut descend d'un rang.
                //
                // ON SAUTE LES LIGNES ETEINTES. Vu en jeu le 6 septembre 2026 : avec un seul
                // village, la ligne du village 2 est eteinte entre lui et « nouveau village »,
                // et s'arreter dessus rendait la ligne de creation INATTEIGNABLE — la fleche du
                // bas ne faisait rien. Un emplacement libre n'est pas une etape du parcours.
                int next = rowIndex - direction.y;

                while (next >= 0 && next <= CreateRowIndex && !IsRowVisible(next))
                {
                    next -= direction.y;
                }

                if (next >= 0 && next <= CreateRowIndex)
                {
                    rowIndex = next;
                    actionIndex = ActionPlay;
                    Refresh();
                }

                return;
            }

            if (rowIndex == CreateRowIndex)
            {
                // La ligne de creation n'a qu'une action : gauche et droite n'y font rien.
                return;
            }

            int wanted = actionIndex + (direction.x > 0 ? 1 : -1);

            if (wanted >= ActionPlay && wanted <= ActionErase)
            {
                actionIndex = wanted;
                Refresh();
            }
        }

        /// <summary>Espace. Publique pour la meme raison que Move.</summary>
        public void Act()
        {
            if (!IsOpen)
            {
                return;
            }

            if (confirming)
            {
                if (confirmYesSelected)
                {
                    SaveSystem.DeleteSlot(rowIndex + 1);
                }

                confirming = false;
                confirmYesSelected = false;

                // L'emplacement efface, la ligne disparait : le curseur doit se poser sur une
                // ligne qui existe encore, sinon il pointerait le vide.
                if (!IsRowVisible(rowIndex))
                {
                    rowIndex = FirstVisibleRow();
                }

                actionIndex = ActionPlay;
                Refresh();
                return;
            }

            if (rowIndex == CreateRowIndex)
            {
                int free = FirstFreeSlot();

                if (free > 0)
                {
                    // Aucun fichier n'est ecrit ici : un emplacement libre EST un village neuf,
                    // et la premiere sauvegarde le creera. Rien a nettoyer si le jeu s'arrete.
                    Close(free);
                }

                return;
            }

            if (actionIndex == ActionErase)
            {
                confirming = true;
                confirmYesSelected = false;
                Refresh();
                return;
            }

            Close(rowIndex + 1);
        }

        private void Close(int slot)
        {
            Hide();
            Chosen?.Invoke(slot);
        }

        /// <summary>Le premier emplacement libre, ou zero s'ils sont tous pris.</summary>
        private static int FirstFreeSlot()
        {
            for (int slot = 1; slot <= SaveSystem.SlotCount; slot++)
            {
                if (!SaveSystem.SlotExists(slot))
                {
                    return slot;
                }
            }

            return 0;
        }

        /// <summary>
        /// Une ligne de village n'est visible que si son emplacement porte un village ; la
        /// ligne de creation, que s'il reste de la place. Les deux ne peuvent etre invisibles
        /// en meme temps : sans village, il reste forcement de la place.
        /// </summary>
        private bool IsRowVisible(int index)
        {
            if (index < 0 || index > CreateRowIndex)
            {
                return false;
            }

            if (index == CreateRowIndex)
            {
                return FirstFreeSlot() > 0;
            }

            return SaveSystem.SlotExists(index + 1);
        }

        private int FirstVisibleRow()
        {
            for (int index = 0; index <= CreateRowIndex; index++)
            {
                if (IsRowVisible(index))
                {
                    return index;
                }
            }

            return CreateRowIndex;
        }

        /// <summary>
        /// Redessine l'ecran : les lignes qui existent, la bande de confirmation, et le cadre
        /// pose sur l'action visee. Appelee a chaque geste, jamais a chaque image.
        /// </summary>
        private void Refresh()
        {
            if (rows != null)
            {
                for (int index = 0; index < rows.Length; index++)
                {
                    if (rows[index] != null && rows[index].root != null)
                    {
                        rows[index].root.SetActive(IsRowVisible(index));
                    }
                }
            }

            if (createRow != null)
            {
                createRow.SetActive(IsRowVisible(CreateRowIndex));
            }

            StackVisibleRows();

            if (confirmPanel != null)
            {
                confirmPanel.SetActive(confirming);
            }

            if (confirming)
            {
                if (confirmLabel != null && nameSprites != null &&
                    rowIndex >= 0 && rowIndex < nameSprites.Length)
                {
                    confirmLabel.sprite = nameSprites[rowIndex];
                }

                MoveCursor(confirmCursor, confirmYesSelected ? confirmYes : confirmNo);

                if (cursor != null)
                {
                    cursor.gameObject.SetActive(false);
                }

                return;
            }

            if (cursor != null)
            {
                cursor.gameObject.SetActive(true);
            }

            MoveCursor(cursor, TargetOfCursor());
        }

        /// <summary>
        /// Reserre les lignes visibles et recentre le bloc.
        ///
        /// Sans cela, un emplacement libre laisserait un trou : avec le seul village 1, on
        /// lisait « VILLAGE 1 », puis quarante pixels de vide, puis « NOUVEAU VILLAGE ». Le
        /// vide se lisait comme un defaut d'affichage, alors qu'il ne disait rien.
        /// </summary>
        private void StackVisibleRows()
        {
            int visible = 0;

            for (int index = 0; index <= CreateRowIndex; index++)
            {
                if (IsRowVisible(index))
                {
                    visible++;
                }
            }

            float top = (visible - 1) * rowSpacing * 0.5f;
            int placed = 0;

            for (int index = 0; index <= CreateRowIndex; index++)
            {
                if (!IsRowVisible(index))
                {
                    continue;
                }

                RectTransform rect = RowRect(index);

                if (rect != null)
                {
                    rect.anchoredPosition = new Vector2(rect.anchoredPosition.x, top - placed * rowSpacing);
                }

                placed++;
            }
        }

        private RectTransform RowRect(int index)
        {
            if (index == CreateRowIndex)
            {
                return createRow == null ? null : createRow.transform as RectTransform;
            }

            if (rows == null || index < 0 || index >= rows.Length || rows[index] == null ||
                rows[index].root == null)
            {
                return null;
            }

            return rows[index].root.transform as RectTransform;
        }

        private RectTransform TargetOfCursor()
        {
            if (rowIndex == CreateRowIndex)
            {
                return createAction;
            }

            if (rows == null || rowIndex < 0 || rowIndex >= rows.Length || rows[rowIndex] == null)
            {
                return null;
            }

            return actionIndex == ActionErase ? rows[rowIndex].erase : rows[rowIndex].play;
        }

        private static void MoveCursor(RectTransform moving, RectTransform target)
        {
            if (moving == null || target == null)
            {
                return;
            }

            moving.position = target.position;
        }
    }
}
