using System;
using System.Collections.Generic;
using AetherNexus.FoundationPlatform.Animation;
using AetherNexus.FoundationPlatform.Editor.Utilities.Validation;
using AetherNexus.FoundationPlatform.Utilities.Menus;
using UnityEditor;
using UnityEngine;
using UnityEngine.Animations;
using UnityEngine.Playables;
using Object = UnityEngine.Object;

namespace AetherNexus.FoundationPlatform.Editor.Animation
{
	/// <summary>
	/// Samples each root-motion entry's clip on its source rig and stores the trajectory on the entry,
	/// so simulation reads root motion from data instead of from the animation graph.
	/// </summary>
	public static class AnimationRootMotionBaker
	{
		public const float SampleRate = 60f;

		[MenuItem(MenuPaths.Rebuild.AnimationRootMotion, false, MenuPriorities.Rebuild + 6)]
		[DesignerFeature(
			"Bake Root Motion",
			"Re-samples every root-motion clip in every Animation Set so movement driven by an animation is identical on every machine, whether or not the character is on screen.",
			"bake root motion animation set clip trajectory determinism network movement rebuild",
			DesignerFeatureKind.Generator,
			"")]
		private static void BakeAllFromMenu()
		{
			var baked = 0;
			var guids = AssetDatabase.FindAssets("t:" + nameof(AnimationSet));
			for (var i = 0; i < guids.Length; i++)
			{
				var set = AssetDatabase.LoadAssetAtPath<AnimationSet>(AssetDatabase.GUIDToAssetPath(guids[i]));
				baked += BakeStale(set);
			}

			Debug.Log($"[Animation] Root motion bake: {baked} entr{(baked == 1 ? "y" : "ies")} re-baked across {guids.Length} animation sets.");
		}

		public static string ComputeSourceHash(AnimationClip clip)
		{
			var path = AssetDatabase.GetAssetPath(clip);
			return AssetDatabase.GetAssetDependencyHash(path) + ":" + clip.name;
		}

		public static bool NeedsBake(AnimationSetEntry entry)
		{
			if (entry == null || entry.rootMotionMode == RootMotionMode.None || entry.clip == null || entry.clip.Clip == null)
				return false;

			var bake = entry.rootMotionBake;
			return bake == null || !bake.IsBaked || bake.sourceHash != ComputeSourceHash(entry.clip.Clip);
		}

		/// <summary>Bakes the set's own entries (not inherited ones) whose bake is missing or stale. Returns how many were baked.</summary>
		public static int BakeStale(AnimationSet set)
		{
			if (set.entries == null)
				return 0;

			var baked = 0;
			for (var i = 0; i < set.entries.Length; i++)
			{
				var entry = set.entries[i];
				if (!NeedsBake(entry))
					continue;

				Undo.RecordObject(set, "Bake Root Motion");
				entry.rootMotionBake = Bake(entry.clip.Clip);
				baked++;
			}

			if (baked > 0)
			{
				EditorUtility.SetDirty(set);
				AssetDatabase.SaveAssetIfDirty(set);
			}

			return baked;
		}

