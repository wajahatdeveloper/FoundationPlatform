using System;

namespace FoundationPlatform.FrameworkInspector
{
    // ---- Enums -------------------------------------------------------------

    public enum ButtonSizes { Small = 0, Medium = 1, Large = 2, Gigantic = 3 }

    public enum InfoMessageType { None = 0, Info = 1, Warning = 2, Error = 3 }

    public enum ObjectFieldAlignment { Left = 0, Center = 1, Right = 2 }

    public enum InlineEditorModes { GUIOnly, GUIAndHeader, GUIAndPreview, SmallPreview, LargePreview, FullEditor }

    public enum InlineEditorObjectFieldModes { Boxed, Foldout, Hidden, CompletelyHidden }

    public enum TitleAlignments { Left = 0, Centered = 1, Right = 2, Split = 3 }

    public enum ButtonStyle { CompactBox = 0, FoldoutButton = 1, Box = 2 }

    public enum IconAlignment { LeftOfText = 0, RightOfText = 1, LeftEdge = 2, RightEdge = 3 }

    // ---- Action attributes -------------------------------------------------

    [AttributeUsage(AttributeTargets.Method | AttributeTargets.Field | AttributeTargets.Property, AllowMultiple = true, Inherited = true)]
    public sealed class ButtonAttribute : Attribute
    {
        public string Name;
        public ButtonSizes ButtonHeight;
        public int ButtonHeightPixels;       // explicit pixel height; wins over ButtonSizes when > 0
        public bool Expanded;                // parameterized buttons: draw parameters expanded
        public bool DisplayParameters = true;
        public bool DirtyOnClick = true;
        public bool DrawResult = true;
        public string Icon;                  // editor icon name (EditorGUIUtility.IconContent)
        public IconAlignment IconAlignment = IconAlignment.LeftOfText;
        public ButtonStyle Style = ButtonStyle.CompactBox;
        public ButtonAlignment ButtonAlignment = ButtonAlignment.Stretch;
        public bool Stretch = true;

        public ButtonAttribute() { }
        public ButtonAttribute(ButtonSizes size) { ButtonHeight = size; }
        public ButtonAttribute(string name) { Name = name; }
        public ButtonAttribute(string name, ButtonSizes size) { Name = name; ButtonHeight = size; }
        public ButtonAttribute(int buttonSize) { ButtonHeightPixels = buttonSize; }
        public ButtonAttribute(string name, int buttonSize) { Name = name; ButtonHeightPixels = buttonSize; }
    }

    public enum ButtonAlignment { Left = 0, Center = 1, Right = 2, Stretch = 3 }

    [AttributeUsage(AttributeTargets.Method | AttributeTargets.Field | AttributeTargets.Property, AllowMultiple = true, Inherited = true)]
    public sealed class ButtonGroupAttribute : Attribute
    {
        public string GroupID;
        public float Order;
        public ButtonGroupAttribute() { GroupID = string.Empty; }
        public ButtonGroupAttribute(string groupId, float order = 0f) { GroupID = groupId; Order = order; }
    }

    [AttributeUsage(AttributeTargets.Method | AttributeTargets.Field | AttributeTargets.Property, AllowMultiple = true, Inherited = true)]
    public sealed class InlineButtonAttribute : Attribute
    {
        public string Action;
        public string Label;
        public string Icon;
        public string ShowIf;
        public InlineButtonAttribute(string action) { Action = action; }
        public InlineButtonAttribute(string action, string label) { Action = action; Label = label; }
    }

    [AttributeUsage(AttributeTargets.Field | AttributeTargets.Property, AllowMultiple = false, Inherited = true)]
    public sealed class RequireComponentButtonAttribute : Attribute
    {
        public Type ComponentType;
        public string Label;
        public string Icon;

        public RequireComponentButtonAttribute() { }
        public RequireComponentButtonAttribute(Type type) { ComponentType = type; }
        public RequireComponentButtonAttribute(Type type, string label) { ComponentType = type; Label = label; }
    }

    [AttributeUsage(AttributeTargets.Field | AttributeTargets.Property, AllowMultiple = true, Inherited = true)]
    public sealed class OnValueChangedAttribute : Attribute
    {
        public string Action;
        public bool IncludeChildren;
        public bool InvokeOnInitialize;
        public OnValueChangedAttribute(string action, bool includeChildren = false) { Action = action; IncludeChildren = includeChildren; }
    }

