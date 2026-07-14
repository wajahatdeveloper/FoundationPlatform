#if UNITY_EDITOR
using System;
using System.Collections;
using System.Collections.Generic;
using AetherNexus.FoundationPlatform.FrameworkInspector;
using UnityEditor;
using UnityEngine;

namespace AetherNexus.FoundationPlatform.FrameworkInspector.Editor
{
    /// <summary>
    /// Read-only key/value grid for <c>IDictionary</c> and <c>Dictionary&lt;,&gt;</c> members
    /// (typically <c>[ShowInInspector]</c> runtime views). Honors <see cref="DictionaryDrawerSettingsAttribute"/>.
    /// </summary>
    internal static class EngineDictionaryDrawer
    {
        private static readonly Dictionary<string, bool> s_foldouts = new Dictionary<string, bool>();

        public static bool IsDictionaryType(Type t)
        {
            if (t == null) return false;
            if (typeof(IDictionary).IsAssignableFrom(t)) return true;
            if (t.IsGenericType && t.GetGenericTypeDefinition() == typeof(Dictionary<,>)) return true;
            return false;
        }

        public static void Draw(object dictionary, DictionaryDrawerSettingsAttribute settings, GUIContent label,
            bool memberReadOnly, string foldoutKey = null)
        {
            if (dictionary == null)
            {
                if (label != null && label != GUIContent.none)
                    EditorGUILayout.LabelField(label, new GUIContent("(null)"));
                else
                    EditorGUILayout.LabelField("(null)", EditorStyles.miniLabel);
                return;
            }

            if (dictionary is not IDictionary dict)
            {
                if (label != null && label != GUIContent.none)
                    EditorGUILayout.LabelField(label, new GUIContent(dictionary.ToString()));
                return;
            }

            settings ??= new DictionaryDrawerSettingsAttribute();
            bool readOnly = memberReadOnly || settings.IsReadOnly;
            string keyLabel = string.IsNullOrEmpty(settings.KeyLabel) ? "Key" : settings.KeyLabel;
            string valueLabel = string.IsNullOrEmpty(settings.ValueLabel) ? "Value" : settings.ValueLabel;
            float keyW = settings.KeyColumnWidth > 0 ? settings.KeyColumnWidth : 0f;
            float valueW = settings.ValueColumnWidth > 0 ? settings.ValueColumnWidth : 0f;

            int count = dict.Count;
            string header = label != null && label != GUIContent.none
                ? $"{label.text} ({count})"
                : $"Dictionary ({count})";

            string fKey = foldoutKey ?? header;
            if (!s_foldouts.TryGetValue(fKey, out bool expanded)) expanded = true;
            expanded = FrameworkInspectorTheme.FoldoutInSection(expanded, header);
            s_foldouts[fKey] = expanded;
            if (!expanded) return;

            if (count == 0)
            {
                EditorGUI.indentLevel++;
                EditorGUILayout.LabelField("(empty)", EditorStyles.miniLabel);
                EditorGUI.indentLevel--;
                return;
            }

            EditorGUI.indentLevel++;
            DrawHeaderRow(keyLabel, valueLabel, keyW, valueW);

            int row = 0;
            foreach (DictionaryEntry entry in dict)
            {
                float rowH = FrameworkInspectorTheme.RowHeight;
                var rect = EditorGUILayout.GetControlRect(false, rowH);
                EditorGUI.DrawRect(rect, (row & 1) == 0
                    ? FrameworkInspectorTheme.TableRowBackgroundA
                    : FrameworkInspectorTheme.TableRowBackgroundB);
                DrawEntryRow(rect, entry.Key, entry.Value, keyW, valueW, readOnly);
                row++;
            }
            EditorGUI.indentLevel--;
        }

        private static void DrawHeaderRow(string keyLabel, string valueLabel, float keyW, float valueW)
        {
            float rowH = FrameworkInspectorTheme.RowHeight + 2f;
            var rect = EditorGUILayout.GetControlRect(false, rowH);
            EditorGUI.DrawRect(rect, FrameworkInspectorTheme.TableHeaderBackground);
            var cells = SplitColumns(rect, keyW, valueW);
            GUI.Label(cells[0], keyLabel, FrameworkInspectorTheme.TableHeader);
            GUI.Label(cells[1], valueLabel, FrameworkInspectorTheme.TableHeader);
        }

        private static void DrawEntryRow(Rect row, object key, object value, float keyW, float valueW, bool readOnly)
        {
            var cells = SplitColumns(row, keyW, valueW);
            string keyText = FormatCell(key);
            string valueText = FormatCell(value);

            using (new EditorGUI.DisabledScope(readOnly))
            {
                GUI.Label(cells[0], keyText, FrameworkInspectorTheme.TableCell);
                if (value is UnityEngine.Object uo && !readOnly)
                {
                    var content = new GUIContent(valueText);
                    EditorGUI.ObjectField(cells[1], content, uo, uo.GetType(), true);
                }
                else
                    GUI.Label(cells[1], valueText, FrameworkInspectorTheme.TableCell);
            }
        }

        private static string FormatCell(object o)
        {
            if (o == null) return "null";
            if (o is UnityEngine.Object uo) return uo.name;
            return o.ToString();
        }

        private static Rect[] SplitColumns(Rect row, float keyW, float valueW)
        {
            float pad = FrameworkInspectorTheme.SectionSpacing;
            float total = row.width - pad;
            float k = keyW > 0 ? Mathf.Min(keyW, total * 0.65f) : total * 0.45f;
            float v = valueW > 0 ? Mathf.Min(valueW, total - k) : total - k;
            if (keyW <= 0 && valueW <= 0) { /* defaults above */ }
            else if (keyW > 0 && valueW <= 0) v = total - k;
            else if (keyW <= 0 && valueW > 0) k = total - v;

            return new[]
            {
                new Rect(row.x, row.y, k, row.height),
                new Rect(row.x + k + pad, row.y, Mathf.Max(0, row.width - k - pad), row.height),
            };
        }
    }
}
#endif
