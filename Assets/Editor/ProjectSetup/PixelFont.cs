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
    /// Les huit noms de villes de la phase 7 sont les premiers a s'en servir. La Fabrique,
    /// en phase 14, aura besoin des memes lettres pour ses noms de panneaux ; les accents
    /// s'ajouteront a ce moment-la, quand ECOLE en aura besoin.
    /// </summary>
    public static class PixelFont
    {
        public const int GlyphWidth = 5;
        public const int GlyphHeight = 7;

        /// <summary>Une colonne vide entre deux lettres.</summary>
        public const int Tracking = 1;

        /// <summary>Le liseré déborde d'un pixel de chaque côté.</summary>
        public const int Padding = 1;

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

        /// <summary>Largeur du dessin d'un mot, liseré compris.</summary>
        public static int WidthOf(string word)
        {
            if (string.IsNullOrEmpty(word))
            {
                return 0;
            }

            return word.Length * (GlyphWidth + Tracking) - Tracking + Padding * 2;
        }

        /// <summary>Hauteur du dessin d'un mot, liseré compris.</summary>
        public static int Height => GlyphHeight + Padding * 2;

        /// <summary>
        /// Dessine un mot en blanc cerné d'un liseré sombre. Le liseré n'est pas une
        /// coquetterie : sans lui, un nom clair posé sur du pavé clair ne se lit plus.
        /// </summary>
        public static Color32[] Render(string word, Color32 ink, Color32 outline)
        {
            int width = WidthOf(word);
            int height = Height;

            Color32[] pixels = new Color32[width * height];
            bool[] mask = new bool[width * height];

            for (int i = 0; i < word.Length; i++)
            {
                string[] glyph = GlyphFor(word[i]);
                if (glyph == null)
                {
                    continue;
                }

                int originX = Padding + i * (GlyphWidth + Tracking);

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

        /// <summary>L'espace et tout caractere inconnu ne dessinent rien.</summary>
        private static string[] GlyphFor(char character)
        {
            char upper = char.ToUpperInvariant(character);

            if (upper < 'A' || upper > 'Z')
            {
                return null;
            }

            return Glyphs[upper - 'A'];
        }
    }
}
