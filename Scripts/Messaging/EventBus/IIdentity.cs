/// <summary>
/// Interface for objects that possess an Identity.
/// Allows for automated filtering based on identity context.
/// </summary>
public interface IIdentity
{
    Identity Identity { get; }
}
