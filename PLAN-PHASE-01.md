# Plan de la phase 1 — Le personnage et la surface

Validé le 2 septembre 2026. Voir CLAUDE.md pour les contraintes du projet et PROGRESS.md
pour l'état d'avancement.

## Contexte

La phase 0 est terminée et committée (`71b302d`) : arborescence, deux assemblies,
`GameManager`, `SceneRouter`, générateurs de scènes sous le menu `Sous La Ville/`, dix Sorting
Layers répartis en deux familles, et l'asset `.inputactions` dont le wrapper
`Assets/Scripts/Core/SousLaVilleInputActions.cs` est généré.

La phase 1 rend le village jouable : une carte de 40x30 tuiles générée par script, un
personnage qui marche aux flèches, une caméra qui le suit. **Critère de fin : je peux marcher
dans le village avec des carrés de couleur.**

Décisions validées, à ne pas rediscuter : déplacement case par case, plan du village écrit à la
main, placeholders en PNG générés par script, personnage unique dans la scène Persistent.

## 1. Art placeholder — `Assets/Editor/ProjectSetup/PlaceholderArtGenerator.cs`

`[MenuItem("Sous La Ville/Générer l'art placeholder")]`. Écrit de vrais fichiers PNG puis règle
leur import : PPU 16, filtre `Point`, `Uncompressed`, `spriteImportMode = Single`.

| Fichier | Taille | Couleur | Rôle |
|---|---|---|---|
| `Art/Tiles/tile_grass.png` | 16x16 | `#4E9A3E` | herbe |
| `Art/Tiles/tile_path.png` | 16x16 | `#C8A96E` | chemin |
| `Art/Tiles/tile_park.png` | 16x16 | `#B8B8B0` | dalle du parc |
| `Art/Tiles/tile_plant_floor.png` | 16x16 | `#6E7B8B` | sol de la station |
| `Art/Tiles/tile_hedge.png` | 16x16 | `#1F5C2E` | haie, bloquant |
| `Art/Tiles/tile_plant_wall.png` | 16x16 | `#3A6EA5` | bâtiment de la station, bloquant |
| `Art/Sprites/manhole.png` | 16x16 | `#3C3C3C` + liseré `#8A8A8A` | bouche d'égout |
| `Art/Sprites/player.png` | 16x24 | corps `#E05A2B`, tête `#F2A07B` | personnage |

Chaque tuile reçoit un liseré 1 px plus sombre : les carrés restent distincts les uns des
autres même côte à côte, ce qui aide à lire la grille.

Le sprite du personnage porte un **repère de direction** de 2 px, indispensable dès la phase 3
où Espace agit sur la case regardée. Pivot personnalisé `(0.5, 1/3)` : le pivot tombe au centre
des 16 px du bas, donc le transform se pose exactement au centre de la case et la tête dépasse.

Le générateur crée aussi les six assets `UnityEngine.Tilemaps.Tile` correspondants dans
`Art/Tiles/` (`Tile_Grass.asset`, etc.), `colliderType = None` : les collisions sont logiques,
pas physiques.

## 2. Le plan du village — `Assets/Editor/SceneBuilders/VillageLayout.cs`

Une carte ASCII de 30 lignes de 40 caractères, dans l'assembly Editor uniquement. Le runtime ne
la lit jamais, il interroge les tilemaps.

```
.  herbe            #  chemin           P  dalle du parc
S  sol station      H  haie (bloquant)  B  bâtiment station (bloquant)
M  bouche d'égout   X  départ du joueur
```

Composition : bordure de haies sur tout le pourtour, une boucle de chemins qui dessert la place
du parc au centre, l'enceinte de la station d'épuration dans un coin, et les trois bouches
réparties le long des chemins, éloignées les unes des autres. `M` et `X` sont des marqueurs :
le builder peint du chemin dessous et pose un GameObject par-dessus.

## 3. Scripts runtime

**`Assets/Scripts/World/GridMap.cs`** — classe abstraite, base commune à `SurfaceMap` et au
futur `UndergroundMap`. Porte les conversions case vers monde, `CellBounds`, `WorldBounds`, et
déclare `abstract bool IsWalkable(Vector2Int)`. Fichier hors liste CLAUDE.md, justifié plus bas.

**`Assets/Scripts/World/SurfaceMap.cs`** — `GridMap` du village. Deux références sérialisées
vers `Tilemap_Ground` et `Tilemap_Blocking`. `IsWalkable(cell)` vaut
`CellBounds.Contains(cell) && !blocking.HasTile(cell)`. Aucune donnée dupliquée : la carte de
collision, c'est la tilemap.

**`Assets/Scripts/Player/PlayerController.cs`** — déplacement case par case.

- Consomme `SousLaVilleInputActions`, carte `Gameplay`, action `Move`. `Enable` dans
  `OnEnable`, `Disable` dans `OnDisable`.
- Résout sa carte au `Start` via `FindFirstObjectByType<GridMap>()` : la couche inactive étant
  éteinte, c'est toujours la bonne qui répond, y compris après la phase 2.
