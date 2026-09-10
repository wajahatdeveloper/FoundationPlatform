#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.IO;
using System.Text.RegularExpressions;

namespace AetherNexus.FoundationPlatform.DesignerIcons.Editor
{
    /// <summary>
    /// Stamps <c>[Icon("Packages/&lt;pkg&gt;/Editor/Icons/&lt;Type&gt;.png")]</c> onto the type's own
    /// declaration. The attribute is the binding — no <c>.meta</c> editing, so the wiring shows up
    /// in a diff and survives reimports.
    /// </summary>
    internal static class DesignerIconAttributeWriter
    {
        internal static bool Stamp(DesignerIconEntry entry)
        {
            string absolutePath = Path.GetFullPath(entry.ScriptPath);
            string source = File.ReadAllText(absolutePath);
            if (source.Contains("[Icon("))
                return false;

            string newline = source.Contains("\r\n") ? "\r\n" : "\n";
            var lines = new List<string>(source.Split(new[] { "\r\n", "\n" }, StringSplitOptions.None));

            int declaration = FindDeclarationLine(lines, entry.Type.Name);
            if (declaration < 0)
                throw new InvalidOperationException(
                    $"Could not find the declaration of '{entry.Type.Name}' in {entry.ScriptPath}. The icon attribute was not written.");

            string indent = LeadingWhitespace(lines[declaration]);
            lines.Insert(declaration, $"{indent}[Icon(\"{entry.IconAssetPath}\")]");

            EnsureUnityEngineUsing(lines);

            File.WriteAllText(absolutePath, string.Join(newline, lines));
            return true;
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

        private static void EnsureUnityEngineUsing(List<string> lines)
        {
            int lastUsing = -1;
            for (int i = 0; i < lines.Count; i++)
            {
                string trimmed = lines[i].TrimStart();
                if (trimmed.StartsWith("using ", StringComparison.Ordinal))
                {
                    if (trimmed.StartsWith("using UnityEngine;", StringComparison.Ordinal))
                        return;

                    lastUsing = i;
                    continue;
                }

                if (trimmed.StartsWith("namespace ", StringComparison.Ordinal))
                    break;
            }

            if (lastUsing < 0)
                throw new InvalidOperationException(
                    "Cannot place 'using UnityEngine;' — the file has no leading using block. Add the using by hand and re-run.");

            lines.Insert(lastUsing + 1, "using UnityEngine;");
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
