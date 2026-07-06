#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using UnityEditor;

namespace FoundationPlatform.Editor.Utilities.Messaging
{
	public class EventBusSharedState
	{
		private static EventBusSharedState _instance;
		public static EventBusSharedState Instance
		{
			get
			{
				if (_instance == null)
				{
					_instance = new EventBusSharedState();
				}
				return _instance;
			}
		}

		[InitializeOnLoadMethod]
		private static void RegisterDomainReloadReset()
		{
			// Detach any handlers and drop the singleton before assemblies reload, so dead
			// EditorWindow handlers that never got OnDisable cannot stay attached and fire
			// into a disposed window. Mirrors IdentityComponent's static-registry reset.
			AssemblyReloadEvents.beforeAssemblyReload -= ResetOnReload;
			AssemblyReloadEvents.beforeAssemblyReload += ResetOnReload;
		}

		private static void ResetOnReload()
		{
			if (_instance != null)
			{
				_instance.ClearEventHandlers();
			}
			_instance = null;
		}

		private void ClearEventHandlers()
		{
			OnSearchTermChanged = null;
			OnEventTypeFilterChanged = null;
			OnSubscriberTypeFilterChanged = null;
			OnPublisherFilterChanged = null;
			OnNavigationContextChanged = null;
		}

		// Events for state changes
		public event Action<string> OnSearchTermChanged;
		public event Action<string> OnEventTypeFilterChanged;
		public event Action<string> OnSubscriberTypeFilterChanged;
		public event Action<string> OnPublisherFilterChanged;
		public event Action OnNavigationContextChanged;

		// Shared state properties
		private string _sharedSearchTerm = string.Empty;
		private string _eventTypeFilter = string.Empty;
		private string _subscriberTypeFilter = string.Empty;
		private string _publisherFilter = string.Empty;
		private NavigationContext _navigationContext;

		// Navigation history for back/forward
		private readonly Stack<NavigationContext> _navigationHistory = new Stack<NavigationContext>();
		private readonly Stack<NavigationContext> _forwardHistory = new Stack<NavigationContext>();

		public string SharedSearchTerm
		{
			get => _sharedSearchTerm;
			set
			{
				if (_sharedSearchTerm != value)
				{
					_sharedSearchTerm = value;
					OnSearchTermChanged?.Invoke(value);
				}
			}
		}

		public string EventTypeFilter
		{
			get => _eventTypeFilter;
			set
			{
				if (_eventTypeFilter != value)
				{
					_eventTypeFilter = value;
					OnEventTypeFilterChanged?.Invoke(value);
				}
			}
		}

		public string SubscriberTypeFilter
		{
			get => _subscriberTypeFilter;
			set
			{
				if (_subscriberTypeFilter != value)
				{
					_subscriberTypeFilter = value;
					OnSubscriberTypeFilterChanged?.Invoke(value);
				}
			}
		}

		public string PublisherFilter
		{
			get => _publisherFilter;
			set
			{
				if (_publisherFilter != value)
				{
					_publisherFilter = value;
					OnPublisherFilterChanged?.Invoke(value);
				}
			}
		}

		public NavigationContext NavigationContext
		{
			get => _navigationContext;
			set
			{
				if (_navigationContext != value)
				{
					// Push current context to history if it's valid
					if (_navigationContext != null && !string.IsNullOrEmpty(_navigationContext.SourceWindow))
					{
						_navigationHistory.Push(_navigationContext);
						// Clear forward history when navigating forward
						_forwardHistory.Clear();
					}
					
					_navigationContext = value;
					OnNavigationContextChanged?.Invoke();
				}
			}
		}

		public bool CanNavigateBack => _navigationHistory.Count > 0;
		public bool CanNavigateForward => _forwardHistory.Count > 0;

		public void NavigateBack()
		{
			if (_navigationHistory.Count > 0)
			{
				_forwardHistory.Push(_navigationContext);
				_navigationContext = _navigationHistory.Pop();
				OnNavigationContextChanged?.Invoke();
			}
		}

		public void NavigateForward()
		{
			if (_forwardHistory.Count > 0)
			{
				_navigationHistory.Push(_navigationContext);
				_navigationContext = _forwardHistory.Pop();
				OnNavigationContextChanged?.Invoke();
			}
		}

		public void ClearFilters()
		{
			SharedSearchTerm = string.Empty;
			EventTypeFilter = string.Empty;
			SubscriberTypeFilter = string.Empty;
			PublisherFilter = string.Empty;
		}

		public void ClearNavigationHistory()
		{
			_navigationHistory.Clear();
			_forwardHistory.Clear();
		}

		private EventBusSharedState()
		{
			_navigationContext = new NavigationContext
			{
				SourceWindow = string.Empty,
				TargetWindow = string.Empty,
				EventType = string.Empty,
				SubscriberType = string.Empty,
				Publisher = string.Empty,
				SearchTerm = string.Empty
			};
		}
	}

	public class NavigationContext
	{
		public string SourceWindow { get; set; }
		public string TargetWindow { get; set; }
		public string EventType { get; set; }
		public string SubscriberType { get; set; }
		public string Publisher { get; set; }
		public string SearchTerm { get; set; }
	}
}
#endif

