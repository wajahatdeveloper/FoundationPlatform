#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using UnityEditor;

namespace FoundationPlatform.Editor.AssetImport
{
    public static class DeferredEditorScheduler
    {
        private static readonly HashSet<string> PendingDelayCallKeys = new(StringComparer.Ordinal);
        private static readonly Dictionary<string, DebouncedEntry> DebouncedEntries = new(StringComparer.Ordinal);
        private static bool _debounceUpdateSubscribed;

        public static void ScheduleOnce(string key, Action action)
        {
            if (string.IsNullOrWhiteSpace(key))
                throw new ArgumentException("Deferred scheduler key must be non-empty.", nameof(key));
            if (action == null)
                throw new ArgumentNullException(nameof(action));
            if (PendingDelayCallKeys.Contains(key))
                return;

            PendingDelayCallKeys.Add(key);
            EditorApplication.delayCall += () =>
            {
                PendingDelayCallKeys.Remove(key);
                action();
            };
        }

        public static void ScheduleDebounced(string key, double debounceSeconds, Action action)
        {
            if (string.IsNullOrWhiteSpace(key))
                throw new ArgumentException("Deferred scheduler key must be non-empty.", nameof(key));
            if (action == null)
                throw new ArgumentNullException(nameof(action));

            if (!DebouncedEntries.TryGetValue(key, out DebouncedEntry entry))
            {
                entry = new DebouncedEntry();
                DebouncedEntries[key] = entry;
            }

            entry.DebounceSeconds = debounceSeconds;
            entry.Action = action;
            entry.LastEnqueueTime = EditorApplication.timeSinceStartup;

            if (_debounceUpdateSubscribed)
                return;

            _debounceUpdateSubscribed = true;
            EditorApplication.update += DebouncedUpdateTick;
        }

        private static void DebouncedUpdateTick()
        {
            if (DebouncedEntries.Count == 0)
            {
                EditorApplication.update -= DebouncedUpdateTick;
                _debounceUpdateSubscribed = false;
                return;
            }

            double now = EditorApplication.timeSinceStartup;
            List<string> readyKeys = null;

            foreach (KeyValuePair<string, DebouncedEntry> pair in DebouncedEntries)
            {
                DebouncedEntry entry = pair.Value;
                if (now - entry.LastEnqueueTime < entry.DebounceSeconds)
                    continue;

                readyKeys ??= new List<string>();
                readyKeys.Add(pair.Key);
            }

            if (readyKeys == null)
                return;

            for (int i = 0; i < readyKeys.Count; i++)
            {
                string key = readyKeys[i];
                if (!DebouncedEntries.TryGetValue(key, out DebouncedEntry entry))
                    continue;

                DebouncedEntries.Remove(key);
                entry.Action?.Invoke();
            }

            if (DebouncedEntries.Count == 0)
            {
                EditorApplication.update -= DebouncedUpdateTick;
                _debounceUpdateSubscribed = false;
            }
        }

        private sealed class DebouncedEntry
        {
            public double DebounceSeconds;
            public double LastEnqueueTime;
            public Action Action;
        }
    }
}
#endif
