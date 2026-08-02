#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using AetherNexus.FoundationPlatform.Messaging;
using AetherNexus.FoundationPlatform.Utilities.Menus;
using UnityEditor;
using UnityEngine;

namespace AetherNexus.FoundationPlatform.Editor.Utilities.Messaging
{
	public static class SearchHistoryManager
	{
		private const int MAX_SEARCH_HISTORY = 10;
		private const string PREFIX = "EventPublishHistoryWindow.SearchHistory";
		private const string GLOBAL_KEY = "Global";
		
		public static List<string> GetSearchHistory(string key)
		{
			var fullKey = $"{PREFIX}.{key}";
			var json = EditorPrefs.GetString(fullKey, string.Empty);
			if (string.IsNullOrEmpty(json) || json == "[]")
			{
				return new List<string>();
			}
			try
			{
				var list = JsonUtility.FromJson<SearchHistoryList>(json);
				return list?.Items ?? new List<string>();
			}
			catch
			{
				return new List<string>();
			}
		}
		
		public static void AddSearchTerm(string key, string term)
		{
			if (string.IsNullOrWhiteSpace(term)) return;
			
			var fullKey = $"{PREFIX}.{key}";
			var history = GetSearchHistory(key);
			
			// Remove if already exists (to move to front)
			history.RemoveAll(s => string.Equals(s, term, StringComparison.OrdinalIgnoreCase));
			
			// Add to front (newest first)
			history.Insert(0, term);
			
			// Limit to max
			if (history.Count > MAX_SEARCH_HISTORY)
			{
				history = history.Take(MAX_SEARCH_HISTORY).ToList();
			}
			
			// Save
			var wrapper = new SearchHistoryList { Items = history };
			var json = JsonUtility.ToJson(wrapper);
			EditorPrefs.SetString(fullKey, json);
			
			// Also add to global history
			AddGlobalSearchTerm(term);
		}
		
		public static void ClearSearchHistory(string key)
		{
			var fullKey = $"{PREFIX}.{key}";
			EditorPrefs.DeleteKey(fullKey);
		}
		
		public static List<string> GetGlobalSearchHistory()
		{
			return GetSearchHistory(GLOBAL_KEY);
		}
		
		public static void AddGlobalSearchTerm(string term)
		{
			if (string.IsNullOrWhiteSpace(term)) return;
			
			var history = GetGlobalSearchHistory();
			
			// Remove if already exists (to move to front)
			history.RemoveAll(s => string.Equals(s, term, StringComparison.OrdinalIgnoreCase));
			
			// Add to front (newest first)
			history.Insert(0, term);
			
			// Limit to max (global pool can be larger - 30 items)
			if (history.Count > 30)
			{
				history = history.Take(30).ToList();
			}
			
			// Save
			var wrapper = new SearchHistoryList { Items = history };
			var json = JsonUtility.ToJson(wrapper);
			var fullKey = $"{PREFIX}.{GLOBAL_KEY}";
			EditorPrefs.SetString(fullKey, json);
		}
		
		public static void ClearGlobalSearchHistory()
		{
			ClearSearchHistory(GLOBAL_KEY);
		}
		
		[Serializable]
		private class SearchHistoryList
		{
			public List<string> Items = new List<string>();
		}
	}
	
	public static class EditorToolNavigation
	{
		public static void NavigateToEventBusHub(string eventTypeName, string searchTerm)
		{
			EventBusWindow.OpenHistoryTab(eventTypeName, null, searchTerm);
		}

		/// <summary>Navigates with no event type or search term.</summary>
		public static void NavigateToEventBusHub() => NavigateToEventBusHub(null, null);

		public static void NavigateToHistoryWindow(string eventType, string publisher, string searchTerm)
		{
			var sharedState = EventBusSharedState.Instance;
			var context = new NavigationContext
			{
				SourceWindow = "External",
				TargetWindow = "History",
				EventType = eventType ?? string.Empty,
				Publisher = publisher ?? string.Empty,
				SearchTerm = searchTerm ?? string.Empty
			};
			sharedState.NavigationContext = context;

			EventBusWindow.OpenHistoryTab(eventType, publisher, searchTerm);
		}

		/// <summary>Navigates to the history window filtered by event type only.</summary>
		public static void NavigateToHistoryWindow(string eventType) => NavigateToHistoryWindow(eventType, null, null);

