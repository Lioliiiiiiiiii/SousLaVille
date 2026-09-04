using UnityEngine;

namespace SousLaVille.EditorTools
{
    /// <summary>
    /// Une police de 5 sur 7 pixels, dessinee a la main, majuscules seulement.
    ///
    /// Pourquoi pas de l'uGUI et une vraie police : une TTF s'affiche lissee et hors grille,
    /// et jurerait a cote d'un art entierement en pixels a la resolution 320x180. Les mots du
    /// jeu sont donc des images, produites une fois pour toutes par le generateur d'art.
    ///
    /// Les huit noms de villes de la phase 7 sont les premiers a s'en servir. La phase 9a y
    /// ajoute les accents et l'apostrophe : ISOLE en avait besoin, et les phrases des
    /// personnages aussi. La Fabrique, en phase 14, reprendra la police telle quelle.
    ///
    /// Un accent demande deux rangees de plus au-dessus des sept du glyphe. Elles ne sont
    /// reservees QUE si le mot porte un accent, d'ou HeightOf(word) plutot qu'une hauteur
    /// constante : sans cela, les huit noms de villes de la phase 7 grandiraient de deux
    /// pixels et leurs images changeraient sans raison.
    /// </summary>
    public static class PixelFont
    {
        public const int GlyphWidth = 5;
        public const int GlyphHeight = 7;

        /// <summary>Une colonne vide entre deux lettres.</summary>
        public const int Tracking = 1;

        /// <summary>Le liseré déborde d'un pixel de chaque côté.</summary>
        public const int Padding = 1;

        /// <summary>Rangées ajoutées au-dessus du glyphe pour porter un accent.</summary>
        public const int AccentHeight = 2;

        // Ligne du haut en premier, comme on ecrit. Le retournement vers le repere des
        // textures, dont l'origine est en bas, se fait au moment du rendu.
        private static readonly string[][] Glyphs =
        {
            new[] { ".###.", "#...#", "#...#", "#####", "#...#", "#...#", "#...#" }, // A
            new[] { "####.", "#...#", "#...#", "####.", "#...#", "#...#", "####." }, // B
            new[] { ".###.", "#...#", "#....", "#....", "#....", "#...#", ".###." }, // C
            new[] { "####.", "#...#", "#...#", "#...#", "#...#", "#...#", "####." }, // D
            new[] { "#####", "#....", "#....", "####.", "#....", "#....", "#####" }, // E
            new[] { "#####", "#....", "#....", "####.", "#....", "#....", "#...." }, // F
            new[] { ".###.", "#...#", "#....", "#.###", "#...#", "#...#", ".###." }, // G
            new[] { "#...#", "#...#", "#...#", "#####", "#...#", "#...#", "#...#" }, // H
            new[] { "#####", "..#..", "..#..", "..#..", "..#..", "..#..", "#####" }, // I
            new[] { "#####", "...#.", "...#.", "...#.", "...#.", "#..#.", ".##.." }, // J
            new[] { "#...#", "#..#.", "#.#..", "##...", "#.#..", "#..#.", "#...#" }, // K
            new[] { "#....", "#....", "#....", "#....", "#....", "#....", "#####" }, // L
            new[] { "#...#", "##.##", "#.#.#", "#.#.#", "#...#", "#...#", "#...#" }, // M
            new[] { "#...#", "##..#", "#.#.#", "#.#.#", "#..##", "#...#", "#...#" }, // N
            new[] { ".###.", "#...#", "#...#", "#...#", "#...#", "#...#", ".###." }, // O
            new[] { "####.", "#...#", "#...#", "####.", "#....", "#....", "#...." }, // P
            new[] { ".###.", "#...#", "#...#", "#...#", "#.#.#", "#..#.", ".##.#" }, // Q
            new[] { "####.", "#...#", "#...#", "####.", "#.#..", "#..#.", "#...#" }, // R
            new[] { ".####", "#....", "#....", ".###.", "....#", "....#", "####." }, // S
            new[] { "#####", "..#..", "..#..", "..#..", "..#..", "..#..", "..#.." }, // T
            new[] { "#...#", "#...#", "#...#", "#...#", "#...#", "#...#", ".###." }, // U
            new[] { "#...#", "#...#", "#...#", "#...#", "#...#", ".#.#.", "..#.." }, // V
            new[] { "#...#", "#...#", "#...#", "#.#.#", "#.#.#", "##.##", "#...#" }, // W
            new[] { "#...#", "#...#", ".#.#.", "..#..", ".#.#.", "#...#", "#...#" }, // X
            new[] { "#...#", "#...#", ".#.#.", "..#..", "..#..", "..#..", "..#.." }, // Y
            new[] { "#####", "....#", "...#.", "..#..", ".#...", "#....", "#####" }, // Z
        };

