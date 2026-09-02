using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace SousLaVille.EditorTools
{
    /// <summary>
    /// Cree les Sorting Layers du jeu, du plus lointain au plus proche.
    ///
    /// Une famille par couche. Les deux couches restent chargees en meme temps, donc leurs
    /// deux Light2D globales coexistent : si elles couvraient les memes Sorting Layers,
    /// URP signalerait un doublon de lumiere globale a chaque chargement. Chaque famille
    /// est eclairee par sa seule lumiere.
    /// </summary>
    public static class SortingLayerSetup
    {
        public static readonly string[] SurfaceLayers =
        {
            "Surface_Ground",   // herbe, chemins, dalles
            "Surface_Pipes",    // raccordements visibles en surface
            "Surface_Water",    // flaques, fontaine
            "Surface_Entities", // joueur, maisons, batiments
            "Surface_Overlay"   // pictogrammes et retours visuels
        };

        public static readonly string[] UndergroundLayers =
        {
            "Underground_Ground",   // terre, roche
            "Underground_Pipes",    // canalisations posees
            "Underground_Water",    // particules d'eau, geysers
            "Underground_Entities", // joueur, bouches, station
            "Underground_Overlay"   // pictogrammes et retours visuels
        };

        /// <summary>Noms non prefixes de la phase 0, remplaces par les deux familles.</summary>
        private static readonly string[] LegacyLayers =
        {
            "Ground", "Pipes", "Water", "Entities", "Overlay"
        };

        [MenuItem("Sous La Ville/Créer les Sorting Layers")]
        public static void CreateSortingLayers()
        {
            Object[] assets = AssetDatabase.LoadAllAssetsAtPath("ProjectSettings/TagManager.asset");
            if (assets == null || assets.Length == 0)
            {
                Debug.LogError("[Sous la Ville] TagManager.asset introuvable.");
                return;
            }

            SerializedObject tagManager = new SerializedObject(assets[0]);
            SerializedProperty sortingLayers = tagManager.FindProperty("m_SortingLayers");

            int removed = RemoveLayers(sortingLayers, LegacyLayers);
            int created = 0;
            int repaired = 0;

            foreach (string layerName in SurfaceLayers)
            {
                EnsureLayer(sortingLayers, layerName, ref created, ref repaired);
            }

            foreach (string layerName in UndergroundLayers)
            {
                EnsureLayer(sortingLayers, layerName, ref created, ref repaired);
            }

            tagManager.ApplyModifiedProperties();
            AssetDatabase.SaveAssets();

            Debug.Log($"[Sous la Ville] Sorting Layers : {created} créé(s), {repaired} réparé(s), " +
                      $"{removed} ancien(s) supprimé(s).");
        }

        /// <summary>Vrai si les dix layers existent et portent un identifiant exploitable.</summary>
        public static bool AreLayersRegistered()
        {
            foreach (string layerName in SurfaceLayers)
            {
                if (!IsRegistered(layerName))
                {
                    return false;
                }
            }

            foreach (string layerName in UndergroundLayers)
            {
                if (!IsRegistered(layerName))
                {
                    return false;
                }
            }

            return true;
        }

        /// <summary>
        /// NameToID rend 0, l'identifiant de Default, pour un nom inconnu : il faut donc
        /// verifier le nom en retour et pas seulement la validite de l'identifiant.
        /// </summary>
        private static bool IsRegistered(string layerName)
        {
            int id = SortingLayer.NameToID(layerName);
            return SortingLayer.IsValid(id) && SortingLayer.IDToName(id) == layerName;
        }

        private static void EnsureLayer(SerializedProperty sortingLayers, string layerName,
            ref int created, ref int repaired)
        {
            int index = IndexOf(sortingLayers, layerName);

            if (index < 0)
            {
                sortingLayers.InsertArrayElementAtIndex(sortingLayers.arraySize);
                SerializedProperty added = sortingLayers.GetArrayElementAtIndex(sortingLayers.arraySize - 1);
                added.FindPropertyRelative("name").stringValue = layerName;
                added.FindPropertyRelative("uniqueID").intValue = StableHash(layerName);
                added.FindPropertyRelative("locked").boolValue = false;
                created++;
                return;
            }

            // Un layer dont l'identifiant vaut 0 se confond avec Default : Unity refuse les
            // identifiants negatifs et les ecrit en 0. On le repare.
            SerializedProperty existing = sortingLayers.GetArrayElementAtIndex(index);
            SerializedProperty uniqueId = existing.FindPropertyRelative("uniqueID");
            if (uniqueId.intValue == 0)
            {
                uniqueId.intValue = StableHash(layerName);
                repaired++;
            }
        }

        private static int RemoveLayers(SerializedProperty sortingLayers, string[] layerNames)
        {
            int removed = 0;

            // Parcours a l'envers : supprimer decale les indices suivants.
            for (int i = sortingLayers.arraySize - 1; i >= 0; i--)
            {
                string name = sortingLayers.GetArrayElementAtIndex(i).FindPropertyRelative("name").stringValue;
                if (System.Array.IndexOf(layerNames, name) < 0)
                {
                    continue;
                }

                sortingLayers.DeleteArrayElementAtIndex(i);
                removed++;
            }

            return removed;
        }

        private static int IndexOf(SerializedProperty sortingLayers, string layerName)
        {
            for (int i = 0; i < sortingLayers.arraySize; i++)
            {
                if (sortingLayers.GetArrayElementAtIndex(i).FindPropertyRelative("name").stringValue == layerName)
                {
                    return i;
                }
            }

            return -1;
        }

        /// <summary>
        /// Hash FNV-1a masque sur 31 bits. Deterministe d'une execution a l'autre,
        /// contrairement a GetHashCode, et surtout toujours positif : Unity ecrit 0 a la
        /// place d'un identifiant negatif, ce qui confond le layer avec Default.
        /// </summary>
        private static int StableHash(string value)
        {
            unchecked
            {
                uint hash = 2166136261u;
                foreach (char c in value)
                {
                    hash ^= c;
                    hash *= 16777619u;
                }

                int id = (int)(hash & 0x7FFFFFFF);
                return id == 0 ? 1 : id;
            }
        }
    }
}
