# Audit d'impact de la phase 12

Produit le 4 septembre 2026, avant d'écrire une ligne de la phase 12. Huit dimensions auditées
en parallèle, **chacune re-vérifiée adversarialement** par un second agent qui devait réfuter les
constats du premier dans le code réel, puis une synthèse et un critique de complétude.
Dix-huit agents, 2,9 millions de jetons.

Ce document est le **seul endroit** où vit ce travail : il ne se refera pas. PLAN-PHASE-12.md
n'en reprend que les décisions ; PROGRESS.md que ce qui a été corrigé en 12a. Tout le reste —
les treize pannes silencieuses, les chiffrages, l'architecture des guides, les questions
ouvertes — est ici.

**Trois constats ont été re-vérifiés à la main avant d'être crus**, parce qu'ils contredisaient
des mesures de la session : la table d'étalement (71/113 et non 73/118), la répartition des
profondeurs (804/336/60 et non 766/374/60), et le fait que le printemps ne débouche pas. Les
trois étaient justes, et sont corrigés depuis.

**Ce qui a été fait en 12a** est marqué au fil du texte. Le reste est à faire.

---

# PHASE 12 — SYNTHÈSE DES HUIT AUDITS

---

## 1. CE QUI CASSE

### 1.1 Silencieux — par ordre de gravité

**S1. Le bilan de l'eau ne tient qu'à une unité près. La 7ᵉ destination inonde le village en permanence.**
`PlantCapacityPerSeason = 9` (UndergroundSceneBuilder.cs:37), pluies 2/0/8/1, `HouseVolumePerSeason = 1`. Condition annuelle : `4D + 11 ≤ 4C`. À D=6, C=9 : 35 contre 36. À D=7, C=9 : 39 contre 36 → bassin saturé dès l'année 2, puis Lost = 5/3/11/4 à régime, **pour toujours, au printemps et en été aussi**, sans qu'aucun geste du joueur ne le défasse. Or `Inflow = ServedCount + pluie` : remplir la rangée de gouttes, c'est-à-dire réussir le jeu, est exactement ce qui déclenche l'inondation. Le commentaire de `FloodView.PaintOverflow` (« Lost plafonne à 5 par le calcul ») devient faux à la 7ᵉ destination. Correctif : une const. Rien ne le signalera.

**S2. Aucune validation de solvabilité du plan des profondeurs. Une porte de crête perdue tue 6 destinations sur 7.**
Expérience faite sur la carte actuelle, en ne changeant que les portes : intactes → 7/7 destinations, 1197 cases vivantes. Porte r=8 supprimée → **1/7, 125 cases**. Porte r=14 supprimée → 1/7, 260 cases. `ValidateAgainstVillage` ne vérifie qu'une chose de profondeur : que la station soit à MaxDepth. Pire mode de panne : sur une carte 40x40 avec crêtes laissées bornées, **une seule route sur sept change** (45 → 39) — un contrôle par échantillon ne verrait rien. Et le commentaire de `DepthRows` ment sur trois points (ce sont des anneaux complets et non des arcs ; les portes font 5 et 4 cases et non une ; elles sont à 90° et non opposées) : c'est ce commentaire que la phase 12 lira avant d'écrire.

**S3. `mapSize` est sérialisé dans quatre scènes, `BuildAllScenes` n'arrête pas la chaîne, `Teleport` ne valide rien.**
Les cinq `Build()` sont `void`, aucun retour n'est testé, et la ligne finale journalise « Les cinq scènes sont construites » même quand Underground a refusé sur `ValidateAgainstVillage`. Résultat : Surface en 60x45, Underground en 40x30, cartes désalignées case pour case. Puis `PlayerController.Teleport` (l.172-182) pose `currentCell` sans `map.Contains` : le joueur atterrit hors carte, les quatre directions sont refusées par `IsWalkable`, écran noir, console vide.

**S4. L'art n'est pas regénéré par « Construire toutes les scènes », et le plan du village est plafonné à 64 px.**
`AreAssetsPresent` ne teste que l'existence, jamais la taille — `village_map.png` fait aujourd'hui 40x30 sur le disque. Agrandir le plan sans relancer le générateur laisse une image périmée étirée sur `Width*4 x Height*4` (Image en Simple, sans preserveAspect). Et `ConfigureImporter(VillageMapTexture, null)` prend `maxSize = 64` par défaut : au-delà de 64 cases, Unity divise la texture par deux, en silence. Correction à l'audit initial : les marqueurs restent alignés (ils viennent de `mapSize`/`mapScale`, pas du sprite) — c'est le **fond** qui devient illisible ou faux.

**S5. La fontaine est le seul couple surface/sous-sol que personne n'apparie.**
`ValidateAgainstVillage` croise bouches↔échelles, maisons↔alcôves, station, bassin. Zéro ligne sur `'O'`. Les deux sont en (20,15) par la seule discipline de la main. Déplacer l'un sans l'autre : `FloodView.PaintFountain` demande `flow.IsServed(basin.Cell)` avec la case de **surface** que le solveur ne connaît pas → la fontaine ne jaillit jamais, la 6ᵉ goutte ne s'allume jamais, construction déclarée réussie. La phase 12 déplace explicitement la fontaine.

**S6. Deux listes de blocage indépendantes, et un caractère inconnu devient de l'herbe.**
`VillageLayout.IsWalkable` (liste négative) et le `switch` de `PaintVillage` sont maintenus à la main. Pire : `GroundAt` retombe sur `default: return cell`, puis `PaintVillage` retombe sur `default: grass`. **Un panneau posé au milieu d'une route peint une case d'herbe**, sur la tilemap et sur le mini-plan. C'est le piège n°1 de « plus de routes avec quelques panneaux ».

