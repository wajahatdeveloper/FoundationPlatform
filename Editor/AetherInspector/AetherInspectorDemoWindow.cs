#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using AetherNexus.FoundationPlatform.Utilities.Menus;
using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;

namespace AetherNexus.FoundationPlatform.AetherInspector.Editor
{
    /// <summary>
    /// Live parity harness for the <see cref="AetherInspectorEditor"/> engine. Menu:
    /// <c>Tools/Diagnostics/AetherInspector Demo</c>. Hosts an in-memory editor-only ScriptableObject
    /// that exercises every supported AetherInspector attribute, drawn through the
    /// in-house engine. Use it as the visual regression harness for the attribute surface.
    /// Editor-only asset — never ships.
    /// </summary>
    public sealed class AetherInspectorDemoWindow : EditorWindow
    {
        private AetherInspectorDemoData _data;
        private UnityEditor.Editor _editor;
        private IMGUIContainer _imguiContainer;
        private Vector2 _scrollPosition;

        [MenuItem(MenuPaths.Diagnostics.AetherInspectorDemo, false, MenuPriorities.Diagnostics + 1)]
        private static void Open() => GetWindow<AetherInspectorDemoWindow>("AetherInspector Demo");

        private void OnEnable()
        {
            _data = CreateInstance<AetherInspectorDemoData>();
            _data.hideFlags = HideFlags.DontSave;
            _editor = UnityEditor.Editor.CreateEditor(_data);
        }

        private void OnDisable()
        {
            if (_editor != null) DestroyImmediate(_editor);
            if (_data != null) DestroyImmediate(_data);
        }

        private void CreateGUI()
        {
            var root = rootVisualElement;
            root.style.flexDirection = FlexDirection.Column;

            var banner = new HelpBox(
                "This inspector is drawn by FoundationPlatform.AetherInspector.Editor.AetherInspectorEditor (in-house). " +
                "Every field below uses a FoundationPlatform.AetherInspector attribute.",
                HelpBoxMessageType.Info);
            root.Add(banner);

            _imguiContainer = new IMGUIContainer(DrawInspectorImgui);
            _imguiContainer.style.flexGrow = 1;
            root.Add(_imguiContainer);
        }

        private void DrawInspectorImgui()
        {
            AetherInspectorTheme.BeginInspectorScope();
            if (_editor != null)
            {
                _scrollPosition = EditorGUILayout.BeginScrollView(_scrollPosition);
                _editor.OnInspectorGUI();
                EditorGUILayout.EndScrollView();
            }
            AetherInspectorTheme.EndInspectorScope();
        }
    }

    [CustomEditor(typeof(AetherInspectorDemoData))]
    public sealed class AetherInspectorInspectorDemoDataEditor : AetherInspectorEditor { }

    [CustomPropertyDrawer(typeof(AetherInspectorDemoData.DemoPayload))]
    internal sealed class DemoPayloadDrawer : AetherInspectorReflectedDrawer { }

    /// <summary>Editor-only data object exercising the supported attribute surface.</summary>
    [TypeInfoBox("[TypeInfoBox] — drawn at the top of the inspector for this type.")]
    public sealed class AetherInspectorDemoData : ScriptableObject
    {
        // --- Titles & simple labels ---
        [Title("AetherInspector Demo", "Exercises the in-house attribute engine")]
        [LabelText("Renamed Label")]
        public string labeled = "value";

        [LabelText("$DynamicLabelSource")]
        public string dynamicLabel = "label above comes from DynamicLabelSource";

        private string DynamicLabelSource => $"Dynamic ({counter})";

        [HideLabel]
        [MultiLineProperty(2)]
        public string hiddenLabel = "no label shown";

        [ReadOnly]
        public int readOnlyValue = 42;

        [LabelWidth(220)]
        public string wideLabelField = "label column is 220px";

        [Indent(2)]
        public string indented = "indented two levels";

        [DisplayAsString(TextAlignment.Center)]
        public string displayedAsString = "read-only, centered";

        [GUIColor("#7FDBFF")]
        public string hexTinted = "hex GUIColor";

        [GUIColor(nameof(CounterColor))]
        public int tintedByMember;

        private Color CounterColor => counter > 5 ? new Color(1f, 0.6f, 0.6f) : Color.white;

        // --- Box group with nested path ---
        [BoxGroup("Identity")]
        public string id = "demo";

        [BoxGroup("Identity")]
        [Required("Assign a reference to clear this warning.")]
        public UnityEngine.Object requiredRef;

        [BoxGroup("Metadata/Details", LabelText = "Details", CenterLabel = true)]
        public string author = "you";

