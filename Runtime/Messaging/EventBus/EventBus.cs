using System;
using System.Collections.Generic;
using System.Linq;
using System.Diagnostics;
using System.Text.RegularExpressions;
using UnityEngine;

namespace AetherNexus.FoundationPlatform.Messaging
{
	
using AetherNexus.FoundationPlatform.DebugX;
	
public enum EventCategory
{
	Domain,    // DomainEvent - published by rules
	Framework  // BaseGameEvent - framework events and system/UI events
}

public enum LoggingLevel
{
	None,      // No logging
	Minimal,   // Basic event publish logging
	Detailed   // Full logging with publisher/subscriber details
}

public static class EventBus
{
	// Subscribers stored per event type: Dictionary<EventType, Dictionary<ChannelIdentity, List<PrioritizedCallback>>>
	private static readonly Dictionary<Type, Dictionary<Identity, List<PrioritizedCallback>>> _subscribers = new();

	// Optional monitoring configuration (editor/dev only behavior gates still apply internally)
	private static bool _monitoringEnabled = false;
	private static int _maxPublishDepth = 10;
	private static bool _stopOnDepthExceeded = true;
	private static bool _warnAtPercentage = true;
	private static int _warningThresholdPercent = 75;
	private static bool _enableEventHistory = false;
	private static bool _enableSubscriptionTracking = false;
	private static LoggingLevel _loggingLevel = LoggingLevel.None;

	// Optional debug signal emitter (registered by GameEngineCore if available)
	private static IEventDebugSignalEmitter _debugSignalEmitter;

	// Reusable buffers for safe iteration without ToArray allocation. Indexed by recursion depth.
	private static readonly List<List<PrioritizedCallback>> _invokeBuffers = new();
	private static int _invokeDepth;

	private static List<PrioritizedCallback> RentInvokeBuffer()
	{
		if (_invokeDepth >= _invokeBuffers.Count)
			_invokeBuffers.Add(new List<PrioritizedCallback>(16));
		return _invokeBuffers[_invokeDepth];
	}

	// Lightweight event publish stack for circular dependency detection (editor/dev only)
	#if UNITY_EDITOR || DEVELOPMENT_BUILD
	private static Stack<PublishRecord> _publishStack = new();
	private static List<EventHistoryEntry> _eventHistory = new();
	private static List<SubscriptionHistoryEntry> _subscriptionHistory = new();
	private static int _maxHistoryEntries = 100;
	private static int _maxSubscriptionHistoryEntries = 200;
	private static ulong _nextEventId = 1;
	private static readonly Stack<BaseGameEvent> _executionStack = new();
	#endif

	// Cache of per-event-type [BroadcastGlobal] attribute lookups to avoid reflection on the hot publish path.
	private static readonly Dictionary<Type, bool> _broadcastGlobalCache = new Dictionary<Type, bool>();

	private static bool HasBroadcastGlobal(Type eventType)
	{
		if (_broadcastGlobalCache.TryGetValue(eventType, out var cached))
			return cached;

		var result = Attribute.IsDefined(eventType, typeof(BroadcastGlobalAttribute));
		_broadcastGlobalCache[eventType] = result;
		return result;
	}

	// Cache of per-event-type reflected "Identity"/"EntityId" property lookups used by the debug signal path.
	// Resolved once per type (a property may be cached as null when absent) to avoid reflection on every publish.
	private struct DebugScopeProps
	{
		public System.Reflection.PropertyInfo IdentityProp;  // typeof(Identity), or null
		public System.Reflection.PropertyInfo EntityIdProp;  // typeof(int?), or null
	}
	private static readonly Dictionary<Type, DebugScopeProps> _debugScopePropsCache = new Dictionary<Type, DebugScopeProps>();

	private static DebugScopeProps GetDebugScopeProps(Type eventType)
	{
		if (_debugScopePropsCache.TryGetValue(eventType, out var cached))
			return cached;

		var props = new DebugScopeProps();
		var identityProperty = eventType.GetProperty("Identity");
		if (identityProperty != null && identityProperty.PropertyType == typeof(Identity))
			props.IdentityProp = identityProperty;
		else
		{
			var entityIdProperty = eventType.GetProperty("EntityId");
			if (entityIdProperty != null && entityIdProperty.PropertyType == typeof(int?))
				props.EntityIdProp = entityIdProperty;
		}

		_debugScopePropsCache[eventType] = props;
		return props;
	}

	private static int _nextTokenId = 1;
	private static int _domainPublishGateDepth;
	private static int _domainRestoreModeDepth;

	/// <summary>
	/// Opens a scoped gate that allows DomainEvent publication from commit paths.
	/// Callers must pair BeginDomainPublishGate/EndDomainPublishGate in try/finally.
	/// </summary>
	public static void BeginDomainPublishGate()
	{
		_domainPublishGateDepth++;
	}

	/// <summary>
	/// Closes a scoped gate that allows DomainEvent publication from commit paths.
	/// </summary>
	public static void EndDomainPublishGate()
	{
		if (_domainPublishGateDepth <= 0)
		{
			_domainPublishGateDepth = 0;
			return;
		}

		_domainPublishGateDepth--;
	}

	/// <summary>
	/// Opens a scoped override that allows DomainEvent publication for restoration flows.
	/// </summary>
	public static void BeginDomainRestoreMode()
	{
		_domainRestoreModeDepth++;
	}

	/// <summary>
	/// Closes a scoped restoration override for DomainEvent publication.
	/// </summary>
	public static void EndDomainRestoreMode()
	{
		if (_domainRestoreModeDepth <= 0)
		{
			_domainRestoreModeDepth = 0;
			return;
		}

		_domainRestoreModeDepth--;
	}

	private class PrioritizedCallback
	{
		public Delegate Callback;
		public int Priority;
		public object Target; // for debug info
		public int TokenId;   // 0 = delegate-based, >0 = token-based
	}

	// SubscriberDetail is runtime-available but usage is controlled by bool flags
	public struct SubscriberDetail
	{
		public string HandlerType;      // Type name of subscriber
		public string MethodName;       // Method name
		public int Priority;            // Subscription priority
		public bool Executed;           // Whether callback executed
		public string ErrorMessage;     // Error if execution failed (null if succeeded)
	}

	#if UNITY_EDITOR || DEVELOPMENT_BUILD
	private struct PublishRecord
	{
		public Type EventType;
		public DateTime Timestamp;
	}
	
	public struct EventHistoryEntry
	{
		public string EventTypeName;
		public DateTime Timestamp;
		public int SubscriberCount;
		public int PublishDepth;
		public string EventData;
		public string PublisherType;    // Type name of publisher
		public string PublisherMethod;  // Method name that published the event
		public EventCategory EventCategory; // Domain/System/Framework
		public Identity Channel;         // Channel the event was published to
		public List<SubscriberDetail> SubscriberDetails; // List of subscribers with execution info
		
		#if RULESYSTEM_PRESENT
		// Original caller information (when GameAction is involved)
		public string OriginalPublisherType;    // Original caller type (from GameActionPipeline)
		public string OriginalPublisherMethod;  // Original caller method (from GameActionPipeline)
		
		// Action information for framework events
		public string ActionTypeName;  // Underlying action type (for GameActionValidationEvent<T>, GameActionCommitted<T>)
		public string ActionData;      // Action data (for framework events)
		#endif
	}
	
	public struct SubscriptionHistoryEntry
	{
		public Type EventType;
		public string SubscriberType;
		public string MethodName;
		public SubscriptionAction Action; // Subscribe or Unsubscribe
		public DateTime Timestamp;
		public int Priority;
		public Identity Channel;
	}
	
	public enum SubscriptionAction
	{
		Subscribe,
		Unsubscribe
	}
	#endif

	private static string FormatTypeName(Type type)
	{
		if (!type.IsGenericType)
			return type.Name;

		var genericDefinition = type.GetGenericTypeDefinition();
		var typeArgs = type.GetGenericArguments();
		var baseName = genericDefinition.Name;

		// Remove the backtick and number from generic type name (e.g., "GameActionCommitted`1" -> "GameActionCommitted")
		var indexOfBacktick = baseName.IndexOf('`');
		if (indexOfBacktick > 0)
			baseName = baseName.Substring(0, indexOfBacktick);

		var typeArgNames = typeArgs.Select(FormatTypeName).ToArray();
		return $"{baseName}<{string.Join(", ", typeArgNames)}>";
	}

