#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.Reflection;
using AetherNexus.FoundationPlatform.Animation;
using AetherNexus.FoundationPlatform.Editor.Animation;
using AetherNexus.FoundationPlatform;
using UnityEditor;
using UnityEditorInternal;
using UnityEngine;

namespace AetherNexus.FoundationPlatform.Editor.Utilities
{
	[CustomEditor(typeof(AnimationSet))]
	internal class AnimationSetEditor : FoundationPlatform.FrameworkInspector.Editor.FrameworkEditor
	{
		private const float DragHandleInset = 18f;

		private SerializedProperty _parentSetProp;
		private SerializedProperty _blendProfileProp;
		private SerializedProperty _validationProfileProp;
		private SerializedProperty _entriesProp;
		private ReorderableList    _list;
		private string             _searchText = string.Empty;
		private readonly List<int> _matchingIndices = new List<int>();
		private bool               _showSequenceChains;
		private bool               _copyFoldout = false;
		private AnimationSet       _sourceAnimationSet;
		private Dictionary<string, SequenceStep> _sequenceStepsById = new Dictionary<string, SequenceStep>(StringComparer.Ordinal);
		private HashSet<string>    _requiredEntryIds = new HashSet<string>(StringComparer.Ordinal);

		protected override void OnEnable()
		{
			base.OnEnable();
			_parentSetProp         = serializedObject.FindProperty("parentSet");
			_blendProfileProp      = serializedObject.FindProperty("blendProfile");
			_validationProfileProp = serializedObject.FindProperty("validationProfile");
			_entriesProp           = serializedObject.FindProperty("entries");
			if (_entriesProp == null)
				return;

			_list = new ReorderableList(serializedObject, _entriesProp, true, true, true, true);
			_list.drawHeaderCallback   = DrawListHeader;
			_list.drawElementCallback  = DrawElement;
			_list.elementHeightCallback = GetElementHeight;
		}

		public override void OnInspectorGUI()
		{
			var handler = target as AnimationSet;
			if (handler == null)
			{
				base.OnInspectorGUI();
				return;
			}
			
			_requiredEntryIds = GetRequiredEntryIds(handler);
			serializedObject.Update();

			EditorGUI.BeginDisabledGroup(true);
			EditorGUILayout.ObjectField("Script", MonoScript.FromScriptableObject(handler), typeof(MonoScript), false);
			EditorGUI.EndDisabledGroup();

			EditorGUILayout.PropertyField(_parentSetProp);

			if (_entriesProp == null || _list == null)
			{
				EditorGUILayout.HelpBox("entries property missing.", MessageType.Error);
				serializedObject.ApplyModifiedProperties();
				return;
			}

			var searchActive = !string.IsNullOrWhiteSpace(_searchText);
			DrawToolbar(searchActive);
			_sequenceStepsById = BuildSequenceStepMap(handler);
			
			EditorGUILayout.HelpBox("Format: [Stance]_[Movement]", MessageType.Info);

			if (searchActive)
				DrawFilteredEntries();
			else
				_list.DoLayoutList();

			DrawSequenceChains();
			DrawInheritedEntriesSection(handler);
			
			EditorGUILayout.Space(12);
			
			FoundationPlatform.FrameworkInspector.Editor.GuiKit.BeginBox();
			DrawBlendProfileSection();
			DrawValidationSection();
			FoundationPlatform.FrameworkInspector.Editor.GuiKit.EndBox();
			
			DrawCopyEntriesSection();
			
			serializedObject.ApplyModifiedProperties();
		}

		private void DrawInheritedEntriesSection(AnimationSet handler)
		{
			if (handler == null || handler.parentSet == null)
				return;

			var localIds = new HashSet<string>(StringComparer.Ordinal);
			if (handler.entries != null)
			{
				foreach (var e in handler.entries)
				{
					if (e != null && !string.IsNullOrEmpty(e.id))
						localIds.Add(e.id);
				}
			}

			var parentResolved = handler.parentSet.GetResolvedEntries();
			var inherited = new List<AnimationSetEntry>();
			foreach (var pair in parentResolved)
			{
				if (!localIds.Contains(pair.Key))
					inherited.Add(pair.Value);
			}

			if (inherited.Count > 0)
			{
				EditorGUILayout.Space(10);
				FoundationPlatform.FrameworkInspector.Editor.GuiKit.BeginBox("Inherited Entries");
				foreach (var entry in inherited)
				{
					if (entry == null) continue;
					EditorGUILayout.BeginHorizontal();
					var label = string.IsNullOrEmpty(entry.category) 
						? entry.id 
						: $"[{entry.category}] {entry.id}";
					EditorGUILayout.LabelField(label, EditorStyles.boldLabel);
					if (GUILayout.Button("Override", GUILayout.Width(70)))
					{
						OverrideEntry(handler, entry);
					}
					EditorGUILayout.EndHorizontal();

					EditorGUI.BeginDisabledGroup(true);
					var clip = entry.clip?.Clip;
					EditorGUILayout.ObjectField("Clip", clip, typeof(AnimationClip), false);
					if (entry.maskAsset != null)
						EditorGUILayout.ObjectField("Mask Asset", entry.maskAsset, typeof(AvatarMask), false);
					else
						EditorGUILayout.EnumPopup("Mask", entry.mask);
					EditorGUI.EndDisabledGroup();
					EditorGUILayout.Space(2);
				}
				FoundationPlatform.FrameworkInspector.Editor.GuiKit.EndBox();
			}
		}

