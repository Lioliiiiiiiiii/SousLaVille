# Phase 15 — La Fabrique

Le deuxième mini-jeu de l'usine à panneaux : **le nom parmi trois noms**, décidé le 3 septembre
2026. Il s'appuie sur le squelette de la phase 14 sans le modifier, et ne commence pas Le Plan.

## 0. Ce qui est repris, sans être refait

- `MiniGameScreen` : le panneau, les deux gardes, les flèches au changement de direction,
  `Closed`, le registre, `AnyOpen`. **Aucune ligne n'y change.**
- Le lancement : `MiniGamePosts` déclare déjà La Fabrique en (9, 4), `ValidateMiniGames` vérifie
  l'appariement, `Villager.ResolveMiniGame` rend l'écran dès qu'il existe. **Il ne reste qu'à
  écrire la sous-classe et à la construire dans le HUD.**
- Les 24 panneaux et les 24 noms de la phase 13. **Aucune image neuve** : le panneau s'affiche
  agrandi trois fois par le Canvas, à filtre point ; les rangées de choix et la jauge sont des
  `Image` teintées sans sprite, comme le voile et les fonds de carte.
- Le tirage par famille du memory, **sorti de `SignMemory.Deal` dans `SignDraw.PerFamily`** pour
  servir aux deux — le memory doit ressortir identique, vérifié sur les mêmes graines.

## 1. La forme, tranchée le 5 septembre 2026

- **Le panneau à gauche, agrandi 3×** (48 × 72), **trois noms à droite** sur des rangées de 32 px
  — le plancher de CLAUDE.md. La rangée fait 204 px : le plus long nom, 193 px, y tient avec
  ses marges. 48 + 12 + 204 = 264 ≤ 312 ; 3 × 32 + 2 × 4 = 104 de haut.
- **8 questions par lancement, 2 par famille**, comme le memory.
- **Les leurres se resserrent d'un lancement à l'autre** : lancement 1, deux leurres d'autres
  familles — la grammaire des formes suffit ; lancement 2, un leurre de la même famille ;
  lancement 3 et suivants, les deux — il faut lire le pictogramme. Compteur dans le composant,
  rien sur le disque.
- **Un nom faux s'éteint, on rechoisit.** Au plus deux erreurs par question, jamais de
  révélation. Un nom juste passe au vert, les deux autres s'éteignent, et **le geste suivant,
  quel qu'il soit, passe à la question suivante** : aucune minuterie, comme en 14.
- **Une jauge de huit carrés** en haut dit où l'on en est. Sans texte.
- À la huitième, le picto de sortie s'allume et Espace referme.

## 2. Les filets

- `ValidateQuizBoard` dans `PersistentSceneBuilder` : rangée ≥ 32, largeur totale ≤ 312, le plus
  long nom tient dans la rangée, 8 divisible par 4, leurres de même famille ≤ 2 et < 6.
  Sabotages : rangée à 30, rangée à 260 px, trois leurres de même famille.
- `SignQuiz.Build(questions, slots, families, sameFamilyLures, rng)` **statique et pur**, vérifié
  sur 1000 graines : 3 noms distincts par question, la cible parmi eux, exactement le nombre
  demandé de leurres de la même famille, 2 cibles par famille.
- `SignMemory.Deal` **inchangé au tirage près** : mêmes graines, mêmes plateaux qu'avant le
  refactor, comparé donne pour donne.

## 3. Vérification en jeu

Entrée par (31, 34) → marche jusqu'en (9, 5) → La Fabrique en (9, 4) → ses deux phrases → huit
questions jouées, dont une réponse fausse volontaire → Espace referme, l'écran ne se rouvre pas →
sortie par (9, 0). Captures : début, en cours après une bonne réponse, fin. Puis lancements 2 et
3 par l'API : leurres 1 puis 2 de la même famille.
