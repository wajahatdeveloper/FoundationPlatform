#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Globalization;
using System.IO;
using AetherNexus.FoundationPlatform.Animation;
using AetherNexus.FoundationPlatform.Editor.Utilities;
using UnityAiBridge;
using UnityEditor;
using UnityEngine;
using Object = UnityEngine.Object;

namespace AetherNexus.FoundationPlatform.AgentTools.Editor
{
	public sealed class RenderResult
	{
		[Description("Absolute path to the saved PNG. Read it to see the render.")]
		public string Path;

		public int Width;
		public int Height;
		public long FileSize;

		[Description("What was rendered: asset path, GUID-resolved path, or scene:<hierarchy path>.")]
		public string Target;

		[Description("Clip sampled for this render, or empty when the target was rendered in its authored pose.")]
		public string Clip;

		[Description("Camera framing actually used, so a follow-up call can reproduce or nudge this render.")]
		public string Framing;

		[Description("One entry per rendered cell, in reading order.")]
		public List<string> Cells = new();

		[Description("Set only by render-compare: fraction of pixels that differ beyond a small threshold.")]
		public float ChangedFraction;
	}

	/// <summary>
	/// Renders models, prefabs, poses and animation frames to PNG for agents. The bridge's
	/// <c>screenshot-capture</c> only sees the Scene or Game camera, so animation and IK work — which is
	/// judged visually — had no verifiable outcome for an agent. These tools render off-scene through
	/// <see cref="PreviewRenderUtility"/>: fixed lighting, fit-to-bounds framing, no scene mutation.
	/// </summary>
	[BridgeToolType]
	public partial class Tool_Render
	{
		public const string RenderPreviewToolId = "render-preview";

		[BridgeTool(RenderPreviewToolId, Title = "Render / Preview")]
		[Description("Renders one model, prefab or scene object to a PNG at a chosen angle, optionally posed at a " +
			"point in an AnimationClip, and returns the file path. Runs in an off-scene preview scene with fixed " +
			"lighting: no open scene is touched and no asset is modified. Targeting a live scene object clones its " +
			"current transforms, which is how an evaluated IK pose gets captured (rigs do not re-evaluate off-scene).")]
		public RenderResult Preview
		(
			[Description("Asset path ('Assets/.../Hero.prefab'), asset GUID, or 'scene:Root/Child' for an object in a loaded scene or open prefab stage.")]
			string target,
			[Description("Optional AnimationClip asset path or GUID. For a clip inside a model file, append '#ClipName'. Leave empty to render the authored pose.")]
			string clip = "",
			[Description("Optional AnimationSet asset path or GUID; used with 'entry' instead of 'clip' to render what a set entry actually plays.")]
			string animationSet = "",
			[Description("AnimationSet entry id, required when 'animationSet' is set. Resolved through the set's parent chain.")]
			string entry = "",
			[Description("Point along the clip, 0 = first frame, 1 = last frame.")]
			float normalizedTime = 0f,
			[Description("Camera yaw in degrees around the target. 0 looks at the target's -Z face, 35 is a three-quarter view.")]
			float yaw = 35f,
			[Description("Camera pitch in degrees. Positive looks down at the target.")]
			float pitch = 12f,
			[Description("Vertical field of view in degrees.")]
			float fieldOfView = 30f,
			[Description("Framing slack around the fitted bounds. 1.0 fills the frame, higher zooms out.")]
			float padding = 1.08f,
			[Description("Output width in pixels.")]
			int width = 640,
			[Description("Output height in pixels.")]
			int height = 640,
			[Description("Optional filename prefix, e.g. 'idle-front'.")]
			string tag = ""
		)
		{
			return UnityAiBridge.Utils.MainThread.Instance.Run(() =>
			{
				GameObject source = EditorObjectReference.ResolveGameObject(target, out string description);
				AnimationClip resolvedClip = AgentClipResolver.Resolve(clip, animationSet, entry);

				using var session = new AgentPreviewSession(source, description);
				session.Sample(resolvedClip, normalizedTime);

				Texture2D texture = session.Render(width, height, yaw, pitch, fieldOfView, padding);
				try
				{
					string path = AgentPreviewIO.Write(texture, string.IsNullOrWhiteSpace(tag) ? source.name : tag);
					var result = BuildResult(path, width, height, description, resolvedClip, session, yaw, pitch, fieldOfView, padding);
					result.Cells.Add(CellLabel(resolvedClip, normalizedTime, yaw));
					return result;
				}
				finally
				{
					Object.DestroyImmediate(texture);
				}
			});
		}

		public const string RenderClipStripToolId = "render-clip-strip";

