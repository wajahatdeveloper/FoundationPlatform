#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.Linq;
using AetherNexus.FoundationPlatform.AetherInspector;
using AetherNexus.FoundationPlatform.Messaging;
using UnityEditor;
using UnityEngine;

namespace AetherNexus.FoundationPlatform.Editor.Utilities.Messaging
{
	public enum HistorySortBy { Timestamp, Type, Category, Publisher, Subscribers, Depth, Channel }
	public enum SubscribersSortBy { EventType, Target, Method, Context, Channel, TokenId }
	public enum SubscriptionsSortBy { Time, EventType, Action, SubscriberType, MethodName, Priority }
	
	public enum TimeRangeFilter
	{
		AllTime,
		Last5Minutes,
		Last15Minutes,
		Last1Hour,
		Last6Hours,
		Last24Hours
	}

	public enum EventCategoryFilter
	{
		All,
		Domain,
		Framework
	}

	public enum SubscriberTypeFilter
	{
		All,
		RuleHandler,
		MonoBehaviour,
		Static
	}

	[Serializable]
	public class EventRow
	{
		// Cached owning window. GetWindow returns the singleton instance and can create/focus
		// the window as a side effect, so resolve it once and reuse it. The Unity null check
		// below detects a destroyed/closed window and triggers a safe re-resolve. Reading the
		// cached reference (instead of GetWindow per row per repaint) keeps IsSelected cheap in
		// the legacy draw path while preserving identical SelectedRow semantics.
		private static EventPublishHistoryWindow _ownerWindow;
		private static EventPublishHistoryWindow OwnerWindow
		{
			get
			{
				if (_ownerWindow == null)
				{
					_ownerWindow = UnityEditor.EditorWindow.GetWindow<EventPublishHistoryWindow>("Event Publish History");
				}
				return _ownerWindow;
			}
		}

		[HideInInspector]
		public DateTime Timestamp;

		[Button("@SelectButtonLabel", ButtonSizes.Small)]
		[GUIColor("@IsSelected ? Color.green : Color.white")]
		[Tooltip("Select this row to use action buttons in the toolbar")]
		private void SelectRow()
		{
			var window = OwnerWindow;
			if (window != null)
			{
				// Toggle selection: if already selected, deselect; otherwise select
				if (window.SelectedRow == this)
				{
					window.SetSelectedRow(null);
				}
				else
				{
					window.SetSelectedRow(this);
				}
			}
		}

		[HideInInspector]
		private string SelectButtonLabel => IsSelected ? "✓" : "Select";

		[HideInInspector]
		private bool IsSelected
		{
			get
			{
				var window = OwnerWindow;
				return window != null && window.SelectedRow == this;
			}
		}

		[ShowInInspector, GUIColor("GetTimeColor")]
		public string Time => Timestamp.ToLocalTime().ToString("HH:mm:ss.fff");
		
		[GUIColor("GetTypeNameColor")] 
		[ReadOnly] 
		public string TypeName;

		[Button("→", ButtonSizes.Small)]
		[Tooltip("Navigate to Subscribers window filtered by this event type")]
		private void NavigateToSubscribersButton()
		{
			NavigateToSubscribers();
		}
		
		[GUIColor("GetCategoryColor")] [ReadOnly] public EventCategory Category;

		[GUIColor("GetChannelColor")] [ReadOnly] public string Channel;

		[GUIColor("GetPublisherColor")] 
		[ReadOnly] 
		public string Publisher;
		
		[GUIColor("GetSubscriberCountColor")] [ReadOnly] public int SubscriberCount;
		
		[GUIColor("GetPublishDepthColor")] [ReadOnly] public int PublishDepth;
		
		[ReadOnly] 
		public string Data;

		[HideInInspector]
		public int MaxDepthSnapshot;
		[HideInInspector]
		public int WarnPercentSnapshot;
		[HideInInspector]
		public List<EventBus.SubscriberDetail> SubscriberDetails;
		[HideInInspector]
		public HistorySortBy SortBy;
		[HideInInspector]
		public bool SortDesc;
		#if RULESYSTEM_PRESENT
		[HideInInspector]
		public bool IsIRuleActionBased;
		#endif

		public Color GetTimeColor()
		{
			return SortBy == HistorySortBy.Timestamp ? EventBusConstants.COLOR_SORT_HIGHLIGHT : Color.white;
		}

		public Color GetTypeNameColor()
		{
			return SortBy == HistorySortBy.Type ? EventBusConstants.COLOR_SORT_HIGHLIGHT : Color.white;
		}

