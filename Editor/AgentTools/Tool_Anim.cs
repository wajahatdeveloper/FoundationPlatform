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

	public sealed class AnimJitterBoneStats
	{
		public string Bone;

		[Description("Second difference of character-local position, m/s².")]
		public float LinearMedian;
		public float LinearP95;
		public float LinearMax;
		public int LinearSpikes;

		[Description("Change in per-frame rotation angle, deg/s².")]
		public float AngularMedian;
		public float AngularP95;
		public float AngularMax;
		public int AngularSpikes;
	}

	public sealed class AnimJitterReport
	{
		public string Target;
		public int Frames;
		public float MeanFrameMs;

		[Description("completed | play-mode-exited | target-destroyed | timed-out")]
		public string EndReason;

		public List<AnimJitterBoneStats> Bones = new();
		public int SpikeCount;

		[Description("Worst spikes first: frame, bone, accelerations, then 'L<layer>:<state>@<layer weight>' for every live layer, plus the previous frame's layers when they changed.")]
		public List<string> Spikes = new();

		[Description("Every frame where the set of playing layer states changed.")]
		public List<string> LayerTimeline = new();
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

		[BridgeTool(AnimSetReportToolId, Title = "Animation / Set Report", ReadOnly = true)]
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

		public const string AnimImportNormalizeToolId = "anim-import-normalize";

		[BridgeTool(AnimImportNormalizeToolId, Title = "Animation / Import Normalize")]
		[Description("Sets humanoid model clip import settings for in-place playback: root transform rotation, Y and " +
			"XZ based upon Original and baked into pose (no root motion), and Loop Pose on clips that loop. Takes " +
			"model files or folders; reimports only models that change and reports every changed clip.")]
		public List<string> ImportNormalize
		(
			[Description("Semicolon separated model file or folder paths under Assets/.")]
			string paths,
			[Description("Skip model files whose path contains this text, e.g. '[RM]' for authored root-motion variants. Empty skips nothing.")]
			string exclude,
			[Description("Report what would change without writing.")]
			bool dryRun
		)
		{
			return UnityAiBridge.Utils.MainThread.Instance.Run(() =>
			{
				var modelPaths = new SortedSet<string>(StringComparer.Ordinal);
				foreach (string raw in paths.Split(';', StringSplitOptions.RemoveEmptyEntries))
				{
					string path = raw.Trim().Replace('\\', '/');
					if (AssetDatabase.IsValidFolder(path))
					{
						foreach (string guid in AssetDatabase.FindAssets("t:Model", new[] { path }))
							modelPaths.Add(AssetDatabase.GUIDToAssetPath(guid));
					}
					else if (AssetImporter.GetAtPath(path) is ModelImporter)
					{
						modelPaths.Add(path);
					}
					else
					{
						throw new ArgumentException($"'{path}' is neither a folder nor a model file.", nameof(paths));
					}
				}

				var changes = new List<string>();
				foreach (string modelPath in modelPaths)
				{
					if (!string.IsNullOrEmpty(exclude) && modelPath.Contains(exclude, StringComparison.Ordinal))
						continue;

					var importer = (ModelImporter)AssetImporter.GetAtPath(modelPath);
					if (importer.animationType != ModelImporterAnimationType.Human)
					{
						changes.Add($"SKIP not humanoid ({importer.animationType}): {modelPath}");
						continue;
					}

					ModelImporterClipAnimation[] clips = importer.clipAnimations.Length > 0
						? importer.clipAnimations
						: importer.defaultClipAnimations;

					var modelChanged = false;
					for (var i = 0; i < clips.Length; i++)
					{
						ModelImporterClipAnimation c = clips[i];
						bool wantLoopPose = c.loopTime;
						if (c.keepOriginalOrientation && c.keepOriginalPositionY && c.keepOriginalPositionXZ
						    && c.lockRootRotation && c.lockRootHeightY && c.lockRootPositionXZ
						    && c.loopPose == wantLoopPose)
							continue;

						changes.Add($"{modelPath}#{c.name}: original+bake, loopPose {c.loopPose}->{wantLoopPose}");
						c.keepOriginalOrientation = true;
						c.keepOriginalPositionY = true;
						c.keepOriginalPositionXZ = true;
						c.lockRootRotation = true;
						c.lockRootHeightY = true;
						c.lockRootPositionXZ = true;
						c.loopPose = wantLoopPose;
						modelChanged = true;
					}

					if (!modelChanged || dryRun)
						continue;

					importer.clipAnimations = clips;
					importer.SaveAndReimport();
				}

				return changes;
			});
		}

		public const string AnimSetEditToolId = "anim-set-edit";

		[BridgeTool(AnimSetEditToolId, Title = "Animation / Set Edit")]
		[Description("Edits entries declared on one AnimationSet in a single undoable batch. One edit per line, fields " +
			"'key=value' separated by '|'. Keys: id (required), clip (asset path, '#ClipName' suffix, or 'none' to " +
			"clear), speed, fade, mask (AnimationMask name), maskAsset (AvatarMask path or 'none'), layer, " +
			"transitionBack, suspendTranslation, template (entry id resolved through the parent chain whose fields " +
			"seed a new entry), remove=true. An id the set does not declare is added (seeded from template when " +
			"given), which is how a child set overrides an inherited entry.")]
		public List<string> SetEdit
		(
			[Description("AnimationSet asset path or GUID.")]
			string animationSet,
			[Description("Newline separated edits, e.g. 'id=Run_Fwd|clip=Assets/.../Run.fbx|speed=1'.")]
			string edits
		)
		{
			return UnityAiBridge.Utils.MainThread.Instance.Run(() =>
			{
				var set = EditorObjectReference.LoadAsset<AnimationSet>(animationSet);
				Undo.RecordObject(set, "Agent AnimationSet Edit");
				var entries = new List<AnimationSetEntry>(set.entries);
				var log = new List<string>();

				foreach (string rawLine in edits.Split('\n', StringSplitOptions.RemoveEmptyEntries))
				{
					string line = rawLine.Trim();
					if (line.Length == 0)
						continue;

					var fields = new Dictionary<string, string>(StringComparer.Ordinal);
					foreach (string part in line.Split('|'))
					{
						int eq = part.IndexOf('=');
						if (eq <= 0)
							throw new ArgumentException($"Field '{part}' in '{line}' is not 'key=value'.", nameof(edits));
						fields[part.Substring(0, eq).Trim()] = part.Substring(eq + 1).Trim();
					}

					if (!fields.TryGetValue("id", out string id) || id.Length == 0)
						throw new ArgumentException($"Edit '{line}' has no id.", nameof(edits));

					int index = entries.FindIndex(e => e != null && e.id == id);
					if (fields.TryGetValue("remove", out string remove) && bool.Parse(remove))
					{
						if (index < 0)
							throw new InvalidOperationException($"'{set.name}' declares no entry '{id}' to remove.");
						entries.RemoveAt(index);
						log.Add($"{id}: removed");
						continue;
					}

					AnimationSetEntry entry;
					if (index >= 0)
					{
						entry = entries[index];
					}
					else
					{
						entry = new AnimationSetEntry { id = id, clip = new ClipTransitionData() };
						if (fields.TryGetValue("template", out string template))
						{
							if (!set.GetResolvedEntries().TryGetValue(template, out AnimationSetEntry source))
								throw new InvalidOperationException($"'{set.name}' resolves no template entry '{template}'.");
							EditorJsonUtility.FromJsonOverwrite(EditorJsonUtility.ToJson(source), entry);
							entry.clip = new ClipTransitionData();
							EditorJsonUtility.FromJsonOverwrite(EditorJsonUtility.ToJson(source.clip), entry.clip);
							entry.id = id;
						}
						entries.Add(entry);
						log.Add($"{id}: added");
					}

					foreach (KeyValuePair<string, string> field in fields)
					{
						switch (field.Key)
						{
							case "id":
							case "template":
								break;
							case "clip":
								entry.clip.Clip = field.Value == "none" ? null : AgentClipResolver.Resolve(field.Value, "", "");
								break;
							case "speed":
								entry.clip.Speed = float.Parse(field.Value, System.Globalization.CultureInfo.InvariantCulture);
								break;
							case "fade":
								entry.clip.FadeDuration = float.Parse(field.Value, System.Globalization.CultureInfo.InvariantCulture);
								break;
							case "mask":
								entry.mask = (AnimationMask)Enum.Parse(typeof(AnimationMask), field.Value);
								break;
							case "maskAsset":
								entry.maskAsset = field.Value == "none" ? null : EditorObjectReference.LoadAsset<AvatarMask>(field.Value);
								break;
							case "layer":
								entry.layerIndex = int.Parse(field.Value, System.Globalization.CultureInfo.InvariantCulture);
								break;
							case "transitionBack":
								entry.transitionBack = bool.Parse(field.Value);
								break;
							case "suspendTranslation":
								entry.suspendTranslation = bool.Parse(field.Value);
								break;
							default:
								throw new ArgumentException($"Unknown key '{field.Key}' in '{line}'.", nameof(edits));
						}
					}

					log.Add($"{id}: clip={(entry.clip.Clip == null ? "none" : entry.clip.Clip.name)} speed={entry.clip.Speed} " +
						$"fade={entry.clip.FadeDuration} mask={entry.mask} maskAsset={(entry.maskAsset == null ? "none" : entry.maskAsset.name)}");
				}

				set.entries = entries.ToArray();
				EditorUtility.SetDirty(set);
				AssetDatabase.SaveAssetIfDirty(set);
				return log;
			});
		}

		public const string AnimClipReportToolId = "anim-clip-report";

		[BridgeTool(AnimClipReportToolId, Title = "Animation / Clip Report", ReadOnly = true)]
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

		public const string AnimJitterProbeToolId = "anim-jitter-probe";

		private static readonly HumanBodyBones[] JitterBones =
		{
			HumanBodyBones.Hips, HumanBodyBones.Chest, HumanBodyBones.Head,
			HumanBodyBones.LeftHand, HumanBodyBones.RightHand,
			HumanBodyBones.LeftFoot, HumanBodyBones.RightFoot
		};

		private const int MaxReportedSpikes = 30;

		[BridgeTool(AnimJitterProbeToolId, Title = "Animation / Jitter Probe")]
		[Description("Play Mode: records key humanoid bones of one live character every rendered frame in " +
			"character-local space (after animation, Animation Rigging IK and LateUpdate overrides), then reports " +
			"per-bone linear and angular acceleration (median, p95, max) and the frames where a bone spikes above " +
			"'spikeFactor' x its median, each tagged with what every PlayableGraphBridge layer was playing. " +
			"Long runs: '_async': true.")]
		public static System.Threading.Tasks.Task<AnimJitterReport> JitterProbe
		(
			[Description("'scene:Root/Child' path of a live character with a humanoid Animator.")]
			string target,
			[Description("Frames to record, 10..3000.")]
			int frames,
			[Description("A frame is a spike when its acceleration exceeds this multiple of the bone's median. 6 is a good start.")]
			float spikeFactor,
			[Description("Stop after this many real seconds.")]
			float maxSeconds,
			[Description("Force AnimatorCullingMode.AlwaysAnimate for the sample window and restore it after, so an off-camera character is not measured as a frozen pose.")]
			bool forceAlwaysAnimate
		)
		{
			if (!EditorApplication.isPlaying)
				throw new InvalidOperationException($"{AnimJitterProbeToolId} requires Play Mode.");
			if (frames < 10 || frames > 3000)
				throw new ArgumentException("'frames' must be in [10, 3000].", nameof(frames));

			GameObject go = EditorObjectReference.ResolveGameObject(target, out string description);
			var animator = go.GetComponentInChildren<Animator>();
			if (animator == null || !animator.isHuman)
				throw new ArgumentException($"'{description}' has no humanoid Animator.", nameof(target));
			var bridge = go.GetComponentInChildren<PlayableGraphBridge>();
			if (bridge == null || bridge.Layers == null)
				throw new ArgumentException($"'{description}' has no initialized PlayableGraphBridge.", nameof(target));

			var bones = new Transform[JitterBones.Length];
			for (var i = 0; i < bones.Length; i++)
				bones[i] = animator.GetBoneTransform(JitterBones[i]);

			Transform root = animator.transform;
			var positions = new List<Vector3[]>();
			var rotations = new List<Quaternion[]>();
			var times = new List<float>();
			var labels = new List<string>();
			var tcs = new System.Threading.Tasks.TaskCompletionSource<AnimJitterReport>();
			int lastFrame = -1;
			double start = EditorApplication.timeSinceStartup;
			string endReason = "completed";
			AnimatorCullingMode originalCulling = animator.cullingMode;
			if (forceAlwaysAnimate)
				animator.cullingMode = AnimatorCullingMode.AlwaysAnimate;

			void Finish(string reason)
			{
				EditorApplication.update -= Tick;
				if (forceAlwaysAnimate && animator != null)
					animator.cullingMode = originalCulling;
				endReason = reason;
				tcs.TrySetResult(BuildJitterReport(description, positions, rotations, times, labels, spikeFactor, endReason));
			}

			void Tick()
			{
				if (!EditorApplication.isPlaying) { Finish("play-mode-exited"); return; }
				if (root == null) { Finish("target-destroyed"); return; }
				if (EditorApplication.timeSinceStartup - start > maxSeconds) { Finish("timed-out"); return; }
				if (Time.frameCount == lastFrame || EditorApplication.isPaused)
					return;
				lastFrame = Time.frameCount;

				var p = new Vector3[bones.Length];
				var r = new Quaternion[bones.Length];
				Quaternion inverseRoot = Quaternion.Inverse(root.rotation);
				for (var i = 0; i < bones.Length; i++)
				{
					if (bones[i] == null)
						continue;
					p[i] = root.InverseTransformPoint(bones[i].position);
					r[i] = inverseRoot * bones[i].rotation;
				}

				positions.Add(p);
				rotations.Add(r);
				times.Add(Time.time);
				labels.Add(DescribeLayers(bridge));

				if (positions.Count >= frames)
					Finish("completed");
			}

			EditorApplication.update += Tick;
			return tcs.Task;
		}

		private static string DescribeLayers(PlayableGraphBridge bridge)
		{
			var sb = new System.Text.StringBuilder();
			for (var i = 0; i < bridge.Layers.Count; i++)
			{
				PlayableLayer layer = bridge.Layers[i];
				if (layer.Weight <= 0.001f || layer.CurrentState == null)
					continue;
				string state = layer.CurrentState is ClipState clip && clip.Clip != null
					? clip.Clip.name
					: layer.CurrentState.GetType().Name;
				int fading = 0;
				for (var s = 0; s < layer.ActiveStates.Count; s++)
					if (layer.ActiveStates[s].Weight > 0.001f && layer.ActiveStates[s].Weight < 0.999f)
						fading++;
				if (sb.Length > 0)
					sb.Append(" | ");
				sb.Append($"L{i}:{state}@{layer.Weight:0.##}");
				if (fading > 0)
					sb.Append($" (fading {fading})");
			}

			return sb.ToString();
		}

		private static AnimJitterReport BuildJitterReport(
			string description,
			List<Vector3[]> positions,
			List<Quaternion[]> rotations,
			List<float> times,
			List<string> labels,
			float spikeFactor,
			string endReason)
		{
			var report = new AnimJitterReport
			{
				Target = description,
				Frames = positions.Count,
				EndReason = endReason
			};
			if (positions.Count < 4)
				return report;

			float meanDt = (times[times.Count - 1] - times[0]) / (times.Count - 1);
			report.MeanFrameMs = meanDt * 1000f;
			float dt2 = Mathf.Max(meanDt * meanDt, 1e-6f);

			int n = positions.Count;
			var linear = new float[JitterBones.Length][];
			var angular = new float[JitterBones.Length][];
			for (var b = 0; b < JitterBones.Length; b++)
			{
				linear[b] = new float[n - 2];
				angular[b] = new float[n - 2];
				for (var t = 2; t < n; t++)
				{
					Vector3 second = positions[t][b] - 2f * positions[t - 1][b] + positions[t - 2][b];
					linear[b][t - 2] = second.magnitude / dt2;
					float w1 = Quaternion.Angle(rotations[t - 2][b], rotations[t - 1][b]);
					float w2 = Quaternion.Angle(rotations[t - 1][b], rotations[t][b]);
					angular[b][t - 2] = Mathf.Abs(w2 - w1) / dt2;
				}
			}

			var spikes = new List<(float score, string line)>();
			for (var b = 0; b < JitterBones.Length; b++)
			{
				var stats = new AnimJitterBoneStats { Bone = JitterBones[b].ToString() };
				Summarize(linear[b], out stats.LinearMedian, out stats.LinearP95, out stats.LinearMax);
				Summarize(angular[b], out stats.AngularMedian, out stats.AngularP95, out stats.AngularMax);

				float linearGate = Mathf.Max(stats.LinearMedian * spikeFactor, 5f);
				float angularGate = Mathf.Max(stats.AngularMedian * spikeFactor, 2000f);
				for (var i = 0; i < linear[b].Length; i++)
				{
					int frame = i + 2;
					bool lin = linear[b][i] > linearGate;
					bool ang = angular[b][i] > angularGate;
					if (!lin && !ang)
						continue;
					if (lin) stats.LinearSpikes++;
					if (ang) stats.AngularSpikes++;
					float score = Mathf.Max(linear[b][i] / linearGate, angular[b][i] / angularGate);
					string transition = labels[frame] == labels[frame - 1] ? "" : $" WAS {labels[frame - 1]}";
					spikes.Add((score, $"f{frame} {stats.Bone} lin={linear[b][i]:0.#} ang={angular[b][i]:0} :: {labels[frame]}{transition}"));
				}

				report.Bones.Add(stats);
			}

			spikes.Sort((a, c) => c.score.CompareTo(a.score));
			for (var i = 0; i < spikes.Count && i < MaxReportedSpikes; i++)
				report.Spikes.Add(spikes[i].line);
			report.SpikeCount = spikes.Count;

			for (var t = 0; t < labels.Count; t++)
				if (t == 0 || labels[t] != labels[t - 1])
					report.LayerTimeline.Add($"f{t} {labels[t]}");
			if (report.LayerTimeline.Count > 40)
				report.LayerTimeline.RemoveRange(40, report.LayerTimeline.Count - 40);

			return report;
		}

		private static void Summarize(float[] values, out float median, out float p95, out float max)
		{
			var sorted = (float[])values.Clone();
			Array.Sort(sorted);
			median = sorted[sorted.Length / 2];
			p95 = sorted[Mathf.Min(sorted.Length - 1, (int)(sorted.Length * 0.95f))];
			max = sorted[sorted.Length - 1];
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
