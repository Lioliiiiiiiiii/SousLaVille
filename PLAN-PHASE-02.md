# Plan de la phase 2 — Le portail

Validé le 2 septembre 2026. Voir CLAUDE.md pour les contraintes du projet et PROGRESS.md
pour l'état d'avancement.

## Contexte

Phases 0 et 1 terminées et committées (`4106d43`). Acquis réutilisés tels quels :

- `SceneRouter.SetActiveLayer` bascule déjà les deux couches résidentes et lève `LayerChanged`.
- `PlayerController` et `CameraFollow` re-résolvent leur `GridMap` dès que celle qu'ils tenaient
  s'éteint. Aucune ligne à changer côté résolution de carte.
- Les objets `Manhole_01..03` et `TreatmentPlant` existent en scène, il ne reste qu'à leur poser
  un composant.
- L'action `Interact` (Espace, plus `buttonSouth` en option) existe dans le wrapper généré,
  jamais consommée jusqu'ici.

**Critère de fin de phase : marcher sur une bouche, appuyer sur Espace, se retrouver sous terre
à la même case, marcher dans les galeries, remonter par une autre bouche. Jamais coincé,
jamais perdu.**

## Décisions de design validées

1. **Sous-sol de départ : terre pleine, plus un réseau de galeries déjà creusées.** Une salle
   3x3 sous chaque bouche et sous la station, reliées par des couloirs d'une case de large.
   Soixante-quatorze cases praticables sur mille deux cents. Le sous-sol doit être praticable
   pour que le portail se vérifie dès cette phase, et très majoritairement plein pour que la
   phase 3 ait de quoi creuser. Un seul fichier ASCII, modifiable à tout moment.
2. **Un repère de couche, pas de mini-carte**, plus un picto d'action au-dessus de la tête
   quand on se tient sur une bouche. Une mini-carte serait vide de sens tant que le réseau
   n'existe pas, et demanderait une légende, donc du texte.
3. **Descendre remet aux mêmes coordonnées.** Les deux cartes font 40x30 et partagent le même
   repère : l'échelle est exactement sous la bouche. « Je suis juste en dessous » est un modèle
   mental qui se construit tout seul, et qui servira quand il faudra relier une maison à la
   station.
4. **Fondu au noir de 0,15 s dans chaque sens.** Une bascule strictement instantanée brouille
   la compréhension du changement de lieu ; plus long serait une attente.

## 1. Art placeholder — `Assets/Editor/ProjectSetup/PlaceholderArtGenerator.cs` (modifié)

Sept PNG de plus, mêmes réglages d'import (PPU 16, Point, Uncompressed, FullRect).

| Fichier | Taille | Couleur | Rôle |
|---|---|---|---|
| `Art/Tiles/tile_earth.png` | 16x16 | `#4A3728` | terre pleine, bloquant |
| `Art/Tiles/tile_tunnel.png` | 16x16 | `#8A7A66` | sol de galerie creusée |
| `Art/Sprites/ladder.png` | 16x16 | `#C9A227`, barreaux sombres | échelle de remontée |
| `Art/Pictos/picto_surface.png` | 32x32 | soleil sur fond ciel | repère « je suis en haut » |
| `Art/Pictos/picto_underground.png` | 32x32 | échelle sur fond terre | repère « je suis en bas » |
| `Art/Pictos/picto_down.png` | 16x16 | flèche blanche vers le bas | « ici on descend » |
| `Art/Pictos/picto_up.png` | 16x16 | flèche blanche vers le haut | « ici on remonte » |

Plus deux assets `Tile` : `Tile_Earth.asset` et `Tile_Tunnel.asset`, `colliderType = None`.
`AreAssetsPresent()` couvre les nouveaux fichiers, sinon `BuildAllScenes` accepterait de
construire un sous-sol sans tuiles.

## 2. Le plan du sous-sol — `Assets/Editor/SceneBuilders/UndergroundLayout.cs` (créé)

Même forme que `VillageLayout` : 30 lignes de 40 caractères, assembly Editor seulement, 40x30
strictement aligné case pour case sur le village.

```
#  terre pleine (bloquant)   .  galerie creusée
E  échelle vers la surface   T  arrivée sous la station
```

Tracé, en cases (x, y) :

- Salle 3x3 autour de chaque marqueur : `T` en (6, 25), `E` en (8, 19), (20, 10) et (33, 5).
- Station vers bouche 1 : x = 6 de y = 24 à y = 19, puis y = 19 de x = 6 à x = 8.
- Bouche 1 vers bouche 2 : x = 8 de y = 19 à y = 10, puis y = 10 de x = 8 à x = 20.
- Bouche 2 vers bouche 3 : x = 20 de y = 10 à y = 5, puis y = 5 de x = 20 à x = 33.
- Tout le reste est plein.

