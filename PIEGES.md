# Les pièges de ce projet

Chacun est tombé **au moins une fois** ici. Ils sont consignés au fil des phases dans les
« notes d'atelier » de PROGRESS.md ; cette liste les rassemble pour qu'une nouvelle session n'ait
pas à les redécouvrir. À relire avant d'écrire, et à compléter quand un nouveau tombe.

## Unity et l'éditeur

**L'éditeur doit être RÉELLEMENT au premier plan pour l'injection clavier.**
`Application.runInBackground = true` fait tourner la boucle de jeu sans focus, mais **ne suffit
pas** : le New Input System laisse la touche enfoncée sur le périphérique et n'en informe jamais
les actions. Vérifier `Application.isFocused` avant de conclure quoi que ce soit d'un test
d'entrée. Une autre application peut reprendre le focus entre deux appels du pont.

**Et le focus perdu RESSEMBLE EXACTEMENT à un bug du jeu.** En phase 14, le pilote a échoué sur
« appui sans effet sur le curseur » : la marche, la porte et le dialogue avaient tous réussi, et
seul le mini-jeu ne répondait pas — le symptôme parfait d'un bug dans le code neuf. Deux
corrections, et les deux comptent. Le pilote **attend le focus** au lieu de compter les coups
perdus : un appui injecté sans focus n'arrive nulle part, et le compter ne sert qu'à expliquer
l'échec après coup. Et le pilote **prend ses captures lui-même** : s'arrêter à chaque étape pour
laisser l'opérateur capturer multiplie les allers-retours, et chacun est une occasion pour une
autre application de reprendre le premier plan au milieu d'une manche.

**Un pilote accroché par `[InitializeOnLoadMethod]` ne tourne que si l'entrée en play recharge le
domaine.** Sinon la méthode ne repasse pas, le pilote reste armé dans `SessionState` et rien ne
s'exécute : play en cours, `runInBackground` faux, aucune capture, aucun log. S'accrocher à
`EditorApplication.update` **aussi dans la commande de menu** qui lance le play. Tombé en 18b.

**`runInBackground` se repose à chaque session de play.** Il ne survit pas à l'arrêt. Une
nouvelle session lancée sans focus reste figée à l'image 1 : Boot ne charge même pas Persistent.
À poser **à chaud**, jamais dans les ProjectSettings, et à vérifier par `git status`.

**Le générateur de scènes refuse en play mode, le générateur d'art non.**
`InvalidOperationException: This cannot be used during play mode`. Vérifier
`EditorApplication.isPlaying` avant les menus, et pas seulement se souvenir d'avoir arrêté.

**Un rechargement de domaine en plein play vide `GameManager.Instance`.** L'instance est posée
dans `Awake`, qu'Unity ne rappelle pas après un rechargement. Relancer le play plutôt que
chercher un bug. (Le poser dans `OnEnable` le réglerait ; question ouverte depuis la phase 7.)

**Un `const` comparé à un `const` fait passer un filet pour du code mort.** `ValidateMemoryBoard`
commençait par `if (MemoryCardSize < 32f)`, deux constantes : le compilateur replie la
comparaison, voit le corps comme inatteignable et sort **CS0162**. Et la tentation est alors de
supprimer la garde pour faire taire l'avertissement, c'est-à-dire de supprimer le filet **parce
qu'il passe**. Un réglage n'est pas une constante de compilation : `static readonly`, et le
plancher devient une constante nommée à part. Tombé en phase 14.

**Un champ `static readonly` ne se sabote pas par réflexion.** `FieldInfo.SetValue` remplace bien
la valeur — `ReferenceEquals` le confirme — mais le runtime a figé la référence à l'initialisation
et les méthodes de la classe lisent toujours l'ancien tableau, **sans un mot**. Un validateur
saboté ainsi dit oui et on le croit. Le sabotage passe donc par le fichier et une vraie
recompilation, jamais par la réflexion.

