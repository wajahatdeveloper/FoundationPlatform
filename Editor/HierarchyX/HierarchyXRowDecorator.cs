using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace HierarchyX {

    /// <summary>
    /// What a decorator wants painted on a row. All fields are additive: a decorator sets
    /// only what it cares about, later decorators (higher <see cref="IHierarchyRowDecorator.Order"/>)
    /// override earlier ones. Alpha &lt;= 0 means "leave alone".
    /// </summary>
    public struct HierarchyRowDecoration {
        /// <summary>Full-width tint painted under the row. Alpha &lt;= 0 = no tint.</summary>
        public Color rowTint;
        public TintMode tintMode;

        /// <summary>Vertical spine painted at the far left edge. Alpha &lt;= 0 = no spine.</summary>
        public Color accent;
        /// <summary>Filled (thicker) spine for a "root"/anchor row vs a thin spine for a member.</summary>
        public bool accentFilled;

        /// <summary>Tooltip surfaced over the accent spine on hover.</summary>
        public string tooltip;

        /// <summary>Short right-aligned chip text (e.g. a category tag). Null/empty = no chip.</summary>
        public string badgeText;
        /// <summary>Chip text + fill color. Alpha &lt;= 0 falls back to a neutral chip color.</summary>
        public Color badgeColor;

        public bool HasTint { get { return rowTint.a > 0.001f; } }
        public bool HasAccent { get { return accent.a > 0.001f; } }
        public bool HasBadge { get { return !string.IsNullOrEmpty(badgeText); } }
    }

    /// <summary>
    /// Implement to add per-row visuals (tint / left-edge spine) to the hierarchy without
    /// coupling HierarchyX to game code. Concrete implementations with a public parameterless
    /// constructor are auto-discovered via <see cref="UnityEditor.TypeCache"/>; you can also
    /// <see cref="HierarchyXRegistry.Register"/> instances manually.
    /// </summary>
    public interface IHierarchyRowDecorator {
        /// <summary>Draw priority. Lower runs first; higher wins on conflicting fields.</summary>
        int Order { get; }

        /// <summary>
        /// Fill <paramref name="decoration"/> for <paramref name="go"/>. Return true if anything
        /// was contributed. Called at most once per row per hierarchy change (result is cached),
        /// so it may safely do parent walks / GetComponentInParent.
        /// </summary>
        bool TryDecorate(GameObject go, ref HierarchyRowDecoration decoration);
    }

    /// <summary>
    /// Registry of <see cref="IHierarchyRowDecorator"/>s plus a per-row decoration cache.
    /// The cache is invalidated on any hierarchy change, so decorators only re-run when the
    /// structure actually changes — the hot draw path just reads the dictionary.
    /// </summary>
    [InitializeOnLoad]
    public static class HierarchyXRegistry {

        private static readonly List<IHierarchyRowDecorator> decorators = new List<IHierarchyRowDecorator>();
        private static readonly Dictionary<int, HierarchyRowDecoration> cache = new Dictionary<int, HierarchyRowDecoration>();
        private static bool discovered;

        static HierarchyXRegistry() {
            EditorApplication.hierarchyChanged -= ClearCache;
            EditorApplication.hierarchyChanged += ClearCache;
        }

        /// <summary>Add a decorator instance (idempotent). Sorted by <see cref="IHierarchyRowDecorator.Order"/>.</summary>
        public static void Register(IHierarchyRowDecorator decorator) {
            if (decorator == null || decorators.Contains(decorator))
                return;
            decorators.Add(decorator);
            decorators.Sort((a, b) => a.Order.CompareTo(b.Order));
            ClearCache();
            EditorApplication.RepaintHierarchyWindow();
        }

        public static void Unregister(IHierarchyRowDecorator decorator) {
            if (decorator != null && decorators.Remove(decorator)) {
                ClearCache();
                EditorApplication.RepaintHierarchyWindow();
            }
        }

        /// <summary>True if at least one decorator is present (cheap early-out for the draw path).</summary>
        public static bool HasAny {
            get {
                EnsureDiscovered();
                return decorators.Count > 0;
            }
        }

        /// <summary>Cached decoration for a row, computing (and caching) it on first request.</summary>
        internal static bool TryGet(GameObject go, out HierarchyRowDecoration decoration) {
            EnsureDiscovered();

            var id = go.GetInstanceID();
            if (cache.TryGetValue(id, out decoration))
                return decoration.HasTint || decoration.HasAccent;

            decoration = default;
            var any = false;
            for (var i = 0; i < decorators.Count; i++) {
                try {
                    any |= decorators[i].TryDecorate(go, ref decoration);
                } catch (Exception e) {
                    Debug.LogError("HierarchyX decorator error: " + decorators[i].GetType().Name + "\n" + e);
                }
            }

            cache[id] = decoration;
            return any && (decoration.HasTint || decoration.HasAccent);
        }

        private static void EnsureDiscovered() {
            if (discovered)
                return;
            discovered = true;

            foreach (var type in TypeCache.GetTypesDerivedFrom<IHierarchyRowDecorator>()) {
                if (type.IsAbstract || type.IsInterface)
                    continue;
                if (type.GetConstructor(Type.EmptyTypes) == null)
                    continue;
                // A manually-registered instance of the same type already covers it.
                if (decorators.Exists(d => d.GetType() == type))
                    continue;
                try {
                    decorators.Add((IHierarchyRowDecorator)Activator.CreateInstance(type));
                } catch (Exception e) {
                    Debug.LogWarning("HierarchyX: could not instantiate decorator " + type.Name + "\n" + e);
                }
            }
            decorators.Sort((a, b) => a.Order.CompareTo(b.Order));
        }

        private static void ClearCache() {
            cache.Clear();
            EditorApplication.RepaintHierarchyWindow();
        }
    }
}
