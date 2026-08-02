#if UNITY_EDITOR
#pragma warning disable CS0414 // Serialized/inspector-driven error flags
using System;
using System.Collections.Generic;
using AetherNexus.FoundationPlatform.AetherInspector;
using AetherNexus.FoundationPlatform.AetherInspector.Editor;
using AetherNexus.FoundationPlatform.Messaging;
using UnityEditor;
using UnityEngine;

namespace AetherNexus.FoundationPlatform.Editor.Utilities.Messaging
{
	public class EventPublishHistoryWindow : EditorWindow
	{
		private SerializedObject _serializedThis;
		private static GUIStyle _richTextLabelStyle;
		private static GUIStyle RichTextLabelStyle
		{
			get
			{
				if (_richTextLabelStyle == null)
				{
					_richTextLabelStyle = new GUIStyle(EditorStyles.label);
					_richTextLabelStyle.richText = true;
				}
				return _richTextLabelStyle;
			}
		}

		// [MenuItem("Window/EventBus/Event Publish History", priority = 100)]
		public static void ShowWindow()
		{
			var win = GetWindow<EventPublishHistoryWindow>(WINDOW_TITLE);
			win.minSize = new Vector2(1024, 600);
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

		/// <summary>Shows the window with no filters applied.</summary>
		public static void ShowWindowWithFilter() => ShowWindowWithFilter(null, null, null);

		public static void ShowWindowWithFilter(string eventType, string publisher, string searchTerm)
		{
			var win = GetWindow<EventPublishHistoryWindow>(WINDOW_TITLE);
			win.minSize = new Vector2(1024, 600);
			win.Show();
			win.Focus();

			// Apply filters
			if (!string.IsNullOrEmpty(eventType))
			{
				win.SetSearchAndFilter(eventType);
			}
			else if (!string.IsNullOrEmpty(publisher))
			{
				win.SetSearchTerm(publisher);
			}
			else if (!string.IsNullOrEmpty(searchTerm))
			{
				win.SetSearchTerm(searchTerm);
			}
		}

		public static Action RequestRefresh;
		public static Action<MonitoringModel> RequestApplyMonitoring;

		// Error state tracking (surfaced via an explicit HelpBox in DrawContent).
		private string _errorMessage;
		private bool _hasError;

		// Tab controller
		private HistoryTabController _historyController;

		// Selected row tracking
		[HideInInspector]
		public EventRow SelectedRow;

		// History toolbar (hidden from the inspector, used for data storage)
		[HideInInspector]
		[SerializeField]
		private HistoryTabToolbar _historyToolbar = new HistoryTabToolbar();

		// Settings panel visibility
		[HideInInspector]
		private bool _settingsExpanded = false;

		[HideInInspector]
		private List<EventRow> _history = new List<EventRow>();

		[HideInInspector]
		private SimpleEditorTableView<EventRow> _tableView;

		[SerializeField]
		public SettingsModel Settings = new SettingsModel();

		[System.Serializable]
		public class SettingsModel
		{
			public MonitoringModel Monitoring = new MonitoringModel();

			public DepthModel Depth = new DepthModel();

			[Tooltip("Number of items per page in the history table. UI-only paging; EventBus history size is managed by the bus.")]
			[Range(EventBusConstants.MIN_PAGE_SIZE, EventBusConstants.MAX_PAGE_SIZE)]
			public int PageSize = EventBusConstants.DEFAULT_PAGE_SIZE;

			[Tooltip("Automatically refresh the EventBus Hub window")]
			public bool AutoRefresh
			{
				get => Monitoring.AutoRefresh;
				set => Monitoring.AutoRefresh = value;
			}

			[Tooltip("Automatically refresh in play mode")]
			public bool AutoRefreshInPlayMode
			{
				get => Monitoring.AutoRefreshInPlayMode;
				set => Monitoring.AutoRefreshInPlayMode = value;
			}

			[Tooltip("Refresh interval in seconds for play mode")]
			[Range(EventBusConstants.MIN_REFRESH_INTERVAL, EventBusConstants.MAX_REFRESH_INTERVAL)]
			public float PlayModeRefreshInterval
			{
				get => Monitoring.PlayModeRefreshInterval;
				set => Monitoring.PlayModeRefreshInterval = value;
			}

		}

		private double _lastRefresh;
		private const double RefreshInterval = EventBusConstants.DEFAULT_REFRESH_INTERVAL;
		private double _lastPlayModeRefresh;
		private const string WINDOW_TITLE = "Event Publish History";

		// Re-entrancy guard: this window is both created via CreateInstance (Unity fires
		// OnEnable) AND enabled manually by EventBusWindow via OnEnableParent. Without this
		// guard OnEnable runs twice, so ApplyMonitoring logs twice and editor callbacks
		// double-subscribe.
		[NonSerialized] private bool _enabledGuard;

		private void OnEnable()
		{
			if (_enabledGuard) return;
			_enabledGuard = true;
			EditorApplication.update += EditorTick;
			EditorApplication.playModeStateChanged += OnPlayModeStateChanged;
			EditorApplication.quitting += OnQuitting;
			RequestRefresh = () => { Rebuild(); Repaint(); };
			RequestApplyMonitoring = (model) => { ApplyMonitoring(model); };
			
			// Subscribe to shared state changes
			var sharedState = EventBusSharedState.Instance;
			sharedState.OnEventTypeFilterChanged += OnSharedEventTypeFilterChanged;
			sharedState.OnPublisherFilterChanged += OnSharedPublisherFilterChanged;
			sharedState.OnSearchTermChanged += OnSharedSearchTermChanged;
			
			// Initialize controller - ensure Settings is initialized first
			if (Settings == null)
			{
				Settings = new SettingsModel();
			}
			_historyController = new HistoryTabController(_historyToolbar, Settings);
			
			// Load persisted state
			LoadState();
			
			// Initialize table
			CreateTable();
			
			// Always apply monitoring settings on window open to ensure EventBus is configured
			ApplyMonitoring(Settings.Monitoring);
			
			// Force initial rebuild
			Rebuild();
		}

		private void UpdateSelection()
		{
			// TableList selection is not directly accessible
			// We maintain selection state by validating that SelectedRow is still in the current list
			// Selection will be set manually through row interactions (e.g., buttons on rows)
			
			if (SelectedRow != null)
			{
				// Verify the selected row is still in the current history list
				if (_history == null || !_history.Contains(SelectedRow))
				{
					// Selected row is no longer in the list (filtered out or removed)
					SelectedRow = null;
					_historyToolbar.SelectedRow = null;
					return;
				}
				
				// Ensure toolbar is in sync
				if (_historyToolbar.SelectedRow != SelectedRow)
				{
					_historyToolbar.SelectedRow = SelectedRow;
				}
			}
			else if (_historyToolbar.SelectedRow != null)
			{
				// Clear toolbar selection if window selection is null
				_historyToolbar.SelectedRow = null;
			}
		}

		private void OnDisable()
		{
			if (!_enabledGuard) return;
			_enabledGuard = false;
			EditorApplication.update -= EditorTick;
			EditorApplication.playModeStateChanged -= OnPlayModeStateChanged;
			EditorApplication.quitting -= OnQuitting;
			RequestRefresh = null;
			RequestApplyMonitoring = null;
			
			// Unsubscribe from shared state changes
			var sharedState = EventBusSharedState.Instance;
			sharedState.OnEventTypeFilterChanged -= OnSharedEventTypeFilterChanged;
			sharedState.OnPublisherFilterChanged -= OnSharedPublisherFilterChanged;
			sharedState.OnSearchTermChanged -= OnSharedSearchTermChanged;
			
			// Persist state
			SaveState();
		}

		private void OnSharedEventTypeFilterChanged(string eventType)
		{
			if (!string.IsNullOrEmpty(eventType))
			{
				_historyToolbar.Search = eventType;
				RequestRefresh?.Invoke();
			}
		}

		private void OnSharedPublisherFilterChanged(string publisher)
		{
			if (!string.IsNullOrEmpty(publisher))
			{
				_historyToolbar.Search = publisher;
				RequestRefresh?.Invoke();
			}
		}

		private void OnSharedSearchTermChanged(string searchTerm)
		{
			// Only sync if search term is meaningful and different from current
			if (!string.IsNullOrEmpty(searchTerm) && _historyToolbar.Search != searchTerm)
			{
				_historyToolbar.Search = searchTerm;
				RequestRefresh?.Invoke();
			}
		}

	private void OnGUI() => DrawContent();

	private void DrawContent()
	{
		if (_hasError && !string.IsNullOrEmpty(_errorMessage))
		{
			GuiKit.ValidationBox(_errorMessage);
		}

		DrawToolbar();

		// Calculate available height for table
		// Window height minus toolbars (2 toolbars) and settings panel if expanded
		float toolbarHeight = EditorStyles.toolbar.fixedHeight * 2; // Two toolbars
		float availableHeight = position.height - toolbarHeight;
		
		// Reserve space for settings if expanded, then draw table with remaining space
		// Settings will be drawn after the table (explicit panel in DrawContent).
		if (_settingsExpanded)
		{
			// Estimate settings height (typically 300-400 pixels when fully expanded)
			// This ensures table doesn't overflow when settings are visible
			availableHeight -= 350f;
		}
		
		// Ensure minimum height for table
		availableHeight = Mathf.Max(availableHeight, 100f);
		
		// Draw empty state message if needed
		if (HistoryEmpty())
		{
			GuiKit.InfoBox("No events recorded yet. Enable history tracking in Settings → Monitoring → Enable Event History, then click Apply. After enabling, trigger some events in Play Mode to see them here.");
		}
		else if (_tableView != null && _history != null && _history.Count > 0)
		{
			// Create a copy of the array since SimpleEditorTableView sorts in-place
			var dataArray = _history.ToArray();
			// Pass maxHeight to constrain table height, allowing settings to fit below
			_tableView.DrawTableGUI(dataArray, maxHeight: availableHeight);
		}
		
		// Settings panel (formerly drawn by the window base's OnImGUI()).
		if (_settingsExpanded)
		{
			_serializedThis ??= new SerializedObject(this);
			_serializedThis.Update();
			var settingsProp = _serializedThis.FindProperty(nameof(Settings));
			if (settingsProp != null)
			{
				EditorGUILayout.PropertyField(settingsProp, new GUIContent("Settings"), true);
			}
			_serializedThis.ApplyModifiedProperties();
		}
	}
		
		private void OnPlayModeStateChanged(PlayModeStateChange state)
		{
			// Save state before domain reload (when entering or exiting play mode)
			if (state == PlayModeStateChange.ExitingEditMode || state == PlayModeStateChange.ExitingPlayMode)
			{
				SaveState();
			}
		}
		
		private void OnQuitting()
		{
			// Save state before Unity closes
			SaveState();
		}
		
		public void SetSearchAndFilter(string eventTypeName)
		{
			_historyToolbar.Search = eventTypeName;
			var sharedState = EventBusSharedState.Instance;
			sharedState.EventTypeFilter = eventTypeName;
			sharedState.SharedSearchTerm = eventTypeName;
			RequestRefresh?.Invoke();
		}
		
		public void SetSearchTerm(string searchTerm)
		{
			_historyToolbar.Search = searchTerm;
			var sharedState = EventBusSharedState.Instance;
			sharedState.SharedSearchTerm = searchTerm;
			RequestRefresh?.Invoke();
		}

		public void SetSelectedRow(EventRow row)
		{
			SelectedRow = row;
			_historyToolbar.SelectedRow = row;
			Repaint();
		}

		private void OnHistoryChanged()
		{
			// Clear selection if the selected row is no longer in the list
			// UpdateSelection() will handle syncing, but we need to check here too
			// in case the list was completely replaced
			if (SelectedRow != null && (_history == null || !_history.Contains(SelectedRow)))
			{
				SelectedRow = null;
				_historyToolbar.SelectedRow = null;
			}
		}
		
		private void SaveState()
		{
			var pos = position;
			EditorPrefs.SetFloat("EventPublishHistoryWindow.Position.X", pos.x);
			EditorPrefs.SetFloat("EventPublishHistoryWindow.Position.Y", pos.y);
			EditorPrefs.SetFloat("EventPublishHistoryWindow.Size.Width", pos.width);
			EditorPrefs.SetFloat("EventPublishHistoryWindow.Size.Height", pos.height);
			
			// Save settings panel state
			EditorPrefs.SetBool("EventPublishHistoryWindow.SettingsExpanded", _settingsExpanded);
			
			// Save toolbar state
			EditorPrefs.SetString("EventPublishHistoryWindow.Search", _historyToolbar.Search ?? string.Empty);
			EditorPrefs.SetString("EventPublishHistoryWindow.CategoryFilter", _historyToolbar.CategoryFilter.ToString());
			EditorPrefs.SetString("EventPublishHistoryWindow.TimeRange", _historyToolbar.TimeRange.ToString());
			EditorPrefs.SetString("EventPublishHistoryWindow.SortBy", _historyToolbar.SortBy.ToString());
			EditorPrefs.SetBool("EventPublishHistoryWindow.SortDesc", _historyToolbar.SortDesc);
		}
		
		private void LoadState()
		{
			// Load window position/size only if not already set (avoid overriding docked windows)
			if (EditorPrefs.HasKey("EventPublishHistoryWindow.Position.X"))
			{
				var savedX = EditorPrefs.GetFloat("EventPublishHistoryWindow.Position.X");
				var savedY = EditorPrefs.GetFloat("EventPublishHistoryWindow.Position.Y");
				var savedWidth = EditorPrefs.GetFloat("EventPublishHistoryWindow.Size.Width");
				var savedHeight = EditorPrefs.GetFloat("EventPublishHistoryWindow.Size.Height");
				
				// Only restore if current position is invalid (e.g., at 0,0 or very small)
				// This prevents overriding Unity's docking system
				if (position.x == 0 && position.y == 0 && position.width < 100 && position.height < 100)
				{
					position = new Rect(savedX, savedY, savedWidth, savedHeight);
				}
			}
			
			// Load settings panel state
			_settingsExpanded = EditorPrefs.GetBool("EventPublishHistoryWindow.SettingsExpanded", false);
			
			// Load toolbar state
			_historyToolbar.Search = EditorPrefs.GetString("EventPublishHistoryWindow.Search", string.Empty);
			
			if (Enum.TryParse<EventCategoryFilter>(EditorPrefs.GetString("EventPublishHistoryWindow.CategoryFilter", "All"), out var categoryFilter))
			{
				_historyToolbar.CategoryFilter = categoryFilter;
			}
			
			if (Enum.TryParse<TimeRangeFilter>(EditorPrefs.GetString("EventPublishHistoryWindow.TimeRange", "AllTime"), out var timeRange))
			{
				_historyToolbar.TimeRange = timeRange;
			}
			
			if (Enum.TryParse<HistorySortBy>(EditorPrefs.GetString("EventPublishHistoryWindow.SortBy", "Timestamp"), out var sortBy))
			{
				_historyToolbar.SortBy = sortBy;
			}
			
			_historyToolbar.SortDesc = EditorPrefs.GetBool("EventPublishHistoryWindow.SortDesc", true);
		}

		private void EditorTick()
		{
			var autoRefresh = Settings.Monitoring.AutoRefresh;
			if (!EventBusEditorWindowRefresh.ShouldPoll(this, autoRefresh))
				return;

			if (Application.isPlaying)
			{
				var interval = Mathf.Max(EventBusConstants.MIN_REFRESH_INTERVAL, Settings.Monitoring.PlayModeRefreshInterval);
				if (!Settings.Monitoring.AutoRefreshInPlayMode) return;
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
			// Clear previous errors
			_hasError = false;
			_errorMessage = null;
			
			// Ensure controller is initialized
			if (_historyController == null)
			{
				_historyController = new HistoryTabController(_historyToolbar, Settings);
			}
			
			// Rebuild history using controller
			// RebuildHistory() returns null if no rebuild needed, or a list if rebuild is needed
			var historyResult = _historyController.RebuildHistory(out var historyError);
			
			// Handle errors and informational messages
			if (!string.IsNullOrEmpty(historyError))
			{
				_hasError = true;
				_errorMessage = historyError;
				
				// Only log actual errors (not filter-related informational messages)
				// Filter messages are informational and already displayed in the UI
				if (!historyError.Contains("No events match the current filters"))
				{
					if (historyError.Contains("Event history is empty"))
						Debug.LogWarning($"EventPublishHistoryWindow: {historyError}");
					else
						Debug.LogError($"EventPublishHistoryWindow: {historyError}");
				}
			}
			
			// Initialize history list if needed
			if (_history == null)
			{
				_history = new List<EventRow>();
			}
			
			// Update the history list if we have a result
			if (historyResult != null)
			{
				// Update the list in place
				_history.Clear();
				_history.AddRange(historyResult);
				// Call OnHistoryChanged to handle selection updates
				OnHistoryChanged();
			}
			
			// Refresh the display after rebuild (filters or other state may have changed).
			Repaint();

			// Update selection after rebuild
			UpdateSelection();
		}

		private bool HistoryEmpty()
		{
			return _history == null || _history.Count == 0;
		}

		private void ApplyMonitoring(MonitoringModel model)
		{
			var depth = Settings.Depth;
			try
			{
				// Use public API directly (no reflection needed)
				EventBus.ConfigureMonitoring(
					model.Enabled,
					depth.MaxDepth,
					depth.StopOnExceeded,
					depth.WarnNear,
					depth.WarnPercent,
					model.EnableEventHistory
				);
				
				// Apply subscription tracking
				EventBus.EnableSubscriptionTracking(model.EnableSubscriptionTracking);
				
				// Apply max event history
				EventBus.SetMaxHistoryEntries(model.MaxEventHistorySize);
				
				// Apply max subscription history
				EventBus.SetMaxSubscriptionHistoryEntries(model.MaxSubscriptionHistorySize);
				
				// Apply logging level
				EventBus.SetLoggingLevel(model.LoggingLevel);
				
				Debug.Log("[EventBus Hub] Monitoring configuration applied.");
			}
			catch (Exception ex)
			{
				Debug.LogError($"EventPublishHistoryWindow: Error applying monitoring: {ex.Message}");
			}
		}

		private void CreateTable()
		{
			_tableView = new SimpleEditorTableView<EventRow>();
			_tableView.SetRowClickCallback(item =>
			{
				if (SelectedRow == item)
				{
					SetSelectedRow(null);
					return;
				}

				SetSelectedRow(item);
			});

			_tableView.SetRowBackgroundColorCallback((item, rowIndex) =>
			{
				if (SelectedRow == item)
				{
					return new Color(0.35f, 0.6f, 1f, 0.35f);
				}

				#if RULESYSTEM_PRESENT
				// Highlight IRuleAction-based events with mild purple
				if (item.IsIRuleActionBased)
				{
					// Differentiate validation vs committed actions
					bool isValidation = !string.IsNullOrEmpty(item.TypeName) && item.TypeName.Contains("(Validation)");
					bool isCommitted = !string.IsNullOrEmpty(item.TypeName) && item.TypeName.Contains("(Committed)");
					
					if (isValidation)
					{
						// Lighter purple for validation actions
						return rowIndex % 2 == 0
							? new Color(0.4f, 0.3f, 0.45f, 1f)  // Lighter purple for even rows
							: new Color(0.45f, 0.35f, 0.5f, 1f);  // Even lighter purple for odd rows
					}
					else if (isCommitted)
					{
						// Darker purple for committed actions
						return rowIndex % 2 == 0
							? new Color(0.35f, 0.25f, 0.4f, 1f)  // Darker purple for even rows
							: new Color(0.4f, 0.3f, 0.45f, 1f);  // Slightly lighter purple for odd rows
					}
					else
					{
						// Default purple for other action-based events
						return rowIndex % 2 == 0
							? new Color(0.35f, 0.25f, 0.4f, 1f)  // Mild purple for even rows
							: new Color(0.4f, 0.3f, 0.45f, 1f);  // Slightly lighter mild purple for odd rows
					}
				}
				#endif

				// Slightly lighter alternating rows to match request
				return rowIndex % 2 == 0
					? new Color(0.22f, 0.22f, 0.22f, 1f)
					: new Color(0.27f, 0.27f, 0.27f, 1f);
			});

			// Map column indices to HistorySortBy values
			// Column indices: 0=Time, 1=Type, 2=Channel, 3=Data (not sortable), 4=Category, 5=Publisher, 6=Subs, 7=Depth
			var columnToSortBy = new Dictionary<int, HistorySortBy>
			{
				{ 0, HistorySortBy.Timestamp },
				{ 1, HistorySortBy.Type },
				{ 2, HistorySortBy.Channel },
				{ 4, HistorySortBy.Category },
				{ 5, HistorySortBy.Publisher },
				{ 6, HistorySortBy.Subscribers },
				{ 7, HistorySortBy.Depth }
			};

			// Set up sorting changed callback
			_tableView.SetSortingChangedCallback((columnIndex, ascending) =>
			{
				if (columnToSortBy.TryGetValue(columnIndex, out var sortBy))
				{
					_historyToolbar.SortBy = sortBy;
					_historyToolbar.SortDesc = !ascending;
					RequestRefresh?.Invoke();
				}
			});

			// Set up context menu callback
			_tableView.SetContextMenuCallback((item, columnIndex, cellRect) =>
			{
				var menu = new GenericMenu();
				
				// View Subscriptions
				menu.AddItem(new GUIContent("View Subscriptions"), false, () =>
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
			}).SetSorting((a, b) => a.Timestamp.CompareTo(b.Timestamp));

			// TypeName column
			_tableView.AddColumn("Type", EventBusConstants.COLUMN_WIDTH_TYPE, (rect, item) =>
			{
				var originalColor = GUI.color;
				GUI.color = item.GetTypeNameColor();
				
				// Apply color to "Committed" (green) and "Validation" (gray) words
				string displayText = item.TypeName ?? string.Empty;
				if (displayText.Contains("(Committed)"))
				{
					displayText = displayText.Replace("(Committed)", "<color=green>(Committed)</color>");
				}
				if (displayText.Contains("(Validation)"))
				{
					displayText = displayText.Replace("(Validation)", "<color=grey>(Validation)</color>");
				}
				
				// Use RichTextLabelStyle to ensure RichText is properly rendered
				EditorGUI.LabelField(rect, new GUIContent(displayText), RichTextLabelStyle);
				GUI.color = originalColor;
			}).SetSorting((a, b) => string.Compare(a.TypeName, b.TypeName, StringComparison.Ordinal));

			// Channel column
			_tableView.AddColumn("Channel", 120, (rect, item) =>
			{
				var originalColor = GUI.color;
				GUI.color = item.GetChannelColor();
				EditorGUI.LabelField(rect, item.Channel ?? string.Empty);
				GUI.color = originalColor;
			}).SetSorting((a, b) => string.Compare(a.Channel ?? string.Empty, b.Channel ?? string.Empty, StringComparison.Ordinal));

			// Data column
			_tableView.AddColumn("Data", EventBusConstants.COLUMN_WIDTH_DATA, (rect, item) =>
			{
				EditorGUI.LabelField(rect, item.Data ?? string.Empty);
			});

			// Category column
			_tableView.AddColumn("Category", EventBusConstants.COLUMN_WIDTH_CATEGORY, (rect, item) =>
			{
				var originalColor = GUI.color;
				GUI.color = item.GetCategoryColor();
				EditorGUI.LabelField(rect, item.Category.ToString());
				GUI.color = originalColor;
			}).SetSorting((a, b) => a.Category.CompareTo(b.Category));

			// Publisher column
			_tableView.AddColumn("Publisher", EventBusConstants.COLUMN_WIDTH_PUBLISHER, (rect, item) =>
			{
				var originalColor = GUI.color;
				GUI.color = item.GetPublisherColor();
				EditorGUI.LabelField(rect, item.Publisher);
				GUI.color = originalColor;
			}).SetSorting((a, b) => string.Compare(a.Publisher, b.Publisher, StringComparison.Ordinal));

			// SubscriberCount column
			_tableView.AddColumn("Subs", EventBusConstants.COLUMN_WIDTH_SUBSCRIBER_COUNT, (rect, item) =>
			{
				var originalColor = GUI.color;
				GUI.color = item.GetSubscriberCountColor();
				EditorGUI.LabelField(rect, item.SubscriberCount.ToString());
				GUI.color = originalColor;
			}).SetSorting((a, b) => a.SubscriberCount.CompareTo(b.SubscriberCount));

			// PublishDepth column
			_tableView.AddColumn("Depth", EventBusConstants.COLUMN_WIDTH_DEPTH, (rect, item) =>
			{
				var originalColor = GUI.color;
				GUI.color = item.GetPublishDepthColor();
				EditorGUI.LabelField(rect, item.PublishDepth.ToString());
				GUI.color = originalColor;
			}).SetSorting((a, b) => a.PublishDepth.CompareTo(b.PublishDepth));

			// Sync header visuals with current toolbar sort state
			SyncHeaderSortVisuals();
		}

		private void SyncHeaderSortVisuals()
		{
			if (_tableView == null) return;

			// Map HistorySortBy to column index
			var sortByToColumn = new Dictionary<HistorySortBy, int>
			{
				{ HistorySortBy.Timestamp, 0 },
				{ HistorySortBy.Type, 1 },
				{ HistorySortBy.Channel, 2 },
				{ HistorySortBy.Category, 4 },
				{ HistorySortBy.Publisher, 5 },
				{ HistorySortBy.Subscribers, 6 },
				{ HistorySortBy.Depth, 7 }
			};

			if (sortByToColumn.TryGetValue(_historyToolbar.SortBy, out var columnIndex))
			{
				// MultiColumnHeader uses ascending=true for descending sort (inverted)
				// So we pass !SortDesc to get the correct visual
				_tableView.SetSortedColumn(columnIndex, !_historyToolbar.SortDesc);
			}
		}

		private string GetCellText(EventRow item, int columnIndex)
		{
			// Column indices: 0=Time, 1=Type, 2=Channel, 3=Data, 4=Category, 5=Publisher, 6=Subs, 7=Depth
			return columnIndex switch
			{
				0 => item.Timestamp.ToLocalTime().ToString("HH:mm:ss.fff"),
				1 => item.TypeName ?? string.Empty,
				2 => item.Channel ?? string.Empty,
				3 => item.Data ?? string.Empty,
				4 => item.Category.ToString(),
				5 => item.Publisher ?? string.Empty,
				6 => item.SubscriberCount.ToString(),
				7 => item.PublishDepth.ToString(),
				_ => string.Empty
			};
		}

		private string GetRowText(EventRow item)
		{
			var parts = new List<string>
			{
				$"Time: {item.Timestamp.ToLocalTime():HH:mm:ss.fff}",
				$"Type: {item.TypeName ?? string.Empty}",
				$"Channel: {item.Channel ?? string.Empty}",
				$"Category: {item.Category}",
				$"Publisher: {item.Publisher ?? string.Empty}",
				$"Subs: {item.SubscriberCount}",
				$"Depth: {item.PublishDepth}"
			};
			
			if (!string.IsNullOrEmpty(item.Data))
			{
				parts.Add($"Data: {item.Data}");
			}
			
			return string.Join("\t", parts);
		}

		private void DrawToolbar()
		{
			DrawActionToolbar();
			DrawFiltersToolbar();
		}

		private void DrawActionToolbar()
		{
			EditorGUILayout.BeginHorizontal(EditorStyles.toolbar);
			
			var eventCount = _history?.Count ?? 0;
			GUILayout.Label($"Events: {eventCount}", EditorStyles.miniLabel, GUILayout.Width(120));
			
			GUILayout.Space(10);
			
			if (GUILayout.Button("Clear", EditorStyles.toolbarButton, GUILayout.Width(60)))
			{
				_historyToolbar.Clear();
				Rebuild();
			}
			
			if (GUILayout.Button("Refresh", EditorStyles.toolbarButton, GUILayout.Width(70)))
			{
				_historyToolbar.Refresh();
			}
			
			if (GUILayout.Button("Active Subscriptions", EditorStyles.toolbarButton, GUILayout.Width(130)))
			{
				_historyToolbar.OpenSubscribers();
			}
			
			if (GUILayout.Button("Subscription History", EditorStyles.toolbarButton, GUILayout.Width(140)))
			{
				_historyToolbar.OpenSubscriptions();
			}
			
			GUILayout.FlexibleSpace();
			
			var autoRefresh = Settings.Monitoring.AutoRefresh;
			var newAutoRefresh = GUILayout.Toggle(autoRefresh, "Auto Refresh", EditorStyles.toolbarButton, GUILayout.Width(100));
			if (newAutoRefresh != autoRefresh)
			{
				Settings.Monitoring.AutoRefresh = newAutoRefresh;
			}
			
			var newSettingsExpanded = GUILayout.Toggle(_settingsExpanded, "⚙ Settings", EditorStyles.toolbarButton, GUILayout.Width(90));
			if (newSettingsExpanded != _settingsExpanded)
			{
				_settingsExpanded = newSettingsExpanded;
				Repaint();
			}
			
			EditorGUILayout.EndHorizontal();
		}

		private void DrawFiltersToolbar()
		{
			EditorGUILayout.BeginHorizontal(EditorStyles.toolbar);
			
			GUILayout.Label("Search:", GUILayout.Width(50));
			var newSearch = EditorGUILayout.TextField(_historyToolbar.Search ?? string.Empty, EditorStyles.toolbarTextField, GUILayout.Width(200));
			if (newSearch != _historyToolbar.Search)
			{
				_historyToolbar.Search = newSearch;
				if (!string.IsNullOrWhiteSpace(newSearch))
				{
					SearchHistoryManager.AddSearchTerm("History", newSearch);
				}
				RequestRefresh?.Invoke();
			}
			
			// Clear button - show when ANY filter is active
			bool hasActiveFilters = !string.IsNullOrEmpty(_historyToolbar.Search) ||
			                       _historyToolbar.CategoryFilter != EventCategoryFilter.All ||
			                       _historyToolbar.TimeRange != TimeRangeFilter.AllTime ||
			                       _historyToolbar.SubscriberFilter != SubscriberTypeFilter.All;
			
			if (hasActiveFilters)
			{
				if (GUILayout.Button("×", EditorStyles.toolbarButton, GUILayout.Width(20)))
				{
					// Clear all filters
					_historyToolbar.Search = string.Empty;
					_historyToolbar.CategoryFilter = EventCategoryFilter.All;
					_historyToolbar.TimeRange = TimeRangeFilter.AllTime;
					_historyToolbar.SubscriberFilter = SubscriberTypeFilter.All;
					
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
			
			GUILayout.Space(5);
			
			GUILayout.Label("Category:", GUILayout.Width(60));
			var categoryOptions = new[] { "All", "Domain", "System", "Framework" };
			var currentCategoryIndex = Array.IndexOf(categoryOptions, _historyToolbar.CategoryFilter.ToString());
			if (currentCategoryIndex < 0) currentCategoryIndex = 0;
			var newCategoryIndex = EditorGUILayout.Popup(currentCategoryIndex, categoryOptions, EditorStyles.toolbarPopup, GUILayout.Width(100));
			if (newCategoryIndex != currentCategoryIndex)
			{
				if (Enum.TryParse<EventCategoryFilter>(categoryOptions[newCategoryIndex], out var newFilter))
				{
					_historyToolbar.CategoryFilter = newFilter;
					RequestRefresh?.Invoke();
				}
			}
			
			GUILayout.Space(5);
			
			GUILayout.Label("Time Range:", GUILayout.Width(75));
			var timeRangeOptions = new[] { "AllTime", "Last5Minutes", "Last15Minutes", "Last1Hour", "Last6Hours", "Last24Hours" };
			var currentTimeRangeIndex = Array.IndexOf(timeRangeOptions, _historyToolbar.TimeRange.ToString());
			if (currentTimeRangeIndex < 0) currentTimeRangeIndex = 0;
			var newTimeRangeIndex = EditorGUILayout.Popup(currentTimeRangeIndex, timeRangeOptions, EditorStyles.toolbarPopup, GUILayout.Width(120));
			if (newTimeRangeIndex != currentTimeRangeIndex)
			{
				if (Enum.TryParse<TimeRangeFilter>(timeRangeOptions[newTimeRangeIndex], out var newTimeRange))
				{
					_historyToolbar.TimeRange = newTimeRange;
					RequestRefresh?.Invoke();
				}
			}
			
			GUILayout.FlexibleSpace(); // Push selection buttons to the right
			
			// Selection buttons (always created to maintain consistent control count)
			#if RULESYSTEM_PRESENT
			var hasSelectedRow = SelectedRow != null;
			var hasRuleHandler = hasSelectedRow && SelectedRow.HasRuleHandlerSubscriber;
			
			// Rule button - always created, disabled when no row selected or no rule handler
			using (new EditorGUI.DisabledScope(!hasSelectedRow || !hasRuleHandler))
			{
				if (GUILayout.Button("Rule", EditorStyles.toolbarButton, GUILayout.Width(60)))
				{
					if (hasSelectedRow && hasRuleHandler)
					{
						var ruleHandler = SelectedRow.GetFirstRuleHandler();
						if (!string.IsNullOrEmpty(ruleHandler))
						{
							EditorToolNavigation.NavigateToRuleExplorerByHandlerType(ruleHandler);
						}
					}
				}
			}
			
			#endif
			
			EditorGUILayout.EndHorizontal();
		}
	}
}
#endif
