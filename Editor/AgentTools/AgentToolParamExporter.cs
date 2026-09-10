#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Globalization;
using System.IO;
using System.Text;
using System.Text.Json;
using AetherNexus.FoundationPlatform.Utilities.Menus;
using UnityAiBridge;
using UnityAiBridge.Editor;
using UnityEditor;
using UnityEngine;

namespace AetherNexus.FoundationPlatform.AgentTools.Editor
{
	public sealed class ParamExportResult
	{
		public string OutputDirectory;
		public int Written;
		public int Unchanged;
		public int Pruned;
		public List<string> PrunedFiles = new();
	}

	/// <summary>
	/// Writes one <c>params/{tool}.json</c> per registered Bridge tool so the MCP server's
	/// <c>tools/list</c> matches what the Editor actually exposes. The MCP server builds its schema
	/// from those files alone, so without this step a new <c>[BridgeTool]</c> exists in Unity but is
	/// invisible to every agent.
	/// </summary>
	public static class AgentToolParamExporter
	{
		private const string DefaultRelativeOutput = ".claude/skills/unity-bridge/params";

		/// <summary>Ids owned by the vendored bridge. A package tool taking one of these would shadow a
		/// builtin in the registry dictionary, and which one wins depends on assembly scan order.</summary>
		private static readonly string[] ReservedPrefixes =
		{
			"gameobject-", "scene-", "assets-", "editor-", "object-", "script-", "console-",
			"package-", "profiler-", "reflection-", "runtime-", "screenshot-", "tests-", "lightprobe-"
		};

		private static readonly string[] BridgeAssemblyPrefixes = { "AiBridge.Unity" };

		[MenuItem(MenuPaths.Platform.AgentToolsExportParams, false, MenuPriorities.Platform + 40)]
		[DesignerFeature(
			"Export Agent Tool Schemas",
			"Regenerates the MCP parameter files so coding agents see every Bridge tool this project exposes.",
			"agent ai mcp bridge tools schema params export claude cursor",
			DesignerFeatureKind.Generator,
			"AGENTS.md")]
		public static void ExportMenu()
		{
			ParamExportResult result = Export(null);
			Debug.Log($"[AgentTools] Exported MCP params to {result.OutputDirectory} — " +
				$"{result.Written} written, {result.Unchanged} unchanged, {result.Pruned} pruned.");
		}

		/// <param name="outputDirectory">Absolute or project-relative folder, or null for
		/// <c>.claude/skills/unity-bridge/params</c> beside the project.</param>
		public static ParamExportResult Export(string outputDirectory)
		{
			BridgeToolRegistry registry = BridgePlugin.Registry;
			if (registry == null)
				throw new InvalidOperationException(
					"BridgePlugin.Registry is null — the Unity AI Bridge did not initialize. Nothing to export.");

			string directory = ResolveOutputDirectory(outputDirectory);
			Directory.CreateDirectory(directory);

			var result = new ParamExportResult { OutputDirectory = directory };
			var exported = new HashSet<string>(StringComparer.Ordinal);
			SchemaBaseline baseline = SchemaBaseline.Load(directory);

			foreach (ToolEntry tool in registry.GetAllTools())
			{
				GuardReservedId(tool);

				string path = Path.Combine(directory, tool.Name + ".json");
				string json = BuildJson(tool, ExistingSchema.Load(path), baseline.For(tool.Name));
				exported.Add(tool.Name);

				if (File.Exists(path) && string.Equals(File.ReadAllText(path), json, StringComparison.Ordinal))
				{
					result.Unchanged++;
					continue;
				}

				File.WriteAllText(path, json, new UTF8Encoding(false));
				result.Written++;
			}

			Prune(directory, exported, result);
			baseline.Save(directory);
			return result;
		}

		/// <summary>
		/// Removes schema files for tools that no longer exist. Files starting with '_' are hand-authored
		/// hints the MCP loader deliberately skips, so they are left alone.
		/// </summary>
		private static void Prune(string directory, HashSet<string> exported, ParamExportResult result)
		{
			foreach (string path in Directory.GetFiles(directory, "*.json"))
			{
				string id = Path.GetFileNameWithoutExtension(path);
				if (id.StartsWith("_", StringComparison.Ordinal) || exported.Contains(id))
					continue;

				File.Delete(path);
				result.Pruned++;
				result.PrunedFiles.Add(id);
			}
		}

