using UnityEngine.Playables;

namespace AetherNexus.FoundationPlatform.Animation
{
	/// <summary>
	/// Manages locomotion blend playables: stances, directional blending, turn resolution.
	/// Bind once per active locomotion AnimationSet; call UpdateBlend every frame while grounded.
	/// </summary>
	public interface ILocomotionBlendLayer
	{
		Playable RootPlayable { get; }
		bool IsBound { get; }
		string ActiveStanceId { get; }

		void Bind(AnimationSet set, PlayableGraph graph);
		void Unbind();
		void SetStance(string stanceId);
		/// <summary>
		/// <paramref name="planarSpeed"/> (m/s) drives each stance's playback rate against its natural speed,
		/// so the feet cover the ground the body does.
		/// </summary>
		void UpdateBlend(float moveX, float moveZ, float planarSpeed);
		/// <summary>Instantly apply blend weights (no damp) so the graph is not bind-pose until the first Update.</summary>
		void SnapBlend(float moveX, float moveZ);
		string GetDominantEntryId();
		string ResolveTurnClipId(float signedAngle);
	}
}
