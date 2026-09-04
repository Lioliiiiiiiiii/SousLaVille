# Sous la Ville, journal de bord

Unity 6000.5.10f1, URP 17.6.0 (Renderer2D), Input System 1.20, Newtonsoft 3.2.2.
Aucun package supplémentaire n'a été ajouté au projet.

## État par phase

| Phase | Titre | État |
|---|---|---|
| 0 | Fondations | Terminée |
| 1 | Le personnage et la surface | Terminée |
| 2 | Le portail | Terminée |
| 3 | Creuser et poser | Terminée |
| 4 | L'eau coule | Terminée |
| 5 | Les saisons et le gel | Terminée |
| 6 | Sauvegarde | Terminée |
| 7 | La plaque gravable | Terminée |
| 8 | La réserve d'eau | Terminée |
| 9a | Les bâtiments | Terminée |
| 9b | L'usine à tuyaux | Terminée |
| 10 | Les fuites | Terminée |
| 11 | Le parc | Terminée |
| 12a | Les filets | Terminée |
| 12b | La carte 64x45 et le grand labyrinthe | Terminée |
| 12c | Le décor | Terminée |
| 12d | La station qui s'agrandit | Terminée |
| 12e | Les huit guides | À faire |
| 13 | L'usine à panneaux | À faire |
| 14 | Le Stock, le memory | À faire |
| 15 | La Fabrique | À faire |
| 16 | Le Plan | À faire |
| 17 | Habillage | À faire |

## Phase 0, ce qui est fait

- Arborescence `Assets/` conforme à CLAUDE.md. Les dossiers encore vides portent un
  `.gitkeep` (git ne suit pas un dossier vide).
- Deux assembly definitions : `SousLaVille.Runtime` (tout `Assets/Scripts`) et
  `SousLaVille.Editor` (`Assets/Editor`, plateforme Editor seulement).
- `Assets/Scripts/Core/` : `GameLayer`, `GameManager`, `SceneRouter`, `Bootstrapper`.
- `Assets/Editor/` : générateurs de scènes sous le menu `Sous La Ville/`, plus la création
  des Sorting Layers.
- Quatre scènes générées par code : Boot, Persistent, Surface, Underground, inscrites au
  build dans cet ordre. `SampleScene` supprimée.
- Sorting Layers : une famille par couche, `Surface_Ground`, `Surface_Pipes`,
  `Surface_Water`, `Surface_Entities`, `Surface_Overlay`, et les cinq mêmes préfixées
  `Underground_`.
- `Assets/Settings/SousLaVille.inputactions` : carte `Gameplay`, `Move` (quatre flèches) et
  `Interact` (Espace), avec bindings manette optionnels. Classe C# générée dans
  `Assets/Scripts/Core/SousLaVilleInputActions.cs`.

## Phase 0, vérifications faites

- Compilation : zéro erreur, zéro warning.
- Les dix Sorting Layers sont enregistrés avec des identifiants distincts et non nuls.
- Chaque `Light2D` globale ne vise que les cinq layers de sa couche, vérifié dans les scènes.
- Play depuis `Boot` : Persistent, Surface et Underground chargées, Boot déchargée, console propre.

## Phase 1, ce qui est fait

- `Assets/Editor/ProjectSetup/PlaceholderArtGenerator.cs` : menu « Générer l'art placeholder ».
  Écrit huit vrais PNG puis règle leur import (PPU 16, filtre Point, `Uncompressed`,
  `Single`, `FullRect`), et crée les six assets `Tile` correspondants, `colliderType = None`.
  Le personnage porte un pivot personnalisé `(0.5, 1/3)` : son transform se pose au centre
  de la case et sa tête dépasse.
- `Assets/Editor/SceneBuilders/VillageLayout.cs` : le plan du village, trente lignes de
  quarante caractères, dans l'assembly Editor seulement. Bordure de haies, boucle de chemins
  autour de la place du parc, enceinte de la station en haut à gauche, trois bouches d'égout
  éloignées les unes des autres, trois bosquets de haies comme obstacles hors bordure.
- `Assets/Scripts/World/GridMap.cs` : base abstraite, conversions case vers monde,
  `CellBounds`, `WorldBounds`, `IsWalkable` abstraite.
- `Assets/Scripts/World/SurfaceMap.cs` : `IsWalkable` = dans les bornes et pas de tuile dans
  la tilemap bloquante. Aucune donnée de collision dupliquée.
- `Assets/Scripts/Player/PlayerController.cs` : déplacement case par case à 5 cases par
  seconde, axe dominant seul, X prioritaire à égalité, case bloquée égale demi-tour sur place.
  Expose `Cell`, `Facing`, `FacingCell` pour les phases 2 et 3. Aucun `Rigidbody2D`, aucun collider.
- `Assets/Scripts/Core/CameraFollow.cs` : suivi en `LateUpdate`, sans lissage, borné aux
  limites de la carte, demi-vue 10 x 5,625 unités.
- `SurfaceSceneBuilder` étendu : `Grid` en cases de 1x1, `Tilemap_Ground` et
  `Tilemap_Blocking` sur `Surface_Ground` en ordres 0 et 1, trois bouches et la station sur
  `Surface_Entities`, `SurfaceMap` câblé sur la racine.
- `PersistentSceneBuilder` étendu : objet `Player` posé sur la case `X`, sur
  `Surface_Entities` en ordre 10, et `CameraFollow` câblé sur lui.
- `SceneBuilderUtility.ApplySortingLayer` : pose un Sorting Layer et hurle s'il manque.
  Unity retombe silencieusement sur `Default`, où aucune des deux lumières globales ne porte.
- `BuildAllScenes` refuse de construire si l'art placeholder n'est pas là.

## Phase 1, vérifications faites

- Compilation : **zéro erreur, zéro warning**. Deux warnings ont été corrigés au passage,
  voir les décisions ci-dessous.
- `Générer l'art placeholder` puis `Construire toutes les scènes` : console propre, quatre
  scènes régénérées.
- Contenu de `Surface.unity` relu par script : 1200 tuiles de sol, 179 tuiles bloquantes,
  `WorldBounds` centré (20, 15) d'extension (20, 15), trois bouches aux cases (33, 5),
  (20, 10) et (8, 19), station à (6, 25), tout sur les bons Sorting Layers.
- Play depuis `Boot` : Boot se décharge, Persistent, Surface et Underground restent chargées,
  Surface active, le personnage naît sur la case `X` (14, 15). **Console entièrement vide.**
- Déplacement vérifié en play mode, par injection d'événements clavier :
  - marche jusqu'à la haie et s'arrête net, sans reculer ;
  - **deux flèches ensemble** (haut + droite) : déplacement sur X seul, `y` reste à 28,5 au
    millième près, aucune diagonale, aucun blocage ;
  - contre la bordure, le personnage **tourne sans avancer** : case et position inchangées,
    `Facing` passe de (-1, 0) à (0, 1), la case visée est bien infranchissable ;
  - vitesse mesurée en régime établi : **5,000 cases par seconde** ;
  - au repos, le personnage se pose exactement au centre de sa case ;
  - caméra bornée sur les quatre bords : x reste dans [10, 30], y dans [5,625 ; 24,375].
- Rendu vérifié par capture d'écran du Game view : tout est éclairé, aucun sprite noir, la
  grille se lit grâce aux liserés, la bouche d'égout ronde se distingue au premier coup d'œil
  des tuiles de chemin.

### Note d'atelier sur le test en play mode

L'éditeur sans focus ne fait pas tourner la boucle de jeu (`Application.runInBackground` est
faux) et le New Input System coupe les périphériques hors focus
(`backgroundBehavior = ResetAndDisableNonBackgroundDevices`). Pour piloter le jeu depuis le
pont MCP il faut donc l'éditeur au premier plan, ou `Application.runInBackground = true` posé
à chaud. Rien de tout cela n'a été écrit dans les ProjectSettings, vérifié par `git status`.

## Phase 2, ce qui est fait

- `Assets/Scripts/Core/GameSortingLayers.cs` : les dix noms de Sorting Layers en constantes
  runtime, plus `EntitiesFor(GameLayer)`. `SortingLayerSetup` et les trois builders les
  consomment. Source unique, plus de chaîne en dur.
- `Assets/Scripts/World/UndergroundMap.cs` : `GridMap` du sous-sol, deux tilemaps comme la
  surface. `Ground` et `Blocking` sont publics : creuser, en phase 3, sera retirer une tuile
  bloquante et repeindre le sol.
- `Assets/Scripts/World/ManholePortal.cs` : un composant pour les deux bouts du passage, avec
  registre statique. `Find(layer, cell)` prend la couche en argument, car une bouche et son
  échelle occupent la même case.
- `Assets/Scripts/Player/PlayerInteractor.cs` : Espace, lu par `WasPressedThisFrame`. Cherche
  un portail sur la case occupée, allume le picto d'action au-dessus de la tête, éteint
  `PlayerController` le temps du voyage.
- `Assets/Scripts/UI/ScreenFader.cs` et `Assets/Scripts/UI/LayerIndicator.cs` : le fondu au
  noir et le repère de couche, en uGUI.
- `SceneRouter.TravelAsync(target, whileBlack)` : fondu, bascule, callback écran noir, fondu
  inverse, sous garde `IsBusy`. Le routeur ne connaît toujours pas le personnage.
- `PlayerController.Teleport(cell)` publique, et bascule du Sorting Layer de tous ses
  `SpriteRenderer` à chaque changement de carte, via `GridMap.Layer`.
- `Assets/Editor/SceneBuilders/UndergroundLayout.cs` : le plan du sous-sol, 30 lignes de 40
  caractères, plus `ValidateAgainstVillage()`.
- `Assets/Editor/SceneBuilders/PortalBuilder.cs` : pose un `ManholePortal` sur un objet déjà
  construit. Factorisé entre les deux builders.
- `UndergroundSceneBuilder` construit la vraie scène : Grid, deux tilemaps, trois échelles,
  l'arrivée de la station, `UndergroundMap`, lumière globale à 0,8.
- `SurfaceSceneBuilder` pose un `ManholePortal` sur les trois bouches et sur la station.
- `PersistentSceneBuilder` ajoute `PlayerInteractor`, l'enfant `Prompt` du personnage et le
  `HUD_Canvas` (voile du fondu, repère de couche), et câble le fader sur le routeur.
- `PlaceholderArtGenerator` produit sept fichiers de plus : `tile_earth`, `tile_tunnel`,
  `ladder`, `picto_surface`, `picto_underground`, `picto_down`, `picto_up`, plus les assets
  `Tile_Earth` et `Tile_Tunnel`. Quinze textures et huit tuiles au total.
- Les deux `.asmdef` gagnent `UnityEngine.UI`, requis par le Canvas et les `Image`.

## Phase 2, vérifications faites

- Compilation relue par le pont MCP : **zéro erreur, zéro warning**. Seul subsiste un warning
  du package MCP lui-même, `[WebSocket] Unexpected receive error`, émis depuis
  `Library/PackageCache`, étranger au projet.
- `Générer l'art placeholder` puis `Construire toutes les scènes` : console propre, quatre
  scènes régénérées.
- Contenu des scènes relu par script : **huit portails, appariés en quatre couples**
  bouche / échelle sur les cases (33, 5), (20, 10), (8, 19) et (6, 25) ; **74 cases
  praticables** au sous-sol ; aucun renderer de l'Underground hors de la famille
  `Underground_*` ; lumière globale à 0,8 restreinte à ses cinq layers ; `HUD_Canvas` en
  320x180, voile éteint, repère à `picto_surface`.
- `ValidateAgainstVillage()` : les deux plans sont alignés, aucune échelle orpheline.
- Play depuis `Boot`, par injection clavier :
  - marche en surface vérifiée après refonte : 18 cases parcourues flèche droite maintenue ;
  - sur une bouche, le picto `picto_down` apparaît au-dessus de la tête ;
  - Espace : arrivée sous terre **à la même case** (20, 10), carte `UndergroundMap`, sprite du
    personnage **et** du picto passés sur `Underground_Entities`, repère du HUD passé sur
    `picto_underground`, caméra recentrée ;
  - marche dans la galerie de (20, 10) à (8, 10), arrêt net sur la terre pleine, puis remontée
    du couloir jusqu'à (8, 20), bloqué par le plafond de terre, le personnage se tourne sans
    avancer ;
  - Espace sur l'échelle (8, 19) : retour en surface à la même case. **Descendre par une
    bouche et remonter par une autre fonctionne.**
  - martelage : fondu allongé à 6 s le temps du test, Espace et flèche droite enfoncés en
    plein fondu. Résultat : une seule transition, `PlayerController` bien éteint pendant le
    voyage, arrivée exacte sur (8, 19), et la marche ne reprend qu'une fois l'écran rendu.
    Durée remise à 0,15 s ensuite ; rien de tout cela n'a été enregistré.
  - console **entièrement vide** sur une session de play complète, transition comprise.
- Rendu vérifié par capture d'écran des deux couches : le personnage est éclairé sous terre,
  la terre pleine se distingue des galeries, les deux pictos du HUD sont lisibles.

## Phase 3, ce qui est fait

- `Assets/Scripts/Network/PipeNode.cs`, `PipeSegment.cs`, `PipeType.cs` : le modèle de
  données de CLAUDE.md, à la lettre. `condition`, `isFrozen` et `isClogged` sont posés dès
  maintenant bien qu'inutilisés avant la phase 5 : les ajouter après coup casserait les
  sauvegardes de la phase 6.
- `Assets/Scripts/Network/PipeNetwork.cs` : le graphe. Poser un tuyau crée le nœud à la
  profondeur lue sur la carte **et le raccorde automatiquement à ses voisins**. Retirer défait
  le nœud et ses segments, sauf sur un nœud permanent. Un seul événement, `Changed`, sur
  lequel la phase 4 branchera le `FlowSolver`.
- `Assets/Scripts/Network/PipeNetworkView.cs` : le rendu, une tuile par masque de raccords.
- `Assets/Scripts/Player/TargetCursor.cs` : le cadre sur la case regardée, visible sous terre
  seulement.
- `UndergroundMap` gagne `DepthAt(cell)`, alimenté par un tableau de 1200 entiers cuit par le
  builder, et `Dig(cell)` qui retire la terre et repeint la galerie à la bonne nuance.
- `PlayerInteractor` : Espace devient contextuel, quatre actions dans un ordre fixe. Passage
  sur la case occupée, puis creuser, poser ou enlever sur la case regardée. Le picto
  au-dessus de la tête annonce toujours laquelle.
- `PlayerController` : les quatre sprites de direction, décidés le 2 septembre 2026, et
  `Map` exposée pour l'interacteur et le curseur.
- `UndergroundLayout` : un second plan de trente lignes de quarante chiffres donne la
  profondeur de chaque case. La carte de la phase 2 n'a pas bougé d'un caractère.
- `Assets/Editor/ProjectSetup/ScriptableObjectSetup.cs` : menu « Créer les ScriptableObjects »,
  qui produit `PipeType_Standard`. `BuildAllScenes` refuse de construire sans lui.
- `PlaceholderArtGenerator` produit 37 textures et 28 tuiles, dont les seize canalisations
  dessinées par une seule fonction lisant un masque de quatre bits, et supprime les trois
  fichiers de la phase 2 qu'il remplace.

## Phase 3, vérifications faites

- Compilation relue par le pont MCP : **zéro erreur, zéro warning**.
- `Créer les ScriptableObjects`, `Générer l'art placeholder`, `Construire toutes les scènes` :
  console propre, quatre scènes régénérées.
- Scène relue par script : **1200 profondeurs cuites**, réparties en 804 / 336 / 60 pour les
  profondeurs 1, 2 et 3 ; station à la profondeur 3, bouches à 2 et 1 ; trois tuiles de
  galerie câblées ; nœud permanent `PlantInlet` en (6, 25) ; seize tuiles de canalisation, une
  seule tilemap `Tilemap_Pipes` ; quatre sprites de personnage et cinq pictos d'action câblés ;
  aucun renderer hors de la famille `Underground_*`.
- Play depuis `Boot`, par injection clavier :
  - le personnage regarde dans les quatre directions, sprite à l'appui (`player_right`,
    `player_up`) ;
  - le curseur est **éteint en surface**, allumé sous terre, et se pose sur la case regardée ;
  - face à un mur : picto `picto_dig`, Espace, la case devient praticable, la terre disparaît
    de la couche bloquante et le sol se peint en `Tile_Tunnel_1`, la nuance de sa profondeur ;
  - le picto bascule aussitôt sur `picto_pipe` : Espace pose un tuyau, nœud de profondeur 1,
    type `Junction`, tuile `Tile_Pipe_00` puisqu'il est isolé ;
  - le picto bascule sur `picto_remove` : Espace l'enlève, la tuile s'efface, le picto revient
    à `picto_pipe`. **Le même geste fait et défait.**
  - trois tuyaux en ligne : 2 segments, masques 2 / 10 / 8, tuiles `#02 #10 #08` ;
  - pose refusée dans la terre pleine, sans message ;
  - une case creusée puis un quatrième tuyau : 3 segments, le nœud du milieu passe au masque
    11, tuile `#11`, un vrai T ;
  - retrait du milieu : 0 segment, les trois voisins restent, leurs masques retombent à 0 ;
  - **la station ne s'enlève pas** : `RemovePipe` rend faux et, face à elle, le picto ne
    s'affiche même pas. Aucun message, aucune sanction, il ne se passe rien.
  - console **entièrement vide** sur une session de play complète.
- Rendu vérifié par capture d'écran : réseau en ligne, coude et T lisibles, tuyaux distincts
  du sol, et les trois nuances de profondeur visibles le long de la galerie qui monte de la
  station.

### Note d'atelier : recompiler pendant le play

Recompiler pendant que le jeu tourne provoque un rechargement de domaine. Unity rappelle alors
`OnEnable` **sans repasser par `Awake`** : les champs non sérialisés sont perdus et
`input.Gameplay.Enable()` levait une `NullReferenceException`. Fragilité héritée de la phase 1,
révélée ici. `PlayerController` et `PlayerInteractor` créent désormais leur input dans un
`EnsureInput()` appelé aux deux endroits.

## Phase 4, ce qui est fait

- `Assets/Scripts/Network/FlowSolver.cs` : le parcours de CLAUDE.md à la lettre, un parcours
  en largeur par maison jusqu'à la station. Une arête n'est franchissable que si la profondeur
  ne diminue pas dans le sens de l'écoulement, si `condition > 0.3`, si le segment n'est ni
  gelé ni bouché. Vit dans **Persistent** : le sous-sol éteint ne pourrait plus répondre aux
  maisons de la surface.
- `Assets/Scripts/World/House.cs` et `HouseSpawner.cs` : cinq maisons créées au réveil à partir
  d'une liste de cases cuite par le builder, chacune avec sa goutte.
- `Assets/Scripts/UI/HouseCounter.cs` : la rangée de gouttes du HUD, remplie de la gauche vers
  la droite.
- `PipeNetworkView` teinte en bleu les tuyaux qui portent l'eau. Aucune image nouvelle : une
  couleur par case, posée après `SetTileFlags(TileFlags.None)`, sans quoi Unity ignorerait la
  teinte en silence.
- `GameManager` expose `Flow` comme il exposait déjà `Router`.
- `VillageLayout` : cinq maisons, marqueur `A`, sur des cases devenues bloquantes.
- `UndergroundLayout` : cinq alcôves alignées sous les maisons, la validation qui refuse de
  construire s'il en manque une, et **le plan des profondeurs avec ses deux crêtes**.
- `PlaceholderArtGenerator` : maison, tuile bloquante de maison, arrivée de maison sous terre,
  goutte pleine et goutte vide. 42 textures et 29 tuiles au total.

### Les crêtes

Deux arcs peu profonds traversent la couronne de profondeur 2, à quatorze et à huit cases de la
station, chacun percé d'une seule porte, les deux portes opposées. Un arc à profondeur 1 posé au
milieu de la profondeur 2 est un mur pour l'eau : un trajet déjà descendu à 2 ne peut pas y
remonter. Ils se voient dans la terre avant même d'être creusés, puisque la terre est teintée
par sa profondeur depuis la phase 3.

Solvabilité vérifiée par calcul **avant** d'écrire le plan, puis confirmée en jeu :

| Maison | Sans crête | Avec crêtes |
|---|---|---|
| (5, 20) | 6 | 6 |
| (13, 17) | 15 | 31 |
| (27, 17) | 29 | 45 |
| (29, 10) | 38 | 46 |
| (34, 4) | 49 | 57 |

## Phase 4, vérifications faites

- Compilation relue par le pont MCP : **zéro erreur, zéro warning**.
- Scènes relues par script : cinq maisons, toutes sur des cases **bloquantes** ; six nœuds
  permanents, la station en profondeur 3 et les cinq `HouseConnection` en profondeur 1 ou 2,
  toutes dans des alcôves déjà creusées ; cinq gouttes au HUD ; `GameManager.Flow` câblé.
