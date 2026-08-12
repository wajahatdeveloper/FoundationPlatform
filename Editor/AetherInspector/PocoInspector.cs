#if UNITY_EDITOR
using System;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using AetherNexus.FoundationPlatform.AetherInspector;
using AetherNexus.FoundationPlatform.Attributes;
using UnityEditor;
using UnityEngine;

namespace AetherNexus.FoundationPlatform.AetherInspector.Editor
{
    /// <summary>
    /// Reflection inspector for a plain C# object (not a <see cref="UnityEngine.Object"/>) — the
    /// property-tree renderer for POCO "host" objects (e.g. the Central Authoring
    /// detail-pane hosts). Renders <c>[ShowInInspector]</c> members, public fields, <c>[Button]</c>
    /// methods; honors <c>[LabelText]/[HideLabel]/[Title]/[InfoBox]/[DisplayAsString]/[ReadOnly]/</c>
    /// <c>[PropertyOrder]/[PropertySpace]/[ShowIf]/[HideIf]</c> and Box/Foldout groups. Editable for
    /// common field types; complex values render read-only.
    /// </summary>
    public static class PocoInspector
    {
        private const BindingFlags Flags =
            BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic;

        public static void Draw(object target)
        {
            if (target == null)
            {
                EditorGUILayout.LabelField("(nothing selected)", EditorStyles.centeredGreyMiniLabel);
                return;
            }

            DrawObject(target, 0, null);
        }

        /// <summary>
        /// Draw one reflected member (field/property/[Button] method) through the POCO pipeline —
        /// used by <see cref="AetherInspectorRenderer"/> for <c>[ShowInInspector]</c> members so
        /// writable values are editable and complex values recurse (property-tree behavior).
        /// </summary>
        public static void DrawSingleMember(object target, MemberInfo mi)
            => DrawSingleMember(target, mi, null, null);

        /// <summary>Draws the member with no visited-set tracking.</summary>
        internal static void DrawSingleMember(object target, MemberInfo mi, MemberMetadata metadata)
            => DrawSingleMember(target, mi, metadata, null);

        internal static void DrawSingleMember(object target, MemberInfo mi, MemberMetadata metadata, HashSet<object> visited)
        {
            var m = MakeMember(mi, 0);
            try
            {
                // Value only — the engine's entry pipeline already drew titles/info boxes/validation.
                if (mi is MethodInfo method)
                {
                    var btn = mi.GetCustomAttribute<ButtonAttribute>();
                    if (btn != null) { m.IsButton = true; m.Button = btn; m.Method = method; DrawButton(m, target); return; }
                }
                DrawValue(m, target, 0, metadata, visited);
            }
            catch (Exception ex)
            {
                AetherInspectorTheme.DrawInfoBox($"{mi.Name}: {ex.InnerException?.Message ?? ex.Message}", InfoMessageType.Error);
            }
        }

        /// <summary>Editable field for common types (used for [Button] parameter fields too).</summary>
        public static object DrawTypedFieldPublic(GUIContent label, Type t, object value)
            => DrawTypedField(label, t, value);

        private static readonly Dictionary<string, bool> s_nestedFoldouts = new Dictionary<string, bool>();

        private sealed class Slot
        {
            public Member Inline;                 // ungrouped single member (null for group slots)
            public string GroupPath;
            public PocoGroupKind Group;
            public bool GroupExpandedDefault;
            public List<Member> Members;          // group members in declaration order
        }

