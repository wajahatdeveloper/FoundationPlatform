#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using UnityEditor;

namespace FoundationPlatform.Editor.Utilities.Validation.UI
{
    internal sealed class UIValidationPostprocessor : AssetPostprocessor
    {
        private const double DebounceSeconds = 0.2;

        private static readonly HashSet<string> PendingPaths = new(StringComparer.OrdinalIgnoreCase);
        private static double _lastEnqueueTime;
        private static bool _debounceTickSubscribed;

        private static void OnPostprocessAllAssets(
            string[] importedAssets,
            string[] deletedAssets,
            string[] movedToAssets,
            string[] movedFromAssets)
        {
            Enqueue(importedAssets);
            Enqueue(movedToAssets);

            if (PendingPaths.Count == 0)
                return;

            _lastEnqueueTime = EditorApplication.timeSinceStartup;
            if (_debounceTickSubscribed)
                return;

            _debounceTickSubscribed = true;
            EditorApplication.update += DebouncedValidationTick;
        }

        private static void DebouncedValidationTick()
        {
            if (EditorApplication.timeSinceStartup - _lastEnqueueTime < DebounceSeconds)
                return;

            EditorApplication.update -= DebouncedValidationTick;
            _debounceTickSubscribed = false;
            RunPendingValidation();
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

            _lastEnqueueTime = EditorApplication.timeSinceStartup;
            if (_debounceTickSubscribed)
                return;

            _debounceTickSubscribed = true;
            EditorApplication.update += DebouncedValidationTick;
        }
    }
}
#endif
