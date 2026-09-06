using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEditor.Build.Reporting;
using UnityEngine;

namespace SousLaVille.EditorTools
{
    /// <summary>
    /// Fabrique le jeu pour le navigateur, puis le depose dans Build/Web.
    ///
    /// C'est la seule facon de mettre le jeu entre les mains de quelqu'un qui n'a ni Unity ni
    /// le projet : une adresse, un clic, et il joue. Un executable macOS non signe obligerait
    /// l'autre foyer a passer par les reglages de securite du systeme.
    /// </summary>
    public static class WebBuild
    {
        /// <summary>Sortie du build, hors de Assets et ignoree par git (voir .gitignore).</summary>
        public const string OutputDirectory = "Build/Web";

        /// <summary>Le gabarit ecrit pour ce jeu, dans Assets/WebGLTemplates/SousLaVille.</summary>
        private const string TemplateName = "PROJECT:SousLaVille";

        [MenuItem("Sous La Ville/Construire le jeu pour le web")]
        public static void BuildForWeb()
        {
            ApplySettings();

            string[] scenes = EnabledScenes();
            if (scenes.Length == 0)
            {
                Debug.LogError("[Sous la Ville] Aucune scene activee dans les Build Settings.");
                return;
            }

            // Le dossier est efface avant chaque build : un reliquat d'un build precedent
            // (ancien nom de fichier compresse, par exemple) ferait echouer le chargement.
            if (Directory.Exists(OutputDirectory))
            {
                Directory.Delete(OutputDirectory, true);
            }

            Directory.CreateDirectory(OutputDirectory);

            BuildPlayerOptions options = new BuildPlayerOptions
            {
                scenes = scenes,
                locationPathName = OutputDirectory,
                target = BuildTarget.WebGL,
                targetGroup = BuildTargetGroup.WebGL,
                options = BuildOptions.None,
            };

            BuildReport report = BuildPipeline.BuildPlayer(options);
            BuildSummary summary = report.summary;

            if (summary.result == BuildResult.Succeeded)
            {
                double megaoctets = summary.totalSize / (1024.0 * 1024.0);
                Debug.Log($"[Sous la Ville] Build web reussi : {megaoctets:F1} Mo dans {OutputDirectory}, " +
                          $"en {summary.totalTime.TotalSeconds:F0} s.");
            }
            else
            {
                Debug.LogError($"[Sous la Ville] Build web {summary.result} : {summary.totalErrors} erreur(s).");
            }
        }

        /// <summary>
        /// Les reglages que le build web exige. Ils sont poses ici et non a la main dans les
        /// Player Settings : un reglage pose a la main se perd, celui-ci est versionne.
        /// </summary>
        [MenuItem("Sous La Ville/Regler le build web")]
        public static void ApplySettings()
        {
            if (EditorUserBuildSettings.activeBuildTarget != BuildTarget.WebGL)
            {
                EditorUserBuildSettings.SwitchActiveBuildTarget(BuildTargetGroup.WebGL, BuildTarget.WebGL);
            }

            PlayerSettings.WebGL.template = TemplateName;

            // GitHub Pages ne pose pas l'en-tete Content-Encoding sur les fichiers qu'il sert.
            // Sans le repli, le navigateur recevrait du gzip brut et ne saurait pas le lire :
            // le jeu resterait bloque sur la barre de chargement. Avec le repli, c'est le
            // chargeur d'Unity qui decompresse en JavaScript. Gzip et non Brotli : la
            // decompression en JavaScript y est nettement plus rapide, et le demarrage compte
            // plus que quelques megaoctets sur un jeu de cette taille.
            PlayerSettings.WebGL.compressionFormat = WebGLCompressionFormat.Gzip;
            PlayerSettings.WebGL.decompressionFallback = true;

            // Le navigateur garde les fichiers du jeu : la deuxieme visite demarre tout de suite.
            PlayerSettings.WebGL.dataCaching = true;

            PlayerSettings.WebGL.linkerTarget = WebGLLinkerTarget.Wasm;

            // La fenetre du navigateur donne la taille ; ces valeurs ne servent qu'au canvas
            // avant que le gabarit ne l'etire sur toute la page. 16/9, comme le 320x180.
            PlayerSettings.defaultWebScreenWidth = 960;
            PlayerSettings.defaultWebScreenHeight = 540;

            AssetDatabase.SaveAssets();
        }

        private static string[] EnabledScenes()
        {
            List<string> scenes = new List<string>();

            foreach (EditorBuildSettingsScene scene in EditorBuildSettings.scenes)
            {
                if (scene.enabled)
                {
                    scenes.Add(scene.path);
                }
            }

            return scenes.ToArray();
        }
    }
}