        private static void DrawObject(object target, int depth, HashSet<object> visited = null)
        {
            if (visited != null)
            {
                if (visited.Contains(target)) return;
                visited.Add(target);
            }

            Type type = target.GetType();
            var members = CollectMembers(type);

            // Bucket members into ordered slots: a group renders once at its first member's
            // position with ALL same-path members together. This is robust to
            // hidden members and non-contiguous ordering, and keeps GUILayout begin/end balanced.
            var slots = new List<Slot>();
            var groupIndex = new Dictionary<string, Slot>();
            foreach (var m in members)
            {
                if (string.IsNullOrEmpty(m.GroupPath))
                {
                    slots.Add(new Slot { Inline = m });
                    continue;
                }
                if (!groupIndex.TryGetValue(m.GroupPath, out var s))
                {
                    s = new Slot
                    {
                        GroupPath = m.GroupPath,
                        Group = m.Group,
                        GroupExpandedDefault = m.GroupExpandedDefault,
                        Members = new List<Member>(),
                    };
                    groupIndex[m.GroupPath] = s;
                    slots.Add(s);
                }
                s.Members.Add(m);
            }

            foreach (var s in slots)
            {
                if (s.Inline != null)
                {
                    if (IsVisible(s.Inline.Info, target)) DrawMemberSafe(s.Inline, target, depth, visited);
                    continue;
                }

                bool anyVisible = false;
                for (int i = 0; i < s.Members.Count; i++)
                    if (IsVisible(s.Members[i].Info, target)) { anyVisible = true; break; }
                if (!anyVisible) continue; // no visible members → skip header entirely

                string title = LastSegment(s.GroupPath);
                bool expanded = true;
                if (s.Group == PocoGroupKind.Foldout)
                {
                    string key = "grp:" + s.GroupPath;
                    if (!s_nestedFoldouts.TryGetValue(key, out expanded)) expanded = s.GroupExpandedDefault;
                    expanded = AetherInspectorTheme.SectionFoldout(expanded, title);
                    s_nestedFoldouts[key] = expanded;
                    if (expanded)
                    {
                        AetherInspectorTheme.BeginSectionFoldoutBody();
                        for (int i = 0; i < s.Members.Count; i++)
                            if (IsVisible(s.Members[i].Info, target)) DrawMemberSafe(s.Members[i], target, depth, visited);
                        AetherInspectorTheme.EndSectionFoldoutBody();
                    }
                    continue;
                }

                AetherInspectorTheme.BeginSection();
                EditorGUILayout.LabelField(title, AetherInspectorTheme.SectionTitle);

                for (int i = 0; i < s.Members.Count; i++)
                    if (IsVisible(s.Members[i].Info, target)) DrawMemberSafe(s.Members[i], target, depth, visited);
                AetherInspectorTheme.EndSection();
            }

            if (visited != null)
            {
                visited.Remove(target);
            }
        }

        private static void DrawMemberSafe(Member m, object target, int depth, HashSet<object> visited = null)
        {
            try { DrawMember(m, target, depth, visited); }
            catch (Exception ex)
            {
                AetherInspectorTheme.DrawValidationBox($"{m.Info.Name}: {ex.InnerException?.Message ?? ex.Message}");
            }
        }

        // ---------------------------------------------------------------- member model

        private sealed class Member
        {
            public MemberInfo Info;
            public FieldInfo Field;
            public PropertyInfo Property;
            public MethodInfo Method;
            public bool IsButton;
            public bool IsInspectorGui;
            public ButtonAttribute Button;
            public float Order;
            public int Sequence;
            public string GroupPath;
            public PocoGroupKind Group;
            public bool GroupExpandedDefault;
        }

        private enum PocoGroupKind { None, Box, Foldout, Title }

        private static List<Member> CollectMembers(Type type)
        {
            var list = new List<Member>();
            int seq = 0;

            foreach (var f in AetherInspectorRenderer.AllFields(type))
            {
                if (f.IsStatic) continue;
                if (f.GetCustomAttribute<HideInInspector>() != null) continue;
                // A field draws only if it is serializable (public / [SerializeField] and
                // NOT [NonSerialized]) or explicitly [ShowInInspector]. This prevents a [NonSerialized]
                // public field that also has a [ShowInInspector] display property from rendering twice.
                bool shown = f.GetCustomAttribute<ShowInInspectorAttribute>() != null;
                bool serialized = (f.IsPublic || f.GetCustomAttribute<SerializeField>() != null)
                    && f.GetCustomAttribute<System.NonSerializedAttribute>() == null;
                if (!serialized && !shown) continue;
                list.Add(MakeMember(f, seq++));
            }
            foreach (var p in AetherInspectorRenderer.AllProperties(type))
            {
                if (p.GetCustomAttribute<ShowInInspectorAttribute>() == null) continue;
                list.Add(MakeMember(p, seq++));
            }
            foreach (var mi in AetherInspectorRenderer.AllMethods(type))
            {
                if (mi.GetParameters().Length != 0) continue;
                var btn = mi.GetCustomAttribute<ButtonAttribute>();
                if (btn != null)
                {
                    var m = MakeMember(mi, seq++);
                    m.IsButton = true;
                    m.Button = btn;
                    list.Add(m);
                    continue;
                }
                if (mi.GetCustomAttribute<OnInspectorGUIAttribute>() != null)
                {
                    var m = MakeMember(mi, seq++);
                    m.IsInspectorGui = true;
                    list.Add(m);
                }
            }

            list.Sort((a, b) => a.Order != b.Order ? a.Order.CompareTo(b.Order) : a.Sequence.CompareTo(b.Sequence));
            return list;
        }

