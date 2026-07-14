using UnityEngine;
using UnityEngine.Animations;
using UnityEngine.Playables;

namespace AetherNexus.FoundationPlatform.Animation
{
    public class ClipState : PlayableState
    {
        public AnimationClip Clip { get; private set; }

        public ClipState(PlayableGraph graph, AnimationClip clip)
        {
            Clip = clip;
            if (clip != null)
            {
                var playable = AnimationClipPlayable.Create(graph, clip);
                playable.Play();
                Playable = playable;
            }
        }

        public override float Length => Clip != null ? Clip.length : 0f;

        public override bool IsLooping => Clip != null && Clip.isLooping;
    }
}
