using System;
using System.Collections.Generic;
using System.Linq;
using AetherNexus.FoundationPlatform.Editor.Utilities;
using UnityEditor;
using UnityEditor.Build;
using UnityEngine;

namespace AetherNexus.FoundationPlatform.Editor.Utilities.Messaging
{
	/// <summary>
	/// Project Settings page (Project/EventBus Debug). Syncs the reflection-in-Development-Build
	/// toggle to scripting define EVENTBUS_DEBUG_REFLECTION.
	/// </summary>
	internal static class EventBusDebugSettingsProvider
	{
		[InitializeOnLoadMethod]
		private static void SyncDefineFromSettings()
		{
			EnsureDefineMatchesSetting(EventBusDebugSettings.Instance.includeReflectionInDevelopmentBuilds);
		}

		[SettingsProvider]
		public static SettingsProvider Create()
		{
			return new SettingsProvider("Project/EventBus Debug", SettingsScope.Project)
			{
				label = "EventBus Debug",
				keywords = new HashSet<string>(new[]
				{
					"eventbus", "reflection", "development", "debug", "EVENTBUS_DEBUG_REFLECTION"
				}),
				guiHandler = _ => DrawGui()
			};
		}

		private static void DrawGui()
		{
			var s = EventBusDebugSettings.Instance;
			EditorGUIUtility.labelWidth = 320f;

			EditorGUILayout.Space();
			EditorGUILayout.HelpBox(
				"Editor always includes EventBus reflection-based debug metadata.\n\n" +
				"Development Builds include that reflection code only when this option is enabled " +
				$"(scripting define {EventBusDebugSettings.DefineSymbol}). " +
				"A Development Build with the option on shows a Continue/Cancel warning dialog at build start. " +
				"Release builds never include this path.",
				MessageType.Info);

			EditorGUI.BeginChangeCheck();
			s.includeReflectionInDevelopmentBuilds = EditorGUILayout.Toggle(
				"Include Reflection In Development Builds",
				s.includeReflectionInDevelopmentBuilds);
			if (EditorGUI.EndChangeCheck())
			{
				s.Save();
				EnsureDefineMatchesSetting(s.includeReflectionInDevelopmentBuilds);
			}
		}

		/// <summary>
		/// Adds/removes the define only when PlayerSettings disagree with the setting.
		/// Avoids RequestScriptReload loops on every domain reload.
		/// </summary>
		private static void EnsureDefineMatchesSetting(bool enabled)
		{
			var symbol = EventBusDebugSettings.DefineSymbol;
			var changed = false;

			foreach (var group in ScriptingDefineSymbolController.GetInstalledBuildTargetGroups())
			{
				try
				{
					var named = NamedBuildTarget.FromBuildTargetGroup(group);
					var symbols = PlayerSettings.GetScriptingDefineSymbols(named)
						.Split(new[] { ';' }, StringSplitOptions.RemoveEmptyEntries)
						.ToList();
					var has = symbols.Contains(symbol);
					if (enabled && !has)
					{
						symbols.Add(symbol);
						PlayerSettings.SetScriptingDefineSymbols(named, string.Join(";", symbols));
						changed = true;
					}
					else if (!enabled && has)
					{
						symbols.Remove(symbol);
						PlayerSettings.SetScriptingDefineSymbols(named, string.Join(";", symbols));
						changed = true;
					}
				}
				catch (Exception e)
				{
					Debug.LogException(e);
				}
			}

			if (changed)
				ScriptingDefineSymbolController.ReloadScript();
		}
	}
}
