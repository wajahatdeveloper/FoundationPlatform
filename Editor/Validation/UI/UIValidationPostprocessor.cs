#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using AetherNexus.FoundationPlatform.Editor.AssetImport;
using UnityEditor;

namespace AetherNexus.FoundationPlatform.Editor.Utilities.Validation.UI
{
    [InitializeOnLoad]
    internal static class UIValidationImportPluginRegistration
    {
        static UIValidationImportPluginRegistration()
        {
            AssetImportPluginRegistry.RegisterBatch(new UIValidationImportPlugin());
        }
    }

    internal sealed class UIValidationImportPlugin : IAssetImportBatchPlugin
    {
        private const double DebounceSeconds = 0.2;
        private const string SchedulerKey = "ui-validation-import";

        private static readonly HashSet<string> PendingPaths = new(StringComparer.OrdinalIgnoreCase);

        public int Order => AssetImportPluginOrders.UIValidation;

        public bool ShouldRun(
            string[] importedAssets,
            string[] deletedAssets,
            string[] movedAssets,
            string[] movedFromAssetPaths)
        {
            return HasValidationPath(importedAssets) || HasValidationPath(movedAssets);
        }

        public void Run(
            string[] importedAssets,
            string[] deletedAssets,
            string[] movedAssets,
            string[] movedFromAssetPaths)
        {
            Enqueue(importedAssets);
            Enqueue(movedAssets);

            if (PendingPaths.Count == 0)
                return;

            DeferredEditorScheduler.ScheduleDebounced(SchedulerKey, DebounceSeconds, RunPendingValidation);
        }

        private static bool HasValidationPath(string[] paths)
        {
            if (paths == null || paths.Length == 0)
                return false;

            for (int i = 0; i < paths.Length; i++)
            {
                string path = paths[i];
                if (string.IsNullOrEmpty(path))
                    continue;

                string normalized = path.Replace('\\', '/');
                if (UIValidationConventions.TriggersIncrementalValidation(normalized))
                    return true;
            }

            return false;
        }

        private static void Enqueue(string[] paths)
        {
            if (paths == null || paths.Length == 0)
                return;

            for (int i = 0; i < paths.Length; i++)
            {
                string path = paths[i];
                if (string.IsNullOrEmpty(path))
                    continue;

                string normalized = path.Replace('\\', '/');
                if (!UIValidationConventions.TriggersIncrementalValidation(normalized))
                    continue;

                PendingPaths.Add(normalized);
            }
        }

        private static void RunPendingValidation()
        {
            if (PendingPaths.Count == 0)
                return;

            string[] paths = new string[PendingPaths.Count];
            PendingPaths.CopyTo(paths);
            PendingPaths.Clear();

            UIValidationResult result = UIValidationEngine.ValidatePaths(paths, fullScan: false);
            if (result.ScannedPathCount == 0 && result.Issues.Count == 0)
                return;

            UIValidationReporter.Report(result, "Changed UI assets");

            if (PendingPaths.Count == 0)
                return;

            DeferredEditorScheduler.ScheduleDebounced(SchedulerKey, DebounceSeconds, RunPendingValidation);
        }
    }
}
#endif