**S7. Un personnage posé en surface est traversé, et debout sur lui on ne peut plus lui parler.**
`SurfaceSceneBuilder.PaintVillage` n'a pas l'équivalent d'`InteriorsSceneBuilder` l.148-156 (tuile bloquante sous `'V'`, avec le commentaire qui explique précisément pourquoi). `PlayerController` ne consulte que la tilemap ; `PlayerInteractor` ne cherche un Villager que sur `FacingCell`. Le personnage est en ordre de tri 5, le joueur en 10 : marcher dessus le fait **disparaître et devenir muet**. Corollaire sous terre : le test du Villager passe avant Dig et PlacePipe dans `Evaluate` — une case de personnage devient increusable, silencieusement.

**S8. `ValidatePark` ne prouve pas ce que sa propre documentation promet.**
Quatre entrées en dur (13,15)(27,15)(20,19)(20,11) ; quatre BFS qui, par inondation complète, explorent **le même unique ensemble de 948 cases** — la validation prouve une fois, en quatre exemplaires, que la fontaine est atteignable « depuis quelque part ». Le `<summary>` promet « aucune case de parc enfermée » : aucune ligne ne le teste. Et `Reaches` ne vérifie pas que son point de départ est praticable.

**S9. Les phrases ont un plafond dur à 42 caractères, et PixelFont dessine du blanc muet.**
`WidthOf = 6n + 1`, `maxSize: 256` → n ≤ 42 ; **une nouvelle phrase générée sans passer ce paramètre tombe à n ≤ 10**. Au-delà, texture divisée par deux, `SpiechBox.Apply` l'affiche fidèlement réduite : du flou, pour un enfant qui apprend à lire. Alphabet réel : A–Z, apostrophe, É È Ê à À. Pas de chiffres, pas de tiret, pas de `!`, pas de `?`, pas de Ç. **Ô Î Û ne rendent rien du tout**, pas même leur lettre de base (le circonflexe existe mais n'est branché que sur Ê). « AIDE-MOI ! » sortirait « AIDE MOI  », sans un warning.

**S10. La sauvegarde est en coordonnées absolues sans empreinte de carte.**
`partie.json` vivant : 58 cases creusées, 73 tuyaux, dont **16 reposent sur des galeries du plan** (donc non rejouables si on les ferme) et 57 sur des cases creusées (donc rejouables — elles **perceront** le nouveau labyrinthe, leur enveloppe allant jusqu'à x=34, y=25). `Dig` et `PlacePipe` refusent proprement, rien ne plante : le dégât est purement sémantique.

**S11. FloodView : 21 % du village est sourd à l'eau, et la nappe attend jusqu'à 600 s.**
252 cases bloquantes sur 1200 ; sur le réseau optimal, **6 tuyaux sur 69 (9 %) sont sous une case muette** — dont la fontaine (20,15) et la maison (27,17). Et `PaintOverflow` lit `LastBudget.Lost`, recalculé au seul tick de saison : quand `PaintLeaks` et `PaintFountain` répondent instantanément à une réparation, la nappe reste **dix minutes**. Enfin `LastBudget` n'est pas sauvegardé alors que `reserveLevel` l'est : recharger assèche le village en silence, cause enregistrée, conséquence non.

**S12. Rangée de gouttes et plan du HUD.** Chevauchement du picto de saison à **14 destinations**, hors écran à **18** (`318 − 18N < 72`). `FlowSolver.DestinationCount` existe, son commentaire dit « c'est ce que le HUD compte en gouttes », et **personne ne l'appelle**. Le plan du village à `scale = 4` déborde de 320x180 au-delà de **80 x 45 cases**, sans masque, sans erreur. `VillageMapScreen.Select` ne filtre pas l'angle : dès qu'il y aura plus de trois bouches, la flèche droite pourra sélectionner une bouche presque à la verticale.

**S13. Divers, gratuits à corriger.** Le bouchon n'est effacé que par `PipeNetwork.Repair` — jamais tout seul, contrairement au commentaire de `SeasonSystem` l.23 : les bouchons s'accumulent et **le jeu éteint son propre spectacle** d'année en année (survie d'une route à k segments exposés : 0,75^(kn)). `GameClock` ne se met **jamais** en pause : un tick peut tomber au milieu d'un dialogue. `PersistentSceneBuilder` et le générateur d'art lisent le plan **sans jamais appeler `IsWellFormed`**, et Persistent est la première scène construite → `IndexOutOfRangeException` avant le message clair. `ManholeFactory.Restore` n'élague jamais une bouche déplacée. Si `SetAside` échoue, `Flush` écrase quand même le fichier qu'on avait promis de ne pas écraser. `UndergroundMap.DepthAt` clampe à 1..3 : une profondeur 4 serait lue 3, gelée dans le nœud, sans erreur.

### 1.2 Bruyant — bon à savoir avant de perdre une heure

