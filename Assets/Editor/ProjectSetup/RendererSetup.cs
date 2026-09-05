using UnityEditor;
using UnityEngine;

namespace SousLaVille.EditorTools
{
    /// <summary>
    /// LE TRI PAR Y, phase 18b. Les sprites d'un meme Sorting Layer et d'un meme ordre se
    /// trient par leur position le long de l'axe Y : ce qui est plus au nord est plus loin,
    /// donc dessine d'abord. C'est ce qui permet a une frondaison de deux cases de recouvrir le
    /// joueur quand il passe derriere l'arbre, et au joueur de recouvrir le tronc quand il
    /// passe devant.
    ///
    /// Le reglage vit dans l'asset du Renderer2D, sous Assets/Settings — PAS dans les
    /// ProjectSettings, que le projet ne touche pas (PIEGES.md). Pose par script comme tout le
    /// reste, et verifie a chaque construction de scenes.
    /// </summary>
    public static class RendererSetup
    {
        public const string RendererAssetPath = "Assets/Settings/Renderer2D.asset";

        /// <summary>La valeur de UnityEngine.TransparencySortMode.CustomAxis, telle que serialisee.</summary>
        private const int CustomAxisMode = (int)TransparencySortMode.CustomAxis;

        private static readonly Vector3 UpAxis = new Vector3(0f, 1f, 0f);

        [MenuItem("Sous La Ville/Régler le tri par Y")]
        public static void EnableYSort()
        {
            SerializedObject renderer = LoadRenderer();
            if (renderer == null)
            {
                return;
            }

            SerializedProperty mode = renderer.FindProperty("m_TransparencySortMode");
            SerializedProperty axis = renderer.FindProperty("m_TransparencySortAxis");

            if (mode == null || axis == null)
            {
                Debug.LogError("[Sous la Ville] L'asset du Renderer2D ne porte pas les champs de tri " +
                               "attendus : la version d'URP a change, relis RendererSetup.");
                return;
            }

            bool changed = mode.intValue != CustomAxisMode || axis.vector3Value != UpAxis;

            mode.intValue = CustomAxisMode;
            axis.vector3Value = UpAxis;
            renderer.ApplyModifiedPropertiesWithoutUndo();

            if (changed)
            {
                AssetDatabase.SaveAssets();
                Debug.Log("[Sous la Ville] Tri par Y active dans le Renderer2D : axe (0, 1, 0).");
            }
        }

        /// <summary>Vrai si le Renderer2D trie bien par l'axe Y. Relu dans l'asset, jamais suppose.</summary>
        public static bool IsYSortEnabled()
        {
            SerializedObject renderer = LoadRenderer();
            if (renderer == null)
            {
                return false;
            }

            SerializedProperty mode = renderer.FindProperty("m_TransparencySortMode");
            SerializedProperty axis = renderer.FindProperty("m_TransparencySortAxis");

            return mode != null && axis != null
                && mode.intValue == CustomAxisMode
                && axis.vector3Value == UpAxis;
        }

        private static SerializedObject LoadRenderer()
        {
            ScriptableObject asset = AssetDatabase.LoadAssetAtPath<ScriptableObject>(RendererAssetPath);
            if (asset == null)
            {
                Debug.LogError($"[Sous la Ville] Renderer2D introuvable : {RendererAssetPath}.");
                return null;
            }

            return new SerializedObject(asset);
        }
    }
}
