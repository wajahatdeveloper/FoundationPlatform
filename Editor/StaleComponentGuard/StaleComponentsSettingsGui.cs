#if UNITY_EDITOR
using HierarchyX;
using UnityEditor;
using UnityEngine;

namespace AetherNexus.FoundationPlatform.StaleComponentGuard.Editor
{
    /// <summary>
    /// "Stale Components" block under Project Settings ▸ HierarchyX: enable toggle, open-scene
    /// findings (Select + Strip), and project-sweep launcher. Registered via
    /// <see cref="HierarchyXSettingsExtras"/> so HierarchyX does not reference this assembly.
    /// </summary>
    public static class StaleComponentsSettingsGui
    {
        [InitializeOnLoadMethod]
        private static void Register()
        {
            HierarchyXSettingsExtras.Register("Stale Components", Draw);
        }

        private static void Draw()
        {
            bool enabled = EditorGUILayout.ToggleLeft("Enable Stale Component Guard", StaleComponentGuardSettings.Enabled);
            if (enabled != StaleComponentGuardSettings.Enabled)
                StaleComponentGuardSettings.Enabled = enabled;

            if (!enabled)
                return;

            var findings = StaleComponentCache.GetSceneFindings();
            if (findings.Count == 0)
            {
                EditorGUILayout.LabelField("No stale components in the open scene(s).", EditorStyles.miniLabel);
            }
            else
            {
                foreach (var f in findings)
                {
                    using (new EditorGUILayout.HorizontalScope())
                    {
                        EditorGUILayout.LabelField(new GUIContent($"{Short(f.TypeName)} — {f.OrphanList}",
                            $"{f.TypeName}\nOrphan fields: {f.OrphanList}"), GUILayout.MinWidth(60f));
                        if (GUILayout.Button("Select", EditorStyles.miniButtonLeft, GUILayout.Width(56f)))
                            StaleComponentCache.SelectInScene(f);
                        if (GUILayout.Button("Strip…", EditorStyles.miniButtonRight, GUILayout.Width(56f)))
                        {
                            StaleComponentStripper.StripWithConfirm(f);
                            GUIUtility.ExitGUI();
                        }
                    }
                }
            }

            EditorGUILayout.Space(2f);
            if (GUILayout.Button("Scan Project…"))
                StaleComponentWindow.Open();
        }

        private static string Short(string typeName)
        {
            if (string.IsNullOrEmpty(typeName))
                return "(unknown)";
            int dot = typeName.LastIndexOf('.');
            return dot >= 0 && dot < typeName.Length - 1 ? typeName.Substring(dot + 1) : typeName;
        }
    }
}
#endif
