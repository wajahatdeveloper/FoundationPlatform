using System;
using AetherNexus.FoundationPlatform.Behaviours;
using UnityEngine;

namespace AetherNexus.FoundationPlatform.Extensions
{
	/// <summary>
	///  <c>UnityEngine.Random</c>'s shape, backed by whatever random source the game installed.
	///  <para>
	///  Determinism fails quietly. A single <c>Random.insideUnitSphere</c> in a simulation path does not
	///  throw, does not warn, and does not show up until a replay diverges or two clients disagree — and the
	///  reason it keeps happening is that the deterministic API did not offer the call, so reaching for
	///  Unity's was the only way to finish the line. This mirrors Unity's surface member for member, so
	///  switching a file is <c>Random.</c> → <c>RandomX.</c> and nothing else, and the deterministic path is
	///  no longer the inconvenient one.
	///  </para>
	///  <para>
	///  Lowercase member names are deliberate: they match <c>UnityEngine.Random</c> exactly so the
	///  substitution stays mechanical.
	///  </para>
	///  <para>
	///  There is no fallback. With no provider installed every call throws, naming the fix — a silent
	///  fallback to a non-deterministic source would reintroduce the exact bug this type exists to remove.
	///  </para>
	/// </summary>
	public static partial class RandomX
	{
		private static IRandomProvider _provider;
		private static IRandomProvider _presentationProvider;

		/// <summary>
		///  The gameplay random source. Installed once at bootstrap by the game engine; unset means every
		///  call below throws rather than guessing.
		/// </summary>
		public static IRandomProvider Provider
		{
			get => _provider;
			set => _provider = value;
		}

		/// <summary>
		///  Optional non-deterministic source for presentation-only randomness (VFX jitter, audio variation).
		///  Draws here never touch the gameplay sequence. Falls back to <see cref="Provider"/> when unset.
		/// </summary>
		public static IRandomProvider PresentationProvider
		{
			get => _presentationProvider;
			set => _presentationProvider = value;
		}

		/// <summary>True once a gameplay provider is installed.</summary>
		public static bool HasProvider => _provider != null;

