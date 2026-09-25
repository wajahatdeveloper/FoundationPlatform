#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using UnityEditor;
using UnityEngine;
using Object = UnityEngine.Object;

namespace AetherNexus.FoundationPlatform.AgentTools.Editor
{
	/// <summary>
	/// Off-scene renderer behind the <c>render-*</c> agent tools. A coding agent cannot see the Editor,
	/// so every model / pose / animation question ends in "what does it look like". This renders a
	/// prefab or scene object into a PNG at a chosen angle and clip time without touching any open
	/// scene, which is what makes the answer reproducible: same inputs, same pixels.
	/// </summary>
	internal sealed class AgentPreviewSession : IDisposable
	{
		private readonly PreviewRenderUtility _utility;
		private readonly GameObject _instance;
		private readonly List<Renderer> _renderers = new();

		internal Bounds FramingBounds { get; }
		internal string SourceName { get; }

		internal AgentPreviewSession(GameObject source, string sourceDescription)
		{
			SourceName = sourceDescription;

			_utility = new PreviewRenderUtility();
			Camera camera = _utility.camera;
			camera.clearFlags = CameraClearFlags.SolidColor;
			camera.backgroundColor = new Color(0.17f, 0.18f, 0.20f, 1f);
			camera.orthographic = false;
			camera.nearClipPlane = 0.01f;
			camera.farClipPlane = 500f;
			camera.allowHDR = false;
			camera.allowMSAA = false;

			_utility.ambientColor = new Color(0.28f, 0.29f, 0.32f, 1f);
			_utility.lights[0].intensity = 1.3f;
			_utility.lights[0].color = Color.white;
			_utility.lights[0].transform.rotation = Quaternion.Euler(38f, 145f, 0f);
			_utility.lights[1].intensity = 0.55f;
			_utility.lights[1].color = new Color(0.85f, 0.9f, 1f, 1f);
			_utility.lights[1].transform.rotation = Quaternion.Euler(-12f, -110f, 0f);

			_instance = InstantiateRenderOnly(source);
			_instance.name = source.name;
			_instance.hideFlags = HideFlags.HideAndDontSave;
			_instance.transform.SetPositionAndRotation(Vector3.zero, Quaternion.identity);
			_utility.AddSingleGO(_instance);

			_instance.GetComponentsInChildren(true, _renderers);
			FramingBounds = ComputeBounds();
		}

		/// <summary>
		/// Clones under an inactive holder so no gameplay Awake/OnEnable runs, then strips every
		/// MonoBehaviour before the clone activates. Cloning a live Play-mode character used to register
		/// the clone with runtime registries (motor lists, managers), which left dangling references once
		/// the preview was destroyed and could hang the Editor.
		/// </summary>
		private static GameObject InstantiateRenderOnly(GameObject source)
		{
			var holder = new GameObject("AgentPreviewHolder") { hideFlags = HideFlags.HideAndDontSave };
			holder.SetActive(false);
			try
			{
				GameObject clone = Object.Instantiate(source, holder.transform);
				StripBehaviours(clone);
				clone.transform.SetParent(null, false);
				clone.SetActive(true);
				return clone;
			}
			finally
			{
				Object.DestroyImmediate(holder);
			}
		}

		private static void StripBehaviours(GameObject clone)
		{
			var behaviours = new List<MonoBehaviour>();
			// [RequireComponent] chains refuse removal while a dependent still exists, so peel in passes.
			for (var pass = 0; pass < 8; pass++)
			{
				clone.GetComponentsInChildren(true, behaviours);
				if (behaviours.Count == 0)
					return;

				for (var i = behaviours.Count - 1; i >= 0; i--)
				{
					if (behaviours[i] != null && CanDestroy(behaviours[i]))
						Object.DestroyImmediate(behaviours[i]);
				}
			}

			clone.GetComponentsInChildren(true, behaviours);
			if (behaviours.Count > 0)
				throw new InvalidOperationException(
					$"Could not strip {behaviours.Count} MonoBehaviour(s) from the preview clone of '{clone.name}' " +
					$"(first: {behaviours[0].GetType().FullName}); a [RequireComponent] cycle blocks removal.");
		}

		private static bool CanDestroy(Component component)
		{
			Component[] siblings = component.GetComponents<Component>();
			Type type = component.GetType();
			for (var i = 0; i < siblings.Length; i++)
			{
				if (siblings[i] == null || siblings[i] == component)
					continue;

				object[] requirements = siblings[i].GetType().GetCustomAttributes(typeof(RequireComponent), true);
				for (var r = 0; r < requirements.Length; r++)
				{
					var require = (RequireComponent)requirements[r];
					if (Requires(require.m_Type0, type) || Requires(require.m_Type1, type) || Requires(require.m_Type2, type))
						return false;
				}
			}

			return true;
		}

