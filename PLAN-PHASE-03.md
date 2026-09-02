# Plan de la phase 3 — Creuser et poser

Validé le 2 septembre 2026. Voir CLAUDE.md pour les contraintes du projet et PROGRESS.md
pour l'état d'avancement.

## Contexte

Phase 2 committée (`e881b0a`). Acquis directement réutilisés :

- `PlayerInteractor` est déjà le seul endroit où Espace agit, et il agit sur la case occupée.
  La case regardée, `FacingCell`, est restée libre exprès.
- `UndergroundMap.Ground` et `.Blocking` sont publics. Creuser, c'est retirer une tuile
  bloquante et repeindre le sol. Aucune donnée de collision à tenir à jour.
- Le picto d'action au-dessus de la tête existe, il suffit de lui donner d'autres images.
- `Assets/Scripts/Network/` et `Assets/ScriptableObjects/` sont vides et attendent cette phase.

**Critère de fin : je regarde un mur de terre, j'appuie sur Espace, la galerie s'ouvre. Je
regarde une galerie vide, j'appuie, un tuyau se pose et se raccorde tout seul à ses voisins.
J'appuie encore, il s'enlève. Rien ne peut être cassé pour de bon.**

L'eau ne coule pas encore : `FlowSolver` est la phase 4. La phase 3 construit le graphe, pas
son parcours.

## Décisions de design validées

1. **La profondeur est une propriété de la case, peinte dans la carte**, pas un choix au
   moment de creuser. Trois zones concentriques autour de la station : poche profonde,
   couronne intermédiaire, lointain peu profond. Le puzzle devient : trouver un chemin qui ne
   remonte jamais en allant vers la station. Laisser le joueur creuser plus profond en
   insistant a été écarté : sans coût, creuser au maximum partout résout tout et le puzzle
   disparaît.
2. **Le même Espace pose et retire.** Réversible, sans punition. Un enfant qui défait son
   réseau par mégarde le refait en trois appuis ; un réseau qu'on ne peut plus modifier serait
   pire.
3. **Les maisons restent en phase 4.** Comparaison faite : la phase 3 pèse huit fichiers
   runtime, trente fichiers d'art et la refonte du plan du sous-sol ; les maisons en phase 4
   coûtent `HouseSpawner`, les nœuds `HouseConnection`, `FlowSolver` et le rendu de
   l'écoulement, soit cinq fichiers sur un graphe déjà construit. La phase 4 reste la plus
   légère. Et sans écoulement, une maison ne montre rien.
4. **Un cadre sur la case regardée.** C'est la première fois qu'Espace agit à distance ; il
   faut voir où.

## 1. Art placeholder — `PlaceholderArtGenerator.cs` (modifié)

| Fichier | Taille | Rôle |
|---|---|---|
| `tile_earth_1/2/3.png` | 16x16 | terre pleine, une nuance par profondeur |
| `tile_tunnel_1/2/3.png` | 16x16 | galerie creusée, même code de nuances |
| `pipe_00..15.png` | 16x16 x16 | canalisation, une image par combinaison de raccords N/E/S/O |
| `player_down/up/left/right.png` | 16x24 | les quatre sprites décidés le 2 septembre 2026 |
| `picto_dig.png` | 16x16 | pelle, « ici on creuse » |
| `picto_pipe.png` | 16x16 | tuyau, « ici on pose » |
| `picto_remove.png` | 16x16 | « ici on enlève » |
| `cursor_target.png` | 16x16 | cadre de la case regardée |

Le brun s'assombrit avec la profondeur, et la galerie vire au gris froid au plus profond : la
nuance se lit sans légende. Les seize canalisations sont dessinées par une seule fonction qui
lit un masque de quatre bits, pas seize dessins à la main.

`tile_earth.png`, `tile_tunnel.png` et `player.png` de la phase 2 sont supprimés au profit des
versions numérotées et des quatre directions. Les assets `Tile` correspondants suivent.

## 2. Le plan du sous-sol — `UndergroundLayout.cs` (modifié)