        [BoxGroup("Metadata/Details")]
        [PropertySpace(6)]
        public string notes = "";

        // --- Foldout (collapsed by default) ---
        [FoldoutGroup("Advanced", expanded: false)]
        public float advancedA = 1f;

        [FoldoutGroup("Advanced")]
        [PropertyRange(0, nameof(MaxAdvanced))]
        public int advancedB = 5;

        private int MaxAdvanced => 10 + counter;

        // --- Title group with subtitle/alignment ---
        [TitleGroup("Stats", "subtitle text", TitleAlignments.Split)]
        [ProgressBar(0, 100, ColorGetter = nameof(HealthColor), DrawValueLabel = true)]
        public float health = 65f;

        private Color HealthColor => health < 25f ? Color.red : health < 60f ? Color.yellow : Color.green;

        [TitleGroup("Stats")]
        [ProgressBar(0, 8, Segmented = true)]
        public int charges = 3;

        [TitleGroup("Stats")]
        [MinMaxSlider(0f, 100f, true)]
        public Vector2 damageRange = new Vector2(10, 40);

        [TitleGroup("Stats")]
        [Wrap(0, 360)]
        public float angle = 30f;

        [TitleGroup("Stats")]
        [MinValue(0), MaxValue(999)]
        public int clamped = 10;

        // --- Enum toggle buttons ---
        [EnumToggleButtons]
        public Mode mode = Mode.Low;

        [EnumToggleButtons]
        public Days activeDays = Days.Mon | Days.Fri;

        public enum Mode { Off, Low, High }

        [Flags]
        public enum Days { None = 0, Mon = 1, Tue = 2, Wed = 4, Thu = 8, Fri = 16 }

        // --- Conditionals ---
        [Title("Conditionals")]
        public bool showExtra;

        [ShowIf(nameof(showExtra))]
        [InfoBox("Visible only when 'showExtra' is true.", InfoMessageType.Info)]
        public string extra = "conditionally shown";

        [EnableIf(nameof(showExtra))]
        public string editableWhenExtra = "enabled with showExtra";

        [ShowIf(nameof(mode), Mode.High)]
        [InfoBox("Shown only when mode == High.", InfoMessageType.Warning)]
        public string highOnly = "high mode field";

        [ShowIf("@showExtra && counter > 3")]
        [DetailedInfoBox("Expression-driven field (click for details).",
            "Visible when showExtra && counter > 3 — the resolver evaluates !, &&, ||, comparisons and parentheses.")]
        public string exprDriven = "shown by @expression";

        // --- Validation ---
        [Title("Validation")]
        [ValidateInput(nameof(ValidatePositive), "Value must be positive.")]
        public int mustBePositive = 1;

        private bool ValidatePositive(int value, ref string message)
        {
            if (value > 0) return true;
            message = $"Value must be positive (got {value}).";
            return false;
        }

        // --- OnValueChanged + GUIColor ---
        [OnValueChanged(nameof(OnCounterChanged))]
        [GUIColor(0.6f, 0.9f, 1f)]
        public int counter;

        [ShowInInspector]
        public string CounterEcho => $"counter = {counter}";

        [ShowInInspector]
        public int EditableProperty { get; set; } = 7;

        private void OnCounterChanged(int newValue) => Debug.Log($"[Demo] counter changed to {newValue}");

        // --- Horizontal group: widths, label width, title ---
        [HorizontalGroup("Row", Title = "Horizontal (flex | 0.3 | 90px)", LabelWidth = 12)]
        public float x;
        [HorizontalGroup("Row", 0.3f)]
        public float y;
        [HorizontalGroup("Row", 90f)]
        public float z;

        // --- Toggle group ---
        [ToggleGroup(nameof(useOverride), "Override Settings")]
        public bool useOverride;

        [ToggleGroup(nameof(useOverride))]
        public float overrideValue = 1.5f;

        [ToggleGroup(nameof(useOverride))]
        public string overrideName = "override";

        // --- Tab group with a nested box ---
        [TabGroup("Tabs", "First")]
        public string tabOne = "in first tab";
        [TabGroup("Tabs", "Second")]
        public string tabTwo = "in second tab";
        [TabGroup("Tabs", "Second"), BoxGroup("Tabs/Second/Nested Box")]
        public string tabTwoBoxed = "boxed inside second tab";

        // --- Dropdowns / assets ---
        [Title("Dropdowns & Assets")]
        [ValueDropdown(nameof(NameOptions))]
        public string chosenName = "alpha";

        private IEnumerable<string> NameOptions() => new[] { "alpha", "beta", "gamma", "group/delta", "group/epsilon" };

