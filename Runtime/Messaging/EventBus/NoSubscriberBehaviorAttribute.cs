using System;

/// <summary>
/// Attribute to control EventBus behavior when an event is published with no subscribers.
/// By default, EventBus logs a warning. Use [NoSubscriberBehavior(Ignore)] to suppress for specific events.
/// </summary>
namespace AetherNexus.FoundationPlatform.Messaging
{
[AttributeUsage(AttributeTargets.Class, Inherited = false, AllowMultiple = false)]
public sealed class NoSubscriberBehaviorAttribute : Attribute
{
	/// <summary>
	/// The behavior to use when no subscribers are found for this event
	/// </summary>
	public NoSubscriberBehavior Behavior { get; }

	/// <summary>
	/// Creates a new attribute with the specified behavior
	/// </summary>
	/// <param name="behavior">The behavior when no subscribers are found</param>
	public NoSubscriberBehaviorAttribute(NoSubscriberBehavior behavior)
	{
		Behavior = behavior;
	}
}

/// <summary>
/// Behavior options for events published with no subscribers
/// </summary>
public enum NoSubscriberBehavior
{
	/// <summary>
	/// Ignore - don't log when no subscribers (opt-in silence).
	/// </summary>
	Ignore = 0,

	/// <summary>
	/// Show a warning if no subscribers are found
	/// </summary>
	Warn = 1,

	/// <summary>
	/// Show an error if no subscribers are found (editor only, warning in runtime)
	/// </summary>
	Error = 2
}
}

