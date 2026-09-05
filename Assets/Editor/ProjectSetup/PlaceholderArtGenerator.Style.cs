using UnityEngine;

namespace SousLaVille.EditorTools
{
    /// <summary>
    /// LA FEUILLE DE STYLE DE LA REFERENCE, phase 18. Les primitives et les dessins qui suivent
    /// les huit regles de PLAN-PHASE-18.md : des sols sans contour a trame reguliere, un trait
    /// d'un pixel autour de tout ce qui se dresse, trois tons par matiere, la vue 3/4, une ombre
    /// au sol, des coins arrondis, des personnages qui sont d'abord une tete, une palette pastel.
    ///
    /// Partie du generateur d'art et non classe a part : les dessins de la phase 18 remplacent
    /// ceux des phases 1 a 17 un par un, et ils partagent Fill, FillEllipse, NewTransparent et
    /// la palette. Le fichier principal fait quatre mille cinq cents lignes ; celui-ci porte le
    /// style, et rien d'autre.
    ///
    /// EN ESPACE DE TEXTURE Y MONTE : la ligne 0 est le bas de l'image, les pieds, le tronc.
    /// </summary>
    public static partial class PlaceholderArtGenerator
    {
        // ---------------------------------------------------------------- gabarits

        /// <summary>
        /// Un arbre de la reference fait DEUX CASES de haut : le tronc dans la sienne, la cime
        /// sur toute celle du nord. C'est ce debordement qui fait un mur de foret quand les arbres
        /// se touchent.
        /// </summary>
        private const int TreeHeight = 32;

        /// <summary>Une maison : deux cases de large, deux de profond, et le faite qui deborde de huit pixels.</summary>
        private const int HouseWidth = 32;
        private const int HouseHeight = 40;

        /// <summary>
        /// Un picto du HUD dans sa boite : 24 pixels, et la boite en fait 32. L'emprise ne change
        /// pas d'avec la phase 5, c'est le cadre qui prend la marge.
        /// </summary>
        private const int HudPictoSize = 24;

        // ---------------------------------------------------------------- primitives

        /// <summary>Pose un pixel, ou rien s'il tombe hors de l'image. Fill ne verifie pas ; ici on le fait.</summary>
        private static void Plot(Color32[] pixels, int width, int x, int y, Color32 color)
        {
            int height = pixels.Length / width;
            if (x < 0 || y < 0 || x >= width || y >= height)
            {
                return;
            }

            pixels[y * width + x] = color;
        }

        private static bool IsOpaque(Color32[] pixels, int width, int x, int y)
        {
            int height = pixels.Length / width;
            if (x < 0 || y < 0 || x >= width || y >= height)
            {
                return false;
            }

            return pixels[y * width + x].a != 0;
        }

        /// <summary>
        /// LE CONTOUR, regle 2 : chaque pixel transparent qui touche un pixel plein par un cote
        /// prend la couleur du trait. Le sprite grandit donc d'un pixel de chaque cote ; on dessine
        /// la silhouette en retrait d'un pixel du bord pour lui laisser sa place.
        ///
        /// Le trait ne se pose que dans le VIDE : deux passes successives, une pour le tronc en
        /// brun et une pour la cime en vert, ne se recouvrent pas.
        /// </summary>
        private static void Outline(Color32[] pixels, int width, Color32 ink)
        {
            int height = pixels.Length / width;
            bool[] edge = new bool[pixels.Length];

            for (int y = 0; y < height; y++)
            {
                for (int x = 0; x < width; x++)
                {
                    if (IsOpaque(pixels, width, x, y))
                    {
                        continue;
                    }

                    edge[y * width + x] = IsOpaque(pixels, width, x - 1, y)
                                       || IsOpaque(pixels, width, x + 1, y)
                                       || IsOpaque(pixels, width, x, y - 1)
                                       || IsOpaque(pixels, width, x, y + 1);
                }
            }

            for (int i = 0; i < pixels.Length; i++)
            {
                if (edge[i])
                {
                    pixels[i] = ink;
                }
            }
        }

