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
| 12e | Les huit guides | Terminée |
| 13 | L'usine à panneaux | Terminée |
| 14 | Le Stock, le memory | Terminée |
| 15 | La Fabrique | Terminée |
| 16 | Le Plan | Terminée |
| 17 | Habillage | Terminée, et le rendu ne convient pas |
| 18 | Le style de la référence | 18a à 18e faites, 18f à 18h à faire |

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

| `Lost` | Rayon | Cases mouillées, 12b | **Corrigé en phase 13** |
|---|---|---|---|
| 0 | — | 0 | 0 |
| 1 | 0 | 7 | 7 |
| 2 | 1 | 35 | **34** |
| 3 | 2 | 89 | **88** |
| 4 | 3 | 172 | **170** |
| 5 | 4 | 280 | **278** |
| 6 | 5 | 412 | **407** |

**La colonne de gauche est périmée depuis la phase 12c** et personne ne l'avait vu : ses
126 arbres ont porté les cases bloquantes de 539 à **647**, et 12c a écrit « la table est
inchangée jusqu'à Lost = 5 » sans la recalculer. Elle a donc été fausse une **troisième** fois,
pour la troisième fois faute d'être revérifiée après un changement de plan. Les chiffres de
droite sont mesurés en phase 13 **par le prédicat du jeu lui-même**, `VillageLayout.IsWalkable`,
celui-là même dont `FloodView.Paint` se sert.

`Lost` plafonne toujours à **5** : l'arrivant plafonne à 21, treize destinations plus huit de
pluie d'automne, la station en traite seize. À `Lost` = 5 la flaque couvre 278 cases, **9,7 %**
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

## Phase 12e, ce qui est fait

Huit personnages-guides, un par leçon. C'était le trou le plus large du jeu : `grep "Depth"` dans
toute l'UI et tout le code du joueur ne rendait **rien**, alors que la règle de profondeur *est*
le puzzle selon CLAUDE.md. Le jeu comptait sept phrases en tout, toutes derrière les portes de
deux boutiques, et toutes sur le choix des plaques et des tuyaux.

| # | Leçon | Où | Il parle tant que |
|---|---|---|---|
| 1 | Relie les maisons | (12, 16), au départ | aucune maison n'est desservie |
| 2 | On descend par une bouche | (10, 11), près d'une bouche | rien n'a été creusé |
| 3 | Les saisons abîment | (26, 26), en surface | la saison bouche ou gèle |
| 4 | Une flaque, un tuyau fuit | (28, 32), près d'une bouche | un tuyau demande réparation |
| 5 | On creuse la terre | (11, 10), sous terre | rien n'a été creusé |
| 6 | Puis pose un tuyau | (5, 22), sous terre | aucun tuyau n'est posé |
| 7 | **L'eau ne remonte jamais** | (39, 26), sous terre | de l'eau morte s'arrête quelque part |
| 8 | L'orage remplit le bassin | (11, 16), sous terre | le bassin n'est pas relié |

**Aucune condition n'invente d'état.** Toutes se lisent sur le monde et redeviennent vraies si le
joueur défait ce qu'il a fait. **Sans mémoire**, décision du 4 septembre : réexpliquer à un
relancement ne punit rien, ne coûte aucun champ de sauvegarde, et évite d'avoir à donner au
`Villager` un identifiant stable puis à le faire entrer dans le ET des quatre de
`SaveSystem.TryLoad`, où il aurait pu n'être jamais relu en silence, comme les plaques en 9a.

**Se taire sans disparaître était gratuit.** `Villager.CanSpeak` valait déjà `LineCount > 0`, et
`Evaluate` ne propose `Talk` que si `CanSpeak` : `SetLines(null)` laisse donc le guide visible,
sur sa case, et Espace ne fait plus rien devant lui.

### La leçon 7 tient à l'eau morte

Le guide de la profondeur parle exactement quand une **route commencée ne rejoint pas la
station** — la frontière que le solveur publie depuis la phase 12a. C'est ce qui rend cette leçon
enseignable : on ne peut pas dire *où* ça casse tant que le jeu l'ignore.

**Le seuil compte.** À `StrandedCount > 0`, il parlait dès le premier lancement, avant qu'une
seule case soit creusée : sans tuyau, chaque destination est déjà sa propre frontière. Le seuil
est donc `> destinations + bassins`, et vérifié en jeu — muet au départ, il parle après cinq
tuyaux posés en aveugle.

### Le coût d'un bloquant n'est pas le même en surface et sous terre

Les guides **bloquent**, recommandation de l'audit : on ne traverse pas quelqu'un, et surtout,
debout **sur** lui on ne pourrait plus lui parler, l'interacteur cherchant un personnage sur la
case **regardée**.

Mais sous terre le prix est plus lourd : le test du `Villager` passe **avant** `Dig` et
`PlacePipe` dans `Evaluate`, donc une case de personnage devient increusable **et** impossible à
tuyauter, **sans un mot**. Les quatre guides du sous-sol se tiennent donc dans des **alcôves en
cul-de-sac** creusées pour eux.

`ValidateGuidePosts` le prouve, et **la preuve est le cul-de-sac, pas la connexité** : une case
de degré un ne peut être la case intermédiaire d'aucun chemin, puisqu'il faudrait y entrer et en
sortir par la même voisine ; comme elle n'est jamais une destination, aucune route ne peut en
avoir besoin.

### Deux réparations que l'audit demandait

- **L'horloge s'arrête pendant qu'on parle.** `GameClock` ne se mettait **jamais** en pause :
  une saison dure 600 secondes, et un tick pouvait tomber au milieu d'une phrase — geler la route
  qu'on venait d'expliquer pendant que la boîte restait ouverte. Un compteur statique sur
  `SpeechBox` suffit ; il se relâche aussi quand la couche s'éteint, sinon l'horloge resterait
  figée pour toujours.
- **Un second afficheur d'attention**, un triangle de danger du vocabulaire routier — la passion
  de Victorien. Il est piloté par **le guide** et non par `PlayerInteractor`, qui éteint sa bulle
  dès que le joueur regarde ailleurs : ce signal doit se voir **de loin**, sinon il n'appelle
  personne.

## Phase 12e, vérifications faites

- Compilation relue par le pont MCP : **zéro erreur, zéro warning**, hors celui du package MCP.
- Les huit guides existent, chacun avec la bonne leçon sur la bonne case, et leurs conditions
  sont justes au premier lancement : le but, la bouche, creuser, poser et le bassin parlent ; les
  saisons se taisent au printemps ; la fuite se tait sur un réseau neuf ; **la profondeur se
  tait** tant qu'aucune route n'est commencée.
- **Se taire quand la leçon est acquise, vérifié en jeu** : quatre cases creusées et cinq tuyaux
  posés, puis retour sous terre — creuser et poser **se taisent**, la profondeur **parle**
  (18 cases d'eau morte contre 14 au départ). Les guides du sous-sol étaient éteints avec leur
  couche pendant les gestes : c'est le rallumage qui a tout rattrapé, comme la règle du projet
  l'exige depuis la phase 1.
- **L'horloge s'arrête, mesuré** : horloge à 4 secondes par saison, boîte ouverte, **zéro tick en
  douze secondes** ; boîte refermée, l'horloge repart.
- **Sabotage de `ValidateGuidePosts`** : le guide de la profondeur déplacé de son alcôve au
  carrefour du collecteur → « L'alcôve de guide (46, 25) a 4 voisine(s) ouverte(s), il en faut
  exactement une : un guide posé dans un couloir de passage stériliserait ce passage », et
  `BuildAllScenes` **s'arrête**.
- **Un filet dont j'ai mesuré la faiblesse plutôt que de la supposer.** Le premier sabotage —
  guide planté au bout du collecteur — n'a **pas** été attrapé par le contrôle de desservabilité :
  le parcours ignore ce qui est creusé, puisque n'importe quelle case peut l'être, donc une case
  bloquée se contourne d'un pas. Le commentaire du code le dit maintenant, au lieu de laisser
  croire que ce filet prouve quelque chose qu'il ne prouve pas.
- **Deux défauts vus à l'écran et corrigés** : le triangle d'attention pointait vers le **bas**,
  ce qui en faisait un « cédez le passage » et non un danger ; et le guide du but se tenait
  directement **sous une maison**, dont la goutte vit sur `Surface_Overlay`, un Sorting Layer
  au-dessus de `Surface_Entities`, et recouvrait donc son signal quel que soit son ordre de tri.
  Un signal qu'on ne voit pas n'appelle personne.
- `git diff ProjectSettings/` : **vide**.

### Note d'atelier : un appariement par l'ordre de balayage ment en silence

Les guides ont d'abord été appariés à leur leçon par **l'ordre de `FindAll`**, du bas vers le
haut. Un poste déplacé d'une case aurait changé de leçon sans que rien ne le dise. L'appariement
est désormais écrit **case par case**, et le builder **refuse** un poste que la table ne connaît
pas.

## Après la phase 12 : les routes et les panneaux selon le Code de la route

Demandé le 5 septembre 2026, avant la phase 13 : « aucun panneau mal positionné, à l'envers ou
imaginaire ». Trois fautes dans la phase 12c, et une méthode qui ne pouvait que les produire :

- les panneaux étaient posés **sur** la chaussée, en remplaçant une case de route ;
- leur type sortait de la parité `(x + y) % 4`, pas de la route ;
- le catalogue portait un **sens interdit** sans aucune rue à sens unique, et un triangle de
  danger **sans panonceau** — deux panneaux imaginaires ici.

### Les panneaux sont désormais dérivés du graphe des routes

`VillageLayout.RoadSigns()` les calcule ; plus aucun marqueur « I » n'est écrit dans le plan du
village, et `ValidateDecor` **refuse** qu'il y en ait un. Un panneau ne peut donc être ni mal
placé, ni à l'envers, ni imaginaire : il est là parce que la route l'exige. Les règles sont
celles du Code :

1. **À chaque carrefour, l'axe qui traverse est prioritaire.** Un bras en T qui y débouche est
   secondaire et reçoit un **cédez le passage** (AB3a, pointe en bas), ou un **stop** (AB4) s'il
   débouche sur la route prioritaire. À une croix, l'axe de la rocade est prioritaire.
2. **À droite du conducteur, une case avant la ligne du carrefour.** On roule à droite ; le
   panneau se pose sur l'herbe, à droite de qui arrive. Si l'herbe manque, une case plus loin ;
   sinon rien, et le builder **le dit**.
3. **Une impasse s'annonce à son entrée** (C13a), à droite de qui s'y engage.
4. **La route prioritaire** — la rocade — s'annonce à chacune de ses entrées (AB2, losange
   jaune) et se clôt à chacune de ses sorties (AB6, le même barré).
5. **Un bras d'au plus deux cases vers une maison est un accès**, pas une rue : aucun panneau.
   Le Code ne signale pas les entrées de garage.

Les panneaux de **priorité** se posent en premier, les impasses ensuite : à deux carrefours
distants de deux cases, c'est l'impasse qui recule, jamais le cédez.

Résultat : **32 panneaux**, 11 cédez, 6 stops, 11 impasses, 2 AB2, 2 AB6. Vérifié par calcul,
panneau par panneau : chacun est sur de l'herbe et a la chaussée **à sa gauche** — c'est-à-dire
à la droite du conducteur qu'il vise. Zéro faute, zéro avertissement de placement.

Les panonceaux de galerie ne sont plus des disques bleus — un B21 « direction obligatoire »
dirait au conducteur ce qu'il **doit** faire — mais des **rectangles de jalonnement** : ils
disent où est la station, rien de plus.

### Ce que la logique des panneaux a trouvé dans le tracé des routes

C'est en signalant une impasse là où il n'aurait pas dû y en avoir qu'elle a révélé un défaut
de la phase 12b que rien n'avait vu : **la rue x = 8 traversait l'enceinte de la station de part
en part**, et les murs la coupaient en deux tronçons morts. Puis, en cherchant l'herbe au bord
des routes, elle a trouvé que les rues de l'atelier et de l'usine **finissaient dans leur
façade** — les portes sont au sud des bâtiments, la route arrivait par le nord — et que trois
rues finissaient **sur** la maison qu'elles desservaient.

Retracé : la rue x = 11 contourne la station ; une **rue des portes** en y = 24 passe sous les
deux bâtiments et rejoint la rue du nord par leurs deux côtés ; les rues s'arrêtent une case
avant les maisons ; le bout de chaussée (45, 39), qui touchait la rocade par deux côtés, a
disparu — c'était un coin coupé, pas une rue.

Et deux gardes de plus, parce que tout cela était passé en silence :

- **`ValidateRoads`** : le réseau routier doit être **d'un seul tenant**. Sabotage : un arbre sur
  la rocade en (30, 40) → « 106 cases de route coupées du réseau », la chaîne s'arrête.
- **Un arbre ne pousse que dans l'herbe**, dans l'atelier de conception : le garde a trouvé
  quatre arbres posés par-dessus autre chose — la façade de l'atelier, le mur de la station, la
  haie de bordure, une haie du labyrinthe. 126 arbres au lieu de 131.

