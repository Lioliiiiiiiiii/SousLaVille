# Phase 18 — Le style de la référence

Le 5 septembre 2026, six captures d'un RPG de console portable du début des années 2000 servent de
**référence visuelle** : le jeu doit leur ressembler. C'est exactement le style que CLAUDE.md
nomme depuis la phase 0 — « RPG top-down sur console portable, début des années 2000, vue 3/4,
palette limitée, contours nets » — et la phase 17 ne l'a pas atteint. Elle a rangé les couleurs
et posé des validateurs ; elle n'a pas changé la **manière de dessiner**.

**Les captures sont une feuille de style, pas une banque d'images.** On en tire des proportions,
des trames, des rampes d'ombre, une perspective ; on n'en copie aucun pixel. Aucun asset Nintendo
ou Pokémon n'entre dans le projet, la ROM n'a pas été ouverte et ne le sera pas : CLAUDE.md.

## 0. Le diagnostic, élément par élément

Les captures de la phase 17 en face des références. Ce qui suit est ce qu'on **voit**, pas ce que
les validateurs mesurent — ils passent tous, et le rendu ne convient pas.

| Élément | Phase 17 | Référence |
|---|---|---|
| Herbe | un vert franc, un bruit de touffes sombres | un **vert menthe pâle**, une **trame régulière** de points clairs, quelques touffes |
| Chemins | un ocre à liseré, angles carrés, pointillé central | un **sable clair** piqueté, **chaque coin arrondi**, un bord plus sombre côté herbe, pas de marquage |
| Arbres | un sapin d'une case, sans contour | une **frondaison ronde de deux cases**, cernée de vert sombre, trois verts, tronc court, **ombre au sol** ; les frondaisons se chevauchent en mur de forêt |
| Maisons | une icône de 16 px : toit pointu, une fenêtre | **un toit en plan large** sur la moitié de la hauteur, faces latérales, bandeau d'avant-toit, bardage, deux fenêtres à carreaux, une porte encadrée — quatre cases sur trois au moins |
| Bâtiments | un aplat de briques avec une bande de toit | même grammaire que les maisons, en plus grand |
| Personnages | sans contour, tête d'un tiers, corps carré | **cernés d'un trait sombre**, **tête de la moitié**, cheveux à reflet, ombre sous le menton |
| HUD | une rangée de gouttes grises à nu sur le monde | tout est dans une **boîte blanche arrondie**, bord sombre, filet gris intérieur |
| Saisons | une teinte de lumière | (hors référence) — le sol, les arbres et les toits **changent** |
| Sous-sol | un brun moucheté, des cailloux | (hors référence) — des blocs de roche cernés, à face du dessus claire |
| Intérieurs | un pavage gris, un mur de bois | (hors référence) — un plancher, un bandeau de mur, une plinthe |

Ce tableau dit une chose : la phase 17 a travaillé la **couleur** de chaque élément et pas sa
**forme**. Un arbre sans contour ni volume reste un sapin quelle que soit sa palette.

## 1. La feuille de style

Huit règles, tirées des captures. Tout dessin de la phase 18 les respecte, et la planche de chaque
sous-phase se regarde avec elles en main.

1. **Les sols n'ont pas de contour.** Deux tons et une trame régulière — période 8, un point sur
   deux décalé — plus une touffe ou un grain çà et là. C'est la trame qui fait la matière, pas le
   bruit : la phase 17b avait mis du bruit.
2. **Tout ce qui se dresse porte un contour d'un pixel**, dans le sombre de sa famille : vert
   profond pour les plantes, brun profond pour le bois, gris ardoise pour la pierre, `Ink` pour
   les personnages et l'interface. Un contour sépare l'objet du sol sans le mettre dans une boîte.
3. **Trois tons par matière, plus le contour** : ombre, base, éclat. Jamais un aplat seul.
4. **Vue 3/4.** Un toit est un plan large — la moitié de la hauteur d'un bâtiment — avec ses
   deux versants latéraux plus clairs et son arête. Une face du dessus est plus claire que la face
   avant. L'arête basse est la plus sombre.
5. **Une ombre au sol** sous tout ce qui se dresse : une ellipse de vert sombre sur l'herbe, sous
   l'arbre, sous la maison, sous le buisson. Pas sous le personnage, qui bouge.
6. **Les coins sont arrondis** : coins extérieurs des chemins, boîtes de l'interface, frondaisons,
   bassins. Un angle droit est un angle de tuile ; la référence n'en montre presque aucun.
7. **Un personnage, c'est une tête.** La moitié de la hauteur, les cheveux un tiers de la tête,
   des yeux d'un pixel de large sur deux de haut, une ombre sous le menton, un contour. Le corps
   est petit et simple ; c'est la silhouette de la tête qui dit qui c'est.
