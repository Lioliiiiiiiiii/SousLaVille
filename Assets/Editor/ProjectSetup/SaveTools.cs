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

        [MenuItem("Sous La Ville/Ouvrir le dossier de sauvegarde")]
        public static void OpenFolder()
        {
            Directory.CreateDirectory(Folder);
            EditorUtility.RevealInFinder(Folder);
            Debug.Log($"[Sous la Ville] Dossier de sauvegarde : {Folder}");
        }

        /// <summary>
        /// Depuis la phase 20 il y a plusieurs villages : ils sont tous mis de cote, plus
        /// l'ancien fichier d'avant la phase 20, sans quoi la migration les ferait revenir.
        /// </summary>
        [MenuItem("Sous La Ville/Repartir d'une partie neuve")]
        public static void StartFresh()
        {
            string stamp = DateTime.Now.ToString("yyyy-MM-dd-HHmmss", CultureInfo.InvariantCulture);
            int moved = 0;

            for (int slot = 1; slot <= SaveSystem.SlotCount; slot++)
            {
                moved += SetAside(SaveSystem.PathForSlot(slot), $"village-{slot}", stamp) ? 1 : 0;
            }

            moved += SetAside(Path.Combine(Folder, SaveSystem.LegacyFileName), "ancienne", stamp) ? 1 : 0;

            if (moved == 0)
            {
                Debug.Log("[Sous la Ville] Aucune partie enregistrée : la prochaine sera neuve.");
            }
        }

        private static bool SetAside(string path, string label, string stamp)
        {
            if (string.IsNullOrEmpty(path) || !File.Exists(path))
            {
                return false;
            }

            string target = Path.Combine(Folder, $"partie-de-cote-{label}-{stamp}.json");
            File.Move(path, target);
            Debug.Log($"[Sous la Ville] Partie mise de côté, rien n'est perdu : {target}");
            return true;
        }
    }
}