`git diff ProjectSettings/` : **vide**.

## Phase 13, ce qui est fait

L'usine à panneaux : le bâtiment et le catalogue. Troisième bâtiment dans lequel on entre, sur
le patron exact de l'atelier des plaques et de l'usine à tuyaux. **Aucun mini-jeu** — Le Stock
vient en 14, La Fabrique en 15, Le Plan en 16.

Elle est une **pure récréation, sans lien avec le réseau** : rien de ce qui s'y passe n'en sort,
donc aucun mini-jeu ne pourra jamais bloquer la progression, et le « aucun échec puni » de
CLAUDE.md reste entier.

### Vingt-quatre panneaux, quatre familles de six

Le catalogue de la phase 12c n'est pas doublé, il est **repris et étendu** : les quatre panneaux
déjà dessinés gardent leur rang, vingt s'ajoutent aux rangs 9 à 28. `SignCount` passe de 9 à 29.

| Rangée | Panneaux |
|---|---|
| Intersection et priorité | AB1, AB25, AB3a, AB4, AB2, AB6 |
| Danger | A1b, A1c, A4, A13b, A14, A21 |
| Interdiction | B0, B1, B2b, B9a, B6a1, B6d |
| Obligation | B21b, B21c1, B21e, B21a1, B22a, B22b |

**Chaque rang existe dans le Code de la route français et porte son numéro.** Les intitulés ont
été recopiés d'une source, pas récités : trois numéros pris de mémoire étaient faux et sont
corrigés — « endroit fréquenté par les enfants » est **A13a** et non A13b, « interdit aux
piétons » est **B9a** (B9b, ce sont les cycles), et « obligation d'aller tout droit » est
**B21b** et non B21-1.

Écartés à dessein, parce qu'illisibles dans une plaque de douze pixels : A3 chaussée rétrécie,
A2a cassis, B3 dépassement interdit (deux voitures), B26 chaînes à neige, B27a autobus. Écartés
aussi comme trop abstraits : AB5 priorité ponctuelle, B15 sens inverse, B2c demi-tour.

**L'impasse C13a et les quatre panonceaux de jalonnement des galeries restent au catalogue** —
le village et le sous-sol les posent — mais ne sont pas sur la planche : ils n'appartiennent à
aucune des quatre familles, et une cinquième rangée dépareillée de cinq casserait la leçon.

### Le cédez le passage était à l'envers depuis la phase 12c

`BuildSign` dessinait AB3a **pointe en haut**, c'est-à-dire un triangle de danger, sous le
commentaire « la pointe EN BAS ». En espace de texture y monte — le poteau occupe le bas, la
plaque le haut — donc une base large en bas fait une pointe en haut. Le commentaire de
`BuildAttentionPicto`, écrit en 12e, disait déjà exactement cela ; celui de `BuildSign` disait
le contraire, et c'est lui qu'on relisait.

Les **onze cédez le passage** du village étaient donc des triangles de danger. Corrigé, et vu à
l'écran avant de l'affirmer.

### Un `default:` qui dessinait autre chose sans le dire

`BuildSign` finissait par un `default:` qui dessinait un panonceau de jalonnement. Porter
`SignCount` de 9 à 29 sans écrire les cas aurait sorti **vingt flèches bleues identiques à la
place des vingt panneaux**, sans un mot — le même défaut que le `default: return cell` de
`GroundAt` relevé par l'audit. Les rangs 5 à 8 sont désormais des `case` explicites, et le
`default:` refuse en nommant le rang.

### Le bâtiment, choisi par calcul parmi 128

Façade en (30..33, 35..36), porte en (31, 34), au **nord du parc**, sur la rue y = 33 qui court
de la bouche (9, 33) à x = 35. Deux marqueurs neufs, `N` façade et `J` porte, avec leurs cinq
éditions cohérentes — constante, `GroundAt`, `IsWalkable`, `PaintVillage`, `BuildVillageMap` —
plus les deux tables `Facades` et `Doors`, dans l'ordre des pièces.

`RoadSigns`, `ValidateRoads`, `ValidateVillage` et `ValidateDecor` ont été **portés en script et
rejoués sur le plan réellement écrit** avant d'écrire une ligne. Le port reproduit au chiffre
près les 32 panneaux consignés le 5 septembre — 11 cédez, 6 stops, 11 impasses, 2 AB2, 2 AB6 —
et c'est seulement après cette preuve que les 128 emplacements possibles ont été mesurés.

| | avant | après |
|---|---|---|
| Panneaux dérivés | 32 | **32**, aucun ajouté ni retiré |
| Carrefours | 20 | 21 — (31, 33) devient un T |
| Cases praticables | 2241 | **2233**, les huit cases de façade |
| Cases de chaussée | 289 | 290 — la seule ajoutée est la porte |
| Étalement à `Lost = 5` | 278 | **278**, inchangé |

**La flaque ne perd rien** : la case de façade la plus proche d'une bouche est à **cinq pas**,
un de plus que `MaxFloodRadius`. Le débordement est le seul retour permanent du jeu ; on ne le
rogne pas pour un bâtiment.

**Le Code décide, pas moi** : le bras nord du nouveau carrefour ne fait qu'une case, c'est donc
un accès et non une rue, et aucun panneau ne s'y pose. L'usine à panneaux est le seul bâtiment
du village sans panneau devant sa porte.

### La pièce : une galerie, une famille par rangée

Créneau (0, 0) — le voisin du dessus est vide et le dessous est hors carte. En (0, 10) on aurait
vu la rangée de mur de l'atelier flotter en l'air, un créneau faisant dix lignes quand la caméra
en montre 11,25.

```
####################   y=9
#..................#   y=8   laissée vide : la rangée de gouttes du HUD passe ici
#...S.S.S.S.S.S....#   y=7   INTERSECTION ET PRIORITÉ
#..................#   y=6
#...S.S.S.S.S.S....#   y=5   DANGER
#..V.....V.....V...#   y=4   Le Stock · La Fabrique · Le Plan
#...S.S.S.S.S.S....#   y=3   INTERDICTION
#..................#   y=2
#...S.S.S.S.S.S....#   y=1   OBLIGATION
#########D##########   y=0
```

**La forme dit la famille avant que le dessin dise le détail** : un triangle bordé de rouge
prévient, un disque bordé de rouge interdit, un disque bleu plein oblige. C'est cette grammaire
qui s'apprend d'abord, et c'est pour elle qu'une rangée vaut une famille.

La géométrie n'est pas libre. Un panneau fait 16x24 au pivot du joueur : il occupe de `y - 0,5`
à `y + 1,0`. Deux rangées écartées de **deux** laissent une demi-case entre la plaque du bas et
le poteau du haut ; à une case elles se chevaucheraient. Et les personnages se tiennent en
**colonnes impaires**, où aucun panneau ne se dresse — sinon un panneau leur passerait devant le
visage.

**La rangée du haut est laissée vide, et ce n'est pas de l'esthétique.** Premier jet, les quatre
rangées étaient en y = 8, 6, 4, 2 : vu à l'écran, la première famille passait **derrière la
rangée de gouttes du HUD**. Tout le tableau est descendu d'une rangée.

### Les trois personnages, et le nom au HUD

| Case | Qui | Ce qu'il dit |
|---|---|---|
| (3, 4) | Le Stock, phase 14 | « MES PANNEAUX SONT EN DÉSORDRE » · « RETROUVE-LES DEUX PAR DEUX » |
| (9, 4) | La Fabrique, phase 15 | « JE DESSINE LES PANNEAUX » · « SAURAS-TU LES NOMMER ? » |
| (15, 4) | Le Plan, phase 16 | « IL MANQUE DES PANNEAUX ICI » · « POSE-LES SUR MON PLAN » |

Ce sont des `Villager`, pas des `GuidePost` : ils ne portent aucune condition, ils parlent
toujours. L'arrêt de l'horloge pendant qu'on parle, l'afficheur d'attention et `SetLines` sont
ceux de la phase 12e, ni redoublés ni modifiés.

**On marche sur un panneau, son nom s'affiche au HUD.** `ItemLabel` est celui des plaques et des
tuyaux depuis la phase 9 ; un composant `SignCatalogue` sur le patron de `PipeFactory` lui dit
quel nom montrer. Aucun `InteractionKind` neuf, et c'est voulu : un panneau **ne se prend pas**,
il se lit — un kind non nul ferait agir Espace devant un panneau exposé.

C'est ce qui sépare une salle décorée d'un catalogue, et c'est là-dessus que La Fabrique
s'appuiera en phase 15 pour demander le nom parmi trois noms.

### `ValidateRooms` : compter ne suffit pas

`InteriorsLayout.IsWellFormed` exigeait **exactement un** personnage par pièce, et le commentaire
d'`InteriorsSceneBuilder` qui promettait le contraire était faux : l'usine en veut trois.

Chaque pièce **déclare son nombre de personnages**, à côté du plan qu'il décrit, et le validateur
s'y compare. On reste sur un « exactement N » : une faute de frappe est toujours refusée.

Mais compter ne prouve rien sur **qui** ils sont. `CreateVillagers` n'apparie plus par le rang de
la pièce — ce qui ne pouvait marcher qu'à un personnage par pièce — mais **case par case**, et
`ValidateRooms` vérifie l'appariement **dans les deux sens** : une case du plan qui n'est dans
aucune table, une table qui ne tombe sur aucune case. Même chose pour les vingt-quatre panneaux.
C'est la leçon des huit guides de la phase 12e : un appariement par l'ordre d'un balayage ment
en silence.

Et l'ordre compte ici pour une raison précise : `FindAll` balaye **du bas vers le haut**, alors
que la planche se lit de haut en bas. S'y fier aurait apparié la rangée OBLIGATION avec la
famille INTERSECTION, et vingt-quatre panneaux seraient sortis sous le mauvais nom.

## Phase 13, vérifications faites

- Compilation relue par le pont MCP : **zéro erreur, zéro warning**.
- **Les six validateurs passent** sur le monde neuf, et journalisent ce qu'ils ont prouvé :
  « 2233 cases praticables, toutes reliées ; 126 arbres, 32 panneaux dérivés des routes »,
  « 2844 cases vivantes sur 2880, 14 destinations atteignables sur 14 », « 4 alcôves de guide »,
  « bilan de l'eau tenable ». Les 2233 et les 32 avaient été **calculés d'avance** : ils tombent
  au chiffre près.
- **Quatre sabotages, quatre refus nommés**, chacun par le fichier et une vraie recompilation —
  jamais par réflexion, un champ `static readonly` ne se sabote pas ainsi :
  - un `V` effacé → « La pièce « Usine a panneaux » porte 2 personnage(s), il en faut exactement
    3 », et `BuildAllScenes` **s'arrête** : « Construction interrompue : une scène a refusé » ;
  - un `V` **déplacé d'une case** → le compte reste juste, `IsWellFormed` passe, et c'est le
    second filet qui attrape : « Le personnage (10, 5) n'est dans aucune table », puis « La table
    annonce un personnage en (9, 5), mais le plan n'y met aucun « V » ». **Les deux filets sont
    indépendants, et le second attrape ce que le premier laisse passer** ;
  - un rang exposé deux fois → « Le rang de panneau 9 est exposé deux fois sur la planche » ;
  - `SignCount` porté à 30 sans dessin → « Le rang de panneau 29 n'est dessiné nulle part.
    SignCount vaut 30 : ajoute son cas dans BuildSign, ou baisse SignCount. »
- **Les vingt-quatre panneaux ont été REGARDÉS, pas seulement écrits.** Une planche agrandie huit
  fois a montré six dessins ratés que le code ne pouvait pas signaler : AB1 et AB25 étaient
  indiscernables, A1b et A1c aussi, et A4, A21 et B2b sortaient en pâtés noirs. Redessinés, et
  revus. Le blanc d'un triangle est étroit et penche : les pictogrammes s'y posent désormais
  sous une garde qui refuse d'écrire ailleurs que sur le fond blanc, au lieu de trouer la
  bordure rouge.
- **Test en play, entrées clavier vraiment injectées**, `Application.isFocused` vérifié à chaque
  appui : **zéro image injectée sans focus**. Entrée par la porte (31, 34) → couche Interior,
  case (9, 0) ; les **trois personnages** parlent, deux phrases chacun, boîte refermée ; deux
  panneaux foulés donnent le bon cartel — (4, 3) → rang 17, B0 « CIRCULATION INTERDITE », et
  (14, 3) → rang 22, B6d, le premier et le sixième de la rangée INTERDICTION ; puis sortie par
  la porte intérieure (9, 0) → retour en surface en (31, 34).
- `Application.runInBackground = true` posé **à chaud à chaque session de play**, jamais dans les
  ProjectSettings. Le pilote de test passe par `DontDestroyOnLoad`, et il a été **supprimé** une
  fois la vérification faite.
- **Captures** dans `Captures/`, dossier ignoré par git : la façade dans le village, la planche
  entière avec ses trois personnages, le nom d'un panneau au HUD, et la planche agrandie.