		[BridgeTool(RenderClipStripToolId, Title = "Render / Clip Strip")]
		[Description("Renders a contact sheet of one target across an animation, one labelled cell per sampled time " +
			"and one row per camera yaw. This is the primary way to review an animation without watching it: the " +
			"whole motion arrives as a single PNG with 't=' printed in every cell.")]
		public RenderResult ClipStrip
		(
			[Description("Asset path, asset GUID, or 'scene:Root/Child'.")]
			string target,
			[Description("AnimationClip asset path or GUID, '#ClipName' suffixed for clips inside a model file. Either this or 'animationSet' + 'entry' is required.")]
			string clip = "",
			[Description("AnimationSet asset path or GUID, used with 'entry' instead of 'clip'.")]
			string animationSet = "",
			[Description("AnimationSet entry id, required when 'animationSet' is set.")]
			string entry = "",
			[Description("Number of evenly spaced samples over the clip, first frame to last. Ignored when 'times' is set.")]
			int frames = 6,
			[Description("Optional explicit normalized times, comma separated, e.g. '0,0.15,0.4,1'. Overrides 'frames'.")]
			string times = "",
			[Description("Optional camera yaws in degrees, comma separated, e.g. '0,90'. One row of cells per yaw.")]
			string yaws = "",
			[Description("Camera pitch in degrees for every cell.")]
			float pitch = 12f,
			[Description("Vertical field of view in degrees.")]
			float fieldOfView = 30f,
			[Description("Framing slack around the fitted bounds; the same framing is used for every cell so the motion is comparable.")]
			float padding = 1.15f,
			[Description("Width of one cell in pixels.")]
			int cellWidth = 320,
			[Description("Height of one cell in pixels.")]
			int cellHeight = 320,
			[Description("Optional filename prefix.")]
			string tag = ""
		)
		{
			return UnityAiBridge.Utils.MainThread.Instance.Run(() =>
			{
				GameObject source = EditorObjectReference.ResolveGameObject(target, out string description);
				AnimationClip resolvedClip = AgentClipResolver.Resolve(clip, animationSet, entry);
				if (resolvedClip == null)
					throw new ArgumentException(
						"render-clip-strip needs a clip: pass 'clip', or 'animationSet' plus 'entry'.", nameof(clip));

				float[] sampleTimes = ParseTimes(times, frames);
				float[] sampleYaws = ParseFloats(yaws, "yaws", new[] { 35f });

				var cells = new List<Texture2D>(sampleTimes.Length * sampleYaws.Length);
				var labels = new List<string>(cells.Capacity);

				using var session = new AgentPreviewSession(source, description);
				try
				{
					for (var y = 0; y < sampleYaws.Length; y++)
					{
						for (var t = 0; t < sampleTimes.Length; t++)
						{
							session.Sample(resolvedClip, sampleTimes[t]);
							cells.Add(session.Render(cellWidth, cellHeight, sampleYaws[y], pitch, fieldOfView, padding));
							labels.Add(CellLabel(resolvedClip, sampleTimes[t], sampleYaws[y]));
						}
					}

					Texture2D sheet = AgentPreviewSheet.Compose(cells, labels, sampleTimes.Length, cellWidth, cellHeight);
					try
					{
						string path = AgentPreviewIO.Write(sheet,
							string.IsNullOrWhiteSpace(tag) ? $"{source.name}-{resolvedClip.name}-strip" : tag);
						RenderResult result = BuildResult(path, sheet.width, sheet.height, description, resolvedClip,
							session, sampleYaws[0], pitch, fieldOfView, padding);
						result.Cells.AddRange(labels);
						return result;
					}
					finally
					{
						Object.DestroyImmediate(sheet);
					}
				}
				finally
				{
					for (var i = 0; i < cells.Count; i++)
						Object.DestroyImmediate(cells[i]);
				}
			});
		}

		public const string RenderCompareToolId = "render-compare";

