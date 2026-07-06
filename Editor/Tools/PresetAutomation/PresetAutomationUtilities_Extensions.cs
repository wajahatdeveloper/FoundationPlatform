using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace FoundationPlatform.Editor.Utilities.PresetAutomation
{
	internal static class PresetAutomationUtilities
	{
		public static bool IsPathEligible(string assetPath, PresetAutomationSettings settings)
		{
			try
			{
				string ext = Path.GetExtension(assetPath);
				if (!string.IsNullOrEmpty(ext))
				{
					// File extensions are case-insensitive; compare ignoring case so that e.g.
					// '.PNG' matches a user-entered filter of '.png'.
					if (settings.excludeExtensions != null && settings.excludeExtensions.Contains(ext, StringComparer.OrdinalIgnoreCase)) return false;
					if (settings.includeExtensions != null && settings.includeExtensions.Count > 0 && !settings.includeExtensions.Contains(ext, StringComparer.OrdinalIgnoreCase)) return false;
				}

				if (settings.includeFolders != null && settings.includeFolders.Count > 0)
				{
					bool underInclude = settings.includeFolders.Any(f => assetPath.StartsWith(NormalizeFolder(f), StringComparison.OrdinalIgnoreCase));
					if (!underInclude) return false;
				}

				if (settings.excludeFolders != null && settings.excludeFolders.Any(f => assetPath.StartsWith(NormalizeFolder(f), StringComparison.OrdinalIgnoreCase)))
				{
					return false;
				}
			}
			catch
			{
				return false;
			}

			return !assetPath.EndsWith(".cs", StringComparison.Ordinal) && !assetPath.EndsWith(".preset", StringComparison.Ordinal);
		}

		public static IEnumerable<string> OrderByFolderPriority(IEnumerable<string> presetPaths, string folder, PresetAutomationSettings settings)
		{
			if (settings == null || settings.folderPriorities == null || settings.folderPriorities.Count == 0)
				return presetPaths;

			int priority = 0;
			foreach (var rule in settings.folderPriorities)
			{
				if (string.IsNullOrEmpty(rule.folderPath)) continue;
				var normalized = NormalizeFolder(rule.folderPath);
				var f = NormalizeFolder(folder);
				if (f.StartsWith(normalized, StringComparison.Ordinal))
				{
					priority = rule.priority;
				}
			}

			// Apply the same priority to all presets from this folder; stable order otherwise
			return presetPaths.OrderBy(p => priority);
		}

		private static string NormalizeFolder(string folder)
		{
			if (string.IsNullOrEmpty(folder)) return string.Empty;
			folder = folder.Replace("\\", "/");
			if (!folder.EndsWith("/", StringComparison.Ordinal)) folder += "/";
			return folder;
		}
	}
}