        /// <summary>
        /// UNE BOITE ARRONDIE, regle 6 : le trait suit le bord, coupe chaque coin de deux pixels,
        /// et un filet plus clair le double a l'interieur. C'est la boite de la reference — blanche,
        /// cernee de sombre, un gris a l'interieur — et c'est la boite de tout le HUD.
        /// </summary>
        private static void RoundedBox(Color32[] pixels, int width, int x0, int x1, int y0, int y1,
            Color32 fill, Color32 edge, Color32? inner)
        {
            for (int y = y0; y <= y1; y++)
            {
                for (int x = x0; x <= x1; x++)
                {
                    int dx = Mathf.Min(x - x0, x1 - x);
                    int dy = Mathf.Min(y - y0, y1 - y);

                    // Les trois pixels de chaque coin restent vides : c'est l'arrondi.
                    if (dx + dy < 2)
                    {
                        continue;
                    }

                    bool onEdge = dx == 0 || dy == 0 || dx + dy == 2;
                    bool onInner = !onEdge && (dx == 1 || dy == 1 || dx + dy == 3);

                    Color32 color = onEdge ? edge : onInner && inner.HasValue ? inner.Value : fill;
                    Plot(pixels, width, x, y, color);
                }
            }
        }

        /// <summary>Une touffe d'herbe : trois pixels en V, la pointe en bas, deux brins qui s'ecartent.</summary>
        private static void Tuft(Color32[] pixels, int width, int x, int y, Color32 color)
        {
            Plot(pixels, width, x + 1, y, color);
            Plot(pixels, width, x, y + 1, color);
            Plot(pixels, width, x + 2, y + 1, color);
        }

        // ---------------------------------------------------------------- les sols

        /// <summary>
        /// LA PELOUSE, regle 1 : un fond menthe et une TRAME REGULIERE de points clairs, periode
        /// huit, un point sur deux decale d'une demi-periode. Deux touffes plus sombres, toujours
        /// aux memes places : la case se repete cent fois sans qu'on lise une grille, et sans le
        /// bruit de la phase 17b, qui faisait de la pelouse un gravier vert.
        /// </summary>
        private static Color32[] BuildLawnTile()
        {
            Color32[] pixels = new Color32[TileSize * TileSize];

            for (int y = 0; y < TileSize; y++)
            {
                for (int x = 0; x < TileSize; x++)
                {
                    bool dot = (x % 8 == 1 && y % 8 == 2) || (x % 8 == 5 && y % 8 == 6);
                    pixels[y * TileSize + x] = dot ? Palette.LawnLight : Palette.Lawn;
                }
            }

            Tuft(pixels, TileSize, 3, 11, Palette.LawnDark);
            Tuft(pixels, TileSize, 10, 3, Palette.LawnDark);

            return pixels;
        }

        /// <summary>
        /// LE CHEMIN DE SABLE a masque de raccord — bit 0 nord, 1 est, 2 sud, 3 ouest, la
        /// convention de tout le decor. La chaussee remplit la case et se borde d'un pixel plus
        /// sombre sur chaque cote libre ; a l'angle de deux cotes libres, LE COIN S'ARRONDIT en
        /// quart de cercle et laisse la pelouse dessous, regle 6. Un bout de chemin est donc une
        /// languette a bout rond, un chemin isole une flaque de sable, et un virage tourne.
        ///
        /// Pas de marquage au sol : ce sont des rues de village, et ce sont les panneaux qui disent
        /// « route », pas la peinture.
        /// </summary>
        private static Color32[] BuildSandTile(int mask)
        {
            bool north = (mask & 1) != 0;
            bool east = (mask & 2) != 0;
            bool south = (mask & 4) != 0;
            bool west = (mask & 8) != 0;
            const int radius = 4;

            // La pelouse dessous : c'est elle qu'on voit dans les coins rognes.
            Color32[] pixels = BuildLawnTile();

            for (int y = 0; y < TileSize; y++)
            {
                for (int x = 0; x < TileSize; x++)
                {
                    float corner = CornerDistance(x, y, north, east, south, west, radius);
                    if (corner > radius - 0.5f)
                    {
                        continue;   // hors du quart de cercle : la pelouse reste
                    }

                    bool onFreeEdge = (!north && y == TileSize - 1) || (!east && x == TileSize - 1)
                                   || (!south && y == 0) || (!west && x == 0);
                    bool rim = onFreeEdge || corner > radius - 1.5f;

                    Color32 color = rim ? Palette.Stone : Palette.Sand;
                    if (!rim && ((x % 8 == 3 && y % 8 == 5) || (x % 8 == 7 && y % 8 == 1)))
                    {
                        color = Palette.SandLight;
                    }

                    pixels[y * TileSize + x] = color;
                }
            }

            return pixels;
        }