- **Fontaine multi-cases** : `ValidatePark` exige `FindAll(Fountain).Count == 1` et arrête Surface — **mais après** que Persistent a déjà cuit 9 gouttes au lieu de 6, et le fichier reste sur le disque.
- **`ValidateReserve`** exige les **neuf** cases au-dessus du bassin en **herbe stricte** (comparaison à `VillageLayout.Grass`). Le bassin est en (10,13), la route x=12 longe son bord est. Arbres et routes élargies réduiront les emplacements légaux. Le message d'erreur parle d'Underground alors que la faute est dans le plan du village.
- **Intérieurs** : 6 créneaux de 20x10, deux occupés. `ValidateRooms` exige **exactement** une porte et **exactement un** personnage par pièce — le commentaire d'`InteriorsSceneBuilder` qui promet le contraire pour la phase 12 est faux. Et un créneau fait 10 lignes quand la caméra en montre 11,25 : prendre (0,10) ou (20,10) montrera la rangée de mur du voisin d'en dessous.
- **Profondeur 4** : `IsWellFormed` rejette le caractère `'4'`, puis `PaintUnderground` lèverait un IndexOutOfRange (`earth[DepthAt-1]` sur un tableau de 3). Cinq constantes à bouger si on y va : le Clamp de `UndergroundMap` l.76, le `Length >= 3` l.100, `PlaceholderArtGenerator.DepthCount`, `SeasonSystem.ShallowDepth`, le `[Range(0,3)]` de `freezeMaxDepth`.
- **Ligne ASCII trop courte** → IndexOutOfRange ; **trop longue** → ignorée en silence.

### 1.3 Ce qui est gratuit (vérifié par recherche exhaustive)

`UndergroundLayout.Width` aliase `VillageLayout.Width`. Les peintures de tilemap sont des `SetTilesBlock` uniques. `GridMap`, `CameraFollow`, `PipeNode/Segment/Network`, `FlowSolver`, `PipeNetworkView`, `ManholePortal` ne connaissent aucune dimension. Le motif 40/30/320/180 dans tout `Assets/Scripts` ne rend que trois lignes, toutes des défauts d'Inspector écrasés par les builders. **Seule exception** : `CameraFollow.halfView = (10 ; 5,625)`, recopié à la main, qu'aucun builder n'écrit. Et les saisons ne teignent que la Light2D globale : un arbre, un panneau, une haie n'ont besoin que d'**une** image.

---

## 2. LE CHIFFRAGE

### 2.1 Bilan de l'eau

```
Condition annuelle de non-débordement :   4·D + R  ≤  4·C
    D = destinations (maisons + cases de fontaine)
    R = pluie annuelle = 2 + 0 + 8 + 1 = 11
    C = PlantCapacityPerSeason           (UndergroundSceneBuilder.cs:37)
=>  C ≥ D + 11/4   =>   C = D + 3   (unique minimum entier)
```

Vérifié par simulation sur six années : à `C = D + 3`, **Lost = 0 à toutes les saisons pour D = 6, 7, 8, 10, 12**, et le bassin fait invariablement 5/3/2/0 sur l'année. La capacité du bassin (10) n'a jamais besoin de bouger : la pointe d'automne vaut 5 quel que soit D. À `C = D + 2` : dérive de +3/an, saturation, panne permanente.

**Nombre de destinations tenable :**
- si `C` reste à 9 → **exactement 6**. La 7ᵉ casse le jeu.
- si `C = N + 3` → tenable jusqu'à **13** (14 fait chevaucher la rangée de gouttes sur le picto de saison).
- Le bassin ne crée jamais de capacité, il ne fait que lisser.

**Conséquence de design, contre-intuitive et à trancher (voir Q1) :** bassin relié + `C = D+3` → **Lost = 0 pour toujours**. Le débordement n'est donc pas un signal de surcharge, c'est le signal « tu n'as pas encore relié le bassin ». Sa fenêtre de visibilité s'ouvre à `served ≥ D − 4` : à D=6 dès la 2ᵉ destination, à D=12 à la 8ᵉ — c'est-à-dire probablement après que le joueur a déjà trouvé le bassin. **Agrandir le village ne repousse pas la leçon, il la supprime.**

**Table d'étalement — celle de PROGRESS est périmée depuis la phase 11.** Rayon = `Lost − 1`, disque de Manhattan `2r²+2r+1` par bouche. Commit 4d0042b : 3/15/38/**73**/**118** (200 cases bloquantes). HEAD : 3/15/38/**71**/**113** (252 cases bloquantes). Lignes fausses : PROGRESS 1017-1018, 1020, 1047, 1054, 1160, 1246, plus le commentaire de `FloodView` l.147-151. À D=14 sans capacité relevée, Lost=13 → rayon 12 → 313 cases par bouche : le village entier sous l'eau, la flaque ne dit plus rien.

### 2.2 Tailles de carte

