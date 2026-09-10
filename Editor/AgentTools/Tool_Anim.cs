#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.ComponentModel;
using AetherNexus.FoundationPlatform.Animation;
using AetherNexus.FoundationPlatform.Editor.Animation;
using AetherNexus.FoundationPlatform.Editor.Utilities;
using UnityAiBridge;
using UnityEditor;
using UnityEngine;

namespace AetherNexus.FoundationPlatform.AgentTools.Editor
{
	public sealed class AnimationSetEntryReport
	{
		public string Id;
		public string Category;

		[Description("Set that declares this entry. Differs from the queried set when the entry is inherited from a parent set.")]
		public string DeclaredBy;

		public string Clip;
		public string ClipPath;
		public float ClipLength;
		public bool IsLooping;
		public float FadeDuration;
		public float Speed;
		public string Mask;
		public int LayerIndex;
		public string RootMotionMode;
		public bool TransitionBack;
		public bool SuspendTranslation;

		[Description("Authored clip events as 'name@normalizedTime'.")]
		public List<string> Events = new();

		[Description("Sequence link, or empty when this entry is standalone.")]
		public string Link;
	}

	public sealed class AnimationSetReport
	{
		public string Set;
		public string SetPath;

		[Description("Parent chain from this set outwards; entries resolve nearest-first.")]
		public List<string> ParentChain = new();

		public string BlendProfile;

		[Description("Stance ids in mixer input order from the resolved LocomotionBlendProfile.")]
		public List<string> Stances = new();

		public string ValidationProfile;
		public int EntryCount;
		public List<AnimationSetEntryReport> Entries = new();

		[Description("Findings from the resolved AnimationSetValidationProfile.")]
		public List<string> ProfileFindings = new();

		[Description("Sequence-link warnings: looping mid-sequence, ignored transitionBack, ignored link hold.")]
		public List<string> LinkWarnings = new();

		[Description("Sequence-link errors: dangling nextEntryId, root motion inside a sequence, bad hold config.")]
		public List<string> LinkErrors = new();

		[Description("Entries whose clip slot is empty; these throw at play time.")]
		public List<string> MissingClips = new();

		[Description("Event names authored on clips in this set that no [AnimationEventNames] class declares, so no code can subscribe to them.")]
		public List<string> UnknownEventNames = new();

		[Description("Everything Unity logged while the standard AnimationSet validation ran.")]
		public List<string> ValidationLog = new();
	}

	public sealed class AnimationClipReport
	{
		public string Clip;
		public string ClipPath;
		public float Length;
		public float FrameRate;
		public int FrameCount;
		public bool IsLooping;
		public bool IsLegacy;
		public bool IsHumanMotion;
		public bool HasRootCurves;
		public bool HasMotionCurves;
		public bool HasGenericRootTransform;
		public string WrapMode;

		[Description("Curve bindings on this clip, counted by kind.")]
		public int TransformCurveCount;

		public int ObjectCurveCount;

		[Description("Native AnimationEvents on the clip (distinct from AnimationSet clip events) as 'functionName@seconds'.")]
		public List<string> NativeEvents = new();

		[Description("Distinct transform paths this clip animates. Truncated to keep the payload readable.")]
		public List<string> AnimatedPaths = new();

		[Description("Set when 'target' was passed: animated paths with no matching transform under the target rig.")]
		public List<string> UnresolvedPaths = new();

		[Description("Set when 'target' was passed: how the clip lines up with that rig.")]
		public string TargetSummary;
	}

	/// <summary>
	/// Read-only reports over the AnimationSet system. An agent editing animation authoring needs the
	/// numbers a designer reads in the Inspector plus the findings that currently only reach the
	/// console, in one payload it can act on.
	/// </summary>
	[BridgeToolType]
	public partial class Tool_Anim
	{
		private const int MaxReportedPaths = 60;

		public const string AnimSetReportToolId = "anim-set-report";

