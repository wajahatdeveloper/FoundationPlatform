using System;

/// <summary>
/// Identity structure holding a single string identifier.
/// Used for unified event filtering and entity context.
/// </summary>
namespace AetherNexus.FoundationPlatform.Messaging
{
public readonly struct Identity : IEquatable<Identity>
{
	public static readonly Identity None = default;

	/// <summary>
	/// Sentinel for the global event channel. Use instead of default when subscribing/publishing globally.
	/// </summary>
	public static readonly Identity Global = new Identity("__global__");

	private readonly string _id;

	public bool IsValid => !string.IsNullOrEmpty(_id);
	public string Value => _id ?? string.Empty;

	public Identity(string id)
	{
		_id = id;
	}

	public bool Equals(Identity other) => string.Equals(_id, other._id, StringComparison.Ordinal);
	public override bool Equals(object obj) => obj is Identity other && Equals(other);

	public override int GetHashCode() => _id != null ? _id.GetHashCode() : 0;

	public override string ToString() => _id ?? "None";

	public static bool operator ==(Identity left, Identity right) => left.Equals(right);
	public static bool operator !=(Identity left, Identity right) => !left.Equals(right);

	public static implicit operator Identity(string id) => new Identity(id);
}
}
