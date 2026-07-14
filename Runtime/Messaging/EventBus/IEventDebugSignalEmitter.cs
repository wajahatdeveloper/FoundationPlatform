/// <summary>
/// Interface for emitting debug signals for events.
/// Allows EventBus to emit debug signals without depending on GameEngineCore.
/// GameEngineCore can provide an implementation that bridges to DebugSignalBus.
/// </summary>
namespace AetherNexus.FoundationPlatform.Messaging
{
public interface IEventDebugSignalEmitter
{
	/// <summary>
	/// Emit a debug signal for an event publication.
	/// </summary>
	/// <param name="channel">Channel category (0=Lifecycle, 1=Rules, 2=Actions, 3=Events, 4=StateMutation, 5=Invariants, 6=Rendering, 7=Performance)</param>
	/// <param name="severity">Severity level (0=Trace, 1=Debug, 2=Info, 3=Warning, 4=Error, 5=Critical)</param>
	/// <param name="scope">Scope information (Global, Entity, System, Rule)</param>
	/// <param name="message">Message string</param>
	void EmitEventSignal(int channel, int severity, EventDebugScope scope, string message);
}
}