		// Statics survive Stop->Play without domain reload, so a provider bound to the previous session's
		// RNG would keep serving draws into the new one — determinism that looks fine and is not.
		[RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
		private static void ResetOnPlayModeEnter()
		{
			_provider = null;
			_presentationProvider = null;
		}

		// ── UnityEngine.Random surface ────────────────────────────────────────────────

		/// <summary>Random float in [0, 1). Mirrors <c>Random.value</c>.</summary>
		public static float value => Required().Range(0f, 1f);

		/// <summary>
		///  Random float in [min, max). Mirrors <c>Random.Range(float, float)</c> in shape.
		///  <para>Note: Unity's float overload is max-<i>inclusive</i>; this one is max-exclusive, matching
		///  the underlying deterministic generator. The difference is one representable float and never
		///  matters in practice, but it is stated here rather than hidden.</para>
		/// </summary>
		public static float Range(float min, float max) => Required().Range(min, max);

		/// <summary>Random int in [min, max). Mirrors <c>Random.Range(int, int)</c> exactly.</summary>
		public static int Range(int min, int max) => Required().Range(min, max);

		/// <summary>Random point inside a unit circle. Mirrors <c>Random.insideUnitCircle</c>.</summary>
		public static Vector2 insideUnitCircle
		{
			get
			{
				var provider = Required();

				// Rejection sampling rather than polar: uniform-by-area without a sqrt, and it draws a
				// bounded number of times from the same sequence, which keeps replays reproducible.
				for (var attempt = 0; attempt < 64; attempt++)
				{
					var x = provider.Range(-1f, 1f);
					var y = provider.Range(-1f, 1f);
					if (x * x + y * y <= 1f)
					{
						return new Vector2(x, y);
					}
				}

				return Vector2.zero;
			}
		}

		/// <summary>Random point inside a unit sphere. Mirrors <c>Random.insideUnitSphere</c>.</summary>
		public static Vector3 insideUnitSphere
		{
			get
			{
				var provider = Required();
				for (var attempt = 0; attempt < 64; attempt++)
				{
					var x = provider.Range(-1f, 1f);
					var y = provider.Range(-1f, 1f);
					var z = provider.Range(-1f, 1f);
					if (x * x + y * y + z * z <= 1f)
					{
						return new Vector3(x, y, z);
					}
				}

				return Vector3.zero;
			}
		}

		/// <summary>Random point on the unit sphere's surface. Mirrors <c>Random.onUnitSphere</c>.</summary>
		public static Vector3 onUnitSphere
		{
			get
			{
				var provider = Required();

				// Uniform on the sphere: z uniform in [-1,1] with a uniform azimuth (Archimedes' theorem).
				var z = provider.Range(-1f, 1f);
				var theta = provider.Range(0f, Mathf.PI * 2f);
				var r = Mathf.Sqrt(Mathf.Max(0f, 1f - z * z));
				return new Vector3(r * Mathf.Cos(theta), r * Mathf.Sin(theta), z);
			}
		}

		/// <summary>Random rotation. Mirrors <c>Random.rotation</c>.</summary>
		public static Quaternion rotation => rotationUniform;

		/// <summary>Uniformly distributed random rotation. Mirrors <c>Random.rotationUniform</c>.</summary>
		public static Quaternion rotationUniform
		{
			get
			{
				var provider = Required();

				// Shoemake's uniform quaternion sampling — three uniform draws, no rejection.
				var u1 = provider.Range(0f, 1f);
				var u2 = provider.Range(0f, Mathf.PI * 2f);
				var u3 = provider.Range(0f, Mathf.PI * 2f);

				var sqrt1MinusU1 = Mathf.Sqrt(Mathf.Max(0f, 1f - u1));
				var sqrtU1 = Mathf.Sqrt(Mathf.Max(0f, u1));

				return new Quaternion(
					sqrt1MinusU1 * Mathf.Sin(u2),
					sqrt1MinusU1 * Mathf.Cos(u2),
					sqrtU1 * Mathf.Sin(u3),
					sqrtU1 * Mathf.Cos(u3));
			}
		}

		/// <summary>Random fully-saturated, fully-bright, opaque colour. Mirrors <c>Random.ColorHSV()</c>.</summary>
		public static Color ColorHSV() => ColorHSV(0f, 1f, 0f, 1f, 0f, 1f, 1f, 1f);

		/// <summary>Random colour with hue constrained. Mirrors <c>Random.ColorHSV(h,h)</c>.</summary>
		public static Color ColorHSV(float hueMin, float hueMax) =>
			ColorHSV(hueMin, hueMax, 0f, 1f, 0f, 1f, 1f, 1f);

		/// <summary>Random colour with hue and saturation constrained.</summary>
		public static Color ColorHSV(float hueMin, float hueMax, float saturationMin, float saturationMax) =>
			ColorHSV(hueMin, hueMax, saturationMin, saturationMax, 0f, 1f, 1f, 1f);

		/// <summary>Random colour with hue, saturation and value constrained.</summary>
		public static Color ColorHSV(
			float hueMin, float hueMax,
			float saturationMin, float saturationMax,
			float valueMin, float valueMax) =>
			ColorHSV(hueMin, hueMax, saturationMin, saturationMax, valueMin, valueMax, 1f, 1f);

		/// <summary>Random colour with every channel constrained. Mirrors the full <c>Random.ColorHSV</c>.</summary>
		public static Color ColorHSV(
			float hueMin, float hueMax,
			float saturationMin, float saturationMax,
			float valueMin, float valueMax,
			float alphaMin, float alphaMax)
		{
			var provider = Required();
			var color = Color.HSVToRGB(
				Mathf.Lerp(hueMin, hueMax, provider.Range(0f, 1f)),
				Mathf.Lerp(saturationMin, saturationMax, provider.Range(0f, 1f)),
				Mathf.Lerp(valueMin, valueMax, provider.Range(0f, 1f)),
				hdr: true);
			color.a = Mathf.Lerp(alphaMin, alphaMax, provider.Range(0f, 1f));
			return color;
		}

		// ── Beyond UnityEngine.Random ─────────────────────────────────────────────────

		/// <summary>
		///  An independent named sequence. Use one per system that rolls — <c>"loot"</c>, <c>"crit"</c>,
		///  <c>"spawn"</c> — so adding a draw in one cannot shift the results of another.
		/// </summary>
		public static IRandomProvider Stream(string name)
		{
			if (string.IsNullOrEmpty(name))
			{
				throw new ArgumentException("RandomX.Stream requires a non-empty stream name.", nameof(name));
			}

			return RequiredStreamSource("Stream").Stream(name);
		}

		/// <summary>
		///  The position of every stream, as an opaque payload. Write it into your save so a load resumes the
		///  sequence instead of replaying rolls the player already saw.
		/// </summary>
		public static string CaptureState() => RequiredStreamSource(nameof(CaptureState)).CaptureState();

		/// <summary>Restores a payload from <see cref="CaptureState"/>.</summary>
		public static void RestoreState(string payload) =>
			RequiredStreamSource(nameof(RestoreState)).RestoreState(payload);

		/// <summary>
		///  Presentation-only randomness — VFX jitter, audio variation, idle flourishes. Never affects the
		///  gameplay sequence, so it is safe in code that must not perturb determinism.
		/// </summary>
		public static float PresentationRange(float min, float max)
		{
			var provider = _presentationProvider ?? Required();
			return provider.Range(min, max);
		}

		// ── Guards ────────────────────────────────────────────────────────────────────

		private static IRandomProvider Required()
		{
			if (_provider == null)
			{
				throw new InvalidOperationException(
					"RandomX has no provider installed. A game engine installs one during bootstrap " +
					"(GameEngineCore does this in SystemBoot). Set RandomX.Provider before rolling, or use " +
					"UnityEngine.Random explicitly if this code is genuinely non-deterministic.");
			}

			return _provider;
		}

		private static IRandomStreamSource RequiredStreamSource(string member)
		{
			if (Required() is IRandomStreamSource source)
			{
				return source;
			}

			throw new InvalidOperationException(
				$"RandomX.{member} requires a provider implementing {nameof(IRandomStreamSource)}; the " +
				$"installed provider ({_provider.GetType().Name}) supports plain Range calls only.");
		}
	}
}