		public Color GetPublisherColor()
		{
			return SortBy == HistorySortBy.Publisher ? EventBusConstants.COLOR_SORT_HIGHLIGHT : Color.white;
		}

		public Color GetSubscriberCountColor()
		{
			return SortBy == HistorySortBy.Subscribers ? EventBusConstants.COLOR_SORT_HIGHLIGHT : Color.white;
		}

		public Color GetPublishDepthColor()
		{
			Color baseColor = GetDepthColor();
			if (SortBy == HistorySortBy.Depth)
			{
				return Color.Lerp(baseColor, EventBusConstants.COLOR_SORT_HIGHLIGHT, 0.7f);
			}
			return baseColor;
		}

		public Color GetDepthColor()
		{
			if (MaxDepthSnapshot <= 0) return Color.white;
			var warnThreshold = Mathf.RoundToInt(MaxDepthSnapshot * (WarnPercentSnapshot / 100f));
			if (PublishDepth > MaxDepthSnapshot) return EventBusConstants.COLOR_DEPTH_ERROR;
			if (PublishDepth >= warnThreshold) return EventBusConstants.COLOR_DEPTH_WARNING;
			return Color.white;
		}

		public Color GetCategoryColor()
		{
			Color baseColor;
			switch (Category)
			{
				case EventCategory.Domain: baseColor = EventBusConstants.COLOR_DOMAIN; break;
				case EventCategory.Framework: baseColor = EventBusConstants.COLOR_FRAMEWORK; break;
				default: baseColor = Color.white; break;
			}
			if (SortBy == HistorySortBy.Category)
			{
				return Color.Lerp(baseColor, EventBusConstants.COLOR_SORT_HIGHLIGHT, 0.7f);
			}
			return baseColor;
		}

		public Color GetChannelColor()
		{
			return SortBy == HistorySortBy.Channel ? EventBusConstants.COLOR_SORT_HIGHLIGHT : Color.white;
		}
		
		#if RULESYSTEM_PRESENT
		[HideInInspector]
		public bool HasRuleHandlerSubscriber => SubscriberDetails != null && SubscriberDetails.Any(s => 
			s.HandlerType != null && (s.HandlerType.Contains("Rule") || s.HandlerType.Contains("Handler")));
		
		public string GetFirstRuleHandler()
		{
			if (SubscriberDetails == null) return null;
			var ruleHandler = SubscriberDetails.FirstOrDefault(s => 
				s.HandlerType != null && (s.HandlerType.Contains("Rule") || s.HandlerType.Contains("Handler")));
			return ruleHandler.HandlerType != null ? ruleHandler.HandlerType : null;
		}
		#endif

		[ContextMenu("Show Subscribers")]
		public void NavigateToSubscribers()
		{
			if (!string.IsNullOrEmpty(TypeName))
			{
				var sharedState = EventBusSharedState.Instance;
				var context = new NavigationContext
				{
					SourceWindow = "History",
					TargetWindow = "Subscribers",
					EventType = TypeName
				};
				sharedState.NavigationContext = context;
				EditorToolNavigation.NavigateToSubscribersWindowByEventType(TypeName);
			}
		}

		[ContextMenu("Show Subscriptions")]
		private void NavigateToSubscriptions()
		{
			if (!string.IsNullOrEmpty(TypeName))
			{
				var sharedState = EventBusSharedState.Instance;
				var context = new NavigationContext
				{
					SourceWindow = "History",
					TargetWindow = "Subscriptions",
					EventType = TypeName
				};
				sharedState.NavigationContext = context;
				EditorToolNavigation.NavigateToSubscriptionsWindowByEventType(TypeName);
			}
		}
	}

	[Serializable]
	public class SubscriberDetailRow
	{
		[TableColumnWidth(80)] [ReadOnly] public int Priority;
		[TableColumnWidth(220)] [ReadOnly] public string HandlerType;
		[TableColumnWidth(160)] [ReadOnly] public string MethodName;
		[HideInInspector] public bool Executed;
		[TableColumnWidth(100)] 
		[GUIColor("@Executed ? Color.green : Color.red")]
		[ReadOnly] 
		public string Status => Executed ? "✓ Executed" : "✗ Failed";
		[TableColumnWidth(300)] 
		[ShowIf("@!Executed && !string.IsNullOrEmpty(ErrorMessage)")]
		[ReadOnly] 
		public string ErrorMessage;
		
		[HideInInspector] public string EventTypeName;

