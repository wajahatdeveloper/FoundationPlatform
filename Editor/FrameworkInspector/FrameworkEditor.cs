#if UNITY_EDITOR
using System;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using UnityEditor;
using UnityEngine;

namespace Framework.Inspector.Editor
{
    /// <summary>
    /// Base inspector that renders the <see cref="Framework.Inspector"/> attributes through the
    /// in-house drawer engine. A type opts in with a 3-line editor:
    /// <code>[CustomEditor(typeof(Foo))] class FooEditor : FrameworkEditor { }</code>
    /// A concrete <c>[CustomEditor(typeof(T))]</c> beats the global <see cref="FrameworkFallbackEditor"/>,
    /// so hand-written inspectors keep priority.
    /// </summary>
    public abstract class FrameworkEditor : UnityEditor.Editor
    {
        private readonly Dictionary<string, bool> _foldouts = new Dictionary<string, bool>();
        private readonly Dictionary<string, int> _tabs = new Dictionary<string, int>();

        // Virtual lifecycle hooks so editors migrated off the previous base class (which declared these virtual)
        // can keep their `protected override void OnEnable()/OnDisable()` + `base.*()` calls compiling.
        // Unity invokes these magic methods on the most-derived type.
        protected virtual void OnEnable() { }
        protected virtual void OnDisable() { }

        public override void OnInspectorGUI()
        {
            serializedObject.Update();
            FrameworkInspectorRenderer.Draw(this, serializedObject, target, _foldouts, _tabs);
            serializedObject.ApplyModifiedProperties();
        }
    }

    internal enum GroupKind { Box, Foldout, Title, TabContainer, TabPage, Horizontal, Vertical, Toggle, ButtonRow }

    internal sealed class InspectorEntry
    {
        public enum Kind { Field, Shown, Button, InspectorGui }

        public Kind EntryKind;
        public float Order;
        public int Sequence;

        // Field
        public SerializedProperty Property;
        public FieldInfo Field;

        // Shown / Button / InspectorGui
        public MemberInfo Member;
        public MethodInfo ButtonMethod;
        public ButtonAttribute Button;

        // Shared metadata
        public MemberInfo AttributeSource;   // where attributes are read from
        public string ContainerPath = string.Empty;
        public string TabName;
        public float SpaceBefore;
        public float SpaceAfter;
        public HorizontalGroupAttribute OwnHorizontal; // this member's own horizontal-cell spec
    }

    internal sealed class GroupNode
    {
        public string Path;
        public string Name;
        public GroupKind Kind = GroupKind.Vertical;
        public bool KindResolved;
        public float Order;
        public int Sequence = int.MaxValue;
        public bool DefaultExpanded = true;
        public bool ShowLabel = true;
        public BoxGroupAttribute Box;
        public FoldoutGroupAttribute FoldoutAttr;
        public TitleGroupAttribute TitleAttr;
        public TabGroupAttribute Tab;
        public ToggleGroupAttribute Toggle;
        public HorizontalGroupAttribute Horizontal;
        public VerticalGroupAttribute Vertical;
        public readonly List<object> Children = new List<object>(); // GroupNode or InspectorEntry
        public readonly Dictionary<string, GroupNode> SubGroups = new Dictionary<string, GroupNode>();
    }

    public static class FrameworkInspectorRenderer
    {
        private const BindingFlags Flags =
            BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic;

        // Once-per-lifetime bookkeeping for [OnInspectorInit] and [OnValueChanged(InvokeOnInitialize)].
        // Keyed by target hash + member name; cleared on domain reload.
        private static readonly HashSet<long> s_initDone = new HashSet<long>();

        public static void Draw(UnityEditor.Editor editor, SerializedObject so, object target,
            Dictionary<string, bool> foldouts, Dictionary<string, int> tabs,
            bool drawScriptRow = true, HashSet<string> skipFields = null)
        {
            Type type = target.GetType();

            // --- Script row (matches Unity's default look) ---
            if (drawScriptRow)
            {
                var scriptProp = so.FindProperty("m_Script");
                if (scriptProp != null)
                {
                    using (new EditorGUI.DisabledScope(true))
                        EditorGUILayout.PropertyField(scriptProp);
                }
            }

            DrawTypeInfoBoxes(type, target);

            var entries = new List<InspectorEntry>();
            int seq = 0;

            // --- Serialized fields (exact serialized set comes from the SerializedObject) ---
            var it = so.GetIterator();
            bool enter = true;
            while (it.NextVisible(enter))
            {
                enter = false;
                if (it.propertyPath == "m_Script") continue;
                if (skipFields != null && skipFields.Contains(it.name)) continue;
                AddFieldEntry(entries, it.Copy(), target, ref seq);
            }

            AddReflectedEntries(entries, type, target, ref seq);
            RenderScope(entries, target, foldouts, tabs);
        }

        private static void DrawTypeInfoBoxes(Type type, object target)
        {
            foreach (var box in type.GetCustomAttributes<TypeInfoBoxAttribute>(true))
                EditorGUILayout.HelpBox(InspectorMemberResolver.ResolveString(target, box.Message), MessageType.Info);
        }

        // Build + group + render a set of entries against a target instance. Shared by the root
        // SerializedObject draw and nested [InlineProperty] objects (which drive it off child props).
        private static void RenderScope(List<InspectorEntry> entries, object target,
            Dictionary<string, bool> foldouts, Dictionary<string, int> tabs)
        {
            entries.Sort((a, b) => a.Order != b.Order ? a.Order.CompareTo(b.Order) : a.Sequence.CompareTo(b.Sequence));

            var root = new GroupNode { Path = string.Empty, Kind = GroupKind.Vertical, KindResolved = true };
            foreach (var e in entries)
                ResolveContainer(root, e, target).Children.Add(e);

            SortGroupChildren(root);
            RenderChildren(root, target, foldouts, tabs);
        }

        // Sibling ordering: groups take the Order of their defining attribute (default 0) and the
        // Sequence of their first member; entries carry their own, so groups and fields interleave.
        private static void SortGroupChildren(GroupNode node)
        {
            node.Children.Sort((a, b) =>
            {
                float oa = a is GroupNode ga ? ga.Order : ((InspectorEntry)a).Order;
                float ob = b is GroupNode gb ? gb.Order : ((InspectorEntry)b).Order;
                if (oa != ob) return oa.CompareTo(ob);
                int sa = a is GroupNode g2 ? g2.Sequence : ((InspectorEntry)a).Sequence;
                int sb = b is GroupNode g3 ? g3.Sequence : ((InspectorEntry)b).Sequence;
                return sa.CompareTo(sb);
            });
            foreach (var child in node.Children)
                if (child is GroupNode g) SortGroupChildren(g);
        }

        private static void AddFieldEntry(List<InspectorEntry> entries, SerializedProperty prop, object target, ref int seq)
        {
            var field = FindField(target.GetType(), prop.name);
            var e = new InspectorEntry
            {
                EntryKind = InspectorEntry.Kind.Field,
                Property = prop,
                Field = field,
                AttributeSource = field,
                Sequence = seq++,
            };
            ApplyMemberMetadata(e, field, target);
            entries.Add(e);
        }

        private static void AddReflectedEntries(List<InspectorEntry> entries, Type type, object target, ref int seq)
        {
            if (type == null) return;
            foreach (var m in EnumerateShowInInspector(type))
            {
                var e = new InspectorEntry
                {
                    EntryKind = InspectorEntry.Kind.Shown,
                    Member = m,
                    AttributeSource = m,
                    Sequence = seq++,
                };
                ApplyMemberMetadata(e, m, target);
                entries.Add(e);
            }

            foreach (var mi in AllMethods(type))
            {
                var btn = mi.GetCustomAttribute<ButtonAttribute>();
                if (btn != null)
                {
                    var e = new InspectorEntry
                    {
                        EntryKind = InspectorEntry.Kind.Button,
                        ButtonMethod = mi,
                        Button = btn,
                        Member = mi,
                        AttributeSource = mi,
                        Sequence = seq++,
                    };
                    ApplyMemberMetadata(e, mi, target);
                    entries.Add(e);
                    continue;
                }

                // [OnInspectorGUI] on a parameterless method → invoked at its position to draw custom IMGUI.
                if (mi.GetCustomAttribute<OnInspectorGUIAttribute>() != null && mi.GetParameters().Length == 0)
                {
                    var e = new InspectorEntry
                    {
                        EntryKind = InspectorEntry.Kind.InspectorGui,
                        ButtonMethod = mi,
                        Member = mi,
                        AttributeSource = mi,
                        Sequence = seq++,
                    };
                    ApplyMemberMetadata(e, mi, target);
                    entries.Add(e);
                }
            }
        }

        internal static IEnumerable<SerializedProperty> ChildProperties(SerializedProperty property)
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

        // ---------------------------------------------------------------- group tree

        private static GroupNode ResolveContainer(GroupNode root, InspectorEntry e, object target)
        {
            var source = e.AttributeSource;
            if (source == null) return root;

            string containerPath = string.Empty;
            int maxDepth = -1;

            foreach (var attr in source.GetCustomAttributes())
            {
                string path = null;

                // Merge semantics: settings are declared on ANY ONE of the attributes sharing a
                // group id; attributes that just reference the id (all defaults) must not reset them.
                switch (attr)
                {
                    case BoxGroupAttribute b:
                        path = b.GroupID;
                        Register(root, path, e, n =>
                        {
                            n.Kind = GroupKind.Box;
                            if (b.Order != 0f) n.Order = b.Order;
                            if (!b.ShowLabel) n.ShowLabel = false;
                            if (n.Box == null) n.Box = b;
                            else
                            {
                                if (b.CenterLabel) n.Box.CenterLabel = true;
                                if (b.LabelText != null) n.Box.LabelText = b.LabelText;
                            }
                        });
                        break;
                    case FoldoutGroupAttribute f:
                        path = f.GroupID;
                        Register(root, path, e, n =>
                        {
                            n.Kind = GroupKind.Foldout;
                            if (f.Order != 0f) n.Order = f.Order;
                            if (f.HasDefinedExpanded) n.DefaultExpanded = f.Expanded;
                            if (n.FoldoutAttr == null) n.FoldoutAttr = f;
                        });
                        break;
                    case TitleGroupAttribute t:
                        path = t.GroupID;
                        Register(root, path, e, n =>
                        {
                            n.Kind = GroupKind.Title;
                            if (t.Order != 0f) n.Order = t.Order;
                            if (n.TitleAttr == null || t.Subtitle != null || t.Alignment != TitleAlignments.Left
                                || !t.HorizontalLine || !t.BoldTitle || t.Indent)
                                n.TitleAttr = t;
                        });
                        break;
                    case TabGroupAttribute tg:
                    {
                        // The tab container is GroupID; each tab is a real child group "GroupID/TabName",
                        // so groups nested inside a tab (e.g. [BoxGroup("Tabs/A/Box")]) land on the right page.
                        Register(root, tg.GroupID, e, n =>
                        {
                            n.Kind = GroupKind.TabContainer;
                            if (tg.Order != 0f) n.Order = tg.Order;
                            if (n.Tab == null || tg.Paddingless || tg.HideTabGroupIfTabGroupOnlyHasOneTab) n.Tab = tg;
                        });
                        path = tg.GroupID + "/" + tg.TabName;
                        e.TabName = tg.TabName;
                        Register(root, path, e, n => { n.Kind = GroupKind.TabPage; });
                        break;
                    }
                    case HorizontalGroupAttribute h:
                        path = h.GroupID;
                        e.OwnHorizontal = h;
                        Register(root, path, e, n =>
                        {
                            n.Kind = GroupKind.Horizontal;
                            if (h.Order != 0f) n.Order = h.Order;
                            if (n.Horizontal == null || !string.IsNullOrEmpty(h.Title) || h.LabelWidth > 0f) n.Horizontal = h;
                        });
                        break;
                    case VerticalGroupAttribute v:
                        path = v.GroupID;
                        Register(root, path, e, n =>
                        {
                            n.Kind = GroupKind.Vertical;
                            n.KindResolved = true;
                            if (v.Order != 0f) n.Order = v.Order;
                            if (n.Vertical == null || v.PaddingTop > 0f || v.PaddingBottom > 0f) n.Vertical = v;
                        });
                        break;
                    case ToggleGroupAttribute tog:
                        path = tog.ToggleMemberName;
                        Register(root, path, e, n =>
                        {
                            n.Kind = GroupKind.Toggle;
                            if (tog.Order != 0f) n.Order = tog.Order;
                            if (n.Toggle == null || tog.GroupTitle != null) n.Toggle = tog;
                        });
                        break;
                    case ButtonGroupAttribute bg:
                        path = string.IsNullOrEmpty(bg.GroupID) ? "_DefaultButtonGroup" : bg.GroupID;
                        Register(root, path, e, n =>
                        {
                            n.Kind = GroupKind.ButtonRow;
                            if (bg.Order != 0f) n.Order = bg.Order;
                        });
                        break;
                    default:
                        continue;
                }

                if (string.IsNullOrEmpty(path)) continue;
                int depth = CountSegments(path);
                if (depth > maxDepth) { maxDepth = depth; containerPath = path; }
            }

            if (string.IsNullOrEmpty(containerPath)) return root;
            return GetNode(root, containerPath, e.Sequence);
        }

