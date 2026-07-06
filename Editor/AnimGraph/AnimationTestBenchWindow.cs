#if UNITY_EDITOR
using System.Collections.Generic;
using FoundationPlatform.Animation;
using FoundationPlatform;
using FoundationPlatform.Editor.Utilities.Debugging;
using FoundationPlatform.Utilities.Menus;
using UnityEditor;
using UnityEngine;

namespace FoundationPlatform.Editor.Utilities
{
	/// <summary>
	///  Designer-facing bench for previewing AnimationSets and transitions.
	///
	///  <para><b>Offline (edit mode):</b> samples the selected entry's clip onto the target rig via
	///  <see cref="AnimationMode"/> — non-destructive pose preview, scrub, and real-time single-clip
	///  playback in the Scene view without entering Play Mode.</para>
	///
	///  <para><b>Live (play mode):</b> drives the real <see cref="PlayableGraphBridge"/> through
	///  <see cref="AnimatorBridgeBase"/> — plays entries, authored sequences, cross-fades with a chosen
	///  fade duration, and scrubs the active state, so the full transition/root-motion flow can be tested.</para>
	/// </summary>
	public class AnimationTestBenchWindow : EditorWindow
	{
		[MenuItem(MenuPaths.Diagnostics.AnimationTestBench, false, MenuPriorities.Diagnostics)]
		public static void Open()
		{
			var window = GetWindow<AnimationTestBenchWindow>("Anim Test Bench");
			window.minSize = new Vector2(360, 420);
			window.Show();
		}

		private GameObject _targetRig;
		private AnimatorBridgeBase _bridge;
		private Animator _animator;

		private AnimationSet _set;
		private readonly List<KeyValuePair<string, AnimationSetEntry>> _entries = new();
		private int _entryIndex;

		private float _normalizedTime;
		private float _fadeDuration = 0.25f;
		private float _speed = 1f;

		// Offline playback clock.
		private bool _offlinePlaying;
		private double _lastEditorTime;

		// Last state started via the bench (plain PlayFromSet doesn't set ActiveSequenceState).
		private PlayableState _liveState;

		// Event authoring / flash.
		private string[] _catalogNames = System.Array.Empty<string>();
		private string _addEventName = "";
		private double[] _eventFlashUntil = System.Array.Empty<double>();
		private float _prevMarkerNormTime;
		private const double FlashDuration = 0.35;

		private Vector2 _scroll;

		private void OnEnable()
		{
			EditorApplication.update += OnEditorUpdate;
			EditorApplication.playModeStateChanged += OnPlayModeChanged;
			RefreshCatalog();
			ResolveFromSelection();
		}

		private void RefreshCatalog()
		{
			_catalogNames = new List<string>(AnimationEventNameCatalog.AllNames()).ToArray();
			// Prefill so a blank name can never be committed by accident.
			if (string.IsNullOrWhiteSpace(_addEventName) && _catalogNames.Length > 0)
				_addEventName = _catalogNames[0];
		}

		private void ResizeFlash()
		{
			int n = CurrentEntry?.clip?.events?.Length ?? 0;
			if (_eventFlashUntil.Length != n)
				_eventFlashUntil = new double[n];
		}

		private void OnDisable()
		{
			EditorApplication.update -= OnEditorUpdate;
			EditorApplication.playModeStateChanged -= OnPlayModeChanged;
			StopOfflineSampling();
		}

		private void OnPlayModeChanged(PlayModeStateChange change)
		{
			// AnimationMode must never survive into Play Mode.
			StopOfflineSampling();
			_bridge = null;
			_animator = null;
			ResolveFromSelection();
		}

		private void OnSelectionChange()
		{
			if (_targetRig == null)
				ResolveFromSelection();
		}

		// ─────────────────────────────────────────────────────────────────────

		private void ResolveFromSelection()
		{
			if (Selection.activeGameObject != null)
				SetTarget(Selection.activeGameObject);
		}