	#if UNITY_EDITOR
	/// <summary>
	/// Determine event category from event type hierarchy.
	/// </summary>
	private static EventCategory GetEventCategory(Type eventType)
	{
		if (eventType == null) return EventCategory.Framework;
		
		// Check inheritance hierarchy
		if (typeof(DomainEvent).IsAssignableFrom(eventType))
			return EventCategory.Domain;
		
		// All other events (including system/UI events) are Framework events
		return EventCategory.Framework;
	}

	#if RULESYSTEM_PRESENT
	/// <summary>
	/// Check if a type is a GameAction infrastructure class (not a user rule class).
	/// </summary>
	public static bool IsRuleSystemClass(Type type)
	{
		if (type == null) return false;
		
		// Check if type is in GameAction namespace
		if (type.Namespace != "GameEngineCore.Rules")
			return false;
		
		// List of GameAction infrastructure classes to filter out
		var infrastructureClassNames = new HashSet<string>
		{
			"GameActionPipeline",
			"RuleRegistry",
			"ValidationContext",
			"RuleDebug",
			"RulePriority",
			"RuleCategories",
			"RuleFeature",
			"IRuleAction",
			"GameActionCommitted",
			"GameActionValidationEvent",
			"GameActionException",
			"RulesBootstrap",
			"RuleSetSubscriptionHelper",
			"StandaloneRuleSet",
			"AsyncFlowVisualizerWindow"
		};
		
		// Check if it's an infrastructure class
		if (infrastructureClassNames.Contains(type.Name))
			return true;
		
		// Check if it's the base RuleHandler class itself (not user rules that inherit from it)
		// User rules inherit from RuleHandler but are not in GameAction namespace
		if (type.Name == "RuleSetBehaviour" && type.Namespace == "GameEngineCore.Rules")
		{
			// This is the base class, not a user rule
			return true;
		}
		
		// Check if it's in the Core namespace (infrastructure)
		return false;
	}
	#endif

	private struct CacheKey : IEquatable<CacheKey>
	{
		public readonly System.Reflection.MethodInfo Method;
		public readonly Type TargetType;

		public CacheKey(System.Reflection.MethodInfo method, Type targetType)
		{
			Method = method;
			TargetType = targetType;
		}

		public bool Equals(CacheKey other)
		{
			return Equals(Method, other.Method) && Equals(TargetType, other.TargetType);
		}

		public override bool Equals(object obj)
		{
			return obj is CacheKey other && Equals(other);
		}

		public override int GetHashCode()
		{
			unchecked
			{
				return ((Method != null ? Method.GetHashCode() : 0) * 397) ^ (TargetType != null ? TargetType.GetHashCode() : 0);
			}
		}
	}

	private static readonly Dictionary<CacheKey, string> _classNameCache = new Dictionary<CacheKey, string>();

	/// <summary>
	/// Extract actual class name from a delegate, handling compiler-generated closure classes.
	/// </summary>
	private static string GetActualClassName(Delegate callback, object target)
	{
		if (callback == null) return "Unknown";
		
		var method = callback.Method;
		if (method == null) return "Unknown";

		var targetType = target?.GetType();
		var key = new CacheKey(method, targetType);

		lock (_classNameCache)
		{
			if (_classNameCache.TryGetValue(key, out var cachedName))
			{
				return cachedName;
			}
		}

		var result = ResolveClassNameInternal(callback, target);

		lock (_classNameCache)
		{
			_classNameCache[key] = result;
		}

		return result;
	}

	private static string ResolveClassNameInternal(Delegate callback, object target)
	{
		var method = callback.Method;
		var declaringType = method.DeclaringType;
		if (declaringType == null) return "Static";
		
		var typeName = declaringType.Name;
		var methodName = method.Name;
		
		// Check if this is a compiler-generated closure class (starts with <>c__ or contains <>c__)
		if (typeName.StartsWith("<>c__", StringComparison.Ordinal) || typeName.Contains("<>c__"))
		{
			// Closure classes are nested classes, so get the parent/containing class
			var parentType = declaringType.DeclaringType;
			
			// This is a lambda/anonymous method in a closure
			// The target is the closure instance itself
			// Closure classes have fields that capture variables from the outer scope
			// Check fields first to get the actual runtime type (polymorphism)
			if (target != null)
			{
				try
				{
					var closureType = target.GetType();
					var fields = closureType.GetFields(System.Reflection.BindingFlags.Instance | 
					                                   System.Reflection.BindingFlags.Public | 
					                                   System.Reflection.BindingFlags.NonPublic);
					
					#if RULESYSTEM_PRESENT
					// GameAction-specific: Look for RuleHandler callbacks and extract user rule methods
					foreach (var field in fields)
					{
						try
						{
							var fieldValue = field.GetValue(target);
							if (fieldValue != null)
							{
								// Check if field is a captured Type object (e.g. handlerType) of a RuleSetBehaviour subclass
								if (fieldValue is Type sysType)
								{
									var currentType = sysType;
									while (currentType != null)
									{
										if (currentType.Name == "RuleSetBehaviour" && sysType.Name != "RuleSetBehaviour")
										{
											return sysType.Name;
										}
										currentType = currentType.BaseType;
									}
								}

								// Check if field is an instance of a RuleSetBehaviour subclass
								var instanceType = fieldValue.GetType();
								var checkType = instanceType;
								while (checkType != null)
								{
									if (checkType.Name == "RuleSetBehaviour" && instanceType.Name != "RuleSetBehaviour")
									{
										return instanceType.Name;
									}
									checkType = checkType.BaseType;
								}

								// Check if this is a delegate (Action<T>) that might be the original callback
								if (fieldValue is Delegate originalCallback)
								{
									var originalMethod = originalCallback.Method;
									if (originalMethod != null)
									{
										var originalDeclaringType = originalMethod.DeclaringType;
										if (originalDeclaringType != null)
										{
											// Check if this inherits from RuleSetBehaviour (using name check to bypass assembly loading issues)
											bool inheritsFromRuleSet = false;
											var currentRuleType = originalDeclaringType;
											while (currentRuleType != null)
											{
												if (currentRuleType.Name == "RuleSetBehaviour")
												{
													inheritsFromRuleSet = true;
													break;
												}
												currentRuleType = currentRuleType.BaseType;
											}

											if (inheritsFromRuleSet && originalDeclaringType.Name != "RuleSetBehaviour")
											{
												// Extract the actual rule method name
												var ruleMethodName = originalMethod.Name;
												
												// Check if the method has [Condition] or [Reaction] attribute (user rule method)
												var condType = Type.GetType("GameEngineCore.Rules.ConditionAttribute, GameEngineCore");
												var reactType = Type.GetType("GameEngineCore.Rules.ReactionAttribute, GameEngineCore");
												var hasRuleAttr = (condType != null && originalMethod.GetCustomAttributes(condType, false).Length > 0)
													|| (reactType != null && originalMethod.GetCustomAttributes(reactType, false).Length > 0);
												
												if (hasRuleAttr || !ruleMethodName.StartsWith("<>", StringComparison.Ordinal))
												{
													// Return the user rule class name
													return originalDeclaringType.Name;
												}
											}
										}
									}
								}
							}
						}
						catch
						{
							// Skip this field if we can't access it
							continue;
						}
					}
					#endif
					
					// First pass: Look for Component/MonoBehaviour types (highest priority)
					// Use GetType() to get actual runtime type for polymorphism
					foreach (var field in fields)
					{
						try
						{
							var fieldValue = field.GetValue(target);
							if (fieldValue != null)
							{
								var fieldType = fieldValue.GetType(); // Actual runtime type
								var fieldTypeName = fieldType.Name;
								
								// Prioritize Component/MonoBehaviour types
								if (!fieldTypeName.StartsWith("<>", StringComparison.Ordinal) && 
								    !fieldTypeName.Contains("<>c__") &&
								    (typeof(UnityEngine.Component).IsAssignableFrom(fieldType) ||
								     typeof(UnityEngine.MonoBehaviour).IsAssignableFrom(fieldType)))
								{
									#if RULESYSTEM_PRESENT
									// Skip GameAction infrastructure classes
									if (!IsRuleSystemClass(fieldType))
									{
										return fieldTypeName;
									}
									#else
									return fieldTypeName;
									#endif
								}
							}
						}
						catch
						{
							// Skip this field if we can't access it
							continue;
						}
					}
					
					// Second pass: Look for any non-compiler-generated class type
					// Check if field type is assignable to parent type (polymorphism)
					foreach (var field in fields)
					{
						try
						{
							var fieldValue = field.GetValue(target);
							if (fieldValue != null)
							{
								var fieldType = fieldValue.GetType(); // Actual runtime type
								var fieldTypeName = fieldType.Name;
								
								// If the field is not a compiler-generated type, it might be the actual class
								if (!fieldTypeName.StartsWith("<>", StringComparison.Ordinal) && 
								    !fieldTypeName.Contains("<>c__") &&
								    fieldType.IsClass && 
								    !fieldType.IsAbstract &&
								    !fieldType.IsPrimitive &&
								    fieldType != typeof(string) &&
								    fieldType != typeof(object) &&
								    !fieldType.IsValueType)
								{
									#if RULESYSTEM_PRESENT
									// Skip GameAction infrastructure classes
									if (IsRuleSystemClass(fieldType))
										continue;
									#endif
									
									// If we have a parent type, prefer types that are assignable to it (polymorphism)
									if (parentType != null && parentType.IsAssignableFrom(fieldType))
									{
										return fieldTypeName;
									}
									// Otherwise, use any non-compiler-generated class
									if (parentType == null)
									{
										return fieldTypeName;
									}
								}
							}
						}
						catch
						{
							// Skip this field if we can't access it
							continue;
						}
					}
					
					// Third pass: Try to get the actual class from the method's module/namespace
					try
					{
						if (parentType != null && (parentType.Name == "RuleSetSubscriptionHelper" || parentType.Name == "StandaloneRuleSet"))
						{
							return "Anonymous";
						}

						var module = declaringType.Module;
						if (module != null)
						{
							// Look for types in the same module that might be the actual class
							// This is a heuristic - we'll look for types that aren't compiler-generated
							var types = module.GetTypes();
							foreach (var type in types)
							{
								var candidateTypeName = type.Name;
								if (!candidateTypeName.StartsWith("<>", StringComparison.Ordinal) && 
								    !candidateTypeName.Contains("<>c__") &&
								    type.IsClass && 
								    !type.IsAbstract &&
								    type.Namespace == declaringType.Namespace)
								{
									// Check if this type has methods that might be related
									var methods = type.GetMethods(System.Reflection.BindingFlags.Instance | 
									                              System.Reflection.BindingFlags.Static | 
									                              System.Reflection.BindingFlags.Public | 
									                              System.Reflection.BindingFlags.NonPublic);
									foreach (var m in methods)
									{
										if (m.Name == "Subscribe" || m.Name.Contains("Subscribe") || 
										    m.Name == "OnEnable" || m.Name == "Start")
										{
											return candidateTypeName;
										}
									}
								}
							}
						}
					}
					catch
					{
						// Fall through to default
					}
				}
				catch
				{
					// Fall through to default
				}
			}
			
			// Fallback: Use parent type if available, otherwise "Anonymous"
			if (parentType != null)
			{
				return parentType.Name;
			}
			
			// Fallback: show "Anonymous" for compiler-generated closures where we can't determine the class
			return "Anonymous";
		}
		
		// Not a compiler-generated class, use the actual runtime type if available (polymorphism)
		if (target != null)
		{
			var targetType = target.GetType();
			// If target's type is assignable to declaring type, use the actual runtime type
			if (declaringType.IsAssignableFrom(targetType))
			{
				#if RULESYSTEM_PRESENT
				// Skip GameAction infrastructure classes
				if (!IsRuleSystemClass(targetType))
				{
					return targetType.Name;
				}
				#else
				return targetType.Name;
				#endif
			}
		}
		
		#if RULESYSTEM_PRESENT
		// Skip GameAction infrastructure classes
		if (IsRuleSystemClass(declaringType))
		{
			return "Anonymous";
		}
		#endif
		
		// Fallback to declaring type name
		return declaringType.Name;
	}

