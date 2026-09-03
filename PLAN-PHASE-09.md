# Plan de la phase 9 — Les bâtiments et l'usine à tuyaux

Design validé le 3 septembre 2026, **choix techniques des intérieurs fixés le même jour**, voir
la section « Choix techniques fixés ». La phase est coupée en deux commits, 9a puis 9b. Voir
CLAUDE.md pour les contraintes du projet et PROGRESS.md pour l'état d'avancement.

## Contexte

Phase 8 committée (`5679574`). Ce qui sert ici :

- `PipeType` existe depuis la phase 3, avec `FrostResistance` et `WearPerSeason`, et le gel de
  la phase 5 lit déjà la résistance : **0 gèle dès le premier hiver, 1 ne gèle jamais**. Un
  seul type, `Standard`, à 0. Le code du gel n'a pas à changer.
- Le voyage entre couches, phase 2 : `SceneRouter.TravelAsync`, fondu au noir, bascule de
  racine, callback écran noir où le personnage se replace. **C'est exactement le geste voulu
  pour entrer dans un bâtiment.**
- L'atelier des plaques, phase 7 : catalogue en ScriptableObjects, échantillons au sol,
  Espace sur la case occupée, picto « celui-là », noms écrits en `PixelFont`. **Il est refait
  en bâtiment dans cette phase**, avec le même contenu.
- La sauvegarde accepte un champ de plus, `CurrentVersion` reste à 1.
- **La question ouverte de la phase 8** : l'hiver gèle les trois maisons peu profondes au tick,
  l'automne les bouche. Les tuyaux de cette phase y répondent.

**Critère de fin : j'entre dans l'usine par sa porte, l'ouvrier me dit ce que je peux faire,
je prends le tuyau qui résiste à l'hiver, je le pose sur la route d'une maison peu profonde,
et cette maison reste desservie en plein hiver. Avec le tuyau ordinaire, elle se coupe comme
avant. Et l'atelier des plaques se visite de la même façon, avec son propre artisan.**

## Décisions de design validées

1. **Trois types de canalisation, un par menace de saison.** `Standard`, `Isolé` qui ne gèle
   pas, `Grillagé` que les feuilles ne bouchent pas. Même usure pour les trois : l'entretien
   reste la boucle, un tuyau ne rend pas la clé inutile.
2. **Tuyaux illimités, pas de stock.** Le choix se fait dans l'usine ; on descend avec un type
   en main, et on revient pour en changer. Pas de compte, pas de panne au fond d'une galerie.
3. **Les usines sont des bâtiments dans lesquels on entre.** Une porte, un fondu au noir comme
   pour descendre sous terre, et un intérieur où les échantillons sont exposés. **Un
   personnage s'y tient et dit ce qu'on peut y faire** : choisir et changer les plaques chez
   l'artisan des plaques, choisir et poser des tuyaux chez l'ouvrier des tuyaux. **L'atelier
   des plaques de la phase 7 est refait sur ce modèle.** L'usine à panneaux, phases 12 à 15,
   suivra le même patron avec trois personnages, un par mini-jeu, chacun expliquant son
   problème et demandant de l'aide ; Espace lance le mini-jeu.
4. **Le type se lit sur le tuyau lui-même**, par un motif, jamais par une couleur : la couleur
   dit déjà l'état depuis la phase 5.
5. **Les noms sont écrits.** Sous chaque échantillon de tuyau, son nom en `PixelFont`, comme
   les noms de villes sous les plaques. Victorien sait lire.

## 1. Les trois types

| Type | Gel | Feuilles | Usure | Motif placeholder | Nom écrit |
|---|---|---|---|---|---|
| `Standard` | gèle | se bouche | 0,1 | corps uni, celui d'aujourd'hui | `NORMAL` |
| `Isolé` | **ne gèle jamais** | se bouche | 0,1 | corps rayé en travers, comme une gaine | `ISOLÉ` |
| `Grillagé` | gèle | **ne se bouche jamais** | 0,1 | corps pointillé, comme une grille | `GRILLÉ` |

