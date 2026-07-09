#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using FoundationPlatform.Editor.AssetImport;
using UnityEditor;
using UnityEditor.Experimental;
using UnityEditor.Presets;
using UnityEngine;

namespace FoundationPlatform.Editor.Utilities.PresetAutomation
{
	[InitializeOnLoad]
	internal static class EnforcePresetPreprocessPluginRegistration
	{
		static EnforcePresetPreprocessPluginRegistration()
		{
			AssetImportPluginRegistry.RegisterPreprocess(new EnforcePresetPreprocessPlugin());
		}
	}

	internal sealed class EnforcePresetPreprocessPlugin : IAssetPreprocessPlugin
	{
		public bool CanPreprocess(string assetPath)
		{
			if (string.IsNullOrEmpty(assetPath))
				return false;
			if (!assetPath.StartsWith("Assets/", StringComparison.Ordinal))
				return false;

			var settings = PresetAutomationSettings.FindOrCreateSettingsAsset(createIfMissing: false);
			if (settings == null || !settings.enabled)
				return false;

			return PresetAutomationUtilities.IsPathEligible(assetPath, settings);
		}

		public void OnPreprocess(string assetPath, AssetPostprocessor host)
		{
			var settings = PresetAutomationSettings.FindOrCreateSettingsAsset(createIfMissing: false);
			if (settings == null || !settings.enabled)
				return;

			try
			{
				var folder = Path.GetDirectoryName(assetPath);
				if (string.IsNullOrEmpty(folder))
					return;

				ApplyPresetsFromFolderRecursively(folder, settings, host);
			}
			catch (Exception ex)
			{
				PresetAutomationLogger.LogError($"OnPreprocessAsset failed for {assetPath}: {ex}");
			}
		}

		private void ApplyPresetsFromFolderRecursively(string folder, PresetAutomationSettings settings, AssetPostprocessor host)
		{
			var parentFolder = Path.GetDirectoryName(folder);
			if (!string.IsNullOrEmpty(parentFolder))
				ApplyPresetsFromFolderRecursively(parentFolder, settings, host);

			host.context.DependsOnCustomDependency($"{PresetAutomationConstants.DependencyKeyPrefix}{folder}");

			IEnumerable<string> presetPaths;
			try
			{
				presetPaths = PresetCache.GetPresetsForFolder(folder);
			}
			catch (Exception ex)
			{
				PresetAutomationLogger.LogError($"Failed to get presets for folder {folder}: {ex}");
				return;
			}

			var orderedPresetPaths = PresetAutomationUtilities.OrderByFolderPriority(presetPaths, folder, settings).ToList();
			int orderedCount = orderedPresetPaths.Count;

			if (settings.debugMode)
			{
				PresetAutomationLogger.LogDebug($"Found {orderedCount} presets in {folder}");
			}

			int index = 0;
			foreach (var presetPath in orderedPresetPaths)
			{
				try
				{
					var preset = AssetDatabase.LoadAssetAtPath<Preset>(presetPath);
					if (preset == null)
					{
						PresetAutomationLogger.LogWarning($"Null preset at {presetPath}");
						continue;
					}

					var importer = AssetImporter.GetAtPath(presetPath);
					if (importer == null || importer.userData != "auto_apply")
					{
						continue;
					}

					bool applied = false;
					if (!settings.dryRun)
					{
						if (settings.showProgressBars)
						{
							EditorUtility.DisplayProgressBar("Applying Presets", preset.name, (float)index / Mathf.Max(1, orderedCount));
						}
						applied = preset.ApplyTo(host.assetImporter);
					}

					if (preset == null || applied)
					{
						host.context.DependsOnArtifact(presetPath);
					}

					if (settings.logLevel >= PresetAutomationSettings.LogLevel.Info)
					{
						PresetAutomationLogger.LogInfo($"{(settings.dryRun ? "[DryRun] would apply" : (applied ? "Applied" : "Not applicable"))} preset {preset.name} to {host.assetPath}");
					}
				}
				catch (Exception ex)
				{
					PresetAutomationLogger.LogError($"Error applying preset from {presetPath} to {host.assetPath}: {ex}");
				}
				finally
				{
					index++;
				}
			}

			if (settings.showProgressBars)
			{
				EditorUtility.ClearProgressBar();
			}
		}
	}

