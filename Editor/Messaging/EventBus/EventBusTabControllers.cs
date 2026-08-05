#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.Linq;
using AetherNexus.FoundationPlatform.Messaging;
using UnityEngine;

namespace AetherNexus.FoundationPlatform.Editor.Utilities.Messaging
{
	internal static class EventBusTabCacheHelpers
	{
		public static bool ShouldSkipRebuild(int lastSourceCount, int currentSourceCount, bool filterChanged, out bool sourceChanged)
		{
			sourceChanged = lastSourceCount == -1 || lastSourceCount != currentSourceCount;
			return !sourceChanged && !filterChanged;
		}

		public static bool SearchChanged(string current, string last) =>
			(current ?? string.Empty) != last;

		public static bool SortChanged<T>(T sortBy, T lastSortBy, bool sortDesc, bool lastSortDesc) where T : struct =>
			!EqualityComparer<T>.Default.Equals(sortBy, lastSortBy) || sortDesc != lastSortDesc;

		public static bool ContainsIgnoreCase(string haystack, string needle) =>
			(haystack?.IndexOf(needle, StringComparison.OrdinalIgnoreCase) ?? -1) >= 0;
	}

	public class HistoryTabController
	{
		public HistoryTabToolbar Toolbar { get; }
		private int _lastHistoryCount = -1;
		private int _lastEventBusHistoryCount = -1;
		private List<EventRow> _fullHistoryCache = new List<EventRow>();
		private HistorySortBy _lastHistorySortBy;
		private bool _lastHistorySortDesc;
		private EventCategoryFilter _lastCategoryFilter;
		private TimeRangeFilter _lastTimeRange;
		private SubscriberTypeFilter _lastSubscriberFilter;
		private string _lastSearch = string.Empty;
		private readonly EventPublishHistoryWindow.SettingsModel _settings;
		#if RULESYSTEM_PRESENT
		// Memoize type-name -> IsRuleSystemClass to avoid repeated Type.GetType + reflection
		// on each cache rebuild. Results are static per type name.
		private readonly Dictionary<string, bool> _ruleSystemClassCache = new Dictionary<string, bool>();
		#endif

		public HistoryTabController(HistoryTabToolbar toolbar, EventPublishHistoryWindow.SettingsModel settings)
		{
			Toolbar = toolbar;
			_settings = settings;
			_lastHistorySortBy = toolbar.SortBy;
			_lastHistorySortDesc = toolbar.SortDesc;
			_lastCategoryFilter = toolbar.CategoryFilter;
			_lastTimeRange = toolbar.TimeRange;
			_lastSubscriberFilter = toolbar.SubscriberFilter;
		}

