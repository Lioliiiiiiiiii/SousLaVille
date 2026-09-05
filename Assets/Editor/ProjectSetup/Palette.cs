using System.Collections.Generic;
using UnityEngine;

namespace SousLaVille.EditorTools
{
    /// <summary>
    /// LA PALETTE DU JEU, phase 17a. Trente-quatre couleurs, et rien d'autre : chaque pixel de
    /// chaque image sortie du generateur porte l'une d'elles, ou est transparent. C'est
    /// `PlaceholderArtGenerator.ValidatePalette` qui l'exige, image par image et pixel par pixel.
    ///
    /// POURQUOI UNE PALETTE, ET POURQUOI SI PEU. Avant cette phase le jeu ne se donnait aucune
    /// regle de couleur : quatre-vingt-huit valeurs choisies une par une au fil de seize phases,
    /// sans nom ni table, dont trois gris pratiquement identiques et quatre jaunes qu'aucun oeil
    /// ne separait. Une palette limitee est le style meme des RPG de console portable du debut
    /// des annees 2000 que CLAUDE.md demande : c'est la contrainte qui fait tenir l'ensemble,
    /// pas le talent de chaque dessin pris a part.
    ///
    /// CHAQUE COULEUR EST NOMMEE PAR CE QU'ELLE EST, jamais par sa valeur. `Stone` est la
    /// couleur du chemin ; le jour ou le chemin change de teinte, il change ici et partout.
    ///
    /// SIX COULEURS SONT IMPOSEES PAR LE GAMEPLAY et ne se fondent avec aucune autre : les
    /// trois profondeurs de terre et les trois de galerie. La regle de profondeur croissante EST
    /// le puzzle selon CLAUDE.md, et elle se lit d'abord a la nuance du sol.
    ///
    /// DEUX AUTRES SONT IMPOSEES PAR LES PERSONNAGES : cinq habitants doivent se reconnaitre de
    /// loin sans un mot — l'artisan, l'ouvrier, et les trois de l'usine a panneaux. Sans le
    /// violet et le sarcelle, deux d'entre eux porteraient la meme couleur.
    /// </summary>
    public static class Palette
    {
        // ------------------------------------------------------------------ neutres

        /// <summary>Le contour de tout. Un brun tres sombre plutot qu'un noir : il ne troue pas l'image.</summary>
        public static readonly Color32 Ink = Rgb(0x2B, 0x1B, 0x14);

        /// <summary>Le fond des ecrans modaux, et les ombres dures.</summary>
        public static readonly Color32 Charcoal = Rgb(0x1C, 0x1A, 0x20);

        /// <summary>Le blanc du jeu. Casse, jamais pur : un blanc pur crie a cote d'une palette limitee.</summary>
        public static readonly Color32 Paper = Rgb(0xF2, 0xF0, 0xEA);

        public static readonly Color32 SteelDark = Rgb(0x5E, 0x66, 0x72);
        public static readonly Color32 Steel = Rgb(0x8C, 0x92, 0x9C);
        public static readonly Color32 SteelLight = Rgb(0xC2, 0xC8, 0xD0);

        // ------------------------------------------------------------------ verts

        public static readonly Color32 GrassDeep = Rgb(0x1F, 0x5C, 0x2E);
        public static readonly Color32 GrassDark = Rgb(0x2C, 0x6E, 0x35);
        public static readonly Color32 Grass = Rgb(0x4E, 0x9A, 0x3E);

        // ------------------------------------------------------------------ bois

        public static readonly Color32 WoodDark = Rgb(0x4A, 0x2E, 0x1E);
        public static readonly Color32 Wood = Rgb(0x7A, 0x55, 0x33);
        public static readonly Color32 Bark = Rgb(0x9A, 0x6E, 0x3A);

        // ------------------------------------------------------------------ pierre et sable

        public static readonly Color32 StoneDark = Rgb(0xA6, 0x8B, 0x59);

        /// <summary>La chaussee. Toutes les rues du village sont de cette couleur.</summary>
        public static readonly Color32 Stone = Rgb(0xC8, 0xA9, 0x6E);

        public static readonly Color32 StoneLight = Rgb(0xD9, 0xC7, 0xA0);
        public static readonly Color32 Bone = Rgb(0xE2, 0xD4, 0xB0);

        // ------------------- les six du gameplay : trois terres, trois galeries -------------

        /// <summary>Profondeur 1, peu profond. Le gel l'atteint.</summary>
        public static readonly Color32 Earth = Rgb(0x6B, 0x4F, 0x38);

        /// <summary>Profondeur 2.</summary>
        public static readonly Color32 EarthMid = Rgb(0x55, 0x40, 0x2D);

        /// <summary>Profondeur 3, le fond. L'eau n'en remonte jamais.</summary>
        public static readonly Color32 EarthDeep = Rgb(0x3E, 0x32, 0x26);