    [AttributeUsage(AttributeTargets.Field | AttributeTargets.Property | AttributeTargets.Method | AttributeTargets.Class | AttributeTargets.Struct, AllowMultiple = true, Inherited = true)]
    public sealed class OnInspectorGUIAttribute : Attribute
    {
        public string Prepend;
        public string Append;
        public OnInspectorGUIAttribute() { }
        public OnInspectorGUIAttribute(string prepend, string append = null) { Prepend = prepend; Append = append; }
    }

    [AttributeUsage(AttributeTargets.Field | AttributeTargets.Property, AllowMultiple = true, Inherited = true)]
    public sealed class OnCollectionChangedAttribute : Attribute
    {
        public string Before;
        public string After;
        public OnCollectionChangedAttribute(string after) { After = after; }
        public OnCollectionChangedAttribute(string before, string after) { Before = before; After = after; }
    }

    [AttributeUsage(AttributeTargets.Field | AttributeTargets.Property, AllowMultiple = true, Inherited = true)]
    public sealed class ValidateInputAttribute : Attribute
    {
        public string Condition;         // member/method name to evaluate
        public string DefaultMessage;
        public InfoMessageType MessageType;
        public bool IncludeChildren;
        public ValidateInputAttribute(string condition) { Condition = condition; MessageType = InfoMessageType.Error; }
        public ValidateInputAttribute(string condition, string defaultMessage) { Condition = condition; DefaultMessage = defaultMessage; MessageType = InfoMessageType.Error; }
        public ValidateInputAttribute(string condition, string defaultMessage, InfoMessageType messageType) { Condition = condition; DefaultMessage = defaultMessage; MessageType = messageType; }
    }

    // ---- Group attributes --------------------------------------------------
    // Group attributes share a "GroupID" path (segments split on '/') and an Order.

    [AttributeUsage(AttributeTargets.Field | AttributeTargets.Property | AttributeTargets.Method | AttributeTargets.Event, AllowMultiple = true, Inherited = true)]
    public sealed class BoxGroupAttribute : Attribute
    {
        public string GroupID;
        public bool ShowLabel;
        public bool CenterLabel;
        public float Order;
        public string LabelText;
        public BoxGroupAttribute(string group, bool showLabel = true, bool centerLabel = false, float order = 0f)
        { GroupID = group; ShowLabel = showLabel; CenterLabel = centerLabel; Order = order; }
    }

    [AttributeUsage(AttributeTargets.Field | AttributeTargets.Property | AttributeTargets.Method | AttributeTargets.Event, AllowMultiple = true, Inherited = true)]
    public sealed class FoldoutGroupAttribute : Attribute
    {
        public string GroupID;
        public bool Expanded;
        public bool HasDefinedExpanded;
        public float Order;
        public string VisibleIf;
        public FoldoutGroupAttribute(string group, float order = 0f) { GroupID = group; Order = order; }
        public FoldoutGroupAttribute(string group, bool expanded, float order = 0f)
        { GroupID = group; Expanded = expanded; HasDefinedExpanded = true; Order = order; }
    }

    [AttributeUsage(AttributeTargets.Field | AttributeTargets.Property | AttributeTargets.Method | AttributeTargets.Event, AllowMultiple = true, Inherited = true)]
    public sealed class TitleGroupAttribute : Attribute
    {
        public string GroupID;
        public string Subtitle;
        public float Order;
        public bool BoldTitle = true;
        public bool HorizontalLine = true;
        public bool Indent;
        public TitleAlignments Alignment = TitleAlignments.Left;
        public TitleGroupAttribute(string title, string subtitle = null, float order = 0f)
        { GroupID = title; Subtitle = subtitle; Order = order; }
        public TitleGroupAttribute(string title, string subtitle, TitleAlignments alignment,
            bool horizontalLine = true, bool boldTitle = true, bool indent = false, float order = 0f)
        { GroupID = title; Subtitle = subtitle; Alignment = alignment; HorizontalLine = horizontalLine; BoldTitle = boldTitle; Indent = indent; Order = order; }
    }

    [AttributeUsage(AttributeTargets.Field | AttributeTargets.Property | AttributeTargets.Method | AttributeTargets.Event, AllowMultiple = true, Inherited = true)]
    public sealed class TabGroupAttribute : Attribute
    {
        public string GroupID;   // container group id
        public string TabName;
        public float Order;
        public bool Paddingless;
        public bool HideTabGroupIfTabGroupOnlyHasOneTab;
        public const string DEFAULT_GROUP = "_DefaultTabGroup";
        public TabGroupAttribute(string tab) { GroupID = DEFAULT_GROUP; TabName = tab; }
        public TabGroupAttribute(string group, string tab, float order = 0f) { GroupID = group; TabName = tab; Order = order; }
    }

