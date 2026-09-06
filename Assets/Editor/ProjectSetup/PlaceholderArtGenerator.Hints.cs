using UnityEngine;

namespace SousLaVille.EditorTools
{
    /// <summary>
    /// LA BANDE D'AIDE DES MINI-JEUX, phase 22. Trois pictos de touches et les mots de ce
    /// qu'elles font.
    ///
    /// Pourquoi elle existe. « Je ne comprenais pas qu'il fallait appuyer sur la flèche du bas
    /// pour valider un panneau sur le plan » — et c'est vrai qu'au Plan, une flèche DEPLACE
    /// tant qu'un poteau est vide, et VERIFIE des qu'ils sont tous garnis. La meme touche fait
    /// deux choses selon l'etat, et rien ne le disait. Meme cause a La Fabrique, ou il faut
    /// re-appuyer sur Espace apres une bonne reponse sans que rien ne l'annonce.
    ///
    /// POURQUOI DES MOTS ET PAS DES PICTOS. CLAUDE.md veut le moins de texte possible et le
    /// pictogramme en premier choix. Mais « verifier », « poser », « retourner » et « suite »
    /// ne se dessinent pas sans ambiguite en seize pixels, et Victorien sait lire. La TOUCHE
    /// est donc un picto — c'est elle qu'il faut reconnaitre d'un coup d'oeil sur le clavier —
    /// et l'ACTION est un mot court, en majuscules, comme les noms de panneaux de la phase 13.
    ///
    /// EN ESPACE DE TEXTURE Y MONTE : la ligne 0 est le bas de l'image.
    /// </summary>
    public static partial class PlaceholderArtGenerator
    {
        /// <summary>Les quatre flèches, en croix. Ce que le joueur cherche des yeux sur son clavier.</summary>
        public const string PictoKeyArrows = PictosFolder + "/picto_key_arrows.png";

        /// <summary>La barre d'espace, longue et basse : sa forme suffit à la reconnaître.</summary>
        public const string PictoKeySpace = PictosFolder + "/picto_key_space.png";

        /// <summary>La touche Échap, une touche carrée portant la flèche du retour.</summary>
        public const string PictoKeyEscape = PictosFolder + "/picto_key_escape.png";

        /// <summary>Les trois pictos de touches, pour la génération et les filets.</summary>
        public static readonly string[] HintKeyTextures =
        {
            PictoKeyArrows, PictoKeySpace, PictoKeyEscape
        };

        /// <summary>
        /// Les mots de la bande d'aide. L'ordre est celui de HintWord : il sert d'index, comme
        /// le rang d'un panneau sur la planche depuis la phase 13.
        /// </summary>
        public static readonly string[] HintWords =
        {
            "SORTIR",     // 0, partout : Échap referme
            "CHOISIR",    // 1, les flèches, partout
            "POSER",      // 2, Espace au Plan
            "VERIFIER",   // 3, une flèche au Plan quand tout est garni
            "RETOURNER",  // 4, Espace au Stock
            "VALIDER",    // 5, Espace à La Fabrique
            "SUITE"       // 6, le geste qui passe à la question d'après
        };

        /// <summary>L'image d'un mot d'aide, nommée par son RANG : le mot peut changer, le rang non.</summary>
        public static string HintWordTexture(int index)
        {
            return PictosFolder + $"/hint_word_{index:00}.png";
        }

        /// <summary>Écrit les trois pictos de touches et les mots.</summary>
        private static void WriteHints()
        {
            WriteTexture(PictoKeyArrows, BuildArrowsKeyPicto());
            WriteTexture(PictoKeySpace, BuildSpaceKeyPicto());
            WriteTexture(PictoKeyEscape, BuildEscapeKeyPicto());

            for (int index = 0; index < HintWords.Length; index++)
            {
                WriteWord(HintWordTexture(index), HintWords[index]);
            }
        }

        /// <summary>
        /// Les quatre flèches autour d'un moyeu. Quatre triangles et non une croix pleine :
        /// une croix pleine se confondrait avec le « plus » du nouveau village.
        /// </summary>
        private static Color32[] BuildArrowsKeyPicto()
        {
            Color32[] pixels = NewTransparent(TileSize * TileSize);
            Color32 face = Palette.SteelLight;

            // UNE CROIX DIRECTIONNELLE PLEINE, d'un seul tenant.
            //
            // Trois dessins ont ete essayes avant celui-ci, tous en quatre pointes separees, et
            // tous ont rendu un ANNEAU : en seize pixels, quatre fleches autour d'un centre ont
            // leurs contours a un pixel les uns des autres, et Outline les soude en losange.
            // Le probleme n'etait pas le dessin, c'etait la taille. Une seule forme n'a qu'un
            // seul contour, et une croix directionnelle est la convention que tout le monde
            // lit — y compris un enfant qui a deja tenu une manette.
            Fill(pixels, TileSize, 6, 9, 2, 13, face);
            Fill(pixels, TileSize, 2, 13, 6, 9, face);

            // Le moyeu, d'un ton plus sombre : il donne son relief a la croix et separe les
            // quatre bras a l'oeil.
            Fill(pixels, TileSize, 6, 9, 6, 9, Palette.Steel);

            Outline(pixels, TileSize, Palette.Ink);

            return pixels;
        }

        /// <summary>La barre d'espace : longue, basse, et rien d'autre. C'est sa forme qui la nomme.</summary>
        private static Color32[] BuildSpaceKeyPicto()
        {
            Color32[] pixels = NewTransparent(TileSize * TileSize);

            Fill(pixels, TileSize, 2, 13, 6, 9, Palette.SteelLight);

            // Un creux le long du bord haut : le relief d'une touche vue de trois quarts.
            Fill(pixels, TileSize, 3, 12, 9, 9, Palette.Steel);

            Outline(pixels, TileSize, Palette.Ink);

            return pixels;
        }

        /// <summary>Une touche carrée portant la flèche du retour, pointe à gauche.</summary>
        private static Color32[] BuildEscapeKeyPicto()
        {
            Color32[] pixels = NewTransparent(TileSize * TileSize);

            Fill(pixels, TileSize, 2, 13, 3, 12, Palette.SteelLight);

            // La flèche : une pointe qui s'ouvre vers la gauche, puis le fût.
            // Une pointe qui s'ouvre franchement, 2 puis 4 puis 6 rangs, et le fut qui part
            // APRES elle. Le premier dessin faisait partir le fut sur la colonne la plus large
            // de la pointe : les deux se fondaient en une seule barre, et l'ensemble lisait
            // « T ». Un rang de decalage suffit a rendre la marche visible.
            Fill(pixels, TileSize, 4, 4, 7, 8, Palette.Ink);
            Fill(pixels, TileSize, 5, 5, 6, 9, Palette.Ink);
            Fill(pixels, TileSize, 6, 6, 5, 10, Palette.Ink);
            // Le fût part APRES la pointe, en 7 et non en 6 : partant de la base même de la
            // pointe, l'ensemble lisait « T » et non « flèche ».
            Fill(pixels, TileSize, 7, 11, 7, 8, Palette.Ink);

            Outline(pixels, TileSize, Palette.Ink);

            return pixels;
        }
    }
}