        /// <summary>
        /// La distance d'un pixel au centre du quart de cercle du coin ou il se trouve, ou -1 s'il
        /// n'est dans aucun coin forme par deux cotes libres. Les quatre carres de coin ne se
        /// recouvrent pas tant que le rayon ne depasse pas la demi-case.
        /// </summary>
        private static float CornerDistance(int x, int y, bool north, bool east, bool south,
            bool west, int radius)
        {
            int hi = TileSize - radius;
            int lo = radius - 1;

            if (!north && !east && x >= hi && y >= hi) return Length(x - hi, y - hi);
            if (!north && !west && x <= lo && y >= hi) return Length(lo - x, y - hi);
            if (!south && !east && x >= hi && y <= lo) return Length(x - hi, lo - y);
            if (!south && !west && x <= lo && y <= lo) return Length(lo - x, lo - y);

            return -1f;
        }

        private static float Length(int dx, int dy)
        {
            return Mathf.Sqrt(dx * dx + dy * dy);
        }

        /// <summary>
        /// L'EAU de la reference : un bleu qu'on voit au travers, et des vaguelettes claires en
        /// accents circonflexes, deux rangs decales pour que deux cases cote a cote ne fassent pas
        /// une ligne.
        /// </summary>
        private static Color32[] BuildWaterV2()
        {
            Color32 water = Palette.WithAlpha(Palette.Water, 0xB4);
            Color32 ripple = Palette.WithAlpha(Palette.Ice, 0xC8);

            Color32[] pixels = new Color32[TileSize * TileSize];
            for (int i = 0; i < pixels.Length; i++)
            {
                pixels[i] = water;
            }

            foreach (Vector2Int wave in new[] { new Vector2Int(2, 3), new Vector2Int(10, 3),
                         new Vector2Int(6, 11), new Vector2Int(14, 11) })
            {
                Plot(pixels, TileSize, wave.x, wave.y, ripple);
                Plot(pixels, TileSize, wave.x + 1, wave.y + 1, ripple);
                Plot(pixels, TileSize, wave.x + 2, wave.y + 1, ripple);
                Plot(pixels, TileSize, wave.x + 3, wave.y, ripple);
                // La derniere vague reprend a gauche : la case se raccorde a sa voisine.
                Plot(pixels, TileSize, wave.x + 3 - TileSize, wave.y, ripple);
            }

            return pixels;
        }

        // ---------------------------------------------------------------- la vegetation

