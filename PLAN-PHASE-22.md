# Phase 22 — Les touches se disent, et l'eau ne ment plus

Cinq points signalés le 6 septembre 2026, après avoir joué. Deux n'étaient pas ce qu'ils
paraissaient : la vérification les a redressés avant qu'une ligne ne soit écrite.

## 1. Ce que la vérification a redressé

**Le Plan n'a pas de bug d'ordre.** Le signalement disait qu'il fallait « valider le panneau à
gauche puis à droite ». Le plan à deux poteaux a été rejoué dans les deux sens, par les méthodes
publiques : gauche-puis-droite et droite-puis-gauche donnent **exactement** le même résultat, et
les deux se valident. La cause réelle est ailleurs : une flèche ne vérifie que si TOUS les
poteaux portent un panneau, et un poteau faux **se vide** à la vérification. Le joueur se
retrouve donc avec un poteau blanc, et les flèches ne font plus que déplacer.

**La flaque de fuite disparaissait déjà.** Le trajet complet a été rejoué : abîmer un tuyau,
remonter (3 flaques), redescendre, réparer, remonter — **0 flaque**. Ce qui ne disparaissait
jamais, c'est l'autre eau.

## 2. Le débordement est supprimé

Le jeu peignait deux eaux avec **la même image** :

- **La fuite**, une flaque isolée au-dessus d'un tuyau crevé, qui réagit aux réparations ;
- **Le débordement**, une nappe autour des bouches, tirée de `SeasonSystem.LastBudget.Lost`.

`ApplyWaterBudget` n'est appelée que depuis `ApplySeason` : le bilan **ne se recalcule qu'au tick
de saison**. Une nappe apparue à l'automne restait donc dix minutes durant, quoi que le joueur
répare. Et comme les deux eaux portaient la même image, il n'avait aucun moyen de les distinguer.

**Le recalcul en continu aurait été pire.** Réparer un tuyau RECONNECTE des maisons, donc
augmente l'arrivant, donc le débordement : le geste juste aurait fait grandir la flaque.

Décidé le 6 septembre 2026 : **on supprime le débordement.** `PaintOverflow` et le champ
`manholeCells` disparaissent de `FloodView`, et le câblage des bouches disparaît de
`SurfaceSceneBuilder`.

**Ce qu'on perd, et c'est assumé** : la leçon du bassin de la phase 8 n'a plus de signe visible.
Le bilan continue de tourner, le bassin continue d'encaisser ; plus rien ne le montre dans la rue.

## 3. Échap sort des mini-jeux

Une manche de seize paires dure plusieurs minutes, et rien ne permettait d'en sortir avant de
l'avoir finie — sinon éteindre le jeu.

- Dans `MiniGameScreen.Update`, avant tout le reste : Échap referme. Les trois mini-jeux
  l'héritent d'un coup.
- **Elle n'entre pas dans `SousLaVilleInputActions`** : cet asset porte les deux gestes du JEU,
  les flèches et Espace. Une sortie d'écran n'est pas un geste de jeu.
- **`GameManager` cède le pas.** Depuis la phase 20, Échap ferme le jeu sur les exécutables de
  bureau. Un mini-jeu ouvert prend la touche pour lui, sinon un seul appui refermerait le
  mini-jeu ET quitterait le jeu, dans un ordre que rien ne garantit.

## 4. La bande d'aide

En bas des trois mini-jeux, en permanence : trois groupes, chacun un picto de touche suivi d'un
mot court.

    [croix]  CHOISIR       [barre]  VALIDER       [touche]  SORTIR

**Les deux premiers mots changent avec l'état.** C'est tout l'objet de la bande :

| Mini-jeu | Flèches | Espace |
|---|---|---|
| Le Stock | CHOISIR | RETOURNER, ou rien sur une carte déjà retournée |
| La Fabrique | CHOISIR, **SUITE** une fois répondu | VALIDER, **SUITE** une fois répondu |
| Le Plan | CHOISIR, **VERIFIER** dès que tout est garni | POSER, ou rien sur un poteau fixé |

`VERIFIER` et `SUITE` sont exactement les deux informations qui manquaient.

**Pourquoi des mots et pas des pictos.** CLAUDE.md veut le moins de texte possible et le
pictogramme en premier choix. Mais « vérifier », « poser » et « suite » ne se dessinent pas sans
ambiguïté en seize pixels, et Victorien sait lire. La TOUCHE est donc un picto — c'est elle qu'il
cherche des yeux sur son clavier — et l'ACTION est un mot court, comme les noms de panneaux.

**Les rangs des mots sont écrits deux fois**, dans `PlaceholderArtGenerator.HintWords` et en
constantes dans `MiniGameScreen`, parce que le runtime ne lit pas l'assembly Editor.
`ValidateHintWords` les compare à chaque construction.

## 5. Le cadre rouge de La Fabrique

Une réponse fausse s'éteignait en gris. Le gris se lisait autant comme « pas encore choisi » que
comme « faux ». Un cadre **rouge** est posé derrière la rangée écartée, débordant de deux pixels.
Il ne dit qu'une chose.

## 6. Les filets

- `ValidateHintWords` : sept mots, dans l'ordre, chacun tenant dans sa colonne.
- Le plan à deux poteaux rejoué dans les deux sens, par les méthodes publiques.
- Le trajet fuite → réparation → remontée, en comptant les flaques.

## 7. Ce que ce plan ne tranche pas

- **Le picto d'Échap est perfectible.** Cinq dessins ont été essayés ; celui-ci lit encore un peu
  « T ». CLAUDE.md dit « art placeholder d'abord, le pixel art vient à la toute fin » : le mot
  SORTIR porte le sens juste à côté, et on n'y a pas insisté davantage.
- **La leçon du bassin n'a plus de signe.** À reprendre si Victorien cesse de relier le bassin.
