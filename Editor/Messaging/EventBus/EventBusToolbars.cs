#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using AetherNexus.FoundationPlatform.AetherInspector;
using AetherNexus.FoundationPlatform.Messaging;
using UnityEditor;
using UnityEngine;

namespace AetherNexus.FoundationPlatform.Editor.Utilities.Messaging
{
	internal static class EventBusToolbarHelpers
	{
		public static void ApplySearch(string historyKey, string value, Action<string> setSearch, Action refresh)
		{
			setSearch(value);
			if (!string.IsNullOrWhiteSpace(value))
			{
				SearchHistoryManager.AddSearchTerm(historyKey, value);
				EventBusSharedState.Instance.SharedSearchTerm = value;
			}
			refresh?.Invoke();
		}

		public static void ClearSearch(Action<string> setSearch, Action refresh)
		{
			setSearch(string.Empty);
			EventBusSharedState.Instance.SharedSearchTerm = string.Empty;
			refresh?.Invoke();
		}

		public static Action ResolveRefresh(Action primary, Action fallback)
		{
			return primary ?? fallback;
		}
	}

	[Serializable]
	public class HistoryTabToolbar
	{
		[HideInInspector]
		public string Search = string.Empty;
		
		[HideInInspector]
		public HistorySortBy SortBy = HistorySortBy.Timestamp;
		
		[HideInInspector]
		public bool SortDesc = true;
		
		[HideInInspector]
		public EventCategoryFilter CategoryFilter = EventCategoryFilter.All;
		
		[HideInInspector]
		public SubscriberTypeFilter SubscriberFilter = SubscriberTypeFilter.All;
		
		[HideInInspector]
		public TimeRangeFilter TimeRange = TimeRangeFilter.AllTime;

		[HideInInspector]
		public EventRow SelectedRow;

		[HorizontalGroup("historyToolbar")]
		[ShowInInspector]
		[LabelText("Search:"), LabelWidth(EventBusConstants.LABEL_WIDTH_SEARCH)]
		[PropertyOrder(-100)]
		public string SearchField
		{
			get => Search;
			set => EventBusToolbarHelpers.ApplySearch("History", value, v => Search = v, EventPublishHistoryWindow.RequestRefresh);
		}
		
		[HorizontalGroup("historyToolbar", Width = 0.11f, MarginLeft = 10)]
		[LabelText("Category"), LabelWidth(EventBusConstants.LABEL_WIDTH_CATEGORY)]
		[PropertyOrder(-98)]
		[Tooltip("Filter events by category (Domain/System/Framework)")]
		public EventCategoryFilter CategoryFilterField
		{
			get => CategoryFilter;
			set { CategoryFilter = value; EventPublishHistoryWindow.RequestRefresh?.Invoke(); }
		}
		
		[HorizontalGroup("historyToolbar", Width = 0.12f)]
		[LabelText("Time Range"), LabelWidth(70)]
		[PropertyOrder(-96)]
		[Tooltip("Filter events by time range")]
		public TimeRangeFilter TimeRangeField
		{
			get => TimeRange;
			set { TimeRange = value; EventPublishHistoryWindow.RequestRefresh?.Invoke(); }
		}

		[HideInInspector]
		public string SortBySelected
		{
			get => GetSortOptionName(SortBy);
			set
			{
				var newSortBy = GetSortByFromName(value);
				if (newSortBy != SortBy)
				{
					SortBy = newSortBy;
					SortDesc = true;
					EventPublishHistoryWindow.RequestRefresh?.Invoke();
				}
			}
		}


		private List<string> GetSortOptions()
		{
			return new List<string>
			{
				"Time",
				"Type",
				"Channel",
				"Category",
				"Publisher",
				"Subs",
				"Depth"
			};
		}

		private string GetSortOptionName(HistorySortBy sortBy)
		{
			return sortBy switch
			{
				HistorySortBy.Timestamp => "Time",
				HistorySortBy.Type => "Type",
				HistorySortBy.Channel => "Channel",
				HistorySortBy.Category => "Category",
				HistorySortBy.Publisher => "Publisher",
				HistorySortBy.Subscribers => "Subs",
				HistorySortBy.Depth => "Depth",
				_ => "Time"
			};
		}

		private HistorySortBy GetSortByFromName(string name)
		{
			return name switch
			{
				"Time" => HistorySortBy.Timestamp,
				"Type" => HistorySortBy.Type,
				"Channel" => HistorySortBy.Channel,
				"Category" => HistorySortBy.Category,
				"Publisher" => HistorySortBy.Publisher,
				"Subs" => HistorySortBy.Subscribers,
				"Depth" => HistorySortBy.Depth,
				_ => HistorySortBy.Timestamp
			};
		}