        /// <summary>
        /// L'ARBRE de la reference, 16 sur 32. Un tronc court dans la case, et une frondaison ovale
        /// qui prend toute la case du nord : trois bosses au sommet, le bas dans l'ombre, des
        /// eclats en haut a gauche ou vient la lumiere, et des traits de feuillage en travers. Le
        /// tronc se cerne de brun, la cime de vert profond — chaque matiere son trait, regle 2.
        ///
        /// Son ombre au sol n'est pas ici : elle est dans la tuile de son pied, BuildTreeBaseV2,
        /// puisque c'est le sol qu'elle assombrit.
        /// </summary>
        private static Color32[] BuildTreeV2()
        {
            const int width = PlayerWidth;
            Color32[] pixels = NewTransparent(width * TreeHeight);

            // Le tronc, et son trait avant que la cime ne le recouvre.
            Fill(pixels, width, 6, 9, 1, 9, Palette.Wood);
            Fill(pixels, width, 6, 6, 1, 9, Palette.WoodDark);
            Fill(pixels, width, 6, 9, 1, 1, Palette.WoodDark);
            Outline(pixels, width, Palette.WoodDark);

            // La cime : UNE MASSE RONDE, pas un ovale debout. Le premier jet la faisait de
            // quatorze pixels de large sur vingt-deux de haut, et les trois arbres de la planche
            // sortaient en cypres. Ici une boule, deux lobes qui l'elargissent au bas, trois
            // bosses au sommet ; elle laisse huit rangees de tronc a decouvert et s'arrete deux
            // rangees sous le bord.
            FillEllipse(pixels, width, 7.5f, 18.5f, 7.0f, 8.5f, Palette.Leaf);
            FillEllipse(pixels, width, 4f, 14f, 3.8f, 3.4f, Palette.Leaf);
            FillEllipse(pixels, width, 11f, 14f, 3.8f, 3.4f, Palette.Leaf);
            FillEllipse(pixels, width, 4.5f, 25f, 3.2f, 3.2f, Palette.Leaf);
            FillEllipse(pixels, width, 10.5f, 25f, 3.2f, 3.2f, Palette.Leaf);
            FillEllipse(pixels, width, 7.5f, 27f, 3.4f, 2.6f, Palette.Leaf);

            for (int y = 8; y < TreeHeight; y++)
            {
                for (int x = 0; x < width; x++)
                {
                    int index = y * width + x;
                    if (!Palette.Same(pixels[index], Palette.Leaf))
                    {
                        continue;
                    }

                    if (y < 13 || (x >= 12 && y < 20))
                    {
                        // Le bas et le flanc droit dans l'ombre.
                        pixels[index] = Palette.LeafDark;
                    }
                    else if (LeafBlob(x, y))
                    {
                        // Les touffes : claires la ou vient la lumiere, sombres ailleurs.
                        pixels[index] = x <= 9 && y >= 15 ? Palette.LeafLight : Palette.LeafDark;
                    }
                }
            }

            Outline(pixels, width, Palette.LeafShadow);
            return pixels;
        }

        /// <summary>
        /// LA TEXTURE DU FEUILLAGE : des touffes de deux pixels sur deux, en quinconce, periode
        /// huit — qui divise seize, donc le motif se poursuit d'une case de haie a la suivante
        /// sans couture. Le meme motif habille la cime des arbres et les buissons : c'est la
        /// meme matiere.
        /// </summary>
        private static bool LeafBlob(int x, int y)
        {
            int bx = (x + (y / 8) * 4) % 8;
            int by = y % 8;
            return (bx == 2 || bx == 3) && (by == 5 || by == 6)
                || (bx == 6 || bx == 7) && (by == 1 || by == 2);
        }

        /// <summary>
        /// LE PIED DE L'ARBRE : la pelouse, et l'ombre de la cime dessus — une ellipse de deux verts
        /// plus sombres, regle 5. C'est la tuile bloquante ; le tronc du sprite se pose au milieu.
        /// </summary>
        private static Color32[] BuildTreeBaseV2()
        {
            Color32[] pixels = BuildLawnTile();

            FillEllipse(pixels, TileSize, 7.5f, 5.5f, 7.2f, 3.4f, Palette.LawnDark);
            FillEllipse(pixels, TileSize, 7.5f, 5.5f, 5.2f, 2.2f, Palette.LawnDeep);

            return pixels;
        }