	#if RULESYSTEM_PRESENT
	/// <summary>
	/// Extract the actual rule method name from a RuleHandler callback.
	/// </summary>
	private static string ExtractRuleMethodName(Delegate callback)
	{
		if (callback == null) return "Unknown";
		
		var method = callback.Method;
		if (method == null) return "Unknown";
		
		var methodName = method.Name;
		
		// If it's a compiler-generated method (like b__0), try to find the original callback
		if (methodName.Contains("b__", StringComparison.Ordinal) || methodName.StartsWith("<>", StringComparison.Ordinal))
		{
			// Try to find the original callback in closure fields
			if (callback.Target != null)
			{
				try
				{
					var closureType = callback.Target.GetType();
					var fields = closureType.GetFields(System.Reflection.BindingFlags.Instance | 
					                                   System.Reflection.BindingFlags.Public | 
					                                   System.Reflection.BindingFlags.NonPublic);
					
					foreach (var field in fields)
					{
						try
						{
							var fieldValue = field.GetValue(callback.Target);
							if (fieldValue is Delegate originalCallback)
							{
								var originalMethod = originalCallback.Method;
								if (originalMethod != null)
								{
									var originalMethodName = originalMethod.Name;
									
									// Check if this is a user rule method (has [Condition] or [Reaction] attribute)
									var condAttr = Type.GetType("GameEngineCore.Rules.ConditionAttribute, GameEngineCore");
									var reactAttr = Type.GetType("GameEngineCore.Rules.ReactionAttribute, GameEngineCore");
									if (condAttr != null && reactAttr != null)
									{
										var hasRuleAttr = originalMethod.GetCustomAttributes(condAttr, false).Length > 0
											|| originalMethod.GetCustomAttributes(reactAttr, false).Length > 0;
										if (hasRuleAttr && !originalMethodName.StartsWith("<>", StringComparison.Ordinal))
										{
											return originalMethodName;
										}
									}
									
									// If not compiler-generated, use it
									if (!originalMethodName.StartsWith("<>", StringComparison.Ordinal) && 
									    !originalMethodName.Contains("b__"))
									{
										return originalMethodName;
									}
								}
							}
						}
						catch
						{
							// Skip this field
							continue;
						}
					}
				}
				catch
				{
					// Fall through
				}
			}
		}
		
		// Check if the method has [Condition] or [Reaction] attribute
		var condType = Type.GetType("GameEngineCore.Rules.ConditionAttribute, GameEngineCore");
		var reactType = Type.GetType("GameEngineCore.Rules.ReactionAttribute, GameEngineCore");
		if (condType != null && reactType != null)
		{
			var hasRuleAttr = method.GetCustomAttributes(condType, false).Length > 0
				|| method.GetCustomAttributes(reactType, false).Length > 0;
			if (hasRuleAttr && !methodName.StartsWith("<>", StringComparison.Ordinal))
			{
				return methodName;
			}
		}
		
		return methodName;
	}
	#endif

	#if UNITY_EDITOR || DEVELOPMENT_BUILD
	private static void InitProvenance<T>(T evt, string originalPublisherType, string originalPublisherMethod, string member, string file, int line) where T : BaseGameEvent
	{
		if (evt == null) return;
		
		ulong parentId = 0;
		if (_executionStack.Count > 0)
		{
			var parent = _executionStack.Peek();
			if (parent != null && parent.Provenance != null)
			{
				parentId = parent.Provenance.EventId;
			}
		}

		string publisherType = typeof(T).Name;
		string publisherMethod = member;

		if (!string.IsNullOrEmpty(originalPublisherType))
		{
			publisherType = originalPublisherType;
		}
		if (!string.IsNullOrEmpty(originalPublisherMethod))
		{
			publisherMethod = originalPublisherMethod;
		}

		string cleanFile = file;
		if (!string.IsNullOrEmpty(file))
		{
			int assetsIndex = file.IndexOf("Assets", StringComparison.OrdinalIgnoreCase);
			if (assetsIndex >= 0)
			{
				cleanFile = file.Substring(assetsIndex).Replace('\\', '/');
			}
		}

		int frame = 0;
		try
		{
			frame = UnityEngine.Time.frameCount;
		}
		catch
		{
			// Safe fallback if called on background thread
		}

		evt.Provenance = new EventProvenance
		{
			EventId = _nextEventId++,
			ParentEventId = parentId,
			PublisherType = publisherType,
			PublisherMethod = publisherMethod,
			File = cleanFile,
			Line = line,
			Frame = frame
		};
	}
	#endif