		[HorizontalGroup("historyToolbar")]
		[Button(ButtonSizes.Small), LabelText("Refresh")]
		[PropertyOrder(-90)]
		[Tooltip("Refresh the event history")]
		public void Refresh() { EventPublishHistoryWindow.RequestRefresh?.Invoke(); }

		[HorizontalGroup("historyToolbar")]
		[Button(ButtonSizes.Small), LabelText("Clear")]
		[PropertyOrder(-89)]
		[Tooltip("Clear all event history")]
		public void Clear() { EventBus.ClearEventHistory(); }

		[HorizontalGroup("historyToolbar")]
		[Button(ButtonSizes.Small), LabelText("Active Subscriptions")]
		[PropertyOrder(-88)]
		[Tooltip("Open Active Subscriptions window")]
		public void OpenSubscribers() { ActiveSubscriptionsWindow.ShowWindow(); }

		[HorizontalGroup("historyToolbar")]
		[Button(ButtonSizes.Small), LabelText("Subscription History")]
		[PropertyOrder(-87)]
		[Tooltip("Open Subscription History window")]
		public void OpenSubscriptions() { SubscriptionHistoryWindow.ShowWindow(); }

	}

	[Serializable]
	public class SubscribersTabToolbar
	{
		[HideInInspector]
		public string Search = string.Empty;
		
		[HideInInspector]
		public SubscribersSortBy SortBy = SubscribersSortBy.EventType;
		
		[HideInInspector]
		public bool SortDesc = false;

		[HideInInspector]
		public SubscriberRow SelectedRow;

		private static Action RefreshAction =>
			EventBusToolbarHelpers.ResolveRefresh(ActiveSubscriptionsWindow.RequestRefresh, EventPublishHistoryWindow.RequestRefresh);

		[HorizontalGroup("subscribersToolbar")]
		[ShowInInspector]
		[LabelText("Search:"), LabelWidth(EventBusConstants.LABEL_WIDTH_SEARCH)]
		[PropertyOrder(-100)]
		public string SearchField
		{
			get => Search;
			set => EventBusToolbarHelpers.ApplySearch("Subscribers", value, v => Search = v, RefreshAction);
		}

		[HorizontalGroup("subscribersToolbar")]
		[Button("×", ButtonSizes.Small)]
		[PropertyOrder(-99)]
		[ShowIf("@!string.IsNullOrEmpty(Search)")]
		[LabelText("")]
		[HideLabel]
		private void ClearSearch() => EventBusToolbarHelpers.ClearSearch(v => Search = v, RefreshAction);

		[HideInInspector]
		public string SortBySelected
		{
			get => GetSortOptionName(SortBy);
			set
			{
				var newSortBy = GetSortByFromName(value);
				if (newSortBy != SortBy)
				{
					SortBy = newSortBy;
					SortDesc = false;
					RefreshAction?.Invoke();
				}
			}
		}

		[HorizontalGroup("subscribersToolbar")]
		[LabelText("Sort:"), LabelWidth(40)]
		[PropertyOrder(-99)]
		[ValueDropdown("GetSortOptions")]
		[Tooltip("Select sort column")]
		public string SortByDropdown
		{
			get => SortBySelected;
			set => SortBySelected = value;
		}

		[HorizontalGroup("subscribersToolbar")]
		[Button(ButtonSizes.Small)]
		[PropertyOrder(-98)]
		[LabelText("@SortOrderLabel")]
		[GUIColor("@SortDesc ? EventBusConstants.COLOR_SORT_BUTTON : Color.white")]
		[Tooltip("Toggle sort order (Ascending/Descending)")]
		public void ToggleSortOrder()
		{
			SortDesc = !SortDesc;
			RefreshAction?.Invoke();
		}

		[HideInInspector]
		public string SortOrderLabel => SortDesc ? "↓" : "↑";

		private List<string> GetSortOptions()
		{
			return new List<string>
			{
				"EventType",
				"Target",
				"Method",
				"Context",
				"Channel",
				"TokenId"
			};
		}

		private string GetSortOptionName(SubscribersSortBy sortBy)
		{
			return sortBy switch
			{
				SubscribersSortBy.EventType => "EventType",
				SubscribersSortBy.Target => "Target",
				SubscribersSortBy.Method => "Method",
				SubscribersSortBy.Context => "Context",
				SubscribersSortBy.Channel => "Channel",
				SubscribersSortBy.TokenId => "TokenId",
				_ => "EventType"
			};
		}

		private SubscribersSortBy GetSortByFromName(string name)
		{
			return name switch
			{
				"EventType" => SubscribersSortBy.EventType,
				"Target" => SubscribersSortBy.Target,
				"Method" => SubscribersSortBy.Method,
				"Context" => SubscribersSortBy.Context,
				"Channel" => SubscribersSortBy.Channel,
				"TokenId" => SubscribersSortBy.TokenId,
				_ => SubscribersSortBy.EventType
			};
		}

