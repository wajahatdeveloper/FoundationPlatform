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
		void UpdateBlend(float moveX, float moveZ);
		string GetDominantEntryId();
		string ResolveTurnClipId(float signedAngle);
	}
}