- Play depuis `Boot` :
  - **un chemin correct** de la maison (5, 20) à la station : maison desservie, 1/5 au HUD,
    tuyaux teintés `#5CA8F0` sur tout le trajet, goutte de la maison pleine ;
  - **un chemin qui remonte** : depuis la maison (13, 17), un trajet tout droit vers l'ouest,
    **physiquement raccordé jusqu'à la station**, vérifié segment par segment. La maison reste
    grise et l'eau n'entre pas : la crête en (12, 17) retombe à la profondeur 1. C'est la règle
    qui bloque, pas un maillon manquant ;
  - **le bon chemin** pour la même maison, trouvé par parcours à profondeur non décroissante :
    31 cases, exactement la longueur calculée à la conception, porte franchie en (11, 25). La
    maison passe à desservie, 2/5, et le trajet naïf reste sec ;
  - **retrait d'un tuyau au milieu** d'un trajet qui marchait : la maison redevient grise dans
    la foulée, 2/5 tombe à 1/5, le tuyau voisin reprend sa couleur grise. Reposé, tout revient ;
  - **le solveur ne tourne qu'aux changements** : 46 résolutions figées sur 64 images sans
    modification, cinq placements égalent cinq résolutions ;
  - remonter en surface rafraîchit les gouttes des maisons ;
  - console **entièrement vide** sur une session de play complète.
- Rendu vérifié par capture d'écran : maisons et gouttes en surface, réseau bleu sous terre.

### Note d'atelier : le solveur démarrait trop tôt

`FlowSolver` vit dans Persistent, qui est chargée **avant** l'Underground. Résoudre sa référence
au réseau une seule fois au `Start` le laissait muet pour toujours, avec une erreur en console.
Même piège que la carte du joueur en phase 1, même remède : résolution paresseuse, retentée tant
qu'elle échoue, puis plus jamais.

## Phase 5, ce qui est fait

- `Assets/Scripts/Core/GameClock.cs` : l'horloge. Elle compte et lève `Tick` à la fin de
  chaque saison, rien de plus. `SeasonDuration` vaut 600 s, sérialisé et réglable à chaud ;
  `SeasonProgress` est déjà là pour que la phase 6 sauvegarde l'instant exact. La boucle
  `while` du tick ne saute aucune saison quand l'horloge est très accélérée.
- `Assets/Scripts/Seasons/SeasonDefinition.cs` : une saison en données. Pictogramme, couleur
  de lumière, `freezeMaxDepth`, `clogChance`, `wearMultiplier`, `thaws`.
- `Assets/Scripts/Seasons/SeasonSystem.cs` : avance d'une saison par tick, applique dégel,
  gel, bouchons et usure, puis appelle `FlowSolver.Solve()`. Dans cet ordre, une fois par
  saison, jamais par frame. Vit dans Persistent, résolution paresseuse du réseau.
- `Assets/Scripts/Seasons/SeasonAmbience.cs` : posé sur la racine de la Surface, il teinte
  sa propre `Light2D`. S'abonne dans `OnEnable`, se désabonne dans `OnDisable`, et **réapplique
  la saison courante à chaque rallumage**.
- `Assets/Scripts/UI/SeasonIndicator.cs` : le picto de saison au HUD, à droite du repère de
  couche, même taille et même marge.
- `PipeNetwork.Repair(cell)` et `PipeNetwork.NeedsRepair(cell)` : remettre à neuf, dégeler,
  déboucher, et dire au picto lequel des deux gestes s'annonce.
- `PlayerInteractor` : cinquième action. L'ordre est passage, creuser, poser, **réparer**,
  enlever. Espace ne retire qu'un tuyau sain.
- `PipeNetworkView` : cinq couleurs, dans l'ordre de priorité gelé, bouché, trop abîmé,
  porteur d'eau, sain. Le seuil de 0,3 est devenu `FlowSolver.MinimumCondition`, public :
  le rendu et le solveur lisent la même constante.
- `GameManager` expose `Clock` et `Seasons` comme il exposait `Router` et `Flow`.
- `ScriptableObjectSetup` produit les quatre saisons et **réécrit les valeurs à chaque
  passage**, y compris celles de `PipeType_Standard` : le menu est la source de vérité.
- `PlaceholderArtGenerator` : quatre pictos de saison 32x32 et une clé de réparation 16x16.
  47 textures et 29 tuiles au total.
- **Règle 9 traitée** : `SousLaVille.Runtime.asmdef` gagne
  `Unity.RenderPipelines.Universal.2D.Runtime`, sans quoi `SeasonAmbience` ne compilerait pas.

### Les quatre saisons

| Saison | Effet | Usure | Lumière |
|---|---|---|---|
| Printemps | dégèle tout | 1 | (0,82 ; 1,00 ; 0,80) |
| Été | rien, saison de répit | 0,5 | (1,00 ; 0,95 ; 0,72) |
| Automne | bouche 25 % des tuyaux de profondeur 1 | 1 | (1,00 ; 0,82 ; 0,60) |
| Hiver | gèle les tuyaux de profondeur 1 | 1,5 | (0,78 ; 0,88 ; 1,00) |

## Phase 5, vérifications faites

- Compilation relue par le pont MCP : **zéro erreur, zéro warning**. Seul subsiste le warning
  du package MCP lui-même, émis depuis `Library/PackageCache`.
- `Générer l'art placeholder`, `Créer les ScriptableObjects`, `Construire toutes les scènes` :
  console propre, quatre scènes régénérées.
- Assets relus par script : les quatre saisons portent leur picto et leurs valeurs du tableau
  ci-dessus ; `PipeType_Standard` est à 0,1 d'usure et 0 de résistance au gel.
- Scènes relues par script : `GameManager` câblé sur `router`, `flow`, `clock` et `seasons` ;
  `SeasonSystem` sur son horloge, son solveur et les quatre saisons dans l'ordre du cycle ;
  `PlayerInteractor` sur `picto_repair` ; picto de saison au HUD en (40, -4), 32x32 ;
  `SeasonAmbience` sur la racine Surface, câblé sur `Global Light 2D`.
- Play depuis `Boot`, deux réseaux construits pour le test : un **entièrement profond** de
  7 cases jusqu'à la maison (5, 20), profondeur minimale 2, et un de **46 cases** jusqu'à la
  maison (27, 17) qui descend à la profondeur 1.
  - **horloge accélérée à 2 s par saison** : 42 ticks, 42 résolutions. Le cycle tourne
    Printemps, Été, Automne, Hiver et **revient au printemps**, index et picto à l'appui ;
  - **le solveur tourne exactement une fois par saison**, compteur à l'appui : +1 résolution
    par `Advance()`, sur cinq saisons consécutives puis sur 42 ticks d'horloge ;
  - **HIVER** : la maison profonde reste desservie, la maison peu profonde se coupe, 2/5
    tombe à 1/5. **17 segments gelés, tous en profondeur 1, aucun en profondeur 2 ou 3** ;
  - **PRINTEMPS, sans le moindre geste du joueur** : 0 gelé, les deux maisons reviennent,
    1/5 remonte à 2/5 ;
  - **AUTOMNE** : un bouchon, en profondeur 1 uniquement. La maison peu profonde se coupe,
    `NeedsRepair` rend vrai sur la case (15, 13), et **réparer cette seule case suffit** à
    la rétablir ;
  - **usure**, mesurée saison par saison sur un segment profond que ni le gel ni les feuilles
    n'atteignent : 0,95 puis 0,85 / 0,70 / 0,60 / 0,55 / 0,45 puis **0,2999 à la septième
    saison**. Le tuyau cesse de porter et la maison se coupe exactement là. La réparation le
    remet à 1,00, et remettre toute la ligne à neuf ramène les deux maisons ;
  - **les cinq couleurs, relues dans la tilemap** en plein hiver : 18 cases en blanc bleuté
    `(0,85 ; 0,93 ; 1,00)`, 7 en bleu d'eau `(0,36 ; 0,66 ; 0,94)`, 30 en blanc de tuyau sain ;
  - **les pictos, par injection clavier réelle** : Espace sur une bouche fait descendre ;
    face à un tuyau sain le picto est `picto_remove` ; l'hiver venu, le même tuyau affiche
    `picto_repair` ; **Espace le répare** (condition 1,00, dégelé, teinte revenue au blanc) et
    le picto rebascule aussitôt sur `picto_remove` ; **Espace l'enlève** alors, 55 nœuds
    tombent à 54, et le picto revient à `picto_pipe`. Le même geste fait et défait ;
  - **le piège de la couche éteinte** : deux saisons passées pendant que la Surface est
    éteinte laissent sa lumière au vert du printemps ; au rallumage elle **se remet d'elle-même
    à la couleur de la saison en cours**, accord exact vérifié. C'est `OnEnable` qui la sauve ;
  - console **entièrement vide** sur un cycle complet de saisons et huit bascules de couche.
- Captures : les quatre ambiances de surface et un réseau gelé vu du sous-sol.
- `git status` : **aucune modification des ProjectSettings**. `Application.runInBackground`,
  `Time.timeScale` et l'horloge accélérée n'ont existé qu'à chaud, en play mode.

### Note d'atelier : deux erreurs qui ne viennent pas du jeu

`Ignoring depth surface load action as it is memoryless` apparaît deux fois en console, mais
**uniquement quand je prends une capture d'écran** : c'est le chemin `ScreenCapture` d'URP sur
Metal, pas le jeu. Vérifié en isolant : un cycle complet de saisons et huit bascules de couche
sans capture laissent la console à zéro entrée, une seule capture fait apparaître les deux
lignes. Rien à corriger côté projet.

## Phase 6, ce qui est fait

- `Assets/Scripts/Core/SaveData.cs` : la forme du fichier, et rien d'autre. `version`,
  `seasonIndex`, `seasonProgress`, les cases creusées, les cases posées, les segments abîmés.
- `Assets/Scripts/Core/SaveSystem.cs` : vit dans Persistent sur le `GameManager`. Écoute
  `PipeNetwork.Changed`, `UndergroundMap.Dug` et `SeasonSystem.SeasonChanged`, écrit au plus
  une fois toutes les deux secondes, plus une dernière à la fermeture. Écriture **atomique**
  par fichier temporaire puis remplacement.
- `UndergroundMap` : un `HashSet` des cases ouvertes **en jeu**, plus l'événement `Dug`. Les
  galeries du plan n'y sont pas : elles viennent de la scène, pas du joueur.
- `PipeNetwork` : `SegmentBetween(a, b)`, et un **mode groupé** `BeginBatch` / `EndBatch` qui
  ne lève `Changed` qu'une fois à la fermeture du groupe.
- `SeasonSystem.Restore(index)` : pose la saison **sans rejouer ses effets**. Charger une
  partie en hiver ne doit pas regeler un réseau déjà gelé.
- `GameManager` expose `Save`.
- `Assets/Editor/ProjectSetup/SaveTools.cs` : « Ouvrir le dossier de sauvegarde » et
  « Repartir d'une partie neuve ». **Aucun des deux ne détruit quoi que ce soit** : repartir
  neuf met l'ancienne partie de côté avec un horodatage.
- `ProjectSettings` : `companyName` passe à `Lio`. Les parties vivent désormais dans
  `~/Library/Application Support/Lio/SousLaVille/partie.json`.

### L'ordre de restauration

1. Rejouer les creusements. Sans terrain ouvert, `PlacePipe` refuse.
2. Reposer les tuyaux. Les segments se recréent seuls, par voisinage.
3. Appliquer l'état de chaque segment : condition, gel, bouchon.
4. Poser la saison, puis l'avancement de l'horloge.
5. `EndBatch` lève `Changed` une fois, et le solveur résout une fois.

## Phase 6, vérifications faites

- Compilation relue par le pont MCP : **zéro erreur, zéro warning**, hors le warning du
  package MCP émis depuis `Library/PackageCache`.
- Scènes reconstruites, `GameManager` câblé sur `save`, `SaveSystem` sur son horloge et ses
  saisons.
- Play depuis `Boot`, dossier de sauvegarde vidé au préalable :
  - **partie neuve** : aucun fichier, aucun message, démarrage au printemps ;
  - réseau de test construit, 55 nœuds, 51 segments, 39 cases creusées dont **une sans tuyau
    dessus**, puis trois saisons passées jusqu'à l'hiver : 17 gelés, 4 bouchés, 1/5 desservie ;
  - fichier écrit, **13 795 octets**, relu à l'œil : lisible, un objet par case, aucun
    `.tmp` laissé derrière ;
  - **sortie puis relance du play mode** : 55 nœuds, 51 segments, 39 creusées, 17 gelés,
    4 bouchés, 1/5 desservie, saison Hiver index 3, et le segment témoin (5, 21)-(5, 20)
    **exactement à 0,7000**. L'avancement dans la saison repart de 0,0507 et continue de
    courir ;
  - **le solveur ne tourne qu'une fois au chargement** : `SolveCount = 1` après avoir reposé
    49 tuyaux. Le mode groupé fait son travail ;
  - **aucune écriture pendant le chargement** : `SaveCount = 0` juste après la restauration ;
  - **l'état rechargé se comporte comme un état vécu** : le printemps dégèle les 17 segments
    venus du disque, et la réparation remet le témoin à 1,00 ;
  - **le joueur repart au départ du village**, case (14, 15), en surface, alors qu'il avait
    quitté sous terre ;
  - **fichier tronqué à la main** : partie neuve, 6 nœuds, 0 creusée, printemps, l'ancien
    fichier retrouvé sous `partie-illisible-2026-09-03-140651.json`, et **un seul
    avertissement en console, rien à l'écran** ;
  - **version inconnue** (99) : même comportement, et la saison du fichier n'est **pas**
    appliquée ;
  - **rien ne peut s'afficher à l'écran** : le HUD ne contient que le voile, le repère de
    couche, le picto de saison et les cinq gouttes, et la scène ne porte **aucun élément de
    texte** ;
  - **chaîne complète au clavier** : Espace devant de la terre pleine creuse la case (35, 4),
    la carte la retient, et le fichier part sur le disque dans la foulée ;
  - console **entièrement vide** sur les sessions normales. Les deux seuls avertissements de
    toute la phase sont ceux, voulus, des tests de fichier abîmé.
- `git diff ProjectSettings/` : **une seule ligne**, `companyName`.

## Phase 7, ce qui est fait

- `Assets/Scripts/Buildings/ManholeCoverDefinition.cs` : une plaque du catalogue, son image et
  l'image de son nom. Même patron que `SeasonDefinition` ; c'est celui que `SignDefinition`
  reprendra en phase 12.
- `Assets/Scripts/Buildings/ManholeFactory.cs` : le catalogue, les cases où les plaques sont
  exposées, les bouches du village, et laquelle porte quoi. Vit dans la scène **Surface**,
  comme `PipeNetwork` vit dans l'Underground.
- `Assets/Scripts/World/ManholeCover.cs` : la plaque que porte une bouche. S'abonne dans
  `OnEnable`, se réapplique à chaque rallumage.
- `Assets/Scripts/UI/VillageMapScreen.cs` : le plan du village. Les flèches passent d'une
  bouche à l'autre, Espace pose et referme.
- `Assets/Editor/ProjectSetup/PixelFont.cs` : **une police de 5 sur 7 pixels, A à Z**, dessinée
  à la main. Fichier non prévu au plan, sorti du générateur d'art pour que la phase 14 le
  reprenne tel quel.
- `PlayerInteractor` : sixième action, `ChooseCover`, sur la case occupée comme le passage.
  **Le picto est la plaque elle-même** : « celle-là ». Aucune image de plus à dessiner.
- `PlaceholderArtGenerator` : huit plaques, huit noms, le plan du village et le pavé de
  l'atelier. 65 textures et 30 tuiles.
- `VillageLayout` : la cour de l'atelier, seize cases sur six, en haut à droite. Ni la
  station, ni les bosquets, ni les maisons, ni les bouches n'ont bougé d'un caractère.
- `SaveData` et `SaveSystem` : le champ `covers`. **`CurrentVersion` reste à 1.**

### Les huit plaques

Géométrie originale inspirée de styles régionaux, jamais l'emblème d'une ville réelle.

| Plaque | Motif |
|---|---|
| PARIS | gaufrage fin en losanges |
| TOKYO | une fleur, six pétales autour d'un cœur |
| BERLIN | anneaux concentriques |
| NEW YORK | gros appareillage de briques, décalé d'un rang à l'autre |
| AMSTERDAM | losanges en diagonale, largement espacés |
| LONDRES | une croix épaisse qui partage la plaque en quatre panneaux |
| ROME | rayons partant du centre |
| LISBONNE | vague en spirale |

## Phase 7, vérifications faites

- Compilation relue par le pont MCP : **zéro erreur, zéro warning**, hors le warning du
  package MCP.
- Plan du village relu par script : **30 lignes de 40 caractères**, huit plaques exposées aux
  cases attendues, 88 cases de pavé, et **les trois bouches, les cinq maisons, le départ et la
  station exactement où ils étaient**.
- **Les huit motifs comparés deux à deux à leur taille réelle**, planche à l'appui. La
  première version donnait à NEW YORK et LONDRES deux quadrillages indistinguables, et à TOKYO
  une croix au lieu d'une fleur : **les trois ont été redessinés, pas doublés**, comme le plan
  l'annonçait.
- Play depuis `Boot` :
  - la cour est praticable, les huit plaques au sol ne bloquent pas le passage, et **chaque nom
    est lisible sous la sienne**, sans chevauchement ;
  - **Espace sur une plaque** : le picto au-dessus de la tête est la plaque elle-même, le plan
    s'ouvre, le personnage s'éteint, et **rien n'est posé dans la foulée** ;
  - le plan montre les trois bouches aux bonnes positions, **chacune portant la plaque qu'elle
    a déjà** ;
  - **les flèches passent d'une bouche à l'autre**, par vrai clavier, sans faire bouger le
    personnage. Pousser vers une direction où il n'y a pas de bouche ne fait rien, et ce n'est
    pas une erreur ;
  - **Espace pose**, le plan se referme, le personnage se rallume, et la bouche du village
    porte la nouvelle plaque ;
  - **reposer une autre plaque sur la même bouche la remplace** : PARIS puis LONDRES sur
    (20, 10) ;
  - **les trois bouches portent trois plaques différentes** : BERLIN, LONDRES, LISBONNE ;
  - **une plaque posée pendant que la surface est éteinte s'applique au rallumage** : c'est
    `OnEnable` qui la sauve ;
  - **quitter et relancer** : les trois plaques sont retrouvées ;
  - **une sauvegarde de phase 6, sans champ `covers`, se relit sans une erreur** : les bouches
    gardent leur allure d'usine et le reste de la partie est intact. C'est exactement la
    compatibilité que la phase 6 promettait ;
  - console **entièrement vide** sur les sessions normales.
- `git diff` des 52 `.meta` de texture : **une seule ligne chacun**, `maxTextureSize` de 32 à
  64. Aucune image existante ne dépasse 32, donc aucune ne change.
- `git status` : **aucune modification des ProjectSettings**, malgré les réglages d'input
  touchés à chaud pendant les essais.

### Le bug trouvé au test : le même Espace lu deux fois

Poser une plaque refermait le plan, puis le rouvrait aussitôt. Le personnage est encore debout
sur la plaque exposée quand le plan se referme, et `PlayerInteractor` lisait la **même**
pression d'Espace que `VillageMapScreen` venait de consommer. Le plan semblait ne jamais se
fermer.

`VillageMapScreen` se gardait déjà de l'appui qui l'ouvre ; il manquait la garde symétrique.
`PlayerInteractor` retient désormais l'image où le plan s'est refermé et ne fait rien pendant
celle-là. **Deux composants qui lisent la même touche ont besoin d'une garde de chaque côté**,
et l'ordre de leurs `Update` n'est garanti par rien.

### Note d'atelier : l'injection clavier demande le focus, vraiment

`Application.runInBackground = true` fait tourner la boucle de jeu sans focus, mais **ne suffit
pas** : le New Input System laisse la touche enfoncée sur le périphérique et n'en informe
jamais les actions. On le voit à `Move.phase = Waiting` alors que `leftArrowKey.isPressed` est
vrai. Ni `backgroundBehavior`, ni `editorInputBehaviorInPlayMode`, ni recréer le clavier ne
débloquent quoi que ce soit à chaud : `canRunInBackground` reste faux.

**Il faut donc que l'éditeur soit réellement au premier plan au moment de l'appui**, et le
vérifier par `Application.isFocused` avant de conclure quoi que ce soit d'un test d'entrée.
Une autre application peut reprendre le focus entre deux appels du pont.

Conséquence de conception, et elle est bonne : `VillageMapScreen` sépare désormais la lecture
du clavier (`ReadDirection`) du choix lui-même (`Select`), qui se vérifie sans clavier.

## Phase 8, ce qui est fait

- `Assets/Scripts/Buildings/WaterReserve.cs` : le bassin d'orage. Niveau entier, capacité,
  `Absorb`, `Release`, `Restore`, et sa propre vue : cinq images pour une cuve, sur la case du
  bassin. Vit dans la scène Underground, réapplique son image dans `OnEnable`.
- `Assets/Scripts/Buildings/TreatmentPlant.cs` : la station devient un objet, posé sur
  `PlantOutlet` dans l'Underground, à côté de son nœud. Elle porte `capacityPerSeason`, 8.
- `PipeNode` : sixième `NodeType`, **`ReserveInlet`, ajouté à la fin**. Écart accepté le
  3 septembre 2026. Permanent comme la station et les maisons.
