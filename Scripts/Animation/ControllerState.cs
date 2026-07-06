using UnityEngine;
using UnityEngine.Animations;
using UnityEngine.Playables;

namespace FoundationPlatform.Animation
{
	public class ControllerState : PlayableState
	{
		public RuntimeAnimatorController Controller { get; private set; }

		public ControllerState(PlayableGraph graph, RuntimeAnimatorController controller)
		{
			Controller = controller;
			if (controller != null)
			{
				Playable = AnimatorControllerPlayable.Create(graph, controller);
			}
		}
	}
}
