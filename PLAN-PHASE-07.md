# Plan de la phase 7 — La plaque gravable

Validé le 3 septembre 2026. Voir CLAUDE.md pour les contraintes du projet et PROGRESS.md pour l'état
d'avancement.

## Contexte

Phase 6 committée (`cfdfd08`). Ce qui sert ici :

- **La sauvegarde accepte un champ de plus sans rien casser.** Newtonsoft relit sans broncher
  un fichier auquel il manque un champ : `CurrentVersion` **reste à 1**. C'est le premier
  vrai bénéfice de la phase 6.
- `ManholePortal` est déjà posé sur les trois bouches, aux cases (33, 5), (20, 10) et (8, 19).
- `VillageLayout` sait accueillir un bâtiment neuf : les cinq maisons de la phase 4 y sont
  entrées de la même façon.
- Le patron « composant local qui s'abonne dans `OnEnable` et se réapplique au rallumage »
  est éprouvé depuis `SeasonAmbience`.

**Critère de fin : je choisis une plaque qui me plaît, je désigne une bouche d'égout sur le
plan du village, et cette bouche porte ma plaque. Elle la porte encore demain.**

## Décisions de design validées

1. **Un catalogue de plaques toutes faites**, inspirées des plaques du monde réel : française,
   japonaise, allemande, américaine. Pas de gravure case par case.
2. **Une plaque par bouche.** Les trois bouches du village portent chacune la sienne.
3. **La plaque est purement décorative.** `NodeType.Manhole` continue d'attendre un usage qui
   ait du sens ; cette phase ne touche pas au réseau.
4. **Le choix se fait en deux appuis** : Espace sur une plaque de l'atelier la prend en main et
   ouvre le plan du village ; Espace sur une bouche du plan la lui pose. Rien d'autre.
5. **Chaque plaque porte le nom de sa ville**, écrit sous elle dans l'atelier. Validé le
   3 septembre 2026 : Victorien sait lire, et huit noms de villes sont huit mots à découvrir.
   Ce sont les huit seuls mots du jeu.

## 1. Le catalogue

**Huit plaques, dessinées par code comme le reste de l'art placeholder.** Leur géométrie est
originale : des familles de motifs inspirées de styles régionaux, jamais l'emblème d'une ville
réelle. Un blason municipal est une œuvre à part entière, et CLAUDE.md n'autorise que
l'original ou le CC0.

| Asset | Inspiration | Motif |
|---|---|---|
| `Cover_Paris` | France | gaufrage en losanges dans un anneau |
| `Cover_Tokyo` | Japon | pétales rayonnants autour d'un cœur |
| `Cover_Berlin` | Allemagne | anneaux concentriques |
| `Cover_NewYork` | États-Unis | nid d'abeilles |
| `Cover_Amsterdam` | Pays-Bas | losanges en diagonale |
| `Cover_Londres` | Royaume-Uni | quadrillage bordé d'un bandeau |
| `Cover_Rome` | Italie | rayons partant du centre |
| `Cover_Lisbonne` | Portugal | vague en spirale |

**Une seule image par plaque, en 16x16**, celle-là même qui se posera sur la bouche. Le
catalogue l'affiche agrandie trois fois, soit 48 px de côté : au-dessus des 32 px imposés par
CLAUDE.md, et surtout **ce qu'il choisit est exactement ce qu'il obtient**.

`ManholeCoverDefinition`, un ScriptableObject par plaque, produit par le menu existant
« Créer les ScriptableObjects ». C'est le même patron que `SeasonDefinition`, et **c'est lui
que le catalogue de panneaux de la phase 12 réutilisera**. Il porte la plaque et l'image de
son nom.

### Une police en pixels

Les noms ne peuvent pas être de l'uGUI : une police TTF s'affiche lissée, hors grille, et
jurerait à côté d'un art entièrement en pixels à la résolution 320x180. Les noms sont donc
**dessinés par `PlaceholderArtGenerator`, comme le reste**, avec une police interne de
**5 sur 7 pixels, A à Z plus l'espace**, en blanc cerné d'un liseré sombre pour rester lisible
sur le pavage.

Ce n'est pas du travail spéculatif : **La Fabrique, en phase 14, doit afficher trois noms de
panneaux écrits parmi lesquels choisir.** La police sera là, et les accents s'ajouteront à ce
moment-là, quand `ÉCOLE` en aura besoin.

