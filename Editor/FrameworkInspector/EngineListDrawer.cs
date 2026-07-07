#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.Reflection;
using UnityEditor;
using UnityEngine;

namespace FoundationPlatform.FrameworkInspector.Editor
{
    /// <summary>
    /// Collection renderer honoring the <c>[ListDrawerSettings]</c> surface — foldout, index labels,
    /// paging, add/remove/move controls, custom add/remove callbacks, per-element label member,
    /// <c>OnTitleBarGUI</c> / <c>OnBeginListElementGUI</c> / <c>OnEndListElementGUI</c> hooks,
    /// <c>[Searchable]</c> filtering, <c>[OnCollectionChanged]</c> notifications and
    /// <c>[ValueDropdown]</c>/<c>[AssetSelector]</c> element dropdowns. Element rows recurse through
    /// the attribute engine when the element type is engine-attributed. Rows reorder by free drag
    /// on the ≡ handle (row rects captured on Repaint; drop resolved against them on MouseUp).
    /// </summary>
    internal static class EngineListDrawer
    {
        private static readonly Dictionary<string, int> s_pages = new Dictionary<string, int>();
        private static readonly Dictionary<string, string> s_search = new Dictionary<string, string>();

        // Free-drag reorder state: row rects are captured on Repaint, drag events resolve the drop
        // index against them, and the move commits on MouseUp (the standard IMGUI reorder pattern —
        // works with variable-height engine-drawn elements where ReorderableList height callbacks can't).
        private static string s_dragKey;
        private static int s_dragIndex = -1;
        private static int s_dropIndex = -1;
        private static readonly Dictionary<string, List<(int index, Rect rect)>> s_rowRects
            = new Dictionary<string, List<(int, Rect)>>();