- **La table d'étalement des flaques, recalculée** — et fausse depuis la phase 12c, qui l'avait
  déclarée inchangée sans la reprendre. Mesurée en phase 13 par `VillageLayout.IsWalkable`, le
  prédicat dont `FloodView.Paint` se sert : 647 cases bloquantes, et 278 cases mouillées à
  `Lost = 5`. Le bâtiment neuf, lui, n'y change rien.
- `git diff ProjectSettings/` : **vide**.

### Note d'atelier : une touche TENUE et une touche TAPÉE ne se valent pas

Le pilote injectait les touches en appelant `InputSystem.Update()` à la main. Les flèches
marchaient — elles se lisent en continu — mais **Espace ne faisait rien** : `WasPressedThisFrame`
se lit sur un front, et le front avait déjà été consommé par une passe qui n'était pas celle du
jeu. Le personnage marchait donc parfaitement et n'entrait jamais dans le bâtiment. On se
contente désormais de **mettre l'évènement en file**, et la boucle normale le traite.

Puis, une fois Espace réparé, la marche : **tenir la flèche et la relâcher à une demi-case du
but** — la technique de la phase 12b — dépassait d'une case environ un pas sur dix. Assez rare
pour qu'un trajet court réussisse, assez fréquent pour qu'un trajet long échoue, et le pilote
finissait contre un personnage bloquant ou sur le mauvais panneau. Un résultat juste aurait eu
l'air faux. Un pas se fait donc par un **appui bref** : le `PlayerController` engage un pas quand
il voit la touche, et ce pas va au bout tout seul ; la touche relâchée, aucun second pas ne
s'engage. Et la marche est **en boucle fermée** — on relit la case où l'on est à chaque tour au
lieu de compter les pas.

## Phase 14, ce qui est fait

**Le Stock**, le premier des trois mini-jeux de l'usine à panneaux : un memory. Il valide le
squelette que La Fabrique (15) et Le Plan (16) reprendront. Rien de ce qui s'y passe ne sort du
bâtiment, et **rien ne s'y sauvegarde**.

### Panneau contre panneau, et le nom en récompense

Le panneau contre son NOM avait été mesuré impossible **avant** d'écrire une ligne. `PixelFont`
est à chasse fixe, `WidthOf = 6n + 1` : sur les 24 noms réels, le plus court fait **37 px**
(DANGER), la médiane **121**, le plus long **193** (ARRÊT ET STATIONNEMENT INTERDITS).

| voie | ce qu'elle donne |
|---|---|
| cartes de 193 px | 1 colonne. Pas une grille. |
| cartes de 150 px | 2 colonnes, 4 rangées, **4 paires**, et 2 noms débordent encore |
| ne garder que les noms courts (≤ 89 px) | il n'y en a que **quatre sur vingt-quatre** |
| rendu multi-ligne dans `PixelFont` | code neuf, 24 images refaites, et **6 paires** |

Donc panneau contre panneau. Et le nom n'est pas perdu, il est **gagné** : chaque paire trouvée
affiche son nom en bas de l'écran, par `SignNameTexture`, qui existait déjà. Le nom devient la
récompense de la trouvaille au lieu d'être l'énigme — ce que le personnage disait depuis la
phase 13, « RETROUVE-LES DEUX PAR DEUX » — et c'est ce qui prépare La Fabrique, dont le sujet
**est** de nommer.

### La grille n'est pas libre, elle est calculée

Cartes de **32 px**, le plancher de zone cliquable de CLAUDE.md ; gouttière de 4 ; une bande de
13 px en bas pour le nom.

| grille | cartes | paires | plateau | marges |
|---|---|---|---|---|
| 4 × 4 | 16 | 8 | 140 × 140 | 90 / 12 |
| 4 × 6 | 24 | 12 | 212 × 140 | 54 / 12 |
| 4 × 8 | 32 | 16 | 284 × 140 | 18 / 12 |

Et les deux bornes dures : **5 rangées font 176 px pour 156 disponibles**, **9 colonnes font
320 px pour 312**. Le panneau reste à **1:1, 16 × 24**, la taille qu'il a dans le village et
dans la pièce ; le doubler plafonnerait à 3 rangées et 9 paires, mesuré aussi.

**Le fond de l'écran est opaque, et ce n'est pas de l'esthétique.** La rangée de gouttes du HUD
occupe le coin haut-droit de y = 160 à 176, exactement là où passe la rangée haute du plateau.
Le voile à 0,6 de `VillageMapScreen` y aurait laissé quatorze gouttes transparaître au travers
des cartes. C'est le piège de la phase 13 à l'identique.

### 8, puis 12, puis 16 — et 2, 3 puis 4 par famille

Les trois tailles sont **divisibles par quatre** : chaque manche tire donc un nombre égal de
panneaux **dans chacune des quatre familles**, au hasard dans la famille. Les quatre formes sont
toujours à l'écran, et la leçon de la pièce — *la forme dit la famille avant que le dessin dise
le détail* — tient dans le mini-jeu au lieu d'y être contredite par un tirage de six triangles
rouges.

**Une manche par lancement**, plus grande à chaque fois, plafonnée à 16 paires. Le compteur est
un **champ du composant**, jamais un octet de `partie.json` : il survit à une sortie du bâtiment,
puisque le `SceneRouter` éteint la couche sans la détruire, et pas à une fermeture du jeu.
`SaveSystem.TryLoad` n'est pas touché.

**On sort après chaque manche**, délibérément : le jeu n'a qu'Espace, et enchaîner quatre manches
d'office enfermerait l'enfant dans un écran dont aucune touche ne permet de sortir.

### Aucune minuterie, nulle part

Deux cartes qui ne vont pas ensemble **restent visibles jusqu'à la prochaine action du joueur,
quelle qu'elle soit** — une flèche ou un Espace les retourne. L'enfant regarde aussi longtemps
qu'il veut, et rien ne lui est demandé dans un délai. C'est la seule façon trouvée de tenir
« aucun timing serré » sans lui reprendre l'information qu'il vient de voir — et c'est aussi la
seule qui se vérifie de façon déterministe, ce qu'un délai n'est pas.

Le curseur **saute les cartes déjà appariées**, et se repose tout seul sur la plus proche carte
encore en jeu quand la sienne vient d'être trouvée : aucun coup perdu, aucun appui dans le vide.
Aucun score, aucun compte de coups, aucun chrono.

### Le squelette, puisque 15 et 16 le reprendront

`Assets/Scripts/Minigames/`, imposé par CLAUDE.md et vide depuis le début, s'ouvre ici.

- **`MiniGameScreen`**, abstraite, porte tout ce que les trois partagent : le panneau éteint, la
  garde `openedFrame` jumelle de `PlayerInteractor.screenClosedFrame`, la lecture des flèches au
  seul changement de direction, le garde-fou de rechargement de domaine, l'événement `Closed`, un
  registre statique, et le compteur `AnyOpen`. C'est le patron de `VillageMapScreen` assemblé
  avec celui de `SpeechBox` ; ni l'un ni l'autre n'est redoublé.
- **`GameClock` lit désormais `SpeechBox.AnyOpen || MiniGameScreen.AnyOpen`.** Sans cela une
  manche de seize paires — plusieurs minutes — verrait tomber un tick de saison, et le village
  gèlerait pendant que l'enfant joue à autre chose, dans une pièce que les saisons ne touchent
  même pas.
- **`Move` et `Validate` sont publiques**, séparées de la lecture du clavier, comme
  `VillageMapScreen.Select` depuis la phase 7. Et **`Deal` est statique, pure et déterministe** :
  c'est elle qui se vérifie par le calcul, sans écran et sans hasard.

### Le lancement : après la phrase, et sans un `InteractionKind` de plus

`Villager` gagne un champ `miniGame`, nul pour les dix autres personnages. `PlayerInteractor`
ouvre l'écran dans `OnSpeechClosed` : Le Stock dit ses deux phrases, **puis** le jeu commence,
l'ordre que ses répliques écrivaient déjà. Le personnage joueur reste éteint d'un écran à
l'autre, et `screenClosedFrame` n'est posé qu'à la fermeture **du mini-jeu**.

**Aucune référence sérialisée ne va du personnage à l'écran**, et ne le pourrait pas : le
personnage vit dans `Interiors`, l'écran dans `Persistent`, et Unity ne sérialise pas une
référence d'une scène vers une autre. Le registre statique de `MiniGameScreen` les relie, comme
la boîte de dialogue l'était déjà.

### Deux images neuves, et pas une de plus

| image | pourquoi |
|---|---|
| `sign_back.png`, 16 × 24 | **le dos d'un panneau** : plaque grise, liseré, bride et deux boulons. Un panneau a un dos, et Victorien le sait. |
| `picto_card_cursor.png`, 32 × 32 | `cursor_target` fait 16 px et flotterait au milieu d'une carte de 32. `BuildCursor` prend une taille en paramètre ; l'image de 16 est au pixel près celle d'avant. |

Le picto de fin de manche est **`picto_exit`**, celui qui sort déjà des bâtiments : même geste,
rien à apprendre. **Aucun texte neuf** : la bande de nom réutilise les 24 images de la phase 13.

## Phase 14, vérifications faites

- Compilation relue par le pont MCP : **zéro erreur, zéro warning**.
- **Les six validateurs passent** sur le monde neuf, aux chiffres exacts de la phase 13 :
  2233 cases praticables, 32 panneaux dérivés, 2844 sur 2880, 14 destinations sur 14, 4 alcôves,
  bilan de l'eau tenable. Rien n'a bougé, et c'était le but : le memory est un écran, il
  n'ajoute rien à la carte ni à la pièce.
- **Le tirage vérifié par le calcul, sur 1000 graines et pour les trois tailles** : exactement
  `2 × paires` cartes, exactement deux exemplaires de chaque rang, aucun rang hors planche, et
  exactement `paires / 4` par famille — 999 tirages sur 999 différents du premier. Et quatre
  refus attendus obtenus : 13 paires (non divisible par 4), 28 paires (plus que 6 par famille),
  une planche de 25, et 0 paire.
- **Cinq sabotages, cinq refus nommés**, chacun par le fichier et une vraie recompilation :
  - `MemoryRows = 5` → « Le plateau du memory fait 176 px de haut pour 156 disponibles » ;
  - une manche de 20 paires → « demande 10 colonnes, soit 356 px pour 312 disponibles » ;
  - `MemoryCardSize = 30f` → « fait 30 px de côté : CLAUDE.md impose au moins 32 px » ;
  - Le Stock posté sur la case de La Fabrique → **les deux filets tirent séparément** : « c'est
    villager_maker.png qui s'y tient et non villager_stock.png : le personnage lancerait le jeu
    de son voisin », puis « Le personnage (3, 4) de l'usine ne tient aucun mini-jeu » ;
  - et le filet de la phase 13 refuse toujours : un `V` effacé → « porte 2 personnage(s), il en
    faut exactement 3 ».
  Les cinq fois, `BuildAllScenes` **s'arrête** : « Construction interrompue : une scène a refusé ».
- **Test en play, entrées clavier vraiment injectées**, trajet complet : entrée par la porte
  (31, 34) → couche Interior en (9, 0) → marche jusqu'en (3, 5), demi-tour vers Le Stock en
  (3, 4) → ses deux phrases → **manche de 8 paires jouée jusqu'à la dernière**, y compris une
  erreur volontaire pour éprouver le chemin des cartes dépareillées → Espace referme → l'écran
  **ne se rouvre pas** → sortie par la porte intérieure (9, 0) → retour en surface en (31, 34).
  **Zéro appui injecté sans focus.**
- **La montée en difficulté vérifiée en jeu** : lancements 2, 3 et 4 → 12 paires en 4 × 6 puis
  16 en 4 × 8 puis 16 encore, familles 3/3/3/3 puis 4/4/4/4.
- **L'horloge s'arrête bien pendant qu'on joue**, et repart à la fermeture. Vérifié par
  `MiniGameScreen.AnyOpen` aux deux instants.
- **Les deux dessins neufs ont été REGARDÉS**, agrandis six fois et posés sur les trois fonds de
  carte — cachée, visible, appariée — avant toute construction.
- **Captures** dans `Captures/` : le plateau au début, une manche en cours avec deux paires
  trouvées et le nom À DROITE OU À GAUCHE en bas, la fin avec le picto de sortie, le plateau le
  plus serré à 16 paires, et la planche des cartes agrandie.
- `Application.runInBackground = true` posé **à chaud à chaque session de play**, jamais dans les
  ProjectSettings. Le pilote de test passait par `DontDestroyOnLoad`, et il a été **supprimé**.
- `git diff ProjectSettings/` : **vide**.

### Note d'atelier : un `const` replié fait passer un filet pour du code mort

`ValidateMemoryBoard` commençait par `if (MemoryCardSize < 32f)`, deux `const` : le compilateur
replie la comparaison, voit le corps comme inatteignable, et sort **CS0162**. La tentation est de
supprimer la garde pour faire taire l'avertissement — c'est-à-dire de supprimer le filet parce
qu'il passe. Un réglage n'est pas une constante de compilation : `MemoryCardSize` est passé en
`static readonly`, le plancher de 32 est devenu une constante nommée, et le filet garde ses dents
— le sabotage à 30 px l'a prouvé ensuite.