		private void OverrideEntry(AnimationSet set, AnimationSetEntry parentEntry)
		{
			Undo.RecordObject(set, "Override Animation Set Entry");

			string json = JsonUtility.ToJson(parentEntry);
			var cloned = JsonUtility.FromJson<AnimationSetEntry>(json);

			var list = new List<AnimationSetEntry>(set.entries ?? Array.Empty<AnimationSetEntry>());
			list.Add(cloned);
			set.entries = list.ToArray();

			EditorUtility.SetDirty(set);
			serializedObject.Update();
		}

		private void DrawBlendProfileSection()
		{
			if (_blendProfileProp == null)
			{
				EditorGUILayout.HelpBox(
					"blendProfile property missing. Reimport FoundationPlatform or refresh the AnimationSet script.",
					MessageType.Warning);
				return;
			}

			var handler = target as AnimationSet;
			var assignedProfile = _blendProfileProp.objectReferenceValue as LocomotionBlendProfile;
			var resolved = handler != null ? handler.ResolvedBlendProfile : null;
			bool isInherited = assignedProfile == null && resolved != null;

			EditorGUILayout.BeginHorizontal();
			float originalLabelWidth = EditorGUIUtility.labelWidth;
			EditorGUIUtility.labelWidth = 100;
			
			if (isInherited)
			{
				GUI.backgroundColor = new Color(0.7f, 0.9f, 1f, 1f);
			}

			EditorGUILayout.PropertyField(
				_blendProfileProp,
				new GUIContent(isInherited ? "Blend Profile [Inh]" : "Blend Profile",
				               "LocomotionBlendProfile asset. Required for locomotion sets; leave null to inherit."));
			
			GUI.backgroundColor = Color.white;
			EditorGUIUtility.labelWidth = originalLabelWidth;

			var activeProfile = assignedProfile != null ? assignedProfile : resolved;
			if (activeProfile != null)
			{
				if (GUILayout.Button("Select Profile", GUILayout.Width(100)))
					Selection.activeObject = activeProfile;
			}
			EditorGUILayout.EndHorizontal();

			if (isInherited)
			{
				EditorGUILayout.HelpBox($"Inherited from parent set: {resolved.name}", MessageType.Info);
			}
			else if (assignedProfile == null)
			{
				EditorGUILayout.HelpBox(
					"No Blend Profile assigned. Create a LocomotionBlendProfile asset and assign it here for locomotion sets.",
					MessageType.Info);
			}
		}

		private void DrawCopyEntriesSection()
		{
			FoundationPlatform.FrameworkInspector.Editor.GuiKit.BeginBox();
			FoundationPlatform.FrameworkInspector.Editor.GuiKit.BeginBoxHeader();
			_copyFoldout = FoundationPlatform.FrameworkInspector.Editor.GuiKit.Foldout(_copyFoldout, "Copy Entries Utility");
			FoundationPlatform.FrameworkInspector.Editor.GuiKit.EndBoxHeader();

			if (_copyFoldout)
			{
				_sourceAnimationSet = (AnimationSet)EditorGUILayout.ObjectField(
					new GUIContent("Source Animation Set", "AnimationSet to copy entries from."),
					_sourceAnimationSet,
					typeof(AnimationSet),
					false);

				using (new EditorGUI.DisabledScope(_sourceAnimationSet == null || _sourceAnimationSet == target))
				{
					if (GUILayout.Button("Overwrite Entries from Source"))
					{
						if (EditorUtility.DisplayDialog("Overwrite Entries", "Are you sure you want to overwrite all entries in this AnimationSet with entries from " + _sourceAnimationSet.name + "?", "Yes", "No"))
						{
							CopyEntriesFrom(_sourceAnimationSet);
						}
					}
				}
			}
			FoundationPlatform.FrameworkInspector.Editor.GuiKit.EndBox();
			EditorGUILayout.Space(2);
		}

		private void CopyEntriesFrom(AnimationSet source)
		{
			if (source == null || source.entries == null) return;

			Undo.RecordObject(target, "Copy Entries From " + source.name);
			var dest = (AnimationSet)target;

			var wrapper = new EntriesWrapper { entries = source.entries };
			string json = JsonUtility.ToJson(wrapper);
			var destWrapper = new EntriesWrapper();
			JsonUtility.FromJsonOverwrite(json, destWrapper);

			dest.entries = destWrapper.entries;
			EditorUtility.SetDirty(target);
			serializedObject.Update();
		}

