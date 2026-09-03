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
| 9b | L'usine à tuyaux | À faire |
| 10 | Les fuites | À faire |
| 11 | Le parc | À faire |
| 12 | L'usine à panneaux | À faire |
| 13 | Le Stock, le memory | À faire |
| 14 | La Fabrique | À faire |
| 15 | Le Plan | À faire |
| 16 | Habillage | À faire |

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
- Scène relue par script : **1200 profondeurs cuites**, réparties en 766 / 374 / 60 pour les
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

## Prochaine étape, phase 9b

L'usine à tuyaux, sur le patron prouvé par 9a. Ce que 9a laisse en place :

- **Le patron du bâtiment** : façade et porte au plan du village, pièce dans un créneau libre,
  personnage et phrases. Ajouter l'usine, c'est ajouter un bloc à `InteriorsLayout`, une façade
  et une porte à `VillageLayout`, et des phrases au générateur d'art.
- **Le patron accepte trois personnages** dans une pièce : `Villager` est un composant par
  personnage, avec son registre et ses phrases, ce dont l'usine à panneaux aura besoin en
  phase 12.
- **La police a ses accents** : `ISOLÉ`, `GRILLÉ` et `ARRÊTE` sont déjà relus sur planche.
- Ce que la phase 8 laissait, toujours vrai : **`PipeType.FrostResistance` a enfin un enjeu**,
  **`SeasonSystem.LastBudget.Lost`** est l'entrée de la phase 10, **`TreatmentPlant` est un
  objet**, et **le patron du nœud permanent** sert trois fois.

## Décisions prises

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

### L'usine à panneaux, phases 12 à 15

Ajoutée le 3 septembre 2026, à la demande de Lio. Victorien est passionné de panneaux de
signalisation ; l'usine en fait un lieu du jeu, sur le Code de la route français.

- **Quatre phases, pas une.** Le bâtiment et le catalogue d'abord, puis un mini-jeu par phase.
- **Elle passe avant l'habillage, jamais après.** Sinon ses panneaux seraient dessinés en
  placeholder puis redessinés une seconde fois, alors que la règle 4 de CLAUDE.md veut un
  seul passage d'art à la fin. L'habillage devient la phase 16.
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
- **La police de 5 sur 7 pixels** se lit, mais quelques lettres sont grasses à cette taille,
  le M et le B surtout. À reprendre à l'habillage, en même temps que le reste.
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
- **Le picto « parler » couvre la tête de l'artisan** quand on l'aborde par en dessous. Le
  picto d'action se pose au-dessus de la tête du JOUEUR depuis la phase 2, « toujours au même
  endroit », et cette place tombe exactement sur le visage de qui se tient une case plus haut.
  Vu de côté ou par le haut, aucun recouvrement. Deux issues, à trancher : garder la règle et
  reprendre le picto à l'habillage, plus petit et transparent ; ou **poser la bulle au-dessus
  de la tête du personnage qui parle**, ce qui est la convention partout ailleurs et ne cache
  rien, au prix de la règle « toujours au même endroit ». Dix lignes dans les deux cas.
  Non tranché.

- ~~**Une scène de plus pour les intérieurs ?**~~ **Tranché le 3 septembre 2026** : oui, une
  cinquième scène `Interiors` et une troisième couche `GameLayer.Interior`. Écart à CLAUDE.md
  accepté explicitement. Voir les décisions de la phase 9a.
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
