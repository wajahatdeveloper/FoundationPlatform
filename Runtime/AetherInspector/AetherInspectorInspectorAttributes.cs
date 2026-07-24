using System;

namespace AetherNexus.FoundationPlatform.AetherInspector
{
    // ---- Enums -------------------------------------------------------------

    /// <summary>Preset button height ranges used by <see cref="ButtonAttribute"/>.</summary>
    public enum ButtonSizes { Small = 0, Medium = 1, Large = 2, Gigantic = 3 }

    /// <summary>Visual severity of an info box or validation message.</summary>
    public enum InfoMessageType { None = 0, Info = 1, Warning = 2, Error = 3 }

    /// <summary>Horizontal alignment for an inline object field preview.</summary>
    public enum ObjectFieldAlignment { Left = 0, Center = 1, Right = 2 }

    /// <summary>Which parts of an inline editor are drawn.</summary>
    public enum InlineEditorModes { GUIOnly, GUIAndHeader, GUIAndPreview, SmallPreview, LargePreview, FullEditor }

    /// <summary>How the object-field picker of an <see cref="InlineEditorAttribute"/> is displayed.</summary>
    public enum InlineEditorObjectFieldModes { Boxed, Foldout, Hidden, CompletelyHidden }

    /// <summary>Horizontal alignment for a section title.</summary>
    public enum TitleAlignments { Left = 0, Centered = 1, Right = 2, Split = 3 }

    /// <summary>Visual chrome style for <see cref="ButtonAttribute"/>.</summary>
    public enum ButtonStyle { CompactBox = 0, FoldoutButton = 1, Box = 2 }

    /// <summary>Where an icon sits relative to button text.</summary>
    public enum IconAlignment { LeftOfText = 0, RightOfText = 1, LeftEdge = 2, RightEdge = 3 }

    /// <summary>Display mode for lists and dictionaries in the inspector.</summary>
    public enum ListDisplayMode { Default = 0, Expanded = 1, Collapsed = 2 }

    // ---- Action attributes -------------------------------------------------

    /// <summary>Draws a clickable button inside the inspector for a method, field, or property.</summary>
    [AttributeUsage(AttributeTargets.Method | AttributeTargets.Field | AttributeTargets.Property, AllowMultiple = true, Inherited = true)]
    public sealed class ButtonAttribute : Attribute
    {
        /// <summary>Override button label text.</summary>
        public string Name;
        /// <summary>Preset height category applied when <see cref="ButtonHeightPixels"/> is 0.</summary>
        public ButtonSizes ButtonHeight;
        /// <summary>Explicit pixel height; wins over <see cref="ButtonHeight"/> when greater than 0.</summary>
        public int ButtonHeightPixels;
        /// <summary>For parameterized methods, draw the parameter area expanded by default.</summary>
        public bool Expanded;
        /// <summary>Whether to draw the parameter area for parameterized methods.</summary>
        public bool DisplayParameters = true;
        /// <summary>Whether the object is marked dirty after the button is clicked.</summary>
        public bool DirtyOnClick = true;
        /// <summary>Whether the method return value is drawn beneath the button.</summary>
        public bool DrawResult = true;
        /// <summary>Editor icon name passed to <c>EditorGUIUtility.IconContent</c>.</summary>
        public string Icon;
        /// <summary>Horizontal placement of the icon relative to the button text.</summary>
        public IconAlignment IconAlignment = IconAlignment.LeftOfText;
        /// <summary>Chrome style used to render the button.</summary>
        public ButtonStyle Style = ButtonStyle.CompactBox;
        /// <summary>Horizontal alignment of the button within its layout row.</summary>
        public ButtonAlignment ButtonAlignment = ButtonAlignment.Stretch;
        /// <summary>Legacy flag; when true the button stretches to fill its container.</summary>
        public bool Stretch = true;

        /// <summary>Default constructor.</summary>
        public ButtonAttribute() { }
        /// <summary>Set the preset button height.</summary>
        /// <param name="size">Height preset to apply.</param>
        public ButtonAttribute(ButtonSizes size) { ButtonHeight = size; }
        /// <summary>Set only the button label text.</summary>
        /// <param name="name">Label shown on the button.</param>
        public ButtonAttribute(string name) { Name = name; }
        /// <summary>Set the button label text and preset height.</summary>
        /// <param name="name">Label shown on the button.</param>
        /// <param name="size">Height preset to apply.</param>
        public ButtonAttribute(string name, ButtonSizes size) { Name = name; ButtonHeight = size; }
        /// <summary>Set an explicit pixel button height.</summary>
        /// <param name="buttonSize">Height in pixels.</param>
        public ButtonAttribute(int buttonSize) { ButtonHeightPixels = buttonSize; }
        /// <summary>Set the button label text and explicit pixel height.</summary>
        /// <param name="name">Label shown on the button.</param>
        /// <param name="buttonSize">Height in pixels.</param>
        public ButtonAttribute(string name, int buttonSize) { Name = name; ButtonHeightPixels = buttonSize; }
    }

    /// <summary>Horizontal alignment used by <see cref="ButtonAttribute.ButtonAlignment"/>.</summary>
    public enum ButtonAlignment { Left = 0, Center = 1, Right = 2, Stretch = 3 }

    /// <summary>Groups buttons into a named set for optional compact rendering.</summary>
    [AttributeUsage(AttributeTargets.Method | AttributeTargets.Field | AttributeTargets.Property, AllowMultiple = true, Inherited = true)]
    public sealed class ButtonGroupAttribute : Attribute
    {
        /// <summary>Shared group identifier.</summary>
        public string GroupID;
        /// <summary>Sort order within the group; lower values appear first.</summary>
        public float Order;
        /// <summary>Create an unnamed button group.</summary>
        public ButtonGroupAttribute() { GroupID = string.Empty; }
        /// <summary>Create a named button group.</summary>
        /// <param name="groupId">Group identifier string.</param>
        /// <param name="order">Sort order within the group.</param>
        public ButtonGroupAttribute(string groupId, float order = 0f) { GroupID = groupId; Order = order; }
    }

    /// <summary>Draws an inline action button next to a field in the same horizontal row.</summary>
    [AttributeUsage(AttributeTargets.Method | AttributeTargets.Field | AttributeTargets.Property, AllowMultiple = true, Inherited = true)]
    public sealed class InlineButtonAttribute : Attribute
    {
        /// <summary>Method name invoked when the button is clicked.</summary>
        public string Action;
        /// <summary>Override label text; falls back to the action name.</summary>
        public string Label;
        /// <summary>Editor icon name passed to <c>EditorGUIUtility.IconContent</c>.</summary>
        public string Icon;
        /// <summary>Member name or expression that gates visibility.</summary>
        public string ShowIf;
        /// <summary>Bind an inline button to an action method.</summary>
        /// <param name="action">Method name to invoke.</param>
        public InlineButtonAttribute(string action) { Action = action; }
        /// <summary>Bind an inline button to an action method with a custom label.</summary>
        /// <param name="action">Method name to invoke.</param>
        /// <param name="label">Label shown on the button.</param>
        public InlineButtonAttribute(string action, string label) { Action = action; Label = label; }
    }

    /// <summary>Draws a button on an object reference field that adds or assigns a required component.</summary>
    [AttributeUsage(AttributeTargets.Field | AttributeTargets.Property, AllowMultiple = false, Inherited = true)]
    public sealed class RequireComponentButtonAttribute : Attribute
    {
        /// <summary>Component type to add or assign.</summary>
        public Type ComponentType;
        /// <summary>Custom button label.</summary>
        public string Label;
        /// <summary>Editor icon name passed to <c>EditorGUIUtility.IconContent</c>.</summary>
        public string Icon;
        /// <summary>Require a component type by default.</summary>
        public RequireComponentButtonAttribute() { }
        /// <summary>Require a specific component type.</summary>
        /// <param name="type">Component type to ensure exists on the GameObject.</param>
        public RequireComponentButtonAttribute(Type type) { ComponentType = type; }
        /// <summary>Require a specific component type with a custom button label.</summary>
        /// <param name="type">Component type to ensure exists on the GameObject.</param>
        /// <param name="label">Label shown on the button.</param>
        public RequireComponentButtonAttribute(Type type, string label) { ComponentType = type; Label = label; }
    }

    /// <summary>Invokes a method when the value of the decorated member changes.</summary>
    [AttributeUsage(AttributeTargets.Field | AttributeTargets.Property, AllowMultiple = true, Inherited = true)]
    public sealed class OnValueChangedAttribute : Attribute
    {
        /// <summary>Method name to invoke on change.</summary>
        public string Action;
        /// <summary>When true, changes to nested children also trigger the callback.</summary>
        public bool IncludeChildren;
        /// <summary>When true, the action is also invoked during initialization.</summary>
        public bool InvokeOnInitialize;
        /// <summary>Bind a value-changed callback to a method.</summary>
        /// <param name="action">Method name to invoke.</param>
        /// <param name="includeChildren">Trigger on nested child value changes as well.</param>
        public OnValueChangedAttribute(string action, bool includeChildren = false) { Action = action; IncludeChildren = includeChildren; }
    }

