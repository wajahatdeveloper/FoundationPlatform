#if UNITY_EDITOR
using System;

namespace AetherNexus.FoundationPlatform.DesignerIcons.Editor
{
    /// <summary>
    /// One designer-facing type and everything the icon pipeline needs to draw, place, and stamp
    /// its script icon. Built by <see cref="DesignerIconCatalog"/>; consumed by the renderer, the
    /// attribute writer, and the coverage report.
    /// </summary>
    internal sealed class DesignerIconEntry
    {
        public DesignerIconEntry(
            Type type,
            string scriptPath,
            string packageRoot,
            string menuPath,
            string domain,
            string monogram,
            bool isAsset,
            bool hasIconAttribute)
        {
            Type = type;
            ScriptPath = scriptPath;
            PackageRoot = packageRoot;
            MenuPath = menuPath;
            Domain = domain;
            Monogram = monogram;
            IsAsset = isAsset;
            HasIconAttribute = hasIconAttribute;
            IconAssetPath = $"{packageRoot}/Editor/Icons/{type.Name}.png";
        }

        public Type Type { get; }
        public string ScriptPath { get; }
        public string PackageRoot { get; }

        /// <summary>`Assets/Create` or `Add Component` path the designer sees, or "" when the type is only [DesignerFeature]-tagged.</summary>
        public string MenuPath { get; }

        /// <summary>Canonical domain bucket, resolved from the menu root (see <see cref="DesignerIconPalette"/>).</summary>
        public string Domain { get; }

        public string Monogram { get; }

        /// <summary>ScriptableObject (document plate) vs MonoBehaviour (component plate).</summary>
        public bool IsAsset { get; }

        public bool HasIconAttribute { get; }

        public string IconAssetPath { get; }

        /// <summary>Package folder name, e.g. <c>com.aethernexus.gameframework</c>.</summary>
        public string PackageId => PackageRoot.Substring("Packages/".Length);
    }
}
#endif