		[TableColumnWidth(EventBusConstants.COLUMN_WIDTH_BUTTON)]
		[Button("🔍 Filter", ButtonSizes.Small)]
		[Tooltip("Filter history by this subscriber")]
		private void FilterBySubscriber()
		{
			if (!string.IsNullOrEmpty(HandlerType))
			{
				var window = UnityEditor.EditorWindow.GetWindow<EventPublishHistoryWindow>("Event Publish History");
				if (window != null)
				{
					window.SetSearchTerm(HandlerType);
				}
			}
		}
		
		[TableColumnWidth(EventBusConstants.COLUMN_WIDTH_BUTTON)]
		[Button("🔍 Event", ButtonSizes.Small)]
		[Tooltip("Filter history by this event type")]
		private void FilterByEventType()
		{
			if (!string.IsNullOrEmpty(EventTypeName))
			{
				var window = UnityEditor.EditorWindow.GetWindow<EventPublishHistoryWindow>("Event Publish History");
				if (window != null)
				{
					window.SetSearchTerm(EventTypeName);
				}
			}
		}
	}

	[Serializable]
	public class SubscriberRow
	{
		[GUIColor("GetEventTypeColor"), TableColumnWidth(EventBusConstants.COLUMN_WIDTH_TYPE)] [ReadOnly] public string EventType;
		[GUIColor("GetTargetColor"), TableColumnWidth(EventBusConstants.COLUMN_WIDTH_TARGET)] [ReadOnly] public string Target;
		[GUIColor("GetMethodColor"), TableColumnWidth(EventBusConstants.COLUMN_WIDTH_METHOD)] [ReadOnly] public string Method;
		[GUIColor("GetContextColor"), TableColumnWidth(EventBusConstants.COLUMN_WIDTH_CONTEXT), MultiLineProperty(2)] [ReadOnly] public string Context;
		[GUIColor("GetChannelColor"), TableColumnWidth(120)] [ReadOnly] public string Channel;
		[GUIColor("GetTokenIdColor"), TableColumnWidth(70)] [ReadOnly] public int TokenId;
		[HideInInspector] public string Raw;
		[HideInInspector] public int InstanceId;
		[HideInInspector] public SubscribersSortBy SortBy;
		[HideInInspector] public bool SortDesc;

		public bool HasInstance() => InstanceId > 0;
		
		#if RULESYSTEM_PRESENT
		[HideInInspector]
		public bool IsRuleHandler => !string.IsNullOrEmpty(Target) && 
		                             (Target.Contains("Rule") || Target.Contains("Handler"));
		#endif

		public Color GetEventTypeColor()
		{
			return SortBy == SubscribersSortBy.EventType ? EventBusConstants.COLOR_SORT_HIGHLIGHT : Color.white;
		}

		public Color GetTargetColor()
		{
			return SortBy == SubscribersSortBy.Target ? EventBusConstants.COLOR_SORT_HIGHLIGHT : Color.white;
		}

		public Color GetMethodColor()
		{
			return SortBy == SubscribersSortBy.Method ? EventBusConstants.COLOR_SORT_HIGHLIGHT : Color.white;
		}

		public Color GetContextColor()
		{
			return SortBy == SubscribersSortBy.Context ? EventBusConstants.COLOR_SORT_HIGHLIGHT : Color.white;
		}

		public Color GetChannelColor()
		{
			return SortBy == SubscribersSortBy.Channel ? EventBusConstants.COLOR_SORT_HIGHLIGHT : Color.white;
		}

		public Color GetTokenIdColor()
		{
			return SortBy == SubscribersSortBy.TokenId ? EventBusConstants.COLOR_SORT_HIGHLIGHT : Color.white;
		}

		[TableColumnWidth(EventBusConstants.COLUMN_WIDTH_BUTTON)]
		[Button("→", ButtonSizes.Small)]
		[Tooltip("Navigate to History window filtered by this event type")]
		private void NavigateToHistoryButton()
		{
			NavigateToHistory();
		}

		[TableColumnWidth(EventBusConstants.COLUMN_WIDTH_BUTTON)]
		[Button("→", ButtonSizes.Small)]
		[Tooltip("Navigate to Subscriptions window filtered by this subscriber")]
		private void NavigateToSubscriptionsButton()
		{
			NavigateToSubscriptions();
		}

		[ContextMenu("Show in History")]
		public void NavigateToHistory()
		{
			if (!string.IsNullOrEmpty(EventType))
			{
				var sharedState = EventBusSharedState.Instance;
				var context = new NavigationContext
				{
					SourceWindow = "Subscribers",
					TargetWindow = "History",
					EventType = EventType
				};
				sharedState.NavigationContext = context;
				EditorToolNavigation.NavigateToHistoryWindow(eventType: EventType);
			}
		}

