#if UNITY_EDITOR
using System;
using FoundationPlatform.Editor.AssetImport;
using UnityEditor;

namespace FoundationPlatform.StaleComponentGuard.Editor
{
    /// <summary>
    /// Re-scan trigger: when a script (<c>.cs</c>) changes its serialized fields can shift, so any open-scene
    /// stale state must be recomputed. Assembly reload already covers most recompiles; this also catches the
    /// batch import path. Registered via <see cref="AssetImportPluginRegistry"/>.
    /// </summary>
    [InitializeOnLoad]
    public sealed class StaleComponentImportPlugin : IAssetImportBatchPlugin
    {
        static StaleComponentImportPlugin()
        {
            AssetImportPluginRegistry.RegisterBatch(new StaleComponentImportPlugin());
        }

        public int Order => AssetImportPluginOrders.ScriptsHierarchyValidator + 1;

        public bool ShouldRun(string[] imported, string[] deleted, string[] moved, string[] movedFrom)
        {
            return HasScript(imported) || HasScript(deleted) || HasScript(moved);
        }

        public void Run(string[] imported, string[] deleted, string[] moved, string[] movedFrom)
        {
            StaleComponentCache.Invalidate();
        }

        private static bool HasScript(string[] paths)
        {
            if (paths == null)
                return false;
            for (int i = 0; i < paths.Length; i++)
                if (paths[i] != null && paths[i].EndsWith(".cs", StringComparison.OrdinalIgnoreCase))
                    return true;
            return false;
        }
    }
}
#endif