        [ValueDropdown(nameof(WeightedOptions))]
        public int weight = 1;

        private IEnumerable<ValueDropdownItem<int>> WeightedOptions() => new[]
        {
            new ValueDropdownItem<int>("Light", 1),
            new ValueDropdownItem<int>("Medium", 5),
            new ValueDropdownItem<int>("Heavy", 20),
        };

        [AssetSelector(Filter = "t:Texture2D")]
        public Texture2D pickedTexture;

        [PreviewField(72, ObjectFieldAlignment.Left)]
        public Texture2D previewTexture;

        [InlineEditor(InlineEditorObjectFieldModes.Foldout)]
        public ScriptableObject inlineEdited;

        [AssetsOnly]
        public GameObject assetOnlyPrefab;

        [SceneObjectsOnly]
        public GameObject sceneOnlyObject;

        // --- Lists ---
        [Title("Lists")]
        [ListDrawerSettings(ShowIndexLabels = true, NumberOfItemsPerPage = 5, ShowPaging = true,
            ListElementLabelName = "Name", CustomAddFunction = nameof(AddEntry))]
        [OnCollectionChanged(nameof(AfterListChange))]
        public List<Entry> entries = new List<Entry>
        {
            new Entry { Name = "one", Value = 1 },
            new Entry { Name = "two", Value = 2 },
        };

        private Entry AddEntry() => new Entry { Name = $"new {entries.Count}", Value = entries.Count };

        private void AfterListChange() => Debug.Log($"[Demo] entries changed → {entries.Count}");

        [Serializable]
        public class Entry
        {
            public string Name;
            public int Value;
        }

        [Searchable]
        public List<string> searchableList = new List<string> { "apple", "banana", "cherry", "date", "elderberry" };

        // --- Table (TableList) ---
        [Title("Table")]
        [TableList(ShowIndexLabels = true, ShowPaging = true, NumberOfItemsPerPage = 10)]
        public List<DemoRow> rows = new List<DemoRow>
        {
            new DemoRow { Name = "alpha", Amount = 3, Enabled = true },
            new DemoRow { Name = "beta", Amount = 7, Enabled = false },
        };

        [Serializable]
        public class DemoRow
        {
            [TableColumnWidth(140)] public string Name;
            [TableColumnWidth(70)] public int Amount;
            [TableColumnWidth(60)] public bool Enabled;

            [ShowInInspector, ReadOnly]
            public string Status => Enabled ? "on" : "off";

            [TableColumnWidth(60)]
            [Button("hi", ButtonSizes.Small)]
            private void Ping() => Debug.Log($"[Demo] row {Name}");
        }

        // --- Inline buttons ---
        [Title("Buttons")]
        [InlineButton(nameof(ResetCounter), "Reset")]
        [InlineButton(nameof(BumpCounter), "$BumpLabel", ShowIf = nameof(showExtra))]
        public int counterWithButtons;

        private string BumpLabel => $"+{counter}";
        private void ResetCounter() { counterWithButtons = 0; }
        private void BumpCounter() { counterWithButtons += Mathf.Max(1, counter); }

        // --- Buttons ---
        [PropertyOrder(100)]
        [ButtonGroup("Actions")]
        [Button("Small", ButtonSizes.Small)]
        private void SmallButton() => Debug.Log("[Demo] small button");

        [PropertyOrder(100)]
        [ButtonGroup("Actions")]
        [Button(ButtonSizes.Large)]
        private void LargeButton() => Debug.Log("[Demo] large button");

        [PropertyOrder(101)]
        [Button("@DynamicButtonLabel")]
        private void DynamicButton() => Debug.Log("[Demo] dynamic-label button");

        private string DynamicButtonLabel => $"Counter is {counter}";

        [PropertyOrder(102)]
        [Button(36)]
        private void PixelHeightButton() => Debug.Log("[Demo] 36px button");

        [PropertyOrder(103)]
        [Button("Parameterized (result shown below)")]
        private string Combine(string prefix, int number) => $"{prefix}-{number}";

        [PropertyOrder(104)]
        [Button("Box Style", Style = ButtonStyle.Box, Icon = "d_SaveAs", IconAlignment = IconAlignment.LeftEdge)]
        private void BoxStyleButton() => Debug.Log("[Demo] box-style button");

        [PropertyOrder(105)]
        [Button(Style = ButtonStyle.FoldoutButton, Icon = "d_PlayButton", IconAlignment = IconAlignment.RightOfText)]
        private void FoldoutStyleButton() => Debug.Log("[Demo] foldout-style button");