		[ContextMenu("Show Subscriptions")]
		public void NavigateToSubscriptions()
		{
			if (!string.IsNullOrEmpty(Target))
			{
				var sharedState = EventBusSharedState.Instance;
				var context = new NavigationContext
				{
					SourceWindow = "Subscribers",
					TargetWindow = "Subscriptions",
					SubscriberType = Target
				};
				sharedState.NavigationContext = context;
				EditorToolNavigation.NavigateToSubscriptionsWindowBySubscriberType(Target);
			}
		}
	}

	[Serializable]
	public class SubscriptionRow
	{
		[ShowInInspector, GUIColor("GetTimeColor"), TableColumnWidth(EventBusConstants.COLUMN_WIDTH_TIME)]
		public string Time => Timestamp.ToLocalTime().ToString("HH:mm:ss.fff");
		[GUIColor("GetEventTypeColor"), TableColumnWidth(EventBusConstants.COLUMN_WIDTH_TYPE)] [ReadOnly] public string EventType;
		[GUIColor("GetActionColor"), TableColumnWidth(EventBusConstants.COLUMN_WIDTH_ACTION)] [ReadOnly] public string Action;
		[GUIColor("GetSubscriberTypeColor"), TableColumnWidth(EventBusConstants.COLUMN_WIDTH_TYPE)] [ReadOnly] public string SubscriberType;
		[GUIColor("GetMethodNameColor"), TableColumnWidth(EventBusConstants.COLUMN_WIDTH_METHOD)] [ReadOnly] public string MethodName;
		[GUIColor("GetPriorityColor"), TableColumnWidth(EventBusConstants.COLUMN_WIDTH_PRIORITY)] [ReadOnly] public int Priority;

		[HideInInspector]
		public DateTime Timestamp;
		[HideInInspector]
		public SubscriptionsSortBy SortBy;
		[HideInInspector]
		public bool SortDesc;

		public Color GetTimeColor()
		{
			return SortBy == SubscriptionsSortBy.Time ? EventBusConstants.COLOR_SORT_HIGHLIGHT : Color.white;
		}

		public Color GetEventTypeColor()
		{
			return SortBy == SubscriptionsSortBy.EventType ? EventBusConstants.COLOR_SORT_HIGHLIGHT : Color.white;
		}

		public Color GetActionColor()
		{
			Color baseColor = Action == "Subscribe" ? EventBusConstants.COLOR_SUBSCRIBE : EventBusConstants.COLOR_UNSUBSCRIBE;
			if (SortBy == SubscriptionsSortBy.Action)
			{
				return Color.Lerp(baseColor, EventBusConstants.COLOR_SORT_HIGHLIGHT, 0.7f);
			}
			return baseColor;
		}

		public Color GetSubscriberTypeColor()
		{
			return SortBy == SubscriptionsSortBy.SubscriberType ? EventBusConstants.COLOR_SORT_HIGHLIGHT : Color.white;
		}

		public Color GetMethodNameColor()
		{
			return SortBy == SubscriptionsSortBy.MethodName ? EventBusConstants.COLOR_SORT_HIGHLIGHT : Color.white;
		}

		public Color GetPriorityColor()
		{
			return SortBy == SubscriptionsSortBy.Priority ? EventBusConstants.COLOR_SORT_HIGHLIGHT : Color.white;
		}

		[TableColumnWidth(EventBusConstants.COLUMN_WIDTH_BUTTON)]
		[Button("→", ButtonSizes.Small)]
		[Tooltip("Navigate to History window filtered by this event type")]
		private void NavigateToHistoryButton()
		{
			NavigateToHistory();
		}

		[TableColumnWidth(EventBusConstants.COLUMN_WIDTH_BUTTON)]
		[Button("→", ButtonSizes.Small)]
		[Tooltip("Navigate to Subscribers window filtered by this subscriber type")]
		private void NavigateToSubscribersButton()
		{
			NavigateToSubscribers();
		}

		[ContextMenu("Show in History")]
		public void NavigateToHistory()
		{
			if (!string.IsNullOrEmpty(EventType))
			{
				var sharedState = EventBusSharedState.Instance;
				var context = new NavigationContext
				{
					SourceWindow = "Subscriptions",
					TargetWindow = "History",
					EventType = EventType
				};
				sharedState.NavigationContext = context;
				EditorToolNavigation.NavigateToHistoryWindow(eventType: EventType);
			}
		}

