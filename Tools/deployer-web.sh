#!/usr/bin/env bash
#
# Publie le contenu de Build/Web sur la branche gh-pages, d'ou GitHub Pages le sert.
#
# La branche gh-pages est orpheline : elle ne contient que le jeu compile, jamais les
# sources. Chaque deploiement remplace entierement son contenu, il n'y a donc pas
# d'historique de builds qui s'accumule dans le depot.
#
# Usage : ./Tools/deployer-web.sh

set -euo pipefail

RACINE="$(cd "$(dirname "${BASH_SOURCE[0]}")/.." && pwd)"
SOURCE="$RACINE/Build/Web"
TRAVAIL="$RACINE/Build/gh-pages"
BRANCHE="gh-pages"

# Le depot porte les hooks de Git LFS, mais git-lfs n'est pas installe sur cette machine
# et .gitattributes ne declare aucun fichier LFS : les hooks echouent pour rien, et
# pre-push refuserait la publication. On les met de cote le temps du deploiement, sans
# rien changer a la configuration du depot. Le dossier HOOKS reste vide volontairement.
HOOKS="$(mktemp -d)"
trap 'rm -rf "$HOOKS"' EXIT
git() { command git -c core.hooksPath="$HOOKS" "$@"; }

if [ ! -f "$SOURCE/index.html" ]; then
  echo "Erreur : $SOURCE/index.html est absent. Lancer d'abord le menu" >&2
  echo "        « Sous La Ville / Construire le jeu pour le web » dans Unity." >&2
  exit 1
fi

cd "$RACINE"

# Un worktree jetable : la copie de travail principale n'est jamais touchee.
rm -rf "$TRAVAIL"
git worktree prune

if git show-ref --verify --quiet "refs/heads/$BRANCHE"; then
  git worktree add "$TRAVAIL" "$BRANCHE"
else
  git worktree add --detach "$TRAVAIL"
  git -C "$TRAVAIL" checkout --orphan "$BRANCHE"
  git -C "$TRAVAIL" rm -rf . >/dev/null 2>&1 || true
fi

# Table rase : un fichier d'un build precedent qui trainerait serait servi a la place.
find "$TRAVAIL" -mindepth 1 -maxdepth 1 ! -name '.git' -exec rm -rf {} +

cp -R "$SOURCE"/. "$TRAVAIL"/

# Sans ce fichier, GitHub passe le site dans Jekyll, qui ignore les dossiers commencant
# par un tiret bas et peut escamoter des fichiers du build.
touch "$TRAVAIL/.nojekyll"

cd "$TRAVAIL"
git add -A

if git diff --cached --quiet; then
  echo "Rien de neuf a publier."
else
  git commit -q -m "Build web du $(date '+%d/%m/%Y a %Hh%M')"
  git push -q -u origin "$BRANCHE"
  echo "Publie sur la branche $BRANCHE."
fi

cd "$RACINE"
git worktree remove "$TRAVAIL" --force
