#if UNITY_EDITOR
using System;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using UnityEditor;
using UnityEngine;

namespace AetherNexus.FoundationPlatform.AetherInspector.Editor
{
    /// <summary>
    /// Native IMGUI grid renderer backing <c>[TableList]</c>. Renders one
    /// row per element and one column per visible member of the element type, honoring
    /// <see cref="TableColumnWidthAttribute"/>, <see cref="GUIColorAttribute"/>, <see cref="ButtonAttribute"/>,
    /// <see cref="ReadOnlyAttribute"/>, <see cref="ShowInInspectorAttribute"/> and hiding
    /// <c>[HideInInspector]</c> members. Two entry points:
    ///  * <see cref="DrawValueTable"/> — reflection over a live <see cref="IList"/> (read-only monitoring
    ///    tables, e.g. the EventBus windows);
    ///  * <see cref="DrawSerializedTable"/> — editable table over an array <see cref="SerializedProperty"/>
    ///    (authoring assets). Wired into <see cref="AetherInspectorRenderer"/> for <c>[TableList]</c> fields.
    /// </summary>
    public static class TableRenderer
    {
        private const BindingFlags MemberFlags =
            BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic;
        private const float CellPad = 2f;

        private sealed class Column
        {
            public string Header;
            public float Width;
            public bool HasWidth;
            public bool ReadOnly;
            public bool IsButton;
            public GUIColorAttribute Color;
            public FieldInfo Field;
            public PropertyInfo Property;
            public MethodInfo Method;      // button
            public ButtonAttribute Button;
            public int Lines = 1;
            public float WidthOverride; // user drag-resize (session), 0 = none
        }

        // ---------------------------------------------------------------- column model

        private static readonly Dictionary<Type, List<Column>> s_columnCache = new Dictionary<Type, List<Column>>();

        private static List<Column> GetColumns(Type elementType)
        {
            if (s_columnCache.TryGetValue(elementType, out var cached)) return cached;

            var cols = new List<Column>();
            var members = new List<MemberInfo>();
            // Hierarchy-walking enumerators so private members of base classes are included.
            foreach (var f in AetherInspectorRenderer.AllFields(elementType)) members.Add(f);
            foreach (var p in AetherInspectorRenderer.AllProperties(elementType)) members.Add(p);
            foreach (var m in AetherInspectorRenderer.AllMethods(elementType)) members.Add(m);
            members.Sort((a, b) => a.MetadataToken.CompareTo(b.MetadataToken));

            foreach (var m in members)
            {
                if (m.GetCustomAttribute<HideInInspector>() != null) continue;
                if (m.GetCustomAttribute<HideLabelAttribute>() != null) { /* still shown, label hidden */ }

                var col = new Column
                {
                    ReadOnly = m.GetCustomAttribute<ReadOnlyAttribute>() != null,
                    Color = m.GetCustomAttribute<GUIColorAttribute>(),
                };
                var width = m.GetCustomAttribute<TableColumnWidthAttribute>();
                if (width != null) { col.Width = width.Width; col.HasWidth = true; }
                var multi = m.GetCustomAttribute<MultiLinePropertyAttribute>();
                if (multi != null) col.Lines = Mathf.Max(1, multi.Lines);
                var label = m.GetCustomAttribute<LabelTextAttribute>();

                switch (m)
                {
                    case FieldInfo f:
                        if (f.IsStatic) continue;
                        bool fieldVisible = f.IsPublic || f.GetCustomAttribute<SerializeField>() != null
                            || f.GetCustomAttribute<ShowInInspectorAttribute>() != null;
                        if (!fieldVisible) continue;
                        col.Field = f;
                        col.Header = label?.Text ?? ObjectNames.NicifyVariableName(f.Name);
                        break;
                    case PropertyInfo p:
                        if (p.GetCustomAttribute<ShowInInspectorAttribute>() == null) continue;
                        col.Property = p;
                        col.ReadOnly = col.ReadOnly || !p.CanWrite;
                        col.Header = label?.Text ?? ObjectNames.NicifyVariableName(p.Name);
                        break;
                    case MethodInfo mi:
                        var btn = m.GetCustomAttribute<ButtonAttribute>();
                        if (btn == null) continue;
                        col.Method = mi;
                        col.Button = btn;
                        col.IsButton = true;
                        col.Header = string.Empty;
                        if (!col.HasWidth) { col.Width = 32f; col.HasWidth = true; }
                        break;
                    default:
                        continue;
                }
                cols.Add(col);
            }

            s_columnCache[elementType] = cols;
            return cols;
        }

        // ---------------------------------------------------------------- value (reflection) mode