		[Serializable]
		private class EntriesWrapper
		{
			public AnimationSetEntry[] entries;
		}

		private void DrawValidationSection()
		{
			if (_validationProfileProp == null)
				return;

			var handler = target as AnimationSet;
			var assigned = _validationProfileProp.objectReferenceValue as AnimationSetValidationProfile;
			var resolved = handler != null ? handler.ResolvedValidationProfile : null;
			bool isInherited = assigned == null && resolved != null;

			EditorGUILayout.BeginHorizontal();
			float originalLabelWidth = EditorGUIUtility.labelWidth;
			EditorGUIUtility.labelWidth = 100;

			if (isInherited)
			{
				GUI.backgroundColor = new Color(0.7f, 0.9f, 1f, 1f);
			}

			EditorGUILayout.PropertyField(
				_validationProfileProp,
				new GUIContent(isInherited ? "Validation Profile [Inh]" : "Validation Profile",
				               "Optional required entry ids for combat/equipment sets. Leave null to inherit."));

			GUI.backgroundColor = Color.white;
			EditorGUIUtility.labelWidth = originalLabelWidth;

			var activeProfile = assigned != null ? assigned : resolved;
			if (activeProfile != null)
			{
				if (GUILayout.Button("Select Profile", GUILayout.Width(100)))
					Selection.activeObject = activeProfile;
			}
			EditorGUILayout.EndHorizontal();

			if (isInherited)
			{
				EditorGUILayout.HelpBox($"Inherited from parent set: {resolved.name}", MessageType.Info);
			}

			DrawValidationResults();
		}

		private void DrawToolbar(bool searchActive)
		{
			EditorGUILayout.BeginHorizontal(EditorStyles.toolbar);
			_searchText = GUILayout.TextField(_searchText, EditorStyles.toolbarSearchField, GUILayout.ExpandWidth(true));
			if (GUILayout.Button("Clear", EditorStyles.toolbarButton, GUILayout.Width(44)))
				_searchText = string.Empty;

			using (new EditorGUI.DisabledScope(searchActive || _list.index < 0 || _list.index >= _entriesProp.arraySize))
			{
				if (GUILayout.Button("Duplicate", EditorStyles.toolbarButton, GUILayout.Width(72)))
					DuplicateSelectedEntry();
			}

			if (GUILayout.Button("Expand All", EditorStyles.toolbarButton, GUILayout.Width(80)))
				SetAllEntriesExpanded(true);

			if (GUILayout.Button("Collapse All", EditorStyles.toolbarButton, GUILayout.Width(80)))
				SetAllEntriesExpanded(false);

			EditorGUILayout.EndHorizontal();
		}

		private void SetAllEntriesExpanded(bool expanded)
		{
			if (_entriesProp == null) return;
			for (int i = 0; i < _entriesProp.arraySize; i++)
			{
				var elem = _entriesProp.GetArrayElementAtIndex(i);
				if (elem != null)
					elem.isExpanded = expanded;
			}
		}

		private void DrawFilteredEntries()
		{
			_matchingIndices.Clear();
			for (int i = 0; i < _entriesProp.arraySize; i++)
			{
				var elem = _entriesProp.GetArrayElementAtIndex(i);
				if (EntryMatchesSearch(elem, _searchText))
					_matchingIndices.Add(i);
			}

			EditorGUILayout.HelpBox(
				"Non-matching entries are hidden. Clear the search to reorder, add, or remove list items.",
				MessageType.Info);

			if (_matchingIndices.Count == 0)
				EditorGUILayout.LabelField("No entries match this search.");
			else
			{
				for (int v = 0; v < _matchingIndices.Count; v++)
				{
					int idx  = _matchingIndices[v];
					var elem = _entriesProp.GetArrayElementAtIndex(idx);
					var label = BuildEntryLabel(elem, idx, $"  [{idx}]");
					var idProp = elem.FindPropertyRelative("id");
					bool isRequired = idProp != null && !string.IsNullOrEmpty(idProp.stringValue) && _requiredEntryIds.Contains(idProp.stringValue);

					var foldoutStyle = EditorStyles.foldout;
					var labelStyle = EditorStyles.label;
					bool prevFoldoutRich = foldoutStyle.richText;
					bool prevLabelRich = labelStyle.richText;

					if (isRequired)
					{
						foldoutStyle.richText = true;
						labelStyle.richText = true;
					}

					try
					{
						EditorGUILayout.PropertyField(elem, new GUIContent(label), true);
					}
					finally
					{
						if (isRequired)
						{
							foldoutStyle.richText = prevFoldoutRich;
							labelStyle.richText = prevLabelRich;
						}
					}
				}
			}
		}

		private void DrawListHeader(Rect rect)
		{
			EditorGUI.LabelField(rect, new GUIContent("Entries", "Animation clips registered on this asset, each with a unique id."));
		}

