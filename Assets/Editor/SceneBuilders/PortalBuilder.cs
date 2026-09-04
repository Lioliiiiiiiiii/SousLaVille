using SousLaVille.Core;
using SousLaVille.World;
using UnityEditor;
using UnityEngine;

namespace SousLaVille.EditorTools
{
    /// <summary>
    /// Pose un ManholePortal sur un objet deja construit. Factorise entre la surface et le
    /// sous-sol : les deux cotes du passage se decrivent exactement de la meme facon, seules
    /// les couches s'echangent.
    /// </summary>
    public static class PortalBuilder
    {
        /// <summary>
        /// Entre la surface et le sous-sol, la case d'arrivee est la case de depart : les
        /// deux cartes ont la meme taille et partagent le meme repere. Descendre depose donc le
        /// personnage juste sous la bouche.
        ///
        /// Les portes de batiment, elles, donnent leur case d'arrivee : la carte des
        /// interieurs est decoupee en pieces et n'a aucune raison d'etre alignee sur le
        /// village.
        /// </summary>
        public static void Attach(GameObject target, Vector2Int cell, GameLayer layer,
            GameLayer destinationLayer, Vector2Int? destinationCell = null)
        {
            ManholePortal portal = target.AddComponent<ManholePortal>();

            SerializedObject serialized = new SerializedObject(portal);
            serialized.FindProperty("cell").vector2IntValue = cell;
            serialized.FindProperty("layer").enumValueIndex = (int)layer;
            serialized.FindProperty("destinationLayer").enumValueIndex = (int)destinationLayer;
            serialized.FindProperty("destinationCell").vector2IntValue =
                destinationCell ?? cell;
            serialized.ApplyModifiedPropertiesWithoutUndo();
        }
    }
}
