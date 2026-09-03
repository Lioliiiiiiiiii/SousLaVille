# Plan de la phase 8 — La réserve d'eau

Validé le 3 septembre 2026. Voir CLAUDE.md pour les contraintes du projet et PROGRESS.md pour l'état
d'avancement.

## Contexte

Phase 7 committée (`6852aa8`). Ce qui sert ici :

- `FlowSolver` fait déjà un parcours à profondeur non décroissante depuis chaque maison
  jusqu'à la station. Tracer un nœud de plus ne coûte rien.
- `SeasonSystem` appelle déjà `FlowSolver.Solve()` une fois par saison. Le bilan de l'eau
  s'accroche exactement là.
- **La sauvegarde accepte un champ de plus sans rien casser**, vérifié pour de bon en phase 7.
- Le patron « nœud permanent posé par le plan, alcôve déjà creusée » existe depuis la phase 4 :
  cinq maisons s'en servent.

**Critère de fin : l'automne fait déborder ce que la station peut traiter. Si j'ai relié le
bassin, il encaisse la pointe et se vide pendant l'année. Sinon, l'eau se perd, et ça se verra
en phase 10.**

## Décisions de design validées

1. **La réserve est un bassin d'orage**, vraie infrastructure d'assainissement. L'automne
   apporte plus d'eau que la station n'en traite ; un bassin raccordé encaisse la pointe et la
   relâche aux saisons suivantes.
2. **Il est fixe dans le monde, déjà creusé**, comme les alcôves des maisons. Le joueur creuse
   et pose jusqu'à lui. Aucun geste nouveau, aucun mode : le problème reste un problème de
   chemin.
3. **Le niveau se lit sur la cuve elle-même**, dans le sous-sol. Pas de jauge au HUD : à
   320 sur 180, le HUD porte déjà le repère de couche, le picto de saison et cinq gouttes.

## 1. Le volume, sans simulation de fluide

CLAUDE.md est catégorique : « Le réseau est un graphe, pas une simulation de fluide », et le
solveur ne tourne « jamais à chaque frame ». Le bilan de l'eau est donc **quatre additions par
saison**, calculées une seule fois au tick, juste après la résolution :

```
arrivant = maisons desservies + pluie de la saison
traité   = min(arrivant, capacité de la station)
surplus  = arrivant − traité
marge    = capacité − traité

si le bassin a une route valide jusqu'à la station :
    le bassin absorbe   min(surplus, place restante)
    puis relâche        min(marge, niveau)
sinon :
    le surplus est perdu, sans un mot
```

Surplus et marge ne sont jamais tous les deux non nuls : une saison remplit ou vide, jamais les
deux.

**Le bassin obéit exactement à la règle du puzzle.** Il ne sert que si `FlowSolver` trouve un
chemin à profondeur non décroissante de lui jusqu'à la station, avec des tuyaux ni gelés, ni
bouchés, ni trop usés. C'est le même parcours que pour une maison, sur un nœud de plus.

### Les nombres

| Réglage | Valeur | Pourquoi |
|---|---|---|
| Une maison desservie | 1 par saison | ses eaux usées |
| Pluie, printemps | 2 | |
| Pluie, été | 0 | l'été ne fait toujours rien |
| Pluie, **automne** | **8** | c'est la saison qui met le réseau à l'épreuve |
| Pluie, hiver | 1 | |
| Capacité de la station | 8 par saison | |
| Capacité du bassin | 10 | |

Une année, cinq maisons desservies, bassin relié, en partant de zéro :

| Saison | Arrivant | Traité | Bassin |
|---|---|---|---|
| Printemps | 7 | 7 | 0 |
| Été | 5 | 5 | 0 |
| **Automne** | **13** | 8 | **+5** |
| Hiver | 6 | 6 | 3 |
| Printemps | 7 | 7 | 2 |
| Été | 5 | 5 | **0** |

**Le cycle est stable** : la pointe monte à cinq, l'été la ramène à zéro. Un réseau entretenu
tient indéfiniment. **Un hiver qui gèle la route du bassin l'empêche de se vider**, et le
niveau monte d'année en année : c'est l'entretien qui garde le bassin bas, exactement comme il
garde les maisons desservies.

Conséquence assumée à signaler : moins de maisons reliées, c'est moins d'eau à traiter, donc un
bassin plus tranquille. L'incitation est perverse sur le papier. En pratique elle ne joue pas :
le but affiché reste les cinq gouttes, et rien ne récompense un bassin vide.

## 2. Où il est

**Bassin en (10, 13), profondeur 2**, dans une chambre de trois cases sur trois déjà creusée,
de (9, 12) à (11, 14).

Emplacement choisi par calcul avant écriture, comme les crêtes de la phase 4 :

- **profondeur 2**, donc il doit descendre vers la station qui est à 3 ;
- **chemin de 25 cases** à profondeur non décroissante jusqu'à la station, vérifié ;
- **entièrement sous de l'herbe** : ni maison, ni atelier, ni station au-dessus ;
- **à deux cases de la galerie verticale existante en x = 8**, celle qui descend vers la
  station. Le creusement supplémentaire est donc minime : l'essentiel du travail est de poser
  du tuyau le long d'une galerie déjà ouverte. C'est un second chantier abordable, pas un
  deuxième réseau.

Creuser une chambre de plus ne peut rien casser : cela n'ajoute que des cases praticables, et
le plan des profondeurs ne bouge pas d'un caractère. La solvabilité des cinq maisons est
inchangée par construction.

