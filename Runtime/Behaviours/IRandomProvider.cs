namespace FoundationPlatform.Behaviours
{
	/// <summary>
	///  Interface for random number generation providers.
	///  Allows components to use either deterministic (GameAction) or non-deterministic (Unity Random) behavior.
	/// </summary>
	public interface IRandomProvider
	{
		/// <summary>
		///  Generate a random float between min (inclusive) and max (inclusive).
		/// </summary>
		float Range(float min, float max);

		/// <summary>
		///  Generate a random int between min (inclusive) and max (exclusive).
		/// </summary>
		int Range(int min, int max);
	}
}

