#if UNITY_EDITOR
using System;
using AetherNexus.FoundationPlatform.DesignerSurfaces.Editor;

namespace AetherNexus.FoundationPlatform.CreateMenus.Editor
{
    /// <summary>One first-party <c>ScriptableObject</c> and whether it is reachable from <c>Assets ▸ Create</c>.</summary>
    internal sealed class CreateMenuEntry
    {
        public CreateMenuEntry(
            Type type,
            string scriptPath,
            string packageRoot,
            string existingMenu,
            CreateMenuVerdict verdict,
            string reason)
        {
            Type = type;
            ScriptPath = scriptPath;
            PackageRoot = packageRoot;
            ExistingMenu = existingMenu;
            ProposedMenu = CreateMenuTaxonomy.Rehome(existingMenu);
            Verdict = verdict;
            Reason = reason;
        }

        public Type Type { get; }
        public string ScriptPath { get; }
        public string PackageRoot { get; }

        /// <summary>Authored menu path, null when the type declares no <c>[CreateAssetMenu]</c>.</summary>
        public string ExistingMenu { get; }

        /// <summary>Path after the native-or-domain-root law. Equals <see cref="ExistingMenu"/> when already home.</summary>
        public string ProposedMenu { get; }

        public CreateMenuVerdict Verdict { get; }

        /// <summary>Why the classifier landed on this verdict, so a wrong call is visible in the report.</summary>
        public string Reason { get; }

        public string PackageId => FirstPartyScriptPaths.PackageIdOf(PackageRoot);
    }
}
#endif
