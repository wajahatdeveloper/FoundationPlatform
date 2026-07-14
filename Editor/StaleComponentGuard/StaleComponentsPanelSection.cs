#if UNITY_EDITOR
using System.Collections.Generic;
using HierarchyX;
using UnityEditor;
using UnityEngine;

namespace AetherNexus.FoundationPlatform.StaleComponentGuard.Editor
{
    /// <summary>
    /// "Stale Components" section in the Hierarchy setup panel: a header chip with the open-scene stale count,
    /// a row per stale component (Select + Strip), and a project-sweep launcher. Auto-discovered by
    /// <c>HierarchyXPanelRegistry</c>.
    /// </summary>
    public sealed class StaleComponentsPanelSection : IHierarchyPanelSection
    {
        public string Id => "FoundationPlatform.StaleComponents";
        public string Title => "Stale Components";
        public int Order => 60;

        public IEnumerable<PanelChip> GetHeaderChips()
        {
            if (!StaleComponentGuardSettings.Enabled)
            {
                yield return new PanelChip("Off", PanelChipStatus.Neutral, "Stale Component Guard is disabled.");
                yield break;
            }
            int count = StaleComponentCache.GetSceneFindings().Count;
            yield return count == 0
                ? new PanelChip("Clean", PanelChipStatus.Ok, "No stale components in the open scene(s).")
                : new PanelChip($"{count} stale", PanelChipStatus.Error, "Components with serialized data their script no longer defines.");
        }

        public IEnumerable<PanelAction> GetToolbarActions()
        {
            yield return new PanelAction("⟳", "Scan the whole project for stale components",
                StaleComponentWindow.Open, "Search Icon");
        }

        public void OnBodyGUI()
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