8. **La palette est pastel** : des verts menthe, un sable jaune, des gris bleutés, des rouges
   assourdis. Pas de noir pur, pas de blanc pur — `Ink` et `Paper` restent.

## 2. La palette, de 34 à 50 couleurs

La phase 17a avait tranché « une trentaine de couleurs ». La référence demande **des rampes de
quatre** — contour, ombre, base, éclat — pour chaque matière qui se dresse, et une pelouse menthe
qui n'est aucun des trois verts actuels. Seize couleurs s'ajoutent, **par familles nommées**, et
rien ne s'invente hors de la table : `Shade` et `Tint` les connaissent toutes.

- **Pelouse** `LawnLight`, `Lawn`, `LawnDark`, `LawnDeep` — le sol. Menthe pâle.
- **Feuillage** `LeafLight`, `Leaf`, `LeafDark`, `LeafShadow` — arbres et buissons, plus saturé et
  plus sombre que la pelouse pour s'en détacher. `LeafShadow` est le contour des plantes.
- **Sable** `SandLight`, `Sand` — les chemins ; leur bord est `Stone`, déjà là.
- **Toit** `RoofLight`, `Roof` — les maisons ; l'ombre du toit est `Brick`, déjà là.
- **Bois** `WoodLight` — l'éclat qui manquait à la rampe du bois.
- **Chair** `SkinShadow` — le menton et le cou.
- **Saisons** `Rust` pour les feuilles d'automne, `FlowerPink` pour les fleurs du printemps ; la
  neige est `Paper`, son ombre `SteelLight`, la glace `Ice`.

Les trois verts de la phase 17 — `Grass`, `GrassDark`, `GrassDeep` — et le trio du chemin restent
le temps de la transition, puis **la sous-phase 18h retire ce que plus aucune image ne porte**. La
planche de palette dira pour chaque couleur combien d'images l'utilisent : une couleur à zéro est
une couleur qui n'existe plus.

Les six couleurs du gameplay — trois terres, trois galeries — ne bougent pas. Les cinq couleurs de
corps des personnages non plus : `ValidateDistinct` continue de les garder.

## 3. Ce qui change dans le monde, et pas seulement sur les images

Trois changements dépassent le dessin, et c'est pour cela que ce plan précède le premier pixel.

### 3.1 Les arbres font deux cases de haut

Un arbre de la référence est un sprite de **16 sur 32** : le tronc dans sa case, la frondaison
déborde de **toute la case du nord**. C'est ce chevauchement qui fait un mur de forêt quand les
arbres se touchent — et le village en a 126, en bouquets de deux et de trois, dessinés pour cela
dès la phase 12b.

Conséquence : **qui passe devant qui** cesse d'être une question d'ordre fixe. Un joueur au sud
d'un arbre passe devant le tronc ; au nord, il passe derrière la frondaison. Aujourd'hui les
arbres sont triés par rangée et le joueur à un ordre fixe de 10 : dans la moitié nord du village
il disparaît déjà derrière un arbre dont il est devant. Le remède est le **tri par l'axe Y**,
réglage du **Renderer2D** — `m_TransparencySortMode` sur l'axe personnalisé (0, 1, 0), dans
`Assets/Settings/Renderer2D.asset`, **pas dans ProjectSettings**, posé par script comme tout le
reste. Tout ce qui se dresse passe à l'ordre 0 et se trie par sa position : chaque sprite étant
posé au centre de sa case, le tri par Y est le tri par rangée, et le joueur, qui glisse d'une case
à l'autre, bascule au milieu du pas. Les pictos au-dessus des têtes gardent un ordre supérieur.

### 3.2 Les maisons font deux cases sur deux

Une maison de la référence ne tient pas dans une case. La plus petite qui **se lise** comme une
maison — un toit en plan, deux fenêtres, une porte — fait **32 sur 40 pixels** sur une empreinte de
**deux cases de large et deux de profond** : la rangée du mur, la rangée du toit, et le faîte qui
déborde de huit pixels.

La case `A` reste la **case de raccordement** — l'alcôve du sous-sol lui est alignée, rien ne
bouge dessous. Un nouveau marqueur, `a`, dit « corps de la maison » : bloquant, sans
raccordement, et la porte de la maison regarde le sud comme celles des trois bâtiments. Douze
maisons, douze fois trois cases à écrire dans le plan, **par indice de ligne** (PIEGES.md). Sur
les douze, onze ont déjà l'espace libre d'un côté ; **la douzième, en (52, 27), est coincée
entre une rue et un arbre**, et l'arbre recule d'une case.

Le validateur du village vérifie ensuite que chaque `A` a son `a` à l'est ou à l'ouest et deux
`a` au nord, et que le chemin d'accès par la porte au sud existe.

### 3.3 Les saisons changent le sol, pas seulement la lumière

