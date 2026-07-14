#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.IO;
using AetherNexus.FoundationPlatform.Utilities.Menus;
using UnityEditor;
using UnityEngine;

namespace AetherNexus.FoundationPlatform.Editor.Windows
{
public class ScriptGeneratorWindow : EditorWindow
{
	private const string LastOutputFolderPrefsKey = "FoundationPlatform.ScriptGeneratorWindow.LastOutputFolder";

	public readonly struct GenerationContext
	{
		public readonly string Folder;
		public readonly string FileName;
		public readonly string ClassName;
		public readonly string Namespace;

		public GenerationContext(string folder, string fileName, string className, string namespaceName)
		{
			Folder = folder;
			FileName = fileName;
			ClassName = className;
			Namespace = namespaceName;
		}
	}

	private string _titleContent = "Script Generator";
	private string _folder;
	private string _fileName;
	private string _className;
	private string _namespace;
	private Func<GenerationContext, string> _buildCode;
	private Func<GenerationContext, string> _validate;
	private string _previewText;
	private Vector2 _scroll;
	private bool _showCodePreview = true;
	private Action<string> _onGenerated;

	public static void Show(
		string title,
		string defaultFolder,
		string defaultFileName,
		string defaultClassName,
		string defaultNamespace,
		Func<GenerationContext, string> buildCode,
		Func<GenerationContext, string> validate,
		Action<string> onGenerated = null)
	{
		if (buildCode == null)
			throw new ArgumentNullException(nameof(buildCode));
		if (validate == null)
			throw new ArgumentNullException(nameof(validate));

		var window = GetWindow<ScriptGeneratorWindow>(true, title);
		window.minSize = new Vector2(500, 400);
		window._titleContent = title;
		var fallbackFolder = defaultFolder ?? "Assets";
		window._folder = EditorPrefs.GetString(LastOutputFolderPrefsKey, fallbackFolder);
		window._fileName = string.IsNullOrEmpty(defaultFileName) ? "Generated.cs" : (defaultFileName.EndsWith(".cs") ? defaultFileName : defaultFileName + ".cs");
		window._className = defaultClassName ?? string.Empty;
		window._namespace = defaultNamespace ?? string.Empty;
		window._buildCode = buildCode;
		window._validate = validate;
		window._onGenerated = onGenerated;
		window._previewText = buildCode(window.CurrentContext());
		window.ShowUtility();
	}

	[MenuItem(MenuPaths.WindowUtilities.ScriptGenerator, priority = 1105)]
	private static void OpenFromMenu()
	{
		GetWindow<ScriptGeneratorWindow>(true, "Script Generator").minSize = new Vector2(500, 400);
	}

	private void RefreshPreview()
	{
		_previewText = _buildCode != null ? _buildCode(CurrentContext()) : "";
	}

	private static string[] _namespacesCache;

	/// <summary>All distinct non-empty namespaces in the loaded assemblies (built once, cached).</summary>
	private static string[] GetProjectNamespaces()
	{
		if (_namespacesCache != null)
			return _namespacesCache;

		var set = new SortedSet<string>(StringComparer.Ordinal);
		foreach (var asm in AppDomain.CurrentDomain.GetAssemblies())
		{
			Type[] types;
			try { types = asm.GetTypes(); }
			catch { continue; }
			foreach (var t in types)
			{
				if (!string.IsNullOrEmpty(t.Namespace))
					set.Add(t.Namespace);
			}
		}

		_namespacesCache = new string[set.Count];
		set.CopyTo(_namespacesCache);
		return _namespacesCache;
	}

	/// <summary>Dropdown of project namespaces, filtered by whatever is already typed in the field.</summary>
	private void ShowNamespaceDropdown()
	{
		var all = GetProjectNamespaces();
		var filter = (_namespace ?? string.Empty).Trim();
		var menu = new GenericMenu();
		var shown = 0;

		for (var i = 0; i < all.Length; i++)
		{
			var ns = all[i];
			if (filter.Length > 0 && ns.IndexOf(filter, StringComparison.OrdinalIgnoreCase) < 0)
				continue;

			var captured = ns;
			// GenericMenu treats '/' as a submenu separator; namespaces use '.', so no escaping needed.
			menu.AddItem(new GUIContent(ns), false, () =>
			{
				_namespace = captured;
				RefreshPreview();
				Repaint();
			});

			if (++shown >= 200) // cap the menu; keep typing to narrow further
				break;
		}

		if (shown == 0)
			menu.AddDisabledItem(new GUIContent(filter.Length > 0 ? "No namespaces match '" + filter + "'" : "No namespaces found"));

		menu.ShowAsContext();
	}

	private GenerationContext CurrentContext()
	{
		return new GenerationContext(_folder, _fileName, _className, _namespace);
	}