        private static Member MakeMember(MemberInfo mi, int seq)
        {
            var m = new Member { Info = mi, Sequence = seq };
            m.Field = mi as FieldInfo;
            m.Property = mi as PropertyInfo;
            m.Method = mi as MethodInfo;
            var order = mi.GetCustomAttribute<PropertyOrderAttribute>();
            if (order != null) m.Order = order.Order;

            foreach (var attr in mi.GetCustomAttributes())
            {
                switch (attr)
                {
                    case BoxGroupAttribute b: m.GroupPath = b.GroupID; m.Group = PocoGroupKind.Box; m.GroupExpandedDefault = true; break;
                    case FoldoutGroupAttribute f:
                        m.GroupPath = f.GroupID; m.Group = PocoGroupKind.Foldout;
                        m.GroupExpandedDefault = !f.HasDefinedExpanded || f.Expanded;
                        break;
                    case TitleGroupAttribute t: m.GroupPath = t.GroupID; m.Group = PocoGroupKind.Title; m.GroupExpandedDefault = true; break;
                }
            }
            return m;
        }

        // ---------------------------------------------------------------- draw

        private static void DrawMember(Member m, object target, int depth, HashSet<object> visited = null)
        {
            var src = m.Info;

            foreach (var info in src.GetCustomAttributes<InfoBoxAttribute>())
            {
                if (!string.IsNullOrEmpty(info.VisibleIf) &&
                    !InspectorMemberResolver.EvaluateBool(target, info.VisibleIf, null, false, true))
                    continue;
                AetherInspectorTheme.DrawInfoBox(ResolveText(target, info.Message), info.InfoMessageType);
            }

            var title = src.GetCustomAttribute<TitleAttribute>();
            if (title != null)
            {
                EditorGUILayout.Space(AetherInspectorTheme.SectionSpacing * 0.5f);
                var align = title.TitleAlignment == TitleAlignments.Centered ? TextAlignment.Center
                    : title.TitleAlignment == TitleAlignments.Right ? TextAlignment.Right : TextAlignment.Left;
                AetherInspectorTheme.DrawTitle(title.Title, title.Subtitle, align, title.HorizontalLine, title.Bold);
            }

            var space = src.GetCustomAttribute<PropertySpaceAttribute>();
            if (space != null && space.SpaceBefore > 0) EditorGUILayout.Space(space.SpaceBefore);

            if (m.IsInspectorGui)
            {
                // [OnInspectorGUI] method — invoke to draw its own custom IMGUI.
                try { m.Method.Invoke(m.Method.IsStatic ? null : target, null); }
                catch (Exception ex) { AetherInspectorTheme.DrawInfoBox($"[OnInspectorGUI] {m.Method.Name}: {ex.InnerException?.Message ?? ex.Message}", InfoMessageType.Error); }
                if (space != null && space.SpaceAfter > 0) EditorGUILayout.Space(space.SpaceAfter);
                return;
            }

            bool enabled = IsEnabled(src, target);
            using (new EditorGUI.DisabledScope(!enabled))
            {
                if (m.IsButton) DrawButton(m, target);
                else DrawValue(m, target, depth, metadata: null, visited: visited);
            }

            if (space != null && space.SpaceAfter > 0) EditorGUILayout.Space(space.SpaceAfter);
        }

