#if UNITY_EDITOR
using System.Collections.Generic;
using AetherNexus.FoundationPlatform.DebugX;
using UnityEditor;
using UnityEngine;

namespace AetherNexus.FoundationPlatform.Editor.Utilities.Debugging
{
	using DebugX = DebugX.DebugX;
	
	/// <summary>
	///  Base class for the project's "single pane" debugger windows (see the AI Debugger). Provides the
	///  shared skeleton — an optional gizmo toggle toolbar, an auto-refreshing color-coded entity list
	///  split into sections, a draggable divider, a detail pane for the current selection, and a live
	///  event log — so a concrete debugger only overrides how to collect its entities, label/color them,
	///  and draw their detail.
	///
	///  <typeparamref name="TEntity"/> is the common base of everything the window lists (e.g. a shared
	///  MonoBehaviour type). Windows that list several unrelated types use that shared base and switch
	///  on the concrete type inside <see cref="DrawDetail"/> — as the AI Debugger does for pawns vs.
	///  commanders.
	/// </summary>
	public abstract class FrameworkDebuggerWindow<TEntity> : EditorWindow where TEntity : Object
	{
		/// <summary>A titled group of entities in the left-hand list (e.g. "Pawns", "Commanders").</summary>
		protected readonly struct Section
		{
			public readonly string Heading;
			public readonly IReadOnlyList<TEntity> Entities;

			public Section(string heading, IReadOnlyList<TEntity> entities)
			{
				Heading = heading;
				Entities = entities;
			}
		}

		private const float RowHeight = 16f;
		private const float SplitterWidth = 5f;
		private const float MinListWidth = 120f;
		private const float MinDetailWidth = 220f;

		private static readonly Color SelectionColor = new(0.24f, 0.48f, 0.90f, 0.55f);
		private static readonly Color SplitterColor = new(0.12f, 0.12f, 0.12f);
		private static readonly Color SplitterLine = new(0.35f, 0.35f, 0.35f);

		private readonly List<Section> _sections = new();

		/// <summary>Live event log; hidden unless <see cref="LogTitle"/> is non-empty.</summary>
		protected readonly EventLogView Log = new();

		private TEntity _selected;
		private Vector2 _listScroll;
		private Vector2 _detailScroll;
		private float _listWidth = -1f;
		private bool _draggingSplitter;
		private bool _copyPending;
		private bool _copyActive;

		private GUIStyle _rowStyle;
		private GUIStyle _headerStyle;

		protected TEntity Selected => _selected;

		// --- Overridable surface --------------------------------------------------------------------

		/// <summary>Fill the buffer with the sections to list. Called every OnGUI, so keep it cheap
		/// (a couple of <c>FindObjectsByType</c> calls into reused buffers).</summary>
		protected abstract void CollectSections(List<Section> buffer);

		/// <summary>Button label for one entity in the list.</summary>
		protected abstract string EntityLabel(TEntity entity);

		/// <summary>Draw the detail pane for the current selection.</summary>
		protected abstract void DrawDetail(TEntity entity);

		/// <summary>Tint for an entity's list row text (typically its state color). White by default.</summary>
		protected virtual Color EntityColor(TEntity entity) => Color.white;

		/// <summary>Called when the selection changes (e.g. to focus a scene gizmo renderer).</summary>
		protected virtual void OnSelectionChanged(TEntity entity) { }

		/// <summary>Optional gizmo toggle toolbar drawn above the list/detail split. No-op by default.</summary>
		protected virtual void DrawGizmoToolbar() { }

		/// <summary>Default left column width in pixels (used until the user drags the divider).</summary>
		protected virtual float ListWidth => 190f;

		/// <summary>Placeholder shown in the detail pane when nothing is selected.</summary>
		protected virtual string EmptySelectionHint => "Select an entity.";

		/// <summary>Title for the live event log, or null/empty to hide it.</summary>
		protected virtual string LogTitle => null;

		// --- Skeleton -------------------------------------------------------------------------------

		protected void SetSelected(TEntity entity)
		{
			if (ReferenceEquals(_selected, entity))
			{
				return;
			}

			_selected = entity;
			OnSelectionChanged(entity);
		}

		protected virtual void OnInspectorUpdate() => Repaint();

		protected virtual void OnGUI()
		{
			EnsureStyles();
			DrawGizmoToolbar();

			_sections.Clear();
			CollectSections(_sections);

			EditorGUILayout.BeginHorizontal();
			DrawList();
			DrawSplitter();
			DrawDetailPane();
			EditorGUILayout.EndHorizontal();

			if (!string.IsNullOrEmpty(LogTitle))
			{
				Log.Draw(LogTitle);
			}
		}