- `FlowSolver` : le bassin est tracé exactement comme une maison, sur un nœud de plus.
  `IsReserveConnected`, `IsReserveConnectedAt(cell)`, `ReserveCount`. Sa route porte la
  teinte de l'eau quand elle est valide.
- `SeasonDefinition.rainVolume` et `SeasonSystem` : le bilan de l'eau, quatre additions au
  tick, juste après la résolution. `WaterBudget` (arrivant, traité, absorbé, relâché, perdu),
  `LastBudget`, `BudgetCount`, `HouseVolumePerSeason = 1`. Résolution paresseuse du bassin et
  de la station, comme celle du réseau.
- `SaveData.reserveLevel` et `SaveSystem` : lu, écrit, et `reserve.Changed` marque la partie
  à sauver. **`CurrentVersion` reste à 1.**
- `ScriptableObjectSetup` : pluie 2 / 0 / 8 / 1.
- `UndergroundLayout` : le marqueur `R` en (10, 13), la chambre de trois sur trois creusée
  de (9, 12) à (11, 14), et `ValidateReserve` : un seul bassin, chambre ouverte, herbe
  au-dessus, pas déjà au fond. **Le plan des profondeurs n'a pas bougé d'un caractère.**
- `UndergroundSceneBuilder` : l'objet `WaterReserve` câblé (case, capacité 10, cinq images),
  le nœud permanent `ReserveInlet`, et `TreatmentPlant` sur l'arrivée de la station.
- `PlaceholderArtGenerator` : cinq images de cuve, `reserve_00` à `reserve_04`. 70 textures
  et 30 tuiles au total.
- **Règle 9 vérifiée** : aucun type URP touché, aucune référence d'assembly à ajouter.

### Les nombres

Maison desservie 1 par saison, pluie 2 / 0 / 8 / 1, station 8 par saison, bassin 10. Le
tableau du plan, 0 / 0 / 5 / 3 / 2 / 0, est reproduit au chiffre près, voir ci-dessous.

## Phase 8, vérifications faites

- Compilation relue par le pont MCP : **zéro erreur, zéro warning**, hors le warning du
  package MCP émis depuis `Library/PackageCache`.
- `Générer l'art placeholder`, `Créer les ScriptableObjects`, `Construire toutes les scènes`,
  éditeur hors play : console propre, quatre scènes régénérées.
- Sous-sol relu par script : **les neuf cases de la chambre sont ouvertes**, nœud
  `ReserveInlet` en (10, 13) à la profondeur 2, sept nœuds permanents, **station en (6, 25)
  profondeur 3 et cinq alcôves exactement où elles étaient**, et les cinq maisons gardent
  leur chemin : 57 / 46 / 31 / 45 / 6 cases, les nombres de la phase 4. 87 cases praticables.
  Aucun renderer hors de la famille `Underground_*`.
- **Chemin du bassin recalculé** : 24 pas jusqu'à la station, **dont 13 cases à creuser**.
  Le plan annonçait un creusement minime le long de la galerie x = 8 ; c'est faux, voir la
  question ouverte. Constat inverse et utile : **les plus courts chemins de quatre maisons
  sur cinq passent par la chambre**, via (11, 13), (10, 13), (9, 13).
- Les cinq cuves comparées à taille réelle, planche à l'appui : 0 / 34 / 68 / 102 / 138
  pixels d'eau, les niveaux se distinguent sans agrandissement.
- Play depuis `Boot`, partie neuve, **par injection clavier réelle**, éditeur au premier plan
  et `Application.isFocused` vérifié à chaque image : descente par la bouche (20, 10), marche
  jusqu'à (8, 12), **Espace pose un tuyau en (8, 13)**, traversée de la chambre et du bassin
  lui-même, demi-tour, **Espace pose en (9, 13)** : 9 nœuds, 2 segments, le bassin est
  raccordé à deux tuyaux. Face au bassin, **le picto reste éteint** : il ne s'enlève pas,
  `RemovePipe` et `PlacePipe` rendent faux dessus.
- Réseau des cinq maisons construit par code en contournant la chambre, 79 nœuds, 5/5
  desservies, **bassin non relié sur deux années** : niveau à zéro quoi qu'il arrive, rien ne
  casse, une résolution et un bilan par saison.
- **Bassin relié d'un seul tuyau**, (9, 13), avec des copies des saisons sans gel ni bouchon
  et le réseau remis à neuf avant chaque tick : **0 / 0 / 5 / 3 / 2 / 0, puis 5 / 3 / 2 / 0
  sur trois années**, au chiffre près. Automne 13 arrivant, 8 traité, 5 absorbé ; hiver 6,
  2 relâché ; printemps 7, 1 relâché ; été 5, 2 relâché. **La cuve change d'image** : 00, 02,
  01, 01, 00.
- **Route du bassin gelée à la main** en été : `IsReserveConnected` faux, rien ne se relâche,
  niveau bloqué à 5 ; **réparée d'un geste**, l'automne suivant le remplit à 10, image 04, et
  l'hiver relâche 2.
- **Bassin plein à l'automne** : 13 arrivant, 0 absorbé, **5 perdus**, aucune erreur, aucun
  message. `Restore(99)` donne 10, `Restore(-3)` donne 0.
- **Aux vraies règles**, bassin relié, réseau entretenu avant chaque tick : automne 2/5
  desservies (8 bouchons), 10 arrivant, +2 ; hiver 2/5 (37 gelés), 3 arrivant, relâche 2.
  Voir la question ouverte : le tableau du plan suppose cinq maisons au tick, ce que les
  effets de saison rendent impossible.
- **Horloge accélérée à 0,4 s** : 643 ticks, **643 bilans, 643 résolutions**, niveau et
  image d'accord. Les 643 viennent de l'avancement accumulé depuis le début de la session,
  rattrapé en une seule image par la boucle « aucune saison sautée », plus une cinquantaine de
  vrais ticks.
- **Niveau posé à 10 pendant que la couche est éteinte** : au retour sous terre, la cuve
  montre l'image 04. C'est `OnEnable` qui la sauve, et `Apply` dans `SetLevel` d'abord.
- Sauvegarde : 21 100 octets, `reserveLevel` écrit, aucun `.tmp`. **Sortie puis relance** :
  niveau 7 retrouvé, image 03, automne, 80 nœuds, 80 segments, 54 creusées, bassin relié,
  **une seule résolution, aucune écriture**.
- **Une partie de forme phase 7**, même contenu sans le champ `reserveLevel` : se relit sans
  une erreur, **bassin à zéro**, réseau intact, rien mis de côté.
- Console **entièrement vide, tous types confondus**, sur la session complète : clavier,
  quarante saisons, deux voyages de couche, sauvegarde. Seules les deux lignes connues de
  `ScreenCapture` apparaissent après les captures.
- Captures : la cuve pleine avec la chambre reliée en bleu, à moitié, vide.
- `git status` : **aucune modification des ProjectSettings**. `runInBackground` et la durée
  de saison n'ont existé qu'à chaud.

### Note d'atelier : la seconde session de play ne tournait pas

Relancer le play mode depuis le pont alors qu'une autre application avait le focus laisse le
jeu figé à l'image 1 : Boot ne charge même pas Persistent, et un script qui cherche le réseau
répond « pas encore ». `Application.runInBackground = true` posé à chaud suffit, mais il faut
le reposer à chaque nouvelle session de play, il ne survit pas à l'arrêt.

## Phase 9a, ce qui est fait

Le patron des bâtiments : une façade et une porte dans le village, une pièce close derrière,
un personnage qui dit ce qu'on peut y faire. L'atelier des plaques de la phase 7 est refait
dessus. **L'usine à tuyaux n'est pas touchée** : ni type, ni motif, ni tuyau en main.

- **Une troisième couche, `GameLayer.Interior`, et une cinquième scène, `Interiors`.** Écart
  explicite aux quatre scènes de CLAUDE.md, accepté le 3 septembre 2026. `Interior` est ajouté
  **à la fin** de l'enum : `ManholePortal.destinationLayer` est sérialisé par son rang, et
  insérer aurait décalé les huit portails de la phase 2.
- `SceneRouter` ne cite plus aucune couche en dur : `SceneNameFor`, `SetActiveLayer` et le
  chargement initial balaient `GameLayers`. Ajouter une couche, c'est ajouter une valeur et
  une scène.
- **Une famille `Interior_*` de cinq Sorting Layers**, dix deviennent quinze, avec sa propre
  `Light2D` globale cantonnée à eux. `GameSortingLayers.Families` remplace les deux tableaux
  cités un à un ; `SortingLayerSetup` les balaie.
- `Assets/Scripts/World/InteriorMap.cs` : la carte des intérieurs. Une seule carte porte
  toutes les pièces, chacune close par ses murs. **Hors des pièces, rien n'est peint et rien
  n'est praticable** : peindre six cents murs qu'on ne verra jamais aurait été du décor pour
  personne.
- **La caméra se borne à la pièce, pas à la carte.** `GridMap.WorldBoundsAround(cell)` est
  virtuelle et rend `WorldBounds` par défaut ; `InteriorMap` la surcharge. `CameraFollow`
  demande les bornes autour de sa cible : deux lignes, et la notion de pièce ne sort pas
  d'`InteriorMap`.
- `Assets/Scripts/Buildings/Villager.cs` : le personnage. Registre statique comme
  `ManholePortal`, parce que l'interacteur le cherche à chaque image.
- `Assets/Scripts/UI/SpeechBox.cs` : ce qu'il dit, une phrase à la fois. Vit dans le HUD,
  **éteint tant que personne ne parle**, comme le voile du fondu depuis la phase 2.
- `PlayerInteractor` : trois actions de plus, `Enter`, `Exit`, `Talk`. Le passage et la plaque
  se prennent sur la case **occupée**, le personnage sur la case **regardée** : on ne se tient
  pas sur quelqu'un.
- `PixelFont` gagne **É, È, À, Ê et l'apostrophe**. `Height` devient `HeightOf(word)` et ne
  réserve les deux rangées d'accent **que si le mot en porte un**.
- `PortalBuilder` accepte une case d'arrivée : la carte des intérieurs n'a aucune raison
  d'être alignée sur le village.
- `VillageLayout` : la cour pavée de seize cases sur six disparaît. À sa place la façade de
  l'atelier, `F`, en `x ∈ [22, 25]`, `y ∈ [26, 27]`, et sa porte, `D`, en **(23, 25)**, sur un
  seuil de chemin.
- `InteriorsLayout` et `InteriorsSceneBuilder` : le plan des pièces et son générateur.
- **`ManholeFactory` déménage** de la scène Surface à la scène Interiors, sans changer une
  ligne de son contenu ni la sauvegarde des plaques.
- `PlaceholderArtGenerator` : façade, mur, porte, artisan, `picto_enter`, `picto_exit`,
  `picto_talk`, et les trois phrases. **81 textures, 32 tuiles.**
- **Règle 9 vérifiée, pas supposée** : `SousLaVille.Editor.asmdef` portait déjà
  `Unity.RenderPipelines.Universal.2D.Runtime`, dont `InteriorsSceneBuilder` a besoin pour la
  `Light2D`. Aucun ajout.

### Le plan des intérieurs

Une différence de forme avec les deux autres plans, et une seule : ce n'est pas une grande
grille de trente lignes, mais **une grille par pièce**, posée dans un créneau. Ajouter un
bâtiment, c'est ajouter un bloc et un créneau, sans rouvrir les lignes des voisins.

La carte fait 40x30 comme les autres couches, découpée en six créneaux de 20 sur 10. **Un
créneau est exactement la vue de la caméra**, 320x180 à PPU 16 : la pièce tient à l'écran d'un
seul tenant, rien ne défile. L'atelier prend le créneau en haut à gauche ; l'usine à tuyaux
prendra celui de droite en 9b, l'usine à panneaux un autre en phase 12.

### Ce que dit l'artisan

Trois phrases, cinq mots ou moins, relues à voix haute pour six ans. En majuscules, la seule
casse que `PixelFont` connaisse.

« CHOISIS UNE PLAQUE » — « PUIS CHOISIS UNE BOUCHE » — « TU PEUX EN CHANGER »

Les deux premières sont les deux gestes, dans l'ordre. La troisième dit que le choix se refait,
ce qui est la promesse du jeu.

## Phase 9a, vérifications faites

- Compilation relue par le pont MCP : **zéro erreur, zéro warning**, hors le warning du package
  MCP émis depuis `Library/PackageCache`.
- Menus dans l'ordre, éditeur hors play : Sorting Layers, art placeholder, ScriptableObjects,
  scènes. Console propre, **cinq scènes** régénérées.
- **Les huit noms de villes de la phase 7 sont inchangés octet pour octet**, `git status` à
  l'appui : `HeightOf` ne réserve les rangées d'accent que pour les mots accentués. Le seul PNG
  existant modifié est `village_map.png`, et il devait l'être.
- Police relue sur planche à taille réelle : É, È, À, Ê et l'apostrophe se lisent. Les trois
  phrases sortent à 109, 139 et 109 pixels de large, **non tronquées** : `maxTextureSize` passe
  à 256 pour elles, le plafond de 64 de la phase 7 les aurait réduites en silence.
- **Plan du village relu par script** : façade aux huit cases attendues, porte en (23, 25), et
  **la station en (6, 25), les cinq maisons, les trois bouches en (33, 5), (20, 10), (8, 19) et
  le départ en (14, 15) exactement où ils étaient**. Cour de l'atelier disparue. Bornes du
  village inchangées, centre (20, 15) d'extension (20, 15). 192 cases bloquantes, les 184 de la
  phase 4 plus les huit de la façade.
- Sous-sol relu par script : **87 cases praticables**, le compte de la phase 8, aucun renderer
  hors de la famille `Underground_*`.
- Scène Interiors relue par script : une pièce en (0, 20, 20, 10), 200 tuiles de sol, 55 murs,
  **145 cases praticables**, huit plaques exposées et leurs huit noms, portail apparié
  `Interior(9, 20) ↔ Surface(23, 25)`, **aucun renderer hors de la famille `Interior_*`**, une
  seule lumière globale sur cinq layers, et **aucune `SeasonAmbience`**.
- Cinq scènes au build dans l'ordre, seize Sorting Layers avec `Default`.
- Play depuis Boot, **par injection clavier réelle**, `Application.isFocused` vérifié :
  - marche réelle de (23, 21) à (23, 25), **arrêt net devant la façade**, picto `picto_enter` ;
  - **Espace : fondu, intérieur**, joueur en (9, 20), carte `InteriorMap`, sprite passé sur
    `Interior_Entities`, **caméra bornée à la pièce en (10, 25)** : la pièce voisine ne se voit
    pas ;
  - marche vers l'artisan, **arrêt net contre lui**, picto `picto_talk`. Il bloque **des deux
    côtés**, et on lui parle aussi bien par le haut que par le bas ;
  - **Espace : il parle.** Ligne 0, puis 1, puis 2, chacune à sa taille exacte en pixels,
    109x9, 139x9, 109x9. Le personnage joueur s'éteint le temps du dialogue ;
  - **le quatrième Espace referme, et ne rouvre pas** alors qu'on regarde toujours l'artisan.
    La double garde tient ;
  - **une plaque se choisit depuis l'intérieur exactement comme en phase 7** : le plan s'ouvre
    avec NEW YORK en main, Espace pose, le plan se referme **et ne rouvre pas** ;
  - **Espace sur la porte ramène en (23, 25), devant la façade**, sprite revenu sur
    `Surface_Entities`, caméra rebornée au village. **La bouche (33, 5) affiche la plaque posée
    depuis l'intérieur** : c'est `OnEnable` et `FindObjectsInactive.Include` qui la sauvent ;
  - **les saisons passent pendant qu'on est dans le bâtiment** — printemps, été, automne — et
    **la lumière de l'intérieur reste blanche pure**. Aucune ligne de code : c'est l'extinction
    de la couche Surface qui suffit. Le picto de saison du HUD suit ;
  - en ressortant, **le village reprend exactement la couleur de la saison en cours**,
    `(0,82 ; 1,00 ; 0,80)` au printemps ;
  - **la marche, la descente, le creusement et la pose sont inchangés** : Espace sur la bouche
    (20, 10) descend, curseur allumé sous terre, `picto_dig` puis `picto_pipe` puis
    `picto_remove`, 80 nœuds deviennent 81, jonction à la profondeur 1 ;
  - **sauvegarde** : les trois plaques écrites, 428 octets, `CurrentVersion` toujours à 1,
    aucun `.tmp` ;
  - **quitter et relancer** : les trois plaques retrouvées et affichées, et **le joueur repart
    au départ du village**, jamais dans un bâtiment ;
  - **une partie de la phase 8 se relit sans une erreur** : 80 nœuds, 80 segments, automne,
    bassin à 7, 5/5 desservies. Les chiffres exacts du journal de la phase 8 ;
  - console **entièrement vide, tous types confondus**, sur la session complète.
- Captures : l'intérieur avec l'artisan qui parle, l'artisan seul, la façade et sa porte vues
  du village. Comme aux phases précédentes, elles ne sont pas versionnées.
- `git diff ProjectSettings/` : **deux fichiers, et ce sont les deux mécanismes voulus**.
  `TagManager` gagne les cinq Sorting Layers `Interior_*`, tous à identifiant positif ;
  `EditorBuildSettings` gagne la scène Interiors. **Aucun `runInBackground`, aucun réglage
  d'input, aucune durée de saison** : ils n'ont existé qu'à chaud.

### Le bug trouvé au test : la phrase deux fois trop large

`Image.SetNativeSize` divise la largeur du sprite par ses pixels par unité, 16 ici, puis la
multiplie par les 100 pixels par unité du Canvas. Une phrase de 109 pixels sortait à **681**,
soit deux fois la largeur de l'écran, et débordait de toutes parts.

Le reste du HUD pose ses tailles en pixels explicites depuis la phase 2, 32x32 pour les pictos,
16x16 pour les gouttes. La boîte de dialogue fait pareil : elle lit le rectangle du sprite.
**`SetNativeSize` n'a rien à faire dans un HUD dont les sprites sont à PPU 16.**

### Le bug trouvé au test : la sauvegarde des plaques, muette

`SaveSystem.TryLoad` résolvait l'atelier juste après le réseau, sur ce raisonnement écrit en
phase 7 : « l'atelier vit dans la scène Surface, chargée AVANT l'Underground : si le réseau
répond, l'atelier existe déjà. » C'était vrai. Le déménagement de l'atelier dans `Interiors`,
que `LoadGameplayScenesAsync` charge **après** l'Underground, l'a rendu faux : `factory` restait
à `null`, et **aucune plaque n'était ni écrite ni relue, sans un message**. Le fichier sortait
avec `"covers": []` alors que les bouches portaient bien leurs plaques à l'écran.

L'atelier rejoint donc la garde, et se retente comme les autres. **Une garde qui repose sur
l'ordre de chargement des scènes est une garde qui ment le jour où une scène change de rang.**
C'est la troisième fois que la résolution paresseuse entre scènes coûte quelque chose, après la
phase 1 et la phase 4 ; c'est la première fois qu'elle échoue en silence.

### Le défaut laissé : le picto « parler » couvre l'artisan

Le picto d'action se pose une unité et quart au-dessus de la tête du joueur, depuis la phase 2.
Quand on parle à quelqu'un **en le regardant par en dessous**, cette place tombe exactement sur
sa tête : l'artisan disparaît derrière un carré blanc de seize pixels.

Vu par le haut ou de côté, il n'y a aucun recouvrement, et le personnage bloque des quatre
côtés, donc on peut toujours l'aborder autrement. Le défaut est cosmétique et réversible en dix
lignes. **Non tranché, voir les questions ouvertes.**

## Phase 9b, ce qui est fait

Les trois types de canalisation, l'usine où on les choisit, et le tuyau en main.

- **La bulle passe au-dessus du personnage qui parle**, décidé après l'avoir vu en jeu en 9a.
  `Villager` porte son propre `SpriteRenderer` de bulle ; `PlayerInteractor` l'allume et
  l'éteint selon ce que le joueur regarde, et n'a plus de `promptTalk`. Seule exception à la
  règle « le picto est au-dessus de la tête du joueur » : un picto qui cache ce qu'il désigne
  ne désigne rien.
- `PipeType` gagne `LeafResistance`, jumelle exacte de `FrostResistance`, plus `PatternIndex`,
  `Sample`, `NameImage` et `DefeatedSeasonIcon`. C'est devenu un catalogue, comme
  `ManholeCoverDefinition` et `SeasonDefinition`.
- **Le type vit sur le nœud**, pas sur le segment. `PipeNode` gagne `PipeType` ; les nœuds
  imposés par le monde, station, maisons et bassin, n'en portent aucun.
- **Un segment est aussi faible que sa plus faible extrémité.** `PipeSegment` expose
  `FrostResistance`, `LeafResistance` et `WearPerSeason` calculées depuis ses deux bouts : la
  plus basse des deux résistances, la plus forte des deux usures. Une extrémité sans type ne
  compte pas ; si aucun des deux n'en a, le type par défaut du monde tranche.
- `PipeNetwork.PlacePipe(cell, type)` : la pose porte le type. Changer le type d'une case,
  c'est l'enlever puis la reposer, le geste de la phase 3. Aucun geste de remplacement.