		[HorizontalGroup("subscribersToolbar")]
		[Button(ButtonSizes.Small), LabelText("Refresh")]
		[PropertyOrder(-95)]
		[Tooltip("Refresh the subscribers list")]
		public void Refresh() => RefreshAction?.Invoke();

	}

	[Serializable]
	public class SubscriptionsTabToolbar
	{
		[HideInInspector]
		public string Search = string.Empty;
		
		[HideInInspector]
		public SubscriptionsSortBy SortBy = SubscriptionsSortBy.Time;
		
		[HideInInspector]
		public bool SortDesc = true;

		[HideInInspector]
		public SubscriptionRow SelectedRow;

		private static Action RefreshAction =>
			EventBusToolbarHelpers.ResolveRefresh(SubscriptionHistoryWindow.RequestRefresh, EventPublishHistoryWindow.RequestRefresh);

		[HorizontalGroup("subscriptionsToolbar")]
		[ShowInInspector]
		[LabelText("Search:"), LabelWidth(EventBusConstants.LABEL_WIDTH_SEARCH)]
		[PropertyOrder(-100)]
		public string SearchField
		{
			get => Search;
			set => EventBusToolbarHelpers.ApplySearch("Subscriptions", value, v => Search = v, RefreshAction);
		}

		[HorizontalGroup("subscriptionsToolbar")]
		[Button("×", ButtonSizes.Small)]
		[PropertyOrder(-99)]
		[ShowIf("@!string.IsNullOrEmpty(Search)")]
		[LabelText("")]
		[HideLabel]
		private void ClearSearch() => EventBusToolbarHelpers.ClearSearch(v => Search = v, RefreshAction);

		[HideInInspector]
		public string SortBySelected
		{
			get => GetSortOptionName(SortBy);
			set
			{
				var newSortBy = GetSortByFromName(value);
				if (newSortBy != SortBy)
				{
					SortBy = newSortBy;
					SortDesc = true;
					RefreshAction?.Invoke();
				}
			}
		}

		[HorizontalGroup("subscriptionsToolbar")]
		[LabelText("Sort:"), LabelWidth(40)]
		[PropertyOrder(-99)]
		[ValueDropdown("GetSortOptions")]
		[Tooltip("Select sort column")]
		public string SortByDropdown
		{
			get => SortBySelected;
			set => SortBySelected = value;
		}

		[HorizontalGroup("subscriptionsToolbar")]
		[Button(ButtonSizes.Small)]
		[PropertyOrder(-98)]
		[LabelText("@SortOrderLabel")]
		[GUIColor("@SortDesc ? EventBusConstants.COLOR_SORT_BUTTON : Color.white")]
		[Tooltip("Toggle sort order (Ascending/Descending)")]
		public void ToggleSortOrder()
		{
			SortDesc = !SortDesc;
			RefreshAction?.Invoke();
		}

		[HideInInspector]
		public string SortOrderLabel => SortDesc ? "↓" : "↑";

		private List<string> GetSortOptions()
		{
			return new List<string>
			{
				"Time",
				"EventType",
				"Action",
				"SubType",
				"Method",
				"Priority"
			};
		}

		private string GetSortOptionName(SubscriptionsSortBy sortBy)
		{
			return sortBy switch
			{
				SubscriptionsSortBy.Time => "Time",
				SubscriptionsSortBy.EventType => "EventType",
				SubscriptionsSortBy.Action => "Action",
				SubscriptionsSortBy.SubscriberType => "SubType",
				SubscriptionsSortBy.MethodName => "Method",
				SubscriptionsSortBy.Priority => "Priority",
				_ => "Time"
			};
		}

		private SubscriptionsSortBy GetSortByFromName(string name)
		{
			return name switch
			{
				"Time" => SubscriptionsSortBy.Time,
				"EventType" => SubscriptionsSortBy.EventType,
				"Action" => SubscriptionsSortBy.Action,
				"SubType" => SubscriptionsSortBy.SubscriberType,
				"Method" => SubscriptionsSortBy.MethodName,
				"Priority" => SubscriptionsSortBy.Priority,
				_ => SubscriptionsSortBy.Time
			};
		}

		[HorizontalGroup("subscriptionsToolbar")]
		[Button(ButtonSizes.Small), LabelText("Refresh")]
		[PropertyOrder(-93)]
		[Tooltip("Refresh the subscription history")]
		public void Refresh() => RefreshAction?.Invoke();

		[HorizontalGroup("subscriptionsToolbar")]
		[Button(ButtonSizes.Small), LabelText("Clear")]
		[PropertyOrder(-92)]
		[Tooltip("Clear all subscription history")]
		public void Clear() { EventBus.ClearSubscriptionHistory(); }
	}
}
#endif
