#if UNITY_EDITOR
using System.IO;
using UnityEditor;

namespace AetherNexus.FoundationPlatform.Utilities.Menus
{
    /// <summary>
    /// Shared project-folder helper for editor asset generators. Consolidates the recursive
    /// <c>EnsureFolder</c> that was copy-pasted across the AI/Character generators and
    /// <c>PresetGen</c>. Lives in FoundationPlatform.Editor so every framework editor asmdef
    /// (all reference it) can call the single implementation.
    /// </summary>
    public static class EditorAssetFolders
    {
        /// <summary>Recursively creates a project folder (e.g. "Assets/Content/Domains/.../Presets").</summary>
        public static void EnsureFolder(string folder)
        {
            if (string.IsNullOrEmpty(folder)) return;
            folder = folder.Replace('\\', '/').TrimEnd('/');
            if (folder == "Assets" || AssetDatabase.IsValidFolder(folder))
                return;

            string parent = Path.GetDirectoryName(folder).Replace('\\', '/');
            string leaf = Path.GetFileName(folder);
            EnsureFolder(parent);
            AssetDatabase.CreateFolder(parent, leaf);
        }
    }
}
#endif
