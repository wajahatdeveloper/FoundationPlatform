#if UNITY_EDITOR
using System;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using UnityEditor;
using UnityEngine;

namespace FoundationPlatform.FrameworkInspector.Editor
{
    /// <summary>
    /// Dropdown infrastructure for <c>[ValueDropdown]</c> and <c>[AssetSelector]</c>.
    /// Small option sets use a <see cref="GenericMenu"/>
    /// (nested "a/b" labels become submenus); once the option count reaches
    /// <c>NumberOfItemsBeforeEnablingSearch</c> a searchable dropdown window opens instead.
    /// Selection is applied deferred (menus fire after the layout pass), re-resolving the property
    /// by path so the write lands on live serialized state.
    /// </summary>
    internal static class InspectorDropdown
    {
        internal sealed class Option
        {
            public string Label;
            public object Value;
        }

        // ---------------------------------------------------------------- ValueDropdown

        /// <summary>Draw the dropdown for a scalar field. Returns false when the getter is unresolvable → caller falls back.</summary>
        public static bool DrawValueDropdown(InspectorEntry e, object target, ValueDropdownAttribute vd, string labelText)
        {
            var options = BuildValueOptions(target, vd);
            if (options == null) return false;
            DrawDropdownForProperty(e.Property, e, target, vd, options, labelText ?? e.Property.displayName);
            return true;
        }

        /// <summary>Draw the dropdown row for a collection element (DrawDropdownForListElements).</summary>
        public static bool DrawValueDropdownElement(SerializedProperty elemProp, InspectorEntry owner, object target,
            ValueDropdownAttribute vd, string label)
        {
            var options = BuildValueOptions(target, vd);
            if (options == null) return false;
            DrawDropdownForProperty(elemProp, owner, target, vd, options, label);
            return true;
        }

        internal static List<Option> BuildValueOptions(object target, ValueDropdownAttribute vd)
        {
            var raw = InspectorMemberResolver.GetValue(target, vd.ValuesGetter, out bool failed);
            if (failed || raw is string || !(raw is IEnumerable en)) return null;

            var options = new List<Option>();
            foreach (var item in en)
            {
                if (item == null) { options.Add(new Option { Label = "(null)", Value = null }); continue; }
                var it = item.GetType();
                if (it.IsGenericType && it.GetGenericTypeDefinition() == typeof(ValueDropdownItem<>))
                {
                    var text = it.GetField("Text")?.GetValue(item) as string;
                    var val = it.GetField("Value")?.GetValue(item);
                    options.Add(new Option { Label = text ?? val?.ToString() ?? "(null)", Value = val });
                }
                else options.Add(new Option { Label = item.ToString(), Value = item });
            }
            if (options.Count == 0) return null;
            if (vd.SortDropdownItems)
                options.Sort((a, b) => string.Compare(a.Label, b.Label, StringComparison.OrdinalIgnoreCase));
            if (vd.FlattenTreeView)
                foreach (var o in options) { int i = o.Label.LastIndexOf('/'); if (i >= 0) o.Label = o.Label.Substring(i + 1); }
            return options;
        }

        private static void DrawDropdownForProperty(SerializedProperty prop, InspectorEntry owner, object target,
            ValueDropdownAttribute vd, List<Option> options, string label)
        {
            object current = FrameworkInspectorRenderer.ReadProperty(prop);
            string currentLabel = "(none)";
            foreach (var o in options)
                if (InspectorMemberResolver.ValuesEqual(current, o.Value)) { currentLabel = LeafOf(o.Label); break; }
            if (currentLabel == "(none)" && current != null) currentLabel = current.ToString();

            var rect = EditorGUILayout.GetControlRect();
            rect = EditorGUI.PrefixLabel(rect, new GUIContent(label));
            if (!EditorGUI.DropdownButton(rect, new GUIContent(currentLabel), FocusType.Keyboard)) return;

            var so = prop.serializedObject;
            string path = prop.propertyPath;
            var src = owner.AttributeSource;
            var field = owner.Field;

            Action<object> apply = value => ApplyDeferred(so, path, value, src, field, target);

            if (options.Count >= vd.NumberOfItemsBeforeEnablingSearch)
            {
                SearchableDropdownWindow.Show(rect, vd.DropdownTitle ?? label, options, current, apply);
                return;
            }

            var menu = new GenericMenu();
            foreach (var o in options)
            {
                var captured = o;
                bool on = InspectorMemberResolver.ValuesEqual(current, o.Value);
                menu.AddItem(new GUIContent(o.Label), on, () => apply(captured.Value));
            }
            menu.DropDown(rect);
        }