		public List<EventRow> RebuildHistory(out string errorMessage)
		{
			errorMessage = null;
			// GetEventHistory() never returns null - it always returns a list (empty if no history)
			var hist = EventBus.GetEventHistory();
			
			// Check if monitoring might not be enabled (empty list could mean no events OR monitoring disabled)
			// We can't directly check monitoring state, but we can provide guidance when list is empty
			// The window's ApplyMonitoring will handle enabling monitoring if needed
			
			var currentEventBusCount = hist.Count;
			var hasFilterChanged = HasHistorySortChanged() ||
			                       HasCategoryFilterChanged() ||
			                       HasTimeRangeFilterChanged() ||
			                       HasSubscriberFilterChanged() ||
			                       HasSearchChanged();

			if (EventBusTabCacheHelpers.ShouldSkipRebuild(_lastEventBusHistoryCount, currentEventBusCount, hasFilterChanged, out var eventBusHistoryChanged))
				return null;

			if (eventBusHistoryChanged)
			{
				_lastEventBusHistoryCount = currentEventBusCount;
				RebuildFullHistoryCache(hist);
			}

			// Start with a copy of the full cache for filtering
			var list = new List<EventRow>(_fullHistoryCache);
			
			// Update SortBy and SortDesc on all items for color highlighting
			foreach (var item in list)
			{
				item.SortBy = Toolbar.SortBy;
				item.SortDesc = Toolbar.SortDesc;
			}

			// Apply search filter
			if (!string.IsNullOrEmpty(Toolbar.Search))
			{
				var s = Toolbar.Search;
				list = list.Where(r =>
					EventBusTabCacheHelpers.ContainsIgnoreCase(r.TypeName, s) ||
					EventBusTabCacheHelpers.ContainsIgnoreCase(r.Publisher, s) ||
					EventBusTabCacheHelpers.ContainsIgnoreCase(r.Data, s) ||
					EventBusTabCacheHelpers.ContainsIgnoreCase(r.Channel, s)).ToList();
			}
			
			// Apply time range filter
			if (Toolbar.TimeRange != TimeRangeFilter.AllTime)
			{
				var cutoffTime = EventBusHubFormatting.CalculateCutoffTime(Toolbar.TimeRange);
				list = list.Where(r => r.Timestamp >= cutoffTime).ToList();
			}
			
			// Apply category filter
			if (Toolbar.CategoryFilter != EventCategoryFilter.All)
			{
				var category = Toolbar.CategoryFilter switch
				{
					EventCategoryFilter.Domain => EventCategory.Domain,
					EventCategoryFilter.Framework => EventCategory.Framework,
					_ => (EventCategory?)null
				};
				if (category.HasValue)
				{
					list = list.Where(r => r.Category == category.Value).ToList();
				}
			}
			
			// Apply subscriber type filter
			if (Toolbar.SubscriberFilter != SubscriberTypeFilter.All && list.Any())
			{
				list = list.Where(r =>
				{
					if (r.SubscriberDetails == null || r.SubscriberDetails.Count == 0) return false;
					
					return Toolbar.SubscriberFilter switch
					{
						SubscriberTypeFilter.RuleHandler => r.SubscriberDetails.Any(s => s.HandlerType != null && s.HandlerType.Contains("Rules")),
						SubscriberTypeFilter.MonoBehaviour => r.SubscriberDetails.Any(s => s.HandlerType != null && !s.HandlerType.Contains("Rules") && s.HandlerType != "Static"),
						SubscriberTypeFilter.Static => r.SubscriberDetails.Any(s => s.HandlerType == "Static"),
						_ => true
					};
				}).ToList();
			}

			// Apply sorting
			switch (Toolbar.SortBy)
			{
				case HistorySortBy.Timestamp:
					list = (Toolbar.SortDesc ? list.OrderByDescending(x => x.Timestamp) : list.OrderBy(x => x.Timestamp)).ToList();
					break;
				case HistorySortBy.Type:
					list = (Toolbar.SortDesc ? list.OrderByDescending(x => x.TypeName) : list.OrderBy(x => x.TypeName)).ToList();
					break;
				case HistorySortBy.Category:
					list = (Toolbar.SortDesc ? list.OrderByDescending(x => x.Category) : list.OrderBy(x => x.Category)).ToList();
					break;
				case HistorySortBy.Publisher:
					list = (Toolbar.SortDesc ? list.OrderByDescending(x => x.Publisher) : list.OrderBy(x => x.Publisher)).ToList();
					break;
				case HistorySortBy.Subscribers:
					list = (Toolbar.SortDesc ? list.OrderByDescending(x => x.SubscriberCount) : list.OrderBy(x => x.SubscriberCount)).ToList();
					break;
				case HistorySortBy.Depth:
					list = (Toolbar.SortDesc ? list.OrderByDescending(x => x.PublishDepth) : list.OrderBy(x => x.PublishDepth)).ToList();
					break;
				case HistorySortBy.Channel:
					list = (Toolbar.SortDesc ? list.OrderByDescending(x => x.Channel ?? string.Empty) : list.OrderBy(x => x.Channel ?? string.Empty)).ToList();
					break;
			}
			var pageSize = Mathf.Clamp(_settings?.PageSize ?? EventBusConstants.DEFAULT_PAGE_SIZE, EventBusConstants.MIN_PAGE_SIZE, EventBusConstants.MAX_PAGE_SIZE);
			var result = list.Take(pageSize).ToList();
			
			// If result is empty, provide guidance
			if (result.Count == 0 && _settings?.Monitoring != null)
			{
				var monitoring = _settings.Monitoring;
				if (!monitoring.Enabled || !monitoring.EnableEventHistory)
				{
					errorMessage = "Event history is empty. Enable event history tracking in Settings → Monitoring → Enable Event History, then click Apply. After enabling, trigger some events in Play Mode to see them here.";
				}
				else if (_fullHistoryCache.Count > 0)
				{
					// History has data but filters are filtering it all out
					var activeFilters = new List<string>();
					if (!string.IsNullOrEmpty(Toolbar.Search))
						activeFilters.Add($"Search: '{Toolbar.Search}'");
					if (Toolbar.CategoryFilter != EventCategoryFilter.All)
						activeFilters.Add($"Category: {Toolbar.CategoryFilter}");
					if (Toolbar.TimeRange != TimeRangeFilter.AllTime)
						activeFilters.Add($"Time Range: {Toolbar.TimeRange}");
					if (Toolbar.SubscriberFilter != SubscriberTypeFilter.All)
						activeFilters.Add($"Subscriber Type: {Toolbar.SubscriberFilter}");
					
					if (activeFilters.Count > 0)
					{
						errorMessage = $"No events match the current filters. Active filters: {string.Join(", ", activeFilters)}. Clear filters to see all events.";
					}
				}
			}
			
			_lastHistoryCount = result.Count;
			_lastHistorySortBy = Toolbar.SortBy;
			_lastHistorySortDesc = Toolbar.SortDesc;
			_lastCategoryFilter = Toolbar.CategoryFilter;
			_lastTimeRange = Toolbar.TimeRange;
			_lastSubscriberFilter = Toolbar.SubscriberFilter;
			_lastSearch = Toolbar.Search ?? string.Empty;
			
			return result;
		}