		private void SetTarget(GameObject go)
		{
			_targetRig = go;
			_bridge = go != null ? go.GetComponentInChildren<AnimatorBridgeBase>() : null;
			_animator = go != null ? go.GetComponentInChildren<Animator>() : null;

			// Prefer the bridge's own sets when available.
			if (_set == null && _bridge != null && _bridge.AnimationSets != null && _bridge.AnimationSets.Count > 0)
				SetSet(_bridge.AnimationSets[0]);
		}

		private void SetSet(AnimationSet set)
		{
			_set = set;
			RebuildEntries();
		}

		private void RebuildEntries()
		{
			_entries.Clear();
			if (_set == null)
				return;

			foreach (var kv in _set.GetResolvedEntries())
			{
				if (kv.Value?.clip?.Clip != null)
					_entries.Add(kv);
			}
			_entries.Sort((a, b) => string.CompareOrdinal(a.Key, b.Key));
			_entryIndex = Mathf.Clamp(_entryIndex, 0, Mathf.Max(0, _entries.Count - 1));
			ResizeFlash();
		}

		private AnimationSetEntry CurrentEntry =>
			(_entries.Count > 0 && _entryIndex >= 0 && _entryIndex < _entries.Count) ? _entries[_entryIndex].Value : null;

		private AnimationClip CurrentClip => CurrentEntry?.clip?.Clip;

		// ─────────────────────────────────────────────────────────────────────

		private void OnGUI()
		{
			_scroll = EditorGUILayout.BeginScrollView(_scroll);

			DebugDrawKit.Title("Animation Test Bench", EditorApplication.isPlaying ? "Live (Play Mode)" : "Offline (Edit Mode)");

			DrawTargetSection();
			if (_targetRig == null || _animator == null)
			{
				EditorGUILayout.HelpBox("Select a rig with an Animator in the scene, or assign one above.", MessageType.Info);
				EditorGUILayout.EndScrollView();
				return;
			}

			DrawSetSection();
			if (_set == null)
			{
				EditorGUILayout.HelpBox("Assign an AnimationSet to preview its entries.", MessageType.Info);
				EditorGUILayout.EndScrollView();
				return;
			}

			if (_entries.Count == 0)
			{
				EditorGUILayout.HelpBox("This set (and its parents) contain no entries with an assigned clip.", MessageType.Warning);
				EditorGUILayout.EndScrollView();
				return;
			}

			DrawEntrySection();

			if (EditorApplication.isPlaying)
				DrawLiveControls();
			else
				DrawOfflineControls();

			EditorGUILayout.EndScrollView();
		}

		private void DrawTargetSection()
		{
			EditorGUILayout.Space(4);
			EditorGUI.BeginChangeCheck();
			var go = (GameObject)EditorGUILayout.ObjectField("Target Rig", _targetRig, typeof(GameObject), true);
			if (EditorGUI.EndChangeCheck())
			{
				StopOfflineSampling();
				SetTarget(go);
			}

			DebugDrawKit.Row("Animator", _animator != null ? _animator.name : "(none)",
				_animator != null ? DebugDrawKit.RowTone.Positive : DebugDrawKit.RowTone.Negative);
			DebugDrawKit.Row("Bridge", _bridge != null ? _bridge.GetType().Name : "(none — offline only)",
				_bridge != null ? DebugDrawKit.RowTone.Positive : DebugDrawKit.RowTone.Neutral);
		}

		private void DrawSetSection()
		{
			EditorGUILayout.Space(4);

			// Quick-pick from the bridge's registered sets (live-playable), plus a free asset field.
			if (_bridge != null && _bridge.AnimationSets != null && _bridge.AnimationSets.Count > 0)
			{
				var sets = _bridge.AnimationSets;
				var names = new string[sets.Count];
				int current = -1;
				for (int i = 0; i < sets.Count; i++)
				{
					names[i] = sets[i] != null ? sets[i].name : "(null)";
					if (sets[i] == _set) current = i;
				}
				int picked = EditorGUILayout.Popup("Registered Set", current, names);
				if (picked >= 0 && picked < sets.Count && sets[picked] != _set)
					SetSet(sets[picked]);
			}

			EditorGUI.BeginChangeCheck();
			var set = (AnimationSet)EditorGUILayout.ObjectField("AnimationSet", _set, typeof(AnimationSet), false);
			if (EditorGUI.EndChangeCheck())
				SetSet(set);
		}

