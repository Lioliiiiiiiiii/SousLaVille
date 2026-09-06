# Sous la Ville

Jeu 2D de construction de réseau d'assainissement, conçu pour un enfant de 6 ans
qui s'appelle Victorien. Il est passionné par les panneaux de signalisation, la
circulation de l'eau, les canalisations, les bouches d'égouts, les installations
souterraines et les labyrinthes. Il joue à Minecraft mais n'est pas encore à
l'aise avec les commandes.

## Objectif de design

Un bac à sable rejouable indéfiniment. Pas de fin, pas de game over. Le joueur
relie les maisons du village à la station d'épuration en creusant et en posant
des canalisations dans une map souterraine. Les saisons mettent le réseau à
l'épreuve en boucle.

## Contraintes non négociables

### Accessibilité 6 ans
- Déplacement aux **flèches directionnelles uniquement**, 4 directions, pas de
  diagonale. **Une seule touche d'action** (Espace) pour tout interagir.
  Support manette optionnel, jamais requis.
- Aucune combinaison de touches, aucun double-clic, aucun timing serré.
- Zones cliquables d'au moins 32 pixels de côté à la résolution de référence.
- **Aucun échec puni.** Un réseau qui déborde est un spectacle rigolo. Pas
  d'écran de défaite, pas de perte de progression, pas de compte à rebours
  stressant.
- **Le moins de texte possible.** Communiquer par pictogrammes de type panneau
  de signalisation. Le peu de texte affiché est en **français**, en phrases de
  moins de six mots.
- Sauvegarde automatique. Le joueur ne doit jamais avoir à penser à sauvegarder.

### Style visuel
- Inspiration : RPG top-down sur console portable, début des années 2000.
  Perspective 3/4 vue de dessus, palette limitée, contours nets.
- **N'utiliser aucun asset Nintendo ou Pokémon.** Uniquement des assets
  originaux ou sous licence CC0. Si un asset manque, générer un placeholder
  coloré et le signaler dans PROGRESS.md.
- Tiles de 16x16 px. Personnage de 16x24 px. Pixels Per Unit = 16.
- Pixel Perfect Camera, résolution de référence 320x180.

### Technique
- Unity 6 LTS, Universal 2D (URP).
- New Input System.
- Sauvegarde en JSON via Newtonsoft, dans Application.persistentDataPath.
- Aucun package externe supplémentaire sans me demander d'abord.
- Le jeu tourne à 60 fps sur un portable modeste. Pas de simulation lourde.

## Architecture imposée

Assets/
  Art/            Tiles, Sprites, UI, Pictos
  Audio/
  Prefabs/
  ScriptableObjects/   SeasonDefinition, PipeType, BuildingDefinition
  Scenes/         Boot, Persistent, Surface, Underground
  Scripts/
    Core/         GameManager, SaveSystem, SceneRouter, GameClock
    Network/      PipeNode, PipeSegment, PipeNetwork, FlowSolver
    World/        SurfaceMap, UndergroundMap, HouseSpawner, ManholePortal
    Player/       PlayerController, PlayerInteractor
    Seasons/      SeasonSystem, SeasonDefinition
    Buildings/    TreatmentPlant, PipeFactory, WaterReserve, ManholeFactory, Fountain
    Minigames/
    UI/
  Editor/         Scripts [MenuItem] de construction de scène

## Modèle de données central

Le réseau est un **graphe**, pas une simulation de fluide.

- `PipeNode` : Vector2Int gridPos, int depth (1 = peu profond, 2 = moyen,
  3 = profond), NodeType { Junction, HouseConnection, Manhole, PlantInlet,
  FountainInlet }
- `PipeSegment` : nodeA, nodeB, PipeType, float condition (0 à 1),
  bool isFrozen, bool isClogged
- `PipeNetwork` : liste d'adjacence, plus les méthodes d'ajout et de suppression
- `FlowSolver` : pour chaque maison, parcours en largeur jusqu'à la station.
  Un segment transporte si condition > 0.3, s'il n'est pas gelé et s'il n'est
  pas bouché. **Rien d'autre.** Deux tuyaux qui se touchent laissent passer
  l'eau.

**La règle de profondeur croissante a été retirée en phase 21.** Elle a tenu des
phases 4 à 20 et ce fichier en faisait le puzzle du jeu. Ce qui l'a emportée est
une mesure, prise sur les quatorze destinations de la carte : elle ne rendait
aucune maison impossible, mais elle **multipliait la longueur de la route par 2
à 3,6**. La maison (27,35) demandait 113 cases au lieu de 31 — trois fois plus
de creusements et de poses, sur un tracé contre-intuitif. Le puzzle était devenu
un péage.

`depth` reste dans les données et à l'écran : les trois nuances de terre font la
beauté du sous-sol. Elles ne décident plus de rien.

Ce qui porte la difficulté à sa place : **le labyrinthe et la distance**, déjà
dans la carte, et **le choix du tuyau** face à la saison qui vient.

Le solveur ne tourne que sur changement de réseau et à chaque tick de saison.
Jamais à chaque frame.

## Règles de travail

1. **Une phase à la fois.** À la fin d'une phase, s'arrêter, résumer ce qui a
   été fait, et attendre ma validation. Ne jamais enchaîner sur la phase
   suivante de sa propre initiative.
2. **Ne pas construire les scènes objet par objet via MCP.** Écrire un script
   Editor avec `[MenuItem("Sous La Ville/Construire la scène X")]` qui génère la
   scène par code, puis l'exécuter. C'est plus rapide, reproductible et
   versionnable.
3. **Vérifier la compilation.** Ne jamais déclarer une phase terminée sans avoir
   lu la console Unity et confirmé zéro erreur.
4. **Art placeholder d'abord.** Carrés de couleurs franches et distinctes. Le
   pixel art vient à la toute fin, une fois le gameplay validé.
5. **Commit git à la fin de chaque phase**, message en français, préfixé par le
   numéro de phase.
6. **Tenir PROGRESS.md à jour** : ce qui est fait, ce qui reste, les décisions
   prises, les placeholders à remplacer, les questions ouvertes.
7. **Poser une question plutôt que deviner** sur toute décision de design non
   couverte par ce fichier.
8. Code commenté en français, noms de classes et de variables en anglais.
9. **Vérifier les références d'assembly.** Tout nouveau script Editor qui touche à des
   types URP doit vérifier que `SousLaVille.Editor.asmdef` porte les références
   nécessaires. Attention, URP découpe son runtime en deux assemblies : `Light2D` et
   `PixelPerfectCamera` sont dans `Unity.RenderPipelines.Universal.2D.Runtime`, pas dans
   `Unity.RenderPipelines.Universal.Runtime`.