| | 40x30 (aujourd'hui) | 48x36 | 60x45 | 80x60 |
|---|---|---|---|---|
| Écrans (20 x 11,25 cases) | 2,0 × 2,7 | 2,4 × 3,2 | 3,0 × 4,0 | 4,0 × 5,3 |
| Diamètre, en pas | 70 | 84 | 105 | 140 |
| Traversée à 5 cases/s | 14 s | 17 s | 21 s | 28 s |
| Plan du HUD à `scale=4` | 160x120 ✔ | 192x144 ✔ | 240x180 ✔ (0 marge) | 320x240 ✘ |
| `maxTextureSize = 64` | ✔ | ✔ | ✔ | ✘ (÷2 muet) |
| Bouches à densité constante | 3 | 4 | 7 | 12 |
| Profondeur 1 si anneaux non redimensionnés | 67 % | ~74 % | 84 % | 91 % |

**Plafonds durs sans toucher à rien : 64 x 45 cases.** Au-delà il faut les trois ensemble : `maxSize: 256`, `scale = min(320/W, 180/H)`, **et** redimensionner curseur (16 px = 4 cases à l'échelle 4) et marqueurs (12 px = 3 cases) qui sont en pixels d'écran.

### 2.3 Ce que l'agrandissement coûte à un enfant de six ans

**L'unité est l'appui sur Espace.** `PlayerInteractor` déclenche sur `WasPressedThisFrame`, jamais sur un maintien : un appui = une case creusée ou un tuyau posé. Aucun geste amorti n'existe.

| Destination | Appuis depuis zéro | Appuis marginaux, tronc posé |
|---|---|---|
| (5,20) | 5 | 5 |
| bassin (10,13) | 35 | 33 |
| (13,17) | 46 | 11 |
| fontaine (20,15) | 56 | 14 |
| (27,17) | **73** | 17 |
| (29,10) | 64 | 17 |
| (34,4) | 68 | 15 |
| **Total 6 gouttes** | **312** | **112** (7 destinations) |

À ~1,5 s par action délibérée : le jeu naïf ≈ 13 min ≈ **1,3 saison** ; sur 80x60 il franchit l'année entière, et l'automne bouche puis l'hiver gèle un réseau à moitié posé. Le facteur d'échelle est **linéaire en (W+H)** : doubler la dimension linéaire porte la pire maison vers ~150 appuis identiques d'affilée.

**Le levier qui paie le plus : le pré-creusement.** 88 cases livrées ouvertes (7,3 % de 1200 — le commentaire du fichier dit encore « soixante-quatorze ») font tomber la pire maison de **73 à 17** appuis marginaux. Garder ce ratio veut dire tracer 190 cases sur 60x45, 350 sur 80x60. C'est du contenu, pas de la mise à l'échelle.

**Le gel n'est pas un risque, c'est une certitude.** `freezeMaxDepth = 1` et `frostResistance(Standard) = 0` : `Random.value >= 0` est toujours vrai → **chaque segment à profondeur 1 gèle à chaque hiver, probabilité 1**. La maison (34,4) en a 29, donc 30 nœuds à repasser en Isolé, un par un, `Weakest` prenant le min des deux bouts. Et le chemin qui **minimise** les segments peu profonds en compte exactement autant que le plus court (29/18/0/9/0/4/0) : aucune habileté ne les évite. Sur une carte doublée à anneaux non redimensionnés, 40 segments consécutifs à profondeur 1 → survie à un automne en Standard : 0,75⁴⁰ ≈ **1 sur 100 000**.

**Le labyrinthe.** 12x7 dalles, 38 praticables, 51 haies, plus long trajet interne 30 pas, entrées à 11/21/18/10 pas de la fontaine. Il tient dans **0,60 × 0,62 d'écran** : l'enfant le résout des yeux. Au-delà de **20 x 11 cases** il sort du cadre, la caméra défile, et l'exercice change de nature — mémoire au lieu de vue. Ce n'est pas une difficulté plus grande, c'en est une autre.

**Les bouches.** Aujourd'hui une pour 316 cases praticables ; pire marche surface→bouche 29 pas (en 31,28) ; pire marche galerie→échelle 22 pas réseau complet, 10 pas au départ. Aller-retour de réparation au pire : 102 pas ≈ 20 s. Invariant à tenir : **jamais plus de 25 pas entre une galerie et l'échelle la plus proche**, quelle que soit la taille.

---

## 3. LES PERSONNAGES-GUIDES

**Architecture recommandée : guide FIXE, BLOQUANT, sur une case validée par calcul, dont seules les PHRASES sont pilotées par l'état, plus un second renderer d'attention. Aucun déplacement en phase 12.**

Je renverse la recommandation « non bloquant » d'un audit intermédiaire : elle reposait sur un chiffre faux (« 53 cases-goulots, 28 au parc »). Le comptage exact donne **19 cases dangereuses** : 16 points d'articulation et 3 culs-de-sac. Elles sont calculables et évitables. Le défaut du non-bloquant, lui, n'est corrigible par aucun placement.

**Pourquoi bloquant.** Le commentaire d'`InteriorsSceneBuilder` l'a déjà écrit : « on ne traverse pas quelqu'un, et surtout, debout SUR lui on ne pourrait plus lui parler, puisque l'interacteur cherche un personnage sur la case REGARDÉE ». Un guide non bloquant est **recouvert** (tri 5 contre 10) et **inadressable** dès qu'on marche dessus. Pour un enfant de six ans, un guide qui s'efface quand on va vers lui est pire qu'un guide un peu mal placé.

**La liste noire, à faire calculer par `ValidateGuidePosts` (~30 lignes, patron de `ValidatePark`, rejouée à chaque construction) :**
- Les 3 pires articulations : **(6,21), (6,22), (6,23)** — le couloir d'une case vers la station. Un bloquant y détache **29 cases** : la station, son sol, son arrivée (6,25) et le portail vers le sous-sol. C'est exactement l'endroit naturel d'un guide du débordement.
- Les 13 autres, toutes dans le labyrinthe, détachent 1 à 7 cases : (17,13) (17,14) (17,15) (18,15) (20,13) (21,13) (22,13) (22,15) (23,13) (23,14) (23,15) (25,14) (25,15).
- Les 3 culs-de-sac : (19,15) (21,15) — les deux voisines de la fontaine — et un troisième. Cas d'échec puni le plus net : Victorien sur (19,15), guide qui apparaît sur (18,15) → enfermé dans une case, définitivement, sans message.
- Les **6 cases de passage** : bouches (8,19) (20,10) (33,5), arrivée station (6,25), portes (23,25) (31,25). Un bloquant les stérilise, un non-bloquant y rend le guide muet (le portail passe avant le Villager dans `Evaluate`).
- Le départ **(14,15)**.

**Le câblage, sans une ligne par frame.** Il n'y a qu'**une** source d'événement utile en jeu : `SeasonSystem.SeasonChanged`. `FlowSolver.Solved` est l'écho de `PipeNetwork.Changed`, et poser/enlever/réparer n'existent que sous terre, où le guide de surface est **éteint** (`SceneRouter.SetActiveLayer` fait `root.SetActive(false)`, et `UnloadLayerAsync` n'a aucun appelant). Donc : s'abonner à `SeasonChanged`, et **relire tout l'état en OnEnable**, exactement comme `FloodView.Subscribe()` finit par `Redraw()`.

**Trois propriétés déjà présentes, gratuites.**
- Se taire sans disparaître : `CanSpeak = LineCount > 0 && ResolveBox() != null`, et `Evaluate` ne propose `Talk` que si `CanSpeak`. Vider les lignes laisse le guide visible avec son signal, et Espace ne fait plus rien.
- Le registre statique ne contient jamais que la couche allumée : `Villager.At` n'a pas besoin de filtrer. **Mais** il rend le premier trouvé — deux guides sur la même case, l'un est inadressable pour toujours.
- La disparition est gratuite pendant le fondu de `TravelAsync`, et réparer n'existe que sous terre : le guide de la fuite aura disparu de lui-même quand le joueur remontera.

**Trois choses à ajouter.**
1. **Un second SpriteRenderer d'attention**, enfant du guide, ordre 11 (le prompt de l'interacteur est à 12, le villageois à 5), piloté par le guide et non par `PlayerInteractor` — celui-ci éteint sa bulle dès que le joueur regarde ailleurs. Un triangle de danger du vocabulaire routier : c'est littéralement la passion de Victorien.
2. **Une garde d'une ligne sur `SpeechBox.IsOpen`** (déjà publique) : `GameClock` ne se met jamais en pause, une saison dure 600 s, un tick peut faire disparaître le personnage au milieu de sa phrase pendant que la boîte reste ouverte.
3. **`FloodView` publie ses cases peintes** et repousse la flaque sur la case praticable voisine la plus proche : 21 % du village est sourd à l'eau, 9 % des tuyaux du réseau optimal sont sous une case muette. Cela règle en même temps la visibilité de la fuite et le placement du guide.