		private void DrawEntrySection()
		{
			EditorGUILayout.Space(4);

			var labels = new string[_entries.Count];
			for (int i = 0; i < _entries.Count; i++)
			{
				var e = _entries[i].Value;
				string clipName = e.clip.Clip != null ? e.clip.Clip.name : "?";
				string loop = e.clip.IsLooping ? " ↻" : "";
				labels[i] = $"{_entries[i].Key}  ({clipName}){loop}";
			}

			EditorGUI.BeginChangeCheck();
			int idx = EditorGUILayout.Popup("Entry", _entryIndex, labels);
			if (EditorGUI.EndChangeCheck())
			{
				_entryIndex = idx;
				_normalizedTime = 0f;
				ResizeFlash();
				if (!EditorApplication.isPlaying)
					SampleOffline();
			}

			var entry = CurrentEntry;
			if (entry != null)
			{
				DebugDrawKit.Row("Fade / Speed", $"{entry.clip.FadeDuration:F2}s  •  {entry.clip.Speed:F2}x", DebugDrawKit.RowTone.Neutral);
				DebugDrawKit.Row("Mask / Layer", $"{entry.mask}  •  L{entry.layerIndex}", DebugDrawKit.RowTone.Neutral);
				DebugDrawKit.Row("Root Motion", entry.rootMotionMode.ToString(),
					entry.rootMotionMode != RootMotionMode.None ? DebugDrawKit.RowTone.Positive : DebugDrawKit.RowTone.Neutral);
				bool hasSeq = entry.link != null && entry.link.HasNext;
				DebugDrawKit.Row("Sequence", hasSeq ? "chains to next" : "standalone",
					hasSeq ? DebugDrawKit.RowTone.Positive : DebugDrawKit.RowTone.Neutral);
			}

			DrawEventEditor(entry);
		}

		// ─── Event authoring ──────────────────────────────────────────────────

		/// <summary>Preview time (0..1) used when adding an event: live state time in Play Mode, else the scrub.</summary>
		private float CurrentPreviewNormalizedTime()
		{
			if (EditorApplication.isPlaying && _bridge != null)
			{
				var s = _bridge.ActiveSequenceState;
				if (s != null && s.IsValid && s.Length > 0f)
					return Mathf.Clamp01(s.NormalizedTime % 1f);
			}
			return _normalizedTime;
		}

		private void DrawEventEditor(AnimationSetEntry entry)
		{
			if (entry?.clip == null)
				return;

			EditorGUILayout.Space(6);
			DebugDrawKit.Title("Events", "Authored on this clip");

			var owning = FindOwningSet(entry);
			if (owning != _set)
				EditorGUILayout.HelpBox($"Entry is inherited from '{(owning != null ? owning.name : "?")}' — edits are written there.", MessageType.None);

			var clip = entry.clip;
			var list = new List<AnimationClipEvent>(clip.events ?? System.Array.Empty<AnimationClipEvent>());
			bool changed = false;

			for (int i = 0; i < list.Count; i++)
			{
				var e = list[i];
				if (e == null) { e = new AnimationClipEvent(); list[i] = e; changed = true; }

				EditorGUILayout.BeginHorizontal();

				EditorGUI.BeginChangeCheck();
				string newName = EditorGUILayout.TextField(e.eventName, GUILayout.MinWidth(90));
				float newT = EditorGUILayout.Slider(e.normalizedTime, 0f, 1f);
				if (EditorGUI.EndChangeCheck())
				{
					e.eventName = newName;
					e.normalizedTime = newT;
					changed = true;
				}

				if (GUILayout.Button("Seek", GUILayout.Width(46)))
					SeekTo(e.normalizedTime);

				if (GUILayout.Button("✕", GUILayout.Width(24)))
				{
					list.RemoveAt(i);
					changed = true;
					i--;
					continue;
				}

				EditorGUILayout.EndHorizontal();

				if (string.IsNullOrWhiteSpace(e.eventName))
					EditorGUILayout.HelpBox("Empty event name — this event fires nothing at runtime.", MessageType.Warning);
			}

			// Add row: type a name (or fill it from the catalog dropdown), then add at the current preview time.
			EditorGUILayout.BeginHorizontal();
			_addEventName = EditorGUILayout.TextField(_addEventName, GUILayout.MinWidth(80));
			if (_catalogNames.Length > 0)
			{
				int cur = System.Array.IndexOf(_catalogNames, _addEventName);
				int pick = EditorGUILayout.Popup(cur, _catalogNames, GUILayout.Width(120));
				if (pick >= 0 && pick < _catalogNames.Length)
					_addEventName = _catalogNames[pick];
			}

			float addTime = CurrentPreviewNormalizedTime();
			using (new EditorGUI.DisabledScope(string.IsNullOrWhiteSpace(_addEventName)))
			{
				if (GUILayout.Button($"+ Add @ {addTime:F2}", GUILayout.Width(100)))
				{
					list.Add(new AnimationClipEvent { eventName = _addEventName.Trim(), normalizedTime = addTime });
					changed = true;
				}
			}
			EditorGUILayout.EndHorizontal();

			if (_catalogNames.Length == 0)
				EditorGUILayout.LabelField("Tip: tag a const-holding class [AnimationEventNames] to populate the dropdown.", EditorStyles.miniLabel);

			if (changed)
			{
				clip.events = list.ToArray();
				ResizeFlash();
				if (owning != null)
					EditorUtility.SetDirty(owning);
			}
		}