**Relâcher une flèche à l'arrivée fait dépasser d'une case.** `currentCell` ne change qu'à
l'arrivée alors que le personnage bouge en continu : relâcher à ce moment laisse la touche tenue
une image de trop et le pas suivant est déjà engagé. Dans un couloir cela se rattrape ; **sur la
case d'arrivée le pilote oscille autour d'elle indéfiniment**. Relâcher à une demi-case du centre.

**Une capture d'écran produit deux erreurs `memoryless`.** Elles viennent du chemin
`ScreenCapture` d'URP sur Metal, pas du jeu. Vérifié en isolant.

**Le premier appel après l'entrée en play lève souvent une `NullReferenceException`** : à
l'image 1, les scènes ne sont pas chargées et `GameManager.Instance` est nul. Rappeler.

**`execute_code` du pont MCP n'accepte pas de directives `using`** (le code est un corps de
méthode) et interdit `Date.now`, `Math.random`. Les textures importées ne sont **pas lisibles** :
relire le PNG sur le disque avec `LoadImage`.

## Architecture des couches

**Persistent est chargée AVANT les couches de jeu.** Tout service qui cherche un objet d'une
couche doit **retenter tant qu'il échoue**, jamais résoudre une seule fois au `Start`. Tombé en
phase 1 (la carte du joueur), phase 4 (le solveur), phase 5 (l'ambiance).

**Un composant d'une couche de jeu s'abonne dans `OnEnable`, se désabonne dans `OnDisable`, et
se RÉAPPLIQUE au rallumage.** Le `SceneRouter` éteint la couche inactive : ce qui s'est passé
pendant l'extinction doit se rattraper au réveil.

**Et il éteint TOUS les objets racines de cette scène, pas seulement sa racine nommée.**
`SetLayerEnabled` parcourt `scene.GetRootGameObjects()` et éteint chacun d'eux. Un objet posé dans
la scène Surface s'éteint donc en descendant, et sa coroutine s'arrête **sans un mot**. Ce qui doit
survivre à un changement de couche appartient à Persistent, ou passe par `DontDestroyOnLoad`.

**Une référence sérialisée ne traverse pas deux scènes.** Unity ne sérialise pas une référence
d'un objet d'une scène vers un objet d'une autre : le champ sort **nul, sans un mot**, et rien à
la construction ne le signale. Le personnage vit dans `Interiors`, son mini-jeu et sa boîte de
dialogue dans `Persistent` : ce qui les relie est un **identifiant sérialisé** — un rang d'enum —
plus un registre statique et une résolution paresseuse. Vu en phase 9a pour `SpeechBox`, écrit
comme règle en phase 14 pour `MiniGameScreen`.

**Chercher un objet d'une couche éteinte demande `FindObjectsInactive.Include`.** Sans lui,
`FindAnyObjectByType` ne le voit pas. C'est le cas de l'usine à tuyaux, consultée depuis le
sous-sol, et de l'atelier, consulté depuis la surface.

**Ne jamais gager sur l'ordre de chargement des scènes.** `SaveSystem` supposait « si le réseau
répond, l'atelier existe déjà » : vrai jusqu'à ce que l'atelier déménage dans une scène chargée
plus tard. Aucune plaque n'était alors ni écrite ni relue, **en silence**. Une garde qui repose
sur l'ordre de chargement ment le jour où une scène change de rang.

**Une `Light2D` globale porte sur des Sorting Layers, pas sur une zone.** Deux lumières globales
sur un même layer font hurler URP à chaque chargement. Chaque couche a sa famille de cinq
Sorting Layers et sa lumière cantonnée à eux. Un sprite laissé sur `Default` n'est éclairé par
aucune et apparaît **noir sans le moindre message**.

## Entrées et interface

**Deux composants qui lisent la même touche ont besoin d'une garde de CHAQUE côté.** L'un retient
l'image où il s'est ouvert, l'autre celle où il s'est fermé. Sans les deux, le même Espace est lu
deux fois et l'écran se rouvre à peine refermé. L'ordre des `Update` n'est garanti par rien.

**Une touche TENUE et une touche TAPÉE ne se valent pas, et seule la seconde fait un front.**
Un pilote qui appelle `InputSystem.Update()` à la main consomme le front dans une passe qui
n'est pas celle du jeu : les flèches marchent quand même — elles se lisent en continu — mais
`WasPressedThisFrame` ne voit jamais rien, donc **Espace ne fait rien** pendant que le
personnage marche parfaitement. Mettre l'évènement **en file** et laisser la boucle normale le
traiter. Tombé en phase 13.

**Tenir une flèche dépasse d'une case environ un pas sur dix**, même relâchée à une demi-case du
but. Assez rare pour qu'un trajet court réussisse, assez fréquent pour qu'un trajet long échoue.
Un pas de pilote se fait par un **appui bref** : le contrôleur engage le pas et le mène au bout
tout seul, la touche relâchée n'en engage pas de second. Et la marche se fait **en boucle
fermée**, en relisant la case où l'on est, jamais en comptant les pas.

**LA RANGÉE DU HAUT D'UNE PIÈCE EST INUTILISABLE, ET ON Y RETOMBE.** Tombé en phase 13 avec la
première famille de la planche, consigné ici, **relu au début de la phase 17 — et refait en 17g**
avec les trois meubles, adossés au mur du fond. Sur trois, un seul se devinait. « Adosser un
meuble au mur du fond » est un réflexe de dessin, pas une décision qu'on prend en consultant une
liste : c'est pour cela qu'une liste ne suffit pas et qu'il faut **regarder**. **Retombé une
troisième fois en 18a**, dans une planche d'essai composée à la main : le bouquet d'arbres — ce
que la planche devait montrer d'abord — sous la boîte de gauche du HUD. Une planche qu'on compose
est un écran comme les autres : ses deux rangées du haut appartiennent au HUD.

**Une pièce d'intérieur fait dix lignes quand la caméra en montre 11,25.** La pièce est donc
centrée, et sa rangée du haut tombe **derrière la rangée de gouttes du HUD**. La première famille
de la planche de l'usine y était à moitié cachée ; tout le tableau est descendu d'une rangée.
Vu à l'écran, pas déduit.

**`Image.SetNativeSize` n'a rien à faire dans ce HUD.** Il divise la largeur du sprite par ses
pixels par unité (16 ici) puis la multiplie par les 100 du Canvas : une phrase de 109 pixels
sortait à 681, deux fois l'écran. Poser `sizeDelta` depuis `sprite.rect`.

**Tout écran qui dure doit arrêter l'horloge, et pas seulement la boîte de dialogue.** Une
saison dure dix minutes ; une manche de seize paires en dure plusieurs. `GameClock` ne
connaissait que `SpeechBox.AnyOpen` : un tick serait tombé au milieu d'une partie et aurait gelé
le village pendant que l'enfant joue à autre chose, **dans une pièce que les saisons ne touchent
même pas**. Chaque famille d'écrans porte donc son compteur statique, remis à zéro par
`[RuntimeInitializeOnLoadMethod]` — sans quoi un écran laissé ouvert à l'arrêt fige l'horloge de
la session suivante.

**Ce qui déborde d'une case déborde de l'écran à la rangée du haut.** Un sprite au pivot du
joueur dépasse de sa case vers le haut, et dans le monde la caméra suit. Dans un écran modal, la
rangée haute d'une grille qui remplit l'écran n'a rien au-dessus d'elle : le cédez du plan 12
sortait coupé en deux. La géométrie des CASES était juste et validée ; celle de ce qui se DRESSE
dessus ne l'était pas, et seule une capture l'a dit. Compter le débordement dans le validateur,
et regarder le plan le plus grand, pas le premier. Tombé en phase 16.

**Deux commandes de menu longues dans le même tour décrochent le pont MCP.** « plugin session
disconnected while awaiting command_result » : la commande s'exécute quand même jusqu'au bout,
mais sa réponse est perdue, et on ne le sait qu'en relisant la console. Un menu long à la fois.
Tombé trois fois en phase 16.

**Le picto d'action se pose au-dessus de la tête du JOUEUR** — sauf la bulle « on peut lui
parler », qui est au-dessus de celle du **personnage**. La place habituelle tombait exactement
sur son visage et l'effaçait. Un picto qui cache ce qu'il désigne ne désigne rien.

## Plans ASCII et données

**Un plan ASCII se modifie par INDICE DE LIGNE, jamais par contenu.** Deux lignes d'une carte
peuvent être identiques au caractère près, et `string.Replace(..., 1)` touche la première
rencontrée. L'alcôve de la fontaine s'est posée une case trop haut comme ça.

**Un plan ASCII dessiné à la main se fragmente sans le dire.** Le labyrinthe de la phase 12b,
dessiné caractère par caractère sur un treillis pourtant régulier, est sorti en **seize
composantes connexes** ; rien dans le dessin ne le montrait. Le creuser en **polylignes**, chacune
devant toucher le tracé déjà posé sous peine d'assertion, rend la connexité structurelle : une
poche morte n'est plus dessinable par inadvertance.

**La table d'étalement des flaques dépend du nombre de BOUCHES autant que des cases bloquantes.**
Elle a été fausse deux fois, chaque fois pour avoir été reportée d'une phase à l'autre sans être
revérifiée. Toute phase qui touche à la carte la recalcule, sur le plan **réellement écrit** et
non sur celui qu'on croit avoir écrit — six cases bloquantes d'écart se sont glissées ainsi entre
deux mesures de la phase 12b. Elle a été fausse une **troisième** fois : la phase 12c a écrit
« la table est inchangée jusqu'à Lost = 5 » après avoir planté 126 arbres, sans la reprendre.
Déclarer une table inchangée est encore la reporter sans la vérifier.

**Vérifier la solvabilité PAR CALCUL avant d'écrire un plan.** Les crêtes de la phase 4, le
labyrinthe de la phase 11, l'alcôve de la fontaine : chacun a été calculé avant d'être posé.
C'est la méthode du projet.

**Un validateur qui dit toujours oui ne vaut rien.** Le vérifier **par sabotage** : casser le
monde exprès, reconstruire, et exiger qu'il refuse en nommant la case et la raison.

**Dans une palette courte, la nuance d'une couleur EST la couleur d'autre chose.**
`Palette.Shade(Brick)` vaut `WoodDark` : le tablier du Stock, teint par réflexe en nuance de son
corps, est sorti du même brun que la caisse qu'il porte, et la couleur du corps — ce qui distingue
les cinq personnages de loin — a disparu. Un vêtement se colore par **ce qu'il est**, jamais par
une opération sur la couleur d'à côté. Tombé en phase 17d.

**Une planche agrandie montre les défauts, elle en invente aussi.** Lu trois fois « le haut du
crâne est crénelé » sur une planche à huit fois : c'était la rangée des yeux, sept rangées plus
bas. Quand la planche contredit le code, **c'est le dump des pixels qui tranche**, pas l'œil.

**Agrandir un sprite au gabarit de ce qui le cache ne le montre pas.** L'échelle disparaissait
sous le joueur ; portée à 16x24 avec le même pivot que lui, elle est restée **exactement**
recouverte — deux rectangles identiques au même endroit. Ce qui rend visible n'est pas la taille
mais **l'endroit où l'on dépasse** : les montants aux colonnes 1 et 14, hors du corps qui occupe
le milieu. Le premier essai compilait, passait les deux validateurs, et ne changeait rien à
l'écran. Tombé en phase 17c.

**Retirer un défaut de style à un endroit le rend visible partout où il restait.** Chaque sol du
jeu était un aplat bordé d'un liseré : cent fois de suite, cela dessine une grille. L'herbe
texturée de la phase 17b a réglé la grille — et révélé d'un coup que les 126 arbres se dressaient
chacun dans une **boîte noire**, leur tuile de sol étant restée un carré bordé. Aucun filet ne
pouvait le dire : ce sont de bonnes couleurs et des images distinctes. **Après chaque changement
de style, regarder ce qui n'a PAS changé.**

**Une palette fond ce que le jeu distinguait, et RIEN ne le dit.** Le joueur portait `#E05A2B`,
Le Stock `#D07A2E` : deux oranges distincts, choisis à deux phases d'écart, que la palette de
trente-quatre a réunis sur la même couleur. Ni la compilation, ni le contrôle de palette — ce sont
de bonnes couleurs —, ni la comparaison des images entre elles ne pouvaient l'attraper : les deux
sprites diffèrent par le repère de direction. Seule une capture l'a montré. **Une contrainte qui
porte sur un ENSEMBLE — six personnages doivent porter six couleurs — ne se vérifie pas en
regardant ses membres un par un** : elle demande sa propre table et son propre filet. Tombé en
phase 17a.

**Une couleur calculée échappe par construction à tout contrôle de palette.** `Darken(couleur,
facteur)` multipliait les canaux : le résultat n'était dans aucune table et personne ne l'avait
choisi. Une nuance se DÉCLARE — `Palette.Shade` rend une autre couleur de la palette, et refuse en
nommant celle dont la nuance manque.

**Un appariement par l'ORDRE d'un balayage ment en silence.** Huit guides appariés à leur leçon
par l'ordre de `FindAll` : un poste déplacé d'une case change de leçon sans un mot. Écrire
l'appariement case par case, et refuser ce que la table ne connaît pas.

**Un Sorting Layer supérieur recouvre tout, quel que soit l'ordre de tri.** La goutte d'une
maison vit sur `Surface_Overlay` et effaçait le signal d'attention d'un guide posé juste dessous,
en ordre 11. L'ordre ne départage que DANS un layer.

**Sous terre, une case de personnage devient increusable en silence.** Le test du `Villager`
passe avant `Dig` et `PlacePipe` dans `Evaluate`. Un personnage posé dans une galerie stérilise
sa case. Les guides du sous-sol se tiennent donc dans des culs-de-sac d'une case : une case de
degré un ne peut être l'intermédiaire d'aucun chemin.

**Un plan qui peint dans l'ordre recouvre en silence.** Routes, puis labyrinthe, puis station,
puis arbres : chaque couche efface ce qu'elle recouvre sans un mot. Une rue a traversé l'enceinte
de la station, deux rues ont fini dans une façade, quatre arbres ont poussé sur des murs. Chaque
couche doit **vérifier qu'elle ne recouvre que de l'herbe**, et le graphe des routes doit être
d'un seul tenant — `ValidateRoads` depuis le 5 septembre.

**Un placement à la main de ce qui se dérive d'un graphe finit faux.** Les panneaux posés à la
main étaient sur la chaussée, typés par parité, et imaginaires pour deux d'entre eux. Dérivés
des routes par `RoadSigns`, ils ne peuvent plus l'être ; et c'est cette dérivation qui a trouvé
les défauts du tracé.

**En espace de texture, y MONTE.** Le poteau d'un panneau occupe le bas, la plaque le haut : une
base large en bas fait donc une **pointe en haut**. `BuildSign` dessinait le cédez le passage
AB3a pointe en haut sous un commentaire qui promettait « la pointe EN BAS » — les onze cédez du
village étaient des triangles de danger, et c'est le commentaire qu'on relisait. Un dessin de
panneau se **regarde** avant d'être cru : une planche agrandie huit fois a montré six autres
dessins ratés que le code ne pouvait pas signaler.

**Un `default:` qui dessine quelque chose de valide est pire qu'une erreur.** Celui de
`BuildSign` dessinait un panonceau de jalonnement : porter `SignCount` sans écrire les cas aurait
sorti vingt flèches bleues identiques à la place de vingt panneaux, sans un mot. Même famille que
le `default: return cell` de `GroundAt`. Un `default:` de table de dessin doit **refuser en
nommant le rang**.

**`FindAll` balaye du BAS vers le haut, et une planche se lit de haut en bas.** S'y fier pour
apparier vingt-quatre panneaux à leurs noms aurait donné la rangée OBLIGATION à la famille
INTERSECTION. Écrire les cases à la main, dans l'ordre de lecture, et vérifier l'appariement
**dans les deux sens** — une case sans table, une table sans case.

**Une donnée dérivée sauvegardée est une donnée qui peut mentir.** Les maisons desservies, l'eau
du village, l'état de la fontaine : tout se recalcule. On sauvegarde des **gestes**, pas un état.

**Un arbre de deux cases couvre toute la case du nord.** Depuis 18c, ce qui se tient juste au
nord d'un arbre disparaît sous sa cime : le guide du but en (12, 16) n'avait plus que la tête.
`ValidateDecor` refuse désormais tout marqueur qu'on doit voir ou toucher au nord d'un arbre.
Plus largement : **un sprite qui grandit recouvre ce que le plan mettait à côté**, et seul l'écran
le montre — le plan, lui, n'a pas changé.

**Une suppression par script qui cherche « le résumé d'avant » remonte trop haut** si la méthode
visée n'a pas de résumé : la coupe de `BuildHedge` a emporté `BuildPlantBasin`. Relire le diff de
la suppression ligne à ligne avant de compiler.

**`PlayerController.Teleport` ne regarde pas si la case est praticable.** Un pilote qui vise la
case d'un bloquant pour le photographier pose le joueur dessus, et le joueur le cache : la capture
de la fontaine de 18d montrait le joueur. Viser la case voisine.

**Une `Image` en `Sliced` a le même piège que `SetNativeSize`** : la bordure du sprite est divisée
par ses pixels par unité, seize, puis multipliée par les cent du Canvas. Une bordure de six pixels
en faisait trente-sept et la boîte du HUD n'était plus que deux coins. `pixelsPerUnitMultiplier =
100 / 16` sur chaque boîte, phase 18f.

**Construire les scènes pendant un play** finit en `InvalidOperationException: This cannot be used
during play mode` au fond d'une pile de dix appels. `BuildAllScenes` le refuse désormais en clair.
Un pilote qui attend le focus laisse le play ouvert tant qu'Unity n'est pas devant : `osascript …
activate` ne suffit pas toujours, `System Events … set frontmost` si.

**Un filet peut se saboter sans toucher aux assets.** `Palette.All` est un tableau `static
readonly` : ses cases se réécrivent en mémoire depuis `execute_code`, le temps d'un appel à
`ValidatePalette`, puis se remettent. Pas de compilation, pas de fichier déplacé, pas de
génération à refaire — deux minutes de moins que le sabotage par édition de source.

**`maxTextureSize` réduit une image de moitié EN SILENCE.** Depuis la phase 12a il est calculé
depuis l'en-tête du PNG ; ne pas le réécrire à la main.

**`PixelFont` ne dessine rien pour un caractère inconnu, mais avance quand même d'une cellule** :
le mot sort troué, sans un mot. `CanRender` le refuse depuis la phase 12a.

**Une tuile ignore `SetColor` sans `SetTileFlags(TileFlags.None)`**, posé par défaut sur
`LockColor`. En silence.

**Newtonsoft n'écrit pas un `Vector2Int` proprement** : il y ajoute `magnitude` et
`sqrMagnitude`, qu'il ne sait pas relire. Les cases s'écrivent en deux entiers explicites.

## Règles du projet faciles à oublier

**Règle 9, les références d'assembly : vérifier, pas supposer.** URP découpe son runtime en deux
assemblies — `Light2D` et `PixelPerfectCamera` sont dans
`Unity.RenderPipelines.Universal.2D.Runtime`, pas dans `...Universal.Runtime`.

**Ne jamais toucher aux ProjectSettings.** `git diff ProjectSettings/` doit rester vide, sauf
pour les deux mécanismes voulus : les Sorting Layers (TagManager) et les scènes au build. La
seule autre modification de tout le projet est `companyName`, en phase 6, volontaire.

**Un ajout à un `enum` sérialisé se fait À LA FIN**, jamais par insertion : les scènes le
sérialisent par son rang. Vrai pour `NodeType`, pour `GameLayer`.

**Ne pas rabaisser la difficulté « parce qu'il a six ans ».** Les contraintes de CLAUDE.md
portent sur les **commandes** et sur le **texte**, jamais sur la difficulté intellectuelle. Un
jeu qui ne demande ni réflexion ni temps est délaissé en cinq minutes.
