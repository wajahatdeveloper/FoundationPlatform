/// <summary>
/// Base class for domain events that represent gameplay state changes.
/// Domain events are published by rules during validation or commit phases.
/// They represent meaningful game state transitions (e.g., city captured, turn started, unit moved).
/// </summary>
namespace AetherNexus.FoundationPlatform.Messaging
{
public abstract class DomainEvent : BaseGameEvent
{
    protected DomainEvent()
    {
    }
}
}