        private static string LeafOf(string label)
        {
            int i = label.LastIndexOf('/');
            return i >= 0 ? label.Substring(i + 1) : label;
        }

        private static void ApplyDeferred(SerializedObject so, string path, object value,
            MemberInfo src, FieldInfo field, object target)
        {
            try
            {
                so.Update();
                var p = so.FindProperty(path);
                if (p == null) return;
                FrameworkInspectorRenderer.WriteProperty(p, value);
                so.ApplyModifiedProperties();

                var entry = new InspectorEntry { Property = p, Field = field, AttributeSource = src };
                FrameworkInspectorRenderer.InvokeOnValueChanged(entry, target);
            }
            catch (Exception ex)
            {
                Debug.LogWarning($"[FoundationPlatform.FrameworkInspector] dropdown apply failed: {ex.Message}");
            }
        }

        // ---------------------------------------------------------------- AssetSelector

        public static void DrawAssetSelector(InspectorEntry e, object target, AssetSelectorAttribute asel)
        {
            var prop = e.Property;
            var t = e.Field != null ? e.Field.FieldType : typeof(UnityEngine.Object);
            var lbl = FrameworkInspectorRenderer.GetLabel(e, target) ?? new GUIContent(prop.displayName);

            var rect = EditorGUILayout.GetControlRect();
            const float btnW = 20f;
            var fieldRect = new Rect(rect.x, rect.y, rect.width - btnW - 2, rect.height);
            var btnRect = new Rect(rect.xMax - btnW, rect.y, btnW, rect.height);

            EditorGUI.BeginChangeCheck();
            var obj = EditorGUI.ObjectField(fieldRect, lbl, prop.objectReferenceValue, t, false);
            if (EditorGUI.EndChangeCheck()) { prop.objectReferenceValue = obj; FrameworkInspectorRenderer.Commit(e, target); }

            if (EditorGUI.DropdownButton(btnRect, new GUIContent("▾"), FocusType.Keyboard))
            {
                var options = BuildAssetOptions(t, asel);
                var so = prop.serializedObject;
                string path = prop.propertyPath;
                var src = e.AttributeSource;
                var field = e.Field;
                Action<object> apply = value => ApplyDeferred(so, path, value, src, field, target);
                SearchableDropdownWindow.Show(rect, asel.DropdownTitle ?? lbl.text, options, prop.objectReferenceValue, apply);
            }
        }

        /// <summary>Asset dropdown row for a collection element.</summary>
        public static void DrawAssetSelectorElement(SerializedProperty elemProp, InspectorEntry owner, object target,
            AssetSelectorAttribute asel, Type elemType, string label)
        {
            var rect = EditorGUILayout.GetControlRect();
            const float btnW = 20f;
            var fieldRect = new Rect(rect.x, rect.y, rect.width - btnW - 2, rect.height);
            var btnRect = new Rect(rect.xMax - btnW, rect.y, btnW, rect.height);

            EditorGUI.BeginChangeCheck();
            var obj = EditorGUI.ObjectField(fieldRect, new GUIContent(label), elemProp.objectReferenceValue, elemType, false);
            if (EditorGUI.EndChangeCheck())
            {
                elemProp.objectReferenceValue = obj;
                elemProp.serializedObject.ApplyModifiedProperties();
            }

            if (EditorGUI.DropdownButton(btnRect, new GUIContent("▾"), FocusType.Keyboard))
            {
                var options = BuildAssetOptions(elemType, asel);
                var so = elemProp.serializedObject;
                string path = elemProp.propertyPath;
                Action<object> apply = value => ApplyDeferred(so, path, value, owner.AttributeSource, owner.Field, target);
                SearchableDropdownWindow.Show(rect, asel.DropdownTitle ?? label, options, elemProp.objectReferenceValue, apply);
            }
        }