    [AttributeUsage(AttributeTargets.Field | AttributeTargets.Property | AttributeTargets.Method | AttributeTargets.Event, AllowMultiple = true, Inherited = true)]
    public sealed class HorizontalGroupAttribute : Attribute
    {
        public string GroupID;
        public float Width;      // this member's column: 0 = flexible, <1 = percentage, >=1 = pixels
        public float Gap = 3f;
        public float MarginLeft;
        public float MarginRight;
        public float PaddingLeft;
        public float PaddingRight;
        public float MinWidth;
        public float MaxWidth;
        public float LabelWidth;
        public float Order;
        public string Title;
        public HorizontalGroupAttribute(string group, float width = 0f, int marginLeft = 0, int marginRight = 0, float order = 0f)
        { GroupID = group; Width = width; MarginLeft = marginLeft; MarginRight = marginRight; Order = order; }
        public HorizontalGroupAttribute(float width = 0f, int marginLeft = 0, int marginRight = 0, float order = 0f)
        { GroupID = "_DefaultHorizontalGroup"; Width = width; MarginLeft = marginLeft; MarginRight = marginRight; Order = order; }
    }

    [AttributeUsage(AttributeTargets.Field | AttributeTargets.Property | AttributeTargets.Method | AttributeTargets.Event, AllowMultiple = true, Inherited = true)]
    public sealed class VerticalGroupAttribute : Attribute
    {
        public string GroupID;
        public float Order;
        public float PaddingTop;
        public float PaddingBottom;
        public VerticalGroupAttribute(string group, float order = 0f) { GroupID = group; Order = order; }
    }

    [AttributeUsage(AttributeTargets.Field | AttributeTargets.Property | AttributeTargets.Method | AttributeTargets.Event, AllowMultiple = true, Inherited = true)]
    public sealed class ToggleGroupAttribute : Attribute
    {
        public string ToggleMemberName;
        public string GroupTitle;
        public float Order;
        public bool CollapseOthersOnExpand = true;
        public ToggleGroupAttribute(string toggleMemberName, string groupTitle = null) { ToggleMemberName = toggleMemberName; GroupTitle = groupTitle; }
        public ToggleGroupAttribute(string toggleMemberName, float order, string groupTitle = null) { ToggleMemberName = toggleMemberName; Order = order; GroupTitle = groupTitle; }
    }

    // ---- Display / layout attributes --------------------------------------

    [AttributeUsage(AttributeTargets.Field | AttributeTargets.Property | AttributeTargets.Method | AttributeTargets.Class, AllowMultiple = true, Inherited = true)]
    public sealed class TitleAttribute : Attribute
    {
        public string Title;      // supports "$member" and "@expression"
        public string Subtitle;
        public bool Bold = true;
        public bool HorizontalLine = true;
        public TitleAlignments TitleAlignment = TitleAlignments.Left;
        public TitleAttribute(string title, string subtitle = null, bool horizontalLine = true, bool bold = true)
        { Title = title; Subtitle = subtitle; HorizontalLine = horizontalLine; Bold = bold; }
        public TitleAttribute(string title, string subtitle, TitleAlignments titleAlignment, bool horizontalLine = true, bool bold = true)
        { Title = title; Subtitle = subtitle; TitleAlignment = titleAlignment; HorizontalLine = horizontalLine; Bold = bold; }
    }

    [AttributeUsage(AttributeTargets.Field | AttributeTargets.Property | AttributeTargets.Method | AttributeTargets.Event, AllowMultiple = false, Inherited = true)]
    public sealed class LabelTextAttribute : Attribute
    {
        public string Text;      // supports "$member" and "@expression"
        public bool NicifyText;
        public LabelTextAttribute(string text) { Text = text; }
        public LabelTextAttribute(string text, bool nicifyText) { Text = text; NicifyText = nicifyText; }
    }

    [AttributeUsage(AttributeTargets.Field | AttributeTargets.Property, AllowMultiple = false, Inherited = true)]
    public sealed class LabelWidthAttribute : Attribute
    {
        public float Width;
        public LabelWidthAttribute(float width) { Width = width; }
    }

    [AttributeUsage(AttributeTargets.Field | AttributeTargets.Property | AttributeTargets.Method | AttributeTargets.Event, AllowMultiple = false, Inherited = true)]
    public sealed class HideLabelAttribute : Attribute { }