		public static AnimationRootMotionBake Bake(AnimationClip clip)
		{
			if (clip.length <= 0f)
				throw new InvalidOperationException($"[Animation:ERROR:RootMotion] Clip '{clip.name}' has no length to bake.");

			ResolveRig(clip, out var rigModel, out var avatar);

			var instance = (GameObject)Object.Instantiate(rigModel);
			instance.hideFlags = HideFlags.HideAndDontSave;
			var graph = PlayableGraph.Create("RootMotionBake");
			try
			{
				var animator = instance.GetComponentInChildren<Animator>(true);
				if (animator == null)
					animator = instance.AddComponent<Animator>();
				if (avatar != null)
					animator.avatar = avatar;
				animator.applyRootMotion = true;
				animator.cullingMode = AnimatorCullingMode.AlwaysAnimate;

				var root = animator.transform;
				root.SetPositionAndRotation(Vector3.zero, Quaternion.identity);

				graph.SetTimeUpdateMode(DirectorUpdateMode.Manual);
				var output = AnimationPlayableOutput.Create(graph, "RootMotionBake", animator);
				var playable = AnimationClipPlayable.Create(graph, clip);
				playable.SetApplyFootIK(false);
				output.SetSourcePlayable(playable);
				playable.SetTime(0d);
				graph.Evaluate(0f);
				root.SetPositionAndRotation(Vector3.zero, Quaternion.identity);

				var count = Mathf.Max(2, Mathf.CeilToInt(clip.length * SampleRate) + 1);
				var step = clip.length / (count - 1);
				var positions = new Vector3[count];
				var rotations = new Quaternion[count];
				rotations[0] = Quaternion.identity;

				for (var i = 1; i < count; i++)
				{
					graph.Evaluate(step);
					positions[i] = root.position;
					rotations[i] = root.rotation;
				}

				return new AnimationRootMotionBake
				{
					sampleRate = (count - 1) / clip.length,
					clipLength = clip.length,
					sourceHash = ComputeSourceHash(clip),
					positions = positions,
					rotations = rotations,
				};
			}
			finally
			{
				graph.Destroy();
				Object.DestroyImmediate(instance);
			}
		}

		// The clip's own model file is the rig its root curves were authored against. A clip that copies its
		// avatar from another model bakes on that model, so the human scale matches what the importer used.
		private static void ResolveRig(AnimationClip clip, out GameObject rigModel, out Avatar avatar)
		{
			var clipPath = AssetDatabase.GetAssetPath(clip);
			if (!(AssetImporter.GetAtPath(clipPath) is ModelImporter importer))
			{
				throw new InvalidOperationException(
					$"[Animation:ERROR:RootMotion] Clip '{clip.name}' at '{clipPath}' is not imported from a model file, so there is no rig to sample its root motion on. Use the clip from its source FBX.");
			}

			avatar = importer.sourceAvatar;
			var rigPath = avatar != null ? AssetDatabase.GetAssetPath(avatar) : clipPath;
			if (avatar == null)
			{
				var subAssets = AssetDatabase.LoadAllAssetsAtPath(clipPath);
				for (var i = 0; i < subAssets.Length; i++)
				{
					if (subAssets[i] is Avatar own)
					{
						avatar = own;
						break;
					}
				}
			}

			rigModel = AssetDatabase.LoadAssetAtPath<GameObject>(rigPath);
			if (rigModel == null)
			{
				throw new InvalidOperationException(
					$"[Animation:ERROR:RootMotion] No model at '{rigPath}' to sample clip '{clip.name}' on.");
			}
		}
	}

	/// <summary>Flags root-motion entries whose baked trajectory is missing or older than the clip.</summary>
	public sealed class AnimationRootMotionBakeValidator : IAuthoringValidator
	{
		private const string SourceName = "AnimationSet";

		public ValidationScope Scope => ValidationScope.Asset;

		public string Source => SourceName;

		public Type TargetType => typeof(AnimationSet);

		public void Collect(in ValidationRequest request, List<AuthoringIssue> issues)
		{
			var set = request.Target as AnimationSet;
			if (set == null || set.entries == null)
				return;

			for (var i = 0; i < set.entries.Length; i++)
			{
				var entry = set.entries[i];
				if (!AnimationRootMotionBaker.NeedsBake(entry))
					continue;

				issues.Add(new AuthoringIssue
				{
					Severity = AuthoringIssueSeverity.Error,
					Source = SourceName,
					Message = $"Entry '{entry.id}' uses root motion but its baked trajectory is missing or older than clip '{entry.clip.Clip.name}'. Simulation reads root motion only from the bake.",
					RelatedObject = set,
					AssetPath = AssetDatabase.GetAssetPath(set),
					Fix = new ValidationFix("Bake root motion for this set", () => AnimationRootMotionBaker.BakeStale(set)),
				});
			}
		}
	}
}
