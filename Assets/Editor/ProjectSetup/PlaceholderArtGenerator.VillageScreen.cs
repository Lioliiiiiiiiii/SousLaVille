using SousLaVille.Core;
using UnityEngine;

namespace SousLaVille.EditorTools
{
    /// <summary>
    /// LES CINQ IMAGES DE L'ECRAN DE CHOIX, phase 20.
    ///
    /// Partie du generateur d'art et non classe a part : ces dessins partagent Fill,
    /// NewTransparent et la palette avec tout le reste. Ils sont ici et pas dans le fichier
    /// principal parce que celui-ci fait deja quatre mille cinq cents lignes, et parce qu'ils
    /// ne servent qu'a un seul ecran.
    ///
    /// Tous en seize pixels de cote, comme les pictos des phases precedentes. L'ecran les
    /// affiche deux fois plus grand : trente-deux pixels a l'ecran, le plancher de zone
    /// cliquable de CLAUDE.md, et un agrandissement entier qui ne trouble aucun pixel.
    ///
    /// EN ESPACE DE TEXTURE Y MONTE : la ligne 0 est le bas de l'image.
    /// </summary>
    public static partial class PlaceholderArtGenerator
    {
        /// <summary>La vignette d'un village. La meme pour les deux : c'est le numero qui distingue.</summary>
        public const string PictoVillage = PictosFolder + "/picto_village.png";

        /// <summary>La croix du « nouveau village ».</summary>
        public const string PictoNew = PictosFolder + "/picto_new.png";

        /// <summary>La poubelle. Un village qu'on jette.</summary>
        public const string PictoErase = PictosFolder + "/picto_erase.png";

        /// <summary>Le oui de la confirmation d'effacement.</summary>
        public const string PictoYes = PictosFolder + "/picto_yes.png";

        /// <summary>Le non de la confirmation d'effacement. Celui sur lequel le curseur se pose.</summary>
        public const string PictoNo = PictosFolder + "/picto_no.png";

        /// <summary>
        /// TOUTES les images de la phase 20, pictos et noms ecrits. Une seule liste, parce
        /// qu'elle sert a deux choses qui doivent rester d'accord : regler l'importeur, et
        /// verifier que rien ne manque.
        ///
        /// LES NOMS EN FONT PARTIE, et ce n'est pas un detail. Sans passage par
        /// ConfigureImporter, Unity importe une image de mot en sprite sheet et la decoupe :
        /// LoadAssetAtPath rend alors la PREMIERE LETTRE au lieu du mot. C'est exactement ce
        /// qui est arrive au premier essai, ou l'ecran affichait « V » et « N ».
        /// </summary>
        public static readonly string[] VillageScreenTextures = BuildVillageScreenTextures();

        private static string[] BuildVillageScreenTextures()
        {
            string[] pictos = { PictoVillage, PictoNew, PictoErase, PictoYes, PictoNo };
            string[] paths = new string[pictos.Length + SaveSystem.SlotCount + 1];

            pictos.CopyTo(paths, 0);

            for (int slot = 1; slot <= SaveSystem.SlotCount; slot++)
            {
                paths[pictos.Length + slot - 1] = VillageNameTexture(slot);
            }

            paths[paths.Length - 1] = VillageNewTexture;

            return paths;
        }

        /// <summary>Ce qu'on lit sur la ligne de creation. Deux mots, majuscules, sans accent.</summary>
        public const string VillageNewLabel = "NOUVEAU VILLAGE";

        /// <summary>L'image du nom d'un emplacement. Le nom se deduit du numero, comme dans SaveSystem.</summary>
        public static string VillageNameTexture(int slot)
        {
            return PictosFolder + $"/village_name_{slot}.png";
        }

        /// <summary>L'image de « NOUVEAU VILLAGE ».</summary>
        public const string VillageNewTexture = PictosFolder + "/village_name_new.png";

        /// <summary>
        /// Les noms ecrits de l'ecran de choix. Le nombre d'emplacements vient de SaveSystem
        /// et de nulle part ailleurs : passer a trois villages ne doit rien demander ici.
        /// </summary>
        private static void WriteVillageNames()
        {
            for (int slot = 1; slot <= SaveSystem.SlotCount; slot++)
            {
                WriteWord(VillageNameTexture(slot), $"VILLAGE {slot}");
            }

            WriteWord(VillageNewTexture, VillageNewLabel);
        }