## 3. Un type de nœud de plus, et il faut ton accord

`PipeNode.NodeType` est fixé par CLAUDE.md : `Junction, HouseConnection, Manhole, PlantInlet,
FountainInlet`. Le bassin en demande un sixième, **`ReserveInlet`, ajouté à la fin de la
liste**.

- Ajouté **à la fin** et non inséré : les scènes sérialisent l'enum par son rang, et insérer
  décalerait les nœuds existants.
- **Aucun effet sur les sauvegardes** : le fichier ne contient que des cases, jamais de type.
  Les nœuds imposés viennent de la scène, ceux du joueur sont toujours des `Junction`.

C'est le seul écart au modèle de CLAUDE.md, et je préfère le poser noir sur blanc plutôt que
le glisser dans le lot. **Accepté le 3 septembre 2026.**

## 4. Les fichiers

**Créés**

- `Scripts/Buildings/WaterReserve.cs` : le niveau, la capacité, et le bilan d'une saison. Vit
  dans la scène Underground, avec le réseau. Il porte aussi son propre affichage : cinq
  images pour une cuve, séparer le modèle de la vue serait de la cérémonie ici, là où
  `PipeNetworkView` en valait la peine pour des centaines de tuiles.
- `Scripts/Buildings/TreatmentPlant.cs` : `capacityPerSeason`. La station existait comme
  sprite et comme nœud depuis la phase 1 ; elle devient enfin un objet, et remplit une des
  cinq cases `Buildings/` de CLAUDE.md.

**Modifiés** — `PipeNode` (`ReserveInlet`), `FlowSolver` (le parcours du bassin et
`IsReserveConnected`), `SeasonDefinition` (`rainVolume`), `SeasonSystem` (le bilan après la
résolution), `SaveData` et `SaveSystem` (`reserveLevel`), `ScriptableObjectSetup` (la pluie des
quatre saisons), `UndergroundLayout` (la chambre et le marqueur `R`), `UndergroundSceneBuilder`
(le nœud permanent et la cuve), `PlaceholderArtGenerator` (cinq images de cuve).

**Référence d'assembly, règle 9 :** rien de neuf. Aucun type URP n'est touché, et
`SousLaVille.Runtime.asmdef` porte déjà tout ce qu'il faut.

## 5. Ce que l'on ne fait pas

- **Pas de débordement visible.** Quand le bassin est plein ou absent, le surplus disparaît
  sans un mot. Le rendre spectaculaire est le sujet entier de la phase 10, et l'avancer ici
  ferait deux phases à moitié.
- **Pas de jauge au HUD.** Décision 3.
- **Pas de second bassin.** Un seul emplacement ; s'il en faut d'autres, ce sera une ligne dans
  le plan du sous-sol.

## Décisions techniques signalées

1. **Le bilan est quatre additions par saison**, jamais par frame. C'est la seule façon
   d'ajouter du volume sans trahir « un graphe, pas une simulation de fluide ».
2. **Le bassin est un nœud comme un autre pour le solveur.** Il obéit à la règle de profondeur,
   au gel, aux bouchons et à l'usure sans une ligne de code de plus : c'est le même parcours.
3. **La station devient un objet.** `capacityPerSeason` n'a pas sa place dans le système des
   saisons : c'est une propriété de la station.
4. **La pluie est une donnée de saison**, comme le gel et l'usure. Changer l'équilibre ne
   demandera pas de recompiler.
5. **Le niveau est un entier, pas un flottant.** Cinq images, dix crans : un entier se lit dans
   l'inspecteur, se sauvegarde sans surprise d'arrondi, et se vérifie exactement.
6. **`CurrentVersion` reste à 1.** Un champ de plus, encore.

## Vérification de fin de phase

1. Les menus, puis construction des scènes.
2. Console relue par le pont MCP : zéro erreur **et** zéro warning.
3. Plan du sous-sol relu par script : la chambre est creusée, le nœud `ReserveInlet` est en
   (10, 13) à la profondeur 2, **les cinq alcôves et la station n'ont pas bougé**, et les cinq
   maisons gardent chacune un chemin valide.
4. Les cinq images de cuve comparées à leur taille réelle : les niveaux se distinguent.
5. Play depuis Boot, avec l'horloge accélérée :
   - **bassin non relié** : le niveau reste à zéro quoi qu'il arrive, et rien ne se casse ;
   - **bassin relié**, réseau complet : l'année suit le tableau ci-dessus, saison par saison,
     0 / 0 / 5 / 3 / 2 / 0. Le cycle est stable sur trois années ;
   - **la cuve montre son niveau** et change d'image quand il change ;
   - **un hiver qui gèle la route du bassin l'empêche de se vider**, et le niveau monte ;
   - réparer la route le fait se vider de nouveau ;
   - **le bassin plein** : le surplus est perdu, aucune erreur, aucun message ;
   - **le solveur tourne toujours une fois par saison**, compteur à l'appui ;
   - **quitter et relancer** : le niveau est retrouvé ;
   - **une partie de la phase 7 se relit sans erreur**, bassin à zéro ;
   - console **entièrement vide** sur une session complète.
6. Captures : la cuve vide, à moitié, pleine, et la chambre reliée.
7. PROGRESS.md à jour, commit `Phase 8 - la réserve d'eau`, puis arrêt.
