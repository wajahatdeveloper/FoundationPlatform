#if UNITY_EDITOR
using AetherNexus.FoundationPlatform.DesignerSurfaces.Editor;

namespace AetherNexus.FoundationPlatform.ComponentMenus.Editor
{
    /// <summary>
    /// Stamps <c>[AddComponentMenu("...")]</c> onto the component's own declaration. An empty path
    /// is the deliberate "hide from Add Component" case, so it is written the same way.
    /// </summary>
    internal static class ComponentMenuAttributeWriter
    {
        internal static bool Stamp(string scriptPath, string typeName, string menuPath)
        {
            return SourceAttributeWriter.InsertAboveDeclaration(
                scriptPath,
                typeName,
                $"[AddComponentMenu(\"{menuPath}\")]",
                "[AddComponentMenu(",
                "using UnityEngine;");
        }
    }
}
#endif
