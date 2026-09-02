using SousLaVille.Network;
using UnityEditor;
using UnityEngine;

namespace SousLaVille.EditorTools
{
    /// <summary>
    /// Cree les ScriptableObjects que les scenes referencent. Comme l'art placeholder, ils
    /// doivent exister avant la construction des scenes, et sont regeneres par un menu plutot
    /// que crees a la main : reproductible et versionnable.
    /// </summary>
    public static class ScriptableObjectSetup
    {
        public const string Folder = "Assets/ScriptableObjects";
        public const string PipeTypeStandard = Folder + "/PipeType_Standard.asset";

        [MenuItem("Sous La Ville/Créer les ScriptableObjects")]
        public static void CreateAll()
        {
            if (!AssetDatabase.IsValidFolder(Folder))
            {
                AssetDatabase.CreateFolder("Assets", "ScriptableObjects");
            }

            int created = 0;

            if (AssetDatabase.LoadAssetAtPath<PipeType>(PipeTypeStandard) == null)
            {
                PipeType pipeType = ScriptableObject.CreateInstance<PipeType>();
                AssetDatabase.CreateAsset(pipeType, PipeTypeStandard);
                created++;
            }

            AssetDatabase.SaveAssets();
            Debug.Log($"[Sous la Ville] ScriptableObjects : {created} créé(s).");
        }

        /// <summary>Vrai si tous les ScriptableObjects attendus sont sur le disque.</summary>
        public static bool ArePresent()
        {
            return AssetDatabase.LoadAssetAtPath<PipeType>(PipeTypeStandard) != null;
        }
    }
}
