#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.ComponentModel;
using AetherNexus.FoundationPlatform.Logging;
using UnityAiBridge;

namespace AetherNexus.FoundationPlatform.AgentTools.Editor
{
	public sealed class DebugXLogsResult
	{
		[Description("Matching rows, oldest first: '#id time LEVEL [Channel] message'.")]
		public List<string> Rows = new();

		public int Matched;

		[Description("Pass as sinceId on the next call to get only newer rows.")]
		public long NextId;
	}

	/// <summary>
	/// The DebugX Console store exposed to agents. DebugX.Logger rows are also copied onto
	/// console-get-logs as `debugx`. This tool is the channel and level filter. DebugX is not
	/// relayed to UnityEngine.Debug.
	/// </summary>
	[BridgeToolType]
	public partial class Tool_DebugX
	{
		public const string DebugXLogsToolId = "debugx-logs";

		/// <summary>DebugX-logger rows for console-get-logs. Unity and compiler mirrors stay out.</summary>
		internal static string[] DebugXOnly(string contains, string regex, int max)
		{
			System.Text.RegularExpressions.Regex? pattern = null;
			if (!string.IsNullOrEmpty(regex))
			{
				try
				{
					pattern = new System.Text.RegularExpressions.Regex(regex, System.Text.RegularExpressions.RegexOptions.IgnoreCase);
				}
				catch (ArgumentException)
				{
					return Array.Empty<string>();
				}
			}

			var rows = new List<string>();
			for (var i = 0; i < ConsoleLogStore.Count; i++)
			{
				var entry = ConsoleLogStore.Get(i);
				if (entry == null || entry.Source != ConsoleSource.DebugX)
					continue;
				if (contains.Length > 0 && (entry.Message == null || entry.Message.IndexOf(contains, StringComparison.OrdinalIgnoreCase) < 0))
					continue;
				if (pattern != null && (entry.Message == null || !pattern.IsMatch(entry.Message)))
					continue;
				rows.Add(Format(entry, 400, 1, 0));
			}
			if (max > 0 && rows.Count > max)
				rows.RemoveRange(0, rows.Count - max);
			return rows.ToArray();
		}

		[BridgeTool(DebugXLogsToolId, Title = "DebugX / Logs", ReadOnly = true)]
		[Description("Rows of the DebugX Console: every DebugX.Logger / DebugX.Builder message plus Unity Debug.Log and " +
			"compiler rows. Use this, not console-get-logs, for game and framework logs (e.g. '[AI:', '[Role:').")]
		public DebugXLogsResult Logs
		(
			[Description("Lowest level kept: Verbose, Debug, Information, Warning, Error, Fatal.")]
			string minLevel = "Information",
			[Description("Exact channel name (case-insensitive), e.g. 'AI'. Empty keeps every channel.")]
			string channel = "",
			[Description("Case-insensitive substring the message must contain.")]
			string contains = "",
			[Description("Only rows with an id above this (from a previous NextId). 0 = no cursor.")]
			long sinceId = 0,
			[Description("Most rows returned; the newest are kept.")]
			int max = 50,
			[Description("Messages longer than this are cut.")]
			int maxChars = 400,
			[Description("Comma-separated case-insensitive substrings; rows whose message contains any are dropped, e.g. 'no subscribers,font asset'.")]
			string exclude = "",
			[Description("Fold rows with the same level, channel and message into one row ('xN', first and last id). Default true.")]
			bool collapse = true
		)
		{
			if (!Enum.TryParse(minLevel, true, out LogLevel floor))
				throw new ArgumentException($"minLevel '{minLevel}' is not one of: {string.Join(", ", Enum.GetNames(typeof(LogLevel)))}.");
			if (max < 1)
				throw new ArgumentException("max must be at least 1.", nameof(max));

			var excludes = new List<string>();
			foreach (var part in exclude.Split(','))
			{
				var trimmed = part.Trim();
				if (trimmed.Length > 0)
					excludes.Add(trimmed);
			}

			return UnityAiBridge.Utils.MainThread.Instance.Run(() =>
			{
				var result = new DebugXLogsResult { NextId = sinceId };
				var matches = new List<ConsoleEntry>();
				for (var i = 0; i < ConsoleLogStore.Count; i++)
				{
					var entry = ConsoleLogStore.Get(i);
					if (entry == null || entry.Source == ConsoleSource.Marker)
						continue;
					if (entry.Id > result.NextId)
						result.NextId = entry.Id;
					if (entry.Id <= sinceId || entry.Level < floor)
						continue;
					if (channel.Length > 0 && !string.Equals(entry.Channel, channel, StringComparison.OrdinalIgnoreCase))
						continue;
					if (contains.Length > 0 && (entry.Message == null || entry.Message.IndexOf(contains, StringComparison.OrdinalIgnoreCase) < 0))
						continue;
					if (excludes.Count > 0 && entry.Message != null && excludes.Exists(x => entry.Message.IndexOf(x, StringComparison.OrdinalIgnoreCase) >= 0))
						continue;
					matches.Add(entry);
				}

				result.Matched = matches.Count;
				if (!collapse)
				{
					for (var i = Math.Max(0, matches.Count - max); i < matches.Count; i++)
						result.Rows.Add(Format(matches[i], maxChars, 1, 0));
					return result;
				}

				// Groups keep the position of their newest row, so the output stays chronological by last sighting.
				var groups = new Dictionary<string, (ConsoleEntry first, ConsoleEntry last, int count)>();
				var order = new List<string>();
				foreach (var entry in matches)
				{
					var key = $"{entry.Level}|{entry.Channel}|{entry.Message}";
					if (groups.TryGetValue(key, out var g))
					{
						groups[key] = (g.first, entry, g.count + 1);
						order.Remove(key);
					}
					else
					{
						groups[key] = (entry, entry, 1);
					}
					order.Add(key);
				}
				for (var i = Math.Max(0, order.Count - max); i < order.Count; i++)
				{
					var g = groups[order[i]];
					result.Rows.Add(Format(g.last, maxChars, g.count, g.first.Id));
				}
				return result;
			});
		}

		static string Format(ConsoleEntry entry, int maxChars, int count, long firstId)
		{
			var message = entry.Message ?? string.Empty;
			if (message.Length > maxChars)
				message = message.Substring(0, maxChars) + "...";
			var repeat = count > 1 ? $" (x{count}, first #{firstId})" : "";
			return $"#{entry.Id} {entry.Timestamp:HH:mm:ss.f} {entry.Level} [{entry.Channel}] {message}{repeat}";
		}
	}

	[UnityEditor.InitializeOnLoad]
	static class DebugXConsoleHook
	{
		static DebugXConsoleHook()
		{
			UnityAiBridge.Editor.BridgeGameHooks.DebugXRows = Tool_DebugX.DebugXOnly;
		}
	}
}
#endif