		private float GetElementHeight(int index)
		{
			if (_entriesProp == null || index < 0 || index >= _entriesProp.arraySize)
				return EditorGUIUtility.singleLineHeight;

			var elem = _entriesProp.GetArrayElementAtIndex(index);
			return EditorGUI.GetPropertyHeight(elem, true) + EditorGUIUtility.standardVerticalSpacing * 2f + 4f;
		}

		private void DrawElement(Rect rect, int index, bool isActive, bool isFocused)
		{
			var elem = _entriesProp.GetArrayElementAtIndex(index);
			rect.xMin  += DragHandleInset;
			rect.y     += 2f;
			rect.height -= 4f;

			var label = BuildEntryLabel(elem, index);
			var idProp = elem.FindPropertyRelative("id");
			bool isRequired = idProp != null && !string.IsNullOrEmpty(idProp.stringValue) && _requiredEntryIds.Contains(idProp.stringValue);

			var foldoutStyle = EditorStyles.foldout;
			var labelStyle = EditorStyles.label;
			bool prevFoldoutRich = foldoutStyle.richText;
			bool prevLabelRich = labelStyle.richText;

			if (isRequired)
			{
				foldoutStyle.richText = true;
				labelStyle.richText = true;
			}

			try
			{
				EditorGUI.PropertyField(rect, elem, new GUIContent(label), true);
			}
			finally
			{
				if (isRequired)
				{
					foldoutStyle.richText = prevFoldoutRich;
					labelStyle.richText = prevLabelRich;
				}
			}
		}

		private string BuildEntryLabel(SerializedProperty elem, int index, string labelSuffix = null)
		{
			var idProp = elem.FindPropertyRelative("id");
			var categoryProp = elem.FindPropertyRelative("category");
			string label = idProp != null && !string.IsNullOrEmpty(idProp.stringValue)
				? idProp.stringValue
				: $"Entry {index}";

			if (categoryProp != null && !string.IsNullOrEmpty(categoryProp.stringValue))
				label = $"[{categoryProp.stringValue}] {label}";

			var rootMotionProp = elem.FindPropertyRelative("rootMotionMode");
			if (rootMotionProp != null
			    && rootMotionProp.propertyType == SerializedPropertyType.Enum
			    && rootMotionProp.enumValueIndex != 0)
			{
				label = $"{label} ({rootMotionProp.enumDisplayNames[rootMotionProp.enumValueIndex]})";
			}

			var suspendTranslationProp = elem.FindPropertyRelative("suspendTranslation");
			if (suspendTranslationProp != null && suspendTranslationProp.boolValue)
				label = $"{label} [Suspend Translation]";

			bool isRequired = idProp != null && !string.IsNullOrEmpty(idProp.stringValue) && _requiredEntryIds.Contains(idProp.stringValue);
			if (isRequired)
			{
				label = $"<color=#ffd800>★ {label}</color>";
			}

			if (!string.IsNullOrEmpty(labelSuffix))
				label = $"{label}{labelSuffix}";

			if (idProp != null && _sequenceStepsById.TryGetValue(idProp.stringValue, out var sequenceStep))
				label = $"{label} [Seq {sequenceStep.Step}/{sequenceStep.Total}]";

			return label;
		}

		private static bool EntryMatchesSearch(SerializedProperty entry, string search)
		{
			if (string.IsNullOrWhiteSpace(search))
				return true;

			search = search.Trim();
			var idProp = entry.FindPropertyRelative("id");
			if (idProp != null && !string.IsNullOrEmpty(idProp.stringValue))
			{
				if (idProp.stringValue.IndexOf(search, System.StringComparison.OrdinalIgnoreCase) >= 0)
					return true;
			}

			var categoryProp = entry.FindPropertyRelative("category");
			if (categoryProp != null && !string.IsNullOrEmpty(categoryProp.stringValue))
			{
				if (categoryProp.stringValue.IndexOf(search, System.StringComparison.OrdinalIgnoreCase) >= 0)
					return true;
			}

			var clipRoot = entry.FindPropertyRelative("clip");
			if (clipRoot != null)
			{
				var clipRef = clipRoot.FindPropertyRelative("clip");
				if (clipRef != null && clipRef.objectReferenceValue is AnimationClip ac && !string.IsNullOrEmpty(ac.name))
				{
					if (ac.name.IndexOf(search, System.StringComparison.OrdinalIgnoreCase) >= 0)
						return true;
				}
			}

			return false;
		}

