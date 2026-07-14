using System;
using System.Collections;
using UnityEngine;
using UnityEngine.Playables;

namespace AetherNexus.FoundationPlatform.Animation
{
	public abstract class PlayableState : IEnumerator
	{
		public Playable Playable { get; protected set; }
		public bool IsValid => Playable.IsValid();
		public bool IsPlaying => IsValid && Playable.GetPlayState() == PlayState.Playing;

		/// <summary>True when the underlying motion loops (see <see cref="ClipState"/>).</summary>
		public virtual bool IsLooping => false;

		public float Speed
		{
			get => IsValid ? (float)Playable.GetSpeed() : 0f;
			set
			{
				if (IsValid) Playable.SetSpeed(value);
			}
		}

		private float _weight;
		public virtual float Weight
		{
			get => _weight;
			set
			{
				_weight = Mathf.Clamp01(value);
			}
		}

		public virtual float Time
		{
			get => IsValid ? (float)Playable.GetTime() : 0f;
			set
			{
				if (IsValid) Playable.SetTime(value);
			}
		}

		public virtual float Length => 0f;

		public float NormalizedTime
		{
			get
			{
				var length = Length;
				if (length <= 0f) return 0f;
				return Time / length;
			}
			set
			{
				Time = value * Length;
			}
		}

		public float EffectiveSpeed => Speed;

		private PlayableStateEvents _events;
		public PlayableStateEvents Events(object owner = null)
		{
			if (_events == null) _events = new PlayableStateEvents(this);
			return _events;
		}

		public virtual void Destroy()
		{
			if (IsValid)
			{
				var graph = Playable.GetGraph();
				if (graph.IsValid())
					graph.DestroyPlayable(Playable);
			}
		}

		public virtual void Update(float deltaTime)
		{
			_events?.Update();
		}

		// Animancer parity: `yield return state` inside a coroutine waits until the state
		// finishes (non-looping: time reaches the clip end) or is interrupted and destroyed.
		// Looping and length-less states wait until they are destroyed, same as Animancer.
		bool IEnumerator.MoveNext()
		{
			if (!IsValid) return false;
			if (IsLooping) return true;
			var length = Length;
			if (length <= 0f) return true;
			return Time < length;
		}

		object IEnumerator.Current => null;

		void IEnumerator.Reset() { }
	}
}
