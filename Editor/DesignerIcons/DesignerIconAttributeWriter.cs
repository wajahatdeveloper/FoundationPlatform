#if UNITY_EDITOR
using AetherNexus.FoundationPlatform.DesignerSurfaces.Editor;

namespace AetherNexus.FoundationPlatform.DesignerIcons.Editor
{
    /// <summary>
    /// Stamps <c>[Icon("Packages/&lt;pkg&gt;/Editor/Icons/&lt;Type&gt;.png")]</c> onto the type's own
    /// declaration.
    /// </summary>
    internal static class DesignerIconAttributeWriter
    {
        internal static bool Stamp(DesignerIconEntry entry)
        {
            return SourceAttributeWriter.InsertAboveDeclaration(
                entry.ScriptPath,
                entry.Type.Name,
                $"[Icon(\"{entry.IconAssetPath}\")]",
                "[Icon(",
                "using UnityEngine;");
        }
    }
}
#endif
