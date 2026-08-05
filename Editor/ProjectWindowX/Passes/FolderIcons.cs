using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace ProjectWindowX {
    /// <summary>
    /// Draws custom icons over Project window folders based on settings rules
    /// (exact path or path-with-children). Works in list and grid modes.
    /// </summary>
    public static class FolderIcons {

        private static readonly Dictionary<string, Texture> builtinCache = new Dictionary<string, Texture>();
        private static readonly Dictionary<string, string> builtinNameMap = new Dictionary<string, string> {
            { "Scene Icon", "SceneAsset Icon" },
            { "d_Scene Icon", "d_SceneAsset Icon" },
            { "Animation Icon", "AnimationClip Icon" },
            { "d_Animation Icon", "d_AnimationClip Icon" },
            { "Audio Icon", "AudioClip Icon" },
            { "d_Audio Icon", "d_AudioClip Icon" },
            { "AudioMixer Icon", "Audio Mixer" },
            { "d_AudioMixer Icon", "d_Audio Mixer" },
        };

        /// <summary>Bumped when folder-icon rules change so HierarchyX can invalidate its row cache.</summary>
        public static int RulesVersion { get; private set; }

        public static void NotifyRulesChanged() {
            RulesVersion++;
        }

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

        public static Texture Resolve(string path, ProjectWindowXSettings s) {
            if (TryResolve(path, s, out var icon, out _))
                return icon;
            return null;
        }

        public static bool TryResolve(string path, ProjectWindowXSettings s, out Texture icon, out string matchedFolderPath) {
            return TryResolve(path, s, false, out icon, out matchedFolderPath);
        }

        /// <summary>
        /// Hierarchy-side resolve: same path matching as <see cref="TryResolve"/> but only rules with
        /// <c>applyToHierarchy</c> set. Project-window callers keep using <see cref="TryResolve"/>.
        /// </summary>
        public static bool TryResolveForHierarchy(string path, ProjectWindowXSettings s, out Texture icon, out string matchedFolderPath) {
            return TryResolve(path, s, true, out icon, out matchedFolderPath);
        }

        private static bool TryResolve(string path, ProjectWindowXSettings s, bool forHierarchy, out Texture icon, out string matchedFolderPath) {
            var rules = s.folderIconRules;
            if (rules == null) {
                icon = null;
                matchedFolderPath = null;
                return false;
            }

            for (var i = 0; i < rules.Count; i++) {
                var rule = rules[i];
                if (rule == null || string.IsNullOrEmpty(rule.folderPath))
                    continue;
                if (forHierarchy && !rule.applyToHierarchy)
                    continue;

                var rulePath = rule.folderPath.TrimEnd('/');
                var match = path == rulePath
                    || (rule.applyToChildren && path.StartsWith(rulePath + "/", System.StringComparison.Ordinal));
                if (!match)
                    continue;

                if (!string.IsNullOrEmpty(rule.builtinIconName)) {
                    icon = Builtin(rule.builtinIconName);
                    matchedFolderPath = rule.folderPath;
                    return true;
                }
                if (rule.customIcon != null) {
                    icon = rule.customIcon;
                    matchedFolderPath = rule.folderPath;
                    return true;
                }
            }

            icon = null;
            matchedFolderPath = null;
            return false;
        }

        private static Texture Builtin(string name) {
            if (builtinNameMap.TryGetValue(name, out var mapped))
                name = mapped;
            if (builtinCache.TryGetValue(name, out var tex))
                return tex;
            var content = EditorGUIUtility.IconContent(name);
            tex = content != null ? content.image : null;
            builtinCache[name] = tex;
            return tex;
        }
    }
}