    /// <summary>Item wrapper for <c>[ValueDropdown]</c> option lists: a display label + value.</summary>
    public readonly struct ValueDropdownItem<T>
    {
        public readonly string Text;
        public readonly T Value;
        public ValueDropdownItem(string text, T value) { Text = text; Value = value; }
    }

    [AttributeUsage(AttributeTargets.Field | AttributeTargets.Property | AttributeTargets.Method | AttributeTargets.Event, AllowMultiple = false, Inherited = true)]
    public sealed class ShowInInspectorAttribute : Attribute { }

    [AttributeUsage(AttributeTargets.Field | AttributeTargets.Property, AllowMultiple = false, Inherited = true)]
    public sealed class DisplayAsStringAttribute : Attribute
    {
        public bool Overflow = true;
        public UnityEngine.TextAlignment Alignment = UnityEngine.TextAlignment.Left;
        public int FontSize;
        public bool EnableRichText;
        public DisplayAsStringAttribute() { }
        public DisplayAsStringAttribute(bool overflow) { Overflow = overflow; }
        public DisplayAsStringAttribute(UnityEngine.TextAlignment alignment) { Alignment = alignment; }
        public DisplayAsStringAttribute(bool overflow, UnityEngine.TextAlignment alignment) { Overflow = overflow; Alignment = alignment; }
        public DisplayAsStringAttribute(int fontSize) { FontSize = fontSize; }
    }

    [AttributeUsage(AttributeTargets.Field | AttributeTargets.Property | AttributeTargets.Event, AllowMultiple = false, Inherited = true)]
    public sealed class ReadOnlyAttribute : Attribute { }

    [AttributeUsage(AttributeTargets.Field | AttributeTargets.Property, AllowMultiple = false, Inherited = true)]
    public sealed class RequiredAttribute : Attribute
    {
        public string ErrorMessage;
        public InfoMessageType MessageType = InfoMessageType.Error;
        public RequiredAttribute() { }
        public RequiredAttribute(string errorMessage) { ErrorMessage = errorMessage; }
        public RequiredAttribute(string errorMessage, InfoMessageType messageType) { ErrorMessage = errorMessage; MessageType = messageType; }
    }

    [AttributeUsage(AttributeTargets.Field | AttributeTargets.Property | AttributeTargets.Method | AttributeTargets.Event, AllowMultiple = false, Inherited = true)]
    public sealed class PropertyOrderAttribute : Attribute
    {
        public float Order;
        public PropertyOrderAttribute(float order) { Order = order; }
    }

    [AttributeUsage(AttributeTargets.Field | AttributeTargets.Property | AttributeTargets.Method | AttributeTargets.Event, AllowMultiple = false, Inherited = true)]
    public sealed class PropertySpaceAttribute : Attribute
    {
        public float SpaceBefore;
        public float SpaceAfter;
        public PropertySpaceAttribute() { SpaceBefore = 8f; }
        public PropertySpaceAttribute(float spaceBefore) { SpaceBefore = spaceBefore; }
        public PropertySpaceAttribute(float spaceBefore, float spaceAfter) { SpaceBefore = spaceBefore; SpaceAfter = spaceAfter; }
    }

    [AttributeUsage(AttributeTargets.Field | AttributeTargets.Property, AllowMultiple = false, Inherited = true)]
    public sealed class IndentAttribute : Attribute
    {
        public int IndentLevel;
        public IndentAttribute(int indentLevel = 1) { IndentLevel = indentLevel; }
    }

    [AttributeUsage(AttributeTargets.Field | AttributeTargets.Property | AttributeTargets.Method | AttributeTargets.Event, AllowMultiple = true, Inherited = true)]
    public sealed class GUIColorAttribute : Attribute
    {
        public float R = 1f, G = 1f, B = 1f, A = 1f;
        public string GetColor;   // member name or "@expression"
        public GUIColorAttribute(float r, float g, float b, float a = 1f) { R = r; G = g; B = b; A = a; }
        public GUIColorAttribute(string getColor) { GetColor = getColor; }
    }

    [AttributeUsage(AttributeTargets.Field | AttributeTargets.Property, AllowMultiple = false, Inherited = true)]
    public sealed class MultiLinePropertyAttribute : Attribute
    {
        public int Lines;
        public MultiLinePropertyAttribute(int lines = 3) { Lines = lines; }
    }

    // ---- Info boxes --------------------------------------------------------

    [AttributeUsage(AttributeTargets.Class | AttributeTargets.Struct | AttributeTargets.Enum, AllowMultiple = true, Inherited = true)]
    public sealed class TypeInfoBoxAttribute : Attribute
    {
        public string Message;
        public TypeInfoBoxAttribute(string message) { Message = message; }
    }

