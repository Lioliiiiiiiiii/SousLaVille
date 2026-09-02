# Sous la Ville, journal de bord

Unity 6000.5.10f1, URP 17.6.0 (Renderer2D), Input System 1.20, Newtonsoft 3.2.2.
Aucun package supplémentaire n'a été ajouté au projet.

## État par phase

| Phase | Titre | État |
|---|---|---|
| 0 | Fondations | Terminée |
| 1 | Le personnage et la surface | Terminée |
| 2 | Le portail | À faire |
| 3 | Creuser et poser | À faire |
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

## Prochaine étape, phase 2

Le portail. Les objets `Manhole_01` à `Manhole_03` et `TreatmentPlant` existent déjà en
scène : il n'y aura qu'à leur ajouter `ManholePortal`, sans retoucher la carte.

Deux points à traiter en phase 2, déjà repérés :

- **Le Sorting Layer du personnage.** Il vit dans Persistent mais rend sur
  `Surface_Entities`. Descendre sous terre devra basculer son `sortingLayerName` en
  `Underground_Entities` en même temps que la couche, sinon il devient noir : la lumière
  globale de l'Underground ne porte pas sur la famille Surface.
- **`UndergroundMap`.** Elle héritera de `GridMap`. `PlayerController` et `CameraFollow` la
  trouveront sans une ligne de changement : ils re-résolvent leur carte dès que celle qu'ils
  tenaient s'éteint.

## Décisions prises

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

- **`companyName` reste `DefaultCompany`.** Les sauvegardes de la phase 6 iront donc dans
  `~/Library/Application Support/DefaultCompany/SousLaVille`. À trancher avant la phase 6 :
  le changer après coup déplacerait les parties existantes de Victorien.
- **`Assets/Settings/InputSystem_Actions.inputactions`**, l'asset d'input par défaut d'Unity,
  est conservé intact car les ProjectSettings le référencent comme *project-wide actions*.
  Il n'est pas utilisé par le jeu. Ménage possible plus tard.
- **Manette.** Des bindings `<Gamepad>/dpad` et `<Gamepad>/buttonSouth` sont posés. Support
  optionnel, jamais requis, conformément à CLAUDE.md. À retirer si tu préfères le clavier seul.