		[BridgeTool(AnimSetReportToolId, Title = "Animation / Set Report")]
		[Description("Reports one AnimationSet: every resolved entry with its clip, looping flag, mask, layer, root " +
			"motion mode and sequence link, which parent set declared it, plus all validation findings — required " +
			"entries, sequence-link errors, missing clips and event names no code declares. Read-only.")]
		public AnimationSetReport SetReport
		(
			[Description("AnimationSet asset path or GUID.")]
			string animationSet,
			[Description("Also run the standard AnimationSet validation and capture everything it logs. This writes to the Editor console, exactly as the designer-facing validation does.")]
			bool runValidationLog = true
		)
		{
			return UnityAiBridge.Utils.MainThread.Instance.Run(() =>
			{
				var set = EditorObjectReference.LoadAsset<AnimationSet>(animationSet);

				var report = new AnimationSetReport
				{
					Set = set.name,
					SetPath = AssetDatabase.GetAssetPath(set),
					BlendProfile = set.ResolvedBlendProfile == null ? "" : set.ResolvedBlendProfile.name,
					ValidationProfile = set.ResolvedValidationProfile == null ? "" : set.ResolvedValidationProfile.name
				};

				AnimationSet cursor = set.parentSet;
				var visited = new HashSet<AnimationSet> { set };
				while (cursor != null && visited.Add(cursor))
				{
					report.ParentChain.Add(cursor.name);
					cursor = cursor.parentSet;
				}

				if (set.ResolvedBlendProfile != null)
					report.Stances.AddRange(set.ResolvedBlendProfile.GetStanceIds());

				Dictionary<string, string> owners = MapDeclaringSets(set);
				Dictionary<string, AnimationSetEntry> resolved = set.GetResolvedEntries();
				report.EntryCount = resolved.Count;

				var knownEvents = new HashSet<string>(AnimationEventNameCatalog.AllNames(), StringComparer.Ordinal);
				var unknown = new SortedSet<string>(StringComparer.Ordinal);

				foreach (KeyValuePair<string, AnimationSetEntry> pair in resolved)
				{
					AnimationSetEntry entry = pair.Value;
					AnimationClip clip = entry.clip == null ? null : entry.clip.Clip;

					var entryReport = new AnimationSetEntryReport
					{
						Id = pair.Key,
						Category = entry.category ?? "",
						DeclaredBy = owners.TryGetValue(pair.Key, out string owner) ? owner : set.name,
						Clip = clip == null ? "" : clip.name,
						ClipPath = clip == null ? "" : AssetDatabase.GetAssetPath(clip),
						ClipLength = clip == null ? 0f : clip.length,
						IsLooping = entry.clip != null && entry.clip.IsLooping,
						FadeDuration = entry.clip == null ? 0f : entry.clip.FadeDuration,
						Speed = entry.clip == null ? 0f : entry.clip.Speed,
						Mask = entry.maskAsset != null ? entry.maskAsset.name : entry.mask.ToString(),
						LayerIndex = entry.layerIndex,
						RootMotionMode = entry.rootMotionMode.ToString(),
						TransitionBack = entry.transitionBack,
						SuspendTranslation = entry.suspendTranslation,
						Link = DescribeLink(entry.link)
					};

					if (clip == null)
						report.MissingClips.Add(pair.Key);

					if (entry.clip != null && entry.clip.events != null)
					{
						for (var i = 0; i < entry.clip.events.Length; i++)
						{
							AnimationClipEvent clipEvent = entry.clip.events[i];
							if (clipEvent == null)
								continue;

							entryReport.Events.Add($"{clipEvent.eventName}@{clipEvent.normalizedTime:0.###}");
							if (!string.IsNullOrWhiteSpace(clipEvent.eventName) && !knownEvents.Contains(clipEvent.eventName))
								unknown.Add($"{pair.Key}: '{clipEvent.eventName}'");
						}
					}

					report.Entries.Add(entryReport);
				}

				report.UnknownEventNames.AddRange(unknown);

				AnimationSetValidationProfile profile = set.ResolvedValidationProfile;
				if (profile != null)
					report.ProfileFindings.AddRange(profile.Validate(set));

				AnimationSetValidator.CollectLinkChainValidation(set, resolved, report.LinkWarnings, report.LinkErrors);

				if (runValidationLog)
					report.ValidationLog.AddRange(CaptureLogs(() => AnimationSetValidator.LogValidation(set)));

				return report;
			});
		}

		public const string AnimClipReportToolId = "anim-clip-report";

