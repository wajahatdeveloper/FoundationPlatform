#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using UnityEditor;
using UnityEditor.Compilation;
using UnityEditor.SceneManagement;
using UnityEngine;

public static class MonoBehaviourScriptDuplicator
{
	private const int MenuPriority = 100;
	private const string PendingComponentKey = "FoundationPlatform.MonoBehaviourScriptDuplicator.PendingComponentGlobalId";
	private const string PendingScriptPathKey = "FoundationPlatform.MonoBehaviourScriptDuplicator.PendingScriptPath";
	private const string PendingSourceTypeNameKey = "FoundationPlatform.MonoBehaviourScriptDuplicator.PendingSourceTypeName";
	private const string PendingReplacementKey = "FoundationPlatform.MonoBehaviourScriptDuplicator.PendingReplacement";

	private static bool _isCompilationHandlerRegistered;

	private readonly struct DuplicationSession
	{
		public readonly MonoBehaviour SourceComponent;
		public readonly Type SourceType;
		public readonly string SourceScriptPath;
		public readonly string SourceNamespace;
		public readonly bool ReplaceAfterGenerate;

		public DuplicationSession(
			MonoBehaviour sourceComponent,
			Type sourceType,
			string sourceScriptPath,
			string sourceNamespace,
			bool replaceAfterGenerate)
		{
			SourceComponent = sourceComponent;
			SourceType = sourceType;
			SourceScriptPath = sourceScriptPath;
			SourceNamespace = sourceNamespace;
			ReplaceAfterGenerate = replaceAfterGenerate;
		}
	}

	[MenuItem("CONTEXT/MonoBehaviour/Duplicate", false, MenuPriority)]
	private static void Duplicate(MenuCommand command)
	{
		OpenGeneratorWindow(command, replaceAfterGenerate: false);
	}

	[MenuItem("CONTEXT/MonoBehaviour/Duplicate and Replace", false, MenuPriority + 1)]
	private static void DuplicateAndReplace(MenuCommand command)
	{
		OpenGeneratorWindow(command, replaceAfterGenerate: true);
	}

	[MenuItem("CONTEXT/MonoBehaviour/Duplicate", true)]
	[MenuItem("CONTEXT/MonoBehaviour/Duplicate and Replace", true)]
	private static bool ValidateMenu(MenuCommand command)
	{
		return TryCreateSession(command, replaceAfterGenerate: false, out _);
	}

	[MenuItem("CONTEXT/MonoBehaviour/Replace Script...", false, MenuPriority + 2)]
	private static void ReplaceScript(MenuCommand command)
	{
		var behaviour = command.context as MonoBehaviour;
		if (behaviour == null) return;
		ScriptReplacerWindow.Show(behaviour);
	}

	[MenuItem("CONTEXT/MonoBehaviour/Replace Script...", true)]
	private static bool ValidateReplaceScript(MenuCommand command)
	{
		return command.context is MonoBehaviour;
	}

	private static void OpenGeneratorWindow(MenuCommand command, bool replaceAfterGenerate)
	{
		if (!TryCreateSession(command, replaceAfterGenerate, out var session))
		{
			throw new InvalidOperationException("MonoBehaviourScriptDuplicator: invalid duplication context.");
		}

		var sourceTypeName = session.SourceType.Name;
		var defaultClassName = sourceTypeName;
		var outputFolder = Path.GetDirectoryName(session.SourceScriptPath).Replace('\\', '/');
		var title = replaceAfterGenerate ? "Duplicate and Replace" : "Duplicate Script";

		ScriptGeneratorWindow.Show(
			title,
			outputFolder,
			defaultClassName + ".cs",
			defaultClassName,
			session.SourceNamespace,
			context => BuildDuplicatedCode(session, context),
			context => ValidateDuplication(session, context),
			replaceAfterGenerate ? generatedPath => HandleGenerated(session, generatedPath) : null);
	}