**Chaque type répond à une saison, et une seule.** L'hiver reste un problème pour le grillagé,
l'automne pour l'isolé : choisir, c'est d'abord comprendre ce qui coupe la maison. Au-dessus de
chaque échantillon, le picto de la saison qu'il vainc, flocon ou feuille, en plus du nom.

`PixelFont` n'a pas d'accents, et `ISOLÉ` en a besoin. Les accents s'ajoutent donc ici plutôt
qu'en phase 14, et en **9a**, avec le reste du travail de police : les phrases des personnages
en ont besoin de toute façon.

`PipeType` gagne `LeafResistance`, jumeau de `FrostResistance` : 0 se bouche à la première
feuille, 1 jamais. Les valeurs sont 0 ou 1 aujourd'hui, mais le champ est une probabilité
comme le gel, pour la même raison : un type intermédiaire ne demanderait pas de code.

**L'usure ne change pas.** Un tuyau qui ne s'use pas supprimerait la clé de réparation, et
c'est le geste qui garde le bassin bas et les maisons desservies. Réversible : c'est un champ
par type, déjà là.

### Ce que cela change au bassin

Avec des tuyaux isolés sur les routes peu profondes, l'hiver ne coupe plus personne au tick ;
avec des grillagés, l'automne non plus. **Le tableau de la phase 8, 13 / 8 / +5 puis 6 / 6 / 3,
devient atteignable en jeu**, pour qui a bien choisi ses tuyaux. La question ouverte de la
phase 8 trouve ici sa réponse sans changer l'ordre du tick.

## 2. Le type est porté par la case

**Le type vit sur le nœud**, pas sur le segment : une case porte un tuyau d'un type, et c'est
ce que le joueur voit. `PipeNode` gagne `PipeType`. Les nœuds imposés par le monde, station,
maisons, bassin, n'en portent aucun.

**Un segment est aussi faible que sa plus faible extrémité.** Entre un isolé et un standard, le
segment gèle : la résistance retenue est la plus basse des deux bouts. Une extrémité sans
type, station ou maison, ne compte pas. C'est la même logique que « un segment est aussi
exposé que son extrémité la moins profonde ».

Conséquence lisible : **une route isolée l'est de bout en bout**, ou elle ne l'est pas. Un
seul tuyau standard au milieu gèle, et sa couleur d'hiver le désigne.

**Changer le type d'une case, c'est enlever puis reposer**, le même geste que depuis la
phase 3. Aucun geste de remplacement.

## 3. Les bâtiments

Le patron, commun à l'atelier des plaques, à l'usine à tuyaux, et plus tard à l'usine à
panneaux :

- **Dehors, une façade dans le village**, sur des cases bloquantes comme une maison, avec **une
  porte** sur une case praticable devant. Espace sur la porte fait entrer, comme Espace sur une
  bouche fait descendre : même geste, même fondu au noir, même `TravelAsync`. Sortir, c'est
  Espace sur la porte vue de l'intérieur.
- **Dedans, une pièce close**, quelques cases de large, avec les échantillons au sol, leurs
  noms, et **le personnage**. On y marche aux flèches comme partout.
- **Le personnage parle quand on lui parle** : Espace face à lui. Il dit ce qu'on peut faire
  ici, en phrases de moins de six mots, en français, conformément à CLAUDE.md. Le picto
  au-dessus de la tête annonce qu'on peut lui parler. Espace referme. Rien ne bloque : on peut
  prendre un échantillon sans lui avoir parlé.
- **Chaque bâtiment a son personnage** : l'artisan des plaques, l'ouvrier des tuyaux. Un
  sprite placeholder chacun, 16x24 comme le joueur, de couleur distincte.

Les phrases exactes sont écrites plus bas, section « Les phrases exactes ». Elles ont été
relues à voix haute pour six ans.

**L'atelier des plaques est refait sur ce patron.** La cour pavée de la phase 7 devient une
façade avec sa porte ; les huit plaques et leurs noms passent à l'intérieur ; le plan du
village s'ouvre toujours depuis une plaque, et se referme toujours sur la bouche choisie. Rien
ne change au choix lui-même ni à la sauvegarde des plaques.

## 4. Le tuyau en main

