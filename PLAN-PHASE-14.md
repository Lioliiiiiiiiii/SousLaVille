# Phase 14 — Le Stock

Le premier des trois mini-jeux de l'usine à panneaux : un **memory**. Il valide le squelette
que La Fabrique (15) et Le Plan (16) reprendront. Les phases 15 et 16 ne sont pas commencées.

Ce document est le plan soumis à validation, règle 1. **Rien n'est écrit tant qu'il n'est pas
validé**, et les points laissés ouverts en fin de document sont posés en question plutôt que
devinés, règle 7.

---

## 0. Ce que la phase 13 laisse, et qui n'est pas refait

- `PlaceholderArtGenerator.SignBoard` : les **24 rangs** dans l'ordre de lecture.
  `SignBoardNames` : les 24 noms alignés dessus. `SignTexture(kind)` et
  `SignNameTexture(kind)` : les deux images. **Aucun panneau n'est ajouté.**
- La pièce de l'usine, créneau (0, 0), et ses trois personnages. **Rien n'est ajouté à la
  pièce** : le memory est un écran, pas un coin de la salle (§3).
- `Villager` (registre statique, `SetLines`, bulle d'attention), `SpeechBox`,
  `VillageMapScreen`, `PlayerInteractor.screenClosedFrame`, `SignCatalogue`,
  `InteriorsSceneBuilder.ValidateRooms` : repris tels quels, étendus, jamais redoublés.

---

## 1. La forme du memory

### 1.1 Ce qui est apparié : **panneau ↔ panneau**, et le NOM s'affiche à l'appariement

Le pont vers La Fabrique est gardé, mais il ne passe pas par la carte. **Mesuré avant de
choisir**, sur les 24 noms réels et la chasse fixe de `PixelFont` (`WidthOf = 6n + 1`) :

| | largeur du nom |
|---|---|
| le plus court, DANGER (6 car.) | **37 px** |
| médiane | **121 px** |
| le plus long, ARRÊT ET STATIONNEMENT INTERDITS (32 car.) | **193 px** |

Une carte doit contenir le plus large nom de la manche. Sur 320 px de large :

- cartes de 193 px → **1 colonne**. Pas une grille.
- cartes de 150 px → 2 colonnes, 4 rangées = 8 cartes = **4 paires**, et 2 noms sur 24
  débordent encore. Un memory de quatre paires n'est pas un memory.
- en ne retenant que les noms courts (≤ 89 px) → il n'y en a que **quatre** sur vingt-quatre.
- en passant `PixelFont` au rendu multi-ligne (code neuf, et les 24 images à refaire) → cartes
  de 104 px, 3 colonnes, 4 rangées = **6 paires**. Le double du coût pour la moitié du jeu.

**Donc : panneau ↔ panneau.** Et le nom n'est pas perdu — il est *gagné* : **chaque paire
trouvée affiche son nom** dans une bande en bas de l'écran, par `SignNameTexture(kind)`, qui
existe déjà et n'a rien à redessiner. Le nom devient la récompense de la trouvaille au lieu
d'être l'énigme, ce qui est exactement le rôle que le personnage lui donne — « RETROUVE-LES
DEUX PAR DEUX » — et ce qui prépare La Fabrique, dont le sujet **est** de nommer.

### 1.2 La grille : quatre rangées, cartes de 32, à 320x180

Contraintes dures : carte **≥ 32 px de côté** (CLAUDE.md), écran de référence 320x180, une
bande de nom en bas (13 px, le plus large nom fait 193 ≤ 312).

Vérifié par calcul, gouttière de 4 px :

| grille | cartes | paires | plateau | marge H | marge V |
|---|---|---|---|---|---|
| 4 × 4 | 16 | **8** | 140 × 140 | 90 | 12 |
| 4 × 6 | 24 | **12** | 212 × 140 | 54 | 12 |
| 4 × 8 | 32 | **16** | 284 × 140 | 18 | 12 |

Et les deux bornes, qui font que la forme n'est pas libre :

- **5 rangées sont impossibles** : 5×32 + 4×4 = **176 px** pour 156 disponibles sous la bande.
- **9 colonnes sont impossibles** : 9×32 + 8×4 = **320 px** pour 312 disponibles.

Le panneau est dessiné à **1:1, 16×24**, centré dans sa carte de 32×32 : la taille exacte qu'il
a dans le village et dans la pièce. Le doubler (32×48) plafonnerait à **3 rangées et 9 paires**,
mesuré aussi.

**Le fond de l'écran est OPAQUE, et ce n'est pas de l'esthétique.** La rangée de gouttes du HUD
occupe le coin haut-droit de y = 160 à 176, exactement là où passe la rangée haute du plateau.
Un voile translucide à 0,6 comme celui de `VillageMapScreen` laisserait quatorze gouttes
transparaître au travers des cartes du haut. C'est le piège de la phase 13, à l'identique
(PIEGES.md, « une pièce d'intérieur fait dix lignes quand la caméra en montre 11,25 »).

### 1.3 Le tirage : les quatre familles, à parts égales

8, 12 et 16 paires sont **toutes divisibles par 4** : chaque manche tire **2, 3 ou 4 panneaux
par famille**, au hasard dans la famille. Les quatre formes sont donc toujours présentes, et la
leçon de la pièce — *la forme dit la famille avant que le dessin dise le détail* — tient dans le
mini-jeu au lieu d'y être contredite par un tirage qui pourrait sortir six triangles rouges.

### 1.4 Le tour de jeu, sans aucun compte à rebours

1. Les flèches déplacent un cadre de case en case. **Une carte déjà appariée se saute** : le
   cadre ne s'y arrête pas, il n'y a donc aucun coup perdu et aucun geste sans effet.
2. Espace retourne la carte visée. Elle reste face visible.
3. Espace sur une seconde carte la retourne aussi.
   - **Même panneau** : les deux restent visibles pour toujours, et **leur nom s'affiche** dans
     la bande du bas.
   - **Panneaux différents** : les deux restent visibles **jusqu'à la prochaine action du
     joueur, quelle qu'elle soit** — une flèche ou un Espace les retourne. **Aucune minuterie** :
     l'enfant regarde aussi longtemps qu'il veut, et rien ne lui est demandé dans un délai.
     C'est la seule façon de tenir « aucun timing serré » sans faire perdre l'information.
4. Toutes les paires trouvées : le cadre disparaît, le picto de sortie s'allume dans le coin, et
   Espace referme. **Aucun score, aucun compte de coups, aucun chrono.** Un memory raté n'existe
   pas : il n'y a que des memory pas encore finis.

### 1.5 La progression, sans une ligne de sauvegarde

`PROGRESS.md` pose que **rien ne se sauvegarde dans l'usine**. La montée en difficulté tient
donc dans la session : **une manche par lancement**, et la manche suivante est plus grande.

| lancement | paires | grille | par famille |
|---|---|---|---|
| 1 | 8 | 4 × 4 | 2 |
| 2 | 12 | 4 × 6 | 3 |
| 3 et suivants | 16 | 4 × 8 | 4 |

Le compteur est un champ du composant, dans la scène `Interiors`. Le `SceneRouter` éteint la
couche mais ne la détruit pas : il survit donc à une sortie du bâtiment, et **pas** à une
fermeture du jeu. Rien n'est écrit sur le disque, rien n'entre dans `SaveSystem`, et le ET des
quatre de `TryLoad` n'est pas touché.

**On sort après chaque manche**, et c'est délibéré : enchaîner quatre manches d'office
enfermerait l'enfant dans un écran dont **aucune touche ne permet de sortir**, le jeu n'ayant
qu'Espace. Une manche, un retour dans la pièce, et on relance si l'on veut.

---

## 2. Où il se joue : **un écran modal**, sur le patron de `VillageMapScreen`

Les deux écrans existants ont été relus avant de trancher.

- `VillageMapScreen` : flèches pour choisir dans un ensemble de cases, Espace pour valider,
  `panel` éteint, garde `openedFrame`, événement `Closed`, la lecture du clavier isolée de la
  décision (`Select` et `Place` sont publiques et se vérifient **sans clavier**).
- `SpeechBox` : Espace défile puis referme, `openedFrame`, `Closed`, `Advance` séparée de la
  lecture du clavier, et le compteur statique `AnyOpen` qui arrête `GameClock`.

**Le memory est littéralement le geste de `VillageMapScreen`** — un curseur dans une grille, une
touche qui valide — avec `SpeechBox` pour les gardes et l'arrêt de l'horloge. En jouer dans la
pièce elle-même coûterait tout ceci et n'apporterait rien :

- la pièce fait 20×10 et ses 24 cases de panneaux **sont déjà la planche** ; un plateau devrait
  la recouvrir ou l'exiler ;
- une carte y ferait **16 px**, sous le plancher de 32 de CLAUDE.md, alors qu'un Canvas à 320x180
  le donne gratuitement ;
- chaque retournement demanderait de **marcher** jusqu'à la carte : deux traversées par tour, sur
  un plateau de six colonnes, pour un jeu qui en compte vingt ;
- il faudrait un plateau **construit à chaud** dans une scène que le builder Editor écrit, contre
  la règle 2 ;
- et les phases 15 et 16 sont elles aussi des écrans : *nommer parmi trois noms* et *poser sur un
  plan*. Le squelette écrit ici doit être celui-là.

**Recommandation : écran modal.**

---

## 3. Le squelette, puisque 15 et 16 le reprendront

### 3.1 `Assets/Scripts/Minigames/MiniGameScreen.cs` — la classe de base

Le dossier `Minigames/` est imposé par CLAUDE.md et vide depuis le début. Il s'ouvre ici.

Classe **abstraite**, `MonoBehaviour`, qui porte **tout ce que les trois mini-jeux partagent** :

- `panel` éteint, `Open()` / `Close()`, `IsOpen`, événement `Closed` ;
- la **garde `openedFrame`**, jumelle de celle de `PlayerInteractor.screenClosedFrame` : sans les
  deux, le même Espace est lu deux fois (PIEGES.md) ;
- la lecture des flèches **au changement de direction seulement**, `Dominant()` recopiée du patron
  des quatre directions sans diagonale ;
- `EnsureInput` / `OnEnable` / `OnDisable` / `OnDestroy`, le garde-fou de rechargement de domaine ;
- un compteur **statique `AnyOpen`**, remis à zéro par `[RuntimeInitializeOnLoadMethod]`, exactement
  comme `SpeechBox.openCount` — et `GameClock` lira `SpeechBox.AnyOpen || MiniGameScreen.AnyOpen`.
  **Sans cela une saison tourne pendant qu'on joue** : une manche de seize paires dure plusieurs
  minutes, une saison en dure dix ;
- deux abstraites, `Move(Vector2Int)` et `Validate()`, **publiques**, donc vérifiables sans clavier
  et sans dépendre du focus de l'éditeur.

### 3.2 `Assets/Scripts/Minigames/SignMemory.cs` — le memory

Il n'implémente que ce qui lui est propre : le tirage, l'état des cartes, l'appariement, la bande
de nom. `Deal(int pairs, int seed)` est **publique et déterministe** : c'est elle qui se vérifie
par le calcul, sans écran.

### 3.3 Le lancement : après la phrase, jamais à sa place

`Villager` gagne **un champ sérialisé** `miniGame` de type `MiniGameScreen`, nul pour les dix
autres personnages du jeu. `PlayerInteractor` **n'ajoute aucun `InteractionKind`** : dans
`OnSpeechClosed`, si le personnage à qui l'on vient de parler porte un mini-jeu, l'écran s'ouvre
et le personnage joueur **reste éteint**. Le Stock dit donc ses deux phrases, *puis* le jeu
commence — l'ordre que la phase 13 avait déjà écrit dans ses répliques.

Un drapeau `isPlaying` tient la relève de `isTalking` entre la fermeture de la boîte et
l'ouverture de l'écran, et `screenClosedFrame` n'est posé qu'à la fermeture **du mini-jeu**.
Aucune fenêtre d'une image où l'interacteur pourrait relancer le dialogue.

### 3.4 Ce qui est construit dans `PersistentSceneBuilder`

Le panneau du memory vit dans le HUD, comme `VillageMap` et `SpeechBox`, créé **juste après**
`CreateVillageMap` pour couvrir gouttes et indicateurs. **32 cartes sont créées d'avance** et
allumées par manche : aucune allocation en cours de jeu.

### 3.5 Art neuf : deux images, et pas une de plus

| image | pourquoi |
|---|---|
| `sign_back.png`, 16×24 | **le dos d'un panneau** : une plaque grise et son poteau. Un panneau a un dos, Victorien le sait. |
| `picto_memory_cursor.png`, 32×32 | `cursor_target` fait 16 px et flotterait au milieu d'une carte de 32. |

Le picto de sortie de fin de manche est **`picto_exit`**, celui qui sort déjà des bâtiments :
c'est le même geste, il n'y a rien à apprendre. Les deux images neuves se déclarent dans
`AreAssetsPresent`, qui barre toute construction si elles manquent. **Aucun texte neuf** : la
bande de nom réutilise les 24 images de `SignNameTexture`.

---

## 4. Les filets, et comment chacun se saborde

Tout est vérifiable **par le calcul, sans écran** — c'est la méthode du projet.

### 4.1 `ValidateMemoryBoard()` dans `InteriorsSceneBuilder`, joué à chaque construction

| ce qu'il prouve | sabotage qui doit le faire refuser |
|---|---|
| carte ≥ 32 px de côté | carte à 30 → refus, en nommant 30 et 32 |
| plateau ≤ 312 px de large | 9 colonnes → refus, en nommant 320 et 312 |
| plateau ≤ 156 px sous la bande | 5 rangées → refus, en nommant 176 et 156 |
| chaque taille de manche est paire, et divisible par 4 | une manche à 13 paires → refus |
| assez de rangs pour tirer : 4 par famille ≤ 6 | une manche à 28 paires → refus |
| chaque rang tiré est sur `SignBoard` et a un nom | — |
| le plus large nom tient à l'écran | — |

### 4.2 `Deal` se vérifie par le calcul, sur mille tirages

Pour chaque taille de manche, mille graines : exactement `2 × paires` cartes, **exactement deux
exemplaires de chaque rang**, aucun rang étranger à `SignBoard`, et **exactement `paires / 4` par
famille**. Un mélange qui perdrait une carte ou en doublerait une le dirait au premier tirage.
Sabotage : un Fisher-Yates à `i < n - 1` au lieu de `i < n`, ou un tirage par famille remplacé par
un tirage global → le compte par famille tombe.

### 4.3 `ValidateRooms` étendu sur son patron, dans les deux sens

La table des personnages déclare **quel mini-jeu** porte chacun. Le validateur exige, **case par
case et dans les deux sens**, que le mini-jeu déclaré tombe sur un personnage du plan, et qu'aucun
personnage ne porte un mini-jeu que la table ne connaît pas. **Jamais par l'ordre d'un balayage** :
`FindAll` balaye du bas vers le haut, et la pièce se lit de haut en bas. Sabotage : déclarer le
memory sur la case de La Fabrique → refus nommant la case.

### 4.4 Chaque sabotage passe par le FICHIER et une vraie recompilation

Jamais par réflexion : un champ `static readonly` remplacé par `FieldInfo.SetValue` laisse les
méthodes lire l'ancien tableau **sans un mot**, et le validateur saboté dit oui (PIEGES.md).

---

## 5. Vérification en jeu

Éditeur hors play et **réellement au premier plan** (`Application.isPlaying`,
`Application.isFocused` relus à chaque appui) ; `Application.runInBackground = true` posé **à
chaud**, jamais dans les ProjectSettings ; pilote en `DontDestroyOnLoad`, **supprimé** après ;
un pas = un **appui bref**, marche en **boucle fermée** ; jamais d'`InputSystem.Update()` à la
main, l'évènement est mis en file.

Le trajet : entrer par la porte (31, 34) → parler au Stock en (3, 4) → ses deux phrases →
**jouer une manche entière de memory jusqu'à la dernière paire** → refermer → ressortir par la
porte intérieure (9, 0) → retour en (31, 34).

- Compilation par le pont MCP : **zéro erreur, zéro warning**.
- Les six validateurs sur le monde neuf, et `ValidateRooms` qui refuse toujours sur une pièce
  sabotée.
- Captures : le memory au début, en cours (une paire trouvée, son nom dans la bande), à la fin.
- `git diff ProjectSettings/` vide.

---

## 6. Ce que la phase ne fait pas

Ni La Fabrique, ni Le Plan, ni un panneau de plus, ni un texte de plus, ni un octet de
sauvegarde, ni une ligne d'art définitif.

---

## 7. Les quatre points, tranchés le 5 septembre 2026

Posés en question plutôt que devinés, règle 7. Lio a validé les quatre recommandations telles
quelles ; elles ne se rouvrent plus.

1. **Ce qui est apparié** — **panneau ↔ panneau**, et le nom de la paire s'affiche à chaque
   trouvaille, par `SignNameTexture`. Le panneau ↔ nom est mesuré impossible (§1.1) : il
   diviserait le jeu par deux et demanderait un rendu multi-ligne, et c'est déjà le sujet de
   La Fabrique.
2. **Combien de paires** — **8, puis 12, puis 16**, en 4 × 4, 4 × 6, 4 × 8, cartes de 32 px.
   Toutes divisibles par 4 : **2, 3 puis 4 panneaux par famille**.
3. **La montée en difficulté** — **une manche par lancement**, plus grande à chaque fois,
   plafonnée à 16 paires, remise à zéro à la fermeture du jeu. On sort après chaque manche :
   le jeu n'a qu'Espace, et un écran dont on ne peut pas sortir serait un piège.
4. **La sauvegarde** — **rien**, conformément à la décision du 3 septembre. Le compteur de
   manches est un champ du composant, jamais un octet de `partie.json`, et `SaveSystem.TryLoad`
   n'est pas touché.

**Où il se joue** — **écran modal**, sur le patron de `VillageMapScreen` (§2).