		private static bool Requires(Type required, Type candidate) =>
			required != null && required.IsAssignableFrom(candidate);

		private Bounds ComputeBounds()
		{
			var found = false;
			var bounds = new Bounds(Vector3.zero, Vector3.zero);

			for (var i = 0; i < _renderers.Count; i++)
			{
				Renderer renderer = _renderers[i];
				if (renderer == null || renderer is ParticleSystemRenderer)
					continue;

				if (renderer is SkinnedMeshRenderer skinned)
				{
					// Skinned bounds in a preview scene follow the root bone, which can be stale before the
					// first sample; the local bounds pushed through the transform are stable.
					Bounds local = skinned.localBounds;
					Transform pivot = skinned.rootBone != null ? skinned.rootBone : skinned.transform;
					var skinnedBounds = new Bounds(pivot.TransformPoint(local.center), Vector3.zero);
					skinnedBounds.Encapsulate(new Bounds(pivot.TransformPoint(local.center), local.size));
					Encapsulate(ref bounds, skinnedBounds, ref found);
					continue;
				}

				Encapsulate(ref bounds, renderer.bounds, ref found);
			}

			if (!found)
				throw new InvalidOperationException(
					$"'{SourceName}' has no MeshRenderer or SkinnedMeshRenderer under it, so there is nothing to " +
					"render. Point the tool at a visual prefab, a model asset, or a scene object with renderers.");

			if (bounds.size.magnitude < 0.0001f)
				bounds.size = Vector3.one * 0.1f;

			return bounds;
		}

		private static void Encapsulate(ref Bounds target, Bounds addition, ref bool found)
		{
			if (!found)
			{
				target = addition;
				found = true;
				return;
			}

			target.Encapsulate(addition);
		}

		/// <summary>Poses the instance at <paramref name="normalizedTime"/> of <paramref name="clip"/>.</summary>
		internal void Sample(AnimationClip clip, float normalizedTime)
		{
			if (clip == null)
				return;

			float time = clip.length * Mathf.Clamp01(normalizedTime);
			try
			{
				clip.SampleAnimation(_instance, time);
			}
			catch (Exception e)
			{
				throw new InvalidOperationException(
					$"Sampling clip '{clip.name}' at t={normalizedTime:0.###} ({time:0.###}s) on '{SourceName}' failed. " +
					"A humanoid clip needs an Animator with a matching Avatar on the target root: " + e.Message, e);
			}
		}

		internal Texture2D Render(int width, int height, float yaw, float pitch, float fieldOfView, float padding)
		{
			Camera camera = _utility.camera;
			float radius = Mathf.Max(FramingBounds.extents.magnitude, 0.001f);
			float distance = radius / Mathf.Sin(fieldOfView * 0.5f * Mathf.Deg2Rad) * padding;
			var rotation = Quaternion.Euler(pitch, yaw, 0f);

			camera.fieldOfView = fieldOfView;
			camera.transform.rotation = rotation;
			camera.transform.position = FramingBounds.center - rotation * Vector3.forward * distance;
			camera.farClipPlane = distance + radius * 4f;

			_utility.BeginStaticPreview(new Rect(0f, 0f, width, height));
			camera.Render();
			Texture2D texture = _utility.EndStaticPreview();

			if (texture == null)
				throw new InvalidOperationException(
					$"PreviewRenderUtility returned no texture for '{SourceName}' at {width}x{height}.");

			return texture;
		}

		public void Dispose()
		{
			if (_instance != null)
				Object.DestroyImmediate(_instance);

			_utility.Cleanup();
		}
	}

	internal static class AgentPreviewIO
	{
		internal static string RenderDirectory =>
			Path.Combine(Application.temporaryCachePath, "UnityBridge", "renders");

		internal static string Write(Texture2D texture, string tag)
		{
			Directory.CreateDirectory(RenderDirectory);

			byte[] png = texture.EncodeToPNG();
			if (png == null || png.Length == 0)
				throw new InvalidOperationException($"PNG encoding produced no bytes for '{tag}'.");

			string safeTag = string.IsNullOrWhiteSpace(tag) ? "render" : Sanitize(tag);
			string path = Path.Combine(RenderDirectory,
				$"{safeTag}_{DateTime.Now.ToString("yyyyMMdd_HHmmss_fff", CultureInfo.InvariantCulture)}.png");
			File.WriteAllBytes(path, png);
			return path;
		}

		private static string Sanitize(string value)
		{
			var chars = value.ToCharArray();
			char[] invalid = Path.GetInvalidFileNameChars();
			for (var i = 0; i < chars.Length; i++)
			{
				if (Array.IndexOf(invalid, chars[i]) >= 0 || chars[i] == ' ')
					chars[i] = '-';
			}
			return new string(chars);
		}
	}

