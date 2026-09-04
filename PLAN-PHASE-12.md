# Plan de la phase 12 — La grande carte

Design tranché le 4 septembre 2026, après un audit exhaustif de l'impact de l'agrandissement.
Voir CLAUDE.md pour les contraintes du projet et PROGRESS.md pour l'état d'avancement.

## Renumérotation

La phase 12 était réservée depuis le 3 septembre à **l'usine à panneaux**, en quatre phases.
Ce travail-ci prend sa place. **L'usine à panneaux devient les phases 13 à 16, l'habillage la
phase 17.** Les décisions du 3 septembre restent valables telles quelles, seul leur rang change :
l'usine passe toujours après l'arc du réseau et toujours avant l'habillage.

## Ce que l'audit a trouvé, et qui doit être réparé avant de toucher à la carte

Un audit de huit dimensions, chacune re-vérifiée adversarialement dans le code, a établi que le
projet **ne signale pas** ses propres ruptures. Les trois plus graves :

1. **`BuildAllScenes` n'examine aucun retour.** Les cinq `Build()` sont `void` ; si un validateur
   refuse, les autres scènes se construisent quand même et le journal annonce « les cinq scènes
   sont construites ». Deux cartes désalignées case pour case ne diraient rien.
2. **Aucune validation de solvabilité du plan des profondeurs.** Mesuré sur la carte actuelle :
   supprimer **une seule** porte de crête fait tomber les destinations desservables de 7 à 1, et
   les cases vivantes de 1197 à 125. `ValidateAgainstVillage` ne vérifie de la profondeur que la
   station. Le pire mode : une seule route sur sept change, et un contrôle par échantillon ne
   voit rien.
3. **La fontaine est le seul couple surface/sous-sol que personne n'apparie.** Les deux sont en
   (20, 15) par la seule discipline de la main. La phase 12 les déplace tous les deux.

Et trois affirmations **fausses** du journal, revérifiées à la main :

- **La table d'étalement des flaques est périmée depuis la phase 11.** Elle vaut 0 / 3 / 15 /
  38 / **71** / **113**, et non 73 / 118. Le labyrinthe de haies a porté le village de 200 à 252
  cases bloquantes, et l'eau ne monte pas dessus. La phase 11 affirmait que la table « reste
  valable telle quelle » : c'était faux et non revérifié.
- **La répartition des profondeurs est fausse depuis la phase 3** : 804 / 336 / 60, et non
  766 / 374 / 60.
- **Le printemps ne débouche pas.** `ApplyToSegment` ne remet que `IsFrozen` à faux ; un bouchon
  n'est effacé que par une réparation à la main. Les bouchons s'accumulent d'année en année.

**Et le trou le plus grave pour la demande de cette phase :** une route de 57 segments fausse
d'une seule case est rendue **entièrement blanche**, comme un tuyau qu'on vient de poser.
`FlowSolver.TraceToPlant` jette son ensemble de cases atteintes quand il échoue. Le retour est
binaire et arrive après 73 appuis sur Espace. C'est la seule chose que l'agrandissement dégrade
**linéairement avec la longueur des routes**, et c'est aussi ce qu'un guide devrait montrer : on
ne peut pas expliquer *où* ça casse tant que le jeu l'ignore.

## Décisions de design validées

1. **La carte passe de 40x30 à 64x45**, le plafond dur au-delà duquel il faudrait ensemble
   relever `maxTextureSize`, calculer l'échelle du plan du HUD et redimensionner curseur et
   marqueurs. À 64x45 on prend tout ce qui est gratuit et rien de plus. 2880 cases, 3,2 x 4,0
   écrans, 24 secondes de traversée.
2. **UN SEUL labyrinthe de haies, plus grand qu'un écran.** Il ne se résout donc pas d'un coup
   d'œil mais de mémoire, et c'est le point : un labyrinthe qui tient dans le cadre n'est pas un
   labyrinthe. La caméra défile dedans comme partout ailleurs.
3. **N destinations, et la station s'agrandit EN JEU.** La règle exacte, dérivée puis confirmée
   par simulation sur six années : sur l'année, l'arrivant vaut `4·D + 11` et la station traite
   `4·C`, donc **`C = D + 3`** est le minimum entier. La capacité cesse d'être un nombre écrit à
   la main : elle devient une chose que le joueur construit.
