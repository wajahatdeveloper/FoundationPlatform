#if UNITY_EDITOR
using UnityEngine;
using AetherNexus.FoundationPlatform.Utilities.Menus;

namespace AetherNexus.FoundationPlatform.Editor.Utilities
{
    [CreateAssetMenu(
        fileName = "DataFolderExemptionMarker",
        menuName = "FoundationPlatform/Exemption Marker")]
    [DesignerFeature(
        "Folder Exemption Marker",
        "Drop into a folder to exclude it and everything under it from content-area ownership and out-of-sync validation.",
        "exempt exemption exclude ignore folder validation unclaimed out of sync manifest skip marker",
        DesignerFeatureKind.Asset,
        "docs/09-EditorHub.md")]
    /// <summary>
    /// Place in a folder to exempt that folder and all descendants from Hub manifest ownership,
    /// drift, and unclaimed-type validation. Types whose defining script lives under the marker
    /// folder (including local assembly roots) are also excluded from manifest population.
    /// </summary>
    [Icon("Packages/com.aethernexus.foundationplatform/Editor/Icons/DataFolderExemptionMarker.png")]
    [DesignerIcon(DesignerSymbol.Tag)]
    public sealed class DataFolderExemptionMarker : ScriptableObject
    {
    }
}
#endif