        /// <summary>
        /// LE BUISSON a masque de raccord — la haie du labyrinthe. Il pousse vers ses voisins, se
        /// retire d'un pixel sur un cote libre pour son trait, arrondit ses angles libres, et porte
        /// une bande d'ombre au pied et des eclats sur le dessus. Deux cent quarante-deux cases de
        /// haie deviennent une haie.
        /// </summary>
        private static Color32[] BuildBushTile(int mask)
        {
            bool north = (mask & 1) != 0;
            bool east = (mask & 2) != 0;
            bool south = (mask & 4) != 0;
            bool west = (mask & 8) != 0;

            Color32[] pixels = NewTransparent(TileSize * TileSize);

            int x0 = west ? 0 : 1;
            int x1 = east ? TileSize - 1 : TileSize - 2;
            int y0 = south ? 0 : 1;
            int y1 = north ? TileSize - 1 : TileSize - 2;

            for (int y = y0; y <= y1; y++)
            {
                for (int x = x0; x <= x1; x++)
                {
                    int dx = Mathf.Min(west ? 99 : x - x0, east ? 99 : x1 - x);
                    int dy = Mathf.Min(south ? 99 : y - y0, north ? 99 : y1 - y);
                    if (dx + dy < 2)
                    {
                        continue;   // l'angle libre s'arrondit
                    }

                    // Le pied dans l'ombre et le dessus au soleil, SEULEMENT AUX BOUTS DE LA HAIE :
                    // une bande d'ombre par case faisait de la haie une pile de boites rayees.
                    // Entre les deux, la texture du feuillage, qui se poursuit de case en case.
                    Color32 color = Palette.Leaf;
                    if (!south && y < y0 + 3)
                    {
                        color = Palette.LeafDark;
                    }
                    else if (!north && y > y1 - 2)
                    {
                        color = Palette.LeafLight;
                    }
                    else if (LeafBlob(x, y))
                    {
                        color = y % 8 >= 4 ? Palette.LeafLight : Palette.LeafDark;
                    }

                    pixels[y * TileSize + x] = color;
                }
            }

            Outline(pixels, TileSize, Palette.LeafShadow);
            return pixels;
        }

        /// <summary>
        /// DEUX FLEURS sur une case : cinq petales roses autour d'un coeur jaune, une tige et deux
        /// feuilles. Le decor du printemps, pose ca et la sur la pelouse.
        /// </summary>
        private static Color32[] BuildFlowers()
        {
            Color32[] pixels = NewTransparent(TileSize * TileSize);

            DrawFlower(pixels, 4, 9);
            DrawFlower(pixels, 11, 3);

            Outline(pixels, TileSize, Palette.LeafShadow);
            return pixels;
        }

        private static void DrawFlower(Color32[] pixels, int cx, int cy)
        {
            Plot(pixels, TileSize, cx, cy - 2, Palette.LeafDark);
            Plot(pixels, TileSize, cx - 1, cy - 3, Palette.LeafDark);
            Plot(pixels, TileSize, cx + 1, cy - 3, Palette.LeafDark);
            Plot(pixels, TileSize, cx, cy - 3, Palette.LeafDark);

            Fill(pixels, TileSize, cx - 1, cx + 1, cy - 1, cy + 1, Palette.FlowerPink);
            Plot(pixels, TileSize, cx, cy + 2, Palette.FlowerPink);
            Plot(pixels, TileSize, cx - 2, cy, Palette.FlowerPink);
            Plot(pixels, TileSize, cx + 2, cy, Palette.FlowerPink);
            Plot(pixels, TileSize, cx, cy, Palette.Sun);
        }

        // ---------------------------------------------------------------- les batiments

