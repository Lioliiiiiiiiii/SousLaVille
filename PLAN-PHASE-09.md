# Plan de la phase 9 — Les bâtiments et l'usine à tuyaux

Design validé le 3 septembre 2026 ; **les choix techniques des intérieurs restent à fixer en
début de session**, voir la dernière section. Voir CLAUDE.md pour les contraintes du projet et
PROGRESS.md pour l'état d'avancement.

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
| `Isolé` | **ne gèle jamais** | se bouche | 0,1 | corps rayé en travers, comme une gaine | `ISOLE` |
| `Grillagé` | gèle | **ne se bouche jamais** | 0,1 | corps pointillé, comme une grille | `GRILLE` |

**Chaque type répond à une saison, et une seule.** L'hiver reste un problème pour le grillagé,
l'automne pour l'isolé : choisir, c'est d'abord comprendre ce qui coupe la maison. Au-dessus de
chaque échantillon, le picto de la saison qu'il vainc, flocon ou feuille, en plus du nom.

Les noms sont provisoires : `PixelFont` n'a pas d'accents, et `ISOLÉ` en aura besoin. Les
accents s'ajoutent ici plutôt qu'en phase 14, puisqu'un mot en a besoin maintenant. Si le
personnage parle, voir ci-dessous, la police en aura besoin de toute façon.

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

Ce que l'artisan des plaques dit, à titre d'exemple, quatre lignes de moins de six mots :
« Choisis une plaque. » « Pose-la sur une bouche. » « Tu peux la changer. » Ce que dit
l'ouvrier des tuyaux : « Choisis un tuyau. » « L'isolé ne gèle pas. » « Le grillé ne se bouche
pas. » Les phrases exactes sont à écrire, et à relire à voix haute pour six ans.

**L'atelier des plaques est refait sur ce patron.** La cour pavée de la phase 7 devient une
façade avec sa porte ; les huit plaques et leurs noms passent à l'intérieur ; le plan du
village s'ouvre toujours depuis une plaque, et se referme toujours sur la bouche choisie. Rien
ne change au choix lui-même ni à la sauvegarde des plaques.

## 4. Le tuyau en main

`PipeFactory` porte le catalogue des trois types, les cases des échantillons, et **le type en
main**, avec un événement `Changed`. Où il vit dépend du choix technique des intérieurs, voir
plus bas ; le patron reste celui de `ManholeFactory`.

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

**Créés** — `Scripts/Buildings/PipeFactory.cs`, plus ce que demandent les intérieurs : au
minimum un composant de porte, un composant de personnage, et l'affichage de ce qu'il dit.
La liste exacte suit le choix technique ci-dessous.