- `SeasonSystem` : les feuilles lisent `LeafResistance` exactement comme le gel lit
  `FrostResistance`, et l'usure vient du segment.
- `Assets/Scripts/Buildings/PipeFactory.cs` : le catalogue, les cases des échantillons et le
  type en main, avec son événement `Changed`. Vit dans `Interiors`, comme `ManholeFactory`.
- `PipeNetworkView` : **quarante-huit tuiles**, seize par motif, indexées `motif * 16 + masque`.
- `PlayerInteractor` : `ChoosePipe` sur la case occupée, et **le picto de pose devient le tuyau
  en main**. `picto_pipe` a disparu.
- `SaveData` : `pipeTypes` et `pipeInHand`. **`CurrentVersion` reste à 1.**
- `VillageLayout` : la façade `G` de l'usine en `x ∈ [30, 33]`, `y ∈ [26, 27]`, sa porte `E` en
  **(31, 25)**. Rien d'autre n'a bougé.
- `InteriorsLayout` : la pièce de l'usine dans le créneau (20, 20), trois échantillons en
  x = 25, 30 et 35.
- `PlaceholderArtGenerator` : les quarante-huit tuiles, l'ouvrier, ses quatre phrases, les
  trois noms. **121 textures, 64 tuiles.** Les seize tuiles d'un seul motif de la phase 3 et
  `picto_pipe` sont retirées par la liste des obsolètes.
- **Règle 9 vérifiée** : aucun type nouveau, aucune référence d'assembly à ajouter.

### Les trois types

| Type | Gel | Feuilles | Usure | Motif | Nom écrit |
|---|---|---|---|---|---|
| Standard | gèle | se bouche | 0,1 | corps uni | `NORMAL` |
| Isolé | **ne gèle jamais** | se bouche | 0,1 | rayé en diagonale | `ISOLÉ` |
| Grillagé | gèle | **ne se bouche jamais** | 0,1 | pointillé | `GRILLÉ` |

**Le motif est une nuance plus sombre du corps, jamais une couleur à lui.** C'est ce qui le
fait survivre aux cinq teintes d'état : teinter multiplie toute la tuile, donc le contraste
entre le corps et son motif est préservé, gelé comme bouché comme porteur d'eau. Et la
silhouette est rigoureusement identique d'un motif à l'autre : deux tuyaux de types différents
se raccordent à l'œil comme ils se raccordent dans le graphe.

### Ce que dit l'ouvrier

Quatre phrases, cinq mots ou moins. « CHOISIS UN TUYAU » — « L'ISOLÉ ARRÊTE LE FROID » —
« LE GRILLÉ ARRÊTE LES FEUILLES » — « REVIENS QUAND TU VEUX »

Les deux du milieu sont symétriques à dessein : **même verbe, ARRÊTE**, et la menace qui
change. Chacune répond au picto affiché au-dessus de son échantillon, flocon ou feuille.
La dernière dit qu'on peut revenir : pas de stock, pas de panne au fond d'une galerie.

## Phase 9b, vérifications faites

- Compilation relue par le pont MCP : **zéro erreur, zéro warning**, hors le warning du package
  MCP.
- Menus dans l'ordre, éditeur hors play : art, ScriptableObjects, scènes. Console propre.
- **Les trois motifs comparés sur les seize masques, à taille réelle** : distincts entre eux, et
  la silhouette identique d'une rangée à l'autre. **Puis sous les cinq couleurs d'état** :
  toujours distincts, y compris sous le brun du bouchon et le rouge de l'abîmé.
- Les trois noms relus : `NORMAL`, `ISOLÉ`, `GRILLÉ`, accents compris.
- Plan du village relu : **deux façades et deux portes** aux cases attendues, et la station, les
  cinq maisons, les trois bouches et le départ **exactement où ils étaient**. Bornes du village
  inchangées. Sous-sol à 87 cases praticables.
- Scène Interiors : deux pièces, 288 cases praticables, deux personnages de 3 et 4 phrases,
  `PipeFactory` avec ses trois types, leurs motifs 0/1/2, leurs résistances 0-0, 1-0 et 0-1, et
  leur usure identique à 0,1. Aucun renderer hors de la famille `Interior_*`.
- Sous-sol : **48 tuiles câblées, aucune nulle**, rang 16 = `Tile_Pipe_1_00`, rang 47 =
  `Tile_Pipe_2_15`.
- Play depuis Boot, par injection clavier, `Application.isFocused` vérifié :
  - **Espace sur la porte de l'usine** : fondu, intérieur, **caméra bornée à SA pièce en
    (30, 25)** — la pièce voisine ne se voit pas ;
  - face à l'ouvrier, **la bulle est au-dessus de SA tête** et le picto du joueur est éteint ;
  - **Espace : il parle**, quatre lignes, 97x9, 139x11, 175x11 et 127x9 pixels. Les deux
    accentuées font bien deux rangées de plus, et la plus large tient largement sous 256 ;
  - **Espace sur l'échantillon isolé** : le picto devient `pipe_1_10`, et le tuyau en main passe
    à Isolé ;
  - sous terre, **le picto de pose est `pipe_1_10`**, pas `picto_pipe` ; Espace pose un nœud de
    type Isolé, motif 1, tuile `Tile_Pipe_1_00` ;
  - **un seul standard au milieu d'isolés** : en hiver, **seuls les deux segments qui le
    touchent gèlent**, les 44 autres non, et la maison se coupe. Une route isolée l'est de bout
    en bout, ou elle ne l'est pas ;
  - **le croisement des trois types et des deux saisons**, sur la même route de 44 cases dont 17
    peu profondes :

| Type | 1 hiver, gelés | 3 automnes, bouchés | Desservie |
|---|---|---|---|
| Standard | 18 | 11 | coupée aux deux |
| Isolé | **0** | 12 | **desservie en hiver**, coupée en automne |
| Grillagé | 18 | **0** | coupée en hiver, **desservie en automne** |

  - **le critère de fin** : la maison peu profonde (27, 17), route de 46 cases, **reste
    desservie en plein hiver en isolé de bout en bout**, et **se coupe en standard**, 18 gelés ;
  - **le tableau de la phase 8, reproduit au chiffre près et sans copie de saison** :

| Saison | Type posé | Desservies | Arrivant | Traité | Absorbé | Relâché | Bassin |
|---|---|---|---|---|---|---|---|
| Été | Standard | 5/5 | 5 | 5 | 0 | 0 | 0 |
| Automne | Grillagé | 5/5 | **13** | **8** | **+5** | 0 | **5** |
| Hiver | Isolé | 5/5 | **6** | 6 | 0 | **2** | **3** |
| Printemps | Standard | 5/5 | **7** | 7 | 0 | **1** | **2** |

  - **sauvegarde** : 68 cases posées, **35 écrites seulement**, les 33 standard ne le sont pas ;
    `pipeInHand` à 2 ; version 1 ; aucun `.tmp` ;
  - **quitter et relancer** : 33 standard, 33 isolés, 2 grillagés retrouvés, grillagé en main,
    plaque retrouvée, joueur au départ du village. **Le chargement ne coûte qu'une résolution** :
    une partie neuve en compte 1, une partie chargée 2 ;
  - **une partie de la phase 8 se relit sans une erreur** : 78 nœuds, **tout standard**, standard
    en main, automne, bassin à 7 ;
  - le solveur **ne tourne pas par frame** : le compteur ne bouge plus après une résolution ;
  - console **entièrement vide, tous types confondus**, sur la session complète.
- Captures : l'ouvrier qui parle dans son usine, un réseau mêlant les trois motifs, le même en
  plein hiver. Couleurs relues dans la tilemap : standard et grillagé en blanc bleuté, isolés en
  blanc, **et les deux isolés en bout de section gelés parce qu'ils partagent un segment avec un
  voisin d'un autre type**. La règle du bout le plus faible se voit à l'écran.
- `git diff ProjectSettings/` : **vide**. Rien n'a bougé cette phase.

### Ce que le croisement apprend

**Aucun type ne met une maison peu profonde à l'abri de l'année entière.** Le nœud d'une maison
peu profonde est à la profondeur 1, donc le segment qui y arrive est exposé quoi qu'on fasse, et
un type ne vainc qu'une menace. Le tableau de la phase 8 n'est donc pas atteignable avec un
réseau figé : **il l'est en changeant de tuyau entre les saisons**, grillagé avant l'automne,
isolé avant l'hiver.

C'est exactement la boucle voulue, et ce n'est pas un contournement : une saison dure dix
minutes, et l'usine est à deux pas du départ. La réponse à la question ouverte de la phase 8 est
donc « oui, mais en jouant », et non « oui, en posant les bons tuyaux une fois pour toutes ».

## Phase 10, ce qui est fait

L'eau dans le village. Elle vient de deux endroits, et les deux ne disent pas la même chose.

- `Assets/Scripts/World/FloodView.cs` : un seul composant, une seule tilemap, deux sources
  d'eau. Il vit dans la scène Surface, comme `SeasonAmbience` : il peint le village.
- **Le débordement** sort des bouches d'égout et montre `LastBudget.Lost`, le seul nombre que
  le jeu calculait depuis la phase 8 et ne montrait nulle part. Il dit « ton réseau reçoit plus
  que la station ne traite ».
- **La fuite** est une flaque posée dans la rue au-dessus d'un tuyau usé sous le seuil. Elle dit
  « il y a un tuyau crevé ici, sous tes pieds », et elle épargne une descente.
- `PipeNetwork.IsWornOut(cell)` : la règle du « trop abîmé » remonte dans le modèle. Le rendu du
  sous-sol et la flaque de surface la lisent désormais au même endroit ; `PipeNetworkView` ne la
  calcule plus en interne.
- `SurfaceSceneBuilder` : une troisième tilemap, `Tilemap_Water`, sur la famille
  **`Surface_Water` créée en phase 0 pour « flaques, fontaine » et restée vide jusqu'ici**. Elle
  se dessine au-dessus du décor et **sous** les entités : le personnage traverse l'eau.
- `PlaceholderArtGenerator` : une tuile d'eau **semi-transparente**. 122 textures, 65 tuiles.
- **Rien n'est sauvegardé.** Les deux eaux sont des données dérivées.
- **Règle 9 vérifiée** : `UnityEngine.Tilemaps` est dans le module core, aucune référence
  d'assembly à ajouter.

### Les deux eaux

`Lost` plafonne à 5, et c'est vérifiable par le calcul : l'arrivant plafonne à 13, cinq maisons
plus huit de pluie d'automne ; la station en traite 8 ; le surplus plafonne donc à 5.

**Les trois bouches débordent de la même façon**, pas d'un tiers chacune : le réseau déborde,
c'est vrai partout, et il le voit où qu'il se trouve. L'étalement croît par anneaux de Manhattan,
et seules les cases praticables prennent l'eau.

| `Lost` | Rayon | Cases mouillées, mesurées en jeu |
|---|---|---|
| 0 | — | 0 |
| 1 | 0 | 3 |
| 2 | 1 | 15 |
| 3 | 2 | 38 |
| 4 | 3 | 71 |
| 5 | 4 | 113 |

Au maximum 118 cases sur 1200, soit un dixième du village : un spectacle, pas une inondation qui
cache le village.

**Seule l'usure fuit.** Un tuyau gelé ou bouché est **bouché**, pas crevé : il ne laisse rien
passer, donc rien ne sort. C'est cette distinction qui rend la flaque informative — elle ne dit
pas « quelque chose va mal ici », elle dit « un tuyau est crevé ici ». Une fuite mouille les
**deux** bouts du segment crevé, puisque c'est tout le tuyau qui est percé.

**Une seule image d'eau pour les deux.** Une flaque est une flaque, et c'est un symbole de moins
à apprendre. C'est la position qui raconte l'histoire : une nappe en losange autour d'une bouche,
ou une flaque isolée au milieu d'une rue.

## Phase 10, vérifications faites

- Compilation relue par le pont MCP : **zéro erreur, zéro warning**, hors le warning du package
  MCP.
- Menus dans l'ordre, éditeur hors play : art, ScriptableObjects, scènes. Console propre, cinq
  scènes régénérées. **Aucun PNG existant modifié** : la tuile d'eau est neuve.
- Scène relue par script : `Tilemap_Water` sur `Surface_Water` en ordre 0, **vide au départ** ;
  `FloodView` câblé sur la carte, la tilemap, `Tile_Water` et les trois bouches ; six portails ;
  200 cases bloquantes, les 192 de la phase 9a plus les huit de la seconde façade ; bornes du
  village inchangées, centre (20, 15) d'extension (20, 15) ; aucun renderer hors de la famille
  `Surface_*`.
- **La tuile d'eau relue à taille réelle sur les trois sols**, herbe, chemin et dalle du parc,
  planche à l'appui : elle se lit comme de l'eau sur les trois, la grille du sol se voit dessous,
  et les vaguelettes se répètent sans couture.
- Play depuis Boot, par injection clavier, `Application.isFocused` vérifié :
  - **les cinq paliers de `Lost`, mesurés case par case** : 3, 15, 38, 71, 113, **exactement les
    nombres calculés depuis le plan du village**. Obtenus en reliant les maisons une par une, en
    grillagé pour que l'automne ne les bouche pas : `Lost` vaut alors exactement le nombre de
    maisons reliées ;
  - **le critère de fin** : bassin débranché, automne, `Lost` vaut 5 et **les trois bouches
    débordent** ; bassin relié, automne, 13 arrivant, 8 traité, 5 absorbé, **`Lost` vaut 0 et le
    village reste sec** ;
  - **l'eau ne monte sur aucune case bloquante** : 113 cases mouillées, 0 sur du bloquant, et la
    couche bloquante est restée à 200 tuiles ;
  - **le personnage traverse l'eau** : parti de (20, 14) flèche bas maintenue, il arrive en
    (20, 1) après **9 cases mouillées**, sans être arrêté ni ralenti ;
  - **un tuyau usé sous le seuil fuit**, sur sa case et celle de l'autre bout du segment ;
  - **un tuyau gelé ne fuit pas, un tuyau bouché ne fuit pas** : trois cases témoins côte à
    côte, une usée, une gelée, une bouchée, et **seule l'usée porte une flaque** ;
  - **réparer sous terre puis remonter efface la flaque, sans aucun tick** : c'est `OnEnable`
    qui la sauve ;
  - **quitter et relancer** : `Lost` repart à zéro donc **le débordement a disparu**, et **la
    fuite est revenue tout de suite**, l'usure étant écrite dans le fichier depuis la phase 6.
    C'est la conséquence annoncée au plan, écrite avant d'être observée ;
  - **le fichier ne contient aucun champ d'eau**, vérifié par recherche de chaîne ;
  - **une partie de la phase 9 se relit sans une erreur** : 80 nœuds, types conservés, isolé en
    main, automne, bassin à 7. Rien à faire : cette phase n'ajoute aucun champ ;
  - console **entièrement vide, tous types confondus**, sur la session complète.
- Captures : le village qui déborde en losange autour d'une bouche avec le personnage dedans, et
  une fuite isolée sur un chemin.
- `git diff ProjectSettings/` : **vide**. Rien n'a bougé.

### Note d'atelier : la scène ouverte décide du play

Le premier « Construire toutes les scènes » de la phase a échoué sur
`InvalidOperationException: This cannot be used during play mode`, alors que l'art et les
ScriptableObjects, eux, s'étaient générés. L'éditeur était resté en play depuis la session
précédente. **Le générateur d'art ne s'en plaint pas, le générateur de scènes si** : il faut donc
vérifier `EditorApplication.isPlaying` avant les menus, et pas seulement se souvenir d'avoir
arrêté.

## Corrections des bâtiments, 4 septembre 2026

Trois défauts relevés par Lio en regardant les intérieurs de la phase 9. Corrigés avant
d'entamer la phase 11.

- **Les personnages étaient plantés devant leur porte**, à deux cases de l'entrée : ils avaient
  l'air d'attendre dans le couloir. Ils passent **au milieu de leur pièce**, en (9, 25) et
  (29, 25). La caméra étant bornée à la pièce et centrée dessus, le personnage est désormais au
  centre de l'écran quand on entre, et les échantillons sont exposés derrière lui comme derrière
  un comptoir.
- **Les noms écrits dans le décor étaient illisibles.** Huit noms de cinq sur sept pixels posés
  sur du pavé, tous affichés en même temps : trop petits, trop nombreux, sur un fond chargé.
  Ils quittent le décor pour le HUD, **un seul à la fois, celui de l'objet foulé**, sur un fond
  sombre uni et à la résolution de référence — exactement ce qui rend les phrases des
  personnages lisibles depuis la phase 9a. Nouveau composant `UI/ItemLabel`.
- **Les pictos de saison au-dessus des échantillons étaient énormes** : trente-deux pixels de
  côté à côté d'un échantillon de seize, deux fois trop gros. Ils quittent le décor eux aussi et
  accompagnent le nom dans le cartel, à dix-huit pixels : ils l'accompagnent, ils ne le
  remplissent plus.

**La boîte du cartel épouse son contenu**, largeur calculée à l'affichage. Une largeur fixe
laissait « ISOLÉ », trente et un pixels, flotter au milieu de cent soixante : le nom paraissait
perdu et la boîte pesait plus que ce qu'elle disait.

Rien de modal : le personnage continue de marcher, le cartel ne consomme aucune touche et
s'efface dès qu'on quitte la case. C'est un cartel, pas un dialogue.

**Vérifié en jeu** : ouvrier et artisan au centre de leur pièce ; sur l'échantillon isolé, la
boîte fait 66 sur 24 avec le flocon et `ISOLÉ` ; sur la plaque LONDRES, 55 sur 24 sans picto,
une plaque ne vainquant aucune saison ; le cartel s'éteint dès qu'on quitte la case ; console
propre.

## Phase 11, ce qui est fait

Le parc devient un labyrinthe de haies avec la fontaine en son centre : les deux choses que le
projet lui réservait depuis la phase 1, en une seule.

- **Le parc passe de neuf cases sur six à treize sur sept**, `x ∈ [14, 26]`, `y ∈ [12, 18]`. Il
  ne mange que de l'herbe : **ni la station, ni les cinq maisons, ni les trois bouches, ni le
  départ, ni les deux façades ne bougent d'un caractère.** Ses quatre entrées existaient déjà
  depuis la phase 1, une au milieu de chaque côté.
- **Le tracé est une spirale à deux anneaux, écrite à la main et vérifiée solvable par calcul
  AVANT d'être posée**, la méthode des crêtes de la phase 4. Les quatre entrées mènent à la
  fontaine en **11, 21, 18 et 10 pas**, et **aucune case du parc n'est orpheline** : on ressort
  toujours.
- `VillageLayout.ValidatePark()` refait cette vérification à chaque construction : un coup de
  crayon dans le labyrinthe ne peut pas enfermer la fontaine en silence.
- `Assets/Scripts/Buildings/Fountain.cs` : le fichier prévu par CLAUDE.md depuis la phase 0, et
  **le premier usage de `NodeType.FountainInlet`**, le seul type du modèle qui n'en avait aucun.
- **La fontaine est une destination**, pas un décor : elle consomme une unité par saison, allume
  une sixième goutte au HUD, et n'est desservie que si le solveur lui trouve une route. À la
  **profondeur 1**, sa route gèle en hiver et se bouche en automne comme celle des trois maisons
  peu profondes.
- **La station passe de 8 à 9 par saison.** Sans cela la fontaine reliée faisait gagner trois
  unités par an au bassin, qui saturait vers la troisième année.
- `FloodView` gagne une troisième eau : **le jet de la fontaine**, sur les cases praticables
  autour du bassin. Le bassin bloque, donc c'est autour de lui que l'eau déborde.
- `PlaceholderArtGenerator` : le bassin de la fontaine. **123 textures, 66 tuiles.**
- **Règle 9 vérifiée** : aucun type nouveau, aucune référence d'assembly à ajouter.

### Les nombres, avec la station à 9

| Saison | Desservies | Arrivant | Traité | Absorbé | Relâché | Bassin |
|---|---|---|---|---|---|---|
| Automne | 6/6 | 14 | 9 | 5 | 0 | **5** |
| Hiver | 6/6 | 7 | 7 | 0 | 2 | **3** |
| Printemps | 6/6 | 8 | 8 | 0 | 1 | **2** |
| Été | 6/6 | 6 | 6 | 0 | 2 | **0** |

**La suite du bassin, 5 / 3 / 2 / 0, est exactement celle de la phase 8.** Seuls l'arrivant et
le traité gagnent une unité chacun. Le tableau de la phase 8 reste vrai dans sa forme ; ses
chiffres, eux, valaient pour cinq destinations et une station à 8.

**Comme en phase 9b, ce tableau demande d'adapter les tuyaux** : grillagé avant l'automne, isolé
avant l'hiver. Tout laisser en grillagé donne 2/6 desservies en hiver, et 3 d'arrivant.

**`Lost` plafonne toujours à 5** — 14 arrivant moins 9 traités. La table d'étalement valait
alors 0 / 3 / 15 / 38 / 71 / 113. **Périmée depuis la phase 12b** : voir la table à jour dans
cette phase. Elle dépend du nombre de bouches et du nombre de cases bloquantes, et elle a
maintenant été fausse deux fois pour avoir été crue « valable telle quelle ».

