using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace EditorEnhancerX {
    /// <summary>Where a shortcut is allowed to fire from.</summary>
    [Flags]
    public enum KeyScope {
        SceneView = 1,
        Hierarchy = 2,
        Global = 4,
        Any = SceneView | Hierarchy | Global,
    }

    /// <summary>
    /// Central shortcut dispatcher. Features register a live binding accessor + an execute callback;
    /// the router matches KeyDown events from its capture tiers and consumes the event on success.
    ///
    /// Tier 1 (public APIs, always on): SceneView.duringSceneGui + a hierarchy-context feed from
    /// hierarchyWindowItemOnGUI (first row callback per event). Tier 2 (opt-in, reflection):
    /// global editor key capture — attached in a later phase behind a settings toggle.
    /// </summary>
    [InitializeOnLoad]
    public static class KeyRouter {

        private sealed class Entry {
            public string id;
            public Func<ShortcutBinding> binding;
            public KeyScope scope;
            public Func<bool> execute; // returns true = handled, event consumed
        }

        private static readonly List<Entry> entries = new List<Entry>();
        private static Event lastHierarchyEvent;

        static KeyRouter() {
            SceneView.duringSceneGui += OnSceneGui;
            EditorApplication.hierarchyWindowItemOnGUI += OnHierarchyItemGui;
        }

        /// <summary>Registers a shortcut. The binding is read live on every dispatch, so settings edits apply instantly.</summary>
        public static void Register(string id, Func<ShortcutBinding> binding, KeyScope scope, Func<bool> execute) {
            if (string.IsNullOrEmpty(id) || binding == null || execute == null) return;
            entries.RemoveAll(x => x.id == id);
            entries.Add(new Entry { id = id, binding = binding, scope = scope, execute = execute });
        }

        public static void Unregister(string id) {
            entries.RemoveAll(x => x.id == id);
        }

        private static void OnSceneGui(SceneView view) {
            Dispatch(Event.current, KeyScope.SceneView);
        }

        private static void OnHierarchyItemGui(int instanceId, Rect rect) {
            var e = Event.current;
            if (e == null || e.type != EventType.KeyDown) return;
            // The same Event instance is shared by every row callback within one GUI pass —
            // dispatch only for the first row we see it on.
            if (ReferenceEquals(e, lastHierarchyEvent)) return;
            lastHierarchyEvent = e;
            Dispatch(e, KeyScope.Hierarchy);
        }

        /// <summary>Tier-2 entry point (global capture); also used by tests.</summary>
        internal static void Dispatch(Event e, KeyScope origin) {
            if (e == null || e.type != EventType.KeyDown || e.keyCode == KeyCode.None) return;
            var settings = EditorEnhancerXSettings.instance;
            if (!settings.masterEnabled) return;

            for (int i = 0; i < entries.Count; i++) {
                var entry = entries[i];
                if ((entry.scope & origin) == 0) continue;
                ShortcutBinding binding;
                try { binding = entry.binding(); }
                catch { continue; }
                if (!binding.Matches(e)) continue;

                bool handled;
                try { handled = entry.execute(); }
                catch (Exception ex) {
                    UnityEngine.Debug.LogError($"[EditorEnhancerX] Shortcut '{entry.id}' failed: {ex}");
                    return;
                }
                if (handled) {
                    e.Use();
                    return;
                }
            }
        }

        /// <summary>All registered shortcut ids with their current bindings — used by the settings provider to warn on duplicates.</summary>
        public static IEnumerable<(string id, ShortcutBinding binding, KeyScope scope)> Registered() {
            foreach (var entry in entries) {
                ShortcutBinding binding;
                try { binding = entry.binding(); }
                catch { continue; }
                yield return (entry.id, binding, entry.scope);
            }
        }
    }
}
