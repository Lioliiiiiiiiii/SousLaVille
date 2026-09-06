# Phase 20 — Deux villages

Victorien mène deux réseaux en parallèle et passe de l'un à l'autre. Décidé le 6 septembre 2026.

- **Le choix se fait au démarrage seulement.** Pas de borne dans le village, pas de retour en
  cours de jeu. Pour changer de village, il relance le jeu.
- **Deux villages au plus.**
- **Aucun nom à taper.** Ils s'appellent `VILLAGE 1` et `VILLAGE 2`. L'écran d'écriture envisagé
  d'abord est abandonné : une trentaine de pressions de flèche pour nommer une partie, c'était
  cher payé, et un enfant de six ans veut jouer, pas remplir un formulaire.

## 1. La règle : le nom se déduit de l'emplacement

- **Deux emplacements**, `partie-1.json` et `partie-2.json`, dans `Application.persistentDataPath`.
- **L'emplacement N s'appelle toujours `VILLAGE N`.** Le nom n'est donc **pas** rangé dans la
  sauvegarde : il n'y a rien à stocker, rien à désynchroniser, et `SaveData` ne change pas d'une
  ligne. `CurrentVersion` reste à 2.
- **Effacer le village 1 ne renomme pas le village 2.** L'emplacement 1 redevient libre, et le
  village créé ensuite s'appellera de nouveau `VILLAGE 1`. Un village qui changerait de nom tout
  seul serait la pire des surprises.
- **Migration.** Un `partie.json` existant — celui d'avant cette phase — est **copié** vers
  `partie-1.json` au premier lancement, et devient donc `VILLAGE 1`. L'original n'est pas
  supprimé : si la copie se passait mal, rien n'est perdu. La copie ne se fait jamais par-dessus
  un `partie-1.json` déjà là.

## 2. L'écran de choix

Des lignes, l'une sous l'autre, de 40 px de haut. Flèches haut et bas pour changer de ligne,
flèches gauche et droite pour changer d'action **sur** la ligne, Espace pour faire. Aucune autre
touche, aucune combinaison, aucun timing.

**Au premier lancement**, une seule ligne :

    [ vignette grise 48x32 ]  + NOUVEAU VILLAGE

Espace crée le village 1 et le jeu démarre dessus dans la foulée. Aucune question, aucun clavier.

**Ensuite**, une ligne par village existant, puis la ligne de création tant qu'il reste de la
place :

    [ vignette 48x32 ]  VILLAGE 1        [ ▶ 32x32 ]  [ ↺ 32x32 ]
    [ vignette 48x32 ]  VILLAGE 2        [ ▶ 32x32 ]  [ ↺ 32x32 ]

Trois lignes au plus — deux villages plus la création — soit 120 px sur 180.

- **La vignette est la même pour tous les villages** : un petit village générique. Elle ne
  distingue rien, c'est le numéro qui distingue ; elle dit seulement « ceci est un village ».
- **▶ joue.** C'est l'action par défaut : le curseur d'action s'y pose en arrivant sur la ligne.
- **↺ efface**, en deux temps. Espace sur ↺ remplace la ligne par une bande de confirmation :
  deux pictos, ✗ et ✓, **le curseur commence sur ✗**. Gauche et droite pour choisir, Espace pour
  faire. ✗ referme la bande sans rien toucher. Deux pressions d'Espace séparées par un
  déplacement volontaire : ni timing, ni combinaison, et rien ne se détruit d'un geste distrait.
- **La ligne de création n'a qu'une action** et disparaît quand les deux villages existent.

Le texte affiché se limite à `VILLAGE 1`, `VILLAGE 2` et `NOUVEAU VILLAGE` : deux mots au plus,
en majuscules, et `PixelFont` a déjà les lettres et les chiffres. Aucun accent, qui manquent
encore à la police. Chaque zone d'action fait 32 px de côté, le plancher de CLAUDE.md.

## 3. Ce qui change dans le code existant

- **`SaveSystem`** : `FilePath` dépend d'un emplacement courant, posé **avant** le chargement.
  Des méthodes statiques pour l'écran de choix, qui n'ont pas besoin de charger une partie :
  `SlotExists`, `DeleteSlot`, `MigrateLegacySave`.
- **Le verrou de chargement.** Aujourd'hui `SaveSystem.Update` charge dès que la carte et le
  réseau répondent. Il faut un verrou : tant qu'aucun emplacement n'est choisi, il ne charge
  rien. C'est le point le plus délicat de la phase — un oubli ici et le jeu charge le village 1
  pendant que l'écran de choix est encore à l'écran.
- **`Bootstrapper`** : Persistent chargée, il ouvre l'écran de choix et **attend**. Ce n'est
  qu'au choix fait qu'il appelle `LoadGameplayScenesAsync`. Les couches de jeu ne sont donc pas
  chargées pendant que l'écran est ouvert, et le fond de l'écran de choix est uni.
- **`VillageSelectScreen`**, dans `Scripts/UI`, sur le modèle de `VillageMapScreen` : un panneau
  éteint, les flèches, Espace, un événement à la fermeture.
- **`PersistentSceneBuilder`** : un panneau neuf, éteint par défaut, construit par code comme le
  reste, dans sa propre méthode.
- **Images neuves** : la vignette du village, et les pictos qui manquent parmi ▶, ↺, +, ✓, ✗ —
  à inventorier dans `PlaceholderArtGenerator` avant d'en générer.

## 4. Les filets

- **Validation de la mise en page**, jouée à chaque construction de Persistent : trois lignes au
  plus, chaque zone d'action d'au moins 32 px, rien qui déborde de 320x180.
- **Sabotage de la migration** : un `partie.json` en version 2, un `partie-1.json` déjà présent
  — la migration ne doit alors rien écraser —, un `partie.json` tronqué, un fichier illisible.
  `SaveSystem` a déjà la mise de côté d'une sauvegarde abîmée : elle doit servir ici aussi.
- **Sabotage du verrou** : forcer les couches de jeu à se charger pendant que l'écran est
  ouvert, et vérifier qu'aucune partie n'est lue.
- **Sabotage de l'effacement** : effacer le village 1 sur deux occupés, vérifier que le
  village 2 est intact au fichier près, et qu'un village créé ensuite reprend l'emplacement 1.

## 5. Vérification en jeu

Sur le build web publié, puisque c'est là que Victorien joue :

1. Premier lancement : une seule ligne, Espace, le jeu démarre.
2. Creuser, fermer l'onglet, rouvrir : `VILLAGE 1` est là, ▶ le reprend, le tunnel y est.
3. Créer le village 2, creuser ailleurs, relancer, reprendre le village 1 : les deux réseaux
   sont distincts.
4. Les deux villages créés, la ligne de création a disparu.
5. Effacer le village 2, vérifier que le village 1 n'a pas bougé.
6. ✗ sur la confirmation d'effacement ne détruit rien.

## 6. Ce que ce plan ne tranche pas

- **La souris.** Tu avais écrit « cliquer sur la vignette ». Le jeu entier se joue aux flèches et
  à Espace, et aucun écran existant ne répond à la souris. Ce plan reste au clavier. La souris
  serait un chantier à part, qui devrait aussi toucher les écrans des phases 14 à 16 — sinon
  Victorien aurait une souris qui ne marche qu'à un seul endroit du jeu.
- **Deux villages suffisent-ils ?** Le nombre est une constante ; passer à trois coûterait la
  relecture de la mise en page, pas plus.