		[BridgeTool(RenderCompareToolId, Title = "Render / Compare")]
		[Description("Renders two poses side by side under identical framing and appends a difference cell, for " +
			"answering 'did this change anything' about a pose, a grip offset, or two variants of the same rig. " +
			"Leave targetB empty to compare two times or two clips on the same target.")]
		public RenderResult Compare
		(
			[Description("Left-hand asset path, asset GUID, or 'scene:Root/Child'.")]
			string targetA,
			[Description("Right-hand target. Empty reuses targetA, which is how two clip times or two clips are compared.")]
			string targetB = "",
			[Description("Left-hand AnimationClip asset path or GUID, '#ClipName' suffixed where needed. Empty renders the authored pose.")]
			string clipA = "",
			[Description("Right-hand AnimationClip. Empty reuses clipA.")]
			string clipB = "",
			[Description("Normalized time for the left-hand render.")]
			float timeA = 0f,
			[Description("Normalized time for the right-hand render.")]
			float timeB = 1f,
			[Description("Camera yaw in degrees, shared by both renders.")]
			float yaw = 35f,
			[Description("Camera pitch in degrees, shared by both renders.")]
			float pitch = 12f,
			[Description("Vertical field of view in degrees.")]
			float fieldOfView = 30f,
			[Description("Framing slack; the left-hand target's bounds frame both cells so the two renders are comparable.")]
			float padding = 1.15f,
			[Description("Width of one cell in pixels.")]
			int cellWidth = 400,
			[Description("Height of one cell in pixels.")]
			int cellHeight = 400,
			[Description("Append a third cell holding the per-pixel difference.")]
			bool includeDifference = true,
			[Description("Optional filename prefix.")]
			string tag = ""
		)
		{
			return UnityAiBridge.Utils.MainThread.Instance.Run(() =>
			{
				GameObject sourceA = EditorObjectReference.ResolveGameObject(targetA, out string descriptionA);
				string rightTarget = string.IsNullOrWhiteSpace(targetB) ? targetA : targetB;
				GameObject sourceB = EditorObjectReference.ResolveGameObject(rightTarget, out string descriptionB);

				AnimationClip resolvedA = AgentClipResolver.Resolve(clipA, "", "");
				AnimationClip resolvedB = string.IsNullOrWhiteSpace(clipB)
					? resolvedA
					: AgentClipResolver.Resolve(clipB, "", "");

				var cells = new List<Texture2D>(3);
				var labels = new List<string>(3);
				float changedFraction;

				using var sessionA = new AgentPreviewSession(sourceA, descriptionA);
				using var sessionB = new AgentPreviewSession(sourceB, descriptionB);
				try
				{
					sessionA.Sample(resolvedA, timeA);
					cells.Add(sessionA.Render(cellWidth, cellHeight, yaw, pitch, fieldOfView, padding));
					labels.Add("a " + CellLabel(resolvedA, timeA, yaw));

					sessionB.Sample(resolvedB, timeB);
					cells.Add(sessionB.Render(cellWidth, cellHeight, yaw, pitch, fieldOfView, padding));
					labels.Add("b " + CellLabel(resolvedB, timeB, yaw));

					Texture2D difference = AgentPreviewSheet.Difference(cells[0], cells[1], out changedFraction);
					if (includeDifference)
					{
						cells.Add(difference);
						labels.Add($"diff {changedFraction * 100f:0.0}#");
					}
					else
					{
						Object.DestroyImmediate(difference);
					}

					Texture2D sheet = AgentPreviewSheet.Compose(cells, labels, cells.Count, cellWidth, cellHeight);
					try
					{
						string path = AgentPreviewIO.Write(sheet,
							string.IsNullOrWhiteSpace(tag) ? $"{sourceA.name}-compare" : tag);
						RenderResult result = BuildResult(path, sheet.width, sheet.height,
							descriptionA == descriptionB ? descriptionA : $"{descriptionA} vs {descriptionB}",
							resolvedA, sessionA, yaw, pitch, fieldOfView, padding);
						result.Cells.AddRange(labels);
						result.ChangedFraction = changedFraction;
						return result;
					}
					finally
					{
						Object.DestroyImmediate(sheet);
					}
				}
				finally
				{
					for (var i = 0; i < cells.Count; i++)
						Object.DestroyImmediate(cells[i]);
				}
			});
		}

		private static RenderResult BuildResult(
			string path,
			int width,
			int height,
			string target,
			AnimationClip clip,
			AgentPreviewSession session,
			float yaw,
			float pitch,
			float fieldOfView,
			float padding)
		{
			Bounds bounds = session.FramingBounds;
			return new RenderResult
			{
				Path = path,
				Width = width,
				Height = height,
				FileSize = new FileInfo(path).Length,
				Target = target,
				Clip = clip == null ? "" : clip.name,
				Framing =
					$"yaw={yaw.ToString("0.#", CultureInfo.InvariantCulture)} " +
					$"pitch={pitch.ToString("0.#", CultureInfo.InvariantCulture)} " +
					$"fov={fieldOfView.ToString("0.#", CultureInfo.InvariantCulture)} " +
					$"padding={padding.ToString("0.###", CultureInfo.InvariantCulture)} " +
					$"boundsCenter=({bounds.center.x:0.###},{bounds.center.y:0.###},{bounds.center.z:0.###}) " +
					$"boundsSize=({bounds.size.x:0.###},{bounds.size.y:0.###},{bounds.size.z:0.###})"
			};
		}

