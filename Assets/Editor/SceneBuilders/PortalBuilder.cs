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
        /// La case d'arrivee est la case de depart : les deux cartes font 40x30 et partagent
        /// le meme repere. Descendre depose donc le personnage juste sous la bouche.
        /// </summary>
        public static void Attach(GameObject target, Vector2Int cell, GameLayer layer,
            GameLayer destinationLayer)
        {
            ManholePortal portal = target.AddComponent<ManholePortal>();

            SerializedObject serialized = new SerializedObject(portal);
            serialized.FindProperty("cell").vector2IntValue = cell;
            serialized.FindProperty("layer").enumValueIndex = (int)layer;
            serialized.FindProperty("destinationLayer").enumValueIndex = (int)destinationLayer;
            serialized.FindProperty("destinationCell").vector2IntValue = cell;
            serialized.ApplyModifiedPropertiesWithoutUndo();
        }
    }
}
