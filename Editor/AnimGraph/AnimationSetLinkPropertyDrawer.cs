#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using FoundationPlatform;
using FoundationPlatform.Animation;
using UnityEditor;
using UnityEngine;

namespace FoundationPlatform.Editor.Utilities
{
	[CustomPropertyDrawer(typeof(AnimationSetLink))]
	internal sealed class AnimationSetLinkPropertyDrawer : PropertyDrawer
	{
		private const string LinkSuffix = ".link";

		public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
		{
			EditorGUI.BeginProperty(position, label, property);

			var line = EditorGUIUtility.singleLineHeight;
			var sp   = EditorGUIUtility.standardVerticalSpacing;
			var x    = position.x;
			var w    = position.width;
			var y    = position.y;

			var foldRect = new Rect(x, y, w, line);
			property.isExpanded = EditorGUI.Foldout(foldRect, property.isExpanded, label, true);
			y += line + sp;

			if (!property.isExpanded)
			{
				EditorGUI.EndProperty();
				return;
			}

			EditorGUI.indentLevel++;
			DrawNextEntryPopup(ref y, x, w, line, sp, property);
			DrawTargetEntryHint(ref y, x, w, line, sp, property);
			DrawRelativeField(ref y, x, w, line, sp, property, "transitionIn");
			DrawRelativeField(ref y, x, w, line, sp, property, "transitionOut");
			DrawRelativeField(ref y, x, w, line, sp, property, "useEntryTransitionBackForTerminal");
			DrawRelativeField(ref y, x, w, line, sp, property, "useLinkHold");
			DrawHoldFields(ref y, x, w, line, sp, property);
			EditorGUI.indentLevel--;

			EditorGUI.EndProperty();
		}

		public override float GetPropertyHeight(SerializedProperty property, GUIContent label)
		{
			var line = EditorGUIUtility.singleLineHeight;
			var sp   = EditorGUIUtility.standardVerticalSpacing;
			var h    = line + sp;
			if (!property.isExpanded)
				return h;

			var nextIdProp = property.FindPropertyRelative("nextEntryId");
			if (nextIdProp != null && !string.IsNullOrWhiteSpace(nextIdProp.stringValue))
				h += line + sp;

			return h + (line + sp) * 5f + GetHoldFieldsHeight(property, line, sp);
		}

		private static float GetHoldFieldsHeight(SerializedProperty linkProperty, float line, float sp)
		{
			var useHoldProp = linkProperty.FindPropertyRelative("useLinkHold");
			if (useHoldProp == null || !useHoldProp.boolValue)
				return 0f;

			return (line + sp) * 3f;
		}

		private static void DrawHoldFields(
			ref float y,
			float x,
			float w,
			float line,
			float sp,
			SerializedProperty linkProperty)
		{
			var useHoldProp = linkProperty.FindPropertyRelative("useLinkHold");
			if (useHoldProp == null || !useHoldProp.boolValue)
				return;

			DrawRelativeField(ref y, x, w, line, sp, linkProperty, "holdStartNormalizedTime");
			DrawRelativeField(ref y, x, w, line, sp, linkProperty, "holdDurationSeconds");
			DrawRelativeField(ref y, x, w, line, sp, linkProperty, "holdMode");
		}

		private static void DrawNextEntryPopup(
			ref float y,
			float x,
			float w,
			float line,
			float sp,
			SerializedProperty linkProperty)
		{
			var nextIdProp = linkProperty.FindPropertyRelative("nextEntryId");
			if (nextIdProp == null)
				return;

			var options = BuildNextEntryOptions(linkProperty, nextIdProp.stringValue, out var selectedIndex);
			var rect    = new Rect(x, y, w, line);

			EditorGUI.BeginChangeCheck();
			var newIndex = EditorGUI.Popup(rect, "Next Entry Id", selectedIndex, options);
			if (EditorGUI.EndChangeCheck())
				nextIdProp.stringValue = IndexToEntryId(options, newIndex);

			y += line + sp;
		}

		private static void DrawTargetEntryHint(
			ref float y,
			float x,
			float w,
			float line,
			float sp,
			SerializedProperty linkProperty)
		{
			var nextIdProp = linkProperty.FindPropertyRelative("nextEntryId");
			if (nextIdProp == null || string.IsNullOrWhiteSpace(nextIdProp.stringValue))
				return;

			var set = linkProperty.serializedObject.targetObject as AnimationSet;
			if (set == null)
				return;

			var resolved = set.GetResolvedEntries();
			if (!resolved.TryGetValue(nextIdProp.stringValue, out var target) || target == null)
				return;

			var looping = target.clip != null && target.clip.IsLooping;
			var hint = looping
				? "Target loops (typical sequence terminal, e.g. InAir until land)."
				: "Target is one-shot.";

			EditorGUI.LabelField(new Rect(x, y, w, line), hint, EditorStyles.miniLabel);
			y += line + sp;
		}

		private static string[] BuildNextEntryOptions(
			SerializedProperty linkProperty,
			string currentNextId,
			out int selectedIndex)
		{
			var labels = new List<string> { "(None)" };
			var values = new List<string> { string.Empty };

			var owningEntryId = GetOwningEntryId(linkProperty);
			var set           = linkProperty.serializedObject.targetObject as AnimationSet;

			var candidateIds = new List<string>();
			if (set != null)
			{
				var resolved = set.GetResolvedEntries();
				foreach (var entry in resolved.Values)
				{
					if (entry == null || string.IsNullOrWhiteSpace(entry.id))
						continue;

					if (string.Equals(entry.id, owningEntryId, StringComparison.Ordinal))
						continue;

					candidateIds.Add(entry.id);
				}
			}

			candidateIds.Sort(StringComparer.Ordinal);
			for (var i = 0; i < candidateIds.Count; i++)
			{
				labels.Add(candidateIds[i]);
				values.Add(candidateIds[i]);
			}

			if (!string.IsNullOrWhiteSpace(currentNextId)
			    && !values.Contains(currentNextId))
			{
				labels.Add($"(Missing) {currentNextId}");
				values.Add(currentNextId);
			}

			selectedIndex = 0;
			for (var i = 0; i < values.Count; i++)
			{
				if (string.Equals(values[i], currentNextId, StringComparison.Ordinal))
				{
					selectedIndex = i;
					break;
				}
			}

			return labels.ToArray();
		}

		private static string IndexToEntryId(string[] options, int index)
		{
			if (index <= 0 || index >= options.Length)
				return string.Empty;

			var label = options[index];
			if (label.StartsWith("(Missing) ", StringComparison.Ordinal))
				return label.Substring("(Missing) ".Length);

			return label;
		}

		private static string GetOwningEntryId(SerializedProperty linkProperty)
		{
			var path = linkProperty.propertyPath;
			if (!path.EndsWith(LinkSuffix, StringComparison.Ordinal))
				return string.Empty;

			var entryPath = path.Substring(0, path.Length - LinkSuffix.Length) + ".id";
			var idProp    = linkProperty.serializedObject.FindProperty(entryPath);
			if (idProp == null || idProp.propertyType != SerializedPropertyType.String)
				return string.Empty;

			return idProp.stringValue ?? string.Empty;
		}

		private static void DrawRelativeField(
			ref float y,
			float x,
			float w,
			float line,
			float sp,
			SerializedProperty parent,
			string relativeName)
		{
			var prop = parent.FindPropertyRelative(relativeName);
			if (prop == null)
				return;

			EditorGUI.PropertyField(new Rect(x, y, w, line), prop, true);
			y += line + sp;
		}
	}
}
#endif