		private AnimationSet FindOwningSet(AnimationSetEntry entry)
		{
			var current = _set;
			var guard = new HashSet<AnimationSet>();
			while (current != null && guard.Add(current))
			{
				if (current.entries != null && System.Array.IndexOf(current.entries, entry) >= 0)
					return current;
				current = current.parentSet;
			}
			return _set;
		}

		private void SeekTo(float normalizedTime)
		{
			_normalizedTime = Mathf.Clamp01(normalizedTime);
			if (EditorApplication.isPlaying)
				PreviewSeekLive();
			else
			{
				_offlinePlaying = false;
				SampleOffline();
			}
		}

		// ─── Event markers + fired-flash ──────────────────────────────────────

		private void DrawEventMarkers(Rect bar, AnimationSetEntry entry)
		{
			if (entry?.clip == null || !entry.clip.HasEvents)
				return;

			double now = EditorApplication.timeSinceStartup;
			var evs = entry.clip.events;
			for (int i = 0; i < evs.Length; i++)
			{
				var e = evs[i];
				if (e == null) continue;
				float x = bar.x + bar.width * Mathf.Clamp01(e.normalizedTime);
				bool flash = i < _eventFlashUntil.Length && now < _eventFlashUntil[i];
				var col = flash ? DebugDrawKit.Ok : DebugDrawKit.Warn;
				EditorGUI.DrawRect(new Rect(x - 1f, bar.y, 2f, bar.height), col);
			}
		}

		/// <summary>Flashes markers whose normalized time was crossed since the last sample (visual verify).</summary>
		private void DetectEventCrossings(float curr)
		{
			var entry = CurrentEntry;
			if (entry?.clip == null || !entry.clip.HasEvents)
			{
				_prevMarkerNormTime = curr;
				return;
			}

			ResizeFlash();
			var evs = entry.clip.events;
			double now = EditorApplication.timeSinceStartup;
			bool wrapped = curr < _prevMarkerNormTime;

			for (int i = 0; i < evs.Length && i < _eventFlashUntil.Length; i++)
			{
				var e = evs[i];
				if (e == null) continue;
				float t = Mathf.Clamp01(e.normalizedTime);
				bool crossed = wrapped
					? (t > _prevMarkerNormTime || t <= curr)
					: (t > _prevMarkerNormTime && t <= curr);
				if (crossed)
					_eventFlashUntil[i] = now + FlashDuration;
			}

			_prevMarkerNormTime = curr;
		}

		// ─── Offline (edit mode) ──────────────────────────────────────────────

