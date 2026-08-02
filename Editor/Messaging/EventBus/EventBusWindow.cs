#if UNITY_EDITOR
using AetherNexus.FoundationPlatform.Utilities.Menus;
using UnityEditor;
using UnityEngine;

namespace AetherNexus.FoundationPlatform.Editor.Utilities.Messaging
{
	public class EventBusWindow : EditorWindow
	{
		public enum Tab
		{
			PublishHistory,
			ActiveSubscriptions,
			SubscriptionHistory
		}

		private Tab _currentTab = Tab.PublishHistory;
		private EventPublishHistoryWindow _publishHistoryWindow;
		private ActiveSubscriptionsWindow _activeSubscriptionsWindow;
		private SubscriptionHistoryWindow _subscriptionHistoryWindow;

		[MenuItem(MenuPaths.WindowEventBus.EventBus, false, MenuPriorities.WindowEventBus)]
		public static void OpenWindow()
		{
			var window = GetWindow<EventBusWindow>("Event Bus");
			window.minSize = new Vector2(1024, 600);
			window.Show();
		}

		/// <summary>Opens the history tab with no filters applied.</summary>
		public static void OpenHistoryTab() => OpenHistoryTab(null, null, null);

		public static void OpenHistoryTab(string eventType, string publisher, string searchTerm)
		{
			var window = GetWindow<EventBusWindow>("Event Bus");
			window.minSize = new Vector2(1024, 600);
			window._currentTab = Tab.PublishHistory;
			window.Show();
			window.Focus();

			if (window._publishHistoryWindow != null)
			{
				if (!string.IsNullOrEmpty(eventType))
				{
					window._publishHistoryWindow.SetSearchAndFilter(eventType);
				}
				else if (!string.IsNullOrEmpty(publisher))
				{
					window._publishHistoryWindow.SetSearchTerm(publisher);
				}
				else if (!string.IsNullOrEmpty(searchTerm))
				{
					window._publishHistoryWindow.SetSearchTerm(searchTerm);
				}
			}
		}

		/// <summary>Opens the subscribers tab with no filters applied.</summary>
		public static void OpenSubscribersTab() => OpenSubscribersTab(null, null, null);

		public static void OpenSubscribersTab(string eventType, string subscriberType, string target)
		{
			var window = GetWindow<EventBusWindow>("Event Bus");
			window.minSize = new Vector2(1024, 600);
			window._currentTab = Tab.ActiveSubscriptions;
			window.Show();
			window.Focus();

			if (window._activeSubscriptionsWindow != null)
			{
				if (!string.IsNullOrEmpty(eventType))
				{
					window._activeSubscriptionsWindow.SetSearch(eventType);
				}
				else if (!string.IsNullOrEmpty(subscriberType))
				{
					window._activeSubscriptionsWindow.SetSearch(subscriberType);
				}
				else if (!string.IsNullOrEmpty(target))
				{
					window._activeSubscriptionsWindow.SetSearch(target);
				}
			}
		}

		/// <summary>Opens the subscriptions tab with no filters applied.</summary>
		public static void OpenSubscriptionsTab() => OpenSubscriptionsTab(null, null);

		public static void OpenSubscriptionsTab(string eventType, string subscriberType)
		{
			var window = GetWindow<EventBusWindow>("Event Bus");
			window.minSize = new Vector2(1024, 600);
			window._currentTab = Tab.SubscriptionHistory;
			window.Show();
			window.Focus();

			if (window._subscriptionHistoryWindow != null)
			{
				if (!string.IsNullOrEmpty(eventType))
				{
					window._subscriptionHistoryWindow.SetSearch(eventType);
				}
				else if (!string.IsNullOrEmpty(subscriberType))
				{
					window._subscriptionHistoryWindow.SetSearch(subscriberType);
				}
			}
		}

		private void OnEnable()
		{
			if (_publishHistoryWindow == null)
			{
				_publishHistoryWindow = CreateInstance<EventPublishHistoryWindow>();
			}
			_publishHistoryWindow.OnEnableParent();

			if (_activeSubscriptionsWindow == null)
			{
				_activeSubscriptionsWindow = CreateInstance<ActiveSubscriptionsWindow>();
			}
			_activeSubscriptionsWindow.OnEnableParent();

			if (_subscriptionHistoryWindow == null)
			{
				_subscriptionHistoryWindow = CreateInstance<SubscriptionHistoryWindow>();
			}
			_subscriptionHistoryWindow.OnEnableParent();
		}

		private void OnDisable()
		{
			if (_publishHistoryWindow != null)
			{
				_publishHistoryWindow.OnDisableParent();
				DestroyImmediate(_publishHistoryWindow);
			}

			if (_activeSubscriptionsWindow != null)
			{
				_activeSubscriptionsWindow.OnDisableParent();
				DestroyImmediate(_activeSubscriptionsWindow);
			}

			if (_subscriptionHistoryWindow != null)
			{
				_subscriptionHistoryWindow.OnDisableParent();
				DestroyImmediate(_subscriptionHistoryWindow);
			}
		}

		private void OnGUI()
		{
			// Sync positions so sub-windows display layout elements correctly
			if (_publishHistoryWindow != null)
			{
				_publishHistoryWindow.position = position;
			}
			if (_activeSubscriptionsWindow != null)
			{
				_activeSubscriptionsWindow.position = position;
			}
			if (_subscriptionHistoryWindow != null)
			{
				_subscriptionHistoryWindow.position = position;
			}

			GUILayout.BeginHorizontal(EditorStyles.toolbar);

			var newTab = (Tab)GUILayout.Toolbar(
				(int)_currentTab,
				new string[] { "Event Publish History", "Active Subscriptions", "Subscription History" },
				EditorStyles.toolbarButton
			);

			if (newTab != _currentTab)
			{
				_currentTab = newTab;
			}

			GUILayout.FlexibleSpace();

			if (GUILayout.Button("Generate Event Channel", EditorStyles.toolbarButton, GUILayout.Width(160)))
			{
				EventChannelGenerator.ShowWindow();
			}

			GUILayout.EndHorizontal();

			// Draw active tab
			switch (_currentTab)
			{
				case Tab.PublishHistory:
					if (_publishHistoryWindow != null)
					{
						_publishHistoryWindow.OnGUIParent();
					}
					break;
				case Tab.ActiveSubscriptions:
					if (_activeSubscriptionsWindow != null)
					{
						_activeSubscriptionsWindow.OnGUIParent();
					}
					break;
				case Tab.SubscriptionHistory:
					if (_subscriptionHistoryWindow != null)
					{
						_subscriptionHistoryWindow.OnGUIParent();
					}
					break;
			}
		}
	}
}
#endif