    [AttributeUsage(AttributeTargets.Field | AttributeTargets.Property | AttributeTargets.Method | AttributeTargets.Class, AllowMultiple = true, Inherited = true)]
    public sealed class InfoBoxAttribute : Attribute
    {
        public string Message;
        public InfoMessageType InfoMessageType;
        public string VisibleIf;
        public bool GUIAlwaysEnabled;
        public InfoBoxAttribute(string message, InfoMessageType infoMessageType = InfoMessageType.Info, string visibleIfMemberName = null)
        { Message = message; InfoMessageType = infoMessageType; VisibleIf = visibleIfMemberName; }
    }

    [AttributeUsage(AttributeTargets.Field | AttributeTargets.Property | AttributeTargets.Method | AttributeTargets.Class, AllowMultiple = true, Inherited = true)]
    public sealed class DetailedInfoBoxAttribute : Attribute
    {
        public string Message;
        public string Details;
        public InfoMessageType InfoMessageType;
        public string VisibleIf;
        public DetailedInfoBoxAttribute(string message, string details, InfoMessageType infoMessageType = InfoMessageType.Info, string visibleIfMemberName = null)
        { Message = message; Details = details; InfoMessageType = infoMessageType; VisibleIf = visibleIfMemberName; }
    }

    // ---- Conditionals ------------------------------------------------------
    // Usage: [ShowIf("member")] or [ShowIf("member", value)]. Base carries the shared state.

    public abstract class ConditionalAttributeBase : Attribute
    {
        public string Condition;
        public object Value;
        public bool HasValue;
        protected ConditionalAttributeBase(string condition) { Condition = condition; }
        protected ConditionalAttributeBase(string condition, object value) { Condition = condition; Value = value; HasValue = true; }
    }

    [AttributeUsage(AttributeTargets.Field | AttributeTargets.Property | AttributeTargets.Method | AttributeTargets.Event, AllowMultiple = true, Inherited = true)]
    public sealed class ShowIfAttribute : ConditionalAttributeBase
    {
        public bool Animate = true;
        public ShowIfAttribute(string condition) : base(condition) { }
        public ShowIfAttribute(string condition, object value) : base(condition, value) { }
    }

    [AttributeUsage(AttributeTargets.Field | AttributeTargets.Property | AttributeTargets.Method | AttributeTargets.Event, AllowMultiple = true, Inherited = true)]
    public sealed class HideIfAttribute : ConditionalAttributeBase
    {
        public bool Animate = true;
        public HideIfAttribute(string condition) : base(condition) { }
        public HideIfAttribute(string condition, object value) : base(condition, value) { }
    }

    [AttributeUsage(AttributeTargets.Field | AttributeTargets.Property | AttributeTargets.Method | AttributeTargets.Event, AllowMultiple = true, Inherited = true)]
    public sealed class EnableIfAttribute : ConditionalAttributeBase
    {
        public EnableIfAttribute(string condition) : base(condition) { }
        public EnableIfAttribute(string condition, object value) : base(condition, value) { }
    }

    [AttributeUsage(AttributeTargets.Field | AttributeTargets.Property | AttributeTargets.Method | AttributeTargets.Event, AllowMultiple = true, Inherited = true)]
    public sealed class DisableIfAttribute : ConditionalAttributeBase
    {
        public DisableIfAttribute(string condition) : base(condition) { }
        public DisableIfAttribute(string condition, object value) : base(condition, value) { }
    }

    [AttributeUsage(AttributeTargets.Field | AttributeTargets.Property | AttributeTargets.Method | AttributeTargets.Event, AllowMultiple = false, Inherited = true)]
    public sealed class HideInEditorModeAttribute : Attribute { }

    [AttributeUsage(AttributeTargets.Field | AttributeTargets.Property | AttributeTargets.Method | AttributeTargets.Event, AllowMultiple = false, Inherited = true)]
    public sealed class HideInPlayModeAttribute : Attribute { }

    [AttributeUsage(AttributeTargets.Field | AttributeTargets.Property | AttributeTargets.Method | AttributeTargets.Event, AllowMultiple = false, Inherited = true)]
    public sealed class ShowInPlayModeAttribute : Attribute { }

    [AttributeUsage(AttributeTargets.Field | AttributeTargets.Property | AttributeTargets.Method | AttributeTargets.Event, AllowMultiple = false, Inherited = true)]
    public sealed class DisableInEditorModeAttribute : Attribute { }

