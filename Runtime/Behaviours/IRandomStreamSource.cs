namespace AetherNexus.FoundationPlatform.Behaviours
{
	/// <summary>
	///  A random source that can also split into independent named streams and round-trip its own state.
	///  <para>
	///  <see cref="IRandomProvider"/> is deliberately tiny — two <c>Range</c> calls — because most callers
	///  need nothing else. The two things it cannot express both matter for determinism:
	///  </para>
	///  <list type="bullet">
	///   <item><b>Named streams.</b> One shared sequence couples unrelated systems: add a loot roll and every
	///   combat roll after it shifts, so a replay diverges for a reason that has nothing to do with combat.
	///   Independent streams keep each system's draws its own.</item>
	///   <item><b>State capture.</b> A save that does not record where the sequence had reached resumes from
	///   the seed, silently replaying rolls the player already saw.</item>
	///  </list>
	///  The state payload is an opaque provider-defined string, so this package stays ignorant of any
	///  particular RNG implementation.
	/// </summary>
	public interface IRandomStreamSource : IRandomProvider
	{
		/// <summary>
		///  An independent sequence for <paramref name="name"/>, derived from the same master seed. Repeated
		///  calls with the same name return the same stream — it is a lookup, not a factory.
		/// </summary>
		IRandomProvider Stream(string name);

		/// <summary>Serializes the current position of every stream. Opaque; pair with <see cref="RestoreState"/>.</summary>
		string CaptureState();

		/// <summary>Restores a payload from <see cref="CaptureState"/>. Throws on a payload it cannot read.</summary>
		void RestoreState(string payload);
	}
}