		public static void NavigateToSubscribersWindow(string eventType, string subscriberType, string target)
		{
			var sharedState = EventBusSharedState.Instance;
			var context = new NavigationContext
			{
				SourceWindow = "External",
				TargetWindow = "Subscribers",
				EventType = eventType ?? string.Empty,
				SubscriberType = subscriberType ?? string.Empty,
				SearchTerm = target ?? string.Empty
			};
			sharedState.NavigationContext = context;

			EventBusWindow.OpenSubscribersTab(eventType, subscriberType, target);
		}

		/// <summary>Navigates to the subscribers window filtered by event type only.</summary>
		public static void NavigateToSubscribersWindowByEventType(string eventType) => NavigateToSubscribersWindow(eventType, null, null);

		/// <summary>Navigates to the subscribers window filtered by subscriber type only.</summary>
		public static void NavigateToSubscribersWindowBySubscriberType(string subscriberType) => NavigateToSubscribersWindow(null, subscriberType, null);

		public static void NavigateToSubscriptionsWindow(string eventType, string subscriberType)
		{
			var sharedState = EventBusSharedState.Instance;
			var context = new NavigationContext
			{
				SourceWindow = "External",
				TargetWindow = "Subscriptions",
				EventType = eventType ?? string.Empty,
				SubscriberType = subscriberType ?? string.Empty
			};
			sharedState.NavigationContext = context;

			EventBusWindow.OpenSubscriptionsTab(eventType, subscriberType);
		}

		/// <summary>Navigates to the subscriptions window filtered by event type only.</summary>
		public static void NavigateToSubscriptionsWindowByEventType(string eventType) => NavigateToSubscriptionsWindow(eventType, null);

		/// <summary>Navigates to the subscriptions window filtered by subscriber type only.</summary>
		public static void NavigateToSubscriptionsWindowBySubscriberType(string subscriberType) => NavigateToSubscriptionsWindow(null, subscriberType);

		public static void NavigateToRuleExplorer(string ruleName, string handlerType)
		{
			// Search through all assemblies to find RuleExplorerWindow
			System.Type windowType = null;
			foreach (var assembly in System.AppDomain.CurrentDomain.GetAssemblies())
			{
				try
				{
					windowType = assembly.GetType("GameEngineCore.Editor.RuleExplorerWindow");
					if (windowType != null) break;
				}
				catch
				{
					// Continue searching
				}
			}
			
			if (windowType == null) return;
			
			// Get or create the window
			var getWindowMethod = typeof(EditorWindow).GetMethod("GetWindow", new System.Type[] { typeof(string) });
			if (getWindowMethod != null)
			{
				var genericMethod = getWindowMethod.MakeGenericMethod(windowType);
				var window = genericMethod.Invoke(null, new object[] { "Rule Explorer" }) as EditorWindow;
				
				if (window != null)
				{
					window.minSize = new Vector2(800, 600);
					window.Show();
					window.Focus();
					
					// Set search or filter
					if (!string.IsNullOrEmpty(ruleName))
					{
						var setSearchMethod = windowType.GetMethod("SetSearchAndSelect", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Public);
						if (setSearchMethod != null)
						{
							setSearchMethod.Invoke(window, new object[] { ruleName });
						}
					}
					else if (!string.IsNullOrEmpty(handlerType))
					{
						var setFilterMethod = windowType.GetMethod("SetHandlerTypeFilter", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Public);
						if (setFilterMethod != null)
						{
							setFilterMethod.Invoke(window, new object[] { handlerType });
						}
					}
				}
			}
		}

		/// <summary>Navigates to the rule explorer filtered by handler type only.</summary>
		public static void NavigateToRuleExplorerByHandlerType(string handlerType) => NavigateToRuleExplorer(null, handlerType);
	}

	public static class EventBusHubFormatting
	{
		// Cache resolved (and negative) type lookups to avoid re-scanning all
		// assemblies for identical type names across repeated history formatting.
		private static readonly Dictionary<string, Type> TypeLookupCache = new Dictionary<string, Type>();

		public static string FormatTypeName(Type type)
		{
			if (type == null) return "Unknown";
			if (!type.IsGenericType) return type.Name;

			var genericDefinition = type.GetGenericTypeDefinition();
			var typeArgs = type.GetGenericArguments();
			var baseName = genericDefinition.Name;

			var indexOfBacktick = baseName.IndexOf('`');
			if (indexOfBacktick > 0)
				baseName = baseName.Substring(0, indexOfBacktick);

			var typeArgNames = typeArgs.Select(FormatTypeName).ToArray();
			return $"{baseName}<{string.Join(", ", typeArgNames)}>";
		}

