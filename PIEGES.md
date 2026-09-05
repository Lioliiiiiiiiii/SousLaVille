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

**`runInBackground` se repose à chaque session de play.** Il ne survit pas à l'arrêt. Une
nouvelle session lancée sans focus reste figée à l'image 1 : Boot ne charge même pas Persistent.
À poser **à chaud**, jamais dans les ProjectSettings, et à vérifier par `git status`.

**Le générateur de scènes refuse en play mode, le générateur d'art non.**
`InvalidOperationException: This cannot be used during play mode`. Vérifier
`EditorApplication.isPlaying` avant les menus, et pas seulement se souvenir d'avoir arrêté.

**Un rechargement de domaine en plein play vide `GameManager.Instance`.** L'instance est posée
dans `Awake`, qu'Unity ne rappelle pas après un rechargement. Relancer le play plutôt que
chercher un bug. (Le poser dans `OnEnable` le réglerait ; question ouverte depuis la phase 7.)

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

**`Image.SetNativeSize` n'a rien à faire dans ce HUD.** Il divise la largeur du sprite par ses
pixels par unité (16 ici) puis la multiplie par les 100 du Canvas : une phrase de 109 pixels
sortait à 681, deux fois l'écran. Poser `sizeDelta` depuis `sprite.rect`.

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
deux mesures de la phase 12b.

**Vérifier la solvabilité PAR CALCUL avant d'écrire un plan.** Les crêtes de la phase 4, le
labyrinthe de la phase 11, l'alcôve de la fontaine : chacun a été calculé avant d'être posé.
C'est la méthode du projet.

**Un validateur qui dit toujours oui ne vaut rien.** Le vérifier **par sabotage** : casser le
monde exprès, reconstruire, et exiger qu'il refuse en nommant la case et la raison.

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

**Une donnée dérivée sauvegardée est une donnée qui peut mentir.** Les maisons desservies, l'eau
du village, l'état de la fontaine : tout se recalcule. On sauvegarde des **gestes**, pas un état.

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
