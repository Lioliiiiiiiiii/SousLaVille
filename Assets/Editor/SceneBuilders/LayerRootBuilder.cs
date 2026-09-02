using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering.Universal;

namespace SousLaVille.EditorTools
{
    /// <summary>
    /// Construit la racine commune aux deux couches de jeu.
    /// Une seule racine par scene : c'est elle que le SceneRouter allume et eteint.
    /// </summary>
    public static class LayerRootBuilder
    {
        /// <param name="targetSortingLayers">
        /// Les Sorting Layers eclaires par la lumiere globale de cette couche. Sans cette
        /// restriction, URP fait porter chaque lumiere globale sur tous les layers du projet,
        /// et les deux couches residentes declenchent une erreur de doublon au chargement.
        /// </param>
        public static GameObject CreateRoot(string rootName, float globalLightIntensity,
            string[] targetSortingLayers)
        {
            GameObject root = new GameObject(rootName);

            // Sans Light2D globale, le Renderer2D affiche des sprites noirs.
            GameObject lightObject = new GameObject("Global Light 2D");
            lightObject.transform.SetParent(root.transform, false);

            Light2D light = lightObject.AddComponent<Light2D>();
            light.lightType = Light2D.LightType.Global;
            light.color = Color.white;
            light.intensity = globalLightIntensity;
            light.targetSortingLayers = ResolveSortingLayers(targetSortingLayers);

            return root;
        }

        private static int[] ResolveSortingLayers(string[] layerNames)
        {
            List<int> ids = new List<int>(layerNames.Length);

            foreach (string layerName in layerNames)
            {
                int id = SortingLayer.NameToID(layerName);

                // NameToID rend 0, l'identifiant de Default, pour un nom inconnu.
                if (!SortingLayer.IsValid(id) || SortingLayer.IDToName(id) != layerName)
                {
                    Debug.LogError($"[Sous la Ville] Sorting Layer manquant : {layerName}. " +
                                   "Lance d'abord « Sous La Ville/Créer les Sorting Layers ».");
                    continue;
                }

                ids.Add(id);
            }

            return ids.ToArray();
        }
    }
}