		private void DrawOfflineControls()
		{
			EditorGUILayout.Space(6);
			DebugDrawKit.Title("Offline Preview", "AnimationMode clip sampling");

			var clip = CurrentClip;
			if (clip == null)
				return;

			_speed = EditorGUILayout.Slider("Speed", _speed, 0f, 3f);

			EditorGUI.BeginChangeCheck();
			_normalizedTime = EditorGUILayout.Slider("Normalized Time", _normalizedTime, 0f, 1f);
			if (EditorGUI.EndChangeCheck())
			{
				_offlinePlaying = false;
				SampleOffline();
			}

			float seconds = _normalizedTime * clip.length;
			DebugDrawKit.Bar("Time", _normalizedTime, $"{seconds:F2}s / {clip.length:F2}s", DebugDrawKit.Fill);
			DrawEventMarkers(GUILayoutUtility.GetLastRect(), CurrentEntry);

			EditorGUILayout.BeginHorizontal();
			if (GUILayout.Button(_offlinePlaying ? "❚❚ Pause" : "▶ Play", GUILayout.Height(26)))
				ToggleOfflinePlay();
			if (GUILayout.Button("Sample", GUILayout.Height(26)))
				SampleOffline();
			if (GUILayout.Button("Reset Pose", GUILayout.Height(26)))
				StopOfflineSampling();
			EditorGUILayout.EndHorizontal();

			if (AnimationMode.InAnimationMode())
				EditorGUILayout.HelpBox("Animation Mode is ON — pose is being driven. Press 'Reset Pose' to restore the rig.", MessageType.None);
			else
				EditorGUILayout.HelpBox("Press Play or Sample to preview. Blended cross-fades preview live in Play Mode.", MessageType.None);
		}

		private void ToggleOfflinePlay()
		{
			_offlinePlaying = !_offlinePlaying;
			_lastEditorTime = EditorApplication.timeSinceStartup;
			if (_offlinePlaying)
				SampleOffline();
		}

		private void OnEditorUpdate()
		{
			if (EditorApplication.isPlaying)
			{
				if (_bridge != null)
					Repaint();
				return;
			}

			if (!_offlinePlaying)
				return;

			var clip = CurrentClip;
			if (clip == null || clip.length <= 0f)
				return;

			double now = EditorApplication.timeSinceStartup;
			float dt = (float)(now - _lastEditorTime);
			_lastEditorTime = now;

			_normalizedTime += dt * _speed / clip.length;
			if (clip.isLooping)
				_normalizedTime -= Mathf.Floor(_normalizedTime);
			else if (_normalizedTime >= 1f)
			{
				_normalizedTime = 1f;
				_offlinePlaying = false;
			}

			DetectEventCrossings(clip.isLooping ? _normalizedTime - Mathf.Floor(_normalizedTime) : Mathf.Clamp01(_normalizedTime));
			SampleOffline();
			Repaint();
		}

		private void SampleOffline()
		{
			var clip = CurrentClip;
			if (clip == null || _animator == null)
				return;

			if (!AnimationMode.InAnimationMode())
				AnimationMode.StartAnimationMode();

			AnimationMode.BeginSampling();
			AnimationMode.SampleAnimationClip(_animator.gameObject, clip, _normalizedTime * clip.length);
			AnimationMode.EndSampling();

			SceneView.RepaintAll();
		}

		private void StopOfflineSampling()
		{
			_offlinePlaying = false;
			if (AnimationMode.InAnimationMode())
				AnimationMode.StopAnimationMode();
		}

		// ─── Live (play mode) ─────────────────────────────────────────────────