    /// <summary>Injects custom inspector GUI around the decorated member.</summary>
    [AttributeUsage(AttributeTargets.Field | AttributeTargets.Property | AttributeTargets.Method | AttributeTargets.Class | AttributeTargets.Struct, AllowMultiple = true, Inherited = true)]
    public sealed class OnInspectorGUIAttribute : Attribute
    {
        /// <summary>Method name to invoke before the member is drawn.</summary>
        public string Prepend;
        /// <summary>Method name to invoke after the member is drawn.</summary>
        public string Append;
        /// <summary>Create a prepend-only inspector GUI hook.</summary>
        public OnInspectorGUIAttribute() { }
        /// <summary>Create prepend/append inspector GUI hooks.</summary>
        /// <param name="prepend">Method called before the member draws.</param>
        /// <param name="append">Method called after the member draws.</param>
        public OnInspectorGUIAttribute(string prepend, string append = null) { Prepend = prepend; Append = append; }
    }

    /// <summary>Notifies before and after a collection mutation occurs.</summary>
    [AttributeUsage(AttributeTargets.Field | AttributeTargets.Property, AllowMultiple = true, Inherited = true)]
    public sealed class OnCollectionChangedAttribute : Attribute
    {
        /// <summary>Method name invoked before the collection changes.</summary>
        public string Before;
        /// <summary>Method name invoked after the collection changes.</summary>
        public string After;
        /// <summary>Only invoke an after-change callback.</summary>
        /// <param name="after">Method called after the collection mutates.</param>
        public OnCollectionChangedAttribute(string after) { After = after; }
        /// <summary>Invoke callbacks before and after collection mutations.</summary>
        /// <param name="before">Method called before the collection mutates.</param>
        /// <param name="after">Method called after the collection mutates.</param>
        public OnCollectionChangedAttribute(string before, string after) { Before = before; After = after; }
    }

    /// <summary>Validates a field or property against a boolean condition and displays a message if invalid.</summary>
    [AttributeUsage(AttributeTargets.Field | AttributeTargets.Property, AllowMultiple = true, Inherited = true)]
    public sealed class ValidateInputAttribute : Attribute
    {
        /// <summary>Member or method name whose return value is evaluated as the condition.</summary>
        public string Condition;
        /// <summary>Message shown when the condition evaluates to false.</summary>
        public string DefaultMessage;
        /// <summary>Visual severity of the validation message.</summary>
        public InfoMessageType MessageType;
        /// <summary>When true, nested children are also validated.</summary>
        public bool IncludeChildren;
        /// <summary>Validate a member against a condition with an error message.</summary>
        /// <param name="condition">Member or method name to evaluate.</param>
        /// <param name="defaultMessage">Message displayed when validation fails.</param>
        /// <param name="messageType">Visual severity of the validation message.</param>
        public ValidateInputAttribute(string condition) { Condition = condition; MessageType = InfoMessageType.Error; }
        /// <summary>Validate a member against a condition with a custom message and severity.</summary>
        /// <param name="condition">Member or method name to evaluate.</param>
        /// <param name="defaultMessage">Message displayed when validation fails.</param>
        /// <param name="messageType">Visual severity of the validation message.</param>
        public ValidateInputAttribute(string condition, string defaultMessage) { Condition = condition; DefaultMessage = defaultMessage; MessageType = InfoMessageType.Error; }
        /// <summary>Validate a member against a condition with full customization.</summary>
        /// <param name="condition">Member or method name to evaluate.</param>
        /// <param name="defaultMessage">Message displayed when validation fails.</param>
        /// <param name="messageType">Visual severity of the validation message.</param>
        public ValidateInputAttribute(string condition, string defaultMessage, InfoMessageType messageType) { Condition = condition; DefaultMessage = defaultMessage; MessageType = messageType; }
    }

    // ---- Group attributes --------------------------------------------------
    // Group attributes share a "GroupID" path (segments split on '/') and an Order.

    /// <summary>Draws fields inside a boxed group with an optional header label.</summary>
    [AttributeUsage(AttributeTargets.Field | AttributeTargets.Property | AttributeTargets.Method | AttributeTargets.Event, AllowMultiple = true, Inherited = true)]
    public sealed class BoxGroupAttribute : Attribute
    {
        /// <summary>Group identifier; segments split on <c>'/'</c> create nested groups.</summary>
        public string GroupID;
        /// <summary>When false, the group header label is hidden.</summary>
        public bool ShowLabel;
        /// <summary>When true, the group label is centered.</summary>
        public bool CenterLabel;
        /// <summary>Sort order; lower values appear first.</summary>
        public float Order;
        /// <summary>Override text for the group label.</summary>
        public string LabelText;
        /// <summary>Add a field to a box group.</summary>
        /// <param name="group">Group identifier string.</param>
        /// <param name="showLabel">Show the group header label.</param>
        /// <param name="centerLabel">Center-align the group label.</param>
        /// <param name="order">Sort order within the group.</param>
        public BoxGroupAttribute(string group, bool showLabel = true, bool centerLabel = false, float order = 0f)
        { GroupID = group; ShowLabel = showLabel; CenterLabel = centerLabel; Order = order; }
    }

    /// <summary>Draws fields inside a foldout group with an optional expansion state.</summary>
    [AttributeUsage(AttributeTargets.Field | AttributeTargets.Property | AttributeTargets.Method | AttributeTargets.Event, AllowMultiple = true, Inherited = true)]
    public sealed class FoldoutGroupAttribute : Attribute
    {
        /// <summary>Group identifier; segments split on <c>'/'</c> create nested groups.</summary>
        public string GroupID;
        /// <summary>Initial expanded/collapsed state of the foldout.</summary>
        public bool Expanded;
        /// <summary>Whether <see cref="Expanded"/> was explicitly set; controls persistence.</summary>
        public bool HasDefinedExpanded;
        /// <summary>Sort order; lower values appear first.</summary>
        public float Order;
        /// <summary>Member name or expression that gates visibility of the entire group.</summary>
        public string VisibleIf;
        /// <summary>Add a field to a foldout group with default expanded state.</summary>
        /// <param name="group">Group identifier string.</param>
        /// <param name="order">Sort order within the group.</param>
        public FoldoutGroupAttribute(string group, float order = 0f) { GroupID = group; Order = order; }
        /// <summary>Add a field to a foldout group with an explicit expanded state.</summary>
        /// <param name="group">Group identifier string.</param>
        /// <param name="expanded">Whether the foldout starts expanded.</param>
        /// <param name="order">Sort order within the group.</param>
        public FoldoutGroupAttribute(string group, bool expanded, float order = 0f)
        { GroupID = group; Expanded = expanded; HasDefinedExpanded = true; Order = order; }
    }

    /// <summary>Draws a title block (with optional subtitle, line, and alignment) before grouped fields.</summary>
    [AttributeUsage(AttributeTargets.Field | AttributeTargets.Property | AttributeTargets.Method | AttributeTargets.Event, AllowMultiple = true, Inherited = true)]
    public sealed class TitleGroupAttribute : Attribute
    {
        /// <summary>Group identifier; also used as the title text.</summary>
        public string GroupID;
        /// <summary>Subtitle rendered below the title.</summary>
        public string Subtitle;
        /// <summary>Sort order; lower values appear first.</summary>
        public float Order;
        /// <summary>Render the title in bold.</summary>
        public bool BoldTitle = true;
        /// <summary>Draw a horizontal rule below the title.</summary>
        public bool HorizontalLine = true;
        /// <summary>Indent the group contents one level.</summary>
        public bool Indent;
        /// <summary>Horizontal alignment of the title text.</summary>
        public TitleAlignments Alignment = TitleAlignments.Left;
        /// <summary>Create a title group with just a title string.</summary>
        /// <param name="title">Title text shown in the inspector.</param>
        /// <param name="subtitle">Optional subtitle text.</param>
        /// <param name="order">Sort order within the group.</param>
        public TitleGroupAttribute(string title, string subtitle = null, float order = 0f)
        { GroupID = title; Subtitle = subtitle; Order = order; }
        /// <summary>Create a fully configured title group.</summary>
        /// <param name="title">Title text shown in the inspector.</param>
        /// <param name="subtitle">Optional subtitle text.</param>
        /// <param name="alignment">Horizontal alignment of the title.</param>
        /// <param name="horizontalLine">Draw a rule under the title.</param>
        /// <param name="boldTitle">Render title in bold.</param>
        /// <param name="indent">Indent contents one level.</param>
        /// <param name="order">Sort order within the group.</param>
        public TitleGroupAttribute(string title, string subtitle, TitleAlignments alignment,
            bool horizontalLine = true, bool boldTitle = true, bool indent = false, float order = 0f)
        { GroupID = title; Subtitle = subtitle; Alignment = alignment; HorizontalLine = horizontalLine; BoldTitle = boldTitle; Indent = indent; Order = order; }
    }