        private static void Register(GroupNode root, string path, InspectorEntry e, Action<GroupNode> configure)
        {
            if (string.IsNullOrEmpty(path)) return;
            var node = GetNode(root, path, e.Sequence);
            configure(node);
            node.KindResolved = true;
        }

        private static GroupNode GetNode(GroupNode root, string path, int sequence)
        {
            var segments = path.Split('/');
            var current = root;
            string acc = string.Empty;
            for (int i = 0; i < segments.Length; i++)
            {
                acc = i == 0 ? segments[i] : acc + "/" + segments[i];
                if (!current.SubGroups.TryGetValue(segments[i], out var child))
                {
                    child = new GroupNode
                    {
                        Path = acc,
                        Name = segments[i],
                        // Intermediate default: first segment reads as a box, deeper as transparent vertical.
                        Kind = i == 0 ? GroupKind.Box : GroupKind.Vertical,
                    };
                    current.SubGroups[segments[i]] = child;
                    current.Children.Add(child);
                }
                if (sequence < child.Sequence) child.Sequence = sequence;
                current = child;
            }
            return current;
        }

        private static int CountSegments(string path)
        {
            int n = 1;
            foreach (char c in path) if (c == '/') n++;
            return n;
        }

        // ---------------------------------------------------------------- rendering

        private static void RenderChildren(GroupNode group, object target, Dictionary<string, bool> foldouts, Dictionary<string, int> tabs)
        {
            foreach (var child in group.Children)
            {
                if (child is GroupNode g) { if (GroupHasVisible(g, target)) RenderGroup(g, target, foldouts, tabs); }
                else if (child is InspectorEntry e) RenderEntry(e, target, foldouts, tabs);
            }
        }

        // A group with no visible descendant entry is skipped entirely (no empty header/box).
        private static bool GroupHasVisible(GroupNode g, object target)
        {
            foreach (var child in g.Children)
            {
                if (child is InspectorEntry e)
                {
                    if (e.AttributeSource == null || IsVisible(e.AttributeSource, target)) return true;
                }
                else if (child is GroupNode sub && GroupHasVisible(sub, target)) return true;
            }
            return false;
        }

        private static void RenderGroup(GroupNode g, object target, Dictionary<string, bool> foldouts, Dictionary<string, int> tabs)
        {
            switch (g.Kind)
            {
                case GroupKind.Box:
                {
                    EditorGUILayout.BeginVertical(EditorStyles.helpBox);
                    if (g.ShowLabel)
                    {
                        string label = g.Box?.LabelText ?? g.Name;
                        label = InspectorMemberResolver.ResolveString(target, label);
                        if (!string.IsNullOrEmpty(label))
                        {
                            if (g.Box != null && g.Box.CenterLabel)
                            {
                                var style = new GUIStyle(EditorStyles.boldLabel) { alignment = TextAnchor.MiddleCenter };
                                EditorGUILayout.LabelField(label, style);
                            }
                            else EditorGUILayout.LabelField(label, EditorStyles.boldLabel);
                        }
                    }
                    RenderChildren(g, target, foldouts, tabs);
                    EditorGUILayout.EndVertical();
                    break;
                }
                case GroupKind.Title:
                {
                    var t = g.TitleAttr;
                    string title = InspectorMemberResolver.ResolveString(target, g.Name);
                    string subtitle = t != null ? InspectorMemberResolver.ResolveString(target, t.Subtitle) : null;
                    EditorGUILayout.Space(4);
                    GuiKit.Title(title, subtitle,
                        ToTextAlignment(t?.Alignment ?? TitleAlignments.Left),
                        t == null || t.HorizontalLine,
                        t == null || t.BoldTitle);
                    bool indent = t != null && t.Indent;
                    if (indent) EditorGUI.indentLevel++;
                    RenderChildren(g, target, foldouts, tabs);
                    if (indent) EditorGUI.indentLevel--;
                    break;
                }
                case GroupKind.Foldout:
                {
                    // Plain foldout matching Unity's native array/struct foldout look (no dark header
                    // bar, no surrounding box — a box border overlaps the arrow and reads as broken).
                    if (!foldouts.TryGetValue(g.Path, out bool expanded)) expanded = g.DefaultExpanded;
                    EditorGUILayout.Space(2);
                    expanded = EditorGUILayout.Foldout(expanded, InspectorMemberResolver.ResolveString(target, g.Name), true);
                    foldouts[g.Path] = expanded;
                    if (expanded)
                    {
                        EditorGUI.indentLevel++;
                        RenderChildren(g, target, foldouts, tabs);
                        EditorGUI.indentLevel--;
                    }
                    break;
                }
                case GroupKind.Horizontal:
                {
                    RenderHorizontalGroup(g, target, foldouts, tabs);
                    break;
                }
                case GroupKind.TabContainer:
                {
                    RenderTabGroup(g, target, foldouts, tabs);
                    break;
                }
                case GroupKind.TabPage:
                {
                    // Rendered by its TabContainer; reaching here means the page is orphaned — draw plain.
                    RenderChildren(g, target, foldouts, tabs);
                    break;
                }
                case GroupKind.Toggle:
                {
                    RenderToggleGroup(g, target, foldouts, tabs);
                    break;
                }
                case GroupKind.ButtonRow:
                {
                    EditorGUILayout.BeginHorizontal();
                    RenderChildren(g, target, foldouts, tabs);
                    EditorGUILayout.EndHorizontal();
                    break;
                }
                default: // Vertical / transparent
                {
                    if (g.Vertical != null && g.Vertical.PaddingTop > 0) GUILayout.Space(g.Vertical.PaddingTop);
                    EditorGUILayout.BeginVertical();
                    RenderChildren(g, target, foldouts, tabs);
                    EditorGUILayout.EndVertical();
                    if (g.Vertical != null && g.Vertical.PaddingBottom > 0) GUILayout.Space(g.Vertical.PaddingBottom);
                    break;
                }
            }
        }

        private static TextAlignment ToTextAlignment(TitleAlignments a) => a switch
        {
            TitleAlignments.Centered => TextAlignment.Center,
            TitleAlignments.Right => TextAlignment.Right,
            _ => TextAlignment.Left,
        };

        // ---- horizontal group: per-member column widths (0 = flex, <1 = fraction, >=1 = pixels) ----

        private static void RenderHorizontalGroup(GroupNode g, object target, Dictionary<string, bool> foldouts, Dictionary<string, int> tabs)
        {
            var spec = g.Horizontal;
            string title = spec?.Title;
            if (!string.IsNullOrEmpty(title))
                GuiKit.Title(InspectorMemberResolver.ResolveString(target, title));

            float available = EditorGUIUtility.currentViewWidth - 30f;
            float gap = spec?.Gap ?? 3f;

            EditorGUILayout.BeginHorizontal();
            if (spec != null && spec.MarginLeft > 0) GUILayout.Space(spec.MarginLeft);

            bool first = true;
            foreach (var child in g.Children)
            {
                if (child is GroupNode sub && !GroupHasVisible(sub, target)) continue;
                if (child is InspectorEntry pe && pe.AttributeSource != null && !IsVisible(pe.AttributeSource, target)) continue;

                if (!first && gap > 0) GUILayout.Space(gap);
                first = false;

                var cell = FindHorizontalSpec(child);
                var options = new List<GUILayoutOption>();
                if (cell != null)
                {
                    if (cell.Width >= 1f) options.Add(GUILayout.Width(cell.Width));
                    else if (cell.Width > 0f) options.Add(GUILayout.Width(available * cell.Width));
                    if (cell.MinWidth >= 1f) options.Add(GUILayout.MinWidth(cell.MinWidth));
                    else if (cell.MinWidth > 0f) options.Add(GUILayout.MinWidth(available * cell.MinWidth));
                    if (cell.MaxWidth >= 1f) options.Add(GUILayout.MaxWidth(cell.MaxWidth));
                    else if (cell.MaxWidth > 0f) options.Add(GUILayout.MaxWidth(available * cell.MaxWidth));
                    if (cell.PaddingLeft > 0) GUILayout.Space(cell.PaddingLeft);
                }

                EditorGUILayout.BeginVertical(options.ToArray());
                float prevLabel = EditorGUIUtility.labelWidth;
                float lw = cell?.LabelWidth ?? spec?.LabelWidth ?? 0f;
                if (lw > 0f && lw < 1f) lw = available * lw; // fractional label width = fraction of the view width
                if (lw > 0f) EditorGUIUtility.labelWidth = lw;

                if (child is GroupNode gn) RenderGroup(gn, target, foldouts, tabs);
                else RenderEntry((InspectorEntry)child, target, foldouts, tabs);

                EditorGUIUtility.labelWidth = prevLabel;
                EditorGUILayout.EndVertical();

                if (cell != null && cell.PaddingRight > 0) GUILayout.Space(cell.PaddingRight);
            }

            if (spec != null && spec.MarginRight > 0) GUILayout.Space(spec.MarginRight);
            EditorGUILayout.EndHorizontal();
        }