### Note d'atelier : le focus perdu ressemble exactement à un bug du jeu

Le premier passage du pilote a échoué sur « appui sans effet sur le curseur ». Rien dans le
symptôme ne disait le focus : la marche, le passage de la porte et le dialogue avaient tous
réussi, et c'est seulement le memory qui ne répondait pas — un résultat qui aurait fait chercher
un bug dans le code neuf. Le compteur d'appuis sans focus, lui, disait 5.

Deux corrections, et les deux comptent. D'abord **le pilote ATTEND le focus au lieu de compter
les coups perdus** : un appui injecté sans focus n'arrive nulle part, et le compter ne sert qu'à
expliquer l'échec après coup. Ensuite **le pilote prend ses captures lui-même** : s'arrêter à
chaque étape pour laisser l'opérateur capturer multiplie les allers-retours, et chacun est une
occasion pour une autre application de reprendre le premier plan au milieu d'une manche.

### Note d'atelier : deux triangles que l'œil confond, et que le programme ne confond pas

En relisant la capture de fin, j'ai compté 1/2/3/2 par famille là où le tirage garantit 2/2/2/2 :
AB1, un triangle bordé de rouge portant une croix noire, se lit comme un triangle de danger à
seize pixels. Le programme, interrogé, a rendu les huit noms et les familles justes. **C'est mon
œil qui avait tort, pas le tirage** — mais le fait qu'un panneau d'INTERSECTION se lise comme un
panneau de DANGER est une remarque pour l'habillage, notée aux placeholders.

## Phase 15, ce qui est fait

**La Fabrique**, le deuxième mini-jeu : un panneau, trois noms écrits, on choisit. Le squelette
de la phase 14 est repris **sans une ligne changée** dans `MiniGameScreen` ; `SignQuiz` n'écrit
que sa règle, comme `SignMemory` n'avait écrit que la sienne. Le lancement était déjà branché :
`MiniGamePosts` déclarait La Fabrique en (9, 4) depuis la phase 14, et `MiniGameScreen.Find` la
trouve dès que l'écran existe.

### La forme, tranchée le 5 septembre 2026

- **Le panneau à gauche, agrandi trois fois par le Canvas** — à filtre point un pixel en fait
  neuf, rien n'est lissé — et **trois noms à droite** sur des rangées de 32 px, le plancher de
  CLAUDE.md. La rangée fait 204 px : le plus long nom, 193, y tient. 48 + 12 + 204 = 264 ≤ 312.
- **8 questions par lancement, 2 par famille**, tirées par `SignDraw.PerFamily`, **sorti de
  `SignMemory.Deal`** pour servir aux deux. Le memory ressort identique : la graine 100 donne
  les mêmes huit panneaux qu'en phase 14, comparés nom pour nom.
- **Les leurres se resserrent d'un lancement à l'autre** : 0, puis 1, puis 2 faux noms de la
  même famille que le panneau. Au premier, la grammaire des formes suffit — un disque bleu n'est
  pas un SENS INTERDIT. Au troisième, il faut lire le pictogramme. Compteur dans le composant,
  rien sur le disque.
- **Un nom faux s'éteint, on rechoisit.** Au plus deux erreurs par question, jamais de
  révélation. Un nom juste passe au vert, les deux autres s'éteignent, et **le geste suivant,
  quel qu'il soit, passe à la question d'après** — aucune minuterie, comme en 14.
- **Une jauge de huit carrés** en haut dit où l'on en est. **Aucune image neuve** : le panneau
  est le sprite de la planche, les rangées et la jauge sont des `Image` teintées sans sprite,
  les noms sont les vingt-quatre de la phase 13.

## Phase 15, vérifications faites

- Compilation : **zéro erreur, zéro warning**. Les six validateurs aux chiffres de la phase 13.
- **`SignQuiz.Build` sur 1000 graines et les trois réglages de leurres** : 8 questions, 2 cibles
  par famille, 3 noms distincts, la cible parmi eux, exactement le nombre demandé de leurres de
  la même famille — 999 manches sur 999 différentes de la première. Refus obtenus : 3 leurres,
  7 questions.
- **`SignMemory.Deal` identique avant et après le refactor**, graine 100, nom pour nom.
- **Trois sabotages, trois refus nommés** dans une même construction, chaîne interrompue :
  rangée à 30 px → « CLAUDE.md impose au moins 32 px » ; rangée à 260 px → « Le quiz fait 320 px
  de large pour 312 disponibles » ; trois leurres → « il n'y a que deux faux noms par question ».
- **Test en play, touches injectées**, trajet complet : porte (31, 34) → (9, 5), demi-tour vers
  La Fabrique → deux phrases → **huit questions jouées**, une faute volontaire à la première
  (« nom faux éteint, on rechoisit ; le choix s'est posé sur la rangée 0 »), passage à la
  suivante tantôt par Espace, tantôt par une flèche → Espace referme, l'écran ne se rouvre pas →
  sortie par (9, 0) → surface en (31, 34). **Zéro attente de focus.**
- **La rampe des leurres vérifiée en jeu**, lancements 2, 3, 4 : 1, 2, 2 de la même famille.
- Horloge arrêtée pendant le quiz, repartie après. Captures : début, en cours, fin.
- Pilote supprimé, `git diff ProjectSettings/` vide.

## Phase 16, ce qui est fait

**Le Plan**, le troisième et dernier mini-jeu de l'usine à panneaux. Une petite carte de rues,
des poteaux vides, et les cinq panneaux de rue du village à poser au bon endroit.

### La règle est le Code, tel que `RoadSigns` l'applique

Les postes — où un panneau manque — et le panneau attendu à chacun **ne sont écrits nulle part**.
Ils sont **dérivés** à la construction par les mêmes règles que les 32 panneaux des rues du
village. Une solution écrite à la main pourrait être fausse ; une solution déduite du Code ne
peut pas l'être — et c'est ce qui rend « vérifié solvable par le calcul » vrai pour les quinze
plans.

Pour cela les règles sont **sorties de `VillageLayout`** dans `RoadSignRules`, derrière une
interface `IRoadGrid` que le village et les plans implémentent chacun. **Les 32 panneaux du
village sont ressortis identiques** — case, type, sens et ordre — comparés à une photographie
prise avant le refactor. Une seule règle s'ajoute, `IsExit` : une rue qui touche le bord d'un
plan **continue** hors du cadre, sinon chaque rue qui sort serait une impasse. Le village n'en a
aucune.

### Quinze plans écrits à la main, du plus simple au plus lourd

| | plan | postes | ce qu'il apprend |
|---|---|---|---|
| 1 | Le premier cédez | 1 | la rue qui débouche cède |
| 2 | Le cédez d'en haut | 1 | le panneau change de côté avec le conducteur |
| 3 | La croix | 2 | l'est-ouest passe, le nord-sud cède |
| 4 | L'impasse | 2 | une rue qui finit devant une maison cède ET est une impasse |
| 5 | Deux rues | 2 | deux cédez, pas du même bord |
| 6 | L'entrée de garage | 1 | un accès de deux cases n'a droit à rien |
| 7 | La route prioritaire | 4 | annoncée à l'entrée, close à la sortie, deux fois |
| 8 | Le premier stop | 5 | sur la prioritaire, ce n'est plus un cédez |
| 9 | L'impasse qui recule | 3 | deux panneaux veulent la même case : la priorité d'abord |
| 10 | La croix prioritaire | 6 | deux stops |
| 11 | Le virage prioritaire | 5 | la prioritaire tourne |
| 12 | L'impasse de la rue | 7 | une impasse débouche sur la rue qui débouche |
| 13 | Trois stops | 7 | croix et rue sur la prioritaire |
| 14 | Le virage à deux rues | 6 | une fin de route qui recule d'une case |
| 15 | Le grand carrefour | 7 | tout ensemble, des deux côtés |

La route prioritaire se dessine avec `=` ; ses coins sont déduits et elle doit être d'un seul
trait. **À l'écran elle est teintée** : c'est la seule chose que le joueur doit voir pour choisir
un stop plutôt qu'un cédez. Chaque plan dérivé a été **regardé**, postes superposés à l'ASCII,
avant d'être cru — et le côté du conducteur revérifié à la main sur les plans 1, 7, 11, 12 et 14.

### Une seule touche, et le geste qui juge

Les flèches vont de poteau en poteau ; Espace **fait défiler** le panneau du poteau visé — vide,
cédez, stop, impasse, prioritaire, fin, vide. Aucun second niveau de choix, aucune palette à
ouvrir : c'est la contrainte de CLAUDE.md, et chaque panneau passe sous les yeux à son tour.

**Le plan vérifie quand tous les poteaux sont garnis, au geste suivant** — la flèche qui dit
« j'ai fini de poser ». Les justes se fixent, sol vert ; les faux se vident ; on repose. Aucun
échec. Un plan par lancement, le suivant à chaque fois, retour au premier après le quinzième et à
la fermeture du jeu : un champ, rien sur le disque.

### Une seule image neuve

Le **poteau vide** : le poteau des vingt-neuf autres, et à la place de la plaque un pointillé.
Les tuiles du plan sont celles du village — herbe, les seize routes, la maison, les cinq
panneaux — **agrandies deux fois par le Canvas**, à filtre point.

## Phase 16, vérifications faites

- Compilation : **zéro erreur, zéro warning**. Les six validateurs aux chiffres de la phase 13.
- **Les 32 panneaux du village identiques au refactor**, comparés case, type, sens et ordre à la
  photographie prise avant.
- **`PlanLayout.Validate` passe sur les quinze**, et **refuse quatre sabotages d'un coup**, chacun
  par son nom : dix-huit plans au lieu de quinze ; la route prioritaire sur la ligne du bord →
  « Pas d'herbe libre pour un panneau PriorityEnd … un plan à redessiner » ; dix colonnes → « fait
  10 x 3 : au plus 9 x 5 » ; le coude prioritaire dans l'axe d'une rue → « au carrefour (4, 2) de
  la route prioritaire, le bras (4, 1) n'est ni prioritaire ni signalé. La route prioritaire y
  céderait le passage à une rue ordinaire. » — le filet le plus subtil, celui qui attrape une
  faute de tracé que le dessin ne montre pas.
- **Test en play, touches injectées**, trajet complet sur le plan 1 : porte (31, 34) → (15, 5),
  demi-tour vers Le Plan → deux phrases → **tous les poteaux garnis dont un faux exprès, la flèche
  qui juge, le faux se vide**, on repose le bon, le plan est juste → Espace referme, l'écran ne se
  rouvre pas → sortie par (9, 0) → surface. **Zéro attente de focus.**
- **Les quinze plans joués par l'API**, sans clavier : navigation de poteau en poteau par
  `Move`, pose par `Validate`, un faux exprès, jugement — le faux vidé, les n − 1 autres fixés,
  puis juste. Quinze sur quinze.
- **Le plan 12 REGARDÉ à l'écran, et c'est lui qui a montré le défaut** : le cédez de la rangée
  haute sortait **coupé par le bord**. Un panneau de 16 sur 24 agrandi deux fois déborde de 16 px
  au-dessus de sa case, et 5 × 32 + 16 = 176 ne laisse que 4 px dans 180. Le plateau descend de
  8 px, `ValidatePlanBoard` compte désormais le débordement, et la capture refaite montre le
  cédez entier. **Ni le validateur, ni la dérivation, ni les quinze plans joués par l'API ne
  pouvaient le voir.**
- Horloge arrêtée pendant le jeu, repartie après. Captures : début, après le jugement, fin, le
  plan 12 en cours, et la planche des images du plan.
- Pilote supprimé, `git diff ProjectSettings/` vide.

### Note d'atelier : ce qui déborde d'une case déborde de l'écran à la rangée du haut

Un sprite au pivot du joueur dépasse de sa case vers le haut — c'est voulu depuis la phase 1, la
tête passe devant ce qui est derrière. Dans le monde la caméra suit et rien ne coupe. Dans un
écran modal, la rangée haute d'une grille qui remplit l'écran n'a **rien au-dessus d'elle**, et
le dépassement sort du cadre. Même famille que la rangée de gouttes de la phase 13 : la géométrie
des cases était juste, celle de ce qui se dresse dessus ne l'était pas, et seule une capture l'a
dit.

### Note d'atelier : deux commandes de menu en parallèle décrochent le pont

Lancer la génération d'art et une construction de scène dans le même tour a rendu « plugin
session disconnected » trois fois — la commande s'exécutait quand même jusqu'au bout, mais sa
réponse était perdue et il fallait relire la console pour le savoir. Un menu long à la fois.

## Phase 17a, ce qui est fait

**La palette et la méthode.** Aucune forme n'a changé : cette sous-phase installe ce qui rend
l'habillage vérifiable, et ramène les couleurs du jeu à une table. C'est délibéré — redessiner
486 images sans règle de couleur donnerait 486 images cohérentes par chance.

### Trente-quatre couleurs, nommées par ce qu'elles sont

Avant : **88 valeurs** choisies une par une au fil de seize phases, sans nom ni table, dont trois
gris qu'aucun œil ne séparait. Après : `Palette`, **34 couleurs**, chacune nommée par son emploi —
`Stone` est la chaussée, `SignRed` le rouge du Code. Les 135 littéraux du générateur ont été
remplacés ; il n'en reste **aucun**.

Six couleurs sont **imposées par le gameplay** et ne fondent avec rien : les trois profondeurs de
terre et les trois de galerie. La règle de profondeur croissante EST le puzzle, et elle se lit
d'abord à la nuance du sol. Deux autres sont imposées par les personnages — le violet et le
sarcelle : sans elles, deux des cinq habitants porteraient la même couleur.

**`Darken` a disparu.** Il multipliait les canaux : le résultat n'était dans aucune table, personne
ne l'avait choisi, et il échappait par construction à tout contrôle. Les neuf appels passent par
`Palette.Shade`, qui rend **une autre couleur de la palette** et refuse en nommant celle dont la
nuance n'est pas déclarée.

### Deux filets neufs, et ils barrent la construction

- **`ValidatePalette`** : chaque pixel de chaque image écrite sur le disque est une couleur de la
  palette, ou transparent. Relu **sur les fichiers**, jamais sur ce que le code croit avoir
  dessiné. L'alpha ne compte pas — l'eau et le voile sont des couleurs de la palette qu'on voit au
  travers.
- **`ValidateDistinct`** : ce que le jeu distingue ne doit pas être deux fois la même image. Huit
  plaques, trois motifs de tuyau, cinq personnages, 29 panneaux, six sols, quatre saisons — et les
  **six couleurs de corps**, qui ne sont pas des images mais un ensemble.

### Et une planche, parce que rien ne se déclare fini sans être regardé

`Sous La Ville/Planche de l'art` rend la palette, les tuiles, les sprites et les pictos agrandis
sur quatre feuilles. C'est la leçon des phases 13, 14 et 16 : trois fois le code compilait, les
validateurs passaient, et seule une image agrandie a montré six dessins ratés, puis une rangée
cachée derrière le HUD, puis un panneau coupé par le bord.

## Phase 17a, vérifications faites

- Compilation : **zéro erreur, zéro warning**. Les six validateurs aux chiffres de la phase 13.
- **243 images, 34 couleurs et pas une de plus.**
- **Deux sabotages, deux refus nommés** : une couleur magenta écrite en dur dans le dos d'un
  panneau → « sign_back.png porte en (7, 0) la couleur #7F007F, qui n'est pas de la palette » ;
  Le Stock remis sur l'orange du joueur → « Les personnages 0 et 3 portent tous deux la couleur
  « Orange » : on ne les distinguerait pas de loin ». Les deux fois, `BuildAllScenes` s'arrête.
- **Le village vu en jeu**, identique à l'œil aux teintes près.
- **Les six personnages regardés côte à côte, agrandis huit fois.**

### Note d'atelier : une palette fond ce que le jeu distinguait, et rien ne le dit

Le personnage joueur portait `#E05A2B`, Le Stock `#D07A2E` — deux oranges distincts à l'œil,
choisis à deux phases d'écart. La palette de trente-quatre les a fondus **sur la même couleur**, et
Le Stock est devenu le sosie du joueur.

Rien ne pouvait le dire. La compilation, non. Le contrôle de palette, non — ce sont de bonnes
couleurs. La comparaison des images entre elles, **non plus** : les deux sprites diffèrent par le
repère de direction du joueur, donc ils ne sont pas identiques au pixel près. C'est **la capture du
village** qui l'a montré. Le Stock passe en brique, les six couleurs de corps vivent désormais dans
une seule table, et `ValidateDistinct` exige qu'elles soient six.

**Une contrainte qui porte sur un ENSEMBLE ne se vérifie pas en regardant ses membres un par un.**

## Phase 17b, ce qui est fait

**La surface.** Le village ne se lit plus comme du papier millimétré.

### Les sols n'ont plus de liseré

Depuis la phase 1, chaque sol était un aplat **bordé d'un liseré d'un pixel**. Cent fois de
suite, ce liseré dessine une **grille** : le village entier se lisait comme un quadrillage. Une
pelouse n'a pas de bord tous les seize pixels.

Cinq sols sont redessinés, tous **sans bord** et donc raccordés sans couture : la pelouse et ses
touffes, la terre battue et ses grains, le pavé du parc et de l'atelier — quatre dalles de huit
pixels, joint sur le bord de la tuile pour que les dalles se poursuivent d'une case à l'autre —,
et le béton de la station. Le motif vient d'un **bruit stable** : le même (x, y) rend toujours la
même valeur, donc un diff d'image ne bouge pas sans raison.

### Les trois bâtiments ont un toit, et une enseigne

`ValidateVillage` disait « une façade se lit comme un bâtiment sur l'herbe, mais elle n'a ni toit,
ni fenêtre, ni enseigne ». Les seize **façades masquées**, sur le patron des routes et des haies,
règlent les deux premiers : une façade fait quatre cases sur deux, donc **les cases sans voisin au
nord sont la rangée du haut, donc le toit** — le masque le dit tout seul, sans que le builder ait
à savoir où commence un bâtiment. Le mur porte une fenêtre, et le contour se cerne du côté où le
bâtiment s'arrête.

Et **trois enseignes**, posées sur la case de façade juste au-dessus de chaque porte : une plaque,
un tuyau, un panneau. Rien ne disait de l'extérieur lequel était l'atelier des plaques ; il fallait
entrer pour le savoir. Le builder **refuse** de poser une enseigne si le plan ne met pas la façade
attendue au-dessus de la porte : une enseigne dans le vide ne dirait rien à personne.

### Le mur de l'enceinte

Un aplat bleu bordé d'un liseré devient des blocs de béton peint, décalés d'une assise à l'autre,
l'arête du haut éclairée : c'est elle qui donne son épaisseur au mur.

## Phase 17b, vérifications faites

- Compilation : **zéro erreur, zéro warning**. Les six validateurs aux chiffres de la phase 13,
  plus la palette (262 images, 34 couleurs) et les familles distinctes.
- **Les cinq sols regardés en 3×3**, pour juger le raccord — et c'est ce qui a montré deux
  défauts : l'éclat des dalles ne faisait **rien du tout** (écrit `= stone`, il repeignait la
  dalle de sa propre couleur), et le joint du béton dessinait une **grille noire**, exactement le
  défaut qu'on venait de retirer à l'herbe.