	private static bool TryCreateSession(MenuCommand command, bool replaceAfterGenerate, out DuplicationSession session)
	{
		session = default;
		var behaviour = command.context as MonoBehaviour;
		if (behaviour == null)
		{
			return false;
		}

		var sourceType = behaviour.GetType();
		var monoScript = MonoScript.FromMonoBehaviour(behaviour);
		if (monoScript == null)
		{
			return false;
		}

		var scriptPath = AssetDatabase.GetAssetPath(monoScript);
		if (string.IsNullOrEmpty(scriptPath) || !scriptPath.StartsWith("Assets/", StringComparison.Ordinal))
		{
			return false;
		}

		if (!scriptPath.EndsWith(".cs", StringComparison.OrdinalIgnoreCase) || !File.Exists(scriptPath))
		{
			return false;
		}

		session = new DuplicationSession(
			behaviour,
			sourceType,
			scriptPath,
			sourceType.Namespace ?? string.Empty,
			replaceAfterGenerate);
		return true;
	}

	private static string BuildDuplicatedCode(DuplicationSession session, ScriptGeneratorWindow.GenerationContext context)
	{
		var sourceText = File.ReadAllText(session.SourceScriptPath);
		var sourceTypeName = session.SourceType.Name;
		var newClassName = context.ClassName.Trim();

		// Code-aware rename is shared with any other generator via CodeAwareRename.
		var result = CodeAwareRename.RenameClass(sourceText, sourceTypeName, newClassName);

		var targetNamespace = (context.Namespace ?? string.Empty).Trim();
		var sourceNamespace = session.SourceNamespace ?? string.Empty;
		if (!string.Equals(targetNamespace, sourceNamespace, StringComparison.Ordinal))
		{
			result = CodeAwareRename.ApplyNamespaceChange(result, sourceNamespace, targetNamespace);
		}

		return result;
	}

	private static string ValidateDuplication(DuplicationSession session, ScriptGeneratorWindow.GenerationContext context)
	{
		var className = (context.ClassName ?? string.Empty).Trim();
		if (string.IsNullOrEmpty(className))
		{
			return "Class name is required.";
		}

		if (!IsValidIdentifier(className))
		{
			return "Class name must be a valid C# identifier.";
		}

		if (string.Equals(className, session.SourceType.Name, StringComparison.Ordinal))
		{
			return "Class name must differ from the source type name.";
		}

		if (!string.IsNullOrWhiteSpace(context.Namespace) && !IsValidNamespace(context.Namespace.Trim()))
		{
			return "Namespace is not a valid C# namespace.";
		}

		var outputPath = Path.Combine(context.Folder, context.FileName).Replace('\\', '/');
		if (string.Equals(outputPath, session.SourceScriptPath, StringComparison.OrdinalIgnoreCase))
		{
			return "Output path must differ from the source script path.";
		}

		try
		{
			BuildDuplicatedCode(session, context);
		}
		catch (Exception ex)
		{
			return ex.Message;
		}

		return null;
	}

	private static void HandleGenerated(DuplicationSession session, string generatedPath)
	{
		if (session.SourceComponent == null)
		{
			throw new InvalidOperationException("MonoBehaviourScriptDuplicator.HandleGenerated: source component is missing.");
		}

		var globalId = GlobalObjectId.GetGlobalObjectIdSlow(session.SourceComponent).ToString();
		SessionState.SetString(PendingComponentKey, globalId);
		SessionState.SetString(PendingScriptPathKey, generatedPath);
		SessionState.SetString(PendingSourceTypeNameKey, session.SourceType.Name);
		SessionState.SetBool(PendingReplacementKey, true);

		if (!_isCompilationHandlerRegistered)
		{
			CompilationPipeline.compilationFinished += OnCompilationFinished;
			_isCompilationHandlerRegistered = true;
		}
	}