		private void DuplicateSelectedEntry()
		{
			int i = _list.index;
			if (i < 0 || i >= _entriesProp.arraySize)
				return;

			serializedObject.ApplyModifiedProperties();
			serializedObject.Update();

			_entriesProp.InsertArrayElementAtIndex(i);
			serializedObject.ApplyModifiedProperties();
			serializedObject.Update();

			var set = (AnimationSet)target;
			var entries = set.entries;
			if (entries == null || i + 1 >= entries.Length)
				return;

			AnimationSetEntry a = entries[i];
			AnimationSetEntry b = entries[i + 1];
			entries[i]     = b;
			entries[i + 1] = a;

			EditorUtility.SetDirty(set);
			serializedObject.Update();

			var dupElem = _entriesProp.GetArrayElementAtIndex(i + 1);
			var idProp  = dupElem.FindPropertyRelative("id");
			if (idProp != null)
				idProp.stringValue = MakeUniqueEntryId(idProp.stringValue, set, i + 1);

			serializedObject.ApplyModifiedProperties();
			_list.index = i + 1;
		}

		private static string MakeUniqueEntryId(string baseId, AnimationSet set, int excludeIndex)
		{
			if (string.IsNullOrEmpty(baseId))
				baseId = "entry";

			var used = new HashSet<string>();
			for (int j = 0; j < set.entries.Length; j++)
			{
				if (j == excludeIndex) continue;
				AnimationSetEntry e = set.entries[j];
				if (e != null && !string.IsNullOrEmpty(e.id))
					used.Add(e.id);
			}

			string candidate = baseId + "_copy";
			if (!used.Contains(candidate))
				return candidate;

			for (int n = 1; n < 10000; n++)
			{
				candidate = baseId + "_" + n;
				if (!used.Contains(candidate))
					return candidate;
			}

			return baseId + "_" + System.Guid.NewGuid().ToString("N").Substring(0, 8);
		}


		private static Dictionary<string, SequenceStep> BuildSequenceStepMap(AnimationSet set)
		{
			var result = new Dictionary<string, SequenceStep>(StringComparer.Ordinal);
			if (set == null)
				return result;

			var resolved = set.GetResolvedEntries();
			if (resolved.Count == 0)
				return result;

			var linkedTo = new HashSet<string>(StringComparer.Ordinal);
			foreach (var e in resolved.Values)
			{
				if (e == null || string.IsNullOrEmpty(e.id)) continue;
				if (e.link != null && e.link.HasNext && !string.IsNullOrEmpty(e.link.nextEntryId))
					linkedTo.Add(e.link.nextEntryId);
			}

			foreach (var e in resolved.Values)
			{
				if (e == null || string.IsNullOrEmpty(e.id)) continue;
				if (e.link == null || !e.link.HasNext || linkedTo.Contains(e.id))
					continue;

				try
				{
					var ids = AnimationSetSequenceUtility.CollectSequenceEntryIds(set, e.id);
					for (var s = 0; s < ids.Count; s++)
						if (!result.ContainsKey(ids[s]))
							result.Add(ids[s], new SequenceStep(s + 1, ids.Count));
				}
				catch (InvalidOperationException)
				{
					// Broken chains remain visible through validation and the Sequence Chains foldout.
				}
			}

			return result;
		}

		internal static bool TryGetSequenceStep(AnimationSet set, string entryId, out SequenceStep step)
		{
			if (string.IsNullOrEmpty(entryId))
			{
				step = default(SequenceStep);
				return false;
			}

			return BuildSequenceStepMap(set).TryGetValue(entryId, out step);
		}

		internal readonly struct SequenceStep
		{
			public readonly int Step;
			public readonly int Total;

			public SequenceStep(int step, int total)
			{
				Step  = step;
				Total = total;
			}
		}

		private void DrawSequenceChains()
		{
			var set = (AnimationSet)target;
			var resolved = set.GetResolvedEntries();
			if (resolved.Count == 0)
				return;

			// Build fast id→entry lookup and track which ids are linked-to (non-heads).
			var byId       = new Dictionary<string, AnimationSetEntry>(resolved, StringComparer.Ordinal);
			var linkedTo   = new HashSet<string>(StringComparer.Ordinal);
			foreach (var pair in resolved)
			{
				var e = pair.Value;
				if (e == null || string.IsNullOrEmpty(e.id)) continue;
				if (e.link != null && e.link.HasNext && !string.IsNullOrEmpty(e.link.nextEntryId))
					linkedTo.Add(e.link.nextEntryId);
			}

			// Collect chain heads: have a link but are not a middle/tail in another chain.
			var heads = new List<AnimationSetEntry>();
			foreach (var pair in resolved)
			{
				var e = pair.Value;
				if (e == null || string.IsNullOrEmpty(e.id)) continue;
				if (e.link != null && e.link.HasNext && !linkedTo.Contains(e.id))
					heads.Add(e);
			}

			if (heads.Count == 0)
				return;

			EditorGUILayout.Space(4);
			_showSequenceChains = EditorGUILayout.BeginFoldoutHeaderGroup(_showSequenceChains, $"Sequence Chains ({heads.Count})");
			if (_showSequenceChains)
			{
				EditorGUI.indentLevel++;
				for (var h = 0; h < heads.Count; h++)
				{
					try
					{
						var ids = AnimationSetSequenceUtility.CollectSequenceEntryIds(set, heads[h].id);
						var sb = new System.Text.StringBuilder();
						for (var s = 0; s < ids.Count; s++)
						{
							if (s > 0) sb.Append(" → ");
							sb.Append(ids[s]);
							if (byId.TryGetValue(ids[s], out var step) && step.clip?.Clip != null)
								sb.Append($" ({step.clip.Clip.length:F2}s)");
							if (s == ids.Count - 1)
							{
								byId.TryGetValue(ids[ids.Count - 1], out var terminalEntry);
								bool terminalLoops = terminalEntry?.clip != null && terminalEntry.clip.IsLooping;
								sb.Append(terminalLoops ? " [loop]" : " [terminal]");
							}
						}
						EditorGUILayout.HelpBox(sb.ToString(), MessageType.None);
					}
					catch (InvalidOperationException ex)
					{
						EditorGUILayout.HelpBox(ex.Message, MessageType.Error);
					}
				}
				EditorGUI.indentLevel--;
			}
			EditorGUILayout.EndFoldoutHeaderGroup();
		}