## Phase 11, vérifications faites

- Compilation relue par le pont MCP : **zéro erreur, zéro warning**, hors le warning du package
  MCP.
- Menus dans l'ordre, `EditorApplication.isPlaying` vérifié avant, pas de mémoire : la leçon de
  la phase 10 a servi.
- **Plan du village relu par script** : bornes du village inchangées, six portails aux mêmes
  cases, fontaine en (20, 15) **et non praticable**, 81 cases de parc praticables pour 54
  bloquantes, aucun renderer hors de la famille `Surface_*`.
- **Sous-sol relu** : 88 cases praticables, l'alcôve (20, 15) ouverte à la profondeur 1, huit
  nœuds permanents dont `FountainInlet` en (20, 15), station à 9, et **les cinq routes de maison
  toujours à 57 / 46 / 31 / 45 / 6 segments**. La route de la fontaine fait 36 segments, la
  longueur calculée avant que l'alcôve soit creusée.
- Play depuis Boot, par injection clavier :
  - **le labyrinthe bloque et guide** : flèche droite depuis l'entrée ouest, arrêt net en
    (15, 15) contre la haie ; flèche haut, arrêt en (15, 17) dans le couloir du haut ;
  - la fontaine est **sèche tant qu'elle n'est pas reliée**, et le HUD montre **six gouttes**
    dont aucune allumée ;
  - **reliée, elle jaillit** et les six gouttes s'allument ;
  - **l'année suit le tableau ci-dessus, au chiffre près**, avec les tuyaux adaptés ;
  - **l'hiver l'arrête** : tout en grillagé, la fontaine n'est plus desservie, 2/6, et son eau
    disparaît ; **en isolé, elle repart**, 6/6, et son eau revient ;
  - **une partie de la phase 10 se relit sans une erreur** : 81 nœuds, hiver, bassin à 7, une
    résolution pour le chargement, aucune écriture ;
  - console **entièrement vide** sur une session complète.
- Captures : le parc vu d'ensemble, et la fontaine qui jaillit au centre du labyrinthe.
- `git diff ProjectSettings/` : **vide**.

### Le bug trouvé au test : deux lignes identiques

Poser le marqueur de l'alcôve par un remplacement de chaîne a touché la mauvaise ligne : les
lignes `y = 15` et `y = 16` du sous-sol sont **identiques au caractère près**, et
`string.Replace(..., 1)` remplace la première rencontrée. Le nœud est sorti en (20, 16), une
case au-dessus de la fontaine.

**Un plan ASCII se modifie par indice de ligne, jamais par contenu.** Le contenu n'est pas une
clé : rien n'oblige deux lignes d'une carte à différer.

## Phase 12a, ce qui est fait

Aucun contenu neuf. Rendre **bruyante** toute rupture que 12b à 12e pourraient causer, et
réparer ce qu'un audit de huit dimensions a trouvé.

- **`BuildAllScenes` s'arrête au premier refus.** Les cinq `Build()` rendent `bool` ; jusqu'ici
  ils étaient `void` et personne ne lisait leur résultat. Un générateur qui refusait laissait les
  quatre autres se construire, et le journal annonçait quand même « les cinq scènes sont
  construites ». Deux cartes désalignées case pour case n'auraient rien dit.
- **`UndergroundLayout.ValidateDepthPuzzle`** : parcours en largeur **depuis la station**, à
  profondeur non croissante en remontant — la règle de CLAUDE.md lue à l'envers. Une destination
  que ce parcours n'atteint pas ne pourra jamais être desservie, quoi que le joueur creuse.
  Rien ne vérifiait le plan des profondeurs depuis la phase 3.
- **`UndergroundSceneBuilder.ValidateWaterBudget`** : refuse de construire un monde
  insoutenable, et **dit la capacité attendue**. Le réglage avait été refait à la main deux fois,
  en phase 8 puis en phase 11, chaque fois en découvrant après coup qu'une destination de plus
  faisait déborder le village pour toujours.
- **`ValidateAgainstVillage` apparie enfin la fontaine**, seul couple surface/sous-sol que
  personne ne vérifiait : les deux étaient en (20, 15) par la seule discipline de la main.
- **`ValidatePark` réécrit.** L'ancien partait de quatre entrées écrites à la main et lançait
  quatre parcours qui, par inondation, exploraient tous le même ensemble : il prouvait quatre
  fois la même chose, et jamais celle que son résumé promettait. Un seul parcours depuis le
  départ prouve les deux : aucune case enfermée, et la fontaine atteignable. Rien n'est écrit à
  la main, donc rien ne se périme quand le parc grandit.
- **Une seule liste de blocage.** `PaintVillage` interrogeait un second `switch` maintenu à la
  main en parallèle de `VillageLayout.IsWalkable` : un marqueur ajouté à l'une et oublié à
  l'autre donnait une case franchissable qui ne se peint pas. C'est le plan qui tranche, et lui
  seul.
- **`Teleport` refuse une case hors carte**, avec un message.
- **`PixelFont` gagne les chiffres, `-`, `!`, `?`, `Ç`, et `Ô Î Û Ù`.** Et `CanRender` refuse
  bruyamment un caractère inconnu : jusqu'ici « AIDE-MOI ! » sortait « AIDE MOI  », sans un mot,
  tout caractère inconnu ne dessinant rien tout en avançant d'une cellule.
- **Le plafond de texture est calculé, plus écrit à la main.** Il est lu dans l'en-tête du PNG
  posé sur le disque. Une image plus large que son plafond était divisée par deux **en silence** ;
  la phase 7 l'a évité de justesse sur AMSTERDAM, la phase 9a sur les phrases de l'ouvrier.
- **`SaveData.CurrentVersion` passe à 2**, la première fois depuis la phase 6, sur le critère que
  la phase 6 avait écrit : c'est le **sens** des cases qui change.

### L'eau morte : le retour qui manquait

`FlowSolver.TraceToPlant` jetait son ensemble de cases atteintes dès qu'il échouait. Conséquence
exacte : **une route de cinquante-sept segments fausse d'une seule case était rendue entièrement
blanche**, comme un tuyau qu'on vient de poser. Le joueur apprenait « ça ne marche pas » après
soixante-treize appuis sur Espace, et rien ne lui disait où.

Le solveur publie désormais cette frontière, et `PipeNetworkView` la peint d'une **sixième
couleur**, un bleu grisé de la même famille que l'eau vive mais éteint : « elle est montée
jusqu'ici et elle s'arrête ». L'eau vive passe avant l'eau morte, une case pouvant porter pour
une maison et rester sur la branche morte d'une autre.

C'était aussi la seule chose que l'agrandissement de la carte dégradait **linéairement avec la
longueur des routes**, et c'est ce qu'un personnage-guide devra pouvoir montrer : on ne peut pas
expliquer *où* ça casse tant que le jeu l'ignore.

### Trois affirmations fausses du journal, corrigées

- **La table d'étalement des flaques** valait 0 / 3 / 15 / 38 / **71** / **113**, et non 73 / 118,
  depuis la phase 11 : le labyrinthe de haies a porté le village de 200 à 252 cases bloquantes.
  La phase 11 affirmait que la table « reste valable telle quelle » sans l'avoir revérifiée.
- **La répartition des profondeurs** est **804 / 336 / 60**, et non 766 / 374 / 60. Faux depuis
  la phase 3.
- **Le printemps ne débouche pas.** `ApplyToSegment` ne remet que `IsFrozen` à faux ; un bouchon
  n'est effacé que par une réparation à la main, et ils s'accumulent d'année en année. Le tableau
  des saisons dit « dégèle tout », ce qui n'est vrai que du gel.

## Phase 12a, vérifications faites

- Compilation relue par le pont MCP : **zéro erreur, zéro warning**, hors le warning du package
  MCP.
- Les quatre validateurs **passent** sur la carte actuelle, connue bonne.
- **Et surtout, ils REFUSENT sur un monde saboté.** Un validateur qui dit toujours oui ne vaut
  rien : j'ai mis l'alcôve de la maison (34, 4) à la profondeur 3 et glissé l'arrivée de la
  fontaine d'une case, puis reconstruit. `ValidateDepthPuzzle` et `ValidateAgainstVillage` ont
  tous deux refusé, en nommant **la case et la raison exactes** : « La destination (34, 4),
  profondeur 3, ne peut JAMAIS être desservie », « Arrivée de fontaine orpheline en (20, 14) ».
  Le sabotage a ensuite été retiré.
- Les validateurs **journalisent** ce qu'ils ont prouvé : « 1197 cases vivantes sur 1200,
  7 destinations atteignables », « 35 arrivant sur l'année contre 36 traités, station à 9 ».
- Plafonds de texture relus un par un : 256 pour les phrases larges, 64 pour le plan du village,
  32 pour une tuile de seize. Aucune image réduite.
- Les nouveaux glyphes relus sur planche : chiffres, trait d'union, point d'exclamation, point
  d'interrogation, cédille, et les quatre accents circonflexes et graves manquants.
- Play depuis Boot :
  - **une partie de version 1 est mise de côté bruyamment** et n'est PAS rejouée : huit nœuds
    permanents et rien d'autre, message en console avec le chemin du fichier gardé ;
  - le village compte toujours **252 cases bloquantes** : la liste unique n'a rien changé ;
  - **l'eau morte** : une route de 32 cases dont **une seule manque** montre 20 cases mortes du
    côté de la maison contre 1 du côté de la station, la maison en fait partie, la station non,
    et la frontière s'arrête exactement au trou ;
  - console **entièrement vide** en dehors du message voulu de mise de côté.
- `git diff ProjectSettings/` : vide.

## Phase 12b, ce qui est fait

La carte passe de **40x30 à 64x45**, 2880 cases, 3,2 x 4,0 écrans. Rien de neuf en décor : les
deux plans réécrits, les profondeurs redessinées, un grand labyrinthe, le pré-creusement, les
bouches et les destinations. Tout est repris par les quatre validateurs de la phase 12a.

**Le sens de l'agrandissement n'est pas neutre.** `At` fait `Rows[Height - 1 - y][x]` : ajouter
les lignes **en tête** des tableaux et les caractères **en fin** de ligne préserve rigoureusement
chaque couple (x, y). Le village a donc grandi vers le **nord** et vers l'**est**. La station
reste en (6, 25), l'atelier et l'usine à tuyaux sur leurs cases, et le départ du joueur devant
l'entrée ouest du parc. Conséquence heureuse : rien n'a eu besoin de bouger côté code — le plan
du HUD tient à l'échelle 4 (256x180 dans 320x180), `CameraFollow.halfView` se déduit de l'écran
et non de la carte, et `maxTextureSize` est calculé depuis l'en-tête du PNG depuis la phase 12a.

### Les trois décisions chiffrées, prises avant d'écrire

- **Treize destinations**, douze maisons et la fontaine. C'est le plafond dur de la rangée de
  gouttes : à quatorze elle chevauche le picto de saison. La densité constante en aurait voulu
  14,4. La capacité de la station passe donc à **16**, la règle `C = D + 3` de la phase 12.
  Elle reste un nombre écrit à la main ; c'est la phase 12d qui la fera grandir en jeu.
- **Quarante et un segments à profondeur 1 sur la route la plus longue.** C'est le rythme de
  l'hiver, et il se règle au rayon près : le nombre vaut **exactement** `r_destination` moins le
  rayon extérieur de la couronne. La couronne va donc jusqu'à r = 31, ce qui laisse 59 % de la
  carte à profondeur 1 contre 67 % avant — les proportions d'aujourd'hui, reproduites.
- **Un seul labyrinthe de haies, 27 x 17.** L'écran montre 20 x 11,25 cases : il déborde donc du
  cadre dans les deux axes, la caméra y défile, et il ne se résout plus d'un coup d'œil mais
  **de mémoire**. C'est le but, pas un effet de bord.

### Le plan des profondeurs est une formule, plus un dessin

L'ancien l'était déjà sans le dire : décodé case par case, il se réduit à quatre nombres et deux
listes de portes, à **zéro écart sur 1200 cases**. Le nouveau l'écrit franchement :

```
r = distance de MANHATTAN à la station (6, 25)
r <= 8   -> profondeur 3      r > 31  -> profondeur 1      sinon profondeur 2
sauf r = 12, 19 et 26, les TROIS CRÊTES, forcées à 1 hors de leurs portes
```

Trois crêtes au lieu de deux : la couronne est 1,56 fois plus large, deux crêtes y seraient plus
clairsemées qu'avant. Chaque crête est percée d'une **porte de cinq cases**, et les trois portes
sont à 90 degrés l'une de l'autre — **est** pour r = 26, **sud** pour r = 19, **nord** pour
r = 12. On les traverse donc en zigzag, et c'est ce zigzag qui fait le puzzle.

**La position des portes compte autant que leur existence.** Premier jet, la porte extérieure
était au nord-ouest : les maisons de l'est devaient alors longer la plaine pour la rejoindre, et
la pire route montait à **61** segments à profondeur 1 au lieu de 41. Mise à l'est, du côté où la
carte a grandi, elle retombe exactement sur les 41 voulus. Un rayon règle le nombre, un azimut
règle le détour.

Répartition : **1809 / 930 / 141**. 2844 cases restent vivantes sur 2880.

### Le pré-creusement est du contenu, pas de la mise à l'échelle

217 galeries livrées ouvertes, **7,5 %** de la carte, le ratio de la phase 11 (88 sur 1200).
Mais le ratio ne suffit pas : **où** elles sont décide de tout. Posées d'abord en chambres autour
des échelles, elles n'économisaient que 16 % des appuis. Réécrites comme l'**ancien collecteur du
village** — le réseau qui existait avant que le joueur arrive, posé sous les routes de la plaine —
les cinq maisons de l'est ne demandent plus **aucun creusement**, seulement la pose.

Le collecteur **s'arrête à r = 33**, en deçà de la crête la plus extérieure. Aucune case de porte
n'est jamais livrée creusée : les portes restent à trouver, et le plan le vérifie avant d'être
émis.

**Coût réel, à modèle égal** (arbre partagé, une case creusée ou un tuyau posé = un appui) :
368 appuis pour 13 destinations, soit **28 par destination**, contre 121 pour 7 sur l'ancienne
carte, soit 17. La carte est 2,4 fois plus grande, le travail 3,0 fois plus long. Environ neuf
minutes de pose pure à 1,5 s par appui délibéré, hors marche.

### Les bouches et l'invariant

**Sept bouches d'égout**, la densité de la phase 1 (3 pour 1200 cases). L'invariant tenu :
**jamais plus de 23 pas** entre une case et l'échelle la plus proche, contre 22 avant et un
plafond fixé à 25.

### La table d'étalement des flaques, recalculée

Elle dépend du **nombre de bouches** et du **nombre de cases bloquantes**, et elle a maintenant
été fausse deux fois pour avoir été reportée sans être revérifiée. Recalculée sur les plans
réellement écrits — 7 bouches, **539 cases bloquantes** sur 2880 :

| `Lost` | Rayon | Cases mouillées |
|---|---|---|
| 0 | — | 0 |
| 1 | 0 | 7 |
| 2 | 1 | 35 |
| 3 | 2 | 89 |
| 4 | 3 | 172 |
| 5 | 4 | **280** |
| 6 | 5 | 412 |

`Lost` plafonne toujours à **5** : l'arrivant plafonne à 21, treize destinations plus huit de
pluie d'automne, la station en traite seize. À `Lost` = 5 la flaque couvre 280 cases, **9,7 %**
du village — la phase 11 en couvrait 113 sur 1200, soit 9,4 %. La proportion est tenue.

### Note d'atelier : un plan dessiné à la main se fragmente sans le dire

Le labyrinthe a d'abord été **dessiné caractère par caractère** sur un treillis strict, cases en
positions impaires et murs en positions paires. Il s'est fragmenté en **seize composantes
connexes**. Rien dans le dessin ne le montrait, et `ValidatePark` ne l'aurait dit qu'à la
construction.

Il est donc **creusé en polylignes** : une liste de segments droits, chacun devant toucher le
tracé déjà posé, l'assertion refusant le contraire. La connexité devient **structurelle** — une
poche morte n'est plus dessinable par inadvertance. Le vérificateur ne mesure plus que la
difficulté : entrées à **24, 32, 34 et 34 pas** de la fontaine, case la plus lointaine à 47 pas,
217 cases de couloir sur 459. Aucune entrée ne voit la fontaine : le cadre montre 20 x 11 cases.

## Phase 12b, vérifications faites

- Compilation relue par le pont MCP : **zéro erreur, zéro warning**, hors le warning du package
  MCP venu de `Library/PackageCache`.
- **Solvabilité vérifiée PAR CALCUL avant écriture**, la méthode des crêtes de la phase 4. Un
  atelier de conception assemble les deux plans, les vérifie sur dix points, et **n'émet le C#
  que si tout passe**. Il a refusé quatre fois avant de céder : une route traversait l'herbe du
  bassin, le labyrinthe avait onze cases enfermées, les portes de crête étaient mal placées, et
  la fontaine se posait sur une ligne du labyrinthe au lieu d'une autre.
- Les quatre validateurs **passent** sur la carte neuve, et journalisent ce qu'ils ont prouvé :
  « 2844 cases vivantes sur 2880, 14 destinations atteignables sur 14 », « 13 destinations,
  63 arrivant sur l'année contre 64 traités, station à 16 par saison ».
- **Et ils REFUSENT sur un monde saboté**, chacun en nommant la case et la raison :
  - **porte de la crête r = 26 bouchée** → les cases vivantes tombent de 2844 à **934**, neuf
    destinations sont nommées une par une, et `BuildAllScenes` **s'arrête** : « Construction
    interrompue : une scène a refusé », Build Settings non touchés ;
  - **case (29, 14) murée**, la seule voisine de la fontaine → « La fontaine (28, 14) n'est
    atteignable depuis aucune case accessible : le labyrinthe l'enferme » ;
  - **arrivée de fontaine effacée au sous-sol** → « 1 fontaine(s) en surface mais 0 arrivée(s) au
    sous-sol » ;
  - **quatorzième maison posée** → « 14 destinations apportent 67 sur l'année, la station n'en
    traite que 64. Capacité attendue : 17 par saison ».
  Le sabotage a ensuite été retiré, et les plans revérifiés case par case.
- **Un bilan qui ne pouvait pas dire non, corrigé au passage.** `ValidateDepthPuzzle` imprimait
  `Destinations().Count` : il annonçait « 14 destinations atteignables » sur la ligne qui suivait
  neuf refus. Il compte désormais les atteintes, et dit « x sur y ».
- **Les deux plans relus par script** après écriture : 12 maisons pour 12 alcôves, 7 bouches pour
  7 échelles, la fontaine pour son arrivée, la station au fond, le bassin sous herbe stricte avec
  sa chambre creusée.
- **Play depuis Boot, par injection clavier réelle**, `Application.isFocused` vérifié à chaque
  appui — **zéro image injectée sans focus**, le pilote attendant le focus plutôt que de tricher :
  - **le labyrinthe traversé** du départ (14, 16) à l'unique voisine de la fontaine (29, 14),
    35 pas en 9,1 secondes ;
  - descente par la bouche (27, 3) ;
  - **la maison la plus lointaine (61, 8) reliée**, route de 148 segments, **263 appuis sur
    Espace** en 53,9 secondes, cinq destinations desservies au bout ;
  - **l'eau morte fait son travail** : la frontière grossit à mesure que la route avance —
    14, 39, 63, 88, 113, 136 cases — puis retombe à 8 quand la route atteint la station ;
  - **une route volontairement fausse** de 32 cases, en ligne droite depuis la maison (27, 35),
    traverse la crête r = 26 ailleurs qu'à sa porte : l'eau monte jusqu'en (23, 35), profondeur
    2, et **s'arrête net** sur (22, 35), profondeur 1. Une seule case peu profonde au milieu de
    la profondeur 2 coupe toute la route, et le jeu le montre enfin.
- **Horloge accélérée**, une année entière saison par saison sur le réseau posé :

  | | gelés | bouchés | arrivant | traité | perdu |
  |---|---|---|---|---|---|
  | Été | 0 | 0 | 5 | 5 | 0 |
  | Automne | 0 | 19 | 11 | 11 | 0 |
  | **Hiver** | **50** | 19 | 4 | 4 | 0 |
  | Printemps | 0 | **19** | 5 | 5 | 0 |

  Les **50** segments gelés sont **exactement** les 50 segments à profondeur 1 du réseau :
  probabilité 1, comme le réglage l'annonçait. Le bilan tient à toutes les saisons avec la
  capacité 16. Et le printemps dégèle mais **ne débouche pas** — la panne connue de l'audit, ici
  en clair : sur 96 ticks accélérés, 50 segments finissent bouchés et plus rien n'est desservi.
- **Captures** dans `Captures/`, dossier ignoré par git comme le reste des artefacts de
  vérification : la carte entière, le labyrinthe 27 x 17, le sous-sol avec la route et sa
  frontière, et le gros plan sur l'eau morte à la crête.
- `git diff ProjectSettings/` : **vide**. `ProjectAuditorSettings.asset`, qu'Unity avait réécrit
  de lui-même, a été remis en état.