	private static void OnCompilationFinished(object _)
	{
		// One-shot: unsubscribe before handling so the handler does not leak for the rest of the editor session.
		CompilationPipeline.compilationFinished -= OnCompilationFinished;
		_isCompilationHandlerRegistered = false;
		TryReplacePendingComponent();
	}

	[InitializeOnLoadMethod]
	private static void InitializeOnLoad()
	{
		EditorApplication.delayCall += TryReplacePendingComponent;
	}

	private static void TryReplacePendingComponent()
	{
		if (!SessionState.GetBool(PendingReplacementKey, false))
		{
			return;
		}

		var componentGlobalId = SessionState.GetString(PendingComponentKey, string.Empty);
		var scriptPath = SessionState.GetString(PendingScriptPathKey, string.Empty);
		var sourceTypeName = SessionState.GetString(PendingSourceTypeNameKey, string.Empty);
		if (string.IsNullOrEmpty(componentGlobalId) || string.IsNullOrEmpty(scriptPath) || string.IsNullOrEmpty(sourceTypeName))
		{
			ClearPendingReplacement();
			throw new InvalidOperationException("MonoBehaviour script replacement state is incomplete.");
		}

		if (!GlobalObjectId.TryParse(componentGlobalId, out var parsedComponentId))
		{
			ClearPendingReplacement();
			throw new InvalidOperationException("Failed parsing pending MonoBehaviour global object id.");
		}

		var sourceComponent = GlobalObjectId.GlobalObjectIdentifierToObjectSlow(parsedComponentId) as MonoBehaviour;
		if (sourceComponent == null)
		{
			ClearPendingReplacement();
			throw new InvalidOperationException("Original MonoBehaviour component could not be resolved.");
		}

		if (!File.Exists(scriptPath))
		{
			ClearPendingReplacement();
			throw new FileNotFoundException("Generated script file not found.", scriptPath);
		}

		var script = AssetDatabase.LoadAssetAtPath<MonoScript>(scriptPath);
		var newType = script != null ? script.GetClass() : null;
		if (newType == null)
		{
			return;
		}

		if (!typeof(MonoBehaviour).IsAssignableFrom(newType))
		{
			ClearPendingReplacement();
			throw new InvalidOperationException($"Generated script type '{newType.FullName}' is not a MonoBehaviour.");
		}

		var sourceType = sourceComponent.GetType();
		if (!string.Equals(sourceType.Name, sourceTypeName, StringComparison.Ordinal))
		{
			ClearPendingReplacement();
			throw new InvalidOperationException(
				$"Source component type changed from '{sourceTypeName}' to '{sourceType.Name}' before replacement could complete.");
		}

		if (!SameNormalizedBase(newType.BaseType, sourceType.BaseType))
		{
			ClearPendingReplacement();
			throw new InvalidOperationException(
				$"Generated type '{newType.FullName}' must inherit from '{sourceType.BaseType?.FullName ?? "null"}' (same base as '{sourceType.FullName}').");
		}

		ReplaceComponent(sourceComponent, newType);
		ClearPendingReplacement();
	}

	private static void ReplaceComponent(MonoBehaviour sourceComponent, Type destinationType)
	{
		if (sourceComponent == null)
		{
			throw new InvalidOperationException("Cannot replace null MonoBehaviour component.");
		}

		var owner = sourceComponent.gameObject;
		if (owner == null)
		{
			throw new InvalidOperationException("MonoBehaviour GameObject is missing.");
		}

		Undo.IncrementCurrentGroup();
		var groupId = Undo.GetCurrentGroup();
		Undo.SetCurrentGroupName("Duplicate and Replace MonoBehaviour");
		Undo.RegisterCompleteObjectUndo(owner, "Duplicate and Replace MonoBehaviour");

		var json = EditorJsonUtility.ToJson(sourceComponent);
		var newComponent = Undo.AddComponent(owner, destinationType) as MonoBehaviour;
		if (newComponent == null)
		{
			Undo.CollapseUndoOperations(groupId);
			throw new InvalidOperationException($"Failed to add generated component '{destinationType.FullName}'.");
		}

		EditorJsonUtility.FromJsonOverwrite(json, newComponent);
		Undo.DestroyObjectImmediate(sourceComponent);
		EditorSceneManager.MarkSceneDirty(owner.scene);
		Selection.activeObject = newComponent;
		EditorGUIUtility.PingObject(newComponent);
		Undo.CollapseUndoOperations(groupId);
	}

