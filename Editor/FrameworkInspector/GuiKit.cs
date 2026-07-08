#if UNITY_EDITOR
using FoundationPlatform.FrameworkInspector;
using UnityEngine;

namespace FoundationPlatform.FrameworkInspector.Editor
{
    /// <summary>
    /// Public facade over <see cref="FrameworkInspectorTheme"/> for editor windows outside the inspector engine.
    /// </summary>
    public static class GuiKit
    {
        public static void BeginBox(string label = null) => FrameworkInspectorTheme.BeginBox(label);

        public static void EndBox() => FrameworkInspectorTheme.EndBox();

        public static void BeginBoxHeader() => FrameworkInspectorTheme.BeginBoxHeader();

        public static void EndBoxHeader() => FrameworkInspectorTheme.EndBoxHeader();

        public static bool Foldout(bool expanded, string label)
            => FrameworkInspectorTheme.Foldout(expanded, label);

        public static bool Foldout(bool expanded, GUIContent label)
            => FrameworkInspectorTheme.Foldout(expanded, label);

        public static bool FoldoutInSection(bool expanded, string label)
            => FrameworkInspectorTheme.FoldoutInSection(expanded, label);

        public static bool FoldoutInSection(bool expanded, GUIContent label)
            => FrameworkInspectorTheme.FoldoutInSection(expanded, label);

        public static void Title(string title) => FrameworkInspectorTheme.DrawTitle(title);

        public static void Title(string title, string subtitle, TextAlignment textAlignment = TextAlignment.Left,
            bool horizontalLine = true, bool boldLabel = true)
            => FrameworkInspectorTheme.DrawTitle(title, subtitle, textAlignment, horizontalLine, boldLabel);

        public static void InfoBox(string message, InfoMessageType type = InfoMessageType.Info)
            => FrameworkInspectorTheme.DrawInfoBox(message, type);

        public static void ValidationBox(string message, InfoMessageType type = InfoMessageType.Error)
            => FrameworkInspectorTheme.DrawValidationBox(message, type);

        public static int Toolbar(int selected, string[] labels)
            => FrameworkInspectorTheme.Toolbar(selected, labels);
    }
}
#endif