        public static void Draw(InspectorEntry e, object target,
            Dictionary<string, bool> foldouts, Dictionary<string, int> tabs, Type elemType,
            ListDrawerSettingsAttribute lds, SearchableAttribute searchable,
            ValueDropdownAttribute vd, AssetSelectorAttribute asel,
            OnCollectionChangedAttribute occ)
        {
            var prop = e.Property;
            string key = prop.propertyPath;
            var label = FrameworkInspectorRenderer.GetLabel(e, target) ?? new GUIContent(prop.displayName);
            bool readOnly = lds != null && lds.IsReadOnly;
            bool engineElems = elemType != null && !FrameworkInspectorRenderer.HasCustomPropertyDrawer(elemType)
                && (elemType.GetCustomAttribute<InlinePropertyAttribute>() != null
                    || FrameworkInspectorRenderer.TypeHasEngineAttributes(elemType));

            // Expansion: foldout persisted on the property; first-seen default from the settings.
            bool showFoldout = lds == null || lds.ShowFoldout;
            string initKey = "listinit:" + key;
            if (lds != null && foldouts != null && !foldouts.ContainsKey(initKey))
            {
                foldouts[initKey] = true;
                if (lds.DefaultExpandedState || lds.Expanded) prop.isExpanded = true;
            }
            bool expanded = !showFoldout || prop.isExpanded;

            // --- Header row ---
            EditorGUILayout.BeginHorizontal();
            string headerText = $"{label.text} ({prop.arraySize})";
            if (showFoldout)
            {
                prop.isExpanded = EditorGUILayout.Foldout(prop.isExpanded, headerText, true);
                expanded = prop.isExpanded;
            }
            else EditorGUILayout.LabelField(headerText, EditorStyles.boldLabel);

            GUILayout.FlexibleSpace();

            if (lds != null && !string.IsNullOrEmpty(lds.OnTitleBarGUI))
                InvokeHook(target, lds.OnTitleBarGUI);

            if (!readOnly && (lds == null || !lds.HideAddButton))
            {
                if (GUILayout.Button("+", EditorStyles.miniButton, GUILayout.Width(22)))
                    AddElement(e, target, elemType, lds, occ);
            }
            EditorGUILayout.EndHorizontal();

            if (!expanded) return;

            // --- Search row ---
            string search = null;
            if (searchable != null)
            {
                s_search.TryGetValue(key, out search);
                search = EditorGUILayout.TextField(GUIContent.none, search ?? string.Empty, EditorStyles.toolbarSearchField);
                s_search[key] = search;
            }

            // --- Paging ---
            int count = prop.arraySize;
            int pageSize = 0;
            if (lds != null && lds.ShowPaging)
                pageSize = lds.NumberOfItemsPerPage > 0 ? lds.NumberOfItemsPerPage : 15;
            int page = 0, pageCount = 1, start = 0, end = count;
            if (pageSize > 0 && count > pageSize)
            {
                pageCount = (count + pageSize - 1) / pageSize;
                s_pages.TryGetValue(key, out page);
                page = Mathf.Clamp(page, 0, pageCount - 1);
                EditorGUILayout.BeginHorizontal();
                GUILayout.FlexibleSpace();
                using (new EditorGUI.DisabledScope(page <= 0))
                    if (GUILayout.Button("◀", EditorStyles.miniButtonLeft, GUILayout.Width(24))) page--;
                GUILayout.Label($"{page + 1}/{pageCount}", EditorStyles.miniLabel, GUILayout.ExpandWidth(false));
                using (new EditorGUI.DisabledScope(page >= pageCount - 1))
                    if (GUILayout.Button("▶", EditorStyles.miniButtonRight, GUILayout.Width(24))) page++;
                EditorGUILayout.EndHorizontal();
                s_pages[key] = page;
                start = page * pageSize;
                end = Mathf.Min(start + pageSize, count);
            }

            bool showIndex = lds != null && lds.ShowIndexLabels;
            bool movable = !readOnly && (lds == null || lds.DraggableItems);
            bool removable = !readOnly && (lds == null || !lds.HideRemoveButton);

            EditorGUI.indentLevel++;
            int removeIndex = -1, moveFrom = -1, moveTo = -1;
            var evt = Event.current;
            bool repaint = evt.type == EventType.Repaint;
            if (repaint) s_rowRects[key] = new List<(int, Rect)>();

            for (int i = start; i < end; i++)
            {
                var elemProp = prop.GetArrayElementAtIndex(i);
                string elemLabel = ElementLabel(elemProp, lds, showIndex, i);

                if (!string.IsNullOrEmpty(search) &&
                    elemLabel.IndexOf(search, StringComparison.OrdinalIgnoreCase) < 0 &&
                    ElementValueText(elemProp).IndexOf(search, StringComparison.OrdinalIgnoreCase) < 0)
                    continue;

                if (lds != null && !string.IsNullOrEmpty(lds.OnBeginListElementGUI))
                    InvokeHook(target, lds.OnBeginListElementGUI, i);

                var rowRect = EditorGUILayout.BeginHorizontal();
                if (repaint) s_rowRects[key].Add((i, rowRect));

                if (movable)
                {
                    GUILayout.Label("≡", EditorStyles.centeredGreyMiniLabel, GUILayout.Width(14), GUILayout.Height(18));
                    HandleRowDrag(GUILayoutUtility.GetLastRect(), key, i, ref moveFrom, ref moveTo);
                }

                EditorGUILayout.BeginVertical();
                using (new EditorGUI.DisabledScope(readOnly))
                    DrawElement(e, target, foldouts, tabs, elemProp, elemType, engineElems, vd, asel, elemLabel);
                EditorGUILayout.EndVertical();

                if (removable)
                {
                    if (GUILayout.Button("✕", EditorStyles.miniButton, GUILayout.Width(20), GUILayout.Height(18)))
                        removeIndex = i;
                }
                EditorGUILayout.EndHorizontal();

                if (lds != null && !string.IsNullOrEmpty(lds.OnEndListElementGUI))
                    InvokeHook(target, lds.OnEndListElementGUI, i);
            }

            // Insertion indicator while dragging over this list.
            if (repaint && s_dragKey == key && s_dropIndex >= 0 && s_rowRects.TryGetValue(key, out var rects) && rects.Count > 0)
            {
                Rect line;
                var after = rects.FindIndex(r => r.index >= s_dropIndex);
                if (after >= 0) { var r = rects[after].rect; line = new Rect(r.x, r.yMin - 1, r.width, 2); }
                else { var r = rects[rects.Count - 1].rect; line = new Rect(r.x, r.yMax - 1, r.width, 2); }
                EditorGUI.DrawRect(line, new Color(0.24f, 0.49f, 0.90f));
            }

            EditorGUI.indentLevel--;

            if (moveFrom >= 0 && moveTo != moveFrom)
            {
                NotifyCollection(target, occ, before: true);
                prop.MoveArrayElement(moveFrom, moveTo);
                prop.serializedObject.ApplyModifiedProperties();
                NotifyCollection(target, occ, before: false);
                if (target is UnityEngine.Object uo) EditorUtility.SetDirty(uo);
            }
            if (removeIndex >= 0)
                RemoveElement(e, target, removeIndex, lds, occ);
        }