**Contenu de la phase : UN seul guide, celui de la fuite**, posté près de la bouche la plus proche du réseau. Il dit qu'il y a une flaque et montre `promptRepair` — que `PlayerInteractor.SpriteFor` rend déjà devant un tuyau abîmé, donc l'enfant n'a rien à mémoriser entre la phrase et le geste. La désignation du lieu, c'est la flaque qui la fait, depuis la phase 10.

**Coût.** ~30 lignes de `ValidateGuidePosts` · un marqueur de plus dans `VillageLayout` (donc les 5 éditions : tuile, `GroundAt`, `PaintVillage`, la liste unique de blocage, `BuildVillageMap`) · ~100 lignes de composant de conditions · une `CreateGuides` dans `SurfaceSceneBuilder` sur le patron exact de `CreateManholes` · `SetLines` sur `Villager` · 2 à 4 images de phrase (≤ 42 caractères, `maxSize: 256` **explicite**, et pas de tiret ni de `!` tant que PixelFont n'a pas ses glyphes) · 1 image d'attention · chaque image à déclarer dans `AreAssetsPresent`, que six points d'entrée interrogent et qui bloque **toute** construction de scène si elle manque. **Zéro sauvegarde, zéro placement au runtime, zéro liste noire à maintenir quand le labyrinthe grandira.**

**Ce qu'on n'achète pas** : le guide n'est pas « à côté de LA flaque ». Si, une fois vu en jeu, ce n'est pas assez, `SetCell` (case + transform ensemble, sinon guide adressable ici et dessiné là) et la publication de `FloodView` sont les mêmes ~25 lignes, faites après plutôt qu'avant.

---

## 4. QUESTIONS OUVERTES — à toi seul

**Q1. Combien de destinations, et le débordement doit-il rester visible après le bassin ?**
Deux questions liées. (a) Rester à **6 destinations**, ajouter des maisons **décoratives** sans goutte ni eau : zéro recalcul, HUD comptable sur les doigts, débordement informatif — mais le village n'a que six maisons vivantes quelle que soit sa taille. (b) **N destinations, `C = N+3`** (une const à `UndergroundSceneBuilder.cs:37`) : ouvre le village, oblige à rejouer le tableau du bilan de la phase 8, plafonne à 13 pour le HUD. **Dans les deux cas** : une fois le bassin relié, `Lost = 0` pour toujours et la fenêtre où le débordement se voit s'ouvre à `served ≥ D−4`, donc de plus en plus tard. Veux-tu que le débordement redevienne un retour permanent (station **agrandissable en jeu**, plutôt qu'une capacité écrite à la main) ?

**Q2. Sens et taille de l'agrandissement — et le partie.json.**
`At` fait `Rows[Height−1−y][x]` : **prépendre les lignes en tête des tableaux et ajouter les caractères en fin de ligne** préserve rigoureusement chaque (x,y) **et** chaque profondeur, sans une ligne de code, vérifiable d'un diff. Le nord est en plus la seule direction où le puzzle grandit : la station en (6,25) est à 4 cases du bord nord, ses anneaux y sont rognés (22/32 et 25/56) ; +10 lignes au nord les portent à 29 et 41 et font **baisser** la plaine plate de 68 à 66 %, quand +20 colonnes à l'est la montent à 78 %. **Mais** : Victorien n'a pas de partie (décision du 2 septembre — il ne joue qu'à la dernière phase ; les 12 fichiers du dossier sont des essais de dev). Un clic sur « Repartir d'une partie neuve » libère entièrement la contrainte, sans toucher à `CurrentVersion`. **Acceptes-tu ce clic ?** Si oui : quelle taille — 48x36 (rien d'autre à changer), 60x45 (station recentrée, anneaux redimensionnés, échelle du plan calculée), au-delà (déplacement rapide entre bouches obligatoire) ?