		private void DrawValidationResults()
		{
			var set = (AnimationSet)target;
			if (set.entries == null)
				return;

			var duplicateIds = new HashSet<string>();
			var seenIds      = new HashSet<string>();
			var hasNullClip  = false;
			var sequenceWarnings = new List<string>();
			var sequenceErrors   = new List<string>();
			var byId = AnimationSetValidator.BuildEntryMap(set);

			for (int i = 0; i < set.entries.Length; i++)
			{
				AnimationSetEntry e = set.entries[i];
				if (e == null || string.IsNullOrEmpty(e.id))
					continue;

				if (seenIds.Contains(e.id))
					duplicateIds.Add(e.id);
				else
					seenIds.Add(e.id);

				if (e.clip == null || e.clip.Clip == null)
					hasNullClip = true;
			}

			AnimationSetValidator.CollectLinkChainValidation(set, byId, sequenceWarnings, sequenceErrors);

			if (duplicateIds.Count <= 0 && !hasNullClip && sequenceWarnings.Count == 0 && sequenceErrors.Count == 0)
			{
				EditorGUILayout.HelpBox("Validation passed. No issues found.", MessageType.Info);
				return;
			}

			if (duplicateIds.Count > 0)
				EditorGUILayout.HelpBox(
					"Duplicate entry id(s): " + string.Join(", ", duplicateIds) + ". Generated method names will conflict.",
					MessageType.Warning);
			if (hasNullClip)
				EditorGUILayout.HelpBox(
					"Some entries have no clip assigned. Runtime play for those ids may throw.",
					MessageType.Warning);


			for (int w = 0; w < sequenceWarnings.Count; w++)
				EditorGUILayout.HelpBox(sequenceWarnings[w], MessageType.Warning);
			for (int e = 0; e < sequenceErrors.Count; e++)
				EditorGUILayout.HelpBox(sequenceErrors[e], MessageType.Error);
		}

		private HashSet<string> GetRequiredEntryIds(AnimationSet set)
		{
			var required = new HashSet<string>(StringComparer.Ordinal);
			if (set == null) return required;

			var blendProfile = set.ResolvedBlendProfile;
			if (blendProfile != null && blendProfile.stances != null)
			{
				foreach (var stance in blendProfile.stances)
				{
					if (stance == null) continue;
					if (stance.directionEntryIds != null)
					{
						foreach (var id in stance.directionEntryIds)
						{
							if (!string.IsNullOrEmpty(id)) required.Add(id);
						}
					}
					if (stance.enableDiagonalDirections && stance.diagonalDirectionEntryIds != null)
					{
						foreach (var id in stance.diagonalDirectionEntryIds)
						{
							if (!string.IsNullOrEmpty(id)) required.Add(id);
						}
					}
					if (stance.enableTurnInPlace)
					{
						if (!string.IsNullOrEmpty(stance.turnLeft180Id)) required.Add(stance.turnLeft180Id);
						if (!string.IsNullOrEmpty(stance.turnRight180Id)) required.Add(stance.turnRight180Id);
						if (stance.customTurns != null)
						{
							foreach (var turn in stance.customTurns)
							{
								if (turn == null) continue;
								if (!string.IsNullOrEmpty(turn.turnLeftId)) required.Add(turn.turnLeftId);
								if (!string.IsNullOrEmpty(turn.turnRightId)) required.Add(turn.turnRightId);
							}
						}
					}
					if (!string.IsNullOrEmpty(stance.jumpClipId)) required.Add(stance.jumpClipId);
					if (!string.IsNullOrEmpty(stance.jumpApexClipId)) required.Add(stance.jumpApexClipId);
				}
			}

			var validationProfile = set.ResolvedValidationProfile;
			if (validationProfile != null)
			{
				if (validationProfile.requiredEntryIds != null)
				{
					foreach (var id in validationProfile.requiredEntryIds)
					{
						if (!string.IsNullOrEmpty(id)) required.Add(id);
					}
				}
				if (validationProfile.requiredCategoryEntries != null)
				{
					foreach (var cat in validationProfile.requiredCategoryEntries)
					{
						if (!string.IsNullOrEmpty(cat.entryId)) required.Add(cat.entryId);
					}
				}
				if (validationProfile.requiredLoopingEntryIds != null)
				{
					foreach (var id in validationProfile.requiredLoopingEntryIds)
					{
						if (!string.IsNullOrEmpty(id)) required.Add(id);
					}
				}
				if (validationProfile.requiredOneShotEntryIds != null)
				{
					foreach (var id in validationProfile.requiredOneShotEntryIds)
					{
						if (!string.IsNullOrEmpty(id)) required.Add(id);
					}
				}
			}

			return required;
		}
	}

