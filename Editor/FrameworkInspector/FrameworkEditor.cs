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
    /// Base inspector that renders the <see cref="FoundationPlatform.FrameworkInspector"/> attributes through the
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
            // Missing script: the engine can't draw a null target — hand over to the fixer.
            if (target == null && InspectorXSettings.instance.missingScriptFixer)
            {
                MissingScriptFixer.OnGUI(serializedObject);
                serializedObject.ApplyModifiedProperties();
                return;
            }
            // Last-resort net: the GUI.Scope conversions inside FrameworkInspectorRenderer keep the
            // layout stack balanced per-element, but this still catches non-GUI failures (metadata
            // building, tree cloning, etc.) so one broken type degrades to the default inspector
            // instead of leaving every inspector window broken until a domain reload.
            try
            {
                FrameworkInspectorRenderer.Draw(this, serializedObject, targets, _foldouts, _tabs);
            }
            catch (ExitGUIException)
            {
                throw;
            }
            catch (Exception ex)
            {
                Debug.LogError($"[FoundationPlatform.FrameworkInspector] inspector draw for '{target?.GetType().Name}' failed: {ex}");
                DrawDefaultInspector();
            }
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
        public MemberMetadata Metadata;
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

        private static Dictionary<Type, TypeMetadata> s_typeCache = new Dictionary<Type, TypeMetadata>();

        [InitializeOnLoadMethod]
        public static void ClearCache()
        {
            s_typeCache?.Clear();
            s_initDone.Clear();
            InspectorMemberResolver.ClearCache();
            FrameworkInspectorTheme.InvalidateSkinCache();
        }

        [MenuItem("CONTEXT/Component/Force Rebuild Framework Inspector Cache")]
        private static void ForceRebuildCache(MenuCommand command)
        {
            ClearCache();
            Debug.Log("[FoundationPlatform.FrameworkInspector] Cache cleared successfully.");
        }

        private static readonly GUIContent s_tempContent = new GUIContent();
        internal static GUIContent TempContent(string text, string tooltip = null)
        {
            s_tempContent.text = text;
            s_tempContent.tooltip = tooltip;
            return s_tempContent;
        }

        internal static TypeMetadata GetOrCreateMetadata(Type type)
        {
            if (s_typeCache == null)
            {
                s_typeCache = new Dictionary<Type, TypeMetadata>();
            }
            if (!s_typeCache.TryGetValue(type, out var meta))
            {
                meta = BuildTypeMetadata(type);
                s_typeCache[type] = meta;
            }
            return meta;
        }

        private static TypeMetadata BuildTypeMetadata(Type type)
        {
            var meta = new TypeMetadata { Type = type };

            // 1. Build serialized fields map
            foreach (var f in AllFields(type))
            {
                var mm = BuildMemberMetadata(f);
                meta.SerializedFieldMap[f.Name] = mm;
            }

            // 2. Build shown members (reflected [ShowInInspector] fields/properties)
            var shownList = new List<MemberMetadata>();
            foreach (var m in EnumerateShowInInspector(type))
            {
                shownList.Add(BuildMemberMetadata(m));
            }
            meta.ShownMembers = shownList.ToArray();

            // 3. Build buttons and inspector guis
            var buttonsList = new List<MemberMetadata>();
            var guisList = new List<MemberMetadata>();
            foreach (var mi in AllMethods(type))
            {
                var btn = mi.GetCustomAttribute<ButtonAttribute>();
                if (btn != null)
                {
                    buttonsList.Add(BuildMemberMetadata(mi, btn));
                }
                else if (mi.GetCustomAttribute<OnInspectorGUIAttribute>() != null && mi.GetParameters().Length == 0)
                {
                    guisList.Add(BuildMemberMetadata(mi));
                }
            }
            meta.Buttons = buttonsList.ToArray();
            meta.InspectorGuis = guisList.ToArray();

            // 4. Cache type info boxes
            var boxes = type.GetCustomAttributes<TypeInfoBoxAttribute>(true);
            meta.TypeInfoBoxes = new List<TypeInfoBoxAttribute>(boxes).ToArray();

            // 5. Pre-build GroupNode tree template (Phase 2a)
            meta.GroupTreeTemplate = BuildGroupTreeTemplate(meta);

            return meta;
        }

        private static MemberMetadata BuildMemberMetadata(MemberInfo m, ButtonAttribute btnAttr = null)
        {
            var mm = new MemberMetadata
            {
                Member = m,
                Name = m.Name,
                Field = m as FieldInfo,
                FieldType = (m is FieldInfo fi) ? fi.FieldType : ((m is PropertyInfo pi) ? pi.PropertyType : null)
            };

            // Group attributes
            var groupAttrs = new List<Attribute>();
            foreach (var attr in m.GetCustomAttributes())
            {
                if (attr is BoxGroupAttribute || attr is FoldoutGroupAttribute || 
                    attr is TitleGroupAttribute || attr is TabGroupAttribute || 
                    attr is HorizontalGroupAttribute || attr is VerticalGroupAttribute || 
                    attr is ToggleGroupAttribute || attr is ButtonGroupAttribute)
                {
                    groupAttrs.Add(attr);
                }
            }
            mm.GroupAttributes = groupAttrs.ToArray();

            // Spacing
            var space = m.GetCustomAttribute<PropertySpaceAttribute>();
            if (space != null)
            {
                mm.SpaceBefore = space.SpaceBefore;
                mm.SpaceAfter = space.SpaceAfter;
            }
            else
            {
                var unitySpace = m.GetCustomAttribute<SpaceAttribute>();
                if (unitySpace != null) mm.SpaceBefore = unitySpace.height;
            }

            // Ordering
            var order = m.GetCustomAttribute<PropertyOrderAttribute>();
            if (order != null) mm.Order = order.Order;

            // Visibility / Enabled
            mm.HideInEditorMode = m.GetCustomAttribute<HideInEditorModeAttribute>() != null;
            mm.HideInPlayMode = m.GetCustomAttribute<HideInPlayModeAttribute>() != null;
            mm.ShowInPlayMode = m.GetCustomAttribute<ShowInPlayModeAttribute>() != null;
            
            var showIfs = m.GetCustomAttributes<ShowIfAttribute>();
            var hideIfs = m.GetCustomAttributes<HideIfAttribute>();
            var enableIfs = m.GetCustomAttributes<EnableIfAttribute>();
            var disableIfs = m.GetCustomAttributes<DisableIfAttribute>();

            mm.ShowIfs = new List<ShowIfAttribute>(showIfs).ToArray();
            mm.HideIfs = new List<HideIfAttribute>(hideIfs).ToArray();
            mm.EnableIfs = new List<EnableIfAttribute>(enableIfs).ToArray();
            mm.DisableIfs = new List<DisableIfAttribute>(disableIfs).ToArray();

            mm.ReadOnly = m.GetCustomAttribute<ReadOnlyAttribute>() != null;
            mm.DisableInEditorMode = m.GetCustomAttribute<DisableInEditorModeAttribute>() != null;
            mm.DisableInPlayMode = m.GetCustomAttribute<DisableInPlayModeAttribute>() != null;

            // Decorators
            var titles = m.GetCustomAttributes<TitleAttribute>();
            var infoBoxes = m.GetCustomAttributes<InfoBoxAttribute>();
            var headers = m.GetCustomAttributes<HeaderAttribute>();
            var detailedInfoBoxes = m.GetCustomAttributes<DetailedInfoBoxAttribute>();

            mm.Titles = new List<TitleAttribute>(titles).ToArray();
            mm.InfoBoxes = new List<InfoBoxAttribute>(infoBoxes).ToArray();
            mm.Headers = new List<HeaderAttribute>(headers).ToArray();
            mm.DetailedInfoBoxes = new List<DetailedInfoBoxAttribute>(detailedInfoBoxes).ToArray();
            mm.InlineButtons = new List<InlineButtonAttribute>(m.GetCustomAttributes<InlineButtonAttribute>()).ToArray();
            mm.RequireComponentButton = m.GetCustomAttribute<RequireComponentButtonAttribute>();

            // Validation
            mm.Required = m.GetCustomAttribute<RequiredAttribute>();
            var validateInputs = m.GetCustomAttributes<ValidateInputAttribute>();
            mm.ValidateInputs = new List<ValidateInputAttribute>(validateInputs).ToArray();

            // Color
            mm.GUIColor = m.GetCustomAttribute<GUIColorAttribute>();

            // Hooks
            var initHooks = m.GetCustomAttributes<OnInspectorInitAttribute>();
            var valChanges = m.GetCustomAttributes<OnValueChangedAttribute>();

            mm.InitHooks = new List<OnInspectorInitAttribute>(initHooks).ToArray();
            mm.ValueChangedHooks = new List<OnValueChangedAttribute>(valChanges).ToArray();

            // Drawing attributes
            mm.DrawWithUnity = m.GetCustomAttribute<DrawWithUnityAttribute>();
            mm.TableList = m.GetCustomAttribute<TableListAttribute>();
            mm.ListDrawerSettings = m.GetCustomAttribute<ListDrawerSettingsAttribute>();
            mm.DictionaryDrawerSettings = m.GetCustomAttribute<DictionaryDrawerSettingsAttribute>();
            mm.Searchable = m.GetCustomAttribute<SearchableAttribute>();
            mm.ValueDropdown = m.GetCustomAttribute<ValueDropdownAttribute>();
            mm.AssetSelector = m.GetCustomAttribute<AssetSelectorAttribute>();
            mm.OnCollectionChanged = m.GetCustomAttribute<OnCollectionChangedAttribute>();
            mm.InlineProperty = m.GetCustomAttribute<InlinePropertyAttribute>() ?? 
                (mm.FieldType != null ? mm.FieldType.GetCustomAttribute<InlinePropertyAttribute>() : null);
            mm.DisplayAsString = m.GetCustomAttribute<DisplayAsStringAttribute>();
            mm.ToggleLeft = m.GetCustomAttribute<ToggleLeftAttribute>();
            mm.MultiLineProperty = m.GetCustomAttribute<MultiLinePropertyAttribute>();
            mm.TextArea = m.GetCustomAttribute<TextAreaAttribute>();
            mm.Multiline = m.GetCustomAttribute<MultilineAttribute>();
            mm.PropertyRange = m.GetCustomAttribute<PropertyRangeAttribute>();
            mm.MinMaxSlider = m.GetCustomAttribute<MinMaxSliderAttribute>();
            mm.ProgressBar = m.GetCustomAttribute<ProgressBarAttribute>();
            mm.EnumToggleButtons = m.GetCustomAttribute<EnumToggleButtonsAttribute>();
            mm.PreviewField = m.GetCustomAttribute<PreviewFieldAttribute>();
            mm.InlineEditor = m.GetCustomAttribute<InlineEditorAttribute>();
            mm.AssetsOnly = m.GetCustomAttribute<AssetsOnlyAttribute>();
            mm.SceneObjectsOnly = m.GetCustomAttribute<SceneObjectsOnlyAttribute>();

            // Label / Tooltip / Layout modifiers
            mm.HideLabel = m.GetCustomAttribute<HideLabelAttribute>() != null;
            mm.Tooltip = m.GetCustomAttribute<TooltipAttribute>();
            mm.LabelText = m.GetCustomAttribute<LabelTextAttribute>();
            mm.Indent = m.GetCustomAttribute<IndentAttribute>();
            mm.LabelWidth = m.GetCustomAttribute<LabelWidthAttribute>();
            mm.OnInspectorGUI = m.GetCustomAttribute<OnInspectorGUIAttribute>();
            mm.OwnHorizontal = m.GetCustomAttribute<HorizontalGroupAttribute>();

            // Button (if method and attribute passed)
            mm.Button = btnAttr;

            // Flags
            if (mm.FieldType != null && mm.FieldType.IsEnum)
            {
                mm.IsFlagsEnum = mm.FieldType.GetCustomAttribute<FlagsAttribute>() != null;
            }

            // Cache GUIContent if label text is completely static
            if (mm.HideLabel)
            {
                mm.CachedLabel = GUIContent.none;
            }
            else if (mm.LabelText != null && !mm.LabelText.Text.StartsWith("$") && !mm.LabelText.Text.StartsWith("@"))
            {
                string text = mm.LabelText.Text;
                string tooltip = mm.Tooltip?.tooltip;
                mm.CachedLabel = new GUIContent(text, tooltip);
            }

            // Cache GUIStyles
            if (mm.DisplayAsString != null)
                mm.DisplayAsStringStyle = FrameworkInspectorTheme.CreateDisplayAsStringStyle(mm.DisplayAsString);

            if (mm.ProgressBar != null)
                mm.ProgressBarStyle = FrameworkInspectorTheme.CreateProgressBarLabelStyle(mm.ProgressBar);

            return mm;
        }

        private static GroupNode BuildGroupTreeTemplate(TypeMetadata meta)
        {
            var root = new GroupNode { Path = string.Empty, Kind = GroupKind.Vertical, KindResolved = true };
            
            var allMembers = new List<MemberMetadata>();
            if (meta.SerializedFieldMap != null) allMembers.AddRange(meta.SerializedFieldMap.Values);
            if (meta.ShownMembers != null) allMembers.AddRange(meta.ShownMembers);
            if (meta.Buttons != null) allMembers.AddRange(meta.Buttons);
            if (meta.InspectorGuis != null) allMembers.AddRange(meta.InspectorGuis);

            int dummySeq = 0;
            foreach (var mm in allMembers)
            {
                string containerPath = ResolveContainerTemplate(root, mm, ref dummySeq);
                mm.ResolvedContainerPath = containerPath;
            }

            return root;
        }

        private static string ResolveContainerTemplate(GroupNode root, MemberMetadata mm, ref int seq)
        {
            var source = mm.Member;
            if (source == null) return string.Empty;

            string containerPath = string.Empty;
            int maxDepth = -1;

            foreach (var attr in mm.GroupAttributes)
            {
                string path = null;
                switch (attr)
                {
                    case BoxGroupAttribute b:
                        path = b.GroupID;
                        RegisterTemplate(root, path, ref seq, n =>
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
                        RegisterTemplate(root, path, ref seq, n =>
                        {
                            n.Kind = GroupKind.Foldout;
                            if (f.Order != 0f) n.Order = f.Order;
                            if (f.HasDefinedExpanded) n.DefaultExpanded = f.Expanded;
                            if (n.FoldoutAttr == null) n.FoldoutAttr = f;
                        });
                        break;
                    case TitleGroupAttribute t:
                        path = t.GroupID;
                        RegisterTemplate(root, path, ref seq, n =>
                        {
                            n.Kind = GroupKind.Title;
                            if (t.Order != 0f) n.Order = t.Order;
                            if (n.TitleAttr == null || t.Subtitle != null || t.Alignment != TitleAlignments.Left
                                || !t.HorizontalLine || !t.BoldTitle || t.Indent)
                                n.TitleAttr = t;
                        });
                        break;
                    case TabGroupAttribute tg:
                        RegisterTemplate(root, tg.GroupID, ref seq, n =>
                        {
                            n.Kind = GroupKind.TabContainer;
                            if (tg.Order != 0f) n.Order = tg.Order;
                            if (n.Tab == null || tg.Paddingless || tg.HideTabGroupIfTabGroupOnlyHasOneTab) n.Tab = tg;
                        });
                        path = tg.GroupID + "/" + tg.TabName;
                        RegisterTemplate(root, path, ref seq, n => { n.Kind = GroupKind.TabPage; });
                        break;
                    case HorizontalGroupAttribute h:
                        path = h.GroupID;
                        RegisterTemplate(root, path, ref seq, n =>
                        {
                            n.Kind = GroupKind.Horizontal;
                            if (h.Order != 0f) n.Order = h.Order;
                            if (n.Horizontal == null || !string.IsNullOrEmpty(h.Title) || h.LabelWidth > 0f) n.Horizontal = h;
                        });
                        break;
                    case VerticalGroupAttribute v:
                        path = v.GroupID;
                        RegisterTemplate(root, path, ref seq, n =>
                        {
                            n.Kind = GroupKind.Vertical;
                            n.KindResolved = true;
                            if (v.Order != 0f) n.Order = v.Order;
                            if (n.Vertical == null || v.PaddingTop > 0f || v.PaddingBottom > 0f) n.Vertical = v;
                        });
                        break;
                    case ToggleGroupAttribute tog:
                        path = tog.ToggleMemberName;
                        RegisterTemplate(root, path, ref seq, n =>
                        {
                            n.Kind = GroupKind.Toggle;
                            if (tog.Order != 0f) n.Order = tog.Order;
                            if (n.Toggle == null || tog.GroupTitle != null) n.Toggle = tog;
                        });
                        break;
                    case ButtonGroupAttribute bg:
                        path = string.IsNullOrEmpty(bg.GroupID) ? "_DefaultButtonGroup" : bg.GroupID;
                        RegisterTemplate(root, path, ref seq, n =>
                        {
                            n.Kind = GroupKind.ButtonRow;
                            if (bg.Order != 0f) n.Order = bg.Order;
                        });
                        break;
                }

                if (string.IsNullOrEmpty(path)) continue;
                int depth = CountSegments(path);
                if (depth > maxDepth) { maxDepth = depth; containerPath = path; }
            }

            return containerPath;
        }

        private static void RegisterTemplate(GroupNode root, string path, ref int seq, Action<GroupNode> configure)
        {
            if (string.IsNullOrEmpty(path)) return;
            var node = GetNode(root, path, seq++);
            configure(node);
            node.KindResolved = true;
        }

        private static readonly Stack<List<InspectorEntry>> s_listPool = new Stack<List<InspectorEntry>>();

        private static List<InspectorEntry> GetPooledList()
        {
            if (s_listPool.Count > 0)
            {
                var l = s_listPool.Pop();
                l.Clear();
                return l;
            }
            return new List<InspectorEntry>();
        }

        private static void ReleasePooledList(List<InspectorEntry> list)
        {
            list.Clear();
            s_listPool.Push(list);
        }

        private static GroupNode CloneGroupNode(GroupNode source, Dictionary<string, GroupNode> flatMap)
        {
            var clone = new GroupNode
            {
                Path = source.Path,
                Name = source.Name,
                Kind = source.Kind,
                KindResolved = source.KindResolved,
                Order = source.Order,
                Sequence = source.Sequence,
                DefaultExpanded = source.DefaultExpanded,
                ShowLabel = source.ShowLabel,
                Box = source.Box,
                FoldoutAttr = source.FoldoutAttr,
                TitleAttr = source.TitleAttr,
                Tab = source.Tab,
                Toggle = source.Toggle,
                Horizontal = source.Horizontal,
                Vertical = source.Vertical
            };
            if (!string.IsNullOrEmpty(clone.Path))
            {
                flatMap[clone.Path] = clone;
            }
            foreach (var child in source.Children)
            {
                if (child is GroupNode childGroup)
                {
                    var childClone = CloneGroupNode(childGroup, flatMap);
                    clone.Children.Add(childClone);
                    clone.SubGroups[childClone.Name] = childClone;
                }
            }
            return clone;
        }

        public static void Draw(UnityEditor.Editor editor, SerializedObject so, object target,
            Dictionary<string, bool> foldouts, Dictionary<string, int> tabs,
            bool drawScriptRow = true, HashSet<string> skipFields = null)
        {
            Draw(editor, so, new[] { target }, foldouts, tabs, drawScriptRow, skipFields);
        }

        public static void Draw(UnityEditor.Editor editor, SerializedObject so, object[] targets,
            Dictionary<string, bool> foldouts, Dictionary<string, int> tabs,
            bool drawScriptRow = true, HashSet<string> skipFields = null)
        {
            Type type = targets[0].GetType();
            var meta = GetOrCreateMetadata(type);

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

            DrawTypeInfoBoxes(meta, targets);

            var entries = GetPooledList();
            int seq = 0;

            // --- Serialized fields (exact serialized set comes from the SerializedObject) ---
            var it = so.GetIterator();
            bool enter = true;
            while (it.NextVisible(enter))
            {
                enter = false;
                if (it.propertyPath == "m_Script") continue;
                if (skipFields != null && skipFields.Contains(it.name)) continue;
                AddFieldEntry(entries, it.Copy(), meta, ref seq);
            }

            AddReflectedEntries(entries, meta, targets, ref seq);
            RenderScope(entries, targets, foldouts, tabs);
            ReleasePooledList(entries);
        }

        private static void DrawTypeInfoBoxes(TypeMetadata meta, object[] targets)
        {
            if (meta.TypeInfoBoxes != null)
            {
                foreach (var box in meta.TypeInfoBoxes)
                    FrameworkInspectorTheme.DrawInfoBox(InspectorMemberResolver.ResolveString(targets[0], box.Message), InfoMessageType.Info);
            }
        }

        // Build + group + render a set of entries against a target instance. Shared by the root
        // SerializedObject draw and nested [InlineProperty] objects (which drive it off child props).
        private static void RenderScope(List<InspectorEntry> entries, object[] targets,
            Dictionary<string, bool> foldouts, Dictionary<string, int> tabs)
        {
            entries.Sort((a, b) => a.Order != b.Order ? a.Order.CompareTo(b.Order) : a.Sequence.CompareTo(b.Sequence));

            var type = targets[0].GetType();
            var meta = GetOrCreateMetadata(type);

            var flatMap = new Dictionary<string, GroupNode>();
            var root = CloneGroupNode(meta.GroupTreeTemplate, flatMap);

            foreach (var e in entries)
            {
                var mm = e.Metadata;
                string path = mm?.ResolvedContainerPath;
                if (!string.IsNullOrEmpty(path) && flatMap.TryGetValue(path, out var node))
                {
                    node.Children.Add(e);
                }
                else
                {
                    root.Children.Add(e);
                }
            }

            SortGroupChildren(root);
            RenderChildren(root, targets, foldouts, tabs);
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

        private static void AddFieldEntry(List<InspectorEntry> entries, SerializedProperty prop, TypeMetadata meta, ref int seq)
        {
            if (meta.SerializedFieldMap.TryGetValue(prop.name, out var mm))
            {
                var e = new InspectorEntry
                {
                    EntryKind = InspectorEntry.Kind.Field,
                    Property = prop,
                    Field = mm.Field,
                    AttributeSource = mm.Member,
                    Sequence = seq++,
                    Metadata = mm
                };
                ApplyMemberMetadata(e, mm);
                entries.Add(e);
            }
            else
            {
                var e = new InspectorEntry
                {
                    EntryKind = InspectorEntry.Kind.Field,
                    Property = prop,
                    Sequence = seq++
                };
                entries.Add(e);
            }
        }

        private static void AddReflectedEntries(List<InspectorEntry> entries, TypeMetadata meta, object target, ref int seq)
        {
            if (meta.ShownMembers != null)
            {
                foreach (var mm in meta.ShownMembers)
                {
                    var e = new InspectorEntry
                    {
                        EntryKind = InspectorEntry.Kind.Shown,
                        Member = mm.Member,
                        AttributeSource = mm.Member,
                        Sequence = seq++,
                        Metadata = mm
                    };
                    ApplyMemberMetadata(e, mm);
                    entries.Add(e);
                }
            }

            if (meta.Buttons != null)
            {
                foreach (var mm in meta.Buttons)
                {
                    var e = new InspectorEntry
                    {
                        EntryKind = InspectorEntry.Kind.Button,
                        ButtonMethod = mm.Member as MethodInfo,
                        Button = mm.Button,
                        Member = mm.Member,
                        AttributeSource = mm.Member,
                        Sequence = seq++,
                        Metadata = mm
                    };
                    ApplyMemberMetadata(e, mm);
                    entries.Add(e);
                }
            }

            if (meta.InspectorGuis != null)
            {
                foreach (var mm in meta.InspectorGuis)
                {
                    var e = new InspectorEntry
                    {
                        EntryKind = InspectorEntry.Kind.InspectorGui,
                        ButtonMethod = mm.Member as MethodInfo,
                        Member = mm.Member,
                        AttributeSource = mm.Member,
                        Sequence = seq++,
                        Metadata = mm
                    };
                    ApplyMemberMetadata(e, mm);
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

        private static void RenderChildren(GroupNode group, object[] targets, Dictionary<string, bool> foldouts, Dictionary<string, int> tabs)
        {
            foreach (var child in group.Children)
            {
                if (child is GroupNode g) { if (GroupHasVisible(g, targets)) RenderGroup(g, targets, foldouts, tabs); }
                else if (child is InspectorEntry e) RenderEntry(e, targets, foldouts, tabs);
            }
        }

        // A group with no visible descendant entry is skipped entirely (no empty header/box).
        private static bool GroupHasVisible(GroupNode g, object[] targets)
        {
            if (!IsGroupVisible(g, targets)) return false;
            foreach (var child in g.Children)
            {
                if (child is InspectorEntry e)
                {
                    if (e.Metadata == null || IsVisible(e.Metadata, targets)) return true;
                }
                else if (child is GroupNode sub && GroupHasVisible(sub, targets)) return true;
            }
            return false;
        }

        private static bool IsGroupVisible(GroupNode g, object[] targets)
        {
            if (g.FoldoutAttr != null && !string.IsNullOrEmpty(g.FoldoutAttr.VisibleIf) && targets != null && targets.Length > 0)
            {
                if (!InspectorMemberResolver.EvaluateBool(targets[0], g.FoldoutAttr.VisibleIf, null, false, true))
                    return false;
            }
            return true;
        }

        private static void RenderGroup(GroupNode g, object[] targets, Dictionary<string, bool> foldouts, Dictionary<string, int> tabs)
        {
            if (!IsGroupVisible(g, targets)) return;
            switch (g.Kind)
            {
                case GroupKind.Box:
                {
                    // VerticalScope guarantees EndVertical fires even if a reflection call inside
                    // RenderChildren throws, so a single bad member can't corrupt the layout stack.
                    using (new EditorGUILayout.VerticalScope(EditorStyles.helpBox))
                    {
                        if (g.ShowLabel)
                        {
                            string label = g.Box?.LabelText ?? g.Name;
                            label = InspectorMemberResolver.ResolveString(targets[0], label);
                            if (!string.IsNullOrEmpty(label))
                            {
                                if (g.Box != null && g.Box.CenterLabel)
                                {
                                    EditorGUILayout.LabelField(label, FrameworkInspectorTheme.CenteredSectionTitle);
                                }
                                else EditorGUILayout.LabelField(label, FrameworkInspectorTheme.SectionTitle);
                            }
                        }
                        RenderChildren(g, targets, foldouts, tabs);
                    }
                    break;
                }
                case GroupKind.Title:
                {
                    var t = g.TitleAttr;
                    string title = InspectorMemberResolver.ResolveString(targets[0], g.Name);
                    string subtitle = t != null ? InspectorMemberResolver.ResolveString(targets[0], t.Subtitle) : null;
                    EditorGUILayout.Space(FrameworkInspectorTheme.SectionSpacing);
                    FrameworkInspectorTheme.DrawTitle(title, subtitle,
                        ToTextAlignment(t?.Alignment ?? TitleAlignments.Left),
                        t == null || t.HorizontalLine,
                        t == null || t.BoldTitle);
                    bool indent = t != null && t.Indent;
                    if (indent)
                    {
                        using (new EditorGUI.IndentLevelScope())
                            RenderChildren(g, targets, foldouts, tabs);
                    }
                    else RenderChildren(g, targets, foldouts, tabs);
                    break;
                }
                case GroupKind.Foldout:
                {
                    if (!foldouts.TryGetValue(g.Path, out bool expanded)) expanded = g.DefaultExpanded;
                    expanded = FrameworkInspectorTheme.SectionFoldout(expanded,
                        InspectorMemberResolver.ResolveString(targets[0], g.Name));
                    foldouts[g.Path] = expanded;
                    if (expanded)
                    {
                        FrameworkInspectorTheme.BeginSectionFoldoutBody();
                        RenderChildren(g, targets, foldouts, tabs);
                        FrameworkInspectorTheme.EndSectionFoldoutBody();
                    }
                    break;
                }
                case GroupKind.Horizontal:
                {
                    RenderHorizontalGroup(g, targets, foldouts, tabs);
                    break;
                }
                case GroupKind.TabContainer:
                {
                    RenderTabGroup(g, targets, foldouts, tabs);
                    break;
                }
                case GroupKind.TabPage:
                {
                    // Rendered by its TabContainer; reaching here means the page is orphaned — draw plain.
                    RenderChildren(g, targets, foldouts, tabs);
                    break;
                }
                case GroupKind.Toggle:
                {
                    RenderToggleGroup(g, targets, foldouts, tabs);
                    break;
                }
                case GroupKind.ButtonRow:
                {
                    using (new EditorGUILayout.HorizontalScope())
                        RenderChildren(g, targets, foldouts, tabs);
                    break;
                }
                default: // Vertical / transparent
                {
                    if (g.Vertical != null && g.Vertical.PaddingTop > 0) GUILayout.Space(g.Vertical.PaddingTop);
                    using (new EditorGUILayout.VerticalScope())
                        RenderChildren(g, targets, foldouts, tabs);
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

        private static void RenderHorizontalGroup(GroupNode g, object[] targets, Dictionary<string, bool> foldouts, Dictionary<string, int> tabs)
        {
            var spec = g.Horizontal;
            string title = spec?.Title;
            if (!string.IsNullOrEmpty(title))
                FrameworkInspectorTheme.DrawTitle(InspectorMemberResolver.ResolveString(targets[0], title));

            float available = EditorGUIUtility.currentViewWidth - 30f;
            float gap = spec?.Gap ?? 3f;

            using (new EditorGUILayout.HorizontalScope())
            {
                if (spec != null && spec.MarginLeft > 0) GUILayout.Space(spec.MarginLeft);

                bool first = true;
                foreach (var child in g.Children)
                {
                    if (child is GroupNode sub && !GroupHasVisible(sub, targets)) continue;
                    if (child is InspectorEntry pe && pe.Metadata != null && !IsVisible(pe.Metadata, targets)) continue;

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

                    float prevLabel = EditorGUIUtility.labelWidth;
                    using (new EditorGUILayout.VerticalScope(options.ToArray()))
                    {
                        try
                        {
                            float lw = cell?.LabelWidth ?? spec?.LabelWidth ?? 0f;
                            if (lw > 0f && lw < 1f) lw = available * lw; // fractional label width = fraction of the view width
                            if (lw > 0f) EditorGUIUtility.labelWidth = lw;

                            if (child is GroupNode gn) RenderGroup(gn, targets, foldouts, tabs);
                            else RenderEntry((InspectorEntry)child, targets, foldouts, tabs);
                        }
                        finally
                        {
                            EditorGUIUtility.labelWidth = prevLabel;
                        }
                    }

                    if (cell != null && cell.PaddingRight > 0) GUILayout.Space(cell.PaddingRight);
                }

                if (spec != null && spec.MarginRight > 0) GUILayout.Space(spec.MarginRight);
            }
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

        private static void RenderTabGroup(GroupNode g, object[] targets, Dictionary<string, bool> foldouts, Dictionary<string, int> tabs)
        {
            var pages = new List<GroupNode>();
            var strays = new List<InspectorEntry>();
            foreach (var c in g.Children)
            {
                if (c is GroupNode page) { if (GroupHasVisible(page, targets)) pages.Add(page); }
                else if (c is InspectorEntry e) strays.Add(e);
            }

            foreach (var s in strays) RenderEntry(s, targets, foldouts, tabs);

            if (pages.Count == 0) return;

            bool hideBar = pages.Count == 1 && g.Tab != null && g.Tab.HideTabGroupIfTabGroupOnlyHasOneTab;
            bool paddingless = g.Tab != null && g.Tab.Paddingless;

            using (EditorGUILayout.VerticalScope scope = paddingless ? null : new EditorGUILayout.VerticalScope(EditorStyles.helpBox))
            {
                if (!paddingless) EditorGUILayout.Space(FrameworkInspectorTheme.SectionSpacing * 0.5f);
                int active = 0;
                if (!hideBar)
                {
                    var names = new string[pages.Count];
                    for (int i = 0; i < pages.Count; i++)
                        names[i] = InspectorMemberResolver.ResolveString(targets[0], pages[i].Name);
                    if (!tabs.TryGetValue(g.Path, out active)) active = 0;
                    active = Mathf.Clamp(active, 0, pages.Count - 1);
                    active = FrameworkInspectorTheme.Toolbar(active, names);
                    tabs[g.Path] = active;
                }
                RenderChildren(pages[active], targets, foldouts, tabs);
            }
        }

        // ---- toggle group: header checkbox bound to ToggleMemberName; body shown while on ----

        private static void RenderToggleGroup(GroupNode g, object[] targets, Dictionary<string, bool> foldouts, Dictionary<string, int> tabs)
        {
            var attr = g.Toggle;
            string toggleMember = attr?.ToggleMemberName ?? g.Name;
            
            bool mixed = false;
            bool on = false;
            var vals = new bool[targets.Length];
            bool failed = false;
            for (int i = 0; i < targets.Length; i++)
            {
                var val = InspectorMemberResolver.GetValue(targets[i], toggleMember, out bool f);
                if (f) failed = true;
                vals[i] = val is bool b && b;
            }
            if (!failed && targets.Length > 0)
            {
                on = vals[0];
                for (int i = 1; i < vals.Length; i++)
                {
                    if (vals[i] != on) mixed = true;
                }
            }

            string title = attr?.GroupTitle ?? ObjectNames.NicifyVariableName(toggleMember);
            title = InspectorMemberResolver.ResolveString(targets[0], title);

            using (new EditorGUILayout.VerticalScope(EditorStyles.helpBox))
            {
                var prevMixed = EditorGUI.showMixedValue;
                if (mixed) EditorGUI.showMixedValue = true;
                EditorGUI.BeginChangeCheck();
                bool next = EditorGUILayout.ToggleLeft(title, on, EditorStyles.boldLabel);
                EditorGUI.showMixedValue = prevMixed;
                if (EditorGUI.EndChangeCheck() && !failed)
                {
                    SetMemberValue(targets, toggleMember, next);
                    on = next;
                }

                if (on)
                {
                    using (new EditorGUI.IndentLevelScope())
                    {
                        foreach (var child in g.Children)
                        {
                            // The toggle member itself is the header; skip its normal row.
                            if (child is InspectorEntry e &&
                                ((e.Field != null && e.Field.Name == toggleMember) ||
                                 (e.Member != null && e.Member.Name == toggleMember)))
                                continue;
                            if (child is GroupNode sub) { if (GroupHasVisible(sub, targets)) RenderGroup(sub, targets, foldouts, tabs); }
                            else if (child is InspectorEntry ie) RenderEntry(ie, targets, foldouts, tabs);
                        }
                    }
                }
            }
        }

        private static void SetMemberValue(object[] targets, string name, object value)
        {
            foreach (var target in targets)
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
                        if (p != null && p.CanWrite) p.SetValue(p.GetGetMethod(true).IsStatic ? null : target, value);
                    }
                }
                catch { }
                if (uo != null) EditorUtility.SetDirty(uo);
            }
        }

        private static void AssignEntryComponentReference(InspectorEntry e, object target, UnityEngine.Object reference)
        {
            var uo = target as UnityEngine.Object;
            if (uo == null) return;

            if (e.Property != null)
            {
                var so = new SerializedObject(uo);
                var prop = so.FindProperty(e.Property.propertyPath);
                if (prop != null && prop.propertyType == SerializedPropertyType.ObjectReference)
                {
                    Undo.RecordObject(uo, "Assign Component");
                    prop.objectReferenceValue = reference;
                    so.ApplyModifiedProperties();
                    return;
                }
            }

            var memberName = e.Field?.Name ?? e.Member?.Name;
            if (!string.IsNullOrEmpty(memberName))
                SetMemberValue(new[] { target }, memberName, reference);
        }

        private static bool IsEntryReferenceMissing(InspectorEntry e, object target)
        {
            var uo = target as UnityEngine.Object;
            if (e.Property != null && uo != null)
            {
                var so = new SerializedObject(uo);
                var prop = so.FindProperty(e.Property.propertyPath);
                if (prop != null && prop.propertyType == SerializedPropertyType.ObjectReference)
                    return prop.objectReferenceValue == null;
            }

            if (e.Field != null)
                return SafeGet(e.Field, target) == null;

            if (e.Member is PropertyInfo pi && pi.CanRead)
            {
                try { return pi.GetValue(pi.GetGetMethod(true).IsStatic ? null : target) == null; }
                catch { return true; }
            }

            return false;
        }

        // ---------------------------------------------------------------- entry pipeline
        // Decorator order: [Title] → info boxes → validation messages → the (possibly wrapped) field.

        private static void RenderEntry(InspectorEntry e, object[] targets, Dictionary<string, bool> foldouts, Dictionary<string, int> tabs)
        {
            var mm = e.Metadata;
            if (mm == null)
            {
                DrawDefaultField(e, targets);
                return;
            }

            // Visibility (ShowIf/HideIf/HideInEditorMode/...).
            if (!IsVisible(mm, targets)) return;

            foreach (var t in targets)
            {
                RunInitHooks(e, t);
            }
            var target = targets[0];

            if (e.SpaceBefore > 0) EditorGUILayout.Space(e.SpaceBefore);

            // [Title] decorator(s).
            if (mm.Titles != null)
            {
                foreach (var t in mm.Titles)
                {
                    EditorGUILayout.Space(FrameworkInspectorTheme.SectionSpacing * 0.5f);
                    FrameworkInspectorTheme.DrawTitle(InspectorMemberResolver.ResolveString(target, t.Title),
                        InspectorMemberResolver.ResolveString(target, t.Subtitle),
                        ToTextAlignment(t.TitleAlignment), t.HorizontalLine, t.Bold);
                }
            }

            // Unity's native [Header] is drawn by PropertyField on the default path.
            // Custom field drawers in RenderField call DrawUnityHeaders first.

            // InfoBox(es) attached to the member.
            if (mm.InfoBoxes != null)
            {
                foreach (var info in mm.InfoBoxes)
                {
                    if (!string.IsNullOrEmpty(info.VisibleIf) &&
                        !InspectorMemberResolver.EvaluateBool(target, info.VisibleIf, null, false, true))
                        continue;
                    FrameworkInspectorTheme.DrawInfoBox(InspectorMemberResolver.ResolveString(target, info.Message), info.InfoMessageType);
                }
            }

            // DetailedInfoBox(es).
            if (mm.DetailedInfoBoxes != null)
            {
                foreach (var info in mm.DetailedInfoBoxes)
                {
                    if (!string.IsNullOrEmpty(info.VisibleIf) &&
                        !InspectorMemberResolver.EvaluateBool(target, info.VisibleIf, null, false, true))
                        continue;
                    DrawDetailedInfoBox(e, target, info, foldouts);
                }
            }

            // Validation feedback draws ABOVE the field.
            RenderValidation(e, targets);

            // Enabled (EnableIf/DisableIf/ReadOnly).
            bool enabled = IsEnabled(mm, targets);

            // GUI color.
            Color prev = GUI.color;
            if (TryGetGuiColor(mm, targets, out var col)) GUI.color = col;

            // LabelWidth / Indent scopes.
            float prevLabelWidth = EditorGUIUtility.labelWidth;
            if (mm.LabelWidth != null && mm.LabelWidth.Width > 0) EditorGUIUtility.labelWidth = mm.LabelWidth.Width;
            if (mm.Indent != null) EditorGUI.indentLevel += mm.Indent.IndentLevel;

            // try/finally guarantees labelWidth/indent/color are restored even if a reflection call
            // below throws — otherwise a single bad member would leak state into every field after it.
            try
            {
                // [OnInspectorGUI(prepend, append)] on a member.
                if (mm.OnInspectorGUI != null && !string.IsNullOrEmpty(mm.OnInspectorGUI.Prepend)) InvokeDrawMethod(target, mm.OnInspectorGUI.Prepend);

                using (new EditorGUI.DisabledScope(!enabled))
                {
                    var inline = mm.InlineButtons;
                    bool hasInline = false;
                    if (inline != null)
                        foreach (var ib in inline) { if (InlineButtonVisible(ib, target)) { hasInline = true; break; } }

                    bool hasReqComp = false;
                    bool reqCompNeedsAdd = false;
                    bool reqCompNeedsAssign = false;
                    Type compType = null;
                    if (mm.RequireComponentButton != null)
                    {
                        compType = mm.RequireComponentButton.ComponentType ?? (e.Field != null ? e.Field.FieldType : (e.Member is PropertyInfo p ? p.PropertyType : null));
                        if (compType != null)
                        {
                            if (compType.IsArray)
                            {
                                compType = compType.GetElementType();
                            }
                            else if (compType.IsGenericType && compType.GetGenericTypeDefinition() == typeof(List<>))
                            {
                                compType = compType.GetGenericArguments()[0];
                            }
                        }
                        if (compType != null && typeof(UnityEngine.Component).IsAssignableFrom(compType))
                        {
                            foreach (var t in targets)
                            {
                                var go = GetGameObject(t);
                                if (go == null) continue;
                                bool missingComp = go.GetComponent(compType) == null;
                                bool missingRef = IsEntryReferenceMissing(e, t);
                                if (missingComp) reqCompNeedsAdd = true;
                                else if (missingRef) reqCompNeedsAssign = true;
                            }
                            hasReqComp = reqCompNeedsAdd || reqCompNeedsAssign;
                        }
                    }

                    using (EditorGUILayout.HorizontalScope hscope = (hasInline || hasReqComp) ? new EditorGUILayout.HorizontalScope() : null)
                    {
                        switch (e.EntryKind)
                        {
                            case InspectorEntry.Kind.Field: RenderField(e, targets, foldouts, tabs); break;
                            case InspectorEntry.Kind.Shown: RenderShown(e, targets); break;
                            case InspectorEntry.Kind.Button: RenderButton(e, targets); break;
                            case InspectorEntry.Kind.InspectorGui: InvokeDrawMethodInfo(target, e.ButtonMethod); break;
                        }

                        if (hasInline && inline != null)
                        {
                            foreach (var ib in inline)
                            {
                                if (!InlineButtonVisible(ib, target)) continue;
                                string label = ib.Label != null
                                    ? InspectorMemberResolver.ResolveString(target, ib.Label)
                                    : ObjectNames.NicifyVariableName(ib.Action);
                                var content = MakeButtonContent(label, ib.Icon);
                                if (GUILayout.Button(content, FrameworkInspectorTheme.CompactButton, GUILayout.ExpandWidth(false)))
                                    InvokeAction(targets, ib.Action);
                            }
                        }

                        if (hasReqComp)
                        {
                            string label = mm.RequireComponentButton.Label;
                            if (string.IsNullOrEmpty(label))
                                label = reqCompNeedsAdd ? "Add" : "Assign";
                            var content = MakeButtonContent(label, mm.RequireComponentButton.Icon ?? "d_Toolbar Plus");
                            if (GUILayout.Button(content, FrameworkInspectorTheme.CompactButton, GUILayout.ExpandWidth(false)))
                            {
                                foreach (var t in targets)
                                {
                                    var go = GetGameObject(t);
                                    if (go == null) continue;
                                    var comp = go.GetComponent(compType);
                                    if (comp == null)
                                    {
                                        comp = Undo.AddComponent(go, compType);
                                        EditorUtility.SetDirty(go);
                                    }
                                    AssignEntryComponentReference(e, t, comp);
                                }
                                if (e.Property != null)
                                    e.Property.serializedObject.Update();
                                InvokeOnValueChanged(e, targets);
                            }
                        }
                    }
                }

                if (mm.OnInspectorGUI != null && !string.IsNullOrEmpty(mm.OnInspectorGUI.Append)) InvokeDrawMethod(target, mm.OnInspectorGUI.Append);
            }
            finally
            {
                if (mm.Indent != null) EditorGUI.indentLevel -= mm.Indent.IndentLevel;
                EditorGUIUtility.labelWidth = prevLabelWidth;
                GUI.color = prev;
            }

            if (e.SpaceAfter > 0) EditorGUILayout.Space(e.SpaceAfter);
        }

        private static bool InlineButtonVisible(InlineButtonAttribute ib, object target)
            => string.IsNullOrEmpty(ib.ShowIf) || InspectorMemberResolver.EvaluateBool(target, ib.ShowIf, null, false, true);

        private static readonly Dictionary<string, Texture> s_buttonIconCache = new Dictionary<string, Texture>();

        private static GUIContent MakeButtonContent(string label, string icon, IconAlignment alignment = IconAlignment.LeftOfText)
        {
            if (string.IsNullOrEmpty(icon) || !TryGetCachedEditorIcon(icon, out var tex))
                return new GUIContent(label);
            if (alignment == IconAlignment.LeftOfText)
                return new GUIContent(label, tex);
            return new GUIContent(label, tex);
        }

        private static bool TryGetCachedEditorIcon(string icon, out Texture tex)
        {
            if (s_buttonIconCache.TryGetValue(icon, out tex))
                return tex != null;

            tex = null;
            var ic = EditorGUIUtility.IconContent(icon);
            if (ic != null && ic.image != null)
                tex = ic.image;
            s_buttonIconCache[icon] = tex;
            return tex != null;
        }

        private static void DrawDetailedInfoBox(InspectorEntry e, object target, DetailedInfoBoxAttribute info, Dictionary<string, bool> foldouts)
        {
            string key = "dibox:" + (e.AttributeSource?.Name ?? "?") + ":" + info.Message;
            foldouts.TryGetValue(key, out bool open);
            FrameworkInspectorTheme.DrawInfoBox(InspectorMemberResolver.ResolveString(target, info.Message)
                + (open ? "\n\n" + InspectorMemberResolver.ResolveString(target, info.Details) : ""), info.InfoMessageType);
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
            var mm = e.Metadata;
            if (mm == null || mm.Member == null) return;

            long key = ((long)(target?.GetHashCode() ?? 0) << 32) ^ (uint)(mm.Member.DeclaringType?.FullName ?? "").GetHashCode() ^ (uint)mm.Name.GetHashCode();
            if (s_initDone.Contains(key)) return;
            s_initDone.Add(key);

            if (mm.InitHooks != null)
            {
                foreach (var init in mm.InitHooks)
                {
                    if (!string.IsNullOrEmpty(init.Action)) InvokeAction(new[] { target }, init.Action);
                    else if (mm.Member is MethodInfo mi && mi.GetParameters().Length == 0)
                        try { mi.Invoke(mi.IsStatic ? null : target, null); } catch { }
                }
            }

            if (mm.ValueChangedHooks != null)
            {
                foreach (var ovc in mm.ValueChangedHooks)
                    if (ovc.InvokeOnInitialize) InvokeChangeAction(e, target, ovc);
            }
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
                FrameworkInspectorTheme.DrawInfoBox($"[OnInspectorGUI] {mi.Name}: {ex.InnerException?.Message ?? ex.Message}", InfoMessageType.Error);
            }
        }

        private static void InvokeAction(object[] targets, string methodName)
        {
            foreach (var target in targets)
            {
                if (target is UnityEngine.Object uo)
                {
                    Undo.RecordObject(uo, $"Action: {methodName}");
                }
                var mi = InspectorMemberResolver.FindMethod(target.GetType(), methodName, Type.EmptyTypes);
                try { mi?.Invoke(mi.IsStatic ? null : target, null); }
                catch (Exception ex) { Debug.LogWarning($"[FoundationPlatform.FrameworkInspector] '{methodName}' threw: {ex.InnerException?.Message ?? ex.Message}"); }
                if (target is UnityEngine.Object uo2) EditorUtility.SetDirty(uo2);
            }
        }

        // ---------------------------------------------------------------- field rendering

        internal static void DrawUnityHeaders(MemberMetadata mm)
        {
            if (mm?.Headers == null) return;
            foreach (var h in mm.Headers)
            {
                EditorGUILayout.Space(4);
                EditorGUILayout.LabelField(h.header, EditorStyles.boldLabel);
            }
        }

        private static void RenderField(InspectorEntry e, object[] targets,
            Dictionary<string, bool> foldouts, Dictionary<string, int> tabs)
        {
            var mm = e.Metadata;
            var prop = e.Property;
            var target = targets[0];

            if (mm == null)
            {
                DrawDefaultField(e, targets);
                return;
            }

            // [DrawWithUnity] → always the stock drawer.
            if (mm.DrawWithUnity != null)
            {
                DrawDefaultField(e, targets);
                return;
            }

            // [TableList] on an array/list → grid renderer.
            if (mm.TableList != null && prop.isArray && e.Field != null)
            {
                var elemType0 = GetElementType(e.Field.FieldType);
                if (elemType0 != null)
                {
                    DrawUnityHeaders(mm);
                    EditorGUI.BeginChangeCheck();
                    TableRenderer.DrawSerializedTable(prop, elemType0, mm.TableList, GetLabel(e, targets));
                    if (EditorGUI.EndChangeCheck())
                    {
                        prop.serializedObject.ApplyModifiedProperties();
                        InvokeOnValueChanged(e, targets);
                    }
                    return;
                }
            }

            // Collections that need the engine list drawer.
            if (prop.isArray && prop.propertyType == SerializedPropertyType.Generic)
            {
                var lds = mm.ListDrawerSettings;
                var searchable = mm.Searchable;
                var vdList = mm.ValueDropdown;
                var asList = mm.AssetSelector;
                var occ = mm.OnCollectionChanged;
                var elemType = GetElementType(e.Field?.FieldType);
                // Object-reference element types (ScriptableObject/Component/etc.) get an object field
                // per row, NOT inline recursion — recursing an object-ref property yields no children
                // (they live on a different serializedObject) and renders blank rows.
                bool engineElems = elemType != null && !HasCustomPropertyDrawer(elemType)
                    && !typeof(UnityEngine.Object).IsAssignableFrom(elemType)
                    && (mm.InlineProperty != null || TypeHasEngineAttributes(elemType));

                if (lds != null || searchable != null || occ != null || engineElems
                    || (vdList != null && vdList.DrawDropdownForListElements)
                    || (asList != null && asList.DrawDropdownForListElements))
                {
                    DrawUnityHeaders(mm);
                    EngineListDrawer.Draw(e, targets, foldouts, tabs, elemType, lds, searchable, vdList, asList, occ);
                    return;
                }
            }

            // [ValueDropdown(getter)] → dropdown of allowed values.
            if (mm.ValueDropdown != null && InspectorDropdown.DrawValueDropdown(e, targets, mm.ValueDropdown, GetLabelText(e, targets))) return;

            // [AssetSelector] on a single object reference.
            if (mm.AssetSelector != null && prop.propertyType == SerializedPropertyType.ObjectReference)
            {
                DrawUnityHeaders(mm);
                InspectorDropdown.DrawAssetSelector(e, targets, mm.AssetSelector);
                return;
            }

            // [DisplayAsString] → read-only text.
            if (mm.DisplayAsString != null)
            {
                DrawUnityHeaders(mm);
                object v = e.Field != null ? SafeGet(e.Field, target) : ReadProperty(prop);
                DrawDisplayAsString(GetLabel(e, targets) ?? TempContent(prop.displayName), v?.ToString() ?? string.Empty, mm);
                return;
            }

            // [ToggleLeft] bool → left-aligned checkbox (label right of the box).
            if (prop.propertyType == SerializedPropertyType.Boolean && mm.ToggleLeft != null)
            {
                DrawUnityHeaders(mm);
                var lbl = GetLabel(e, targets) ?? TempContent(prop.displayName);
                EditorGUI.BeginChangeCheck();
                bool v = EditorGUILayout.ToggleLeft(lbl, prop.boolValue);
                if (EditorGUI.EndChangeCheck()) { prop.boolValue = v; Commit(e, targets); }
                return;
            }

            // [MultiLineProperty(lines)] string → text area.
            if (mm.MultiLineProperty != null && prop.propertyType == SerializedPropertyType.String)
            {
                DrawUnityHeaders(mm);
                var lbl = GetLabel(e, targets) ?? TempContent(prop.displayName);
                EditorGUILayout.LabelField(lbl);
                EditorGUI.BeginChangeCheck();
                float h = Mathf.Max(1, mm.MultiLineProperty.Lines) * EditorGUIUtility.singleLineHeight;
                string s = EditorGUILayout.TextArea(prop.stringValue, GUILayout.MinHeight(h));
                if (EditorGUI.EndChangeCheck()) { prop.stringValue = s; Commit(e, targets); }
                return;
            }

            // [TextArea(min, max)] string → text area.
            if (mm.TextArea != null && prop.propertyType == SerializedPropertyType.String)
            {
                DrawUnityHeaders(mm);
                var lbl = GetLabel(e, targets) ?? TempContent(prop.displayName);
                EditorGUILayout.LabelField(lbl);
                EditorGUI.BeginChangeCheck();
                float minH = Mathf.Max(1, mm.TextArea.minLines) * EditorGUIUtility.singleLineHeight;
                float maxH = Mathf.Max(1, mm.TextArea.maxLines) * EditorGUIUtility.singleLineHeight;
                string s = EditorGUILayout.TextArea(prop.stringValue, GUILayout.MinHeight(minH), GUILayout.MaxHeight(maxH));
                if (EditorGUI.EndChangeCheck()) { prop.stringValue = s; Commit(e, targets); }
                return;
            }

            // [Multiline(lines)] string → text area.
            if (mm.Multiline != null && prop.propertyType == SerializedPropertyType.String)
            {
                DrawUnityHeaders(mm);
                var lbl = GetLabel(e, targets) ?? TempContent(prop.displayName);
                EditorGUILayout.LabelField(lbl);
                EditorGUI.BeginChangeCheck();
                float h = Mathf.Max(1, mm.Multiline.lines) * EditorGUIUtility.singleLineHeight;
                string s = EditorGUILayout.TextArea(prop.stringValue, GUILayout.MinHeight(h));
                if (EditorGUI.EndChangeCheck()) { prop.stringValue = s; Commit(e, targets); }
                return;
            }

            // [PropertyRange(min,max)] numeric → slider (getters resolved via member names).
            if (mm.PropertyRange != null && TryDrawPropertyRange(e, targets, mm.PropertyRange)) return;

            // [MinMaxSlider] on Vector2/Vector2Int.
            if (mm.MinMaxSlider != null && TryDrawMinMaxSlider(e, targets, mm.MinMaxSlider)) return;

            // [ProgressBar] on a numeric.
            if (mm.ProgressBar != null && TryDrawProgressBar(e, targets, mm.ProgressBar)) return;

            // [EnumToggleButtons] on an enum.
            if (prop.propertyType == SerializedPropertyType.Enum &&
                mm.EnumToggleButtons != null &&
                TryDrawEnumToggleButtons(e, targets))
                return;

            // Object-reference specials.
            if (prop.propertyType == SerializedPropertyType.ObjectReference)
            {
                if (mm.PreviewField != null) { DrawUnityHeaders(mm); DrawPreviewField(e, targets, mm.PreviewField); return; }

                if (mm.InlineEditor != null) { DrawUnityHeaders(mm); DrawInlineEditor(e, targets, mm.InlineEditor, foldouts); return; }

                if (mm.AssetsOnly != null)
                {
                    DrawUnityHeaders(mm);
                    DrawObjectField(e, targets, allowScene: false);
                    return;
                }
                if (mm.SceneObjectsOnly != null)
                {
                    DrawUnityHeaders(mm);
                    DrawSceneObjectField(e, targets);
                    return;
                }

                // Plain object reference without a custom PropertyDrawer → enhanced field
                // (pencil / drag-out / right-click selector, all settings-gated).
                var refFieldType = e.Field?.FieldType;
                if (refFieldType != null && !HasCustomPropertyDrawer(refFieldType))
                {
                    DrawUnityHeaders(mm);
                    DrawObjectField(e, targets, allowScene: true);
                    return;
                }
            }

            // Nested serializable object — recurse through the engine (property-tree style) when it
            // either carries [InlineProperty] (draw inline, no wrapper) OR declares ANY FoundationPlatform.FrameworkInspector
            // attribute (draw under a collapsible foldout). Plain data (only Unity attrs) and types with
            // their own custom PropertyDrawer fall through to the default PropertyField below.
            var fieldType = e.Field?.FieldType;
            bool explicitInline = mm.InlineProperty != null;
            if (prop.propertyType == SerializedPropertyType.Generic && prop.hasVisibleChildren && !prop.isArray
                && TypeHidesReferencePicker(fieldType) && !HasCustomPropertyDrawer(fieldType))
            {
                DrawUnityHeaders(mm);
                float prevLw = EditorGUIUtility.labelWidth;
                if (mm.InlineProperty != null && mm.InlineProperty.LabelWidth > 0) EditorGUIUtility.labelWidth = mm.InlineProperty.LabelWidth;
                DrawNestedObject(e, targets, foldouts, tabs, inline: explicitInline);
                EditorGUIUtility.labelWidth = prevLw;
                return;
            }
            if (prop.propertyType == SerializedPropertyType.Generic && prop.hasVisibleChildren && !prop.isArray
                && !HasCustomPropertyDrawer(fieldType)
                && (explicitInline || TypeHasEngineAttributes(fieldType)))
            {
                DrawUnityHeaders(mm);
                float prevLw = EditorGUIUtility.labelWidth;
                if (mm.InlineProperty != null && mm.InlineProperty.LabelWidth > 0) EditorGUIUtility.labelWidth = mm.InlineProperty.LabelWidth;
                DrawNestedObject(e, targets, foldouts, tabs, inline: explicitInline);
                EditorGUIUtility.labelWidth = prevLw;
                return;
            }

            // Default field + numeric constraints ([MinValue]/[MaxValue]/[Wrap]).
            DrawDefaultField(e, targets);
        }

        private static void DrawDefaultField(InspectorEntry e, object[] targets)
        {
            var label = GetLabel(e, targets);
            var prop = e.Property;
            EditorGUI.BeginChangeCheck();
            // Long bool labels truncate inside indented foldouts; ToggleLeft uses the full row width.
            if (prop.propertyType == SerializedPropertyType.Boolean
                && label != null && label != GUIContent.none
                && !string.IsNullOrEmpty(label.text) && label.text.Length > 30)
            {
                bool v = EditorGUILayout.ToggleLeft(label, prop.boolValue);
                if (EditorGUI.EndChangeCheck()) { prop.boolValue = v; Commit(e, targets); }
                return;
            }
            if (label != null) EditorGUILayout.PropertyField(prop, label, true);
            else EditorGUILayout.PropertyField(prop, true);
            // UnityEvent fields accept dropped GameObjects/Components as new persistent listeners.
            var defaultFieldType = e.Field?.FieldType;
            if (defaultFieldType != null && typeof(UnityEngine.Events.UnityEventBase).IsAssignableFrom(defaultFieldType))
                UnityEventDropTarget.Handle(GUILayoutUtility.GetLastRect(), prop);
            if (EditorGUI.EndChangeCheck())
            {
                ApplyNumericConstraints(e, targets);
                Commit(e, targets);
            }
        }

        // [MinValue]/[MaxValue] clamp and [Wrap] wrap the edited value (floats, ints, vectors).
        private static void ApplyNumericConstraints(InspectorEntry e, object[] targets)
        {
            var src = e.AttributeSource;
            if (src == null) return;
            var prop = e.Property;

            var minA = src.GetCustomAttribute<MinValueAttribute>();
            var maxA = src.GetCustomAttribute<MaxValueAttribute>();
            var wrap = src.GetCustomAttribute<WrapAttribute>();
            if (minA == null && maxA == null && wrap == null) return;

            double min = minA != null ? (minA.Expression != null ? ResolveNumber(targets[0], minA.Expression, double.MinValue) : minA.Min) : double.MinValue;
            double max = maxA != null ? (maxA.Expression != null ? ResolveNumber(targets[0], maxA.Expression, double.MaxValue) : maxA.Max) : double.MaxValue;

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

        internal static void Commit(InspectorEntry e, object[] targets)
        {
            e.Property.serializedObject.ApplyModifiedProperties();
            InvokeOnValueChanged(e, targets);
        }

        private static object SafeGet(FieldInfo f, object target)
        {
            try { return f.GetValue(f.IsStatic ? null : target); } catch { return null; }
        }

        // ---- exotic value drawers ---------------------------------------------------

        private static void DrawDisplayAsString(GUIContent label, string text, MemberMetadata mm)
        {
            var style = mm.DisplayAsStringStyle ?? EditorStyles.label;
            using (new EditorGUI.DisabledScope(true))
            {
                if (label == GUIContent.none) EditorGUILayout.LabelField(text, style);
                else EditorGUILayout.LabelField(label, TempContent(text), style);
            }
        }

        private static void DrawObjectField(InspectorEntry e, object[] targets, bool allowScene)
        {
            var prop = e.Property;
            var t = e.Field != null ? e.Field.FieldType : typeof(UnityEngine.Object);
            var lbl = GetLabel(e, targets) ?? TempContent(prop.displayName);
            EditorGUI.BeginChangeCheck();
            var rect = EditorGUILayout.GetControlRect();
            var obj = ObjectFieldX.Draw(rect, lbl, prop.objectReferenceValue, t, allowScene, prop);
            if (EditorGUI.EndChangeCheck()) { prop.objectReferenceValue = obj; Commit(e, targets); }
        }

        // [SceneObjectsOnly]: reject persistent assets on assignment.
        private static void DrawSceneObjectField(InspectorEntry e, object[] targets)
        {
            var prop = e.Property;
            var t = e.Field != null ? e.Field.FieldType : typeof(UnityEngine.Object);
            var lbl = GetLabel(e, targets) ?? TempContent(prop.displayName);
            EditorGUI.BeginChangeCheck();
            var rect = EditorGUILayout.GetControlRect();
            var obj = ObjectFieldX.Draw(rect, lbl, prop.objectReferenceValue, t, true, prop);
            if (EditorGUI.EndChangeCheck())
            {
                if (obj != null && EditorUtility.IsPersistent(obj))
                    Debug.LogWarning($"[FoundationPlatform.FrameworkInspector] '{prop.displayName}' accepts scene objects only.");
                else { prop.objectReferenceValue = obj; Commit(e, targets); }
            }
        }

        // [PreviewField]: square preview that IS the picker (tall ObjectField rects draw as previews).
        private static void DrawPreviewField(InspectorEntry e, object[] targets, PreviewFieldAttribute pf)
        {
            var prop = e.Property;
            var t = e.Field != null ? e.Field.FieldType : typeof(UnityEngine.Object);
            float h = pf.Height > 0 ? pf.Height : 64f;
            var lbl = GetLabel(e, targets) ?? TempContent(prop.displayName);

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
            if (EditorGUI.EndChangeCheck()) { prop.objectReferenceValue = obj; Commit(e, targets); }
        }

        private static readonly Dictionary<int, UnityEditor.Editor> s_inlineEditors = new Dictionary<int, UnityEditor.Editor>();

        private static void DrawInlineEditor(InspectorEntry e, object[] targets, InlineEditorAttribute ie, Dictionary<string, bool> foldouts)
        {
            var prop = e.Property;
            var obj = prop.objectReferenceValue;

            // Object field per ObjectFieldMode; [HideReferenceObjectPicker] on type suppresses picker chrome.
            var fieldType = e.Field?.FieldType;
            bool hideRefPicker = TypeHidesReferencePicker(fieldType)
                || (obj != null && TypeHidesReferencePicker(obj.GetType()));
            bool showField = !hideRefPicker && ie.ObjectFieldMode switch
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
                    var lbl = GetLabel(e, targets) ?? TempContent(prop.displayName);
                    var rect = EditorGUILayout.GetControlRect();
                    var foldRect = new Rect(rect.x, rect.y, EditorGUIUtility.labelWidth, rect.height);
                    expanded = EditorGUI.Foldout(foldRect, expanded, lbl, true);
                    var fieldRect = new Rect(rect.x + EditorGUIUtility.labelWidth, rect.y,
                        rect.width - EditorGUIUtility.labelWidth, rect.height);
                    EditorGUI.BeginChangeCheck();
                    var picked = EditorGUI.ObjectField(fieldRect, prop.objectReferenceValue, t, true);
                    if (EditorGUI.EndChangeCheck()) { prop.objectReferenceValue = picked; Commit(e, targets); }
                    foldouts[foldKey] = expanded;
                }
                else
                {
                    DrawObjectField(e, targets, allowScene: true);
                    obj = prop.objectReferenceValue;
                    // Boxed nested editors honor Expanded; when collapsed, a foldout toggles the inline GUI.
                    if (ie.ObjectFieldMode == InlineEditorObjectFieldModes.Boxed && ie.DrawGUI && obj != null)
                    {
                        if (ie.Expanded)
                        {
                            expanded = true;
                        }
                        else
                        {
                            expanded = EditorGUILayout.Foldout(expanded, TempContent("Nested Inspector"), true);
                        }
                        foldouts[foldKey] = expanded;
                    }
                    else if (ie.ObjectFieldMode != InlineEditorObjectFieldModes.Foldout && ie.DrawPreview && !ie.DrawGUI)
                    {
                        expanded = true;
                    }
                }
            }
            else expanded = true;

            obj = prop.objectReferenceValue;
            if (obj == null || !expanded) return;
            // Guard against inlining the object being inspected (infinite recursion).
            if (Array.IndexOf(targets, obj) >= 0) return;

            int id = obj.GetInstanceID();
            if (!s_inlineEditors.TryGetValue(id, out var ed) || ed == null || ed.target != obj)
            {
                if (ed != null) UnityEngine.Object.DestroyImmediate(ed);
                ed = UnityEditor.Editor.CreateEditor(obj);
                s_inlineEditors[id] = ed;
            }
            if (ed == null) return;

            using (new EditorGUILayout.VerticalScope(EditorStyles.helpBox))
            {
                if (ie.DrawHeader) ed.DrawHeader();
                if (ie.DrawGUI)
                {
                    // Nested editor's own OnInspectorGUI() is third-party/unknown code; the VerticalScope
                    // above still guarantees this box closes even if it throws.
                    using (new EditorGUI.IndentLevelScope())
                        ed.OnInspectorGUI();
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
            }
        }

        private static object[] GetTargetsForScope(SerializedProperty prop, object[] parentTargets)
        {
            if (parentTargets == null || parentTargets.Length == 0) return Array.Empty<object>();
            if (prop == null || string.IsNullOrEmpty(prop.propertyPath)) return parentTargets;

            var list = new List<object>();
            foreach (var t in parentTargets)
            {
                var val = InspectorMemberResolver.GetPropertyValue(t, prop.propertyPath, out bool failed);
                if (!failed && val != null) list.Add(val);
            }
            return list.ToArray();
        }

        // Draw a nested serializable object through the engine. inline=true → no wrapper
        // ([InlineProperty]); inline=false → collapsible foldout (default collapsed), the default
        // rendering of an attributed nested object.
        internal static void DrawNestedObject(InspectorEntry e, object[] targets,
            Dictionary<string, bool> foldouts, Dictionary<string, int> tabs, bool inline,
            GUIContent labelOverride = null)
        {
            object[] nestedTargets = GetTargetsForScope(e.Property, targets);

            var lbl = labelOverride ?? GetLabel(e, targets);
            bool hideLabel = lbl == GUIContent.none;

            // Fallback: no boxed instance (multi-edit/unresolvable) → default field draw.
            if (nestedTargets == null || nestedTargets.Length == 0)
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
                if (!hideLabel) EditorGUILayout.LabelField(lbl ?? TempContent(e.Property.displayName));
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
                    lbl ?? TempContent(e.Property.displayName), true);
                var rect = GUILayoutUtility.GetLastRect();
                if (Event.current.type == EventType.MouseDown && rect.Contains(Event.current.mousePosition) && Event.current.button == 0)
                {
                    e.Property.isExpanded = !e.Property.isExpanded;
                    Event.current.Use();
                }
                if (!e.Property.isExpanded) return;
                indent = true;
            }

            if (indent) EditorGUI.indentLevel++;
            try
            {
                var nestedMeta = GetOrCreateMetadata(nestedTargets[0].GetType());
                DrawTypeInfoBoxes(nestedMeta, nestedTargets);
                var nested = GetPooledList();
                int seq = 0;
                foreach (var child in ChildProperties(e.Property))
                    AddFieldEntry(nested, child, nestedMeta, ref seq);
                AddReflectedEntries(nested, nestedMeta, nestedTargets, ref seq);
                RenderScope(nested, nestedTargets, foldouts, tabs);
                ReleasePooledList(nested);
            }
            catch (ExitGUIException)
            {
                throw;
            }
            catch (Exception ex)
            {
                // Degrade to flat children rather than blanking the whole inspector.
                Debug.LogWarning($"[FoundationPlatform.FrameworkInspector] nested draw of '{e.Property?.propertyPath}' failed: {ex.Message}");
                foreach (var child in ChildProperties(e.Property))
                    EditorGUILayout.PropertyField(child, true);
            }
            if (indent) EditorGUI.indentLevel--;
        }

        // Cached: does the type declare ANY FoundationPlatform.FrameworkInspector attribute (on itself or any member)?
        // Used to auto-recurse attributed nested objects without requiring an explicit [InlineProperty].
        private const string EngineAttrNamespace = "FoundationPlatform.FrameworkInspector";
        private static readonly Dictionary<Type, bool> s_engineAttrTypes = new Dictionary<Type, bool>();

        internal static bool TypeHidesReferencePicker(Type t)
            => t != null && t.GetCustomAttribute<HideReferenceObjectPickerAttribute>() != null;

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

        private static bool TryDrawPropertyRange(InspectorEntry e, object[] targets, PropertyRangeAttribute pr)
        {
            double min = ResolveNumber(targets[0], pr.MinGetter, pr.Min);
            double max = ResolveNumber(targets[0], pr.MaxGetter, pr.Max);
            var prop = e.Property;
            var lbl = GetLabel(e, targets) ?? TempContent(prop.displayName);
            if (prop.propertyType == SerializedPropertyType.Integer)
            {
                DrawUnityHeaders(e.Metadata);
                EditorGUI.BeginChangeCheck();
                int v = EditorGUILayout.IntSlider(lbl, prop.intValue, (int)min, (int)max);
                if (EditorGUI.EndChangeCheck()) { prop.intValue = v; Commit(e, targets); }
                return true;
            }
            if (prop.propertyType == SerializedPropertyType.Float)
            {
                DrawUnityHeaders(e.Metadata);
                EditorGUI.BeginChangeCheck();
                float v = EditorGUILayout.Slider(lbl, prop.floatValue, (float)min, (float)max);
                if (EditorGUI.EndChangeCheck()) { prop.floatValue = v; Commit(e, targets); }
                return true;
            }
            return false;
        }

        private static bool TryDrawMinMaxSlider(InspectorEntry e, object[] targets, MinMaxSliderAttribute mms)
        {
            var prop = e.Property;
            float lo = mms.MinValue, hi = mms.MaxValue;
            if (!string.IsNullOrEmpty(mms.MinMaxValueGetter))
            {
                var v = InspectorMemberResolver.GetValue(targets[0], mms.MinMaxValueGetter, out bool failed);
                if (!failed && v is Vector2 range) { lo = range.x; hi = range.y; }
            }
            else
            {
                lo = (float)ResolveNumber(targets[0], mms.MinValueGetter, lo);
                hi = (float)ResolveNumber(targets[0], mms.MaxValueGetter, hi);
            }

            var lbl = GetLabel(e, targets) ?? TempContent(prop.displayName);

            if (prop.propertyType == SerializedPropertyType.Vector2)
            {
                DrawUnityHeaders(e.Metadata);
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
                    Commit(e, targets);
                }
                return true;
            }

            if (prop.propertyType == SerializedPropertyType.Vector2Int)
            {
                DrawUnityHeaders(e.Metadata);
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
                    Commit(e, targets);
                }
                return true;
            }

            return false;
        }

        private static bool TryDrawProgressBar(InspectorEntry e, object[] targets, ProgressBarAttribute pb)
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

            DrawUnityHeaders(e.Metadata);
            double min = ResolveNumber(targets[0], pb.MinGetter, pb.Min);
            double max = ResolveNumber(targets[0], pb.MaxGetter, pb.Max);
            if (max <= min) max = min + 1;

            Color fill = new Color(pb.R, pb.G, pb.B);
            if (!string.IsNullOrEmpty(pb.ColorGetter))
            {
                var cv = InspectorMemberResolver.GetValue(targets[0], pb.ColorGetter, out bool cf);
                if (!cf && cv is Color c) fill = c;
            }
            Color back = FrameworkInspectorTheme.ProgressBarBackground;
            if (!string.IsNullOrEmpty(pb.BackgroundColorGetter))
            {
                var bv = InspectorMemberResolver.GetValue(targets[0], pb.BackgroundColorGetter, out bool bf);
                if (!bf && bv is Color bc) back = bc;
            }

            var lbl = GetLabel(e, targets) ?? TempContent(prop.displayName);
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
                    Commit(e, targets);
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
                    text = InspectorMemberResolver.ResolveString(targets[0], pb.CustomValueStringGetter);
                if (string.IsNullOrEmpty(text))
                    text = isInt ? $"{(int)value}/{(int)max}" : $"{value:0.##}";
                var style = e.Metadata?.ProgressBarStyle ?? EditorStyles.miniLabel;
                GUI.Label(barRect, text, style);
            }
            return true;
        }

        private static bool TryDrawEnumToggleButtons(InspectorEntry e, object[] targets)
        {
            var enumType = e.Field?.FieldType;
            if (enumType == null || !enumType.IsEnum) return false;

            DrawUnityHeaders(e.Metadata);
            var prop = e.Property;
            var lbl = GetLabel(e, targets) ?? TempContent(prop.displayName);
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
                    Commit(e, targets);
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
                Commit(e, targets);
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

        private static bool IsMixed(object[] values)
        {
            if (values == null || values.Length <= 1) return false;
            var first = values[0];
            for (int i = 1; i < values.Length; i++)
            {
                if (!Equals(first, values[i])) return true;
            }
            return false;
        }

        // [ShowInInspector] members: editable when writable (fields and set-able properties);
        // complex values recurse through the POCO inspector (property-tree style).
        private static void RenderShown(InspectorEntry e, object[] targets)
        {
            if (targets.Length > 1)
            {
                var mm = e.Metadata;
                if (mm == null || e.Member == null) return;

                var valueType = e.Field != null ? e.Field.FieldType : (e.Member is PropertyInfo p ? p.PropertyType : null);
                if (valueType == null) return;

                var values = new object[targets.Length];
                for (int i = 0; i < targets.Length; i++)
                {
                    if (e.Field != null) values[i] = SafeGet(e.Field, targets[i]);
                    else if (e.Member is PropertyInfo pi) values[i] = pi.GetValue(targets[i]);
                }

                bool mixed = IsMixed(values);
                object value = values[0];

                string label = GetLabelText(e, targets);
                bool hideLabel = mm.HideLabel;
                bool readOnly = mm.ReadOnly || e.Member is PropertyInfo pi2 && !pi2.CanWrite
                    || mm.DictionaryDrawerSettings != null;

                if (EngineDictionaryDrawer.IsDictionaryType(valueType) || value is IDictionary)
                {
                    var lbl = hideLabel ? GUIContent.none : TempContent(label);
                    string foldKey = e.Member.Name + ":multi";
                    EngineDictionaryDrawer.Draw(value, mm.DictionaryDrawerSettings, lbl, true, foldKey);
                    return;
                }

                if (mm.DisplayAsString != null || readOnly)
                {
                    string text = mixed ? "—" : (value?.ToString() ?? string.Empty);
                    if (hideLabel) EditorGUILayout.LabelField(text);
                    else EditorGUILayout.LabelField(label, text);
                    return;
                }

                var prevMixed = EditorGUI.showMixedValue;
                if (mixed) EditorGUI.showMixedValue = true;

                EditorGUI.BeginChangeCheck();
                object edited = PocoInspector.DrawTypedFieldPublic(hideLabel ? GUIContent.none : TempContent(label), valueType, value);

                EditorGUI.showMixedValue = prevMixed;

                if (EditorGUI.EndChangeCheck())
                {
                    foreach (var target in targets)
                    {
                        var uo = target as UnityEngine.Object;
                        if (uo != null) Undo.RecordObject(uo, "Inspector");
                        try
                        {
                            if (e.Field != null) e.Field.SetValue(target, edited);
                            else if (e.Member is PropertyInfo pi3) pi3.SetValue(target, edited);
                        }
                        catch { }
                        if (uo != null) EditorUtility.SetDirty(uo);
                    }
                    InvokeOnValueChanged(e, targets);
                }
            }
            else
            {
                PocoInspector.DrawSingleMember(targets[0], e.Member, e.Metadata);
            }
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

        private static void RenderButton(InspectorEntry e, object[] targets)
        {
            var target = targets[0];
            string label = e.Button.Name;
            if (string.IsNullOrEmpty(label)) label = ObjectNames.NicifyVariableName(e.ButtonMethod.Name);
            else label = InspectorMemberResolver.ResolveString(target, label);

            float height = ButtonHeight(e.Button);
            var content = MakeButtonContent(label, e.Button.Icon, e.Button.IconAlignment);
            var ps = e.ButtonMethod.GetParameters();

            if (ps.Length == 0)
            {
                if (DrawAlignedButton(e.Button, content, height))
                    InvokeButton(e, targets, null);
            }
            else if (e.Button.DisplayParameters)
            {
                DrawParameterizedButton(e, targets, content, height, ps);
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

        private static GUIStyle ResolveButtonGuiStyle(ButtonAttribute b) => b.Style switch
        {
            ButtonStyle.Box => FrameworkInspectorTheme.ButtonBox,
            ButtonStyle.FoldoutButton => FrameworkInspectorTheme.ButtonFoldout,
            _ => FrameworkInspectorTheme.CompactButton,
        };

        private static bool DrawAlignedButton(ButtonAttribute b, GUIContent content, float height)
        {
            if (b.Style == ButtonStyle.Box)
            {
                using (new EditorGUILayout.VerticalScope(FrameworkInspectorTheme.ButtonStyleFor(ButtonStyle.Box)))
                    return DrawIconButton(b, content, height, true);
            }

            if (b.Stretch && b.ButtonAlignment == ButtonAlignment.Stretch)
                return DrawIconButton(b, content, height, true);

            using (new EditorGUILayout.HorizontalScope())
            {
                if (b.ButtonAlignment != ButtonAlignment.Left) GUILayout.FlexibleSpace();
                bool clicked = DrawIconButton(b, content, height, false);
                if (b.ButtonAlignment != ButtonAlignment.Right) GUILayout.FlexibleSpace();
                return clicked;
            }
        }

        private static bool DrawIconButton(ButtonAttribute b, GUIContent content, float height, bool expandWidth)
        {
            var style = ResolveButtonGuiStyle(b);
            var opts = expandWidth
                ? new[] { GUILayout.Height(height), GUILayout.ExpandWidth(true) }
                : new GUILayoutOption[] { GUILayout.Height(height) };

            if (content.image == null || b.IconAlignment == IconAlignment.LeftOfText)
                return GUILayout.Button(content, style, opts);

            float width = expandWidth ? EditorGUIUtility.currentViewWidth - 40f : Mathf.Max(80f, style.CalcSize(content).x + 24f);
            var rect = GUILayoutUtility.GetRect(width, height, style, opts);
            bool clicked = GUI.Button(rect, GUIContent.none, style);
            PaintButtonIconLabel(rect, content.text, content.image, b.IconAlignment, style);
            return clicked;
        }

        private static void PaintButtonIconLabel(Rect rect, string text, Texture icon, IconAlignment align, GUIStyle style)
        {
            const float iconSize = 16f;
            const float pad = 4f;
            var iconRect = new Rect(rect.x + pad, rect.y + (rect.height - iconSize) * 0.5f, iconSize, iconSize);
            var textStyle = new GUIStyle(style) { alignment = TextAnchor.MiddleCenter };
            switch (align)
            {
                case IconAlignment.RightOfText:
                {
                    var textSize = textStyle.CalcSize(new GUIContent(text));
                    float total = textSize.x + pad + iconSize;
                    float start = rect.x + (rect.width - total) * 0.5f;
                    GUI.Label(new Rect(start, rect.y, textSize.x, rect.height), text, textStyle);
                    GUI.DrawTexture(new Rect(start + textSize.x + pad, iconRect.y, iconSize, iconSize), icon);
                    break;
                }
                case IconAlignment.LeftEdge:
                    GUI.DrawTexture(iconRect, icon);
                    GUI.Label(new Rect(rect.x + pad + iconSize + pad, rect.y, rect.width - iconSize - pad * 3f, rect.height), text, textStyle);
                    break;
                case IconAlignment.RightEdge:
                    GUI.Label(new Rect(rect.x + pad, rect.y, rect.width - iconSize - pad * 3f, rect.height), text, textStyle);
                    GUI.DrawTexture(new Rect(rect.xMax - pad - iconSize, iconRect.y, iconSize, iconSize), icon);
                    break;
            }
        }

        internal static bool DrawAlignedButtonPublic(ButtonAttribute b, GUIContent content, float height)
            => DrawAlignedButton(b, content, height);

        private static string ButtonKey(InspectorEntry e, object target)
            => (target?.GetHashCode() ?? 0) + ":" + e.ButtonMethod.DeclaringType?.FullName + "." + e.ButtonMethod.Name;

        // Parameterized [Button]: a box with one field per parameter + an invoke button.
        private static void DrawParameterizedButton(InspectorEntry e, object[] targets, GUIContent content, float height, ParameterInfo[] ps)
        {
            var target = targets[0];
            string key = ButtonKey(e, target);
            if (!s_buttonParams.TryGetValue(key, out var st) || st.Values == null || st.Values.Length != ps.Length)
            {
                st = new ButtonParamState { Values = new object[ps.Length] };
                for (int i = 0; i < ps.Length; i++)
                    st.Values[i] = ps[i].HasDefaultValue ? ps[i].DefaultValue : DefaultOf(ps[i].ParameterType);
                s_buttonParams[key] = st;
            }

            using (new EditorGUILayout.VerticalScope(EditorStyles.helpBox))
            {
                EditorGUILayout.LabelField(content, FrameworkInspectorTheme.SectionTitle);
                for (int i = 0; i < ps.Length; i++)
                {
                    var lbl = new GUIContent(ObjectNames.NicifyVariableName(ps[i].Name));
                    st.Values[i] = PocoInspector.DrawTypedFieldPublic(lbl, ps[i].ParameterType, st.Values[i]);
                }
                if (GUILayout.Button("Invoke", FrameworkInspectorTheme.CompactButton, GUILayout.Height(Mathf.Min(height, 24f))))
                    InvokeButton(e, targets, st.Values);
            }
        }

        private static object DefaultOf(Type t) => t.IsValueType ? Activator.CreateInstance(t) : null;

        private static void InvokeButton(InspectorEntry e, object[] targets, object[] args)
        {
            foreach (var target in targets)
            {
                if (target is UnityEngine.Object uo)
                {
                    Undo.RecordObject(uo, $"Button: {e.ButtonMethod.Name}");
                }
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
                    Debug.LogError($"[FoundationPlatform.FrameworkInspector] Button '{e.ButtonMethod.Name}' threw: {ex.InnerException?.Message ?? ex.Message}");
                }
                if (e.Button.DirtyOnClick && target is UnityEngine.Object uo2) EditorUtility.SetDirty(uo2);
            }
        }

        // ---------------------------------------------------------------- metadata

        private static void ApplyMemberMetadata(InspectorEntry e, MemberMetadata mm)
        {
            if (mm == null) return;
            e.Order = mm.Order;
            e.SpaceBefore = mm.SpaceBefore;
            e.SpaceAfter = mm.SpaceAfter;
            e.OwnHorizontal = mm.OwnHorizontal;
            e.TabName = mm.TabName;
        }

        internal static bool IsVisible(MemberMetadata mm, object[] targets)
        {
            if (mm == null || targets == null || targets.Length == 0) return true;
            var target = targets[0];
            if (mm.HideInEditorMode && !Application.isPlaying) return false;
            if (mm.HideInPlayMode && Application.isPlaying) return false;
            if (mm.ShowInPlayMode && !Application.isPlaying) return false;

            if (mm.ShowIfs != null)
            {
                foreach (var s in mm.ShowIfs)
                    if (!InspectorMemberResolver.EvaluateBool(target, s.Condition, s.Value, s.HasValue, true)) return false;
            }
            if (mm.HideIfs != null)
            {
                foreach (var h in mm.HideIfs)
                    if (InspectorMemberResolver.EvaluateBool(target, h.Condition, h.Value, h.HasValue, false)) return false;
            }
            return true;
        }

        private static bool IsEnabled(MemberMetadata mm, object[] targets)
        {
            if (mm == null || targets == null || targets.Length == 0) return true;
            var target = targets[0];
            if (mm.ReadOnly) return false;
            if (mm.DisableInEditorMode && !Application.isPlaying) return false;
            if (mm.DisableInPlayMode && Application.isPlaying) return false;

            if (mm.EnableIfs != null)
            {
                foreach (var en in mm.EnableIfs)
                    if (!InspectorMemberResolver.EvaluateBool(target, en.Condition, en.Value, en.HasValue, true)) return false;
            }
            if (mm.DisableIfs != null)
            {
                foreach (var di in mm.DisableIfs)
                    if (InspectorMemberResolver.EvaluateBool(target, di.Condition, di.Value, di.HasValue, false)) return false;
            }
            return true;
        }

        private static bool TryGetGuiColor(MemberMetadata mm, object[] targets, out Color color)
        {
            color = Color.white;
            if (mm == null || mm.GUIColor == null || targets == null || targets.Length == 0) return false;
            var target = targets[0];
            var attr = mm.GUIColor;
            if (!string.IsNullOrEmpty(attr.GetColor))
            {
                var v = InspectorMemberResolver.GetValue(target, attr.GetColor, out bool failed);
                if (!failed && v is Color c) { color = c; return true; }
                if (ColorUtility.TryParseHtmlString(attr.GetColor, out var html)) { color = html; return true; }
                return false;
            }
            color = new Color(attr.R, attr.G, attr.B, attr.A);
            return true;
        }

        // ---------------------------------------------------------------- validation (drawn above the field)

        private static void RenderValidation(InspectorEntry e, object[] targets)
        {
            var mm = e.Metadata;
            if (mm == null || targets == null || targets.Length == 0) return;
            var target = targets[0];

            if (mm.Required != null && e.Property != null && IsEmptyRef(e.Property))
            {
                string msg = mm.Required.ErrorMessage != null
                    ? InspectorMemberResolver.ResolveString(target, mm.Required.ErrorMessage)
                    : $"{GetLabelText(e, targets) ?? e.Property.displayName} is required.";
                FrameworkInspectorTheme.DrawValidationBox(msg, mm.Required.MessageType);
            }

            if (mm.ValidateInputs != null)
            {
                foreach (var v in mm.ValidateInputs)
                {
                    if (!RunValidator(v, target, e, out bool ok, out string message, out InfoMessageType msgType)) continue;
                    if (!ok)
                    {
                        string msg = message ?? (v.DefaultMessage != null
                            ? InspectorMemberResolver.ResolveString(target, v.DefaultMessage)
                            : "Invalid value.");
                        FrameworkInspectorTheme.DrawValidationBox(msg, msgType);
                    }
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

        internal static void InvokeOnValueChanged(InspectorEntry e, object[] targets)
        {
            var src = e.AttributeSource;
            if (src == null || targets == null) return;
            bool any = false;
            foreach (var attr in src.GetCustomAttributes<OnValueChangedAttribute>())
            {
                foreach (var target in targets)
                {
                    InvokeChangeAction(e, target, attr);
                }
                any = true;
            }
            if (any)
            {
                foreach (var target in targets)
                {
                    if (target is UnityEngine.Object uo) EditorUtility.SetDirty(uo);
                }
            }
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
            catch (Exception ex) { Debug.LogWarning($"[FoundationPlatform.FrameworkInspector] OnValueChanged '{attr.Action}' threw: {ex.InnerException?.Message ?? ex.Message}"); }
        }

        internal static GUIContent GetLabel(InspectorEntry e, object[] targets)
        {
            var mm = e.Metadata;
            if (mm == null) return null;
            if (mm.HideLabel) return GUIContent.none;
            if (mm.CachedLabel != null) return mm.CachedLabel;

            string text = GetLabelText(e, targets);
            string tooltip = mm.Tooltip?.tooltip;
            if (text != null) return new GUIContent(text, tooltip);
            if (!string.IsNullOrEmpty(tooltip)) return new GUIContent(e.Property.displayName, tooltip);
            return null; // let PropertyField use its default
        }

        internal static string GetLabelText(InspectorEntry e, object[] targets)
        {
            var mm = e.Metadata;
            if (mm == null || mm.LabelText == null) return null;
            string text = InspectorMemberResolver.ResolveString(targets[0], mm.LabelText.Text);
            if (mm.LabelText.NicifyText && !string.IsNullOrEmpty(text)) text = ObjectNames.NicifyVariableName(text);
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

        private static GameObject GetGameObject(object target)
        {
            if (target is Component c) return c.gameObject;
            if (target is GameObject go) return go;
            return null;
        }

        private static bool IsUnitySerialized(FieldInfo f)
        {
            if (f.IsStatic) return false;
            if (f.GetCustomAttribute<NonSerializedAttribute>() != null) return false;
            bool serializable = f.IsPublic || f.GetCustomAttribute<SerializeField>() != null;
            return serializable;
        }
    }

    internal sealed class MemberMetadata
    {
        public MemberInfo Member;
        public string Name;
        public FieldInfo Field;
        public Type FieldType;

        // Grouping attributes
        public Attribute[] GroupAttributes;
        public string ResolvedContainerPath;

        // Ordering / Spacing
        public float Order;
        public float SpaceBefore;
        public float SpaceAfter;

        // Visibility / Enabled
        public bool HideInEditorMode;
        public bool HideInPlayMode;
        public bool ShowInPlayMode;
        public ShowIfAttribute[] ShowIfs;
        public HideIfAttribute[] HideIfs;
        public bool ReadOnly;
        public bool DisableInEditorMode;
        public bool DisableInPlayMode;
        public EnableIfAttribute[] EnableIfs;
        public DisableIfAttribute[] DisableIfs;

        // Decorators
        public TitleAttribute[] Titles;
        public InfoBoxAttribute[] InfoBoxes;
        public HeaderAttribute[] Headers;
        public DetailedInfoBoxAttribute[] DetailedInfoBoxes;

        // Validation
        public RequiredAttribute Required;
        public ValidateInputAttribute[] ValidateInputs;

        // Color
        public GUIColorAttribute GUIColor;

        // Hooks
        public OnInspectorInitAttribute[] InitHooks;
        public OnValueChangedAttribute[] ValueChangedHooks;

        // Drawing attributes
        public DrawWithUnityAttribute DrawWithUnity;
        public TableListAttribute TableList;
        public ListDrawerSettingsAttribute ListDrawerSettings;
        public DictionaryDrawerSettingsAttribute DictionaryDrawerSettings;
        public SearchableAttribute Searchable;
        public ValueDropdownAttribute ValueDropdown;
        public AssetSelectorAttribute AssetSelector;
        public OnCollectionChangedAttribute OnCollectionChanged;
        public InlinePropertyAttribute InlineProperty;
        public DisplayAsStringAttribute DisplayAsString;
        public ToggleLeftAttribute ToggleLeft;
        public MultiLinePropertyAttribute MultiLineProperty;
        public TextAreaAttribute TextArea;
        public MultilineAttribute Multiline;
        public PropertyRangeAttribute PropertyRange;
        public MinMaxSliderAttribute MinMaxSlider;
        public ProgressBarAttribute ProgressBar;
        public EnumToggleButtonsAttribute EnumToggleButtons;
        public PreviewFieldAttribute PreviewField;
        public InlineEditorAttribute InlineEditor;
        public AssetsOnlyAttribute AssetsOnly;
        public SceneObjectsOnlyAttribute SceneObjectsOnly;

        // Label / Tooltip / Layout modifiers
        public bool HideLabel;
        public TooltipAttribute Tooltip;
        public LabelTextAttribute LabelText;
        public IndentAttribute Indent;
        public LabelWidthAttribute LabelWidth;
        public OnInspectorGUIAttribute OnInspectorGUI;
        public HorizontalGroupAttribute OwnHorizontal;
        public string TabName;

        // Button specifics
        public ButtonAttribute Button;
        public InlineButtonAttribute[] InlineButtons;
        public RequireComponentButtonAttribute RequireComponentButton;

        // Flags
        public bool IsFlagsEnum;

        // Cached GUIContent for static labels
        public GUIContent CachedLabel;

        // Cached GUIStyles to prevent allocations
        public GUIStyle DisplayAsStringStyle;
        public GUIStyle ProgressBarStyle;
    }

    internal sealed class TypeMetadata
    {
        public Type Type;
        public MemberMetadata[] ShownMembers;
        public MemberMetadata[] Buttons;
        public MemberMetadata[] InspectorGuis;
        public TypeInfoBoxAttribute[] TypeInfoBoxes;
        
        // Fast lookup for serialized fields
        public Dictionary<string, MemberMetadata> SerializedFieldMap = new Dictionary<string, MemberMetadata>();

        // Pre-built GroupNode tree template
        public GroupNode GroupTreeTemplate;
    }
}
#endif
