#if UNITY_EDITOR
using System.Text;
using AetherNexus.FoundationPlatform.Utilities.Menus;
using UnityEditor;
using UnityEngine;

namespace AetherNexus.FoundationPlatform.Editor.Utilities.Debugging
{
	/// <summary>
	///  The world-scope diagnostics window: session, players, lifecycle stages, subsystems, level, RNG, and
	///  the action pipeline, all reading live state.
	///  <para>
	///  This is deliberately the mirror of the Scene-View <see cref="EntityDebuggerOverlay"/>, not a
	///  replacement for it. The overlay answers "what's up with the thing I selected"; this answers "what's
	///  up with the game" — the state that belongs to no GameObject and therefore has nowhere to appear.
	///  Both draw from a registry of drop-in sections, so a framework adds a block by implementing an
	///  interface.
	///  </para>
	///  A shell only: it owns the list, the live repaint, and Copy Info. Every domain-specific block lives
	///  in the package that owns that state.
	/// </summary>
	public sealed class GameStateWindow : EditorWindow
	{
		private const long RepaintIntervalMs = 200;
		private const string SelectedSectionKey = "AetherNexus.GameStateWindow.SelectedSection";

		private Vector2 _listScroll;
		private Vector2 _detailScroll;
		private string _selectedTitle;
		private bool _copyPending;
		private bool _copyActive;

		[MenuItem(MenuPaths.DomainWindow.GameState, false, MenuPriorities.WindowDomainCore)]
		public static void Open()
		{
			var window = GetWindow<GameStateWindow>("Game State");
			window.minSize = new Vector2(560f, 320f);
			window.Show();
		}

		private void OnEnable()
		{
			_selectedTitle = SessionState.GetString(SelectedSectionKey, null);
			AssemblyReloadEvents.afterAssemblyReload += OnAfterAssemblyReload;

			// Live state moves without any editor event firing, so drive repaints from a timer rather than
			// waiting for a selection or hierarchy change that will never come.
			EditorApplication.update += TickRepaint;
		}

		private void OnDisable()
		{
			AssemblyReloadEvents.afterAssemblyReload -= OnAfterAssemblyReload;
			EditorApplication.update -= TickRepaint;
		}

		private static void OnAfterAssemblyReload()
		{
			WorldDebugSectionRegistry.Invalidate();
		}

		private double _nextRepaint;

		private void TickRepaint()
		{
			var now = EditorApplication.timeSinceStartup;
			if (now < _nextRepaint)
			{
				return;
			}

			_nextRepaint = now + RepaintIntervalMs / 1000d;
			Repaint();
		}

		private void OnGUI()
		{
			var sections = WorldDebugSectionRegistry.Sections;
			if (sections.Count == 0)
			{
				EditorGUILayout.HelpBox(
					"No world debug sections found. Sections are discovered automatically — implement " +
					nameof(IWorldDebugSection) + " in any editor assembly.",
					MessageType.Info);
				return;
			}

			DrawToolbar();

			using (new EditorGUILayout.HorizontalScope())
			{
				DrawSectionList(sections);
				DrawDetail(sections);
			}
		}

		private void DrawToolbar()
		{
			using (new EditorGUILayout.HorizontalScope(EditorStyles.toolbar))
			{
				GUILayout.Label(
					Application.isPlaying ? "Playing" : "Edit mode — most sections need Play",
					EditorStyles.miniLabel);

				GUILayout.FlexibleSpace();

				var selection = Selection.activeGameObject;
				using (new EditorGUI.DisabledScope(!EntityDebuggerHandoff.CanReveal(selection)))
				{
					if (GUILayout.Button(
						    new GUIContent("Debug selected entity →",
							    "Per-entity state lives in the Scene View overlay. This selects the object and focuses a Scene View so it surfaces."),
						    EditorStyles.toolbarButton))
					{
						EntityDebuggerHandoff.Reveal(selection);
					}
				}

				if (GUILayout.Button(new GUIContent("Copy Info", "Copy the visible section as text."),
					    EditorStyles.toolbarButton))
				{
					_copyPending = true;
				}
			}
		}

		private void DrawSectionList(System.Collections.Generic.IReadOnlyList<IWorldDebugSection> sections)
		{
			using (new EditorGUILayout.VerticalScope(GUILayout.Width(150f)))
			{
				_listScroll = EditorGUILayout.BeginScrollView(_listScroll);

				for (var i = 0; i < sections.Count; i++)
				{
					var section = sections[i];
					var selected = section.Title == _selectedTitle;

					// Unavailable sections stay listed and greyed: "the Level section is empty" is a much
					// more useful reading than "there is no Level section".
					var prev = GUI.color;
					if (!section.IsAvailable)
					{
						GUI.color = new Color(1f, 1f, 1f, 0.5f);
					}

					if (GUILayout.Toggle(selected, section.Title, EditorStyles.miniButton) && !selected)
					{
						_selectedTitle = section.Title;
						SessionState.SetString(SelectedSectionKey, _selectedTitle);
					}

					GUI.color = prev;
				}

				EditorGUILayout.EndScrollView();
			}
		}

		private void DrawDetail(System.Collections.Generic.IReadOnlyList<IWorldDebugSection> sections)
		{
			var section = Resolve(sections);
			if (section == null)
			{
				return;
			}

			using (new EditorGUILayout.VerticalScope(EditorStyles.helpBox))
			{
				EditorGUILayout.LabelField(section.Title, EditorStyles.boldLabel);

				if (!section.IsAvailable)
				{
					EditorGUILayout.HelpBox(
						string.IsNullOrEmpty(section.UnavailableReason)
							? "Not available right now."
							: section.UnavailableReason,
						MessageType.Info);
					return;
				}

				// DebugDrawKit mirrors every draw call into ActiveRecorder when set, so "copy what I'm
				// looking at" needs no separate serialization path — same trick the entity overlay uses.
				if (_copyPending && Event.current.type == EventType.Layout)
				{
					_copyActive = true;
					var sb = new StringBuilder();
					sb.AppendLine("# " + section.Title);
					DebugDrawKit.ActiveRecorder = sb;
				}

				_detailScroll = EditorGUILayout.BeginScrollView(_detailScroll);

				var prevLabelWidth = EditorGUIUtility.labelWidth;
				EditorGUIUtility.labelWidth = 180f;
				section.DrawDetail();
				EditorGUIUtility.labelWidth = prevLabelWidth;

				EditorGUILayout.EndScrollView();

				if (_copyActive && Event.current.type == EventType.Repaint)
				{
					var result = DebugDrawKit.ActiveRecorder?.ToString();
					DebugDrawKit.ActiveRecorder = null;
					_copyPending = false;
					_copyActive = false;
					if (!string.IsNullOrEmpty(result))
					{
						EditorGUIUtility.systemCopyBuffer = result;
						Debug.Log($"Copied '{section.Title}' state to clipboard.");
					}
				}
			}
		}

		private IWorldDebugSection Resolve(System.Collections.Generic.IReadOnlyList<IWorldDebugSection> sections)
		{
			for (var i = 0; i < sections.Count; i++)
			{
				if (sections[i].Title == _selectedTitle)
				{
					return sections[i];
				}
			}

			_selectedTitle = sections[0].Title;
			return sections[0];
		}
	}
}
#endif