	[CustomPropertyDrawer(typeof(AnimationSetEntry))]
	internal sealed class AnimationSetEntryPropertyDrawer : PropertyDrawer
	{
		private const string SequenceSuffixMarker = " [Seq ";
		private const float  SequenceSuffixWidth  = 74f;
		private static GUIStyle _sequenceSuffixStyle;

		public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
		{
			EditorGUI.BeginProperty(position, label, property);

			float y    = position.y;
			float sp   = EditorGUIUtility.standardVerticalSpacing;
			float w    = position.width;
			float x    = position.x;
			float line = EditorGUIUtility.singleLineHeight;

			// [HideLabel] arrives as GUIContent.none: skip the foldout header, draw children flush.
			bool headerless = label == null || label == GUIContent.none;
			if (!headerless)
			{
				Rect foldRect = new Rect(x, y, w, line);
				var foldLabel = SplitSequenceSuffixLabel(label, out var sequenceSuffix);
				var foldoutRect = foldRect;
				if (!string.IsNullOrEmpty(sequenceSuffix))
					foldoutRect.xMax -= SequenceSuffixWidth + 4f;

				EditorGUI.BeginChangeCheck();
				bool expanded = EditorGUI.Foldout(foldoutRect, property.isExpanded, foldLabel, true);
				if (EditorGUI.EndChangeCheck())
					property.isExpanded = expanded;

				if (!string.IsNullOrEmpty(sequenceSuffix))
				{
					var suffixRect = new Rect(foldRect.xMax - SequenceSuffixWidth, foldRect.y, SequenceSuffixWidth, foldRect.height);
					EditorGUI.LabelField(suffixRect, sequenceSuffix, GetSequenceSuffixStyle());
				}

				y += line + sp;

				if (!property.isExpanded)
				{
					EditorGUI.EndProperty();
					return;
				}

				EditorGUI.indentLevel++;
			}

			DrawEntryField(ref y, x, w, sp, property, "id");
			DrawEntryField(ref y, x, w, sp, property, "category");
			DrawEntryField(ref y, x, w, sp, property, "clip", true);
			DrawEntryField(ref y, x, w, sp, property, "transitionBack");
			DrawEntryField(ref y, x, w, sp, property, "mask");
			DrawEntryField(ref y, x, w, sp, property, "rootMotionMode");
			DrawEntryField(ref y, x, w, sp, property, "suspendTranslation");
			DrawLinkField(ref y, x, w, sp, property);
			if (!headerless)
				EditorGUI.indentLevel--;

			EditorGUI.EndProperty();
		}

		private static GUIContent SplitSequenceSuffixLabel(GUIContent label, out string sequenceSuffix)
		{
			var text = string.IsNullOrEmpty(label.text) ? "Entry" : label.text;
			sequenceSuffix = string.Empty;

			var sequenceIndex = text.LastIndexOf(SequenceSuffixMarker, StringComparison.Ordinal);
			if (sequenceIndex >= 0 && text.EndsWith("]", StringComparison.Ordinal))
			{
				sequenceSuffix = text.Substring(sequenceIndex + 1);
				text = text.Substring(0, sequenceIndex);
			}

			return new GUIContent(text, label.tooltip);
		}

		private static GUIStyle GetSequenceSuffixStyle()
		{
			if (_sequenceSuffixStyle == null)
			{
				_sequenceSuffixStyle = new GUIStyle(EditorStyles.miniLabel)
				{
					alignment = TextAnchor.MiddleRight
				};
			}

			return _sequenceSuffixStyle;
		}

		public override float GetPropertyHeight(SerializedProperty property, GUIContent label)
		{
			float line = EditorGUIUtility.singleLineHeight;
			float sp   = EditorGUIUtility.standardVerticalSpacing;
			bool headerless = label == null || label == GUIContent.none;
			float h    = headerless ? 0f : line + sp;
			if (!headerless && !property.isExpanded)
				return h;

			h += GetEntryFieldHeight(property, "id", sp);
			h += GetEntryFieldHeight(property, "category", sp);
			h += GetEntryFieldHeight(property, "clip", sp, true);
			h += GetEntryFieldHeight(property, "transitionBack", sp);
			h += GetEntryFieldHeight(property, "mask", sp);
			h += GetEntryFieldHeight(property, "rootMotionMode", sp);
			h += GetEntryFieldHeight(property, "suspendTranslation", sp);
			h += GetLinkFieldHeight(property, sp);
			return h;
		}