    /// <summary>Assigns a field to a named tab within a tab group.</summary>
    [AttributeUsage(AttributeTargets.Field | AttributeTargets.Property | AttributeTargets.Method | AttributeTargets.Event, AllowMultiple = true, Inherited = true)]
    public sealed class TabGroupAttribute : Attribute
    {
        /// <summary>Container group id that owns the tab strip.</summary>
        public string GroupID;
        /// <summary>Tab name displayed in the tab strip.</summary>
        public string TabName;
        /// <summary>Sort order; lower values appear first.</summary>
        public float Order;
        /// <summary>When true, removes padding around the tab content.</summary>
        public bool Paddingless;
        /// <summary>When true, hides the entire tab strip if only one tab exists.</summary>
        public bool HideTabGroupIfTabGroupOnlyHasOneTab;
        /// <summary>Default container group id used when no group is specified.</summary>
        public const string DEFAULT_GROUP = "_DefaultTabGroup";
        /// <summary>Create a tab on the default tab group.</summary>
        /// <param name="tab">Tab name displayed in the tab strip.</param>
        public TabGroupAttribute(string tab) { GroupID = DEFAULT_GROUP; TabName = tab; }
        /// <summary>Create a tab on a specific container group.</summary>
        /// <param name="group">Container group id.</param>
        /// <param name="tab">Tab name displayed in the tab strip.</param>
        /// <param name="order">Sort order; lower values appear first.</param>
        public TabGroupAttribute(string group, string tab, float order = 0f) { GroupID = group; TabName = tab; Order = order; }
    }

    /// <summary>Places a field into a horizontal layout column within a horizontal group.</summary>
    [AttributeUsage(AttributeTargets.Field | AttributeTargets.Property | AttributeTargets.Method | AttributeTargets.Event, AllowMultiple = true, Inherited = true)]
    public sealed class HorizontalGroupAttribute : Attribute
    {
        /// <summary>Group identifier.</summary>
        public string GroupID;
        /// <summary>Column width: 0 = flexible, less than 1 = percentage of remaining space, 1 or greater = pixels.</summary>
        public float Width;
        /// <summary>Gap between adjacent columns in pixels.</summary>
        public float Gap = 3f;
        /// <summary>Left margin applied before this column in pixels.</summary>
        public float MarginLeft;
        /// <summary>Right margin applied after this column in pixels.</summary>
        public float MarginRight;
        /// <summary>Inner left padding in pixels.</summary>
        public float PaddingLeft;
        /// <summary>Inner right padding in pixels.</summary>
        public float PaddingRight;
        /// <summary>Minimum column width in pixels.</summary>
        public float MinWidth;
        /// <summary>Maximum column width in pixels.</summary>
        public float MaxWidth;
        /// <summary>Override label width for the member in this column.</summary>
        public float LabelWidth;
        /// <summary>Sort order; lower values appear first.</summary>
        public float Order;
        /// <summary>Title drawn above the entire horizontal group.</summary>
        public string Title;
        /// <summary>Add a field to a named horizontal group.</summary>
        /// <param name="group">Group identifier string.</param>
        /// <param name="width">Default column width.</param>
        /// <param name="marginLeft">Left margin in pixels.</param>
        /// <param name="marginRight">Right margin in pixels.</param>
        /// <param name="order">Sort order within the group.</param>
        public HorizontalGroupAttribute(string group, float width = 0f, int marginLeft = 0, int marginRight = 0, float order = 0f)
        { GroupID = group; Width = width; MarginLeft = marginLeft; MarginRight = marginRight; Order = order; }
        /// <summary>Add a field to the default horizontal group.</summary>
        /// <param name="width">Default column width.</param>
        /// <param name="marginLeft">Left margin in pixels.</param>
        /// <param name="marginRight">Right margin in pixels.</param>
        /// <param name="order">Sort order within the group.</param>
        public HorizontalGroupAttribute(float width = 0f, int marginLeft = 0, int marginRight = 0, float order = 0f)
        { GroupID = "_DefaultHorizontalGroup"; Width = width; MarginLeft = marginLeft; MarginRight = marginRight; Order = order; }
    }

    /// <summary>Adds vertical padding to a grouped set of members.</summary>
    [AttributeUsage(AttributeTargets.Field | AttributeTargets.Property | AttributeTargets.Method | AttributeTargets.Event, AllowMultiple = true, Inherited = true)]
    public sealed class VerticalGroupAttribute : Attribute
    {
        /// <summary>Group identifier.</summary>
        public string GroupID;
        /// <summary>Sort order; lower values appear first.</summary>
        public float Order;
        /// <summary>Space added above the first member in the group (in pixels).</summary>
        public float PaddingTop;
        /// <summary>Space added below the last member in the group (in pixels).</summary>
        public float PaddingBottom;
        /// <summary>Add a field to a vertical group.</summary>
        /// <param name="group">Group identifier string.</param>
        /// <param name="order">Sort order within the group.</param>
        public VerticalGroupAttribute(string group, float order = 0f) { GroupID = group; Order = order; }
    }

    /// <summary>Maps a boolean field to a named toggle group that shows/hides an entire section.</summary>
    [AttributeUsage(AttributeTargets.Field | AttributeTargets.Property | AttributeTargets.Method | AttributeTargets.Event, AllowMultiple = true, Inherited = true)]
    public sealed class ToggleGroupAttribute : Attribute
    {
        /// <summary>Name of the boolean field or property controlling this group.</summary>
        public string ToggleMemberName;
        /// <summary>Header text displayed above the toggle group.</summary>
        public string GroupTitle;
        /// <summary>Sort order; lower values appear first.</summary>
        public float Order;
        /// <summary>When true (default), expanding this group collapses all other toggle groups.</summary>
        public bool CollapseOthersOnExpand = true;
        /// <summary>Create a toggle group.</summary>
        /// <param name="toggleMemberName">Member name or path used as the toggle.</param>
        /// <param name="groupTitle">Header text for the group.</param>
        public ToggleGroupAttribute(string toggleMemberName, string groupTitle = null) { ToggleMemberName = toggleMemberName; GroupTitle = groupTitle; }
        /// <summary>Create a toggle group with explicit order.</summary>
        /// <param name="toggleMemberName">Member name or path used as the toggle.</param>
        /// <param name="order">Sort order within the group.</param>
        /// <param name="groupTitle">Header text for the group.</param>
        public ToggleGroupAttribute(string toggleMemberName, float order, string groupTitle = null) { ToggleMemberName = toggleMemberName; Order = order; GroupTitle = groupTitle; }
    }

    // ---- Display / layout attributes --------------------------------------

    /// <summary>Draws a title block (with optional subtitle, line, and alignment) inline before a member.</summary>
    [AttributeUsage(AttributeTargets.Field | AttributeTargets.Property | AttributeTargets.Method | AttributeTargets.Class, AllowMultiple = true, Inherited = true)]
    public sealed class TitleAttribute : Attribute
    {
        /// <summary>Title text; supports <c>$member</c> and <c>@expression</c> syntax.</summary>
        public string Title;
        /// <summary>Subtitle text; supports <c>$member</c> and <c>@expression</c> syntax.</summary>
        public string Subtitle;
        /// <summary>Render the title in bold.</summary>
        public bool Bold = true;
        /// <summary>Draw a horizontal rule below the title.</summary>
        public bool HorizontalLine = true;
        /// <summary>Horizontal alignment of the title text.</summary>
        public TitleAlignments TitleAlignment = TitleAlignments.Left;
        /// <summary>Create a title decorator.</summary>
        /// <param name="title">Title text shown in the inspector.</param>
        /// <param name="subtitle">Optional subtitle text.</param>
        /// <param name="horizontalLine">Draw a rule under the title.</param>
        /// <param name="bold">Render title in bold.</param>
        public TitleAttribute(string title, string subtitle = null, bool horizontalLine = true, bool bold = true)
        { Title = title; Subtitle = subtitle; HorizontalLine = horizontalLine; Bold = bold; }
        /// <summary>Create a title decorator with explicit alignment.</summary>
        /// <param name="title">Title text shown in the inspector.</param>
        /// <param name="subtitle">Optional subtitle text.</param>
        /// <param name="titleAlignment">Horizontal alignment of the title.</param>
        /// <param name="horizontalLine">Draw a rule under the title.</param>
        /// <param name="bold">Render title in bold.</param>
        public TitleAttribute(string title, string subtitle, TitleAlignments titleAlignment, bool horizontalLine = true, bool bold = true)
        { Title = title; Subtitle = subtitle; TitleAlignment = titleAlignment; HorizontalLine = horizontalLine; Bold = bold; }
    }