        // Drag handle event pump. Rects come from the previous Repaint; the move commits on MouseUp.
        private static void HandleRowDrag(Rect handleRect, string key, int index, ref int moveFrom, ref int moveTo)
        {
            EditorGUIUtility.AddCursorRect(handleRect, MouseCursor.MoveArrow);
            int id = GUIUtility.GetControlID(FocusType.Passive);
            var evt = Event.current;
            switch (evt.GetTypeForControl(id))
            {
                case EventType.MouseDown:
                    if (handleRect.Contains(evt.mousePosition))
                    {
                        GUIUtility.hotControl = id;
                        s_dragKey = key;
                        s_dragIndex = index;
                        s_dropIndex = index;
                        evt.Use();
                    }
                    break;
                case EventType.MouseDrag:
                    if (GUIUtility.hotControl == id && s_dragKey == key)
                    {
                        s_dropIndex = ResolveDropIndex(key, evt.mousePosition.y);
                        evt.Use();
                    }
                    break;
                case EventType.MouseUp:
                    if (GUIUtility.hotControl == id && s_dragKey == key)
                    {
                        GUIUtility.hotControl = 0;
                        int from = s_dragIndex;
                        int to = s_dropIndex > s_dragIndex ? s_dropIndex - 1 : s_dropIndex;
                        s_dragKey = null;
                        s_dragIndex = s_dropIndex = -1;
                        if (from >= 0 && to >= 0 && to != from) { moveFrom = from; moveTo = to; }
                        evt.Use();
                    }
                    break;
            }
        }

        private static int ResolveDropIndex(string key, float mouseY)
        {
            if (!s_rowRects.TryGetValue(key, out var rects) || rects.Count == 0) return s_dropIndex;
            foreach (var (index, rect) in rects)
                if (mouseY < rect.center.y) return index;
            return rects[rects.Count - 1].index + 1;
        }

        private static void DrawElement(InspectorEntry e, object target,
            Dictionary<string, bool> foldouts, Dictionary<string, int> tabs,
            SerializedProperty elemProp, Type elemType, bool engineElems,
            ValueDropdownAttribute vd, AssetSelectorAttribute asel, string elemLabel)
        {
            if (vd != null && vd.DrawDropdownForListElements &&
                InspectorDropdown.DrawValueDropdownElement(elemProp, e, target, vd, elemLabel))
                return;

            if (asel != null && asel.DrawDropdownForListElements &&
                elemProp.propertyType == SerializedPropertyType.ObjectReference && elemType != null)
            {
                InspectorDropdown.DrawAssetSelectorElement(elemProp, e, target, asel, elemType, elemLabel);
                return;
            }

            if (engineElems)
            {
                bool elemInline = elemType.GetCustomAttribute<InlinePropertyAttribute>() != null;
                var entry = new InspectorEntry
                {
                    EntryKind = InspectorEntry.Kind.Field,
                    Property = elemProp,
                    AttributeSource = null,
                };
                FrameworkInspectorRenderer.DrawNestedObject(entry, target, foldouts, tabs, inline: elemInline,
                    labelOverride: new GUIContent(elemLabel));
                return;
            }

            EditorGUI.BeginChangeCheck();
            EditorGUILayout.PropertyField(elemProp, new GUIContent(elemLabel), true);
            if (EditorGUI.EndChangeCheck())
            {
                elemProp.serializedObject.ApplyModifiedProperties();
                FrameworkInspectorRenderer.InvokeOnValueChanged(e, target);
            }
        }

