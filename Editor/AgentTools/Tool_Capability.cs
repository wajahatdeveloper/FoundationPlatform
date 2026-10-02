#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.ComponentModel;
using AetherNexus.FoundationPlatform.FeatureFinder.Editor;
using UnityAiBridge;
using UnityEngine;

namespace AetherNexus.FoundationPlatform.AgentTools.Editor
{
	public sealed class CapabilityLogLine
	{
		public string Type;
		public string Message;
	}

	public sealed class CapabilityInvokeResult
	{
		public string Key;
		public string MenuPath;
		public string Title;

		/// <summary>Everything Unity logged while the feature ran, so the caller sees the outcome of a
		/// void editor action without a second console round-trip.</summary>
		public List<CapabilityLogLine> Logs = new();
	}

	/// <summary>
	/// The project's designer feature index, exposed to coding agents. Every entry here is a menu
	/// item a designer can already reach, so an agent that lists and invokes capabilities drives the
	/// same authoring paths a human would instead of reimplementing them.
	/// </summary>
	[BridgeToolType]
	public partial class Tool_Capability
	{
		public const string CapabilityListToolId = "platform-capability-list";

		[BridgeTool(CapabilityListToolId, Title = "Capability / List", Tier = BridgeToolTier.Direct, ReadOnly = true)]
		[Description("Search the project's designer features (owned menu items) by keyword before wiring anything " +
			"by hand. Run one with platform-capability-invoke using its 'key'. Agent tools are listed by unity-run list.")]
		public IReadOnlyList<FeatureDescriptor> List
		(
			[Description("Case-insensitive substring matched against title, keywords, menu path and detail. Empty returns the whole catalog.")]
			string keyword = "",
			[Description("Only return entries carrying a [DesignerFeature] tag (title + blurb + keywords). Untagged entries are menu-path only.")]
			bool taggedOnly = false
		)
		{
			return UnityAiBridge.Utils.MainThread.Instance.Run(() =>
			{
				IReadOnlyList<FeatureDescriptor> all = FeatureCatalogApi.List(keyword);
				if (!taggedOnly)
					return all;

				var tagged = new List<FeatureDescriptor>(all.Count);
				for (var i = 0; i < all.Count; i++)
				{
					if (all[i].IsTagged)
						tagged.Add(all[i]);
				}
				return (IReadOnlyList<FeatureDescriptor>)tagged;
			});
		}

		public const string CapabilityInvokeToolId = "platform-capability-invoke";

		[BridgeTool(CapabilityInvokeToolId, Title = "Capability / Invoke", Tier = BridgeToolTier.Direct)]
		[Description("Run one designer feature by the 'key' from platform-capability-list; returns the logs it wrote. " +
			"Features that open a window or dialog block on user input.")]
		public CapabilityInvokeResult Invoke
		(
			[Description("Menu path exactly as returned by platform-capability-list, e.g. 'Tools/Platform/Rebuild All Generated Registries'.")]
			string key
		)
		{
			return UnityAiBridge.Utils.MainThread.Instance.Run(() =>
			{
				var result = new CapabilityInvokeResult { Key = key };

				IReadOnlyList<FeatureDescriptor> matches = FeatureCatalogApi.List(key);
				for (var i = 0; i < matches.Count; i++)
				{
					if (string.Equals(matches[i].Key, key, StringComparison.OrdinalIgnoreCase))
					{
						result.MenuPath = matches[i].MenuPath;
						result.Title = matches[i].Title;
						break;
					}
				}

				Application.LogCallback capture = (message, stackTrace, type) =>
					result.Logs.Add(new CapabilityLogLine { Type = type.ToString(), Message = message });

				Application.logMessageReceived += capture;
				try
				{
					FeatureCatalogApi.Invoke(key);
				}
				finally
				{
					Application.logMessageReceived -= capture;
				}

				return result;
			});
		}
	}
}
#endif
