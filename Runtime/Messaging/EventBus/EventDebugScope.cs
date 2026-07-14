/// <summary>
/// Scope type for event debug signals, indicating the context level.
/// </summary>
namespace AetherNexus.FoundationPlatform.Messaging
{
public enum EventDebugScopeType
{
	Global,   // System-wide scope
	Entity,   // Entity-specific scope
	System,   // System-specific scope
	Rule      // Rule-specific scope
}

/// <summary>
/// Scope information for event debug signals, allowing correlation across systems, entities, rules, and frames.
/// </summary>
public readonly struct EventDebugScope
{
	/// <summary>
	/// Type of scope (Global, Entity, System, Rule)
	/// </summary>
	public EventDebugScopeType Type { get; }

	/// <summary>
	/// Entity identity if scope is Entity-specific; invalid otherwise.
	/// </summary>
	public Identity EntityIdentity { get; }

	/// <summary>
	/// Entity ID (string) if scope is Entity-specific and identity is valid; null otherwise.
	/// </summary>
	public string EntityId => Type == EventDebugScopeType.Entity && EntityIdentity.IsValid ? EntityIdentity.Value : null;

	/// <summary>
	/// Create a global scope (system-wide)
	/// </summary>
	public static EventDebugScope Global => new EventDebugScope(EventDebugScopeType.Global, default);

	/// <summary>
	/// Create an entity scope from Identity.
	/// </summary>
	public static EventDebugScope Entity(Identity identity) => new EventDebugScope(EventDebugScopeType.Entity, identity);

	/// <summary>
	/// Create an entity scope from string ID.
	/// </summary>
	public static EventDebugScope Entity(string entityId) => new EventDebugScope(EventDebugScopeType.Entity, new Identity(entityId));

	/// <summary>
	/// Create an entity scope from int ID (convenience; converts to string for legacy events).
	/// </summary>
	public static EventDebugScope Entity(int entityId) => Entity(entityId.ToString());

	private EventDebugScope(EventDebugScopeType type, Identity entityIdentity)
	{
		Type = type;
		EntityIdentity = entityIdentity;
	}
}
}
