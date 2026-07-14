#if UNITY_EDITOR
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace AetherNexus.FoundationPlatform.Editor.Utilities.Debugging
{
	/// <summary>
	///  A rolling, newest-first text log with a header and Clear button — the "live transitions" panel
	///  every debugger wants. Framework windows wire their own EventBus subscription and call
	///  <see cref="Push"/>; this class only owns buffering + rendering, so it stays event-type agnostic.
	/// </summary>
	public sealed class EventLogView
	{
		private readonly List<string> _entries = new();
		private readonly int _maxEntries;

		public EventLogView(int maxEntries = 40) => _maxEntries = maxEntries;

		public void Push(string entry)
		{
			_entries.Insert(0, entry);
			if (_entries.Count > _maxEntries)
			{
				_entries.RemoveRange(_maxEntries, _entries.Count - _maxEntries);
			}
		}

		public void Clear() => _entries.Clear();

		public void Draw(string title, string emptyHint = "(none yet)")
		{
			EditorGUILayout.Space();
			EditorGUILayout.BeginHorizontal();
			EditorGUILayout.LabelField(title, EditorStyles.boldLabel);
			if (GUILayout.Button("Clear", EditorStyles.miniButton, GUILayout.Width(60f)))
			{
				Clear();
			}

			EditorGUILayout.EndHorizontal();

			if (_entries.Count == 0)
			{
				EditorGUILayout.LabelField(emptyHint, EditorStyles.miniLabel);
				return;
			}

			foreach (var entry in _entries)
			{
				EditorGUILayout.LabelField(entry, EditorStyles.miniLabel);
			}
		}
	}
}
#endif