        /// <summary>
        /// LA MAISON de la reference, 32 sur 40 sur deux cases de large et deux de profond. En bas
        /// la rangee du mur : un bardage clair a lignes, deux fenetres a carreaux sur leur appui,
        /// une porte encadree, une plinthe sombre. Au-dessus le bandeau d'avant-toit et son ombre.
        /// Puis LE TOIT, la moitie de la hauteur, regle 4 : un plan a planches, deux versants
        /// lateraux plus clairs, le faite en arete, les angles du haut arrondis. Le tout cerne.
        /// </summary>
        private static Color32[] BuildHouseV2()
        {
            const int w = HouseWidth;
            Color32[] p = NewTransparent(w * HouseHeight);

            // Le mur, y 1..15, en retrait de DEUX pixels de chaque cote : un pour le trait, un
            // pour que l'avant-toit le surplombe.
            Fill(p, w, 2, 29, 1, 15, Palette.Bone);
            foreach (int line in new[] { 3, 7, 11 })
            {
                Fill(p, w, 2, 29, line, line, Palette.StoneLight);
            }
            Fill(p, w, 2, 29, 1, 1, Palette.SteelDark);   // la plinthe

            // La porte, au milieu, et son seuil.
            Fill(p, w, 13, 18, 2, 12, Palette.WoodDark);
            Fill(p, w, 14, 17, 2, 11, Palette.Wood);
            Fill(p, w, 15, 16, 8, 9, Palette.Ice);        // le judas
            Plot(p, w, 17, 6, Palette.Sun);              // la poignee
            Fill(p, w, 12, 19, 1, 1, Palette.Stone);       // le seuil

            DrawWindow(p, w, 4, 6);
            DrawWindow(p, w, 20, 6);

            // L'avant-toit : un bandeau clair qui deborde du mur, et son ombre sur le mur.
            Fill(p, w, 2, 29, 16, 16, Palette.SteelDark);
            Fill(p, w, 1, 30, 17, 18, Palette.SteelLight);

            // Le toit, y 19..38 : les deux versants clairs, le plan a planches, le faite.
            Fill(p, w, 1, 30, 19, 38, Palette.Roof);
            Fill(p, w, 1, 3, 19, 37, Palette.RoofLight);
            Fill(p, w, 28, 30, 19, 37, Palette.RoofLight);
            Fill(p, w, 4, 4, 19, 36, Palette.Brick);
            Fill(p, w, 27, 27, 19, 36, Palette.Brick);
            foreach (int plank in new[] { 22, 26, 30, 34 })
            {
                Fill(p, w, 5, 26, plank, plank, Palette.Brick);
            }
            Fill(p, w, 4, 27, 37, 38, Palette.RoofLight);  // le faite
            Fill(p, w, 1, 30, 19, 19, Palette.Brick);      // l'egout du toit, dans l'ombre

            // Les angles du haut, rognes d'un pixel : le trait les arrondira.
            Plot(p, w, 1, 38, new Color32(0, 0, 0, 0));
            Plot(p, w, 30, 38, new Color32(0, 0, 0, 0));

            Outline(p, w, Palette.Ink);
            return p;
        }

        /// <summary>Une fenetre a deux carreaux, huit sur six, son cadre, son reflet et son appui.</summary>
        private static void DrawWindow(Color32[] p, int w, int x, int y)
        {
            Fill(p, w, x, x + 7, y, y + 5, Palette.SteelDark);
            Fill(p, w, x + 1, x + 6, y + 1, y + 4, Palette.Ice);
            Fill(p, w, x + 3, x + 4, y + 1, y + 4, Palette.SteelDark);   // le meneau
            Plot(p, w, x + 1, y + 4, Palette.Paper);
            Plot(p, w, x + 5, y + 4, Palette.Paper);
            Fill(p, w, x - 1, x + 8, y - 1, y - 1, Palette.SteelLight); // l'appui
        }

        /// <summary>
        /// LE POTEAU d'un panneau : un fut d'acier a deux tons, un pied, et son trait. Le poteau
        /// des vingt-neuf panneaux du Code ; les plaques ne changent pas, elles sont le Code.
        /// </summary>
        private static Color32[] BuildSignPostV2()
        {
            Color32[] pixels = NewTransparent(PlayerWidth * PlayerHeight);

            Fill(pixels, PlayerWidth, 7, 8, 1, 12, Palette.Steel);
            Fill(pixels, PlayerWidth, 8, 8, 1, 12, Palette.SteelDark);
            Fill(pixels, PlayerWidth, 6, 9, 1, 1, Palette.SteelDark);

            Outline(pixels, PlayerWidth, Palette.Ink);
            return pixels;
        }

        // ---------------------------------------------------------------- les personnages

