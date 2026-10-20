#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using UnityAiBridge;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using Object = UnityEngine.Object;

namespace AetherNexus.FoundationPlatform.AgentTools.Editor
{
	public sealed class VfxPreviewResult
	{
		[Description("Absolute path to the contact-sheet PNG: one column per prefab, one row per time. Read it to see the effects.")]
		public string Path;

		[Description("Column order, left to right.")]
		public List<string> Prefabs = new();

		public List<float> Times = new();

		[Description("Per prefab and time, live particles per layer: 'Prefab t=0.15: Layer=12 Grit=5'. A layer at 0 at every time never shows.")]
		public List<string> Counts = new();

		[Description("With report: one line per layer with the settings that decide how it looks.")]
		public List<string> Layers = new();
	}

	/// <summary>
	/// Contact sheet of particle-effect prefabs, simulated to chosen times in an isolated preview scene. Replaces
	/// hand-written render probes: one call shows every effect in a folder and how many particles each layer has.
	/// </summary>
	[BridgeToolType]
	public partial class Tool_VfxPreview
	{
		public const string VfxPreviewToolId = "vfx-preview";

		[BridgeTool(VfxPreviewToolId, Title = "Render / VFX Preview", ReadOnly = true)]
		[Description("Renders particle-effect prefabs to one PNG contact sheet (one column per prefab, one row per sampled time) " +
			"in an isolated preview scene with a sun light, and reports live particle counts per layer. No open scene or asset " +
			"is touched. pose 'point' aims the prefab's +Z sideways (contact effects that fire along a hit normal); 'ground' " +
			"tilts it toward the camera (rings, dust, auras); 'auto' picks per prefab from its layer shapes.")]
		public VfxPreviewResult Preview
		(
			[Description("Comma-separated prefab paths, or one folder: every prefab in it with a ParticleSystem.")]
			string prefabs,
			[Description("Comma-separated simulation times in seconds, one row each, e.g. '0.15,0.6'.")]
			string times = "0.15,0.6",
			[Description("auto | point | ground.")]
			string pose = "auto",
			[Description("Metres of world space per column.")]
			float cellSize = 2.6f,
			[Description("Background colour as 'r,g,b' 0..1.")]
			string background = "0.36,0.4,0.33",
			[Description("Output width in pixels; height follows from the grid.")]
			int width = 1800,
			[Description("Also list each layer's material, loop, duration, lifetime, size, emission, sheet and render mode.")]
			bool report = false,
			[Description("Optional filename prefix.")]
			string tag = ""
		)
		{
			var timeList = times.Split(',').Select(s => s.Trim()).Where(s => s.Length > 0)
				.Select(s => float.Parse(s, CultureInfo.InvariantCulture)).ToList();
			if (timeList.Count == 0)
				throw new ArgumentException("times lists no values.", nameof(times));
			if (pose != "auto" && pose != "point" && pose != "ground")
				throw new ArgumentException($"pose must be auto, point or ground; got '{pose}'.", nameof(pose));
			var bg = ParseColor(background);

			return UnityAiBridge.Utils.MainThread.Instance.Run(() =>
			{
				var paths = ResolvePrefabs(prefabs);
				var result = new VfxPreviewResult { Times = timeList };
				var scene = EditorSceneManager.NewPreviewScene();
				var spawned = new List<GameObject>();
				RenderTexture rt = null;
				Texture2D tex = null;
				try
				{
					var sun = new GameObject("VfxPreviewSun");
					SceneManager.MoveGameObjectToScene(sun, scene);
					var light = sun.AddComponent<Light>();
					light.type = LightType.Directional;
					light.intensity = 1.2f;
					sun.transform.rotation = Quaternion.Euler(45f, -30f, 0f);

					int cols = paths.Count, rows = timeList.Count;
					var camGo = new GameObject("VfxPreviewCamera");
					SceneManager.MoveGameObjectToScene(camGo, scene);
					var cam = camGo.AddComponent<Camera>();
					cam.scene = scene;
					cam.enabled = false;
					cam.clearFlags = CameraClearFlags.SolidColor;
					cam.backgroundColor = bg;
					cam.orthographic = true;
					float rowHeight = cellSize * 1.1f;
					int height = Mathf.Max(64, Mathf.RoundToInt(width * (rows * rowHeight) / (cols * cellSize)));
					cam.orthographicSize = rows * rowHeight * 0.5f;
					cam.aspect = (float)width / height;
					cam.nearClipPlane = 0.1f;
					cam.farClipPlane = 100f;
					cam.transform.position = new Vector3((cols - 1) * cellSize * 0.5f, 0f, -20f);

					for (int r = 0; r < rows; r++)
					{
						float rowCentre = cam.orthographicSize - rowHeight * (r + 0.5f);
						for (int c = 0; c < cols; c++)
						{
							var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(paths[c]);
							var go = (GameObject)Object.Instantiate(prefab);
							SceneManager.MoveGameObjectToScene(go, scene);
							spawned.Add(go);
							bool point = pose == "point" || (pose == "auto" && LooksLikePoint(go));
							go.transform.SetPositionAndRotation(
								new Vector3(c * cellSize, rowCentre + (point ? 0f : -rowHeight * 0.3f), 0f),
								point ? Quaternion.Euler(-35f, 90f, 0f) : Quaternion.Euler(25f, 0f, 0f));
							var root = go.GetComponentInChildren<ParticleSystem>();
							if (root == null)
								throw new InvalidOperationException($"'{paths[c]}' has no ParticleSystem.");
							root.Simulate(timeList[r], true, true, true);

							var line = new StringBuilder($"{prefab.name} t={timeList[r].ToString("0.###", CultureInfo.InvariantCulture)}:");
							foreach (var ps in go.GetComponentsInChildren<ParticleSystem>(true))
							{
								var renderer = ps.GetComponent<ParticleSystemRenderer>();
								if (renderer == null || !renderer.enabled)
									continue;
								line.Append(' ').Append(ps.name).Append('=').Append(ps.particleCount);
							}
							result.Counts.Add(line.ToString());
						}
					}

					rt = new RenderTexture(width, height, 24, RenderTextureFormat.ARGB32);
					cam.targetTexture = rt;
					cam.Render();
					var previous = RenderTexture.active;
					RenderTexture.active = rt;
					tex = new Texture2D(width, height, TextureFormat.RGB24, false);
					tex.ReadPixels(new Rect(0, 0, width, height), 0, 0);
					tex.Apply();
					RenderTexture.active = previous;
					cam.targetTexture = null;
					result.Path = AgentPreviewIO.Write(tex, string.IsNullOrWhiteSpace(tag) ? "vfx" : tag);

					foreach (var p in paths)
						result.Prefabs.Add(p);
					if (report)
					{
						foreach (var p in paths)
						{
							var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(p);
							foreach (var ps in prefab.GetComponentsInChildren<ParticleSystem>(true))
							{
								var renderer = ps.GetComponent<ParticleSystemRenderer>();
								if ((renderer == null || !renderer.enabled) && !ps.emission.enabled)
									continue; // controller root: no particles of its own
								result.Layers.Add(prefab.name + "/" + LayerSummary(ps));
							}
						}
					}
					return result;
				}
				finally
				{
					if (tex != null)
						Object.DestroyImmediate(tex);
					if (rt != null)
					{
						rt.Release();
						Object.DestroyImmediate(rt);
					}
					EditorSceneManager.ClosePreviewScene(scene);
				}
			});
		}

		static List<string> ResolvePrefabs(string prefabs)
		{
			var trimmed = prefabs.Trim().TrimEnd('/');
			var list = new List<string>();
			if (AssetDatabase.IsValidFolder(trimmed))
			{
				foreach (var guid in AssetDatabase.FindAssets("t:Prefab", new[] { trimmed }))
				{
					var path = AssetDatabase.GUIDToAssetPath(guid);
					var go = AssetDatabase.LoadAssetAtPath<GameObject>(path);
					if (go != null && go.GetComponentInChildren<ParticleSystem>(true) != null)
						list.Add(path);
				}
				list.Sort(StringComparer.OrdinalIgnoreCase);
			}
			else
			{
				foreach (var part in prefabs.Split(','))
				{
					var path = part.Trim();
					if (path.Length == 0)
						continue;
					if (AssetDatabase.LoadAssetAtPath<GameObject>(path) == null)
						throw new ArgumentException($"No prefab at '{path}'.", nameof(prefabs));
					list.Add(path);
				}
			}
			if (list.Count == 0)
				throw new ArgumentException($"'{prefabs}' resolves to no particle prefab.", nameof(prefabs));
			if (list.Count > 24)
				throw new ArgumentException($"'{prefabs}' resolves to {list.Count} prefabs; pass at most 24 per sheet.", nameof(prefabs));
			return list;
		}

		// Contact effects are one-shots emitting along +Z through cones; looping effects and ones that use circles
		// or point a cone upward (by shape or layer rotation) sit on the ground or a body.
		static bool LooksLikePoint(GameObject go)
		{
			var root = go.GetComponentInChildren<ParticleSystem>();
			if (root != null && root.main.loop)
				return false;
			foreach (var ps in go.GetComponentsInChildren<ParticleSystem>(true))
			{
				var shape = ps.shape;
				if (!shape.enabled)
					continue;
				if (shape.shapeType == ParticleSystemShapeType.Circle || shape.shapeType == ParticleSystemShapeType.Donut)
					return false;
				if (shape.shapeType == ParticleSystemShapeType.Cone)
				{
					var axis = ps.transform.localRotation * Quaternion.Euler(shape.rotation) * Vector3.forward;
					if (axis.y > 0.7f)
						return false;
				}
			}
			return true;
		}

		static string LayerSummary(ParticleSystem ps)
		{
			var m = ps.main;
			var e = ps.emission;
			var r = ps.GetComponent<ParticleSystemRenderer>();
			var ts = ps.textureSheetAnimation;
			var sb = new StringBuilder(ps.name);
			sb.Append(" mat=").Append(r != null && r.sharedMaterial != null ? r.sharedMaterial.name : "-");
			if (r != null && !r.enabled)
				sb.Append(" (renderer off)");
			sb.Append(" loop=").Append(m.loop).Append(" dur=").Append(F(m.duration));
			sb.Append(" life=").Append(Range(m.startLifetime)).Append(" size=").Append(Range(m.startSize));
			sb.Append(" speed=").Append(Range(m.startSpeed)).Append(" grav=").Append(F(m.gravityModifierMultiplier));
			sb.Append(" sim=").Append(m.simulationSpace);
			sb.Append(" emit=").Append(e.enabled ? $"rate {F(e.rateOverTime.constantMax)} dist {F(e.rateOverDistance.constantMax)} bursts {e.burstCount}" : "off");
			sb.Append(" sheet=").Append(ts.enabled ? $"{ts.numTilesX}x{ts.numTilesY}" : "off");
			sb.Append(" render=").Append(r != null ? r.renderMode.ToString() : "-");
			sb.Append(" color=").Append(ColorText(m.startColor));
			return sb.ToString();
		}

		static string Range(ParticleSystem.MinMaxCurve c) =>
			c.mode == ParticleSystemCurveMode.TwoConstants ? $"{F(c.constantMin)}-{F(c.constantMax)}" :
			c.mode == ParticleSystemCurveMode.Constant ? F(c.constant) : $"{c.mode}x{F(c.curveMultiplier)}";

		static string ColorText(ParticleSystem.MinMaxGradient g)
		{
			var col = g.mode == ParticleSystemGradientMode.TwoColors ? g.colorMax : g.color;
			return $"({F(col.r)},{F(col.g)},{F(col.b)},{F(col.a)}){(g.mode != ParticleSystemGradientMode.Color ? " " + g.mode : "")}";
		}

		static string F(float v) => v.ToString("0.##", CultureInfo.InvariantCulture);

		static Color ParseColor(string text)
		{
			var parts = text.Split(',');
			if (parts.Length != 3)
				throw new ArgumentException($"background '{text}' must be 'r,g,b'.", nameof(text));
			return new Color(
				float.Parse(parts[0].Trim(), CultureInfo.InvariantCulture),
				float.Parse(parts[1].Trim(), CultureInfo.InvariantCulture),
				float.Parse(parts[2].Trim(), CultureInfo.InvariantCulture));
		}
	}
}
#endif