### Note d'atelier : le SceneRouter éteint TOUS les objets racines d'une couche

`SetLayerEnabled` parcourt `scene.GetRootGameObjects()` et éteint **chacun** d'eux, pas seulement
la racine nommée de la couche. Un objet posé dans la scène Surface s'éteint donc en descendant, et
sa coroutine s'arrête **sans un mot**. Le pilote de test l'a appris en mourant en silence au
moment de la descente. Tout objet qui doit survivre à un changement de couche appartient à
Persistent, ou passe par `DontDestroyOnLoad`.

### Note d'atelier : relâcher une touche à l'arrivée fait dépasser d'une case

`PlayerController.currentCell` ne change qu'à **l'arrivée**, alors que le personnage se déplace en
continu. Relâcher la flèche au moment où la case change la laisse tenue une image de trop : le pas
suivant est déjà engagé, et le personnage dépasse d'une case. Dans un couloir cela se rattrape
tout seul ; **sur la case d'arrivée il oscille autour d'elle sans jamais s'arrêter**. Le remède
est de relâcher **avant** l'arrivée, à une demi-case du centre, le pas étant déjà engagé.

Cela ne concerne que le pilotage automatique — un humain relâche la flèche quand il voit qu'il est
arrivé, une image plus tard, et la case suivante est de toute façon celle qu'il voulait.

## Phase 12c, ce qui est fait

Le décor. Aucun changement de règle : la carte, les profondeurs, le pré-creusement et le bilan
de l'eau sont ceux de la phase 12b, au chiffre près.

### Seize tuiles par famille, sur le patron exact des canalisations

Un labyrinthe de **242 cases de haie** peint avec une seule tuile, ce sont 242 carrés verts
identiques séparés d'un liseré : on ne lit plus un mur, on lit un damier. Idem pour les
**245 cases de route**, qui formaient une nappe beige sans direction.

Les deux familles reçoivent donc un **masque de raccord** — bit 0 nord, 1 est, 2 sud, 3 ouest,
la convention de `BuildPipe` depuis la phase 3, et désormais la seule convention de direction du
projet. La haie pousse vers ses voisines et montre sa tranche sur ses côtés libres ; la chaussée
s'étend vers les siennes et se borde d'un accotement. La bande blanche ne se pose que sur une
portion **droite** : un virage et un carrefour n'en portent pas, comme en vrai.

Deux calculs de masque, et la distinction compte : `GroundMask` interroge `GroundAt`, si bien
qu'une bouche d'égout, une porte ou un panneau — qui posent tous du chemin sous eux — ne coupent
pas la route ; `BlockingMask` interroge `At`, dont le hors-carte rend `Hedge`, si bien que la
bordure de haies se prolonge au-delà du bord au lieu de s'y interrompre.

### La fontaine, séparée en deux images

Sa tuile bloquante et son sprite sortaient du **même fichier de 16x16**, ce qui la clouait à la
taille d'une case : elle ne pouvait pas grandir sans que le mur du parc grandisse avec elle.
Séparées sur le patron exact de `house.png` / `tile_house.png`, elle fait maintenant 16x24, avec
bassin, colonne, vasque et jet, et déborde vers le haut comme une maison.

### Les arbres bloquent, et c'est une contrainte de jeu

131 arbres, en bosquets et isolés. Ils **bloquent**, recommandation de l'audit : un décor qu'on
traverse n'est pas un décor, c'est un motif de sol. Deux règles, vérifiées à chaque construction
par `ValidateDecor` :

- **Aucun arbre à moins de cinq pas d'une bouche d'égout.** Le rayon de la flaque vaut
  `Lost - 1` et `Lost` plafonne à 5 : une case bloquante ne prend pas l'eau, donc un arbre planté
  plus près retirerait des cases au débordement **sans que rien ne le dise**. Le débordement est
  le seul retour permanent du jeu ; on ne le rogne pas pour un arbre. Mesure : la table
  d'étalement est **inchangée jusqu'à Lost = 5** malgré 96 cases bloquantes de plus.
- **Rien n'est enfermé, nulle part.** C'est ce que la phase 12c a demandé au validateur en
  l'élargissant.

### Les panneaux sont des repères, et ils ne bloquent jamais

Le catalogue compte **huit panneaux** dessinés par code : danger, stop, sens interdit, cédez le
passage, et quatre panneaux de direction, un par point cardinal. Seize en rue, **douze aux
carrefours de galeries**.

**Les panneaux de galerie règlent un vrai manque.** Le sous-sol n'avait aucun repère : trois
nuances de brun, des galeries qui se ressemblent toutes, et une carte passée de 1200 à
2880 cases. La flèche montre **la station**, calculée une fois à la construction depuis le plan.
Un panneau de direction ne dit pas le chemin, il dit la direction : le puzzle des profondeurs
reste entier. Les douze carrefours ont été relevés **par calcul** sur le plan — 44 candidats —
puis choisis à la main, un par secteur.

**Décision, à ta demande : ce catalogue est le PREMIER dessin des panneaux, pas le troisième.**
L'usine à panneaux de la phase 13 le reprend au lieu d'en créer un second, ce qui conserve la
décision du 3 septembre — un seul passage d'art, règle 4 de CLAUDE.md.

### `ValidatePark` devient `ValidateVillage`

Elle ne prouvait plus seulement le parc. Sa composante connexe depuis le départ doit maintenant
contenir **toute** case praticable de la carte : un arbre qui détache un coin de plaine est
refusé par construction, ce qu'aucune version précédente n'aurait vu.

## Phase 12c, vérifications faites

- Compilation relue par le pont MCP : **zéro erreur, zéro warning**, hors celui du package MCP.
- `ValidateVillage` passe et journalise : « 2245 cases praticables, toutes reliées ; 131 arbres,
  16 panneaux ». Les trois autres validateurs sont inchangés et passent.
- **Sabotage du décor, trois façons, trois refus nommés** :
  - un arbre planté en (11, 11) → « L'arbre (11, 11) est à 2 pas de la bouche (9, 11) : il mange
    une case de flaque. Le rayon du débordement plafonne à 4 » ;
  - deux arbres cernant le coin sud-est → « La case (62, 1), « . », est enfermée » ;
  - un panneau muré au cœur du labyrinthe → « Le panneau (27, 15) n'est atteignable depuis aucune
    case accessible : personne ne le lira jamais ».
- **Un contrôle qui ne pouvait pas dire non, remplacé.** La première version vérifiait qu'un
  panneau ne bloque pas — mais « I » n'est pas dans la liste de blocage, donc la réponse était
  oui quoi qu'il arrive. Il vérifie désormais qu'il est **atteignable**, ce qui peut échouer et
  vient d'échouer.
- **Le compte de l'art est mesuré, plus écrit à la main.** Le journal annonçait « 123 textures,
  66 tuiles » quelle que soit la réalité : la phase 12c en a ajouté quarante-deux sans que le
  nombre bouge d'une unité. Il compte maintenant les fichiers sur le disque.
- Captures relues : le labyrinthe se lit comme un mur continu et non comme un damier, les routes
  ont leurs virages et leurs carrefours, la fontaine a son jet, et les douze flèches du sous-sol
  pointent toutes vers la station.
- `git diff ProjectSettings/` : **vide**.

## Phase 12d, ce qui est fait

La capacité de la station cesse d'être un nombre écrit à la main. Elle vaut
`baseCapacity + bassins`, et **le joueur construit ses bassins en appuyant sur Espace devant
l'arrivée de la station, sous terre**.

### Pourquoi, et pas seulement comment

- **Le débordement redevient un retour permanent.** À capacité écrite à la main et réglée sur
  `D + 3`, une fois le bassin d'orage relié, `Lost` valait zéro **pour toujours** : le
  débordement n'apprenait plus rien, et sa fenêtre de visibilité se refermait d'autant plus tôt
  que le village était grand. Mesuré en jeu : station à **zéro bassin**, l'automne perd **5** ;
  à **trois bassins**, il n'en perd plus que **2**. Chaque bassin se voit dans la flaque.
- **La règle est dérivée, pas inventée.** Sur l'année, `4S + 11 ≤ 4C` a pour minimum entier
  `C = S + 3`. Avec `baseCapacity = 3` — la pluie annuelle divisée par quatre, arrondie
  au-dessus — cela fait **exactement un bassin par destination reliée**. La station livrée
  encaisse la pluie et rien d'autre.
- **Rien ne se paie.** Il n'y a pas de monnaie dans ce jeu et il n'y en aura pas. La difficulté
  est de **comprendre** qu'il faut agrandir, pas d'amasser de quoi le faire.
- **Le créneau était libre.** Le nœud `PlantInlet` n'offrait aucune action tant qu'il n'était pas
  abîmé. C'est le seul endroit du monde où ce geste a un sens.

### La station grossit à l'écran

Treize cuves sont posées une fois pour toutes sur le sol de l'enceinte, **éteintes**, et
`PlantBasinsView` en allume autant qu'il y a de bassins. Rien ne s'instancie en jeu.

Elle vit dans la scène **Surface** alors que le geste se fait **sous terre** : la couche est donc
éteinte au moment où le bassin s'ajoute. Le composant s'abonne dans `OnEnable`, se désabonne dans
`OnDisable`, et **se réapplique au rallumage** — la règle du projet depuis la phase 1, et c'est
ici qu'elle compte le plus, puisqu'on remonte exactement pour voir ce qu'on vient de construire.

Les emplacements sont les cases de sol dont les **quatre** voisines appartiennent encore à la
station : le premier jet posait une cuve dans l'ouverture de l'enceinte, où elle bouchait
visuellement la porte sans rien bloquer.

### Ce qui se sauvegarde

`plantBasins`, ajouté **à la fin** de `SaveData`. C'est du **progrès de joueur** et non une donnée
dérivée : les maisons desservies, l'eau du village et l'état de la fontaine se recalculent, les
bassins non. `CurrentVersion` reste à **2** : un champ ajouté se relit sans rien casser, et une
partie d'avant arrive avec zéro bassin — ce qui est exactement l'état de départ voulu.

## Phase 12d, vérifications faites

- Compilation relue par le pont MCP : **zéro erreur, zéro warning**, hors celui du package MCP.
- **Un appui réel sur Espace = un bassin.** Injection clavier vraie, `Application.isFocused`
  vérifié, temps figé pour n'obtenir qu'un changement d'orientation : le personnage en (6, 24)
  regarde l'arrivée en (6, 25), un Espace, et la capacité passe de **3 à 4**.
- **Le plafond tient** : 20 tentatives de plus donnent 13 bassins, capacité 16, **8 refus**, et
  `CanGrow` passe à faux — le picto disparaît alors, et Espace ne fait plus rien devant
  l'arrivée. Aucun échec puni : on ne peut simplement plus agrandir ce qui est complet.
- **La sauvegarde fait l'aller-retour** : trois bassins écrits dans `partie.json`, relus après
  un arrêt et une reprise, capacité 6, et **trois cuves allumées** — donc la vue s'est bien
  réappliquée au rallumage de la couche.
- **Une année saison par saison, à deux capacités** :

  | | à 0 bassin (capacité 3) | à 3 bassins (capacité 6) |
  |---|---|---|
  | Automne : arrivant | 8 | 8 |
  | Automne : traité | 3 | 6 |
  | **Automne : perdu** | **5** | **2** |

### Un validateur qui ne pouvait plus dire non

`ValidateWaterBudget` comparait `4D + R` à `4C`. Depuis que `C` vaut `D + 3` et **se dérive du
plan**, le traité vaut `4D + 12` et l'arrivant `4D + 11` : la comparaison est vraie **quel que
soit le nombre de maisons**. Le validateur était devenu inutile sans que rien ne le dise, exactement
comme le bilan des profondeurs qui annonçait « 14 destinations atteignables » sur la ligne suivant
neuf refus.

Réécrit sur ce qui peut réellement casser :

1. **La pluie passe la marge de la règle.** `C = D + 3` laisse 12 unités par an à la pluie ; elle
   en vaut 11. C'est un champ sérialisé sur un ScriptableObject, modifiable d'un clic.
2. **Le bassin n'encaisse plus la pointe** de la pire saison.
3. **La rangée de gouttes déborde de l'écran** au-delà de treize destinations.

**Vérifié par sabotage** : pluie d'automne portée de 8 à 10 → « Pluie INSOUTENABLE : 13 sur
l'année alors que la règle « capacité = destinations + 3 » n'en laisse que 12. Le bassin
dériverait de 1 par an, saturerait, et le village déborderait pour toujours. Baisse une saison, ou
relève la constante 3 de PlantBaseCapacity. » Remise à 8, il repasse.

Le journal dit maintenant ce qu'il a prouvé : « 13 destinations, 11 de pluie sur l'année pour 12
de marge, pointe de 5 pour un bassin de 10. Station de 3 à 16 par saison, soit 13 bassins à
construire. »

- `git diff ProjectSettings/` : **vide**.

## Les documents du projet

- **CLAUDE.md** — les contraintes non négociables. Ne se discute pas.
- **PROGRESS.md** — ce fichier. Le journal : ce qui est fait, les décisions et leur pourquoi,
  les placeholders, les questions ouvertes.
- **PIEGES.md** — les pièges tombés au moins une fois, rassemblés. À relire avant d'écrire.
- **PLAN-PHASE-NN.md** — le plan de chaque phase, validé avant implémentation.
- **AUDIT-PHASE-12.md** — l'audit d'impact de l'agrandissement de la carte, 4 septembre 2026.
  Huit dimensions, chacune re-vérifiée adversarialement. **Il ne se refera pas** : les treize
  pannes silencieuses, les chiffrages et l'architecture des guides n'existent que là.

## Prochaine étape, phase 12b

La carte 64x45 et le grand labyrinthe, désormais couverts par les filets de 12a. Ce que les
phases précédentes laissent en place :

- **Tout le modèle de CLAUDE.md est désormais utilisé** : `FountainInlet` était le dernier type
  sans usage, et `Buildings/Fountain` le dernier fichier de l'arborescence jamais écrit.
- **Le patron du nœud permanent** sert cinq fois : station, maisons, bassin, fontaine.
- **Le patron du bâtiment** : façade et porte au plan du village, pièce dans un créneau libre,
  personnage et phrases. **Quatre créneaux de pièce restent libres** dans la scène Interiors,
  et `Villager` accepte déjà plusieurs personnages dans une pièce, ce dont l'usine à panneaux
  aura besoin en phase 12.

## Décisions prises

### Phase 10

- **Deux eaux, et elles ne disent pas la même chose.** Le débordement dit « ton réseau reçoit
  plus que la station ne traite » ; la fuite dit « un tuyau est crevé ici, sous tes pieds ».
  Deux messages, une seule image d'eau : c'est la position qui les distingue, et c'est un
  symbole de moins à apprendre.
- **L'eau sort par les bouches d'égout.** Validé le 3 septembre 2026. Victorien aime les bouches
  d'égout, l'eau sort par là où le réseau aboutit, et elle sort de sous la plaque qu'il a
  choisie en phase 7. La station est écartée : au fond de son enceinte murée, on ne la voit
  presque jamais. Les rues entières sont écartées : une troisième tilemap sur tout le village
  pour un effet qui ne se lit pas mieux.
- **Les trois bouches débordent de la même façon**, pas d'un tiers chacune. Le réseau déborde,
  c'est vrai partout, et il le voit où qu'il se trouve dans le village.
- **Aucune mémoire, aucune sauvegarde.** Validé le 3 septembre 2026. Les deux eaux sont des
  données dérivées, et le projet a la règle depuis la phase 6 : « une donnée dérivée sauvegardée
  est une donnée qui peut mentir ». Conséquence assumée et écrite au plan avant d'être observée :
  après une relance le village est sec jusqu'au tick suivant, mais les flaques de fuite
  reviennent tout de suite.
- **Seule l'usure fuit.** Gelé et bouché sont **bouchés**, pas crevés : rien n'en sort. C'est
  cette distinction qui rend la flaque informative.
- **La règle du « trop abîmé » remonte dans `PipeNetwork`.** Le rendu du sous-sol et la flaque de
  surface la lisaient chacun de leur côté ; deux règles pour un même mot finiraient par diverger,
  comme le seuil de 0,3 l'aurait fait s'il était resté en double en phase 5.
- **`FloodView` ne s'abonne PAS à `PipeNetwork.Changed`**, et ce n'est pas un oubli. Creuser,
  poser, enlever et réparer n'existent que **sous terre**, donc l'état des tuyaux ne peut changer
  que pendant que la Surface est éteinte, ou à un tick. Le rallumage et le tick couvrent donc
  tous les cas, sans s'abonner à travers une couche éteinte.
- **Un seul composant pour les deux eaux.** Elles partagent la tilemap et le même geste de
  repeinte ; deux composants se disputeraient la même tilemap.
- **L'eau est semi-transparente.** Un bleu opaque ferait un carré plein qui cacherait le village ;
  une eau qui laisse voir le sol dessous se lit tout de suite comme de l'eau.
- **Rien ne bloque, rien ne punit.** L'eau se peint sur `Surface_Water`, jamais sur la couche
  bloquante. Vérifié en jeu : 113 cases mouillées, 0 sur du bloquant, et le personnage traverse.
- **Aucun changement au bilan de l'eau ni à l'ordre du tick.** La question ouverte de la phase 8
  reste ouverte ; cette phase ne fait que montrer un nombre déjà calculé.
- **La fontaine reste au parc.** `NodeType.FountainInlet` et `Buildings/Fountain` attendent la
  phase 11, bien que le commentaire de la phase 0 range la fontaine avec les flaques.

### Phase 9b

- **La bulle « on peut lui parler » est au-dessus de la tête du PERSONNAGE**, seule exception à
  la règle « le picto d'action est au-dessus de la tête du joueur ». Tranché le 3 septembre 2026
  après l'avoir vu en jeu : la place habituelle tombe exactement sur le visage de qui se tient
  une case plus haut. Un picto qui cache ce qu'il désigne ne désigne rien. Les huit autres
  pictos ne bougent pas.
- **Le type vit sur le nœud, la résistance effective sur le segment.** Une case porte un tuyau
  d'un type, c'est ce que le joueur voit et ce qu'il pose. Le segment retient **la plus basse
  des deux résistances** et la plus forte des deux usures : il est aussi faible que son bout le
  plus faible, comme il est aussi exposé que son extrémité la moins profonde depuis la phase 5.
- **Une extrémité sans type ne compte pas.** Station, maisons et bassin ne sont pas des tuyaux
  qu'on a choisi de poser ; c'est l'autre bout qui décide seul.
- **`LeafResistance` est une probabilité de tenir**, symétrique exacte de `FrostResistance`.
  Elle vaut 0 ou 1 aujourd'hui, mais le champ reste une probabilité : un type intermédiaire ne
  demanderait pas une ligne de code.
- **L'usure est la même pour les trois.** Un tuyau qui ne s'use pas rendrait la clé inutile, et
  c'est ce geste qui garde le bassin bas.
- **Le motif plutôt que la teinte, et le motif est une nuance du corps.** C'est ce qui le fait
  survivre aux cinq couleurs d'état : teinter multiplie toute la tuile. `PipeType.Tint` reste
  inutilisé plutôt que de casser les assets.
- **La silhouette est identique d'un motif à l'autre.** Seul le remplissage change : deux tuyaux
  de types différents se raccordent à l'œil comme dans le graphe.
- **Les quarante-huit tuiles sont nommées `Tile_Pipe_<motif>_<masque>`**, et les seize de la
  phase 3 sont retirées. Un schéma mixte, seize sans motif plus trente-deux avec, aurait été une
  verrue dont la phase 12 aurait hérité.
- **L'échantillon EST la tuile posée en jeu**, masque est-ouest. Le picto de pose ne peut donc
  pas mentir sur ce qu'il va poser, et il n'y a aucune image de plus à dessiner.
- **Le picto de pose est le tuyau en main**, comme le picto de choix est la plaque en phase 7.
  `picto_pipe` disparaît : un symbole générique ne disait plus rien des trois types.
- **Seules les cases non standard sont écrites**, comme seuls les segments abîmés le sont.
  Chaque ligne du fichier est un choix, pas un état.
- **Le type en main est un entier sauvegardé.** Exception assumée à « on ne sauvegarde pas ce
  qu'on ne relit pas » : redescendre pour découvrir qu'on a repris le standard serait une
  surprise, et le jeu n'en fait pas.
- **`PlayerInteractor` garde l'usine à tuyaux une fois trouvée, en `FindObjectsInactive.Include`.**
  C'est le contraire de l'atelier : on consulte l'atelier sur place, mais on pose des tuyaux
  **sous terre**, donc l'usine est toujours éteinte au moment où l'on s'en sert.
- **`SaveSystem` cherche l'usine pour elle-même** plutôt que de supposer qu'elle arrive avec
  l'atelier, bien qu'elles vivent dans la même scène. C'est exactement la supposition qui avait
  coûté les plaques en 9a.
- **Le tableau de la phase 8 se reproduit en changeant de tuyau entre les saisons**, pas avec un
  réseau figé. Voir « Ce que le croisement apprend ».

### Phase 9a, choix techniques tranchés le 3 septembre 2026

