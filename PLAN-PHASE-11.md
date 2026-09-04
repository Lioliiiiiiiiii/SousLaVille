# Plan de la phase 11 — Le parc

Design tranché le 4 septembre 2026. Voir CLAUDE.md pour les contraintes du projet et
PROGRESS.md pour l'état d'avancement.

## Contexte

Phase 10 committée (`4d0042b`), plus les corrections des bâtiments (`ed8afbc`). Ce qui sert ici :

- **La dalle du parc**, marqueur `P`, occupe neuf cases sur six au centre du village et **n'a
  jamais rien porté** depuis la phase 1. Elle a déjà quatre accès, un au milieu de chaque côté.
- **`NodeType.FountainInlet`** est le **seul type du modèle de CLAUDE.md qui n'ait aucun
  usage**. `Buildings/Fountain` est dans l'arborescence imposée et n'a jamais été écrit.
- **`Surface_Water`** a sa tilemap et son eau semi-transparente depuis la phase 10 : la fontaine
  a déjà sa couche et son image.
- **Le patron du nœud permanent** sert quatre fois — station, maisons, bassin — et la fontaine
  le reprend tel quel.
- CLAUDE.md : Victorien aime **les labyrinthes**.

**Critère de fin : le parc est un labyrinthe de haies. Je cherche mon chemin jusqu'à la fontaine
en son centre. Je descends, je creuse jusqu'à elle et je la relie : elle se met à jaillir, et
une sixième goutte s'allume au HUD. Un hiver qui gèle sa route l'arrête, une réparation la
relance.**

## Décisions de design validées

1. **Le parc est un labyrinthe de haies avec la fontaine en son centre.** Les deux choses que le
   projet lui réservait, en une seule.
2. **Le labyrinthe est écrit à la main**, comme `VillageLayout`, `UndergroundLayout` et les
   quinze plans de la phase 15. **Sa solvabilité est vérifiée par calcul avant d'être écrite**,
   la méthode des crêtes de la phase 4.
   Cela **contredit le commentaire de `VillageLayout`** écrit en phase 1, « le procédural est
   réservé au labyrinthe de la phase 11 ». Ce commentaire est corrigé : la pratique du projet
   a tranché dans l'autre sens depuis, et un plan engendré ne se vérifie plus une fois pour
   toutes.
3. **La fontaine compte comme une sixième destination.** Elle consomme une unité par saison
   comme une maison, et allume une sixième goutte au HUD.
4. **La station passe de 8 à 9 par saison.** Sans cela, la fontaine reliée fait gagner trois
   unités par an au bassin, qui sature vers la troisième année et fait déborder le village à
   chaque automne. Voir le calcul ci-dessous.

## 1. Les nombres

Pluie 2 / 0 / 8 / 1, une unité par destination desservie, bassin de 10.

| | Sans la fontaine | Avec la fontaine |
|---|---|---|
| Arrivant sur l'année | 4×5 + 11 = 31 | 4×6 + 11 = 35 |
| Station à **8** | 32, bilan −1 | 32, **bilan +3, insoutenable** |
| Station à **9** | 36, bilan −5 | 36, **bilan −1, tenable** |

Avec la station à 9 et la fontaine reliée, l'année attendue est :

| Saison | Arrivant | Traité | Absorbé | Relâché | Bassin |
|---|---|---|---|---|---|
| Automne | 14 | 9 | 5 | 0 | 5 |
| Hiver | 7 | 7 | 0 | 2 | 3 |
| Printemps | 8 | 8 | 0 | 1 | 2 |
| Été | 6 | 6 | 0 | 2 | 0 |

**La suite du bassin, 5 / 3 / 2 / 0, est exactement celle de la phase 8.** Seuls l'arrivant et
le traité gagnent une unité chacun. Le tableau de la phase 8 survit dans sa forme ; il est
réécrit dans PROGRESS.md avec les nouveaux chiffres, et l'ancien reste comme repère historique.

**`Lost` plafonne toujours à 5** : 14 arrivant moins 9 traités. La table d'étalement de la
phase 10, 0 / 3 / 15 / 38 / 73 / 118, reste donc valable telle quelle.

## 2. Le labyrinthe

**Le parc s'agrandit** de neuf cases sur six à **treize sur sept**, `x ∈ [14, 26]`,
`y ∈ [12, 18]`. Il ne mange que de l'herbe : **ni la station, ni les cinq maisons, ni les trois
bouches, ni le départ, ni les deux façades ne bougent d'un caractère.**

Les cases suivantes restent ce qu'elles sont et forment les **quatre entrées**, une au milieu de
chaque côté, déjà présentes depuis la phase 1 :

- ouest : (13, 15), (14, 15) le départ du joueur, (15, 15) ;
- est : (25, 15), (26, 15), (27, 15) ;
- nord : (20, 18) ;
- sud : (20, 11).

**La fontaine est au centre, en (20, 15).** Elle est atteignable depuis les quatre entrées, et
le chemin depuis chacune est vérifié par calcul avant d'écrire le plan.

**Le labyrinthe ne piège pas.** Aucun cul-de-sac ne se referme, aucune porte ne se verrouille,
rien ne se perd à s'y perdre : c'est un lieu où se promener. CLAUDE.md, « aucun échec puni ».

## 3. La fontaine

- **En surface**, un objet sur la case (20, 15), avec son bassin de pierre. Quand elle est
  desservie, **l'eau jaillit** : la tuile d'eau de la phase 10 se pose sur sa case et sur ses
  quatre voisines. Quand la route gèle, se bouche ou casse, l'eau s'arrête.
- **Sous terre**, un nœud permanent `FountainInlet` sur la même case, dans une alcôve déjà
  creusée, exactement comme les cinq maisons depuis la phase 4.
