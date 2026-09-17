#if UNITY_EDITOR
using AetherNexus.FoundationPlatform.AetherInspector.Editor;
using UnityEditor;
using UnityEngine;

namespace AetherNexus.FoundationPlatform.Editor.Utilities.Messaging
{
	public static class EventBusConstants
	{
		// Page sizes
		public const int DEFAULT_PAGE_SIZE = 200;
		public const int DEFAULT_SUBSCRIBERS_PAGE_SIZE = 300;
		public const int MIN_PAGE_SIZE = 1;
		public const int MAX_PAGE_SIZE = 1000;
		
		// History sizes
		public const int DEFAULT_MAX_EVENT_HISTORY = 1000;
		public const int DEFAULT_MAX_SUBSCRIPTION_HISTORY = 2000;
		public const int MIN_HISTORY_SIZE = 50;
		public const int MAX_HISTORY_SIZE = 100000;
		
		// Refresh intervals
		public const double DEFAULT_REFRESH_INTERVAL = 1.0;
		public const float DEFAULT_PLAY_MODE_REFRESH_INTERVAL = 1f;
		public const float MIN_REFRESH_INTERVAL = 0.05f;
		public const float MAX_REFRESH_INTERVAL = 5f;
		
		// Table column widths
		public const int COLUMN_WIDTH_TIME = 110;
		public const int COLUMN_WIDTH_TYPE = 200;
		public const int COLUMN_WIDTH_CATEGORY = 80;
		public const int COLUMN_WIDTH_PUBLISHER = 180;
		public const int COLUMN_WIDTH_SUBSCRIBER_COUNT = 90;
		public const int COLUMN_WIDTH_DEPTH = 80;
		public const int COLUMN_WIDTH_DATA = 200;
		public const int COLUMN_WIDTH_TARGET = 220;
		public const int COLUMN_WIDTH_METHOD = 160;
		public const int COLUMN_WIDTH_CONTEXT = 240;
		public const int COLUMN_WIDTH_BUTTON = 60;
		public const int COLUMN_WIDTH_PRIORITY = 80;
		public const int COLUMN_WIDTH_ACTION = 100;
		
		// Label widths
		public const int LABEL_WIDTH_SEARCH = 50;
		public const int LABEL_WIDTH_CATEGORY = 60;
		
		// Depth guard
		public const int DEFAULT_MAX_DEPTH = 10;
		public const int MIN_MAX_DEPTH = 3;
		public const int MAX_MAX_DEPTH = 30;
		public const int DEFAULT_WARN_PERCENT = 75;
		public const int MIN_WARN_PERCENT = 50;
		public const int MAX_WARN_PERCENT = 95;
		
		// Colors. Pale tints read on the dark skin and vanish on the light one, so every accent has
		// a darker light-skin counterpart and the table chrome defers to AetherInspectorTheme.
		private static bool Pro => EditorGUIUtility.isProSkin;

		public static Color COLOR_SORT_HIGHLIGHT => Pro ? new Color(0.7f, 0.85f, 1f) : new Color(0.13f, 0.32f, 0.60f);
		public static Color COLOR_SORT_BUTTON => Pro ? new Color(0.7f, 0.9f, 1f) : new Color(0.13f, 0.35f, 0.62f);
		public static Color COLOR_DOMAIN => Pro ? new Color(0.5f, 0.8f, 1f) : new Color(0.10f, 0.36f, 0.62f);
		public static Color COLOR_SYSTEM => Pro ? new Color(1f, 0.8f, 0.5f) : new Color(0.55f, 0.36f, 0.02f);
		public static Color COLOR_FRAMEWORK => AetherInspectorTheme.SecondaryTextColor;
		public static Color COLOR_DEPTH_ERROR => Pro ? new Color(1f, 0.4f, 0.4f) : new Color(0.68f, 0.10f, 0.10f);
		public static Color COLOR_DEPTH_WARNING => Pro ? new Color(1f, 0.9f, 0.5f) : new Color(0.54f, 0.40f, 0f);
		public static Color COLOR_SUBSCRIBE => Pro ? new Color(0.5f, 1f, 0.5f) : new Color(0.10f, 0.45f, 0.13f);
		public static Color COLOR_UNSUBSCRIBE => Pro ? new Color(1f, 0.5f, 0.5f) : new Color(0.62f, 0.13f, 0.13f);

		/// <summary>Background for the row the user picked, matching menu/list selection elsewhere.</summary>
		public static Color RowSelectionBackground => AetherInspectorTheme.MenuSelectionBackground;

		/// <summary>Zebra striping for table rows.</summary>
		public static Color RowBackground(int rowIndex) => rowIndex % 2 == 0
			? AetherInspectorTheme.TableRowBackgroundA
			: AetherInspectorTheme.TableRowBackgroundB;

		/// <summary>
		/// Semantic row tint (category highlighting) laid over the zebra striping as a translucent wash,
		/// so the hue survives without deciding the row's lightness for the skin.
		/// </summary>
		public static Color RowTintedBackground(int rowIndex, Color hue, float strength)
		{
			Color baseColor = RowBackground(rowIndex);
			Color tint = hue;
			tint.a = strength;
			return new Color(
				Mathf.Lerp(baseColor.r, tint.r, tint.a),
				Mathf.Lerp(baseColor.g, tint.g, tint.a),
				Mathf.Lerp(baseColor.b, tint.b, tint.a),
				Mathf.Max(baseColor.a, tint.a));
		}

		/// <summary>Hue for rows backed by an IRuleAction, shaded per lifecycle stage.</summary>
		public static Color RULE_ACTION_HUE => Pro ? new Color(0.62f, 0.45f, 0.78f) : new Color(0.42f, 0.24f, 0.62f);
	}
}
#endif