**Garde-fou :** `ValidateAgainstVillage()` vérifie que chaque `E` tombe exactement sur un `M` du
village, que `T` tombe sur le `T` du village, et qu'il n'en manque aucun. Une carte mal alignée
devient une erreur de console explicite, jamais un portail silencieusement mort. C'est le seul
vrai risque de deux cartes écrites à la main.

## 3. Scripts runtime créés

**`Assets/Scripts/World/UndergroundMap.cs`** — `GridMap` du sous-sol. Deux tilemaps,
`Tilemap_Ground` décoratif et `Tilemap_Blocking` qui porte la terre pleine.
`IsWalkable(cell)` vaut `Contains(cell) && !blocking.HasTile(cell)`. Expose `Ground` et
`Blocking` en public : la phase 3 creusera en retirant une tuile bloquante et en peignant du
sol de galerie.

**`Assets/Scripts/World/ManholePortal.cs`** — un composant, posé aussi bien sur les bouches en
surface que sur les échelles en dessous.

- `Vector2Int cell`, `GameLayer layer`, `GameLayer destinationLayer`, `Vector2Int destinationCell`.
- Registre statique, `Register` en `OnEnable` et `Unregister` en `OnDisable`.
  `ManholePortal.Find(layer, cell)` ne rend qu'un portail de la couche demandée : les deux
  couches partagent le même repère, une bouche et son échelle occupent la même case.

**`Assets/Scripts/Player/PlayerInteractor.cs`** — la touche unique.

- Sa propre instance de `SousLaVilleInputActions`, lue par `Interact.WasPressedThisFrame()`.
  Un appui, jamais un maintien : impossible de faire du yo-yo en gardant Espace enfoncé.
- Cherche un portail **sur la case occupée**, pas sur la case regardée. Marcher dessus puis
  appuyer, c'est le geste le plus simple à six ans. La case regardée est réservée au creusement
  de la phase 3, qui viendra dans ce même fichier.
- Allume le picto d'action au-dessus de la tête dès que la case porte un portail.
- Désactive `PlayerController` le temps du voyage : une flèche maintenue pendant le fondu ne
  fait pas partir le personnage de travers à l'arrivée.

**`Assets/Scripts/Core/GameSortingLayers.cs`** — les dix noms de Sorting Layers en constantes
runtime, plus `EntitiesFor(GameLayer)`. `SortingLayerSetup` et les builders les consomment au
lieu de leurs chaînes en dur. Source unique : un renommage ne peut plus laisser un objet sur
`Default`, c'est-à-dire noir et sans la moindre erreur, le piège déjà rencontré en phase 1.

**`Assets/Scripts/UI/ScreenFader.cs`** — fondu au noir. Une `Image` plein écran, alpha animée en
`Awaitable` sur temps non mis à l'échelle, 0,15 s par sens.

**`Assets/Scripts/UI/LayerIndicator.cs`** — le repère de couche. S'abonne à
`SceneRouter.LayerChanged`, échange le sprite de son `Image` entre les deux pictos. Zéro texte.

## 4. Scripts runtime modifiés

**`World/GridMap.cs`** — gagne `public abstract GameLayer Layer { get; }`. C'est ce qui permet
au personnage de savoir sur quelle famille de Sorting Layers il doit rendre, sans que personne
n'interroge le `SceneRouter`.

**`World/SurfaceMap.cs`** — implémente `Layer => GameLayer.Surface`.

**`Core/SceneRouter.cs`** — une méthode :
`public async Awaitable TravelAsync(GameLayer target, Action whileBlack = null)`.
Garde `IsBusy`, fondu au noir, `SetActiveLayer`, appel de `whileBlack` écran noir, fondu
inverse. Le routeur ne connaît toujours pas le personnage : c'est l'interacteur qui passe le
replacement en callback.

**`Player/PlayerController.cs`** — `Teleport(Vector2Int cell)` publique : re-résout la carte,
replace le personnage au centre de la case, remet à zéro `stepProgress`, `isMoving` et
`targetCell`. Et surtout, à chaque changement de carte, **bascule le Sorting Layer de tous ses
`SpriteRenderer`** sur `GameSortingLayers.EntitiesFor(map.Layer)`. C'est le piège identifié en
fin de phase 1 : sans cette bascule, le personnage descend et devient noir, la lumière globale
de l'Underground ne portant pas sur la famille Surface.

## 5. Scripts Editor modifiés

**`UndergroundSceneBuilder.cs`** produit désormais :

```
Underground                  (racine, + UndergroundMap)
├── Global Light 2D          intensité 0,8, cf. décision 4
├── Grid                     (cellSize 1,1,0)
│   ├── Tilemap_Ground       Underground_Ground, order 0
│   └── Tilemap_Blocking     Underground_Ground, order 1
├── Ladders/Ladder_01..03    SpriteRenderer + ManholePortal vers Surface
└── PlantOutlet              SpriteRenderer + ManholePortal vers Surface
```