	// Walk full inheritance chain, treating open generic defs as equivalent to their closed forms.
	// Required because TypeCache.GetTypesDerivedFrom breaks at generic intermediates
	// e.g. ShopItemView_Offer -> ShopItemViewBase -> ScrollItem<T> -> ScrollItem
	private static bool InheritsFrom(Type derived, Type ancestor)
	{
		var t = derived.BaseType;
		while (t != null && t != typeof(object))
		{
			var normalized = t.IsGenericType ? t.GetGenericTypeDefinition() : t;
			if (normalized == ancestor || t == ancestor) return true;
			t = t.BaseType;
		}
		return false;
	}

	// Compares two base types treating closed generics as equivalent when they share the open-generic definition,
	// mirroring the normalization used by InheritsFrom (e.g. ScrollItem<Offer> vs ScrollItem<Bundle>).
	private static bool SameNormalizedBase(Type a, Type b)
	{
		if (a == b) return true;
		if (a == null || b == null) return false;
		var na = a.IsGenericType ? a.GetGenericTypeDefinition() : a;
		var nb = b.IsGenericType ? b.GetGenericTypeDefinition() : b;
		return na == nb;
	}

	private static List<Type> FindCompatibleTypes(Type sourceType)
	{
		var seen = new HashSet<Type>();
		var results = new List<Type>();
		var baseType = sourceType.BaseType;

		foreach (var type in TypeCache.GetTypesDerivedFrom<MonoBehaviour>())
		{
			if (type == sourceType) continue;
			if (type.IsAbstract || type.IsGenericTypeDefinition) continue;

			bool isChild     = InheritsFrom(type, sourceType);          // type derives from source
			bool isSibling   = type.BaseType == baseType;               // same direct base
			bool isSuperclass = InheritsFrom(sourceType, type);         // source derives from type

			if ((isChild || isSibling || isSuperclass) && seen.Add(type))
				results.Add(type);
		}

		results.Sort((a, b) => string.Compare(a.FullName, b.FullName, StringComparison.Ordinal));
		return results;
	}

	private static void ClearPendingReplacement()
	{
		SessionState.EraseString(PendingComponentKey);
		SessionState.EraseString(PendingScriptPathKey);
		SessionState.EraseString(PendingSourceTypeNameKey);
		SessionState.SetBool(PendingReplacementKey, false);
	}

	private static bool IsValidIdentifier(string value)
	{
		if (string.IsNullOrEmpty(value))
		{
			return false;
		}

		if (!(char.IsLetter(value[0]) || value[0] == '_'))
		{
			return false;
		}

		for (var i = 1; i < value.Length; i++)
		{
			var c = value[i];
			if (!(char.IsLetterOrDigit(c) || c == '_'))
			{
				return false;
			}
		}

		return true;
	}

	private static bool IsValidNamespace(string namespaceValue)
	{
		var segments = namespaceValue.Split('.');
		for (var i = 0; i < segments.Length; i++)
		{
			if (!IsValidIdentifier(segments[i]))
			{
				return false;
			}
		}

		return true;
	}
	private class ScriptReplacerWindow : EditorWindow
	{
		private MonoBehaviour _target;
		private List<Type> _allTypes;
		private List<Type> _filteredTypes;
		private string _search = "";
		private Vector2 _scroll;
		private int _selectedIndex = -1;

