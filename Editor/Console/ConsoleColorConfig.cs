using AetherNexus.FoundationPlatform.AetherInspector.Editor;
using AetherNexus.FoundationPlatform.Logging;
using UnityEditor;
using UnityEngine;

namespace AetherNexus.FoundationPlatform.Logging.ConsoleView.Editor
{
    /// <summary>
    /// Appearance facade over <see cref="DebugXConsoleSettings"/> (per-project). Per-channel tints are
    /// derived deterministically from the channel name so every channel gets a stable, distinct colour
    /// (Console Pro's "custom log types" equivalent) without any manual setup.
    /// </summary>
    internal static class ConsoleColorConfig
    {
        private static DebugXConsoleSettings S => DebugXConsoleSettings.Instance;

        public static int FontSize
        {
            get => S.fontSize;
            set { S.fontSize = Mathf.Clamp(value, 9, 22); S.Save(); }
        }

        public static int RowHeight
        {
            get => S.rowHeight;
            set { S.rowHeight = Mathf.Clamp(value, 16, 40); S.Save(); }
        }

        public enum TimeFormat { None, Clock, ClockMillis, Delta, Frame }

        public static TimeFormat TimeStampFormat
        {
            get => (TimeFormat)Mathf.Clamp(S.timeFormat, 0, 4);
            set { S.timeFormat = (int)value; S.Save(); }
        }

        public static bool TwoLineRows
        {
            get => S.twoLineRows;
            set { S.twoLineRows = value; S.Save(); }
        }

        // Resizable list columns (dragged on the header, persisted per project).
        public static int TimeWidth
        {
            get => S.colTimeWidth;
            set { S.colTimeWidth = Mathf.Clamp(value, 30, 220); S.Save(); }
        }

        public static int ChannelWidth
        {
            get => S.colChannelWidth;
            set { S.colChannelWidth = Mathf.Clamp(value, 40, 300); S.Save(); }
        }

        public static int CountWidth
        {
            get => S.colCountWidth;
            set { S.colCountWidth = Mathf.Clamp(value, 20, 100); S.Save(); }
        }

        public static bool AlternatingRows
        {
            get => S.alternatingRows;
            set { S.alternatingRows = value; S.Save(); }
        }

        public static bool ShowHeader
        {
            get => S.showHeader;
            set { S.showHeader = value; S.Save(); }
        }

        /// <summary>Highlight colour for selected rows (explicit, since our per-row backgrounds override ListView's).</summary>
        public static Color SelectionColor => AetherInspectorTheme.MenuSelectionBackground;

        /// <summary>Dimmed colour for synthetic marker/divider rows (play-mode transitions).</summary>
        public static Color MarkerColor => EditorGUIUtility.isProSkin
            ? new Color(0.55f, 0.65f, 0.55f)
            : new Color(0.24f, 0.40f, 0.24f);

        public const int DefaultFontSize = 12;
        public const int DefaultRowHeight = 20;

        public static Color LevelColor(LogLevel level)
        {
            switch (level)
            {
                case LogLevel.Error:
                case LogLevel.Fatal:
                    return EditorGUIUtility.isProSkin
                        ? new Color(1f, 0.45f, 0.42f)
                        : new Color(0.70f, 0.11f, 0.08f);
                case LogLevel.Warning:
                    return EditorGUIUtility.isProSkin
                        ? new Color(1f, 0.85f, 0.4f)
                        : new Color(0.55f, 0.40f, 0f);
                case LogLevel.Verbose:
                    return AetherInspectorTheme.TertiaryTextColor;
                default:
                    return AetherInspectorTheme.PrimaryTextColor;
            }
        }

        /// <summary>Stable, distinct tint for a channel chip, derived from the channel name.</summary>
        public static Color ChannelColor(string channel)
        {
            if (string.IsNullOrEmpty(channel))
                return AetherInspectorTheme.TertiaryTextColor;

            int hash = 17;
            foreach (char c in channel)
                hash = hash * 31 + c;

            float hue = (hash & 0x7fffffff) % 360 / 360f;
            // Light skin needs saturated-and-darker chips; the same value/saturation reads as washed out on white.
            return EditorGUIUtility.isProSkin
                ? Color.HSVToRGB(hue, 0.45f, 0.9f)
                : Color.HSVToRGB(hue, 0.85f, 0.55f);
        }

        /// <summary>Subtle zebra-stripe background for odd rows when alternating rows is enabled.</summary>
        public static Color RowBackground(int index)
        {
            if (!AlternatingRows || (index & 1) == 0)
                return Color.clear;
            return AetherInspectorTheme.TableRowBackgroundB;
        }
    }
}
