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
    /// Reusable property drawer for a nested <c>[Serializable]</c> type that carries engine-only members
    /// (<c>[ShowInInspector]</c> computed properties/fields, <c>[Button]</c> methods) and/or per-field
    /// conditionals (<c>[ShowIf]/[HideIf]/[ReadOnly]/...</c>) which Unity's default drawer cannot honor —
    /// the gap Unity's default drawer leaves when recursing into serialized graphs (e.g. list
    /// elements). It draws a foldout, then the serialized child fields filtered/gated by their attributes,
    /// then the reflected engine-only members read from the property's boxed instance.
    ///
    /// Register per type with a 3-line subclass — Unity applies it to fields AND list/array elements:
    /// <code>[CustomPropertyDrawer(typeof(MySpec))] sealed class MySpecDrawer : AetherInspectorReflectedDrawer { }</code>
    /// </summary>
    public abstract class AetherInspectorReflectedDrawer : PropertyDrawer
    {
        private const BindingFlags Flags = BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic;
        private const float Pad = 2f;

        private static readonly Dictionary<Type, List<MemberInfo>> s_extraCache = new Dictionary<Type, List<MemberInfo>>();

        private static float Line => EditorGUIUtility.singleLineHeight;

        // [HideLabel] on the host field arrives as GUIContent.none: draw children flush, no foldout header.
        private static bool IsHeaderless(GUIContent label) => label == null || label == GUIContent.none;

        public override float GetPropertyHeight(SerializedProperty property, GUIContent label)
        {
            object inst = SafeBoxed(property);
            if (inst == null) return EditorGUI.GetPropertyHeight(property, label, true); // fallback (multi-edit/unresolved)

            bool headerless = IsHeaderless(label);
            float h = headerless ? 0f : Line; // foldout header (skipped when headerless)
            if (!headerless && !property.isExpanded) return h;

            Type t = inst.GetType();
            foreach (var child in Children(property))
            {
                var f = FindField(t, child.name);
                if (f != null && !IsFieldVisible(f, inst)) continue;
                h += Pad + EditorGUI.GetPropertyHeight(child, true);
            }
            foreach (var m in ExtraMembers(t))
                h += Pad + ExtraHeight(m, inst);
            return h;
        }

        public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
        {
            object inst = SafeBoxed(property);
            if (inst == null) { EditorGUI.PropertyField(position, property, label, true); return; }

            bool headerless = IsHeaderless(label);
            float y = position.y;
            int indent = EditorGUI.indentLevel;

            if (!headerless)
            {
                var header = new Rect(position.x, position.y, position.width, Line);
                property.isExpanded = EditorGUI.Foldout(header, property.isExpanded, ResolveElementLabel(inst, label), true, AetherInspectorTheme.FlatFoldoutStyle);
                if (!property.isExpanded) return;
                y = header.yMax;
                EditorGUI.indentLevel = indent + 1; // children indented under the header
            }

            Type t = inst.GetType();

            foreach (var child in Children(property))
            {
                var f = FindField(t, child.name);
                if (f != null && !IsFieldVisible(f, inst)) continue;
                float ch = EditorGUI.GetPropertyHeight(child, true);
                bool enabled = f == null || IsFieldEnabled(f, inst);
                using (new EditorGUI.DisabledScope(!enabled))
                    EditorGUI.PropertyField(new Rect(position.x, y + Pad, position.width, ch), child, true);
                y += Pad + ch;
            }

            foreach (var m in ExtraMembers(t))
            {
                float mh = ExtraHeight(m, inst);
                DrawExtra(new Rect(position.x, y + Pad, position.width, mh), m, inst);
                y += Pad + mh;
            }

            EditorGUI.indentLevel = indent;
        }

        // ---- serialized children -----------------------------------------------

        private static IEnumerable<SerializedProperty> Children(SerializedProperty property)
        {
            var it = property.Copy();
            var end = it.GetEndProperty();
            if (!it.NextVisible(true)) yield break;
            do
            {
                if (SerializedProperty.EqualContents(it, end)) yield break;
                yield return it.Copy();
            } while (it.NextVisible(false));
        }

        // ---- reflected engine-only members --------------------------------------

        private static List<MemberInfo> ExtraMembers(Type type)
        {
            if (s_extraCache.TryGetValue(type, out var cached)) return cached;
            var list = new List<MemberInfo>();
            foreach (var p in type.GetProperties(Flags))
                if (p.GetCustomAttribute<ShowInInspectorAttribute>() != null) list.Add(p);
            foreach (var f in type.GetFields(Flags))
                if (!f.IsStatic && f.GetCustomAttribute<ShowInInspectorAttribute>() != null
                    && f.GetCustomAttribute<HideInInspector>() == null && f.GetCustomAttribute<SerializeField>() == null)
                    list.Add(f);
            foreach (var mi in type.GetMethods(Flags))
                if (mi.GetParameters().Length == 0 && mi.GetCustomAttribute<ButtonAttribute>() != null) list.Add(mi);
            s_extraCache[type] = list;
            return list;
        }

        private static float ExtraHeight(MemberInfo m, object inst)
        {
            if (m is MethodInfo) return Line + 2f;
            object v = Read(m, inst);
            if (v is IEnumerable en && !(v is string))
                return Line + Math.Max(1, Count(en)) * Line; // header + rows (or one "(empty)")
            return Line;
        }

        private void DrawExtra(Rect r, MemberInfo m, object inst)
        {
            if (m is MethodInfo mi)
            {
                if (GUI.Button(new Rect(r.x, r.y, r.width, Line), NiceName(mi, mi.Name)))
                    try { mi.Invoke(mi.IsStatic ? null : inst, null); } catch { }
                return;
            }

            var label = new GUIContent(NiceName(m, m.Name));
            object v = Read(m, inst);

            if (v is IEnumerable en && !(v is string))
            {
                int n = Count(en);
                EditorGUI.LabelField(new Rect(r.x, r.y, r.width, Line), label, new GUIContent(n == 0 ? "(empty)" : $"({n})"));
                float y = r.y + Line;
                using (new EditorGUI.DisabledScope(true))
                {
                    EditorGUI.indentLevel++;
                    foreach (var item in en)
                    {
                        EditorGUI.LabelField(new Rect(r.x, y, r.width, Line), "• " + (item?.ToString() ?? "null"));
                        y += Line;
                    }
                    EditorGUI.indentLevel--;
                }
                return;
            }

            using (new EditorGUI.DisabledScope(true))
                EditorGUI.LabelField(new Rect(r.x, r.y, r.width, Line), label, new GUIContent(v?.ToString() ?? string.Empty));
        }

        // ---- attribute-driven field state ---------------------------------------

        private static bool IsFieldVisible(FieldInfo f, object inst)
        {
            if (f.GetCustomAttribute<HideInInspector>() != null) return false;
            if (f.GetCustomAttribute<HideInEditorModeAttribute>() != null && !Application.isPlaying) return false;
            if (f.GetCustomAttribute<HideInPlayModeAttribute>() != null && Application.isPlaying) return false;
            if (f.GetCustomAttribute<ShowInPlayModeAttribute>() != null && !Application.isPlaying) return false;
            foreach (var s in f.GetCustomAttributes<ShowIfAttribute>())
                if (!InspectorMemberResolver.EvaluateBool(inst, s.Condition, s.Value, s.HasValue, true)) return false;
            foreach (var h in f.GetCustomAttributes<HideIfAttribute>())
                if (InspectorMemberResolver.EvaluateBool(inst, h.Condition, h.Value, h.HasValue, false)) return false;
            return true;
        }

        private static bool IsFieldEnabled(FieldInfo f, object inst)
        {
            if (f.GetCustomAttribute<ReadOnlyAttribute>() != null) return false;
            foreach (var en in f.GetCustomAttributes<EnableIfAttribute>())
                if (!InspectorMemberResolver.EvaluateBool(inst, en.Condition, en.Value, en.HasValue, true)) return false;
            foreach (var di in f.GetCustomAttributes<DisableIfAttribute>())
                if (InspectorMemberResolver.EvaluateBool(inst, di.Condition, di.Value, di.HasValue, false)) return false;
            return true;
        }

        // ---- helpers ------------------------------------------------------------

        private static object SafeBoxed(SerializedProperty p)
        {
            try { return p.boxedValue; } catch { return null; }
        }

        // Project convention (mirrors ListDrawerSettings.ListElementLabelName = "ElementLabel"):
        // if the element type exposes a readable string member named "ElementLabel", use it as the row label.
        private static GUIContent ResolveElementLabel(object inst, GUIContent fallback)
        {
            var t = inst.GetType();
            var p = t.GetProperty("ElementLabel", Flags);
            string s = null;
            if (p != null && p.CanRead && p.PropertyType == typeof(string)) s = Read(p, inst) as string;
            else
            {
                var f = t.GetField("ElementLabel", Flags);
                if (f != null && f.FieldType == typeof(string)) s = Read(f, inst) as string;
            }
            return string.IsNullOrEmpty(s) ? fallback : new GUIContent(s);
        }

        private static FieldInfo FindField(Type type, string name)
        {
            for (var t = type; t != null && t != typeof(object); t = t.BaseType)
            {
                var f = t.GetField(name, BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.DeclaredOnly);
                if (f != null) return f;
            }
            return null;
        }

        private static object Read(MemberInfo m, object inst)
        {
            try
            {
                if (m is FieldInfo f) return f.GetValue(f.IsStatic ? null : inst);
                if (m is PropertyInfo p && p.CanRead) return p.GetValue(p.GetGetMethod(true).IsStatic ? null : inst);
            }
            catch { }
            return null;
        }

        private static int Count(IEnumerable en)
        {
            if (en is ICollection c) return c.Count;
            int n = 0; foreach (var _ in en) n++; return n;
        }

        private static string NiceName(MemberInfo m, string fallback)
        {
            var lt = m.GetCustomAttribute<LabelTextAttribute>();
            if (lt != null && !string.IsNullOrEmpty(lt.Text)) return lt.Text;
            var btn = m.GetCustomAttribute<ButtonAttribute>();
            if (btn != null && !string.IsNullOrEmpty(btn.Name)) return btn.Name;
            return ObjectNames.NicifyVariableName(fallback);
        }
    }
}
#endif
