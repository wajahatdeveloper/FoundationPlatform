/// <summary>
/// Interface for classifying rule-system infrastructure types for EventBus's debug-display windows.
/// Allows EventBus to filter GameAction/RuleSystem infrastructure out of publisher/subscriber name
/// display without hardcoding GameEngineCore class names in the foundation layer. GameEngineCore
/// registers an implementation via <see cref="EventBus.RegisterRuleSystemClassifier"/> when its rule
/// system is present.
/// </summary>
namespace AetherNexus.FoundationPlatform.Messaging
{
public interface IRuleSystemDebugClassifier
{
	/// <summary>
	/// True if <paramref name="type"/> is GameAction/RuleSystem infrastructure (not a user rule class)
	/// and should be filtered out of debug-display publisher/subscriber names.
	/// </summary>
	bool IsInfrastructureClass(System.Type type);
}
}