    /// <summary>Overrides the label text drawn next to a field or property.</summary>
    [AttributeUsage(AttributeTargets.Field | AttributeTargets.Property | AttributeTargets.Method | AttributeTargets.Event, AllowMultiple = false, Inherited = true)]
    public sealed class LabelTextAttribute : Attribute
    {
        /// <summary>Custom label text; supports <c>$member</c> and <c>@expression</c> syntax.</summary>
        public string Text;
        /// <summary>When true, applies <c>ObjectNames.NicifyVariableName</c> to the text.</summary>
        public bool NicifyText;
        /// <summary>Override the label text.</summary>
        /// <param name="text">Custom label text.</param>
        public LabelTextAttribute(string text) { Text = text; }
        /// <summary>Override the label text with optional nicify.</summary>
        /// <param name="text">Custom label text.</param>
        /// <param name="nicifyText">Apply nicify to the text.</param>
        public LabelTextAttribute(string text, bool nicifyText) { Text = text; NicifyText = nicifyText; }
    }

    /// <summary>Sets a custom label width for a single field or property.</summary>
    [AttributeUsage(AttributeTargets.Field | AttributeTargets.Property, AllowMultiple = false, Inherited = true)]
    public sealed class LabelWidthAttribute : Attribute
    {
        /// <summary>Label width in pixels.</summary>
        public float Width;
        /// <summary>Set a custom label width.</summary>
        /// <param name="width">Width in pixels.</param>
        public LabelWidthAttribute(float width) { Width = width; }
    }

    /// <summary>Hides the label for a field, property, method, or event; draws the control flush left.</summary>
    [AttributeUsage(AttributeTargets.Field | AttributeTargets.Property | AttributeTargets.Method | AttributeTargets.Event, AllowMultiple = false, Inherited = true)]
    public sealed class HideLabelAttribute : Attribute { }

    /// <summary>Item wrapper for <c>[ValueDropdown]</c> option lists: a display label + value.</summary>
    public readonly struct ValueDropdownItem<T>
    {
        /// <summary>Display text shown in the dropdown.</summary>
        public readonly string Text;
        /// <summary>Value returned when this item is selected.</summary>
        public readonly T Value;
        /// <summary>Create a dropdown item.</summary>
        /// <param name="text">Display text.</param>
        /// <param name="value">Associated value.</param>
        public ValueDropdownItem(string text, T value) { Text = text; Value = value; }
    }

    /// <summary>Forces a property or method to be shown in the inspector even if it has no Unity serialization.</summary>
    [AttributeUsage(AttributeTargets.Field | AttributeTargets.Property | AttributeTargets.Method | AttributeTargets.Event, AllowMultiple = false, Inherited = true)]
    public sealed class ShowInInspectorAttribute : Attribute { }

    /// <summary>Draws a field or property value as a read-only string instead of its default control.</summary>
    [AttributeUsage(AttributeTargets.Field | AttributeTargets.Property, AllowMultiple = false, Inherited = true)]
    public sealed class DisplayAsStringAttribute : Attribute
    {
        /// <summary>When false, text is clipped to a single line; when true, text wraps.</summary>
        public bool Overflow = true;
        /// <summary>Text alignment within the drawn area.</summary>
        public UnityEngine.TextAlignment Alignment = UnityEngine.TextAlignment.Left;
        /// <summary>Override font size; 0 uses the default.</summary>
        public int FontSize;
        /// <summary>Enable rich text formatting in the displayed string.</summary>
        public bool EnableRichText;
        /// <summary>Draw the value as a string with defaults.</summary>
        public DisplayAsStringAttribute() { }
        /// <summary>Draw the value as a string with overflow control.</summary>
        /// <param name="overflow">Whether text wraps or clips.</param>
        public DisplayAsStringAttribute(bool overflow) { Overflow = overflow; }
        /// <summary>Draw the value as a string with text alignment.</summary>
        /// <param name="alignment">Text alignment.</param>
        public DisplayAsStringAttribute(UnityEngine.TextAlignment alignment) { Alignment = alignment; }
        /// <summary>Draw the value as a string with overflow and alignment.</summary>
        /// <param name="overflow">Whether text wraps or clips.</param>
        /// <param name="alignment">Text alignment.</param>
        public DisplayAsStringAttribute(bool overflow, UnityEngine.TextAlignment alignment) { Overflow = overflow; Alignment = alignment; }
        /// <summary>Draw the value as a string with custom font size.</summary>
        /// <param name="fontSize">Font size in points.</param>
        public DisplayAsStringAttribute(int fontSize) { FontSize = fontSize; }
    }

    /// <summary>Marks a field or property as read-only in the inspector.</summary>
    [AttributeUsage(AttributeTargets.Field | AttributeTargets.Property | AttributeTargets.Event, AllowMultiple = false, Inherited = true)]
    public sealed class ReadOnlyAttribute : Attribute { }

    /// <summary>Marks a field or property as required; shows an error if null/empty.</summary>
    [AttributeUsage(AttributeTargets.Field | AttributeTargets.Property, AllowMultiple = false, Inherited = true)]
    public sealed class RequiredAttribute : Attribute
    {
        /// <summary>Custom error message; falls back to a generated one.</summary>
        public string ErrorMessage;
        /// <summary>Visual severity of the validation message.</summary>
        public InfoMessageType MessageType = InfoMessageType.Error;
        /// <summary>Mark the member as required.</summary>
        public RequiredAttribute() { }
        /// <summary>Mark the member as required with a custom error message.</summary>
        /// <param name="errorMessage">Message shown when validation fails.</param>
        public RequiredAttribute(string errorMessage) { ErrorMessage = errorMessage; }
        /// <summary>Mark the member as required with a custom error message and severity.</summary>
        /// <param name="errorMessage">Message shown when validation fails.</param>
        /// <param name="messageType">Visual severity of the validation message.</param>
        public RequiredAttribute(string errorMessage, InfoMessageType messageType) { ErrorMessage = errorMessage; MessageType = messageType; }
    }

    /// <summary>Sets the draw order of a member relative to other members in the same scope.</summary>
    [AttributeUsage(AttributeTargets.Field | AttributeTargets.Property | AttributeTargets.Method | AttributeTargets.Event, AllowMultiple = false, Inherited = true)]
    public sealed class PropertyOrderAttribute : Attribute
    {
        /// <summary>Sort order; lower values appear first.</summary>
        public float Order;
        /// <summary>Set the draw order.</summary>
        /// <param name="order">Sort order value.</param>
        public PropertyOrderAttribute(float order) { Order = order; }
    }

    /// <summary>Adds vertical spacing before and/or after a member.</summary>
    [AttributeUsage(AttributeTargets.Field | AttributeTargets.Property | AttributeTargets.Method | AttributeTargets.Event, AllowMultiple = false, Inherited = true)]
    public sealed class PropertySpaceAttribute : Attribute
    {
        /// <summary>Space in pixels added before the member.</summary>
        public float SpaceBefore;
        /// <summary>Space in pixels added after the member.</summary>
        public float SpaceAfter;
        /// <summary>Add 8px space before the member.</summary>
        public PropertySpaceAttribute() { SpaceBefore = 8f; }
        /// <summary>Add custom space before the member.</summary>
        /// <param name="spaceBefore">Pixels of space before.</param>
        public PropertySpaceAttribute(float spaceBefore) { SpaceBefore = spaceBefore; }
        /// <summary>Add custom space before and after the member.</summary>
        /// <param name="spaceBefore">Pixels of space before.</param>
        /// <param name="spaceAfter">Pixels of space after.</param>
        public PropertySpaceAttribute(float spaceBefore, float spaceAfter) { SpaceBefore = spaceBefore; SpaceAfter = spaceAfter; }
    }

    /// <summary>Increases the indent level for a member and its children.</summary>
    [AttributeUsage(AttributeTargets.Field | AttributeTargets.Property, AllowMultiple = false, Inherited = true)]
    public sealed class IndentAttribute : Attribute
    {
        /// <summary>Number of indent levels to add.</summary>
        public int IndentLevel;
        /// <summary>Increase indent by a specific number of levels.</summary>
        /// <param name="indentLevel">Additional indent levels (default 1).</param>
        public IndentAttribute(int indentLevel = 1) { IndentLevel = indentLevel; }
    }

