#if UNITY_EDITOR
using System;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using UnityEditor;
using UnityEditor.IMGUI.Controls;
using UnityEngine;

namespace AetherNexus.FoundationPlatform.AetherInspector.Editor
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
        public static bool DrawValueDropdown(InspectorEntry e, object[] targets, ValueDropdownAttribute vd, string labelText)
        {
            var options = BuildValueOptions(targets[0], vd);
            if (options == null) return false;
            AetherInspectorRenderer.DrawUnityHeaders(e.Metadata);
            DrawDropdownForProperty(e.Property, e, targets, vd, options, labelText ?? e.Property.displayName);
            return true;
        }

        /// <summary>Draw the dropdown row for a collection element (DrawDropdownForListElements).</summary>
        public static bool DrawValueDropdownElement(SerializedProperty elemProp, InspectorEntry owner, object[] targets,
            ValueDropdownAttribute vd, string label)
        {
            var options = BuildValueOptions(targets[0], vd);
            if (options == null) return false;
            DrawDropdownForProperty(elemProp, owner, targets, vd, options, label);
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

        private static void DrawDropdownForProperty(SerializedProperty prop, InspectorEntry owner, object[] targets,
            ValueDropdownAttribute vd, List<Option> options, string label)
        {
            object current = AetherInspectorRenderer.ReadProperty(prop);
            string currentLabel = "(none)";
            foreach (var o in options)
                if (InspectorMemberResolver.ValuesEqual(current, o.Value)) { currentLabel = LeafOf(o.Label); break; }
            if (currentLabel == "(none)" && current != null) currentLabel = current.ToString();

            var rect = EditorGUILayout.GetControlRect();
            rect = EditorGUI.PrefixLabel(rect, AetherInspectorTheme.TempContent(label));
            if (!AetherInspectorTheme.DrawStyledDropdown(rect, AetherInspectorTheme.TempContent(label), currentLabel)) return;

            var so = prop.serializedObject;
            string path = prop.propertyPath;
            var src = owner.AttributeSource;
            var field = owner.Field;

            Action<object> apply = value => ApplyDeferred(so, path, value, src, field, targets);

            if (options.Count >= vd.NumberOfItemsBeforeEnablingSearch)
            {
                InspectorOptionDropdown.Show(rect, vd.DropdownTitle ?? label, options, apply);
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
            MemberInfo src, FieldInfo field, object[] targets)
        {
            try
            {
                so.Update();
                var p = so.FindProperty(path);
                if (p == null) return;
                AetherInspectorRenderer.WriteProperty(p, value);
                so.ApplyModifiedProperties();

                var entry = new InspectorEntry { Property = p, Field = field, AttributeSource = src };
                AetherInspectorRenderer.InvokeOnValueChanged(entry, targets);
            }
            catch (Exception ex)
            {
                Debug.LogWarning($"[FoundationPlatform.AetherInspector] dropdown apply failed: {ex.Message}");
            }
        }

        // ---------------------------------------------------------------- AssetSelector

        public static void DrawAssetSelector(InspectorEntry e, object[] targets, AssetSelectorAttribute asel)
        {
            var prop = e.Property;
            var t = e.Field != null ? e.Field.FieldType : typeof(UnityEngine.Object);
            var lbl = AetherInspectorRenderer.GetLabel(e, targets) ?? new GUIContent(prop.displayName);

            var rect = EditorGUILayout.GetControlRect();
            const float btnW = 20f;
            var fieldRect = new Rect(rect.x, rect.y, rect.width - btnW - 2, rect.height);
            var btnRect = new Rect(rect.xMax - btnW, rect.y, btnW, rect.height);

            EditorGUI.BeginChangeCheck();
            var obj = EditorGUI.ObjectField(fieldRect, lbl, prop.objectReferenceValue, t, false);
            if (EditorGUI.EndChangeCheck()) { prop.objectReferenceValue = obj; AetherInspectorRenderer.Commit(e, targets); }

            AetherInspectorTheme.DrawFieldChrome(btnRect);
            AetherInspectorTheme.DrawDropdownCaret(btnRect);
            if (EditorGUI.DropdownButton(btnRect, GUIContent.none, FocusType.Keyboard, GUIStyle.none))
            {
                var options = BuildAssetOptions(t, asel);
                var so = prop.serializedObject;
                string path = prop.propertyPath;
                var src = e.AttributeSource;
                var field = e.Field;
                Action<object> apply = value => ApplyDeferred(so, path, value, src, field, targets);
                InspectorOptionDropdown.Show(rect, asel.DropdownTitle ?? lbl.text, options, apply);
            }
        }

        /// <summary>Asset dropdown row for a collection element.</summary>
        public static void DrawAssetSelectorElement(SerializedProperty elemProp, InspectorEntry owner, object[] targets,
            AssetSelectorAttribute asel, Type elemType, string label)
        {
            var rect = EditorGUILayout.GetControlRect();
            const float btnW = 20f;
            var fieldRect = new Rect(rect.x, rect.y, rect.width - btnW - 2, rect.height);
            var btnRect = new Rect(rect.xMax - btnW, rect.y, btnW, rect.height);

            EditorGUI.BeginChangeCheck();
            var obj = EditorGUI.ObjectField(fieldRect, AetherInspectorTheme.TempContent(label), elemProp.objectReferenceValue, elemType, false);
            if (EditorGUI.EndChangeCheck())
            {
                elemProp.objectReferenceValue = obj;
                elemProp.serializedObject.ApplyModifiedProperties();
            }

            AetherInspectorTheme.DrawFieldChrome(btnRect);
            AetherInspectorTheme.DrawDropdownCaret(btnRect);
            if (EditorGUI.DropdownButton(btnRect, GUIContent.none, FocusType.Keyboard, GUIStyle.none))
            {
                var options = BuildAssetOptions(elemType, asel);
                var so = elemProp.serializedObject;
                string path = elemProp.propertyPath;
                Action<object> apply = value => ApplyDeferred(so, path, value, owner.AttributeSource, owner.Field, targets);
                InspectorOptionDropdown.Show(rect, asel.DropdownTitle ?? label, options, apply);
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
    /// Searchable option dropdown built on Unity's <see cref="AdvancedDropdown"/>, so a
    /// <c>[ValueDropdown]</c> or <c>[AssetSelector]</c> field reads like Add Component or a layer
    /// picker: same search field, keyboard model, breadcrumb navigation and skin. Nested "a/b"
    /// labels become child pages.
    /// </summary>
    internal sealed class InspectorOptionDropdown : AdvancedDropdown
    {
        private sealed class OptionItem : AdvancedDropdownItem
        {
            public readonly InspectorDropdown.Option Option;

            public OptionItem(string label, InspectorDropdown.Option option) : base(label)
            {
                Option = option;
            }
        }

        private readonly string _title;
        private readonly List<InspectorDropdown.Option> _options;
        private readonly Action<object> _onSelect;

        private InspectorOptionDropdown(string title, List<InspectorDropdown.Option> options,
            Action<object> onSelect, float activatorWidth)
            : base(new AdvancedDropdownState())
        {
            _title = title;
            _options = options;
            _onSelect = onSelect;
            minimumSize = new Vector2(
                Mathf.Max(activatorWidth, 240f),
                Mathf.Clamp(options.Count * 18f + 60f, 140f, 340f));
        }

        public static void Show(Rect activatorRect, string title, List<InspectorDropdown.Option> options,
            Action<object> onSelect)
        {
            new InspectorOptionDropdown(title, options, onSelect, activatorRect.width).Show(activatorRect);
        }

        protected override AdvancedDropdownItem BuildRoot()
        {
            var root = new AdvancedDropdownItem(_title ?? "Select");
            var groups = new Dictionary<string, AdvancedDropdownItem>(StringComparer.Ordinal);

            foreach (var option in _options)
            {
                string[] segments = option.Label.Split('/');
                AdvancedDropdownItem parent = root;
                string walked = null;

                for (int i = 0; i < segments.Length - 1; i++)
                {
                    walked = walked == null ? segments[i] : walked + "/" + segments[i];
                    if (!groups.TryGetValue(walked, out AdvancedDropdownItem group))
                    {
                        group = new AdvancedDropdownItem(segments[i]);
                        groups[walked] = group;
                        parent.AddChild(group);
                    }
                    parent = group;
                }

                parent.AddChild(new OptionItem(segments[segments.Length - 1], option));
            }

            return root;
        }

        protected override void ItemSelected(AdvancedDropdownItem item)
        {
            if (item is OptionItem optionItem)
                _onSelect?.Invoke(optionItem.Option.Value);
        }
    }
}
#endif
