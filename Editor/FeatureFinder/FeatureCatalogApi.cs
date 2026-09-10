#if UNITY_EDITOR
using System;
using System.Collections.Generic;

namespace AetherNexus.FoundationPlatform.FeatureFinder.Editor
{
	/// <summary>
	/// One catalog row flattened for callers outside this assembly. <see cref="FeatureEntry"/> carries a
	/// live <see cref="Action"/> and cached lowercase search state, neither of which survives a trip
	/// through JSON or means anything to a non-UI reader.
	/// </summary>
	public sealed class FeatureDescriptor
	{
		public string MenuPath;
		public string Title;
		public string Blurb;
		public string Keywords;
		public string Kind;
		public string Doc;
		public string Detail;
		public bool IsTagged;

		/// <summary>What <see cref="FeatureCatalogApi.Invoke"/> expects: the menu path, or the title for
		/// entries contributed through <see cref="IFeatureCatalogSource"/> that have no menu path.</summary>
		public string Key;
	}

	/// <summary>
	/// Read + dispatch access to <see cref="FeatureCatalog"/> for assemblies that are not the Feature
	/// Finder window — the agent tool layer, chiefly. Dispatch goes through the entry's own invoke
	/// action, so the catalog stays the single execution path and nothing here becomes a second one.
	/// </summary>
	public static class FeatureCatalogApi
	{
		public static IReadOnlyList<FeatureDescriptor> List(string keyword)
		{
			var entries = FeatureCatalog.Entries;
			bool filtered = !string.IsNullOrWhiteSpace(keyword);
			string needle = filtered ? keyword.Trim().ToLowerInvariant() : null;

			var result = new List<FeatureDescriptor>(filtered ? 16 : entries.Count);
			for (var i = 0; i < entries.Count; i++)
			{
				FeatureEntry entry = entries[i];
				if (filtered && !entry.SearchHaystack.Contains(needle, StringComparison.Ordinal))
					continue;

				result.Add(Describe(entry));
			}

			return result;
		}

		/// <summary>
		/// Runs the catalog entry identified by <paramref name="key"/> (menu path, or title for a
		/// contributed entry). Unknown keys throw with the closest candidates attached: an agent that
		/// guessed a menu path needs the real one back, not a silent no-op.
		/// </summary>
		public static void Invoke(string key)
		{
			if (string.IsNullOrWhiteSpace(key))
				throw new ArgumentException("Feature key is required (menu path or title).", nameof(key));

			var entries = FeatureCatalog.Entries;
			for (var i = 0; i < entries.Count; i++)
			{
				if (string.Equals(entries[i].MenuPath, key, StringComparison.Ordinal))
				{
					entries[i].Invoke();
					return;
				}
			}

			for (var i = 0; i < entries.Count; i++)
			{
				if (string.Equals(entries[i].Title, key, StringComparison.OrdinalIgnoreCase))
				{
					entries[i].Invoke();
					return;
				}
			}

			throw new InvalidOperationException(
				$"No feature in the catalog matches '{key}'. Only catalogued features can be invoked. " +
				$"Closest entries:\n{Suggest(key)}");
		}

		public static void Invalidate() => FeatureCatalog.Invalidate();

		private static FeatureDescriptor Describe(FeatureEntry entry)
		{
			return new FeatureDescriptor
			{
				MenuPath = entry.MenuPath,
				Title = entry.Title,
				Blurb = entry.Blurb,
				Keywords = entry.Keywords,
				Kind = entry.Kind.ToString(),
				Doc = entry.Doc,
				Detail = entry.Detail,
				IsTagged = entry.IsTagged,
				Key = entry.MenuPath.Length > 0 ? entry.MenuPath : entry.Title
			};
		}

		private static string Suggest(string key)
		{
			string needle = key.Trim().ToLowerInvariant();
			var entries = FeatureCatalog.Entries;
			var lines = new List<string>(8);

			for (var i = 0; i < entries.Count && lines.Count < 8; i++)
			{
				FeatureEntry entry = entries[i];
				if (entry.SearchHaystack.Contains(needle, StringComparison.Ordinal))
					lines.Add("  " + (entry.MenuPath.Length > 0 ? entry.MenuPath : entry.Title));
			}

			if (lines.Count == 0)
				return "  (none — list the catalog first)";

			return string.Join("\n", lines);
		}
	}
}
#endif