**Q3. Un grand labyrinthe ou trois ou quatre petits ?**
Au-delà de 20 x 11 cases il sort du cadre de la caméra : l'enfant mémorise au lieu de voir. Trois labyrinthes de 19x11 donnent le même volume de contenu sans ce basculement. Et acceptes-tu que je trace 190 à 350 cases de galerie pré-creusées pour tenir le ratio de 7,3 % ? C'est ce qui décide si la carte agrandie est jouable (73 appuis contre 17), et c'est du contenu, pas de la mise à l'échelle.

**Q4. Le plan des profondeurs devient-il une formule ?**
Il est reproductible **à 0 écart sur 1200 cases** par 4 nombres et 2 listes de portes : la décision « écrit à la main et non engendré » (VillageLayout.cs:42-43) ne protège rien ici — elle reste juste pour `Rows` et pour le labyrinthe de haies. Si oui : deux crêtes, trois (un anneau à 18 doublerait la matière), ou une quatrième profondeur (5 constantes en dur à bouger, et une vraie transition de plus dans le puzzle) ? Et quel **plafond de segments à profondeur 1** sur la route la plus longue — 29 aujourd'hui, donc 30 nœuds à repasser en Isolé chaque hiver, un par un ? C'est le vrai réglage de rythme, et il prime sur la taille.