        private static string ElementLabel(SerializedProperty elemProp, ListDrawerSettingsAttribute lds, bool showIndex, int index)
        {
            string label = null;
            if (lds != null && !string.IsNullOrEmpty(lds.ListElementLabelName))
            {
                object boxed = null;
                try { boxed = elemProp.boxedValue; } catch { }
                if (boxed != null)
                {
                    var v = InspectorMemberResolver.GetValue(boxed, lds.ListElementLabelName, out bool failed);
                    if (!failed && v != null) label = v.ToString();
                }
            }
            if (label == null) label = elemProp.displayName;
            return showIndex ? $"{index}: {label}" : label;
        }

        private static string ElementValueText(SerializedProperty elemProp)
        {
            switch (elemProp.propertyType)
            {
                case SerializedPropertyType.String: return elemProp.stringValue ?? string.Empty;
                case SerializedPropertyType.ObjectReference: return elemProp.objectReferenceValue != null ? elemProp.objectReferenceValue.name : string.Empty;
                case SerializedPropertyType.Enum: return elemProp.enumNames.Length > 0 && elemProp.enumValueIndex >= 0 && elemProp.enumValueIndex < elemProp.enumNames.Length ? elemProp.enumNames[elemProp.enumValueIndex] : string.Empty;
                default: return string.Empty;
            }
        }

        // ---------------------------------------------------------------- add / remove

        private static void AddElement(InspectorEntry e, object target, Type elemType,
            ListDrawerSettingsAttribute lds, OnCollectionChangedAttribute occ)
        {
            var prop = e.Property;
            NotifyCollection(target, occ, before: true);

            if (lds != null && !string.IsNullOrEmpty(lds.CustomAddFunction))
            {
                var mi = InspectorMemberResolver.FindMethod(target.GetType(), lds.CustomAddFunction, Type.EmptyTypes);
                if (mi != null)
                {
                    try
                    {
                        object result = mi.Invoke(mi.IsStatic ? null : target, null);
                        if (mi.ReturnType != typeof(void))
                        {
                            prop.arraySize++;
                            var last = prop.GetArrayElementAtIndex(prop.arraySize - 1);
                            FrameworkInspectorRenderer.WriteProperty(last, result);
                        }
                        // void: the callback mutated the list itself.
                        prop.serializedObject.ApplyModifiedProperties();
                        prop.serializedObject.Update();
                    }
                    catch (Exception ex) { Debug.LogWarning($"[FoundationPlatform.FrameworkInspector] CustomAddFunction '{lds.CustomAddFunction}' threw: {ex.InnerException?.Message ?? ex.Message}"); }
                    FinishMutation(e, target, occ);
                    return;
                }
            }

            prop.arraySize++;
            // Unity's insert copies the previous element (== AddCopiesLastElement);
            // AlwaysAddDefaultValue forces a cleared element instead.
            if (lds != null && lds.AlwaysAddDefaultValue && !lds.AddCopiesLastElement)
                ClearToDefault(prop.GetArrayElementAtIndex(prop.arraySize - 1), elemType);
            prop.serializedObject.ApplyModifiedProperties();
            FinishMutation(e, target, occ);
        }

