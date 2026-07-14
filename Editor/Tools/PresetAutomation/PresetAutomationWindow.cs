#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using AetherNexus.FoundationPlatform.Utilities.Menus;
using UnityEditor;
using UnityEditor.Presets;
using UnityEngine;

namespace AetherNexus.FoundationPlatform.Editor.Utilities.PresetAutomation
{
	public class PresetAutomationWindow : EditorWindow
	{
		private Vector2 _scroll;
		private string _diagnosticsFolder = "Assets";
		private PresetAutomationSettings _settings;

		private void OnEnable()
		{
			// Resolve (and create if needed) the settings asset once when the window opens,
			// rather than hitting the AssetDatabase on every OnGUI repaint.
			_settings = PresetAutomationSettings.FindOrCreateSettingsAsset();
		}

		[MenuItem(MenuPaths.WindowUtilities.PresetAutomation, priority = 1106)] 
		public static void Open()
		{
			var wnd = GetWindow<PresetAutomationWindow>(false, "Preset Automation");
			wnd.minSize = new Vector2(500, 360);
			wnd.Show();
		}

		void OnGUI()
		{
			// Reuse the asset loaded in OnEnable; re-resolve only if it was lost (e.g. domain reload).
			if (_settings == null)
			{
				_settings = PresetAutomationSettings.FindOrCreateSettingsAsset();
			}
			var settings = _settings;
			if (settings == null)
			{
				EditorGUILayout.HelpBox("Settings asset not found.", MessageType.Error);
				return;
			}

			EditorGUILayout.LabelField("General", EditorStyles.boldLabel);
			settings.enabled = EditorGUILayout.Toggle("Enabled", settings.enabled);
			settings.debugMode = EditorGUILayout.Toggle("Debug Mode", settings.debugMode);
			settings.logLevel = (PresetAutomationSettings.LogLevel)EditorGUILayout.EnumPopup("Log Level", settings.logLevel);

			EditorGUILayout.Space();
			EditorGUILayout.LabelField("Performance", EditorStyles.boldLabel);
			settings.maxBatchSize = EditorGUILayout.IntField("Max Batch Size", settings.maxBatchSize);
			settings.debounceMilliseconds = EditorGUILayout.IntField("Debounce (ms)", settings.debounceMilliseconds);

			EditorGUILayout.Space();
			EditorGUILayout.LabelField("Filters", EditorStyles.boldLabel);
			DrawList("Include Folders", settings.includeFolders);
			DrawList("Exclude Folders", settings.excludeFolders);
			DrawList("Include Extensions", settings.includeExtensions);
			DrawList("Exclude Extensions", settings.excludeExtensions);

			EditorGUILayout.Space();
			EditorGUILayout.LabelField("UX", EditorStyles.boldLabel);
			settings.showProgressBars = EditorGUILayout.Toggle("Show Progress Bars", settings.showProgressBars);
			settings.dryRun = EditorGUILayout.Toggle("Dry Run", settings.dryRun);

			EditorGUILayout.Space();
			EditorGUILayout.LabelField("Folder Priorities", EditorStyles.boldLabel);
			if (settings.folderPriorities == null) settings.folderPriorities = new List<PresetAutomationSettings.FolderPriorityRule>();
			for (int i = 0; i < settings.folderPriorities.Count; i++)
			{
				var rule = settings.folderPriorities[i];
				EditorGUILayout.BeginHorizontal();
				rule.folderPath = EditorGUILayout.TextField(rule.folderPath);
				rule.priority = EditorGUILayout.IntField(rule.priority, GUILayout.Width(80));
				if (GUILayout.Button("X", GUILayout.Width(22)))
				{
					settings.folderPriorities.RemoveAt(i);
					GUI.FocusControl(null);
					break;
				}
				EditorGUILayout.EndHorizontal();
			}
			if (GUILayout.Button("Add Priority Rule"))
			{
				settings.folderPriorities.Add(new PresetAutomationSettings.FolderPriorityRule());
			}

			EditorGUILayout.Space();
			if (GUILayout.Button("Save Settings"))
			{
				EditorUtility.SetDirty(settings);
				AssetDatabase.SaveAssets();
			}

			EditorGUILayout.Space();
			EditorGUILayout.LabelField("Diagnostics", EditorStyles.boldLabel);

			EditorGUILayout.BeginHorizontal();
			_diagnosticsFolder = EditorGUILayout.TextField("Folder", _diagnosticsFolder);
			if (GUILayout.Button("Scan Presets", GUILayout.Width(120)))
			{
				ScanDiagnostics(_diagnosticsFolder);
			}
			if (GUILayout.Button("Validate", GUILayout.Width(90)))
			{
				RunValidation(_diagnosticsFolder);
			}
			if (GUILayout.Button("Dependency View", GUILayout.Width(140)))
			{
				ShowDependencyView(_diagnosticsFolder);
			}
			EditorGUILayout.EndHorizontal();
		}

