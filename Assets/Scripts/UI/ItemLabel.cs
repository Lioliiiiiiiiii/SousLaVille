using UnityEngine;
using UnityEngine.UI;

namespace SousLaVille.UI
{
    /// <summary>
    /// Le nom de l'objet sur lequel on se tient, affiche au HUD.
    ///
    /// POURQUOI PAS DANS LE DECOR. Jusqu'au 4 septembre 2026, chaque plaque et chaque
    /// echantillon portait son nom ecrit juste en dessous, en permanence. A l'ecran, huit
    /// noms de cinq sur sept pixels poses sur du pave, tous en meme temps, ne se lisaient
    /// pas : trop petits, trop nombreux, et sur un fond charge.
    ///
    /// Ils sont donc remontes ici. Un seul nom a la fois, celui de l'objet foule, sur un
    /// fond sombre uni et a la resolution de reference : c'est exactement ce qui rend les
    /// phrases des personnages lisibles depuis la phase 9a. Le decor, lui, ne montre plus
    /// que les objets.
    ///
    /// Le picto de la saison vaincue l'accompagne, quand l'objet en a un. Il etait dans le
    /// decor lui aussi, en trente-deux pixels de cote a cote d'un echantillon de seize :
    /// deux fois trop gros. Ici, il fait la taille des autres pictos du HUD.
    ///
    /// Rien de modal : le personnage continue de marcher, la boite ne consomme aucune touche
    /// et disparait des qu'on quitte la case. C'est un cartel, pas un dialogue.
    /// </summary>
    public class ItemLabel : MonoBehaviour
    {
        [Tooltip("Le panneau entier, eteint tant qu'on ne foule aucun objet.")]
        [SerializeField] private GameObject panel;

        [Tooltip("L'image du nom, dessinee par PixelFont.")]
        [SerializeField] private Image label;

        [Tooltip("Le picto de la saison vaincue, quand l'objet en a un.")]
        [SerializeField] private Image icon;

        /// <summary>Marge autour du contenu, en pixels de la resolution de reference.</summary>
        private const float Padding = 6f;

        /// <summary>Cote du picto de saison. Deux tiers de la boite : il l'accompagne, il ne la remplit pas.</summary>
        // Vingt-quatre depuis la phase 18f : la taille exacte du picto, plus aucune mise a
        // l'echelle a virgule.
        private const float IconSize = 24f;

        /// <summary>Blanc entre le picto et le nom.</summary>
        private const float Gap = 5f;

        private Sprite shown;

        /// <summary>Le nom affiche, ou null. Sert aux verifications.</summary>
        public Sprite Shown => shown;

        private void Awake()
        {
            Hide();
        }

        /// <summary>
        /// Montre un nom, et le picto qui l'accompagne s'il y en a un. Rappeler avec le meme
        /// nom ne fait rien : la boite est interrogee a chaque image.
        /// </summary>
        public void Show(Sprite name, Sprite seasonIcon)
        {
            if (name == null)
            {
                Hide();
                return;
            }

            if (shown == name)
            {
                return;
            }

            shown = name;

            // Surtout pas SetNativeSize : il divise par les pixels par unite du sprite,
            // seize ici, puis multiplie par les cent du Canvas. Le rectangle du sprite donne
            // directement la taille en pixels de la resolution de reference. Meme piege que
            // la boite de dialogue en phase 9a.
            float nameWidth = name.rect.width;
            float iconWidth = seasonIcon != null ? IconSize + Gap : 0f;

            // LA BOITE EPOUSE SON CONTENU. Une largeur fixe laissait « ISOLE », trente et un
            // pixels, flotter au milieu de cent soixante : le nom paraissait perdu et la
            // boite, elle, pesait plus que ce qu'elle disait.
            float total = Padding * 2f + iconWidth + nameWidth;

            RectTransform panelRect = panel != null ? panel.transform as RectTransform : null;
            if (panelRect != null)
            {
                panelRect.sizeDelta = new Vector2(total, panelRect.sizeDelta.y);
            }

            // Le picto d'abord, le nom ensuite, le tout centre dans la boite.
            float left = -total * 0.5f + Padding;

            if (icon != null)
            {
                icon.sprite = seasonIcon;
                icon.enabled = seasonIcon != null;
                icon.rectTransform.sizeDelta = new Vector2(IconSize, IconSize);
                icon.rectTransform.anchoredPosition = new Vector2(left + IconSize * 0.5f, 0f);
            }

            if (label != null)
            {
                label.sprite = name;
                label.rectTransform.sizeDelta = new Vector2(nameWidth, name.rect.height);
                label.rectTransform.anchoredPosition =
                    new Vector2(left + iconWidth + nameWidth * 0.5f, 0f);
            }

            if (panel != null)
            {
                panel.SetActive(true);
            }
        }

        /// <summary>Efface le cartel. Appele des qu'on quitte la case.</summary>
        public void Hide()
        {
            if (shown == null && panel != null && !panel.activeSelf)
            {
                return;
            }

            shown = null;

            if (panel != null)
            {
                panel.SetActive(false);
            }
        }
    }
}
