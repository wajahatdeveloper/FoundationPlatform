#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace AetherNexus.FoundationPlatform.Editor.Utilities.Debugging
{
	/// <summary>
	///  One block of <i>world-scope</i> live state — the counterpart to <see cref="IEntityDebugSection"/>,
	///  which answers "what's up with this object?".
	///  <para>
	///  A running game holds a lot of state that belongs to no GameObject: the session's readiness and
	///  roster, which subsystems initialized, which lifecycle stage the scene reached, the current level,
	///  the RNG seed, the action pipeline. Selecting something cannot reveal any of it, so it has no home in
	///  the Scene-View overlay. Sections implementing this interface are stacked by
	///  <see cref="GameStateWindow"/> instead.
	///  </para>
	///  Implementations must be concrete with a public parameterless constructor; they are discovered
	///  automatically via <see cref="WorldDebugSectionRegistry"/>.
	/// </summary>
	public interface IWorldDebugSection
	{
		/// <summary>Heading shown in the section list, e.g. "Session", "Subsystems".</summary>
		string Title { get; }

		/// <summary>Ordering in the list (ascending). Ties break by title.</summary>
		int Order { get; }

		/// <summary>
		///  False when this section has nothing to say right now — typically because it needs Play mode, or
		///  its subsystem is not registered in this project. The window greys it rather than hiding it, so
		///  the absence itself stays visible.
		/// </summary>
		bool IsAvailable { get; }

		/// <summary>
		///  Why the section is unavailable, shown in its place. Ignored when <see cref="IsAvailable"/> is true.
		/// </summary>
		string UnavailableReason { get; }

		/// <summary>
		///  Draw the body with IMGUI (EditorGUILayout / DebugDrawKit). Called only when the section is
		///  selected; may be called every repaint, so keep it read-only and cheap.
		/// </summary>
		void DrawDetail();
	}

	/// <summary>
	///  Auto-discovers every concrete <see cref="IWorldDebugSection"/> across loaded editor assemblies and
	///  caches one instance of each, sorted by <see cref="IWorldDebugSection.Order"/>. Adding a section is
	///  drop-in: implement the interface in any framework's editor assembly, no registration call.
	/// </summary>
	public static class WorldDebugSectionRegistry
	{
		private static IWorldDebugSection[] _sections;

		public static IReadOnlyList<IWorldDebugSection> Sections => _sections ??= Build();

		/// <summary>Drop the cached instances (e.g. after a domain reload changed the type set).</summary>
		public static void Invalidate()
		{
			_sections = null;
		}

		private static IWorldDebugSection[] Build()
		{
			var list = new List<IWorldDebugSection>();
			foreach (var type in TypeCache.GetTypesDerivedFrom<IWorldDebugSection>())
			{
				if (type.IsAbstract || type.IsGenericTypeDefinition || type.GetConstructor(Type.EmptyTypes) == null)
				{
					continue;
				}

				try
				{
					list.Add((IWorldDebugSection)Activator.CreateInstance(type));
				}
				catch (Exception e)
				{
					Debug.LogError($"[GameStateWindow] Failed to instantiate section '{type.FullName}': {e.Message}");
				}
			}

			list.Sort((a, b) =>
			{
				var byOrder = a.Order.CompareTo(b.Order);
				return byOrder != 0 ? byOrder : string.CompareOrdinal(a.Title, b.Title);
			});
			return list.ToArray();
		}
	}
}
#endif