- **Le village regardé en jeu**, devant l'usine à panneaux et devant la station.

### Note d'atelier : retirer un défaut à un endroit le laisse ailleurs

Les 126 arbres se dressaient chacun dans une **boîte noire**. Leur tuile de sol était
`BuildTile(GrassDeep)` — un carré de vert sombre bordé d'un liseré — et tant que l'herbe autour
était elle-même un carré bordé, personne ne voyait la boîte. **L'herbe texturée l'a révélée d'un
coup, sur toute la carte.**

Le contrôle de palette l'acceptait, et il avait raison : ce sont de bonnes couleurs. Le validateur
des familles aussi : la tuile est bien distincte des autres. Corriger un défaut de style à un
endroit **le rend visible partout où il restait**, et seule une capture le dit.

## Phase 17c, ce qui est fait

**Le sous-sol.** Même traitement qu'en surface — les sols perdent leur liseré — plus deux
réponses à des questions ouvertes depuis les phases 2 et 3.

### LA PROFONDEUR SE COMPTE

C'est la décision de la sous-phase, et elle mérite d'être relue. « L'écart entre profondeur 1 et
2 » est une question ouverte depuis la phase 3 : *« distinct sur les captures, mais l'écart est
faible »*. Une nuance de brun ne se compare **qu'en voyant les deux côte à côte** — et sous terre
on voit une case à la fois.

Chaque case porte désormais **autant de cailloux clairs que sa profondeur** : un à la profondeur 1,
deux à la 2, trois à la 3, aux mêmes places d'une tuile à l'autre. Un nombre se compte sur **une
seule case**. La nuance de brun reste, elle ne remplace rien ; elle est doublée d'une quantité.

La règle de profondeur croissante EST le puzzle selon CLAUDE.md, et c'était la seule chose du jeu
que le joueur devait lire sans qu'aucun retour ne la lui dise sur place.

### L'échelle ne disparaît plus sous le joueur

Elle tenait dans une seule case : debout dessus, le joueur la **recouvrait entièrement** et le
seul chemin vers la surface s'effaçait sous ses pieds. Relevé en phase 2, laissé à l'habillage.

Elle passe au gabarit du personnage, 16 sur 24, pivot au tiers — **et ses montants passent aux
colonnes 1 et 14**. Le premier essai les avait laissés au milieu, sous le corps du joueur : au
même gabarit que lui, l'échelle était *exactement* recouverte et rien n'avait changé. Aux bords,
elle dépasse de chaque côté.

### Le reste

Terre et galeries texturées sans bord, grain plus sombre, raccord sans couture. Les 48
canalisations ont été relues sur la planche et gardées : leurs motifs se lisent, et c'est le motif
et non la teinte qui dit le type depuis la phase 9b.

## Phase 17c, vérifications faites

- Compilation : **zéro erreur, zéro warning**. Les six validateurs, la palette (262 images,
  34 couleurs) et les familles distinctes.
- **Les six sols du sous-sol regardés en 3×3**, terre et galerie aux trois profondeurs : les
  cailloux se comptent, 1, 2 puis 3 par case.
- **Le sous-sol regardé en jeu**, debout sur l'échelle — et c'est là que le premier essai a montré
  qu'il ne servait à rien.

### Note d'atelier : agrandir un sprite au gabarit de ce qui le cache ne le montre pas

L'échelle disparaissait sous le joueur. La portée au gabarit du personnage — même taille, même
pivot — l'a laissée **exactement recouverte** : deux rectangles de mêmes dimensions au même
endroit, l'un derrière l'autre. Ce qui la rend visible n'est pas sa taille mais **l'endroit où
elle dépasse** : ses montants aux colonnes 1 et 14, hors du corps du joueur qui occupe le milieu.
Le premier essai a compilé, généré, passé les deux validateurs et n'a **rien changé à l'écran**.

## Phase 17d, ce qui est fait

**Les personnages.** Sept silhouettes distinctes là où il y avait un seul corps repeint six fois.

La liste des placeholders le disait deux fois : *« l'artisan est le personnage joueur repeint en
vert »*, *« les trois ne se distinguent que par la couleur »*. Une couleur se **compare** ; une
silhouette se **reconnaît**. Chacun porte désormais quelque chose sur la tête, qui change son
contour, et quelque chose sur la poitrine, qui dit son métier.

| | ce qu'on voit |
|---|---|
| L'artisan des plaques | casquette plate à visière, une plaque ronde sur la poitrine |
| L'ouvrier des tuyaux | casque de chantier jaune, un tuyau en travers |
| Le Stock | bonnet, tablier de toile claire, une caisse sous le bras |
| La Fabrique | béret d'atelier, un crayon à la main |
| Le Plan | visière claire, lunettes, un rouleau de plans sous le bras |
| **Les huit guides** | **casquette et gilet de chantier** |

### Le guide était le boutiquier

Les huit postes de la phase 12e portaient le sprite de **l'artisan des plaques**. Un guide croisé
dans la rue ressemblait trait pour trait au commerçant qu'on va voir dans son atelier — et rien,
ni dans le code ni à l'écran, ne disait que c'étaient deux rôles.

Il a maintenant son gilet de chantier, sur un vert qui n'appartient qu'à lui : le vocabulaire de
celui qui prévient, et c'est celui des panneaux. `CharacterColors` passe à **sept couleurs**, et
le validateur exige toujours qu'elles soient toutes différentes.

## Phase 17d, vérifications faites

- Compilation : **zéro erreur, zéro warning**. Les six validateurs, la palette (263 images) et les
  familles distinctes — six personnages, sept couleurs de corps.
- **Les sept regardés côte à côte, agrandis huit fois**, deux fois : le premier passage a montré
  que Le Stock sortait **avec des cornes** et Le Plan **en masque de soudeur**.
- **Le Stock relu pixel par pixel**, ce qui a montré que son tablier avait mangé tout son corps.
- **Les trois de l'usine regardés dans leur pièce**, en jeu.

### Note d'atelier : `Shade(Brick)` est `WoodDark`, et deux vêtements deviennent le même brun

Le tablier du Stock était `Palette.Shade(body)` — le réflexe habituel pour une nuance de
vêtement — et sa caisse en `Palette.Wood`. Or `Shade(Brick)` **est** `WoodDark` : le tablier et la
caisse sont sortis du même brun, sur six rangées, et **la couleur du corps ne se voyait plus du
tout** — c'est-à-dire justement ce qui distingue les cinq de loin.

Une palette de trente-quatre couleurs fait se rencontrer des familles qui n'ont rien à voir. La
nuance d'un rouge brique **est** le brun d'une caisse. Un vêtement se colore par ce qu'il est —
un tablier est en toile claire —, jamais par une opération sur la couleur d'à côté.

### Note d'atelier : à huit fois, une rangée d'yeux ressemble à des créneaux

