#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using AetherNexus.FoundationPlatform.Animation;
using AetherNexus.FoundationPlatform.Editor.Animation;
using AetherNexus.FoundationPlatform.Editor.Utilities;
using UnityAiBridge;
using UnityEditor;
using UnityEngine;
using UnityEngine.Playables;

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

		[Description("Root-motion windows as 'position=a..b rotation=c..d' for the channels the mode drives; empty when root motion is off.")]
		public string RootMotionWindows;

		[Description("Gameplay tags held over part of the clip, as 'tag@start..end'.")]
		public List<string> TagWindows = new();

		[Description("Per-predecessor start times, as 'sourceEntryId->startNormalizedTime'.")]
		public List<string> SourceStartOffsets = new();

		[Description("Named entry curves read with TryGetTimelineCurveValue, as 'name(keys)'.")]
		public List<string> Curves = new();
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

		[Description("Tag window, source start offset, curve and root-motion window errors on the set's own entries.")]
		public List<string> EntryWindowErrors = new();

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

	public sealed class AnimClipCompareReport
	{
		public string ClipA;
		public string ClipB;
		public string Rig;
		public int Samples;

		[Description("Set when expectBone was passed: the rotation added to clip A's pose before comparing, as 'Bone euler(x,y,z)'.")]
		public string Expectation;

		[Description("Worst angle between the two clips per compared bone, degrees, largest first. Bones under 0.05 degrees are left out.")]
		public List<string> BoneAngles = new();

		public float MaxBoneAngle;

		[Description("Muscles whose largest difference exceeds muscleThreshold, as 'name max=d', largest first.")]
		public List<string> MuscleDiffs = new();

		[Description("Length, loop, frame rate and clip settings that differ between the clips.")]
		public List<string> SettingDiffs = new();

		public string Verdict;
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
			"motion mode and windows, sequence link, tag windows, source start offsets and curves, which parent set declared it, " +
			"plus all validation findings — required entries, sequence-link errors, entry window errors, missing clips and event " +
			"names no code declares. Read-only.")]
		public AnimationSetReport SetReport
		(
			[Description("AnimationSet asset path or GUID.")]
			string animationSet,
			[Description("Also run the standard AnimationSet validation and capture everything it logs. This writes to the Editor console, exactly as the designer-facing validation does.")]
			bool runValidationLog = true,
			[Description("Comma-separated entry ids to report. Empty = every resolved entry. Findings always cover the whole set.")]
			string entries = "",
			[Description("Report only the set header and findings, no entries.")]
			bool findingsOnly = false
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
						Link = DescribeLink(entry.link),
						RootMotionWindows = DescribeRootMotionWindows(entry)
					};

					if (entry.tagWindows != null)
					{
						foreach (AnimationTagWindow w in entry.tagWindows)
						{
							if (w != null)
								entryReport.TagWindows.Add($"{w.tagName}@{w.normalizedWindow.x:0.###}..{w.normalizedWindow.y:0.###}");
						}
					}

					if (entry.sourceStartOffsets != null)
					{
						foreach (AnimationSourceStartOffset o in entry.sourceStartOffsets)
						{
							if (o != null)
								entryReport.SourceStartOffsets.Add($"{o.sourceEntryId}->{o.startNormalizedTime:0.###}");
						}
					}

					if (entry.curves != null)
					{
						foreach (AnimationEntryCurve c in entry.curves)
						{
							if (c != null)
								entryReport.Curves.Add($"{c.curveName}({(c.curve == null ? 0 : c.curve.length)})");
						}
					}

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
				AnimationSetValidator.CollectEntryWindowValidation(set, resolved, report.EntryWindowErrors);

				if (runValidationLog)
					report.ValidationLog.AddRange(CaptureLogs(() => AnimationSetValidator.LogValidation(set)));

				if (findingsOnly)
				{
					report.Entries.Clear();
				}
				else if (!string.IsNullOrWhiteSpace(entries))
				{
					var wanted = new HashSet<string>(entries.Split(',').Select(e => e.Trim()).Where(e => e.Length > 0), StringComparer.Ordinal);
					report.Entries.RemoveAll(e => !wanted.Contains(e.Id));
				}

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
			"transitionBack, fadeBack (seconds back to the layer base; 0 = reuse fade), suspendTranslation, next (sequence link " +
			"target entry id, 'none' to clear), linkIn (blend seconds into the linked entry), template (entry id resolved through the parent chain whose fields " +
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
							case "fadeBack":
								entry.transitionBackFadeDuration = float.Parse(field.Value, System.Globalization.CultureInfo.InvariantCulture);
								break;
							case "suspendTranslation":
								entry.suspendTranslation = bool.Parse(field.Value);
								break;
							case "next":
								entry.link ??= new AnimationSetLink();
								entry.link.nextEntryId = field.Value == "none" ? string.Empty : field.Value;
								break;
							case "linkIn":
								entry.link ??= new AnimationSetLink();
								entry.link.transitionIn = float.Parse(field.Value, System.Globalization.CultureInfo.InvariantCulture);
								break;
							default:
								throw new ArgumentException($"Unknown key '{field.Key}' in '{line}'.", nameof(edits));
						}
					}

					log.Add($"{id}: clip={(entry.clip.Clip == null ? "none" : entry.clip.Clip.name)} speed={entry.clip.Speed} " +
						$"fade={entry.clip.FadeDuration} fadeBack={entry.transitionBackFadeDuration} mask={entry.mask} maskAsset={(entry.maskAsset == null ? "none" : entry.maskAsset.name)}" +
						(entry.link != null && !string.IsNullOrEmpty(entry.link.nextEntryId) ? $" next={entry.link.nextEntryId} linkIn={entry.link.transitionIn}" : ""));
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

		private static string DescribeRootMotionWindows(AnimationSetEntry entry)
		{
			string position = entry.DrivesRootMotionPosition
				? $"position={entry.rootMotionPositionWindow.x:0.###}..{entry.rootMotionPositionWindow.y:0.###}"
				: "";
			string rotation = entry.DrivesRootMotionRotation
				? $"rotation={entry.rootMotionRotationWindow.x:0.###}..{entry.rootMotionRotationWindow.y:0.###}"
				: "";
			return (position + " " + rotation).Trim();
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

		public const string AnimClipCompareToolId = "anim-clip-compare";

		[BridgeTool(AnimClipCompareToolId, Title = "Animation / Clip Compare", ReadOnly = true)]
		[Description("Samples two humanoid clips on the same rig in Edit Mode and reports how far their poses differ: worst " +
			"angle per bone, muscles that differ, and clip settings that differ. Pass expectBone + expectEuler to check that " +
			"clip B equals clip A with a local rotation added to one bone (e.g. a baked wrist correction). The rig is copied " +
			"and forced to AlwaysAnimate, and a clip whose samples never move fails instead of comparing frozen poses. Read-only.")]
		public AnimClipCompareReport ClipCompare
		(
			[Description("Clip A: AnimationClip asset path or GUID; for a clip inside a model file append '#ClipName'.")]
			string clipA,
			[Description("Clip B, same format as clipA.")]
			string clipB,
			[Description("Rig with a humanoid Animator: prefab asset path, asset GUID, or 'scene:Root/Child'. A copy is sampled; the original is not touched.")]
			string rig,
			[Description("Sample times spread evenly over the shorter clip, 2..60.")]
			int samples = 5,
			[Description("Comma-separated HumanBodyBones to compare. Empty = every bone the avatar maps.")]
			string bones = "",
			[Description("Optional HumanBodyBones name: clip A's pose gets expectEuler added to this bone's local rotation before comparing.")]
			string expectBone = "",
			[Description("Local rotation added to expectBone, as 'x,y,z' degrees.")]
			string expectEuler = "",
			[Description("Muscle differences above this are listed.")]
			float muscleThreshold = 0.01f
		)
		{
			if (samples < 2 || samples > 60)
				throw new ArgumentException("'samples' must be in [2, 60].", nameof(samples));

			return UnityAiBridge.Utils.MainThread.Instance.Run(() =>
			{
				AnimationClip a = AgentClipResolver.Resolve(clipA, "", "");
				AnimationClip b = AgentClipResolver.Resolve(clipB, "", "");
				if (a == null || b == null)
					throw new ArgumentException("anim-clip-compare needs both clipA and clipB.");
				if (!a.humanMotion || !b.humanMotion)
					throw new ArgumentException($"Both clips must be humanoid ('{a.name}' human={a.humanMotion}, '{b.name}' human={b.humanMotion}).");

				GameObject source = EditorObjectReference.ResolveGameObject(rig, out string description);
				var report = new AnimClipCompareReport
				{
					ClipA = $"{AssetDatabase.GetAssetPath(a)}#{a.name}",
					ClipB = $"{AssetDatabase.GetAssetPath(b)}#{b.name}",
					Rig = description,
					Samples = samples,
				};
				CompareClipSettings(a, b, report.SettingDiffs);

				HumanBodyBones expectHuman = HumanBodyBones.LastBone;
				Quaternion expectRotation = Quaternion.identity;
				if (!string.IsNullOrWhiteSpace(expectBone))
				{
					if (!Enum.TryParse(expectBone.Trim(), true, out expectHuman) || expectHuman == HumanBodyBones.LastBone)
						throw new ArgumentException($"expectBone '{expectBone}' is not a HumanBodyBones name.", nameof(expectBone));
					expectRotation = Quaternion.Euler(ParseVector3(expectEuler, nameof(expectEuler)));
					report.Expectation = $"{expectHuman} euler({expectRotation.eulerAngles.x:0.##},{expectRotation.eulerAngles.y:0.##},{expectRotation.eulerAngles.z:0.##})";
				}

				var instance = UnityEngine.Object.Instantiate(source);
				instance.hideFlags = HideFlags.HideAndDontSave;
				try
				{
					var animator = instance.GetComponentsInChildren<Animator>(true).FirstOrDefault(x => x.avatar != null && x.avatar.isHuman);
					if (animator == null)
						throw new ArgumentException($"'{description}' has no Animator with a humanoid Avatar.", nameof(rig));
					animator.cullingMode = AnimatorCullingMode.AlwaysAnimate;
					animator.applyRootMotion = false;

					var compared = ResolveCompareBones(animator, bones);
					Transform expectTransform = null;
					if (expectHuman != HumanBodyBones.LastBone)
					{
						expectTransform = animator.GetBoneTransform(expectHuman);
						if (expectTransform == null)
							throw new ArgumentException($"The avatar on '{description}' does not map {expectHuman}.", nameof(expectBone));
					}

					var worstAngle = new float[compared.Count];
					var worstMuscle = new float[HumanTrait.MuscleCount];
					var handler = new HumanPoseHandler(animator.avatar, animator.transform);
					var pose = new HumanPose();
					var aFirst = new Quaternion[compared.Count];
					var bFirst = new Quaternion[compared.Count];
					bool aMoved = false, bMoved = false;
					float length = Mathf.Min(a.length, b.length);
					try
					{
						for (int s = 0; s < samples; s++)
						{
							float time = length * s / (samples - 1);

							SampleHumanoidClip(animator, a, time);
							if (expectTransform != null)
								expectTransform.localRotation *= expectRotation;
							var aRot = CaptureCharacterSpace(animator.transform, compared);
							handler.GetHumanPose(ref pose);
							var aMuscles = (float[])pose.muscles.Clone();

							SampleHumanoidClip(animator, b, time);
							var bRot = CaptureCharacterSpace(animator.transform, compared);
							handler.GetHumanPose(ref pose);

							for (int i = 0; i < compared.Count; i++)
							{
								worstAngle[i] = Mathf.Max(worstAngle[i], Quaternion.Angle(aRot[i], bRot[i]));
								if (s == 0) { aFirst[i] = aRot[i]; bFirst[i] = bRot[i]; }
								else
								{
									aMoved |= Quaternion.Angle(aFirst[i], aRot[i]) > 0.01f;
									bMoved |= Quaternion.Angle(bFirst[i], bRot[i]) > 0.01f;
								}
							}

							for (int m = 0; m < worstMuscle.Length; m++)
								worstMuscle[m] = Mathf.Max(worstMuscle[m], Mathf.Abs(aMuscles[m] - pose.muscles[m]));
						}
					}
					finally
					{
						handler.Dispose();
					}

					if (!aMoved || !bMoved)
						throw new InvalidOperationException(
							$"anim-clip-compare: every sample of {(!aMoved ? $"'{a.name}'" : $"'{b.name}'")} returned the same pose on '{description}'. " +
							"The graph did not evaluate (or the clip is a single held pose), so a comparison would be meaningless.");

					for (int i = 0; i < compared.Count; i++)
						report.MaxBoneAngle = Mathf.Max(report.MaxBoneAngle, worstAngle[i]);
					report.BoneAngles = Enumerable.Range(0, compared.Count)
						.Where(i => worstAngle[i] >= 0.05f)
						.OrderByDescending(i => worstAngle[i])
						.Select(i => $"{compared[i].bone} {worstAngle[i]:0.##}")
						.ToList();
					report.MuscleDiffs = Enumerable.Range(0, worstMuscle.Length)
						.Where(m => worstMuscle[m] > muscleThreshold)
						.OrderByDescending(m => worstMuscle[m])
						.Select(m => $"{HumanTrait.MuscleName[m]} max={worstMuscle[m]:0.###}")
						.ToList();

					string subject = expectTransform != null ? "B vs A + expectation" : "B vs A";
					report.Verdict = $"{subject}: worst bone {report.MaxBoneAngle:0.##} deg over {compared.Count} bones, " +
					                 $"{report.MuscleDiffs.Count} muscles above {muscleThreshold}, {report.SettingDiffs.Count} setting differences.";
				}
				finally
				{
					UnityEngine.Object.DestroyImmediate(instance);
				}

				return report;
			});
		}

		private static List<(HumanBodyBones bone, Transform transform)> ResolveCompareBones(Animator animator, string bones)
		{
			var result = new List<(HumanBodyBones, Transform)>();
			if (string.IsNullOrWhiteSpace(bones))
			{
				for (int i = 0; i < (int)HumanBodyBones.LastBone; i++)
				{
					var t = animator.GetBoneTransform((HumanBodyBones)i);
					if (t != null)
						result.Add(((HumanBodyBones)i, t));
				}
				return result;
			}

			foreach (string part in bones.Split(','))
			{
				string name = part.Trim();
				if (name.Length == 0)
					continue;
				if (!Enum.TryParse(name, true, out HumanBodyBones bone) || bone == HumanBodyBones.LastBone)
					throw new ArgumentException($"'{name}' is not a HumanBodyBones name.", nameof(bones));
				var t = animator.GetBoneTransform(bone);
				if (t == null)
					throw new ArgumentException($"The avatar does not map {bone}.", nameof(bones));
				result.Add((bone, t));
			}
			return result;
		}

		private static void SampleHumanoidClip(Animator animator, AnimationClip clip, float time)
		{
			var graph = UnityEngine.Playables.PlayableGraph.Create("anim-clip-compare");
			try
			{
				graph.SetTimeUpdateMode(UnityEngine.Playables.DirectorUpdateMode.Manual);
				var output = UnityEngine.Animations.AnimationPlayableOutput.Create(graph, "compare", animator);
				var playable = UnityEngine.Animations.AnimationClipPlayable.Create(graph, clip);
				playable.SetApplyFootIK(false);
				output.SetSourcePlayable(playable);
				playable.SetTime(time);
				graph.Evaluate(0f);
			}
			finally
			{
				graph.Destroy();
			}
		}

		private static Quaternion[] CaptureCharacterSpace(Transform root, List<(HumanBodyBones bone, Transform transform)> bones)
		{
			var inverse = Quaternion.Inverse(root.rotation);
			var result = new Quaternion[bones.Count];
			for (int i = 0; i < bones.Count; i++)
				result[i] = inverse * bones[i].transform.rotation;
			return result;
		}

		private static void CompareClipSettings(AnimationClip a, AnimationClip b, List<string> diffs)
		{
			void Check<T>(string name, T x, T y)
			{
				if (!EqualityComparer<T>.Default.Equals(x, y))
					diffs.Add($"{name}: {x} vs {y}");
			}

			Check("length", Mathf.Round(a.length * 1000f) / 1000f, Mathf.Round(b.length * 1000f) / 1000f);
			Check("frameRate", a.frameRate, b.frameRate);
			Check("isLooping", a.isLooping, b.isLooping);
			var sa = AnimationUtility.GetAnimationClipSettings(a);
			var sb = AnimationUtility.GetAnimationClipSettings(b);
			Check("loopTime", sa.loopTime, sb.loopTime);
			Check("loopBlend", sa.loopBlend, sb.loopBlend);
			Check("cycleOffset", sa.cycleOffset, sb.cycleOffset);
			Check("mirror", sa.mirror, sb.mirror);
			Check("loopBlendOrientation", sa.loopBlendOrientation, sb.loopBlendOrientation);
			Check("loopBlendPositionY", sa.loopBlendPositionY, sb.loopBlendPositionY);
			Check("loopBlendPositionXZ", sa.loopBlendPositionXZ, sb.loopBlendPositionXZ);
			Check("keepOriginalOrientation", sa.keepOriginalOrientation, sb.keepOriginalOrientation);
			Check("keepOriginalPositionY", sa.keepOriginalPositionY, sb.keepOriginalPositionY);
			Check("keepOriginalPositionXZ", sa.keepOriginalPositionXZ, sb.keepOriginalPositionXZ);
			Check("heightFromFeet", sa.heightFromFeet, sb.heightFromFeet);
			Check("orientationOffsetY", sa.orientationOffsetY, sb.orientationOffsetY);
			Check("level", sa.level, sb.level);
			Check("events", AnimationUtility.GetAnimationEvents(a).Length, AnimationUtility.GetAnimationEvents(b).Length);
		}

		private static Vector3 ParseVector3(string value, string parameter)
		{
			var parts = (value ?? "").Split(',');
			if (parts.Length != 3
			    || !float.TryParse(parts[0], System.Globalization.NumberStyles.Float, System.Globalization.CultureInfo.InvariantCulture, out float x)
			    || !float.TryParse(parts[1], System.Globalization.NumberStyles.Float, System.Globalization.CultureInfo.InvariantCulture, out float y)
			    || !float.TryParse(parts[2], System.Globalization.NumberStyles.Float, System.Globalization.CultureInfo.InvariantCulture, out float z))
				throw new ArgumentException($"'{value}' is not 'x,y,z'.", parameter);
			return new Vector3(x, y, z);
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
