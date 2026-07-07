using System;
using System.Collections.Generic;
using System.Linq;
using FoundationPlatform.FrameworkInspector;
using UnityEngine;

namespace FoundationPlatform.Animation
{
	[CreateAssetMenu(fileName = "LocomotionBlendProfile", menuName = "Foundation/Animation/Locomotion Blend Profile", order = 44)]
	public class LocomotionBlendProfile : ScriptableObject
	{
		[TitleGroup("Stances (Mixer Input Order)")]
		[Tooltip("Ordered stance definitions. Order matches stance mixer input index.")]
		[ListDrawerSettings(ShowIndexLabels = true, ListElementLabelName = "stanceId")]
		public LocomotionBlendStanceDefinition[] stances = Array.Empty<LocomotionBlendStanceDefinition>();
		
		[Tooltip("Default blend tuning. Individual stances can override via overrideBlendParams.")]
		public LocomotionBlendParams defaultBlendParams = LocomotionBlendParams.Default;

		/// <summary>Returns the index of <paramref name="stanceId"/> in <see cref="stances"/>, or -1.</summary>
		public int GetStanceIndex(string stanceId)
		{
			if (string.IsNullOrEmpty(stanceId) || stances == null)
				return -1;

			for (var i = 0; i < stances.Length; i++)
			{
				if (stances[i] != null && stances[i].stanceId == stanceId)
					return i;
			}

			return -1;
		}

		/// <summary>Returns the first stance id, or throws if none exist.</summary>
		public string RequireDefaultStanceId()
		{
			if (stances == null || stances.Length == 0 || stances[0] == null || string.IsNullOrWhiteSpace(stances[0].stanceId))
				throw new InvalidOperationException($"'{name}': no default stance defined.");

			return stances[0].stanceId;
		}

		public void ValidateOrThrow()
		{
			if (stances == null || stances.Length == 0)
				throw new InvalidOperationException($"'{name}': no locomotion blend stances defined.");

			var seenStanceIds = new HashSet<string>(StringComparer.Ordinal);
			for (var i = 0; i < stances.Length; i++)
			{
				var stance = stances[i];
				if (stance == null)
					throw new InvalidOperationException($"'{name}': null stance at index {i}.");

				stance.ValidateOrThrow(name);
				if (!seenStanceIds.Add(stance.stanceId))
					throw new InvalidOperationException($"'{name}': duplicate stanceId '{stance.stanceId}'.");
			}
		}

		public IEnumerable<string> GetStanceIds()
		{
			if (stances == null) return Enumerable.Empty<string>();
			return stances.Where(s => s != null && !string.IsNullOrWhiteSpace(s.stanceId)).Select(s => s.stanceId);
		}

#if UNITY_EDITOR
		[TitleGroup("Tools")]
		[Button("Apply Walk / Run / Crouch Template", ButtonSizes.Medium)]
		internal void ApplyDefaultLocomotionTemplate()
		{
			stances = new[]
			{
				LocomotionBlendTemplateUtility.CreateWalkStance(),
				LocomotionBlendTemplateUtility.CreateRunStance(),
				LocomotionBlendTemplateUtility.CreateCrouchStance(),
			};
			defaultBlendParams = LocomotionBlendParams.Default;
			UnityEditor.EditorUtility.SetDirty(this);
		}

		[TitleGroup("Tools")]
		[Button("Validate", ButtonSizes.Medium)]
		private void ValidateProfile()
		{
			try
			{
				ValidateOrThrow();
				Debug.Log($"[LocomotionBlendProfile] '{name}' validation passed.", this);
			}
			catch (InvalidOperationException ex)
			{
				Debug.LogError($"[LocomotionBlendProfile] {ex.Message}", this);
			}
		}

		[FoldoutGroup("Blend Weight Preview")]
		[InfoBox("Shows what the direction weights would be for a given move input. Adjust Move X/Z and Idle Threshold below to tune blend behaviour without entering Play Mode.")]
		[ShowInInspector, PropertyRange(-1f, 1f)]
		private float _previewMoveX;

		[FoldoutGroup("Blend Weight Preview")]
		[ShowInInspector, PropertyRange(-1f, 1f)]
		private float _previewMoveZ = 1f;

		[FoldoutGroup("Blend Weight Preview")]
		[Button("Reset Input")]
		private void ResetPreviewInput()
		{
			_previewMoveX = 0f;
			_previewMoveZ = 1f;
		}

		[FoldoutGroup("Blend Weight Preview")]
		[OnInspectorGUI]
		private void DrawPreviewBarChart()
		{
			var weights = new float[5];
			ComputePreviewWeights(_previewMoveX, _previewMoveZ, defaultBlendParams.idleVelocityThreshold, weights);

			var area = UnityEditor.EditorGUILayout.GetControlRect(false, 100f);
			UnityEditor.EditorGUI.DrawRect(area, new Color(0.15f, 0.15f, 0.15f, 1f));
			string[] labels = { "Idle", "Fwd", "Back", "Left", "Right" };
			var barWidth = area.width / 5;
			var labelH = 16f;
			var barArea = new Rect(area.x, area.y, area.width, area.height - labelH);

			for (var i = 0; i < 5; i++)
			{
				var w = Mathf.Clamp01(weights[i]);
				var colX = area.x + i * barWidth;
				var fillH = barArea.height * w;
				var fillRect = new Rect(colX + 2, barArea.y + barArea.height - fillH, barWidth - 4, fillH);
				UnityEditor.EditorGUI.DrawRect(fillRect, new Color(0.3f, 0.75f, 0.4f, 1f));
				var labelRect = new Rect(colX, area.y + area.height - labelH, barWidth, labelH);
				GUI.Label(labelRect, $"{labels[i]}\n{w:P0}", UnityEditor.EditorStyles.centeredGreyMiniLabel);
			}
		}

		private static void ComputePreviewWeights(float moveX, float moveZ, float idleThreshold, float[] weights)
		{
			var magSq = moveX * moveX + moveZ * moveZ;
			if (magSq < idleThreshold * idleThreshold)
			{
				weights[0] = 1f; weights[1] = 0f; weights[2] = 0f; weights[3] = 0f; weights[4] = 0f;
				return;
			}

			var mag = Mathf.Sqrt(magSq);
			var moveWeight = Mathf.Clamp01(mag);
			var fwd = Mathf.Max(0f, moveZ);
			var back = Mathf.Max(0f, -moveZ);
			var right = Mathf.Max(0f, moveX);
			var left = Mathf.Max(0f, -moveX);
			var sum = fwd + back + left + right;
			if (sum < 0.0001f) sum = 1f;

			weights[0] = 1f - moveWeight;
			weights[1] = moveWeight * fwd / sum;
			weights[2] = moveWeight * back / sum;
			weights[3] = moveWeight * left / sum;
			weights[4] = moveWeight * right / sum;
		}
#endif
	}
}
