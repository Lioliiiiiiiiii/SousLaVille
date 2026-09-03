# Plan de la phase 10 — Les fuites

Design tranché le 3 septembre 2026. Voir CLAUDE.md pour les contraintes du projet et
PROGRESS.md pour l'état d'avancement.

## Contexte

Phase 9b committée (`8522de7`). Ce qui sert ici :

- **`SeasonSystem.LastBudget.Lost`** est calculé à chaque tick depuis la phase 8 et **ne
  s'affiche nulle part**. C'est le seul nombre que le jeu calcule et ne montre jamais.
- **Les Sorting Layers `Surface_Water` et `Underground_Water`**, créés en phase 0 avec le
  commentaire « flaques, fontaine », **n'ont jamais servi**. Ils attendent cette phase.
- `FlowSolver.MinimumCondition` vaut 0,3 et est publique depuis la phase 5 : le rendu et le
  solveur lisent la même constante pour dire « trop abîmé ».
- `PipeNetworkView.TintFor` sait déjà déclarer une case « trop abîmée » : le pire de ses
  segments l'emporte. La flaque reprendra exactement cette règle.
- Surface et Underground **font 40x30 et partagent le même repère** depuis la phase 2 : la
  case (x, y) du sous-sol est sous la case (x, y) du village, sans conversion.
- `SeasonChanged` est levé **après** `ApplyWaterBudget`, vérifié dans `Advance()` : un
  composant qui l'écoute lit un bilan déjà à jour. Aucun événement nouveau à créer.

**Critère de fin : je laisse le bassin débranché, l'automne arrive, et l'eau sort des trois
bouches d'égout et s'étale dans le village. Je relie le bassin, l'automne suivant le village
reste sec. Et quand un tuyau s'use jusqu'à ne plus porter, une flaque apparaît dans la rue
juste au-dessus de lui, qui me dit où creuser sans descendre.**

## Décisions de design validées

1. **La phase rend visibles DEUX eaux, et elles ne disent pas la même chose.**
   - **Le débordement** : `Lost`, le surplus que rien n'a retenu, sort **par les bouches
     d'égout**. Il dit « ton réseau reçoit plus que la station ne traite ».
   - **La fuite** : un tuyau usé jusqu'à ne plus porter laisse une flaque **dans la rue
     au-dessus de lui**. Elle dit « il y a un tuyau crevé ici, sous tes pieds ».
2. **L'eau sort par les bouches d'égout.** Victorien aime les bouches d'égout, l'eau sort par
   là où le réseau aboutit, et elle sort de sous la plaque qu'il a choisie en phase 7. La
   station est écartée : elle est au fond de son enceinte murée, on ne la voit presque jamais.
   Les rues entières sont écartées : une troisième tilemap sur tout le village pour un effet
   qui ne se lit pas mieux.
3. **Aucune mémoire, aucune sauvegarde.** L'eau est une donnée dérivée : le débordement se lit
   sur `LastBudget.Lost`, la fuite sur l'état des segments. Le projet a déjà cette règle,
   posée en phase 6 : « une donnée dérivée sauvegardée est une donnée qui peut mentir ».
   Conséquence assumée, voir plus bas.
4. **Rien ne bloque, rien ne punit.** L'eau se peint sur `Surface_Water`, jamais sur la couche
   bloquante. Le personnage la traverse. CLAUDE.md : « Un réseau qui déborde est un spectacle
   rigolo. »

## 1. Le débordement aux bouches d'égout

`Lost` vaut au plus 5, et c'est vérifiable par le calcul : l'arrivant plafonne à 13, cinq
maisons desservies plus huit de pluie d'automne ; la station en traite 8 ; le surplus plafonne
donc à 5, et le bassin absent, débranché ou plein le laisse entier.

**Les trois bouches débordent de la même façon**, pas d'un tiers chacune. Le réseau déborde,
c'est vrai partout, et il le voit où qu'il se trouve dans le village.

L'étalement croît par anneaux de Manhattan autour de chaque bouche :

| `Lost` | Rayon | Cases par bouche |
|---|---|---|
| 0 | — | village sec |
| 1 | 0 | 1, la bouche seule |
| 2 | 1 | 5 |
| 3 | 2 | 13 |
| 4 | 3 | 25 |
| 5 | 4 | 41 |

Au maximum, trois fois 41 cases, soit environ un dixième du village sous l'eau : un spectacle,
pas une inondation qui cache le village. **Seules les cases praticables prennent l'eau** :
l'eau ne monte pas sur les haies, les maisons ni les façades.

**Ce que cela apprend.** Un joueur qui n'a pas relié le bassin voit son village déborder à
chaque automne. C'est exactement la leçon de la phase 8, rendue visible sans un mot.

## 2. La fuite au-dessus d'un tuyau crevé

Une case du sous-sol est « trop abîmée » si l'un de ses segments est tombé au seuil, la règle
exacte que `PipeNetworkView` applique déjà pour la peindre en rouge terne. Cette case-là reçoit
**une flaque dans le village, sur la même case**, les deux cartes partageant le même repère.

**Seule l'usure fuit.** Un tuyau gelé ou bouché est **bouché**, pas crevé : il ne laisse rien
passer, donc rien ne sort. C'est cette distinction qui rend la flaque informative — elle ne
dit pas « quelque chose va mal ici », elle dit « un tuyau est crevé ici ».

**Une seule image d'eau pour les deux.** Une flaque est une flaque, et c'est un symbole de
moins à apprendre. C'est la position qui raconte l'histoire : une nappe autour d'une bouche,
ou une flaque isolée au milieu d'une rue.

## 3. Ce que l'on ne fait pas