Voir PLAN-PHASE-09.md, section « Choix techniques fixés ». Les quatre points laissés ouverts
par le plan de design ont été tranchés avant d'écrire une ligne, règle 7.

- **Les intérieurs vivent dans une troisième couche et une cinquième scène.** Écart explicite
  aux quatre scènes de CLAUDE.md. La voie écartée, les pièces peintes dans la Surface, aurait
  payé le même prix sans obtenir la couche : la carte dépassait la colonne 40, donc les bornes
  de caméra du village changeaient, et surtout **une `Light2D` globale porte sur des Sorting
  Layers, pas sur une zone** — les pièces auraient pris la couleur de la saison.
- **La porte est un `ManholePortal` réutilisé tel quel**, Espace sur la case comme sur une
  bouche depuis la phase 2. Entrer en marchant aurait demandé une garde « je viens d'arriver »,
  la famille de bug de l'Espace lu deux fois de la phase 7.
- **Les phrases sont des images dessinées par `PixelFont` à la génération.** La police reste
  côté Editor, les images sont versionnées et relisibles à l'œil.
- **La phase est coupée en deux commits**, 9a puis 9b : tout le risque d'architecture est dans
  9a, et 9b n'est plus que du travail de tuyaux sur un patron prouvé.
- **`GameLayer.Interior` est ajouté à la fin**, comme `NodeType.ReserveInlet` en phase 8 :
  `ManholePortal.destinationLayer` est sérialisé par son rang.
- **Cinq Sorting Layers pour les intérieurs, pas trois.** `Interior_Pipes` et `Interior_Water`
  ne servent à rien aujourd'hui ; ils gardent `GameSortingLayers` en table sans exception.
- **La caméra se borne à la pièce par `WorldBoundsAround`**, virtuelle sur `GridMap`. La notion
  de pièce ne sort pas d'`InteriorMap`, et `CameraFollow` change de deux lignes.
- **Hors des pièces, rien n'est peint et rien n'est praticable.** `InteriorMap.IsWalkable`
  refuse toute case hors pièce plutôt que de peindre six cents murs invisibles.
- **La case du personnage est bloquante**, avec la tuile de sol dessous, comme l'herbe sous la
  maison depuis la phase 1. Trouvé en jeu : sans cela on lui marche dessus, et **debout sur lui
  on ne peut plus lui parler**, puisque l'interacteur cherche un personnage sur la case
  regardée.
- **`HeightOf(word)` plutôt qu'une hauteur constante** dans `PixelFont` : les deux rangées
  d'accent ne sont réservées que si le mot en porte un, sinon les huit noms de villes de la
  phase 7 auraient grandi de deux pixels sans raison.
- **La police est à chasse fixe, apostrophe comprise.** Le blanc autour de l'apostrophe de
  `L'ISOLÉ` est un peu large ; c'est un placeholder de plus.
- **`maxTextureSize` devient un paramètre**, 256 pour les phrases, 64 pour tout le reste.
- **La boîte de dialogue est une `Image` éteinte du HUD**, comme le voile du fondu : le HUD ne
  gagne aucun indicateur permanent.
- **`SpeechBox` lit Espace lui-même**, comme `VillageMapScreen`, et sépare `Advance` de la
  lecture du clavier pour se vérifier sans dépendre du focus de l'éditeur.
- **`Villager` tient un registre statique**, comme `ManholePortal` : l'interacteur cherche un
  personnage à chaque image, et un `FindObjectsByType` par image allouerait soixante tableaux
  par seconde.
- **`PlayerInteractor` ne cite plus aucune couche en dur** pour le choix des plaques. La garde
  `CurrentLayer == Surface` de la phase 7 aurait bloqué l'atelier le jour de son déménagement ;
  `ResolveFactory` ne rend l'atelier que si sa couche est allumée, comme `ResolveNetwork` pour
  le réseau.
- **`LayerIndicator` ne change pas** : dans un bâtiment il montre le picto de surface, ce qui
  est vrai, on n'est pas descendu.
- **Le sol des pièces est le pavé de l'atelier de la phase 7.** Même matière, simplement passée
  à l'intérieur : une tuile de moins à dessiner.

### Phase 9, design validé le 3 septembre 2026

- **Trois types de canalisation, un par menace de saison** : Standard, Isolé qui ne gèle pas,
  Grillagé que les feuilles ne bouchent pas. Même usure pour les trois.
- **Pas de stock.** Tuyaux illimités, le choix se fait dans l'usine.
- **Les usines sont des bâtiments dans lesquels on entre**, par une porte et un fondu comme
  pour descendre sous terre. **Un personnage s'y tient et dit ce qu'on peut y faire.**
  Décision rétroactive : **l'atelier des plaques de la phase 7 est refait sur ce modèle**, la
  cour à ciel ouvert disparaît. Décision d'avance : **l'usine à panneaux, phases 12 à 15, est
  un bâtiment avec trois personnages**, un par mini-jeu, chacun expliquant son problème et
  demandant de l'aide ; Espace lance le mini-jeu.
- **Le type se lit par un motif**, jamais par une couleur : la couleur dit l'état.
- **Les noms des tuyaux sont écrits**, sous les échantillons, comme les noms de villes.

- **La route du bassin porte la teinte de l'eau** quand elle est valide, comme celle d'une
  maison. C'est le même parcours, et c'est le seul retour qui dise « relié » avant que le
  niveau ne bouge.
- **`TreatmentPlant` vit sur `PlantOutlet`, dans l'Underground**, à côté du nœud `PlantInlet`.
  Le plan ne le disait pas ; c'est la seule scène qu'il liste comme modifiée.
- **`HouseVolumePerSeason` est une constante de `SeasonSystem`**, pas une donnée de maison ni
  de saison : le plan la fixe à 1 sans réglage.
- **`WaterBudget` est une struct publique**, cinq entiers exposés par `LastBudget`. La phase 10
  lira `Lost` ; les vérifications lisent le reste.
- **Le bilan lit `IsReserveConnectedAt(reserve.Cell)`** plutôt que le booléen global : un seul
  bassin aujourd'hui, mais le solveur en accepte plusieurs.
- **La cuve est un sprite de 16 par 16 sur la case du bassin**, pas une image de trois cases.
  Un sprite de 48 px sur `Underground_Entities` cacherait les tuyaux posés dans la chambre.
- **Les cinq images sont dessinées par une seule fonction** : douze pixels d'intérieur, trois
  par palier, et trois graduations claires qui passent par-dessus l'eau.
- **`SpriteIndexFor` est publique et statique** : vide et pleine exactes, trois paliers égaux
  entre les deux, 1 à 3, 4 à 6, 7 à 9. Se vérifie sans scène.
- **`reserve.Changed` marque la partie à sauver**, en plus de `SeasonChanged` qui suffirait :
  explicite plutôt que dépendant de l'ordre des événements dans `Advance`.
- **`ValidateReserve` refuse** deux bassins, une chambre non creusée, autre chose que de
  l'herbe au-dessus, et un bassin déjà au fond.
- **Le test du tableau utilise des copies des saisons** sans gel ni bouchon, créées à chaud et
  détruites après, jamais les assets. Le tableau est une propriété de la formule, pas du
  monde, voir la question ouverte.

### Phase 7

- **Un catalogue de plaques toutes faites**, inspirées des plaques du monde réel, plutôt qu'une
  gravure case par case. Validé le 3 septembre 2026.
- **Une plaque par bouche**, et **le choix se fait sur un plan du village**. La mini-carte
  écartée en phase 2 « faute d'objet à montrer » en a enfin un, et elle ne demande toujours pas
  de légende : chaque bouche y porte la plaque qu'elle a déjà.
- **La plaque reste décorative.** `NodeType.Manhole` continue d'attendre un usage qui ait du
  sens ; cette phase ne touche pas au réseau, donc aucun mini-détour ne peut couper une maison.
- **Le geste tient en deux appuis** : Espace sur une plaque de l'atelier, Espace sur une bouche
  du plan. Le premier se prend sur la case occupée, comme une bouche d'égout depuis la phase 2.
- **Le picto de choix est la plaque elle-même.** Aucune image de plus à dessiner, aucun symbole
  à apprendre : « celle-là ».
- **Chaque plaque porte le nom de sa ville**, écrit sous elle en permanence, comme les cartels
  d'une vitrine. Validé le 3 septembre 2026 : Victorien sait lire. Ce sont les huit seuls mots
  du jeu, et ils n'apparaissent que dans l'atelier — ni sur le plan, ni dans la rue.
- **Les noms sont des images, pas de l'uGUI.** Une police TTF s'affiche lissée et hors grille.
  `PixelFont` dessine les mots une fois pour toutes, et **la phase 14 en a besoin de toute
  façon** pour ses trois noms de panneaux : ce n'est pas du travail spéculatif.
- **`maxTextureSize` passe de 32 à 64.** Le plan du village fait 40 px de large et le nom
  AMSTERDAM 55 : un plafond à 32 les réduirait en silence et détruirait la police. Ce plafond
  ne fait que tronquer ; aucune des 52 images existantes ne change.
- **Les plaques sont espacées de quatre cases dans l'atelier.** Ce n'est pas décoratif :
  AMSTERDAM mesure 3,4 cases de large, et deux cartels voisins se chevaucheraient à moins.
- **L'atelier est à ciel ouvert.** Un bâtiment avec intérieur demanderait une scène, une
  transition et un mode de plus, pour un décor que la cour rend déjà.
- **Le plan est engendré depuis `VillageLayout`.** Deux dessins d'un même village finiraient
  par diverger ; celui-là en sort, donc il ne peut pas mentir.
- **Une seule image par plaque, agrandie pour le catalogue.** Deux images, une petite et une
  grande, finiraient par ne plus se ressembler, et il choisirait autre chose que ce qu'il
  obtient.
- **`ManholeCoverDefinition` est un ScriptableObject** bien qu'il ne porte presque rien : c'est
  le patron de catalogue que la phase 12 reprendra pour les panneaux.
- **`ManholeFactory` connaît ses propres échantillons**, plutôt qu'un composant par plaque
  exposée. L'atelier possède sa vitrine ; cela évite un cinquième fichier.
- **`VillageMapScreen` sépare la lecture du clavier du choix.** `ReadDirection` lit,
  `Select(direction)` choisit. La logique se vérifie ainsi sans dépendre du focus de
  l'éditeur, ce qui a permis de la tester quand le clavier ne passait plus.
- **Deux gardes symétriques sur la même touche**, une à l'ouverture du plan et une à sa
  fermeture. Voir le bug ci-dessus : sans elles, le même Espace est lu deux fois.

### Phase 6

- **`companyName` devient `Lio`.** Validé le 3 septembre 2026. Posé **avant** la première
  sauvegarde écrite : le changer ensuite aurait déplacé les parties de Victorien. C'est la
  seule modification de `ProjectSettings` de tout le projet, et elle est volontaire.
- **On sauvegarde des gestes, pas un état.** La liste des cases creusées et des cases posées
  suffit, puisque `Dig` et `PlacePipe` sont les seules mutations du monde. Conséquence :
  **tout état chargé est un état atteignable en jouant**, et un fichier bricolé à la main ne
  peut pas produire un réseau impossible.
- **Écriture groupée à chaque geste**, au plus une toutes les deux secondes, plus une à la
  fermeture. Au pire deux secondes de perdues.
- **Au redémarrage, Victorien repart toujours au départ du village.** Validé le 3 septembre
  2026. Sa position et sa couche ne sont donc **pas écrites du tout** : on ne sauvegarde pas
  ce qu'on ne relit pas. La note provisoire de la phase 2 devient la règle.
- **Seuls les segments abîmés sont écrits.** Reposer un tuyau le recrée neuf ; écrire un
  segment intact ne changerait rien au chargement. Chaque ligne du fichier est une cicatrice.
- **Les nœuds permanents ne sont pas sauvegardés.** La scène les recrée. Les figer dans le
  fichier gèlerait le plan du monde dans les parties de Victorien, et le moindre changement de
  carte les casserait.
- **Les maisons desservies ne sont pas sauvegardées.** Le solveur les recalcule. Une donnée
  dérivée sauvegardée est une donnée qui peut mentir.
- **Les cases s'écrivent en deux entiers explicites, pas en `Vector2Int`.** Newtonsoft
  écrirait aussi `magnitude` et `sqrMagnitude`, qu'il ne saurait pas relire. Le fichier ne
  doit pas dépendre de la façon dont Unity sérialise ses types.
- **Écriture atomique**, par fichier temporaire puis remplacement. Une coupure en pleine
  écriture ne doit jamais laisser un fichier tronqué à la place d'une bonne partie.
- **Un fichier qu'on n'a pas su lire n'est jamais écrasé.** Il est mis de côté avec un
  horodatage, la partie repart neuve, et **rien ne s'affiche à l'écran**. Un avertissement en
  console, pour moi.
- **Aucun retour visuel de sauvegarde.** Un témoin qui clignote dirait qu'il existe un risque
  de perdre quelque chose, et toute la promesse de CLAUDE.md est qu'il n'y en a pas.
  Réversible : c'est une `Image` de plus dans le HUD.
- **Le menu Editor s'appelle « Repartir d'une partie neuve » et ne détruit rien**, au lieu du
  « Effacer la sauvegarde » du plan. Il met l'ancienne partie de côté. Un clic malheureux ne
  doit pas coûter la partie de Victorien, même à moi.
- **`Time.unscaledTime` pour l'anti-rebond**, et non `Time.time` : ralentir le jeu pour mes
  tests ne doit pas ralentir les écritures.

### L'usine à panneaux, phases 13 à 16

Ajoutée le 3 septembre 2026, à la demande de Lio. Victorien est passionné de panneaux de
signalisation ; l'usine en fait un lieu du jeu, sur le Code de la route français.

- **Quatre phases, pas une.** Le bâtiment et le catalogue d'abord, puis un mini-jeu par phase.
- **Décalée de 12–15 à 13–16 le 4 septembre 2026**, la phase 12 étant prise par la grande carte.
  Les décisions ci-dessous restent valables telles quelles : seul leur rang change.
- **Elle passe avant l'habillage, jamais après.** Sinon ses panneaux seraient dessinés en
  placeholder puis redessinés une seconde fois, alors que la règle 4 de CLAUDE.md veut un
  seul passage d'art à la fin. L'habillage devient la phase 17.
- **Elle passe après l'arc du réseau.** Aucune phase de 7 à 11 n'en dépend, et le squelette de
  mini-jeu profitera des schémas stabilisés par six phases.
- **L'ordre des trois mini-jeux va du plus simple au plus lourd** : Le Stock valide le
  squelette, La Fabrique s'appuie dessus, Le Plan vient en dernier.
- **Les panneaux sont un placeholder idéal.** Triangle bordé de rouge, disque bleu, octogone :
  leur géométrie est déjà presque leur forme finale, contrairement au personnage. Dessinés par
  code dans `PlaceholderArtGenerator`, jamais récupérés en fichiers. Les panneaux du Code de
  la route sont des dessins officiels de l'État, sans rapport avec l'interdiction d'assets
  Nintendo ou Pokémon de CLAUDE.md.
- **La Fabrique demande le nom parmi trois noms écrits.** Validé le 3 septembre 2026 :
  Victorien sait lire, et les noms de panneaux sont courts et en majuscules. Le pictogramme
  reste le premier choix partout ailleurs.
- **Le Plan repose sur quinze plans écrits à la main**, dans le style ASCII de
  `VillageLayout` et `UndergroundLayout`, et non sur un générateur aléatoire. Chacun est relu
  et vérifié solvable avant d'être écrit, et la difficulté progresse dans un ordre choisi.
  C'est la méthode qui avait permis de vérifier les crêtes de la phase 4 par le calcul.
- **L'usine est une pure récréation, sans lien avec le réseau.** Rien de ce qui s'y fabrique
  ne sort du bâtiment. Aucun mini-jeu ne peut donc bloquer la progression du réseau, ce qui
  garde intact le « aucun échec puni » de CLAUDE.md.
- **C'est un bâtiment dans lequel on entre**, décidé le 3 septembre 2026 avec la phase 9 :
  trois personnages à trois endroits de l'intérieur, un par mini-jeu. Chacun explique son
  problème et demande de l'aide ; Espace face à lui lance son mini-jeu. Le patron du bâtiment
  et du personnage qui parle est construit en phase 9.

### Phase 5

- **Une saison dure dix minutes**, cycle complet de quarante minutes. `seasonDuration` est
  sérialisé et réglable à chaud : les tests l'abaissent, la scène garde 600 s.
- **Le jeu démarre au printemps.** Le plan ne le disait pas ; c'est le premier de la liste du
  cycle, et c'est ce qui fait tomber l'usure sous 0,3 à la septième saison exactement.
- **Espace répare un tuyau abîmé, gelé ou bouché, et n'enlève que les tuyaux sains.**
  Conséquence assumée : enlever un tuyau cassé demande de le réparer d'abord.
- **La réparation s'annonce aussi sur un nœud permanent.** La phase 3 n'affichait aucun picto
  face à la station ou à une maison. Désormais, si le tuyau qui y arrive est gelé ou abîmé, le
  picto de réparation s'affiche et Espace répare. L'enlèvement, lui, reste impossible : le
  picto d'enlèvement ne s'affiche toujours pas. Un raccordement de maison gelé doit pouvoir se
  dégeler à la main, sans attendre le printemps.
- **`clogChance` vaut 0,25 à l'automne.** Le plan crée le champ sans donner de valeur. Un quart
  des tuyaux peu profonds par automne fait un bouchon quasi certain sur un long trajet, aucun
  sur un trajet court et profond. **À valider en jouant** : c'est un réglage sérialisé, il se
  change d'un clic.
- **« Peu profond » est une constante du système, pas un champ de saison.** Le plan décrit
  `clogChance` comme la probabilité qu'un tuyau *peu profond* se bouche, sans deuxième réglage
  de profondeur. C'est la même frontière que celle du gel : la profondeur 1.
- **Un segment est aussi exposé que son extrémité la moins profonde.** `Mathf.Min` des deux
  profondeurs : si un bout affleure, le froid et les feuilles entrent par là.
- **La résistance au gel est une probabilité de tenir**, conformément au tooltip posé en
  phase 3 : 0 gèle dès le premier hiver, 1 ne gèle jamais. Le type standard est à 0, donc le
  gel est parfaitement déterministe tant qu'il n'y a qu'un type. La phase 9 s'en servira.
- **L'usure passe de 0,05 à 0,1 par saison**, modulée par saison. Les saisons sont cinq fois
  plus longues que dans la proposition initiale. Sept saisons, soit environ soixante-dix
  minutes de jeu, avant qu'un tuyau ne cesse de porter.
- **`FlowSolver.MinimumCondition` devient public.** Le rendu peint en rouge terne à partir du
  même seuil que celui qui coupe l'eau ; le laisser en double aurait fini par diverger.
- **`FrozenCount` et `CloggedCount` sont recalculés à la lecture**, pas mis en cache. Une
  première version cachait le compte au dernier changement de saison : une réparation du joueur
  ne s'y voyait pas. Ils ne servent qu'aux vérifications, jamais au jeu.
- **`SeasonAmbience` vit dans la scène Surface**, pas dans le système. Une couche éteinte ne
  répondrait pas ; un composant local qui se réabonne à chaque rallumage évite le piège
  rencontré en phase 1 et en phase 4. Vérifié en jeu, saisons passées couche éteinte.
- **Le sous-sol ne change pas de lumière.** Les saisons se voient dessus, se subissent dessous.
- **Les saisons sont des données, pas du code.** Ajouter une saison ou changer un effet ne
  demande pas de recompiler.
- **`ScriptableObjectSetup` réécrit les valeurs des assets qu'il possède**, au lieu de se
  contenter de les créer s'ils manquent. C'est ce qui a permis de relever l'usure de
  `PipeType_Standard` d'un clic, sans édition à la main.
- **`BuildAllScenes` vérifie l'art avant les ScriptableObjects.** Les saisons portent leur
  pictogramme : l'ordre des deux gardes devait s'inverser.
- **Le tick n'a lieu qu'à la fin d'une saison.** Le solveur tourne quatre fois par cycle, plus
  une fois par action du joueur. Toujours jamais par frame.

### Phase 4

- **Cinq maisons posées à la main**, marqueur `A` dans le plan du village, sur des cases
  bloquantes. Validé le 2 septembre 2026.
- **Une alcôve déjà creusée sous chaque maison**, portant un nœud permanent `HouseConnection`.
  Le joueur creuse jusqu'à elle : c'est la boucle de jeu.
- **Deux crêtes peu profondes**, arcs percés d'une porte, pour forcer de vrais détours. Validé
  le 2 septembre 2026. Solvabilité vérifiée par calcul avant écriture.
- **Trois retours visuels** : tuyaux teintés, goutte par maison, rangée de gouttes en HUD.
  La rangée est un compteur et non une liste : l'ordre des maisons n'a pas à être appris.
- **Le solveur vit dans Persistent.** La couche éteinte ne peut pas répondre aux maisons de la
  couche allumée.
- **`GameManager` expose `Flow`.** Core dépend de Network, comme il dépend déjà de UI.
- **La teinte plutôt qu'une animation d'eau.** Une eau qui défile demanderait des images
  animées et un composant de plus. À rediscuter à l'habillage, phase 12.