	/// <summary>
	/// Extract publisher information from stack trace (editor-only).
	/// </summary>
	private static void GetPublisherInfo(out string publisherType, out string publisherMethod)
	{
		publisherType = "Unknown";
		publisherMethod = "Unknown";
		
		try
		{
			var stackTrace = new StackTrace(skipFrames: 2, fNeedFileInfo: false);
			var frames = stackTrace.GetFrames();
			if (frames == null || frames.Length == 0) return;
			
			// Track best candidate frame (prefer non-async state machines, or async state machines with valid parent)
			string bestType = null;
			string bestMethod = null;
			bool foundNonAsyncFrame = false;
			
			// Walk through all frames to find the best candidate
			foreach (var frame in frames)
			{
				var method = frame.GetMethod();
				if (method == null) continue;
				
				var declaringType = method.DeclaringType;
				if (declaringType == null) continue;
				
				// Skip EventBus internal methods
				if (declaringType == typeof(EventBus)) continue;
				
				// Skip UniTask infrastructure classes
				if (declaringType.Namespace != null && 
				    (declaringType.Namespace.StartsWith("Cysharp.Threading.Tasks", StringComparison.Ordinal) ||
				     declaringType.Namespace == "Cysharp.Threading.Tasks.CompilerServices"))
				{
					continue;
				}
				
				#if RULESYSTEM_PRESENT
				// Skip GameAction infrastructure classes
				if (IsRuleSystemClass(declaringType)) continue;
				#endif
				
				var typeName = declaringType.Name;
				var methodName = method.Name;
				
				// Skip DisplayClass closure frames (compiler-generated closures)
				if (IsDisplayClassClosure(typeName))
				{
					// Continue walking the stack to find a better frame
					continue;
				}
				
				// Check if this is an async state machine
				if (IsAsyncStateMachineType(typeName))
				{
					// Extract method name from state machine type if method is MoveNext
					string extractedMethodName = null;
					if (methodName == "MoveNext")
					{
						extractedMethodName = ExtractAsyncMethodName(typeName);
						if (!string.IsNullOrEmpty(extractedMethodName))
						{
							methodName = extractedMethodName;
						}
					}
					
					// Get the parent/containing class (async state machines are nested classes)
					var parentType = declaringType.DeclaringType;
					if (parentType != null)
					{
						#if RULESYSTEM_PRESENT
						// Skip if parent is a GameAction class - continue walking to find actual caller
						if (IsRuleSystemClass(parentType))
						{
							// Continue walking the stack to find a better frame
							continue;
						}
						#endif
						
						// This async state machine has a valid parent - use it if we haven't found a non-async frame
						if (!foundNonAsyncFrame)
						{
							bestType = parentType.Name;
							bestMethod = methodName;
						}
					}
					else if (!string.IsNullOrEmpty(extractedMethodName))
					{
						// Parent type is null but we extracted the method name
						// Use as fallback only if we haven't found anything better
						if (bestType == null)
						{
							bestType = extractedMethodName;
							bestMethod = extractedMethodName;
						}
					}
					else
					{
						// No parent and couldn't extract method name - use as fallback only
						if (bestType == null)
						{
							bestType = typeName;
							bestMethod = methodName;
						}
					}
				}
				else
				{
					// This is a non-async state machine frame - prefer this over async state machines
					foundNonAsyncFrame = true;
					bestType = typeName;
					bestMethod = methodName;
					// Found a good frame, we can stop here
					break;
				}
			}
			
			// Use the best candidate we found
			if (bestType != null && bestMethod != null)
			{
				publisherType = bestType;
				publisherMethod = bestMethod;
			}
		}
		catch
		{
			// Fallback to unknown if stack trace fails
		}
	}
	
	/// <summary>
	/// Check if a type name is a DisplayClass closure (compiler-generated closure class).
	/// Pattern: &lt;&gt;c__DisplayClass&lt;number&gt; or contains DisplayClass
	/// </summary>
	private static bool IsDisplayClassClosure(string typeName)
	{
		if (string.IsNullOrEmpty(typeName)) return false;
		
		// Match pattern: <>c__DisplayClass<number> or contains DisplayClass
		return typeName.Contains("DisplayClass", StringComparison.Ordinal) ||
		       Regex.IsMatch(typeName, @"^<>c__DisplayClass\d+");
	}
	
	/// <summary>
	/// Check if a type name matches the async state machine pattern.
	/// Pattern: &lt;MethodName&gt;d__&lt;number&gt; or &lt;MethodName&gt;d__&lt;number&gt;`&lt;genericCount&gt;
	/// </summary>
	private static bool IsAsyncStateMachineType(string typeName)
	{
		if (string.IsNullOrEmpty(typeName)) return false;
		
		// Match pattern: <MethodName>d__<number> or <MethodName>d__<number>`<genericCount>
		return Regex.IsMatch(typeName, @"^<[^>]+>d__\d+(`\d+)?");
	}
	
	/// <summary>
	/// Extract the original method name from an async state machine type name.
	/// Pattern: &lt;MethodName&gt;d__&lt;number&gt; or &lt;MethodName&gt;d__&lt;number&gt;`&lt;genericCount&gt;
	/// </summary>
	private static string ExtractAsyncMethodName(string typeName)
	{
		if (string.IsNullOrEmpty(typeName)) return null;
		
		// Match pattern: <MethodName>d__<number> or <MethodName>d__<number>`<genericCount>
		var match = Regex.Match(typeName, @"^<([^>]+)>d__\d+(`\d+)?");
		if (match.Success && match.Groups.Count > 1)
		{
			return match.Groups[1].Value;
		}
		
		return null;
	}
	#endif

	/// <summary>
	/// Subscribe to an event with optional priority and channel identity.
	/// When channel is not valid, uses Identity.Global.
	/// </summary>
	public static void Subscribe<T>(Action<T> callback, int priority = 0, Identity channel = default) where T : BaseGameEvent
	{
		SubscribeInternal(callback, priority, channel, false, out _);
	}

	/// <summary>
	/// Subscribe and return a token for reliable unsubscribe via token.Dispose().
	/// Use when the callback is a lambda/closure where delegate equality would fail.
	/// IMPORTANT: token-based subscriptions can ONLY be removed via the returned token
	/// (Unsubscribe(SubscriptionToken)). Unsubscribe(Action&lt;T&gt;) and UnsubscribeAll
	/// deliberately ignore token-based entries (TokenId &gt; 0), so if the token is lost the
	/// subscriber leaks until Clear() is called. Retain the token for the subscriber's lifetime.
	/// </summary>
	public static SubscriptionToken SubscribeWithToken<T>(Action<T> callback, int priority = 0, Identity channel = default) where T : BaseGameEvent
	{
		SubscribeInternal(callback, priority, channel, true, out var token);
		return token;
	}