Conséquence technique : `AMSTERDAM` fait 55 px de large, or l'importeur plafonne aujourd'hui
à 32. **`maxTextureSize` passe à 64.** Aucune image existante ne dépasse 32, donc rien ne
change pour elles : ce plafond ne fait que tronquer, il n'agrandit rien.

## 2. L'atelier

**Un chantier à ciel ouvert dans le village**, pas un bâtiment à intérieur : ni porte, ni
transition, ni scène de plus. Il y entre en marchant, il en sort en marchant.

Une cour pavée de seize cases sur six, entrée dans `VillageLayout` comme les maisons de la
phase 4, **dans la zone d'herbe libre en haut à droite** : elle ne touche ni à la station, ni
aux trois bosquets, ni à aucune maison, et l'herbe alentour la rend accessible de partout.

**Les huit plaques sont posées au sol, en deux rangées de quatre, espacées de quatre cases.**
Cet écart n'est pas décoratif : `AMSTERDAM` mesure 3,4 cases de large, et deux noms voisins se
chevaucheraient à un écart plus faible. Chaque plaque porte **son nom écrit juste en dessous**,
en permanence, comme les cartels d'une vitrine. Aucune logique, aucun mode : huit images de
plus dans le décor.

Les plaques ne bloquent pas le passage, comme les tuyaux depuis la phase 3.

Le geste : **Espace sur la case occupée**, exactement comme on descend par une bouche. Aucun
mode, aucune touche nouvelle. `PlayerInteractor` gagne une sixième action, `ChooseCover`,
juste après le passage dans l'ordre existant.

## 3. Le plan du village

C'est la deuxième moitié du geste, et la phase 2 l'avait justement écartée faute d'objet à
montrer : « une mini-carte serait vide de sens tant que le réseau n'existe pas, et demanderait
une légende ». Elle a maintenant un objet, et elle ne demande toujours pas de légende.

- **Le plan est engendré depuis `VillageLayout`**, une case par pixel, 40x30, affiché quatre
  fois plus grand. Il ne peut donc pas mentir sur le village : il en sort.
- **Les trois bouches y sont marquées, chacune portant la plaque qu'elle a déjà.** Choisir
  devient entièrement visuel : il voit ce qui est posé et ce qu'il va changer.
- **Les flèches déplacent le choix d'une bouche à l'autre, Espace pose.** Le plan se referme
  dans la foulée.
- **Aucune annulation, et aucun piège.** Poser n'est jamais une erreur, puisque reposer autre
  chose se fait du même geste. Il ne peut pas rester coincé dans le plan : Espace en sort
  toujours.
- La station d'épuration n'y figure pas comme une bouche : elle garde son allure.

Pendant que le plan est ouvert, `PlayerController` est éteint, exactement comme pendant un
voyage entre couches. Une flèche ne doit pas faire marcher le personnage et déplacer le
choix en même temps.

## 4. Ce qui porte la plaque

`ManholeCover`, un composant posé sur chacune des trois bouches de la scène Surface. Il
demande sa plaque à l'usine et l'affiche.

**Il s'abonne dans `OnEnable` et se désabonne dans `OnDisable`**, et il réapplique sa plaque à
chaque rallumage : c'est un composant d'une couche de jeu, et le `SceneRouter` éteint la
racine de la couche inactive. Le piège a coûté une correction en phase 1, une en phase 4, et
il a été évité en phase 5 par ce même patron.

## 5. Où vit l'état

`ManholeFactory` vit dans la scène **Surface**, comme `PipeNetwork` vit dans l'Underground.
Une couche éteinte n'est pas déchargée : son état survit, et `SaveSystem` sait déjà aller le
chercher avec `FindObjectsInactive.Include`.

## 6. La sauvegarde

Un champ de plus dans `SaveData`, et rien d'autre :

```
List<SaveCover> covers      cell (la bouche), cover (le rang dans le catalogue)
```

**`CurrentVersion` reste à 1.** Un champ ajouté se relit sans casser les parties existantes ;
seul un champ dont le sens change imposerait de monter la version. Une plaque dont le rang
n'existe plus dans le catalogue est ignorée en silence, et la bouche garde son allure d'usine.

## 7. Fichiers

**Créés** — `Scripts/Buildings/ManholeCoverDefinition.cs`,
`Scripts/Buildings/ManholeFactory.cs`, `Scripts/World/ManholeCover.cs`,
`Scripts/UI/VillageMapScreen.cs`.

