# Plan de la phase 5 — Les saisons et le gel

Validé le 3 septembre 2026. Voir CLAUDE.md pour les contraintes du projet et PROGRESS.md pour
l'état d'avancement.

## Contexte

Phase 4 committée (`9552a2c`). Le terrain est prêt :

- `FlowSolver.Solve()` est public : un tick de saison n'aura qu'à l'appeler.
- `PipeSegment.Condition`, `IsFrozen` et `IsClogged` sont **déjà lus** par le solveur et
  modifiables. Geler un segment le retire du réseau sans une ligne de plus côté écoulement.
- `PipeType.FrostResistance` et `WearPerSeason` attendent leur premier usage réel.
- `PipeNetworkView` teinte déjà les tuyaux : les nouveaux états n'ajoutent que des couleurs.

**Critère de fin : le temps passe, l'hiver gèle mes tuyaux peu profonds, la maison se coupe, le
printemps dégèle et tout revient. Je peux réparer ce qui s'abîme. Rien n'est jamais perdu pour
de bon.**

## Décisions de design validées

1. **Une saison dure dix minutes.** Le cycle complet fait donc quarante minutes.
2. **Espace répare** un tuyau abîmé, gelé ou bouché, et n'enlève que les tuyaux sains. Le picto
   au-dessus de la tête dit lequel des deux va se produire. Conséquence assumée : enlever un
   tuyau cassé demande de le réparer d'abord.
3. **Les quatre effets ci-dessous**, l'été ne faisant rien : une saison de répit dans le cycle.
4. **L'usure est conservée et relevée à `0,1` par saison**, puisque les saisons sont cinq fois
   plus longues que dans la proposition initiale. Modulée par saison : été 0,5, printemps et
   automne 1, hiver 1,5. Un tuyau tombe sous le seuil de 0,3 après **sept saisons, environ
   soixante-dix minutes de jeu**, presque deux années de jeu.

## 1. `Scripts/Core/GameClock.cs` (créé)

L'horloge, prévue par CLAUDE.md depuis la phase 0. Elle ne fait qu'une chose : compter, et
lever un `Tick` à la fin de chaque saison. Elle expose la progression de 0 à 1, dont la phase 6
aura besoin pour sauvegarder l'instant exact.

Durée par défaut : **600 secondes**. Champ sérialisé, pour pouvoir accélérer pendant les tests.

## 2. `Scripts/Seasons/SeasonDefinition.cs` (créé, ScriptableObject)

Une saison décrite en données, pas en code :

- son pictogramme et la **couleur de la lumière de surface** ;
- `freezeMaxDepth` : profondeur jusqu'à laquelle les tuyaux gèlent, 0 pour aucune ;
- `clogChance` : probabilité qu'un tuyau peu profond se bouche ;
- `wearMultiplier` : usure de la saison, multipliée par `PipeType.WearPerSeason` ;
- `thaws` : la saison dégèle tout ce qui était gelé.

Quatre assets créés par le menu existant `Sous La Ville/Créer les ScriptableObjects` :
`Season_Printemps`, `Season_Ete`, `Season_Automne`, `Season_Hiver`.

| Saison | Effet | Usure | Lumière |
|---|---|---|---|
| Printemps | dégèle tout, rien d'autre | 1 | vert clair |
| Été | rien. Une saison pour construire tranquille | 0,5 | doré |
| Automne | les feuilles bouchent des tuyaux **peu profonds** | 1 | orangé |
| Hiver | **gèle les tuyaux de profondeur 1** | 1,5 | bleu pâle |

C'est le cœur de la boucle : **l'hiver récompense ceux qui ont creusé profond.** La règle de
profondeur cesse d'être une contrainte abstraite, elle devient une leçon qui revient chaque
année.

## 3. `Scripts/Seasons/SeasonSystem.cs` (créé)

Avance d'une saison à chaque `Tick`, applique les effets aux segments, puis appelle
`FlowSolver.Solve()`. Dans cet ordre, une seule fois par saison, jamais par frame.

Le gel épargne les tuyaux dont le `PipeType.FrostResistance` est élevé : c'est ce qui donnera
un sens aux types de canalisation, en phase 9.

Vit dans Persistent avec les autres services globaux ; `GameManager` l'expose comme il expose
`Router` et `Flow`.