        private static HorizontalGroupAttribute FindHorizontalSpec(object child)
        {
            if (child is InspectorEntry e) return e.OwnHorizontal;
            if (child is GroupNode g)
            {
                foreach (var c in g.Children)
                {
                    var s = FindHorizontalSpec(c);
                    if (s != null) return s;
                }
            }
            return null;
        }

        // ---- tab group: pages are child groups; nested groups render inside their page ----

        private static void RenderTabGroup(GroupNode g, object target, Dictionary<string, bool> foldouts, Dictionary<string, int> tabs)
        {
            var pages = new List<GroupNode>();
            var strays = new List<InspectorEntry>();
            foreach (var c in g.Children)
            {
                if (c is GroupNode page) { if (GroupHasVisible(page, target)) pages.Add(page); }
                else if (c is InspectorEntry e) strays.Add(e);
            }

            foreach (var s in strays) RenderEntry(s, target, foldouts, tabs);

            if (pages.Count == 0) return;

            bool hideBar = pages.Count == 1 && g.Tab != null && g.Tab.HideTabGroupIfTabGroupOnlyHasOneTab;
            bool paddingless = g.Tab != null && g.Tab.Paddingless;

            if (!paddingless) EditorGUILayout.BeginVertical(EditorStyles.helpBox);
            int active = 0;
            if (!hideBar)
            {
                var names = new string[pages.Count];
                for (int i = 0; i < pages.Count; i++)
                    names[i] = InspectorMemberResolver.ResolveString(target, pages[i].Name);
                if (!tabs.TryGetValue(g.Path, out active)) active = 0;
                active = Mathf.Clamp(active, 0, pages.Count - 1);
                active = GUILayout.Toolbar(active, names);
                tabs[g.Path] = active;
            }
            RenderChildren(pages[active], target, foldouts, tabs);
            if (!paddingless) EditorGUILayout.EndVertical();
        }

        // ---- toggle group: header checkbox bound to ToggleMemberName; body shown while on ----

        private static void RenderToggleGroup(GroupNode g, object target, Dictionary<string, bool> foldouts, Dictionary<string, int> tabs)
        {
            var attr = g.Toggle;
            string toggleMember = attr?.ToggleMemberName ?? g.Name;
            bool on = false;
            var val = InspectorMemberResolver.GetValue(target, toggleMember, out bool failed);
            if (!failed && val is bool b) on = b;

            string title = attr?.GroupTitle ?? ObjectNames.NicifyVariableName(toggleMember);
            title = InspectorMemberResolver.ResolveString(target, title);

            EditorGUILayout.BeginVertical(EditorStyles.helpBox);
            EditorGUI.BeginChangeCheck();
            bool next = EditorGUILayout.ToggleLeft(title, on, EditorStyles.boldLabel);
            if (EditorGUI.EndChangeCheck() && !failed)
            {
                SetMemberValue(target, toggleMember, next);
                on = next;
                if (next && attr != null && attr.CollapseOthersOnExpand)
                {
                    // Sibling toggle groups could collapse when one expands; we approximate by
                    // turning off nothing (state is data, not UI) — expansion == the toggle itself.
                }
            }

            if (on)
            {
                EditorGUI.indentLevel++;
                foreach (var child in g.Children)
                {
                    // The toggle member itself is the header; skip its normal row.
                    if (child is InspectorEntry e &&
                        ((e.Field != null && e.Field.Name == toggleMember) ||
                         (e.Member != null && e.Member.Name == toggleMember)))
                        continue;
                    if (child is GroupNode sub) { if (GroupHasVisible(sub, target)) RenderGroup(sub, target, foldouts, tabs); }
                    else if (child is InspectorEntry ie) RenderEntry(ie, target, foldouts, tabs);
                }
                EditorGUI.indentLevel--;
            }
            EditorGUILayout.EndVertical();
        }

        private static void SetMemberValue(object target, string name, object value)
        {
            var uo = target as UnityEngine.Object;
            if (uo != null) Undo.RecordObject(uo, "Inspector");
            var t = target.GetType();
            try
            {
                var f = FindField(t, name);
                if (f != null) { f.SetValue(target, value); }
                else
                {
                    var p = t.GetProperty(name, BindingFlags.Instance | BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic);
                    if (p != null && p.CanWrite) p.SetValue(p.GetSetMethod(true).IsStatic ? null : target, value);
                }
            }
            catch { }
            if (uo != null) EditorUtility.SetDirty(uo);
        }

        // ---------------------------------------------------------------- entry pipeline
        // Decorator order: [Title] → info boxes → validation messages → the (possibly wrapped) field.

        private static void RenderEntry(InspectorEntry e, object target, Dictionary<string, bool> foldouts, Dictionary<string, int> tabs)
        {
            var src = e.AttributeSource;

            // Visibility (ShowIf/HideIf/HideInEditorMode/...).
            if (src != null && !IsVisible(src, target)) return;

            RunInitHooks(e, target);

            if (e.SpaceBefore > 0) EditorGUILayout.Space(e.SpaceBefore);

            if (src != null)
            {
                // [Title] decorator(s).
                foreach (var t in src.GetCustomAttributes<TitleAttribute>())
                {
                    EditorGUILayout.Space(2);
                    GuiKit.Title(InspectorMemberResolver.ResolveString(target, t.Title),
                        InspectorMemberResolver.ResolveString(target, t.Subtitle),
                        ToTextAlignment(t.TitleAlignment), t.HorizontalLine, t.Bold);
                }

                // InfoBox(es) attached to the member.
                foreach (var info in src.GetCustomAttributes<InfoBoxAttribute>())
                {
                    if (!string.IsNullOrEmpty(info.VisibleIf) &&
                        !InspectorMemberResolver.EvaluateBool(target, info.VisibleIf, null, false, true))
                        continue;
                    EditorGUILayout.HelpBox(InspectorMemberResolver.ResolveString(target, info.Message), ToMsgType(info.InfoMessageType));
                }

                foreach (var info in src.GetCustomAttributes<DetailedInfoBoxAttribute>())
                {
                    if (!string.IsNullOrEmpty(info.VisibleIf) &&
                        !InspectorMemberResolver.EvaluateBool(target, info.VisibleIf, null, false, true))
                        continue;
                    DrawDetailedInfoBox(e, target, info, foldouts);
                }

                // Validation feedback draws ABOVE the field.
                RenderValidation(e, target);
            }

            // Enabled (EnableIf/DisableIf/ReadOnly).
            bool enabled = src == null || IsEnabled(src, target);

            // GUI color.
            Color prev = GUI.color;
            if (src != null && TryGetGuiColor(src, target, out var col)) GUI.color = col;

            // LabelWidth / Indent scopes.
            float prevLabelWidth = EditorGUIUtility.labelWidth;
            var lw = src?.GetCustomAttribute<LabelWidthAttribute>();
            if (lw != null && lw.Width > 0) EditorGUIUtility.labelWidth = lw.Width;
            var ind = src?.GetCustomAttribute<IndentAttribute>();
            if (ind != null) EditorGUI.indentLevel += ind.IndentLevel;

            // [OnInspectorGUI(prepend, append)] on a member.
            var onGui = src?.GetCustomAttribute<OnInspectorGUIAttribute>();
            if (onGui != null && !string.IsNullOrEmpty(onGui.Prepend)) InvokeDrawMethod(target, onGui.Prepend);

            using (new EditorGUI.DisabledScope(!enabled))
            {
                var inline = src?.GetCustomAttributes<InlineButtonAttribute>();
                bool hasInline = false;
                if (inline != null)
                    foreach (var ib in inline) { if (InlineButtonVisible(ib, target)) { hasInline = true; break; } }

                if (hasInline) EditorGUILayout.BeginHorizontal();

                switch (e.EntryKind)
                {
                    case InspectorEntry.Kind.Field: RenderField(e, target, foldouts, tabs); break;
                    case InspectorEntry.Kind.Shown: RenderShown(e, target); break;
                    case InspectorEntry.Kind.Button: RenderButton(e, target); break;
                    case InspectorEntry.Kind.InspectorGui: InvokeDrawMethodInfo(target, e.ButtonMethod); break;
                }

                if (hasInline)
                {
                    foreach (var ib in inline)
                    {
                        if (!InlineButtonVisible(ib, target)) continue;
                        string label = ib.Label != null
                            ? InspectorMemberResolver.ResolveString(target, ib.Label)
                            : ObjectNames.NicifyVariableName(ib.Action);
                        var content = MakeButtonContent(label, ib.Icon);
                        if (GUILayout.Button(content, EditorStyles.miniButton, GUILayout.ExpandWidth(false)))
                            InvokeAction(target, ib.Action);
                    }
                    EditorGUILayout.EndHorizontal();
                }
            }

            if (onGui != null && !string.IsNullOrEmpty(onGui.Append)) InvokeDrawMethod(target, onGui.Append);

            if (ind != null) EditorGUI.indentLevel -= ind.IndentLevel;
            EditorGUIUtility.labelWidth = prevLabelWidth;
            GUI.color = prev;

            if (e.SpaceAfter > 0) EditorGUILayout.Space(e.SpaceAfter);
        }

        private static bool InlineButtonVisible(InlineButtonAttribute ib, object target)
            => string.IsNullOrEmpty(ib.ShowIf) || InspectorMemberResolver.EvaluateBool(target, ib.ShowIf, null, false, true);

        private static GUIContent MakeButtonContent(string label, string icon)
        {
            if (!string.IsNullOrEmpty(icon))
            {
                try
                {
                    var ic = EditorGUIUtility.IconContent(icon);
                    if (ic != null && ic.image != null) return new GUIContent(label, ic.image);
                }
                catch { }
            }
            return new GUIContent(label);
        }

        private static void DrawDetailedInfoBox(InspectorEntry e, object target, DetailedInfoBoxAttribute info, Dictionary<string, bool> foldouts)
        {
            string key = "dibox:" + (e.AttributeSource?.Name ?? "?") + ":" + info.Message;
            foldouts.TryGetValue(key, out bool open);
            EditorGUILayout.HelpBox(InspectorMemberResolver.ResolveString(target, info.Message)
                + (open ? "\n\n" + InspectorMemberResolver.ResolveString(target, info.Details) : ""), ToMsgType(info.InfoMessageType));
            var r = GUILayoutUtility.GetLastRect();
            if (Event.current.type == EventType.MouseDown && r.Contains(Event.current.mousePosition))
            {
                foldouts[key] = !open;
                Event.current.Use();
                GUI.changed = true;
            }
        }