		public static string FormatEventData(EventBus.EventHistoryEntry entry)
		{
			#if RULESYSTEM_PRESENT
			// Framework events: Extract key action properties (Actor, Target, Validation status)
			if (entry.EventCategory == EventCategory.Framework && !string.IsNullOrEmpty(entry.ActionData))
			{
				return FormatFrameworkEventData(entry.ActionData);
			}
			#endif

			// Domain/System events: Check if ToString() is meaningful, otherwise use reflection
			return FormatDomainSystemEventData(entry.EventData, entry.EventTypeName, entry.EventCategory);
		}

		#if RULESYSTEM_PRESENT
		private static string FormatFrameworkEventData(string actionData)
		{
			if (string.IsNullOrEmpty(actionData))
				return string.Empty;

			// Parse action ToString() format: "[Type] ... | Validation: status"
			var parts = new List<string>();

			// Extract action type
			var typeMatch = Regex.Match(actionData, @"\[(\w+)\]");
			if (typeMatch.Success)
			{
				var actionType = typeMatch.Groups[1].Value.Trim();
				
				// Extract type-specific information
				switch (actionType)
				{
					case "RegisterUnit":
						// Extract Entity name
						var entityMatch = Regex.Match(actionData, @"Entity:\s*([^|]+)");
						if (entityMatch.Success)
						{
							var entityName = entityMatch.Groups[1].Value.Trim();
							parts.Add($"Entity: {entityName}");
						}
						break;
					
					case "ChangeMatchPhase":
						// Extract State name
						var stateMatch = Regex.Match(actionData, @"State:\s*(\w+)");
						if (stateMatch.Success)
						{
							var stateName = stateMatch.Groups[1].Value.Trim();
							parts.Add($"State: {stateName}");
						}
						break;
					
					case "StartTurn":
					case "EndTurn":
						// Extract Player ID
						var playerIdMatch = Regex.Match(actionData, @"PlayerId:\s*(\d+)");
						if (playerIdMatch.Success)
						{
							var playerId = playerIdMatch.Groups[1].Value.Trim();
							parts.Add($"Player: {playerId}");
						}
						break;
					
					case "GenerateIncome":
						// Extract Amount
						var amountMatch = Regex.Match(actionData, @"Amount:\s*(\d+)");
						if (amountMatch.Success)
						{
							var amount = amountMatch.Groups[1].Value.Trim();
							parts.Add($"Amount: {amount}");
						}
						break;
					
					default:
						// For other actions, extract Actor and Target as before
						var actorMatch = Regex.Match(actionData, @"Actor:\s*([^|→]+)");
						if (actorMatch.Success)
						{
							var actor = actorMatch.Groups[1].Value.Trim();
							parts.Add($"Actor: {actor}");
						}

						var targetMatch = Regex.Match(actionData, @"→\s*Target:\s*([^|]+)");
						if (targetMatch.Success)
						{
							var target = targetMatch.Groups[1].Value.Trim();
							parts.Add($"Target: {target}");
						}
						break;
				}
			}

			return parts.Count > 0 ? string.Join(", ", parts) : actionData;
		}
		#endif

