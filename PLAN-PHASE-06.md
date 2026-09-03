# Plan de la phase 6 — La sauvegarde

À valider. Voir CLAUDE.md pour les contraintes du projet et PROGRESS.md pour l'état
d'avancement.

## Contexte

Phase 5 committée (`b5b9aa0`). Le terrain est prêt, et il l'est en grande partie parce que la
phase 3 avait vu venir celle-ci :

- `PipeNode` et `PipeSegment` sont `[Serializable]` depuis la phase 3 et portent déjà
  `condition`, `isFrozen` et `isClogged`. **Aucun champ n'aura à être ajouté après coup.**
- `UndergroundMap.Dig(cell)` est **la seule mutation du terrain**. Rejouer la liste des cases
  creusées reconstitue exactement la carte.
- `PipeNetwork.PlacePipe(cell)` raccorde tout seul aux voisins. Rejouer la liste des cases
  posées reconstitue exactement le graphe, segments compris.
- `GameClock.SeasonProgress` a un accesseur en écriture, posé en phase 5 pour cette phase-ci.
- `SeasonSystem.CurrentIndex` donne le rang de la saison dans le cycle.
- Newtonsoft 3.2.2 est déjà dans le projet et déjà déclaré dans
  `SousLaVille.Runtime.asmdef`. **Aucun package à ajouter.**

**Critère de fin : je creuse, je pose, l'hiver gèle mes tuyaux. Je ferme le jeu. Je le
rouvre : tout est là où je l'avais laissé, et je n'ai rien fait pour ça.**

## Décisions de design validées

1. **`companyName` devient `Lio`.** Les parties vivront dans
   `~/Library/Application Support/Lio/SousLaVille/`. Une ligne de `ProjectSettings`, posée
   **avant** la première sauvegarde écrite : la changer ensuite déplacerait les parties de
   Victorien.
2. **On sauvegarde à chaque geste, par écritures groupées.** Creuser, poser, réparer, enlever
   et changer de saison marquent la partie à sauver ; l'écriture part au plus une fois toutes
   les deux secondes, plus une dernière à la fermeture. Au pire deux secondes de perdues.
3. **Au redémarrage, Victorien repart toujours au départ du village**, case `X`, en surface.
   Sa position et sa couche ne sont donc pas sauvegardées du tout : ce sont des données
   mortes, et on n'écrit pas ce qu'on ne relit pas. La note de la phase 2, « on démarre
   toujours en surface », cesse d'être provisoire et devient la règle.

## 1. `Scripts/Core/SaveData.cs` (créé)

Les classes de données, et rien d'autre. Aucun `MonoBehaviour`, aucune logique : c'est la
forme du fichier sur le disque, elle doit se lire d'un coup d'œil.

```
SaveData
  int        version          1 pour l'instant
  int        seasonIndex      rang de la saison dans le cycle
  float      seasonProgress   0 à 1 dans la saison en cours
  SaveCell[] dugCells         les cases creusées par le joueur
  SaveCell[] pipeCells        les cases où il a posé un tuyau
  SaveSegment[] segments      a, b, condition, isFrozen, isClogged

SaveCell     int x, int y
SaveSegment  SaveCell a, SaveCell b, float condition, bool isFrozen, bool isClogged
```

**Pas de `Vector2Int` dans le JSON.** Newtonsoft écrirait aussi ses propriétés calculées
`magnitude` et `sqrMagnitude`, qu'il ne saurait pas relire. Deux entiers explicites : le
fichier ne dépend pas de la façon dont Unity sérialise ses types.

## 2. `Scripts/Core/SaveSystem.cs` (créé)

Vit dans Persistent, sur l'objet `GameManager`, à côté du solveur, de l'horloge et des
saisons. `GameManager` l'expose comme il expose déjà `Router`, `Flow`, `Clock` et `Seasons`.

Fichier : `Application.persistentDataPath/partie.json`.

**Sauvegarder**

- `MarkDirty()` sur `PipeNetwork.Changed`, sur `UndergroundMap.Dug` et sur
  `SeasonSystem.SeasonChanged`.