        private static void DrawButton(Member m, object target)
        {
            string label = m.Button.Name;
            if (string.IsNullOrEmpty(label)) label = ObjectNames.NicifyVariableName(m.Method.Name);
            else label = InspectorMemberResolver.ResolveString(target, label);
            float h = AetherInspectorRenderer.ButtonHeight(m.Button);
            var content = new GUIContent(label);
            if (!string.IsNullOrEmpty(m.Button.Icon))
            {
                var ic = EditorGUIUtility.IconContent(m.Button.Icon);
                if (ic != null && ic.image != null) content.image = ic.image;
            }
            var pseudo = new ButtonAttribute { ButtonAlignment = ButtonAlignment.Stretch, Style = m.Button.Style, IconAlignment = m.Button.IconAlignment };
            if (AetherInspectorRenderer.DrawAlignedButtonPublic(pseudo, content, h) && m.Method.GetParameters().Length == 0)
                m.Method.Invoke(m.Method.IsStatic ? null : target, null);
        }

        private static void DrawValue(Member m, object target, int depth, MemberMetadata metadata = null, HashSet<object> visited = null)
        {
            // [TooltipIcon(text)] wraps the member's normal rendering with a trailing help-icon, so it
            // composes with whatever DrawValueCore below chooses (editable field, read-only text, nested
            // object, …) instead of needing its own dedicated branch.
            var tooltipIcon = m.Info.GetCustomAttribute<TooltipIconAttribute>();
            if (tooltipIcon != null)
            {
                AetherInspectorTheme.DrawWithTooltipIcon(
                    () => DrawValueCore(m, target, depth, metadata, visited),
                    tooltipIcon.Tooltip);
                return;
            }
            DrawValueCore(m, target, depth, metadata, visited);
        }