        /// <summary>Read-only-friendly grid over a live list (reflection). Used by monitoring windows.</summary>
        /// <summary>Draws using the element type's name as the title.</summary>
        public static void DrawValueTable(IList list, Type elementType) => DrawValueTable(list, elementType, null);

        public static void DrawValueTable(IList list, Type elementType, string title)
        {
            var cols = GetColumns(elementType);
            if (!string.IsNullOrEmpty(title))
                EditorGUILayout.LabelField(title, AetherInspectorTheme.SectionTitle);

            float rowH = AetherInspectorTheme.RowHeight * MaxLines(cols);
            DrawHeaderRow(cols, title ?? elementType?.FullName);

            if (list == null || list.Count == 0)
            {
                EditorGUILayout.LabelField("(empty)", EditorStyles.centeredGreyMiniLabel);
                return;
            }

            for (int i = 0; i < list.Count; i++)
            {
                object element = list[i];
                Rect row = EditorGUILayout.GetControlRect(false, rowH);
                EditorGUI.DrawRect(row, (i & 1) == 0 ? AetherInspectorTheme.TableRowBackgroundA : AetherInspectorTheme.TableRowBackgroundB);
                LayoutCells(row, cols, (cell, col) => DrawValueCell(cell, col, element));
            }
        }

        private static void DrawValueCell(Rect cell, Column col, object element)
        {
            if (element == null) return;
            Color prev = GUI.color;
            if (TryColor(col, element, out var c)) GUI.color = c;

            try
            {
                if (col.IsButton)
                {
                    string label = string.IsNullOrEmpty(col.Button.Name) ? "▶" : col.Button.Name;
                    if (GUI.Button(cell, label, AetherInspectorTheme.CompactButton) && col.Method.GetParameters().Length == 0)
                        col.Method.Invoke(element, null);
                }
                else
                {
                    object value = col.Field != null ? col.Field.GetValue(element)
                        : col.Property != null && col.Property.CanRead ? col.Property.GetValue(element) : null;
                    GUI.Label(cell, value != null ? value.ToString() : string.Empty, AetherInspectorTheme.TableCell);
                }
            }
            catch { /* defensive: never let one cell break the table */ }

            GUI.color = prev;
        }

        // ---------------------------------------------------------------- serialized mode

        private static readonly Dictionary<string, Vector2> s_scroll = new Dictionary<string, Vector2>();
        private static readonly Dictionary<string, int> s_page = new Dictionary<string, int>();