		public static void Show(MonoBehaviour target)
		{
			var compatible = FindCompatibleTypes(target.GetType());
			if (compatible.Count == 0)
			{
				EditorUtility.DisplayDialog("Replace Script",
					$"No compatible types found for '{target.GetType().Name}'.\n\nCompatible types share the same base class, are subclasses, or are superclasses.",
					"OK");
				return;
			}

			var win = CreateInstance<ScriptReplacerWindow>();
			win._target = target;
			win._allTypes = compatible;
			win._filteredTypes = new List<Type>(compatible);
			win.titleContent = new GUIContent($"Replace: {target.GetType().Name}");
			win.minSize = new Vector2(340, 420);
			win.ShowUtility();
		}

		private void OnGUI()
		{
			if (_target == null) { Close(); return; }

			EditorGUILayout.Space(4);
			EditorGUILayout.LabelField($"Source: {_target.GetType().FullName}", EditorStyles.miniLabel);
			EditorGUILayout.LabelField($"Base:   {_target.GetType().BaseType?.FullName}", EditorStyles.miniLabel);
			EditorGUILayout.Space(6);

			EditorGUI.BeginChangeCheck();
			_search = EditorGUILayout.TextField("Search", _search);
			if (EditorGUI.EndChangeCheck())
			{
				var q = _search;
				_filteredTypes = string.IsNullOrEmpty(q)
					? new List<Type>(_allTypes)
					: _allTypes.Where(t =>
						t.Name.IndexOf(q, StringComparison.OrdinalIgnoreCase) >= 0 ||
						(t.FullName != null && t.FullName.IndexOf(q, StringComparison.OrdinalIgnoreCase) >= 0))
					  .ToList();
				_selectedIndex = -1;
			}

			EditorGUILayout.Space(4);

			if (_filteredTypes.Count == 0)
			{
				EditorGUILayout.HelpBox("No matching types.", MessageType.Info);
			}
			else
			{
				_scroll = EditorGUILayout.BeginScrollView(_scroll);
				for (var i = 0; i < _filteredTypes.Count; i++)
				{
					var type = _filteredTypes[i];
					var rect = EditorGUILayout.GetControlRect(false, 18f);

					if (Event.current.type == EventType.MouseDown && rect.Contains(Event.current.mousePosition))
					{
						_selectedIndex = i;
						if (Event.current.clickCount == 2) DoReplace();
						Event.current.Use();
						Repaint();
					}

					if (_selectedIndex == i)
						EditorGUI.DrawRect(rect, new Color(0.17f, 0.36f, 0.53f, 1f));

					var relation = GetRelation(_target.GetType(), type);
					EditorGUI.LabelField(rect,
						new GUIContent(type.Name, type.FullName),
						new GUIContent($"  [{relation}]  {type.Namespace}"));
				}
				EditorGUILayout.EndScrollView();
			}

			EditorGUILayout.Space(4);
			EditorGUI.BeginDisabledGroup(_selectedIndex < 0 || _selectedIndex >= _filteredTypes.Count);
			if (GUILayout.Button("Replace", GUILayout.Height(26)))
				DoReplace();
			EditorGUI.EndDisabledGroup();
		}

		private static string GetRelation(Type source, Type candidate)
		{
			if (source.IsAssignableFrom(candidate)) return "subclass";
			if (candidate.IsAssignableFrom(source)) return "superclass";
			return "sibling";
		}

		private void DoReplace()
		{
			if (_selectedIndex < 0 || _selectedIndex >= _filteredTypes.Count) return;
			var type = _filteredTypes[_selectedIndex];
			if (!EditorUtility.DisplayDialog("Replace Script",
				$"Replace '{_target.GetType().Name}' with '{type.Name}'?\n\nShared serialized fields will be preserved.",
				"Replace", "Cancel"))
				return;
			ReplaceComponent(_target, type);
			Close();
		}
	}
}
#endif
