using System.Collections.Generic;
using SousLaVille.Core;
using SousLaVille.UI;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

namespace SousLaVille.EditorTools
{
    /// <summary>
    /// L'ECRAN DE CHOIX DU VILLAGE, phase 20. Le premier ecran du jeu.
    ///
    /// Partie du constructeur de Persistent et non fichier a part : il vit dans le meme HUD que
    /// le plan du village et les trois mini-jeux, et il partage CreateCenteredImage, LoadSprite
    /// et les constantes de mise en page. Le fichier principal fait mille cinq cents lignes ;
    /// celui-ci porte cet ecran, et rien d'autre.
    ///
    /// Toute la geometrie tient dans les cinq nombres qui suivent, et ValidateVillageScreen les
    /// rejoue par le calcul a chaque construction. Rien ici n'est libre.
    /// </summary>
    public static partial class PersistentSceneBuilder
    {
        /// <summary>Hauteur d'une ligne. Elle porte une vignette de 32 et deux actions de 32.</summary>
        private const float VillageRowHeight = 40f;

        /// <summary>
        /// Largeur d'une ligne, marges de l'ecran deduites.
        ///
        /// static readonly et non const, pour la raison du memory de la phase 14 : le
        /// compilateur replierait « 280f &gt; 312f » et signalerait le filet plus bas comme du
        /// code mort, ce qui reviendrait a supprimer la garde pour faire taire l'avertissement.
        /// C'est un reglage, pas une constante de compilation.
        /// </summary>
        private static readonly float VillageRowWidth = 280f;

        /// <summary>Blanc entre deux lignes.</summary>
        private const float VillageRowGutter = 4f;

        /// <summary>
        /// Cote d'une vignette et d'une zone d'action. C'est le plancher de zone cliquable de
        /// CLAUDE.md, et c'est deux fois les seize pixels des images : un agrandissement entier,
        /// qui ne trouble aucun pixel.
        ///
        /// static readonly et non const, pour la meme raison que le memory de la phase 14 : le
        /// compilateur replierait « 32f &lt; 32f » et signalerait la garde comme du code mort.
        /// </summary>
        private static readonly float VillageActionSize = 32f;

        /// <summary>Blanc entre la vignette et le nom, et entre les deux actions.</summary>
        private const float VillageGap = 6f;