## 4. Le retour visuel

- **Un pictogramme de saison** en HUD, à côté du repère de couche. Quatre images, zéro texte.
- **La lumière de la surface change de couleur** avec la saison. C'est le signal le plus fort
  et il ne coûte rien : la `Light2D` globale existe depuis la phase 0.
- **Les tuyaux disent leur état par leur couleur**, dans cet ordre de priorité :

| État | Couleur |
|---|---|
| gelé | blanc bleuté |
| bouché | brun |
| trop abîmé pour porter | rouge terne |
| porte l'eau | bleu |
| sain, sans eau | gris |

Le sous-sol garde sa lumière : les saisons se voient dessus, se subissent dessous.

## 5. Réparer

`PlayerInteractor` gagne une cinquième action, dans l'ordre existant : passage sur la case
occupée, puis, sur la case regardée, creuser, poser, **réparer si le tuyau est abîmé, gelé ou
bouché**, enlever s'il est sain. `PipeNetwork.Repair(cell)` remet la condition à 1, dégèle,
débouche, et lève `Changed` comme les autres.

## 6. Fichiers

**Créés** — `Scripts/Core/GameClock.cs`, `Scripts/Seasons/SeasonDefinition.cs`,
`Scripts/Seasons/SeasonSystem.cs`, `Scripts/Seasons/SeasonAmbience.cs` (posé sur la racine de
la surface, il teinte sa propre lumière), `Scripts/UI/SeasonIndicator.cs`.

**Modifiés** — `GameManager` (expose `Clock` et `Seasons`), `PipeNetwork` (`Repair`),
`PipeNetworkView` (les cinq couleurs), `PlayerInteractor` (le geste de réparation),
`ScriptableObjectSetup` (les quatre saisons), `PersistentSceneBuilder` (horloge, système,
picto de saison), `SurfaceSceneBuilder` (l'ambiance), `PlaceholderArtGenerator` (quatre pictos
de saison, un picto de réparation).

**Référence d'assembly, règle 9 :** `SeasonAmbience` touche `Light2D`, qui vit dans
`Unity.RenderPipelines.Universal.2D.Runtime`. **`SousLaVille.Runtime.asmdef` doit gagner cette
référence** — exactement le piège que CLAUDE.md signale. Le côté Editor l'a déjà.

## Décisions techniques signalées

1. **Les saisons sont des données, pas du code.** Ajouter une saison ou changer un effet ne
   demandera pas de recompiler.
2. **`SeasonAmbience` vit dans la scène Surface**, et non dans le système. La couche éteinte ne
   répondrait pas ; un composant local qui s'abonne et se réabonne à chaque rallumage évite le
   piège rencontré deux fois déjà.
3. **Le gel et le bouchon sont réversibles, l'usure demande un geste.** Rien n'est jamais
   détruit définitivement, mais la boucle a besoin d'un entretien pour exister.
4. **Aucune sauvegarde encore.** Le temps qui passe rend la phase 6 franchement urgente.
5. **Le tick n'a lieu qu'à la fin d'une saison.** Le solveur tourne quatre fois par cycle, plus
   une fois par action du joueur. Toujours jamais par frame.

## Vérification de fin de phase

1. Les menus, puis construction des scènes.
2. Console relue par le pont MCP : zéro erreur **et** zéro warning.
3. Play depuis Boot, avec une horloge accélérée le temps du test :
   - le cycle tourne dans l'ordre et revient au printemps ;
   - un réseau qui passe par de la profondeur 1 **se coupe en hiver**, la goutte s'éteint, et
     **revient au printemps sans rien faire** ;
   - un réseau entièrement profond traverse l'hiver sans broncher ;
   - un tuyau bouché en automne coupe la maison jusqu'à réparation ;
   - l'usure fait tomber un tuyau sous 0,3 après sept saisons, et la réparation le remet à
     neuf ;
   - le picto de réparation n'apparaît que sur un tuyau abîmé, le picto d'enlèvement que sur un
     tuyau sain ;
   - la lumière et le picto de saison changent à chaque saison ;
   - le solveur tourne une fois par saison, compteur à l'appui ;
   - console vide sur une session complète.
4. Captures : les quatre ambiances, un réseau gelé.
5. PROGRESS.md à jour, commit `Phase 5 - les saisons et le gel`, puis arrêt.