		private static string FormatDomainSystemEventData(string eventData, string eventTypeName, EventCategory category)
		{
			if (string.IsNullOrEmpty(eventData) || eventData == "null")
				return string.Empty;

			var trimmedEventData = eventData.Trim();
			var trimmedTypeName = eventTypeName.Trim();
			
			// Check if this is serialized key-value data (from EventData attributes)
			if (trimmedEventData.Contains(": "))
			{
				// Try compact format first
				var compactFormat = TryFormatCompact(trimmedEventData, trimmedTypeName);
				if (compactFormat != null)
				{
					return compactFormat;
				}
				
				// Fall back to key-value pairs (already formatted)
				return trimmedEventData;
			}
			
			// Check if ToString() returns more than just the type name
			// If eventData is just the type name (e.g., "UnitDestroyedEvent"), it's not meaningful
			if (trimmedEventData == trimmedTypeName || 
			    trimmedEventData == trimmedTypeName + "()" ||
			    (trimmedEventData.StartsWith(trimmedTypeName + "(", StringComparison.Ordinal) && trimmedEventData.EndsWith(")")))
			{
				// ToString() is not meaningful - try to use reflection to get property information
				try
				{
					// Try to find the type by searching all loaded assemblies
					Type eventType = FindTypeInAllAssemblies(trimmedTypeName);
					
					if (eventType != null)
					{
						var properties = eventType.GetProperties(System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.Public)
							.Where(p => p.CanRead && p.GetIndexParameters().Length == 0)
							.Where(p => !p.Name.Equals("Timestamp", StringComparison.OrdinalIgnoreCase) && 
							            !p.Name.Equals("IsValidated", StringComparison.OrdinalIgnoreCase) &&
							            !p.Name.Equals("ValidationMessage", StringComparison.OrdinalIgnoreCase)) // Skip base class properties
							.Select(p => p.Name)
							.Take(8) // Limit to first 8 properties
							.ToList();
						
						if (properties.Count > 0)
						{
							return $"{string.Join(", ", properties)}";
						}
					}
				}
				catch
				{
					// Reflection failed, fall back to simple format
				}
				
				// Fall back to showing the type name with category
				return $"{category} Event";
			}

			// ToString() appears to be meaningful (contains more than just the type name), use it
			return eventData;
		}

		private static string TryFormatCompact(string eventData, string eventTypeName)
		{
			// Parse key-value pairs from serialized data
			var dataDict = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
			var parts = eventData.Split(new[] { ", " }, StringSplitOptions.RemoveEmptyEntries);
			
			foreach (var part in parts)
			{
				var colonIndex = part.IndexOf(": ", StringComparison.Ordinal);
				if (colonIndex > 0)
				{
					var key = part.Substring(0, colonIndex).Trim();
					var value = part.Substring(colonIndex + 2).Trim();
					dataDict[key] = value;
				}
			}
			
			// Try to find the event type and call its FormatCompact method
			try
			{
				var eventType = FindTypeInAllAssemblies(eventTypeName);
				if (eventType != null)
				{
					var formatMethod = eventType.GetMethod("FormatCompact", 
						System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Static,
						null,
						new[] { typeof(Dictionary<string, string>) },
						null);
					
					if (formatMethod != null)
					{
						var result = formatMethod.Invoke(null, new object[] { dataDict }) as string;
						if (!string.IsNullOrEmpty(result))
						{
							return result;
						}
					}
				}
			}
			catch
			{
				// Reflection failed, fall back to key-value pairs
			}
			
			// No FormatCompact method found or it returned null, fall back to key-value pairs
			return null;
		}

		private static Type FindTypeInAllAssemblies(string typeName)
		{
			// Return cached result (including negative/null lookups) to avoid
			// re-scanning every loaded assembly for the same type name.
			if (TypeLookupCache.TryGetValue(typeName, out var cached))
			{
				return cached;
			}

			var resolved = ResolveTypeInAllAssemblies(typeName);
			TypeLookupCache[typeName] = resolved;
			return resolved;
		}

		private static Type ResolveTypeInAllAssemblies(string typeName)
		{
			// First try direct type lookup (works for assembly-qualified names)
			var type = System.Type.GetType(typeName);
			if (type != null) return type;

			// Try common namespace prefixes
			var commonNamespaces = new[]
			{
				"Events",
				"FoundationPlatform.Messaging.EventBus",
				"Scripts.Events",
				"Scripts",
				""
			};

			foreach (var ns in commonNamespaces)
			{
				var fullName = string.IsNullOrEmpty(ns) ? typeName : $"{ns}.{typeName}";
				type = System.Type.GetType(fullName);
				if (type != null) return type;
			}

			// Search through all loaded assemblies
			foreach (var assembly in System.AppDomain.CurrentDomain.GetAssemblies())
			{
				try
				{
					type = assembly.GetType(typeName);
					if (type != null) return type;

					// Also try with namespace prefixes
					foreach (var ns in commonNamespaces)
					{
						if (string.IsNullOrEmpty(ns))
						{
							type = assembly.GetType(typeName);
						}
						else
						{
							type = assembly.GetType($"{ns}.{typeName}");
						}
						if (type != null) return type;
					}
				}
				catch
				{
					// Skip assemblies that can't be queried
					continue;
				}
			}

			return null;
		}