	private static void SubscribeInternal<T>(Action<T> callback, int priority, Identity channel, bool withToken, out SubscriptionToken token) where T : BaseGameEvent
	{
		token = default;
		if (callback == null)
		{
			DebugX.Logger(LogChannels.Framework).Warning("[EventBus] Attempted to subscribe with null callback");
			return;
		}

		if (!channel.IsValid) channel = Identity.Global;

		var eventType = typeof(T);
		if (!_subscribers.TryGetValue(eventType, out var channels))
		{
			channels = new Dictionary<Identity, List<PrioritizedCallback>>();
			_subscribers[eventType] = channels;
		}

		if (!channels.TryGetValue(channel, out var list))
		{
			list = new List<PrioritizedCallback>();
			channels[channel] = list;
		}

		var tokenId = 0;
		if (withToken)
		{
			tokenId = _nextTokenId++;
			token = new SubscriptionToken(eventType, channel, tokenId);
		}
		var wrapper = new PrioritizedCallback
		{
			Callback = callback,
			Priority = priority,
			Target = callback.Target,
			TokenId = tokenId
		};

		// Insert sorted by priority (higher first).
		// Tie-break: equal-priority subscribers preserve insertion order (the new
		// subscriber is placed after all existing entries of the same priority).
		int insertIndex = list.FindIndex(x => x.Priority < priority);
		if (insertIndex == -1)
			list.Add(wrapper);
		else
			list.Insert(insertIndex, wrapper);

		#if UNITY_EDITOR
		// Track subscription lifecycle
		if (_enableSubscriptionTracking)
		{
			var subscriberType = GetActualClassName(callback, callback.Target);
			#if RULESYSTEM_PRESENT
			var methodName = ExtractRuleMethodName(callback);
			// If we couldn't extract a rule method name, fall back to the original method name
			if (methodName == "Unknown" || methodName.Contains("b__", StringComparison.Ordinal))
			{
				methodName = callback.Method?.Name ?? "Unknown";
				// For compiler-generated methods (like b__0 or <Subscribe>b__0), replace with "Subscribe"
				if (methodName.Contains("b__", StringComparison.Ordinal))
				{
					methodName = "Subscribe";
				}
			}
			#else
			var methodName = callback.Method?.Name ?? "Unknown";
			
			// For compiler-generated methods (like b__0 or <Subscribe>b__0), replace with "Subscribe"
			if (methodName.Contains("b__", StringComparison.Ordinal))
			{
				methodName = "Subscribe";
			}
			#endif
			
			var entry = new SubscriptionHistoryEntry
			{
				EventType = eventType,
				SubscriberType = subscriberType,
				MethodName = methodName,
				Action = SubscriptionAction.Subscribe,
				Timestamp = DateTime.Now,
				Priority = priority,
				Channel = channel
			};
			
			_subscriptionHistory.Add(entry);
			
			// Limit history size
			if (_subscriptionHistory.Count > _maxSubscriptionHistoryEntries)
			{
				_subscriptionHistory.RemoveAt(0);
			}
			
		// Log subscription if enabled
		if (_loggingLevel >= LoggingLevel.Detailed)
		{
			DebugX.Logger(LogChannels.Framework).Info("[EventBus] Subscribed: {SubscriberType}.{MethodName} -> {EventType} (Priority: {Priority}, Channel: {Channel})", subscriberType, methodName, FormatTypeName(eventType), priority, channel);
		}
		}
		#endif
	}

	/// <summary>
	/// Unsubscribe from an event. With no channel, removes from global channel only.
	/// </summary>
	public static void Unsubscribe<T>(Action<T> callback, Identity channel = default) where T : BaseGameEvent
	{
		if (callback == null) return;
		if (!channel.IsValid) channel = Identity.Global;

		var eventType = typeof(T);
		if (_subscribers.TryGetValue(eventType, out var channels) && channels.TryGetValue(channel, out var list))
		{
			list.RemoveAll(x => x.TokenId == 0 && x.Callback != null && x.Callback.Equals(callback));
			if (list.Count == 0)
				channels.Remove(channel);
			if (channels.Count == 0)
				_subscribers.Remove(eventType);
		}
	}

	/// <summary>
	/// Unsubscribe from an event across all channels (explicit opt-in).
	/// </summary>
	public static void UnsubscribeAll<T>(Action<T> callback) where T : BaseGameEvent
	{
		if (callback == null) return;

		var eventType = typeof(T);
		if (_subscribers.TryGetValue(eventType, out var channels))
		{
			foreach (var channelList in channels.Values)
			{
				channelList.RemoveAll(x => x.TokenId == 0 && x.Callback != null && x.Callback.Equals(callback));
			}
			var emptyChannels = channels.Where(kvp => kvp.Value.Count == 0).Select(kvp => kvp.Key).ToList();
			foreach (var empty in emptyChannels)
				channels.Remove(empty);
			if (channels.Count == 0)
				_subscribers.Remove(eventType);
		}
	}

	/// <summary>
	/// Unsubscribe using a subscription token (from SubscribeWithToken).
	/// </summary>
	public static void Unsubscribe(SubscriptionToken token)
	{
		if (!token.IsValid) return;

		if (_subscribers.TryGetValue(token.EventType, out var channels) && channels.TryGetValue(token.Channel, out var list))
		{
			list.RemoveAll(x => x.TokenId == token.Id);
			if (list.Count == 0)
				channels.Remove(token.Channel);
			if (channels.Count == 0)
				_subscribers.Remove(token.EventType);
		}
	}

	#if UNITY_EDITOR || DEVELOPMENT_BUILD
	/// <summary>
	/// Serialize event properties marked with [EventData] attribute.
	/// </summary>
	private static string SerializeEventData<T>(T evt) where T : BaseGameEvent
	{
		if (evt == null) return null;
		
		try
		{
			var eventType = typeof(T);
			var properties = eventType.GetProperties(System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.Public)
				.Where(p => p.CanRead && p.GetIndexParameters().Length == 0)
				.Where(p => Attribute.IsDefined(p, typeof(EventDataAttribute)))
				.ToList();
			
			if (properties.Count == 0) return null;
			
			var parts = new List<string>();
			foreach (var prop in properties)
			{
				try
				{
					var value = prop.GetValue(evt);
					var valueStr = FormatPropertyValue(value, prop.PropertyType);
					parts.Add($"{prop.Name}: {valueStr}");
				}
				catch
				{
					// Skip properties that can't be read
					continue;
				}
			}
			
			return parts.Count > 0 ? string.Join(", ", parts) : null;
		}
		catch
		{
			return null;
		}
	}
	
	/// <summary>
	/// Format a property value for display, handling complex types by finding their "Name" property.
	/// </summary>
	private static string FormatPropertyValue(object value, Type propertyType)
	{
		if (value == null) return "null";
		
		// Handle primitive types and strings
		if (propertyType.IsPrimitive || propertyType == typeof(string) || propertyType == typeof(decimal))
		{
			return value.ToString();
		}
		
		// Handle enums
		if (propertyType.IsEnum)
		{
			return value.ToString();
		}
		
		// Handle nullable types
		if (propertyType.IsGenericType && propertyType.GetGenericTypeDefinition() == typeof(Nullable<>))
		{
			return value.ToString();
		}
		
		// Handle collections and dictionaries - just show the type name
		if (typeof(System.Collections.ICollection).IsAssignableFrom(propertyType) || 
		    typeof(System.Collections.IDictionary).IsAssignableFrom(propertyType))
		{
			return propertyType.Name;
		}
		
		// For complex types, try to find a "Name" property
		var namePropertyNames = new[] { "CurrencyName", "Name", "Id", "DisplayName" };
		foreach (var namePropName in namePropertyNames)
		{
			try
			{
				var nameProp = value.GetType().GetProperty(namePropName, System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.Public);
				if (nameProp != null && nameProp.CanRead)
				{
					var nameValue = nameProp.GetValue(value);
					if (nameValue != null)
					{
						return nameValue.ToString();
					}
				}
			}
			catch
			{
				// Continue to next property name
				continue;
			}
		}
		
		// Fall back to type name
		return propertyType.Name;
	}
	#endif

	/// <summary>
	/// Publish an event to subscribers of the matching channel.
	/// If no channel is specified, uses evt.Identity.
	/// Events with [BroadcastGlobal] are also delivered to subscribers on Identity.Global.
	/// </summary>
	public static void Publish<T>(
		T evt,
		Identity channel = default,
		[System.Runtime.CompilerServices.CallerMemberName] string member = "",
		[System.Runtime.CompilerServices.CallerFilePath] string file = "",
		[System.Runtime.CompilerServices.CallerLineNumber] int line = 0) where T : BaseGameEvent
	{
		if (evt == null)
		{
			DebugX.Logger(LogChannels.Framework).Error("[EventBus] Publish called with null event of type {EventType}", typeof(T).Name);
			return;
		}
		if (!channel.IsValid) channel = evt.Identity.IsValid ? evt.Identity : Identity.Global;
		#if UNITY_EDITOR || DEVELOPMENT_BUILD
		InitProvenance(evt, null, null, member, file, line);
		#endif
		PublishInternal(evt, null, null, channel);
	}

	#if RULESYSTEM_PRESENT
	/// <summary>
	/// Publish an event with original caller information (for GameAction framework events).
	/// Events with [BroadcastGlobal] are also delivered to subscribers on Identity.Global.
	/// </summary>
	public static void Publish<T>(
		T evt,
		string originalPublisherType,
		string originalPublisherMethod,
		Identity channel = default,
		[System.Runtime.CompilerServices.CallerMemberName] string member = "",
		[System.Runtime.CompilerServices.CallerFilePath] string file = "",
		[System.Runtime.CompilerServices.CallerLineNumber] int line = 0) where T : BaseGameEvent
	{
		if (evt == null)
		{
			DebugX.Logger(LogChannels.Framework).Error("[EventBus] Publish called with null event of type {EventType}", typeof(T).Name);
			return;
		}
		if (!channel.IsValid) channel = evt.Identity.IsValid ? evt.Identity : Identity.Global;
		#if UNITY_EDITOR || DEVELOPMENT_BUILD
		InitProvenance(evt, originalPublisherType, originalPublisherMethod, member, file, line);
		#endif
		PublishInternal(evt, originalPublisherType, originalPublisherMethod, channel);
	}
	#endif

