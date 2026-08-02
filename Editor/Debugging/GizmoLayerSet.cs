#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace AetherNexus.FoundationPlatform.Editor.Utilities.Debugging
{
	/// <summary>
	///  A named set of scene-gizmo visibility toggles, persisted per-key in EditorPrefs so designers
	///  keep their preferences across sessions. Replaces hand-rolled GetPref/SetPref bool properties:
	///  a framework's gizmo renderer declares its layers once, reads them via the indexer, and the
	///  debugger window renders the whole toggle toolbar with a single <see cref="DrawToolbar"/> call.
	///
	///  The EditorPrefs key for a layer is <c>prefPrefix + key</c>, so an existing renderer can migrate
	///  onto this without losing designers' saved preferences as long as the prefix and keys match.
	/// </summary>
	public sealed class GizmoLayerSet
	{
		private readonly struct Layer
		{
			public readonly string Key;
			public readonly string Label;
			public readonly bool Default;

			public Layer(string key, string label, bool def)
			{
				Key = key;
				Label = label;
				Default = def;
			}
		}

		private readonly string _prefix;
		private readonly List<Layer> _layers = new();

		public GizmoLayerSet(string prefPrefix) => _prefix = prefPrefix;

		/// <summary>Declare a layer. Fluent, so sets can be built inline.</summary>
		public GizmoLayerSet Add(string key, string label, bool defaultValue)
		{
			_layers.Add(new Layer(key, label, defaultValue));
			return this;
		}

		/// <summary>Declares a layer defaulting to visible.</summary>
		public GizmoLayerSet Add(string key, string label) => Add(key, label, true);

		public bool this[string key] => Get(key);

		public bool Get(string key) => EditorPrefs.GetBool(_prefix + key, DefaultOf(key));

		public void Set(string key, bool value) => EditorPrefs.SetBool(_prefix + key, value);

		/// <summary>Draws every layer as a mini-button toggle in a horizontal row. Invokes
		/// <paramref name="onChanged"/> after any flip (typically <c>SceneView.RepaintAll</c>).</summary>
		public void DrawToolbar(Action onChanged)
		{
			EditorGUILayout.BeginHorizontal();
			foreach (var layer in _layers)
			{
				var current = Get(layer.Key);
				var next = GUILayout.Toggle(current, layer.Label, EditorStyles.miniButton);
				if (next != current)
				{
					Set(layer.Key, next);
					onChanged?.Invoke();
				}
			}

			EditorGUILayout.EndHorizontal();
		}

		/// <summary>Draws the toolbar with no change callback.</summary>
		public void DrawToolbar() => DrawToolbar(null);

		private bool DefaultOf(string key)
		{
			for (var i = 0; i < _layers.Count; i++)
			{
				if (_layers[i].Key == key)
				{
					return _layers[i].Default;
				}
			}

			return true;
		}
	}
}
#endif