        // [OnInspectorInit] + [OnValueChanged(InvokeOnInitialize = true)] — run once per member.
        private static void RunInitHooks(InspectorEntry e, object target)
        {
            var src = e.AttributeSource;
            if (src == null) return;

            long key = ((long)(target?.GetHashCode() ?? 0) << 32) ^ (uint)(src.DeclaringType?.FullName ?? "").GetHashCode() ^ (uint)src.Name.GetHashCode();
            if (s_initDone.Contains(key)) return;
            s_initDone.Add(key);

            foreach (var init in src.GetCustomAttributes<OnInspectorInitAttribute>())
            {
                if (!string.IsNullOrEmpty(init.Action)) InvokeAction(target, init.Action);
                else if (src is MethodInfo mi && mi.GetParameters().Length == 0)
                    try { mi.Invoke(mi.IsStatic ? null : target, null); } catch { }
            }

            foreach (var ovc in src.GetCustomAttributes<OnValueChangedAttribute>())
                if (ovc.InvokeOnInitialize) InvokeChangeAction(e, target, ovc);
        }

        private static void InvokeDrawMethod(object target, string methodName)
        {
            var mi = InspectorMemberResolver.FindMethod(target.GetType(), methodName, Type.EmptyTypes);
            InvokeDrawMethodInfo(target, mi);
        }

        private static void InvokeDrawMethodInfo(object target, MethodInfo mi)
        {
            if (mi == null) return;
            try { mi.Invoke(mi.IsStatic ? null : target, null); }
            catch (Exception ex)
            {
                EditorGUILayout.HelpBox($"[OnInspectorGUI] {mi.Name}: {ex.InnerException?.Message ?? ex.Message}", MessageType.Error);
            }
        }

        private static void InvokeAction(object target, string methodName)
        {
            var mi = InspectorMemberResolver.FindMethod(target.GetType(), methodName, Type.EmptyTypes);
            try { mi?.Invoke(mi.IsStatic ? null : target, null); }
            catch (Exception ex) { Debug.LogWarning($"[Framework.Inspector] '{methodName}' threw: {ex.InnerException?.Message ?? ex.Message}"); }
            if (target is UnityEngine.Object uo) EditorUtility.SetDirty(uo);
        }

        // ---------------------------------------------------------------- field rendering

        private static void RenderField(InspectorEntry e, object target,
            Dictionary<string, bool> foldouts, Dictionary<string, int> tabs)
        {
            var src = e.AttributeSource;
            var prop = e.Property;

            // [DrawWithUnity] → always the stock drawer.
            if (src?.GetCustomAttribute<DrawWithUnityAttribute>() != null)
            {
                DrawDefaultField(e, target);
                return;
            }

            // [TableList] on an array/list → grid renderer.
            var table = src?.GetCustomAttribute<TableListAttribute>();
            if (table != null && prop.isArray && e.Field != null)
            {
                var elemType0 = GetElementType(e.Field.FieldType);
                if (elemType0 != null)
                {
                    EditorGUI.BeginChangeCheck();
                    TableRenderer.DrawSerializedTable(prop, elemType0, table, GetLabel(e, target));
                    if (EditorGUI.EndChangeCheck())
                    {
                        prop.serializedObject.ApplyModifiedProperties();
                        InvokeOnValueChanged(e, target);
                    }
                    return;
                }
            }

            // Collections that need the engine list drawer.
            if (prop.isArray && prop.propertyType == SerializedPropertyType.Generic)
            {
                var lds = src?.GetCustomAttribute<ListDrawerSettingsAttribute>();
                var searchable = src?.GetCustomAttribute<SearchableAttribute>();
                var vdList = src?.GetCustomAttribute<ValueDropdownAttribute>();
                var asList = src?.GetCustomAttribute<AssetSelectorAttribute>();
                var occ = src?.GetCustomAttribute<OnCollectionChangedAttribute>();
                var elemType = GetElementType(e.Field?.FieldType);
                bool engineElems = elemType != null && !HasCustomPropertyDrawer(elemType)
                    && (elemType.GetCustomAttribute<InlinePropertyAttribute>() != null || TypeHasEngineAttributes(elemType));

                if (lds != null || searchable != null || occ != null || engineElems
                    || (vdList != null && vdList.DrawDropdownForListElements)
                    || (asList != null && asList.DrawDropdownForListElements))
                {
                    EngineListDrawer.Draw(e, target, foldouts, tabs, elemType, lds, searchable, vdList, asList, occ);
                    return;
                }
            }

            // [ValueDropdown(getter)] → dropdown of allowed values.
            var vd = src?.GetCustomAttribute<ValueDropdownAttribute>();
            if (vd != null && InspectorDropdown.DrawValueDropdown(e, target, vd, GetLabelText(e, target))) return;

            // [AssetSelector] on a single object reference.
            var asel = src?.GetCustomAttribute<AssetSelectorAttribute>();
            if (asel != null && prop.propertyType == SerializedPropertyType.ObjectReference)
            {
                InspectorDropdown.DrawAssetSelector(e, target, asel);
                return;
            }

            // [DisplayAsString] → read-only text.
            var das = src?.GetCustomAttribute<DisplayAsStringAttribute>();
            if (das != null)
            {
                object v = e.Field != null ? SafeGet(e.Field, target) : ReadProperty(prop);
                DrawDisplayAsString(GetLabel(e, target) ?? new GUIContent(prop.displayName), v?.ToString() ?? string.Empty, das);
                return;
            }

            // [ToggleLeft] bool → left-aligned checkbox (label right of the box).
            if (prop.propertyType == SerializedPropertyType.Boolean &&
                src?.GetCustomAttribute<ToggleLeftAttribute>() != null)
            {
                var lbl = GetLabel(e, target) ?? new GUIContent(prop.displayName);
                EditorGUI.BeginChangeCheck();
                bool v = EditorGUILayout.ToggleLeft(lbl, prop.boolValue);
                if (EditorGUI.EndChangeCheck()) { prop.boolValue = v; Commit(e, target); }
                return;
            }

            // [MultiLineProperty(lines)] string → text area.
            var ml = src?.GetCustomAttribute<MultiLinePropertyAttribute>();
            if (ml != null && prop.propertyType == SerializedPropertyType.String)
            {
                var lbl = GetLabel(e, target) ?? new GUIContent(prop.displayName);
                EditorGUILayout.LabelField(lbl);
                EditorGUI.BeginChangeCheck();
                float h = Mathf.Max(1, ml.Lines) * EditorGUIUtility.singleLineHeight;
                string s = EditorGUILayout.TextArea(prop.stringValue, GUILayout.MinHeight(h));
                if (EditorGUI.EndChangeCheck()) { prop.stringValue = s; Commit(e, target); }
                return;
            }

            // [PropertyRange(min,max)] numeric → slider (getters resolved via member names).
            var pr = src?.GetCustomAttribute<PropertyRangeAttribute>();
            if (pr != null && TryDrawPropertyRange(e, target, pr)) return;

            // [MinMaxSlider] on Vector2/Vector2Int.
            var mms = src?.GetCustomAttribute<MinMaxSliderAttribute>();
            if (mms != null && TryDrawMinMaxSlider(e, target, mms)) return;

            // [ProgressBar] on a numeric.
            var pb = src?.GetCustomAttribute<ProgressBarAttribute>();
            if (pb != null && TryDrawProgressBar(e, target, pb)) return;

            // [EnumToggleButtons] on an enum.
            if (prop.propertyType == SerializedPropertyType.Enum &&
                src?.GetCustomAttribute<EnumToggleButtonsAttribute>() != null &&
                TryDrawEnumToggleButtons(e, target))
                return;

            // Object-reference specials.
            if (prop.propertyType == SerializedPropertyType.ObjectReference)
            {
                var pf = src?.GetCustomAttribute<PreviewFieldAttribute>();
                if (pf != null) { DrawPreviewField(e, target, pf); return; }

                var ie = src?.GetCustomAttribute<InlineEditorAttribute>();
                if (ie != null) { DrawInlineEditor(e, target, ie, foldouts); return; }

                if (src?.GetCustomAttribute<AssetsOnlyAttribute>() != null)
                {
                    DrawObjectField(e, target, allowScene: false);
                    return;
                }
                if (src?.GetCustomAttribute<SceneObjectsOnlyAttribute>() != null)
                {
                    DrawSceneObjectField(e, target);
                    return;
                }
            }

            // Nested serializable object — recurse through the engine (property-tree style) when it
            // either carries [InlineProperty] (draw inline, no wrapper) OR declares ANY Framework.Inspector
            // attribute (draw under a collapsible foldout). Plain data (only Unity attrs) and types with
            // their own custom PropertyDrawer fall through to the default PropertyField below.
            var fieldType = e.Field?.FieldType;
            var inlineAttr = src?.GetCustomAttribute<InlinePropertyAttribute>()
                             ?? (fieldType != null ? fieldType.GetCustomAttribute<InlinePropertyAttribute>() : null);
            bool explicitInline = inlineAttr != null;
            if (prop.propertyType == SerializedPropertyType.Generic && prop.hasVisibleChildren && !prop.isArray
                && !HasCustomPropertyDrawer(fieldType)
                && (explicitInline || TypeHasEngineAttributes(fieldType)))
            {
                float prevLw = EditorGUIUtility.labelWidth;
                if (inlineAttr != null && inlineAttr.LabelWidth > 0) EditorGUIUtility.labelWidth = inlineAttr.LabelWidth;
                DrawNestedObject(e, target, foldouts, tabs, inline: explicitInline);
                EditorGUIUtility.labelWidth = prevLw;
                return;
            }

            // Default field + numeric constraints ([MinValue]/[MaxValue]/[Wrap]).
            DrawDefaultField(e, target);
        }

        private static void DrawDefaultField(InspectorEntry e, object target)
        {
            var label = GetLabel(e, target);
            EditorGUI.BeginChangeCheck();
            if (label != null) EditorGUILayout.PropertyField(e.Property, label, true);
            else EditorGUILayout.PropertyField(e.Property, true);
            if (EditorGUI.EndChangeCheck())
            {
                ApplyNumericConstraints(e, target);
                Commit(e, target);
            }
        }

        // [MinValue]/[MaxValue] clamp and [Wrap] wrap the edited value (floats, ints, vectors).
        private static void ApplyNumericConstraints(InspectorEntry e, object target)
        {
            var src = e.AttributeSource;
            if (src == null) return;
            var prop = e.Property;

            var minA = src.GetCustomAttribute<MinValueAttribute>();
            var maxA = src.GetCustomAttribute<MaxValueAttribute>();
            var wrap = src.GetCustomAttribute<WrapAttribute>();
            if (minA == null && maxA == null && wrap == null) return;

            double min = minA != null ? (minA.Expression != null ? ResolveNumber(target, minA.Expression, double.MinValue) : minA.Min) : double.MinValue;
            double max = maxA != null ? (maxA.Expression != null ? ResolveNumber(target, maxA.Expression, double.MaxValue) : maxA.Max) : double.MaxValue;

            double Constrain(double v)
            {
                if (wrap != null && wrap.Max > wrap.Min)
                {
                    double range = wrap.Max - wrap.Min;
                    v = wrap.Min + ((v - wrap.Min) % range + range) % range;
                }
                if (v < min) v = min;
                if (v > max) v = max;
                return v;
            }

            switch (prop.propertyType)
            {
                case SerializedPropertyType.Integer: prop.intValue = (int)Constrain(prop.intValue); break;
                case SerializedPropertyType.Float: prop.floatValue = (float)Constrain(prop.floatValue); break;
                case SerializedPropertyType.Vector2: { var v = prop.vector2Value; v.x = (float)Constrain(v.x); v.y = (float)Constrain(v.y); prop.vector2Value = v; break; }
                case SerializedPropertyType.Vector3: { var v = prop.vector3Value; v.x = (float)Constrain(v.x); v.y = (float)Constrain(v.y); v.z = (float)Constrain(v.z); prop.vector3Value = v; break; }
                case SerializedPropertyType.Vector4: { var v = prop.vector4Value; v.x = (float)Constrain(v.x); v.y = (float)Constrain(v.y); v.z = (float)Constrain(v.z); v.w = (float)Constrain(v.w); prop.vector4Value = v; break; }
            }
        }

