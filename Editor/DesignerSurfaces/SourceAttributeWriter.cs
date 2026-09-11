#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.IO;
using System.Text.RegularExpressions;

namespace AetherNexus.FoundationPlatform.DesignerSurfaces.Editor
{
    /// <summary>
    /// Writes an attribute onto a type's own declaration. The attribute is the binding — no
    /// <c>.meta</c> editing, so the wiring shows up in a diff and survives reimports. Shared by the
    /// designer icon and component menu passes.
    /// </summary>
    internal static class SourceAttributeWriter
    {
        /// <summary>
        /// Inserts <paramref name="attributeSource"/> directly above the declaration of
        /// <paramref name="typeName"/>. Returns false when the file already contains
        /// <paramref name="skipWhenSourceContains"/>, meaning the attribute is already authored.
        /// </summary>
        internal static bool InsertAboveDeclaration(
            string scriptPath,
            string typeName,
            string attributeSource,
            string skipWhenSourceContains,
            string requiredUsing)
        {
            string absolutePath = Path.GetFullPath(scriptPath);
            string source = File.ReadAllText(absolutePath);
            if (source.Contains(skipWhenSourceContains))
                return false;

            string newline = source.Contains("\r\n") ? "\r\n" : "\n";
            var lines = new List<string>(source.Split(new[] { "\r\n", "\n" }, StringSplitOptions.None));

            int declaration = FindDeclarationLine(lines, typeName);
            if (declaration < 0)
                throw new InvalidOperationException(
                    $"Could not find the declaration of '{typeName}' in {scriptPath}. '{attributeSource}' was not written.");

            string indent = LeadingWhitespace(lines[declaration]);
            lines.Insert(declaration, $"{indent}{attributeSource}");

            EnsureUsing(lines, requiredUsing);

            File.WriteAllText(absolutePath, string.Join(newline, lines));
            return true;
        }

        /// <summary>Rewrites <c>menuName = "..."</c> on an existing <c>[CreateAssetMenu]</c>. Returns false when already at <paramref name="menuName"/>.</summary>
        internal static bool ReplaceCreateAssetMenuName(string scriptPath, string typeName, string menuName)
        {
            string absolutePath = Path.GetFullPath(scriptPath);
            string source = File.ReadAllText(absolutePath);
            string newline = source.Contains("\r\n") ? "\r\n" : "\n";
            var lines = new List<string>(source.Split(new[] { "\r\n", "\n" }, StringSplitOptions.None));

            int declaration = FindDeclarationLine(lines, typeName);
            if (declaration < 0)
                throw new InvalidOperationException(
                    $"Could not find the declaration of '{typeName}' in {scriptPath}. menuName was not rewritten.");

            for (int i = declaration - 1; i >= 0; i--)
            {
                string trimmed = lines[i].TrimStart();
                if (trimmed.Length == 0)
                    continue;
                if (trimmed.StartsWith("//", StringComparison.Ordinal) ||
                    trimmed.StartsWith("*", StringComparison.Ordinal) ||
                    trimmed.StartsWith("/*", StringComparison.Ordinal))
                    continue;
                if (!trimmed.StartsWith("[", StringComparison.Ordinal))
                    break;

                if (trimmed.IndexOf("CreateAssetMenu", StringComparison.Ordinal) < 0)
                    continue;

                var match = Regex.Match(lines[i], @"menuName\s*=\s*""([^""]*)""");
                if (!match.Success)
                    throw new InvalidOperationException(
                        $"'{typeName}' in {scriptPath} has [CreateAssetMenu] without menuName. Refusing to guess.");

                if (string.Equals(match.Groups[1].Value, menuName, StringComparison.Ordinal))
                    return false;

                lines[i] = lines[i].Substring(0, match.Groups[1].Index) + menuName + lines[i].Substring(match.Groups[1].Index + match.Groups[1].Length);
                File.WriteAllText(absolutePath, string.Join(newline, lines));
                return true;
            }

            throw new InvalidOperationException(
                $"Could not find [CreateAssetMenu] above '{typeName}' in {scriptPath}.");
        }

        private static int FindDeclarationLine(List<string> lines, string typeName)
        {
            var pattern = new Regex($@"\bclass\s+{Regex.Escape(typeName)}\b");

            for (int i = 0; i < lines.Count; i++)
            {
                string trimmed = lines[i].TrimStart();
                if (trimmed.StartsWith("//", StringComparison.Ordinal) ||
                    trimmed.StartsWith("*", StringComparison.Ordinal) ||
                    trimmed.StartsWith("/*", StringComparison.Ordinal))
                    continue;

                if (pattern.IsMatch(lines[i]))
                    return i;
            }

            return -1;
        }

        private static void EnsureUsing(List<string> lines, string usingStatement)
        {
            int lastUsing = -1;
            int namespaceLine = -1;

            for (int i = 0; i < lines.Count; i++)
            {
                string trimmed = lines[i].TrimStart();
                if (trimmed.StartsWith("using ", StringComparison.Ordinal))
                {
                    if (trimmed.StartsWith(usingStatement, StringComparison.Ordinal))
                        return;

                    lastUsing = i;
                    continue;
                }

                if (trimmed.StartsWith("namespace ", StringComparison.Ordinal))
                {
                    namespaceLine = i;
                    break;
                }
            }

            if (lastUsing >= 0)
            {
                lines.Insert(lastUsing + 1, usingStatement);
                return;
            }

            // A file that leans on global usings has no block to extend, so the statement opens its
            // own one above the namespace.
            int insertAt = namespaceLine < 0 ? 0 : namespaceLine;
            lines.Insert(insertAt, string.Empty);
            lines.Insert(insertAt, usingStatement);
        }

        private static string LeadingWhitespace(string line)
        {
            int i = 0;
            while (i < line.Length && char.IsWhiteSpace(line[i]))
                i++;

            return line.Substring(0, i);
        }
    }
}
#endif