    [AttributeUsage(AttributeTargets.Field | AttributeTargets.Property | AttributeTargets.Method | AttributeTargets.Event, AllowMultiple = false, Inherited = true)]
    public sealed class DisableInPlayModeAttribute : Attribute { }

    // ---- Numeric / range ---------------------------------------------------

    [AttributeUsage(AttributeTargets.Field | AttributeTargets.Property, AllowMultiple = false, Inherited = true)]
    public sealed class PropertyRangeAttribute : Attribute
    {
        public double Min;
        public double Max;
        public string MinGetter;
        public string MaxGetter;
        public PropertyRangeAttribute(double min, double max) { Min = min; Max = max; }
        public PropertyRangeAttribute(double min, string maxGetter) { Min = min; MaxGetter = maxGetter; }
        public PropertyRangeAttribute(string minGetter, double max) { MinGetter = minGetter; Max = max; }
        public PropertyRangeAttribute(string minGetter, string maxGetter) { MinGetter = minGetter; MaxGetter = maxGetter; }
    }

    [AttributeUsage(AttributeTargets.Field | AttributeTargets.Property, AllowMultiple = false, Inherited = true)]
    public sealed class MinMaxSliderAttribute : Attribute
    {
        public float MinValue;
        public float MaxValue;
        public bool ShowFields;
        public string MinValueGetter;
        public string MaxValueGetter;
        public string MinMaxValueGetter;   // member returning Vector2 (x=min, y=max)
        public MinMaxSliderAttribute(float minValue, float maxValue, bool showFields = false)
        { MinValue = minValue; MaxValue = maxValue; ShowFields = showFields; }
        public MinMaxSliderAttribute(string minValueGetter, float maxValue, bool showFields = false)
        { MinValueGetter = minValueGetter; MaxValue = maxValue; ShowFields = showFields; }
        public MinMaxSliderAttribute(float minValue, string maxValueGetter, bool showFields = false)
        { MinValue = minValue; MaxValueGetter = maxValueGetter; ShowFields = showFields; }
        public MinMaxSliderAttribute(string minValueGetter, string maxValueGetter, bool showFields = false)
        { MinValueGetter = minValueGetter; MaxValueGetter = maxValueGetter; ShowFields = showFields; }
        public MinMaxSliderAttribute(string minMaxValueGetter, bool showFields = false)
        { MinMaxValueGetter = minMaxValueGetter; ShowFields = showFields; }
    }

    [AttributeUsage(AttributeTargets.Field | AttributeTargets.Property, AllowMultiple = false, Inherited = true)]
    public sealed class ProgressBarAttribute : Attribute
    {
        public double Min;
        public double Max;
        public string MinGetter;
        public string MaxGetter;
        public float R = 0.15f, G = 0.47f, B = 0.74f;
        public string ColorGetter;             // member returning Color
        public string BackgroundColorGetter;
        public bool Segmented;
        public int Height = 12;
        public bool DrawValueLabel = true;
        public UnityEngine.TextAlignment ValueLabelAlignment = UnityEngine.TextAlignment.Center;
        public string CustomValueStringGetter; // member/$-string producing the label text
        public ProgressBarAttribute(double min, double max) { Min = min; Max = max; }
        public ProgressBarAttribute(string minGetter, double max) { MinGetter = minGetter; Max = max; }
        public ProgressBarAttribute(double min, string maxGetter) { Min = min; MaxGetter = maxGetter; }
        public ProgressBarAttribute(string minGetter, string maxGetter) { MinGetter = minGetter; MaxGetter = maxGetter; }
        public ProgressBarAttribute(double min, double max, float r, float g, float b) { Min = min; Max = max; R = r; G = g; B = b; }
    }

    [AttributeUsage(AttributeTargets.Field | AttributeTargets.Property, AllowMultiple = false, Inherited = true)]
    public sealed class WrapAttribute : Attribute
    {
        public double Min;
        public double Max;
        public WrapAttribute(double min, double max) { Min = min; Max = max; }
    }

    [AttributeUsage(AttributeTargets.Field | AttributeTargets.Property, AllowMultiple = false, Inherited = true)]
    public sealed class EnumToggleButtonsAttribute : Attribute { }

    [AttributeUsage(AttributeTargets.Field | AttributeTargets.Property, AllowMultiple = false, Inherited = true)]
    public sealed class ToggleLeftAttribute : Attribute { }