        /// <summary>
        /// Deux maisons et une route. Deux et non une : une seule maison dirait « maison »,
        /// et le joueur en relie treize. Deux disent « village ».
        /// </summary>
        private static Color32[] BuildVillagePicto()
        {
            Color32[] pixels = NewTransparent(TileSize * TileSize);

            // EN RETRAIT D'UN PIXEL DU BORD. Outline ne peint que dans le vide : une image
            // opaque jusqu'au bord n'aurait aucun cadre, et la vignette se confondrait avec
            // le fond de l'ecran. Le pixel laisse libre tout autour EST le cadre.
            Fill(pixels, TileSize, 1, 14, 1, 14, Palette.Lawn);

            // La route qui traverse en bas, et son bord sombre.
            Fill(pixels, TileSize, 1, 14, 2, 4, Palette.Stone);
            Fill(pixels, TileSize, 1, 14, 1, 1, Palette.StoneDark);

            // Maison de gauche : corps clair, toit rouge, une porte sombre.
            Fill(pixels, TileSize, 2, 6, 5, 8, Palette.Bone);
            Fill(pixels, TileSize, 2, 6, 9, 11, Palette.Roof);
            Fill(pixels, TileSize, 3, 5, 12, 12, Palette.Roof);
            Fill(pixels, TileSize, 3, 4, 5, 7, Palette.WoodDark);

            // Maison de droite, un peu plus basse : un village n'est pas un alignement.
            Fill(pixels, TileSize, 9, 13, 5, 7, Palette.Bone);
            Fill(pixels, TileSize, 9, 13, 8, 10, Palette.Roof);
            Fill(pixels, TileSize, 10, 12, 11, 11, Palette.Roof);
            Fill(pixels, TileSize, 10, 11, 5, 6, Palette.WoodDark);

            Outline(pixels, TileSize, Palette.Ink);

            return pixels;
        }

        /// <summary>Une croix pleine, centree. Elle ne dit qu'une chose : « un de plus ».</summary>
        private static Color32[] BuildNewPicto()
        {
            Color32[] pixels = NewTransparent(TileSize * TileSize);

            Fill(pixels, TileSize, 2, 13, 6, 9, Palette.Lawn);
            Fill(pixels, TileSize, 6, 9, 2, 13, Palette.Lawn);

            Outline(pixels, TileSize, Palette.Ink);

            return pixels;
        }

        /// <summary>
        /// UNE POUBELLE. Le premier dessin essaye etait la pelle du jeu barree de rouge : elle
        /// disait « creuser interdit », pas « effacer ce village ». Une poubelle ne dit qu'une
        /// chose, et un enfant de six ans la connait avant de savoir lire.
        ///
        /// Le corps s'effile vers le bas, comme un seau : c'est ce qui la distingue d'une boite.
        /// </summary>
        private static Color32[] BuildErasePicto()
        {
            Color32[] pixels = NewTransparent(TileSize * TileSize);

            // Le corps, un pixel plus etroit de chaque cote a mesure qu'on descend.
            for (int y = 1; y <= 10; y++)
            {
                int inset = (10 - y) / 5;
                Fill(pixels, TileSize, 3 + inset, 12 - inset, y, y, Palette.Steel);
            }

            // Trois rainures verticales, qui creusent le seau.
            Fill(pixels, TileSize, 6, 6, 2, 9, Palette.SteelDark);
            Fill(pixels, TileSize, 9, 9, 2, 9, Palette.SteelDark);

            // Le couvercle, plus large que le corps, et sa poignee.
            Fill(pixels, TileSize, 2, 13, 11, 12, Palette.SteelDark);
            Fill(pixels, TileSize, 7, 8, 13, 14, Palette.SteelDark);

            Outline(pixels, TileSize, Palette.Ink);

            return pixels;
        }

        /// <summary>Le crochet du oui, en vert.</summary>
        private static Color32[] BuildYesPicto()
        {
            Color32[] pixels = NewTransparent(TileSize * TileSize);

            // La branche courte qui descend, puis la longue qui remonte.
            for (int i = 0; i < 4; i++)
            {
                Fill(pixels, TileSize, 2 + i, 4 + i, 7 - i, 9 - i, Palette.LeafDark);
            }

            for (int i = 0; i < 8; i++)
            {
                Fill(pixels, TileSize, 5 + i, 7 + i, 4 + i, 6 + i, Palette.LeafDark);
            }

            Outline(pixels, TileSize, Palette.Ink);

            return pixels;
        }

        /// <summary>La croix du non, en rouge. Deux diagonales pleines.</summary>
        private static Color32[] BuildNoPicto()
        {
            Color32[] pixels = NewTransparent(TileSize * TileSize);

            for (int i = 0; i < 11; i++)
            {
                Fill(pixels, TileSize, 2 + i, 3 + i, 2 + i, 3 + i, Palette.SignRed);
                Fill(pixels, TileSize, 2 + i, 3 + i, 13 - i, 14 - i, Palette.SignRed);
            }

            Outline(pixels, TileSize, Palette.Ink);

            return pixels;
        }
    }
}
