# Phase 17 — L'habillage

Le pixel art final, prévu depuis le 3 septembre 2026 comme la toute dernière phase : le gameplay
est validé de la phase 1 à la phase 16, l'art vient maintenant, une seule fois (règle 4).

**Art original, dessiné par code**, décidé le 5 septembre 2026. Aucun fichier externe, aucun asset
Nintendo ou Pokémon — la ROM proposée n'a pas été ouverte et ne le sera pas, CLAUDE.md l'interdit.
L'inspiration de style — RPG top-down de console portable du début des années 2000, vue 3/4,
palette limitée, contours nets — se prend sans copier un pixel.

## 0. L'état des lieux

- **486 PNG** sortent de `PlaceholderArtGenerator` : 198 tuiles, 232 sprites, 56 pictos, par
  **41 fonctions de dessin** faites d'appels `Fill` — des rectangles empilés. Lisibles dans le
  code seulement en les rejouant de tête.
- **88 couleurs distinctes**, choisies une par une au fil des phases, sans nom ni table. Le jeu
  n'a pas de palette : il a des couleurs.
- La liste des placeholders de PROGRESS.md fait **une trentaine d'entrées**, de la maison sans
  toit à la police trop grasse, en passant par les trois bâtiments sans enseigne, les cinq
  personnages qui ne se distinguent que par la couleur, et les pièces vides.

C'est trop pour une phase d'un seul tenant. Comme la phase 12, elle se coupe en sous-phases,
chacune finissant sur une planche regardée, un résumé et ta validation (règle 1).

## 1. La méthode, avant le premier pixel

### 1.1 Une palette nommée, et un validateur qui la fait respecter

Une classe `Palette` — une seule — porte **toutes** les couleurs du jeu, chacune **nommée** par ce
qu'elle est (`GrassLight`, `RoadDust`, `SignRed`, `Ink`), pas par sa valeur. Le générateur ne
connaît plus aucun `new Color32(0x..)` en dehors d'elle.

Et un filet neuf, `ValidatePalette`, rejoué à chaque génération : **chaque pixel de chaque image
sortie est une couleur de la palette ou transparent**. Sabotage : une couleur écrite en dur dans
un dessin → refus nommant l'image, la position et la couleur. C'est ce qui rend la cohérence
**vérifiable** et non seulement souhaitée.

### 1.2 Les sprites en ASCII, une lettre par couleur

Comme les glyphes de `PixelFont` et comme les plans du village : chaque sprite s'écrit **ligne par
ligne, une lettre par pixel**, la table lettre → couleur de palette à côté. `Paint(rows, legend)`
remplace les piles de `Fill`. Un sprite devient **lisible dans le fichier**, revu au diff, et le
validateur refuse une lettre absente de la légende ou une ligne de mauvaise longueur — le même
filet que `IsWellFormed` pour les cartes.

Les formes géométriques régulières — les seize masques de route, de haie, de canalisation, les
disques et triangles des panneaux — restent dessinées par code : c'est là que le code est plus
juste qu'une main.

### 1.3 La planche, à chaque sous-phase

Un menu **« Planche de l'art »** rend **toutes** les images agrandies sur une seule feuille, par
famille, avec leur nom. Rien ne se déclare fini sans l'avoir regardée : c'est la leçon des phases
13, 14 et 16, où le code compilait et les validateurs passaient sur des dessins ratés.

## 2. Le découpage proposé

| | sous-phase | ce qui s'y dessine | ce qui se vérifie |
|---|---|---|---|
| **17a** | La palette et la méthode | `Palette`, `Paint`, `ValidatePalette`, la planche. Les 88 couleurs ramenées à la palette. **Aucune forme ne change encore.** | le validateur refuse une couleur hors palette ; les six validateurs ; le jeu identique à l'œil près des teintes |
| **17b** | La surface | herbe, chemin, parc, haies, arbres, **maisons avec toit**, fontaine, bouches, **façades avec toit et enseigne** (un picto par métier : plaque, tuyau, panneau), **un sprite propre à la station**, ses murs et son sol | planche ; captures du village aux quatre saisons ; le contraste des saisons jugé plein écran |
| **17c** | Le sous-sol | terre et galeries aux trois profondeurs — l'écart 1/2 à creuser —, les 48 canalisations, l'échelle **qui déborde vers le haut**, l'arrivée de maison, la cuve, les bassins | planche ; captures ; le gel qui se voit sur le corps du tuyau |
| **17d** | Les personnages | le joueur en quatre directions, puis **cinq silhouettes distinctes** — artisan, ouvrier, Le Stock, La Fabrique, Le Plan — et les huit guides : pas des recolorations, des personnes | planche ; chacun vu dans sa pièce ou à son poste |
| **17e** | L'interface | les pictos d'action (dont « enlever » dans le vocabulaire des panneaux et « parler » en vraie bulle), les gouttes, les quatre saisons, le curseur, les cartes du memory, les fonds des écrans, la **police** (M, B, apostrophe) | planche ; les trois mini-jeux et le HUD en captures |
| **17f** | Les panneaux | les 29 du Code — **le STOP avec ses lettres**, **la croix d'AB1** lisible —, le poteau vide, le dos, les 24 noms redessinés avec la police | planche agrandie huit fois, chaque numéro revérifié sur une source |
| **17g** | Les intérieurs | murs, sol, et **du mobilier** : établi, étagères, râteliers — pour que trois pièces soient des lieux | captures des trois pièces |

L'ordre suit ce que Victorien voit en premier — le village — et finit par ce qui demande la
police et les panneaux, les plus fins. Chaque sous-phase : compilation zéro erreur zéro warning,
les six validateurs plus `ValidatePalette`, une planche regardée, captures, commit
« Phase 17x - … », arrêt, validation.

## 3. Ce que la phase ne fait pas

Aucun gameplay ne change. Aucune image nouvelle sans un placeholder qu'elle remplace, sauf le
mobilier des pièces et les enseignes, déjà listés. Aucun fichier externe. Rien dans
ProjectSettings ; le contraste des saisons se règle dans les `SeasonDefinition`, pas dans les
tuiles, comme PROGRESS.md le demande déjà.

## 4. Les trois points, tranchés le 5 septembre 2026

1. **Une trentaine de couleurs nommées**, une seule classe `Palette`, et `ValidatePalette` qui
   refuse tout pixel hors palette. La cohérence devient vérifiable, pas seulement souhaitée.
2. **L'eau reste immobile**, mieux dessinée : une image par tuile, aucun composant, aucune horloge.
3. **L'ordre du tableau** : 17a palette et méthode, puis surface, sous-sol, personnages,
   interface, panneaux, intérieurs.

Chaque sous-phase s'arrête sur une planche regardée, un résumé et ta validation (règle 1). La
première, 17a, ne change aucune forme : elle installe la palette, `Paint`, le validateur et la
planche, et ramène les 88 couleurs à la trentaine — le jeu doit ressortir identique à l'œil, aux
teintes près, et c'est ce qui prouve que la méthode tient avant qu'on redessine quoi que ce soit.