L'usine connaît aussi **les cases de ses propres plaques d'exposition** : pas de composant
séparé pour les huit échantillons, l'atelier possède sa vitrine.

**Modifiés** — `PlayerInteractor` (le choix, et l'extinction du personnage pendant le plan),
`SaveData` et `SaveSystem` (le champ `covers`), `ScriptableObjectSetup` (les huit plaques),
`PlaceholderArtGenerator` (les huit plaques, le plan du village, le picto de choix),
`VillageLayout` (la cour de l'atelier), `SurfaceSceneBuilder` (l'atelier, l'usine, les
`ManholeCover`), `PersistentSceneBuilder` (le plan dans le HUD).

**Référence d'assembly, règle 9 :** rien de neuf. `VillageMapScreen` est de l'uGUI, déjà
couvert par `UnityEngine.UI` dans les deux `.asmdef` depuis la phase 2. Aucun type URP n'est
touché.

## Décisions techniques signalées

1. **Le plan est engendré depuis le plan du village**, pas dessiné à part. Deux dessins d'un
   même village finissent toujours par diverger.
2. **Une seule image par plaque, agrandie pour le catalogue.** Deux images, une petite et une
   grande, finiraient par ne plus se ressembler, et il choisirait autre chose que ce qu'il
   obtient.
3. **`ManholeCoverDefinition` est un ScriptableObject** bien qu'il ne porte presque rien.
   C'est le patron du catalogue que la phase 12 reprendra pour les panneaux, et l'établir ici
   coûte trois lignes.
4. **L'atelier est à ciel ouvert.** Un bâtiment avec intérieur demanderait une scène, une
   transition et un mode de plus, pour rien : le décor n'apporte rien que la cour n'apporte
   déjà.
5. **Les noms sont des images, pas du texte uGUI.** Une police TTF sortirait de la grille de
   pixels et demanderait un asset de police ; une police dessinée coûte une table de glyphes et
   sert déjà la phase 14. Le nom s'affiche en permanence sous chaque plaque, jamais dans un
   panneau qui s'ouvre : rien de nouveau à comprendre.
6. **Le nom n'apparaît que dans l'atelier.** Ni sur le plan du village, ni sur la bouche une
   fois posée : à quatre fois la taille d'une case, le plan serait illisible, et une bouche
   d'égout ne porte pas d'étiquette dans la rue.
7. **Risque assumé : huit motifs en 16x16, c'est serré.** Le nid d'abeilles et le gaufrage
   peuvent se ressembler à cette taille. La vérification les compare deux à deux ; si deux se
   confondent, j'en redessine un plutôt que d'en ajouter.
8. **Phase courte et sans risque pour le réseau.** Rien de ce qui est fait ici ne peut couper
   une maison ni bloquer une construction.

## Vérification de fin de phase

1. Les menus, puis construction des scènes.
2. Console relue par le pont MCP : zéro erreur **et** zéro warning.
3. Les huit plaques comparées deux à deux à leur taille réelle, 16x16, capture à l'appui.
   Les huit noms relus à la résolution de référence : lisibles, sans chevauchement.
4. Play depuis Boot :
   - la cour de l'atelier est praticable, les huit plaques au sol ne bloquent pas le passage,
     et **chacune porte son nom lisible en dessous** ;
   - **Espace sur une plaque** ouvre le plan du village, la plaque choisie en main ;
   - le plan montre les trois bouches **aux bonnes cases**, chacune portant sa plaque du
     moment ;
   - **les flèches passent d'une bouche à l'autre**, sans faire bouger le personnage ;
   - **Espace pose**, le plan se referme, et la bouche du village porte la nouvelle plaque ;
   - **les trois bouches peuvent porter trois plaques différentes** ;
   - reposer une autre plaque sur la même bouche la remplace ;
   - descendre puis remonter : les plaques sont toujours là, `OnEnable` fait son travail ;
   - **quitter et relancer** : les trois plaques sont retrouvées ;
   - **une partie de la phase 6, sans champ `covers`, se relit sans erreur** et les bouches
     gardent leur allure d'usine. C'est la compatibilité que la phase 6 promettait ;
   - console **entièrement vide** sur une session complète.
5. Captures : le catalogue de l'atelier, le plan ouvert, et une bouche avant / après.
6. PROGRESS.md à jour, commit `Phase 7 - la plaque gravable`, puis arrêt.
