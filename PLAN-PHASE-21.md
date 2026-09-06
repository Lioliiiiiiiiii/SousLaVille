# Phase 21 — L'eau coule là où les tuyaux se touchent

La règle de profondeur croissante est retirée. Décidé le 6 septembre 2026, sur mesure et non
sur impression.

## 1. Pourquoi, et le chiffre qui a tranché

Le doute portait sur autre chose : « l'eau ne passe pas en reliant certaines maisons ». **Ce
n'était pas cela.** Sur la partie réelle, les quatorze parcours rendaient le même résultat avec
et sans la règle — 2 desservies dans les deux cas. Onze maisons sur treize n'avaient aucun tuyau
posé. Aucune n'était raccordée jusqu'au bout et refusée.

Mais la mesure faite ensuite sur toute la carte, en supposant tout creusable, a tranché
autrement :

| Maison | Route naturelle | Avec la règle | Facteur |
|---|---|---|---|
| (27,35) | 31 cases | 113 | ×3,6 |
| (43,25) | 37 | 113 | ×3,1 |
| (28,14) | 33 | 109 | ×3,3 |
| (13,17) | 15 | 47 | ×3,1 |
| (34,4) | 49 | 121 | ×2,5 |
| (58,41) | 68 | 144 | ×2,1 |

**La règle ne rendait aucune maison impossible** — zéro sur quatorze. Elle multipliait la route
par 2 à 3,6. Chaque case étant un creusement ET une pose, relier une maison passait de ~70
appuis à ~220, sur un tracé dont la forme n'a rien d'intuitif. C'est cela qui l'a emportée.

Un second chiffre, sur la seule route commencée du joueur, en (13,17) : l'eau parcourait
**4 cases avec la règle, 9 sans**. La règle mordait bien, mais sur une route inachevée.

## 2. Ce qui change

- **`FlowSolver`** : une ligne. `next.Depth < current.Depth` disparaît du parcours. Un segment
  transporte s'il tient, s'il n'est pas gelé et s'il n'est pas bouché.
- **Le gel et les bouchons ne regardent plus la profondeur.** Ils frappaient la profondeur 1,
  la même frontière que le puzzle. Ils frappent maintenant **au hasard, n'importe où** :
  `freezeChance` remplace `freezeMaxDepth` sur `SeasonDefinition`.
  - **Hiver : 0,20.** Un tuyau sur cinq. Avant, l'hiver gelait TOUTE la profondeur 1 sans
    tirage : c'était une certitude, c'est devenu une proportion.
  - **Automne : 0,20**, contre 0,25 avant. La valeur ne frappait que la profondeur 1 ; la
    garder telle quelle aurait rendu l'automne bien plus dur qu'avant, sans que personne ne
    l'ait demandé.
- **La boucle du jeu ne bouge pas.** La résistance du type de tuyau s'applique après le tirage
  de la saison : l'isolé ne gèle jamais, le grillagé ne se bouche jamais. C'est toujours la
  saison qui vient qui décide du tuyau qu'on pose — et cela compte désormais **partout**, plus
  seulement sur les tuyaux peu profonds.
- **Le bassin ne change pas.** Il reste un tampon qui doit lui-même rejoindre la station, et non
  une seconde destination pour les maisons. Tranché le 6 septembre 2026.
- **Rien ne change visuellement.** Les trois nuances de terre restent. Elles ne décident plus de
  rien ; elles font la beauté du sous-sol, et c'était la demande.

## 3. Ce que le jeu dit maintenant

Trois phrases devenaient fausses.

- Guide 3 : « L'HIVER GELE LES PEU PROFONDS » → **« L'HIVER EN GELE D'AUTRES »**, et
  « L'AUTOMNE BOUCHE LES TUYAUX » → « L'AUTOMNE BOUCHE DES TUYAUX ».
- Guide 7, dont la leçon ENTIÈRE était la profondeur : « L'EAU NE REMONTE JAMAIS » et
  « CREUSE TOUJOURS PLUS PROFOND » → **« TU CHOISIS LA ROUTE »** et
  **« AUCUN TROU DANS LE TUYAU »**. « CHERCHE LE PASSAGE » reste : c'est le labyrinthe, et il
  n'a pas changé.
- `GuidePost.Lesson.Depth` devient `Lesson.Route`. **Sa condition n'a pas bougé d'une ligne** :
  une route inachevée laisse la même eau morte qu'une route qui remontait. Seul ce qu'il en dit
  a changé.

## 4. Les filets

- **La preuve directe** : relier la maison (13,17) à la station par le plus court chemin
  naturel, celui que l'ancienne règle refusait, et vérifier que l'eau arrive.
- **Les saisons** : un hiver et un automne complets, en comptant les gelés et les bouchés par
  profondeur. Ils doivent être répartis sur les trois profondeurs, à ~20 %.
- **La boucle** : une ligne d'isolé et une ligne de grillagé, une année complète. L'isolé ne
  doit pas geler, le grillagé ne doit pas se boucher.

## 5. Vérification en jeu

Sur le build publié, puisque c'est là que Victorien joue : relier une maison par la route qui
paraît évidente, et voir l'eau arriver.

## 6. Ce que ce plan ne tranche pas

- **La difficulté qui reste est-elle suffisante ?** Le labyrinthe, la distance et le choix du
  tuyau. Ça ne se décide qu'en le regardant jouer.
- **0,20 pour les deux saisons.** Valeur choisie faute d'indication, comme le 0,25 de la phase
  9b avant elle. Sur un réseau de 60 segments, cela fait une douzaine de tuyaux à reprendre par
  saison. À juger en jouant ; c'est un champ sérialisé sur chaque saison.