		private void RebuildFullHistoryCache(List<EventBus.EventHistoryEntry> hist)
		{
			_fullHistoryCache = hist
				.Select(e =>
				{
					#if RULESYSTEM_PRESENT
					// Extract action type from framework events
					string displayTypeName = e.EventTypeName;
					string eventPhase = null;
					
					if (e.EventCategory == EventCategory.Framework && !string.IsNullOrEmpty(e.ActionTypeName))
					{
						// Use the extracted action type name
						displayTypeName = e.ActionTypeName;
						// Determine event phase from event type name
						if (e.EventTypeName.StartsWith("GameActionValidationEvent<", StringComparison.OrdinalIgnoreCase))
							eventPhase = "Validation";
						else if (e.EventTypeName.StartsWith("GameActionCommitted<", StringComparison.OrdinalIgnoreCase))
							eventPhase = "Committed";
						
						// Add subscript with framework event wrapper info
						if (!string.IsNullOrEmpty(eventPhase))
						{
							displayTypeName = $"{displayTypeName} ({eventPhase})";
						}
					}
					
					// Use original caller if available and publisher is GameAction class
					string displayPublisher = string.IsNullOrEmpty(e.PublisherMethod) 
						? e.PublisherType 
						: $"{e.PublisherType}.{e.PublisherMethod}";
					
					if (!string.IsNullOrEmpty(e.OriginalPublisherType) && !string.IsNullOrEmpty(e.OriginalPublisherMethod))
					{
						// Check if current publisher is a GameAction class
						var publisherTypeName = e.PublisherType;
						if (IsRuleSystemTypeName(publisherTypeName))
						{
							// Use original caller instead
							displayPublisher = string.IsNullOrEmpty(e.OriginalPublisherMethod)
								? e.OriginalPublisherType
								: $"{e.OriginalPublisherType}.{e.OriginalPublisherMethod}";
						}
					}
					
					// Format data using helper method
					string displayData = EventBusHubFormatting.FormatEventData(e);
					
					// Check if this is an IRuleAction-based event
					bool isIRuleActionBased = e.EventCategory == EventCategory.Framework && 
					                          !string.IsNullOrEmpty(e.ActionTypeName);
					
					return new EventRow
					{
						Timestamp = e.Timestamp,
						TypeName = displayTypeName,
						Category = e.EventCategory,
						Channel = e.Channel.ToString(),
						Publisher = displayPublisher,
						SubscriberCount = e.SubscriberCount,
						PublishDepth = e.PublishDepth,
						Data = displayData,
						MaxDepthSnapshot = _settings?.Depth?.MaxDepth ?? EventBusConstants.DEFAULT_MAX_DEPTH,
						WarnPercentSnapshot = _settings?.Depth?.WarnPercent ?? EventBusConstants.DEFAULT_WARN_PERCENT,
						SubscriberDetails = e.SubscriberDetails ?? new List<EventBus.SubscriberDetail>(),
						SortBy = Toolbar.SortBy,
						SortDesc = Toolbar.SortDesc,
						#if RULESYSTEM_PRESENT
						IsIRuleActionBased = isIRuleActionBased
						#endif
					};
					#else
					// Format data using helper method
					string displayData = EventBusHubFormatting.FormatEventData(e);
					
					return new EventRow
					{
						Timestamp = e.Timestamp,
						TypeName = e.EventTypeName,
						Category = e.EventCategory,
						Channel = e.Channel.ToString(),
						Publisher = string.IsNullOrEmpty(e.PublisherMethod) 
							? e.PublisherType 
							: $"{e.PublisherType}.{e.PublisherMethod}",
						SubscriberCount = e.SubscriberCount,
						PublishDepth = e.PublishDepth,
						Data = displayData,
						MaxDepthSnapshot = _settings?.Depth?.MaxDepth ?? EventBusConstants.DEFAULT_MAX_DEPTH,
						WarnPercentSnapshot = _settings?.Depth?.WarnPercent ?? EventBusConstants.DEFAULT_WARN_PERCENT,
						SubscriberDetails = e.SubscriberDetails ?? new List<EventBus.SubscriberDetail>(),
						SortBy = Toolbar.SortBy,
						SortDesc = Toolbar.SortDesc,
						#if RULESYSTEM_PRESENT
						IsIRuleActionBased = false // Non-GameAction builds don't have IRuleAction events
						#endif
					};
					#endif
				})
				.ToList();
			
			#if RULESYSTEM_PRESENT
			// Filter out GameAction infrastructure classes from history cache
			_fullHistoryCache = _fullHistoryCache.Where(e =>
			{
				// Filter out publishers that are GameAction infrastructure classes
				if (!string.IsNullOrEmpty(e.Publisher))
				{
					var publisherTypeName = e.Publisher.Split('.')[0];
					if (IsRuleSystemTypeName(publisherTypeName))
					{
						return false;
					}
				}
				
				// Filter out subscriber details that are GameAction infrastructure classes
				if (e.SubscriberDetails != null && e.SubscriberDetails.Count > 0)
				{
					e.SubscriberDetails = e.SubscriberDetails.Where(s =>
					{
						var handlerTypeName = s.HandlerType;
						if (IsRuleSystemTypeName(handlerTypeName))
						{
							return false;
						}
						return true;
					}).ToList();
					
					// Update subscriber count after filtering
					e.SubscriberCount = e.SubscriberDetails.Count;
				}
				
				return true;
			}).ToList();
			#endif
		}

