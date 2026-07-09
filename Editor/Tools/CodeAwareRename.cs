#if UNITY_EDITOR
using System;
using System.Text;
using System.Text.RegularExpressions;

/// <summary>
/// Code-aware C# source edits shared by script generation / duplication tools: rename the primary
/// class and change (or inject) the file namespace while preserving everything else. Regex-based on
/// the first declaration — sufficient for the single-type scaffold/duplicate scripts this project
/// generates; it is NOT a full semantic rewrite (it will not rewrite constructor names, XML-doc refs,
/// or references in other files).
/// </summary>
public static class CodeAwareRename
{
	/// <summary>Renames the first <c>class {oldClassName}</c> declaration to <c>class {newClassName}</c>. Throws if not found.</summary>
	public static string RenameClass(string sourceText, string oldClassName, string newClassName)
	{
		var classPattern = new Regex(@"\bclass\s+" + Regex.Escape(oldClassName) + @"\b");
		if (!classPattern.IsMatch(sourceText))
		{
			throw new InvalidOperationException($"Could not find 'class {oldClassName}' in source.");
		}

		return classPattern.Replace(sourceText, "class " + newClassName, 1);
	}

	/// <summary>
	/// Changes the file namespace from <paramref name="sourceNamespace"/> to
	/// <paramref name="targetNamespace"/>. If the source had no namespace, wraps the body in the
	/// target namespace (keeping the using header, with a blank line after the usings). Removing a
	/// namespace is not supported.
	/// </summary>
	public static string ApplyNamespaceChange(string sourceText, string sourceNamespace, string targetNamespace)
	{
		if (!string.IsNullOrEmpty(sourceNamespace))
		{
			var namespacePattern = new Regex(@"\bnamespace\s+" + Regex.Escape(sourceNamespace) + @"\b");
			if (!namespacePattern.IsMatch(sourceText))
			{
				throw new InvalidOperationException($"Could not find namespace '{sourceNamespace}' in source script.");
			}

			if (string.IsNullOrEmpty(targetNamespace))
			{
				throw new InvalidOperationException(
					"Removing a namespace from a script is not supported. Leave the namespace unchanged or set a new valid namespace.");
			}

			return namespacePattern.Replace(sourceText, "namespace " + targetNamespace, 1);
		}

		if (string.IsNullOrEmpty(targetNamespace))
		{
			return sourceText;
		}

		var lines = sourceText.Replace("\r\n", "\n").Split('\n');
		var header = new StringBuilder();
		var body = new StringBuilder();
		var inHeader = true;
		for (var i = 0; i < lines.Length; i++)
		{
			var line = lines[i];
			var trimmed = line.TrimStart();
			if (inHeader && (string.IsNullOrWhiteSpace(line) || trimmed.StartsWith("using ", StringComparison.Ordinal)))
			{
				header.AppendLine(line);
				continue;
			}

			inHeader = false;
			body.AppendLine(line);
		}

		var wrapped = new StringBuilder();
		if (header.Length > 0)
		{
			wrapped.Append(header.ToString().TrimEnd('\n', '\r'));
			// Blank line between usings and the namespace declaration.
			wrapped.AppendLine();
			wrapped.AppendLine();
		}

		wrapped.AppendLine("namespace " + targetNamespace);
		wrapped.AppendLine("{");
		var bodyLines = body.ToString().Replace("\r\n", "\n").Split('\n');
		for (var i = 0; i < bodyLines.Length; i++)
		{
			var line = bodyLines[i];
			if (string.IsNullOrEmpty(line) && i == bodyLines.Length - 1)
			{
				continue;
			}

			if (line.Length == 0)
			{
				wrapped.AppendLine();
			}
			else
			{
				wrapped.AppendLine("\t" + line);
			}
		}

		wrapped.AppendLine("}");
		return wrapped.ToString();
	}
}
#endif