		private void DrawLiveControls()
		{
			EditorGUILayout.Space(6);
			DebugDrawKit.Title("Live Playback", "Drives the real PlayableGraphBridge");

			if (_bridge == null)
			{
				EditorGUILayout.HelpBox("No AnimatorBridgeBase on the target — live driving needs one. Offline sampling only.", MessageType.Warning);
				return;
			}

			bool registered = IsSetRegistered();
			if (!registered)
				EditorGUILayout.HelpBox("This set is not registered on the bridge, so live Play will throw. Pick a Registered Set above.", MessageType.Warning);

			_fadeDuration = EditorGUILayout.Slider("Fade Duration", _fadeDuration, 0f, 1f);

			EditorGUI.BeginChangeCheck();
			float speed = EditorGUILayout.Slider("Graph Speed", _bridge.Animancer != null && _bridge.Animancer.IsValid ? _bridge.Animancer.Speed : 1f, 0f, 3f);
			if (EditorGUI.EndChangeCheck() && _bridge.Animancer != null && _bridge.Animancer.IsValid)
				_bridge.Animancer.Speed = speed;

			var entry = CurrentEntry;
			bool hasSeq = entry?.link != null && entry.link.HasNext;

			using (new EditorGUI.DisabledScope(!registered))
			{
				EditorGUILayout.BeginHorizontal();
				if (GUILayout.Button("▶ Play Entry", GUILayout.Height(28)))
					PlayLive(false);
				using (new EditorGUI.DisabledScope(!hasSeq))
				{
					if (GUILayout.Button("▶ Play Sequence", GUILayout.Height(28)))
						PlayLive(true);
				}
				EditorGUILayout.EndHorizontal();

				EditorGUILayout.BeginHorizontal();
				if (GUILayout.Button("Preview Seek @ Time"))
					PreviewSeekLive();
				if (GUILayout.Button("Stop / Release"))
					ReleaseLive();
				EditorGUILayout.EndHorizontal();
			}

			DrawLiveActiveState();
		}

		private bool IsSetRegistered()
		{
			if (_bridge?.AnimationSets == null || _set == null)
				return false;
			foreach (var s in _bridge.AnimationSets)
				if (s == _set) return true;
			return false;
		}

		private void PlayLive(bool sequence)
		{
			var entry = CurrentEntry;
			if (entry == null || _set == null)
				return;
			try
			{
				if (sequence)
				{
					_bridge.PlayFromSetSequenceStrict(_set.name, entry.id, null);
					_liveState = _bridge.ActiveSequenceState;
				}
				else
				{
					_liveState = _bridge.PlayFromSetStrict(_set.name, entry.id, null);
				}
			}
			catch (System.Exception e)
			{
				Debug.LogWarning($"[Anim Test Bench] Play failed: {e.Message}", _bridge);
			}
		}

		private void PreviewSeekLive()
		{
			var entry = CurrentEntry;
			if (entry == null || _set == null)
				return;
			try
			{
				_bridge.PlayFromSetPreviewStrict(_set.name, entry.id, _normalizedTime);
			}
			catch (System.Exception e)
			{
				Debug.LogWarning($"[Anim Test Bench] Preview seek failed: {e.Message}", _bridge);
			}
		}

		private void ReleaseLive()
		{
			try
			{
				_bridge.CancelActiveSetSequencePlayback();
				_liveState = null;
			}
			catch (System.Exception e)
			{
				Debug.LogWarning($"[Anim Test Bench] Release failed: {e.Message}", _bridge);
			}
		}

		private void DrawLiveActiveState()
		{
			var state = _bridge.ActiveSequenceState ?? _liveState;
			if (state == null || !state.IsValid)
				return;

			EditorGUILayout.Space(4);
			string clipName = state is ClipState cs && cs.Clip != null ? cs.Clip.name : state.GetType().Name;
			float progress = state.Length > 0f ? Mathf.Clamp01(state.NormalizedTime % 1f) : 0f;
			DebugDrawKit.Bar($"▶ {clipName}", progress, $"{state.Time:F2}s / {state.Length:F2}s  •  w {state.Weight * 100f:F0}%", DebugDrawKit.Fill);
			DrawEventMarkers(GUILayoutUtility.GetLastRect(), CurrentEntry);
			DetectEventCrossings(Mathf.Clamp01(state.NormalizedTime % 1f));

			EditorGUI.BeginChangeCheck();
			float t = EditorGUILayout.Slider("Scrub", state.Time, 0f, Mathf.Max(0.0001f, state.Length));
			if (EditorGUI.EndChangeCheck())
			{
				state.Time = t;
				if (_bridge.Animancer != null) _bridge.Animancer.Evaluate();
			}
		}
	}
}
#endif