        // --- ToggleLeft / VerticalGroup / DrawWithUnity ---
        [Title("Layout Extras")]
        [VerticalGroup("Extras", PaddingTop = 4, PaddingBottom = 4)]
        [ToggleLeft]
        public bool toggleLeftBool = true;

        [VerticalGroup("Extras")]
        [DrawWithUnity]
        public Vector3 unityDrawnVector = Vector3.one;

        [VerticalGroup("Extras")]
        [InfoBox("GUIAlwaysEnabled info — drawn even when parent would disable.", GUIAlwaysEnabled = true)]
        [DisableIf(nameof(toggleLeftBool))]
        public string disabledUnlessToggleOff = "disabled when toggleLeftBool is true";

        // --- Dictionary drawer ---
        [Title("Dictionary")]
        [ShowInInspector, ReadOnly, DictionaryDrawerSettings(KeyLabel = "Id", ValueLabel = "Label", KeyColumnWidth = 80)]
        private Dictionary<int, string> demoDictionary => new Dictionary<int, string>
        {
            { 1, "alpha" }, { 2, "beta" }, { 42, "answer" },
        };

        // --- HideReferenceObjectPicker ---
        [Title("HideReferenceObjectPicker")]
        public DemoHiddenPicker hiddenPicker = new DemoHiddenPicker();

        [Serializable]
        [HideReferenceObjectPicker]
        public class DemoHiddenPicker
        {
            public string note = "No reference picker chrome on this nested type.";
            public int value = 1;
        }

        // --- RequireComponentButton ---
        [Title("Require Component")]
        [RequireComponentButton(typeof(BoxCollider), "Add BoxCollider", Icon = "d_Toolbar Plus")]
        public BoxCollider optionalCollider;

        // --- Foldout VisibleIf (partial: group visibility) ---
        [Title("Foldout VisibleIf")]
        public bool showFoldoutGroup;

        [FoldoutGroup("ConditionalFoldout", VisibleIf = nameof(showFoldoutGroup))]
        public string foldoutVisibleField = "visible when showFoldoutGroup";

        // --- Nested list elements (AetherInspectorReflectedDrawer pattern) ---
        [Title("Nested List Payloads")]
        [ListDrawerSettings(ShowIndexLabels = true)]
        public List<DemoPayload> payloadList = new List<DemoPayload>
        {
            new DemoPayload { label = "row A", weight = 1 },
            new DemoPayload { label = "row B", weight = 5 },
        };

        [InfoBox("Partial support: CollapseOthersOnExpand, ShowIf/HideIf Animate, ValueDropdown AppendNextDrawer — API only.", InfoMessageType.Warning)]
        [PropertyOrder(199)]
        public string unsupportedApiNote = "see DOCS/AetherInspector.md";

        // --- Fragment pattern repro (nested box paths + inline payload + private base button) ---
        [Title("Fragment Pattern")]
        public DemoFragment fragment = new DemoFragment();

        [Serializable]
        public class DemoFragment : DemoFragmentBase
        {
            [BoxGroup("Frag")]
            [HideLabel]
            [ShowIf(nameof(source), Mode.High)]
            [InlineProperty]
            public DemoPayload custom = new DemoPayload();
        }

        [Serializable]
        public class DemoFragmentBase
        {
            [BoxGroup("Frag", false)]
            [BoxGroup("Frag/SrcBox", ShowLabel = false)]
            [HorizontalGroup("Frag/SrcBox/Src")]
            [GUIColor(0.55f, 0.55f, 0.6f)]
            [LabelWidth(100)]
            public Mode source = Mode.Low;

            [HorizontalGroup("Frag/SrcBox/Src", Width = 170)]
            [GUIColor(0.55f, 0.55f, 0.6f)]
            [ShowIf(nameof(source), Mode.High)]
            [Button("Base-Class Button")]
            private void BaseButton() => Debug.Log("[Demo] private base-class button");
        }

        [Serializable]
        public class DemoPayload
        {
            public string label = "inline payload";
            [PropertyRange(0, 10)] public int weight = 3;
        }

        // --- OnInspectorGUI / OnInspectorInit ---
        [PropertyOrder(200)]
        [OnInspectorGUI]
        private void CustomGuiBlock()
        {
            GuiKit.InfoBox("[OnInspectorGUI] — this block is drawn by a method on the target.", InfoMessageType.None);
        }

        [PropertyOrder(201)]
        [OnInspectorInit(nameof(NoteInit))]
        public string initHooked = "OnInspectorInit logged once on first draw";

        private void NoteInit() => Debug.Log("[Demo] OnInspectorInit ran");
    }
}
#endif
