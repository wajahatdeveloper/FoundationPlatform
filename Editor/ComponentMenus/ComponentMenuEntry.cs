#if UNITY_EDITOR
using System;
using AetherNexus.FoundationPlatform.DesignerSurfaces.Editor;

namespace AetherNexus.FoundationPlatform.ComponentMenus.Editor
{
    /// <summary>
    /// One first-party component and where the pass thinks it belongs in <c>Add Component</c>.
    /// Written to the proposal file, read back after review, then stamped.
    /// </summary>
    internal sealed class ComponentMenuEntry
    {
        public ComponentMenuEntry(
            Type type,
            string scriptPath,
            string packageRoot,
            string existingMenu,
            bool hasAttribute,
            ComponentMenuVerdict verdict,
            string proposedMenu,
            string reason)
        {
            Type = type;
            ScriptPath = scriptPath;
            PackageRoot = packageRoot;
            ExistingMenu = existingMenu;
            HasAttribute = hasAttribute;
            Verdict = verdict;
            ProposedMenu = proposedMenu;
            Reason = reason;
        }

        public Type Type { get; }
        public string ScriptPath { get; }
        public string PackageRoot { get; }

        /// <summary>Authored path, "" for a deliberately hidden component, null when the attribute is absent.</summary>
        public string ExistingMenu { get; }

        public bool HasAttribute { get; }
        public ComponentMenuVerdict Verdict { get; }

        /// <summary>Path to stamp. Empty string for <see cref="ComponentMenuVerdict.Internal"/>.</summary>
        public string ProposedMenu { get; }

        /// <summary>Why the classifier landed on this verdict, so a wrong call is visible in review.</summary>
        public string Reason { get; }

        public string PackageId => FirstPartyScriptPaths.PackageIdOf(PackageRoot);
    }
}
#endif
