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
| 4 | L'eau coule | À faire |
| 5 | Les saisons et le gel | À faire |
| 6 | Sauvegarde | À faire |
| 7 | La plaque gravable | À faire |
| 8 | La réserve d'eau | À faire |
| 9 | L'usine à tuyaux | À faire |
| 10 | Les fuites | À faire |
| 11 | Le parc | À faire |
| 12 | Habillage | À faire |

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

## Prochaine étape, phase 4

L'eau coule, et les maisons arrivent avec elle. Le terrain est prêt :

- **`PipeNetwork.Changed`** est le seul signal dont le `FlowSolver` a besoin. Il ne tournera
  que là-dessus et aux ticks de saison, jamais par frame.
- **`PipeNode.Depth`** est déjà rempli à la pose, et la station est le seul point à la
  profondeur 3 : la règle de profondeur croissante se vérifie telle quelle.
- **`HouseSpawner` et le type `HouseConnection`** restent à écrire. Les maisons se poseront en
  surface et déclareront un nœud permanent sous elles, comme la station le fait déjà via
  `fixedNodes`.
- **La phase 6, la sauvegarde, devient prioritaire juste après.** C'est depuis la phase 3 le
  premier moment où le joueur perd du travail en relançant.

## Décisions prises

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

- **Lisibilité des trois nuances de profondeur.** Le brun s'assombrit et la galerie vire au
  gris froid au plus profond. Distinct sur les captures, mais l'écart entre profondeur 1 et 2
  est le plus faible des deux : à juger plein écran avant de figer.

- **Intensité de la lumière du sous-sol et contraste terre / galerie.** 0,8 et deux bruns
  distincts sur les captures ; à juger en vrai, plein écran, avant de figer.

- **`companyName` reste `DefaultCompany`.** Les sauvegardes de la phase 6 iront donc dans
  `~/Library/Application Support/DefaultCompany/SousLaVille`. À trancher avant la phase 6 :
  le changer après coup déplacerait les parties existantes de Victorien.
- **`Assets/Settings/InputSystem_Actions.inputactions`**, l'asset d'input par défaut d'Unity,
  est conservé intact car les ProjectSettings le référencent comme *project-wide actions*.
  Il n'est pas utilisé par le jeu. Ménage possible plus tard.
- **Manette.** Des bindings `<Gamepad>/dpad` et `<Gamepad>/buttonSouth` sont posés. Support
  optionnel, jamais requis, conformément à CLAUDE.md. À retirer si tu préfères le clavier seul.