    /// <summary>Applies a custom GUI color to a member and its children.</summary>
    [AttributeUsage(AttributeTargets.Field | AttributeTargets.Property | AttributeTargets.Method | AttributeTargets.Event, AllowMultiple = true, Inherited = true)]
    public sealed class GUIColorAttribute : Attribute
    {
        /// <summary>Red channel (0-1).</summary>
        public float R = 1f;
        /// <summary>Green channel (0-1).</summary>
        public float G = 1f;
        /// <summary>Blue channel (0-1).</summary>
        public float B = 1f;
        /// <summary>Alpha channel (0-1).</summary>
        public float A = 1f;
        /// <summary>Member name or <c>@expression</c> returning a <c>Color</c> to use instead of RGBA.</summary>
        public string GetColor;
        /// <summary>Set a constant GUI color.</summary>
        /// <param name="r">Red channel.</param>
        /// <param name="g">Green channel.</param>
        /// <param name="b">Blue channel.</param>
        /// <param name="a">Alpha channel.</param>
        public GUIColorAttribute(float r, float g, float b, float a = 1f) { R = r; G = g; B = b; A = a; }
        /// <summary>Set a dynamic GUI color from a member or expression.</summary>
        /// <param name="getColor">Member name or <c>@expression</c> returning a <c>Color</c>.</param>
        public GUIColorAttribute(string getColor) { GetColor = getColor; }
    }

    /// <summary>Draws a string field as a multi-line text area.</summary>
    [AttributeUsage(AttributeTargets.Field | AttributeTargets.Property, AllowMultiple = false, Inherited = true)]
    public sealed class MultiLinePropertyAttribute : Attribute
    {
        /// <summary>Number of text lines to display.</summary>
        public int Lines;
        /// <summary>Draw a multi-line text area.</summary>
        /// <param name="lines">Number of visible lines (default 3).</param>
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
        public bool Collapsible;
        public bool Expanded = true;
        public InfoBoxAttribute(string message, InfoMessageType infoMessageType = InfoMessageType.Info, string visibleIfMemberName = null, bool collapsible = false)
        { Message = message; InfoMessageType = infoMessageType; VisibleIf = visibleIfMemberName; Collapsible = collapsible; }
        public InfoBoxAttribute(string message, InfoMessageType infoMessageType, string visibleIfMemberName, bool collapsible, bool expanded)
        { Message = message; InfoMessageType = infoMessageType; VisibleIf = visibleIfMemberName; Collapsible = collapsible; Expanded = expanded; }
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

    /// <summary>Base class for conditional attributes; stores the condition member name and optional value.</summary>
    public abstract class ConditionalAttributeBase : Attribute
    {
        /// <summary>Member name or method to evaluate.</summary>
        public string Condition;
        /// <summary>Optional value to compare against.</summary>
        public object Value;
        /// <summary>True when <see cref="Value"/> was supplied.</summary>
        public bool HasValue;
        /// <summary>Initialize with a condition member name.</summary>
        /// <param name="condition">Member or method name to evaluate.</param>
        protected ConditionalAttributeBase(string condition) { Condition = condition; }
        /// <summary>Initialize with a condition member name and comparison value.</summary>
        /// <param name="condition">Member or method name to evaluate.</param>
        /// <param name="value">Value to compare against.</param>
        protected ConditionalAttributeBase(string condition, object value) { Condition = condition; Value = value; HasValue = true; }
    }

    /// <summary>Shows the decorated member only when the condition evaluates to true.</summary>
    [AttributeUsage(AttributeTargets.Field | AttributeTargets.Property | AttributeTargets.Method | AttributeTargets.Event, AllowMultiple = true, Inherited = true)]
    public sealed class ShowIfAttribute : ConditionalAttributeBase
    {
        /// <summary>When true, transitions animate when visibility changes.</summary>
        public bool Animate = true;
        /// <summary>Show when a condition member is truthy.</summary>
        /// <param name="condition">Member or method name to evaluate.</param>
        public ShowIfAttribute(string condition) : base(condition) { }
        /// <summary>Show when a condition member equals a specific value.</summary>
        /// <param name="condition">Member or method name to evaluate.</param>
        /// <param name="value">Expected value.</param>
        public ShowIfAttribute(string condition, object value) : base(condition, value) { }
    }

    /// <summary>Hides the decorated member when the condition evaluates to true.</summary>
    [AttributeUsage(AttributeTargets.Field | AttributeTargets.Property | AttributeTargets.Method | AttributeTargets.Event, AllowMultiple = true, Inherited = true)]
    public sealed class HideIfAttribute : ConditionalAttributeBase
    {
        /// <summary>When true, transitions animate when visibility changes.</summary>
        public bool Animate = true;
        /// <summary>Hide when a condition member is truthy.</summary>
        /// <param name="condition">Member or method name to evaluate.</param>
        public HideIfAttribute(string condition) : base(condition) { }
        /// <summary>Hide when a condition member equals a specific value.</summary>
        /// <param name="condition">Member or method name to evaluate.</param>
        /// <param name="value">Expected value.</param>
        public HideIfAttribute(string condition, object value) : base(condition, value) { }
    }

    /// <summary>Enables the decorated member only when the condition evaluates to true.</summary>
    [AttributeUsage(AttributeTargets.Field | AttributeTargets.Property | AttributeTargets.Method | AttributeTargets.Event, AllowMultiple = true, Inherited = true)]
    public sealed class EnableIfAttribute : ConditionalAttributeBase
    {
        /// <summary>Enable when a condition member is truthy.</summary>
        /// <param name="condition">Member or method name to evaluate.</param>
        public EnableIfAttribute(string condition) : base(condition) { }
        /// <summary>Enable when a condition member equals a specific value.</summary>
        /// <param name="condition">Member or method name to evaluate.</param>
        /// <param name="value">Expected value.</param>
        public EnableIfAttribute(string condition, object value) : base(condition, value) { }
    }

    /// <summary>Disables the decorated member when the condition evaluates to true.</summary>
    [AttributeUsage(AttributeTargets.Field | AttributeTargets.Property | AttributeTargets.Method | AttributeTargets.Event, AllowMultiple = true, Inherited = true)]
    public sealed class DisableIfAttribute : ConditionalAttributeBase
    {
        /// <summary>Disable when a condition member is truthy.</summary>
        /// <param name="condition">Member or method name to evaluate.</param>
        public DisableIfAttribute(string condition) : base(condition) { }
        /// <summary>Disable when a condition member equals a specific value.</summary>
        /// <param name="condition">Member or method name to evaluate.</param>
        /// <param name="value">Expected value.</param>
        public DisableIfAttribute(string condition, object value) : base(condition, value) { }
    }

    /// <summary>Hides the decorated member while the editor is not in play mode.</summary>
    [AttributeUsage(AttributeTargets.Field | AttributeTargets.Property | AttributeTargets.Method | AttributeTargets.Event, AllowMultiple = false, Inherited = true)]
    public sealed class HideInEditorModeAttribute : Attribute { }

    /// <summary>Hides the decorated member while the editor is in play mode.</summary>
    [AttributeUsage(AttributeTargets.Field | AttributeTargets.Property | AttributeTargets.Method | AttributeTargets.Event, AllowMultiple = false, Inherited = true)]
    public sealed class HideInPlayModeAttribute : Attribute { }

    /// <summary>Shows the decorated member only while the editor is in play mode.</summary>
    [AttributeUsage(AttributeTargets.Field | AttributeTargets.Property | AttributeTargets.Method | AttributeTargets.Event, AllowMultiple = false, Inherited = true)]
    public sealed class ShowInPlayModeAttribute : Attribute { }

    /// <summary>Disables the decorated member while the editor is not in play mode.</summary>
    [AttributeUsage(AttributeTargets.Field | AttributeTargets.Property | AttributeTargets.Method | AttributeTargets.Event, AllowMultiple = false, Inherited = true)]
    public sealed class DisableInEditorModeAttribute : Attribute { }

    /// <summary>Disables the decorated member while the editor is in play mode.</summary>
    [AttributeUsage(AttributeTargets.Field | AttributeTargets.Property | AttributeTargets.Method | AttributeTargets.Event, AllowMultiple = false, Inherited = true)]
    public sealed class DisableInPlayModeAttribute : Attribute { }

    // ---- Numeric / range ---------------------------------------------------

    /// <summary>Clamps a numeric field or property to a fixed or dynamic minimum/maximum range.</summary>
    [AttributeUsage(AttributeTargets.Field | AttributeTargets.Property, AllowMultiple = false, Inherited = true)]
    public sealed class PropertyRangeAttribute : Attribute
    {
        /// <summary>Fixed minimum value; ignored when <see cref="MinGetter"/> is set.</summary>
        public double Min;
        /// <summary>Fixed maximum value; ignored when <see cref="MaxGetter"/> is set.</summary>
        public double Max;
        /// <summary>Member name or expression returning the minimum value.</summary>
        public string MinGetter;
        /// <summary>Member name or expression returning the maximum value.</summary>
        public string MaxGetter;
        /// <summary>Clamp between two constant doubles.</summary>
        /// <param name="min">Minimum allowed value.</param>
        /// <param name="max">Maximum allowed value.</param>
        public PropertyRangeAttribute(double min, double max) { Min = min; Max = max; }
        /// <summary>Clamp between a constant minimum and a dynamic maximum.</summary>
        /// <param name="min">Minimum allowed value.</param>
        /// <param name="maxGetter">Member or expression returning the maximum.</param>
        public PropertyRangeAttribute(double min, string maxGetter) { Min = min; MaxGetter = maxGetter; }
        /// <summary>Clamp between a dynamic minimum and a constant maximum.</summary>
        /// <param name="minGetter">Member or expression returning the minimum.</param>
        /// <param name="max">Maximum allowed value.</param>
        public PropertyRangeAttribute(string minGetter, double max) { MinGetter = minGetter; Max = max; }
        /// <summary>Clamp between two dynamic values.</summary>
        /// <param name="minGetter">Member or expression returning the minimum.</param>
        /// <param name="maxGetter">Member or expression returning the maximum.</param>
        public PropertyRangeAttribute(string minGetter, string maxGetter) { MinGetter = minGetter; MaxGetter = maxGetter; }
    }