        /// <summary>
        /// L'apostrophe. Elle occupe une cellule entiere comme les lettres : la police est a
        /// chasse fixe, et WidthOf compte les caracteres. Le blanc qu'elle laisse autour
        /// d'elle est un peu large, c'est un placeholder de plus a reprendre a l'habillage.
        /// </summary>
        private static readonly string[] Apostrophe =
        {
            "..#..", "..#..", ".....", ".....", ".....", ".....", "....."
        };

        // Ponctuation et chiffres, ajoutes en phase 12a. Les phrases des personnages-guides en
        // ont besoin : jusque-la, « AIDE-MOI ! » sortait « AIDE MOI  », sans un avertissement,
        // tout caractere inconnu ne dessinant rien tout en avancant d'une cellule.
        private static readonly string[] Hyphen =
        {
            ".....", ".....", ".....", ".###.", ".....", ".....", "....."
        };

        private static readonly string[] Exclamation =
        {
            "..#..", "..#..", "..#..", "..#..", "..#..", ".....", "..#.."
        };

        private static readonly string[] Question =
        {
            ".###.", "#...#", "....#", "...#.", "..#..", ".....", "..#.."
        };

        /// <summary>La cedille tient dans la septieme rangee : la police n'a pas de jambage.</summary>
        private static readonly string[] Cedilla =
        {
            ".###.", "#...#", "#....", "#....", "#...#", ".###.", "..#.."
        };

        private static readonly string[][] DigitGlyphs =
        {
            new[] { ".###.", "#...#", "#..##", "#.#.#", "##..#", "#...#", ".###." }, // 0
            new[] { "..#..", ".##..", "..#..", "..#..", "..#..", "..#..", "#####" }, // 1
            new[] { ".###.", "#...#", "....#", "...#.", "..#..", ".#...", "#####" }, // 2
            new[] { "#####", "...#.", "..#..", "...#.", "....#", "#...#", ".###." }, // 3
            new[] { "...#.", "..##.", ".#.#.", "#..#.", "#####", "...#.", "...#." }, // 4
            new[] { "#####", "#....", "####.", "....#", "....#", "#...#", ".###." }, // 5
            new[] { "..##.", ".#...", "#....", "####.", "#...#", "#...#", ".###." }, // 6
            new[] { "#####", "....#", "...#.", "..#..", ".#...", ".#...", ".#..." }, // 7
            new[] { ".###.", "#...#", "#...#", ".###.", "#...#", "#...#", ".###." }, // 8
            new[] { ".###.", "#...#", "#...#", ".####", "....#", "...#.", ".##.." }, // 9
        };

        // Les trois accents, dessines dans les deux rangees au-dessus du glyphe. Ligne du
        // haut en premier, comme les lettres.
        private static readonly string[] Acute = { "...#.", "..#.." };
        private static readonly string[] Grave = { ".#...", "..#.." };
        private static readonly string[] Circumflex = { "..#..", ".#.#." };

        /// <summary>Largeur du dessin d'un mot, liseré compris.</summary>
        public static int WidthOf(string word)
        {
            if (string.IsNullOrEmpty(word))
            {
                return 0;
            }

            return word.Length * (GlyphWidth + Tracking) - Tracking + Padding * 2;
        }

        /// <summary>
        /// Hauteur du dessin d'un mot, liseré compris. Les deux rangées d'accent ne sont
        /// réservées que si le mot en porte un : un mot sans accent garde exactement la
        /// hauteur qu'il avait en phase 7.
        /// </summary>
        public static int HeightOf(string word)
        {
            return GlyphHeight + Padding * 2 + (HasAccent(word) ? AccentHeight : 0);
        }

        /// <summary>
        /// Vrai si chaque caractere du mot sait se dessiner. Un caractere inconnu ne dessine
        /// rien MAIS avance d'une cellule : le mot sort avec un trou, sans un avertissement.
        /// Le generateur d'art appelle ceci avant d'ecrire quoi que ce soit.
        /// </summary>
        public static bool CanRender(string word, out char missing)
        {
            missing = '\0';

            if (string.IsNullOrEmpty(word))
            {
                return true;
            }

            foreach (char character in word)
            {
                if (character == ' ' || GlyphFor(character) != null)
                {
                    continue;
                }

                missing = character;
                return false;
            }

            return true;
        }

        /// <summary>Vrai si le mot porte au moins une lettre accentuée.</summary>
        public static bool HasAccent(string word)
        {
            if (string.IsNullOrEmpty(word))
            {
                return false;
            }

            foreach (char character in word)
            {
                if (AccentFor(character) != null)
                {
                    return true;
                }
            }

            return false;
        }