        /// <summary>
        /// Construit l'ecran. Une ligne par emplacement de SaveSystem, plus la ligne de
        /// creation : le nombre vient de la, et de nulle part ailleurs.
        /// </summary>
        private static void CreateVillageSelect(GameObject canvasObject)
        {
            if (!ValidateVillageScreen())
            {
                return;
            }

            GameObject panel = new GameObject("VillageSelect");
            panel.transform.SetParent(canvasObject.transform, false);

            RectTransform panelRect = panel.AddComponent<RectTransform>();
            panelRect.anchorMin = Vector2.zero;
            panelRect.anchorMax = Vector2.one;
            panelRect.offsetMin = Vector2.zero;
            panelRect.offsetMax = Vector2.zero;

            // Opaque, pour la raison du memory de la phase 14 : les gouttes du HUD occupent le
            // coin haut droit et transparaitraient au travers d'un voile.
            Image background = panel.AddComponent<Image>();
            background.color = MiniGameBackground;
            background.raycastTarget = false;

            Sprite thumbnail = LoadSprite(PlaceholderArtGenerator.PictoVillage);
            Sprite playIcon = LoadSprite(PlaceholderArtGenerator.PictoEnter);
            Sprite eraseIcon = LoadSprite(PlaceholderArtGenerator.PictoErase);
            Sprite newIcon = LoadSprite(PlaceholderArtGenerator.PictoNew);

            int rowCount = SaveSystem.SlotCount + 1;
            float spacing = VillageRowHeight + VillageRowGutter;
            float top = (rowCount - 1) * spacing * 0.5f;

            var rows = new List<GameObject>(SaveSystem.SlotCount);
            var plays = new List<RectTransform>(SaveSystem.SlotCount);
            var erases = new List<RectTransform>(SaveSystem.SlotCount);
            var names = new List<Sprite>(SaveSystem.SlotCount);

            for (int slot = 1; slot <= SaveSystem.SlotCount; slot++)
            {
                float y = top - (slot - 1) * spacing;
                Sprite nameSprite = LoadSprite(PlaceholderArtGenerator.VillageNameTexture(slot));
                names.Add(nameSprite);

                GameObject row = CreateVillageRow(panel.transform, $"Village_{slot}", y,
                    thumbnail, nameSprite);

                plays.Add(CreateVillageAction(row.transform, "Play", ActionX(second: false), playIcon));
                erases.Add(CreateVillageAction(row.transform, "Erase", ActionX(second: true), eraseIcon));

                rows.Add(row);
            }

            // La ligne de creation : la vignette en gris, le nom, et une seule action.
            GameObject createRow = CreateVillageRow(panel.transform, "Create",
                top - SaveSystem.SlotCount * spacing, thumbnail,
                LoadSprite(PlaceholderArtGenerator.VillageNewTexture));

            // La vignette grisee dit « il n'y a pas encore de village ici » sans un mot, et sans
            // une image de plus a dessiner.
            Image createThumb = createRow.transform.Find("Thumbnail").GetComponent<Image>();
            createThumb.color = new Color(1f, 1f, 1f, 0.35f);

            RectTransform createAction =
                CreateVillageAction(createRow.transform, "New", ActionX(second: true), newIcon);

            // Le cadre de selection, cree APRES les actions : il passe par-dessus.
            Image cursor = CreateCenteredImage(panel.transform, "Cursor", Vector2.zero,
                new Vector2(VillageActionSize, VillageActionSize));
            cursor.sprite = LoadSprite(PlaceholderArtGenerator.CursorTarget);

            GameObject confirm = CreateVillageConfirm(panel.transform, eraseIcon,
                out Image confirmLabel, out RectTransform confirmNo, out RectTransform confirmYes,
                out RectTransform confirmCursor);

            VillageSelectScreen screen = canvasObject.AddComponent<VillageSelectScreen>();

            SerializedObject serialized = new SerializedObject(screen);
            serialized.FindProperty("panel").objectReferenceValue = panel;
            serialized.FindProperty("createRow").objectReferenceValue = createRow;
            serialized.FindProperty("createAction").objectReferenceValue = createAction;
            serialized.FindProperty("cursor").objectReferenceValue = cursor.rectTransform;
            serialized.FindProperty("confirmPanel").objectReferenceValue = confirm;
            serialized.FindProperty("confirmLabel").objectReferenceValue = confirmLabel;
            serialized.FindProperty("confirmNo").objectReferenceValue = confirmNo;
            serialized.FindProperty("confirmYes").objectReferenceValue = confirmYes;
            serialized.FindProperty("confirmCursor").objectReferenceValue = confirmCursor;
            serialized.FindProperty("rowSpacing").floatValue = spacing;

            SerializedProperty rowProperty = serialized.FindProperty("rows");
            rowProperty.arraySize = rows.Count;

            for (int i = 0; i < rows.Count; i++)
            {
                SerializedProperty element = rowProperty.GetArrayElementAtIndex(i);
                element.FindPropertyRelative("root").objectReferenceValue = rows[i];
                element.FindPropertyRelative("play").objectReferenceValue = plays[i];
                element.FindPropertyRelative("erase").objectReferenceValue = erases[i];
            }

            SerializedProperty nameProperty = serialized.FindProperty("nameSprites");
            nameProperty.arraySize = names.Count;

            for (int i = 0; i < names.Count; i++)
            {
                nameProperty.GetArrayElementAtIndex(i).objectReferenceValue = names[i];
            }

            serialized.ApplyModifiedPropertiesWithoutUndo();

            // Eteint au depart : le Bootstrapper l'allume, une fois Persistent chargee.
            panel.SetActive(false);
        }

        /// <summary>Abscisse d'une zone d'action dans sa ligne. La seconde est la plus a droite.</summary>
        private static float ActionX(bool second)
        {
            float right = VillageRowWidth * 0.5f - VillageRowGutter - VillageActionSize * 0.5f;
            return second ? right : right - VillageActionSize - VillageGap;
        }

        /// <summary>Une ligne : sa vignette a gauche, son nom a cote. Les actions viennent apres.</summary>
        private static GameObject CreateVillageRow(Transform parent, string name, float y,
            Sprite thumbnail, Sprite label)
        {
            GameObject row = new GameObject(name);
            row.transform.SetParent(parent, false);

            RectTransform rect = row.AddComponent<RectTransform>();
            rect.anchorMin = new Vector2(0.5f, 0.5f);
            rect.anchorMax = new Vector2(0.5f, 0.5f);
            rect.pivot = new Vector2(0.5f, 0.5f);
            rect.anchoredPosition = new Vector2(0f, y);
            rect.sizeDelta = new Vector2(VillageRowWidth, VillageRowHeight);

            float left = -VillageRowWidth * 0.5f + VillageRowGutter;

            Image thumb = CreateCenteredImage(row.transform, "Thumbnail",
                new Vector2(left + VillageActionSize * 0.5f, 0f),
                new Vector2(VillageActionSize, VillageActionSize));
            thumb.sprite = thumbnail;

            // Le nom est cale a GAUCHE, contre la vignette : « VILLAGE 1 » et « NOUVEAU VILLAGE »
            // n'ont pas la meme largeur, et deux noms centres danseraient d'une ligne a l'autre.
            float textLeft = left + VillageActionSize + VillageGap;
            float width = label != null ? label.rect.width : 0f;
            float height = label != null ? label.rect.height : 0f;

            Image text = CreateCenteredImage(row.transform, "Name",
                new Vector2(textLeft + width * 0.5f, 0f), new Vector2(width, height));
            text.sprite = label;

            return row;
        }

