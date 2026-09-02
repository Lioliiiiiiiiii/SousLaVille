# Plan de la phase 4 — L'eau coule

Validé le 2 septembre 2026. Voir CLAUDE.md pour les contraintes du projet et PROGRESS.md
pour l'état d'avancement.

## Contexte

Phase 3 committée (`327a7d0`). Tout est en place pour le solveur :

- `PipeNetwork.Changed` est le seul signal dont il a besoin.
- `PipeNode.Depth` est rempli à la pose, la station est le seul point à la profondeur 3.
- `PipeSegment` porte déjà `condition`, `isFrozen` et `isClogged`, que le solveur doit lire
  selon CLAUDE.md même si rien ne les modifie avant la phase 5.
- `fixedNodes` sait déjà déclarer des nœuds permanents : les maisons s'y ajoutent sans
  mécanisme nouveau.

**Critère de fin : je creuse depuis une maison jusqu'à la station, je pose des tuyaux, l'eau
se met à couler dedans et la goutte au-dessus de la maison devient bleue. Si mon chemin
remonte, elle reste grise et rien ne me punit.**

## Décisions de design validées

1. **Cinq maisons**, posées à la main dans le plan du village, marqueur `A`.
2. **Trois retours visuels** : tuyaux teintés en bleu quand ils portent l'eau, goutte au-dessus
   de chaque maison, rangée de gouttes en HUD.
3. **Des crêtes peu profondes sont ajoutées** au plan des profondeurs, pour forcer de vrais
   détours.
4. **L'eau d'une maison non reliée ne se voit pas** en phase 4. La goutte reste grise, les
   débordements restent le sujet de la phase 10.

## 1. Les maisons

| Case | Contexte | Profondeur de son alcôve |
|---|---|---|
| (5, 20) | contre le chemin qui descend de la station | 2 |
| (13, 17) | angle nord-ouest de la boucle du parc | 2 |
| (27, 17) | angle nord-est de la boucle | 1 |
| (29, 10) | à l'est du chemin du bas | 1 |
| (34, 4) | près de la bouche du coin sud-est | 1 |

La case d'une maison devient bloquante, comme le bâtiment de la station : on passe devant,
pas dedans. **Sous chaque maison, une alcôve d'une case déjà creusée** porte un nœud permanent
`HouseConnection`. Le joueur doit creuser jusqu'à elle : c'est la boucle de jeu.
`ValidateAgainstVillage()` refuse de construire si une maison n'a pas son alcôve.

## 2. Les crêtes

Le plan des profondeurs gagne **deux arcs peu profonds**, à 14 et à 8 cases de la station en
distance de Manhattan, chacun percé d'une seule porte, les deux portes placées à l'opposé
l'une de l'autre : la porte lointaine au sud, la porte proche à l'est.

Un arc à profondeur 1 posé au milieu de la couronne de profondeur 2 est un mur pour l'eau :
un trajet déjà descendu à 2 ne peut pas y remonter. Il faut donc trouver la porte, où la
profondeur reste à 2. Les arcs se voient dans la terre avant même d'être creusés, puisque la
terre est teintée par sa profondeur depuis la phase 3.

Chemins monotones les plus courts, vérifiés par calcul avant écriture du plan :

| Maison | Sans crête | Avec crêtes | Détour |
|---|---|---|---|
| (5, 20) | 6 | 6 | 0 |
| (13, 17) | 15 | 31 | +16 |
| (27, 17) | 29 | 45 | +16 |
| (29, 10) | 38 | 46 | +8 |
| (34, 4) | 49 | 57 | +8 |

La maison la plus proche reste triviale : c'est la première réussite, celle qui apprend la
règle. Les quatre autres demandent de trouver les portes. **Aucune maison n'est rendue
impossible**, ce qui est vérifié par un parcours en largeur à profondeur non décroissante sur
les 1200 cases, avant et après l'ajout des crêtes.

## 3. `Scripts/Network/FlowSolver.cs` (créé)

Le parcours de CLAUDE.md, à la lettre : **pour chaque maison, un parcours en largeur jusqu'à
la station.** Une arête n'est franchissable que si, dans le sens de l'écoulement,
`nodeB.depth >= nodeA.depth`, et si le segment a `condition > 0.3`, n'est pas gelé et n'est
pas bouché.