	/// <summary>
	/// Folder dependency hashes when presets change.
	/// </summary>
	public class UpdateFolderPresetDependency : AssetsModifiedProcessor
	{
		[InitializeOnLoadMethod]
		static void InitPresetDependencies()
		{
			// Initialization runs at editor load; avoid creating assets implicitly.
			var settings = PresetAutomationSettings.FindOrCreateSettingsAsset(createIfMissing: false);
			if (settings == null || !settings.enabled) return;

			var sw = Stopwatch.StartNew();
			bool atLeastOneUpdate = false;
			try
			{
				var allPaths = AssetDatabase.FindAssets(PresetAutomationConstants.PresetGlob)
					.Select(AssetDatabase.GUIDToAssetPath)
					.OrderBy(a => a)
					.ToList();

				string previousPath = string.Empty;
				Hash128 hash = new Hash128();
				for (var index = 0; index < allPaths.Count; index++)
				{
					var path = allPaths[index];
					var folder = Path.GetDirectoryName(path);
					if (folder != previousPath)
					{
						if (previousPath != string.Empty)
						{
							AssetDatabase.RegisterCustomDependency($"{PresetAutomationConstants.DependencyKeyPrefix}{previousPath}", hash);
							atLeastOneUpdate = true;
						}
						hash = new Hash128();
						previousPath = folder;
					}

					hash.Append(path);
					var pr = AssetDatabase.LoadAssetAtPath<Preset>(path);
					if (pr != null)
					{
						hash.Append(pr.GetTargetFullTypeName());
						var importer = AssetImporter.GetAtPath(path);
						if (importer != null)
						{
							hash.Append(importer.userData);
						}
					}
				}

				if (previousPath != string.Empty)
				{
					AssetDatabase.RegisterCustomDependency($"{PresetAutomationConstants.DependencyKeyPrefix}{previousPath}", hash);
					atLeastOneUpdate = true;
				}
			}
			catch (Exception ex)
			{
				PresetAutomationLogger.LogError($"InitPresetDependencies failed: {ex}");
			}
			finally
			{
				sw.Stop();
				if (atLeastOneUpdate)
				{
					AssetDatabase.Refresh();
				}
				PresetAutomationLogger.LogDebug($"InitPresetDependencies completed in {sw.ElapsedMilliseconds} ms");
			}
		}

		protected override void OnAssetsModified(string[] changedAssets, string[] addedAssets, string[] deletedAssets, AssetMoveInfo[] movedAssets)
		{
			// Asset change processing occurs during import; avoid creating assets implicitly.
			var settings = PresetAutomationSettings.FindOrCreateSettingsAsset(createIfMissing: false);
			if (settings == null || !settings.enabled) return;

			HashSet<string> folders = new HashSet<string>();
			void Consider(string path)
			{
				if (path.EndsWith(".preset", StringComparison.Ordinal))
				{
					var folder = Path.GetDirectoryName(path);
					if (!string.IsNullOrEmpty(folder)) folders.Add(folder);
				}
			}

			foreach (var a in changedAssets) Consider(a);
			foreach (var a in addedAssets) Consider(a);
			foreach (var a in deletedAssets) Consider(a);
			foreach (var m in movedAssets)
			{
				Consider(m.destinationAssetPath);
				Consider(m.sourceAssetPath);
			}

			if (folders.Count == 0) return;

			// Debounce and batch dependency registration
			PresetAutomationBatchedRegistrar.EnqueueFolders(folders);
		}

		internal static void DelayedDependencyRegistration(HashSet<string> folders)
		{
			var sw = Stopwatch.StartNew();
			try
			{
				foreach (var folder in folders)
				{
					var presetPaths = AssetDatabase
						.FindAssets(PresetAutomationConstants.PresetGlob, new[] { folder })
						.Select(AssetDatabase.GUIDToAssetPath)
						.Where(p => Path.GetDirectoryName(p) == folder)
						.OrderBy(a => a);

					Hash128 hash = new Hash128();
					foreach (var presetPath in presetPaths)
					{
						hash.Append(presetPath);
						var pr = AssetDatabase.LoadAssetAtPath<Preset>(presetPath);
						if (pr != null)
						{
							hash.Append(pr.GetTargetFullTypeName());
							var importer = AssetImporter.GetAtPath(presetPath);
							if (importer != null)
							{
								hash.Append(importer.userData);
							}
						}
					}

					AssetDatabase.RegisterCustomDependency($"{PresetAutomationConstants.DependencyKeyPrefix}{folder}", hash);
				}
			}
			catch (Exception ex)
			{
				PresetAutomationLogger.LogError($"DelayedDependencyRegistration failed: {ex}");
			}
			finally
			{
				sw.Stop();
				AssetDatabase.Refresh();
				PresetAutomationLogger.LogDebug($"Dependency registration for {folders.Count} folder(s) took {sw.ElapsedMilliseconds} ms");
			}
		}
	}