	/// <summary>
	/// Internal publish implementation.
	/// </summary>
	private static void PublishInternal<T>(T evt, string originalPublisherType, string originalPublisherMethod, Identity channel) where T : BaseGameEvent
	{
		var eventType = typeof(T);

		#if UNITY_EDITOR || DEVELOPMENT_BUILD
		if (evt is DomainEvent)
		{
			var isRestoring = _domainRestoreModeDepth > 0;
			if (_domainPublishGateDepth <= 0 && !isRestoring)
			{
				var msg = $"[EventBus] DomainEvent '{eventType.Name}' published outside Commit/Restoring. Domain events must be emitted from commit reactions or restoration mode.";
				DebugX.Logger(LogChannels.Framework).Error("{Message}", msg);
				throw new InvalidOperationException(msg);
			}
		}
		#endif

		#if UNITY_EDITOR || DEVELOPMENT_BUILD
		if (_monitoringEnabled)
		{
			// Circular dependency detection / depth control
		if (_publishStack.Count >= _maxPublishDepth)
		{
			var chain = string.Join(" -> ", _publishStack.Select(r => FormatTypeName(r.EventType)));
			DebugX.Logger(LogChannels.Framework).Error("[EventBus] Circular dependency or depth limit exceeded!\nEvent Chain: {Chain} -> {EventType}\nDepth: {CurrentDepth}/{MaxDepth}", chain, FormatTypeName(eventType), _publishStack.Count, _maxPublishDepth);
			if (_stopOnDepthExceeded)
			{
				DebugX.Logger(LogChannels.Framework).Error("[EventBus] Stopping event '{EventType}' due to depth limit.", FormatTypeName(eventType));
				return;
			}
		}

		if (_warnAtPercentage && _maxPublishDepth > 0)
		{
			var threshold = (_maxPublishDepth * _warningThresholdPercent) / 100;
			if (_publishStack.Count >= threshold && _publishStack.Count < _maxPublishDepth)
			{
				var chain = string.Join(" -> ", _publishStack.Select(r => FormatTypeName(r.EventType)));
				DebugX.Logger(LogChannels.Framework).Warning("[EventBus] Approaching depth limit ({CurrentDepth}/{MaxDepth})\nChain: {Chain} -> {EventType}", _publishStack.Count, _maxPublishDepth, chain, FormatTypeName(eventType));
			}
		}

			_publishStack.Push(new PublishRecord { EventType = eventType, Timestamp = DateTime.Now });
		}
		#endif

		try
		{
			#if UNITY_EDITOR
			// Capture publisher information (editor-only)
			string publisherType = "Unknown";
			string publisherMethod = "Unknown";
			if (_monitoringEnabled && _enableEventHistory)
			{
				#if UNITY_EDITOR || DEVELOPMENT_BUILD
				if (evt.Provenance != null)
				{
					publisherType = evt.Provenance.PublisherType;
					publisherMethod = evt.Provenance.PublisherMethod;
				}
				else
				#endif
				if (!string.IsNullOrEmpty(originalPublisherType) && !string.IsNullOrEmpty(originalPublisherMethod))
				{
					publisherType = originalPublisherType;
					publisherMethod = originalPublisherMethod;
				}
				else
				{
					GetPublisherInfo(out publisherType, out publisherMethod);
				}
			}
			#endif

			if (_subscribers.TryGetValue(eventType, out var channels))
			{
				bool hasSpecific = channels.TryGetValue(channel, out var specificList);
				
				// [BroadcastGlobal] events also deliver to Identity.Global subscribers
				bool shouldDeliverToGlobal = HasBroadcastGlobal(eventType)
					&& channel.IsValid && channel != Identity.Global;
				List<PrioritizedCallback> globalList = null;
				bool hasGlobal = shouldDeliverToGlobal && channels.TryGetValue(Identity.Global, out globalList);

				if (hasSpecific || hasGlobal)
				{
					#if UNITY_EDITOR || DEVELOPMENT_BUILD
					if (_monitoringEnabled && (_enableEventHistory || _loggingLevel >= LoggingLevel.Minimal))
					{
						var subscriberDetails = new List<SubscriberDetail>();
						var eventCategory = GetEventCategory(eventType);
						var subscriberCount = (hasSpecific ? specificList.Count : 0) + (hasGlobal ? globalList.Count : 0);

						#if RULESYSTEM_PRESENT
						// Extract action information for framework events
						string actionTypeName = null;
						string actionData = null;
						if (eventCategory == EventCategory.Framework)
						{
							if (eventType.IsGenericType)
							{
								var genericDefinition = eventType.GetGenericTypeDefinition();
								var genericDefName = genericDefinition.Name;
								var indexOfBacktick = genericDefName.IndexOf('`');
								if (indexOfBacktick > 0) genericDefName = genericDefName.Substring(0, indexOfBacktick);
								
								if (genericDefName == "GameActionValidationEvent" || genericDefName == "GameActionCommitted")
								{
									var typeArgs = eventType.GetGenericArguments();
									if (typeArgs.Length > 0)
									{
										actionTypeName = FormatTypeName(typeArgs[0]);
										try
										{
											var actionProperty = eventType.GetProperty("Action");
											if (actionProperty != null)
											{
												var actionValue = actionProperty.GetValue(evt);
												if (actionValue != null) actionData = actionValue.ToString();
											}
										}
										catch { actionData = evt?.ToString() ?? "null"; }
									}
								}
							}
						}
						#endif

						if (hasSpecific) InvokeSubscribersTracking(specificList, evt, eventType, channel, publisherType, publisherMethod, eventCategory, subscriberDetails);
						if (hasGlobal) InvokeSubscribersTracking(globalList, evt, eventType, Identity.Global, publisherType, publisherMethod, eventCategory, subscriberDetails);

						if (_enableEventHistory)
						{
							var historyEntry = new EventHistoryEntry
							{
								EventTypeName = FormatTypeName(eventType),
								Timestamp = DateTime.Now,
								SubscriberCount = subscriberCount,
								PublishDepth = _publishStack.Count,
								EventData = SerializeEventData(evt) ?? evt?.ToString() ?? "null",
								PublisherType = publisherType,
								PublisherMethod = publisherMethod,
								EventCategory = eventCategory,
								Channel = channel,
								SubscriberDetails = subscriberDetails
								#if RULESYSTEM_PRESENT
								,
								OriginalPublisherType = originalPublisherType ?? string.Empty,
								OriginalPublisherMethod = originalPublisherMethod ?? string.Empty,
								ActionTypeName = actionTypeName ?? string.Empty,
								ActionData = actionData ?? string.Empty
								#endif
							};
							_eventHistory.Add(historyEntry);
							if (_eventHistory.Count > _maxHistoryEntries) _eventHistory.RemoveAt(0);
						}

						if (_loggingLevel >= LoggingLevel.Minimal)
						{
							var logMessage = $"[EventBus] Event '{FormatTypeName(eventType)}' published to channel '{channel}' by {publisherType}.{publisherMethod} to {subscriberCount} subscriber(s). Depth: {_publishStack.Count}";
							if (_loggingLevel >= LoggingLevel.Detailed)
							{
								var failedCount = subscriberDetails.Count(s => !s.Executed);
								if (failedCount > 0) logMessage += $" ({failedCount} failed)";
							}
							DebugX.Logger(LogChannels.Framework).Info("{LogMessage}", logMessage);
						}
					}
					else
					#endif
					{
						if (hasSpecific) InvokeSubscribers(specificList, evt, eventType);
						if (hasGlobal) InvokeSubscribers(globalList, evt, eventType);
					}

					// Emit debug signal for event publication (optional, if emitter is registered)
					if (_debugSignalEmitter != null)
					{
						var scope = EventDebugScope.Global;
						// Prefer Identity (type Identity) or IIdentity; else fall back to EntityId (int?)
						if (evt is IIdentity identitySource && identitySource.Identity.IsValid)
							scope = EventDebugScope.Entity(identitySource.Identity);
						else
						{
							var scopeProps = GetDebugScopeProps(eventType);
							if (scopeProps.IdentityProp != null)
							{
								var id = (Identity)scopeProps.IdentityProp.GetValue(evt);
								if (id.IsValid)
									scope = EventDebugScope.Entity(id);
							}
							else if (scopeProps.EntityIdProp != null)
							{
								var entityId = scopeProps.EntityIdProp.GetValue(evt) as int?;
								if (entityId.HasValue)
									scope = EventDebugScope.Entity(entityId.Value);
							}
						}
						// Channel: Events = 3, Severity: Trace = 0
						_debugSignalEmitter.EmitEventSignal(3, 0, scope, $"EventPublished: {FormatTypeName(eventType)}");
					}
				}
				else
				{
					HandleNoSubscribers(eventType, channel);
				}
			}
			else
			{
				HandleNoSubscribers(eventType, channel);
			}
		}
		finally
		{
			#if UNITY_EDITOR || DEVELOPMENT_BUILD
			if (_monitoringEnabled && _publishStack.Count > 0)
				_publishStack.Pop();
			#endif
		}
	}