J'ai lu trois fois « le haut du crâne est crénelé » sur la planche agrandie, et cherché le défaut
dans le dessin des couvre-chefs. Le dump des pixels a montré des casquettes parfaitement pleines :
ce que je lisais était la **rangée des yeux**, deux blocs sombres de deux pixels dans la peau, à
sept rangées de là. **Une planche agrandie montre les défauts, elle en invente aussi** — quand
elle contredit le code, c'est le dump qui tranche, pas l'œil.

## Phase 17e, ce qui est fait

**L'interface.** Deux défauts nommés dans les placeholders, et une note périmée retirée.

### « Enlever » disait « interdit »

Le picto d'enlèvement était un **disque blanc barré** — c'est-à-dire, à seize pixels, presque un
sens interdit. Le vocabulaire de l'interdiction pour un geste qui n'interdit rien : on retire ce
qu'on a posé. Relevé dans les placeholders depuis la phase 3.

C'est désormais **un tuyau vu en bout et une flèche qui l'en sort par le haut** : le geste, pas
une défense. Et il ne peut plus se confondre avec un panneau du Code, ce qui comptait dans un jeu
où l'on en expose vingt-quatre.

### Le M était la lettre la plus grasse de la police

Trois rangées de fût central sur cinq pixels de large : plus d'encre que n'importe quelle autre
lettre, visible dans AMSTERDAM comme dans STATIONNEMENT INTERDIT. Deux rangées suffisent à faire
le creux.

### Une note périmée

*« Le picto parler est un carré blanc opaque de seize pixels »* — ce n'est plus vrai depuis
longtemps : c'est une bulle cernée, avec sa queue et ses trois points. Et la crainte qu'il cache
l'interlocuteur a été réglée autrement, en phase 9b : la bulle est passée **au-dessus de la tête
du personnage**. La ligne est retirée des placeholders.

## Phase 17e, vérifications faites

- Compilation : **zéro erreur, zéro warning**. Les six validateurs, la palette (263 images) et les
  familles distinctes.
- **La planche des pictogrammes regardée**, avant et après.

### Ce qui reste, et qui est assumé

L'apostrophe de `PixelFont` occupe toujours une cellule entière : la police est **à chasse fixe**,
et lui donner sa largeur propre veut dire passer toute la police en largeur variable — `WidthOf`,
le rendu, et les validateurs qui mesurent les noms pour vérifier qu'ils tiennent dans une rangée
du quiz. C'est une refonte, pas un réglage, et elle n'a qu'un blanc un peu large pour bénéfice.
Le dos de carte du memory et le cadre de choix restent des aplats : ils se lisent.

## Phase 17f, ce qui est fait

**Les panneaux.** Un défaut corrigé, et un autre qui n'en était pas un.

### Le STOP portait une barre, comme un sens interdit

`AB4` était un octogone rouge avec **un seul trait blanc horizontal**. À seize pixels, c'est
presque exactement `B1`, le sens interdit : un rouge, une barre blanche. Et le jeu montre **les
deux** — le stop dans les rues, le sens interdit sur la planche de l'usine.

Il porte désormais **quatre traits verticaux** de trois rangées. On ne peut pas écrire STOP en
huit pixels ; on peut écrire **qu'il y a quelque chose d'écrit**, et c'est ce qui sépare les deux
d'un coup d'œil. Vu côte à côte, très agrandi, avant et après.

### AB1 n'a pas de défaut : c'est le Code qui a deux exceptions

La phase 14 avait noté « AB1 se lit comme un panneau de danger ». En le regardant à douze fois, sa
croix de Saint-André est **nette et juste** — le dessin n'est pas en cause.

Ce qui est en cause, c'est la leçon que la pièce prétend enseigner : *« la forme dit la famille
avant que le dessin dise le détail »*. **Elle est fausse deux fois**, et pas par notre faute :
`AB1` (priorité à droite) et `AB25` (giratoire) sont des **triangles bordés de rouge** — la forme
de la famille DANGER — alors qu'ils appartiennent à INTERSECTION. C'est le Code de la route qui en
décide, et on ne le corrige pas en redessinant.

La rangée du haut de la planche contient donc deux triangles qui ressemblent à ceux de la rangée
d'en dessous. **C'est une propriété du sujet, pas un placeholder** ; la ligne est reformulée dans
les placeholders plutôt que laissée comme un défaut à réparer.

## Phase 17f, vérifications faites

- Compilation : **zéro erreur, zéro warning**. Les six validateurs, la palette et les familles
  distinctes — dont les 29 panneaux, toujours deux à deux différents.
- **Les vingt-neuf regardés sur une planche à sept fois**, puis les cinq qui se ressemblent
  — stop, sens interdit, circulation interdite, AB1, AB25 — **à douze fois, côte à côte**.

## Phase 17g, ce qui est fait

**Les intérieurs**, et la fin de l'habillage.

### Un mur d'atelier, et trois meubles

Le mur des pièces était un aplat de bois bordé d'un liseré. Ce sont désormais des **planches
verticales** avec leur grain et deux lisses horizontales qui courent d'une case à l'autre sans se
couper.

Et trois meubles par pièce — **un établi, un râtelier d'outils, une pile de caisses** — pour que
les salles cessent d'être *« propres, mais pas encore un lieu »*.

**Ils se posent sur des cases de MUR, jamais sur le sol**, et c'est ce qui rend cet ajout gratuit :
une case de mur ne se traverse déjà pas, donc `IsWalkable` ne bouge pas, `ValidateRooms` n'a aucun
marqueur de plus à connaître, et rien de ce que le joueur peut faire ne change. Un meuble posé sur
le sol aurait demandé un marqueur dans les trois plans, une règle de blocage et une case retirée à
chaque pièce. Le builder refuse en nommant la case si le plan n'y met pas de mur.

## Phase 17g, vérifications faites

- Compilation : **zéro erreur, zéro warning**. Les six validateurs, la palette (266 images,
  34 couleurs) et les familles distinctes.
- **La pièce de l'usine à panneaux regardée en jeu**, deux fois — et c'est le premier passage qui
  a montré la faute.

### Note d'atelier : je suis retombé dans le piège de la phase 13

Les trois meubles étaient d'abord adossés au **mur du fond**, qui est leur place naturelle dans
une vue de trois quarts. La rangée de gouttes du HUD les a recouverts aux deux tiers : sur trois
meubles, **un seul se devinait**.

C'est exactement ce que la phase 13 avait trouvé et écrit — *« une pièce d'intérieur fait dix
lignes quand la caméra en montre 11,25, donc sa rangée du haut tombe derrière la rangée de gouttes
du HUD »* — et c'est écrit dans PIEGES.md depuis. **Je l'ai relu au début de la session et je l'ai
quand même refait**, parce que « adosser un meuble au mur du fond » est un réflexe de dessin et
non une décision qu'on prend en consultant une liste. Les meubles sont au mur du bas.

## Phase 18a, ce qui est fait

Le 5 septembre 2026, six captures d'un RPG de console portable du début des années 2000 servent de
**référence visuelle** : le jeu doit leur ressembler, et la phase 17 n'y est pas. Diagnostic,
feuille de style et découpage dans **PLAN-PHASE-18.md**. Les captures sont une feuille de style,
pas une banque d'images : aucun pixel n'en est copié, la ROM n'a pas été ouverte.

- **Le diagnostic** : la phase 17 a travaillé la *couleur* de chaque élément, jamais sa *forme*.
  Herbe en bruit là où la référence a une trame régulière ; chemins à angles carrés là où elle
  arrondit chaque coin ; arbres d'une case sans contour là où elle a des frondaisons de deux
  cases ; maisons de 16 px là où elle a un toit en plan sur la moitié de la hauteur ;
  personnages sans contour à tête d'un tiers ; HUD à nu là où tout est dans une boîte blanche.
- **La feuille de style**, huit règles : sols sans contour à trame régulière ; un trait d'un pixel
  autour de tout ce qui se dresse, dans le sombre de sa famille ; trois tons par matière ; vue
  3/4 ; une ombre au sol ; des coins arrondis ; un personnage est d'abord une tête ; palette pastel.
- **Seize couleurs** ajoutées à `Palette`, par familles nommées : `Lawn` ×4 (la pelouse menthe),
  `Leaf` ×4 (le feuillage, dont `LeafShadow`, le contour des plantes), `Sand` ×2, `Roof` ×2,
  `WoodLight`, `SkinShadow`, `Rust`, `FlowerPink`. 34 → 50. Toutes ont leur `Shade`. Les verts et
  l'ocre de la phase 17 restent le temps de la transition ; 18h retirera ce que plus rien ne porte.
- **Les primitives du style**, dans `PlaceholderArtGenerator.Style.cs` (le générateur devient
  `partial`) : `Outline` — le trait posé dans le vide autour d'une silhouette, donc on dessine en
  retrait d'un pixel —, `RoundedBox`, `Plot` borné, `Tuft`, `LeafBlob` (la texture du feuillage,
  période 8 pour se poursuivre de case en case), `CornerDistance` (le quart de cercle d'un coin de
  chemin). Et les premiers dessins : pelouse, chemin à seize masques, eau, arbre 16×32, pied
  d'arbre, buisson à seize masques, fleurs, maison 32×40, poteau, personnage, goutte, boîte,
  pictos soleil et printemps.