- Résultat : l'ensemble des segments qui portent de l'eau, et l'état desservi ou non de chaque
  maison.
- `event Solved`, levé après chaque résolution.
- Ne tourne **que** sur `PipeNetwork.Changed`, et plus tard aux ticks de saison. Jamais par
  frame.

**Le solveur vit dans la scène Persistent**, sur le GameManager, et non dans l'Underground.
Raison technique : quand le joueur remonte, la racine de l'Underground s'éteint, et un solveur
éteint ne pourrait plus répondre aux maisons de la surface, justement allumées à ce moment-là.
Il résout sa référence au réseau une fois, avec `FindObjectsInactive.Include`, et la garde.

## 4. Fichiers

**Créés** — `Scripts/Network/FlowSolver.cs`, `Scripts/World/House.cs`,
`Scripts/World/HouseSpawner.cs`, `Scripts/UI/HouseCounter.cs`.

**Modifiés** — `PipeNetworkView` (teinte au signal `Solved`), `GameManager` (expose `Flow`
comme il expose `Router`), `VillageLayout` (cinq `A` et la tuile bloquante des maisons),
`UndergroundLayout` (cinq alcôves, le plan des profondeurs avec crêtes, la validation),
`SurfaceSceneBuilder` (le `HouseSpawner`), `UndergroundSceneBuilder` (les nœuds
`HouseConnection` et leurs repères), `PersistentSceneBuilder` (le solveur et la rangée de
gouttes), `PlaceholderArtGenerator` (maison, tuile bloquante de maison, arrivée de maison sous
terre, goutte pleine, goutte vide).

Règle 9 : rien de nouveau côté assemblies. Aucun type URP, et `UnityEngine.UI` est référencé
depuis la phase 2.

## Décisions techniques signalées

1. **`HouseSpawner` instancie les maisons au réveil** à partir d'une liste de cases cuite par
   le builder, plutôt que cinq objets figés dans la scène. C'est ce que son nom promet, et la
   scène reste légère.
2. **`House.cs` en plus de la liste de CLAUDE.md.** Une maison a un état, il lui faut un
   composant ; le spawner ne peut pas porter cinq états.
3. **Le solveur en Persistent, pas dans l'Underground.** Voir plus haut : c'est la couche
   éteinte qui décide.
4. **`GameManager` expose `Flow`.** Core dépend donc de Network, comme il dépend déjà de UI
   depuis la phase 2. Une seule assembly runtime, aucun cycle.
5. **La teinte plutôt qu'une animation.** Une eau qui défile demanderait des images animées et
   un composant de plus ; la couleur suffit à dire que ça marche. À rediscuter à l'habillage.
6. **La rangée du HUD est un compteur, pas une liste.** Elle remplit les N premières gouttes
   sur cinq : l'ordre des maisons n'a pas à être appris.
7. **Rien n'est encore sauvegardé.** Après cette phase, la sauvegarde devient la priorité : le
   joueur peut désormais perdre un vrai réseau.

## Vérification de fin de phase

1. Les trois menus, puis construction des scènes.
2. Console relue par le pont MCP : zéro erreur **et** zéro warning.
3. Scène relue par script : cinq maisons en surface sur des cases bloquantes, cinq alcôves
   alignées dessous, cinq nœuds `HouseConnection` permanents, station toujours unique, et le
   plan des profondeurs conforme au calcul des crêtes.
4. Play depuis Boot :
   - un chemin correct de bout en bout : la maison est desservie, ses segments passent au
     bleu, la goutte du HUD se remplit ;
   - un chemin qui **remonte** en cours de route : la maison reste grise, aucun message ;
   - retirer un tuyau au milieu d'un chemin qui marchait : la maison redevient grise
     immédiatement ;
   - deux maisons desservies par un tronc commun : les deux gouttes se remplissent ;
   - le solveur ne tourne qu'aux changements, vérifié par compteur d'appels sur plusieurs
     centaines d'images.
5. Captures d'écran : un réseau qui coule, un réseau qui ne coule pas.
6. PROGRESS.md à jour, commit `Phase 4 - l'eau coule`, puis arrêt.
