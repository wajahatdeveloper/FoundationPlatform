/// <summary>
/// Interface for objects that possess an Identity.
/// Allows for automated filtering based on identity context.
/// </summary>
namespace AetherNexus.FoundationPlatform.Messaging
{
public interface IIdentity
{
    Identity Identity { get; }
}
}
