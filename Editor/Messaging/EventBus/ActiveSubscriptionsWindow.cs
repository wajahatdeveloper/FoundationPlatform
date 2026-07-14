#if UNITY_EDITOR
#pragma warning disable CS0414 // Serialized/inspector-driven error flags
using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace AetherNexus.FoundationPlatform.Editor.Utilities.Messaging
{
	public class ActiveSubscriptionsWindow : EditorWindow
	{
		// [MenuItem("Window/EventBus/Active Subscriptions")]
		public static void ShowWindow()
		{
			var win = GetWindow<ActiveSubscriptionsWindow>("Active Subscriptions");
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
			if (_subscribersToolbar != null)
			{
				_subscribersToolbar.Search = term;
			}
			Rebuild();
			Repaint();
		}

		public static void ShowWindowWithFilter(string eventType = null, string subscriberType = null, string target = null)
		{
			var win = GetWindow<ActiveSubscriptionsWindow>("Active Subscriptions");
			win.minSize = new Vector2(800, 400);
			win.Show();
			win.Focus();

			// Apply filters
			if (!string.IsNullOrEmpty(eventType))
			{
				win._subscribersToolbar.Search = eventType;
			}
			else if (!string.IsNullOrEmpty(subscriberType))
			{
				win._subscribersToolbar.Search = subscriberType;
			}
			else if (!string.IsNullOrEmpty(target))
			{
				win._subscribersToolbar.Search = target;
			}

			RequestRefresh?.Invoke();
		}

		public static Action RequestRefresh;

		// This window instance's own refresh delegate. Used so OnDisable only clears the
		// shared static slot when it still points to THIS instance (multiple windows can
		// exist: a standalone window and a hidden child created by EventBusWindow).
		private Action _requestRefreshHandler;

		// Error state tracking (surfaced via an explicit HelpBox in DrawContent).
		private string _errorMessage;
		private bool _hasError;

		// Tab controller
		private SubscribersTabController _subscribersController;

		// Selected row tracking
		[HideInInspector]
		public SubscriberRow SelectedRow;

		[HideInInspector]
		[SerializeField]
		private SubscribersTabToolbar _subscribersToolbar = new SubscribersTabToolbar();

		private List<SubscriberRow> _subscribers = new List<SubscriberRow>();
		private SimpleEditorTableView<SubscriberRow> _tableView;

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
			_requestRefreshHandler = () => { Rebuild(); Repaint(); };
			RequestRefresh = _requestRefreshHandler;
			
			// Subscribe to shared state changes
			var sharedState = EventBusSharedState.Instance;
			sharedState.OnEventTypeFilterChanged += OnSharedEventTypeFilterChanged;
			sharedState.OnSubscriberTypeFilterChanged += OnSharedSubscriberTypeFilterChanged;
			sharedState.OnSearchTermChanged += OnSharedSearchTermChanged;
			
			// Initialize controller
			_subscribersController = new SubscribersTabController(_subscribersToolbar);
			
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
			// Only clear the shared static slot if it still points to this instance's
			// delegate; otherwise another live window owns it and must keep working.
			if (RequestRefresh == _requestRefreshHandler)
				RequestRefresh = null;
			_requestRefreshHandler = null;
			
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
				_subscribersToolbar.Search = eventType;
				RequestRefresh?.Invoke();
			}
		}

		private void OnSharedSubscriberTypeFilterChanged(string subscriberType)
		{
			if (!string.IsNullOrEmpty(subscriberType))
			{
				_subscribersToolbar.Search = subscriberType;
				RequestRefresh?.Invoke();
			}
		}

		private void OnSharedSearchTermChanged(string searchTerm)
		{
			// Only sync if search term is meaningful and different from current
			if (!string.IsNullOrEmpty(searchTerm) && _subscribersToolbar.Search != searchTerm)
			{
				_subscribersToolbar.Search = searchTerm;
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
			EditorPrefs.SetFloat("ActiveSubscriptionsWindow.Position.X", pos.x);
			EditorPrefs.SetFloat("ActiveSubscriptionsWindow.Position.Y", pos.y);
			EditorPrefs.SetFloat("ActiveSubscriptionsWindow.Size.Width", pos.width);
			EditorPrefs.SetFloat("ActiveSubscriptionsWindow.Size.Height", pos.height);
			
			EditorPrefs.SetString("ActiveSubscriptionsWindow.Search", _subscribersToolbar.Search ?? string.Empty);
			EditorPrefs.SetBool("ActiveSubscriptionsWindow.AutoRefresh", _autoRefresh);
		}
		
		private void LoadState()
		{
			if (EditorPrefs.HasKey("ActiveSubscriptionsWindow.Position.X"))
			{
				var savedX = EditorPrefs.GetFloat("ActiveSubscriptionsWindow.Position.X");
				var savedY = EditorPrefs.GetFloat("ActiveSubscriptionsWindow.Position.Y");
				var savedWidth = EditorPrefs.GetFloat("ActiveSubscriptionsWindow.Size.Width");
				var savedHeight = EditorPrefs.GetFloat("ActiveSubscriptionsWindow.Size.Height");
				
				if (position.x == 0 && position.y == 0 && position.width < 100 && position.height < 100)
				{
					position = new Rect(savedX, savedY, savedWidth, savedHeight);
				}
			}
			
			_subscribersToolbar.Search = EditorPrefs.GetString("ActiveSubscriptionsWindow.Search", string.Empty);
			_autoRefresh = EditorPrefs.GetBool("ActiveSubscriptionsWindow.AutoRefresh", true);
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
			
			var subscribersResult = _subscribersController.RebuildSubscribers(out var subscribersError);
			if (!string.IsNullOrEmpty(subscribersError))
			{
				_hasError = true;
				_errorMessage = subscribersError;
					Debug.LogError($"ActiveSubscriptionsWindow: {subscribersError}");
			}
			if (subscribersResult != null)
			{
				_subscribers = subscribersResult;
				if (_tableView != null)
				{
					SyncHeaderSortVisuals();
				}
			}
		}

		private bool SubscribersEmpty()
		{
			return _subscribers == null || _subscribers.Count == 0;
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
			if (SubscribersEmpty())
			{
				EditorGUILayout.HelpBox("No subscribers detected. Enter Play Mode to see active subscribers, or ensure EventBus is properly initialized and systems have registered their subscriptions.", MessageType.Info);
			}
			else if (_tableView != null && _subscribers != null && _subscribers.Count > 0)
			{
				// Create a copy of the array since SimpleEditorTableView sorts in-place
				var dataArray = _subscribers.ToArray();
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
			
			var subscriberCount = _subscribers?.Count ?? 0;
			GUILayout.Label($"Subscribers: {subscriberCount}", EditorStyles.miniLabel, GUILayout.Width(120));
			
			GUILayout.Space(10);
			
			using (new EditorGUI.DisabledScope(true))
			{
				GUILayout.Button("Clear", EditorStyles.toolbarButton, GUILayout.Width(60));
			}
			
			if (GUILayout.Button("Refresh", EditorStyles.toolbarButton, GUILayout.Width(70)))
			{
				_subscribersToolbar.Refresh();
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
			var newSearch = EditorGUILayout.TextField(_subscribersToolbar.Search ?? string.Empty, EditorStyles.toolbarTextField, GUILayout.Width(200));
			if (newSearch != _subscribersToolbar.Search)
			{
				_subscribersToolbar.Search = newSearch;
				if (!string.IsNullOrWhiteSpace(newSearch))
				{
					SearchHistoryManager.AddSearchTerm("Subscribers", newSearch);
					var sharedState = EventBusSharedState.Instance;
					sharedState.SharedSearchTerm = newSearch;
				}
				RequestRefresh?.Invoke();
			}
			
			// Clear button - show when ANY filter is active (currently only search)
			bool hasActiveFilters = !string.IsNullOrEmpty(_subscribersToolbar.Search);
			
			if (hasActiveFilters)
			{
				if (GUILayout.Button("×", EditorStyles.toolbarButton, GUILayout.Width(20)))
				{
					// Clear all filters
					_subscribersToolbar.Search = string.Empty;
					
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
				// Copy button
				if (GUILayout.Button("Copy", EditorStyles.toolbarButton, GUILayout.Width(60)))
				{
					if (SelectedRow != null)
					{
						EditorGUIUtility.systemCopyBuffer = string.IsNullOrEmpty(SelectedRow.Raw)
							? ($"{SelectedRow.EventType} | {SelectedRow.Target} :: {SelectedRow.Method} @ {SelectedRow.Context}")
							: SelectedRow.Raw;
					}
				}
				
				// Ping button
				var hasInstance = SelectedRow != null && SelectedRow.HasInstance();
				using (new EditorGUI.DisabledScope(!hasInstance))
				{
					if (GUILayout.Button("Ping", EditorStyles.toolbarButton, GUILayout.Width(60)))
					{
						if (SelectedRow != null && hasInstance)
						{
							var obj = EditorUtility.EntityIdToObject(SelectedRow.InstanceId);
							if (obj != null)
							{
								EditorGUIUtility.PingObject(obj);
								Selection.activeObject = obj;
								EditorUtility.FocusProjectWindow();
							}
						}
					}
				}
				
				#if RULESYSTEM_PRESENT
				// Rule button
				var isRuleHandler = SelectedRow != null && SelectedRow.IsRuleHandler;
				using (new EditorGUI.DisabledScope(!isRuleHandler))
				{
					if (GUILayout.Button("Rule", EditorStyles.toolbarButton, GUILayout.Width(60)))
					{
						if (SelectedRow != null && isRuleHandler && !string.IsNullOrEmpty(SelectedRow.Target))
						{
							var handlerType = SelectedRow.Target.Split('.')[0];
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
			_tableView = new SimpleEditorTableView<SubscriberRow>();
			_tableView.SetRowClickCallback(item =>
			{
				if (SelectedRow == item)
				{
					SelectedRow = null;
					_subscribersToolbar.SelectedRow = null;
					return;
				}

				SelectedRow = item;
				_subscribersToolbar.SelectedRow = item;
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

			// Map column indices to SubscribersSortBy values
			// Column indices: 0=EventType, 1=Target, 2=Method, 3=Context, 4=Channel, 5=TokenId
			var columnToSortBy = new Dictionary<int, SubscribersSortBy>
			{
				{ 0, SubscribersSortBy.EventType },
				{ 1, SubscribersSortBy.Target },
				{ 2, SubscribersSortBy.Method },
				{ 3, SubscribersSortBy.Context },
				{ 4, SubscribersSortBy.Channel },
				{ 5, SubscribersSortBy.TokenId }
			};

			// Set up sorting changed callback
			_tableView.SetSortingChangedCallback((columnIndex, ascending) =>
			{
				if (columnToSortBy.TryGetValue(columnIndex, out var sortBy))
				{
					_subscribersToolbar.SortBy = sortBy;
					_subscribersToolbar.SortDesc = !ascending;
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
				
				// View Subscriptions
				menu.AddItem(new GUIContent("View Subscriptions"), false, () =>
				{
					item.NavigateToSubscriptions();
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

			// EventType column
			_tableView.AddColumn("EventType", EventBusConstants.COLUMN_WIDTH_TYPE, (rect, item) =>
			{
				var originalColor = GUI.color;
				GUI.color = item.GetEventTypeColor();
				EditorGUI.LabelField(rect, item.EventType ?? string.Empty);
				GUI.color = originalColor;
			}).SetMaxWidth(EventBusConstants.COLUMN_WIDTH_TYPE + 100)
			  .SetSorting((a, b) => string.Compare(a.EventType, b.EventType, StringComparison.Ordinal));

			// Target column
			_tableView.AddColumn("Target", EventBusConstants.COLUMN_WIDTH_TARGET, (rect, item) =>
			{
				var originalColor = GUI.color;
				GUI.color = item.GetTargetColor();
				EditorGUI.LabelField(rect, item.Target ?? string.Empty);
				GUI.color = originalColor;
			}).SetMaxWidth(EventBusConstants.COLUMN_WIDTH_TARGET + 100)
			  .SetSorting((a, b) => string.Compare(a.Target, b.Target, StringComparison.Ordinal));

			// Method column
			_tableView.AddColumn("Method", EventBusConstants.COLUMN_WIDTH_METHOD, (rect, item) =>
			{
				var originalColor = GUI.color;
				GUI.color = item.GetMethodColor();
				EditorGUI.LabelField(rect, item.Method ?? string.Empty);
				GUI.color = originalColor;
			}).SetMaxWidth(EventBusConstants.COLUMN_WIDTH_METHOD + 100)
			  .SetSorting((a, b) => string.Compare(a.Method, b.Method, StringComparison.Ordinal));

			// Context column
			_tableView.AddColumn("Context", EventBusConstants.COLUMN_WIDTH_CONTEXT, (rect, item) =>
			{
				var originalColor = GUI.color;
				GUI.color = item.GetContextColor();
				// Multi-line support for context
				var style = new GUIStyle(EditorStyles.label) { wordWrap = true };
				EditorGUI.LabelField(rect, item.Context ?? string.Empty, style);
				GUI.color = originalColor;
			}).SetMaxWidth(EventBusConstants.COLUMN_WIDTH_CONTEXT + 200)
			  .SetSorting((a, b) => string.Compare(a.Context, b.Context, StringComparison.Ordinal));

			// Channel column
			_tableView.AddColumn("Channel", 120, (rect, item) =>
			{
				var originalColor = GUI.color;
				GUI.color = item.GetChannelColor();
				EditorGUI.LabelField(rect, item.Channel ?? string.Empty);
				GUI.color = originalColor;
			}).SetMaxWidth(180)
			  .SetSorting((a, b) => string.Compare(a.Channel ?? string.Empty, b.Channel ?? string.Empty, StringComparison.Ordinal));

			// TokenId column
			_tableView.AddColumn("TokenId", 70, (rect, item) =>
			{
				var originalColor = GUI.color;
				GUI.color = item.GetTokenIdColor();
				EditorGUI.LabelField(rect, item.TokenId != 0 ? item.TokenId.ToString() : string.Empty);
				GUI.color = originalColor;
			}).SetMaxWidth(90)
			  .SetSorting((a, b) => a.TokenId.CompareTo(b.TokenId));

			// Sync header visuals with current toolbar sort state
			SyncHeaderSortVisuals();
		}

		private void SyncHeaderSortVisuals()
		{
			if (_tableView == null) return;

			// Map SubscribersSortBy to column index
			var sortByToColumn = new Dictionary<SubscribersSortBy, int>
			{
				{ SubscribersSortBy.EventType, 0 },
				{ SubscribersSortBy.Target, 1 },
				{ SubscribersSortBy.Method, 2 },
				{ SubscribersSortBy.Context, 3 },
				{ SubscribersSortBy.Channel, 4 },
				{ SubscribersSortBy.TokenId, 5 }
			};

			if (sortByToColumn.TryGetValue(_subscribersToolbar.SortBy, out var columnIndex))
			{
				// MultiColumnHeader uses ascending=true for descending sort (inverted)
				// So we pass !SortDesc to get the correct visual
				_tableView.SetSortedColumn(columnIndex, !_subscribersToolbar.SortDesc);
			}
		}

		private string GetCellText(SubscriberRow item, int columnIndex)
		{
			// Column indices: 0=EventType, 1=Target, 2=Method, 3=Context, 4=Channel, 5=TokenId
			return columnIndex switch
			{
				0 => item.EventType ?? string.Empty,
				1 => item.Target ?? string.Empty,
				2 => item.Method ?? string.Empty,
				3 => item.Context ?? string.Empty,
				4 => item.Channel ?? string.Empty,
				5 => item.TokenId != 0 ? item.TokenId.ToString() : string.Empty,
				_ => string.Empty
			};
		}

		private string GetRowText(SubscriberRow item)
		{
			var parts = new List<string>
			{
				$"EventType: {item.EventType ?? string.Empty}",
				$"Target: {item.Target ?? string.Empty}",
				$"Method: {item.Method ?? string.Empty}",
				$"Context: {item.Context ?? string.Empty}",
				$"Channel: {item.Channel ?? string.Empty}",
				$"TokenId: {(item.TokenId != 0 ? item.TokenId.ToString() : "")}"
			};
			
			return string.Join("\t", parts);
		}
	}
}
#endif

