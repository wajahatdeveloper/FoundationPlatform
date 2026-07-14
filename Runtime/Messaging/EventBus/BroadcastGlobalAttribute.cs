using System;

/// <summary>
/// When present on an event type, publishing that event also delivers to subscribers on the global channel,
/// so handlers subscribed with Identity.Global receive entity-scoped events.
/// </summary>
namespace AetherNexus.FoundationPlatform.Messaging
{
[AttributeUsage(AttributeTargets.Class, Inherited = false, AllowMultiple = false)]
public sealed class BroadcastGlobalAttribute : Attribute { }
}