- **Elle compte comme une destination** dans le solveur et dans le bilan : le HUD gagne une
  sixième goutte.

**`FlowSolver` ne change pas de règle**, il gagne une destination. La fontaine obéit à la
profondeur croissante, au gel, aux bouchons et à l'usure comme tout le reste, sans une ligne de
code de plus — le même constat qu'au bassin en phase 8.

## 4. Ce que l'on ne fait pas

- **Pas de labyrinthe engendré.** Décision 2.
- **Pas de labyrinthe souterrain.** Le sous-sol a déjà ses crêtes ; un second dédale au-dessous
  rendrait le réseau illisible.
- **Pas d'eau animée.** La fontaine jaillit ou ne jaillit pas ; elle n'ondule pas. Même report
  qu'à la phase 10, à rediscuter à l'habillage.
- **Pas de récompense cachée dans le labyrinthe.** La fontaine est au centre, on la voit de
  loin par-dessus les haies : le plaisir est d'y arriver, pas de deviner ce qu'il y a.
- **Aucun changement à l'ordre du tick.** La question ouverte de la phase 8 reste ouverte.

## 5. Décisions techniques signalées

1. **La solvabilité est vérifiée par calcul avant l'écriture**, depuis les quatre entrées, et
   `VillageLayout` gagne une validation qui refuse de construire un parc dont la fontaine
   serait enfermée. La méthode des crêtes de la phase 4.
2. **L'alcôve de la fontaine ne touche pas au plan des profondeurs.** Seules des cases sont
   ouvertes ; les longueurs des cinq routes de maison sont recalculées et doivent rester
   57 / 46 / 31 / 45 / 6.
3. **La fontaine est un `Fountain` dans la scène Surface**, comme `WaterReserve` est dans
   l'Underground : elle s'abonne dans `OnEnable`, se désabonne dans `OnDisable` et se réapplique
   au rallumage.
4. **Le jaillissement passe par `FloodView`**, qui possède déjà la tilemap d'eau. Trois sources
   d'eau, une seule tilemap : deux composants se disputeraient la même.
5. **La capacité de la station est un champ sérialisé** sur `TreatmentPlant` depuis la phase 8.
   La passer de 8 à 9 est une ligne dans `UndergroundSceneBuilder`.
6. **Rien de neuf n'est sauvegardé.** La fontaine n'a aucun état propre : elle jaillit si le
   solveur lui trouve une route, comme une maison est desservie ou non.

## 6. Fichiers

**Créé** — `Scripts/Buildings/Fountain.cs`.

**Modifiés** — `VillageLayout` (le labyrinthe, la fontaine, sa validation), `UndergroundLayout`
(l'alcôve et son marqueur), `SurfaceSceneBuilder` (la fontaine), `UndergroundSceneBuilder` (le
nœud permanent, la station à 9), `FlowSolver` et `SeasonSystem` (la fontaine est une
destination), `HouseCounter` ou son câblage (une sixième goutte), `FloodView` (le
jaillissement), `PlaceholderArtGenerator` (la fontaine).

**Référence d'assembly, règle 9 :** rien de neuf attendu, à vérifier plutôt que supposer.

## Vérification de fin de phase

1. Éditeur **hors play**, vérifié par `EditorApplication.isPlaying` et pas de mémoire. Menus
   dans l'ordre : art, ScriptableObjects, scènes.
2. Console relue par le pont MCP : zéro erreur **et** zéro warning, hors le warning du package
   MCP.
3. **Plan du village relu par script** : la station en (6, 25), les cinq maisons, les trois
   bouches en (33, 5), (20, 10), (8, 19), le départ en (14, 15), les deux façades et les deux
   portes **exactement où ils étaient**. Bornes du village inchangées.
4. **Le labyrinthe relu par script** : la fontaine est atteignable **depuis les quatre entrées**,
   et le plus court chemin depuis chacune est mesuré et écrit dans PROGRESS.md.
5. **Sous-sol relu** : l'alcôve de la fontaine ouverte, le nœud `FountainInlet` à sa case, huit
   nœuds permanents, et **les cinq routes de maison toujours à 57 / 46 / 31 / 45 / 6**.
6. La fontaine relue à taille réelle, à l'arrêt et jaillissante.
7. Play depuis Boot, par injection clavier, horloge accélérée :
   - on entre dans le parc par chacune des quatre entrées et **on atteint la fontaine** ;
   - **rien ne piège** : depuis n'importe quelle case du labyrinthe, on ressort ;
   - la fontaine est **sèche tant qu'elle n'est pas reliée**, et le HUD montre cinq gouttes
     plus une éteinte ;
   - **reliée, elle jaillit** et la sixième goutte s'allume ;
   - **l'hiver gèle sa route** : elle s'arrête, la goutte s'éteint ; **une réparation la
     relance** ;
   - **l'année suit le tableau du plan** : 14 / 9 / +5, puis 7 / 2 relâchés, 8 / 1, 6 / 2, et
     le bassin fait 5 / 3 / 2 / 0 ;
   - **sans la fontaine reliée, le village ne déborde pas** davantage qu'avant ;
   - **quitter et relancer** : la fontaine retrouve son état, qui n'est que celui de sa route ;
   - **une partie de la phase 10 se relit sans une erreur** ;
   - console **entièrement vide** sur une session complète.
8. Captures : le parc vu d'en haut, la fontaine qui jaillit, la fontaine arrêtée en hiver.
9. `git status` : **aucune modification des ProjectSettings**.
10. PROGRESS.md à jour, **le tableau de la phase 8 réécrit avec la station à 9**, commit
    `Phase 11 - le parc`, **arrêt et résumé**. On n'enchaîne pas sur la phase 12.