		private void DrawList()
		{
			EditorGUILayout.BeginVertical(GUILayout.Width(CurrentListWidth));
			_listScroll = EditorGUILayout.BeginScrollView(_listScroll);

			foreach (var section in _sections)
			{
				var entities = section.Entities;
				var count = entities?.Count ?? 0;
				EditorGUILayout.LabelField($"{section.Heading} ({count})", _headerStyle);

				for (var i = 0; i < count; i++)
				{
					var entity = entities[i];
					if (entity != null)
					{
						DrawEntityRow(entity);
					}
				}

				GUILayout.Space(4f);
			}

			EditorGUILayout.EndScrollView();
			EditorGUILayout.EndVertical();
		}

		private void DrawEntityRow(TEntity entity)
		{
			var rect = GUILayoutUtility.GetRect(0f, RowHeight, GUILayout.ExpandWidth(true));
			var isSelected = ReferenceEquals(entity, _selected);

			if (isSelected && Event.current.type == EventType.Repaint)
			{
				EditorGUI.DrawRect(rect, SelectionColor);
			}

			var e = Event.current;
			if (e.type == EventType.MouseDown && e.button == 0 && rect.Contains(e.mousePosition))
			{
				SetSelected(entity);
				e.Use();
				Repaint();
			}

			var labelRect = new Rect(rect.x + 4f, rect.y, rect.width - 6f, rect.height);
			var previous = GUI.color;
			GUI.color = EntityColor(entity);
			GUI.Label(labelRect, EntityLabel(entity), _rowStyle);
			GUI.color = previous;
		}

		private void DrawSplitter()
		{
			var rect = GUILayoutUtility.GetRect(SplitterWidth, SplitterWidth, GUILayout.Width(SplitterWidth), GUILayout.ExpandHeight(true));
			if (Event.current.type == EventType.Repaint)
			{
				EditorGUI.DrawRect(rect, SplitterColor);
				var line = new Rect(rect.x + rect.width * 0.5f - 0.5f, rect.y, 1f, rect.height);
				EditorGUI.DrawRect(line, SplitterLine);
			}

			EditorGUIUtility.AddCursorRect(rect, MouseCursor.ResizeHorizontal);

			var e = Event.current;
			switch (e.type)
			{
				case EventType.MouseDown when e.button == 0 && rect.Contains(e.mousePosition):
					_draggingSplitter = true;
					e.Use();
					break;
				case EventType.MouseDrag when _draggingSplitter:
					_listWidth = Mathf.Clamp(e.mousePosition.x, MinListWidth, position.width - MinDetailWidth);
					e.Use();
					Repaint();
					break;
				case EventType.MouseUp when _draggingSplitter:
					_draggingSplitter = false;
					EditorPrefs.SetFloat(ListWidthPrefKey, _listWidth);
					e.Use();
					break;
			}
		}

		private void DrawDetailPane()
		{
			EditorGUILayout.BeginVertical(EditorStyles.helpBox);

			if (_selected != null)
			{
				if (_copyPending)
				{
					_copyActive = true;
					if (Event.current.type == EventType.Layout)
					{
						var sb = new System.Text.StringBuilder();
						sb.AppendLine($"# Entity: {_selected.name}");
						DebugDrawKit.ActiveRecorder = sb;
					}
				}

				EditorGUILayout.BeginHorizontal();
				GUILayout.FlexibleSpace();
				if (GUILayout.Button("Copy Info", EditorStyles.miniButton, GUILayout.Width(75f)))
				{
					_copyPending = true;
				}
				EditorGUILayout.EndHorizontal();
			}

			_detailScroll = EditorGUILayout.BeginScrollView(_detailScroll);

			if (_selected != null)
			{
				DrawDetail(_selected);
			}
			else
			{
				EditorGUILayout.LabelField(EmptySelectionHint);
			}

			EditorGUILayout.EndScrollView();

			if (_copyActive && _selected != null)
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
						DebugX.Logger(LogChannels.Editor).Info("Copied debug info for entity '{EntityName}' to clipboard!", _selected.name);
					}
				}
			}

			EditorGUILayout.EndVertical();
		}

		private float CurrentListWidth
		{
			get
			{
				if (_listWidth < 0f)
				{
					_listWidth = EditorPrefs.GetFloat(ListWidthPrefKey, ListWidth);
				}

				return Mathf.Clamp(_listWidth, MinListWidth, Mathf.Max(MinListWidth, position.width - MinDetailWidth));
			}
		}

		private string ListWidthPrefKey => $"FrameworkDebugger.{GetType().Name}.ListWidth";

		private void EnsureStyles()
		{
			if (_rowStyle != null)
			{
				return;
			}

			_rowStyle = new GUIStyle(EditorStyles.miniLabel)
			{
				alignment = TextAnchor.MiddleLeft,
				clipping = TextClipping.Clip,
				padding = new RectOffset(2, 2, 0, 0),
			};
			_headerStyle = new GUIStyle(EditorStyles.miniBoldLabel)
			{
				alignment = TextAnchor.MiddleLeft,
			};
		}
	}
}
#endif