    /// <summary>Draws a two-handle min/max slider with optional inline float fields.</summary>
    [AttributeUsage(AttributeTargets.Field | AttributeTargets.Property, AllowMultiple = false, Inherited = true)]
    public sealed class MinMaxSliderAttribute : Attribute
    {
        /// <summary>Fixed minimum bound; ignored when <see cref="MinValueGetter"/> or <see cref="MinMaxValueGetter"/> is set.</summary>
        public float MinValue;
        /// <summary>Fixed maximum bound; ignored when <see cref="MaxValueGetter"/> or <see cref="MinMaxValueGetter"/> is set.</summary>
        public float MaxValue;
        /// <summary>When true, inline float fields are drawn alongside the slider.</summary>
        public bool ShowFields;
        /// <summary>Member name or expression returning the minimum bound.</summary>
        public string MinValueGetter;
        /// <summary>Member name or expression returning the maximum bound.</summary>
        public string MaxValueGetter;
        /// <summary>Member name or expression returning a <c>Vector2</c> where x=min, y=max.</summary>
        public string MinMaxValueGetter;
        /// <summary>Create a min/max slider with constant bounds.</summary>
        /// <param name="minValue">Minimum bound.</param>
        /// <param name="maxValue">Maximum bound.</param>
        /// <param name="showFields">Show inline float fields.</param>
        public MinMaxSliderAttribute(float minValue, float maxValue, bool showFields = false)
        { MinValue = minValue; MaxValue = maxValue; ShowFields = showFields; }
        /// <summary>Create a min/max slider with a dynamic maximum.</summary>
        /// <param name="minValueGetter">Member or expression returning the minimum.</param>
        /// <param name="maxValue">Maximum bound.</param>
        /// <param name="showFields">Show inline float fields.</param>
        public MinMaxSliderAttribute(string minValueGetter, float maxValue, bool showFields = false)
        { MinValueGetter = minValueGetter; MaxValue = maxValue; ShowFields = showFields; }
        /// <summary>Create a min/max slider with a dynamic minimum.</summary>
        /// <param name="minValue">Minimum bound.</param>
        /// <param name="maxValueGetter">Member or expression returning the maximum.</param>
        /// <param name="showFields">Show inline float fields.</param>
        public MinMaxSliderAttribute(float minValue, string maxValueGetter, bool showFields = false)
        { MinValue = minValue; MaxValueGetter = maxValueGetter; ShowFields = showFields; }
        /// <summary>Create a min/max slider with both bounds dynamic.</summary>
        /// <param name="minValueGetter">Member or expression returning the minimum.</param>
        /// <param name="maxValueGetter">Member or expression returning the maximum.</param>
        /// <param name="showFields">Show inline float fields.</param>
        public MinMaxSliderAttribute(string minValueGetter, string maxValueGetter, bool showFields = false)
        { MinValueGetter = minValueGetter; MaxValueGetter = maxValueGetter; ShowFields = showFields; }
        /// <summary>Create a min/max slider controlled by a single <c>Vector2</c> getter.</summary>
        /// <param name="minMaxValueGetter">Member or expression returning a <c>Vector2</c> (x=min, y=max).</param>
        /// <param name="showFields">Show inline float fields.</param>
        public MinMaxSliderAttribute(string minMaxValueGetter, bool showFields = false)
        { MinMaxValueGetter = minMaxValueGetter; ShowFields = showFields; }
    }

    /// <summary>Draws a field as a filled progress bar instead of its default control.</summary>
    [AttributeUsage(AttributeTargets.Field | AttributeTargets.Property, AllowMultiple = false, Inherited = true)]
    public sealed class ProgressBarAttribute : Attribute
    {
        /// <summary>Fixed minimum value; ignored when <see cref="MinGetter"/> is set.</summary>
        public double Min;
        /// <summary>Fixed maximum value; ignored when <see cref="MaxGetter"/> is set.</summary>
        public double Max;
        /// <summary>Member name or expression returning the minimum value.</summary>
        public string MinGetter;
        /// <summary>Member name or expression returning the maximum value.</summary>
        public string MaxGetter;
        /// <summary>Bar fill color (RGB 0-1).</summary>
        public float R = 0.15f;
        /// <summary>Bar fill color (RGB 0-1).</summary>
        public float G = 0.47f;
        /// <summary>Bar fill color (RGB 0-1).</summary>
        public float B = 0.74f;
        /// <summary>Member name or expression returning a <c>Color</c>; overrides RGB when set.</summary>
        public string ColorGetter;
        /// <summary>Member name or expression returning a background <c>Color</c>.</summary>
        public string BackgroundColorGetter;
        /// <summary>Render the bar as segmented blocks instead of a continuous fill.</summary>
        public bool Segmented;
        /// <summary>Bar height in pixels.</summary>
        public int Height = 12;
        /// <summary>Draw the numeric value label centered on the bar.</summary>
        public bool DrawValueLabel = true;
        /// <summary>Horizontal alignment of the value label.</summary>
        public UnityEngine.TextAlignment ValueLabelAlignment = UnityEngine.TextAlignment.Center;
        /// <summary>Member name or <c>$</c>-expression producing the label text.</summary>
        public string CustomValueStringGetter;
        /// <summary>Create a progress bar with constant bounds.</summary>
        /// <param name="min">Minimum value.</param>
        /// <param name="max">Maximum value.</param>
        public ProgressBarAttribute(double min, double max) { Min = min; Max = max; }
        /// <summary>Create a progress bar with a dynamic maximum.</summary>
        /// <param name="minGetter">Member or expression returning the minimum.</param>
        /// <param name="max">Maximum value.</param>
        public ProgressBarAttribute(string minGetter, double max) { MinGetter = minGetter; Max = max; }
        /// <summary>Create a progress bar with a dynamic minimum.</summary>
        /// <param name="min">Minimum value.</param>
        /// <param name="maxGetter">Member or expression returning the maximum.</param>
        public ProgressBarAttribute(double min, string maxGetter) { Min = min; MaxGetter = maxGetter; }
        /// <summary>Create a progress bar with both bounds dynamic.</summary>
        /// <param name="minGetter">Member or expression returning the minimum.</param>
        /// <param name="maxGetter">Member or expression returning the maximum.</param>
        public ProgressBarAttribute(string minGetter, string maxGetter) { MinGetter = minGetter; MaxGetter = maxGetter; }
        /// <summary>Create a progress bar with constant bounds and custom fill color.</summary>
        /// <param name="min">Minimum value.</param>
        /// <param name="max">Maximum value.</param>
        /// <param name="r">Red channel.</param>
        /// <param name="g">Green channel.</param>
        /// <param name="b">Blue channel.</param>
        public ProgressBarAttribute(double min, double max, float r, float g, float b) { Min = min; Max = max; R = r; G = g; B = b; }
    }

    /// <summary>Wraps a numeric value into a range by applying modulo.</summary>
    [AttributeUsage(AttributeTargets.Field | AttributeTargets.Property, AllowMultiple = false, Inherited = true)]
    public sealed class WrapAttribute : Attribute
    {
        /// <summary>Minimum value of the wrap range.</summary>
        public double Min;
        /// <summary>Maximum value of the wrap range.</summary>
        public double Max;
        /// <summary>Wrap a numeric value between min and max.</summary>
        /// <param name="min">Minimum value.</param>
        /// <param name="max">Maximum value.</param>
        public WrapAttribute(double min, double max) { Min = min; Max = max; }
    }

    /// <summary>Draws an enum field as a row of toggle buttons instead of a dropdown.</summary>
    [AttributeUsage(AttributeTargets.Field | AttributeTargets.Property, AllowMultiple = false, Inherited = true)]
    public sealed class EnumToggleButtonsAttribute : Attribute { }

    /// <summary>Draws a bool toggle on the left side of its label instead of the default right alignment.</summary>
    [AttributeUsage(AttributeTargets.Field | AttributeTargets.Property, AllowMultiple = false, Inherited = true)]
    public sealed class ToggleLeftAttribute : Attribute { }

