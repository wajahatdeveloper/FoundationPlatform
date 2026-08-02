using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine;

namespace AetherNexus.FoundationPlatform.Editor.Utilities.PresetAutomation
{
	/// <summary>
	/// Central settings for Preset Automation. Controls behavior, filtering, and UX toggles.
	/// Create an instance via the menu: Assets/Create/Preset Automation/Settings.
	/// </summary>
	public class PresetAutomationSettings : ScriptableObject
	{
		public enum LogLevel
		{
			Silent = 0,
			Error = 1,
			Warning = 2,
			Info = 3,
			Debug = 4
		}

		[Header("General")]
		public bool enabled = true;
		[Tooltip("When enabled, additional diagnostic information is logged.")]
		public bool debugMode = false;
		[Tooltip("Controls verbosity of logs emitted by the system.")]
		public LogLevel logLevel = LogLevel.Warning;

		[Header("Performance")]
		[Tooltip("Maximum number of dependency updates to coalesce into one batch.")]
		public int maxBatchSize = 200;
		[Tooltip("Milliseconds to debounce dependency registrations and heavy operations.")]
		public int debounceMilliseconds = 150;

		[Header("Application Filters")]
		[Tooltip("If non-empty, only assets under these folders are considered. Paths are project-relative (e.g. Assets/Textures).")]
		public List<string> includeFolders = new List<string>();
		[Tooltip("Assets under these folders are ignored. Paths are project-relative.")]
		public List<string> excludeFolders = new List<string>();
		[Tooltip("Only assets with these extensions (e.g. .png, .fbx) are considered. Empty means all.")]
		public List<string> includeExtensions = new List<string>();
		[Tooltip("Assets with these extensions are ignored.")]
		public List<string> excludeExtensions = new List<string> { ".cs", ".preset" };

		[Header("UX")]
		[Tooltip("Show progress bars during long-running refreshes and scans.")]
		public bool showProgressBars = true;
		[Tooltip("If enabled, the system will compute and display what would be applied without mutating importers.")]
		public bool dryRun = false;

		[Serializable]
		public class FolderPriorityRule
		{
			[Tooltip("Folder path (e.g. Assets/Art). Applies to this folder and subfolders.")]
			public string folderPath = string.Empty;
			[Tooltip("When multiple presets match, lowest number applies first; highest last.")]
			public int priority = 0;
		}

		[Header("Priorities")]
		[Tooltip("Optional folder-based priority rules. Higher priority applies later (wins conflicts)." )]
		public List<FolderPriorityRule> folderPriorities = new List<FolderPriorityRule>();

		/// <summary>
		/// Attempts to find the settings asset at the default path. Optionally creates it if missing.
		/// IMPORTANT: Do not create during import/modification callbacks; pass createIfMissing=false there.
		/// </summary>
		public static PresetAutomationSettings FindOrCreateSettingsAsset(bool createIfMissing)
		{
			#if UNITY_EDITOR
			const string defaultPath = PresetAutomationConstants.DefaultSettingsAssetPath;
			var loaded = UnityEditor.AssetDatabase.LoadAssetAtPath<PresetAutomationSettings>(defaultPath);
			if (loaded != null) return loaded;
			if (!createIfMissing)
			{
				return null;
			}
			try
			{
				var dir = Path.GetDirectoryName(defaultPath);
				if (!string.IsNullOrEmpty(dir) && !Directory.Exists(dir))
				{
					Directory.CreateDirectory(dir);
				}
				var instance = CreateInstance<PresetAutomationSettings>();
				UnityEditor.AssetDatabase.CreateAsset(instance, defaultPath);
				UnityEditor.AssetDatabase.SaveAssets();
				UnityEditor.AssetDatabase.Refresh();
				return instance;
			}
			catch (Exception ex)
			{
				UnityEngine.Debug.LogError($"[PresetAutomation] Failed to create settings at {defaultPath}: {ex.Message}");
				return null;
			}
			#else
			return null;
			#endif
		}

		/// <summary>Finds or creates the settings asset, creating it if missing.</summary>
		public static PresetAutomationSettings FindOrCreateSettingsAsset() => FindOrCreateSettingsAsset(true);
	}

	public static class PresetAutomationSettingsMenu
	{
		#if UNITY_EDITOR
		[UnityEditor.MenuItem("Assets/Create/Foundation/Preset Automation Settings", false, 2000)]
		private static void CreateSettingsAsset()
		{
			var settings = ScriptableObject.CreateInstance<PresetAutomationSettings>();
			const string name = "PresetAutomationSettings";
			var path = UnityEditor.AssetDatabase.GenerateUniqueAssetPath($"Assets/{name}.asset");
			UnityEditor.AssetDatabase.CreateAsset(settings, path);
			UnityEditor.AssetDatabase.SaveAssets();
			UnityEditor.Selection.activeObject = settings;
		}
		#endif
	}
}


