#if UNITY_EDITOR
using System.Collections.Generic;
using System.Linq;
using AetherNexus.FoundationPlatform.Utilities.Menus;
using UnityEditor;
using UnityEngine;

namespace AetherNexus.FoundationPlatform.StaleComponentGuard.Editor
{
    /// <summary>
    /// On-demand project-wide sweep for stale components, grouped by asset, with per-item Ping/Strip and a
    /// "Strip All". The live surfaces (hierarchy row, inspector) cover the open scene; this covers everything
    /// on disk you haven't opened.
    /// </summary>
    public sealed class StaleComponentWindow : EditorWindow
    {
        private List<StaleFinding> _findings;
        private Vector2 _scroll;

        [MenuItem(MenuPaths.Linting.StaleComponentScanner, false, MenuPriorities.Linting + 5)]
        public static void Open()
        {
            var w = GetWindow<StaleComponentWindow>("Stale Components");
            w.minSize = new Vector2(460f, 300f);
            w.Show();
        }

        private void OnGUI()
        {
            using (new EditorGUILayout.HorizontalScope(EditorStyles.toolbar))
            {
                if (GUILayout.Button("Scan Project", EditorStyles.toolbarButton, GUILayout.Width(100f)))
                    _findings = StaleComponentScanner.ScanProject();
                GUILayout.FlexibleSpace();
                using (new EditorGUI.DisabledScope(_findings == null || _findings.Count == 0))
                {
                    if (GUILayout.Button("Strip All…", EditorStyles.toolbarButton, GUILayout.Width(80f)))
                    {
                        StaleComponentStripper.StripAllWithConfirm(_findings);
                        _findings = StaleComponentScanner.ScanProject();
                        GUIUtility.ExitGUI();
                    }
                }
            }

            if (_findings == null)
            {
                EditorGUILayout.HelpBox("Press \"Scan Project\" to find components whose serialized data references " +
                                        "fields their script no longer defines.", MessageType.Info);
                return;
            }

            if (_findings.Count == 0)
            {
                EditorGUILayout.HelpBox("No stale components found. 🎉", MessageType.Info);
                return;
            }

            EditorGUILayout.LabelField($"{_findings.Count} stale component(s) across " +
                                       $"{_findings.Select(f => f.AssetPath).Distinct().Count()} asset(s):",
                                       EditorStyles.boldLabel);

            _scroll = EditorGUILayout.BeginScrollView(_scroll);
            foreach (var group in _findings.GroupBy(f => f.AssetPath).OrderBy(g => g.Key))
            {
                EditorGUILayout.Space(2f);
                using (new EditorGUILayout.HorizontalScope())
                {
                    EditorGUILayout.LabelField(group.Key, EditorStyles.miniBoldLabel);
                    if (GUILayout.Button("Ping", EditorStyles.miniButton, GUILayout.Width(44f)))
                    {
                        var asset = AssetDatabase.LoadMainAssetAtPath(group.Key);
                        if (asset != null) EditorGUIUtility.PingObject(asset);
                    }
                }
                foreach (var f in group)
                {
                    using (new EditorGUILayout.HorizontalScope())
                    {
                        GUILayout.Space(12f);
                        EditorGUILayout.LabelField(new GUIContent($"{f.TypeName} — {f.OrphanList}",
                            $"Orphan fields: {f.OrphanList}"));
                        if (GUILayout.Button("Strip…", EditorStyles.miniButton, GUILayout.Width(56f)))
                        {
                            StaleComponentStripper.StripWithConfirm(f);
                            _findings = StaleComponentScanner.ScanProject();
                            GUIUtility.ExitGUI();
                        }
                    }
                }
            }
            EditorGUILayout.EndScrollView();
        }
    }
}
#endif