		#if RULESYSTEM_PRESENT
		// Memoized lookup: resolves "AetherNexus.GameEngineCore.{typeName}, GameEngineCore.Runtime" once per
		// distinct type name and caches whether it is a GameAction infrastructure class.
		private bool IsRuleSystemTypeName(string typeName)
		{
			if (string.IsNullOrEmpty(typeName))
			{
				return false;
			}
			if (_ruleSystemClassCache.TryGetValue(typeName, out var cached))
			{
				return cached;
			}
			var resolvedType = System.Type.GetType($"AetherNexus.GameEngineCore.{typeName}, GameEngineCore.Runtime");
			var result = resolvedType != null && EventBus.IsRuleSystemClass(resolvedType);
			_ruleSystemClassCache[typeName] = result;
			return result;
		}
		#endif

		private bool HasHistorySortChanged() =>
			EventBusTabCacheHelpers.SortChanged(Toolbar.SortBy, _lastHistorySortBy, Toolbar.SortDesc, _lastHistorySortDesc);

		private bool HasCategoryFilterChanged() => Toolbar.CategoryFilter != _lastCategoryFilter;

		private bool HasTimeRangeFilterChanged() => Toolbar.TimeRange != _lastTimeRange;

		private bool HasSubscriberFilterChanged() => Toolbar.SubscriberFilter != _lastSubscriberFilter;

		private bool HasSearchChanged() => EventBusTabCacheHelpers.SearchChanged(Toolbar.Search, _lastSearch);
	}

	public class SubscribersTabController
	{
		public SubscribersTabToolbar Toolbar { get; }
		private int _lastSubscribersCount = -1;
		private int _lastEventBusSubscribersCount = -1;
		private List<SubscriberRow> _fullSubscribersCache = new List<SubscriberRow>();
		private SubscribersSortBy _lastSubscribersSortBy;
		private bool _lastSubscribersSortDesc;
		private string _lastSearch = string.Empty;