4. **Huit guides, un par leçon, sans mémoire.** Le but, creuser, poser, **la profondeur**, la
   bouche et l'échelle, les saisons, réparer, le bassin. Réexpliquer à chaque lancement ne punit
   rien et ne coûte aucun champ de sauvegarde.

**La difficulté est une qualité.** Ces quatre décisions vont toutes dans le sens de plus long,
plus grand, plus exigeant. C'est voulu : un jeu qui ne demande ni réflexion ni temps est délaissé
en cinq minutes.

## 1. La carte, 64x45

Le sens de l'agrandissement n'est pas neutre. `At` fait `Rows[Height - 1 - y][x]` : **ajouter les
lignes en tête des tableaux et les caractères en fin de ligne** préserve rigoureusement chaque
couple (x, y) existant, et se relit d'un diff. C'est ce que l'on fait, mais la phase 12 réécrit
de toute façon les deux plans en entier — le village pour ses routes et son labyrinthe, le
sous-sol pour ses profondeurs.

**La sauvegarde monte à `CurrentVersion = 2.`** C'est la première fois depuis la phase 6, et le
critère est celui que la phase 6 a écrit elle-même : « un champ ajouté se relit sans rien casser ;
un champ dont le **sens** change, non. » Les cases d'une partie de la phase 11 ne désignent plus
la même chose sur une carte de 64x45. Une vieille partie est donc mise de côté avec un horodatage
et le jeu repart neuf — bruyamment, et non en rejouant des gestes devenus absurdes.

**Le pré-creusement est du contenu, pas de la mise à l'échelle.** Aujourd'hui 88 cases livrées
ouvertes sur 1200 font tomber la pire maison de 73 à 17 appuis marginaux. Tenir le même ratio sur
2880 cases demande d'en tracer environ 210.

## 2. La station qui s'agrandit

`TreatmentPlant` porte `baseCapacity` et un nombre de **bassins de traitement ajoutés**. La
capacité effective vaut `baseCapacity + bassins`, plafonnée à `D + 3`.

- **Espace sur l'arrivée de la station, sous terre**, ajoute un bassin. Le nœud `PlantInlet`
  n'offre aujourd'hui aucune action tant qu'il n'est pas abîmé : le créneau est libre.
- La station **grossit à l'écran** d'un bassin à chaque agrandissement : le progrès se voit.
- **Rien ne se paie.** Il n'y a pas de monnaie dans ce jeu et il n'y en aura pas. La difficulté
  est de **comprendre** qu'il faut agrandir, pas d'amasser de quoi le faire.
- **C'est le débordement qui l'enseigne**, et cela répare un défaut trouvé par l'audit : à
  capacité écrite à la main, une fois le bassin relié `Lost` vaut zéro **pour toujours** et le
  débordement n'apprend plus rien. Avec une station qui commence trop petite, il redevient un
  retour permanent.
- **Le nombre de bassins est sauvegardé** : c'est du progrès de joueur, pas une donnée dérivée.

## 3. Les huit leçons

`grep "Depth"` dans toute l'UI et tout le code du joueur ne rend **rien** : la règle de
profondeur, qui *est* le puzzle selon CLAUDE.md, n'est affichée nulle part. Le jeu entier compte
sept phrases, toutes derrière les portes des deux boutiques.

| # | Leçon | Découvrable aujourd'hui ? | Où le guide se tient |
|---|---|---|---|
| 1 | Le but : relier les maisons | non | au départ du village |
| 2 | On descend par une bouche | par le picto seul | près d'une bouche |
| 3 | On creuse la terre | par le picto seul | sous terre, devant la terre pleine |
| 4 | On pose un tuyau | par le picto seul | sous terre, devant une galerie |
| 5 | **L'eau ne remonte jamais** | **non** | sous terre, devant une crête |
| 6 | Les saisons abîment | non | en surface, à l'automne ou l'hiver |
| 7 | Réparer, et où ça fuit | par la flaque seule | près d'une flaque |
| 8 | Le bassin encaisse l'orage | non | près de la chambre du bassin |

Chaque guide est un `Villager` posté sur une case **validée par calcul** (il bloque, donc il ne
doit fermer aucun passage), qui ne parle que si sa condition est vraie. Il se tait sans
disparaître quand sa leçon est acquise : `CanSpeak` est déjà `LineCount > 0`, vider les lignes
suffit.