`PipeFactory` porte le catalogue des trois types, les cases des échantillons, et **le type en
main**, avec un événement `Changed`. Il vit dans la scène `Interiors`, comme `ManholeFactory`
y déménage en 9a ; le patron reste celui de `ManholeFactory`.

**Le picto de pose devient le tuyau en main.** Face à une galerie vide, le picto au-dessus de
la tête n'est plus `picto_pipe` mais l'échantillon du type courant : il sait toujours ce qu'il
va poser, sans jauge. `picto_pipe` disparaît.

**Le HUD ne change pas.** Le tuyau en main se voit au moment où il compte, devant une galerie,
et sur chaque tuyau posé.

**Le type en main est sauvegardé**, un entier : redescendre pour découvrir qu'on a repris le
standard serait une surprise.

## 5. Le rendu

**Un motif par type, seize tuiles par motif** : quarante-huit tuiles, dessinées par la
fonction unique de la phase 3 avec un paramètre de plus. `PipeNetworkView` choisit la famille
par le type du nœud, puis la tuile par le masque, puis la couleur par l'état, comme avant.

La couleur reste au seul service de l'état. **Un tuyau isolé gelé est impossible** par
construction, et **un standard gelé est blanc bleuté** comme depuis la phase 5 : le motif dit
le type, la couleur dit ce qui lui arrive.

## 6. La sauvegarde

Deux champs de plus dans `SaveData`, et rien d'autre :

```
List<SavePipeType> pipeTypes    cell, type : seules les cases NON standard
int pipeInHand                  le rang du type en main, 0 standard
```

**`CurrentVersion` reste à 1.** Une partie de la phase 8 n'a ni l'un ni l'autre : tous ses
tuyaux sont standard, le standard est en main, et elle se relit sans une erreur. Un type dont
le rang n'existe plus retombe sur le standard, en silence. Au redémarrage, le joueur repart au
départ du village, jamais dans un bâtiment : la couche courante n'est toujours pas écrite.

## 7. Fichiers

**Créés** — `Scripts/Buildings/PipeFactory.cs`. Les fichiers des intérieurs, le personnage et
la boîte de dialogue sont créés en **9a**, voir la liste de cette phase plus bas.

