#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using UnityEditor;

namespace FoundationPlatform.Editor.AssetImport
{
    using DebugX;
    
    public static class AssetImportPluginRegistry
    {
        private static readonly List<IAssetImportBatchPlugin> BatchPlugins = new();
        private static readonly List<IAssetPreprocessPlugin> PreprocessPlugins = new();

        public static void RegisterBatch(IAssetImportBatchPlugin plugin)
        {
            if (plugin == null)
                throw new ArgumentNullException(nameof(plugin));

            BatchPlugins.Add(plugin);
            BatchPlugins.Sort((a, b) => a.Order.CompareTo(b.Order));
        }

        public static void RegisterPreprocess(IAssetPreprocessPlugin plugin)
        {
            if (plugin == null)
                throw new ArgumentNullException(nameof(plugin));

            PreprocessPlugins.Add(plugin);
        }

        public static void InvokeBatch(
            string[] importedAssets,
            string[] deletedAssets,
            string[] movedAssets,
            string[] movedFromAssetPaths)
        {
            for (int i = 0; i < BatchPlugins.Count; i++)
            {
                IAssetImportBatchPlugin plugin = BatchPlugins[i];
                try
                {
                    if (!plugin.ShouldRun(importedAssets, deletedAssets, movedAssets, movedFromAssetPaths))
                        continue;

                    plugin.Run(importedAssets, deletedAssets, movedAssets, movedFromAssetPaths);
                }
                catch (Exception ex)
                {
                    DebugX.Error(ex, "[AssetImportPluginRegistry] Batch plugin failed ({PluginType}): {Message}",
                        plugin.GetType().FullName, ex.Message);
                }
            }
        }

        public static void InvokePreprocess(string assetPath, AssetPostprocessor host)
        {
            for (int i = 0; i < PreprocessPlugins.Count; i++)
            {
                IAssetPreprocessPlugin plugin = PreprocessPlugins[i];
                try
                {
                    if (!plugin.CanPreprocess(assetPath))
                        continue;

                    plugin.OnPreprocess(assetPath, host);
                }
                catch (Exception ex)
                {
                    DebugX.Error(ex, "[AssetImportPluginRegistry] Preprocess plugin failed ({PluginType}): {Message}",
                        plugin.GetType().FullName, ex.Message);
                }
            }
        }
    }
}
#endif