**La leçon 5 a besoin que le solveur parle.** Publier la frontière atteinte — l'ensemble
`visited` que `TraceToPlant` jette déjà — donne « l'eau monte jusqu'ici, et pas plus loin »,
c'est-à-dire l'endroit exact où la règle de profondeur casse. C'est la moitié du travail de
`FlowSolver`, et c'est ce qui rend la leçon centrale enseignable.

## 4. Découpage

Cinq sous-phases, chacune finissant sur un résumé et une validation, règle 1.

### 12a — Les filets
Aucun contenu neuf. Rendre **bruyante** toute rupture que 12b à 12e pourraient causer, et réparer
ce que l'audit a trouvé.
- `BuildAllScenes` : les cinq `Build()` rendent `bool`, la chaîne s'arrête au premier refus, le
  message de succès devient conditionnel.
- `ValidateDepthPuzzle` : parcours inverse depuis la station à profondeur non décroissante,
  refus si une destination n'est pas atteinte, journal des cases mortes et des portes de crête.
- `ValidateWaterBudget` : refuse si `4·D + R > 4·C`, en annonçant la capacité attendue ; vérifie
  aussi que le nombre de gouttes du HUD vaut `D`.
- `ValidateAgainstVillage` apparie enfin la **fontaine**.
- `ValidatePark` réécrit : entrées **dérivées du plan** et non codées en dur, connexité complète.
- `PlayerController.Teleport` : garde `Contains`.
- **Une seule liste de blocage** : `PaintVillage` interroge `VillageLayout`, au lieu d'un second
  `switch` maintenu à la main.
- `PixelFont` : refuser toute phrase trop large pour son plafond de texture, et ajouter les
  glyphes manquants — chiffres, `-`, `!`, `?`, `Ç`, `Ô Î Û Ù`.
- `FlowSolver` publie sa **frontière atteinte**.
- `SaveData.CurrentVersion` passe à **2**.
- Corriger le journal : table d'étalement, répartition des profondeurs, le printemps qui ne
  débouche pas, et les commentaires périmés relevés par l'audit.

### 12b — La carte 64x45 et le grand labyrinthe
Les deux plans réécrits, les profondeurs redessinées, le labyrinthe unique tracé et **vérifié
solvable par calcul avant d'être posé**, le pré-creusement au ratio, les bouches à densité
constante, les destinations posées. Tout est repris par les validateurs de 12a.

### 12c — Le décor
Routes, arbres, panneaux de circulation, fontaine agrandie. Chaque caractère neuf du plan demande
cinq éditions cohérentes. **Séparer d'abord `tile_fountain.png` en deux images**, sur le patron
`house.png` / `tile_house.png` : aujourd'hui la tuile bloquante et le sprite sortent du même
fichier, ce qui cloue la fontaine à 16x16. Et engendrer **seize tuiles de haie à masque de
raccord**, sur le patron exact de `BuildPipe` : sans quoi un grand labyrinthe est trois cents
carrés verts identiques.

### 12d — La station qui s'agrandit
`TreatmentPlant` à capacité variable, l'action sous terre, la croissance visible, le champ de
sauvegarde, et le plafond `D + 3`.

### 12e — Les huit guides
`ValidateGuidePosts`, un marqueur de poste au plan, le composant de conditions, le second
renderer d'attention, la garde sur `SpeechBox.IsOpen`, et les phrases des huit leçons.

## 5. Ce que l'on ne fait pas

- **Pas de son.** `Assets/Audio/` est le seul dossier de l'arborescence imposée resté vide, il
  n'y a pas un `AudioSource` dans le projet, et le mot n'apparaît pas une fois dans le journal.
  Avec « le moins de texte possible », le son est le canal non textuel évident. **Question
  ouverte assumée**, et l'ajouter plus tard obligera à repasser sur chaque geste déjà écrit.
- **Pas de mémoire des leçons.** Décision 4.
- **Pas de déplacement rapide entre bouches.** À 64x45 la traversée fait 24 secondes ; les trois
  pièces existent (`VillageMapScreen`, `ManholePortal.Find`, `TravelAsync`) si cela se révèle
  nécessaire.
- **Pas de guides mobiles.** Ils sont postés, sur des cases validées.
- **Aucun changement à l'ordre du tick.** La question ouverte de la phase 8 reste ouverte.