- Un anti-rebond : au plus une écriture toutes les deux secondes, dans `Update`.
- Une écriture forcée sur `OnApplicationQuit` et sur `OnApplicationPause(true)`.
- **Écriture atomique** : on écrit `partie.json.tmp`, puis on remplace. Une coupure de courant
  en pleine écriture ne doit jamais laisser un fichier tronqué à la place d'une bonne partie.

**Charger**

Résolution paresseuse, comme le solveur depuis la phase 4 : Persistent est chargée **avant**
Underground. On retente tant que `UndergroundMap` et `PipeNetwork` ne répondent pas, puis on
charge une fois, et plus jamais.

Ordre de restauration, et il compte :

1. **Rejouer les creusements.** Sans terrain ouvert, `PlacePipe` refuse.
2. **Reposer les tuyaux.** Les segments se recréent seuls, par voisinage.
3. **Appliquer l'état de chaque segment** : condition, gel, bouchon.
4. **Poser la saison et la progression de l'horloge.**
5. **Une seule résolution du solveur**, à la fin.

**Aucune sauvegarde pendant le chargement.** Un drapeau coupe `MarkDirty` le temps de la
restauration : sans lui, chaque tuyau reposé déclencherait une écriture d'un monde à moitié
reconstruit.

**Un fichier illisible n'est jamais écrasé.** Version inconnue, JSON corrompu, exception en
plein chargement : on met le fichier de côté sous `partie-illisible-<date>.json`, on démarre
une partie neuve, et **on ne dit rien à l'écran**. Un `Debug.LogWarning` pour moi, rien pour
Victorien : il n'a pas à savoir qu'un fichier existe.

## 3. Ce que l'on ne sauvegarde pas, et pourquoi

| Donnée | Pourquoi pas |
|---|---|
| Les nœuds permanents, station et maisons | La scène les recrée. Les figer dans le fichier gèlerait le plan du monde dans les parties de Victorien, et le moindre changement de carte les casserait. |
| Les maisons desservies | Le solveur les recalcule. Une donnée dérivée sauvegardée est une donnée qui peut mentir. |
| La profondeur des nœuds | Lue sur la carte à la pose, comme depuis la phase 3. |
| Les galeries déjà creusées au départ | Elles viennent du plan, pas du joueur. `UndergroundMap` ne retient que les cases creusées **en jeu**. |
| La position et la couche du joueur | Décision 3 : il repart toujours au départ du village. |

## 4. Les fichiers touchés à côté

- **`UndergroundMap`** : un `HashSet` des cases creusées en jeu, exposé en lecture, et un
  événement `Dug`. C'est la source de vérité du terrain, et ce que `SaveSystem` écoute.
- **`PipeNetwork`** : `SegmentBetween(a, b)` pour retrouver un segment à restaurer, et un mode
  groupé pour que reposer cinquante tuyaux ne lève `Changed` **qu'une seule fois** au lieu de
  cinquante, donc ne déclenche qu'une résolution au lieu de cinquante.
- **`SeasonSystem`** : `Restore(index)`, qui pose la saison **sans appliquer ses effets**.
  Charger une partie en hiver ne doit pas regeler le réseau une deuxième fois.
- **`GameManager`** : expose `Save`.
- **`PersistentSceneBuilder`** : pose et câble le `SaveSystem`.
- **`ProjectSettings`** : `companyName` passe à `Lio`. **C'est la première fois de ce projet
  que je touche aux ProjectSettings**, et c'est volontaire : jusqu'ici toutes les phases ont
  vérifié par `git status` qu'elles n'y touchaient pas.

## 5. `Editor/ProjectSetup/SaveTools.cs` (créé)

Deux menus, pour moi et pas pour le jeu :

- `Sous La Ville/Ouvrir le dossier de sauvegarde`
- `Sous La Ville/Effacer la sauvegarde`

Ils rendent la phase vérifiable : sans eux, tester une partie neuve demanderait d'aller
supprimer un fichier à la main entre deux essais.

