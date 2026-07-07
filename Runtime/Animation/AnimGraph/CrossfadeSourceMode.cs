namespace FoundationPlatform.Animation
{
	/// <summary>
	/// Selects the playable mixed against a new overlay clip in <see cref="AnimGraph.CrossfadeAsync"/>.
	/// </summary>
	public enum CrossfadeSourceMode
	{
		/// <summary>Blend from the locomotion loop base (or layer home) after preempting any prior overlay.</summary>
		BlendLayerBase,

		/// <summary>Blend from the active non-loop overlay output (sequence handoff; no preempt-to-base).</summary>
		PreviousOverlay
	}
}
