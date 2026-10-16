using System;

namespace AetherNexus.FoundationPlatform.Attributes
{
	/// <summary>
	///  Declares that <c>UnityEngine.Random</c> here is intentionally non-deterministic (audio variation, cosmetic
	///  jitter, presentation-only helpers) and never decides a gameplay outcome. Without it, analyzer AN0002 rejects
	///  <c>UnityEngine.Random</c> in first-party runtime code; gameplay uses <c>RandomX.Stream("&lt;system&gt;")</c>.
	/// </summary>
	[AttributeUsage(AttributeTargets.Class | AttributeTargets.Struct | AttributeTargets.Method | AttributeTargets.Property
	                | AttributeTargets.Constructor | AttributeTargets.Assembly, Inherited = false, AllowMultiple = false)]
	public sealed class PresentationRandomAttribute : Attribute
	{
	}
}
