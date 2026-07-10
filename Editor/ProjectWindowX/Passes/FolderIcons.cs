using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace ProjectWindowX {
    /// <summary>
    /// Draws custom icons over Project window folders based on settings rules
    /// (exact path or path-with-children). Works in list and grid modes.
    /// </summary>
    internal static class FolderIcons {

        private static readonly Dictionary<string, Texture> builtinCache = new Dictionary<string, Texture>();

        internal static void Draw(ProjectWindowX.RowContext ctx, Rect rect, bool listMode, ProjectWindowXSettings s) {
            if (Event.current.type != EventType.Repaint)
                return;

            var icon = Resolve(ctx.path, s);
            if (icon == null)
                return;

            Rect iconRect;
            if (listMode) {
                iconRect = new Rect(rect.x, rect.y, 16f, 16f);
            } else {
                // Grid tile: icon area above the label line.
                var labelHeight = EditorGUIUtility.singleLineHeight;
                iconRect = new Rect(rect.x, rect.y, rect.width, Mathf.Max(16f, rect.height - labelHeight));
            }
            GUI.DrawTexture(iconRect, icon, ScaleMode.ScaleToFit);
        }

        private static Texture Resolve(string path, ProjectWindowXSettings s) {
            var rules = s.folderIconRules;
            if (rules == null)
                return null;

            for (var i = 0; i < rules.Count; i++) {
                var rule = rules[i];
                if (rule == null || string.IsNullOrEmpty(rule.folderPath))
                    continue;

                var rulePath = rule.folderPath.TrimEnd('/');
                var match = path == rulePath
                    || (rule.applyToChildren && path.StartsWith(rulePath + "/", System.StringComparison.Ordinal));
                if (!match)
                    continue;

                if (!string.IsNullOrEmpty(rule.builtinIconName))
                    return Builtin(rule.builtinIconName);
                if (rule.customIcon != null)
                    return rule.customIcon;
            }
            return null;
        }

        private static Texture Builtin(string name) {
            if (builtinCache.TryGetValue(name, out var tex))
                return tex;
            var content = EditorGUIUtility.IconContent(name);
            tex = content != null ? content.image : null;
            builtinCache[name] = tex;
            return tex;
        }
    }
}
