#if UNITY_EDITOR
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
		
		// Colors
		public static readonly Color COLOR_SORT_HIGHLIGHT = new Color(0.7f, 0.85f, 1f);
		public static readonly Color COLOR_SORT_BUTTON = new Color(0.7f, 0.9f, 1f);
		public static readonly Color COLOR_DOMAIN = new Color(0.5f, 0.8f, 1f);
		public static readonly Color COLOR_SYSTEM = new Color(1f, 0.8f, 0.5f);
		public static readonly Color COLOR_FRAMEWORK = new Color(0.8f, 0.8f, 0.8f);
		public static readonly Color COLOR_DEPTH_ERROR = new Color(1f, 0.4f, 0.4f);
		public static readonly Color COLOR_DEPTH_WARNING = new Color(1f, 0.9f, 0.5f);
		public static readonly Color COLOR_SUBSCRIBE = new Color(0.5f, 1f, 0.5f);
		public static readonly Color COLOR_UNSUBSCRIBE = new Color(1f, 0.5f, 0.5f);
	}
}
#endif

