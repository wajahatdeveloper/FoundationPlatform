#if UNITY_EDITOR
using System;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using AetherNexus.FoundationPlatform.AetherInspector;
using UnityEditor;
using UnityEngine;

namespace AetherNexus.FoundationPlatform.AetherInspector.Editor
{
    /// <summary>
    /// Simple LRU cache implementation using Dictionary + LinkedList.
    /// Thread-safe for single-threaded Unity main thread usage.
    /// </summary>
    internal sealed class LruCache<TKey, TValue>
    {
        private readonly int _capacity;
        private readonly Dictionary<TKey, LinkedListNode<LruEntry>> _map;
        private readonly LinkedList<LruEntry> _list;
        private readonly Action<TKey, TValue> _onEvict;

        public LruCache(int capacity, Action<TKey, TValue> onEvict = null)
        {
            _capacity = Math.Max(1, capacity);
            _map = new Dictionary<TKey, LinkedListNode<LruEntry>>();
            _list = new LinkedList<LruEntry>();
            _onEvict = onEvict;
        }

        public bool TryGetValue(TKey key, out TValue value)
        {
            if (_map.TryGetValue(key, out var node))
            {
                value = node.Value.Value;
                _list.Remove(node);
                _list.AddFirst(node);
                return true;
            }
            value = default;
            return false;
        }

        public void AddOrUpdate(TKey key, TValue value)
        {
            if (_map.TryGetValue(key, out var node))
            {
                node.Value.Value = value;
                _list.Remove(node);
                _list.AddFirst(node);
            }
            else
            {
                if (_map.Count >= _capacity)
                {
                    var lru = _list.Last;
                    if (lru != null)
                    {
                        _map.Remove(lru.Value.Key);
                        _onEvict?.Invoke(lru.Value.Key, lru.Value.Value);
                        _list.RemoveLast();
                    }
                }
                var newNode = new LinkedListNode<LruEntry>(new LruEntry { Key = key, Value = value });
                _list.AddFirst(newNode);
                _map[key] = newNode;
            }
        }

        public bool Remove(TKey key)
        {
            if (_map.TryGetValue(key, out var node))
            {
                _list.Remove(node);
                _map.Remove(key);
                return true;
            }
            return false;
        }

        public void Clear()
        {
            _map.Clear();
            _list.Clear();
        }

        public int Count => _map.Count;

        private sealed class LruEntry
        {
            public TKey Key;
            public TValue Value;
        }
    }

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
        private static readonly LruCache<string, int> s_pages = new LruCache<string, int>(100);
        private static readonly LruCache<string, string> s_search = new LruCache<string, string>(100);

        // Free-drag reorder state: row rects are captured on Repaint, drag events resolve the drop
        // index against them, and the move commits on MouseUp (the standard IMGUI reorder pattern —
        // works with variable-height engine-drawn elements where ReorderableList height callbacks can't).
        private static string s_dragKey;
        private static int s_dragIndex = -1;
        private static int s_dropIndex = -1;
        private static readonly LruCache<string, List<(int index, Rect rect)>> s_rowRects
            = new LruCache<string, List<(int, Rect)>>(100);