    /// <summary>Configures how a <c>Dictionary&lt;TKey, TValue&gt;</c> is drawn in the inspector.</summary>
    [AttributeUsage(AttributeTargets.Field | AttributeTargets.Property, AllowMultiple = false, Inherited = true)]
    public sealed class DictionaryDrawerSettingsAttribute : Attribute
    {
        /// <summary>Header text shown above the key column.</summary>
        public string KeyLabel = "Key";
        /// <summary>Header text shown above the value column.</summary>
        public string ValueLabel = "Value";
        /// <summary>When true, entries cannot be added or removed.</summary>
        public bool IsReadOnly;
        /// <summary>Display mode identifier; interpretation is engine-defined.</summary>
        public ListDisplayMode DisplayMode;
        /// <summary>Fixed width of the key column in pixels.</summary>
        public int KeyColumnWidth;
        /// <summary>Fixed width of the value column in pixels.</summary>
        public int ValueColumnWidth;
        /// <summary>Create default dictionary drawer settings.</summary>
        public DictionaryDrawerSettingsAttribute() { }
    }

    /// <summary>Sets a hard minimum value for a numeric field.</summary>
    [AttributeUsage(AttributeTargets.Field | AttributeTargets.Property, AllowMultiple = false, Inherited = true)]
    public sealed class MinValueAttribute : Attribute
    {
        /// <summary>Constant minimum value; ignored when <see cref="Expression"/> is set.</summary>
        public double Min;
        /// <summary>Member name or expression returning the minimum value.</summary>
        public string Expression;
        /// <summary>Clamp to a constant minimum.</summary>
        /// <param name="min">Minimum allowed value.</param>
        public MinValueAttribute(double min) { Min = min; }
        /// <summary>Clamp to a dynamic minimum.</summary>
        /// <param name="expression">Member or expression returning the minimum.</param>
        public MinValueAttribute(string expression) { Expression = expression; }
    }

    /// <summary>Sets a hard maximum value for a numeric field.</summary>
    [AttributeUsage(AttributeTargets.Field | AttributeTargets.Property, AllowMultiple = false, Inherited = true)]
    public sealed class MaxValueAttribute : Attribute
    {
        /// <summary>Constant maximum value; ignored when <see cref="Expression"/> is set.</summary>
        public double Max;
        /// <summary>Member name or expression returning the maximum value.</summary>
        public string Expression;
        /// <summary>Clamp to a constant maximum.</summary>
        /// <param name="max">Maximum allowed value.</param>
        public MaxValueAttribute(double max) { Max = max; }
        /// <summary>Clamp to a dynamic maximum.</summary>
        /// <param name="expression">Member or expression returning the maximum.</param>
        public MaxValueAttribute(string expression) { Expression = expression; }
    }

    // ---- Collections / assets / dropdowns ----------------------------------

    /// <summary>Configures how a list or array is drawn in the inspector, including add/remove, reorder, and paging.</summary>
    [AttributeUsage(AttributeTargets.Field | AttributeTargets.Property, AllowMultiple = false, Inherited = true)]
    public sealed class ListDrawerSettingsAttribute : Attribute
    {
        /// <summary>When true, each element is prefixed with its index (e.g. <c>0: Element 0</c>).</summary>
        public bool ShowIndexLabels;
        /// <summary>When true, elements can be reordered by dragging the ≡ handle.</summary>
        public bool DraggableItems = true;
        /// <summary>When true, the list header is drawn as a foldout that expands/collapses the element set.</summary>
        public bool ShowFoldout = true;
        /// <summary>When true, the add and remove buttons are hidden and the list is read-only.</summary>
        public bool IsReadOnly;
        /// <summary>When true, paging controls are available for large lists.</summary>
        public bool ShowPaging = true;
        /// <summary>When true, the + add button is hidden.</summary>
        public bool HideAddButton;
        /// <summary>When true, the ✕ remove button is hidden.</summary>
        public bool HideRemoveButton;
        /// <summary>When true, newly created lists start expanded.</summary>
        public bool DefaultExpandedState;
        /// <summary>Member name on the element type whose value is used as the element label.</summary>
        public string ListElementLabelName;
        /// <summary>Method name invoked when the user clicks the add button; must return void.</summary>
        public string CustomAddFunction;
        /// <summary>Method name invoked to remove an element by value; receives the element as the sole argument.</summary>
        public string CustomRemoveElementFunction;
        /// <summary>Method name invoked to remove an element by index; receives the index as the sole argument.</summary>
        public string CustomRemoveIndexFunction;
        /// <summary>Method name invoked to draw custom GUI on the list title bar.</summary>
        public string OnTitleBarGUI;
        /// <summary>Method name invoked before each list element GUI is drawn; signature is <c>void(int index)</c>.</summary>
        public string OnBeginListElementGUI;
        /// <summary>Method name invoked after each list element GUI is drawn; signature is <c>void(int index)</c>.</summary>
        public string OnEndListElementGUI;
        /// <summary>When true, adding an element copies the previous element instead of clearing it.</summary>
        public bool AddCopiesLastElement;
        /// <summary>When true, newly added elements are cleared to their default value instead of copying the previous element.</summary>
        public bool AlwaysAddDefaultValue;
        /// <summary>Number of items shown per page when paging is enabled.</summary>
        public int NumberOfItemsPerPage;
        /// <summary>Initial expanded/collapsed state of the list foldout.</summary>
        public bool Expanded;
        /// <summary>Display mode for the collection foldout.</summary>
        public ListDisplayMode DisplayMode;
        /// <summary>When true (default), per-element labels (e.g. <c>Element 0</c>) are shown; set to false for a flat list appearance.</summary>
        public bool ShowElementLabels = true;
        /// <summary>Create default list drawer settings.</summary>
        public ListDrawerSettingsAttribute() { }
    }

    /// <summary>Renders a list or array as a table with column headers and scroll view.</summary>
    [AttributeUsage(AttributeTargets.Field | AttributeTargets.Property | AttributeTargets.Class | AttributeTargets.Struct, AllowMultiple = false, Inherited = true)]
    public sealed class TableListAttribute : Attribute
    {
        /// <summary>When true, row index labels are drawn in the first column.</summary>
        public bool ShowIndexLabels;
        /// <summary>When true, the table is rendered inside a scroll view.</summary>
        public bool DrawScrollView = true;
        /// <summary>When true, cells cannot be edited.</summary>
        public bool IsReadOnly;
        /// <summary>When true, the table starts fully expanded.</summary>
        public bool AlwaysExpanded;
        /// <summary>When true, the table toolbar (add/remove/search) is hidden.</summary>
        public bool HideToolbar;
        /// <summary>When true, paging controls are shown.</summary>
        public bool ShowPaging;
        /// <summary>When true, the total item count label is drawn in the toolbar.</summary>
        public bool ShowItemCount = true;
        /// <summary>Number of rows shown per page when paging is enabled.</summary>
        public int NumberOfItemsPerPage;
        /// <summary>Maximum height of the scroll view in pixels.</summary>
        public int MaxScrollViewHeight;
        /// <summary>Minimum height of the scroll view in pixels.</summary>
        public int MinScrollViewHeight;
        /// <summary>Default column width for auto-generated columns in pixels.</summary>
        public int DefaultMinColumnWidth = 40;
        /// <summary>Create default table list settings.</summary>
        public TableListAttribute() { }
    }

    /// <summary>Overrides the width of a specific column in a <see cref="TableListAttribute"/> table.</summary>
    [AttributeUsage(AttributeTargets.Field | AttributeTargets.Property | AttributeTargets.Method | AttributeTargets.Event, AllowMultiple = false, Inherited = true)]
    public sealed class TableColumnWidthAttribute : Attribute
    {
        /// <summary>Column width in pixels.</summary>
        public int Width;
        /// <summary>When true, the column can be resized by the user.</summary>
        public bool Resizable = true;
        /// <summary>Set a column width.</summary>
        /// <param name="width">Width in pixels.</param>
        /// <param name="resizable">Allow user resizing.</param>
        public TableColumnWidthAttribute(int width, bool resizable = true) { Width = width; Resizable = resizable; }
    }