		private static void GuardReservedId(ToolEntry tool)
		{
			string assembly = tool.DeclaringType.Assembly.GetName().Name ?? "";
			for (var i = 0; i < BridgeAssemblyPrefixes.Length; i++)
			{
				if (assembly.StartsWith(BridgeAssemblyPrefixes[i], StringComparison.Ordinal))
					return;
			}

			for (var i = 0; i < ReservedPrefixes.Length; i++)
			{
				if (!tool.Name.StartsWith(ReservedPrefixes[i], StringComparison.Ordinal))
					continue;

				throw new InvalidOperationException(
					$"Tool '{tool.Name}' in assembly '{assembly}' ({tool.DeclaringType.FullName}) uses the reserved " +
					$"prefix '{ReservedPrefixes[i]}' owned by the Unity AI Bridge. Package tools must use their own " +
					"area prefix (core-, platform-, character-, gas-, liveops-).");
			}
		}

		private static string ResolveOutputDirectory(string outputDirectory)
		{
			string projectRoot = Directory.GetParent(Application.dataPath)!.FullName;
			if (string.IsNullOrWhiteSpace(outputDirectory))
				return Path.GetFullPath(Path.Combine(projectRoot, DefaultRelativeOutput));

			return Path.IsPathRooted(outputDirectory)
				? Path.GetFullPath(outputDirectory)
				: Path.GetFullPath(Path.Combine(projectRoot, outputDirectory));
		}

		// The MCP loader reads a flat schema where every scalar is a string ("required": "true"), so this
		// writes that shape directly instead of reusing ToolEntry.InputSchema's typed JSON.
		private static string BuildJson(ToolEntry tool, ExistingSchema existing, ToolBaseline baseline)
		{
			var sb = new StringBuilder(1024);
			sb.Append("{\n");
			sb.Append("  \"name\": ").Append(Quote(tool.Name)).Append(",\n");
			sb.Append("  \"title\": ").Append(Quote(tool.Title)).Append(",\n");
			sb.Append("  \"description\": ").Append(Quote(existing.Curated(null, tool.Description, baseline))).Append(",\n");
			sb.Append("  \"parameters\": [");

			for (var i = 0; i < tool.Parameters.Count; i++)
			{
				ToolParameter parameter = tool.Parameters[i];
				sb.Append(i == 0 ? "\n" : ",\n");
				sb.Append("    {\n");
				sb.Append("      \"name\": ").Append(Quote(parameter.Name)).Append(",\n");
				sb.Append("      \"type\": ").Append(Quote(parameter.Type)).Append(",\n");
				sb.Append("      \"required\": ").Append(Quote(parameter.IsRequired ? "true" : "false"));

				string description = existing.Curated(parameter.Name, parameter.Description, baseline);
				if (!string.IsNullOrEmpty(description))
					sb.Append(",\n      \"description\": ").Append(Quote(description));

				string enumValues = EnumValuesOf(parameter);
				if (enumValues != null)
					sb.Append(",\n      \"enum\": ").Append(Quote(enumValues));

				if (parameter.HasDefault && parameter.DefaultValue != null)
					sb.Append(",\n      \"default\": ").Append(Quote(FormatDefault(parameter.DefaultValue)));

				sb.Append("\n    }");
			}

			sb.Append(tool.Parameters.Count > 0 ? "\n  ]" : "]");
			existing.AppendExtraKeys(sb);
			sb.Append("\n}\n");
			return sb.ToString();
		}

		/// <summary>
		/// What the exporter itself wrote last time, per tool and parameter, kept in
		/// <c>_generated.json</c> (the MCP loader skips underscore files). It is the third side of the
		/// merge: a description that still equals its baseline is untouched generated text and gets
		/// overwritten, while anything else was curated by hand and survives. Without it the exporter
		/// can only choose between deleting curated agent guidance and never propagating code edits.
		/// </summary>
		private sealed class SchemaBaseline
		{
			private const string FileName = "_generated.json";