	/// <summary>Grid compositing and pixel diffing for the contact-sheet and A/B tools.</summary>
	internal static class AgentPreviewSheet
	{
		private static readonly Color32 CellBorder = new(60, 62, 70, 255);
		private static readonly Color32 LabelColor = new(235, 235, 240, 255);

		internal static Texture2D Compose(List<Texture2D> cells, List<string> labels, int columns, int cellWidth, int cellHeight)
		{
			if (cells.Count == 0)
				throw new InvalidOperationException("Nothing to compose: the cell list is empty.");

			int rows = Mathf.CeilToInt(cells.Count / (float)columns);
			int width = columns * cellWidth;
			int height = rows * cellHeight;

			var sheet = new Texture2D(width, height, TextureFormat.RGB24, false);
			var pixels = new Color32[width * height];
			for (var i = 0; i < pixels.Length; i++)
				pixels[i] = CellBorder;

			for (var i = 0; i < cells.Count; i++)
			{
				int column = i % columns;
				int row = i / columns;
				// Texture rows run bottom-up, so the first cell must land on the top row.
				int originX = column * cellWidth;
				int originY = (rows - 1 - row) * cellHeight;
				Blit(pixels, width, cells[i], originX, originY);
				AgentPreviewFont.Draw(pixels, width, height, originX + 4, originY + 4, labels[i], LabelColor, 2);
			}

			sheet.SetPixels32(pixels);
			sheet.Apply(false, false);
			return sheet;
		}

		/// <summary>Per-pixel absolute difference, so an A/B pose check reports where it changed.</summary>
		internal static Texture2D Difference(Texture2D a, Texture2D b, out float changedFraction)
		{
			if (a.width != b.width || a.height != b.height)
				throw new InvalidOperationException(
					$"Cannot diff {a.width}x{a.height} against {b.width}x{b.height}; both renders must use the same size.");

			Color32[] left = a.GetPixels32();
			Color32[] right = b.GetPixels32();
			var output = new Color32[left.Length];
			var changed = 0;

			for (var i = 0; i < left.Length; i++)
			{
				int dr = Mathf.Abs(left[i].r - right[i].r);
				int dg = Mathf.Abs(left[i].g - right[i].g);
				int db = Mathf.Abs(left[i].b - right[i].b);
				int magnitude = Mathf.Min(255, dr + dg + db);
				if (magnitude > 12)
					changed++;

				output[i] = new Color32((byte)magnitude, (byte)(magnitude / 3), (byte)(magnitude / 6), 255);
			}

			changedFraction = changed / (float)left.Length;

			var texture = new Texture2D(a.width, a.height, TextureFormat.RGB24, false);
			texture.SetPixels32(output);
			texture.Apply(false, false);
			return texture;
		}

		private static void Blit(Color32[] destination, int destinationWidth, Texture2D source, int originX, int originY)
		{
			Color32[] pixels = source.GetPixels32();
			for (var y = 0; y < source.height; y++)
			{
				int destinationRow = (originY + y) * destinationWidth + originX;
				int sourceRow = y * source.width;
				for (var x = 0; x < source.width; x++)
					destination[destinationRow + x] = pixels[sourceRow + x];
			}
		}
	}

	/// <summary>
	/// 3x5 bitmap glyphs. Cell labels have to survive a PNG round-trip with no GUI context and no font
	/// asset, so the text is written straight into the pixel buffer.
	/// </summary>
	internal static class AgentPreviewFont
	{
		private const int GlyphWidth = 3;
		private const int GlyphHeight = 5;