		private void ScanDiagnostics(string folder)
		{
			var guids = AssetDatabase.FindAssets(PresetAutomationConstants.PresetGlob, new[] { folder });
			var list = guids.Select(AssetDatabase.GUIDToAssetPath).OrderBy(a => a).ToList();
			EditorUtility.DisplayDialog("Preset Automation", $"Found {list.Count} preset(s) under {folder}.", "OK");
		}

		private void RunValidation(string folder)
		{
			var settings = PresetAutomationSettings.FindOrCreateSettingsAsset();
			var guids = AssetDatabase.FindAssets(PresetAutomationConstants.PresetGlob, new[] { folder });
			var paths = guids.Select(AssetDatabase.GUIDToAssetPath).OrderBy(a => a).ToList();
			int nullTargets = 0;
			foreach (var p in paths)
			{
				var pr = AssetDatabase.LoadAssetAtPath<Preset>(p);
				if (pr == null)
				{
					nullTargets++;
					continue;
				}
			}
			EditorUtility.DisplayDialog("Preset Validation", $"Checked {paths.Count} preset(s). Null: {nullTargets}.", "OK");
		}

		private void ShowDependencyView(string folder)
		{
			var guids = AssetDatabase.FindAssets(PresetAutomationConstants.PresetGlob, new[] { folder });
			var paths = guids.Select(AssetDatabase.GUIDToAssetPath).OrderBy(a => a).ToList();
			var grouped = paths.GroupBy(p => System.IO.Path.GetDirectoryName(p)).OrderBy(g => g.Key);
			var msg = string.Join("\n\n", grouped.Select(g => $"{g.Key}\n - " + string.Join("\n - ", g.Select(x => System.IO.Path.GetFileName(x)))));
			EditorUtility.DisplayDialog("Preset Dependencies (folder→presets)", string.IsNullOrEmpty(msg) ? "No presets found." : msg, "OK");
		}

		private void DrawList(string label, List<string> list)
		{
			if (list == null) return;
			EditorGUILayout.LabelField(label, EditorStyles.miniBoldLabel);
			for (int i = 0; i < list.Count; i++)
			{
				EditorGUILayout.BeginHorizontal();
				list[i] = EditorGUILayout.TextField(list[i]);
				if (GUILayout.Button("X", GUILayout.Width(22)))
				{
					list.RemoveAt(i);
					GUI.FocusControl(null);
					break;
				}
				EditorGUILayout.EndHorizontal();
			}
			if (GUILayout.Button($"Add to {label}"))
			{
				list.Add(string.Empty);
			}
		}
	}

	[CustomEditor(typeof(Preset))]
	[CanEditMultipleObjects]
	public class PresetAutomationInspector : UnityEditor.Editor
	{
		private UnityEditor.Editor _defaultEditor;

		private void OnEnable()
		{
			var editorType = typeof(UnityEditor.Editor).Assembly.GetType("UnityEditor.Presets.PresetEditor");
			if (editorType != null)
			{
				_defaultEditor = CreateEditor(targets, editorType);
			}
		}

		private void OnDisable()
		{
			if (_defaultEditor != null)
			{
				DestroyImmediate(_defaultEditor);
				_defaultEditor = null;
			}
		}

		public override void OnInspectorGUI()
		{
			if (_defaultEditor != null)
			{
				_defaultEditor.OnInspectorGUI();
			}
			else
			{
				DrawDefaultInspector();
			}

			EditorGUILayout.Space();
			EditorGUILayout.LabelField("Preset Automation", EditorStyles.boldLabel);

			bool allAutoApply = true;
			bool mixedAutoApply = false;
			bool first = true;

			foreach (var t in targets)
			{
				var path = AssetDatabase.GetAssetPath(t);
				if (string.IsNullOrEmpty(path)) continue;
				var importer = AssetImporter.GetAtPath(path);
				bool enabled = importer != null && importer.userData == "auto_apply";

				if (first)
				{
					allAutoApply = enabled;
					first = false;
				}
				else if (enabled != allAutoApply)
				{
					mixedAutoApply = true;
				}
			}

			EditorGUI.showMixedValue = mixedAutoApply;
			EditorGUI.BeginChangeCheck();
			bool newAutoApply = EditorGUILayout.Toggle("Auto Apply (Folder & Subfolders)", allAutoApply);
			if (EditorGUI.EndChangeCheck())
			{
				foreach (var t in targets)
				{
					var path = AssetDatabase.GetAssetPath(t);
					if (string.IsNullOrEmpty(path)) continue;
					var importer = AssetImporter.GetAtPath(path);
					if (importer != null)
					{
						importer.userData = newAutoApply ? "auto_apply" : "";
						importer.SaveAndReimport();
					}
				}
			}
			EditorGUI.showMixedValue = false;
		}
	}
}
#endif