			private readonly Dictionary<string, Dictionary<string, string>> _previous = new(StringComparer.Ordinal);
			private readonly Dictionary<string, Dictionary<string, string>> _current = new(StringComparer.Ordinal);

			public static SchemaBaseline Load(string directory)
			{
				var baseline = new SchemaBaseline();
				string path = Path.Combine(directory, FileName);
				if (!File.Exists(path))
					return baseline;

				using JsonDocument document = JsonDocument.Parse(File.ReadAllText(path));
				foreach (JsonProperty tool in document.RootElement.EnumerateObject())
				{
					var slots = new Dictionary<string, string>(StringComparer.Ordinal);
					foreach (JsonProperty slot in tool.Value.EnumerateObject())
						slots[slot.Name] = slot.Value.GetString() ?? "";

					baseline._previous[tool.Name] = slots;
				}

				return baseline;
			}

			public ToolBaseline For(string toolName)
			{
				var current = new Dictionary<string, string>(StringComparer.Ordinal);
				_current[toolName] = current;
				_previous.TryGetValue(toolName, out Dictionary<string, string> previous);
				return new ToolBaseline(previous, current);
			}

			public void Save(string directory)
			{
				var sb = new StringBuilder(4096);
				sb.Append("{");

				var firstTool = true;
				foreach (KeyValuePair<string, Dictionary<string, string>> tool in _current)
				{
					sb.Append(firstTool ? "\n" : ",\n");
					firstTool = false;
					sb.Append("  ").Append(Quote(tool.Key)).Append(": {");

					var firstSlot = true;
					foreach (KeyValuePair<string, string> slot in tool.Value)
					{
						sb.Append(firstSlot ? "\n" : ",\n");
						firstSlot = false;
						sb.Append("    ").Append(Quote(slot.Key)).Append(": ").Append(Quote(slot.Value));
					}

					sb.Append(tool.Value.Count > 0 ? "\n  }" : "}");
				}

				sb.Append(_current.Count > 0 ? "\n}\n" : "}\n");
				File.WriteAllText(Path.Combine(directory, FileName), sb.ToString(), new UTF8Encoding(false));
			}
		}

		private readonly struct ToolBaseline
		{
			private readonly Dictionary<string, string> _previous;
			private readonly Dictionary<string, string> _current;

			public ToolBaseline(Dictionary<string, string> previous, Dictionary<string, string> current)
			{
				_previous = previous;
				_current = current;
			}

			public void Record(string slot, string generated) => _current[slot] = generated;

			public bool WasGenerated(string slot, string existing)
			{
				if (_previous == null || !_previous.TryGetValue(slot, out string previous))
					return false;

				return string.Equals(previous, existing, StringComparison.Ordinal);
			}
		}

		/// <summary>
		/// What a previous schema file said, so regeneration never deletes hand-added agent guidance.
		/// These files carry curation C# cannot express — payload shapes, ENUM hints, whole extra keys
		/// such as <c>hints</c> — written against tools this repo vendors rather than owns.
		/// </summary>
		private sealed class ExistingSchema
		{
			private static readonly ExistingSchema None = new();

			private readonly Dictionary<string, string> _descriptions = new(StringComparer.Ordinal);
			private readonly List<KeyValuePair<string, string>> _extraKeys = new();

			private const string ToolDescriptionSlot = "\0tool";

			public static ExistingSchema Load(string path)
			{
				if (!File.Exists(path))
					return None;

				string text = File.ReadAllText(path);
				JsonDocument document;
				try
				{
					document = JsonDocument.Parse(text);
				}
				catch (JsonException e)
				{
					throw new InvalidOperationException(
						$"Existing schema '{path}' is not valid JSON, so curated content cannot be preserved: {e.Message}", e);
				}

				using (document)
				{
					var schema = new ExistingSchema();
					foreach (JsonProperty property in document.RootElement.EnumerateObject())
					{
						switch (property.Name)
						{
							case "name":
							case "title":
								break;
							case "description":
								schema._descriptions[ToolDescriptionSlot] = property.Value.GetString() ?? "";
								break;
							case "parameters":
								schema.ReadParameters(property.Value);
								break;
							default:
								schema._extraKeys.Add(new KeyValuePair<string, string>(
									property.Name, property.Value.GetRawText()));
								break;
						}
					}
					return schema;
				}
			}