		private static readonly Dictionary<char, byte[]> Glyphs = new()
		{
			['0'] = new byte[] { 0b111, 0b101, 0b101, 0b101, 0b111 },
			['1'] = new byte[] { 0b010, 0b110, 0b010, 0b010, 0b111 },
			['2'] = new byte[] { 0b111, 0b001, 0b111, 0b100, 0b111 },
			['3'] = new byte[] { 0b111, 0b001, 0b111, 0b001, 0b111 },
			['4'] = new byte[] { 0b101, 0b101, 0b111, 0b001, 0b001 },
			['5'] = new byte[] { 0b111, 0b100, 0b111, 0b001, 0b111 },
			['6'] = new byte[] { 0b111, 0b100, 0b111, 0b101, 0b111 },
			['7'] = new byte[] { 0b111, 0b001, 0b001, 0b001, 0b001 },
			['8'] = new byte[] { 0b111, 0b101, 0b111, 0b101, 0b111 },
			['9'] = new byte[] { 0b111, 0b101, 0b111, 0b001, 0b111 },
			['.'] = new byte[] { 0b000, 0b000, 0b000, 0b000, 0b010 },
			[','] = new byte[] { 0b000, 0b000, 0b000, 0b010, 0b010 },
			[':'] = new byte[] { 0b000, 0b010, 0b000, 0b010, 0b000 },
			['='] = new byte[] { 0b000, 0b111, 0b000, 0b111, 0b000 },
			['-'] = new byte[] { 0b000, 0b000, 0b111, 0b000, 0b000 },
			['+'] = new byte[] { 0b000, 0b010, 0b111, 0b010, 0b000 },
			['/'] = new byte[] { 0b001, 0b001, 0b010, 0b100, 0b100 },
			['#'] = new byte[] { 0b101, 0b111, 0b101, 0b111, 0b101 },
			['a'] = new byte[] { 0b000, 0b111, 0b101, 0b101, 0b111 },
			['b'] = new byte[] { 0b100, 0b111, 0b101, 0b101, 0b111 },
			['c'] = new byte[] { 0b000, 0b111, 0b100, 0b100, 0b111 },
			['d'] = new byte[] { 0b001, 0b111, 0b101, 0b101, 0b111 },
			['e'] = new byte[] { 0b111, 0b101, 0b111, 0b100, 0b111 },
			['f'] = new byte[] { 0b011, 0b100, 0b111, 0b100, 0b100 },
			['g'] = new byte[] { 0b111, 0b101, 0b111, 0b001, 0b111 },
			['h'] = new byte[] { 0b100, 0b101, 0b111, 0b101, 0b101 },
			['i'] = new byte[] { 0b010, 0b000, 0b010, 0b010, 0b010 },
			['j'] = new byte[] { 0b001, 0b000, 0b001, 0b101, 0b111 },
			['k'] = new byte[] { 0b100, 0b101, 0b110, 0b101, 0b101 },
			['l'] = new byte[] { 0b100, 0b100, 0b100, 0b100, 0b111 },
			['m'] = new byte[] { 0b000, 0b111, 0b111, 0b101, 0b101 },
			['n'] = new byte[] { 0b000, 0b110, 0b101, 0b101, 0b101 },
			['o'] = new byte[] { 0b000, 0b111, 0b101, 0b101, 0b111 },
			['p'] = new byte[] { 0b111, 0b101, 0b111, 0b100, 0b100 },
			['q'] = new byte[] { 0b111, 0b101, 0b111, 0b001, 0b001 },
			['r'] = new byte[] { 0b000, 0b111, 0b100, 0b100, 0b100 },
			['s'] = new byte[] { 0b011, 0b100, 0b010, 0b001, 0b110 },
			['t'] = new byte[] { 0b010, 0b111, 0b010, 0b010, 0b011 },
			['u'] = new byte[] { 0b000, 0b101, 0b101, 0b101, 0b111 },
			['v'] = new byte[] { 0b000, 0b101, 0b101, 0b101, 0b010 },
			['w'] = new byte[] { 0b000, 0b101, 0b101, 0b111, 0b111 },
			['x'] = new byte[] { 0b000, 0b101, 0b010, 0b010, 0b101 },
			['y'] = new byte[] { 0b101, 0b101, 0b011, 0b001, 0b110 },
			['z'] = new byte[] { 0b111, 0b001, 0b010, 0b100, 0b111 },
		};

		internal static void Draw(Color32[] pixels, int bufferWidth, int bufferHeight, int x, int y, string text, Color32 color, int scale)
		{
			if (string.IsNullOrEmpty(text))
				return;

			int cursorX = x;
			for (var i = 0; i < text.Length; i++)
			{
				char c = char.ToLowerInvariant(text[i]);
				if (c == ' ')
				{
					cursorX += (GlyphWidth + 1) * scale;
					continue;
				}

				if (Glyphs.TryGetValue(c, out byte[] rows))
					DrawGlyph(pixels, bufferWidth, bufferHeight, cursorX, y, rows, color, scale);

				cursorX += (GlyphWidth + 1) * scale;
			}
		}

		private static void DrawGlyph(Color32[] pixels, int bufferWidth, int bufferHeight, int x, int y, byte[] rows, Color32 color, int scale)
		{
			for (var row = 0; row < GlyphHeight; row++)
			{
				byte bits = rows[row];
				for (var column = 0; column < GlyphWidth; column++)
				{
					if ((bits & (1 << (GlyphWidth - 1 - column))) == 0)
						continue;

					// Glyph rows read top-down while the pixel buffer runs bottom-up.
					int baseX = x + column * scale;
					int baseY = y + (GlyphHeight - 1 - row) * scale;
					for (var sy = 0; sy < scale; sy++)
					{
						int py = baseY + sy;
						if (py < 0 || py >= bufferHeight)
							continue;

						for (var sx = 0; sx < scale; sx++)
						{
							int px = baseX + sx;
							if (px < 0 || px >= bufferWidth)
								continue;

							pixels[py * bufferWidth + px] = color;
						}
					}
				}
			}
		}
	}
}
#endif