- **La planche d'essai**, `Sous La Ville/Planche d'essai 18` → `Captures/planche_essai_18.png` :
  onze cases sur huit composées avec ces dessins, à quatre fois, **sans toucher à une image du
  jeu**. C'est elle qui se valide.

### Ce que la planche a dit, en trois passes

1. **Le bouquet d'arbres était sous la boîte du HUD.** Première composition : les arbres — ce que
   la planche devait montrer d'abord — en haut à gauche, exactement sous la boîte de couche et de
   saison. Le piège de la rangée du haut, une troisième fois, dans une planche que j'avais
   composée moi-même. Recomposée : rien sous les deux boîtes.
2. **Les arbres sortaient en cyprès.** Une cime de quatorze pixels de large sur vingt-deux de
   haut est un ovale debout, quelle que soit sa texture, et la texture en diagonales lisait comme
   un tricot. Redessinée en boule à deux lobes et trois bosses, texturée de touffes en quinconce.
3. **Les buissons sortaient en pile de boîtes rayées** : une bande d'ombre par case, répétée tous
   les seize pixels. L'ombre n'est plus qu'au pied de la haie et la lumière qu'à son sommet ;
   entre les deux, la texture du feuillage, qui se poursuit de case en case parce que sa période
   divise seize.

## Phase 18a, vérifications faites

- Compilation par le pont MCP : zéro erreur, zéro avertissement — deux champs `static readonly`
  déclarés pour 18c et 18d ont été retirés avant compilation, un champ privé jamais lu étant un
  CS0414.
- La planche regardée trois fois, corrigée deux fois. `git diff ProjectSettings/` vide.
- Aucune image du jeu n'a changé : `ValidatePalette` et `ValidateDistinct` n'ont rien de neuf à
  lire. Les seize couleurs n'y changent rien, un ajout ne peut pas mettre une image hors palette.

## Phase 18b, ce qui est fait

Planche validée et trois décisions prises le 6 septembre 2026, toutes dans le sens recommandé :
arbres de deux cases, maisons de deux sur deux, saisons par images.

- **Les cinq sols** redessinés selon la règle 1 : la pelouse (`BuildLawnTile`), le chemin à seize
  masques aux **coins arrondis en quart de cercle** (`BuildSandTile`, plus de pointillé central),
  les dalles du parc et le béton de la station (`BuildSlabTile`, une trame et deux fissures en L,
  sans joint), l'eau (`BuildWater`). Et **le pied d'arbre** passe à la pelouse neuve tout de suite —
  sinon les 126 arbres se dressaient dans des carrés de vieux vert, le piège exact de 17b.
- **La carte du village** prend les couleurs du monde : menthe, sable, feuillage.
- **Le tri par Y.** `RendererSetup` pose `TransparencySortMode.CustomAxis` (0, 1, 0) dans
  `Assets/Settings/Renderer2D.asset` — pas dans les ProjectSettings —, `BuildAllScenes` le pose et
  le vérifie, et refuse sinon. Deux aides dans `SceneBuilderUtility` : `ApplyStandingSort` (ordre 0,
  point de tri **au pivot**, posé au centre de la case) pour tout ce qui se dresse — arbres,
  fontaine, guides, habitants, meubles, enseignes, joueur — et `ApplyGroundMarkSort` (ordre −1)
  pour tout ce qu'on foule — bouches, portes, échelles, panneaux, cuves, échantillons, curseur.
  Sans le −1, le joueur debout sur une bouche serait à égalité de Y avec elle. `HouseSpawner`
  pose le point de tri au pivot lui aussi.
- Suppression de `BuildGrassTile`, `BuildDirtTile`, `BuildConcreteTile`, `BuildRoad`, et des anciens
  `BuildTreeBase` et `BuildWater` : les remplacés ne restent pas.

## Phase 18b, vérifications faites

- Compilation : zéro erreur, zéro avertissement.
- Art régénéré : 266 textures, 115 tuiles ; **palette tenue, 50 couleurs** ; familles distinctes.
- Cinq scènes construites ; « Tri par Y activé dans le Renderer2D : axe (0, 1, 0) ».
- **Sabotage** : `m_TransparencySortMode` remis à 0 par script → `IsYSortEnabled()` rend faux ;
  `EnableYSort()` le répare et il rend vrai. Le filet lit le fichier, pas une supposition.
- **Quatre captures en jeu** par un pilote de téléportation (`Assets/Editor/Pilot18.cs`, non
  commité, supprimé en 18h) : départ, routes, parc, station. Regardées. Le tri par Y se voit : le
  guide au nord d'un arbre passe **derrière** la cime, jambes couvertes.
- `git diff ProjectSettings/` vide ; `Application.runInBackground` posé à chaud par le pilote.

### Note d'atelier : un pilote armé qui ne tourne jamais

Premier lancement : play en cours, `runInBackground` faux, aucune capture, aucun log. Le pilote
s'accrochait à `EditorApplication.update` dans un `[InitializeOnLoadMethod]`, en comptant sur le
rechargement de domaine de l'entrée en play — qui **n'a pas eu lieu**. Il s'accroche désormais
aussi dans la commande de menu. Consigné dans PIEGES.md.

## Phase 18c, ce qui est fait

La végétation de la référence, règles 2, 3, 5 et 6 de la feuille de style.

- **L'arbre fait deux cases**, `tree.png` 16×32, pivot au quart (`TreePivot`, le centre des seize
  pixels du bas) : le tronc dans sa case, la cime sur toute la case du nord, et le tri par Y de 18b
  fait le reste. Les 126 arbres se posent sans qu'une ligne du builder change. La cime est **une
  boule qui se resserre sur le tronc** : la masse, deux lobes au milieu, le bas qui pend, trois
  bosses au sommet ; un croissant d'ombre au bas et au flanc droit, un éclat en haut à gauche, des
  arcs de feuillage clairs et sombres ; tronc cerné de brun, cime de vert profond. Son ombre au
  sol est dans la tuile de son pied (18b).
- **Les buissons du labyrinthe**, seize masques (`BuildBushTile`) : retrait d'un pixel et angle
  arrondi sur chaque côté libre, ombre au pied et lumière au sommet seulement aux bouts de la
  haie, **sommet festonné** (deux pixels sur huit rognés sur un dessus libre, le trait suit le
  creux), et la texture `LeafTexture` — un arc clair et un arc sombre par carré de huit, en
  quinconce, période qui divise seize donc sans couture de case en case. La tuile `tile_hedge`
  sans masque est le buisson fermé de toutes parts.
- **Les fleurs**, `tile_flowers.png` / `Tile_Flowers` : la pelouse et deux fleurs roses à cœur
  jaune, cernées. Une tuile de **sol**, semée par `SurfaceSceneBuilder.IsFlowerCell` sur une case
  d'herbe libre sur vingt, par un hachage stable des coordonnées — jamais sous un panneau. C'est la
  tuile que 18g fera changer avec la saison d'un seul `SwapTile`. En attendant, elle fleurit toute
  l'année : placeholder assumé.
- **Un filet neuf dans `ValidateDecor`** : rien de ce qu'on doit voir ou toucher — guide, bouche,
  porte, maison, fontaine, entrée de station, départ — juste au nord d'un arbre, dont la cime le
  couvrirait. Une rue peut y passer : marcher derrière un arbre est le jeu.
- **Un arbre a reculé** : celui de (12, 15) mangeait le corps du guide du but en (12, 16), vu sur
  la capture. Il est en (10, 15). Le plan ne change que là ; 126 arbres, 2233 cases praticables,
  32 panneaux, inchangés.
- **Les panneaux de rue passent au tri « ce qui se dresse »** (ordre 0, par Y) : à l'ordre −1 de
  18b, un panneau au sud d'un arbre passait derrière son tronc, vu sur la capture de la pelouse.
  Sa plaque monte de vingt-quatre pixels ; ce n'est pas une chose qu'on foule. Le joueur debout sur
  sa case est alors à égalité de Y avec lui : ordre indéfini l'espace d'un pas, assumé.
- Suppression de `BuildTree`, `BuildHedge` et `LeafBlob` : les remplacés ne restent pas.

### Ce que les planches ont dit

Trois passes sur la cime, chacune regardée à six fois avant d'être crue. La cime de 18a — un
ovale de quatorze sur vingt et une texture en pois — sortait **en capsule** une fois posée dans le
jeu : côtés droits, sommet plat, presque pas de modelé. Un prototype Python avec la palette exacte
a permis d'itérer en secondes : d'abord le modelé en croissant, qui donnait du volume mais gardait
le cornichon ; puis **le bas qui se resserre** sur le tronc et les lobes du milieu, et l'arbre
s'est lu. Porté en C# à l'identique, planche des images du jeu relue : la même.

## Phase 18c, vérifications faites

- Compilation : zéro erreur, zéro avertissement, trois fois (le dessin, le filet, le sabotage).
- Art régénéré : **267 textures, 116 tuiles** ; palette tenue, 50 couleurs ; familles distinctes.
- Cinq scènes construites ; « 126 arbre(s), 32 panneau(x) dérivé(s) ».
- **Sabotage** : l'arbre remis en (12, 15) sous le guide → « L'arbre (12, 15) a « V » juste au
  nord : sa cime de deux cases le cacherait », et la scène Surface **n'a pas été écrite**. Réparé,
  recompilé, reconstruit.
- **Captures en jeu regardées** : le bosquet de trois sur deux fait un mur de cimes rondes, les
  troncs du rang sud passent devant ; le labyrinthe est une haie festonnée à bouts ronds et non
  une pile de boîtes ; les fleurs par paires sur la pelouse ; le guide du but dégagé.
- `git diff ProjectSettings/` vide.

### Note d'atelier : `BuildHedge` n'avait pas de résumé

En coupant « du résumé de `BuildHedge` à la fin de `BuildTree` », la coupe est remontée au résumé
précédent et a emporté `BuildPlantBasin`, deux méthodes plus haut. Un CS0103 l'a dit à la
compilation suivante ; remis à sa place. Une suppression par script se relit dans le diff avant de
compiler, ligne à ligne, et pas seulement par le nom de ce qu'on voulait supprimer.

## Phase 18d, ce qui est fait

Les bâtiments de la référence, règles 2, 3, 4 et 6 : un toit en plan sur la moitié de la hauteur,
trois tons par matière, un trait autour de tout ce qui se dresse, des angles de toit arrondis.

- **La maison fait deux cases sur deux**, `house.png` 32×40 (`BuildHouseV2`), pivot `HousePivot`
  (0,5 ; 8/40) : posée à un demi-carreau à l'est du centre de sa case d'ancrage, elle couvre ses
  deux cases du bas et se trie par Y sur cette rangée. Bardage clair à lignes, deux fenêtres à
  carreaux sur leur appui, porte encadrée, plinthe, avant-toit et son ombre, toit à planches et
  versants clairs, faîte, angles du haut arrondis, trait d'Ink.
- **Le plan reçoit les maisons** : un marqueur `a`, corps de maison — bloquant, sans raccordement,
  herbe dessous. La case `A` reste la case de raccordement, **n'importe où dans le carré** : pour
  huit maisons sur douze la rue passe juste au nord de `A`, donc `A` est la rangée du toit et les
  murs descendent au sud. `VillageLayout.HouseAnchor` trouve le carré ; `ValidateHouses` exige
  pour chaque `A` un carré de trois `a` et un seul, aucun `a` orphelin, et une case praticable
  devant la porte. Trente-six `a` écrits à la main par indice de ligne. Le village passe de 2233 à
  **2197 cases praticables** ; 126 arbres, 32 panneaux, inchangés.
- **Trois déplacements pour loger les carrés** : le guide du but de (12, 16) à (11, 16) — table
  des leçons mise à jour —, l'arbre (53, 17) en (54, 17), l'arbre (58, 42) en (55, 42).
- **Une maison remonte** : (10, 34) était coincée entre les rues y = 33 et y = 35, aucun carré
  possible. Elle est en `A` = (10, 36), carré (10..11, 36..37), porte sur la rue y = 35. **Son
  alcôve suit au sous-sol** : `A` en (10, 36), la galerie (10, 35) creusée depuis la chambre de
  l'échelle (9, 33). Profondeur 2 des deux côtés, aucune crête traversée : « 14 destinations
  atteignables sur 14 », bilan de l'eau inchangé. Une sauvegarde antérieure qui raccordait
  (10, 34) perdrait ce raccord ; il n'y en a pas à garder.
- `HouseSpawner` reçoit les **ancres** en plus des cases de raccordement, et pose la goutte à 2,6
  unités, au-dessus du faîte. Sans ancre cuite il retombe sur la case et le dit.
- **Le symbole de maison du mini-jeu Le Plan**, `house_icon.png` 16×24 (`BuildHouseIcon`) : la
  carte du plan a des cases de seize pixels, la maison de deux sur deux n'y tient pas ; c'est un
  symbole sur une carte, même grammaire en petit.
- **Les trois façades** (`BuildFacadeV2`, seize masques) : rangée du toit — planches, versants
  clairs aux bouts libres, faîte, égout dans l'ombre — et rangée du mur — bardage, fenêtre à
  carreaux, plinthe, avant-toit. Les côtés libres se cernent d'Ink par le même `Outline` que tout
  le reste, appliqué aux seuls bords où le bâtiment s'arrête ; seuls les angles du **toit**
  s'arrondissent, la base d'un bâtiment est d'équerre.
- **Les enseignes** (`BuildSignboardV2`) : un panonceau de bois arrondi à trois tons, cerné.
- **La station** : le mur de l'enceinte en blocs de béton à trois tons (`BuildPlantWallV2`, plus
  le bleu de piscine de 17b) ; **l'entrée a son image**, `plant_inlet.png`, une grille sur le puits
  — elle empruntait celle du mur depuis la phase 1 ; les cuves sont des bassins ronds au bord
  éclairé (`BuildPlantBasinV2`).
- **La bouche** (`BuildManholeV2`) : disque de plaque, jonc d'acier éclairé vers la lumière par
  `LightRim`, deux fentes, trait. **La porte** (`BuildDoorV2`) : vantail à trois tons, poignée,
  trait. **La fontaine** (`BuildFountainSpriteV2`) : bassin de pierre, colonne, vasque, jet ; son
  pied est la dalle du parc et l'ombre du bassin.
- **Le poteau** : `DrawSignPost` — fût à deux tons, pied, trait d'Ink — dessiné **avant** la
  plaque dans `BuildSign`, le poteau vide et le dos de carte. Les vingt-neuf plaques ne changent
  pas : elles sont le Code.
- Suppression de `BuildHouse`, `BuildFacade`, `BuildSignboard`, `BuildPlantWallTile`,
  `BuildManhole`, `BuildPlantBasin`, `BuildFountainSprite`, `BuildFountainBase`, `BuildFountain`,
  `BuildDoor`.

## Phase 18d, vérifications faites

- Compilation : zéro erreur, zéro avertissement, quatre fois (dessins et plan, sabotage, réparation,
  pilote).
- Art régénéré : **269 textures, 116 tuiles** ; palette tenue, 50 couleurs ; familles distinctes.
- Planche des bâtiments regardée à cinq fois (`Captures/planche_18d_batiments.png`) : une reprise,
  les angles du bas des façades remis d'équerre.
- Cinq scènes construites : « 2197 cases praticables, toutes reliées », « 14 destinations
  atteignables sur 14 », bilan de l'eau tenable.
- **Sabotage** : le `a` de (42, 26) retiré → « La maison (43, 25) n'a pas son carré de deux sur
  deux » et « La case (42, 25) est un corps de maison qui n'appartient à aucune maison », scène
  Surface **non écrite**. Réparé, recompilé, reconstruit.
- **Captures en jeu regardées** : le départ — la maison (12..13, 16..17) entre le guide et le
  joueur, la goutte au-dessus du faîte —, l'atelier et l'usine à tuyaux avec toit, enseigne et
  porte sur la rue, la station en béton avec sa grille, la maison remontée porte sur la rue y = 35.
  **La capture de la fontaine ne montre rien** : le pilote a téléporté le joueur SUR la case de la
  fontaine, qui bloque, et il la cache. Vue sur la planche ; à reprendre en jeu au pilote de 18e.
- `git diff ProjectSettings/` vide.

## Phase 18e, ce qui est fait

Les personnages de la référence, règle 7 : **un personnage, c'est une tête** — la moitié des
vingt-quatre pixels, les cheveux un tiers de la tête avec une mèche claire, des yeux d'un pixel sur
deux, une ombre sous le menton, un petit corps, deux bras, deux jambes, des chaussures, le tout
cerné d'Ink.

- **Le joueur**, `BuildPlayerV2`, quatre directions sur `DrawFigure` : de dos la nuque, de côté un
  œil et la frange qui retombe. Corps `CharacterColors[0]`, pantalon bleu profond, cheveux bruns.
- **Les six métiers**, `BuildVillagerV2(body, trade)` : la figure de face dans sa couleur, puis la
  silhouette de 17d redessinée aux nouvelles rangées (chaussures 1..2, jambes 3..5, torse 6..12,
  visage 13..17, cheveux 18..22). L'artisan des plaques : casquette plate et plaque sur la
  poitrine. L'ouvrier : casque jaune à crête, tuyau en travers. Le Stock : bonnet à pompon, tablier
  clair, caisse sous le bras. La Fabrique : béret qui déborde, crayon. Le Plan : visière, petites
  lunettes, rouleau de plans. Le guide : casquette et gilet à deux bandes. Le trait vient en
  dernier, accessoires compris.
- **Les casquettes sont en gris ardoise**, pas en anthracite : les cheveux le sont déjà, et sur la
  première planche l'artisan et le guide semblaient nu-tête. Vu à six fois, corrigé, revu à huit.
- Suppression de `BuildPlayer` et `BuildVillager`. Sept couleurs de corps, `ValidateDistinct`
  toujours vrai.

## Phase 18e, vérifications faites

- Compilation : zéro erreur, zéro avertissement, trois fois.
- Art régénéré : 269 textures, 116 tuiles ; palette tenue, 50 couleurs ; « 6 personnages, 7
  couleurs de corps — aucune paire identique ».
- Planche des personnages regardée à six puis huit fois (`Captures/planche_18e_personnages.png`).
- Aucune scène à reconstruire : les sprites gardent leur chemin et leur pivot.
- **Captures en jeu regardées** : le départ — le joueur devant la maison, le guide du but à sa
  gauche, celui de la bouche plus bas, gilets et casquettes lisibles — et **la fontaine**, reprise
  de 18d depuis la case voisine : bassin, colonne, vasque et jet cernés ; le joueur au sud passe
  devant son bassin, comme il doit.
- `git diff ProjectSettings/` vide.

### Ce qui reste à l'œil

La fontaine fait une case de haut au milieu d'un labyrinthe de haies : elle est petite pour ce
qu'elle est. Deux cases de haut, comme l'arbre, lui rendraient sa place — hors plan de la phase 18,
à trancher.

## L'habillage de la phase 17 est terminé — et il ne convient pas

Les sept sous-phases de la phase 17 sont faites : la palette et la méthode, la surface, le
sous-sol, les personnages, l'interface, les panneaux, les intérieurs. **266 images, 34 couleurs**,
deux validateurs qui barrent la construction, et une planche qu'on regarde.

Le jeu est complet de la phase 1 à la phase 17 : le réseau, les saisons, les bâtiments, l'usine à
panneaux et ses trois mini-jeux, et l'art. **Reste à le faire jouer par Victorien** — décision du
2 septembre 2026 : il ne joue qu'à la fin.

## Les documents du projet

- **CLAUDE.md** — les contraintes non négociables. Ne se discute pas.
- **PROGRESS.md** — ce fichier. Le journal : ce qui est fait, les décisions et leur pourquoi,
  les placeholders, les questions ouvertes.
- **PIEGES.md** — les pièges tombés au moins une fois, rassemblés. À relire avant d'écrire.
- **PLAN-PHASE-NN.md** — le plan de chaque phase, validé avant implémentation.
- **AUDIT-PHASE-12.md** — l'audit d'impact de l'agrandissement de la carte, 4 septembre 2026.
  Huit dimensions, chacune re-vérifiée adversarialement. **Il ne se refera pas** : les treize
  pannes silencieuses, les chiffrages et l'architecture des guides n'existent que là.

## Prochaine étape : la phase 18f, l'interface

Les boîtes blanches arrondies du HUD, les gouttes, les pictos de saison, de couche et d'action, le
cartel, la boîte de dialogue, le fond et les cadres des trois mini-jeux. Puis 18g et 18h. Victorien
joue après.

## Décisions prises

### Phase 18a, tranchées le 5 septembre 2026

- **Les captures de référence sont une feuille de style, pas des assets.** Proportions, trames,
  rampes, perspective ; aucun pixel copié. CLAUDE.md tient, la ROM reste fermée.
- **La palette passe de 34 à 50 couleurs**, par rampes de quatre. Revient sur « une trentaine »
  de 17a : la référence demande contour, ombre, base et éclat pour chaque matière qui se dresse,
  et une pelouse menthe qui n'est aucun des trois verts de 17.
- **La pelouse s'appelle `Lawn`, pas `Grass`** : les trois `Grass` de la phase 17 restent le temps
  de la transition, et deux familles du même nom se seraient confondues dans quatre mille lignes.
- **Le pointillé central des routes disparaît** en 18b : la référence n'en a pas, ce sont des rues
  de village, et ce sont les panneaux qui disent « route ».
- **Les pictos du HUD font 24 px dans une boîte de 32** : l'emprise de la phase 5 ne change pas,
  c'est le cadre qui prend la marge.
- **Tranchées le 6 septembre 2026, dans le sens recommandé** : arbres de deux cases, maisons de
  deux sur deux, saisons par images.
- **Phase 18b.** Ce qu'on foule est à l'ordre −1, ce qui se dresse à 0 et se trie par Y au pivot.
  Le réglage vit dans l'asset du Renderer2D, versionné sous `Assets/Settings`.
- **Phase 18d.** La case `A` reste la case de raccordement mais peut être n'importe laquelle des
  quatre du carré : la rue passe souvent juste au nord d'une maison, et son toit se tourne alors
  vers la rue. Une maison qui n'a aucun carré possible **déménage avec son alcôve** plutôt que de
  devenir une exception d'une case. Le plan du mini-jeu garde un symbole de maison de seize pixels.
- **Phase 18c.** Les fleurs sont une tuile de sol semée par hachage, une case d'herbe libre sur
  vingt : c'est la tuile que les saisons échangeront. Rien de ce qu'on doit voir ne se tient juste
  au nord d'un arbre ; le plan bouge d'un arbre pour cela.

### Phase 17c, tranchée le 5 septembre 2026

- **La profondeur se compte.** Chaque case de terre ou de galerie porte autant de cailloux clairs
  que sa profondeur. Réponse à la question ouverte depuis la phase 3 sur l'écart entre les
  profondeurs 1 et 2 : une nuance ne se compare qu'en voyant deux cases ensemble, un nombre se
  lit sur une seule. **À rouvrir si tu préfères t'en tenir à la nuance seule.**

### Phase 17, tranchées le 5 septembre 2026

- **Art original dessiné par code.** Aucun fichier externe, aucun asset Nintendo ou Pokémon : la
  ROM de Pokémon Rubis proposée ce jour-là n'a pas été ouverte, CLAUDE.md l'interdit. L'inspiration
  de style se prend sans copier un pixel.
- **Une palette de trente-quatre couleurs nommées**, et un validateur qui refuse tout pixel hors
  palette. La cohérence devient vérifiable, pas seulement souhaitée.
- **L'eau reste immobile**, mieux dessinée : pas de composant, pas d'horloge.
- **Sept sous-phases**, 17a à 17g, dans l'ordre de ce que Victorien voit en premier.

### Phase 16, tranchées le 5 septembre 2026

- **La règle du jeu est le Code, tel que `RoadSigns` l'applique.** Postes et panneaux attendus
  dérivés, jamais écrits ; palette des cinq panneaux de rue. La variante « solutions à la main,
  palette des 24 » ne pouvait pas être vérifiée.
- **Le plan vérifie quand tous les poteaux sont garnis**, au geste suivant, et non poste par
  poste : cinq panneaux possibles, un retour immédiat permettrait de les essayer sans raisonner.
- **Le rang du plan vit dans le composant**, rien sur le disque : la décision du 3 septembre
  tient sans exception. Retour au premier après le quinzième.
- **Espace fait défiler le panneau du poteau**, aucune palette : une seule touche, aucun second
  niveau de choix.
- **La route prioritaire se dessine avec `=` et se teinte à l'écran.**
- **Une rue qui touche le bord continue** (`IsExit`), règle propre aux plans.
- **`RoadSignRules` derrière `IRoadGrid`**, le village ne fait que se décrire.

### Phase 15, tranchées le 5 septembre 2026

- **Huit questions par lancement, deux par famille**, le panneau agrandi trois fois à gauche et
  trois noms à droite sur des rangées de 32 px.
- **Les leurres se resserrent** : 0, 1 puis 2 faux noms de la même famille. La grammaire des
  formes d'abord, la lecture du pictogramme ensuite.
- **Un nom faux s'éteint et on rechoisit**, jamais de révélation : la variante « le bon nom se
  révèle à la faute » permettait de cliquer au hasard sans jamais lire.
- **Le tirage par famille est partagé** (`SignDraw`), le memory doit ressortir identique.
- **Aucune image neuve.**

### Phase 14, tranchées le 5 septembre 2026

- **Le memory apparie un panneau AVEC LE MÊME PANNEAU**, et le nom de la paire s'affiche à la
  trouvaille. Le panneau contre son nom est mesuré impossible : il diviserait le jeu par deux et
  demanderait un rendu multi-ligne, et c'est déjà le sujet de La Fabrique.
- **8, puis 12, puis 16 paires**, en 4 × 4, 4 × 6, 4 × 8, cartes de 32 px. Toutes divisibles par
  quatre : **2, 3 puis 4 panneaux par famille**, jamais un tirage global.
- **Une manche par lancement**, plafonnée à 16 paires, remise à zéro à la fermeture du jeu. On
  sort après chaque manche : le jeu n'a qu'Espace, et un écran dont on ne peut pas sortir serait
  un piège.
- **Rien ne se sauvegarde**, conformément à la décision du 3 septembre. Le compteur de manches
  est un champ du composant.
- **Le mini-jeu est un écran modal**, sur le patron de `VillageMapScreen`. Dans la pièce, une
  carte ferait 16 px — sous le plancher de 32 —, les 24 cases de panneaux sont déjà prises,
  chaque tour demanderait deux traversées du plateau, et il faudrait construire le plateau à
  chaud dans une scène que le builder Editor écrit. Et les phases 15 et 16 sont aussi des écrans.
- **Aucun `InteractionKind` neuf** : Espace fait déjà parler, et parler mène au jeu. Un kind de
  plus aurait demandé un second geste pour la même chose.
- **`MiniGameKind` est un enum sérialisé** : tout ajout se fait à la fin, comme `GameLayer` et
  `NodeType`. La Fabrique et Le Plan y sont déjà, sans écran derrière eux.
- **Le fond de l'écran est opaque**, pas un voile : la rangée de gouttes du HUD passe exactement
  où passe la rangée haute du plateau.
- **Aucune minuterie** : deux cartes dépareillées restent visibles jusqu'au geste suivant.

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

- ~~**Le STOP à seize pixels ressemble à un sens interdit**~~ **Fait en phase 17f** : quatre
  traits verticaux au lieu d'une barre horizontale. On ne peut pas écrire STOP en huit pixels ;
  on peut écrire qu'il y a quelque chose d'écrit.
- **Le poteau vide de la phase 16** : un poteau et un pointillé. Il se lit, c'est un aplat.

- **AB1 et AB25 sont des triangles dans la famille INTERSECTION**, et ce n'est PAS un
  placeholder. Vérifié en phase 17f à douze fois : la croix de Saint-André d'AB1 est nette et
  juste. C'est le **Code de la route** qui met deux triangles bordés de rouge — la forme de la
  famille DANGER — dans la famille INTERSECTION. La leçon de la pièce, « la forme dit la famille
  avant que le dessin dise le détail », a donc **deux exceptions**, et aucun dessin ne les
  supprimera. À dire à Victorien plutôt qu'à corriger.
- **Le dos d'un panneau et le cadre de carte de la phase 14** : une plaque grise à bride et deux
  boulons, et quatre équerres jaune pâle. Les deux se lisent, les deux sont des aplats.

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
- ~~**Le picto « parler » est un carré blanc opaque de seize pixels.**~~ **Périmé, retiré en
  phase 17e** : c'est une bulle cernée, avec sa queue et ses trois points, et la crainte qu'elle
  cache l'interlocuteur a été réglée en phase 9b en la passant au-dessus de SA tête.
- **Les cinq PNG de la phase 8**, la cuve du bassin : cadre gris, intérieur sombre, eau qui
  monte. Lisible, mais c'est une boîte ; un vrai bassin d'orage vu de dessus reste à dessiner.
- **Les dix-huit PNG de la phase 7** : les huit plaques, les huit noms, le plan du village et
  le pavé de l'atelier.
- **La police de 5 sur 7 pixels** se lit **au HUD**, sur fond uni, et ne se lisait pas dans le
  décor : c'est ce qui a fait passer les noms au cartel le 4 septembre 2026. Quelques lettres
  restent grasses à cette taille, le M et le B surtout. À reprendre à l'habillage.
- ~~**Les pièces sont vides**~~ **Fait en phase 17g** : un établi, un râtelier d'outils et une
  pile de caisses par pièce, posés sur des cases de mur pour ne rien retirer au jeu.
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
- ~~**Le picto « enlever » est un disque barré**~~ **Fait en phase 17e** : un tuyau vu en bout et
  une flèche qui l'en sort. Le geste, pas une défense.
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