        private static void DrawValueCore(Member m, object target, int depth, MemberMetadata metadata, HashSet<object> visited)
        {
            string label = ResolveText(target, GetLabel(m.Info));
            bool hideLabel = m.Info.GetCustomAttribute<HideLabelAttribute>() != null;
            bool displayString = m.Info.GetCustomAttribute<DisplayAsStringAttribute>() != null;
            bool readOnly = m.Info.GetCustomAttribute<ReadOnlyAttribute>() != null
                || m.Property != null && !m.Property.CanWrite;

            object value = ReadValue(m, target);

            var declaredType = m.Field != null ? m.Field.FieldType : m.Property?.PropertyType;
            var dictSettings = metadata?.DictionaryDrawerSettings ?? m.Info.GetCustomAttribute<DictionaryDrawerSettingsAttribute>();
            if (EngineDictionaryDrawer.IsDictionaryType(declaredType) || value is IDictionary)
            {
                var lbl = hideLabel ? GUIContent.none : new GUIContent(label);
                string foldKey = (m.Info.DeclaringType?.FullName ?? "") + "." + m.Info.Name;
                EngineDictionaryDrawer.Draw(value, dictSettings, lbl, readOnly, foldKey);
                return;
            }

            // Any enumerable (List/array/IReadOnlyList/HashSet/...) except string → grid or bullet list.
            if (value is System.Collections.IEnumerable seq && !(value is string))
            {
                var items = new List<object>();
                foreach (var o in seq) items.Add(o);
                var et = GetListElementType(declaredType) ?? (items.Count > 0 ? items[0]?.GetType() : null);
                if (!hideLabel && !string.IsNullOrEmpty(label))
                    EditorGUILayout.LabelField($"{label} ({items.Count})", AetherInspectorTheme.SectionTitle);
                if (et != null && IsComplexElement(et))
                {
                    TableRenderer.DrawValueTable(items, et, null);
                }
                else
                {
                    using (new AetherInspectorTheme.NestedIndentScope())
                    {
                        if (items.Count == 0) EditorGUILayout.LabelField("(empty)", EditorStyles.miniLabel);
                        else for (int i = 0; i < items.Count; i++) EditorGUILayout.LabelField("• " + (items[i]?.ToString() ?? "null"));
                    }
                }
                return;
            }

            // Nested complex object (not a Unity type / list / primitive) → recurse under a foldout.
            int maxDepth = InspectorXSettings.instance.maxNestedDepth;
            if (value != null && depth < maxDepth && !(value is System.Collections.IEnumerable) && IsComplexElement(value.GetType()))
            {
                if (visited != null && visited.Contains(value)) return;
                string key = (m.Info.DeclaringType?.FullName ?? "") + "." + m.Info.Name;
                if (!s_nestedFoldouts.TryGetValue(key, out bool exp)) exp = true;
                exp = AetherInspectorTheme.SectionFoldout(exp, string.IsNullOrEmpty(label) ? m.Info.Name : label);
                s_nestedFoldouts[key] = exp;
                if (exp)
                {
                    using (new AetherInspectorTheme.NestedIndentScope())
                    {
                        if (visited != null) { visited.Add(value); }
                        try
                        {
                            DrawObject(value, depth + 1, visited);
                        }
                        finally
                        {
                            if (visited != null) { visited.Remove(value); }
                        }
                    }
                }
                return;
            }

            if (displayString || readOnly || (m.Field == null && m.Property == null))
            {
                // Read-only display.
                if (hideLabel) EditorGUILayout.LabelField(value?.ToString() ?? string.Empty);
                else EditorGUILayout.LabelField(label, value?.ToString() ?? string.Empty);
                return;
            }

            // [Layer]/[Tag] on a string or int member → popup from Unity's built-in layer/tag list
            // (mirrors the retired classic LayerDrawer/TagDrawer/StringArrayPopupDrawer; SerializedProperty
            // fields get the equivalent treatment via AetherInspectorEditor's TryDrawLayerOrTagPopup).
            string[] layerOrTagOptions = m.Info.GetCustomAttribute<LayerAttribute>() != null
                ? UnityEditorInternal.InternalEditorUtility.layers
                : m.Info.GetCustomAttribute<TagAttribute>() != null
                    ? UnityEditorInternal.InternalEditorUtility.tags
                    : null;
            if (layerOrTagOptions != null && layerOrTagOptions.Length > 0
                && (declaredType == typeof(string) || declaredType == typeof(int)))
            {
                var popupLabel = hideLabel ? GUIContent.none : new GUIContent(label);
                EditorGUI.BeginChangeCheck();
                object popupEdited;
                if (declaredType == typeof(string))
                {
                    int index = Array.IndexOf(layerOrTagOptions, value as string);
                    index = Mathf.Clamp(index, 0, layerOrTagOptions.Length - 1);
                    int picked = EditorGUILayout.Popup(popupLabel.text, index, layerOrTagOptions);
                    popupEdited = layerOrTagOptions[picked];
                }
                else
                {
                    int index = Mathf.Clamp(value is int iv ? iv : 0, 0, layerOrTagOptions.Length - 1);
                    popupEdited = EditorGUILayout.Popup(popupLabel.text, index, layerOrTagOptions);
                }
                if (EditorGUI.EndChangeCheck())
                {
                    try
                    {
                        if (m.Field != null) m.Field.SetValue(target, popupEdited);
                        else m.Property.SetValue(target, popupEdited);
                    }
                    catch { }
                    if (target is UnityEngine.Object uo1) EditorUtility.SetDirty(uo1);
                    InvokeOnValueChanged(m.Info, target);
                }
                return;
            }

            // Editable field for common types (fields and writable properties).
            var valueType = m.Field != null ? m.Field.FieldType : m.Property.PropertyType;
            EditorGUI.BeginChangeCheck();
            object edited = DrawTypedField(hideLabel ? GUIContent.none : new GUIContent(label), valueType, value);
            if (EditorGUI.EndChangeCheck())
            {
                try
                {
                    if (m.Field != null) m.Field.SetValue(target, edited);
                    else m.Property.SetValue(target, edited);
                }
                catch { }
                if (target is UnityEngine.Object uo) EditorUtility.SetDirty(uo);
                InvokeOnValueChanged(m.Info, target);
            }
        }

        private static object DrawTypedField(GUIContent label, Type t, object value)
        {
            if (t == typeof(string)) return EditorGUILayout.TextField(label, (string)(value ?? string.Empty));
            if (t == typeof(int)) return EditorGUILayout.IntField(label, value is int i ? i : 0);
            if (t == typeof(float)) return EditorGUILayout.FloatField(label, value is float f ? f : 0f);
            if (t == typeof(bool)) return EditorGUILayout.Toggle(label, value is bool b && b);
            if (t.IsEnum) return EditorGUILayout.EnumPopup(label, (Enum)(value ?? Activator.CreateInstance(t)));
            if (t == typeof(Vector2)) return EditorGUILayout.Vector2Field(label, value is Vector2 v2 ? v2 : Vector2.zero);
            if (t == typeof(Vector3)) return EditorGUILayout.Vector3Field(label, value is Vector3 v3 ? v3 : Vector3.zero);
            if (t == typeof(Color)) return EditorGUILayout.ColorField(label, value is Color c ? c : Color.white);
            if (typeof(UnityEngine.Object).IsAssignableFrom(t))
                return EditorGUILayout.ObjectField(label, value as UnityEngine.Object, t, true);

