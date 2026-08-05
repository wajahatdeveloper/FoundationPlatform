#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEditor.Build;

namespace AetherNexus.FoundationPlatform.Editor
{
	/// <summary>
	/// Removes leftover PlayerSettings <c>HOMAM_GEC</c> when Game Engine Core is not a registered
	/// UPM package. FP's ProjectWindowX/HierarchyX/EditorEnhancerX/StaleComponentGuard asmdefs compile
	/// unconditionally (empty <c>defineConstraints</c>); they only use <c>versionDefines</c> so the
	/// symbol is available for optional <c>#if HOMAM_GEC</c> guards. UIWidgets' GEC-integration editor
	/// asmdef still gates on the symbol via <c>defineConstraints</c> — a stale PlayerSettings entry
	/// would keep that assembly compiling after GEC uninstall. Does not add the symbol.
	/// </summary>
	[InitializeOnLoad]
	internal static class HomamGecOrphanDefineCleaner
	{
		private const string Symbol = "HOMAM_GEC";
		private const string GecPackageId = "com.aethernexus.gameenginecore";

		static HomamGecOrphanDefineCleaner()
		{
			EditorApplication.delayCall += CleanIfOrphaned;
		}

		private static void CleanIfOrphaned()
		{
			if (IsGecPackageRegistered())
				return;

			foreach (BuildTargetGroup group in Enum.GetValues(typeof(BuildTargetGroup)))
			{
				if (group == BuildTargetGroup.Unknown)
					continue;

				try
				{
					RemoveSymbol(NamedBuildTarget.FromBuildTargetGroup(group));
				}
				catch
				{
					// Unknown / deprecated / unsupported groups — skip
				}
			}
		}

		private static bool IsGecPackageRegistered()
		{
			foreach (UnityEditor.PackageManager.PackageInfo package in
			         UnityEditor.PackageManager.PackageInfo.GetAllRegisteredPackages())
			{
				if (package.name == GecPackageId)
					return true;
			}

			return false;
		}

		private static void RemoveSymbol(NamedBuildTarget namedTarget)
		{
			string defines = PlayerSettings.GetScriptingDefineSymbols(namedTarget);
			var list = new List<string>(defines.Split(new[] { ';' }, StringSplitOptions.RemoveEmptyEntries));
			if (!list.Remove(Symbol))
				return;

			PlayerSettings.SetScriptingDefineSymbols(namedTarget, string.Join(";", list));
		}
	}
}
#endif