		private static void DrawEntryField(
			ref float y,
			float x,
			float w,
			float sp,
			SerializedProperty parent,
			string relativeName,
			bool includeChildren = false)
		{
			var prop = parent.FindPropertyRelative(relativeName);
			if (prop == null)
				return;

			float h = EditorGUI.GetPropertyHeight(prop, includeChildren);
			EditorGUI.PropertyField(new Rect(x, y, w, h), prop, includeChildren);
			y += h + sp;
		}

		private static float GetEntryFieldHeight(SerializedProperty parent, string relativeName, float sp, bool includeChildren = false)
		{
			var prop = parent.FindPropertyRelative(relativeName);
			if (prop == null)
				return 0f;

			return EditorGUI.GetPropertyHeight(prop, includeChildren) + sp;
		}

		private static void DrawLinkField(ref float y, float x, float w, float sp, SerializedProperty parent)
		{
			var line = EditorGUIUtility.singleLineHeight;
			var linkProp = parent.FindPropertyRelative("link");
			if (linkProp == null)
				return;

			var label = new GUIContent(
				"Link",
				"Sequence handoff for PlayFromSetSequence. Next Entry Id lists other rows on this AnimationSet.");

			float h = EditorGUI.GetPropertyHeight(linkProp, label, true);
			EditorGUI.PropertyField(new Rect(x, y, w, h), linkProp, label, true);
			y += h + sp;
			DrawSequenceChainHint(ref y, x, w, line, sp, parent);
		}

		private static void DrawSequenceChainHint(
			ref float y,
			float x,
			float w,
			float line,
			float sp,
			SerializedProperty entryProp)
		{
			var idProp = entryProp.FindPropertyRelative("id");
			var linkProp = entryProp.FindPropertyRelative("link");
			if (idProp == null || linkProp == null)
				return;

			var set = entryProp.serializedObject.targetObject as AnimationSet;
			if (set == null || string.IsNullOrWhiteSpace(idProp.stringValue))
				return;

			var nextIdProp = linkProp.FindPropertyRelative("nextEntryId");
			var hasNextId = nextIdProp != null && !string.IsNullOrWhiteSpace(nextIdProp.stringValue);
			var hasSequenceStep = AnimationSetEditor.TryGetSequenceStep(set, idProp.stringValue, out var sequenceStep);
			if (!hasSequenceStep && !hasNextId)
				return;

			try
			{
				var ids = AnimationSetSequenceUtility.CollectSequenceEntryIds(set, idProp.stringValue);
				var terminal = ids[ids.Count - 1];
				var hint = hasSequenceStep
					? $"Sequence: step {sequenceStep.Step}/{sequenceStep.Total} -> {terminal}."
					: ids.Count == 1
						? $"Sequence from here: 1 step ({terminal})."
						: $"Sequence from here: {ids.Count} steps -> {terminal}.";
				EditorGUI.LabelField(new Rect(x, y, w, line), hint, EditorStyles.miniLabel);
				y += line + sp;
			}
			catch (InvalidOperationException ex)
			{
				var boxH = line * 2f;
				EditorGUI.HelpBox(new Rect(x, y, w, boxH), ex.Message, MessageType.Error);
				y += boxH + sp;
			}
		}

		private static float GetLinkFieldHeight(SerializedProperty parent, float sp)
		{
			var linkProp = parent.FindPropertyRelative("link");
			if (linkProp == null)
				return 0f;

			var line = EditorGUIUtility.singleLineHeight;
			var label = new GUIContent("Link");
			var h = EditorGUI.GetPropertyHeight(linkProp, label, true) + sp;

			var nextIdProp = linkProp.FindPropertyRelative("nextEntryId");
			var idProp = parent.FindPropertyRelative("id");
			var set = parent.serializedObject.targetObject as AnimationSet;
			var hasSequenceStep = set != null
			                      && idProp != null
			                      && AnimationSetEditor.TryGetSequenceStep(set, idProp.stringValue, out _);
			var hasNextId = nextIdProp != null && !string.IsNullOrWhiteSpace(nextIdProp.stringValue);
			if (hasSequenceStep || hasNextId)
			{
				if (set != null && idProp != null && !string.IsNullOrWhiteSpace(idProp.stringValue))
				{
					try
					{
						AnimationSetSequenceUtility.CollectSequenceEntryIds(set, idProp.stringValue);
						h += line + sp;
					}
					catch (InvalidOperationException)
					{
						h += line * 2f + sp;
					}
				}
			}

			return h;
		}
	}
}
#endif