**Modifiés** — `PipeType`, `PipeNode`, `PipeNetwork`, `PipeSegment` (résistances effectives
depuis les deux bouts), `SeasonSystem` (les feuilles lisent la résistance, comme le gel),
`PipeNetworkView` (la famille de tuiles par type), `PlayerInteractor` (`ChoosePipe`, parler,
entrer et sortir, le picto de pose), `SaveData` et `SaveSystem`, `ScriptableObjectSetup`
(trois types), `PixelFont` (les accents), `VillageLayout` (deux façades et deux portes, la cour
de l'atelier disparaît), `SurfaceSceneBuilder`, `PersistentSceneBuilder`,
`PlaceholderArtGenerator` (trente-deux tuiles de tuyau, deux façades, deux personnages, les
noms, les phrases si elles sont des images), `ManholeFactory` (ses échantillons changent de
place).

**Référence d'assembly, règle 9 :** rien de neuf attendu, à vérifier plutôt que supposer.

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

## À fixer en début de session, avant d'écrire une ligne

Ces points sont techniques mais changent l'architecture. Chacun a une recommandation ; ils
demandent un accord explicite, règle 7.

1. **Où vivent les intérieurs.** Deux voies :
   - **une troisième couche `GameLayer.Interior` et une scène `Interiors`**, avec toutes les
     pièces côte à côte dans une seule carte et une caméra bornée à la pièce courante. Propre,
     même patron que Surface et Underground, `SceneRouter` généralisé à N couches. **Mais
     c'est une scène de plus que les quatre imposées par CLAUDE.md**, écart à accepter ;
   - **les pièces peintes dans la scène Surface hors du village**, au-delà de la case 40, sans
     scène nouvelle. Aucun écart à CLAUDE.md, mais `CameraFollow` et `SurfaceMap` doivent
     apprendre des bornes par pièce, et `SeasonAmbience` teinterait les intérieurs.
   Recommandation : la troisième couche. Le SceneRouter éteint et rallume déjà des racines, et
   les intérieurs ne doivent pas changer de lumière avec les saisons.
2. **La porte.** Recommandation : Espace sur la case de la porte, comme sur une bouche, avec le
   picto d'entrée au-dessus de la tête. Alternative : entrer en marchant dessus, sans Espace.
   Le geste unique de CLAUDE.md milite pour Espace.
3. **Comment le personnage parle.** Recommandation : **les phrases sont des images**, dessinées
   par `PixelFont` à la génération comme les noms de villes, affichées dans une boîte en bas
   du HUD, une ligne à la fois, Espace passe à la suivante et referme. Alternative : déplacer
   `PixelFont` dans l'assembly Runtime et dessiner à chaud. Les phrases sont fixes, connues à
   la génération : les images suffisent et gardent la police côté Editor.
4. **Une phase ou deux.** Le patron des intérieurs plus la refonte de l'atelier, puis l'usine à
   tuyaux : c'est gros pour une seule phase. Recommandation : **deux commits dans la phase 9**,
   « Phase 9a - les bâtiments » qui refait l'atelier des plaques sur le nouveau patron, puis
   « Phase 9b - l'usine à tuyaux », avec arrêt et validation entre les deux. La numérotation
   des phases 10 à 16 ne bouge pas.

## Vérification de fin de phase

1. Les menus, puis construction des scènes.
2. Console relue par le pont MCP : zéro erreur **et** zéro warning.
3. Plan du village relu par script : deux façades et deux portes aux cases attendues, **la
   station, les cinq maisons, les trois bouches et le départ exactement où ils étaient**, la
   cour de l'atelier disparue. Sous-sol inchangé, chemins inchangés.
4. Les trois motifs comparés à taille réelle, sur les seize masques : distincts entre eux, et
   distincts sous les cinq couleurs d'état. Les trois noms lisibles, accent compris.
5. Play depuis Boot, par injection clavier, horloge accélérée :
   - **Espace sur la porte de l'atelier** : fondu, intérieur, le personnage est là, le
     personnage joueur aussi ; **Espace face à l'artisan** : il parle, Espace referme ;
   - une plaque se choisit et se pose depuis l'intérieur exactement comme en phase 7, et
     **Espace sur la porte ramène dans le village**, devant la façade ;
   - **Espace sur la porte de l'usine**, l'ouvrier parle ; **Espace sur l'échantillon isolé** :
     le picto de pose change, le personnage continue ;
   - une pose sous terre crée un nœud **de ce type**, tuile du bon motif ;
   - **un standard entre deux isolés gèle, les isolés non**, couleurs à l'appui ;
   - **hiver** : une maison peu profonde reliée en isolé de bout en bout **reste desservie**,
     la même en standard se coupe. C'est le critère de fin ;
   - **automne** : la même chose en grillagé face aux feuilles, sur plusieurs automnes ;
   - un grillagé gèle, un isolé se bouche : un type par menace ;
   - le bassin : **l'année suit le tableau de la phase 8** avec un réseau isolé et grillagé là
     où il faut, sans copie de saison cette fois ;
   - les saisons passent pendant qu'on est dans un bâtiment, et l'intérieur ne change pas de
     lumière ;
   - **quitter et relancer** : les types, le tuyau en main et les plaques sont retrouvés, et le
     joueur repart au départ du village ;
   - **une partie de la phase 8 se relit sans erreur**, tout en standard, standard en main ;
   - console **entièrement vide** sur une session complète.
6. Captures : les deux intérieurs avec leur personnage qui parle, un réseau mêlant les trois
   motifs, le même en plein hiver.
7. PROGRESS.md à jour, commits `Phase 9a - les bâtiments` puis `Phase 9b - l'usine à tuyaux`
   si la phase est coupée en deux, puis arrêt.