	internal static class PresetAutomationBatchedRegistrar
	{
		private static readonly HashSet<string> pendingFolders = new HashSet<string>();
		private static double nextRunAt = 0;

		public static void EnqueueFolders(IEnumerable<string> folders)
		{
			foreach (var f in folders) pendingFolders.Add(f);
			if (nextRunAt == 0)
			{
				Schedule();
			}
		}

		private static void Schedule()
		{
			// Scheduling from editor update; avoid accidental asset creation.
			var settings = PresetAutomationSettings.FindOrCreateSettingsAsset(createIfMissing: false);
			int delayMs = Mathf.Max(50, settings != null ? settings.debounceMilliseconds : 150);
			nextRunAt = EditorApplication.timeSinceStartup + (delayMs / 1000.0);
			EditorApplication.update -= OnUpdate;
			EditorApplication.update += OnUpdate;
		}

		private static void OnUpdate()
		{
			if (EditorApplication.timeSinceStartup < nextRunAt) return;
			EditorApplication.update -= OnUpdate;
			nextRunAt = 0;

			var copy = new HashSet<string>(pendingFolders);
			pendingFolders.Clear();
			UpdateFolderPresetDependency.DelayedDependencyRegistration(copy);
		}
	}

	internal static class PresetCache
	{
		private static readonly Dictionary<string, (long timestamp, List<string> presets)> folderToPresets = new Dictionary<string, (long, List<string>)>();

		public static IEnumerable<string> GetPresetsForFolder(string folder)
		{
			long now = DateTime.UtcNow.Ticks;
			if (folderToPresets.TryGetValue(folder, out var entry))
			{
				// Reuse cache for a short time window. Return a copy so callers cannot
				// mutate (sort/add to) the internal cached list and corrupt the entry.
				if (now - entry.timestamp < TimeSpan.FromSeconds(2).Ticks)
				{
					return new List<string>(entry.presets);
				}
			}

			var list = Directory
				.EnumerateFiles(folder, "*.preset", SearchOption.TopDirectoryOnly)
				.OrderBy(a => a)
				.ToList();
			folderToPresets[folder] = (now, list);
			return list;
		}
	}

	internal static class PresetAutomationLogger
	{
		public static void LogInfo(string message)
		{
			var s = PresetAutomationSettings.FindOrCreateSettingsAsset(createIfMissing: false);
			if (s != null && s.logLevel >= PresetAutomationSettings.LogLevel.Info)
			{
				UnityEngine.Debug.Log($"[PresetAutomation] {message}");
			}
		}

		public static void LogWarning(string message)
		{
			var s = PresetAutomationSettings.FindOrCreateSettingsAsset(createIfMissing: false);
			if (s == null || s.logLevel < PresetAutomationSettings.LogLevel.Warning) return;
			UnityEngine.Debug.LogWarning($"[PresetAutomation] {message}");
		}

		public static void LogError(string message)
		{
			var s = PresetAutomationSettings.FindOrCreateSettingsAsset(createIfMissing: false);
			if (s != null && s.logLevel == PresetAutomationSettings.LogLevel.Silent) return;
			UnityEngine.Debug.LogError($"[PresetAutomation] {message}");
		}

		public static void LogDebug(string message)
		{
			var s = PresetAutomationSettings.FindOrCreateSettingsAsset(createIfMissing: false);
			if (s != null && s.logLevel >= PresetAutomationSettings.LogLevel.Debug)
			{
				UnityEngine.Debug.Log($"[PresetAutomation][Debug] {message}");
			}
		}
	}

}
#endif

