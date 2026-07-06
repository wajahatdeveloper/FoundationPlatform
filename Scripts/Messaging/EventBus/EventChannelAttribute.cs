using System;

/// <summary>
/// Attribute to specify the EventBus channel for an event type.
/// When present, subscribers (e.g. RuleHandler) should subscribe on this channel instead of a handler-specific channel.
/// </summary>
[AttributeUsage(AttributeTargets.Class, Inherited = false, AllowMultiple = false)]
public sealed class EventChannelAttribute : Attribute
{
	/// <summary>
	/// The channel name (used as string Identity for Subscribe/Publish).
	/// </summary>
	public string ChannelName { get; }

	public EventChannelAttribute(string channelName)
	{
		ChannelName = channelName ?? string.Empty;
	}
}