    [AttributeUsage(AttributeTargets.Field | AttributeTargets.Property, AllowMultiple = false, Inherited = true)]
    public sealed class DictionaryDrawerSettingsAttribute : Attribute
    {
        public string KeyLabel = "Key";
        public string ValueLabel = "Value";
        public bool IsReadOnly;
        public int DisplayMode;
        public int KeyColumnWidth;
        public int ValueColumnWidth;
        public DictionaryDrawerSettingsAttribute() { }
    }

    [AttributeUsage(AttributeTargets.Field | AttributeTargets.Property, AllowMultiple = false, Inherited = true)]
    public sealed class MinValueAttribute : Attribute
    {
        public double Min;
        public string Expression;
        public MinValueAttribute(double min) { Min = min; }
        public MinValueAttribute(string expression) { Expression = expression; }
    }

    [AttributeUsage(AttributeTargets.Field | AttributeTargets.Property, AllowMultiple = false, Inherited = true)]
    public sealed class MaxValueAttribute : Attribute
    {
        public double Max;
        public string Expression;
        public MaxValueAttribute(double max) { Max = max; }
        public MaxValueAttribute(string expression) { Expression = expression; }
    }

    // ---- Collections / assets / dropdowns ----------------------------------

    [AttributeUsage(AttributeTargets.Field | AttributeTargets.Property, AllowMultiple = false, Inherited = true)]
    public sealed class ListDrawerSettingsAttribute : Attribute
    {
        public bool ShowIndexLabels;
        public bool DraggableItems = true;
        public bool ShowFoldout = true;
        public bool IsReadOnly;
        public bool ShowPaging = true;
        public bool HideAddButton;
        public bool HideRemoveButton;
        public bool DefaultExpandedState;
        public string ListElementLabelName;
        public string CustomAddFunction;
        public string CustomRemoveElementFunction;
        public string CustomRemoveIndexFunction;
        public string OnTitleBarGUI;
        public string OnBeginListElementGUI;   // method(int index)
        public string OnEndListElementGUI;     // method(int index)
        public bool AddCopiesLastElement;
        public bool AlwaysAddDefaultValue;
        public int NumberOfItemsPerPage;
        public bool Expanded;
        public ListDrawerSettingsAttribute() { }
    }

    [AttributeUsage(AttributeTargets.Field | AttributeTargets.Property | AttributeTargets.Class | AttributeTargets.Struct, AllowMultiple = false, Inherited = true)]
    public sealed class TableListAttribute : Attribute
    {
        public bool ShowIndexLabels;
        public bool DrawScrollView = true;
        public bool IsReadOnly;
        public bool AlwaysExpanded;
        public bool HideToolbar;
        public bool ShowPaging;
        public bool ShowItemCount = true;
        public int NumberOfItemsPerPage;
        public int MaxScrollViewHeight;
        public int MinScrollViewHeight;
        public int DefaultMinColumnWidth = 40;
        public TableListAttribute() { }
    }

    [AttributeUsage(AttributeTargets.Field | AttributeTargets.Property | AttributeTargets.Method | AttributeTargets.Event, AllowMultiple = false, Inherited = true)]
    public sealed class TableColumnWidthAttribute : Attribute
    {
        public int Width;
        public bool Resizable = true;
        public TableColumnWidthAttribute(int width, bool resizable = true) { Width = width; Resizable = resizable; }
    }

    [AttributeUsage(AttributeTargets.Field | AttributeTargets.Property, AllowMultiple = false, Inherited = true)]
    public sealed class ValueDropdownAttribute : Attribute
    {
        public string ValuesGetter;
        public int NumberOfItemsBeforeEnablingSearch = 10;
        public bool IsUniqueList;
        public bool DrawDropdownForListElements = true;
        public bool ExpandAllMenuItems;
        public bool AppendNextDrawer;
        public bool DisableListAddButtonBehaviour;
        public bool SortDropdownItems;
        public bool HideChildProperties;
        public bool FlattenTreeView;
        public string DropdownTitle;
        public ValueDropdownAttribute(string valuesGetter) { ValuesGetter = valuesGetter; }
    }

    [AttributeUsage(AttributeTargets.Field | AttributeTargets.Property, AllowMultiple = false, Inherited = true)]
    public sealed class AssetSelectorAttribute : Attribute
    {
        public string Filter;                  // AssetDatabase.FindAssets filter; type filter added automatically
        public string Paths;                   // "|"-separated search folders
        public bool IsUniqueList = true;
        public bool DrawDropdownForListElements = true;
        public bool ExcludeExistingValuesInList;
        public bool ExpandAllMenuItems = true;
        public bool FlattenTreeView;
        public string DropdownTitle;
        public AssetSelectorAttribute() { }
    }