Une teinte de lumière est ce que la phase 5 pouvait faire ; PROGRESS.md note depuis qu'« elle se
voit peu sur l'herbe ». La référence n'a pas de saisons ; un jeu qui en fait sa boucle doit les
**montrer**. Chaque saison a donc ses images :

| | Pelouse | Arbres et buissons | Chemin | Eau | Toits |
|---|---|---|---|---|---|
| Printemps | menthe claire, **des fleurs** posées çà et là | vert tendre | sable | eau | — |
| Été | menthe, touffes | vert plein | sable | eau | — |
| Automne | menthe ternie, **feuilles au sol** | **roux et or** | sable | eau | — |
| Hiver | **neige**, quelques touffes qui percent | **neige sur la cime** | neige tassée | **glace** | **neige** |

La bascule ne coûte rien à la frame : `Tilemap.SwapTile(été, hiver)` remplace toutes les
occurrences d'une tuile en un appel, et un composant `SeasonalSprite` sur chaque arbre, buisson
et maison change son sprite sur `SeasonChanged`. Ni instanciation ni boucle par image. La lumière
reste, adoucie : elle ne porte plus la saison seule.

## 4. Le découpage proposé

Huit sous-phases, chacune finissant sur **une capture regardée**, les validateurs, zéro erreur et
zéro avertissement, un commit préfixé `Phase 18x - `, et **ta validation** avant la suivante
(règle 1).

- **18a — La feuille de style et la planche d'essai.** Ce plan, les seize couleurs, et un bout
  de village dessiné dans le nouveau style — herbe, chemin arrondi, trois arbres, une maison, un
  buisson, un panneau, une flaque, le joueur, un habitant, les deux boîtes du HUD — rendu à quatre
  fois. **C'est cette planche que tu valides**, pas un texte. Aucune image du jeu ne change encore.
- **18b — Les sols.** Pelouse, chemins arrondis à seize masques, dalles du parc, sol de station,
  eau. Le tri par Y.
- **18c — La végétation.** Arbres de deux cases, buissons du labyrinthe, fleurs, les ombres au sol.
- **18d — Les bâtiments.** Maisons 2×2 et le plan qui les reçoit, les trois façades, la station,
  la fontaine, la bouche, les portes, les enseignes, les poteaux des panneaux. Les vingt-neuf faces
  du Code ne changent pas : elles sont le Code.
- **18e — Les personnages.** Le joueur dans ses quatre directions, les six métiers, aux nouvelles
  proportions et cernés.
- **18f — L'interface.** Les boîtes, les gouttes, les pictos de saison, de couche et d'action, le
  cartel, la boîte de dialogue, le fond et les cadres des trois mini-jeux.
- **18g — Les saisons.** Les variantes, `SwapTile`, `SeasonalSprite`, la lumière adoucie.
- **18h — Le sous-sol et les intérieurs**, puis le ménage de la palette.

## 5. Ce que la phase ne fait pas

- **Rien au gameplay.** Le graphe, le solveur, les saisons, les trois mini-jeux : intacts.
- **Rien aux plans**, hors les douze maisons et l'arbre qui recule.
- **`PixelFont` reste à chasse fixe.** L'apostrophe large est notée depuis la phase 17.
- **La profondeur reste comptée en cailloux** — décision de 17c, à rouvrir si tu veux ; le
  sous-sol de 18h la garde et l'habille.
- **Le pointillé central des routes disparaît.** La référence n'en a pas ; ce sont des rues de
  village, et ce sont les panneaux qui disent « route », pas la peinture au sol.

## 6. La vérification, à chaque sous-phase

- Compilation par le pont MCP, **zéro erreur et zéro avertissement**.
- `ValidatePalette`, `ValidateDistinct`, et les six validateurs de scène ; tout filet neuf
  **vérifié par sabotage**, en nommant ce qui est faux et pourquoi.
- Une capture en jeu — pilote recréé pour la phase, `DontDestroyOnLoad`, supprimé à la fin —
  **regardée**, pas seulement produite.
- `git diff ProjectSettings/` vide ; `Application.runInBackground` reposé à chaud à chaque session.
- PROGRESS.md et PIEGES.md à jour, commit, arrêt.

## 7. Les points à trancher

Trois décisions sont les tiennes, et la planche d'essai les montre :

1. **Les arbres : deux cases** (recommandé, § 3.1) ou une case et demie comme aujourd'hui.
2. **Les maisons : deux cases sur deux** (recommandé, § 3.2) ou une cabane d'une case, sans toucher
   au plan.
3. **Les saisons par images** (recommandé, § 3.3) ou par la lumière seule, mieux dosée.

Le reste — la palette menthe, les contours, le tri par Y, les boîtes du HUD, la fin du pointillé —
se voit sur la planche et se discute là.
