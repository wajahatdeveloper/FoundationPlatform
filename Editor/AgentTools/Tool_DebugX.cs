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
	/// The DebugX Console store exposed to agents. In the editor DebugX.Logger output goes only to this
	/// store (it is not relayed to UnityEngine.Debug), so console-get-logs never sees it.
	/// </summary>
	[BridgeToolType]
	public partial class Tool_DebugX
	{
		public const string DebugXLogsToolId = "debugx-logs";

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
			int maxChars = 400
		)
		{
			if (!Enum.TryParse(minLevel, true, out LogLevel floor))
				throw new ArgumentException($"minLevel '{minLevel}' is not one of: {string.Join(", ", Enum.GetNames(typeof(LogLevel)))}.");
			if (max < 1)
				throw new ArgumentException("max must be at least 1.", nameof(max));

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
					matches.Add(entry);
				}

				result.Matched = matches.Count;
				for (var i = Math.Max(0, matches.Count - max); i < matches.Count; i++)
				{
					var entry = matches[i];
					var message = entry.Message ?? string.Empty;
					if (message.Length > maxChars)
						message = message.Substring(0, maxChars) + "...";
					result.Rows.Add($"#{entry.Id} {entry.Timestamp:HH:mm:ss.f} {entry.Level} [{entry.Channel}] {message}");
				}

				return result;
			});
		}
	}
}
#endif