    [AttributeUsage(AttributeTargets.Field | AttributeTargets.Property, AllowMultiple = false, Inherited = true)]
    public sealed class AssetsOnlyAttribute : Attribute { }

    [AttributeUsage(AttributeTargets.Field | AttributeTargets.Property, AllowMultiple = false, Inherited = true)]
    public sealed class SceneObjectsOnlyAttribute : Attribute { }

    [AttributeUsage(AttributeTargets.Field | AttributeTargets.Property, AllowMultiple = false, Inherited = true)]
    public sealed class SearchableAttribute : Attribute
    {
        public bool Recursive = true;
        public SearchableAttribute() { }
    }

    // ---- Inline / composite -------------------------------------------------

    [AttributeUsage(AttributeTargets.Field | AttributeTargets.Property | AttributeTargets.Class | AttributeTargets.Struct, AllowMultiple = false, Inherited = true)]
    public sealed class InlinePropertyAttribute : Attribute
    {
        public int LabelWidth;
        public InlinePropertyAttribute() { }
        public InlinePropertyAttribute(int labelWidth) { LabelWidth = labelWidth; }
    }

    [AttributeUsage(AttributeTargets.Field | AttributeTargets.Property, AllowMultiple = false, Inherited = true)]
    public sealed class InlineEditorAttribute : Attribute
    {
        public bool Expanded;
        public bool DrawHeader = true;
        public bool DrawGUI = true;
        public bool DrawPreview;
        public float MaxHeight;
        public float PreviewWidth = 100f;
        public float PreviewHeight = 35f;
        public InlineEditorModes Mode = InlineEditorModes.GUIOnly;
        public InlineEditorObjectFieldModes ObjectFieldMode = InlineEditorObjectFieldModes.Boxed;
        public InlineEditorAttribute() { }
        public InlineEditorAttribute(InlineEditorModes mode)
        {
            Mode = mode;
            switch (mode)
            {
                case InlineEditorModes.GUIOnly: DrawGUI = true; DrawHeader = false; DrawPreview = false; break;
                case InlineEditorModes.GUIAndHeader: DrawGUI = true; DrawHeader = true; DrawPreview = false; break;
                case InlineEditorModes.GUIAndPreview: DrawGUI = true; DrawHeader = false; DrawPreview = true; break;
                case InlineEditorModes.SmallPreview: DrawGUI = false; DrawHeader = false; DrawPreview = true; Expanded = true; break;
                case InlineEditorModes.LargePreview: DrawGUI = false; DrawHeader = false; DrawPreview = true; Expanded = true; PreviewHeight = 170f; break;
                case InlineEditorModes.FullEditor: DrawGUI = true; DrawHeader = true; DrawPreview = true; break;
            }
        }
        public InlineEditorAttribute(InlineEditorObjectFieldModes objectFieldMode) { ObjectFieldMode = objectFieldMode; }
    }

    [AttributeUsage(AttributeTargets.Field | AttributeTargets.Property | AttributeTargets.Class | AttributeTargets.Struct, AllowMultiple = false, Inherited = true)]
    public sealed class HideReferenceObjectPickerAttribute : Attribute { }

    [AttributeUsage(AttributeTargets.Method | AttributeTargets.Field | AttributeTargets.Property, AllowMultiple = true, Inherited = true)]
    public sealed class OnInspectorInitAttribute : Attribute
    {
        public string Action;
        public OnInspectorInitAttribute() { }
        public OnInspectorInitAttribute(string action) { Action = action; }
    }

    [AttributeUsage(AttributeTargets.Field | AttributeTargets.Property, AllowMultiple = false, Inherited = true)]
    public sealed class PreviewFieldAttribute : Attribute
    {
        public float Height;
        public ObjectFieldAlignment Alignment = ObjectFieldAlignment.Left;
        public PreviewFieldAttribute() { }
        public PreviewFieldAttribute(float height) { Height = height; }
        public PreviewFieldAttribute(ObjectFieldAlignment alignment) { Alignment = alignment; }
        public PreviewFieldAttribute(float height, ObjectFieldAlignment alignment) { Height = height; Alignment = alignment; }
    }

    [AttributeUsage(AttributeTargets.Field | AttributeTargets.Property | AttributeTargets.Class | AttributeTargets.Struct, AllowMultiple = false, Inherited = true)]
    public sealed class DrawWithUnityAttribute : Attribute { }
}
