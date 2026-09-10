#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace AetherNexus.FoundationPlatform.Editor.Utilities
{
	/// <summary>
	/// Resolves the one string form a tool caller can always produce — asset path, asset GUID, or
	/// <c>scene:Root/Child</c> — into the object it names. Tools that take a target from outside the
	/// Editor (agent bridge, CLI, batch jobs) all need the same three cases, and each of them has to
	/// fail with the path that was asked for rather than a null reference.
	/// </summary>
	public static class EditorObjectReference
	{
		public const string ScenePrefix = "scene:";

		public static string ResolveAssetPath(string reference)
		{
			if (string.IsNullOrWhiteSpace(reference))
				throw new ArgumentException("Asset reference is empty.", nameof(reference));

			if (reference.Contains('/') || reference.Contains('\\'))
				return reference;

			string path = AssetDatabase.GUIDToAssetPath(reference);
			if (string.IsNullOrEmpty(path))
				throw new InvalidOperationException($"'{reference}' is neither an asset path nor a known asset GUID.");

			return path;
		}

		public static T LoadAsset<T>(string reference) where T : UnityEngine.Object
		{
			string assetPath = ResolveAssetPath(reference);
			var asset = AssetDatabase.LoadAssetAtPath<T>(assetPath);
			if (asset == null)
				throw new InvalidOperationException(
					$"No {typeof(T).Name} at '{assetPath}'. AssetDatabase reports main type " +
					$"'{AssetDatabase.GetMainAssetTypeAtPath(assetPath)?.Name ?? "none"}'.");

			return asset;
		}

		/// <param name="reference">Asset path, asset GUID, or <c>scene:Root/Child</c>.</param>
		/// <param name="description">Normalized form of what was resolved, for messages and reports.</param>
		public static GameObject ResolveGameObject(string reference, out string description)
		{
			if (string.IsNullOrWhiteSpace(reference))
				throw new ArgumentException(
					"Target is empty. Pass an asset path ('Assets/.../Hero.prefab'), an asset GUID, or a scene " +
					"hierarchy path ('scene:Hero/Visuals').", nameof(reference));

			if (reference.StartsWith(ScenePrefix, StringComparison.OrdinalIgnoreCase))
			{
				string hierarchyPath = reference.Substring(ScenePrefix.Length);
				description = ScenePrefix + hierarchyPath;
				return FindInLoadedScenes(hierarchyPath);
			}

			string assetPath = ResolveAssetPath(reference);
			var asset = AssetDatabase.LoadAssetAtPath<GameObject>(assetPath);
			if (asset == null)
				throw new InvalidOperationException(
					$"No GameObject asset at '{assetPath}'. Prefabs and imported models load as GameObject; " +
					$"AssetDatabase reports main type '{AssetDatabase.GetMainAssetTypeAtPath(assetPath)?.Name ?? "none"}'.");

			description = assetPath;
			return asset;
		}

		public static GameObject FindInLoadedScenes(string hierarchyPath)
		{
			if (string.IsNullOrWhiteSpace(hierarchyPath))
				throw new ArgumentException("Hierarchy path is empty.", nameof(hierarchyPath));

			string[] segments = hierarchyPath.Split('/');
			var roots = new List<GameObject>();

			for (var i = 0; i < SceneManager.sceneCount; i++)
			{
				Scene scene = SceneManager.GetSceneAt(i);
				if (!scene.isLoaded)
					continue;

				roots.Clear();
				scene.GetRootGameObjects(roots);
				for (var r = 0; r < roots.Count; r++)
				{
					GameObject match = Walk(roots[r], segments);
					if (match != null)
						return match;
				}
			}

			PrefabStage stage = PrefabStageUtility.GetCurrentPrefabStage();
			if (stage != null && stage.prefabContentsRoot != null)
			{
				GameObject match = Walk(stage.prefabContentsRoot, segments);
				if (match != null)
					return match;
			}

			throw new InvalidOperationException(
				$"No GameObject at hierarchy path '{hierarchyPath}' in any loaded scene or open prefab stage.");
		}

		private static GameObject Walk(GameObject root, string[] segments)
		{
			if (root.name != segments[0])
				return null;

			Transform cursor = root.transform;
			for (var i = 1; i < segments.Length; i++)
			{
				Transform next = null;
				for (var c = 0; c < cursor.childCount; c++)
				{
					if (cursor.GetChild(c).name != segments[i])
						continue;
					next = cursor.GetChild(c);
					break;
				}

				if (next == null)
					return null;
				cursor = next;
			}

			return cursor.gameObject;
		}
	}
}
#endif
