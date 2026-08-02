#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEditor.Build;
using UnityEngine;

namespace AetherNexus.FoundationPlatform.Editor.Utilities
{
public static class ScriptingDefineSymbolController
{
	public static void ToggleScriptingDefineSymbol(string symbol, bool value)
	{
		if (value)
		{
			AddingDefineSymbols(symbol);
		}
		else
		{
			RemovingDefineSymbols(symbol);
		}
	}

	public static void AddingDefineSymbols(string symbol)
	{
		foreach (var group in GetInstalledBuildTargetGroups())
		{
			try
			{
				var symbols = new List<string>(PlayerSettings.GetScriptingDefineSymbols(NamedBuildTarget.FromBuildTargetGroup(group)).Split(new[] { ';' }, StringSplitOptions.RemoveEmptyEntries));
				if (!symbols.Contains(symbol))
				{
					symbols.Add(symbol);
				}

				var defines = string.Join(";", symbols.ToArray());
				PlayerSettings.SetScriptingDefineSymbols(NamedBuildTarget.FromBuildTargetGroup(group), defines);
			}
			catch (Exception e)
			{
				Debug.LogException(e);
			}
		}

		ReloadScript();
	}

	public static void RemovingDefineSymbols(string symbol)
	{
		foreach (var group in GetInstalledBuildTargetGroups())
		{
			try
			{
				var symbols = new List<string>(PlayerSettings.GetScriptingDefineSymbols(NamedBuildTarget.FromBuildTargetGroup(group)).Split(';'));
				symbols.Remove(symbol);
				var defines = string.Join(";", symbols.ToArray());
				PlayerSettings.SetScriptingDefineSymbols(NamedBuildTarget.FromBuildTargetGroup(group), defines);
			}
			catch (Exception e)
			{
				Debug.LogException(e);
			}
		}

		ReloadScript();
	}

	public static HashSet<BuildTargetGroup> GetInstalledBuildTargetGroups()
	{
		var targetGroups = new HashSet<BuildTargetGroup>();
		foreach (var target in (BuildTarget[])Enum.GetValues(typeof(BuildTarget)))
		{
			var group = BuildPipeline.GetBuildTargetGroup(target);
			if (BuildPipeline.IsBuildTargetSupported(group, target))
			{
				targetGroups.Add(group);
			}
		}

		return targetGroups;
	}

	/// <summary>
	/// Reimports scripts from the specified asset path. If path is null or empty, only refreshes the asset database.
	/// </summary>
	/// <param name="assetPath">Optional asset path to reimport. If null or empty, only refreshes the asset database.</param>
	public static void ReimportScripts(string assetPath)
	{
		if (!string.IsNullOrEmpty(assetPath))
		{
			AssetDatabase.ImportAsset(assetPath);
		}
	}

	/// <summary>Refreshes the asset database without reimporting a specific path.</summary>
	public static void ReimportScripts() => ReimportScripts(null);

	public static void ReloadScript()
	{
		AssetDatabase.SaveAssets();
		AssetDatabase.Refresh();
		EditorUtility.RequestScriptReload();
	}
}
}
#endif