		public SubscribersTabController(SubscribersTabToolbar toolbar)
		{
			Toolbar = toolbar;
			_lastSubscribersSortBy = toolbar.SortBy;
			_lastSubscribersSortDesc = toolbar.SortDesc;
		}

		public List<SubscriberRow> RebuildSubscribers(out string errorMessage)
		{
			errorMessage = null;
			var info = EventBus.GetSubscriberDebugInfo();
			if (info == null)
			{
				errorMessage = "EventBus subscriber tracking is unavailable. Enable monitoring in Settings → Monitoring → Enable Event History. Ensure EventBus is properly initialized and try entering Play Mode.";
				return new List<SubscriberRow>();
			}

			var currentEventBusCount = info.Sum(kv => kv.Value?.Count ?? 0);
			var hasFilterChanged = HasSubscribersSortChanged() || HasSearchChanged();

			if (EventBusTabCacheHelpers.ShouldSkipRebuild(_lastEventBusSubscribersCount, currentEventBusCount, hasFilterChanged, out var eventBusSubscribersChanged))
				return null;

			if (eventBusSubscribersChanged)
			{
				_lastEventBusSubscribersCount = currentEventBusCount;
				RebuildFullSubscribersCache(info);
			}

			// Work with a copy of the cache
			var list = new List<SubscriberRow>(_fullSubscribersCache);

			// Update SortBy and SortDesc for color highlighting
			foreach (var item in list)
			{
				item.SortBy = Toolbar.SortBy;
				item.SortDesc = Toolbar.SortDesc;
			}

			// Apply search filter
			if (!string.IsNullOrEmpty(Toolbar.Search))
			{
				var s = Toolbar.Search;
				list = list.Where(r =>
					EventBusTabCacheHelpers.ContainsIgnoreCase(r.EventType, s) ||
					EventBusTabCacheHelpers.ContainsIgnoreCase(r.Target, s) ||
					EventBusTabCacheHelpers.ContainsIgnoreCase(r.Method, s) ||
					EventBusTabCacheHelpers.ContainsIgnoreCase(r.Context, s) ||
					EventBusTabCacheHelpers.ContainsIgnoreCase(r.Channel, s)
				).ToList();
			}

			// Apply sorting
			IOrderedEnumerable<SubscriberRow> ordered;
			switch (Toolbar.SortBy)
			{
				case SubscribersSortBy.EventType:
					ordered = Toolbar.SortDesc ? list.OrderByDescending(r => r.EventType) : list.OrderBy(r => r.EventType);
					break;
				case SubscribersSortBy.Target:
					ordered = Toolbar.SortDesc ? list.OrderByDescending(r => r.Target) : list.OrderBy(r => r.Target);
					break;
				case SubscribersSortBy.Method:
					ordered = Toolbar.SortDesc ? list.OrderByDescending(r => r.Method) : list.OrderBy(r => r.Method);
					break;
				case SubscribersSortBy.Context:
					ordered = Toolbar.SortDesc ? list.OrderByDescending(r => r.Context) : list.OrderBy(r => r.Context);
					break;
				case SubscribersSortBy.Channel:
					ordered = Toolbar.SortDesc ? list.OrderByDescending(r => r.Channel ?? string.Empty) : list.OrderBy(r => r.Channel ?? string.Empty);
					break;
				case SubscribersSortBy.TokenId:
					ordered = Toolbar.SortDesc ? list.OrderByDescending(r => r.TokenId) : list.OrderBy(r => r.TokenId);
					break;
				default:
					ordered = list.OrderBy(r => r.EventType).ThenBy(r => r.Target);
					break;
			}
			var result = ordered.ToList();
			
			_lastSubscribersCount = result.Count;
			_lastSubscribersSortBy = Toolbar.SortBy;
			_lastSubscribersSortDesc = Toolbar.SortDesc;
			_lastSearch = Toolbar.Search ?? string.Empty;
			
			return result;
		}