        /// <summary>
        /// UN PERSONNAGE de la reference, regle 7 : la tete fait la moitie des vingt-quatre pixels,
        /// les cheveux un tiers de la tete avec une meche plus claire, des yeux d'un pixel sur deux,
        /// une ombre sous le menton ; un petit corps, deux bras, deux jambes, des chaussures. Le
        /// tout dessine en retrait d'un pixel, puis cerne d'Ink.
        ///
        /// Le corps est le meme pour tous ; c'est la couleur du vetement, le couvre-chef et ce qu'il
        /// porte qui font les sept silhouettes de la phase 17d — a reprendre ici en 18e.
        /// </summary>
        private static void DrawFigure(Color32[] p, Color32 body, Color32 trousers, Color32 hair,
            Color32 hairLight, Vector2Int facing)
        {
            const int w = PlayerWidth;

            // Chaussures et jambes.
            Fill(p, w, 4, 6, 1, 2, Palette.Charcoal);
            Fill(p, w, 9, 11, 1, 2, Palette.Charcoal);
            Fill(p, w, 4, 6, 3, 5, trousers);
            Fill(p, w, 9, 11, 3, 5, trousers);

            // Le torse, son ombre en bas, les bras et les mains.
            Fill(p, w, 3, 12, 6, 12, body);
            Fill(p, w, 3, 12, 6, 6, Palette.Shade(body));
            Fill(p, w, 2, 2, 8, 11, body);
            Fill(p, w, 13, 13, 8, 11, body);
            Plot(p, w, 2, 7, Palette.Skin);
            Plot(p, w, 13, 7, Palette.Skin);

            // La tete : le visage, les joues qui debordent, le menton dans l'ombre.
            Fill(p, w, 3, 12, 13, 17, Palette.Skin);
            Fill(p, w, 2, 13, 14, 17, Palette.Skin);
            Fill(p, w, 4, 11, 13, 13, Palette.SkinShadow);

            // Les cheveux : la calotte, et la frange qui descend sur le front.
            Fill(p, w, 2, 13, 18, 21, hair);
            Fill(p, w, 3, 12, 22, 22, hair);
            Fill(p, w, 4, 6, 21, 21, hairLight);
            Plot(p, w, 5, 22, hairLight);

            if (facing == Vector2Int.up)
            {
                // De dos : la nuque, et la meche claire seulement.
                Fill(p, w, 2, 13, 14, 17, hair);
                Fill(p, w, 3, 12, 13, 13, hair);
                return;
            }

            Plot(p, w, 3, 17, hair);
            Plot(p, w, 12, 17, hair);

            if (facing == Vector2Int.left)
            {
                Fill(p, w, 4, 4, 14, 15, Palette.Ink);
                Fill(p, w, 9, 13, 17, 17, hair);
            }
            else if (facing == Vector2Int.right)
            {
                Fill(p, w, 11, 11, 14, 15, Palette.Ink);
                Fill(p, w, 2, 6, 17, 17, hair);
            }
            else
            {
                Fill(p, w, 5, 5, 14, 15, Palette.Ink);
                Fill(p, w, 10, 10, 14, 15, Palette.Ink);
                Plot(p, w, 8, 17, hair);
            }
        }

        private static Color32[] BuildPlayerV2(Vector2Int facing)
        {
            Color32[] pixels = NewTransparent(PlayerWidth * PlayerHeight);
            DrawFigure(pixels, Palette.Orange, Palette.BlueDeep, Palette.WoodDark, Palette.Bark, facing);
            Outline(pixels, PlayerWidth, Palette.Ink);
            return pixels;
        }

        // ---------------------------------------------------------------- l'interface

        /// <summary>
        /// LA GOUTTE du HUD et des toits : ronde en bas, pointue en haut, un reflet en haut a
        /// gauche, cernee. Pleine, elle est d'eau ; vide, elle est de papier avec un fond d'ombre —
        /// pas grise : une goutte vide est un verre a remplir, pas une goutte en panne.
        /// </summary>
        private static Color32[] BuildDropV2(bool full)
        {
            Color32[] pixels = NewTransparent(TileSize * TileSize);
            int[] halves = { 2, 3, 4, 4, 4, 4, 3, 3, 2, 2, 1, 1, 0 };

            for (int row = 0; row < halves.Length; row++)
            {
                int half = halves[row];
                Fill(pixels, TileSize, 7 - half, 8 + half, 1 + row, 1 + row,
                    full ? Palette.Water : Palette.Paper);
            }

            if (full)
            {
                Fill(pixels, TileSize, 5, 5, 6, 8, Palette.Ice);
                Plot(pixels, TileSize, 6, 9, Palette.Ice);
                Fill(pixels, TileSize, 5, 5, 8, 8, Palette.Paper);
                Fill(pixels, TileSize, 4, 10, 2, 3, Palette.BlueDeep);
            }
            else
            {
                Fill(pixels, TileSize, 5, 10, 2, 3, Palette.SteelLight);
                Fill(pixels, TileSize, 9, 10, 4, 6, Palette.SteelLight);
            }

            Outline(pixels, TileSize, Palette.Ink);
            return pixels;
        }