        internal static void Commit(InspectorEntry e, object target)
        {
            e.Property.serializedObject.ApplyModifiedProperties();
            InvokeOnValueChanged(e, target);
        }

        private static object SafeGet(FieldInfo f, object target)
        {
            try { return f.GetValue(f.IsStatic ? null : target); } catch { return null; }
        }

        // ---- exotic value drawers ---------------------------------------------------

        private static void DrawDisplayAsString(GUIContent label, string text, DisplayAsStringAttribute das)
        {
            var style = new GUIStyle(EditorStyles.label)
            {
                wordWrap = !das.Overflow,
                richText = das.EnableRichText,
                alignment = das.Alignment == TextAlignment.Center ? TextAnchor.MiddleCenter
                    : das.Alignment == TextAlignment.Right ? TextAnchor.MiddleRight : TextAnchor.MiddleLeft,
            };
            if (das.FontSize > 0) style.fontSize = das.FontSize;
            using (new EditorGUI.DisabledScope(true))
            {
                if (label == GUIContent.none) EditorGUILayout.LabelField(text, style);
                else EditorGUILayout.LabelField(label, new GUIContent(text), style);
            }
        }

        private static void DrawObjectField(InspectorEntry e, object target, bool allowScene)
        {
            var prop = e.Property;
            var t = e.Field != null ? e.Field.FieldType : typeof(UnityEngine.Object);
            var lbl = GetLabel(e, target) ?? new GUIContent(prop.displayName);
            EditorGUI.BeginChangeCheck();
            var obj = EditorGUILayout.ObjectField(lbl, prop.objectReferenceValue, t, allowScene);
            if (EditorGUI.EndChangeCheck()) { prop.objectReferenceValue = obj; Commit(e, target); }
        }

        // [SceneObjectsOnly]: reject persistent assets on assignment.
        private static void DrawSceneObjectField(InspectorEntry e, object target)
        {
            var prop = e.Property;
            var t = e.Field != null ? e.Field.FieldType : typeof(UnityEngine.Object);
            var lbl = GetLabel(e, target) ?? new GUIContent(prop.displayName);
            EditorGUI.BeginChangeCheck();
            var obj = EditorGUILayout.ObjectField(lbl, prop.objectReferenceValue, t, true);
            if (EditorGUI.EndChangeCheck())
            {
                if (obj != null && EditorUtility.IsPersistent(obj))
                    Debug.LogWarning($"[Framework.Inspector] '{prop.displayName}' accepts scene objects only.");
                else { prop.objectReferenceValue = obj; Commit(e, target); }
            }
        }

        // [PreviewField]: square preview that IS the picker (tall ObjectField rects draw as previews).
        private static void DrawPreviewField(InspectorEntry e, object target, PreviewFieldAttribute pf)
        {
            var prop = e.Property;
            var t = e.Field != null ? e.Field.FieldType : typeof(UnityEngine.Object);
            float h = pf.Height > 0 ? pf.Height : 64f;
            var lbl = GetLabel(e, target) ?? new GUIContent(prop.displayName);

            var rect = EditorGUILayout.GetControlRect(false, h);
            var labelRect = new Rect(rect.x, rect.y, EditorGUIUtility.labelWidth, EditorGUIUtility.singleLineHeight);
            if (lbl != GUIContent.none) EditorGUI.LabelField(labelRect, lbl);

            float x = rect.x + EditorGUIUtility.labelWidth;
            float w = rect.width - EditorGUIUtility.labelWidth;
            var square = new Rect(x, rect.y, h, h);
            if (pf.Alignment == ObjectFieldAlignment.Center) square.x = x + (w - h) * 0.5f;
            else if (pf.Alignment == ObjectFieldAlignment.Right) square.x = rect.xMax - h;

            EditorGUI.BeginChangeCheck();
            var obj = EditorGUI.ObjectField(square, prop.objectReferenceValue, t, true);
            if (EditorGUI.EndChangeCheck()) { prop.objectReferenceValue = obj; Commit(e, target); }
        }

        private static readonly Dictionary<int, UnityEditor.Editor> s_inlineEditors = new Dictionary<int, UnityEditor.Editor>();

        private static void DrawInlineEditor(InspectorEntry e, object target, InlineEditorAttribute ie, Dictionary<string, bool> foldouts)
        {
            var prop = e.Property;
            var obj = prop.objectReferenceValue;

            // Object field per ObjectFieldMode.
            bool showField = ie.ObjectFieldMode switch
            {
                InlineEditorObjectFieldModes.CompletelyHidden => false,
                InlineEditorObjectFieldModes.Hidden => obj == null,
                _ => true,
            };

            string foldKey = "inline:" + prop.propertyPath;
            if (!foldouts.TryGetValue(foldKey, out bool expanded)) expanded = ie.Expanded;

            if (showField)
            {
                if (ie.ObjectFieldMode == InlineEditorObjectFieldModes.Foldout && obj != null)
                {
                    var t = e.Field != null ? e.Field.FieldType : typeof(UnityEngine.Object);
                    var lbl = GetLabel(e, target) ?? new GUIContent(prop.displayName);
                    var rect = EditorGUILayout.GetControlRect();
                    var foldRect = new Rect(rect.x, rect.y, EditorGUIUtility.labelWidth, rect.height);
                    expanded = EditorGUI.Foldout(foldRect, expanded, lbl, true);
                    var fieldRect = new Rect(rect.x + EditorGUIUtility.labelWidth, rect.y,
                        rect.width - EditorGUIUtility.labelWidth, rect.height);
                    EditorGUI.BeginChangeCheck();
                    var picked = EditorGUI.ObjectField(fieldRect, prop.objectReferenceValue, t, true);
                    if (EditorGUI.EndChangeCheck()) { prop.objectReferenceValue = picked; Commit(e, target); }
                    foldouts[foldKey] = expanded;
                }
                else
                {
                    DrawObjectField(e, target, allowScene: true);
                    if (ie.ObjectFieldMode != InlineEditorObjectFieldModes.Foldout) expanded = true;
                }
            }
            else expanded = true;

            obj = prop.objectReferenceValue;
            if (obj == null || !expanded) return;
            // Guard against inlining the object being inspected (infinite recursion).
            if (obj == (UnityEngine.Object)target) return;

            int id = obj.GetInstanceID();
            if (!s_inlineEditors.TryGetValue(id, out var ed) || ed == null || ed.target != obj)
            {
                if (ed != null) UnityEngine.Object.DestroyImmediate(ed);
                ed = UnityEditor.Editor.CreateEditor(obj);
                s_inlineEditors[id] = ed;
            }
            if (ed == null) return;

            EditorGUILayout.BeginVertical(EditorStyles.helpBox);
            if (ie.DrawHeader) ed.DrawHeader();
            if (ie.DrawGUI)
            {
                EditorGUI.indentLevel++;
                ed.OnInspectorGUI();
                EditorGUI.indentLevel--;
            }
            if (ie.DrawPreview && ed.HasPreviewGUI())
            {
                float ph = ie.PreviewHeight > 0 ? ie.PreviewHeight : 35f;
                var pr = EditorGUILayout.GetControlRect(false, ph);
                ed.OnPreviewGUI(pr, GUIStyle.none);
            }
            else if (ie.DrawPreview)
            {
                var tex = AssetPreview.GetAssetPreview(obj) ?? AssetPreview.GetMiniThumbnail(obj);
                if (tex != null)
                {
                    float ph = ie.PreviewHeight > 0 ? ie.PreviewHeight : 35f;
                    var pr = EditorGUILayout.GetControlRect(false, ph);
                    GUI.DrawTexture(pr, tex, ScaleMode.ScaleToFit);
                }
            }
            EditorGUILayout.EndVertical();
        }

        // Draw a nested serializable object through the engine. inline=true → no wrapper
        // ([InlineProperty]); inline=false → collapsible foldout (default collapsed), the default
        // rendering of an attributed nested object.
        internal static void DrawNestedObject(InspectorEntry e, object target,
            Dictionary<string, bool> foldouts, Dictionary<string, int> tabs, bool inline,
            GUIContent labelOverride = null)
        {
            object inst = null;
            try { inst = e.Property.boxedValue; } catch { }

            var lbl = labelOverride ?? GetLabel(e, target);
            bool hideLabel = lbl == GUIContent.none;

            // Fallback: no boxed instance (multi-edit/unresolvable) → default field draw.
            if (inst == null)
            {
                if (lbl != null) EditorGUILayout.PropertyField(e.Property, lbl, true);
                else EditorGUILayout.PropertyField(e.Property, true);
                return;
            }

            // Layout:
            //   [InlineProperty] + [HideLabel] → children flush, no header, no indent.
            //   [InlineProperty] with a label  → label row, children indented one level.
            //   default (attributed nested)    → collapsible foldout; HideLabel drops the header entirely.
            bool indent;
            if (inline)
            {
                if (!hideLabel) EditorGUILayout.LabelField(lbl ?? new GUIContent(e.Property.displayName));
                indent = !hideLabel;
            }
            else if (hideLabel)
            {
                indent = false; // headerless — draw children flush
            }
            else
            {
                // Collapsible foldout header; skip the body when collapsed.
                e.Property.isExpanded = EditorGUILayout.Foldout(e.Property.isExpanded,
                    lbl ?? new GUIContent(e.Property.displayName), true);
                if (!e.Property.isExpanded) return;
                indent = true;
            }

            if (indent) EditorGUI.indentLevel++;
            try
            {
                DrawTypeInfoBoxes(inst.GetType(), inst);
                var nested = new List<InspectorEntry>();
                int seq = 0;
                foreach (var child in ChildProperties(e.Property))
                    AddFieldEntry(nested, child, inst, ref seq);
                AddReflectedEntries(nested, inst.GetType(), inst, ref seq);
                RenderScope(nested, inst, foldouts, tabs);
            }
            catch (Exception ex)
            {
                // Degrade to flat children rather than blanking the whole inspector.
                Debug.LogWarning($"[Framework.Inspector] nested draw of '{e.Property?.propertyPath}' failed: {ex.Message}");
                foreach (var child in ChildProperties(e.Property))
                    EditorGUILayout.PropertyField(child, true);
            }
            if (indent) EditorGUI.indentLevel--;
        }

        // Cached: does the type declare ANY Framework.Inspector attribute (on itself or any member)?
        // Used to auto-recurse attributed nested objects without requiring an explicit [InlineProperty].
        private const string EngineAttrNamespace = "Framework.Inspector";
        private static readonly Dictionary<Type, bool> s_engineAttrTypes = new Dictionary<Type, bool>();