**`SurfaceSceneBuilder.cs`** — ajoute un `ManholePortal` sur chaque `Manhole_xx` et sur
`TreatmentPlant`, destination `Underground`, case identique. Appariement par case, jamais par
nom d'objet.

**`PersistentSceneBuilder.cs`** — ajoute `PlayerInteractor` au Player, un enfant `Prompt`
(le picto d'action, éteint par défaut), et le HUD :

```
HUD_Canvas                   Screen Space Overlay, CanvasScaler 320x180, pixelPerfect
├── Fader                    Image noire plein écran, raycast off, alpha 0
└── LayerIndicator           Image 32x32, coin haut gauche, marge 4 px
```

Pas d'`EventSystem` : rien n'est cliquable, le HUD est purement informatif.

**`SortingLayerSetup.cs`** — ses deux tableaux pointent vers `GameSortingLayers`.

## 6. Références d'assembly, règle 9

- `SousLaVille.Runtime.asmdef` : ajout de `UnityEngine.UI` (`Canvas`, `CanvasScaler`, `Image`).
  Aucun type URP nouveau côté runtime.
- `SousLaVille.Editor.asmdef` : ajout de `UnityEngine.UI` pour construire le HUD par code.
  `Unity.RenderPipelines.Universal.2D.Runtime` y est déjà, ce qui couvre le `Light2D` dont on
  change l'intensité et la `PixelPerfectCamera` de la phase 1.

## Décisions techniques signalées

1. **`UndergroundMap` ne partage pas le code de `SurfaceMap`.** Trois lignes dupliquées plutôt
   qu'un couplage prématuré : dès la phase 3, le sous-sol devient mutable et la surface non.
2. **Le portail est un composant, pas un déclencheur physique.** Aucun collider dans ce jeu, la
   détection est une comparaison de cases. Un enfant ne peut pas descendre par accident en
   frôlant une bouche.
3. **Interaction sur la case occupée, pas sur la case regardée.** Le creusement de la phase 3
   utilisera `FacingCell` ; les deux gestes cohabiteront sans ambiguïté, on ne creuse pas la
   case où l'on se tient.
4. **Lumière globale du sous-sol abaissée à 0,8.** Le commentaire de `UndergroundSceneBuilder`
   l'annonçait depuis la phase 0. Assez pour que ça sente le souterrain, pas assez pour gêner
   la lecture. À rediscuter une fois vu à l'écran.
5. **HUD en uGUI plutôt qu'en sprites enfants de la caméra.** Un sprite d'interface devrait
   choisir une famille de Sorting Layers et en changer à chaque bascule, sous peine de passer
   derrière le décor ou de rendre noir. Le Canvas en Screen Space ignore les Sorting Layers et
   les lumières 2D. Coût : une référence d'assembly de plus dans chaque asmdef.
6. **`SceneRouter` référence `ScreenFader`, donc Core dépend de UI.** Une seule assembly
   runtime, aucun risque de cycle. Une interface intermédiaire serait de la cérémonie pour
   deux méthodes.
7. **Pas de sauvegarde de la couche courante.** La phase 6 s'en charge. Au lancement, on
   démarre toujours en surface.

## Vérification de fin de phase

1. `Générer l'art placeholder`, puis `Construire toutes les scènes`.
2. Console Unity relue via le pont MCP : **zéro erreur et zéro warning**, pas seulement zéro
   erreur.
3. Contenu de `Underground.unity` relu par script : cases praticables, `ManholePortal` présents
   et appariés deux à deux avec ceux de `Surface.unity`, tous les renderers sur la famille
   `Underground_*`.
4. Play depuis `Boot`, vérifications par injection clavier :
   - marcher jusqu'à une bouche, Espace, arriver sous terre sur la même case ;
   - le personnage n'est pas noir sous terre, son Sorting Layer a bien basculé ;
   - la caméra reste bornée, aucun vide visible autour des galeries ;
   - marcher dans la galerie jusqu'à une autre échelle, Espace, remonter par une autre bouche ;
   - la terre pleine arrête net, comme les haies ;
   - Espace martelé ou maintenu pendant le fondu ne déclenche pas de double transition ;
   - une flèche maintenue pendant le fondu ne fait pas glisser le personnage à l'arrivée ;
   - le repère de couche change à chaque bascule, le picto d'action n'apparaît que sur une
     bouche.
5. Capture d'écran du Game view des deux couches, pour juger la lisibilité et l'intensité
   lumineuse.
6. PROGRESS.md à jour, commit `Phase 2 - le portail`.
7. **Arrêt.** Résumé, attente de validation avant la phase 3 (règle 1).