        private static void ClearToDefault(SerializedProperty p, Type elemType)
        {
            switch (p.propertyType)
            {
                case SerializedPropertyType.Integer: p.intValue = 0; break;
                case SerializedPropertyType.Float: p.floatValue = 0f; break;
                case SerializedPropertyType.Boolean: p.boolValue = false; break;
                case SerializedPropertyType.String: p.stringValue = string.Empty; break;
                case SerializedPropertyType.ObjectReference: p.objectReferenceValue = null; break;
                case SerializedPropertyType.Enum: p.intValue = 0; break;
                default:
                    if (elemType != null && elemType.IsValueType)
                        try { p.boxedValue = Activator.CreateInstance(elemType); } catch { }
                    else if (elemType != null && !typeof(UnityEngine.Object).IsAssignableFrom(elemType))
                        try { p.boxedValue = Activator.CreateInstance(elemType); } catch { }
                    break;
            }
        }

        private static void RemoveElement(InspectorEntry e, object target, int index,
            ListDrawerSettingsAttribute lds, OnCollectionChangedAttribute occ)
        {
            var prop = e.Property;
            NotifyCollection(target, occ, before: true);

            if (lds != null && !string.IsNullOrEmpty(lds.CustomRemoveIndexFunction))
            {
                var mi = InspectorMemberResolver.FindMethod(target.GetType(), lds.CustomRemoveIndexFunction, typeof(int));
                if (mi != null)
                {
                    try { mi.Invoke(mi.IsStatic ? null : target, new object[] { index }); }
                    catch (Exception ex) { Debug.LogWarning($"[FoundationPlatform.FrameworkInspector] CustomRemoveIndexFunction threw: {ex.InnerException?.Message ?? ex.Message}"); }
                    prop.serializedObject.Update();
                    FinishMutation(e, target, occ);
                    return;
                }
            }

            if (lds != null && !string.IsNullOrEmpty(lds.CustomRemoveElementFunction))
            {
                object value = null;
                try { value = prop.GetArrayElementAtIndex(index).boxedValue; } catch { }
                foreach (var mi in InspectorMemberResolver.FindMethods(target.GetType(), lds.CustomRemoveElementFunction))
                {
                    var ps = mi.GetParameters();
                    if (ps.Length != 1) continue;
                    try { mi.Invoke(mi.IsStatic ? null : target, new[] { value }); }
                    catch (Exception ex) { Debug.LogWarning($"[FoundationPlatform.FrameworkInspector] CustomRemoveElementFunction threw: {ex.InnerException?.Message ?? ex.Message}"); }
                    prop.serializedObject.Update();
                    FinishMutation(e, target, occ);
                    return;
                }
            }

            prop.DeleteArrayElementAtIndex(index);
            prop.serializedObject.ApplyModifiedProperties();
            FinishMutation(e, target, occ);
        }

        private static void FinishMutation(InspectorEntry e, object target, OnCollectionChangedAttribute occ)
        {
            NotifyCollection(target, occ, before: false);
            FrameworkInspectorRenderer.InvokeOnValueChanged(e, target);
            if (target is UnityEngine.Object uo) EditorUtility.SetDirty(uo);
        }

        private static void NotifyCollection(object target, OnCollectionChangedAttribute occ, bool before)
        {
            if (occ == null) return;
            string method = before ? occ.Before : occ.After;
            if (string.IsNullOrEmpty(method)) return;
            InvokeHook(target, method);
        }

        private static void InvokeHook(object target, string method, int? index = null)
        {
            try
            {
                if (index.HasValue)
                {
                    var withInt = InspectorMemberResolver.FindMethod(target.GetType(), method, typeof(int));
                    if (withInt != null) { withInt.Invoke(withInt.IsStatic ? null : target, new object[] { index.Value }); return; }
                }
                var mi = InspectorMemberResolver.FindMethod(target.GetType(), method, Type.EmptyTypes);
                mi?.Invoke(mi.IsStatic ? null : target, null);
            }
            catch (Exception ex)
            {
                Debug.LogWarning($"[FoundationPlatform.FrameworkInspector] list hook '{method}' threw: {ex.InnerException?.Message ?? ex.Message}");
            }
        }
    }
}
#endif