- État : `currentCell`, `targetCell`, `stepProgress`, `facing`.
- À l'arrêt seulement, lit l'input, garde **l'axe dominant** (X prioritaire à égalité) : aucune
  diagonale ne peut sortir du composite, quelle que soit la façon dont l'enfant appuie.
- `tilesPerSecond = 5`, vitesse constante, pas d'accélération. Flèche maintenue égale pas
  enchaînés.
- Case bloquée : le personnage **tourne vers elle sans avancer**. Pas de son d'échec, pas de
  recul.
- Expose `Cell`, `Facing`, `FacingCell` pour les phases 2 et 3.
- Aucun `Rigidbody2D`, aucun collider. Le déplacement case par case rend la physique inutile.

**`Assets/Scripts/Core/CameraFollow.cs`** — suivi en `LateUpdate`, `z` conservé à -10.

- Clamp sur `WorldBounds` de la carte active, réduite de la demi-vue : 10 unités en largeur,
  5,625 en hauteur pour 320x180 à PPU 16. Le joueur ne voit jamais le vide autour du village.
- Aucun lissage. Un lissage se bat avec le pixel snapping et produit du tremblement. À
  rediscuter une fois le rendu vu.
- Fichier hors liste CLAUDE.md, comme `Bootstrapper` en phase 0.

## 4. Scripts Editor étendus

**`SurfaceSceneBuilder.cs`** produit désormais :

```
Surface                     (racine, + SurfaceMap)
├── Global Light 2D         (déjà en place, vise la famille Surface_*)
├── Grid                    (cellSize 1,1,0)
│   ├── Tilemap_Ground      sorting layer Surface_Ground, order 0
│   └── Tilemap_Blocking    sorting layer Surface_Ground, order 1
├── Manholes/Manhole_01..03 SpriteRenderer, sorting layer Surface_Entities
└── TreatmentPlant          SpriteRenderer, sorting layer Surface_Entities
```

**`PersistentSceneBuilder.cs`** gagne un objet `Player` (SpriteRenderer sur
`Surface_Entities`, `PlayerController`) placé sur la case `X` du plan, et câble
`CameraFollow.target` sur lui.

Attention, le personnage vit dans Persistent alors que son Sorting Layer appartient à la
famille Surface. C'est correct tant qu'il est en surface ; la phase 2 devra basculer son
Sorting Layer en `Underground_Entities` en même temps que la couche.

Aucun type URP nouveau dans ces scripts : la règle 9 de CLAUDE.md est déjà satisfaite par la
référence `Unity.RenderPipelines.Universal.2D.Runtime`.

## Décisions techniques signalées

1. **`GridMap` et `CameraFollow` en plus des fichiers listés dans CLAUDE.md.** `GridMap` donne
   au joueur et à la caméra un type commun à interroger, ce qui évite de réécrire leur
   résolution de carte en phase 2 quand `UndergroundMap` arrivera. `CameraFollow` n'est listé
   nulle part alors que la phase l'exige explicitement ; il rejoint `Core/` avec les autres
   services globaux.
2. **Pas de prefab pour le personnage.** Le builder le crée directement dans Persistent. Un
   prefab deviendra utile quand il y aura de quoi le varier, pas avant.
3. **Aucune physique 2D.** Ni `Rigidbody2D`, ni `TilemapCollider2D`, ni `CompositeCollider2D`.
   Les collisions sont une lecture de tilemap. Plus prévisible pour un enfant, et cela tient
   les 60 fps sans effort.
4. **Les bouches et la station sont déjà des GameObjects**, pas des tuiles peintes. La phase 2
   n'aura plus qu'à leur ajouter `ManholePortal` sans retoucher la carte.
5. **Priorité à l'axe X en cas d'appui simultané.** Choix arbitraire, mais il faut trancher :
   deux flèches enfoncées ne doivent jamais produire de diagonale ni d'immobilité.

## Vérification de fin de phase

1. Dans l'ordre : `Générer l'art placeholder`, puis `Construire toutes les scènes`. L'art doit
   exister avant la construction de Surface, qui référence les assets `Tile`. Le second menu
   crée déjà les Sorting Layers.
2. Console Unity : zéro erreur, zéro warning issu de nos scripts.
3. Play depuis `Boot`. À vérifier :
   - le village s'affiche, herbe, chemins, place du parc, station, trois bouches distinctes ;
   - les flèches déplacent le personnage case par case, à vitesse constante, sans diagonale ;
   - deux flèches enfoncées ensemble ne bloquent ni ne font glisser en biais ;
   - les haies et le bâtiment arrêtent le personnage, qui se contente de se tourner ;
   - la caméra suit et s'arrête net aux bords du village, sans jamais montrer de vide ;
   - le rendu reste net, sans pixels déformés, en redimensionnant la fenêtre Game.
4. Mise à jour de PROGRESS.md, puis commit `Phase 1 - le personnage et la surface`.
5. **Arrêt.** Résumé, puis attendre validation avant la phase 2 (règle 1).