        public static void Draw(InspectorEntry e, object[] targets,
            Dictionary<string, bool> foldouts, Dictionary<string, int> tabs, Type elemType,
            ListDrawerSettingsAttribute lds, SearchableAttribute searchable,
            ValueDropdownAttribute vd, AssetSelectorAttribute asel,
            OnCollectionChangedAttribute occ, int maxDepth = -1, HashSet<object> visited = null)
        {
            var prop = e.Property;
            string key = prop.propertyPath;
            
            // Get label with proper fallback to LabelText attribute on the field
            var label = AetherInspectorRenderer.GetLabel(e, targets);
            if (label == null && e.Field != null)
            {
                var ltAttr = e.Field.GetCustomAttribute(typeof(LabelTextAttribute)) as LabelTextAttribute;
                if (ltAttr != null)
                {
                    string text = InspectorMemberResolver.ResolveString(targets[0], ltAttr.Text);
                    if (ltAttr.NicifyText && !string.IsNullOrEmpty(text)) 
                        text = ObjectNames.NicifyVariableName(text);
                    label = new GUIContent(text);
                }
            }
            if (label == null) label = new GUIContent(prop.displayName);
            
            var target = targets[0];
            bool readOnly = lds != null && lds.IsReadOnly;
            bool engineElems = elemType != null && !AetherInspectorRenderer.HasCustomPropertyDrawer(elemType)
                && !typeof(UnityEngine.Object).IsAssignableFrom(elemType)
                && (elemType.GetCustomAttribute<InlinePropertyAttribute>() != null
                    || AetherInspectorRenderer.TypeHasEngineAttributes(elemType));

            // Expansion: foldout persisted on the property; first-seen default from the settings.
            bool showFoldout = lds == null || lds.ShowFoldout;
            string initKey = "listinit:" + key;
            if (lds != null && foldouts != null && !foldouts.ContainsKey(initKey))
            {
                foldouts[initKey] = true;
                if (lds.DefaultExpandedState || lds.Expanded || lds.DisplayMode == ListDisplayMode.Expanded) prop.isExpanded = true;
                else if (lds.DisplayMode == ListDisplayMode.Collapsed) prop.isExpanded = false;
            }
            bool expanded = !showFoldout || prop.isExpanded;

            // --- Header row ---
            // Explicit-rect foldout (SectionHeaderRow) so nested list headers keep a reliable hit
            // target; EditorGUILayout.Foldout inside Horizontal+FlexibleSpace often mis-hits.
            string headerText = $"{label.text} ({prop.arraySize})";
            var headerContent = new GUIContent(headerText);
            bool showAdd = !readOnly && (lds == null || !lds.HideAddButton);
            bool disableAdd = vd != null && vd.DisableListAddButtonBehaviour && vd.IsUniqueList;
            Rect headerRect = EditorGUILayout.GetControlRect(false, EditorGUIUtility.singleLineHeight);

            if (showFoldout)
            {
                prop.isExpanded = AetherInspectorTheme.SectionHeaderRow(
                    headerRect, headerContent, prop.isExpanded, showAdd ? 1 : 0, out Rect[] trailing);
                expanded = prop.isExpanded;
                if (showAdd && trailing.Length > 0)
                {
                    using (new EditorGUI.DisabledScope(disableAdd))
                    {
                        if (GUI.Button(trailing[0], "+", EditorStyles.miniButton))
                            AddElement(e, targets, elemType, lds, occ);
                    }
                }
            }
            else
            {
                Rect[] trailing = Array.Empty<Rect>();
                if (showAdd)
                {
                    const float gap = 2f;
                    const float buttonSize = 20f;
                    const float buttonHeight = 16f;
                    float by = headerRect.y + (headerRect.height - buttonHeight) * 0.5f;
                    trailing = new[] { new Rect(headerRect.xMax - buttonSize, by, buttonSize, buttonHeight) };
                    headerRect = new Rect(headerRect.x, headerRect.y,
                        Mathf.Max(0f, headerRect.width - buttonSize - gap), headerRect.height);
                }
                GUI.Label(headerRect, headerContent, AetherInspectorTheme.FlatHeaderLabel);
                if (showAdd && trailing.Length > 0)
                {
                    using (new EditorGUI.DisabledScope(disableAdd))
                    {
                        if (GUI.Button(trailing[0], "+", EditorStyles.miniButton))
                            AddElement(e, targets, elemType, lds, occ);
                    }
                }
            }

            if (lds != null && !string.IsNullOrEmpty(lds.OnTitleBarGUI))
            {
                foreach (var t in targets) InvokeHook(t, lds.OnTitleBarGUI);
            }

            if (!expanded) return;

            // --- Search row ---
            string search = null;
            if (searchable != null)
            {
                s_search.TryGetValue(key, out search);
                search = EditorGUILayout.TextField(GUIContent.none, search ?? string.Empty, EditorStyles.toolbarSearchField);
                s_search.AddOrUpdate(key, search);
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
                using (new EditorGUILayout.HorizontalScope())
                {
                    GUILayout.FlexibleSpace();
                    using (new EditorGUI.DisabledScope(page <= 0))
                        if (GUILayout.Button("◀", EditorStyles.miniButtonLeft, GUILayout.Width(24))) page--;
                    GUILayout.Label($"{page + 1}/{pageCount}", EditorStyles.miniLabel, GUILayout.ExpandWidth(false));
                    using (new EditorGUI.DisabledScope(page >= pageCount - 1))
                        if (GUILayout.Button("▶", EditorStyles.miniButtonRight, GUILayout.Width(24))) page++;
                }
                s_pages.AddOrUpdate(key, page);
                start = page * pageSize;
                end = Mathf.Min(start + pageSize, count);
            }

            bool showIndex = lds != null && lds.ShowIndexLabels;
            bool movable = !readOnly && (lds == null || lds.DraggableItems);
            bool removable = !readOnly && (lds == null || !lds.HideRemoveButton);

            int removeIndex = -1, moveFrom = -1, moveTo = -1;
            var evt = Event.current;
            bool repaint = evt.type == EventType.Repaint;
            if (repaint) s_rowRects.AddOrUpdate(key, new List<(int, Rect)>());

            // movable rows reserve a "≡" drag-handle column to the left of element content; a nested
            // element's own foldout arrow must not pull left into that column (NestedGroupScope cancels
            // the pull), whereas non-draggable rows have nothing there to protect (plain indent is fine).
            IDisposable rowsIndentScope = movable
                ? new AetherInspectorTheme.NestedGroupScope()
                : (IDisposable)new AetherInspectorTheme.NestedIndentScope();
            using (rowsIndentScope)
            {
                for (int i = start; i < end; i++)
                {
                    var elemProp = prop.GetArrayElementAtIndex(i);
                    string elemLabel = ElementLabel(elemProp, lds, showIndex, i);

                    if (!MatchSearch(elemProp, elemLabel, search, searchable != null && searchable.Recursive))
                        continue;

                    if (lds != null && !string.IsNullOrEmpty(lds.OnBeginListElementGUI))
                    {
                        foreach (var t in targets) InvokeHook(t, lds.OnBeginListElementGUI, i);
                    }

// HorizontalScope/VerticalScope below guarantee the row closes even if DrawElement
                    // throws (e.g. a reflection-driven element drawer) — otherwise one bad row would
                    // corrupt the layout stack for every row after it.
                    using (var rowScope = new EditorGUILayout.HorizontalScope())
                    {
                        if (repaint && s_rowRects.TryGetValue(key, out var rectList))
                            rectList.Add((i, rowScope.rect));

                        // Drag handle on the left
                        if (movable)
                        {
                            GUILayout.Label("≡", EditorStyles.centeredGreyMiniLabel, GUILayout.Width(14), GUILayout.Height(18));
                            HandleRowDrag(GUILayoutUtility.GetLastRect(), key, i, ref moveFrom, ref moveTo);
                        }

                        // Element content - fills remaining horizontal space
                        using (new EditorGUILayout.VerticalScope(GUILayout.ExpandWidth(true)))
                        using (new EditorGUI.DisabledScope(readOnly))
                            DrawElement(e, targets, foldouts, tabs, elemProp, elemType, engineElems, vd, asel, elemLabel, i, maxDepth, visited);

                        // Remove button on the right
                        if (removable)
                        {
                            if (GUILayout.Button("✕", EditorStyles.miniButton, GUILayout.Width(20), GUILayout.Height(18)))
                                removeIndex = i;
                        }
                    }

                    if (lds != null && !string.IsNullOrEmpty(lds.OnEndListElementGUI))
                    {
                        foreach (var t in targets) InvokeHook(t, lds.OnEndListElementGUI, i);
                    }
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
            }

            if (moveFrom >= 0 && moveTo != moveFrom)
            {
                NotifyCollection(targets, occ, before: true);
                prop.MoveArrayElement(moveFrom, moveTo);
                prop.serializedObject.ApplyModifiedProperties();
                NotifyCollection(targets, occ, before: false);
                foreach (var t in targets)
                {
                    if (t is UnityEngine.Object uo) EditorUtility.SetDirty(uo);
                }
            }
            if (removeIndex >= 0)
                RemoveElement(e, targets, removeIndex, lds, occ);
        }

        // Drag handle event pump. Rects come from the previous Repaint; the move commits on MouseUp.
        private static void HandleRowDrag(Rect handleRect, string key, int index, ref int moveFrom, ref int moveTo)
        {
            EditorGUIUtility.AddCursorRect(handleRect, MouseCursor.MoveArrow);
            // Hashed on the list's property path + row index (not call order) so paging/searching/filtering,
            // which change how many rows precede this one, can't shift which control receives the drag.
            int id = GUIUtility.GetControlID((key + "#row" + index).GetHashCode(), FocusType.Passive);
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

        private static void DrawElement(InspectorEntry e, object[] targets,
            Dictionary<string, bool> foldouts, Dictionary<string, int> tabs,
            SerializedProperty elemProp, Type elemType, bool engineElems,
            ValueDropdownAttribute vd, AssetSelectorAttribute asel, string elemLabel, int index,
            int maxDepth = -1, HashSet<object> visited = null)
        {
            var labelContent = string.IsNullOrEmpty(elemLabel) ? GUIContent.none : new GUIContent(elemLabel);

            if (vd != null && vd.DrawDropdownForListElements &&
                InspectorDropdown.DrawValueDropdownElement(elemProp, e, targets, vd, elemLabel))
                return;

            if (asel != null && asel.DrawDropdownForListElements &&
                elemProp.propertyType == SerializedPropertyType.ObjectReference && elemType != null)
            {
                InspectorDropdown.DrawAssetSelectorElement(elemProp, e, targets, asel, elemType, elemLabel);
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
                // Resolve element instances from current list owners — never walk full root
                // propertyPath against already-nested targets (breaks L2+ engine attrs).
                object[] elemTargets = ResolveListElementTargets(targets, e, index);
                AetherInspectorRenderer.DrawNestedObject(entry, targets, foldouts, tabs, inline: elemInline,
                    labelOverride: labelContent, maxDepth: maxDepth, visited: visited,
                    preResolvedTargets: elemTargets.Length > 0 ? elemTargets : null);
                return;
            }

            EditorGUI.BeginChangeCheck();
            EditorGUILayout.PropertyField(elemProp, labelContent, true);
            if (EditorGUI.EndChangeCheck())
            {
                elemProp.serializedObject.ApplyModifiedProperties();
                AetherInspectorRenderer.InvokeOnValueChanged(e, targets);
            }
        }

        /// <summary>
        /// Boxed list element instances for the current list-owner targets (relative to owners,
        /// not via root SerializedProperty path).
        /// </summary>
        private static object[] ResolveListElementTargets(object[] listOwners, InspectorEntry listEntry, int index)
        {
            if (listOwners == null || listOwners.Length == 0) return Array.Empty<object>();

            var list = new List<object>(listOwners.Length);
            string fallbackMember = null;
            if (listEntry.Field == null && listEntry.Property != null)
                fallbackMember = ListFieldNameFromPath(listEntry.Property.propertyPath);

            foreach (var owner in listOwners)
            {
                if (owner == null) continue;

                object listObj = null;
                if (listEntry.Field != null)
                {
                    try { listObj = listEntry.Field.GetValue(owner); }
                    catch { listObj = null; }
                }
                else if (!string.IsNullOrEmpty(fallbackMember))
                {
                    listObj = InspectorMemberResolver.GetValue(owner, fallbackMember, out bool failed);
                    if (failed) listObj = null;
                }

                if (listObj is IList ilist && index >= 0 && index < ilist.Count)
                {
                    var elem = ilist[index];
                    if (elem != null) list.Add(elem);
                }
            }

            return list.ToArray();
        }

        private static string ListFieldNameFromPath(string propertyPath)
        {
            if (string.IsNullOrEmpty(propertyPath)) return null;
            // e.g. typeMappings.Array.data[0].Tokens → Tokens
            //      Tokens → Tokens
            int arrayIdx = propertyPath.LastIndexOf(".Array", StringComparison.Ordinal);
            string beforeArray = arrayIdx >= 0 ? propertyPath.Substring(0, arrayIdx) : propertyPath;
            int dot = beforeArray.LastIndexOf('.');
            return dot >= 0 ? beforeArray.Substring(dot + 1) : beforeArray;
        }

        private static string ElementLabel(SerializedProperty elemProp, ListDrawerSettingsAttribute lds, bool showIndex, int index)
        {
            if (lds != null && !lds.ShowElementLabels)
                return string.Empty;

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
            // SerializeReference polymorphic elements: fall back to the concrete type name
            // (the actual step/verb) instead of Unity's generic "Element N" displayName.
            if (label == null && elemProp.propertyType == SerializedPropertyType.ManagedReference)
                label = ManagedReferenceTypeLabel(elemProp);
            if (label == null) label = elemProp.displayName;
            return showIndex ? $"{index}: {label}" : label;
        }

        // "UnityEngine.CoreModule GameplayAbilitySystem.ApplyEffectStep" -> "Apply Effect".
        private static string ManagedReferenceTypeLabel(SerializedProperty elemProp)
        {
            string full = elemProp.managedReferenceFullTypename;
            if (string.IsNullOrEmpty(full)) return null; // null/empty reference: leave as displayName ("None")
            int space = full.LastIndexOf(' ');
            string typeName = space >= 0 ? full.Substring(space + 1) : full;
            int dot = typeName.LastIndexOf('.');
            if (dot >= 0) typeName = typeName.Substring(dot + 1);
            if (typeName.EndsWith("Step", StringComparison.Ordinal) && typeName.Length > 4)
                typeName = typeName.Substring(0, typeName.Length - 4);
            return ObjectNames.NicifyVariableName(typeName);
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

        private static void AddElement(InspectorEntry e, object[] targets, Type elemType,
            ListDrawerSettingsAttribute lds, OnCollectionChangedAttribute occ)
        {
            var prop = e.Property;
            NotifyCollection(targets, occ, before: true);

            if (lds != null && !string.IsNullOrEmpty(lds.CustomAddFunction))
            {
                foreach (var target in targets)
                {
                    var mi = InspectorMemberResolver.FindMethod(target.GetType(), lds.CustomAddFunction, Type.EmptyTypes);
                    if (mi != null)
                    {
                        try
                        {
                            object result = mi.Invoke(mi.IsStatic ? null : target, null);
                            if (mi.ReturnType != typeof(void))
                            {
                                // Write to the newly added index on all targets or let Unity copy.
                                // But since arraySize++ is called, we just let it copy or we can set it.
                            }
                        }
                        catch (Exception ex) { Debug.LogWarning($"[FoundationPlatform.AetherInspector] CustomAddFunction '{lds.CustomAddFunction}' threw: {ex.InnerException?.Message ?? ex.Message}"); }
                    }
                }
                // void custom add: target callback already mutated the list.
                // We let serializedObject update to sync the elements.
                prop.serializedObject.ApplyModifiedProperties();
                prop.serializedObject.Update();
                FinishMutation(e, targets, occ);
                return;
            }

            prop.arraySize++;
            // Unity's insert copies the previous element (== AddCopiesLastElement);
            // AlwaysAddDefaultValue forces a cleared element instead.
            if (lds != null && lds.AlwaysAddDefaultValue && !lds.AddCopiesLastElement)
                ClearToDefault(prop.GetArrayElementAtIndex(prop.arraySize - 1), elemType);
            prop.serializedObject.ApplyModifiedProperties();
            FinishMutation(e, targets, occ);
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

        private static void RemoveElement(InspectorEntry e, object[] targets, int index,
            ListDrawerSettingsAttribute lds, OnCollectionChangedAttribute occ)
        {
            var prop = e.Property;
            NotifyCollection(targets, occ, before: true);

            if (lds != null && !string.IsNullOrEmpty(lds.CustomRemoveIndexFunction))
            {
                foreach (var target in targets)
                {
                    var mi = InspectorMemberResolver.FindMethod(target.GetType(), lds.CustomRemoveIndexFunction, typeof(int));
                    if (mi != null)
                    {
                        try { mi.Invoke(mi.IsStatic ? null : target, new object[] { index }); }
                        catch (Exception ex) { Debug.LogWarning($"[FoundationPlatform.AetherInspector] CustomRemoveIndexFunction threw: {ex.InnerException?.Message ?? ex.Message}"); }
                    }
                }
                prop.serializedObject.Update();
                FinishMutation(e, targets, occ);
                return;
            }

            if (lds != null && !string.IsNullOrEmpty(lds.CustomRemoveElementFunction))
            {
                object value = null;
                try { value = prop.GetArrayElementAtIndex(index).boxedValue; } catch { }
                foreach (var target in targets)
                {
                    foreach (var mi in InspectorMemberResolver.FindMethods(target.GetType(), lds.CustomRemoveElementFunction))
                    {
                        var ps = mi.GetParameters();
                        if (ps.Length != 1) continue;
                        try { mi.Invoke(mi.IsStatic ? null : target, new[] { value }); }
                        catch (Exception ex) { Debug.LogWarning($"[FoundationPlatform.AetherInspector] CustomRemoveElementFunction threw: {ex.InnerException?.Message ?? ex.Message}"); }
                    }
                }
                prop.serializedObject.Update();
                FinishMutation(e, targets, occ);
                return;
            }

            prop.DeleteArrayElementAtIndex(index);
            prop.serializedObject.ApplyModifiedProperties();
            FinishMutation(e, targets, occ);
        }

        private static void FinishMutation(InspectorEntry e, object[] targets, OnCollectionChangedAttribute occ)
        {
            NotifyCollection(targets, occ, before: false);
            AetherInspectorRenderer.InvokeOnValueChanged(e, targets);
            foreach (var target in targets)
            {
                if (target is UnityEngine.Object uo) EditorUtility.SetDirty(uo);
            }
        }

        private static void NotifyCollection(object[] targets, OnCollectionChangedAttribute occ, bool before)
        {
            if (occ == null) return;
            string method = before ? occ.Before : occ.After;
            if (string.IsNullOrEmpty(method)) return;
            foreach (var target in targets)
            {
                InvokeHook(target, method);
            }
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
                Debug.LogWarning($"[FoundationPlatform.AetherInspector] list hook '{method}' threw: {ex.InnerException?.Message ?? ex.Message}");
            }
        }
        private static bool MatchSearch(SerializedProperty elemProp, string elemLabel, string search, bool recursive)
        {
            if (string.IsNullOrEmpty(search)) return true;
            if (elemLabel != null && elemLabel.IndexOf(search, StringComparison.OrdinalIgnoreCase) >= 0) return true;
            string valText = ElementValueText(elemProp);
            if (valText != null && valText.IndexOf(search, StringComparison.OrdinalIgnoreCase) >= 0) return true;
            if (recursive && elemProp != null)
            {
                var copy = elemProp.Copy();
                var end = elemProp.GetEndProperty();
                while (copy.NextVisible(true) && !SerializedProperty.EqualContents(copy, end))
                {
                    string text = ElementValueText(copy);
                    if (!string.IsNullOrEmpty(text) && text.IndexOf(search, StringComparison.OrdinalIgnoreCase) >= 0) return true;
                }
            }
            return false;
        }
    }
}
#endif
