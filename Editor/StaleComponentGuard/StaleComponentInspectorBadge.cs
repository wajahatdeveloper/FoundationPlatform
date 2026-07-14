#if UNITY_EDITOR
using System.Collections.Generic;
using FoundationPlatform.FrameworkInspector;
using FoundationPlatform.FrameworkInspector.Editor;
using UnityEditor;
using UnityEngine;

namespace FoundationPlatform.StaleComponentGuard.Editor
{
    /// <summary>
    /// Draws a stale-data warning in the inspector — listing the orphan field names, with Strip actions.
    /// Uses <see cref="Editor.finishedDefaultHeaderGUI"/>, which fires for the selected object's MAIN header
    /// (the GameObject, or a directly-inspected asset) — NOT for each component's inline title bar. So for a
    /// GameObject we aggregate across its components; for an inspected asset we test the target directly.
    /// </summary>
    [InitializeOnLoad]
    public static class StaleComponentInspectorBadge
    {
        static StaleComponentInspectorBadge()
        {
            UnityEditor.Editor.finishedDefaultHeaderGUI -= OnHeaderGUI;
            UnityEditor.Editor.finishedDefaultHeaderGUI += OnHeaderGUI;
        }

        private static void OnHeaderGUI(UnityEditor.Editor editor)
        {
            if (editor == null || editor.target == null)
                return;

            // GameObject selected: the hook gives us the GO, so check each of its components.
            if (editor.target is GameObject go)
            {
                var findings = new List<StaleFinding>();
                foreach (var mb in go.GetComponents<MonoBehaviour>())
                {
                    if (mb == null)
                        continue; // missing script — different problem
                    if (StaleComponentCache.TryGet(mb, out var f))
                        findings.Add(f);
                }
                if (findings.Count > 0)
                    DrawWarning(findings);
                return;
            }

            // Directly-inspected asset (e.g. a ScriptableObject .asset). Components are already covered by the
            // GameObject-header pass above, so skip them here — avoids a double warning if the hook also fires
            // per component in some Unity versions.
            if (!(editor.target is Component) && StaleComponentCache.TryGet(editor.target, out var finding))
                DrawWarning(new List<StaleFinding> { finding });
        }

        private static void DrawWarning(List<StaleFinding> findings)
        {
            string body = findings.Count == 1
                ? $"Stale component — {ShortType(findings[0].TypeName)} serializes {findings[0].OrphanFields?.Length ?? 0} " +
                  $"field(s) its script no longer defines: {findings[0].OrphanList}."
                : $"{findings.Count} stale components on this object:\n" +
                  string.Join("\n", findings.ConvertAll(f => $"• {ShortType(f.TypeName)}: {f.OrphanList}"));

            FrameworkInspectorTheme.DrawInfoBox(
                body + "\nRe-author the component, or Strip to discard the orphan data (permanent).",
                InfoMessageType.Warning);

            using (new EditorGUILayout.HorizontalScope())
            {
                GUILayout.FlexibleSpace();
                if (GUILayout.Button(findings.Count > 1 ? "Strip All…" : "Strip…", EditorStyles.miniButton, GUILayout.Width(80f)))
                {
                    StaleComponentStripper.StripAllWithConfirm(findings);
                    GUIUtility.ExitGUI();
                }
            }
            EditorGUILayout.Space(2f);
        }

        private static string ShortType(string typeName)
        {
            if (string.IsNullOrEmpty(typeName))
                return "(unknown)";
            int dot = typeName.LastIndexOf('.');
            return dot >= 0 && dot < typeName.Length - 1 ? typeName.Substring(dot + 1) : typeName;
        }
    }
}
#endif
