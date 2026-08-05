#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using AetherNexus.FoundationPlatform.DebugX;
using UnityEditor;
using UnityEngine;

namespace AetherNexus.FoundationPlatform.Editor.Utilities.Debugging
{
	using DebugX = DebugX.DebugX;
	
	/// <summary>
	///  One collapsible detail block in the <see cref="EntityDebuggerOverlay"/> — the in-context
	///  counterpart to a <see cref="FrameworkDebuggerWindow{TEntity}"/>'s detail pane. A section knows
	///  how to recognise the GameObject it cares about (e.g. "has a CharacterLocomotion") and how to
	///  draw that object's live state. The overlay binds to the Scene-View selection, asks every
	///  registered section whether it applies, and stacks the ones that do — so clicking a unit in
	///  Play mode shows its Character / AI / GAS / Combat state right beside it, with no window hunting.
	///
	///  Implementations must be concrete with a public parameterless constructor; they are discovered
	///  automatically via <see cref="EntityDebugSectionRegistry"/>. Keep <see cref="AppliesTo"/> cheap
	///  (a GetComponent-style check) — it is polled on every selection change.
	/// </summary>
	public interface IEntityDebugSection
	{
		/// <summary>Foldout heading, e.g. "Character", "AI", "GAS".</summary>
		string Title { get; }

		/// <summary>Stacking order in the overlay (ascending). Ties break by title.</summary>
		int Order { get; }

		/// <summary>True when this section has something to show for <paramref name="go"/>.</summary>
		bool AppliesTo(GameObject go);

		/// <summary>Draw the section body with IMGUI (EditorGUILayout / DebugDrawKit). Only called
		/// when <see cref="AppliesTo"/> returned true for the same object and the foldout is open.
		/// This is the single source of the detail drawing — the matching full debugger window
		/// delegates its detail pane here.</summary>
		void DrawDetail(GameObject go);

		/// <summary>Open the full multi-entity debugger window this section is the in-context glance for
		/// (the overlay's "Open" button). Sections live in the same editor assembly as their window, so
		/// this is a direct call to the window's static Open().</summary>
		void OpenFullWindow();
	}

	/// <summary>
	///  Auto-discovers every concrete <see cref="IEntityDebugSection"/> across loaded editor assemblies
	///  and caches one instance of each, sorted by <see cref="IEntityDebugSection.Order"/>. Adding a new
	///  section is drop-in: implement the interface in any framework's editor assembly, no registration.
	/// </summary>
	public static class EntityDebugSectionRegistry
	{
		private static IEntityDebugSection[] _sections;

		public static IReadOnlyList<IEntityDebugSection> Sections => _sections ??= Build();

		public static bool HasApplicable(GameObject go)
		{
			if (go == null)
			{
				return false;
			}

			var sections = Sections;
			for (var i = 0; i < sections.Count; i++)
			{
				if (sections[i].AppliesTo(go))
				{
					return true;
				}
			}

			return false;
		}

		private static IEntityDebugSection[] Build()
		{
			var list = new List<IEntityDebugSection>();
			foreach (var type in TypeCache.GetTypesDerivedFrom<IEntityDebugSection>())
			{
				if (type.IsAbstract || type.IsGenericTypeDefinition || type.GetConstructor(Type.EmptyTypes) == null)
				{
					continue;
				}

				try
				{
					list.Add((IEntityDebugSection)Activator.CreateInstance(type));
				}
				catch (Exception e)
				{
					DebugX.Logger(LogChannels.Editor).Error("[EntityDebuggerOverlay] Failed to instantiate section '{TypeFullName}': {Message}", type.FullName, e.Message);
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