La carte de la phase 2 ne bouge pas d'un caractère. Un **second tableau** de trente lignes de
quarante chiffres vient à côté, qui donne la profondeur de chaque case, `1`, `2` ou `3`. Deux
plans superposés plutôt qu'un alphabet à neuf lettres : chacun reste lisible.

Zones, par distance de Manhattan à la station (6, 25) : profondeur 3 jusqu'à 5 cases,
profondeur 2 jusqu'à 20, profondeur 1 au-delà. Les trois bouches tombent alors en profondeur 2
pour la plus proche et 1 pour les deux autres ; la station seule est au fond.

`ValidateAgainstVillage()` gagne deux contrôles : toute case porte une profondeur entre 1 et 3,
et la station est bien à la profondeur maximale de la carte.

## 3. Scripts runtime créés

**`Scripts/Network/PipeNode.cs`** — `[Serializable]`, `Vector2Int gridPos`, `int depth`,
`NodeType type`, avec l'énumération de CLAUDE.md (`Junction`, `HouseConnection`, `Manhole`,
`PlantInlet`, `FountainInlet`). Une classe sérialisable et non un `MonoBehaviour` : la phase 6
doit pouvoir l'écrire en JSON telle quelle.

**`Scripts/Network/PipeSegment.cs`** — `[Serializable]`, `nodeA`, `nodeB`, `PipeType`,
`float condition = 1f`, `bool isFrozen`, `bool isClogged`. Les trois derniers ne servent qu'à
partir de la phase 5 ; ils sont là parce que le modèle de CLAUDE.md les impose et que les
ajouter plus tard casserait les sauvegardes.

**`Scripts/Network/PipeType.cs`** — `ScriptableObject` : nom affiché, teinte, résistance au
gel, vitesse d'usure. Un seul asset en phase 3,
`Assets/ScriptableObjects/PipeType_Standard.asset`.

**`Scripts/Network/PipeNetwork.cs`** — le graphe, posé sur la racine de l'Underground.

- Dictionnaire de nœuds par case, liste d'adjacence, liste de segments.
- `PlacePipe(cell)` : crée le nœud à la profondeur lue sur la carte **et le raccorde
  automatiquement aux voisins qui portent déjà un nœud**. Aucun geste de raccordement séparé.
- `RemovePipe(cell)` : retire le nœud et ses segments. Refuse sur un nœud permanent.
- `event Action Changed`, que la phase 4 branchera sur le `FlowSolver`. Le solveur ne tournera
  que là-dessus et aux ticks de saison, jamais par frame.
- `fixedNodes` sérialisé : les nœuds que le monde impose, la station pour l'instant, recréés
  au réveil.

**`Scripts/Network/PipeNetworkView.cs`** — le rendu. Une `Tilemap_Pipes` sur
`Underground_Pipes` ; à chaque changement, chaque nœud reçoit la tuile correspondant à son
masque de voisins. Un tuyau isolé, un coude, un T et un croisement se distinguent donc sans
code de dessin particulier.

**`Scripts/World/TargetCursor.cs`** — le cadre sur la case regardée, enfant du personnage,
visible sous terre seulement.

## 4. Scripts runtime modifiés

**`UndergroundMap.cs`** — tableau `depths` sérialisé rempli par le builder, `DepthAt(cell)`,
et `Dig(cell)` qui retire la tuile bloquante et peint la galerie de la bonne nuance. `Dig`
rend `false` si la case n'était pas de la terre : aucun échec, juste rien qui se passe.

**`PlayerInteractor.cs`** — Espace devient contextuel, dans cet ordre :

1. un passage sur la **case occupée** : descendre ou remonter, comme en phase 2 ;
2. sinon, sous terre, **case regardée** en terre pleine : creuser ;
3. sinon, case regardée en galerie sans tuyau : poser ;
4. sinon, case regardée en galerie avec tuyau : enlever.