**Q5. Arbres et panneaux : bloquants ? Et l'usine à panneaux passe-t-elle avant ?**
Recommandation, sur la base des 21 % de cases déjà sourdes à l'eau : **panneaux non bloquants et repères** — y compris aux carrefours de galeries, ce qui règle en même temps l'absence totale de repère souterrain (`LayerIndicator` porte encore « pas de mini-carte : elle serait vide de sens tant que le réseau n'existe pas », caduc depuis la phase 3) ; **arbres bloquants**, hors des chemins et du parc. Mais surtout : **PROGRESS l.1465-1496 pose que l'usine à panneaux occupe les phases 12 à 15 et « passe avant l'habillage, jamais après », précisément pour ne pas dessiner les panneaux deux fois.** Des panneaux dans les rues dès maintenant en font un troisième dessin. On renonce à cette décision, ou on fait l'usine d'abord et le décor réutilise son catalogue ?

**Q6. Les guides : fixes et bloquants comme recommandé, ou mobiles ? Et la progression se sauvegarde-t-elle ?**
Le fixe bloquant sur case validée coûte ~150 lignes et zéro état. Le mobile demande `SetCell`, la publication des cases de `FloodView`, une liste noire recalculée, et accepte qu'on puisse effacer le guide en marchant dessus. Deuxième volet : retenir « ce qui a déjà été dit » demande d'abord de donner à `Villager` un **identifiant texte stable** (aujourd'hui son seul état d'identité est sa case, `At` compare la case), puis de faire entrer le porteur de cet état dans le **ET des quatre** de `SaveSystem.TryLoad` — sinon il ne sera jamais relu, en silence, exactement comme les plaques en phase 9a. Réexpliquer à chaque lancement ne punit rien et ne coûte aucun champ.

---

## 5. DÉCOUPAGE PROPOSÉ

La phase est trop grosse pour un seul arrêt. Cinq sous-phases, chacune finissant sur un résumé et ta validation, conformément à la règle 1.

### 12a — Les filets (aucun contenu neuf)
Objectif : que **toutes** les ruptures de 12b à 12e deviennent bruyantes.
- `BuildAllScenes` : les cinq `Build` rendent `bool`, chaîne arrêtée au premier refus, log de succès conditionnel ; **régénérer l'art** dans la foulée (ou faire comparer par `AreAssetsPresent` la taille de `village_map.png` à `Width × Height`).
- `ConfigureImporter(VillageMapTexture, null, maxSize: 256)` ; `scale = min(320/W, 180/H)` calculée et passée à `VillageMapScreen` ; curseur et marqueurs dimensionnés en cases.
- `PlayerController.Teleport` : garde `map.Contains` + log.
- **Une seule liste de blocage** : `PaintVillage` interroge `VillageLayout`.
- `ValidatePark` réécrit : entrées **dérivées du plan**, connexité complète des dalles, départ praticable.
- `ValidateAgainstVillage` : apparier la **fontaine** surface↔sous-sol (6 lignes, patron des maisons).
- **`ValidateWaterBudget`** : `D = FindAll(House) + FindAll(Fountain)`, `R = Σ rainVolume`, refuser si `4D + R > 4C` en annonçant la capacité attendue ; vérifier aussi la capacité du bassin et que le nombre de gouttes de Persistent vaut `D` (brancher `DestinationCount`, qui existe et n'est appelé nulle part).
- **`ValidateDepthPuzzle`** : BFS inverse depuis la station à profondeur non décroissante ; exiger que les 7 destinations soient atteintes ; journaliser les cases mortes et le nombre de cases de porte **effectivement en carte** par crête.
- `PersistentSceneBuilder` et le générateur d'art appellent `IsWellFormed` en tête.
- PixelFont : refuser toute phrase > 42 caractères (ou dériver `maxSize` de `WidthOf`) ; ajouter les 10 chiffres, `-`, `!`, `?`, `Ç`, et mapper `Ô Î Û Ù`.
- `PersistentSceneBuilder` écrit `CameraFollow.halfView` depuis les constantes de référence.
- Corriger les commentaires qui mentent : `DepthRows` (anneaux complets, portes de 5 et 4 cases, à 90°), « 74 cases praticables » → **88**, `TreatmentPlant` (8 → 9), `SeasonSystem` l.23 (le bouchon **ne** se défait **pas** tout seul).
- Mettre `partie.json` de côté via `SaveTools` — **sans** monter `CurrentVersion` (le test est une inégalité stricte).
- Corriger PROGRESS : table d'étalement 0/3/15/38/**71**/**113**, 252 cases bloquantes, et la répartition 766/374/60 fausse depuis la phase 4 (le vrai est 804/336/60).

### 12b — La carte agrandie, sans contenu neuf
Taille tranchée (Q2), plans réécrits en respectant le sens choisi, profondeurs (Q4), pré-creusement au ratio (Q3), `PlantCapacityPerSeason` recalculée (Q1), bouches à densité constante. Tout est vérifié par les validateurs de 12a. Recalculer et réécrire la table d'étalement.

### 12c — Le décor : routes, arbres, panneaux, fontaine
Chaque caractère neuf = 5 éditions. **Séparer `tile_fountain.png` en deux images** (patron `house.png` / `tile_house.png`) avant de toucher à la taille de la fontaine — aujourd'hui la tuile bloquante et le sprite sortent du **même fichier**, ce qui la cloue à 16x16. Et engendrer **16 tuiles de haie à masque de raccord** sur le patron exact de `BuildPipe` (48 images de tuyaux existent déjà) : sinon le labyrinthe agrandi est trois à quatre cents carrés verts identiques séparés par un liseré d'un pixel. Si un Sorting Layer est ajouté pour les cimes d'arbre : `GameSortingLayers.Families` **et** les cibles de la Light2D globale, sinon sprites noirs sans message.

### 12d — Le personnage-guide de la fuite
`ValidateGuidePosts`, marqueur de poste, composant de conditions, second renderer d'attention, garde `SpeechBox.IsOpen`, `FloodView` publie ses cases et repousse la flaque sur la case praticable voisine.

### 12e — Le confort, si le temps le permet
`ApplyWaterBudget` rejoué **à blanc** sur `Solved` (la nappe attend jusqu'à 600 s quand la fuite et la fontaine répondent tout de suite) · `LastBudget` sauvegardé · `VillageMapScreen.Select` filtré à 45° · Espace sur une échelle rouvre le plan pour remonter par une autre bouche (les trois pièces existent : `VillageMapScreen`, `ManholePortal.Find(layer, cell)`, `TravelAsync`) · élagage de `ManholeFactory.Restore` · drapeau d'interdiction d'écriture si `SetAside` échoue.

---

**Fichiers les plus chargés en dette pour cette phase** :
`/Users/leonielarribau/SousLaVille/Assets/Editor/SceneBuilders/VillageLayout.cs` ·
`/Users/leonielarribau/SousLaVille/Assets/Editor/SceneBuilders/UndergroundLayout.cs` ·
`/Users/leonielarribau/SousLaVille/Assets/Editor/SceneBuilders/PersistentSceneBuilder.cs` ·
`/Users/leonielarribau/SousLaVille/Assets/Editor/SceneBuilders/BuildAllScenes.cs` ·
`/Users/leonielarribau/SousLaVille/Assets/Editor/ProjectSetup/PlaceholderArtGenerator.cs` ·
`/Users/leonielarribau/SousLaVille/Assets/Scripts/Seasons/SeasonSystem.cs` ·
`/Users/leonielarribau/SousLaVille/Assets/Scripts/World/FloodView.cs` ·
`/Users/leonielarribau/SousLaVille/Assets/Scripts/Player/PlayerInteractor.cs`

---

# Critique de complétude

Un dernier agent, chargé de trouver ce que la synthèse avait manqué.

CE QUI MANQUE — six trous, vérifiés dans le dépôt

**1. La diagnosticabilité d'une route fausse. Le trou le plus grave, et personne ne l'a nommé.**
`FlowSolver.TraceToPlant` (`/Users/leonielarribau/SousLaVille/Assets/Scripts/Network/FlowSolver.cs`, l.199-236) ne remplit `carrying` que par `MarkPath`, appelé **uniquement quand la station est atteinte**. En cas d'échec il rend `false` et jette son `visited`. Or `PipeNetworkView.TintFor` peint `waterTint` si et seulement si `flow.IsCarryingAt(cell)`, sinon `idleTint = Color.white`. Conséquence exacte : une route de 57 segments fausse **d'une seule case** est rendue **entièrement blanche**, indiscernable d'un tuyau qu'on vient de poser. Le retour est binaire et arrive après 73 appuis sur Espace. C'est la seule chose du jeu que l'agrandissement dégrade **linéairement avec la longueur des routes**, et les huit audits ont chiffré les 73 appuis sans jamais regarder ce que l'enfant obtient au bout. Le remède est déjà à moitié écrit : `visited` contient la frontière atteinte depuis la maison — la publier donne « l'eau monte jusqu'ici, et pas plus loin », c'est-à-dire l'endroit exact où la règle de profondeur casse. Et c'est ce qu'un personnage-guide devrait montrer ; on ne peut pas expliquer « où » tant que le jeu l'ignore.

**2. La règle centrale du jeu n'est affichée nulle part, et personne n'a inventorié ce qu'il faut enseigner.**
`grep -rn "Depth" Assets/Scripts/UI Assets/Scripts/Player` → **zéro résultat**. La profondeur ne se lit que dans trois nuances de brun (`#6B4F38`, `#55402D`, `#3E3226`, deux crans de luminosité, `PlaceholderArtGenerator.cs` l.200-212). Le texte total du jeu fait **sept phrases**, toutes derrière les portes de deux boutiques (`CraftsmanLines` l.103, `WorkerLines` l.166) et toutes sur le choix des plaques et des tuyaux. Rien ne dit le but, rien ne dit qu'on creuse, rien ne dit la règle de profondeur — qui EST le puzzle selon CLAUDE.md. La demande de l'utilisateur est « comprendre **petit à petit les missions** » ; la synthèse répond par **un** guide sur la fuite, c'est-à-dire la leçon la moins essentielle, et tranche explicitement contre la mémoire (« réexpliquer à chaque lancement ne punit rien ») alors que « petit à petit » demande une progression, donc un état. Il manque le livrable de base : la liste des leçons (but, creuser, poser, **profondeur**, bouche/échelle, saisons, réparer, bassin) et, pour chacune, découvrable aujourd'hui oui/non. Sans elle, 12d n'a pas de contenu, seulement une architecture.

**3. L'audio : zéro, et c'est le seul dossier de l'arborescence imposée resté vide.**
`Assets/Audio/` ne contient qu'un `.gitkeep`. `grep -rn "AudioSource\|AudioClip\|PlayOneShot" Assets/` → **aucun résultat** (un `AudioListener` traîne sur la caméra de Persistent, il n'écoute rien). `grep -iE "audio|sonore|bruitage|musique"` sur les 1827 lignes de PROGRESS.md → **aucune occurrence**. Aucun des huit audits ne prononce le mot. Ce n'est pas du confort : la contrainte « le moins de texte possible » de CLAUDE.md rend le son le canal non textuel évident, un personnage qui « demande de l'aide » sans un son ne demande rien, et le « second renderer d'attention » recommandé en 12d serait le seul signal du jeu — muet, hors écran dès que la caméra a bougé. À trancher maintenant, parce qu'ajouter le son après coup oblige à repasser sur chaque geste déjà écrit (creuser, poser, réparer, parler, descendre, saison qui tourne).

**4. Le conflit de numérotation, au niveau du plan et pas seulement des panneaux.**
Le tableau d'état de PROGRESS.md (l.7-27) dit : **12 = L'usine à panneaux, 13 = Le Stock, 14 = La Fabrique, 15 = Le Plan, 16 = Habillage**. La demande de l'utilisateur est une phase 12 entièrement différente. La synthèse n'en parle qu'en Q5, et seulement sous l'angle « les panneaux seraient dessinés deux fois ». Les vraies conséquences non nommées : cinq lignes du tableau à renuméroter ; la décision du 3 septembre « elle passe **avant** l'habillage, jamais après » et « elle passe **après** l'arc du réseau » à rejuger ; le découpage 12a-12e proposé qui fait cinq arrêts et cinq commits pour **une** ligne du tableau (règles 1, 5 et 6 de CLAUDE.md) ; et la note périmée de la phase 4, « la teinte plutôt qu'une animation d'eau, à rediscuter à l'habillage, **phase 12** » (PROGRESS l.~1258), qui pointe désormais sur autre chose.

**5. Une ambiguïté de la demande jamais soulevée : quel labyrinthe ?**
« un labyrinthe bien plus grand (et difficile) » — les huit audits ont tous supposé le labyrinthe de haies du parc (phase 11). Le jeu en a deux : les haies en surface, et le réseau souterrain, que CLAUDE.md décrit comme « une map souterraine » et que la passion de Victorien pour « les canalisations, les installations souterraines et les labyrinthes » désigne tout autant. Les deux réponses n'ont rien en commun : l'une est du dessin de haies borné à 20 × 11 cases par la caméra, l'autre est le plan des profondeurs, les crêtes et le pré-creusement. Question à poser avant d'écrire une ligne de plan.

**6. Le prix de l'agrandissement sur la reprise de partie, jamais chiffré.**
`SaveData` (`/Users/leonielarribau/SousLaVille/Assets/Scripts/Core/SaveData.cs`) ne porte **ni la case ni la couche du joueur** — décision validée le 3 septembre, « Victorien repart toujours au départ du village ». `SceneRouter.CurrentLayer` démarre à `GameLayer.Surface`. J'ai mesuré sur le plan actuel depuis le départ (14,15) : bouches à **10, 11 et 29 pas**, case la plus lointaine à **38 pas**, soit 6 à 8 secondes à 5 cases/s. Doublez la dimension linéaire et chaque lancement de partie commence par 20 à 30 secondes de marche pour revenir là où l'enfant travaillait, sans aucun déplacement rapide depuis la surface (le plan du village ne s'ouvre que dans l'atelier, pour poser une plaque). Une décision gratuite à 40x30 qui cesse de l'être ; les audits n'ont regardé que les trajets de réparation **une fois sur place**.

Points mineurs vérifiés et non couverts, sans gravité : les performances ne sont pas un risque (tout est événementiel, `SetTilesBlock` uniques, aucun `Find` par frame sauf des gardes court-circuitées — la contrainte 60 fps de CLAUDE.md tient à n'importe quelle taille) ; `SousLaVilleInputActions` / `SousLaVille.inputactions` n'a été lu par aucun audit mais ne pose rien (flèches + Espace, dpad et buttonSouth manette, direction dominante donc pas de diagonale) ; `Bootstrapper` et `BootSceneBuilder` n'ont été lus par personne et ne connaissent aucune dimension ; les deux asmdef portent déjà les deux assemblies URP exigées par la règle 9 de CLAUDE.md. Enfin, aucun des huit audits n'a ouvert Unity : l'état de compilation de référence (zéro erreur **et** zéro warning) n'est pas établi avant de commencer, et aucun des calculs sur lesquels repose tout le plan (BFS de solvabilité, simulation du bilan de l'eau, points d'articulation, table d'étalement) n'existe dans le dépôt — ils ont été refaits trois fois dans des scripts jetables, avec des résultats divergents entre agents. La sous-phase 12a doit les faire entrer dans `Assets/Editor` **avant** 12b, sinon la phase suivante repartira de la prose.