	#if UNITY_EDITOR || DEVELOPMENT_BUILD
	private static void InvokeSubscribersTracking<T>(List<PrioritizedCallback> list, T evt, Type eventType, Identity channel, string publisherType, string publisherMethod, EventCategory eventCategory, List<SubscriberDetail> subscriberDetails) where T : BaseGameEvent
	{
		var buffer = RentInvokeBuffer();
		buffer.Clear();
		buffer.AddRange(list);
		_invokeDepth++;
		try
		{
			for (int i = 0; i < buffer.Count; i++)
			{
				var wrapper = buffer[i];
				var handlerType = GetActualClassName(wrapper.Callback, wrapper.Target);

				#if RULESYSTEM_PRESENT
				if (wrapper.Callback != null && wrapper.Callback.Target != null)
				{
					var targetType = wrapper.Callback.Target.GetType();
					if (IsRuleSystemClass(targetType))
						continue;
				}

				var methodName = ExtractRuleMethodName(wrapper.Callback);
				if (methodName == "Unknown" || methodName.Contains("b__", StringComparison.Ordinal))
				{
					methodName = wrapper.Callback?.Method?.Name ?? "Unknown";
					if (methodName.StartsWith("b__", StringComparison.Ordinal))
					{
						methodName = "Subscribe";
					}
				}
				#else
				var methodName = wrapper.Callback?.Method?.Name ?? "Unknown";
				if (methodName.StartsWith("b__", StringComparison.Ordinal))
				{
					methodName = "Subscribe";
				}
				#endif

				var priority = wrapper.Priority;
				bool executed = false;
				string errorMessage = null;

				#if UNITY_EDITOR || DEVELOPMENT_BUILD
				_executionStack.Push(evt);
				#endif
				try
				{
					(wrapper.Callback as Action<T>)?.Invoke(evt);
					executed = true;
				}
				catch (Exception ex)
				{
					executed = false;
					errorMessage = ex.Message;
					#if UNITY_EDITOR || DEVELOPMENT_BUILD
					var prov = evt.Provenance;
					if (prov != null)
					{
						UnityEngine.Debug.LogError(
							$"[EventBus] Subscriber Exception in {handlerType}.{methodName}\n" +
							$"Event: {FormatTypeName(eventType)} #{prov.EventId}\n" +
							$"ParentEventId: #{prov.ParentEventId}\n" +
							$"Published From:\n" +
							$"    {prov.PublisherType}.{prov.PublisherMethod}\n" +
							$"    (at {prov.File}:{prov.Line})\n" +
							$"Frame: {prov.Frame}\n" +
							$"Exception: {ex}");
					}
					else
					{
						DebugX.Logger(LogChannels.Framework).Error("[EventBus] Error invoking callback for {EventType} on channel {Channel}: {Exception}", FormatTypeName(eventType), channel, ex);
					}
					#else
					DebugX.Logger(LogChannels.Framework).Error("[EventBus] Error invoking callback for {EventType} on channel {Channel}: {Exception}", FormatTypeName(eventType), channel, ex);
					#endif
				}
				#if UNITY_EDITOR || DEVELOPMENT_BUILD
				finally
				{
					_executionStack.Pop();
				}
				#endif

				subscriberDetails.Add(new SubscriberDetail
				{
					HandlerType = handlerType,
					MethodName = methodName,
					Priority = priority,
					Executed = executed,
					ErrorMessage = errorMessage
				});
			}
		}
		finally
		{
			_invokeDepth--;
			buffer.Clear();
		}
	}
	#endif

	private static void InvokeSubscribers<T>(List<PrioritizedCallback> list, T evt, Type eventType) where T : BaseGameEvent
	{
		var buffer = RentInvokeBuffer();
		buffer.Clear();
		buffer.AddRange(list);
		_invokeDepth++;
		try
		{
			for (int i = 0; i < buffer.Count; i++)
			{
				var callbackWrapper = buffer[i];
				#if UNITY_EDITOR || DEVELOPMENT_BUILD
				_executionStack.Push(evt);
				#endif
				try
				{
					(callbackWrapper.Callback as Action<T>)?.Invoke(evt);
				}
				catch (Exception ex)
				{
					#if UNITY_EDITOR || DEVELOPMENT_BUILD
					var prov = evt.Provenance;
					var handlerType = GetActualClassName(callbackWrapper.Callback, callbackWrapper.Target);
					var methodName = callbackWrapper.Callback?.Method?.Name ?? "Unknown";
					if (methodName.StartsWith("b__", StringComparison.Ordinal))
					{
						methodName = "Subscribe";
					}

					if (prov != null)
					{
						UnityEngine.Debug.LogError(
							$"[EventBus] Subscriber Exception in {handlerType}.{methodName}\n" +
							$"Event: {FormatTypeName(eventType)} #{prov.EventId}\n" +
							$"ParentEventId: #{prov.ParentEventId}\n" +
							$"Published From:\n" +
							$"    {prov.PublisherType}.{prov.PublisherMethod}\n" +
							$"    (at {prov.File}:{prov.Line})\n" +
							$"Frame: {prov.Frame}\n" +
							$"Exception: {ex}");
					}
					else
					{
						DebugX.Logger(LogChannels.Framework).Error("[EventBus] Error invoking callback for {EventType}: {Exception}", FormatTypeName(eventType), ex);
					}
					#else
					DebugX.Logger(LogChannels.Framework).Error("[EventBus] Error invoking callback for {EventType}: {Exception}", FormatTypeName(eventType), ex);
					#endif
				}
				#if UNITY_EDITOR || DEVELOPMENT_BUILD
				finally
				{
					_executionStack.Pop();
				}
				#endif
			}
		}
		finally
		{
			_invokeDepth--;
			buffer.Clear();
		}
	}

	private static void HandleNoSubscribers(Type eventType, Identity channel)
	{
		var behaviorAttr = Attribute.GetCustomAttribute(eventType, typeof(NoSubscriberBehaviorAttribute)) as NoSubscriberBehaviorAttribute;
		var behavior = behaviorAttr?.Behavior ?? NoSubscriberBehavior.Warn;

		if (behavior == NoSubscriberBehavior.Ignore) return;

		if (behavior == NoSubscriberBehavior.Warn)
		{
			DebugX.Logger(LogChannels.Framework).Warning("[EventBus] Event '{EventType}' published to channel '{Channel}' with no subscribers.", FormatTypeName(eventType), channel);
		}
		else if (behavior == NoSubscriberBehavior.Error)
		{
			#if UNITY_EDITOR
			DebugX.Logger(LogChannels.Framework).Error("[EventBus] Event '{EventType}' published to channel '{Channel}' with no subscribers! This may indicate missing rules or handlers.", FormatTypeName(eventType), channel);
			#else
			DebugX.Logger(LogChannels.Framework).Warning("[EventBus] Event '{EventType}' published to channel '{Channel}' with no subscribers.", FormatTypeName(eventType), channel);
			#endif
		}
	}