- **Le solveur recalcule tout à chaque changement.** Cinq maisons sur quelques centaines de
  nœuds : le calcul incrémental serait un risque d'erreur sans gain mesurable.
- **`House.cs` en plus de la liste de CLAUDE.md.** Une maison a un état, il lui faut un
  composant.
- **L'eau d'une maison non reliée ne se voit pas.** La goutte reste grise ; les débordements
  restent le sujet de la phase 10. Validé le 2 septembre 2026.

### Phase 3

- **La profondeur est peinte dans la carte, pas choisie en creusant.** Trois zones
  concentriques autour de la station, qui seule est au fond. Le puzzle devient un problème de
  chemin : ne jamais remonter en allant vers la station. Laisser le joueur creuser plus
  profond en insistant a été écarté, faute de coût : creuser au maximum partout aurait tout
  résolu. Validé le 2 septembre 2026.
- **Le même Espace pose et retire.** Réversible, sans punition.
- **Espace est contextuel, dans un ordre fixe** : passage sur la case occupée, puis creuser,
  poser ou enlever sur la case regardée. Une seule touche, aucun mode.
- **Le raccordement est automatique.** Deux tuyaux voisins sont reliés. Un geste de
  raccordement séparé aurait demandé une deuxième touche.
- **Le tuyau ne bloque pas le passage.** Un enfant coincé derrière sa propre construction,
  c'est un échec puni déguisé.
- **Seule la station est un nœud permanent.** Les échelles restent des cases ordinaires : le
  type `Manhole` du modèle attendra d'avoir un usage réel.
- **Le rendu du réseau est redessiné en entier à chaque changement.** Quelques centaines de
  cases, et seulement sur action du joueur.
- **La profondeur est cuite dans un tableau sérialisé**, pas relue dans une tilemap : donnée
  de carte statique, dont la tuile n'est que l'affichage.
- **Deux plans superposés pour le sous-sol**, l'état et la profondeur, plutôt qu'un alphabet à
  neuf lettres. Chacun reste lisible.
- **Les maisons restent en phase 4**, comparaison de charge faite : huit fichiers runtime et
  trente fichiers d'art en phase 3, contre cinq fichiers en phase 4 sur un graphe déjà
  construit. Validé le 2 septembre 2026.
- **`TargetCursor` rangé dans `Player/`** plutôt que dans `World/` : il suit le regard du
  personnage, il n'appartient pas à la carte.
- **`PipeNetworkView` séparé de `PipeNetwork`.** Le graphe ne connaît pas ses tuiles ; la
  phase 4 pourra teinter le rendu selon l'écoulement sans toucher au modèle.

### Phase 2

- **Le sous-sol démarre en terre pleine plus un réseau de galeries déjà creusées.** Une salle
  3x3 sous chaque bouche et sous la station, reliées par des couloirs d'une case. 74 cases
  praticables sur 1200. Praticable pour vérifier le portail dès la phase 2, très majoritairement
  plein pour que la phase 3 ait de quoi creuser. Validé le 2 septembre 2026.
- **Un repère de couche en HUD plus un picto d'action au-dessus de la tête, pas de mini-carte.**
  Une mini-carte serait vide de sens tant que le réseau n'existe pas, et demanderait une
  légende, donc du texte.
- **Descendre remet le personnage à la même case.** Les deux cartes font 40x30 et partagent le
  même repère. `destinationCell` reste tout de même un champ sérialisé, au cas où la phase 11
  en aurait besoin.
- **Fondu au noir de 0,15 s dans chaque sens.**
- **L'interaction porte sur la case occupée, pas sur la case regardée.** Marcher sur la bouche
  puis appuyer, c'est le geste le plus simple à six ans. La case regardée reste libre pour le
  creusement de la phase 3 : on ne creuse pas la case où l'on se tient.
- **`WasPressedThisFrame` et non `ReadValue`.** Espace maintenu ne fait pas descendre et
  remonter en boucle.
- **`PlayerController` éteint pendant le voyage.** Une flèche maintenue pendant le fondu ne
  doit pas faire partir le personnage de travers à l'arrivée. Vérifié.
- **HUD en uGUI plutôt qu'en sprites enfants de la caméra.** Un sprite d'interface devrait
  choisir une famille de Sorting Layers et en changer à chaque bascule, sous peine de passer
  derrière le décor ou de rendre noir. Le Canvas en Screen Space ignore les Sorting Layers et
  les lumières 2D. Coût : `UnityEngine.UI` dans les deux `.asmdef`.
- **`SceneRouter` référence `ScreenFader`, donc Core dépend de UI.** Une seule assembly
  runtime, aucun cycle possible. Une interface intermédiaire serait de la cérémonie pour deux
  méthodes.
- **`GameSortingLayers` côté runtime**, fichier hors liste CLAUDE.md. Le personnage a besoin
  des noms pour changer de famille en descendant ; les garder en double avec l'éditeur aurait
  fini par diverger.
- **`PortalBuilder` côté Editor**, non prévu au plan. Les deux bouts d'un passage se décrivent
  exactement de la même façon : la factorisation évite de recopier quatre lignes de
  `SerializedObject` dans deux builders.
- **`UndergroundMap` ne partage pas le code de `SurfaceMap`.** Trois lignes dupliquées plutôt
  qu'un couplage prématuré : dès la phase 3, le sous-sol devient mutable et la surface non.
- **Lumière globale du sous-sol à 0,8.** À rediscuter une fois vu à l'écran en grand.
- **Rien n'est sauvegardé de la couche courante.** La phase 6 s'en charge ; on démarre toujours
  en surface.

### Phases 0 et 1

- **Surface et Underground restent chargées en permanence.** `SceneRouter` bascule la scène
  active et allume ou éteint la racine de chaque couche. La transition de la phase 2 sera
  instantanée et l'état du réseau souterrain ne sera jamais rechargé.
- **Deux assembly definitions plutôt qu'une par dossier.** Pas de plomberie de références,
  aucun risque de dépendance circulaire entre Buildings, Network et World.
- **`Bootstrapper` en cinquième fichier de `Core/`**, non prévu par CLAUDE.md. Le GameManager
  vit dans Persistent, il faut donc quelque chose dans Boot qui charge Persistent.
- **`LayerRootBuilder` côté Editor**, factorisation de la racine commune à Surface et
  Underground.
- **Une `Light2D` globale par couche, cantonnée à sa famille de Sorting Layers.** Sous le
  Renderer2D, le matériau sprite par défaut est Sprite-Lit : sans lumière globale, tout est
  noir. Par défaut URP fait porter une lumière globale sur tous les Sorting Layers du projet.
  Les deux couches étant résidentes, leurs deux lumières se chevauchaient et URP levait
  « More than one global light on layer Default » à chaque chargement. Chaque lumière ne
  couvre donc que les layers de sa couche. Conséquence pour les phases suivantes : tout
  sprite ou tilemap doit choisir le Sorting Layer préfixé de la couche où il vit.
- **Identifiants de Sorting Layer forcés positifs.** Unity écrit 0, l'identifiant de
  `Default`, à la place d'un identifiant négatif. Le hash FNV-1a est donc masqué sur
  31 bits, sinon `Ground`, `Pipes` et `Water` se confondaient silencieusement avec
  `Default`.
- **`Awaitable` plutôt que des coroutines** pour le chargement de scènes. Natif Unity 6.
- **Pas de stub `SaveSystem` ni `GameClock`.** Ils arrivent en phase 6 et en phase 5.
- **Composite 2D Vector en mode Digital Normalized.** Il autorise encore les diagonales : la
  restriction aux quatre directions se fera dans `PlayerController` en phase 1, en gardant
  l'axe dominant.
- **`FindAnyObjectByType` et non `FindFirstObjectByType`.** Le plan citait le second, qui est
  déprécié en 6000.5.10f1 et produisait un warning. Même sémantique ici : les deux ignorent
  les objets éteints, et il n'y a jamais qu'une seule carte allumée.
- **Résolution paresseuse de la carte, et pas seulement au `Start`.** Le plan prévoyait un
  `FindFirstObjectByType<GridMap>()` au `Start`. Or le personnage vit dans Persistent, qui est
  chargée **avant** Surface : au `Start` la carte n'existe pas encore, le personnage serait
  resté inerte. `PlayerController` et `CameraFollow` retentent donc tant qu'ils n'ont pas de
  carte, et re-résolvent dès que celle qu'ils tenaient s'éteint. Le même mécanisme servira
  telle quelle la bascule de la phase 2.
- **Marqueur `T` ajouté à la légende du plan.** Le plan exigeait un GameObject
  `TreatmentPlant` sans dire où le poser. `T` marque son entrée, à l'intérieur de l'enceinte,
  et le builder peint du sol de station dessous.
- **Report du surplus de `stepProgress` d'un pas au suivant.** Sans lui, chaque case perdrait
  la fraction de frame qui dépasse et une flèche maintenue avancerait un peu moins vite que
  la consigne. Avec, la mesure tombe sur 5,000 cases par seconde.
- **Du sol peint sous les tuiles bloquantes.** De l'herbe sous les haies, du sol de station
  sous les murs. La couche bloquante se pose par-dessus, en ordre 1.
- **`_ = SceneManager.UnloadSceneAsync(...)` dans `Bootstrapper`.** Warning CS4014 hérité de
  la phase 0 : ne pas attendre est voulu, le discard le dit au compilateur.
- **Quatre sprites de personnage, un par direction. Validé le 2 septembre 2026.** Le repère
  de 2 px dessiné sur l'unique `player.png` ne sait montrer que la direction du bas. À la
  place : `player_down`, `player_up`, `player_left` et `player_right`, produits par
  `PlaceholderArtGenerator`, et `PlayerController` choisit le sprite d'après `Facing`. Pas de
  pictogramme de case visée. **À faire en phase 3**, quand Espace agira pour la première fois
  sur `FacingCell` ; rien à changer côté logique, `Facing` est déjà correct.

## Placeholders à remplacer

- **La tuile d'eau de la phase 10** est un bleu semi-transparent avec deux trains de
  vaguelettes. Elle se lit comme de l'eau sur les trois sols, mais elle ne bouge pas : une eau
  qui ondule demanderait des images animées et un composant de plus. À rediscuter à l'habillage,
  avec la teinte de l'eau des tuyaux, en attente depuis la phase 4.
- **Une flaque de fuite posée juste sous une maison est en partie cachée** par le sprite de la
  maison, qui déborde de huit pixels vers le haut à cause de son pivot au tiers. Sans conséquence
  sur le jeu — la flaque est bien là — mais la première capture a dû être refaite ailleurs. Même
  famille de problème que l'échelle qui disparaît sous le personnage, notée en phase 2.
- **L'ouvrier des tuyaux est le personnage joueur repeint en bleu**, comme l'artisan l'est en
  vert. Les trois ne se distinguent que par la couleur.
- **Les échantillons de tuyaux font seize pixels**, comme les tuiles posées, ce qui est voulu
  mais les rend petits dans la vitrine. Les motifs se lisent, mais de près.
- **Le blanc bleuté du gel se voit peu sur le gris pâle des tuyaux.** Relu dans la tilemap, la
  teinte est bien posée ; à l'œil, plein écran, l'écart est faible. À revoir à l'habillage, du
  côté de la couleur du corps plutôt que de celle du gel.
- **Les onze PNG de la phase 9a** : la façade de l'atelier, le mur de pièce, la porte,
  l'artisan, `picto_enter`, `picto_exit`, `picto_talk` et les trois phrases. La façade est un
  aplat ocre de quatre cases sur deux : elle se lit comme un bâtiment sur l'herbe, mais elle
  n'a ni toit, ni fenêtre, ni enseigne. **Rien ne dit de l'extérieur que c'est l'atelier des
  plaques** ; l'enseigne est à dessiner à l'habillage, et elle vaudra pour les trois bâtiments.
- **L'artisan est le personnage joueur repeint en vert.** Même silhouette, même pose, même
  visage. Il se distingue à la couleur et à rien d'autre.
- **L'apostrophe de `PixelFont` occupe une cellule entière**, la police étant à chasse fixe :
  le blanc autour d'elle est trop large dans `L'ISOLÉ`. À reprendre avec le reste de la police.
- **Le picto « parler » est un carré blanc opaque de seize pixels.** Une vraie bulle, plus
  petite et à fond transparent, cacherait beaucoup moins l'interlocuteur. Voir la question
  ouverte.
- **Les cinq PNG de la phase 8**, la cuve du bassin : cadre gris, intérieur sombre, eau qui
  monte. Lisible, mais c'est une boîte ; un vrai bassin d'orage vu de dessus reste à dessiner.
- **Les dix-huit PNG de la phase 7** : les huit plaques, les huit noms, le plan du village et
  le pavé de l'atelier.
- **La police de 5 sur 7 pixels** se lit **au HUD**, sur fond uni, et ne se lisait pas dans le
  décor : c'est ce qui a fait passer les noms au cartel le 4 septembre 2026. Quelques lettres
  restent grasses à cette taille, le M et le B surtout. À reprendre à l'habillage.
- **Les pièces sont vides** depuis que les noms et les pictos ont quitté le décor. Trois
  échantillons et deux personnages dans une salle de vingt sur dix : c'est propre, mais ce n'est
  pas encore un lieu. Mobilier, établi, étagères : à l'habillage.
- **L'atelier n'a ni mur ni toit.** Une cour pavée posée sur l'herbe se lit comme un lieu, mais
  ce n'est pas encore une usine. À habiller.
- **Les cinq PNG de la phase 5** : les quatre pictos de saison et la clé de réparation. La
  clé se lit bien au-dessus de la tête ; les quatre saisons se distinguent surtout par la
  couleur de fond, le motif venant après.
- **Le contraste des saisons se voit peu sur l'herbe.** Les quatre couleurs de lumière
  changent nettement le chemin de terre et le sol de la station, beaucoup moins l'herbe :
  le vert est porté par le canal qui varie le moins entre les quatre saisons. À juger plein
  écran avant de figer, et à corriger côté couleurs de lumière plutôt que côté tuiles.
- **Le blanc bleuté du gel et le bleu de l'eau** sont très différents en valeur sur les
  captures, mais les deux restent des bleus. À revoir si Victorien les confond.
- **Les cinq PNG de la phase 4** : maison, tuile bloquante de maison, arrivée de maison sous
  terre, goutte pleine, goutte vide.
- **La goutte au-dessus des maisons est grande** par rapport au toit, et flotte au ras des
  tuiles. À caler à l'habillage.
- **Les trente PNG de la phase 3** : les six nuances de terre et de galerie, les seize
  canalisations, les quatre personnages, les pictos et le curseur.
- **Le picto « enlever »** est un disque barré, vocabulaire d'interdiction plutôt que de
  retrait. À redessiner avec le vocabulaire des panneaux, que Victorien aime.
- **Les sept PNG de la phase 2** : terre, galerie, échelle, et les quatre pictogrammes. Mêmes
  carrés de couleur que le reste.
- **L'échelle disparaît sous le personnage** quand il se tient dessus, les deux occupant la
  même case. Sans conséquence sur le jeu, mais un vrai sprite d'échelle devra déborder vers le
  haut, comme le personnage, pour rester visible.
- **Les huit PNG de la phase 1**, carrés de couleur pleins bordés d'un liseré. Le pixel art
  viendra à la toute fin, une fois le gameplay validé.
- **`TreatmentPlant` réutilise le sprite `tile_plant_wall`.** Le plan de la phase 1 listait
  huit fichiers et aucun sprite dédié à la station. Le carré bleu est bien visible sur le sol
  gris, mais il est identique aux murs de l'enceinte. Un sprite propre à la station est à
  prévoir.
- **`player.png` est un sprite unique** dont le repère de direction ne vaut que vers le bas.
  Il sera remplacé en phase 3 par quatre sprites, un par direction, décision validée le
  2 septembre 2026. `Facing` est déjà correct côté code, seul l'affichage ne suit pas.
- Couleur de fond de la caméra : `#181425`, provisoire.

## Questions ouvertes

- **Les personnages parlent, et CLAUDE.md veut le moins de texte possible.** Décidé le
  3 septembre 2026 : ils parlent en phrases de moins de six mots, en français, et Victorien
  sait lire. Les phrases exactes sont à écrire et à relire à voix haute pour six ans. Les
  accents manquent encore à `PixelFont`.
- ~~**Le picto « parler » couvre la tête de l'artisan.**~~ **Tranché le 3 septembre 2026** : la
  bulle passe au-dessus de la tête du personnage qui parle. Fait en 9b.

- ~~**Une scène de plus pour les intérieurs ?**~~ **Tranché le 3 septembre 2026** : oui, une
  cinquième scène `Interiors` et une troisième couche `GameLayer.Interior`. Écart à CLAUDE.md
  accepté explicitement. Voir les décisions de la phase 9a.
- **Le tableau de la phase 8 demande de changer de tuyau entre les saisons.** La phase 9b
  montre qu'il se reproduit au chiffre près, mais seulement si le joueur pose du grillagé avant
  l'automne et de l'isolé avant l'hiver : aucun type ne met une maison peu profonde à l'abri de
  l'année entière, par construction. C'est la boucle voulue, mais **elle demande de comprendre
  que la saison qui vient décide du tuyau**. À observer quand Victorien jouera : c'est le point
  de compréhension le plus exigeant du jeu à ce jour.

- **Le bilan de l'eau est calculé au tick, après les effets de la saison qui commence.** Le
  plan le demande ainsi, et c'est fait ainsi. Conséquence vue en jeu : l'hiver gèle les trois
  maisons peu profondes **avant** le bilan, l'automne en bouche autant, et aucun entretien ne
  peut s'intercaler, puisque effets et bilan partagent le même tick. Aux vraies règles, avec
  le réseau remis à neuf avant chaque saison, l'automne voit 2/5 desservies, 10 arrivant,
  +2 au bassin ; l'hiver voit 3 arrivant et vide le bassin. **Le tableau du plan, 13 / 8 / +5
  puis 6 / 6 / 3, n'apparaît jamais en jeu** : il ne vaut que pour cinq maisons desservies au
  tick. Deux issues possibles, à trancher : garder, la pointe d'automne est alors surtout
  perdue dans les bouchons ; ou calculer au tick le bilan de la saison **qui se termine**,
  avant d'appliquer les effets de la nouvelle, ce qui rendrait l'entretien de l'hiver visible
  dans le bilan. Non tranché, plan respecté.
- **La route du bassin ne peut ni geler ni se boucher.** À profondeur 2, un chemin à
  profondeur non décroissante ne repasse jamais par la profondeur 1 : seule l'usure la coupe.
  « Un hiver qui gèle la route du bassin » ne peut pas arriver à cet emplacement ; le cas a
  été vérifié avec un segment gelé à la main.
- **Relier le bassin n'est pas un petit chantier.** 24 pas dont 13 à creuser : la galerie
  x = 8 est coupée pour l'eau par la crête qui passe sous l'échelle (8, 19), et il faut
  contourner par (9, 19), (10, 19) puis descendre par x = 12. En revanche les plus courts
  chemins de quatre maisons traversent la chambre : relier les maisons lointaines relie le
  bassin presque gratuitement. À décider si c'est voulu.
- **L'incitation perverse** ne mord pas : en jeu, ce sont les bouchons et le gel qui font
  baisser l'arrivant au tick, jamais un choix du joueur.
- **`GameManager.Instance` ne survit pas à un rechargement de domaine.** L'instance est posée
  dans `Awake`, qu'Unity ne rappelle pas après un rechargement en plein play ; tout ce qui en
  dépend devient muet jusqu'au prochain lancement. Cela n'arrive que dans l'éditeur, jamais
  dans un build, et cela a coûté une fausse piste en phase 7. Le poser dans `OnEnable` le
  réglerait en une ligne, mais cela touche du code de la phase 0 : à décider.
- **`clogChance` à 0,25 par automne.** Valeur choisie faute d'indication dans le plan. Un
  quart des tuyaux peu profonds : quasi certain sur un long trajet, jamais sur un trajet court
  et profond. À valider en jouant ; c'est un champ sérialisé sur `Season_Automne`.
- **La difficulté des crêtes pour un enfant de six ans.** Les portes se voient dans la teinte
  de la terre, mais elles demandent de comprendre que la bande claire est un mur. À observer
  quand Victorien jouera, en dernière phase.

- **Lisibilité des trois nuances de profondeur.** Le brun s'assombrit et la galerie vire au
  gris froid au plus profond. Distinct sur les captures, mais l'écart entre profondeur 1 et 2
  est le plus faible des deux : à juger plein écran avant de figer.

- **Intensité de la lumière du sous-sol et contraste terre / galerie.** 0,8 et deux bruns
  distincts sur les captures ; à juger en vrai, plein écran, avant de figer.

- **`Assets/Settings/InputSystem_Actions.inputactions`**, l'asset d'input par défaut d'Unity,
  est conservé intact car les ProjectSettings le référencent comme *project-wide actions*.
  Il n'est pas utilisé par le jeu. Ménage possible plus tard.
- **Manette.** Des bindings `<Gamepad>/dpad` et `<Gamepad>/buttonSouth` sont posés. Support
  optionnel, jamais requis, conformément à CLAUDE.md. À retirer si tu préfères le clavier seul.
