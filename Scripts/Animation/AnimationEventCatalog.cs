using System;
using System.Collections.Generic;
using System.Reflection;

namespace FoundationPlatform.Animation
{
	/// <summary>
	///  Marks a class whose <c>public const string</c> fields are valid animation-event names.
	///  The Test Bench / clip-data drawer TypeCache-scans every class carrying this attribute to build
	///  the event-name dropdown, so designers pick from the exact names code subscribes with (no typos).
	///
	///  <para>Usage: put this on any static class that declares the event-name constants your gameplay
	///  code passes to <c>AddEventCallback</c>. The const's <b>value</b> (not its field name) is the
	///  event name fired into the graph's event dispatcher.</para>
	/// </summary>
	[AttributeUsage(AttributeTargets.Class, Inherited = false, AllowMultiple = false)]
	public sealed class AnimationEventNamesAttribute : Attribute { }

	/// <summary>
	///  Built-in animation-event names shared across characters. Code subscribes with these consts
	///  (<c>animator.AddEventCallback(CoreAnimationEvents.JumpStart, ...)</c>) and designers author a
	///  matching event marker on the clip. Add project-specific names here or in any other class tagged
	///  <see cref="AnimationEventNamesAttribute"/>.
	/// </summary>
	[AnimationEventNames]
	public static class CoreAnimationEvents
	{
		/// <summary>Fired at the takeoff frame of a jump clip. Commits the queued jump impulse + jump cue.</summary>
		public const string JumpStart = "Jump_Start";
	}

	/// <summary>Editor-facing lookup of all names declared under <see cref="AnimationEventNamesAttribute"/> classes.</summary>
	public static class AnimationEventNameCatalog
	{
		/// <summary>All known event names, sorted. Empty at runtime (the scan is editor-only).</summary>
		public static IEnumerable<string> AllNames()
		{
#if UNITY_EDITOR
			var names = new SortedSet<string>(StringComparer.Ordinal);
			foreach (var type in UnityEditor.TypeCache.GetTypesWithAttribute<AnimationEventNamesAttribute>())
			{
				foreach (var field in type.GetFields(BindingFlags.Public | BindingFlags.Static | BindingFlags.FlattenHierarchy))
				{
					if (field.IsLiteral && !field.IsInitOnly && field.FieldType == typeof(string))
					{
						var value = (string)field.GetRawConstantValue();
						if (!string.IsNullOrWhiteSpace(value))
							names.Add(value);
					}
				}
			}
			return names;
#else
			return Array.Empty<string>();
#endif
		}
	}
}