        /// <summary>
        /// Dessine un mot en blanc cerné d'un liseré sombre. Le liseré n'est pas une
        /// coquetterie : sans lui, un nom clair posé sur du pavé clair ne se lit plus.
        /// </summary>
        public static Color32[] Render(string word, Color32 ink, Color32 outline)
        {
            int width = WidthOf(word);
            int height = HeightOf(word);

            Color32[] pixels = new Color32[width * height];
            bool[] mask = new bool[width * height];

            for (int i = 0; i < word.Length; i++)
            {
                int originX = Padding + i * (GlyphWidth + Tracking);

                string[] glyph = GlyphFor(word[i]);
                if (glyph != null)
                {
                    for (int row = 0; row < GlyphHeight; row++)
                    {
                        for (int column = 0; column < GlyphWidth; column++)
                        {
                            if (glyph[row][column] != '#')
                            {
                                continue;
                            }

                            // La ligne 0 de la lettre est en haut, la ligne 0 de la texture en bas.
                            int y = Padding + (GlyphHeight - 1 - row);
                            mask[y * width + originX + column] = true;
                        }
                    }
                }

                // L'accent se pose dans les deux rangees reservees au-dessus des sept du
                // glyphe. Elles n'existent que si le mot porte un accent, donc la lettre
                // elle-meme ne bouge pas : c'est le dessin qui grandit vers le haut.
                string[] accent = AccentFor(word[i]);
                if (accent == null)
                {
                    continue;
                }

                for (int row = 0; row < AccentHeight; row++)
                {
                    for (int column = 0; column < GlyphWidth; column++)
                    {
                        if (accent[row][column] != '#')
                        {
                            continue;
                        }

                        int y = Padding + GlyphHeight + (AccentHeight - 1 - row);
                        mask[y * width + originX + column] = true;
                    }
                }
            }

            for (int y = 0; y < height; y++)
            {
                for (int x = 0; x < width; x++)
                {
                    int index = y * width + x;

                    if (mask[index])
                    {
                        pixels[index] = ink;
                    }
                    else if (Touches(mask, width, height, x, y))
                    {
                        pixels[index] = outline;
                    }
                }
            }

            return pixels;
        }

        /// <summary>Vrai si une des huit cases voisines porte de l'encre.</summary>
        private static bool Touches(bool[] mask, int width, int height, int x, int y)
        {
            for (int dy = -1; dy <= 1; dy++)
            {
                for (int dx = -1; dx <= 1; dx++)
                {
                    int nx = x + dx;
                    int ny = y + dy;

                    if (nx < 0 || ny < 0 || nx >= width || ny >= height)
                    {
                        continue;
                    }

                    if (mask[ny * width + nx])
                    {
                        return true;
                    }
                }
            }

            return false;
        }

        /// <summary>
        /// La lettre a dessiner. Une lettre accentuee rend le glyphe de sa lettre de base :
        /// l'accent lui-meme est dessine a part, par AccentFor. L'espace et tout caractere
        /// inconnu ne dessinent rien.
        /// </summary>
        private static string[] GlyphFor(char character)
        {
            switch (character)
            {
                case '\'': return Apostrophe;
                case '-': return Hyphen;
                case '!': return Exclamation;
                case '?': return Question;
                case 'Ç':
                case 'ç': return Cedilla;
            }

            if (character >= '0' && character <= '9')
            {
                return DigitGlyphs[character - '0'];
            }

            char upper = char.ToUpperInvariant(BaseLetter(character));

            if (upper < 'A' || upper > 'Z')
            {
                return null;
            }

            return Glyphs[upper - 'A'];
        }

        /// <summary>La lettre sans son accent, ou le caractere tel quel s'il n'en porte pas.</summary>
        private static char BaseLetter(char character)
        {
            switch (character)
            {
                case 'É':
                case 'È':
                case 'Ê':
                case 'é':
                case 'è':
                case 'ê':
                    return 'E';
                case 'À':
                case 'à':
                    return 'A';
                case 'Ô':
                case 'ô':
                    return 'O';
                case 'Î':
                case 'î':
                    return 'I';
                case 'Û':
                case 'û':
                case 'Ù':
                case 'ù':
                    return 'U';
                default:
                    return character;
            }
        }

        /// <summary>Les deux rangees de l'accent d'un caractere, ou null s'il n'en porte pas.</summary>
        private static string[] AccentFor(char character)
        {
            switch (character)
            {
                case 'É':
                case 'é':
                    return Acute;
                case 'È':
                case 'è':
                case 'À':
                case 'à':
                    return Grave;
                case 'Ê':
                case 'ê':
                case 'Ô':
                case 'ô':
                case 'Î':
                case 'î':
                case 'Û':
                case 'û':
                    return Circumflex;
                case 'Ù':
                case 'ù':
                    return Grave;
                default:
                    return null;
            }
        }
    }
}
