#if UNITY_EDITOR
#pragma warning disable CS0414 // Serialized/inspector-driven error flags
using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace FoundationPlatform.Editor.Utilities.Messaging
{
	public class SubscriptionHistoryWindow : EditorWindow
	{
		// [MenuItem("Window/EventBus/Subscription History")]
		public static void ShowWindow()
		{
			var win = GetWindow<SubscriptionHistoryWindow>("Subscription History");
			win.minSize = new Vector2(800, 400);
			win.Show();
		}

		public void OnEnableParent()
		{
			OnEnable();
		}

		public void OnDisableParent()
		{
			OnDisable();
		}

		public void OnGUIParent()
		{
			DrawContent();
		}

		public void SetSearch(string term)
		{
			if (_subscriptionsToolbar != null)
			{
				_subscriptionsToolbar.Search = term;
			}
			Rebuild();
			Repaint();
		}

		public static void ShowWindowWithFilter(string eventType = null, string subscriberType = null)
		{
			var win = GetWindow<SubscriptionHistoryWindow>("Subscription History");
			win.minSize = new Vector2(800, 400);
			win.Show();
			win.Focus();

			// Apply filters
			if (!string.IsNullOrEmpty(eventType))
			{
				win._subscriptionsToolbar.Search = eventType;
			}
			else if (!string.IsNullOrEmpty(subscriberType))
			{
				win._subscriptionsToolbar.Search = subscriberType;
			}

			RequestRefresh?.Invoke();
		}

		public static Action RequestRefresh;

		// Error state tracking (surfaced via an explicit HelpBox in DrawContent).
		private string _errorMessage;
		private bool _hasError;

		// Tab controller
		private SubscriptionsTabController _subscriptionsController;

		// Selected row tracking
		[HideInInspector]
		public SubscriptionRow SelectedRow;

		[HideInInspector]
		[SerializeField]
		private SubscriptionsTabToolbar _subscriptionsToolbar = new SubscriptionsTabToolbar();

		private List<SubscriptionRow> _subscriptions = new List<SubscriptionRow>();
		private SimpleEditorTableView<SubscriptionRow> _tableView;

		private double _lastRefresh;
		private const double RefreshInterval = EventBusConstants.DEFAULT_REFRESH_INTERVAL;
		private double _lastPlayModeRefresh;
		private bool _autoRefresh = true;

		// Re-entrancy guard: this window is both created via CreateInstance (Unity fires
		// OnEnable) AND enabled manually by EventBusWindow via OnEnableParent. Without this
		// guard OnEnable runs twice, double-subscribing to editor callbacks.
		[NonSerialized] private bool _enabledGuard;

		private void OnEnable()
		{
			if (_enabledGuard) return;
			_enabledGuard = true;
			EditorApplication.update += EditorTick;
			EditorApplication.playModeStateChanged += OnPlayModeStateChanged;
			EditorApplication.quitting += OnQuitting;
			RequestRefresh = () => { Rebuild(); Repaint(); };
			
			// Subscribe to shared state changes
			var sharedState = EventBusSharedState.Instance;
			sharedState.OnEventTypeFilterChanged += OnSharedEventTypeFilterChanged;
			sharedState.OnSubscriberTypeFilterChanged += OnSharedSubscriberTypeFilterChanged;
			sharedState.OnSearchTermChanged += OnSharedSearchTermChanged;
			
			// Initialize controller
			_subscriptionsController = new SubscriptionsTabController(_subscriptionsToolbar);
			
			// Load persisted state
			LoadState();
			
			// Create table
			CreateTable();
			
			Rebuild();
		}


		private void OnDisable()
		{
			if (!_enabledGuard) return;
			_enabledGuard = false;
			EditorApplication.update -= EditorTick;
			EditorApplication.playModeStateChanged -= OnPlayModeStateChanged;
			EditorApplication.quitting -= OnQuitting;
			RequestRefresh = null;
			
			// Unsubscribe from shared state changes
			var sharedState = EventBusSharedState.Instance;
			sharedState.OnEventTypeFilterChanged -= OnSharedEventTypeFilterChanged;
			sharedState.OnSubscriberTypeFilterChanged -= OnSharedSubscriberTypeFilterChanged;
			sharedState.OnSearchTermChanged -= OnSharedSearchTermChanged;
			
			// Persist state
			SaveState();
		}

		private void OnSharedEventTypeFilterChanged(string eventType)
		{
			if (!string.IsNullOrEmpty(eventType))
			{
				_subscriptionsToolbar.Search = eventType;
				RequestRefresh?.Invoke();
			}
		}

		private void OnSharedSubscriberTypeFilterChanged(string subscriberType)
		{
			if (!string.IsNullOrEmpty(subscriberType))
			{
				_subscriptionsToolbar.Search = subscriberType;
				RequestRefresh?.Invoke();
			}
		}

		private void OnSharedSearchTermChanged(string searchTerm)
		{
			// Only sync if search term is meaningful and different from current
			if (!string.IsNullOrEmpty(searchTerm) && _subscriptionsToolbar.Search != searchTerm)
			{
				_subscriptionsToolbar.Search = searchTerm;
				RequestRefresh?.Invoke();
			}
		}
		
		private void OnPlayModeStateChanged(PlayModeStateChange state)
		{
			if (state == PlayModeStateChange.ExitingEditMode || state == PlayModeStateChange.ExitingPlayMode)
			{
				SaveState();
			}
		}
		
		private void OnQuitting()
		{
			SaveState();
		}

		
		private void SaveState()
		{
			var pos = position;
			EditorPrefs.SetFloat("SubscriptionHistoryWindow.Position.X", pos.x);
			EditorPrefs.SetFloat("SubscriptionHistoryWindow.Position.Y", pos.y);
			EditorPrefs.SetFloat("SubscriptionHistoryWindow.Size.Width", pos.width);
			EditorPrefs.SetFloat("SubscriptionHistoryWindow.Size.Height", pos.height);
			
			EditorPrefs.SetString("SubscriptionHistoryWindow.Search", _subscriptionsToolbar.Search ?? string.Empty);
			EditorPrefs.SetBool("SubscriptionHistoryWindow.AutoRefresh", _autoRefresh);
		}
		
		private void LoadState()
		{
			if (EditorPrefs.HasKey("SubscriptionHistoryWindow.Position.X"))
			{
				var savedX = EditorPrefs.GetFloat("SubscriptionHistoryWindow.Position.X");
				var savedY = EditorPrefs.GetFloat("SubscriptionHistoryWindow.Position.Y");
				var savedWidth = EditorPrefs.GetFloat("SubscriptionHistoryWindow.Size.Width");
				var savedHeight = EditorPrefs.GetFloat("SubscriptionHistoryWindow.Size.Height");
				
				if (position.x == 0 && position.y == 0 && position.width < 100 && position.height < 100)
				{
					position = new Rect(savedX, savedY, savedWidth, savedHeight);
				}
			}
			
			_subscriptionsToolbar.Search = EditorPrefs.GetString("SubscriptionHistoryWindow.Search", string.Empty);
			_autoRefresh = EditorPrefs.GetBool("SubscriptionHistoryWindow.AutoRefresh", true);
		}

		private void EditorTick()
		{
			if (!EventBusEditorWindowRefresh.ShouldPoll(this, _autoRefresh))
				return;

			if (Application.isPlaying)
			{
				var interval = EventBusConstants.DEFAULT_PLAY_MODE_REFRESH_INTERVAL;
				if (EditorApplication.timeSinceStartup - _lastPlayModeRefresh > interval)
				{
					Rebuild();
					_lastPlayModeRefresh = EditorApplication.timeSinceStartup;
					Repaint();
				}
				return;
			}

			if (EditorApplication.timeSinceStartup - _lastRefresh > RefreshInterval)
			{
				Rebuild();
				_lastRefresh = EditorApplication.timeSinceStartup;
				Repaint();
			}
		}

		private void Rebuild()
		{
			_hasError = false;
			_errorMessage = null;
			
			var subscriptionsResult = _subscriptionsController.RebuildSubscriptions();
			if (subscriptionsResult != null)
			{
				_subscriptions = subscriptionsResult;
				if (_tableView != null)
				{
					SyncHeaderSortVisuals();
				}
			}
		}

		private bool SubscriptionsEmpty()
		{
			return _subscriptions == null || _subscriptions.Count == 0;
		}

		private void OnGUI() => DrawContent();

		private void DrawContent()
		{
			DrawToolbar();

			if (_hasError && !string.IsNullOrEmpty(_errorMessage))
			{
				EditorGUILayout.HelpBox(_errorMessage, MessageType.Error);
			}

			// Draw empty state message if needed
			if (SubscriptionsEmpty())
			{
				EditorGUILayout.HelpBox("No subscription activity yet. Enable subscription tracking in Settings → Monitoring → Enable Subscription Tracking, then click Apply. Subscriptions will appear here as they are registered.", MessageType.Info);
			}
			else if (_tableView != null && _subscriptions != null && _subscriptions.Count > 0)
			{
				// Create a copy of the array since SimpleEditorTableView sorts in-place
				var dataArray = _subscriptions.ToArray();
				_tableView.DrawTableGUI(dataArray);
			}
		}

		private void DrawToolbar()
		{
			DrawActionToolbar();
			DrawFiltersToolbar();
		}

		private void DrawActionToolbar()
		{
			EditorGUILayout.BeginHorizontal(EditorStyles.toolbar);
			
			var subscriptionCount = _subscriptions?.Count ?? 0;
			GUILayout.Label($"Subscriptions: {subscriptionCount}", EditorStyles.miniLabel, GUILayout.Width(120));
			
			GUILayout.Space(10);
			
			if (GUILayout.Button("Clear", EditorStyles.toolbarButton, GUILayout.Width(60)))
			{
				_subscriptionsToolbar.Clear();
				Rebuild();
			}
			
			if (GUILayout.Button("Refresh", EditorStyles.toolbarButton, GUILayout.Width(70)))
			{
				_subscriptionsToolbar.Refresh();
			}
			
			GUILayout.FlexibleSpace();

			var newAutoRefresh = GUILayout.Toggle(_autoRefresh, "Auto Refresh", EditorStyles.toolbarButton, GUILayout.Width(100));
			if (newAutoRefresh != _autoRefresh)
				_autoRefresh = newAutoRefresh;

			EditorGUILayout.EndHorizontal();
		}

		private void DrawFiltersToolbar()
		{
			EditorGUILayout.BeginHorizontal(EditorStyles.toolbar);
			
			GUILayout.Label("Search:", GUILayout.Width(50));
			var newSearch = EditorGUILayout.TextField(_subscriptionsToolbar.Search ?? string.Empty, EditorStyles.toolbarTextField, GUILayout.Width(200));
			if (newSearch != _subscriptionsToolbar.Search)
			{
				_subscriptionsToolbar.Search = newSearch;
				if (!string.IsNullOrWhiteSpace(newSearch))
				{
					SearchHistoryManager.AddSearchTerm("Subscriptions", newSearch);
					var sharedState = EventBusSharedState.Instance;
					sharedState.SharedSearchTerm = newSearch;
				}
				RequestRefresh?.Invoke();
			}
			
			// Clear button - show when ANY filter is active (currently only search)
			bool hasActiveFilters = !string.IsNullOrEmpty(_subscriptionsToolbar.Search);
			
			if (hasActiveFilters)
			{
				if (GUILayout.Button("×", EditorStyles.toolbarButton, GUILayout.Width(20)))
				{
					// Clear all filters
					_subscriptionsToolbar.Search = string.Empty;
					
					var sharedState = EventBusSharedState.Instance;
					sharedState.SharedSearchTerm = string.Empty;
					
					Repaint(); // Force repaint to update text field display
					RequestRefresh?.Invoke();
				}
			}
			else
			{
				GUILayout.Space(20); // Reserve space to prevent layout shift
			}
			
			GUILayout.FlexibleSpace(); // Push selection buttons to the right
			
			// Selection buttons (always drawn to prevent layout shifts and IMGUI mismatches, disabled when selection is null)
			bool hasSelection = SelectedRow != null;
			using (new EditorGUI.DisabledScope(!hasSelection))
			{
				#if RULESYSTEM_PRESENT
				// Rule button
				var isRuleHandler = SelectedRow != null && SelectedRow.IsRuleHandler;
				using (new EditorGUI.DisabledScope(!isRuleHandler))
				{
					if (GUILayout.Button("Rule", EditorStyles.toolbarButton, GUILayout.Width(60)))
					{
						if (SelectedRow != null && isRuleHandler && !string.IsNullOrEmpty(SelectedRow.SubscriberType))
						{
							var handlerType = SelectedRow.SubscriberType.Split('.')[0];
							EditorToolNavigation.NavigateToRuleExplorer(handlerType: handlerType);
						}
					}
				}
				#endif
			}
			
			EditorGUILayout.EndHorizontal();
		}

		private void CreateTable()
		{
			_tableView = new SimpleEditorTableView<SubscriptionRow>();
			_tableView.SetRowClickCallback(item =>
			{
				if (SelectedRow == item)
				{
					SelectedRow = null;
					_subscriptionsToolbar.SelectedRow = null;
					return;
				}

				SelectedRow = item;
				_subscriptionsToolbar.SelectedRow = item;
			});

			_tableView.SetRowBackgroundColorCallback((item, rowIndex) =>
			{
				if (SelectedRow == item)
				{
					return new Color(0.35f, 0.6f, 1f, 0.35f);
				}

				// Alternating rows
				return rowIndex % 2 == 0
					? new Color(0.22f, 0.22f, 0.22f, 1f)
					: new Color(0.27f, 0.27f, 0.27f, 1f);
			});

			// Map column indices to SubscriptionsSortBy values
			// Column indices: 0=Time, 1=EventType, 2=Action, 3=SubscriberType, 4=MethodName, 5=Priority
			var columnToSortBy = new Dictionary<int, SubscriptionsSortBy>
			{
				{ 0, SubscriptionsSortBy.Time },
				{ 1, SubscriptionsSortBy.EventType },
				{ 2, SubscriptionsSortBy.Action },
				{ 3, SubscriptionsSortBy.SubscriberType },
				{ 4, SubscriptionsSortBy.MethodName },
				{ 5, SubscriptionsSortBy.Priority }
			};

			// Set up sorting changed callback
			_tableView.SetSortingChangedCallback((columnIndex, ascending) =>
			{
				if (columnToSortBy.TryGetValue(columnIndex, out var sortBy))
				{
					_subscriptionsToolbar.SortBy = sortBy;
					_subscriptionsToolbar.SortDesc = !ascending;
					RequestRefresh?.Invoke();
				}
			});

			// Set up context menu callback
			_tableView.SetContextMenuCallback((item, columnIndex, cellRect) =>
			{
				var menu = new GenericMenu();
				
				// View in History
				menu.AddItem(new GUIContent("View in History"), false, () =>
				{
					item.NavigateToHistory();
				});
				
				// View Subscribers
				menu.AddItem(new GUIContent("View Subscribers"), false, () =>
				{
					item.NavigateToSubscribers();
				});
				
				// Copy (cell content)
				menu.AddItem(new GUIContent("Copy"), false, () =>
				{
					var cellText = GetCellText(item, columnIndex);
					EditorGUIUtility.systemCopyBuffer = cellText;
				});
				
				// Copy Row
				menu.AddItem(new GUIContent("Copy Row"), false, () =>
				{
					var rowText = GetRowText(item);
					EditorGUIUtility.systemCopyBuffer = rowText;
				});
				
				menu.ShowAsContext();
			});

			// Time column
			_tableView.AddColumn("Time", EventBusConstants.COLUMN_WIDTH_TIME, (rect, item) =>
			{
				var originalColor = GUI.color;
				GUI.color = item.GetTimeColor();
				EditorGUI.LabelField(rect, item.Timestamp.ToLocalTime().ToString("HH:mm:ss.fff"));
				GUI.color = originalColor;
			}).SetMaxWidth(EventBusConstants.COLUMN_WIDTH_TIME + 50)
			  .SetSorting((a, b) => a.Timestamp.CompareTo(b.Timestamp));

			// EventType column
			_tableView.AddColumn("EventType", EventBusConstants.COLUMN_WIDTH_TYPE, (rect, item) =>
			{
				var originalColor = GUI.color;
				GUI.color = item.GetEventTypeColor();
				EditorGUI.LabelField(rect, item.EventType ?? string.Empty);
				GUI.color = originalColor;
			}).SetMaxWidth(EventBusConstants.COLUMN_WIDTH_TYPE + 100)
			  .SetSorting((a, b) => string.Compare(a.EventType, b.EventType, StringComparison.Ordinal));

			// Action column
			_tableView.AddColumn("Action", EventBusConstants.COLUMN_WIDTH_ACTION, (rect, item) =>
			{
				var originalColor = GUI.color;
				GUI.color = item.GetActionColor();
				EditorGUI.LabelField(rect, item.Action ?? string.Empty);
				GUI.color = originalColor;
			}).SetMaxWidth(EventBusConstants.COLUMN_WIDTH_ACTION + 50)
			  .SetSorting((a, b) => string.Compare(a.Action, b.Action, StringComparison.Ordinal));

			// SubscriberType column
			_tableView.AddColumn("SubscriberType", EventBusConstants.COLUMN_WIDTH_TYPE, (rect, item) =>
			{
				var originalColor = GUI.color;
				GUI.color = item.GetSubscriberTypeColor();
				EditorGUI.LabelField(rect, item.SubscriberType ?? string.Empty);
				GUI.color = originalColor;
			}).SetMaxWidth(EventBusConstants.COLUMN_WIDTH_TYPE + 100)
			  .SetSorting((a, b) => string.Compare(a.SubscriberType, b.SubscriberType, StringComparison.Ordinal));

			// MethodName column
			_tableView.AddColumn("Method", EventBusConstants.COLUMN_WIDTH_METHOD, (rect, item) =>
			{
				var originalColor = GUI.color;
				GUI.color = item.GetMethodNameColor();
				EditorGUI.LabelField(rect, item.MethodName ?? string.Empty);
				GUI.color = originalColor;
			}).SetMaxWidth(EventBusConstants.COLUMN_WIDTH_METHOD + 100)
			  .SetSorting((a, b) => string.Compare(a.MethodName, b.MethodName, StringComparison.Ordinal));

			// Priority column
			_tableView.AddColumn("Priority", EventBusConstants.COLUMN_WIDTH_PRIORITY, (rect, item) =>
			{
				var originalColor = GUI.color;
				GUI.color = item.GetPriorityColor();
				EditorGUI.LabelField(rect, item.Priority.ToString());
				GUI.color = originalColor;
			}).SetMaxWidth(EventBusConstants.COLUMN_WIDTH_PRIORITY + 30)
			  .SetSorting((a, b) => a.Priority.CompareTo(b.Priority));

			// Sync header visuals with current toolbar sort state
			SyncHeaderSortVisuals();
		}

		private void SyncHeaderSortVisuals()
		{
			if (_tableView == null) return;

			// Map SubscriptionsSortBy to column index
			var sortByToColumn = new Dictionary<SubscriptionsSortBy, int>
			{
				{ SubscriptionsSortBy.Time, 0 },
				{ SubscriptionsSortBy.EventType, 1 },
				{ SubscriptionsSortBy.Action, 2 },
				{ SubscriptionsSortBy.SubscriberType, 3 },
				{ SubscriptionsSortBy.MethodName, 4 },
				{ SubscriptionsSortBy.Priority, 5 }
			};

			if (sortByToColumn.TryGetValue(_subscriptionsToolbar.SortBy, out var columnIndex))
			{
				// MultiColumnHeader uses ascending=true for descending sort (inverted)
				// So we pass !SortDesc to get the correct visual
				_tableView.SetSortedColumn(columnIndex, !_subscriptionsToolbar.SortDesc);
			}
		}

		private string GetCellText(SubscriptionRow item, int columnIndex)
		{
			// Column indices: 0=Time, 1=EventType, 2=Action, 3=SubscriberType, 4=MethodName, 5=Priority
			return columnIndex switch
			{
				0 => item.Timestamp.ToLocalTime().ToString("HH:mm:ss.fff"),
				1 => item.EventType ?? string.Empty,
				2 => item.Action ?? string.Empty,
				3 => item.SubscriberType ?? string.Empty,
				4 => item.MethodName ?? string.Empty,
				5 => item.Priority.ToString(),
				_ => string.Empty
			};
		}

		private string GetRowText(SubscriptionRow item)
		{
			var parts = new List<string>
			{
				$"Time: {item.Timestamp.ToLocalTime():HH:mm:ss.fff}",
				$"EventType: {item.EventType ?? string.Empty}",
				$"Action: {item.Action ?? string.Empty}",
				$"SubscriberType: {item.SubscriberType ?? string.Empty}",
				$"Method: {item.MethodName ?? string.Empty}",
				$"Priority: {item.Priority}"
			};
			
			return string.Join("\t", parts);
		}
	}
}
#endif

