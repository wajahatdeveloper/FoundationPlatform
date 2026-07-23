using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace ProjectWindowX {
    /// <summary>
    /// Main dispatcher. Hooks the Project window rows and layers registered
    /// <see cref="IProjectWindowXPass"/>es (zebra tint, folder icons, extension labels,
    /// hover create-actions, …) on top of each row.
    /// Row metadata is cached per GUID and invalidated when the project changes.
    /// </summary>
    [InitializeOnLoad]
    public static class ProjectWindowX {

        /// <summary>Cached per-row metadata available to passes and context-menu contributors.</summary>
        public sealed class RowContext {
            public string guid;
            public string path;
            public string extension;   // lowercase, with dot; "" for folders
            public bool isFolder;
        }

        private static readonly Dictionary<string, RowContext> rows = new Dictionary<string, RowContext>();

        static ProjectWindowX() {
            EditorApplication.projectWindowItemOnGUI += OnItemGUI;
            EditorApplication.projectChanged += rows.Clear;
            EditorApplication.update += RepaintWhileHovered;
        }

        // The Project window does not repaint on mouse-move, so hover-driven UI
        // (the "+" button) reads a stale Event.current.mousePosition and flickers /
        // sticks to the last-repainted row. Force a repaint while the pointer is
        // over the Project browser so hover state tracks the cursor live.
        private static void RepaintWhileHovered() {
            if (!ProjectWindowXSettings.instance.contextActions)
                return;

            var w = EditorWindow.mouseOverWindow;
            if (w != null && w.GetType().Name == "ProjectBrowser")
                w.Repaint();
        }

        private static RowContext Get(string guid) {
            if (rows.TryGetValue(guid, out var ctx))
                return ctx;

            var path = AssetDatabase.GUIDToAssetPath(guid);
            ctx = new RowContext {
                guid = guid,
                path = path,
                isFolder = !string.IsNullOrEmpty(path) && AssetDatabase.IsValidFolder(path),
            };
            ctx.extension = ctx.isFolder || string.IsNullOrEmpty(path)
                ? string.Empty
                : System.IO.Path.GetExtension(path).ToLowerInvariant();
            rows[guid] = ctx;
            return ctx;
        }

        private static void OnItemGUI(string guid, Rect rect) {
            var s = ProjectWindowXSettings.instance;
            if (!s.enabled)
                return;

            var ctx = Get(guid);
            if (string.IsNullOrEmpty(ctx.path))
                return;

            // Grid tiles are taller than list rows; most passes are list-only.
            var listMode = rect.height <= 20f;
            var rightInset = 0f;

            try {
                var passes = ProjectWindowXPassRegistry.Passes;
                for (var i = 0; i < passes.Count; i++) {
                    var pass = passes[i];
                    if (!pass.Enabled(s))
                        continue;
                    pass.Draw(ctx, rect, listMode, ref rightInset);
                }
            } catch (Exception e) {
                Debug.LogError("ProjectWindowX draw error\n" + e);
            }
        }
    }
}
