using System;
using System.Text.RegularExpressions;
using UnityEditor;
using UnityEngine;
using Object = UnityEngine.Object;

namespace AetherNexus.FoundationPlatform.EditorEnhancerX {
    /// <summary>
    /// Rename / mass-rename dialog. Single target: plain rename. Multiple targets:
    /// token expansion — {N} original name, {C} counter (optionally {C:start}) —
    /// or regex find/replace. Scene objects rename with Undo; assets via AssetDatabase.
    /// Also doubles as a generic name prompt (e.g. for Group).
    /// </summary>
    internal sealed class RenameWindow : EditorWindow {

        private Object[] targets;
        private Action<string> onCommit;   // prompt mode: commit callback instead of renaming targets
        private string input = string.Empty;
        private bool useRegex;
        private string regexPattern = string.Empty;
        private string regexReplacement = string.Empty;
        private bool focused;

        internal static void OpenForTargets(Object[] targets) {
            if (targets == null || targets.Length == 0)
                return;
            var window = CreateInstance<RenameWindow>();
            window.titleContent = new GUIContent(targets.Length > 1 ? $"Rename {targets.Length} Objects" : "Rename");
            window.targets = targets;
            window.input = targets[0].name;
            window.ShowAuxWindow();
        }

        internal static void OpenPrompt(string title, string initial, Action<string> onCommit) {
            var window = CreateInstance<RenameWindow>();
            window.titleContent = new GUIContent(title);
            window.input = initial;
            window.onCommit = onCommit;
            window.ShowAuxWindow();
        }

        private void OnGUI() {
            var isPrompt = onCommit != null;
            var isMass = !isPrompt && targets.Length > 1;

            var e = Event.current;
            if (e.type == EventType.KeyDown) {
                if (e.keyCode == KeyCode.Return || e.keyCode == KeyCode.KeypadEnter) { Apply(); e.Use(); return; }
                if (e.keyCode == KeyCode.Escape) { Close(); e.Use(); return; }
            }

            if (isMass) {
                useRegex = GUILayout.Toolbar(useRegex ? 1 : 0, new[] { "Tokens", "Regex" }) == 1;
                EditorGUILayout.Space(4f);
            }

            if (useRegex && isMass) {
                regexPattern = EditorGUILayout.TextField("Pattern", regexPattern);
                regexReplacement = EditorGUILayout.TextField("Replacement", regexReplacement);
            } else {
                GUI.SetNextControlName("RenameInput");
                input = EditorGUILayout.TextField(isMass ? "Name ({N} {C} {C:5})" : "Name", input);
                if (!focused) {
                    EditorGUI.FocusTextInControl("RenameInput");
                    focused = true;
                }
            }

            if (isMass) {
                EditorGUILayout.Space(4f);
                EditorGUILayout.LabelField("Preview", EditorStyles.miniBoldLabel);
                var count = Mathf.Min(targets.Length, 8);
                for (var i = 0; i < count; i++)
                    EditorGUILayout.LabelField($"{targets[i].name}  →  {Evaluate(targets[i].name, i)}", EditorStyles.miniLabel);
                if (targets.Length > count)
                    EditorGUILayout.LabelField($"… and {targets.Length - count} more", EditorStyles.miniLabel);
            }

            EditorGUILayout.Space(6f);
            using (new EditorGUILayout.HorizontalScope()) {
                GUILayout.FlexibleSpace();
                if (GUILayout.Button("Cancel", GUILayout.Width(80f)))
                    Close();
                if (GUILayout.Button("Apply", GUILayout.Width(80f)))
                    Apply();
            }
        }

        private string Evaluate(string originalName, int index) {
            try {
                if (useRegex && targets != null && targets.Length > 1)
                    return Regex.Replace(originalName, regexPattern ?? string.Empty, regexReplacement ?? string.Empty);

                var result = input ?? string.Empty;
                result = result.Replace("{N}", originalName);
                result = Regex.Replace(result, @"\{C(?::(\d+))?\}", m => {
                    var start = m.Groups[1].Success ? int.Parse(m.Groups[1].Value) : 1;
                    return (start + index).ToString();
                });
                return result;
            } catch {
                return originalName;
            }
        }

        private void Apply() {
            if (onCommit != null) {
                if (!string.IsNullOrEmpty(input))
                    onCommit(input);
                Close();
                return;
            }

            for (var i = 0; i < targets.Length; i++) {
                var obj = targets[i];
                if (obj == null)
                    continue;
                var newName = Evaluate(obj.name, i);
                if (string.IsNullOrEmpty(newName) || newName == obj.name)
                    continue;

                if (EditorUtility.IsPersistent(obj)) {
                    var path = AssetDatabase.GetAssetPath(obj);
                    if (!string.IsNullOrEmpty(path))
                        AssetDatabase.RenameAsset(path, newName);
                } else {
                    Undo.RecordObject(obj, "Rename");
                    obj.name = newName;
                }
            }
            Close();
        }
    }

    /// <summary>Shortcut entry for rename (SceneView scope; the Hierarchy keeps Unity's native F2).</summary>
    [InitializeOnLoad]
    internal static class RenameShortcut {
        static RenameShortcut() {
            KeyRouter.Register("rename",
                () => EditorEnhancerXSettings.instance.renameKey,
                KeyScope.SceneView,
                () => {
                    var selection = Selection.objects;
                    if (selection == null || selection.Length == 0)
                        return false;
                    RenameWindow.OpenForTargets(selection);
                    return true;
                });
        }
    }
}