**Modifiés** — `PipeType`, `PipeNode`, `PipeNetwork`, `PipeSegment` (résistances effectives
depuis les deux bouts), `SeasonSystem` (les feuilles lisent la résistance, comme le gel),
`PipeNetworkView` (la famille de tuiles par type), `PlayerInteractor` (`ChoosePipe`, parler,
entrer et sortir, le picto de pose), `SaveData` et `SaveSystem`, `ScriptableObjectSetup`
(trois types), `PixelFont` (les accents), `VillageLayout` (deux façades et deux portes, la cour
de l'atelier disparaît), `SurfaceSceneBuilder`, `PersistentSceneBuilder`,
`PlaceholderArtGenerator` (trente-deux tuiles de tuyau, deux façades, deux personnages, les
noms, les phrases si elles sont des images), `ManholeFactory` (ses échantillons changent de
place).

**Référence d'assembly, règle 9 :** rien de neuf attendu **en 9b** ; en 9a,
`InteriorsSceneBuilder` touche un type URP. À vérifier plutôt que supposer, dans les deux cas.

## 8. Ce que l'on ne fait pas

- **Pas de stock, pas de compte de tuyaux.** Décision 2.
- **Pas de tuyau anti-usure.** L'entretien reste la boucle. Réversible en un champ.
- **Pas de tuyau anti-tout.** Un type par menace, sinon il n'y a plus de choix.
- **Pas de dialogue à choix.** Le personnage parle, Espace referme. Aucune question posée au
  joueur, aucune réponse à donner.
- **Pas l'usine à panneaux.** Son bâtiment suivra le même patron en phase 12 ; on ne le
  construit pas ici. On s'assure seulement que le patron accepte trois personnages.
- **Pas de type de tuyau pour le bassin ni pour la station** : les nœuds du monde ne portent
  pas de type.

## Décisions techniques signalées

1. **Le type est sur le nœud, la résistance effective sur le segment.**
2. **`LeafResistance` est une probabilité de tenir**, symétrique de `FrostResistance`.
3. **Le motif plutôt que la teinte.** `PipeType.Tint` reste inutilisé plutôt que de casser les
   assets.
4. **Le picto de pose est l'échantillon**, comme la plaque en phase 7.
5. **Seules les cases non standard sont écrites**, comme seuls les segments abîmés le sont.
6. **Le type en main est un entier sauvegardé**, exception assumée à « on ne sauvegarde pas ce
   qu'on ne relit pas ».
7. **Entrer dans un bâtiment est un voyage `TravelAsync`** : fondu, bascule, replacement du
   personnage. Aucun nouveau mécanisme de transition.

## Choix techniques fixés le 3 septembre 2026

Les quatre points laissés ouverts par le plan de design ont été tranchés avant d'écrire une
ligne, règle 7. Les quatre recommandations sont retenues.

### 1. Les intérieurs vivent dans une troisième couche

`GameLayer.Interior` et une scène `Interiors`, avec toutes les pièces côte à côte dans une
seule carte. **C'est une cinquième scène, écart explicite aux quatre scènes de CLAUDE.md**,
accepté le 3 septembre 2026.

La voie écartée, les pièces peintes dans la Surface hors du village, finissait par payer le
même prix sans obtenir la couche : la carte dépasserait la colonne 40, donc `mapSize` change,
donc `WorldBounds` du village change, donc la caméra du village n'est plus bornée à
`x ∈ [10, 30]` comme la phase 1 l'a vérifié ; et surtout **une `Light2D` globale porte sur des
Sorting Layers, pas sur une zone** : les pièces prendraient la couleur de la saison. Le seul
remède aurait été une famille de Sorting Layers à part et une seconde lumière globale, c'est-à-dire
le coût de la troisième couche sans la troisième couche.

Ce que cela implique, vérifié dans le code plutôt que supposé :

- **`GameLayer.Interior` s'ajoute À LA FIN**, jamais inséré. `ManholePortal.destinationLayer`
  est un enum sérialisé dans les scènes : insérer décalerait les huit portails de la phase 2.
  Même règle que `NodeType.ReserveInlet` en phase 8.
- `SceneRouter` cesse de citer deux couches en dur : `SceneNameFor`, `SetActiveLayer` et le
  chargement initial balaient les trois.
- **Une famille `Interior_*` de cinq Sorting Layers**, dix deviennent quinze, et **une `Light2D`
  globale cantonnée à eux**. Sans quoi on retombe sur le « More than one global light on layer
  Default » de la phase 0. Cinq et non trois, pour que `GameSortingLayers` reste un tableau
  sans exception.
- **La caméra se borne à la pièce, pas à la carte.** `GridMap` gagne
  `WorldBoundsAround(cell)`, virtuelle, qui rend `WorldBounds` par défaut ; `InteriorMap` la
  surcharge pour rendre les bornes de la pièce contenant la case. `CameraFollow` demande les
  bornes autour de sa cible plutôt que celles de la carte : deux lignes, et la notion de pièce
  ne sort pas d'`InteriorMap`. Une pièce fait au plus 20 cases de large sur 10 de haut, soit
  la vue exactement : la caméra se centre, rien ne défile.
- **`ManholeFactory` change de scène**, Surface vers Interiors, puisque ses échantillons
  passent à l'intérieur. Conséquence à traiter, et c'est le piège de la résolution paresseuse
  entre scènes : `ManholeCover` vit en Surface et s'abonne à l'atelier alors que les Interiors
  sont éteints. Il devra le chercher en **`FindObjectsInactive.Include`**, comme `SaveSystem`
  le fait déjà. `PlayerInteractor.ResolveFactory` n'a rien à changer : il ne cherche l'atelier
  que lorsqu'on est dedans, donc allumé.
- `LayerIndicator` **ne change pas** : dans un bâtiment il montre le picto de surface, ce qui
  est vrai, on n'est pas descendu. Aucune image de plus au HUD.

### 2. La porte : Espace dessus, comme sur une bouche

`ManholePortal` et `PortalBuilder` sont réutilisés tels quels : un portail est déjà décrit par
`(couche, case)` vers `(couche, case)`, et une porte n'est rien d'autre. Il ne reste qu'à ouvrir
le choix du picto dans `PlayerInteractor`, qui décide aujourd'hui par
`DestinationLayer == Underground ? bas : haut` et passe à trois cas. **Deux pictos à dessiner**,
`picto_enter` et `picto_exit`.

Entrer en marchant a été écarté : la porte est sur une case praticable devant la façade, donc
la longer ferait entrer par accident, et ressortir dépose le personnage **sur** la porte, qui le
ferait rentrer aussitôt. Il faudrait une garde « je viens d'arriver », la famille de bug de
l'Espace lu deux fois de la phase 7. Et le geste d'Espace sur la case occupée est appris depuis
la phase 2.

### 3. Les phrases sont des images, dessinées par `PixelFont`

Les phrases sont fixes et connues à la génération. Les dessiner à chaud demanderait de déplacer
`PixelFont` dans l'assembly Runtime, d'y ajouter un cache de textures, et produirait des images
ni versionnées ni relisibles à l'œil. La police reste côté Editor.

Deux détails signalés plutôt que découverts en route :

- **Un accent demande deux rangées de plus** au-dessus d'un glyphe de 5 sur 7. `PixelFont.Height`
  devient `HeightOf(word)` et ne réserve ces deux rangées **que si le mot porte un accent** :
  les huit noms de villes de la phase 7 restent alors **au pixel près** ce qu'ils sont.
- **`maxTextureSize` est à 64 depuis la phase 7.** « LE GRILLÉ ARRÊTE LES FEUILLES » fait environ
  170 pixels de large : à 64, Unity la réduirait **en silence**, exactement le piège que la
  phase 7 a évité. Les textures de phrases passent à 256.

Glyphes ajoutés : **É**, **Ê**, **È**, **À** et l'**apostrophe**. Seuls É, Ê et l'apostrophe
servent aujourd'hui ; È et À coûtent deux tableaux et la phase 14 les voudra.

**La boîte de dialogue est une `Image` éteinte de `HUD_Canvas`**, allumée seulement pendant que
le personnage parle, comme le voile du fondu depuis la phase 2. « Le HUD ne change pas » est lu
comme « aucun indicateur permanent de plus ».

**Deux gardes symétriques sur Espace**, une côté `Villager` et une côté `PlayerInteractor` : les
deux liront la même touche, et l'ordre de leurs `Update` n'est garanti par rien. C'est la leçon
de la phase 7, écrite avant d'être répétée.

### Les phrases exactes

Toutes à cinq mots ou moins, en français, relues à voix haute pour six ans. Écrites en
majuscules, la seule casse que `PixelFont` connaisse.

**L'artisan des plaques**, trois lignes :

1. « CHOISIS UNE PLAQUE »
2. « PUIS CHOISIS UNE BOUCHE »
3. « TU PEUX EN CHANGER »

**L'ouvrier des tuyaux**, quatre lignes :

1. « CHOISIS UN TUYAU »
2. « L'ISOLÉ ARRÊTE LE FROID »
3. « LE GRILLÉ ARRÊTE LES FEUILLES »
4. « REVIENS QUAND TU VEUX »

Les deux phrases des tuyaux sont symétriques à dessein : **même verbe, ARRÊTE**, mot que
Victorien connaît et qui est déjà sur les panneaux qu'il aime, et la menace qui change. Chacune
répond au picto affiché au-dessus de son échantillon, flocon ou feuille. « Le grillagé ne se
bouche pas » a été écartée : six mots. Chaque troisième ligne dit que le choix se refait, ce qui
est la promesse du jeu.

**Le nom écrit sous l'échantillon est `GRILLÉ`**, pas `GRILLAGÉ` : plus court, et c'est le mot
de la phrase. Les trois noms deviennent donc `NORMAL`, `ISOLÉ`, `GRILLÉ`. Le nom du type dans le
code reste `Grillage`.

### 4. Deux commits, 9a puis 9b

Tout le risque d'architecture est dans 9a : troisième couche, routeur, Sorting Layers, bornes de
caméra par pièce, la parole. 9a se termine sur quelque chose de jouable et vérifiable de bout en
bout, et 9b n'est plus que du travail de tuyaux sur un patron prouvé. Si 9a se passe mal, on le
sait avant d'avoir écrit les tuyaux dessus. La numérotation des phases 10 à 16 ne bouge pas.

**Les accents de `PixelFont` sont dans 9a**, avec le reste du travail de police, pour que 9b soit
purement des tuyaux.

## Phase 9a — les bâtiments

Le patron des intérieurs, le personnage qui parle, et l'atelier des plaques refait dessus.
**L'usine à tuyaux n'est pas touchée** : ni type, ni motif, ni tuyau en main.

### Le village

La cour pavée de seize cases sur six disparaît. À sa place, en haut à droite, **la façade de
l'atelier**, quatre cases sur deux en cases bloquantes, et **sa porte** sur une case praticable
devant elle. Deux marqueurs de plus au plan : `F` façade de l'atelier, `D` sa porte. La porte
porte du chemin dessous, comme un seuil.

La façade occupe `x ∈ [22, 25]`, `y ∈ [26, 27]` ; la porte est en **(23, 25)**. La façade de
l'usine, `G`, et sa porte `E` en (31, 25), viennent en 9b : la place leur est laissée libre.

**Rien d'autre du plan ne bouge d'un caractère** : la station, les cinq maisons, les trois
bouches et le départ restent exactement où ils sont.

### Les intérieurs

`InteriorsLayout`, trente lignes de quarante caractères comme les deux autres plans, découpé en
**créneaux de 20 cases sur 10**, un par pièce. Six créneaux, de quoi loger l'atelier, l'usine et
l'usine à panneaux de la phase 12 sans redécouper. L'atelier prend le créneau en haut à gauche.

Une pièce est close : des murs bloquants tout autour, un sol, **une porte dans le mur du bas**
qui ramène devant la façade dans le village. Les huit plaques et leurs noms passent à
l'intérieur, toujours espacées de quatre cases, deux rangées de quatre. L'artisan se tient dans
la pièce, sprite 16x24 comme le joueur, d'une couleur distincte.

### Ce que 9a crée et modifie

**Créés**, runtime — `World/InteriorMap.cs`, `Buildings/Villager.cs`, `UI/SpeechBox.cs`.
**Créés**, Editor — `SceneBuilders/InteriorsLayout.cs`, `SceneBuilders/InteriorsSceneBuilder.cs`.

**Modifiés** — `GameLayer` (+ `Interior`, à la fin), `SceneRouter` (trois couches),
`GameSortingLayers` et `SortingLayerSetup` (famille `Interior_*`), `GridMap`
(`WorldBoundsAround`), `CameraFollow` (bornes autour de la cible), `PlayerInteractor` (entrer,
sortir, parler, les pictos), `ManholeCover` (`FindObjectsInactive.Include`), `PixelFont` (les
accents et `HeightOf`), `VillageLayout` (la façade et la porte, la cour disparaît),
`SurfaceSceneBuilder`, `PersistentSceneBuilder` (la boîte de dialogue au HUD),
`PlaceholderArtGenerator` (façade, porte, murs et sol de pièce, artisan, `picto_enter`,
`picto_exit`, `picto_talk`, les trois phrases), `BuildAllScenes` (cinq scènes).

**`ManholeFactory` déménage** de la scène Surface à la scène Interiors, sans changer d'une ligne
son contenu ni la sauvegarde des plaques.

**Référence d'assembly, règle 9 :** `InteriorsSceneBuilder` crée une `Light2D` globale, donc il
touche un type d'`Unity.RenderPipelines.Universal.2D.Runtime`. `SousLaVille.Editor.asmdef` doit
porter cette référence. **À vérifier, pas à supposer.**

## Phase 9b — l'usine à tuyaux

Les sections 1 à 7 de ce plan, sur le patron prouvé par 9a : les trois types, le type sur le
nœud, la façade et la porte de l'usine, sa pièce, l'ouvrier, les trois échantillons et leurs
noms, le tuyau en main, les quarante-huit tuiles, la sauvegarde.

## Vérification de fin de phase 9a

1. Éditeur hors play. Menus dans l'ordre : art placeholder, ScriptableObjects, scènes.
2. Console relue par le pont MCP : zéro erreur **et** zéro warning, hors le warning du package
   MCP émis depuis `Library/PackageCache`.
3. **Plan du village relu par script** : la façade de l'atelier et sa porte aux cases attendues,
   **la station, les cinq maisons, les trois bouches et le départ exactement où ils étaient**,
   la cour de l'atelier disparue. Sous-sol inchangé, chemins inchangés.
4. Scènes relues par script : cinq scènes au build dans l'ordre ; quinze Sorting Layers ; **trois
   lumières globales, chacune cantonnée aux cinq layers de sa couche** ; aucun renderer des
   Interiors hors de la famille `Interior_*` ; deux portails appariés atelier / village ; huit
   plaques exposées et huit noms dans la pièce ; la boîte de dialogue éteinte au HUD.
5. **Les huit noms de villes comparés octet par octet à ceux de la phase 7** : `HeightOf` ne doit
   pas les avoir touchés. Les phrases relues à taille réelle, accents compris, non tronquées.
6. Play depuis Boot, par injection clavier, `Application.isFocused` vérifié :
   - **Espace sur la porte de l'atelier** : fondu, intérieur, le personnage joueur est là,
     l'artisan aussi, la caméra est centrée sur la pièce et ne montre pas la pièce voisine ;
   - **Espace face à l'artisan** : il parle, une ligne à la fois, Espace passe à la suivante,
     Espace referme, **et ne rouvre pas dans la foulée** ;
   - une plaque se choisit et se pose depuis l'intérieur **exactement comme en phase 7**, et
     **Espace sur la porte ramène dans le village, devant la façade** ;
   - **les saisons passent pendant qu'on est dans le bâtiment**, et l'intérieur **ne change pas
     de lumière** ; en ressortant, la surface est à la couleur de la saison en cours ;
   - la marche, le creusement et la pose sous terre sont **inchangés** ;
   - **quitter et relancer** : les plaques sont retrouvées, et le joueur repart au départ du
     village, jamais dans un bâtiment ;
   - **une partie de la phase 8 se relit sans une erreur** ;
   - console **entièrement vide** sur une session complète.
7. Captures : l'intérieur de l'atelier avec l'artisan qui parle, la façade et sa porte vues du
   village.
8. `git status` : **aucune modification des ProjectSettings**.
9. PROGRESS.md à jour, commit `Phase 9a - les bâtiments`, **arrêt et résumé**.

## Vérification de fin de phase 9b

1. Les trois premiers points de 9a, avec **deux** façades et **deux** portes au plan du village.
2. Les trois motifs comparés à taille réelle, sur les seize masques : distincts entre eux, et
   distincts sous les cinq couleurs d'état. Les trois noms lisibles, accent compris.
3. Play depuis Boot, par injection clavier, horloge accélérée :
   - **Espace sur la porte de l'usine**, l'ouvrier parle ; **Espace sur l'échantillon isolé** :
     le picto de pose change, le personnage continue ;
   - une pose sous terre crée un nœud **de ce type**, tuile du bon motif ;
   - **un standard entre deux isolés gèle, les isolés non**, couleurs à l'appui ;
   - **hiver** : une maison peu profonde reliée en isolé de bout en bout **reste desservie**, la
     même en standard se coupe. C'est le critère de fin ;
   - **automne** : la même chose en grillagé face aux feuilles, sur plusieurs automnes ;
   - un grillagé gèle, un isolé se bouche : un type par menace ;
   - le bassin : **l'année suit le tableau de la phase 8** avec un réseau isolé et grillagé là où
     il faut, **sans copie de saison cette fois**, et sans changer l'ordre du tick ;
   - **quitter et relancer** : les types, le tuyau en main et les plaques sont retrouvés ;
   - **une partie de la phase 8 se relit sans erreur**, tout en standard, standard en main ;
   - console **entièrement vide** sur une session complète.
4. Captures : l'intérieur de l'usine avec l'ouvrier qui parle, un réseau mêlant les trois motifs,
   le même en plein hiver.
5. `git status` : aucune modification des ProjectSettings.
6. PROGRESS.md à jour, commit `Phase 9b - l'usine à tuyaux`, **arrêt et résumé**. On n'enchaîne
   pas sur la phase 10.