		public static DateTime CalculateCutoffTime(TimeRangeFilter filter)
		{
			var now = DateTime.Now;
			return filter switch
			{
				TimeRangeFilter.Last5Minutes => now.AddMinutes(-5),
				TimeRangeFilter.Last15Minutes => now.AddMinutes(-15),
				TimeRangeFilter.Last1Hour => now.AddHours(-1),
				TimeRangeFilter.Last6Hours => now.AddHours(-6),
				TimeRangeFilter.Last24Hours => now.AddHours(-24),
				_ => DateTime.MinValue
			};
		}

		public struct SubscriberParsed
		{
			public string Target;
			public string Method;
			public string Context;
			public int InstanceId;
			public string Channel;
			public int TokenId;
		}

		public static SubscriberParsed ParseSubscriberEntry(string entry)
		{
			var result = new SubscriberParsed
			{
				Target = string.Empty,
				Method = string.Empty,
				Context = string.Empty,
				InstanceId = 0
			};

			if (string.IsNullOrEmpty(entry)) return result;

			// Extract context after '@' if present
			var parts = entry.Split(new[] { '@' }, 2, StringSplitOptions.RemoveEmptyEntries);
			var left = entry;
			if (parts.Length == 2)
			{
				left = parts[0].Trim();
				result.Context = parts[1].Trim();
			}
			else
			{
				left = entry.Trim();
			}

			// Try to pull InstanceID: N
			var m = Regex.Match(entry, @"InstanceID\s*:\s*(-?\d+)");
			if (m.Success && int.TryParse(m.Groups[1].Value, out var id))
			{
				result.InstanceId = id;
			}

			// Extract channel: [Global] or [Channel: xxx]
			var channelMatch = Regex.Match(entry, @"\[\s*Channel\s*:\s*([^\]]+)\s*\]");
			if (channelMatch.Success)
				result.Channel = channelMatch.Groups[1].Value.Trim();
			else if (entry.Contains("[Global]"))
				result.Channel = "__global__";

			// Extract Token: N
			var tokenMatch = Regex.Match(entry, @"#Token\s*:\s*(\d+)");
			if (tokenMatch.Success && int.TryParse(tokenMatch.Groups[1].Value, out var tokenId))
				result.TokenId = tokenId;

			// Determine method separator (prefer :: over .)
			var idxDouble = left.LastIndexOf("::", StringComparison.Ordinal);
			var idxDot = left.LastIndexOf('.');
			var splitIdx = Math.Max(idxDouble, idxDot);
			if (splitIdx > 0)
			{
				var sepLen = splitIdx == idxDouble ? 2 : 1;
				result.Target = left.Substring(0, splitIdx).Trim();
				result.Method = left.Substring(splitIdx + sepLen).Trim();
			}
			else
			{
				result.Target = left.Trim();
			}

			return result;
		}
	}

	public static class EventBusMonitoringMenu
	{
		private const string MenuPath = MenuPaths.Debug.MonitorEventBus;
		private const string PrefKey = "EventBus.MonitoringEnabled";

		[MenuItem(MenuPath, priority = MenuPriorities.Debug + 4)]
		private static void ToggleMonitoring()
		{
			bool next = !EditorPrefs.GetBool(PrefKey, true);
			EditorPrefs.SetBool(PrefKey, next);

			if (Application.isPlaying)
			{
				EventBus.ConfigureMonitoring(
					next,
					enableEventHistory: next
				);
				EventBus.EnableSubscriptionTracking(next);
				EventBus.SetLoggingLevel(next ? LoggingLevel.Detailed : LoggingLevel.None);
			}

			// Sync the window's settings if it's open
			if (EditorWindow.HasOpenInstances<EventPublishHistoryWindow>())
			{
				var win = EditorWindow.GetWindow<EventPublishHistoryWindow>();
				if (win != null && win.Settings?.Monitoring != null)
				{
					win.Settings.Monitoring.Enabled = next;
					win.Settings.Monitoring.EnableEventHistory = next;
					win.Settings.Monitoring.EnableSubscriptionTracking = next;
					win.Settings.Monitoring.LoggingLevel = next ? LoggingLevel.Detailed : LoggingLevel.None;
					EventPublishHistoryWindow.RequestApplyMonitoring?.Invoke(win.Settings.Monitoring);
				}
			}

			Debug.Log($"[EventBus] Monitoring {(next ? "enabled" : "disabled")}");
		}

		[MenuItem(MenuPath, true)]
		private static bool ToggleMonitoringValidate()
		{
			Menu.SetChecked(MenuPath, EditorPrefs.GetBool(PrefKey, true));
			return true;
		}
	}
}
#endif