	/// <summary>
	/// Clear all subscribers (useful for tests or resets).
	/// </summary>
	public static void Clear()
	{
		_subscribers.Clear();
		_nextTokenId = 1;

		// Reset the reusable invoke buffers only when not inside a publish. Clearing them
		// mid-dispatch (_invokeDepth > 0) would corrupt the buffer/depth invariant that the
		// active InvokeSubscribers loop relies on, so we leave them intact in that case.
		if (_invokeDepth == 0)
		{
			_invokeDepth = 0;
			_invokeBuffers.Clear();
		}
		#if UNITY_EDITOR || DEVELOPMENT_BUILD
		_publishStack.Clear();
		_eventHistory.Clear();
		_subscriptionHistory.Clear();
		_nextEventId = 1;
		_executionStack.Clear();
		#endif
	}

	/// <summary>
	/// Get subscriber count for an event type.
	/// </summary>
	public static int GetSubscriberCount<T>(Identity channel = default) where T : BaseGameEvent
	{
		if (_subscribers.TryGetValue(typeof(T), out var channels))
		{
			if (channel.IsValid)
				return channels.TryGetValue(channel, out var list) ? list.Count : 0;
			
			// Total count across all channels if None provided
			return channels.Values.Sum(l => l.Count);
		}
		return 0;
	}

	#if UNITY_EDITOR
	/// <summary>
	/// Debug info for all subscribers (Editor only).
	/// </summary>
	public static Dictionary<Type, List<string>> GetSubscriberDebugInfo()
	{
		var result = new Dictionary<Type, List<string>>();
		foreach (var kvp in _subscribers)
		{
			var eventTypeTargets = new List<string>();
			foreach (var channelKvp in kvp.Value)
			{
				var channel = channelKvp.Key;
				foreach (var wrapper in channelKvp.Value)
				{
					var target = wrapper.Target;
					var callback = wrapper.Callback;
					var priority = wrapper.Priority;

					#if RULESYSTEM_PRESENT
					// Skip GameAction infrastructure classes
					if (target != null)
					{
						var targetType = target.GetType();
						if (IsRuleSystemClass(targetType))
							continue;
					}
					#endif

					string targetName;
					int instanceId = 0;
					if (target is MonoBehaviour mb)
					{
						// Handle destroyed UnityEngine.Object instances gracefully in editor
						if (mb == null)
						{
							targetName = "<Destroyed>.MonoBehaviour";
						}
						else
						{
							// Use GetActualClassName to handle polymorphism and get actual runtime type
							var actualClassName = GetActualClassName(callback, mb);
							targetName = $"{mb.gameObject.name}.{actualClassName}";
							instanceId = mb.GetInstanceID();
						}
					}
					else
					{
						// Use GetActualClassName to handle closures and polymorphism
						targetName = GetActualClassName(callback, target);
					}

					#if RULESYSTEM_PRESENT
					// Extract rule method name
					var methodName = ExtractRuleMethodName(callback);
					// If we couldn't extract a rule method name, fall back to the original method name
					if (methodName == "Unknown" || methodName.Contains("b__", StringComparison.Ordinal))
					{
						methodName = callback.Method?.Name ?? "Unknown";
						// Get method name and replace compiler-generated names
						if (methodName.Contains("b__", StringComparison.Ordinal))
						{
							methodName = "Subscribe";
						}
					}
					#else
					// Get method name and replace compiler-generated names
					var methodName = callback.Method?.Name ?? "Unknown";
					if (methodName.Contains("b__", StringComparison.Ordinal))
					{
						methodName = "Subscribe";
					}
					#endif

					var channelInfo = channel == Identity.Global ? " [Global]" : channel.IsValid ? $" [Channel: {channel}]" : " [Global]";
					var tokenSuffix = wrapper.TokenId != 0 ? $" #Token:{wrapper.TokenId}" : "";

					// Include InstanceID for MonoBehaviour instances to enable Ping functionality
					if (instanceId != 0)
					{
						eventTypeTargets.Add($"[{priority}]{channelInfo} {targetName}.{methodName}{tokenSuffix} @ InstanceID: {instanceId}");
					}
					else
					{
						eventTypeTargets.Add($"[{priority}]{channelInfo} {targetName}.{methodName}{tokenSuffix}");
					}
				}
			}
			result[kvp.Key] = eventTypeTargets;
		}
		return result;
	}

	/// <summary>
	/// Current publish stack (Editor only).
	/// </summary>
	public static List<string> GetCurrentPublishStack()
	{
		#if UNITY_EDITOR || DEVELOPMENT_BUILD
		// Stack enumerates in LIFO order (most recent first), but we need FIFO order (oldest first) for display
		var stackArray = _publishStack.ToArray();
		Array.Reverse(stackArray);
		return stackArray.Select(r => FormatTypeName(r.EventType)).ToList();
		#else
		return new List<string>();
		#endif
	}
	
	/// <summary>
	/// Get event history (Editor only).
	/// </summary>
	public static List<EventHistoryEntry> GetEventHistory()
	{
		#if UNITY_EDITOR || DEVELOPMENT_BUILD
		return new List<EventHistoryEntry>(_eventHistory);
		#else
		return new List<EventHistoryEntry>();
		#endif
	}
	
	/// <summary>
	/// Clear event history (Editor only).
	/// </summary>
	public static void ClearEventHistory()
	{
		#if UNITY_EDITOR || DEVELOPMENT_BUILD
		_eventHistory.Clear();
		#endif
	}
	
	/// <summary>
	/// Set max history entries (Editor only).
	/// </summary>
	public static void SetMaxHistoryEntries(int maxEntries)
	{
		#if UNITY_EDITOR || DEVELOPMENT_BUILD
		_maxHistoryEntries = Mathf.Max(10, maxEntries);
		#endif
	}
	
	/// <summary>
	/// Get subscription history (Editor only).
	/// </summary>
	public static List<SubscriptionHistoryEntry> GetSubscriptionHistory()
	{
		#if UNITY_EDITOR || DEVELOPMENT_BUILD
		return new List<SubscriptionHistoryEntry>(_subscriptionHistory);
		#else
		return new List<SubscriptionHistoryEntry>();
		#endif
	}
	
	/// <summary>
	/// Clear subscription history (Editor only).
	/// </summary>
	public static void ClearSubscriptionHistory()
	{
		#if UNITY_EDITOR || DEVELOPMENT_BUILD
		_subscriptionHistory.Clear();
		#endif
	}
	
	/// <summary>
	/// Set max subscription history entries (Editor only).
	/// </summary>
	public static void SetMaxSubscriptionHistoryEntries(int maxEntries)
	{
		#if UNITY_EDITOR || DEVELOPMENT_BUILD
		_maxSubscriptionHistoryEntries = Mathf.Max(10, maxEntries);
		#endif
	}
	
	/// <summary>
	/// Set logging level (Editor only).
	/// </summary>
	public static void SetLoggingLevel(LoggingLevel level)
	{
		#if UNITY_EDITOR || DEVELOPMENT_BUILD
		_loggingLevel = level;
		#endif
	}
	
	/// <summary>
	/// Enable or disable subscription tracking (Editor only).
	/// </summary>
	public static void EnableSubscriptionTracking(bool enable)
	{
		#if UNITY_EDITOR || DEVELOPMENT_BUILD
		_enableSubscriptionTracking = enable;
		#endif
	}
	#endif

	/// <summary>
	/// Configure monitoring behavior without introducing hard type dependencies.
	/// Intended to be called by an optional monitor component.
	/// </summary>
	public static void ConfigureMonitoring(bool enabled, int maxPublishDepth = 10, bool stopOnDepthExceeded = true,
		bool warnAtPercentage = true, int warningThresholdPercent = 75, bool enableEventHistory = false)
	{
		_monitoringEnabled = enabled;
		_maxPublishDepth = Mathf.Max(1, maxPublishDepth);
		_stopOnDepthExceeded = stopOnDepthExceeded;
		_warnAtPercentage = warnAtPercentage;
		_warningThresholdPercent = Mathf.Clamp(warningThresholdPercent, 0, 100);
		_enableEventHistory = enableEventHistory;
	}

	/// <summary>
	/// Register a debug signal emitter for optional integration with GameEngineCore's DebugSignalBus.
	/// If no emitter is registered, debug signals are silently skipped.
	/// </summary>
	public static void RegisterDebugSignalEmitter(IEventDebugSignalEmitter emitter)
	{
		_debugSignalEmitter = emitter;
	}

}
}