		private static string CellLabel(AnimationClip clip, float normalizedTime, float yaw)
		{
			string time = $"t={normalizedTime.ToString("0.##", CultureInfo.InvariantCulture)}";
			string angle = $"y={yaw.ToString("0", CultureInfo.InvariantCulture)}";
			return clip == null ? $"pose {angle}" : $"{time} {angle} {clip.length.ToString("0.##", CultureInfo.InvariantCulture)}s";
		}

		private static float[] ParseTimes(string times, int frames)
		{
			if (!string.IsNullOrWhiteSpace(times))
				return ParseFloats(times, "times", null);

			if (frames < 1)
				throw new ArgumentOutOfRangeException(nameof(frames), frames, "frames must be at least 1.");

			var values = new float[frames];
			if (frames == 1)
			{
				values[0] = 0f;
				return values;
			}

			for (var i = 0; i < frames; i++)
				values[i] = i / (float)(frames - 1);

			return values;
		}

		private static float[] ParseFloats(string csv, string parameterName, float[] fallback)
		{
			if (string.IsNullOrWhiteSpace(csv))
			{
				if (fallback == null)
					throw new ArgumentException($"{parameterName} is empty.", parameterName);
				return fallback;
			}

			string[] parts = csv.Split(',', StringSplitOptions.RemoveEmptyEntries);
			var values = new float[parts.Length];
			for (var i = 0; i < parts.Length; i++)
			{
				if (!float.TryParse(parts[i].Trim(), NumberStyles.Float, CultureInfo.InvariantCulture, out values[i]))
					throw new ArgumentException(
						$"{parameterName} entry '{parts[i]}' is not a number. Expected a comma separated list such as '0,0.5,1'.",
						parameterName);
			}

			return values;
		}
	}

	internal static class AgentClipResolver
	{
		/// <summary>Resolves a clip from a direct asset reference, or from an AnimationSet entry.</summary>
		internal static AnimationClip Resolve(string clip, string animationSet, string entry)
		{
			if (!string.IsNullOrWhiteSpace(animationSet))
				return FromSet(animationSet, entry);

			if (string.IsNullOrWhiteSpace(clip))
				return null;

			string reference = clip;
			string subAssetName = null;
			int separator = reference.IndexOf('#');
			if (separator >= 0)
			{
				subAssetName = reference.Substring(separator + 1);
				reference = reference.Substring(0, separator);
			}

			string assetPath = EditorObjectReference.ResolveAssetPath(reference);

			if (!string.IsNullOrEmpty(subAssetName))
			{
				Object[] all = AssetDatabase.LoadAllAssetsAtPath(assetPath);
				for (var i = 0; i < all.Length; i++)
				{
					if (all[i] is AnimationClip candidate && candidate.name == subAssetName)
						return candidate;
				}

				throw new InvalidOperationException(
					$"No AnimationClip named '{subAssetName}' inside '{assetPath}'. Available clips: {ListClips(all)}");
			}

			var direct = AssetDatabase.LoadAssetAtPath<AnimationClip>(assetPath);
			if (direct != null)
				return direct;

			Object[] contents = AssetDatabase.LoadAllAssetsAtPath(assetPath);
			var found = 0;
			AnimationClip single = null;
			for (var i = 0; i < contents.Length; i++)
			{
				if (contents[i] is not AnimationClip candidate)
					continue;
				found++;
				single = candidate;
			}

			if (found == 1)
				return single;

			if (found > 1)
				throw new InvalidOperationException(
					$"'{assetPath}' holds {found} clips, so the name must be explicit: pass '{assetPath}#ClipName'. " +
					$"Available clips: {ListClips(contents)}");

			throw new InvalidOperationException($"No AnimationClip at or inside '{assetPath}'.");
		}

		private static AnimationClip FromSet(string animationSet, string entry)
		{
			if (string.IsNullOrWhiteSpace(entry))
				throw new ArgumentException(
					"'entry' is required when 'animationSet' is set: the set holds many clips.", nameof(entry));

			var set = EditorObjectReference.LoadAsset<AnimationSet>(animationSet);

			Dictionary<string, AnimationSetEntry> resolved = set.GetResolvedEntries();
			if (!resolved.TryGetValue(entry, out AnimationSetEntry match))
				throw new InvalidOperationException(
					$"AnimationSet '{set.name}' has no entry '{entry}'. Known ids: {string.Join(", ", resolved.Keys)}");

			if (match.clip == null || match.clip.Clip == null)
				throw new InvalidOperationException(
					$"AnimationSet '{set.name}' entry '{entry}' has no AnimationClip assigned.");

			return match.clip.Clip;
		}

		private static string ListClips(Object[] assets)
		{
			var names = new List<string>();
			for (var i = 0; i < assets.Length; i++)
			{
				if (assets[i] is AnimationClip clip)
					names.Add(clip.name);
			}

			return names.Count == 0 ? "(none)" : string.Join(", ", names);
		}
	}
}
#endif