        internal static List<Option> BuildAssetOptions(Type assetType, AssetSelectorAttribute asel)
        {
            var options = new List<Option>();
            string filter = asel.Filter ?? string.Empty;
            if (!filter.Contains("t:")) filter = $"t:{assetType.Name} {filter}".Trim();
            string[] folders = string.IsNullOrEmpty(asel.Paths)
                ? null
                : asel.Paths.Split(new[] { '|' }, StringSplitOptions.RemoveEmptyEntries);

            string[] guids;
            try { guids = folders != null ? AssetDatabase.FindAssets(filter, folders) : AssetDatabase.FindAssets(filter); }
            catch { guids = Array.Empty<string>(); }

            var seen = new HashSet<UnityEngine.Object>();
            foreach (var guid in guids)
            {
                string path = AssetDatabase.GUIDToAssetPath(guid);
                var asset = AssetDatabase.LoadAssetAtPath(path, assetType);
                if (asset == null || !seen.Add(asset)) continue;
                string label = asel.FlattenTreeView
                    ? asset.name
                    : path.StartsWith("Assets/") ? path.Substring("Assets/".Length) : path;
                options.Add(new Option { Label = label, Value = asset });
            }
            options.Sort((a, b) => string.Compare(a.Label, b.Label, StringComparison.OrdinalIgnoreCase));
            options.Insert(0, new Option { Label = "(none)", Value = null });
            return options;
        }
    }

    /// <summary>
    /// Searchable dropdown list window: search field + scrollable
    /// option list; click (or Enter on the single match) selects. Shown as a dropdown under the control.
    /// </summary>
    internal sealed class SearchableDropdownWindow : EditorWindow
    {
        private List<InspectorDropdown.Option> _options;
        private object _current;
        private Action<object> _onSelect;
        private string _search = string.Empty;
        private Vector2 _scroll;
        private bool _focusPending = true;

        public static void Show(Rect activatorRect, string title, List<InspectorDropdown.Option> options,
            object current, Action<object> onSelect)
        {
            var win = CreateInstance<SearchableDropdownWindow>();
            win.titleContent = new GUIContent(title ?? "Select");
            win._options = options;
            win._current = current;
            win._onSelect = onSelect;
            var screenRect = GUIUtility.GUIToScreenRect(activatorRect);
            float height = Mathf.Clamp(options.Count * 18f + 40f, 90f, 320f);
            win.ShowAsDropDown(screenRect, new Vector2(Mathf.Max(activatorRect.width, 240f), height));
        }

        private void OnGUI()
        {
            GUI.SetNextControlName("dropdown-search");
            _search = EditorGUILayout.TextField(_search, EditorStyles.toolbarSearchField);
            if (_focusPending) { EditorGUI.FocusTextInControl("dropdown-search"); _focusPending = false; }

            var visible = new List<InspectorDropdown.Option>();
            foreach (var o in _options)
                if (string.IsNullOrEmpty(_search) || o.Label.IndexOf(_search, StringComparison.OrdinalIgnoreCase) >= 0)
                    visible.Add(o);

            if (Event.current.type == EventType.KeyDown)
            {
                if (Event.current.keyCode == KeyCode.Escape) { Close(); return; }
                if (Event.current.keyCode == KeyCode.Return && visible.Count == 1)
                {
                    Select(visible[0]);
                    return;
                }
            }

            _scroll = EditorGUILayout.BeginScrollView(_scroll);
            foreach (var o in visible)
            {
                bool isCurrent = InspectorMemberResolver.ValuesEqual(_current, o.Value);
                var style = new GUIStyle(EditorStyles.label) { fontStyle = isCurrent ? FontStyle.Bold : FontStyle.Normal };
                if (GUILayout.Button((isCurrent ? "✓ " : "   ") + o.Label, style))
                {
                    Select(o);
                    EditorGUILayout.EndScrollView();
                    return;
                }
            }
            EditorGUILayout.EndScrollView();
        }

        private void Select(InspectorDropdown.Option o)
        {
            try { _onSelect?.Invoke(o.Value); } catch { }
            Close();
        }
    }
}
#endif
