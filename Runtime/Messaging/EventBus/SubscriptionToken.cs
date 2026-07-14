using System;

/// <summary>
/// Token returned by EventBus.Subscribe for reliable unsubscribe. Dispose to unsubscribe.
/// Eliminates delegate equality issues with lambdas/closures.
/// </summary>
namespace AetherNexus.FoundationPlatform.Messaging
{
public readonly struct SubscriptionToken : IDisposable
{
	internal readonly Type EventType;
	internal readonly Identity Channel;
	internal readonly int Id;

	internal SubscriptionToken(Type eventType, Identity channel, int id)
	{
		EventType = eventType;
		Channel = channel;
		Id = id;
	}

	public bool IsValid => Id != 0;

	public void Dispose() => EventBus.Unsubscribe(this);
}
}