		private void RebuildFullSubscribersCache(Dictionary<Type, List<string>> info)
		{
			_fullSubscribersCache = info
				.SelectMany(kv => kv.Value.Select(entry =>
				{
					var parsed = EventBusHubFormatting.ParseSubscriberEntry(entry);
					
					#if RULESYSTEM_PRESENT
					// Extract action type from framework events
					string displayEventType = kv.Key?.Name ?? string.Empty;
					if (kv.Key != null && kv.Key.IsGenericType)
					{
						var genericDefinition = kv.Key.GetGenericTypeDefinition();
						var genericDefName = genericDefinition.Name;
						
						// Remove backtick and number (e.g., "GameActionValidationEvent`1" -> "GameActionValidationEvent")
						var indexOfBacktick = genericDefName.IndexOf('`');
						if (indexOfBacktick > 0)
							genericDefName = genericDefName.Substring(0, indexOfBacktick);
						
						if (genericDefName == "GameActionValidationEvent" || genericDefName == "GameActionCommitted")
						{
							// Extract the action type (T parameter)
							var typeArgs = kv.Key.GetGenericArguments();
							if (typeArgs.Length > 0)
							{
								var actionTypeName = EventBusHubFormatting.FormatTypeName(typeArgs[0]);
								var eventPhase = genericDefName == "GameActionValidationEvent" ? "Validation" : "Committed";
								displayEventType = $"{actionTypeName} ({eventPhase})";
							}
						}
					}
					#else
					string displayEventType = kv.Key?.Name ?? string.Empty;
					#endif
					
					return new SubscriberRow
					{
						EventType = displayEventType,
						Target = parsed.Target,
						Method = parsed.Method,
						Context = parsed.Context,
						Channel = parsed.Channel ?? string.Empty,
						TokenId = parsed.TokenId,
						Raw = entry,
						InstanceId = parsed.InstanceId,
						SortBy = Toolbar.SortBy,
						SortDesc = Toolbar.SortDesc
					};
				}))
				.ToList();
		}

		private bool HasSubscribersSortChanged() =>
			EventBusTabCacheHelpers.SortChanged(Toolbar.SortBy, _lastSubscribersSortBy, Toolbar.SortDesc, _lastSubscribersSortDesc);

		private bool HasSearchChanged() => EventBusTabCacheHelpers.SearchChanged(Toolbar.Search, _lastSearch);
	}

	public class SubscriptionsTabController
	{
		public SubscriptionsTabToolbar Toolbar { get; }
		private int _lastSubscriptionsCount = -1;
		private int _lastEventBusSubscriptionsCount = -1;
		private List<SubscriptionRow> _fullSubscriptionsCache = new List<SubscriptionRow>();
		private SubscriptionsSortBy _lastSubscriptionsSortBy;
		private bool _lastSubscriptionsSortDesc;
		private string _lastSearch = string.Empty;

		public SubscriptionsTabController(SubscriptionsTabToolbar toolbar)
		{
			Toolbar = toolbar;
			_lastSubscriptionsSortBy = toolbar.SortBy;
			_lastSubscriptionsSortDesc = toolbar.SortDesc;
		}

