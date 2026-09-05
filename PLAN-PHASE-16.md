# Phase 16 — Le Plan

Le troisième et dernier mini-jeu de l'usine à panneaux. Il s'appuie sur le squelette de la
phase 14 sans le modifier.

## 1. La règle, tranchée le 5 septembre 2026 : le Code, tel que `RoadSigns` l'applique

Chaque plan est une petite carte de rues en ASCII, style `VillageLayout`. Les **postes** — où un
panneau manque — et le **panneau attendu** à chacun ne sont écrits nulle part : ils sont
**dérivés** par les mêmes règles que les 32 panneaux du village. Une solution écrite à la main
pourrait être fausse ; une solution déduite du Code ne peut pas l'être.

- **`RoadSignRules`** : les règles sortent de `VillageLayout` dans une classe partagée derrière
  une interface `IRoadGrid`. **Les 32 panneaux du village doivent ressortir identiques** — case,
  type, sens, ordre — comparés à une photographie prise avant le refactor.
- **Une rue qui touche le bord continue hors du plan** (`IsExit`) : sans cela chaque rue qui
  sort du cadre serait une impasse. Le village n'en a aucune.
- **La route prioritaire se dessine avec `=`** ; ses coins sont déduits, et elle doit être d'un
  seul trait. À l'écran elle est **teintée** : c'est la seule chose que le joueur doit voir pour
  choisir un stop plutôt qu'un cédez.
- **Quinze plans écrits à la main**, du plus simple au plus lourd : le cédez seul, l'impasse,
  l'entrée de garage qui n'a droit à rien, la route prioritaire et ses quatre panneaux, le
  stop, l'impasse qui recule, le virage prioritaire, les combinaisons.

## 2. La forme

- **Une case fait 32 px**, le plancher de CLAUDE.md ; **9 × 5 au plus** : 288 sur 320, 160 sur
  180. Les tuiles du village agrandies deux fois par le Canvas ; **une seule image neuve**, le
  poteau vide.
- **Une seule touche.** Les flèches vont de poteau en poteau ; Espace **fait défiler** le panneau
  du poteau visé — vide, cédez, stop, impasse, prioritaire, fin, vide. Aucun second niveau de
  choix : c'est la contrainte de CLAUDE.md, et chaque panneau passe sous les yeux à son tour.
- **Le plan vérifie quand tous les poteaux sont garnis, au geste suivant** — la flèche qui dit
  « j'ai fini de poser ». Les justes se fixent (sol vert), les faux se vident, on repose. Aucun
  échec.
- **Un plan par lancement**, le suivant à chaque fois, retour au premier après le quinzième et à
  la fermeture du jeu. Rien sur le disque.

## 3. Les filets

- `PlanLayout.Validate`, joué à chaque construction de Persistent : au plus 9 × 5, lignes égales,
  caractères connus, rues d'un seul tenant, `=` d'un seul trait, **au moins un poste**, **aucun
  panneau sans herbe** (ce qui n'est qu'un avertissement au village est un refus ici), et à
  chaque carrefour de la route prioritaire les bras non signalés **sont** la route prioritaire
  — sinon elle céderait le passage à une rue ordinaire. Sabotages : route prioritaire sur la
  ligne du bord (ses fins tombent hors du plan), plan à 10 colonnes, coude prioritaire dans
  l'axe d'une rue.
- Les 32 panneaux du village identiques au refactor.
- Chaque plan dérivé est **regardé**, postes superposés au plan ASCII, avant d'être cru.

## 4. Vérification en jeu

Porte (31, 34) → (15, 5), demi-tour vers Le Plan → deux phrases → **le premier plan joué** : tous
les poteaux garnis dont un faux exprès, la flèche qui juge, le faux se vide et le juste se fixe,
on repose le bon, le plan est juste → Espace referme, l'écran ne se rouvre pas → sortie. Puis les
quinze plans ouverts par l'API. Captures : début, après le jugement, fin, et la planche des
images.
