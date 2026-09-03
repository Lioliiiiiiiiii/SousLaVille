using SousLaVille.Network;
using SousLaVille.Seasons;
using UnityEditor;
using UnityEngine;

namespace SousLaVille.EditorTools
{
    /// <summary>
    /// Cree les ScriptableObjects que les scenes referencent. Comme l'art placeholder, ils
    /// doivent exister avant la construction des scenes, et sont regeneres par un menu plutot
    /// que crees a la main : reproductible et versionnable.
    ///
    /// Les valeurs sont toujours reecrites, meme sur un asset existant : le menu est la
    /// source de verite, et un reglage change ici se propage d'un clic.
    /// </summary>
    public static class ScriptableObjectSetup
    {
        public const string Folder = "Assets/ScriptableObjects";
        public const string PipeTypeStandard = Folder + "/PipeType_Standard.asset";

        // Les quatre saisons, dans l'ordre du cycle. Le jeu demarre au printemps.
        public const string SeasonSpring = Folder + "/Season_Printemps.asset";
        public const string SeasonSummer = Folder + "/Season_Ete.asset";
        public const string SeasonAutumn = Folder + "/Season_Automne.asset";
        public const string SeasonWinter = Folder + "/Season_Hiver.asset";

        /// <summary>Les quatre saisons dans l'ordre du cycle, tel que le systeme les lira.</summary>
        public static readonly string[] SeasonCycle =
        {
            SeasonSpring, SeasonSummer, SeasonAutumn, SeasonWinter
        };

        [MenuItem("Sous La Ville/Créer les ScriptableObjects")]
        public static void CreateAll()
        {
            // Les saisons portent leur pictogramme : l'art doit exister avant elles.
            if (!PlaceholderArtGenerator.AreAssetsPresent())
            {
                Debug.LogError("[Sous la Ville] Art placeholder absent. Lance d'abord " +
                               "« Sous La Ville/Générer l'art placeholder ».");
                return;
            }

            if (!AssetDatabase.IsValidFolder(Folder))
            {
                AssetDatabase.CreateFolder("Assets", "ScriptableObjects");
            }

            CreatePipeTypes();
            CreateSeasons();

            AssetDatabase.SaveAssets();
            Debug.Log("[Sous la Ville] ScriptableObjects à jour : 1 type de tuyau, 4 saisons.");
        }

        /// <summary>
        /// Le seul type de canalisation pour l'instant. L'usure de 0,1 par saison fait
        /// tomber un tuyau sous le seuil de 0,3 apres sept saisons, soit environ soixante-dix
        /// minutes de jeu : assez lent pour ne jamais surprendre, assez rapide pour que
        /// l'entretien existe.
        ///
        /// Resistance au gel nulle : le tuyau standard gele des le premier hiver. C'est ce
        /// qui donnera un sens aux types de l'usine a tuyaux, en phase 9.
        /// </summary>
        private static void CreatePipeTypes()
        {
            SerializedObject serialized = new SerializedObject(LoadOrCreate<PipeType>(PipeTypeStandard));
            serialized.FindProperty("displayName").stringValue = "Standard";
            serialized.FindProperty("tint").colorValue = Color.white;
            serialized.FindProperty("frostResistance").floatValue = 0f;
            serialized.FindProperty("wearPerSeason").floatValue = 0.1f;
            serialized.ApplyModifiedPropertiesWithoutUndo();
        }

        /// <summary>
        /// Les quatre saisons. C'est le coeur de la boucle : l'hiver gele les tuyaux de
        /// profondeur 1, donc l'hiver recompense ceux qui ont creuse profond.
        ///
        /// L'ete ne fait rien : une saison de repit dans le cycle, pour construire tranquille.
        ///
        /// Les couleurs de lumiere restent claires : une saison ne doit jamais rendre l'ecran
        /// sombre ou illisible.
        /// </summary>
        private static void CreateSeasons()
        {
            WriteSeason(SeasonSpring, "Printemps", PlaceholderArtGenerator.PictoSpring,
                new Color(0.82f, 1f, 0.80f), freezeMaxDepth: 0, clogChance: 0f,
                wearMultiplier: 1f, thaws: true);

            WriteSeason(SeasonSummer, "Été", PlaceholderArtGenerator.PictoSummer,
                new Color(1f, 0.95f, 0.72f), freezeMaxDepth: 0, clogChance: 0f,
                wearMultiplier: 0.5f, thaws: false);

            WriteSeason(SeasonAutumn, "Automne", PlaceholderArtGenerator.PictoAutumn,
                new Color(1f, 0.82f, 0.60f), freezeMaxDepth: 0, clogChance: 0.25f,
                wearMultiplier: 1f, thaws: false);

            WriteSeason(SeasonWinter, "Hiver", PlaceholderArtGenerator.PictoWinter,
                new Color(0.78f, 0.88f, 1f), freezeMaxDepth: 1, clogChance: 0f,
                wearMultiplier: 1.5f, thaws: false);
        }

        private static void WriteSeason(string assetPath, string displayName, string pictoPath,
            Color lightColor, int freezeMaxDepth, float clogChance, float wearMultiplier,
            bool thaws)
        {
            SerializedObject serialized =
                new SerializedObject(LoadOrCreate<SeasonDefinition>(assetPath));

            serialized.FindProperty("displayName").stringValue = displayName;
            serialized.FindProperty("picto").objectReferenceValue =
                AssetDatabase.LoadAssetAtPath<Sprite>(pictoPath);
            serialized.FindProperty("lightColor").colorValue = lightColor;
            serialized.FindProperty("freezeMaxDepth").intValue = freezeMaxDepth;
            serialized.FindProperty("clogChance").floatValue = clogChance;
            serialized.FindProperty("wearMultiplier").floatValue = wearMultiplier;
            serialized.FindProperty("thaws").boolValue = thaws;

            serialized.ApplyModifiedPropertiesWithoutUndo();
        }

        private static T LoadOrCreate<T>(string assetPath) where T : ScriptableObject
        {
            T asset = AssetDatabase.LoadAssetAtPath<T>(assetPath);
            if (asset != null)
            {
                return asset;
            }

            asset = ScriptableObject.CreateInstance<T>();
            AssetDatabase.CreateAsset(asset, assetPath);

            return asset;
        }

        /// <summary>Vrai si tous les ScriptableObjects attendus sont sur le disque.</summary>
        public static bool ArePresent()
        {
            if (AssetDatabase.LoadAssetAtPath<PipeType>(PipeTypeStandard) == null)
            {
                return false;
            }

            foreach (string path in SeasonCycle)
            {
                if (AssetDatabase.LoadAssetAtPath<SeasonDefinition>(path) == null)
                {
                    return false;
                }
            }

            return true;
        }
    }
}