		[ContextMenu("Show Subscribers")]
		public void NavigateToSubscribers()
		{
			if (!string.IsNullOrEmpty(SubscriberType))
			{
				var sharedState = EventBusSharedState.Instance;
				var context = new NavigationContext
				{
					SourceWindow = "Subscriptions",
					TargetWindow = "Subscribers",
					SubscriberType = SubscriberType
				};
				sharedState.NavigationContext = context;
				EditorToolNavigation.NavigateToSubscribersWindowBySubscriberType(SubscriberType);
			}
		}
		
		#if RULESYSTEM_PRESENT
		[HideInInspector]
		public bool IsRuleHandler => !string.IsNullOrEmpty(SubscriberType) && 
		                             (SubscriberType.Contains("Rule") || SubscriberType.Contains("Handler"));
		#endif
	}

	[Serializable]
	public class DepthModel
	{
		[LabelText("Max Depth"), Tooltip("Maximum depth of event publishing chain before error. Prevents infinite loops from circular event dependencies."), Range(EventBusConstants.MIN_MAX_DEPTH, EventBusConstants.MAX_MAX_DEPTH)] 
		public int MaxDepth = EventBusConstants.DEFAULT_MAX_DEPTH;
		
		[LabelText("Warn %"), Tooltip("Percentage of max depth at which to log warnings. When publish depth reaches this percentage, warnings are logged."), Range(EventBusConstants.MIN_WARN_PERCENT, EventBusConstants.MAX_WARN_PERCENT)] 
		public int WarnPercent = EventBusConstants.DEFAULT_WARN_PERCENT;
		
		[LabelText("Stop On Exceeded"), Tooltip("If true, publishing stops when depth limit is exceeded. If false, only logs an error but continues.")] 
		public bool StopOnExceeded = true;
		
		[LabelText("Warn Near Limit"), Tooltip("If true, logs warnings when approaching the depth limit (based on Warn %). If false, only logs when limit is exceeded.")] 
		public bool WarnNear = true;
	}

	[Serializable]
	public class MonitoringModel
	{
		[LabelText("Monitoring Enabled"), Tooltip("Enable/disable EventBus monitoring features. Must be enabled for event history, subscription tracking, and depth protection to work.")] 
		public bool Enabled = true;
		
		[LabelText("Enable Event History"), Tooltip("Track event publish history for debugging. Records publisher info, subscriber details, and execution results. Editor-only, no runtime overhead.")] 
		public bool EnableEventHistory = true;
		
		[LabelText("Enable Subscription Tracking"), Tooltip("Track subscription/unsubscription lifecycle events. Helps identify subscription leaks and understand subscription patterns. Editor-only.")] 
		public bool EnableSubscriptionTracking = true;
		
		[LabelText("Auto Refresh"), Tooltip("Automatically refresh the EventBus Hub window. Uses the refresh interval below.")] 
		public bool AutoRefresh = true;
		
		[LabelText("Play Mode Auto Refresh"), Tooltip("Automatically refresh the EventBus Hub window in play mode. Uses the refresh interval below.")] 
		public bool AutoRefreshInPlayMode = true;
		
		[LabelText("Play Mode Interval (s)"), Tooltip("Refresh interval in seconds for play mode auto-refresh. Lower values update more frequently but may impact performance."), Range(EventBusConstants.MIN_REFRESH_INTERVAL, EventBusConstants.MAX_REFRESH_INTERVAL)] 
		public float PlayModeRefreshInterval = EventBusConstants.DEFAULT_PLAY_MODE_REFRESH_INTERVAL;
		
		[Title("History Limits")]
		[LabelText("Max Event History Size"), Tooltip("Maximum number of event publish events to keep in history. Older entries are removed when limit is reached."), Range(EventBusConstants.MIN_HISTORY_SIZE, EventBusConstants.MAX_HISTORY_SIZE)] 
		public int MaxEventHistorySize = EventBusConstants.DEFAULT_MAX_EVENT_HISTORY;
		
		[LabelText("Max Subscription History Size"), Tooltip("Maximum number of subscription/unsubscription events to keep in history. Older entries are removed when limit is reached."), Range(EventBusConstants.MIN_HISTORY_SIZE, EventBusConstants.MAX_HISTORY_SIZE)] 
		public int MaxSubscriptionHistorySize = EventBusConstants.DEFAULT_MAX_SUBSCRIPTION_HISTORY;
		
		[Title("Logging")]
		[LabelText("Logging Level"), Tooltip("Verbosity level for EventBus logging:\n- None: No logging\n- Minimal: Basic event publish logging\n- Detailed: Full logging with publisher/subscriber details and subscription lifecycle")] 
		public LoggingLevel LoggingLevel = LoggingLevel.Detailed;
	}
}
#endif