        /// <summary>Le sol d'une galerie a profondeur 1.</summary>
        public static readonly Color32 Tunnel = Rgb(0xC2, 0xB3, 0x93);

        public static readonly Color32 TunnelMid = Rgb(0x9C, 0x91, 0x79);

        /// <summary>Le fond vire au gris froid : la nuance dit la profondeur sans legende.</summary>
        public static readonly Color32 TunnelDeep = Rgb(0x77, 0x80, 0x8A);

        // ------------------------------------------------------------------ bleus

        public static readonly Color32 BlueDeep = Rgb(0x2A, 0x4C, 0x7D);

        /// <summary>Le bleu du Code de la route : obligation, indication.</summary>
        public static readonly Color32 SignBlue = Rgb(0x2E, 0x5F, 0xA8);

        public static readonly Color32 Water = Rgb(0x3A, 0x7C, 0xC8);

        /// <summary>Le bleu tres clair du gel et des reflets.</summary>
        public static readonly Color32 Ice = Rgb(0x9C, 0xD4, 0xF0);

        // ------------------------------------------------------------------ rouges et chairs

        public static readonly Color32 Brick = Rgb(0xA0, 0x44, 0x2B);

        /// <summary>Le rouge du Code de la route : danger, interdiction, priorite.</summary>
        public static readonly Color32 SignRed = Rgb(0xC8, 0x2F, 0x2F);

        public static readonly Color32 Orange = Rgb(0xE0, 0x5A, 0x2B);
        public static readonly Color32 Skin = Rgb(0xF2, 0xA0, 0x7B);

        // ------------------------------------------------------------------ jaunes

        public static readonly Color32 Gold = Rgb(0xC9, 0xA2, 0x27);

        /// <summary>Le jaune du Code de la route : route prioritaire.</summary>
        public static readonly Color32 Sun = Rgb(0xF2, 0xC8, 0x2A);

        // ------------------- les deux des personnages, pour qu'ils se distinguent -----------

        /// <summary>L'artisan des plaques, phase 9a.</summary>
        public static readonly Color32 Teal = Rgb(0x3E, 0x8E, 0x7A);

        /// <summary>La Fabrique, phase 13. Sans elle, elle porterait le bleu de l'ouvrier.</summary>
        public static readonly Color32 Violet = Rgb(0x7A, 0x4E, 0xA8);

        /// <summary>Toutes, dans l'ordre de la declaration. Sert au validateur et a la planche.</summary>
        public static readonly Color32[] All =
        {
            Ink, Charcoal, Paper, SteelDark, Steel, SteelLight,
            GrassDeep, GrassDark, Grass,
            WoodDark, Wood, Bark,
            StoneDark, Stone, StoneLight, Bone,
            Earth, EarthMid, EarthDeep,
            Tunnel, TunnelMid, TunnelDeep,
            BlueDeep, SignBlue, Water, Ice,
            Brick, SignRed, Orange, Skin,
            Gold, Sun,
            Teal, Violet
        };

        /// <summary>Leurs noms, dans le meme ordre. Sert aux messages et a la planche.</summary>
        public static readonly string[] Names =
        {
            "Ink", "Charcoal", "Paper", "SteelDark", "Steel", "SteelLight",
            "GrassDeep", "GrassDark", "Grass",
            "WoodDark", "Wood", "Bark",
            "StoneDark", "Stone", "StoneLight", "Bone",
            "Earth", "EarthMid", "EarthDeep",
            "Tunnel", "TunnelMid", "TunnelDeep",
            "BlueDeep", "SignBlue", "Water", "Ice",
            "Brick", "SignRed", "Orange", "Skin",
            "Gold", "Sun",
            "Teal", "Violet"
        };

        /// <summary>
        /// LA NUANCE PLUS SOMBRE d'une couleur, et c'est UNE AUTRE COULEUR DE LA PALETTE.
        ///
        /// Elle remplace le `Darken(couleur, facteur)` d'avant la phase 17, qui MULTIPLIAIT les
        /// canaux : le resultat n'etait dans aucune table, personne ne l'avait choisi, et il
        /// echappait par construction a tout validateur de palette. Un liseré de tuile, le motif
        /// d'un tuyau, les jambes d'un personnage : tous se calculaient ainsi.
        ///
        /// Refuse en nommant la couleur plutot que d'en inventer une : une couleur sans nuance
        /// declaree est une couleur qu'on n'a pas fini de choisir.
        /// </summary>
        public static Color32 Shade(Color32 color)
        {
            int key = Key(color);

            if (Shades.TryGetValue(key, out Color32 darker))
            {
                return darker;
            }

            Debug.LogError($"[Sous la Ville] La couleur #{key:X6} n'a pas de nuance déclarée dans " +
                           "la palette : ajoute-la à Palette.Shades, ou n'en demande pas la nuance.");
            return Ink;
        }