Une seule touche, aucun mode, aucune combinaison. Le picto au-dessus de la tête annonce
toujours celle des quatre actions qui va se produire, ou disparaît s'il n'y en a aucune.

**`PlayerController.cs`** — quatre sprites sérialisés, choisis d'après `Facing`. Expose aussi
`Map`, dont l'interacteur et le curseur ont besoin.

## 5. Scripts Editor modifiés

- **`UndergroundSceneBuilder`** : peint les nuances de profondeur, bake le tableau des
  profondeurs, ajoute `Tilemap_Pipes`, `PipeNetwork` et `PipeNetworkView`, et déclare le nœud
  permanent `PlantInlet` sous la station.
- **`PersistentSceneBuilder`** : câble les quatre sprites du personnage, l'enfant `Cursor` et
  les trois nouveaux pictos d'action.
- **`PlaceholderArtGenerator`** : l'art ci-dessus, et le ménage des trois fichiers remplacés.
- **Nouveau `Sous La Ville/Créer les ScriptableObjects`** : crée `PipeType_Standard` s'il
  manque. `BuildAllScenes` refuse de construire sans lui, comme il refuse déjà sans l'art.

Références d'assembly, règle 9 : rien de nouveau. Aucun type URP, aucun type UI supplémentaire.
`Unity.2D.Tilemap.Extras` est déjà référencé et suffit.

## Décisions techniques signalées

1. **Le raccordement est automatique.** Deux tuyaux voisins sont reliés, point. Un geste de
   raccordement séparé serait une deuxième touche.
2. **Le tuyau ne bloque pas le passage.** On marche dessus. Un enfant coincé derrière sa propre
   construction, c'est un échec puni déguisé.
3. **Tuyaux illimités.** `PipeFactory` est la phase 9 ; c'est elle qui introduira une
   contrainte, douce.
4. **Un seul `PipeType`.** Les variantes n'ont d'intérêt qu'avec le gel, en phase 5.
5. **La profondeur est stockée en tableau sérialisé**, pas relue dans une tilemap. C'est une
   donnée de carte statique, pas une donnée de collision ; la tuile n'en est que l'affichage.
6. **Le rendu du réseau est redessiné en entier à chaque changement.** Quelques centaines de
   cases, et seulement sur action du joueur. Le calcul incrémental viendra s'il se voit.
7. **Seule la station est un nœud permanent.** Les échelles restent des cases ordinaires ; le
   type `Manhole` du modèle attendra d'avoir un usage réel.
8. **Rien n'est sauvegardé.** Ce qui est creusé est perdu au relancement jusqu'à la phase 6.
   C'est le premier moment du projet où le joueur perd du travail : la phase 6 devient
   prioritaire juste après la 4.
9. **Creuser est instantané.** Pas de maintien, pas de barre de progression : CLAUDE.md
   interdit tout timing serré.

## Vérification de fin de phase

1. `Créer les ScriptableObjects`, `Générer l'art placeholder`, `Construire toutes les scènes`.
2. Console relue par le pont MCP : zéro erreur **et** zéro warning.
3. Scène relue par script : profondeurs cohérentes sur les 1200 cases, station à la profondeur
   maximale, `PipeNetwork` avec son seul nœud permanent.
4. Play depuis Boot, par injection clavier :
   - creuser un mur, la case devient praticable et se peint à la bonne nuance ;
   - y marcher ;
   - poser trois tuyaux en ligne : deux segments créés, la tuile de raccordement change au bon
     moment ;
   - poser un tuyau en T, vérifier les trois segments ;
   - enlever celui du milieu : deux segments disparaissent, les nœuds voisins restent ;
   - le nœud de la station refuse d'être enlevé, sans message ni sanction ;
   - le picto annonce toujours la bonne action, et le personnage regarde dans les quatre
     directions ;
   - descendre et remonter fonctionnent toujours, la case occupée gardant la priorité.
5. Captures d'écran des trois zones de profondeur et d'un réseau posé.
6. PROGRESS.md à jour, commit `Phase 3 - creuser et poser`, puis arrêt.