        /// <summary>Editable grid over an array/list SerializedProperty. Used for authoring <c>[TableList]</c> fields.</summary>
        public static void DrawSerializedTable(SerializedProperty arrayProp, Type elementType, TableListAttribute settings, GUIContent label)
        {
            if (arrayProp == null || !arrayProp.isArray) return;
            settings ??= new TableListAttribute();
            string key = arrayProp.propertyPath;
            List<Column> cols;
            try { cols = GetColumns(elementType); }
            catch (Exception ex)
            {
                EditorGUILayout.HelpBox($"[TableList] column build failed for {elementType?.Name}: {ex.Message}", MessageType.Error);
                EditorGUILayout.PropertyField(arrayProp, label ?? new GUIContent(arrayProp.displayName), true);
                return;
            }

            // --- Title / toolbar row ---
            string headerText = label != null ? label.text : arrayProp.displayName;
            if (settings.ShowItemCount) headerText += $"  ({arrayProp.arraySize})";
            using (new EditorGUILayout.HorizontalScope())
            {
                if (settings.AlwaysExpanded)
                    EditorGUILayout.LabelField(headerText, AetherInspectorTheme.SectionTitle);
                else
                    arrayProp.isExpanded = EditorGUILayout.Foldout(arrayProp.isExpanded, headerText, true);
                GUILayout.FlexibleSpace();
                if (!settings.HideToolbar && !settings.IsReadOnly &&
                    GUILayout.Button("+", AetherInspectorTheme.CompactButton, GUILayout.Width(22)))
                {
                    arrayProp.InsertArrayElementAtIndex(arrayProp.arraySize);
                    arrayProp.serializedObject.ApplyModifiedProperties();
                }
            }
            if (!settings.AlwaysExpanded && !arrayProp.isExpanded) return;

            // --- Paging ---
            int count = arrayProp.arraySize;
            int start = 0, end = count;
            if (!settings.HideToolbar && settings.ShowPaging)
            {
                int pageSize = settings.NumberOfItemsPerPage > 0 ? settings.NumberOfItemsPerPage : 15;
                if (count > pageSize)
                {
                    int pageCount = (count + pageSize - 1) / pageSize;
                    s_page.TryGetValue(key, out int page);
                    page = Mathf.Clamp(page, 0, pageCount - 1);
                    using (new EditorGUILayout.HorizontalScope())
                    {
                        GUILayout.FlexibleSpace();
                        using (new EditorGUI.DisabledScope(page <= 0))
                            if (GUILayout.Button("◀", EditorStyles.miniButtonLeft, GUILayout.Width(24))) page--;
                        GUILayout.Label($"{page + 1}/{pageCount}", EditorStyles.miniLabel, GUILayout.ExpandWidth(false));
                        using (new EditorGUI.DisabledScope(page >= pageCount - 1))
                            if (GUILayout.Button("▶", EditorStyles.miniButtonRight, GUILayout.Width(24))) page++;
                    }
                    s_page[key] = page;
                    start = page * pageSize;
                    end = Mathf.Min(start + pageSize, count);
                }
            }

            float rowH = AetherInspectorTheme.RowHeight * MaxLines(cols);
            DrawHeaderRow(cols, key, settings.ShowIndexLabels, !settings.IsReadOnly, settings.DefaultMinColumnWidth);

            // --- Scroll view (bounded height) ---
            bool scroll = settings.DrawScrollView && settings.MaxScrollViewHeight > 0
                && (end - start) * rowH > settings.MaxScrollViewHeight;
            if (scroll)
            {
                s_scroll.TryGetValue(key, out var sv);
                float minH = settings.MinScrollViewHeight > 0 ? settings.MinScrollViewHeight : 0f;
                sv = EditorGUILayout.BeginScrollView(sv,
                    GUILayout.MaxHeight(settings.MaxScrollViewHeight), GUILayout.MinHeight(minH));
                s_scroll[key] = sv;
            }

            int removeAt = -1;
            for (int i = start; i < end; i++)
            {
                var element = arrayProp.GetArrayElementAtIndex(i);
                Rect row = EditorGUILayout.GetControlRect(false, rowH);
                EditorGUI.DrawRect(row, (i & 1) == 0 ? AetherInspectorTheme.TableRowBackgroundA : AetherInspectorTheme.TableRowBackgroundB);

                float x = row.x;
                if (settings.ShowIndexLabels)
                {
                    GUI.Label(new Rect(x, row.y, 24, row.height), i.ToString(), AetherInspectorTheme.TableCell);
                    x += 24;
                }

                var widths = ResolveWidths(cols, row.width - (x - row.x) - (settings.IsReadOnly ? 0 : 20f), settings.DefaultMinColumnWidth);
                for (int c = 0; c < cols.Count; c++)
                {
                    var cell = new Rect(x + CellPad, row.y, widths[c] - CellPad * 2, row.height);
                    DrawSerializedCell(cell, cols[c], element);
                    x += widths[c];
                }

                if (!settings.IsReadOnly)
                {
                    if (GUI.Button(new Rect(row.xMax - 18, row.y, 16, AetherInspectorTheme.RowHeight), "×", AetherInspectorTheme.CompactButton))
                        removeAt = i;
                }
            }

            if (scroll) EditorGUILayout.EndScrollView();

            if (removeAt >= 0)
            {
                arrayProp.DeleteArrayElementAtIndex(removeAt);
                arrayProp.serializedObject.ApplyModifiedProperties();
            }
        }

        private static void DrawSerializedCell(Rect cell, Column col, SerializedProperty element)
        {
            Color prev = GUI.color;
            object boxed = null;
            try { boxed = element.boxedValue; } catch { }
            if (boxed != null && TryColor(col, boxed, out var c)) GUI.color = c;

            if (col.IsButton)
            {
                if (boxed != null && GUI.Button(cell, string.IsNullOrEmpty(col.Button.Name) ? "▶" : col.Button.Name, AetherInspectorTheme.CompactButton)
                    && col.Method.GetParameters().Length == 0)
                    col.Method.Invoke(boxed, null);
                GUI.color = prev;
                return;
            }

            string memberName = col.Field != null ? col.Field.Name : col.Property?.Name;
            var sub = memberName != null ? element.FindPropertyRelative(memberName) : null;
            using (new EditorGUI.DisabledScope(col.ReadOnly))
            {
                if (sub != null)
                {
                    EditorGUI.PropertyField(cell, sub, GUIContent.none);
                }
                else if (boxed != null && col.Property != null && col.Property.CanRead)
                {
                    string text;
                    try { text = col.Property.GetValue(boxed)?.ToString() ?? string.Empty; }
                    catch { text = "<n/a>"; }
                    GUI.Label(cell, text, AetherInspectorTheme.TableCell); // getter-only prop
                }
            }
            GUI.color = prev;
        }

        // ---------------------------------------------------------------- shared layout