## Décisions techniques signalées

1. **On sauvegarde des gestes, pas un état.** La liste des cases creusées et des cases posées
   suffit, parce que `Dig` et `PlacePipe` sont déjà les seules façons de modifier le monde.
   Conséquence : **tout état chargé est un état atteignable en jouant.** Un fichier bricolé à
   la main ne peut pas produire un réseau impossible.
2. **Aucun retour visuel de sauvegarde.** Pas d'icône, pas de pictogramme, rien. Un témoin qui
   clignote dit qu'il existe un risque de perdre quelque chose ; toute la promesse de CLAUDE.md
   est qu'il n'y en a pas. **Réversible** : si Victorien demande un jour à voir que ça enregistre,
   c'est un `Image` de plus dans le HUD.
3. **Un seul fichier, aucune notion de partie multiple.** Pas de menu, pas de choix, pas de
   texte. Un bac à sable unique qui reprend tout seul.
4. **Aucun moyen d'effacer la partie depuis le jeu.** Un bouton « recommencer » à portée d'un
   enfant de six ans, c'est la perte de progression que CLAUDE.md interdit, déguisée en
   fonctionnalité. Le menu Editor suffit pour mes essais. **À rouvrir** si le besoin apparaît
   en jouant.
5. **Un champ `version` dès la première version.** Les phases 7 à 12 ajouteront de l'état à
   sauvegarder ; un champ manquant se relit sans rien casser avec Newtonsoft, mais un
   changement de forme, non.
6. **Limite assumée** : si le jeu se ferme brutalement sans le moindre geste depuis dix
   minutes, la progression dans la saison en cours revient au dernier geste. Le réseau, lui,
   est intact. C'est le seul cas où quelque chose se perd, et ce qui se perd est un compteur
   invisible.

**Référence d'assembly, règle 9 :** `Newtonsoft.Json.dll` est déjà dans les
`precompiledReferences` de `SousLaVille.Runtime.asmdef`, et `overrideReferences` y est à
`true`, donc la liste fait autorité. Vérifié : rien à ajouter cette fois. `SaveTools` ne
touche que `UnityEngine` et `UnityEditor`, déjà couverts côté Editor.

## Vérification de fin de phase

1. `companyName` posé, puis les menus, puis construction des scènes.
2. Console relue par le pont MCP : zéro erreur **et** zéro warning.
3. Play depuis Boot, sauvegarde effacée au préalable :
   - **partie neuve** : aucun fichier, aucun message, le jeu démarre au printemps ;
   - creuser, poser un réseau jusqu'à une maison, la faire desservir, puis **quitter le play
     mode et le relancer** : mêmes cases creusées, mêmes tuyaux, même maison desservie ;
   - **le gel survit** : geler le réseau en hiver, quitter, relancer, les tuyaux sont encore
     gelés, la teinte blanc bleuté est là, et le printemps les dégèle comme avant ;
   - **l'usure survit** : un tuyau à 0,45 est encore à 0,45 après un redémarrage ;
   - **la saison survit** : quitter en automne, revenir en automne, progression comprise ;
   - **le joueur repart au départ du village**, en surface, quelle que soit la couche quittée ;
   - **le solveur ne tourne qu'une fois au chargement**, compteur à l'appui, pas une fois par
     tuyau ;
   - **aucune écriture pendant le chargement**, vérifiée en comptant les écritures ;
   - **un fichier corrompu à la main** : le jeu démarre neuf, sans message à l'écran, et
     l'ancien fichier est retrouvé à côté sous son nom d'illisible ;
   - console **entièrement vide** sur une session complète.
4. Le fichier relu à l'œil : petit, lisible, et le chemin est bien
   `~/Library/Application Support/Lio/SousLaVille/partie.json`.
5. `git status` : **`ProjectSettings.asset` modifié, et lui seul**, sur la seule ligne
   `companyName`.
6. PROGRESS.md à jour, commit `Phase 6 - la sauvegarde`, puis arrêt.
