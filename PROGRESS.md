# Sous la Ville, journal de bord

Unity 6000.5.10f1, URP 17.6.0 (Renderer2D), Input System 1.20, Newtonsoft 3.2.2.
Aucun package supplémentaire n'a été ajouté au projet.

## État par phase

| Phase | Titre | État |
|---|---|---|
| 0 | Fondations | Terminée |
| 1 | Le personnage et la surface | À faire |
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

## Prochaine étape, phase 1

Décisions déjà validées, à appliquer sans les rediscuter :

- **Déplacement case par case**, environ 5 tuiles par seconde, flèche maintenue égale pas
  enchaînés. Aucune physique 2D : les collisions sont une lecture de tilemap.
- **Plan du village écrit à la main**, carte ASCII de 30 lignes de 40 caractères dans
  l'assembly Editor. Le procédural est réservé au labyrinthe de la phase 11.
- **Placeholders en PNG générés par script**, PPU 16, filtre Point, sans compression, plus les
  assets `Tile` correspondants.
- **Un seul personnage, dans la scène Persistent.** Surface et Underground partagent le même
  repère : descendre par une bouche en phase 2 devient un échange de décor.
- Chaque tilemap et chaque sprite doit choisir le Sorting Layer **préfixé de sa couche**.

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

## Placeholders à remplacer

- Aucun asset graphique pour l'instant. La phase 1 introduira les premiers carrés de couleur.
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
- **Où vit le joueur ?** Un personnage par couche, ou un seul objet déplacé d'une couche à
  l'autre. Question de la phase 2.