        /// <summary>La boite du HUD, aux dimensions demandees : papier, trait sombre, filet gris.</summary>
        private static Color32[] BuildBanner(int width, int height)
        {
            Color32[] pixels = NewTransparent(width * height);
            RoundedBox(pixels, width, 0, width - 1, 0, height - 1, Palette.Paper, Palette.Ink,
                Palette.SteelLight);
            return pixels;
        }

        /// <summary>
        /// Le fond d'un picto du HUD : une plaque de couleur aux coins arrondis, cernee. La couleur
        /// dit la saison ou la couche avant que le motif ne le dise, decision de la phase 5.
        /// </summary>
        private static Color32[] PictoPlate(Color32 background)
        {
            Color32[] pixels = NewTransparent(HudPictoSize * HudPictoSize);
            RoundedBox(pixels, HudPictoSize, 0, HudPictoSize - 1, 0, HudPictoSize - 1, background,
                Palette.Ink, null);
            return pixels;
        }

        /// <summary>Le repere de surface : un soleil et ses huit rayons sur un ciel.</summary>
        private static Color32[] BuildSunPictoV2()
        {
            Color32[] p = PictoPlate(Palette.SignBlue);
            const int w = HudPictoSize;

            // Le disque est centre entre les pixels 11 et 12 ; les rayons partent de ce couple.
            FillEllipse(p, w, 11.5f, 11.5f, 5.2f, 5.2f, Palette.Sun);
            FillEllipse(p, w, 10.5f, 12.5f, 2.2f, 2.2f, Palette.Paper);

            for (int step = 7; step <= 9; step++)
            {
                Fill(p, w, 11, 12, 12 + step, 12 + step, Palette.Sun);   // nord
                Fill(p, w, 11, 12, 11 - step, 11 - step, Palette.Sun);   // sud
                Fill(p, w, 12 + step, 12 + step, 11, 12, Palette.Sun);   // est
                Fill(p, w, 11 - step, 11 - step, 11, 12, Palette.Sun);   // ouest
            }

            for (int step = 5; step <= 7; step++)
            {
                Plot(p, w, 12 + step, 12 + step, Palette.Sun);
                Plot(p, w, 11 - step, 12 + step, Palette.Sun);
                Plot(p, w, 12 + step, 11 - step, Palette.Sun);
                Plot(p, w, 11 - step, 11 - step, Palette.Sun);
            }

            return p;
        }

        /// <summary>Le printemps : une fleur sur un fond de pelouse.</summary>
        private static Color32[] BuildSpringPictoV2()
        {
            Color32[] p = PictoPlate(Palette.Lawn);
            const int w = HudPictoSize;

            Fill(p, w, 11, 12, 3, 9, Palette.LeafDark);
            Fill(p, w, 7, 10, 5, 6, Palette.Leaf);
            Fill(p, w, 13, 16, 7, 8, Palette.Leaf);

            FillEllipse(p, w, 11.5f, 14.5f, 5.6f, 5.6f, Palette.FlowerPink);
            foreach (Vector2Int notch in new[] { new Vector2Int(11, 20), new Vector2Int(12, 20),
                         new Vector2Int(6, 14), new Vector2Int(17, 14), new Vector2Int(8, 10), new Vector2Int(15, 10) })
            {
                Plot(p, w, notch.x, notch.y, Palette.Lawn);
            }
            FillEllipse(p, w, 11.5f, 14.5f, 2.2f, 2.2f, Palette.Sun);

            return p;
        }
    }
}
