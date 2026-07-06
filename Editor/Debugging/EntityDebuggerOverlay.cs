#if UNITY_EDITOR
using System.Collections.Generic;
using UnityEditor;
using UnityEditor.Overlays;
using UnityEngine;
using UnityEngine.UIElements;

namespace FoundationPlatform.Editor.Utilities.Debugging
{
	/// <summary>
	///  Scene-View overlay that folds the project's per-entity debugger detail panes into the scene,
	///  beside the object they describe. Auto-shows (<see cref="ITransientOverlay"/>) whenever the
	///  selected GameObject matches at least one registered <see cref="IEntityDebugSection"/>, so a
	///  designer clicks a unit in Play mode and sees its live Character / AI / GAS / Combat state
	///  without opening — or re-selecting into — six separate floating debugger windows. Each section
	///  header carries an "Open" button to the matching full window for the multi-entity list + log.
	///
	///  The section is the single source of the detail drawing (the full <see cref="FrameworkDebuggerWindow{TEntity}"/>
	///  delegates to it); this overlay is the fast in-context "what's up with THIS object" glance.
	/// </summary>
	[Overlay(typeof(SceneView), "Entity Debugger", true)]
	public sealed class EntityDebuggerOverlay : Overlay, ITransientOverlay
	{
		private const long RepaintIntervalMs = 150;

		private readonly Dictionary<string, bool> _folds = new();
		private Vector2 _scrollPos;
		private bool _copyPending;
		private bool _copyActive;

		private IMGUIContainer _imgui;

		// Computed standalone (not cached from CreatePanelContent) — a transient overlay does not build
		// its panel content until it first becomes visible, so visibility must not depend on it.
		public bool visible => EntityDebugSectionRegistry.HasApplicable(Selection.activeGameObject);

		public override void OnCreated()
		{
			Selection.selectionChanged += OnSelectionChanged;
		}

		public override void OnWillBeDestroyed()
		{
			Selection.selectionChanged -= OnSelectionChanged;
			base.OnWillBeDestroyed();
		}

		private void OnSelectionChanged()
		{
			_imgui?.MarkDirtyRepaint();
			// Nudge the Scene View so the transient-overlay visibility re-evaluates immediately.
			SceneView.RepaintAll();
		}

		public override VisualElement CreatePanelContent()
		{
			var root = new VisualElement { name = "EntityDebuggerOverlayRoot" };
			root.style.minWidth = 300;
			root.style.maxWidth = 420;
			root.style.maxHeight = 640;
			root.style.overflow = Overflow.Hidden;

			_imgui = new IMGUIContainer(DrawIMGUI);
			_imgui.style.flexGrow = 1;
			_imgui.style.flexShrink = 1;
			root.Add(_imgui);

			// Live state (speeds, weights, cooldowns) changes without a selection event, so nudge a
			// repaint on a timer while the overlay is shown. Stops automatically when the panel is torn down.
			root.schedule.Execute(() => _imgui?.MarkDirtyRepaint()).Every(RepaintIntervalMs);
			return root;
		}

		private void DrawIMGUI()
		{
			var go = Selection.activeGameObject;
			if (go == null)
			{
				EditorGUILayout.LabelField("Select a GameObject.", EditorStyles.miniLabel);
				return;
			}

			if (_copyPending)
			{
				_copyActive = true;
				if (Event.current.type == EventType.Layout)
				{
					var sb = new System.Text.StringBuilder();
					sb.AppendLine($"# Entity: {go.name}");
					DebugDrawKit.ActiveRecorder = sb;
				}
			}

			EditorGUILayout.BeginHorizontal();
			EditorGUILayout.LabelField(go.name, EditorStyles.boldLabel);
			if (GUILayout.Button("Copy Info", EditorStyles.miniButton, GUILayout.Width(75f)))
			{
				_copyPending = true;
			}
			EditorGUILayout.EndHorizontal();

			if (!Application.isPlaying)
			{
				EditorGUILayout.LabelField("Enter Play mode for live state.", EditorStyles.miniLabel);
			}

			_scrollPos = EditorGUILayout.BeginScrollView(_scrollPos);

			var drew = false;
			var sections = EntityDebugSectionRegistry.Sections;
			var prevLabelWidth = EditorGUIUtility.labelWidth;
			EditorGUIUtility.labelWidth = 160f;

			for (var i = 0; i < sections.Count; i++)
			{
				var section = sections[i];
				if (!section.AppliesTo(go))
				{
					continue;
				}

				drew = true;

				EditorGUILayout.BeginHorizontal();
				var open = !_folds.TryGetValue(section.Title, out var v) || v;
				open = EditorGUILayout.Foldout(open, section.Title, true);
				_folds[section.Title] = open;
				GUILayout.FlexibleSpace();
				if (GUILayout.Button("Open", EditorStyles.miniButton, GUILayout.Width(46f)))
				{
					section.OpenFullWindow();
				}

				EditorGUILayout.EndHorizontal();

				if (open)
				{
					EditorGUI.indentLevel++;
					section.DrawDetail(go);
					EditorGUI.indentLevel--;
				}
			}

			EditorGUIUtility.labelWidth = prevLabelWidth;

			if (!drew)
			{
				EditorGUILayout.LabelField("No debug sections for this object.", EditorStyles.miniLabel);
			}

			EditorGUILayout.EndScrollView();

			if (_copyActive)
			{
				if (Event.current.type == EventType.Repaint)
				{
					var result = DebugDrawKit.ActiveRecorder?.ToString();
					DebugDrawKit.ActiveRecorder = null;
					_copyPending = false;
					_copyActive = false;
					if (!string.IsNullOrEmpty(result))
					{
						EditorGUIUtility.systemCopyBuffer = result;
						Debug.Log($"Copied debug info for entity '{go.name}' to clipboard!");
					}
				}
			}
		}

	}
}
#endif
