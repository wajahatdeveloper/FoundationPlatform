#if UNITY_EDITOR
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace AetherNexus.FoundationPlatform.Editor.Utilities.Debugging
{
	/// <summary>
	///  Stateless IMGUI draw helpers shared by every framework debugger window so each new debugger
	///  speaks the same visual language — labeled progress bars, cooldown/duration timelines, tag
	///  chips, stat rows and legend swatches — without re-implementing it. Extracted from the AI
	///  Debugger's original private DrawBar/LegendSwatch.
	/// </summary>
	public static class DebugDrawKit
	{
		public static System.Text.StringBuilder ActiveRecorder;

		public static readonly Color BarTrack = new(0.15f, 0.15f, 0.15f);
		public static readonly Color BarFill = new(0.35f, 0.55f, 0.85f);
		public static readonly Color BarHighlight = new(0.3f, 0.8f, 0.3f);
		public static readonly Color BarWarn = new(0.9f, 0.6f, 0.2f);

		private const float LabelWidth = 190f;

		/// <summary>Labeled horizontal bar: [label][====fill====][F2 value]. fill01 is clamped 0..1.</summary>
		public static void Bar(string label, float fill01, float rawValue, bool highlight)
		{
			Bar(label, fill01, rawValue.ToString("F2"), highlight, null);
		}

		/// <summary>Draws with no highlight.</summary>
		public static void Bar(string label, float fill01, float rawValue) => Bar(label, fill01, rawValue, false);

		/// <summary>As <see cref="Bar(string,float,float,bool)"/> but with free-form value text and an
		/// optional explicit fill color (ignored when <paramref name="highlight"/> is true).</summary>
		public static void Bar(string label, float fill01, string valueText, bool highlight, Color? fill)
		{
			if (ActiveRecorder != null)
			{
				ActiveRecorder.AppendLine($"  {label}: {valueText}");
			}

			var rect = GUILayoutUtility.GetRect(1f, 18f, GUILayout.ExpandWidth(true));
			var labelRect = new Rect(rect.x, rect.y, LabelWidth, rect.height);
			var barRect = new Rect(rect.x + LabelWidth + 4f, rect.y + 2f, rect.width - LabelWidth - 60f, rect.height - 4f);
			var valueRect = new Rect(rect.xMax - 52f, rect.y, 52f, rect.height);

			EditorGUI.LabelField(labelRect, label);
			EditorGUI.DrawRect(barRect, BarTrack);
			var filled = new Rect(barRect.x, barRect.y, barRect.width * Mathf.Clamp01(fill01), barRect.height);
			EditorGUI.DrawRect(filled, highlight ? BarHighlight : fill ?? BarFill);
			EditorGUI.LabelField(valueRect, valueText);
		}

		/// <summary>Draws with no fill color override.</summary>
		public static void Bar(string label, float fill01, string valueText, bool highlight) => Bar(label, fill01, valueText, highlight, null);

		/// <summary>Draws with no highlight and no fill color override.</summary>
		public static void Bar(string label, float fill01, string valueText) => Bar(label, fill01, valueText, false, null);

		/// <summary>Cooldown / effect-duration bar: fills as time remains, shows "ready" at zero.</summary>
		public static void Timeline(string label, float remaining, float total)
		{
			var fill = total > 0f ? remaining / total : 0f;
			var ready = remaining <= 0f;
			Bar(label, fill, ready ? "ready" : $"{remaining:F1}s", ready, ready ? BarHighlight : BarWarn);
		}

		/// <summary>Simple key/value row, kept here so stat lines match bar label width.</summary>
		public static void Stat(string label, string value)
		{
			if (ActiveRecorder != null)
			{
				ActiveRecorder.AppendLine($"  {label}: {value}");
			}
			EditorGUILayout.LabelField(label, value);
		}

		/// <summary>A single colored tag chip. Use inside a horizontal group.</summary>
		public static void Chip(string label, Color color)
		{
			var previous = GUI.backgroundColor;
			GUI.backgroundColor = color;
			GUILayout.Label(label, EditorStyles.helpBox, GUILayout.Height(18f));
			GUI.backgroundColor = previous;
		}

		/// <summary>A labeled row of chips (e.g. owned gameplay tags). Flushes left.</summary>
		public static void ChipRow(string label, IEnumerable<string> chips, Color color)
		{
			if (ActiveRecorder != null)
			{
				ActiveRecorder.AppendLine($"  {(string.IsNullOrEmpty(label) ? "Tags" : label)}: {string.Join(", ", chips)}");
			}

			EditorGUILayout.BeginHorizontal();
			if (!string.IsNullOrEmpty(label))
			{
				EditorGUILayout.LabelField(label, GUILayout.Width(LabelWidth));
			}

			foreach (var chip in chips)
			{
				Chip(chip, color);
			}

			GUILayout.FlexibleSpace();
			EditorGUILayout.EndHorizontal();
		}

		public static void Label(string text, GUIStyle style)
		{
			if (ActiveRecorder != null)
			{
				ActiveRecorder.AppendLine(text);
			}
			if (style != null)
			{
				EditorGUILayout.LabelField(text, style);
			}
			else
			{
				EditorGUILayout.LabelField(text);
			}
		}

		/// <summary>Draws using the default label style.</summary>
		public static void Label(string text) => Label(text, null);

		/// <summary>A 12x12 color swatch followed by its label — one legend entry.</summary>
		public static void LegendSwatch(string label, Color color)
		{
			var rect = GUILayoutUtility.GetRect(12f, 12f, GUILayout.Width(12f), GUILayout.Height(12f));
			EditorGUI.DrawRect(rect, color);
			GUILayout.Label(label, EditorStyles.miniLabel, GUILayout.Width(62f));
		}

		// ---- boxed / toned detail-pane helpers (merged from the former second draw kit that lived in
		// the separate debug-kit assembly): titled headers, foldout box sections, overlaid bars and
		// colored key/value rows shared by the domain debugger sections.

		public static readonly Color Track = new(0.16f, 0.16f, 0.16f);
		public static readonly Color Fill = new(0.35f, 0.6f, 0.9f);
		public static readonly Color Ok = new(0.4f, 0.85f, 0.4f);
		public static readonly Color Bad = new(0.95f, 0.45f, 0.45f);
		public static readonly Color Warn = new(0.9f, 0.65f, 0.25f);
		public static readonly Color Buff = new(0.35f, 0.8f, 0.85f);
		public static readonly Color Low = new(0.9f, 0.4f, 0.35f);
		public static readonly Color Neutral = new(0.55f, 0.55f, 0.6f);

		private static readonly Color OkRowBg = new(0.22f, 0.35f, 0.22f);
		private static readonly Color BadRowBg = new(0.38f, 0.22f, 0.22f);
		private static readonly Color NeutralRowBg = new(0.24f, 0.24f, 0.26f);
		private static readonly Color BorderColor = new(0f, 0f, 0f, 0.5f);
		private static readonly Color HeaderBg = new(0f, 0f, 0f, 0.15f);
		private static readonly Color RuleColor = new(0.5f, 0.5f, 0.5f, 0.5f);

		private static GUIStyle _left;
		private static GUIStyle _right;
		private static GUIStyle _titleStyle;
		private static GUIStyle _subtitleStyle;

		/// <summary>Header with a title, dimmed subtitle and a horizontal rule.</summary>
		public static void Title(string title, string subtitle)
		{
			if (ActiveRecorder != null)
			{
				ActiveRecorder.AppendLine($"\n# {title} ({subtitle})");
			}

			EnsureStyles();
			EditorGUILayout.Space(2f);
			var rect = EditorGUILayout.GetControlRect(false, 20f);
			GUI.Label(rect, title, _titleStyle);
			if (!string.IsNullOrEmpty(subtitle))
				GUI.Label(rect, subtitle, _subtitleStyle);
			var rule = new Rect(rect.x, rect.yMax, rect.width, 1f);
			EditorGUI.DrawRect(rule, RuleColor);
			EditorGUILayout.Space(2f);
		}

		/// <summary>Open a collapsible box section. Returns the (possibly toggled) expanded state; only
		/// draw the body when it is true, then call <see cref="EndSection"/>.</summary>
		public static bool BeginSection(string title, bool expanded)
		{
			if (ActiveRecorder != null)
			{
				ActiveRecorder.AppendLine($"\n## [{title}]");
				expanded = true;
			}

			EditorGUILayout.BeginVertical(EditorStyles.helpBox);
			var headerRect = EditorGUILayout.GetControlRect(false, EditorGUIUtility.singleLineHeight);
			EditorGUI.DrawRect(headerRect, HeaderBg);
			expanded = EditorGUI.Foldout(headerRect, expanded, title, true, EditorStyles.foldoutHeader);
			return expanded;
		}

		public static void EndSection() => EditorGUILayout.EndVertical();

		/// <summary>Progress bar with the label overlaid left and the value overlaid right.</summary>
		public static void Bar(string label, float fill01, string value, Color fill)
		{
			if (ActiveRecorder != null)
			{
				ActiveRecorder.AppendLine($"  {label}: {value}");
			}

			EnsureStyles();
			var rect = EditorGUILayout.GetControlRect(false, 18f);
			EditorGUI.DrawRect(rect, Track);

			var filled = rect;
			filled.width = Mathf.Max(0f, rect.width * Mathf.Clamp01(fill01));
			EditorGUI.DrawRect(filled, fill);
			DrawBorders(rect, 1f);

			var inner = new Rect(rect.x + 6f, rect.y, rect.width - 12f, rect.height);
			GUI.Label(inner, label, _left);
			GUI.Label(inner, value, _right);
		}

		/// <summary>Colored key/value row — green (positive), red (negative) or grey (neutral) background.</summary>
		public static void Row(string left, string right, RowTone tone)
		{
			if (ActiveRecorder != null)
			{
				ActiveRecorder.AppendLine($"  {left}: {right}");
			}

			EnsureStyles();
			var rect = EditorGUILayout.GetControlRect(false, 18f);
			var bg = tone switch
			{
				RowTone.Positive => OkRowBg,
				RowTone.Negative => BadRowBg,
				_ => NeutralRowBg,
			};
			EditorGUI.DrawRect(rect, bg);
			DrawBorders(rect, 1f);

			var l = new Rect(rect.x + 6f, rect.y, rect.width * 0.44f - 6f, rect.height);
			var r = new Rect(rect.x + rect.width * 0.44f, rect.y, rect.width * 0.56f - 6f, rect.height);
			GUI.Label(l, left, _left);
			GUI.Label(r, right, _right);
		}

		public enum RowTone
		{
			Neutral,
			Positive,
			Negative,
		}

		private static void DrawBorders(Rect r, float thickness)
		{
			EditorGUI.DrawRect(new Rect(r.x, r.y, r.width, thickness), BorderColor);
			EditorGUI.DrawRect(new Rect(r.x, r.yMax - thickness, r.width, thickness), BorderColor);
			EditorGUI.DrawRect(new Rect(r.x, r.y, thickness, r.height), BorderColor);
			EditorGUI.DrawRect(new Rect(r.xMax - thickness, r.y, thickness, r.height), BorderColor);
		}

		private static void EnsureStyles()
		{
			if (_left != null)
			{
				return;
			}

			_left = new GUIStyle(EditorStyles.whiteLabel)
			{
				alignment = TextAnchor.MiddleLeft,
				fontStyle = FontStyle.Bold,
				fontSize = 11,
			};
			_right = new GUIStyle(EditorStyles.whiteMiniLabel)
			{
				alignment = TextAnchor.MiddleRight,
			};
			_titleStyle = new GUIStyle(EditorStyles.boldLabel)
			{
				alignment = TextAnchor.MiddleLeft,
				fontSize = 12,
			};
			_subtitleStyle = new GUIStyle(EditorStyles.miniLabel)
			{
				alignment = TextAnchor.MiddleRight,
			};
		}
	}
}
#endif
