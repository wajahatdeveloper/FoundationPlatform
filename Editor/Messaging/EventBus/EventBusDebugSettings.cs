using System;
using System.IO;
using UnityEngine;

namespace AetherNexus.FoundationPlatform.Editor.Utilities.Messaging
{
	/// <summary>
	/// Per-project EventBus debug settings at ProjectSettings/EventBusDebug.json.
	/// Controls whether DEVELOPMENT_BUILD players compile EventBus reflection debug metadata
	/// (scripting define EVENTBUS_DEBUG_REFLECTION).
	/// </summary>
	[Serializable]
	internal sealed class EventBusDebugSettings
	{
		public const string DefineSymbol = "EVENTBUS_DEBUG_REFLECTION";

		public bool includeReflectionInDevelopmentBuilds;

		private static readonly string Path_ =
			Path.Combine(Directory.GetCurrentDirectory(), "ProjectSettings", "EventBusDebug.json");

		private static EventBusDebugSettings _instance;
		public static EventBusDebugSettings Instance => _instance ??= Load();

		private static EventBusDebugSettings Load()
		{
			try
			{
				if (File.Exists(Path_))
				{
					var s = JsonUtility.FromJson<EventBusDebugSettings>(File.ReadAllText(Path_));
					if (s != null)
						return s;
				}
			}
			catch { /* corrupt: start fresh */ }
			return new EventBusDebugSettings();
		}

		public void Save()
		{
			try { File.WriteAllText(Path_, JsonUtility.ToJson(this, true)); }
			catch { /* best effort */ }
		}

		public static void Reload() => _instance = Load();
	}
}