        private static void DrawHeaderRow(List<Column> cols, string key = null, bool indexColumn = false, bool actionColumn = false, int minColumnWidth = 40)
        {
            Rect header = EditorGUILayout.GetControlRect(false, AetherInspectorTheme.RowHeight);
            EditorGUI.DrawRect(header, AetherInspectorTheme.TableHeaderBackground);
            float x = header.x;
            if (indexColumn)
            {
                GUI.Label(new Rect(x, header.y, 24, header.height), "#", AetherInspectorTheme.TableHeader);
                x += 24;
            }
            float actionReserve = actionColumn ? 20f : 0f;
            var widths = ResolveWidths(cols, header.width - (x - header.x) - actionReserve, minColumnWidth);
            for (int c = 0; c < cols.Count; c++)
            {
                GUI.Label(new Rect(x + CellPad, header.y, widths[c] - CellPad * 2, header.height), cols[c].Header, AetherInspectorTheme.TableHeader);
                EditorGUI.DrawRect(new Rect(x, header.y, 1, header.height), AetherInspectorTheme.TableGridLine);
                HandleColumnResize(new Rect(x + widths[c] - 3f, header.y, 6f, header.height), cols[c], widths[c], key, c);
                x += widths[c];
            }
        }

        private static Column s_dragCol;
        private static float s_dragStartX;
        private static float s_dragStartW;

        private static void HandleColumnResize(Rect handle, Column col, float currentWidth, string key, int columnIndex)
        {
            EditorGUIUtility.AddCursorRect(handle, MouseCursor.ResizeHorizontal);
            // Hashed on the table's property path + column index (not call order) so column count/order
            // changes elsewhere can't shift which control receives the resize drag.
            int hint = (key ?? string.Empty).GetHashCode() ^ (columnIndex * 397);
            int id = GUIUtility.GetControlID(hint, FocusType.Passive);
            var e = Event.current;
            switch (e.GetTypeForControl(id))
            {
                case EventType.MouseDown:
                    if (handle.Contains(e.mousePosition))
                    {
                        GUIUtility.hotControl = id;
                        s_dragCol = col;
                        s_dragStartX = e.mousePosition.x;
                        s_dragStartW = currentWidth;
                        e.Use();
                    }
                    break;
                case EventType.MouseDrag:
                    if (GUIUtility.hotControl == id && s_dragCol == col)
                    {
                        col.WidthOverride = Mathf.Max(24f, s_dragStartW + (e.mousePosition.x - s_dragStartX));
                        e.Use();
                    }
                    break;
                case EventType.MouseUp:
                    if (GUIUtility.hotControl == id)
                    {
                        GUIUtility.hotControl = 0;
                        s_dragCol = null;
                        e.Use();
                    }
                    break;
            }
        }

        private static void LayoutCells(Rect row, List<Column> cols, Action<Rect, Column> draw)
        {
            var widths = ResolveWidths(cols, row.width);
            float x = row.x;
            for (int c = 0; c < cols.Count; c++)
            {
                draw(new Rect(x + CellPad, row.y, widths[c] - CellPad * 2, row.height), cols[c]);
                x += widths[c];
            }
        }

        private static float[] ResolveWidths(List<Column> cols, float available, int minColumnWidth = 40)
        {
            var w = new float[cols.Count];
            float fixedTotal = 0f;
            int flex = 0;
            for (int i = 0; i < cols.Count; i++)
            {
                if (cols[i].WidthOverride > 0f) fixedTotal += cols[i].WidthOverride;
                else if (cols[i].HasWidth) fixedTotal += cols[i].Width;
                else flex++;
            }
            float flexW = flex > 0 ? Mathf.Max(minColumnWidth, (available - fixedTotal) / flex) : 0f;
            for (int i = 0; i < cols.Count; i++)
                w[i] = cols[i].WidthOverride > 0f ? cols[i].WidthOverride
                     : cols[i].HasWidth ? cols[i].Width : flexW;
            return w;
        }

        private static int MaxLines(List<Column> cols)
        {
            int max = 1;
            for (int i = 0; i < cols.Count; i++) if (cols[i].Lines > max) max = cols[i].Lines;
            return max;
        }

        private static bool TryColor(Column col, object element, out Color color)
        {
            color = Color.white;
            if (col.Color == null) return false;
            if (!string.IsNullOrEmpty(col.Color.GetColor))
            {
                var v = InspectorMemberResolver.GetValue(element, col.Color.GetColor, out bool failed);
                if (!failed && v is Color c) { color = c; return true; }
                return false; // unresolved (e.g. @-expression) → no tint
            }
            color = new Color(col.Color.R, col.Color.G, col.Color.B, col.Color.A);
            return true;
        }
    }
}
#endif