    /// <summary>Replaces a field's default input with a dropdown populated by a member returning <see cref="ValueDropdownItem{T}"/> or an array of strings.</summary>
    [AttributeUsage(AttributeTargets.Field | AttributeTargets.Property, AllowMultiple = false, Inherited = true)]
    public sealed class ValueDropdownAttribute : Attribute
    {
        /// <summary>Member name or method returning the dropdown options.</summary>
        public string ValuesGetter;
        /// <summary>Number of options before a search field is automatically shown.</summary>
        public int NumberOfItemsBeforeEnablingSearch = 10;
        /// <summary>When true, duplicate values are filtered from the dropdown options.</summary>
        public bool IsUniqueList;
        /// <summary>When true, list elements also render with the dropdown instead of the default drawer.</summary>
        public bool DrawDropdownForListElements = true;
        /// <summary>When true, all menu items are expanded by default.</summary>
        public bool ExpandAllMenuItems;
        /// <summary>When true, appends the next drawer after this dropdown instead of replacing it.</summary>
        public bool AppendNextDrawer;
        /// <summary>When true, adding elements from the dropdown does not trigger the list add button behavior.</summary>
        public bool DisableListAddButtonBehaviour;
        /// <summary>When true, dropdown items are sorted alphabetically.</summary>
        public bool SortDropdownItems;
        /// <summary>When true, child properties of the selected value are hidden.</summary>
        public bool HideChildProperties;
        /// <summary>When true, tree-view items are flattened into a single list.</summary>
        public bool FlattenTreeView;
        /// <summary>Custom title shown on the dropdown window.</summary>
        public string DropdownTitle;
        /// <summary>Bind a dropdown to a values getter.</summary>
        /// <param name="valuesGetter">Member or method name returning the option list.</param>
        public ValueDropdownAttribute(string valuesGetter) { ValuesGetter = valuesGetter; }
    }

    /// <summary>Shows an asset picker filtered by the Asset Database on a field.</summary>
    [AttributeUsage(AttributeTargets.Field | AttributeTargets.Property, AllowMultiple = false, Inherited = true)]
    public sealed class AssetSelectorAttribute : Attribute
    {
        /// <summary>AssetDatabase.FindAssets filter string; the field type filter is applied automatically.</summary>
        public string Filter;
        /// <summary>Search folders separated by <c>'|'</c>.</summary>
        public string Paths;
        /// <summary>When true, duplicate selections are prevented.</summary>
        public bool IsUniqueList = true;
        /// <summary>When true, list elements also render with the asset selector dropdown.</summary>
        public bool DrawDropdownForListElements = true;
        /// <summary>When true, assets already referenced in the list are excluded from the dropdown.</summary>
        public bool ExcludeExistingValuesInList;
        /// <summary>When true, folders are expanded by default in the picker.</summary>
        public bool ExpandAllMenuItems = true;
        /// <summary>When true, tree-view items are flattened into a single list.</summary>
        public bool FlattenTreeView;
        /// <summary>Custom title shown on the asset picker window.</summary>
        public string DropdownTitle;
        /// <summary>Create default asset selector settings.</summary>
        public AssetSelectorAttribute() { }
    }

    /// <summary>Restricts an object reference field to assets only (no scene objects).</summary>
    [AttributeUsage(AttributeTargets.Field | AttributeTargets.Property, AllowMultiple = false, Inherited = true)]
    public sealed class AssetsOnlyAttribute : Attribute { }

    /// <summary>Restricts an object reference field to scene objects only (no assets).</summary>
    [AttributeUsage(AttributeTargets.Field | AttributeTargets.Property, AllowMultiple = false, Inherited = true)]
    public sealed class SceneObjectsOnlyAttribute : Attribute { }

    /// <summary>Adds a search field above the decorated list or table.</summary>
    [AttributeUsage(AttributeTargets.Field | AttributeTargets.Property, AllowMultiple = false, Inherited = true)]
    public sealed class SearchableAttribute : Attribute
    {
        /// <summary>When true (default), nested children are searched as well.</summary>
        public bool Recursive = true;
        /// <summary>Mark the list or table as searchable.</summary>
        public SearchableAttribute() { }
    }

    // ---- Inline / composite -------------------------------------------------

    /// <summary>Draws a serializable type inline (without a foldout header) when used as a nested object.</summary>
    [AttributeUsage(AttributeTargets.Field | AttributeTargets.Property | AttributeTargets.Class | AttributeTargets.Struct, AllowMultiple = false, Inherited = true)]
    public sealed class InlinePropertyAttribute : Attribute
    {
        /// <summary>Override label width for the inlined property.</summary>
        public int LabelWidth;
        /// <summary>Draw the object inline using the default label width.</summary>
        public InlinePropertyAttribute() { }
        /// <summary>Draw the object inline with a custom label width.</summary>
        /// <param name="labelWidth">Label width in pixels.</param>
        public InlinePropertyAttribute(int labelWidth) { LabelWidth = labelWidth; }
    }

    /// <summary>Embeds an inspector preview for an Object, GameObject, Component, or ScriptableObject.</summary>
    [AttributeUsage(AttributeTargets.Field | AttributeTargets.Property, AllowMultiple = false, Inherited = true)]
    public sealed class InlineEditorAttribute : Attribute
    {
        /// <summary>Initial expanded/collapsed state of the inline editor.</summary>
        public bool Expanded;
        /// <summary>When true, the object header/title bar is drawn.</summary>
        public bool DrawHeader = true;
        /// <summary>When true, the object's Inspector GUI is drawn.</summary>
        public bool DrawGUI = true;
        /// <summary>When true, the object preview image is drawn.</summary>
        public bool DrawPreview;
        /// <summary>Maximum height of the inline editor in pixels.</summary>
        public float MaxHeight;
        /// <summary>Width of the preview image in pixels.</summary>
        public float PreviewWidth = 100f;
        /// <summary>Height of the preview image in pixels.</summary>
        public float PreviewHeight = 35f;
        /// <summary>Which parts of the inline editor are visible.</summary>
        public InlineEditorModes Mode = InlineEditorModes.GUIOnly;
        /// <summary>How the object-field picker is rendered.</summary>
        public InlineEditorObjectFieldModes ObjectFieldMode = InlineEditorObjectFieldModes.Boxed;
        /// <summary>Create default inline editor settings.</summary>
        public InlineEditorAttribute() { }
        /// <summary>Create an inline editor with a preset mode.</summary>
        /// <param name="mode">Preset mode that configures header/GUI/preview visibility.</param>
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
        /// <summary>Create an inline editor with a custom object-field mode.</summary>
        /// <param name="objectFieldMode">How the object picker field is rendered.</param>
        public InlineEditorAttribute(InlineEditorObjectFieldModes objectFieldMode) { ObjectFieldMode = objectFieldMode; }
    }

    /// <summary>Hides the Unity Object picker popup for reference fields in nested objects.</summary>
    [AttributeUsage(AttributeTargets.Field | AttributeTargets.Property | AttributeTargets.Class | AttributeTargets.Struct, AllowMultiple = false, Inherited = true)]
    public sealed class HideReferenceObjectPickerAttribute : Attribute { }

    /// <summary>Registers an action to run once when the inspector initializes this member.</summary>
    [AttributeUsage(AttributeTargets.Method | AttributeTargets.Field | AttributeTargets.Property, AllowMultiple = true, Inherited = true)]
    public sealed class OnInspectorInitAttribute : Attribute
    {
        /// <summary>Method name to invoke on initialization.</summary>
        public string Action;
        /// <summary>Create an init hook with no action.</summary>
        public OnInspectorInitAttribute() { }
        /// <summary>Create an init hook bound to a method.</summary>
        /// <param name="action">Method name to invoke on initialization.</param>
        public OnInspectorInitAttribute(string action) { Action = action; }
    }

    /// <summary>Draws a small preview texture or inspector for an object reference field.</summary>
    [AttributeUsage(AttributeTargets.Field | AttributeTargets.Property, AllowMultiple = false, Inherited = true)]
    public sealed class PreviewFieldAttribute : Attribute
    {
        /// <summary>Height of the preview area in pixels; 0 uses the default.</summary>
        public float Height;
        /// <summary>Horizontal alignment of the preview inside its area.</summary>
        public ObjectFieldAlignment Alignment = ObjectFieldAlignment.Left;
        /// <summary>Draw a preview field with default settings.</summary>
        public PreviewFieldAttribute() { }
        /// <summary>Draw a preview field with a custom height.</summary>
        /// <param name="height">Height in pixels.</param>
        public PreviewFieldAttribute(float height) { Height = height; }
        /// <summary>Draw a preview field with custom alignment.</summary>
        /// <param name="alignment">Horizontal alignment.</param>
        public PreviewFieldAttribute(ObjectFieldAlignment alignment) { Alignment = alignment; }
        /// <summary>Draw a preview field with custom height and alignment.</summary>
        /// <param name="height">Height in pixels.</param>
        /// <param name="alignment">Horizontal alignment.</param>
        public PreviewFieldAttribute(float height, ObjectFieldAlignment alignment) { Height = height; Alignment = alignment; }
    }

    /// <summary>Forces the field or type to be drawn by Unity's default PropertyDrawer instead of the engine drawer.</summary>
    [AttributeUsage(AttributeTargets.Field | AttributeTargets.Property | AttributeTargets.Class | AttributeTargets.Struct, AllowMultiple = false, Inherited = true)]
    public sealed class DrawWithUnityAttribute : Attribute { }
}
