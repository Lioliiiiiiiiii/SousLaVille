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
        private static Color32[] BuildWater()
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

        /// <summary>
        /// LES DALLES du parc et LE BETON de la station, regle 1 : un fond, la trame de points de
        /// la pelouse, et deux fissures en L toujours aux memes places — l'equivalent mineral
        /// des touffes. Sans joint : le pavage de la phase 17 dessinait une grille de huit
        /// pixels, et la cour de la station un quadrillage. Une dalle vue de loin est unie.
        /// </summary>
        private static Color32[] BuildSlabTile(Color32 stone, Color32 dot, Color32 crack)
        {
            Color32[] pixels = new Color32[TileSize * TileSize];

            for (int y = 0; y < TileSize; y++)
            {
                for (int x = 0; x < TileSize; x++)
                {
                    bool speck = (x % 8 == 1 && y % 8 == 2) || (x % 8 == 5 && y % 8 == 6);
                    pixels[y * TileSize + x] = speck ? dot : stone;
                }
            }

            // Deux fissures en L, trois pixels chacune.
            Plot(pixels, TileSize, 3, 12, crack);
            Plot(pixels, TileSize, 4, 12, crack);
            Plot(pixels, TileSize, 4, 11, crack);
            Plot(pixels, TileSize, 11, 4, crack);
            Plot(pixels, TileSize, 11, 5, crack);
            Plot(pixels, TileSize, 12, 4, crack);

            return pixels;
        }

        // ---------------------------------------------------------------- la vegetation

        /// <summary>
        /// L'ARBRE de la reference, 16 sur 32. Un tronc court dans la case, et une frondaison qui
        /// prend toute la case du nord : UNE BOULE QUI SE RESSERRE EN BAS — large au milieu, en
        /// dome a trois bosses au sommet, le bas qui se referme autour du tronc. Le flanc droit et
        /// le bas dans l'ombre, un eclat en haut a gauche, et des arcs de feuillage clairs et
        /// sombres. Le tronc se cerne de brun, la cime de vert profond — chaque matiere son trait.
        ///
        /// Trois jets avant celui-ci, tous regardes sur une planche : un ovale debout sortait en
        /// cypres, une capsule a flancs droits en cornichon. C'est le bas qui se resserre et le
        /// modele en croissant qui font lire un arbre dans seize pixels de large.
        ///
        /// Son ombre au sol n'est pas ici : elle est dans la tuile de son pied, BuildTreeBase,
        /// puisque c'est le sol qu'elle assombrit.
        /// </summary>
        private static Color32[] BuildTreeV2()
        {
            const int width = PlayerWidth;
            Color32[] pixels = NewTransparent(width * TreeHeight);

            // Le tronc a trois tons, et son trait avant que la cime ne le recouvre.
            Fill(pixels, width, 6, 9, 1, 9, Palette.Wood);
            Fill(pixels, width, 6, 6, 1, 9, Palette.WoodDark);
            Fill(pixels, width, 9, 9, 2, 9, Palette.Bark);
            Fill(pixels, width, 6, 9, 1, 1, Palette.WoodDark);
            Outline(pixels, width, Palette.WoodDark);

            // La silhouette : la masse, deux lobes au milieu, le bas qui pend, trois bosses.
            FillEllipse(pixels, width, 7.5f, 21.0f, 6.9f, 8.2f, Palette.Leaf);
            FillEllipse(pixels, width, 3.3f, 20.0f, 3.0f, 3.4f, Palette.Leaf);
            FillEllipse(pixels, width, 11.7f, 20.0f, 3.0f, 3.4f, Palette.Leaf);
            FillEllipse(pixels, width, 7.5f, 13.0f, 4.6f, 3.6f, Palette.Leaf);
            FillEllipse(pixels, width, 4.8f, 27.8f, 2.6f, 2.6f, Palette.Leaf);
            FillEllipse(pixels, width, 10.2f, 27.8f, 2.6f, 2.6f, Palette.Leaf);
            FillEllipse(pixels, width, 7.5f, 28.9f, 2.6f, 2.0f, Palette.Leaf);

            // Le modele : tout ce qui sort d'une ellipse decalee vers le haut et la gauche est
            // dans l'ombre — un croissant au bas et au flanc droit — et un petit disque en haut a
            // gauche prend la lumiere.
            for (int y = 8; y < TreeHeight; y++)
            {
                for (int x = 0; x < width; x++)
                {
                    if (!PixelIs(pixels, width, x, y, Palette.Leaf))
                    {
                        continue;
                    }

                    if (!InsideEllipse(x, y, 6.4f, 22.2f, 5.9f, 7.4f))
                    {
                        pixels[y * width + x] = Palette.LeafDark;
                    }
                    else if (InsideEllipse(x, y, 5.0f, 24.5f, 2.5f, 2.9f))
                    {
                        pixels[y * width + x] = Palette.LeafLight;
                    }
                }
            }

            // Les arcs de feuillage : clairs sur le dessus des touffes, sombres en dessous.
            foreach (Vector2Int arc in new[] { new Vector2Int(2, 21), new Vector2Int(8, 26),
                         new Vector2Int(3, 16), new Vector2Int(9, 21), new Vector2Int(6, 15) })
            {
                LeafArc(pixels, width, arc.x, arc.y, light: true);
            }

            foreach (Vector2Int arc in new[] { new Vector2Int(5, 18), new Vector2Int(10, 24),
                         new Vector2Int(1, 19), new Vector2Int(11, 16), new Vector2Int(6, 12) })
            {
                LeafArc(pixels, width, arc.x, arc.y, light: false);
            }

            Outline(pixels, width, Palette.LeafShadow);
            return pixels;
        }

        private static bool PixelIs(Color32[] pixels, int width, int x, int y, Color32 color)
        {
            return IsOpaque(pixels, width, x, y) && Palette.Same(pixels[y * width + x], color);
        }

        private static bool InsideEllipse(int x, int y, float cx, float cy, float rx, float ry)
        {
            float dx = (x - cx) / rx;
            float dy = (y - cy) / ry;
            return dx * dx + dy * dy <= 1f;
        }

        /// <summary>
        /// UN ARC DE FEUILLAGE, trois pixels : clair, c'est le dessus d'une touffe qui prend la
        /// lumiere, en accent circonflexe ouvert ; sombre, c'est son dessous, en coupe. Il ne se
        /// pose que sur du vert de base, pour ne pas mordre l'ombre ni l'eclat.
        /// </summary>
        private static void LeafArc(Color32[] pixels, int width, int x, int y, bool light)
        {
            if (light)
            {
                if (!PixelIs(pixels, width, x, y, Palette.Leaf) || !PixelIs(pixels, width, x + 2, y + 1, Palette.Leaf))
                {
                    return;
                }

                Plot(pixels, width, x, y, Palette.LeafLight);
                Plot(pixels, width, x + 1, y + 1, Palette.LeafLight);
                Plot(pixels, width, x + 2, y + 1, Palette.LeafLight);
            }
            else
            {
                if (!PixelIs(pixels, width, x, y + 1, Palette.Leaf) || !PixelIs(pixels, width, x + 2, y, Palette.Leaf))
                {
                    return;
                }

                Plot(pixels, width, x, y + 1, Palette.LeafDark);
                Plot(pixels, width, x + 1, y, Palette.LeafDark);
                Plot(pixels, width, x + 2, y, Palette.LeafDark);
            }
        }

        /// <summary>
        /// LA TEXTURE DU FEUILLAGE des buissons : dans chaque carre de huit, un arc clair et un
        /// arc sombre, decales d'une demi-periode a chaque rangee de huit. Periode huit, qui
        /// divise seize : le motif se poursuit d'une case de haie a la suivante sans couture.
        /// Rend 0 pour le vert de base, 1 pour un pixel clair, 2 pour un pixel sombre.
        /// </summary>
        private static int LeafTexture(int x, int y)
        {
            int bx = (x + (y / 8) * 4) % 8;
            int by = y % 8;

            if ((bx == 1 && by == 5) || ((bx == 2 || bx == 3) && by == 6)) return 1;
            if ((bx == 5 && by == 2) || ((bx == 6 || bx == 7) && by == 1)) return 2;
            return 0;
        }

        /// <summary>
        /// LE PIED DE L'ARBRE : la pelouse, et l'ombre de la cime dessus — une ellipse de deux verts
        /// plus sombres, regle 5. C'est la tuile bloquante ; le tronc du sprite se pose au milieu.
        /// </summary>
        private static Color32[] BuildTreeBase()
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
                    else
                    {
                        int texture = LeafTexture(x, y);
                        color = texture == 1 ? Palette.LeafLight : texture == 2 ? Palette.LeafDark : Palette.Leaf;
                    }

                    pixels[y * TileSize + x] = color;
                }
            }

            // LE SOMMET FESTONNE : sur un dessus libre, deux pixels sur huit se rognent, et le
            // trait suivra le creux. Une haie a bord droit est un mur peint en vert ; ce sont ces
            // creux qui en font des touffes.
            if (!north)
            {
                for (int x = x0; x <= x1; x++)
                {
                    if (x % 8 == 3 || x % 8 == 4)
                    {
                        pixels[y1 * TileSize + x] = new Color32(0, 0, 0, 0);
                    }
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

        /// <summary>
        /// LA PELOUSE FLEURIE : la tuile de pelouse, et les deux fleurs posees dessus, trait
        /// compris. C'est une tuile de SOL, pas un sprite : elle se peint dans la tilemap du sol a
        /// la place de la pelouse nue, et la phase 18g la fera changer avec la saison.
        /// </summary>
        private static Color32[] BuildFlowersTile()
        {
            Color32[] pixels = BuildLawnTile();
            Color32[] flowers = BuildFlowers();

            for (int i = 0; i < pixels.Length; i++)
            {
                if (flowers[i].a != 0)
                {
                    pixels[i] = flowers[i];
                }
            }

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
        /// LE POTEAU d'un panneau : un fut d'acier a deux tons, un pied, et son trait d'Ink. Le
        /// poteau des vingt-neuf panneaux du Code, du poteau vide et du dos de carte ; les
        /// plaques ne changent pas, elles sont le Code. A dessiner AVANT la plaque : le trait ne
        /// se pose que dans le vide, il ne mordra donc pas ce qui vient ensuite.
        /// </summary>
        private static void DrawSignPost(Color32[] pixels, int width)
        {
            Fill(pixels, width, 7, 8, 1, 13, Palette.Steel);
            Fill(pixels, width, 8, 8, 1, 13, Palette.SteelDark);
            Fill(pixels, width, 6, 9, 1, 1, Palette.SteelDark);
            Outline(pixels, width, Palette.Ink);
        }

        private static Color32[] BuildSignPostV2()
        {
            Color32[] pixels = NewTransparent(PlayerWidth * PlayerHeight);
            DrawSignPost(pixels, PlayerWidth);
            return pixels;
        }

        /// <summary>
        /// LA MAISON DU PLAN, 16 sur 24 : le symbole d'une maison sur la carte du mini-jeu Le
        /// Plan, ou une case fait seize pixels et ou la maison de deux cases sur deux n'a pas sa
        /// place. La meme grammaire en petit : un toit en plan, deux fenetres, une porte, cernee.
        /// </summary>
        private static Color32[] BuildHouseIcon()
        {
            const int w = PlayerWidth;
            Color32[] p = NewTransparent(w * PlayerHeight);

            Fill(p, w, 2, 13, 1, 9, Palette.Bone);
            Fill(p, w, 2, 13, 1, 1, Palette.SteelDark);            // la plinthe
            Fill(p, w, 2, 13, 5, 5, Palette.StoneLight);
            Fill(p, w, 6, 9, 2, 7, Palette.WoodDark);              // la porte
            Fill(p, w, 7, 8, 2, 6, Palette.Wood);
            Fill(p, w, 3, 4, 5, 7, Palette.Ice);                   // deux fenetres
            Fill(p, w, 11, 12, 5, 7, Palette.Ice);
            Fill(p, w, 2, 13, 10, 10, Palette.SteelDark);          // l'avant-toit
            Fill(p, w, 1, 14, 11, 11, Palette.SteelLight);

            Fill(p, w, 1, 14, 12, 22, Palette.Roof);
            Fill(p, w, 1, 2, 12, 21, Palette.RoofLight);
            Fill(p, w, 13, 14, 12, 21, Palette.RoofLight);
            Fill(p, w, 3, 12, 15, 15, Palette.Brick);
            Fill(p, w, 3, 12, 19, 19, Palette.Brick);
            Fill(p, w, 1, 14, 12, 12, Palette.Brick);
            Fill(p, w, 3, 12, 22, 22, Palette.RoofLight);         // le faite
            Plot(p, w, 1, 22, new Color32(0, 0, 0, 0));
            Plot(p, w, 14, 22, new Color32(0, 0, 0, 0));

            Outline(p, w, Palette.Ink);
            return p;
        }

        /// <summary>
        /// UNE CASE DE FACADE des trois batiments, phase 18d, sur le patron des masques de 17b :
        /// pas de voisin au nord, c'est la rangee du TOIT — un plan a planches, ses versants
        /// clairs aux bouts libres, le faite en haut, l'egout dans l'ombre en bas ; sinon le MUR —
        /// bardage clair, une fenetre a carreaux sur son appui, la plinthe, et l'avant-toit qui
        /// le surplombe. Les cotes libres se cernent d'Ink et leurs angles s'arrondissent : le
        /// meme Outline que tout ce qui se dresse, applique aux seuls bords ou le batiment
        /// s'arrete. Meme grammaire que la maison, en plus grand.
        /// </summary>
        private static Color32[] BuildFacadeV2(int mask)
        {
            bool north = (mask & 1) != 0;
            bool east = (mask & 2) != 0;
            bool south = (mask & 4) != 0;
            bool west = (mask & 8) != 0;
            const int w = TileSize;
            Color32[] p = NewTransparent(w * w);
            Color32 clear = new Color32(0, 0, 0, 0);

            if (!north)
            {
                Fill(p, w, 0, 15, 0, 15, Palette.Roof);
                foreach (int plank in new[] { 4, 8, 12 })
                {
                    Fill(p, w, 0, 15, plank, plank, Palette.Brick);
                }

                if (!west)
                {
                    Fill(p, w, 1, 2, 1, 15, Palette.RoofLight);
                    Fill(p, w, 3, 3, 1, 12, Palette.Brick);
                }

                if (!east)
                {
                    Fill(p, w, 13, 14, 1, 15, Palette.RoofLight);
                    Fill(p, w, 12, 12, 1, 12, Palette.Brick);
                }

                Fill(p, w, 0, 15, 13, 14, Palette.RoofLight);       // le faite
                if (south)
                {
                    Fill(p, w, 0, 15, 0, 0, Palette.Brick);         // l'egout, dans l'ombre
                }
            }
            else
            {
                Fill(p, w, 0, 15, 0, 12, Palette.Bone);
                foreach (int line in new[] { 3, 7, 11 })
                {
                    Fill(p, w, 0, 15, line, line, Palette.StoneLight);
                }

                if (!south)
                {
                    Fill(p, w, 0, 15, 1, 1, Palette.SteelDark);     // la plinthe
                }

                DrawWindow(p, w, 4, 5);

                Fill(p, w, 0, 15, 13, 13, Palette.SteelDark);       // l'ombre de l'avant-toit
                Fill(p, w, 0, 15, 14, 15, Palette.SteelLight);      // l'avant-toit
            }

            // Les cotes libres : le bord se vide, l'angle se rogne, et le trait vient s'y poser.
            if (!north) Fill(p, w, 0, 15, 15, 15, clear);
            if (!south) Fill(p, w, 0, 15, 0, 0, clear);
            if (!east) Fill(p, w, 15, 15, 0, 15, clear);
            if (!west) Fill(p, w, 0, 0, 0, 15, clear);
            // Seuls les angles du TOIT s'arrondissent : un batiment repose a plat sur le sol,
            // et sa base a des angles droits, comme celle de la maison.
            if (!north && !west) Plot(p, w, 1, 14, clear);
            if (!north && !east) Plot(p, w, 14, 14, clear);

            Outline(p, w, Palette.Ink);
            return p;
        }

        /// <summary>
        /// UNE ENSEIGNE, phase 18d : un panonceau de bois arrondi a trois tons, et dessus le signe
        /// du metier — zero les plaques, un les tuyaux, deux les panneaux. Cernee, comme tout ce
        /// qui se dresse ; elle se pose sur le mur, au-dessus de la porte.
        /// </summary>
        private static Color32[] BuildSignboardV2(int trade)
        {
            const int w = TileSize;
            Color32[] p = NewTransparent(w * w);

            RoundedBox(p, w, 1, 14, 2, 13, Palette.Wood, Palette.WoodDark, Palette.WoodLight);

            switch (trade)
            {
                case 0:
                    // Une plaque d'egout : un disque et ses deux barres.
                    FillEllipse(p, w, 7.5f, 7.5f, 3.6f, 3.6f, Palette.Steel);
                    Fill(p, w, 6, 9, 6, 6, Palette.Charcoal);
                    Fill(p, w, 6, 9, 9, 9, Palette.Charcoal);
                    break;

                case 1:
                    // Un tuyau vu en bout : un anneau clair et son trou sombre.
                    FillEllipse(p, w, 7.5f, 7.5f, 3.6f, 3.6f, Palette.SteelLight);
                    FillEllipse(p, w, 7.5f, 7.5f, 1.8f, 1.8f, Palette.Charcoal);
                    break;

                default:
                    // Un panneau : le triangle borde de rouge, la forme que Victorien lit en premier.
                    for (int row = 0; row < 7; row++)
                    {
                        int half = 6 - row;
                        Fill(p, w, 8 - half, 7 + half, 4 + row, 4 + row, Palette.SignRed);
                    }

                    for (int row = 0; row < 3; row++)
                    {
                        int half = 3 - row;
                        Fill(p, w, 8 - half, 7 + half, 5 + row, 5 + row, Palette.Paper);
                    }

                    break;
            }

            Outline(p, w, Palette.Ink);
            return p;
        }

        /// <summary>
        /// LE MUR DE L'ENCEINTE de la station, phase 18d : des blocs de beton a trois tons — la
        /// face, le joint dans l'ombre, l'arete du haut de chaque bloc eclairee — decales d'une
        /// assise a l'autre. Plus le bleu de 17b, qui faisait un mur de piscine.
        /// </summary>
        private static Color32[] BuildPlantWallV2()
        {
            const int w = TileSize;
            Color32[] p = new Color32[w * w];

            for (int i = 0; i < p.Length; i++)
            {
                p[i] = Palette.Steel;
            }

            for (int y = 0; y < w; y++)
            {
                int course = y / 4;
                if (y % 4 == 0)
                {
                    Fill(p, w, 0, 15, y, y, Palette.SteelDark);
                    continue;
                }

                if (y % 4 == 3)
                {
                    Fill(p, w, 0, 15, y, y, Palette.SteelLight);
                }

                for (int x = (course % 2) * 4; x < w; x += 8)
                {
                    p[y * w + x] = Palette.SteelDark;
                }
            }

            return p;
        }

        /// <summary>
        /// L'ENTREE DE LA STATION, phase 18d : une grille carree aux coins arrondis, ses barreaux
        /// d'acier sur le noir du puits. Le passage vers le sous-sol se lit comme la bouche
        /// d'egout, et ne se confond plus avec le mur de l'enceinte dont il empruntait l'image.
        /// </summary>
        private static Color32[] BuildPlantInlet()
        {
            const int w = TileSize;
            Color32[] p = NewTransparent(w * w);

            RoundedBox(p, w, 1, 14, 1, 14, Palette.Charcoal, Palette.SteelDark, Palette.Steel);
            foreach (int x in new[] { 4, 7, 10 })
            {
                Fill(p, w, x, x + 1, 3, 12, Palette.Steel);
                Fill(p, w, x, x, 3, 12, Palette.SteelLight);
            }

            Outline(p, w, Palette.Ink);
            return p;
        }

        /// <summary>
        /// UNE CUVE de la station, phase 18d : un bassin rond vu de dessus, le bord d'acier
        /// eclaire vers la lumiere, l'eau dedans et son reflet. Cernee.
        /// </summary>
        private static Color32[] BuildPlantBasinV2()
        {
            const int w = TileSize;
            Color32[] p = NewTransparent(w * w);

            FillEllipse(p, w, 7.5f, 7.5f, 6.6f, 6.6f, Palette.Steel);
            LightRim(p, w, 7.5f, 7.5f, 6.6f, Palette.SteelLight);
            FillEllipse(p, w, 7.5f, 7.5f, 4.6f, 4.6f, Palette.Water);
            Fill(p, w, 5, 6, 9, 9, Palette.Ice);
            Plot(p, w, 5, 8, Palette.Ice);

            Outline(p, w, Palette.Ink);
            return p;
        }

        /// <summary>
        /// Le bord eclaire d'un disque : sa moitie tournee vers la lumiere, en haut a gauche,
        /// passe a la couleur claire, sur une couronne d'un pixel et demi. C'est ce qui fait
        /// lire un rond comme un objet pose et non comme une tache.
        /// </summary>
        private static void LightRim(Color32[] p, int w, float cx, float cy, float radius, Color32 light)
        {
            int h = p.Length / w;
            for (int y = 0; y < h; y++)
            {
                for (int x = 0; x < w; x++)
                {
                    float dx = x - cx;
                    float dy = y - cy;
                    float d = Mathf.Sqrt(dx * dx + dy * dy);
                    if (d <= radius && d > radius - 1.6f && dy - dx > 1.5f)
                    {
                        p[y * w + x] = light;
                    }
                }
            }
        }

        /// <summary>
        /// LA BOUCHE D'EGOUT, phase 18d : le disque sombre de la plaque, un jonc d'acier
        /// eclaire vers la lumiere, deux fentes, et le trait. Ronde, pour se distinguer des
        /// dalles du chemin au premier coup d'oeil — decision de la phase 1, qui tient.
        /// </summary>
        private static Color32[] BuildManholeV2()
        {
            const int w = TileSize;
            Color32[] p = NewTransparent(w * w);

            FillEllipse(p, w, 7.5f, 7.5f, 7.0f, 7.0f, Palette.Steel);
            LightRim(p, w, 7.5f, 7.5f, 7.0f, Palette.SteelLight);
            FillEllipse(p, w, 7.5f, 7.5f, 5.4f, 5.4f, Palette.Charcoal);
            Fill(p, w, 6, 6, 5, 10, Palette.Ink);
            Fill(p, w, 9, 9, 5, 10, Palette.Ink);

            Outline(p, w, Palette.Ink);
            return p;
        }

        /// <summary>
        /// LA PORTE d'un batiment, posee sur le seuil devant sa facade : un encadrement sombre,
        /// un vantail a trois tons, sa poignee, et le trait. On la reconnait de loin, et Espace
        /// dessus fait entrer comme sur une bouche d'egout.
        /// </summary>
        private static Color32[] BuildDoorV2()
        {
            const int w = TileSize;
            Color32[] p = NewTransparent(w * w);

            Fill(p, w, 3, 12, 1, 14, Palette.WoodDark);
            Fill(p, w, 4, 11, 1, 13, Palette.Wood);
            Fill(p, w, 4, 4, 1, 13, Palette.WoodLight);
            Fill(p, w, 4, 11, 13, 13, Palette.WoodLight);
            Fill(p, w, 5, 10, 8, 8, Palette.WoodDark);
            Fill(p, w, 10, 10, 6, 7, Palette.Sun);

            Outline(p, w, Palette.Ink);
            return p;
        }

        /// <summary>
        /// LA FONTAINE du parc, 16 sur 24, phase 18d : un bassin de pierre a trois tons, l'eau
        /// dedans, une colonne, une vasque haute, et le jet qui retombe. Cernee d'Ink.
        /// </summary>
        private static Color32[] BuildFountainSpriteV2()
        {
            const int w = PlayerWidth;
            Color32[] p = NewTransparent(w * PlayerHeight);

            RoundedBox(p, w, 1, 14, 1, 7, Palette.Steel, Palette.SteelDark, null);
            Fill(p, w, 2, 13, 7, 7, Palette.SteelLight);        // la margelle
            Fill(p, w, 3, 12, 3, 6, Palette.Water);
            Fill(p, w, 4, 5, 5, 5, Palette.Ice);

            Fill(p, w, 6, 9, 8, 14, Palette.Steel);               // la colonne
            Fill(p, w, 6, 6, 8, 14, Palette.SteelLight);
            Fill(p, w, 9, 9, 8, 14, Palette.SteelDark);

            Fill(p, w, 4, 11, 15, 17, Palette.Steel);             // la vasque haute
            Fill(p, w, 4, 11, 15, 15, Palette.SteelDark);
            Fill(p, w, 4, 11, 17, 17, Palette.SteelLight);
            Fill(p, w, 5, 10, 16, 16, Palette.Water);

            Fill(p, w, 7, 8, 18, 22, Palette.Ice);                // le jet
            Fill(p, w, 5, 5, 19, 20, Palette.Ice);
            Fill(p, w, 10, 10, 19, 20, Palette.Ice);

            Outline(p, w, Palette.Ink);
            return p;
        }

        /// <summary>
        /// LE PIED DE LA FONTAINE : la dalle du parc, et l'ombre du bassin dessus. La tuile
        /// bloquante ; le sprite se pose par-dessus.
        /// </summary>
        private static Color32[] BuildFountainBaseV2()
        {
            Color32[] p = BuildSlabTile(Palette.SteelLight, Palette.Paper, Palette.Steel);
            FillEllipse(p, TileSize, 7.5f, 4.0f, 7.4f, 3.2f, Palette.Steel);
            return p;
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
