using SousLaVille.Buildings;
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
        public const string PipeTypeInsulated = Folder + "/PipeType_Isole.asset";
        public const string PipeTypeGrated = Folder + "/PipeType_Grillage.asset";

        /// <summary>
        /// Les trois types, dans l'ordre du catalogue. Le rang 0 est le standard : c'est ce
        /// que la pose donne par defaut, et ce que la sauvegarde n'ecrit pas.
        /// </summary>
        public static readonly string[] PipeTypes =
        {
            PipeTypeStandard, PipeTypeInsulated, PipeTypeGrated
        };

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

        /// <summary>Chemin de la plaque d'egout de rang donne, dans l'ordre du catalogue.</summary>
        public static string CoverAsset(int index)
        {
            return $"{Folder}/Cover_{PlaceholderArtGenerator.CoverNames[index].Replace(" ", string.Empty)}.asset";
        }

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
            CreateCovers();

            AssetDatabase.SaveAssets();
            Debug.Log("[Sous la Ville] ScriptableObjects à jour : " + PipeTypes.Length
                      + " types de tuyau, 4 saisons, "
                      + PlaceholderArtGenerator.CoverCount + " plaques.");
        }

        /// <summary>
        /// Les trois types de canalisation, un par menace de saison.
        ///
        /// L'usure de 0,1 par saison fait tomber un tuyau sous le seuil de 0,3 apres sept
        /// saisons, soit environ soixante-dix minutes de jeu : assez lent pour ne jamais
        /// surprendre, assez rapide pour que l'entretien existe. ELLE EST LA MEME POUR LES
        /// TROIS : un tuyau qui ne s'use pas rendrait la cle inutile.
        ///
        /// Les resistances sont des probabilites de TENIR. Elles valent 0 ou 1 aujourd'hui,
        /// mais le champ reste une probabilite, comme le gel depuis la phase 3 : un type
        /// intermediaire ne demanderait pas une ligne de code.
        ///
        /// Chaque type ne resiste qu'a UNE menace. L'hiver reste un probleme pour le
        /// grillage, l'automne pour l'isole : c'est ce qui fait qu'il y a un choix.
        /// </summary>
        private static void CreatePipeTypes()
        {
            CreatePipeType(PipeTypeStandard, "Standard", pattern: 0,
                frost: 0f, leaf: 0f, seasonIcon: null);

            CreatePipeType(PipeTypeInsulated, "Isolé", pattern: 1,
                frost: 1f, leaf: 0f, seasonIcon: PlaceholderArtGenerator.PictoWinter);

            CreatePipeType(PipeTypeGrated, "Grillagé", pattern: 2,
                frost: 0f, leaf: 1f, seasonIcon: PlaceholderArtGenerator.PictoAutumn);
        }

        private static void CreatePipeType(string path, string displayName, int pattern,
            float frost, float leaf, string seasonIcon)
        {
            SerializedObject serialized = new SerializedObject(LoadOrCreate<PipeType>(path));
            serialized.FindProperty("displayName").stringValue = displayName;
            serialized.FindProperty("tint").colorValue = Color.white;
            serialized.FindProperty("frostResistance").floatValue = frost;
            serialized.FindProperty("leafResistance").floatValue = leaf;
            serialized.FindProperty("wearPerSeason").floatValue = 0.1f;
            serialized.FindProperty("patternIndex").intValue = pattern;

            // L'echantillon EST la tuile posee en jeu, masque est-ouest : le picto de pose ne
            // peut donc pas mentir sur ce qu'il va poser.
            serialized.FindProperty("sample").objectReferenceValue =
                LoadSprite(PlaceholderArtGenerator.PipeTexture(
                    pattern, PlaceholderArtGenerator.PipeSampleMask));

            serialized.FindProperty("nameImage").objectReferenceValue =
                LoadSprite(PlaceholderArtGenerator.PipeNameTexture(pattern));

            serialized.FindProperty("defeatedSeasonIcon").objectReferenceValue =
                seasonIcon != null ? LoadSprite(seasonIcon) : null;

            serialized.ApplyModifiedPropertiesWithoutUndo();
        }

        private static Sprite LoadSprite(string path)
        {
            Sprite sprite = AssetDatabase.LoadAssetAtPath<Sprite>(path);
            if (sprite == null)
            {
                Debug.LogError($"[Sous la Ville] Sprite introuvable : {path}");
            }

            return sprite;
        }

        /// <summary>
        /// Les quatre saisons. C'est le coeur de la boucle : l'hiver gele les tuyaux de
        /// profondeur 1, donc l'hiver recompense ceux qui ont creuse profond.
        ///
        /// L'ete ne fait rien : une saison de repit dans le cycle, pour construire tranquille.
        ///
        /// La pluie, phase 8 : 2 / 0 / 8 / 1. L'automne apporte plus que la station ne traite,
        /// c'est la saison qui met le reseau a l'epreuve et fait exister le bassin d'orage.
        ///
        /// Les couleurs de lumiere restent claires : une saison ne doit jamais rendre l'ecran
        /// sombre ou illisible.
        /// </summary>
        private static void CreateSeasons()
        {
            WriteSeason(SeasonSpring, "Printemps", PlaceholderArtGenerator.PictoSpring,
                new Color(0.82f, 1f, 0.80f), freezeMaxDepth: 0, clogChance: 0f,
                wearMultiplier: 1f, thaws: true, rainVolume: 2);

            WriteSeason(SeasonSummer, "Été", PlaceholderArtGenerator.PictoSummer,
                new Color(1f, 0.95f, 0.72f), freezeMaxDepth: 0, clogChance: 0f,
                wearMultiplier: 0.5f, thaws: false, rainVolume: 0);

            WriteSeason(SeasonAutumn, "Automne", PlaceholderArtGenerator.PictoAutumn,
                new Color(1f, 0.82f, 0.60f), freezeMaxDepth: 0, clogChance: 0.25f,
                wearMultiplier: 1f, thaws: false, rainVolume: 8);

            WriteSeason(SeasonWinter, "Hiver", PlaceholderArtGenerator.PictoWinter,
                new Color(0.78f, 0.88f, 1f), freezeMaxDepth: 1, clogChance: 0f,
                wearMultiplier: 1.5f, thaws: false, rainVolume: 1);
        }

        private static void WriteSeason(string assetPath, string displayName, string pictoPath,
            Color lightColor, int freezeMaxDepth, float clogChance, float wearMultiplier,
            bool thaws, int rainVolume)
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
            serialized.FindProperty("rainVolume").intValue = rainVolume;

            serialized.ApplyModifiedPropertiesWithoutUndo();
        }

        /// <summary>
        /// Les huit plaques du catalogue. Chacune porte son image et l'image de son nom :
        /// ce sont les huit seuls mots du jeu.
        /// </summary>
        private static void CreateCovers()
        {
            for (int index = 0; index < PlaceholderArtGenerator.CoverCount; index++)
            {
                SerializedObject serialized =
                    new SerializedObject(LoadOrCreate<ManholeCoverDefinition>(CoverAsset(index)));

                serialized.FindProperty("displayName").stringValue =
                    PlaceholderArtGenerator.CoverNames[index];
                serialized.FindProperty("cover").objectReferenceValue =
                    AssetDatabase.LoadAssetAtPath<Sprite>(PlaceholderArtGenerator.CoverTexture(index));
                serialized.FindProperty("nameImage").objectReferenceValue =
                    AssetDatabase.LoadAssetAtPath<Sprite>(PlaceholderArtGenerator.CoverNameTexture(index));

                serialized.ApplyModifiedPropertiesWithoutUndo();
            }
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
            foreach (string path in PipeTypes)
            {
                if (AssetDatabase.LoadAssetAtPath<PipeType>(path) == null)
                {
                    return false;
                }
            }

            foreach (string path in SeasonCycle)
            {
                if (AssetDatabase.LoadAssetAtPath<SeasonDefinition>(path) == null)
                {
                    return false;
                }
            }

            for (int index = 0; index < PlaceholderArtGenerator.CoverCount; index++)
            {
                if (AssetDatabase.LoadAssetAtPath<ManholeCoverDefinition>(CoverAsset(index)) == null)
                {
                    return false;
                }
            }

            return true;
        }
    }
}
