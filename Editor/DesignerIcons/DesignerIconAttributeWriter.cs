#if UNITY_EDITOR
using AetherNexus.FoundationPlatform.DesignerSurfaces.Editor;
using AetherNexus.FoundationPlatform.Utilities.Menus;

namespace AetherNexus.FoundationPlatform.DesignerIcons.Editor
{
    /// <summary>
    /// Stamps the two attributes that bind a type to its icon: <c>[DesignerIcon]</c>, which says
    /// what to draw, and Unity's <c>[Icon]</c>, which points at the PNG that was drawn.
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

        internal static bool StampSymbol(DesignerIconEntry entry, DesignerSymbol symbol)
        {
            return SourceAttributeWriter.InsertAboveDeclaration(
                entry.ScriptPath,
                entry.Type.Name,
                $"[DesignerIcon(DesignerSymbol.{symbol})]",
                "[DesignerIcon(",
                "using AetherNexus.FoundationPlatform.Utilities.Menus;");
        }
    }
}
#endif