            // Fallback: read-only string. Use the (GUIContent, GUIContent) overload — passing a raw
            // string as the 2nd arg binds to (GUIContent, GUIStyle) and treats it as a style name.
            using (new EditorGUI.DisabledScope(true))
                EditorGUILayout.LabelField(label, new GUIContent(value?.ToString() ?? "null"));
            return value;
        }

        // ---------------------------------------------------------------- helpers

        private static bool IsComplexElement(Type t)
        {
            if (t == null) return false;
            if (t.IsPrimitive || t.IsEnum) return false;
            if (t == typeof(string) || t == typeof(decimal)) return false;
            if (typeof(UnityEngine.Object).IsAssignableFrom(t)) return false;
            return t.IsClass || (t.IsValueType && !t.IsPrimitive); // class or struct
        }

        private static Type GetListElementType(Type t)
        {
            if (t == null) return null;
            if (t.IsArray) return t.GetElementType();
            if (t.IsGenericType && t.GetGenericTypeDefinition() == typeof(List<>))
                return t.GetGenericArguments()[0];
            return null;
        }

        private static object ReadValue(Member m, object target)
        {
            try
            {
                if (m.Field != null) return m.Field.GetValue(target);
                if (m.Property != null && m.Property.CanRead) return m.Property.GetValue(target);
            }
            catch { }
            return null;
        }

        private static bool IsVisible(MemberInfo m, object target)
        {
            if (m.GetCustomAttribute<HideInEditorModeAttribute>() != null && !Application.isPlaying) return false;
            if (m.GetCustomAttribute<HideInPlayModeAttribute>() != null && Application.isPlaying) return false;
            foreach (var s in m.GetCustomAttributes<ShowIfAttribute>())
                if (!InspectorMemberResolver.EvaluateBool(target, s.Condition, s.Value, s.HasValue, true)) return false;
            foreach (var h in m.GetCustomAttributes<HideIfAttribute>())
                if (InspectorMemberResolver.EvaluateBool(target, h.Condition, h.Value, h.HasValue, false)) return false;
            return true;
        }

        private static bool IsEnabled(MemberInfo m, object target)
        {
            if (m.GetCustomAttribute<ReadOnlyAttribute>() != null) return false;
            foreach (var en in m.GetCustomAttributes<EnableIfAttribute>())
                if (!InspectorMemberResolver.EvaluateBool(target, en.Condition, en.Value, en.HasValue, true)) return false;
            foreach (var di in m.GetCustomAttributes<DisableIfAttribute>())
                if (InspectorMemberResolver.EvaluateBool(target, di.Condition, di.Value, di.HasValue, false)) return false;
            return true;
        }

        private static void InvokeOnValueChanged(MemberInfo m, object target)
        {
            var attr = m.GetCustomAttribute<OnValueChangedAttribute>();
            if (attr == null) return;
            var mi = InspectorMemberResolver.FindMethod(target.GetType(), attr.Action, Type.EmptyTypes);
            try { mi?.Invoke(mi.IsStatic ? null : target, null); } catch { }
        }

        private static string GetLabel(MemberInfo m)
        {
            var lt = m.GetCustomAttribute<LabelTextAttribute>();
            return lt != null ? lt.Text : ObjectNames.NicifyVariableName(m.Name);
        }

        private static string ResolveText(object target, string message)
            => InspectorMemberResolver.ResolveString(target, message);

        private static string LastSegment(string path)
        {
            int i = path.LastIndexOf('/');
            return i >= 0 ? path.Substring(i + 1) : path;
        }

        private static MessageType ToMsgType(InfoMessageType t) => t switch
        {
            InfoMessageType.Warning => MessageType.Warning,
            InfoMessageType.Error => MessageType.Error,
            InfoMessageType.Info => MessageType.Info,
            _ => MessageType.None,
        };
    }
}
#endif