        /// <summary>
        /// LA NUANCE PLUS CLAIRE, symetrique exacte de Shade : c'est la couleur dont celle-ci
        /// est la nuance sombre. Un eclat sur une dalle, une arete eclairee. Refuse en nommant,
        /// comme Shade : la palette n'invente pas de couleur.
        /// </summary>
        public static Color32 Tint(Color32 color)
        {
            foreach (KeyValuePair<int, Color32> pair in Shades)
            {
                if (Key(pair.Value) == Key(color) && pair.Key != Key(color))
                {
                    return FromKey(pair.Key);
                }
            }

            Debug.LogError($"[Sous la Ville] La couleur {NameOf(color)} n'est la nuance sombre " +
                           "d'aucune autre : elle n'a pas d'éclat déclaré dans la palette.");
            return color;
        }

        private static Color32 FromKey(int key)
        {
            return Rgb((byte)(key >> 16), (byte)((key >> 8) & 0xFF), (byte)(key & 0xFF));
        }

        /// <summary>Vrai si deux couleurs sont la meme, l'alpha mis a part.</summary>
        public static bool Same(Color32 a, Color32 b)
        {
            return Key(a) == Key(b);
        }

        /// <summary>Vrai si ce rouge, ce vert et ce bleu sont ceux d'une couleur de la palette.</summary>
        public static bool Contains(Color32 color)
        {
            return Lookup.Contains(Key(color));
        }

        /// <summary>Le nom d'une couleur de la palette, ou sa valeur en clair si elle n'en est pas.</summary>
        public static string NameOf(Color32 color)
        {
            for (int i = 0; i < All.Length; i++)
            {
                if (Key(All[i]) == Key(color))
                {
                    return Names[i];
                }
            }

            return $"#{Key(color):X6}";
        }

        /// <summary>
        /// La meme couleur, rendue translucide. L'eau et le voile du fondu s'en servent : ce
        /// sont des couleurs de la palette qu'on voit au travers, pas des couleurs de plus.
        /// </summary>
        public static Color32 WithAlpha(Color32 color, byte alpha)
        {
            return new Color32(color.r, color.g, color.b, alpha);
        }

        private static Color32 Rgb(byte r, byte g, byte b)
        {
            return new Color32(r, g, b, 0xFF);
        }

        /// <summary>Le rouge, le vert et le bleu en un seul entier. L'ALPHA NE COMPTE PAS : l'eau
        /// et le voile du fondu sont des couleurs de la palette rendues translucides.</summary>
        private static int Key(Color32 color)
        {
            return (color.r << 16) | (color.g << 8) | color.b;
        }

        private static readonly HashSet<int> Lookup = BuildLookup();

        private static HashSet<int> BuildLookup()
        {
            HashSet<int> set = new HashSet<int>();
            foreach (Color32 color in All)
            {
                set.Add(Key(color));
            }

            return set;
        }

        /// <summary>
        /// La nuance plus sombre de chacune. Chaque couleur en a une, et c'est toujours une
        /// couleur de la palette : les degrades restent dans la table.
        /// </summary>
        private static readonly Dictionary<int, Color32> Shades = BuildShades();

        private static Dictionary<int, Color32> BuildShades()
        {
            return new Dictionary<int, Color32>
            {
                { Key(Paper), SteelLight },
                { Key(SteelLight), Steel },
                { Key(Steel), SteelDark },
                { Key(SteelDark), Charcoal },
                { Key(Charcoal), Charcoal },
                { Key(Ink), Charcoal },

                { Key(Grass), GrassDark },
                { Key(GrassDark), GrassDeep },
                { Key(GrassDeep), Ink },

                { Key(Bark), Wood },
                { Key(Wood), WoodDark },
                { Key(WoodDark), Ink },

                { Key(Bone), StoneLight },
                { Key(StoneLight), Stone },
                { Key(Stone), StoneDark },
                { Key(StoneDark), Wood },

                { Key(Earth), EarthMid },
                { Key(EarthMid), EarthDeep },
                { Key(EarthDeep), Ink },

                { Key(Tunnel), TunnelMid },
                { Key(TunnelMid), TunnelDeep },
                { Key(TunnelDeep), SteelDark },

                { Key(Ice), Water },
                { Key(Water), SignBlue },
                { Key(SignBlue), BlueDeep },
                { Key(BlueDeep), Charcoal },

                { Key(Skin), Orange },
                { Key(Orange), Brick },
                { Key(SignRed), Brick },
                { Key(Brick), WoodDark },

                { Key(Sun), Gold },
                { Key(Gold), Bark },

                { Key(Teal), GrassDeep },
                { Key(Violet), BlueDeep }
            };
        }
    }
}