        internal static bool TypeHasEngineAttributes(Type t)
        {
            if (t == null) return false;
            if (s_engineAttrTypes.TryGetValue(t, out bool cached)) return cached;

            bool has = HasEngineAttr(t.GetCustomAttributes(true));
            // Walk the hierarchy with DeclaredOnly so PRIVATE fields inherited from base classes
            // (which GetMembers on the derived type would omit) are still inspected.
            const BindingFlags F = BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.DeclaredOnly;
            for (var cur = t; cur != null && cur != typeof(object) && !has; cur = cur.BaseType)
            {
                var members = cur.GetMembers(F);
                for (int i = 0; i < members.Length && !has; i++)
                {
                    if (members[i] is Type) continue; // nested type declarations
                    has = HasEngineAttr(members[i].GetCustomAttributes(true));
                }
            }
            s_engineAttrTypes[t] = has;
            return has;
        }

        private static bool HasEngineAttr(object[] attrs)
        {
            for (int i = 0; i < attrs.Length; i++)
                if (attrs[i].GetType().Namespace == EngineAttrNamespace) return true;
            return false;
        }

        private static bool TryDrawPropertyRange(InspectorEntry e, object target, PropertyRangeAttribute pr)
        {
            double min = ResolveNumber(target, pr.MinGetter, pr.Min);
            double max = ResolveNumber(target, pr.MaxGetter, pr.Max);
            var prop = e.Property;
            var lbl = GetLabel(e, target) ?? new GUIContent(prop.displayName);
            if (prop.propertyType == SerializedPropertyType.Integer)
            {
                EditorGUI.BeginChangeCheck();
                int v = EditorGUILayout.IntSlider(lbl, prop.intValue, (int)min, (int)max);
                if (EditorGUI.EndChangeCheck()) { prop.intValue = v; Commit(e, target); }
                return true;
            }
            if (prop.propertyType == SerializedPropertyType.Float)
            {
                EditorGUI.BeginChangeCheck();
                float v = EditorGUILayout.Slider(lbl, prop.floatValue, (float)min, (float)max);
                if (EditorGUI.EndChangeCheck()) { prop.floatValue = v; Commit(e, target); }
                return true;
            }
            return false;
        }

        private static bool TryDrawMinMaxSlider(InspectorEntry e, object target, MinMaxSliderAttribute mms)
        {
            var prop = e.Property;
            float lo = mms.MinValue, hi = mms.MaxValue;
            if (!string.IsNullOrEmpty(mms.MinMaxValueGetter))
            {
                var v = InspectorMemberResolver.GetValue(target, mms.MinMaxValueGetter, out bool failed);
                if (!failed && v is Vector2 range) { lo = range.x; hi = range.y; }
            }
            else
            {
                lo = (float)ResolveNumber(target, mms.MinValueGetter, lo);
                hi = (float)ResolveNumber(target, mms.MaxValueGetter, hi);
            }

            var lbl = GetLabel(e, target) ?? new GUIContent(prop.displayName);

            if (prop.propertyType == SerializedPropertyType.Vector2)
            {
                var v = prop.vector2Value;
                EditorGUI.BeginChangeCheck();
                if (mms.ShowFields)
                {
                    var rect = EditorGUILayout.GetControlRect();
                    rect = EditorGUI.PrefixLabel(rect, lbl);
                    const float fieldW = 50f;
                    var minRect = new Rect(rect.x, rect.y, fieldW, rect.height);
                    var sliderRect = new Rect(rect.x + fieldW + 4, rect.y, rect.width - (fieldW + 4) * 2, rect.height);
                    var maxRect = new Rect(rect.xMax - fieldW, rect.y, fieldW, rect.height);
                    v.x = EditorGUI.FloatField(minRect, v.x);
                    EditorGUI.MinMaxSlider(sliderRect, ref v.x, ref v.y, lo, hi);
                    v.y = EditorGUI.FloatField(maxRect, v.y);
                }
                else
                {
                    var rect = EditorGUILayout.GetControlRect();
                    EditorGUI.MinMaxSlider(rect, lbl, ref v.x, ref v.y, lo, hi);
                }
                if (EditorGUI.EndChangeCheck())
                {
                    v.x = Mathf.Clamp(v.x, lo, Mathf.Min(v.y, hi));
                    v.y = Mathf.Clamp(v.y, Mathf.Max(v.x, lo), hi);
                    prop.vector2Value = v;
                    Commit(e, target);
                }
                return true;
            }

            if (prop.propertyType == SerializedPropertyType.Vector2Int)
            {
                var v = prop.vector2IntValue;
                float fx = v.x, fy = v.y;
                EditorGUI.BeginChangeCheck();
                var rect = EditorGUILayout.GetControlRect();
                if (mms.ShowFields)
                {
                    rect = EditorGUI.PrefixLabel(rect, lbl);
                    const float fieldW = 50f;
                    var minRect = new Rect(rect.x, rect.y, fieldW, rect.height);
                    var sliderRect = new Rect(rect.x + fieldW + 4, rect.y, rect.width - (fieldW + 4) * 2, rect.height);
                    var maxRect = new Rect(rect.xMax - fieldW, rect.y, fieldW, rect.height);
                    fx = EditorGUI.IntField(minRect, (int)fx);
                    EditorGUI.MinMaxSlider(sliderRect, ref fx, ref fy, lo, hi);
                    fy = EditorGUI.IntField(maxRect, (int)fy);
                }
                else EditorGUI.MinMaxSlider(rect, lbl, ref fx, ref fy, lo, hi);
                if (EditorGUI.EndChangeCheck())
                {
                    prop.vector2IntValue = new Vector2Int(
                        Mathf.RoundToInt(Mathf.Clamp(fx, lo, fy)),
                        Mathf.RoundToInt(Mathf.Clamp(fy, fx, hi)));
                    Commit(e, target);
                }
                return true;
            }

            return false;
        }

        private static bool TryDrawProgressBar(InspectorEntry e, object target, ProgressBarAttribute pb)
        {
            var prop = e.Property;
            double value;
            bool isInt;
            switch (prop.propertyType)
            {
                case SerializedPropertyType.Integer: value = prop.intValue; isInt = true; break;
                case SerializedPropertyType.Float: value = prop.floatValue; isInt = false; break;
                default: return false;
            }

            double min = ResolveNumber(target, pb.MinGetter, pb.Min);
            double max = ResolveNumber(target, pb.MaxGetter, pb.Max);
            if (max <= min) max = min + 1;

            Color fill = new Color(pb.R, pb.G, pb.B);
            if (!string.IsNullOrEmpty(pb.ColorGetter))
            {
                var cv = InspectorMemberResolver.GetValue(target, pb.ColorGetter, out bool cf);
                if (!cf && cv is Color c) fill = c;
            }
            Color back = new Color(0.16f, 0.16f, 0.16f);
            if (!string.IsNullOrEmpty(pb.BackgroundColorGetter))
            {
                var bv = InspectorMemberResolver.GetValue(target, pb.BackgroundColorGetter, out bool bf);
                if (!bf && bv is Color bc) back = bc;
            }

            var lbl = GetLabel(e, target) ?? new GUIContent(prop.displayName);
            float height = Mathf.Max(EditorGUIUtility.singleLineHeight, pb.Height);
            var rect = EditorGUILayout.GetControlRect(false, height);
            rect = EditorGUI.PrefixLabel(rect, lbl);
            var barRect = new Rect(rect.x, rect.y + (rect.height - pb.Height) * 0.5f, rect.width, pb.Height);

            // Interactive: drag sets the value (the progress bar is editable).
            int id = GUIUtility.GetControlID(FocusType.Passive);
            var evt = Event.current;
            bool changed = false;
            if (GUI.enabled)
            {
                switch (evt.GetTypeForControl(id))
                {
                    case EventType.MouseDown:
                        if (barRect.Contains(evt.mousePosition)) { GUIUtility.hotControl = id; changed = true; evt.Use(); }
                        break;
                    case EventType.MouseDrag:
                        if (GUIUtility.hotControl == id) { changed = true; evt.Use(); }
                        break;
                    case EventType.MouseUp:
                        if (GUIUtility.hotControl == id) { GUIUtility.hotControl = 0; evt.Use(); }
                        break;
                }
                if (changed)
                {
                    double t = Mathf.Clamp01((evt.mousePosition.x - barRect.x) / Mathf.Max(1f, barRect.width));
                    value = min + t * (max - min);
                    if (isInt) prop.intValue = (int)Math.Round(value);
                    else prop.floatValue = (float)value;
                    Commit(e, target);
                }
            }

            float frac = Mathf.Clamp01((float)((value - min) / (max - min)));
            EditorGUI.DrawRect(barRect, back);
            if (pb.Segmented)
            {
                int segments = Mathf.Max(1, (int)Math.Round(max - min));
                float segW = barRect.width / segments;
                int filled = (int)Math.Round((value - min));
                for (int i = 0; i < filled; i++)
                    EditorGUI.DrawRect(new Rect(barRect.x + i * segW + 1, barRect.y + 1, segW - 2, barRect.height - 2), fill);
            }
            else
            {
                EditorGUI.DrawRect(new Rect(barRect.x, barRect.y, barRect.width * frac, barRect.height), fill);
            }

            if (pb.DrawValueLabel)
            {
                string text = null;
                if (!string.IsNullOrEmpty(pb.CustomValueStringGetter))
                    text = InspectorMemberResolver.ResolveString(target, pb.CustomValueStringGetter);
                if (string.IsNullOrEmpty(text))
                    text = isInt ? $"{(int)value}/{(int)max}" : $"{value:0.##}";
                var style = new GUIStyle(EditorStyles.miniLabel)
                {
                    alignment = pb.ValueLabelAlignment == TextAlignment.Left ? TextAnchor.MiddleLeft
                        : pb.ValueLabelAlignment == TextAlignment.Right ? TextAnchor.MiddleRight : TextAnchor.MiddleCenter,
                    normal = { textColor = Color.white },
                };
                GUI.Label(barRect, text, style);
            }
            return true;
        }

