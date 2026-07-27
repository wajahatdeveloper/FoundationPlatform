#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEditor.Build;

namespace AetherNexus.FoundationPlatform.Editor
{
	/// <summary>
	/// Removes leftover PlayerSettings <c>HOMAM_GEC</c> when Game Engine Core is not a registered
	/// UPM package. Gated FP/UIWidgets asmdefs use <c>defineConstraints</c> + <c>versionDefines</c>;
	/// a stale PlayerSettings symbol would keep them compiling and break hard refs to GEC after uninstall.
	/// Does not add the symbol — presence gating is versionDefines-only.
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
