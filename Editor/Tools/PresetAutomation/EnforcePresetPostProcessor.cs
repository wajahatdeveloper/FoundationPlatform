#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.Experimental;
using UnityEditor.Presets;
using UnityEngine;

namespace FoundationPlatform.Editor.Utilities.PresetAutomation
{
	/// <summary>
	/// Applies Presets automatically to Assets in the folder tree and manages dependencies.
	/// Enhanced with settings-driven filtering, batching, caching, error handling, and diagnostics.
	/// </summary>
	public class EnforcePresetPostProcessor : AssetPostprocessor
	{
		private static readonly Dictionary<string, DateTime> s_lastFolderTouch = new Dictionary<string, DateTime>();
		private static readonly HashSet<string> s_pendingDependencyFolders = new HashSet<string>();
		private static double s_lastBatchInvocationEditorTime;

		void OnPreprocessAsset()
		{
			// Do not create assets during import; only try to load existing settings.
			var settings = PresetAutomationSettings.FindOrCreateSettingsAsset(createIfMissing: false);
			if (settings == null || !settings.enabled)
			{
				return;
			}

			try
			{
				if (!assetPath.StartsWith("Assets/", StringComparison.Ordinal))
					return;

				// Extension and folder filters
				if (!PresetAutomationUtilities.IsPathEligible(assetPath, settings))
					return;

				var folder = Path.GetDirectoryName(assetPath);
				if (string.IsNullOrEmpty(folder)) return;

				ApplyPresetsFromFolderRecursively(folder, settings);
			}
			catch (Exception ex)
			{
				PresetAutomationLogger.LogError($"OnPreprocessAsset failed for {assetPath}: {ex}");
			}
		}

		private void ApplyPresetsFromFolderRecursively(string folder, PresetAutomationSettings settings)
		{
			// Apply from parent to child so closer presets win
			var parentFolder = Path.GetDirectoryName(folder);
			if (!string.IsNullOrEmpty(parentFolder))
				ApplyPresetsFromFolderRecursively(parentFolder, settings);

			// Dependency on folder key for change in contents
			context.DependsOnCustomDependency($"{PresetAutomationConstants.DependencyKeyPrefix}{folder}");

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

			// Optional: apply ordering via folder priority (higher number wins, applied later)
			// Materialize once to avoid repeated deferred enumeration/sorting in the loop below.
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
						applied = preset.ApplyTo(assetImporter);
					}

					if (preset == null || applied)
					{
						context.DependsOnArtifact(presetPath);
					}

					if (settings.logLevel >= PresetAutomationSettings.LogLevel.Info)
					{
						PresetAutomationLogger.LogInfo($"{(settings.dryRun ? "[DryRun] would apply" : (applied ? "Applied" : "Not applicable"))} preset {preset.name} to {assetPath}");
					}
				}
				catch (Exception ex)
				{
					PresetAutomationLogger.LogError($"Error applying preset from {presetPath} to {assetPath}: {ex}");
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
	/// Initializes and updates folder dependency hashes when presets change.
	/// Adds debounced batching and diagnostics.
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