        private static bool TryDrawEnumToggleButtons(InspectorEntry e, object target)
        {
            var enumType = e.Field?.FieldType;
            if (enumType == null || !enumType.IsEnum) return false;

            var prop = e.Property;
            var lbl = GetLabel(e, target) ?? new GUIContent(prop.displayName);
            var names = Enum.GetNames(enumType);
            var values = (Array)Enum.GetValues(enumType);
            bool flags = enumType.GetCustomAttribute<FlagsAttribute>() != null;

            var rect = EditorGUILayout.GetControlRect();
            rect = EditorGUI.PrefixLabel(rect, lbl);

            if (!flags)
            {
                int current = prop.intValue;
                int sel = -1;
                for (int i = 0; i < values.Length; i++)
                    if (Convert.ToInt32(values.GetValue(i)) == current) { sel = i; break; }
                EditorGUI.BeginChangeCheck();
                int next = GUI.Toolbar(rect, sel, names);
                if (EditorGUI.EndChangeCheck() && next >= 0)
                {
                    prop.intValue = Convert.ToInt32(values.GetValue(next));
                    Commit(e, target);
                }
                return true;
            }

            // [Flags]: one toggle button per non-zero bit value; zero-valued name acts as "clear".
            int cur = prop.intValue;
            float bw = rect.width / names.Length;
            EditorGUI.BeginChangeCheck();
            int result = cur;
            for (int i = 0; i < names.Length; i++)
            {
                int v = Convert.ToInt32(values.GetValue(i));
                var brect = new Rect(rect.x + i * bw, rect.y, bw, rect.height);
                bool onNow = v == 0 ? result == 0 : (result & v) == v;
                bool onNew = GUI.Toggle(brect, onNow, names[i], EditorStyles.miniButtonMid);
                if (onNew == onNow) continue;
                if (v == 0) result = 0;
                else if (onNew) result |= v;
                else result &= ~v;
            }
            if (EditorGUI.EndChangeCheck() && result != cur)
            {
                prop.intValue = result;
                Commit(e, target);
            }
            return true;
        }

        internal static double ResolveNumber(object target, string getter, double fallback)
        {
            if (string.IsNullOrEmpty(getter)) return fallback;
            var v = InspectorMemberResolver.GetValue(target, getter, out bool failed);
            if (failed || v == null) return fallback;
            try { return Convert.ToDouble(v); } catch { return fallback; }
        }

        internal static object ReadProperty(SerializedProperty p)
        {
            switch (p.propertyType)
            {
                case SerializedPropertyType.String: return p.stringValue;
                case SerializedPropertyType.ObjectReference: return p.objectReferenceValue;
                case SerializedPropertyType.Integer: return p.intValue;
                case SerializedPropertyType.Float: return p.floatValue;
                case SerializedPropertyType.Boolean: return p.boolValue;
                case SerializedPropertyType.Enum: return p.intValue;
                default:
                    try { return p.boxedValue; } catch { return null; }
            }
        }

        internal static void WriteProperty(SerializedProperty p, object value)
        {
            switch (p.propertyType)
            {
                case SerializedPropertyType.String: p.stringValue = value as string ?? value?.ToString() ?? string.Empty; break;
                case SerializedPropertyType.ObjectReference: p.objectReferenceValue = value as UnityEngine.Object; break;
                case SerializedPropertyType.Integer: p.intValue = ToInt(value); break;
                case SerializedPropertyType.Enum: p.intValue = ToInt(value); break;
                case SerializedPropertyType.Float: try { p.floatValue = Convert.ToSingle(value); } catch { } break;
                case SerializedPropertyType.Boolean: p.boolValue = value is bool b && b; break;
                default: try { p.boxedValue = value; } catch { } break;
            }
        }

        private static int ToInt(object v)
        {
            if (v == null) return 0;
            try { return Convert.ToInt32(v); } catch { return 0; }
        }

        internal static Type GetElementType(Type fieldType)
        {
            if (fieldType == null) return null;
            if (fieldType.IsArray) return fieldType.GetElementType();
            if (fieldType.IsGenericType && fieldType.GetGenericTypeDefinition() == typeof(List<>))
                return fieldType.GetGenericArguments()[0];
            return null;
        }

        // [ShowInInspector] members: editable when writable (fields and set-able properties);
        // complex values recurse through the POCO inspector (property-tree style).
        private static void RenderShown(InspectorEntry e, object target)
        {
            PocoInspector.DrawSingleMember(target, e.Member);
        }

        // ---------------------------------------------------------------- buttons

        private sealed class ButtonParamState
        {
            public object[] Values;
            public object LastResult;
            public bool HasResult;
        }

        private static readonly Dictionary<string, ButtonParamState> s_buttonParams = new Dictionary<string, ButtonParamState>();

        internal static float ButtonHeight(ButtonAttribute b)
        {
            if (b.ButtonHeightPixels > 0) return b.ButtonHeightPixels;
            return b.ButtonHeight switch
            {
                ButtonSizes.Small => 18f,
                ButtonSizes.Medium => 24f,
                ButtonSizes.Large => 34f,
                ButtonSizes.Gigantic => 46f,
                _ => 20f,
            };
        }

        private static void RenderButton(InspectorEntry e, object target)
        {
            string label = e.Button.Name;
            if (string.IsNullOrEmpty(label)) label = ObjectNames.NicifyVariableName(e.ButtonMethod.Name);
            else label = InspectorMemberResolver.ResolveString(target, label);

            float height = ButtonHeight(e.Button);
            var content = MakeButtonContent(label, e.Button.Icon);
            var ps = e.ButtonMethod.GetParameters();

            if (ps.Length == 0)
            {
                if (DrawAlignedButton(e.Button, content, height))
                    InvokeButton(e, target, null);
            }
            else if (e.Button.DisplayParameters)
            {
                DrawParameterizedButton(e, target, content, height, ps);
            }
            else
            {
                using (new EditorGUI.DisabledScope(true))
                    DrawAlignedButton(e.Button, content, height);
            }

            // [Button(DrawResult = true)]: show the last returned value.
            string key = ButtonKey(e, target);
            if (e.Button.DrawResult && e.ButtonMethod.ReturnType != typeof(void)
                && s_buttonParams.TryGetValue(key, out var st) && st.HasResult)
            {
                using (new EditorGUI.DisabledScope(true))
                    EditorGUILayout.LabelField("Result", st.LastResult?.ToString() ?? "null");
            }
        }

        private static bool DrawAlignedButton(ButtonAttribute b, GUIContent content, float height)
        {
            if (b.Stretch && b.ButtonAlignment == ButtonAlignment.Stretch)
                return GUILayout.Button(content, GUILayout.Height(height));

            EditorGUILayout.BeginHorizontal();
            if (b.ButtonAlignment != ButtonAlignment.Left) GUILayout.FlexibleSpace();
            bool clicked = GUILayout.Button(content, GUILayout.Height(height), GUILayout.ExpandWidth(false));
            if (b.ButtonAlignment != ButtonAlignment.Right) GUILayout.FlexibleSpace();
            EditorGUILayout.EndHorizontal();
            return clicked;
        }

        private static string ButtonKey(InspectorEntry e, object target)
            => (target?.GetHashCode() ?? 0) + ":" + e.ButtonMethod.DeclaringType?.FullName + "." + e.ButtonMethod.Name;

        // Parameterized [Button]: a box with one field per parameter + an invoke button.
        private static void DrawParameterizedButton(InspectorEntry e, object target, GUIContent content, float height, ParameterInfo[] ps)
        {
            string key = ButtonKey(e, target);
            if (!s_buttonParams.TryGetValue(key, out var st) || st.Values == null || st.Values.Length != ps.Length)
            {
                st = new ButtonParamState { Values = new object[ps.Length] };
                for (int i = 0; i < ps.Length; i++)
                    st.Values[i] = ps[i].HasDefaultValue ? ps[i].DefaultValue : DefaultOf(ps[i].ParameterType);
                s_buttonParams[key] = st;
            }

            EditorGUILayout.BeginVertical(EditorStyles.helpBox);
            EditorGUILayout.LabelField(content, EditorStyles.boldLabel);
            for (int i = 0; i < ps.Length; i++)
            {
                var lbl = new GUIContent(ObjectNames.NicifyVariableName(ps[i].Name));
                st.Values[i] = PocoInspector.DrawTypedFieldPublic(lbl, ps[i].ParameterType, st.Values[i]);
            }
            if (GUILayout.Button("Invoke", GUILayout.Height(Mathf.Min(height, 24f))))
                InvokeButton(e, target, st.Values);
            EditorGUILayout.EndVertical();
        }

        private static object DefaultOf(Type t) => t.IsValueType ? Activator.CreateInstance(t) : null;

        private static void InvokeButton(InspectorEntry e, object target, object[] args)
        {
            try
            {
                object result = e.ButtonMethod.Invoke(e.ButtonMethod.IsStatic ? null : target, args);
                if (e.ButtonMethod.ReturnType != typeof(void))
                {
                    string key = ButtonKey(e, target);
                    if (!s_buttonParams.TryGetValue(key, out var st)) s_buttonParams[key] = st = new ButtonParamState();
                    st.LastResult = result;
                    st.HasResult = true;
                }
            }
            catch (Exception ex)
            {
                Debug.LogError($"[Framework.Inspector] Button '{e.ButtonMethod.Name}' threw: {ex.InnerException?.Message ?? ex.Message}");
            }
            if (e.Button.DirtyOnClick && target is UnityEngine.Object uo) EditorUtility.SetDirty(uo);
        }

        // ---------------------------------------------------------------- metadata

        private static void ApplyMemberMetadata(InspectorEntry e, MemberInfo m, object target)
        {
            if (m == null) return;

            var order = m.GetCustomAttribute<PropertyOrderAttribute>();
            if (order != null) e.Order = order.Order;

            var space = m.GetCustomAttribute<PropertySpaceAttribute>();
            if (space != null) { e.SpaceBefore = space.SpaceBefore; e.SpaceAfter = space.SpaceAfter; }
        }

        internal static bool IsVisible(MemberInfo m, object target)
        {
            if (m.GetCustomAttribute<HideInEditorModeAttribute>() != null && !Application.isPlaying) return false;
            if (m.GetCustomAttribute<HideInPlayModeAttribute>() != null && Application.isPlaying) return false;
            if (m.GetCustomAttribute<ShowInPlayModeAttribute>() != null && !Application.isPlaying) return false;

            foreach (var s in m.GetCustomAttributes<ShowIfAttribute>())
                if (!InspectorMemberResolver.EvaluateBool(target, s.Condition, s.Value, s.HasValue, true)) return false;
            foreach (var h in m.GetCustomAttributes<HideIfAttribute>())
                if (InspectorMemberResolver.EvaluateBool(target, h.Condition, h.Value, h.HasValue, false)) return false;
            return true;
        }

        private static bool IsEnabled(MemberInfo m, object target)
        {
            if (m.GetCustomAttribute<ReadOnlyAttribute>() != null) return false;
            if (m.GetCustomAttribute<DisableInEditorModeAttribute>() != null && !Application.isPlaying) return false;
            if (m.GetCustomAttribute<DisableInPlayModeAttribute>() != null && Application.isPlaying) return false;

            foreach (var en in m.GetCustomAttributes<EnableIfAttribute>())
                if (!InspectorMemberResolver.EvaluateBool(target, en.Condition, en.Value, en.HasValue, true)) return false;
            foreach (var di in m.GetCustomAttributes<DisableIfAttribute>())
                if (InspectorMemberResolver.EvaluateBool(target, di.Condition, di.Value, di.HasValue, false)) return false;
            return true;
        }

        private static bool TryGetGuiColor(MemberInfo m, object target, out Color color)
        {
            color = Color.white;
            var attr = m.GetCustomAttribute<GUIColorAttribute>();
            if (attr == null) return false;
            if (!string.IsNullOrEmpty(attr.GetColor))
            {
                var v = InspectorMemberResolver.GetValue(target, attr.GetColor, out bool failed);
                if (!failed && v is Color c) { color = c; return true; }
                // "#RRGGBB(AA)" / named HTML color literals.
                if (ColorUtility.TryParseHtmlString(attr.GetColor, out var html)) { color = html; return true; }
                return false; // unresolved → skip tint
            }
            color = new Color(attr.R, attr.G, attr.B, attr.A);
            return true;
        }