	private void OnGUI()
	{
		EditorGUILayout.LabelField(_titleContent, EditorStyles.boldLabel);
		EditorGUILayout.Space(5);

		EditorGUILayout.BeginVertical(EditorStyles.helpBox);

		_folder = EditorGUILayout.TextField("Output Folder", _folder);
		EditorGUILayout.BeginHorizontal();
		GUILayout.FlexibleSpace();
		if (GUILayout.Button("Browse...", GUILayout.Width(80)))
		{
			var selected = EditorUtility.OpenFolderPanel("Select Folder", _folder, "");
			if (!string.IsNullOrEmpty(selected) && selected.StartsWith(Application.dataPath))
				_folder = "Assets" + selected.Substring(Application.dataPath.Length).Replace('\\', '/');
		}
		EditorGUILayout.EndHorizontal();

		_fileName = EditorGUILayout.TextField("File Name", _fileName);
		if (!string.IsNullOrEmpty(_fileName) && !_fileName.EndsWith(".cs"))
			_fileName += ".cs";
		_className = EditorGUILayout.TextField("Class Name", _className);

		EditorGUILayout.BeginHorizontal();
		_namespace = EditorGUILayout.TextField("Namespace", _namespace);
		if (GUILayout.Button(new GUIContent("▾", "Pick from existing project namespaces"), GUILayout.Width(22)))
			ShowNamespaceDropdown();
		EditorGUILayout.EndHorizontal();

		EditorGUILayout.EndVertical();

		var validationError = _validate != null ? _validate(CurrentContext()) : null;
		if (!string.IsNullOrEmpty(validationError))
			EditorGUILayout.HelpBox(validationError, MessageType.Error);

		EditorGUILayout.Space(5);
		EditorGUILayout.BeginHorizontal();
		_showCodePreview = EditorGUILayout.Foldout(_showCodePreview, "Code Preview", true);
		if (GUILayout.Button("Refresh", GUILayout.Width(60)))
			RefreshPreview();
		EditorGUILayout.EndHorizontal();

		if (_showCodePreview)
		{
			RefreshPreview();
			var codeStyle = new GUIStyle(EditorStyles.textArea) { font = EditorStyles.label.font };
			_scroll = EditorGUILayout.BeginScrollView(_scroll, GUILayout.ExpandHeight(true));
			EditorGUILayout.TextArea(_previewText ?? "", codeStyle, GUILayout.ExpandHeight(true));
			EditorGUILayout.EndScrollView();
		}

		EditorGUILayout.Space(5);
		var canGenerate = _buildCode != null && string.IsNullOrEmpty(validationError);
		EditorGUI.BeginDisabledGroup(!canGenerate);
		EditorGUILayout.BeginHorizontal();
		var generateButtonStyle = new GUIStyle(GUI.skin.button);
		generateButtonStyle.fontStyle = FontStyle.Bold;
		if (GUILayout.Button("Copy to Clipboard", GUILayout.Height(25)))
		{
			EditorGUIUtility.systemCopyBuffer = _previewText ?? "";
		}
		if (GUILayout.Button("Generate", generateButtonStyle, GUILayout.Height(25)))
			Generate();
		EditorGUILayout.EndHorizontal();
		EditorGUI.EndDisabledGroup();
	}

	private void Generate()
	{
		var context = CurrentContext();
		var err = _validate != null ? _validate(context) : null;
		if (!string.IsNullOrEmpty(err))
		{
			EditorUtility.DisplayDialog("Validation Error", err, "OK");
			return;
		}

		var path = Path.Combine(_folder, _fileName).Replace('\\', '/');
		if (File.Exists(path) && !EditorUtility.DisplayDialog("Overwrite?", $"File already exists:\n{path}", "Overwrite", "Cancel"))
			return;

		var code = _buildCode != null ? _buildCode(context) : null;
		if (string.IsNullOrEmpty(code))
		{
			EditorUtility.DisplayDialog("Error", "Could not generate code.", "OK");
			return;
		}

		try
		{
			var dir = Path.GetDirectoryName(path);
			if (!string.IsNullOrEmpty(dir) && !Directory.Exists(dir))
				Directory.CreateDirectory(dir);
			File.WriteAllText(path, code);
			EditorPrefs.SetString(LastOutputFolderPrefsKey, _folder);
			// Invoke before Refresh so "attach after generate" can subscribe to compilationFinished
			// (Refresh blocks until compilation finishes, so subscribing after would be too late)
			_onGenerated?.Invoke(path);
			AssetDatabase.Refresh();
			var asset = AssetDatabase.LoadAssetAtPath<UnityEngine.Object>(path);
			if (asset != null)
				EditorGUIUtility.PingObject(asset);
			EditorUtility.DisplayDialog("Success", $"Generated:\n{path}", "OK");
			Close();
		}
		catch (Exception ex)
		{
			EditorUtility.DisplayDialog("Error", "Failed to generate file:\n" + ex.Message, "OK");
		}
	}
}
}
#endif