- **Pas de fuite sous terre.** Les cinq couleurs de la phase 5 disent déjà gelé, bouché et trop
  abîmé sur la case même. Une flaque par-dessus serait de la décoration, pas de l'information.
- **Pas de mémoire du débordement.** Décision 3.
- **Aucun changement au bilan de l'eau ni à l'ordre du tick.** La question ouverte de la
  phase 8 reste ouverte ; cette phase ne fait que montrer un nombre déjà calculé.
- **Pas d'eau animée.** Une eau qui défile demanderait des images animées et un composant de
  plus. À rediscuter à l'habillage, comme la teinte de l'eau depuis la phase 4.
- **Pas de fontaine.** `NodeType.FountainInlet` et `Buildings/Fountain` sont dans CLAUDE.md et
  n'ont jamais servi. Le commentaire de la phase 0 range la fontaine avec les flaques, mais la
  fontaine est dans le parc, et le parc est la phase 11.
- **L'eau ne bloque pas, ne ralentit pas, n'abîme rien.** Décision 4.

## 4. Décisions techniques signalées

1. **Une seule tilemap d'eau, `Tilemap_Water`**, sur `Surface_Water`, repeinte en entier à
   chaque mise à jour. Quelques dizaines de cases, et seulement aux ticks : le même choix que
   `PipeNetworkView` depuis la phase 3.
2. **Un seul composant, `FloodView`**, pour les deux eaux. Elles partagent la tilemap et le
   même geste de repeinte ; deux composants se disputeraient la même tilemap.
3. **`FloodView` vit dans la scène Surface**, comme `SeasonAmbience` : il peint le village.
4. **Il se met à jour sur `SeasonChanged` et sur `OnEnable`, et pas sur `PipeNetwork.Changed`.**
   Ce n'est pas un oubli : creuser, poser, enlever et réparer n'existent que **sous terre**,
   donc l'état des tuyaux ne peut changer que pendant que la Surface est éteinte, ou à un tick.
   Le rallumage et le tick couvrent donc tous les cas, sans s'abonner à travers une couche
   éteinte.
5. **L'eau est peinte sur les seules cases praticables**, lues dans `SurfaceMap`. Aucune donnée
   dupliquée : la carte de collision, c'est la tilemap bloquante, depuis la phase 1.
6. **La flaque de fuite reprend la règle de `PipeNetworkView`** pour « trop abîmé » : le pire
   des segments d'une case l'emporte. Deux règles pour un même mot finiraient par diverger.
7. **Rien n'est sauvegardé.** Conséquence : après une relance, **le village est sec jusqu'au
   tick suivant**, puisque `LastBudget` repart à zéro et que la phase 6 interdit de rejouer les
   effets d'une saison au chargement. Les flaques de fuite, elles, reviennent tout de suite :
   l'usure des segments est écrite dans le fichier depuis la phase 6.

## 5. Fichiers

**Créé** — `Scripts/World/FloodView.cs`.

**Modifiés** — `SurfaceSceneBuilder` (la tilemap d'eau et le composant), `PlaceholderArtGenerator`
(une tuile d'eau).

**Référence d'assembly, règle 9 :** rien de neuf attendu, `Tilemap` étant déjà référencé.
À vérifier plutôt que supposer.

## Vérification de fin de phase

1. Éditeur hors play. Menus dans l'ordre : art placeholder, ScriptableObjects, scènes.
2. Console relue par le pont MCP : zéro erreur **et** zéro warning, hors le warning du package
   MCP émis depuis `Library/PackageCache`.
3. **Plan du village relu par script** : la station, les cinq maisons, les trois bouches, le
   départ et les deux façades et portes **exactement où ils étaient**. Sous-sol inchangé,
   87 cases praticables.
4. Scènes relues : `Tilemap_Water` sur `Surface_Water`, vide au départ, `FloodView` câblé sur
   la carte, la tuile d'eau et les trois bouches. Aucun renderer hors de sa famille.
5. La tuile d'eau relue à taille réelle, sur l'herbe, sur le chemin et sur la dalle du parc :
   elle doit se lire comme de l'eau sur les trois, et laisser voir le sol dessous.
6. Play depuis Boot, par injection clavier, horloge accélérée :
   - **bassin débranché, automne** : `Lost` vaut 5, **les trois bouches débordent**, et le
     nombre de cases mouillées est celui du tableau ;
   - **bassin relié, automne** : `Lost` vaut 0, **le village reste sec** ;
   - les cinq paliers de `Lost` donnent les cinq étalements du tableau, comptés case par case ;
   - **l'eau ne monte pas sur les haies, les maisons ni les façades** ;
   - **le personnage traverse l'eau** : la case reste praticable, rien ne le ralentit ;
   - **un tuyau usé sous le seuil** : une flaque apparaît dans la rue **sur sa case**, et le
     tuyau est bien celui que le sous-sol peint en rouge terne ;
   - **le réparer efface la flaque**, au retour en surface ;
   - **un tuyau gelé ou bouché ne fuit pas** : bouché n'est pas crevé ;
   - **descendre et remonter** : l'eau est toujours juste, c'est `OnEnable` qui la sauve ;
   - **le débordement tient toute la saison** et se remet à jour au tick suivant ;
   - **quitter et relancer** : le village est sec jusqu'au tick suivant, et **les flaques de
     fuite reviennent tout de suite**, l'usure étant sauvegardée ;
   - **une partie de la phase 9 se relit sans une erreur** ;
   - console **entièrement vide** sur une session complète.
7. Captures : le village qui déborde aux trois bouches, une flaque de fuite isolée dans une rue.
8. `git status` : **aucune modification des ProjectSettings**.
9. PROGRESS.md à jour, commit `Phase 10 - les fuites`, **arrêt et résumé**. On n'enchaîne pas
   sur la phase 11.