        /// <summary>Une zone d'action de 32 px, son picto dedans. Rend le rectangle, pour le curseur.</summary>
        private static RectTransform CreateVillageAction(Transform parent, string name, float x,
            Sprite icon)
        {
            Image image = CreateCenteredImage(parent, name, new Vector2(x, 0f),
                new Vector2(VillageActionSize, VillageActionSize));
            image.sprite = icon;
            return image.rectTransform;
        }

        /// <summary>
        /// La bande de confirmation d'effacement : la poubelle, le nom du village vise, puis
        /// NON et OUI. NON est a GAUCHE parce que le curseur s'y pose, et qu'on ne quitte pas
        /// un refus par accident en poussant vers la gauche.
        /// </summary>
        private static GameObject CreateVillageConfirm(Transform parent, Sprite eraseIcon,
            out Image label, out RectTransform no, out RectTransform yes, out RectTransform cursor)
        {
            GameObject confirm = new GameObject("Confirm");
            confirm.transform.SetParent(parent, false);

            RectTransform rect = confirm.AddComponent<RectTransform>();
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;

            Image background = confirm.AddComponent<Image>();
            background.color = MiniGameBackground;
            background.raycastTarget = false;

            Image icon = CreateCenteredImage(confirm.transform, "Icon", new Vector2(0f, 48f),
                new Vector2(VillageActionSize, VillageActionSize));
            icon.sprite = eraseIcon;

            // Le nom est pose sans sprite : VillageSelectScreen y met celui du village vise.
            label = CreateCenteredImage(confirm.transform, "Name", new Vector2(0f, 16f),
                new Vector2(VillageNameWidth, VillageNameHeight));

            no = CreateVillageAction(confirm.transform, "No", -32f,
                LoadSprite(PlaceholderArtGenerator.PictoNo));
            no.anchoredPosition = new Vector2(-32f, -24f);

            yes = CreateVillageAction(confirm.transform, "Yes", 32f,
                LoadSprite(PlaceholderArtGenerator.PictoYes));
            yes.anchoredPosition = new Vector2(32f, -24f);

            Image frame = CreateCenteredImage(confirm.transform, "Cursor", new Vector2(-32f, -24f),
                new Vector2(VillageActionSize, VillageActionSize));
            frame.sprite = LoadSprite(PlaceholderArtGenerator.CursorTarget);
            cursor = frame.rectTransform;

            confirm.SetActive(false);
            return confirm;
        }

        /// <summary>Largeur reservee au nom sur la bande de confirmation. « VILLAGE 9 » au plus large.</summary>
        private const float VillageNameWidth = 80f;

        private const float VillageNameHeight = 16f;

        /// <summary>
        /// LE FILET DE LA PHASE 20, joue a chaque construction. Il rejoue la mise en page par le
        /// calcul : si un nombre change et que l'ecran deborde, la construction le dit, elle ne
        /// dessine pas un ecran faux en silence.
        /// </summary>
        private static bool ValidateVillageScreen()
        {
            bool ok = true;

            if (VillageActionSize < MinimumTouchSize)
            {
                Debug.LogError($"[Sous la Ville] Une action de l'écran de choix fait " +
                               $"{VillageActionSize} px : le plancher est {MinimumTouchSize}.");
                ok = false;
            }

            int rowCount = SaveSystem.SlotCount + 1;
            float height = rowCount * VillageRowHeight + (rowCount - 1) * VillageRowGutter;

            if (height > ReferenceHeight - 2 * ScreenMargin)
            {
                Debug.LogError($"[Sous la Ville] {rowCount} lignes font {height} px de haut : " +
                               $"elles ne tiennent pas dans les {ReferenceHeight} px de l'écran.");
                ok = false;
            }

            if (VillageRowWidth > ReferenceWidth - 2 * ScreenMargin)
            {
                Debug.LogError($"[Sous la Ville] Une ligne fait {VillageRowWidth} px de large : " +
                               $"elle ne tient pas dans les {ReferenceWidth} px de l'écran.");
                ok = false;
            }

            // Le nom doit tenir entre la vignette et la premiere action, sinon il passerait
            // dessous. C'est le seul endroit ou la longueur du texte contraint la mise en page.
            float room = ActionX(second: false) - VillageActionSize * 0.5f - VillageGap
                       - (-VillageRowWidth * 0.5f + VillageRowGutter + VillageActionSize + VillageGap);

            for (int slot = 1; slot <= SaveSystem.SlotCount; slot++)
            {
                ok &= FitsVillageName($"VILLAGE {slot}", room);
            }

            ok &= FitsVillageName(PlaceholderArtGenerator.VillageNewLabel, room);

            return ok;
        }

        private static bool FitsVillageName(string word, float room)
        {
            int width = PixelFont.WidthOf(word);

            if (width <= room)
            {
                return true;
            }

            Debug.LogError($"[Sous la Ville] Le nom « {word} » fait {width} px de large, " +
                           $"pour {room} px libres entre la vignette et les actions.");
            return false;
        }
    }
}