			private void ReadParameters(JsonElement parameters)
			{
				if (parameters.ValueKind != JsonValueKind.Array)
					return;

				foreach (JsonElement parameter in parameters.EnumerateArray())
				{
					if (!parameter.TryGetProperty("name", out JsonElement name) ||
					    !parameter.TryGetProperty("description", out JsonElement description))
						continue;

					string key = name.GetString();
					if (!string.IsNullOrEmpty(key))
						_descriptions[key] = description.GetString() ?? "";
				}
			}

			/// <param name="parameterName">Null for the tool's own description.</param>
			public string Curated(string parameterName, string generated, ToolBaseline baseline)
			{
				string slot = parameterName ?? ToolDescriptionSlot;
				baseline.Record(slot, generated);

				if (!_descriptions.TryGetValue(slot, out string existing) || string.IsNullOrEmpty(existing))
					return generated;
				if (string.Equals(existing, generated, StringComparison.Ordinal))
					return generated;

				// Unchanged since the last export means nobody curated it, so the code wins.
				return baseline.WasGenerated(slot, existing) ? generated : existing;
			}

			public void AppendExtraKeys(StringBuilder sb)
			{
				for (var i = 0; i < _extraKeys.Count; i++)
					sb.Append(",\n  ").Append(Quote(_extraKeys[i].Key)).Append(": ").Append(_extraKeys[i].Value);
			}
		}

		private static string EnumValuesOf(ToolParameter parameter)
		{
			Type type = Nullable.GetUnderlyingType(parameter.ParameterType) ?? parameter.ParameterType;
			if (!type.IsEnum)
				return null;

			return string.Join(",", Enum.GetNames(type));
		}

		private static string FormatDefault(object value)
		{
			switch (value)
			{
				case bool b: return b ? "true" : "false";
				case float f: return f.ToString(CultureInfo.InvariantCulture);
				case double d: return d.ToString(CultureInfo.InvariantCulture);
				case decimal m: return m.ToString(CultureInfo.InvariantCulture);
				case Enum e: return e.ToString();
				case IFormattable formattable: return formattable.ToString(null, CultureInfo.InvariantCulture);
				default: return value.ToString();
			}
		}

		private static string Quote(string value)
		{
			var sb = new StringBuilder((value?.Length ?? 0) + 2);
			sb.Append('"');
			for (var i = 0; i < (value?.Length ?? 0); i++)
			{
				char c = value[i];
				switch (c)
				{
					case '"': sb.Append("\\\""); break;
					case '\\': sb.Append("\\\\"); break;
					case '\n': sb.Append("\\n"); break;
					case '\r': sb.Append("\\r"); break;
					case '\t': sb.Append("\\t"); break;
					default:
						if (c < 0x20)
							sb.Append("\\u").Append(((int)c).ToString("x4", CultureInfo.InvariantCulture));
						else
							sb.Append(c);
						break;
				}
			}
			sb.Append('"');
			return sb.ToString();
		}
	}

	/// <summary>Exposes the exporter to agents so a tool added mid-session can be published without a
	/// human clicking the menu entry.</summary>
	[BridgeToolType]
	public partial class Tool_AgentTools
	{
		public const string ExportParamsToolId = "platform-agenttools-export-params";

		[BridgeTool(ExportParamsToolId, Title = "Agent Tools / Export MCP Params")]
		[Description("Regenerates the MCP parameter schema files from the live Bridge tool registry. " +
			"Run after adding or changing a [BridgeTool]; the MCP server must then be restarted to pick up " +
			"new tools, since it reads the schema files at startup.")]
		public ParamExportResult ExportParams
		(
			[Description("Output folder, absolute or project-relative. Empty uses .claude/skills/unity-bridge/params.")]
			string outputDirectory = ""
		)
		{
			return UnityAiBridge.Utils.MainThread.Instance.Run(() => AgentToolParamExporter.Export(outputDirectory));
		}
	}
}
#endif
