#if UNITY_EDITOR
using AetherNexus.FoundationPlatform.Animation;
using AetherNexus.FoundationPlatform.Editor.Utilities.Debugging;
using AetherNexus.FoundationPlatform.AetherInspector.Editor;
using UnityEditor;
using UnityEngine;

namespace AetherNexus.FoundationPlatform.Editor.Utilities
{
	/// <summary>
	///  Live play-mode control surface for a <see cref="PlayableGraphBridge"/>. Beyond monitoring, this lets
	///  designers drive the graph directly: scrub, pause, solo and stop individual states, and tune per-state
	///  and global playback speed. For authoring/previewing whole AnimationSets and transitions, use the
	///  <see cref="AnimationTestBenchWindow"/> (button below).
	/// </summary>
	[CustomEditor(typeof(PlayableGraphBridge))]
	public class PlayableGraphBridgeEditor : AetherInspectorEditor
	{
		private bool _layersSectionExpanded = true;
		private float _testFadeDuration = 0.25f;

		public override void OnInspectorGUI()
		{
			base.OnInspectorGUI();

			var bridge = (PlayableGraphBridge)target;
			if (bridge == null)
				return;

			EditorGUILayout.Space(5);
			if (GUILayout.Button("Open Animation Test Bench", GUILayout.Height(24)))
				AnimationTestBenchWindow.Open();

			if (!EditorApplication.isPlaying)
			{
				EditorGUILayout.Space(5);
				EditorGUILayout.HelpBox("Graph details are only available during Play Mode. Use the Test Bench for offline clip/pose preview.", MessageType.Info);
				return;
			}

			if (!bridge.IsGraphInitialized || !bridge.IsValid)
			{
				EditorGUILayout.Space(5);
				EditorGUILayout.HelpBox("PlayableGraph is not initialized.", MessageType.Warning);
				return;
			}

			EditorGUILayout.Space(5);
			DebugDrawKit.Title("Playable Graph Bridge", "Runtime Control");

			// Global graph speed + quick pause / resume.
			EditorGUILayout.BeginHorizontal();
			EditorGUI.BeginChangeCheck();
			float newSpeed = EditorGUILayout.Slider("Graph Speed", bridge.Speed, 0f, 3f);
			if (EditorGUI.EndChangeCheck())
				bridge.Speed = newSpeed;
			if (GUILayout.Button("Pause", GUILayout.Width(58)))
				bridge.Speed = 0f;
			if (GUILayout.Button("1x", GUILayout.Width(34)))
				bridge.Speed = 1f;
			EditorGUILayout.EndHorizontal();

			_testFadeDuration = EditorGUILayout.Slider(
				new GUIContent("Test Fade (s)", "Fade duration used by the per-state Stop button below."),
				_testFadeDuration, 0f, 1f);

			if (bridge.Layers == null || bridge.Layers.Count == 0)
			{
				EditorGUILayout.HelpBox("No layers initialized in the graph.", MessageType.Info);
				return;
			}

			_layersSectionExpanded = DebugDrawKit.BeginSection($"Graph Layers ({bridge.Layers.Count})", _layersSectionExpanded);
			if (_layersSectionExpanded)
			{
				for (int i = 0; i < bridge.Layers.Count; i++)
					DrawLayer(bridge.Layers[i]);
			}
			DebugDrawKit.EndSection();

			// Force repaint while playing so progress bars update dynamically.
			Repaint();
		}

		private void DrawLayer(PlayableLayer layer)
		{
			if (layer == null) return;

			EditorGUILayout.BeginVertical("box");

			// Layer header: name + live weight bar.
			EditorGUILayout.BeginHorizontal();
			EditorGUILayout.LabelField($"Layer {layer.Index}", EditorStyles.boldLabel, GUILayout.Width(70));
			EditorGUI.BeginChangeCheck();
			float newLayerWeight = EditorGUILayout.Slider(layer.Weight, 0f, 1f);
			if (EditorGUI.EndChangeCheck())
				layer.Weight = newLayerWeight;
			EditorGUILayout.EndHorizontal();

			var activeStates = layer.ActiveStates;
			if (activeStates == null || activeStates.Count == 0)
			{
				EditorGUILayout.LabelField("  (no active states)", EditorStyles.miniLabel);
				EditorGUILayout.EndVertical();
				EditorGUILayout.Space(5);
				return;
			}

			for (int j = 0; j < activeStates.Count; j++)
				DrawState(layer, activeStates[j]);

			EditorGUILayout.EndVertical();
			EditorGUILayout.Space(5);
		}

		private void DrawState(PlayableLayer layer, PlayableLayer.ActiveState active)
		{
			if (active?.State == null) return;
			var state = active.State;

			string clipName = state switch
			{
				ClipState clip when clip.Clip != null => clip.Clip.name,
				MixerState mixer => mixer.GetType().Name,
				_ => "Unknown State",
			};

			bool isCurrent = layer.CurrentState == state;
			string indicator = isCurrent ? "▶ " : "   ";
			bool paused = state.IsValid && Mathf.Approximately(state.Speed, 0f);

			// Progress bar (loops within [0,1)).
			float progress = state.Length > 0f ? (state.Time / state.Length) % 1f : 0f;
			if (progress < 0f) progress += 1f;
			Color barColor = isCurrent ? DebugDrawKit.Fill : DebugDrawKit.Neutral;
			string barValue = $"{state.Time:F2}s / {state.Length:F2}s  •  w {state.Weight * 100f:F0}%  •  →{active.TargetWeight * 100f:F0}%";
			DebugDrawKit.Bar($"{indicator}{clipName}", progress, barValue, barColor);

			// Scrub time.
			EditorGUI.BeginChangeCheck();
			float newTime = EditorGUILayout.Slider("  Scrub", state.Time, 0f, Mathf.Max(0.0001f, state.Length));
			if (EditorGUI.EndChangeCheck())
			{
				state.Time = newTime;
				layer.Bridge.Evaluate();
			}

			// Per-state controls row.
			EditorGUILayout.BeginHorizontal();
			GUILayout.Space(14);

			EditorGUILayout.LabelField("Speed", GUILayout.Width(42));
			EditorGUI.BeginChangeCheck();
			float newStateSpeed = EditorGUILayout.FloatField(state.Speed, GUILayout.Width(48));
			if (EditorGUI.EndChangeCheck())
				state.Speed = newStateSpeed;

			if (GUILayout.Button(paused ? "Resume" : "Pause", GUILayout.Width(64)))
				state.Speed = paused ? 1f : 0f;

			if (GUILayout.Button(new GUIContent("Solo", "Weight this state to 1 and fade siblings on this layer to 0."), GUILayout.Width(48)))
				SoloState(layer, active);

			if (GUILayout.Button(new GUIContent("Stop", "Fade this state out using the Test Fade duration."), GUILayout.Width(48)))
			{
				active.TargetWeight = 0f;
				active.FadeSpeed = _testFadeDuration > 0f ? 1f / _testFadeDuration : 1000f;
			}

			EditorGUILayout.EndHorizontal();
			EditorGUILayout.Space(3);
		}

		private static void SoloState(PlayableLayer layer, PlayableLayer.ActiveState solo)
		{
			foreach (var other in layer.ActiveStates)
			{
				other.TargetWeight = other == solo ? 1f : 0f;
				other.FadeSpeed = 1000f;
			}
		}
	}
}
#endif