        // ---------------------------------------------------------------- validation (drawn above the field)

        private static void RenderValidation(InspectorEntry e, object target)
        {
            var src = e.AttributeSource;

            var req = src.GetCustomAttribute<RequiredAttribute>();
            if (req != null && e.Property != null && IsEmptyRef(e.Property))
            {
                string msg = req.ErrorMessage != null
                    ? InspectorMemberResolver.ResolveString(target, req.ErrorMessage)
                    : $"{GetLabelText(e, target) ?? e.Property.displayName} is required.";
                EditorGUILayout.HelpBox(msg, ToMsgType(req.MessageType));
            }

            foreach (var v in src.GetCustomAttributes<ValidateInputAttribute>())
            {
                if (!RunValidator(v, target, e, out bool ok, out string message, out InfoMessageType msgType)) continue;
                if (!ok)
                {
                    string msg = message ?? (v.DefaultMessage != null
                        ? InspectorMemberResolver.ResolveString(target, v.DefaultMessage)
                        : "Invalid value.");
                    EditorGUILayout.HelpBox(msg, ToMsgType(msgType));
                }
            }
        }

        // Validator signatures, most-specific first:
        //   bool M(T value, ref string errorMessage, ref InfoMessageType messageType)
        //   bool M(T value, ref string errorMessage)
        //   bool M(T value)
        //   bool M()
        private static bool RunValidator(ValidateInputAttribute v, object target, InspectorEntry e,
            out bool ok, out string message, out InfoMessageType msgType)
        {
            ok = true;
            message = null;
            msgType = v.MessageType;

            object value = null;
            if (e.Field != null) value = SafeGet(e.Field, target);
            else if (e.Property != null) value = ReadProperty(e.Property);

            foreach (var mi in InspectorMemberResolver.FindMethods(target.GetType(), v.Condition))
            {
                if (mi.ReturnType != typeof(bool)) continue;
                var ps = mi.GetParameters();
                try
                {
                    object self = mi.IsStatic ? null : target;
                    switch (ps.Length)
                    {
                        case 0:
                            ok = (bool)mi.Invoke(self, null);
                            return true;
                        case 1:
                            if (!ParamAccepts(ps[0], value)) continue;
                            ok = (bool)mi.Invoke(self, new[] { CoerceParam(ps[0], value) });
                            return true;
                        case 2:
                            if (!ParamAccepts(ps[0], value) || !ps[1].ParameterType.IsByRef) continue;
                            {
                                var args = new[] { CoerceParam(ps[0], value), (object)message };
                                ok = (bool)mi.Invoke(self, args);
                                message = args[1] as string;
                                return true;
                            }
                        case 3:
                            if (!ParamAccepts(ps[0], value) || !ps[1].ParameterType.IsByRef || !ps[2].ParameterType.IsByRef) continue;
                            {
                                var args = new[] { CoerceParam(ps[0], value), (object)message, (object)msgType };
                                ok = (bool)mi.Invoke(self, args);
                                message = args[1] as string;
                                if (args[2] is InfoMessageType imt) msgType = imt;
                                return true;
                            }
                    }
                }
                catch { }
            }
            return false;
        }

        private static bool ParamAccepts(ParameterInfo p, object value)
        {
            var pt = p.ParameterType;
            if (pt.IsByRef) return false;
            if (value == null) return !pt.IsValueType;
            return pt.IsAssignableFrom(value.GetType())
                || (pt.IsEnum && value is int); // enum props read back as int
        }

        // Reflection Invoke does not convert int → enum; box the right type up front.
        private static object CoerceParam(ParameterInfo p, object value)
        {
            var pt = p.ParameterType;
            if (value != null && pt.IsEnum && !pt.IsInstanceOfType(value))
                try { return Enum.ToObject(pt, value); } catch { }
            return value;
        }

        // ---------------------------------------------------------------- helpers

        internal static void InvokeOnValueChanged(InspectorEntry e, object target)
        {
            var src = e.AttributeSource;
            if (src == null) return;
            bool any = false;
            foreach (var attr in src.GetCustomAttributes<OnValueChangedAttribute>())
            {
                InvokeChangeAction(e, target, attr);
                any = true;
            }
            if (any && target is UnityEngine.Object uo) EditorUtility.SetDirty(uo);
        }

        // Supports both callback shapes: M() and M(T newValue).
        private static void InvokeChangeAction(InspectorEntry e, object target, OnValueChangedAttribute attr)
        {
            try
            {
                var mi = InspectorMemberResolver.FindMethod(target.GetType(), attr.Action, Type.EmptyTypes);
                if (mi != null) { mi.Invoke(mi.IsStatic ? null : target, null); return; }

                object value = e.Field != null ? SafeGet(e.Field, target)
                    : e.Property != null ? ReadProperty(e.Property) : null;
                foreach (var cand in InspectorMemberResolver.FindMethods(target.GetType(), attr.Action))
                {
                    var ps = cand.GetParameters();
                    if (ps.Length == 1 && ParamAccepts(ps[0], value))
                    {
                        cand.Invoke(cand.IsStatic ? null : target, new[] { CoerceParam(ps[0], value) });
                        return;
                    }
                }
            }
            catch (Exception ex) { Debug.LogWarning($"[Framework.Inspector] OnValueChanged '{attr.Action}' threw: {ex.InnerException?.Message ?? ex.Message}"); }
        }

        internal static GUIContent GetLabel(InspectorEntry e, object target)
        {
            if (e.AttributeSource?.GetCustomAttribute<HideLabelAttribute>() != null)
                return GUIContent.none;
            string text = GetLabelText(e, target);
            if (text != null) return new GUIContent(text);
            return null; // let PropertyField use its default
        }

        internal static string GetLabelText(InspectorEntry e, object target)
        {
            var lt = e.AttributeSource?.GetCustomAttribute<LabelTextAttribute>();
            if (lt == null) return null;
            string text = InspectorMemberResolver.ResolveString(target, lt.Text);
            if (lt.NicifyText && !string.IsNullOrEmpty(text)) text = ObjectNames.NicifyVariableName(text);
            return text;
        }

        private static bool IsEmptyRef(SerializedProperty p)
        {
            switch (p.propertyType)
            {
                case SerializedPropertyType.ObjectReference: return p.objectReferenceValue == null;
                case SerializedPropertyType.String: return string.IsNullOrEmpty(p.stringValue);
                case SerializedPropertyType.ExposedReference: return p.exposedReferenceValue == null;
                default: return false;
            }
        }

        internal static MessageType ToMsgType(InfoMessageType t) => t switch
        {
            InfoMessageType.Warning => MessageType.Warning,
            InfoMessageType.Error => MessageType.Error,
            InfoMessageType.Info => MessageType.Info,
            _ => MessageType.None,
        };

        // True when Unity has a registered CustomPropertyDrawer for the type — such fields MUST render
        // through PropertyField (their own drawer), never the InlineProperty recursion. Built once from
        // TypeCache by reading each PropertyDrawer's [CustomPropertyDrawer] target (stable field names,
        // version-independent). Honors useForChildren (drawer applies to subclasses).
        private static HashSet<Type> s_drawnExact;
        private static HashSet<Type> s_drawnForChildren;
        private static readonly Dictionary<Type, bool> s_hasDrawer = new Dictionary<Type, bool>();

        private static void EnsureDrawerMap()
        {
            if (s_drawnExact != null) return;
            s_drawnExact = new HashSet<Type>();
            s_drawnForChildren = new HashSet<Type>();
            try
            {
                var cpd = typeof(CustomPropertyDrawer);
                var fType = cpd.GetField("m_Type", BindingFlags.Instance | BindingFlags.NonPublic);
                var fChildren = cpd.GetField("m_UseForChildren", BindingFlags.Instance | BindingFlags.NonPublic);
                foreach (var drawer in TypeCache.GetTypesDerivedFrom<PropertyDrawer>())
                {
                    foreach (var attr in drawer.GetCustomAttributes(typeof(CustomPropertyDrawer), true))
                    {
                        var target = fType?.GetValue(attr) as Type;
                        if (target == null) continue;
                        s_drawnExact.Add(target);
                        if (fChildren != null && (bool)fChildren.GetValue(attr)) s_drawnForChildren.Add(target);
                    }
                }
            }
            catch { /* leave maps empty → HasCustomPropertyDrawer returns false */ }
        }

        internal static bool HasCustomPropertyDrawer(Type t)
        {
            if (t == null) return false;
            if (s_hasDrawer.TryGetValue(t, out bool cached)) return cached;
            EnsureDrawerMap();
            bool result = s_drawnExact.Contains(t);
            if (!result)
                foreach (var b in s_drawnForChildren)
                    if (b.IsAssignableFrom(t)) { result = true; break; }
            s_hasDrawer[t] = result;
            return result;
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

        // GetMembers on a derived type omits PRIVATE members of base classes (e.g. fields/methods of a
        // generic base like FragmentData<,>). Walk the hierarchy DeclaredOnly, most-derived first,
        // deduping by name so overrides/shadows only surface once.
        private const BindingFlags DeclaredFlags =
            BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.DeclaredOnly;

        internal static IEnumerable<FieldInfo> AllFields(Type type)
        {
            var seen = new HashSet<string>();
            for (var t = type; t != null && t != typeof(object); t = t.BaseType)
                foreach (var f in t.GetFields(DeclaredFlags))
                    if (seen.Add(f.Name)) yield return f;
        }

        internal static IEnumerable<PropertyInfo> AllProperties(Type type)
        {
            var seen = new HashSet<string>();
            for (var t = type; t != null && t != typeof(object); t = t.BaseType)
                foreach (var p in t.GetProperties(DeclaredFlags))
                    if (seen.Add(p.Name)) yield return p;
        }

        internal static IEnumerable<MethodInfo> AllMethods(Type type)
        {
            var seen = new HashSet<string>();
            for (var t = type; t != null && t != typeof(object); t = t.BaseType)
                foreach (var m in t.GetMethods(DeclaredFlags))
                    if (!m.IsSpecialName && seen.Add(m.Name + "#" + m.GetParameters().Length))
                        yield return m;
        }

        private static IEnumerable<MemberInfo> EnumerateShowInInspector(Type type)
        {
            foreach (var f in AllFields(type))
                if (f.GetCustomAttribute<ShowInInspectorAttribute>() != null && !IsUnitySerialized(f))
                    yield return f;
            foreach (var p in AllProperties(type))
                if (p.GetCustomAttribute<ShowInInspectorAttribute>() != null)
                    yield return p;
        }

        private static bool IsUnitySerialized(FieldInfo f)
        {
            if (f.IsStatic) return false;
            if (f.GetCustomAttribute<NonSerializedAttribute>() != null) return false;
            bool serializable = f.IsPublic || f.GetCustomAttribute<SerializeField>() != null;
            return serializable;
        }
    }
}
#endif