		[BridgeTool(AnimClipReportToolId, Title = "Animation / Clip Report")]
		[Description("Reports one AnimationClip: length, frame rate, looping, humanoid or generic, root motion curve " +
			"presence, native events, and which transform paths it animates. Pass 'target' to check the clip against " +
			"a rig and get the animated paths that do not exist on it. Read-only.")]
		public AnimationClipReport ClipReport
		(
			[Description("AnimationClip asset path or GUID. For a clip inside a model file, append '#ClipName'.")]
			string clip,
			[Description("Optional rig to check the clip against: prefab asset path, asset GUID, or 'scene:Root/Child'. Generic clips are matched by transform path; humanoid clips retarget through the Avatar and are reported as such.")]
			string target = ""
		)
		{
			return UnityAiBridge.Utils.MainThread.Instance.Run(() =>
			{
				AnimationClip resolved = AgentClipResolver.Resolve(clip, "", "");
				if (resolved == null)
					throw new ArgumentException("anim-clip-report needs a clip reference.", nameof(clip));

				var report = new AnimationClipReport
				{
					Clip = resolved.name,
					ClipPath = AssetDatabase.GetAssetPath(resolved),
					Length = resolved.length,
					FrameRate = resolved.frameRate,
					FrameCount = Mathf.RoundToInt(resolved.length * resolved.frameRate),
					IsLooping = resolved.isLooping,
					IsLegacy = resolved.legacy,
					IsHumanMotion = resolved.humanMotion,
					HasRootCurves = resolved.hasRootCurves,
					HasMotionCurves = resolved.hasMotionCurves,
					HasGenericRootTransform = resolved.hasGenericRootTransform,
					WrapMode = resolved.wrapMode.ToString()
				};

				AnimationEvent[] nativeEvents = AnimationUtility.GetAnimationEvents(resolved);
				for (var i = 0; i < nativeEvents.Length; i++)
					report.NativeEvents.Add($"{nativeEvents[i].functionName}@{nativeEvents[i].time:0.###}s");

				EditorCurveBinding[] transformBindings = AnimationUtility.GetCurveBindings(resolved);
				EditorCurveBinding[] objectBindings = AnimationUtility.GetObjectReferenceCurveBindings(resolved);
				report.TransformCurveCount = transformBindings.Length;
				report.ObjectCurveCount = objectBindings.Length;

				var paths = new SortedSet<string>(StringComparer.Ordinal);
				for (var i = 0; i < transformBindings.Length; i++)
					paths.Add(transformBindings[i].path);

				var truncated = 0;
				foreach (string path in paths)
				{
					if (report.AnimatedPaths.Count >= MaxReportedPaths)
					{
						truncated++;
						continue;
					}

					report.AnimatedPaths.Add(string.IsNullOrEmpty(path) ? "(root)" : path);
				}

				if (truncated > 0)
					report.AnimatedPaths.Add($"... {truncated} more paths");

				if (!string.IsNullOrWhiteSpace(target))
					CheckAgainstTarget(report, resolved, paths, target);

				return report;
			});
		}

		private static void CheckAgainstTarget(
			AnimationClipReport report,
			AnimationClip clip,
			SortedSet<string> paths,
			string target)
		{
			GameObject rig = EditorObjectReference.ResolveGameObject(target, out string description);
			var animator = rig.GetComponent<Animator>();
			Avatar avatar = animator == null ? null : animator.avatar;

			if (clip.humanMotion)
			{
				bool humanoid = avatar != null && avatar.isHuman;
				report.TargetSummary = humanoid
					? $"'{description}' is humanoid; the clip retargets through Avatar '{avatar.name}', so transform paths are not compared."
					: $"'{description}' has no humanoid Avatar ({(avatar == null ? "no Avatar" : "generic Avatar")}), " +
					  "so this humanoid clip will not retarget onto it.";
				return;
			}

			var missing = 0;
			foreach (string path in paths)
			{
				if (string.IsNullOrEmpty(path))
					continue;

				if (rig.transform.Find(path) != null)
					continue;

				missing++;
				if (report.UnresolvedPaths.Count < MaxReportedPaths)
					report.UnresolvedPaths.Add(path);
			}

			if (missing > report.UnresolvedPaths.Count)
				report.UnresolvedPaths.Add($"... {missing - report.UnresolvedPaths.Count} more unresolved paths");

			report.TargetSummary =
				$"'{description}': {paths.Count - missing} of {paths.Count} animated paths resolve on this rig" +
				(avatar == null ? " (no Avatar on the root Animator)." : $" (Avatar '{avatar.name}').");
		}

		private static Dictionary<string, string> MapDeclaringSets(AnimationSet set)
		{
			var owners = new Dictionary<string, string>(StringComparer.Ordinal);
			var hierarchy = new List<AnimationSet>();
			var visited = new HashSet<AnimationSet>();

			AnimationSet cursor = set;
			while (cursor != null && visited.Add(cursor))
			{
				hierarchy.Add(cursor);
				cursor = cursor.parentSet;
			}

			// Furthest ancestor first so the nearest set wins, matching GetResolvedEntries.
			for (int i = hierarchy.Count - 1; i >= 0; i--)
			{
				AnimationSet current = hierarchy[i];
				if (current.entries == null)
					continue;

				for (var j = 0; j < current.entries.Length; j++)
				{
					AnimationSetEntry entry = current.entries[j];
					if (entry == null || string.IsNullOrEmpty(entry.id))
						continue;

					owners[entry.id] = current.name;
				}
			}

			return owners;
		}

		private static string DescribeLink(AnimationSetLink link)
		{
			if (link == null || !link.HasNext)
				return "";

			string hold = link.useLinkHold
				? $" hold={link.holdMode} from {link.holdStartNormalizedTime:0.##} for {link.holdDurationSeconds:0.###}s"
				: "";

			return $"next='{link.nextEntryId}' in={link.transitionIn:0.###} out={link.transitionOut:0.###}{hold}";
		}

		private static List<string> CaptureLogs(Action action)
		{
			var lines = new List<string>();
			Application.LogCallback capture = (message, stackTrace, type) => lines.Add($"{type}: {message}");

			Application.logMessageReceived += capture;
			try
			{
				action();
			}
			finally
			{
				Application.logMessageReceived -= capture;
			}

			return lines;
		}
	}
}
#endif
