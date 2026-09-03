using System;
using System.Globalization;
using System.IO;
using SousLaVille.Core;
using UnityEditor;
using UnityEngine;

namespace SousLaVille.EditorTools
{
    /// <summary>
    /// Deux menus pour moi, aucun pour le jeu. Sans eux, tester une partie neuve demanderait
    /// d'aller supprimer un fichier a la main entre deux essais.
    ///
    /// Aucun de ces menus ne detruit quoi que ce soit : « repartir d'une partie neuve » met
    /// l'ancienne de cote au lieu de l'effacer. Un clic malheureux ne doit pas coûter la
    /// partie de Victorien.
    /// </summary>
    public static class SaveTools
    {
        private static string Folder => Application.persistentDataPath;

        private static string FilePath => Path.Combine(Folder, SaveSystem.FileName);

        [MenuItem("Sous La Ville/Ouvrir le dossier de sauvegarde")]
        public static void OpenFolder()
        {
            Directory.CreateDirectory(Folder);
            EditorUtility.RevealInFinder(Folder);
            Debug.Log($"[Sous la Ville] Dossier de sauvegarde : {Folder}");
        }

        [MenuItem("Sous La Ville/Repartir d'une partie neuve")]
        public static void StartFresh()
        {
            if (!File.Exists(FilePath))
            {
                Debug.Log("[Sous la Ville] Aucune partie enregistrée : la prochaine sera neuve.");
                return;
            }

            string stamp = DateTime.Now.ToString("yyyy-MM-dd-HHmmss", CultureInfo.InvariantCulture);
            string target = Path.Combine(Folder, $"partie-de-cote-{stamp}.json");

            File.Move(FilePath, target);
            Debug.Log($"[Sous la Ville] Partie mise de côté, rien n'est perdu : {target}");
        }
    }
}