		public List<SubscriptionRow> RebuildSubscriptions()
		{
			var subHist = EventBus.GetSubscriptionHistory();
			if (subHist == null)
			{
				return new List<SubscriptionRow>();
			}

			var currentEventBusCount = subHist.Count;
			var hasFilterChanged = HasSubscriptionsSortChanged() || HasSearchChanged();

			if (EventBusTabCacheHelpers.ShouldSkipRebuild(_lastEventBusSubscriptionsCount, currentEventBusCount, hasFilterChanged, out var eventBusSubscriptionsChanged))
				return null;

			if (eventBusSubscriptionsChanged)
			{
				_lastEventBusSubscriptionsCount = currentEventBusCount;
				RebuildFullSubscriptionsCache(subHist);
			}

			// Work with a copy of the cache
			var list = new List<SubscriptionRow>(_fullSubscriptionsCache);

			// Update SortBy and SortDesc for color highlighting
			foreach (var item in list)
			{
				item.SortBy = Toolbar.SortBy;
				item.SortDesc = Toolbar.SortDesc;
			}

			// Apply search filter
			if (!string.IsNullOrEmpty(Toolbar.Search))
			{
				var s = Toolbar.Search;
				list = list.Where(r =>
					EventBusTabCacheHelpers.ContainsIgnoreCase(r.EventType, s) ||
					EventBusTabCacheHelpers.ContainsIgnoreCase(r.SubscriberType, s) ||
					EventBusTabCacheHelpers.ContainsIgnoreCase(r.MethodName, s)
				).ToList();
			}

			// Apply sorting
			IOrderedEnumerable<SubscriptionRow> orderedSubs;
			switch (Toolbar.SortBy)
			{
				case SubscriptionsSortBy.Time:
					orderedSubs = Toolbar.SortDesc ? list.OrderByDescending(r => r.Timestamp) : list.OrderBy(r => r.Timestamp);
					break;
				case SubscriptionsSortBy.EventType:
					orderedSubs = Toolbar.SortDesc ? list.OrderByDescending(r => r.EventType) : list.OrderBy(r => r.EventType);
					break;
				case SubscriptionsSortBy.Action:
					orderedSubs = Toolbar.SortDesc ? list.OrderByDescending(r => r.Action) : list.OrderBy(r => r.Action);
					break;
				case SubscriptionsSortBy.SubscriberType:
					orderedSubs = Toolbar.SortDesc ? list.OrderByDescending(r => r.SubscriberType) : list.OrderBy(r => r.SubscriberType);
					break;
				case SubscriptionsSortBy.MethodName:
					orderedSubs = Toolbar.SortDesc ? list.OrderByDescending(r => r.MethodName) : list.OrderBy(r => r.MethodName);
					break;
				case SubscriptionsSortBy.Priority:
					orderedSubs = Toolbar.SortDesc ? list.OrderByDescending(r => r.Priority) : list.OrderBy(r => r.Priority);
					break;
				default:
					orderedSubs = list.OrderByDescending(r => r.Timestamp);
					break;
			}
			var result = orderedSubs.ToList();
			
			_lastSubscriptionsCount = result.Count;
			_lastSubscriptionsSortBy = Toolbar.SortBy;
			_lastSubscriptionsSortDesc = Toolbar.SortDesc;
			_lastSearch = Toolbar.Search ?? string.Empty;
			
			return result;
		}

		private void RebuildFullSubscriptionsCache(List<EventBus.SubscriptionHistoryEntry> subHist)
		{
			_fullSubscriptionsCache = subHist
				.Select(e =>
				{
					#if RULESYSTEM_PRESENT
					// Extract action type from framework events
					string displayEventType = EventBusHubFormatting.FormatTypeName(e.EventType);
					if (e.EventType != null && e.EventType.IsGenericType)
					{
						var genericDefinition = e.EventType.GetGenericTypeDefinition();
						var genericDefName = genericDefinition.Name;
						
						// Remove backtick and number (e.g., "GameActionValidationEvent`1" -> "GameActionValidationEvent")
						var indexOfBacktick = genericDefName.IndexOf('`');
						if (indexOfBacktick > 0)
							genericDefName = genericDefName.Substring(0, indexOfBacktick);
						
						if (genericDefName == "GameActionValidationEvent" || genericDefName == "GameActionCommitted")
						{
							// Extract the action type (T parameter)
							var typeArgs = e.EventType.GetGenericArguments();
							if (typeArgs.Length > 0)
							{
								var actionTypeName = EventBusHubFormatting.FormatTypeName(typeArgs[0]);
								var eventPhase = genericDefName == "GameActionValidationEvent" ? "Validation" : "Committed";
								displayEventType = $"{actionTypeName} ({eventPhase})";
							}
						}
					}
					#else
					string displayEventType = EventBusHubFormatting.FormatTypeName(e.EventType);
					#endif
					
					return new SubscriptionRow
					{
						Timestamp = e.Timestamp,
						EventType = displayEventType,
						Action = e.Action.ToString(),
						SubscriberType = e.SubscriberType,
						MethodName = e.MethodName,
						Priority = e.Priority,
						SortBy = Toolbar.SortBy,
						SortDesc = Toolbar.SortDesc
					};
				})
				.ToList();
		}

		private bool HasSubscriptionsSortChanged() =>
			EventBusTabCacheHelpers.SortChanged(Toolbar.SortBy, _lastSubscriptionsSortBy, Toolbar.SortDesc, _lastSubscriptionsSortDesc);

		private bool HasSearchChanged() => EventBusTabCacheHelpers.SearchChanged(Toolbar.Search, _lastSearch);
	}
}
#endif

