using System;

/// <summary>
/// Marks a property to be serialized and displayed in the EventBus history window.
/// Only properties marked with this attribute will be included in the event data display.
/// </summary>
namespace AetherNexus.FoundationPlatform.Messaging
{
[AttributeUsage(AttributeTargets.Property, AllowMultiple = false)]
public class EventDataAttribute : Attribute
{
}
}

