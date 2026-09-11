#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace AetherNexus.FoundationPlatform.ComponentMenus.Editor
{
    /// <summary>
    /// The reviewed proposal on disk. The report writes it, a human corrects it, the stamper reads
    /// it back — so the file, not the classifier, is what decides where a component lands.
    /// </summary>
    internal static class ComponentMenuProposalFile
    {
        internal const string Directory = "Temp/ComponentMenus";
        internal const string Path = Directory + "/proposal.tsv";

        private const string Header = "verdict\tmenu\ttype\tpackage\tscript\treason";

        internal static void Write(List<ComponentMenuEntry> entries)
        {
            var builder = new StringBuilder();
            builder.AppendLine("# Component menu proposal. Correct the verdict and menu columns, then run");
            builder.AppendLine("# Tools > Platform > Component Menus > Apply Package... for each package.");
            builder.AppendLine("# verdict: AlreadySet (skipped) | DesignerPlaced (menu required) | Internal (menu must be empty)");
            builder.AppendLine("#");
            builder.AppendLine(Header);

            foreach (var entry in entries)
            {
                builder.AppendLine(string.Join("\t", new[]
                {
                    entry.Verdict.ToString(),
                    entry.ProposedMenu,
                    entry.Type.Name,
                    entry.PackageId,
                    entry.ScriptPath,
                    entry.Reason,
                }));
            }

            System.IO.Directory.CreateDirectory(Directory);
            File.WriteAllText(Path, builder.ToString());
        }

        internal static List<ComponentMenuProposalRow> Read(string packageId)
        {
            if (!File.Exists(Path))
                throw new InvalidOperationException(
                    $"No reviewed proposal at {Path}. Run Tools > Platform > Component Menus > Report Coverage first, review the file, then apply.");

            var rows = new List<ComponentMenuProposalRow>();
            int lineNumber = 0;

            foreach (string line in File.ReadAllLines(Path))
            {
                lineNumber++;
                if (line.Length == 0 || line[0] == '#' || string.Equals(line, Header, StringComparison.Ordinal))
                    continue;

                string[] columns = line.Split('\t');
                if (columns.Length < 5)
                    throw new InvalidOperationException(
                        $"{Path} line {lineNumber} has {columns.Length} tab-separated columns, expected at least 5: '{line}'.");

                if (!Enum.TryParse(columns[0], false, out ComponentMenuVerdict verdict))
                    throw new InvalidOperationException(
                        $"{Path} line {lineNumber} has verdict '{columns[0]}'. Expected {nameof(ComponentMenuVerdict.AlreadySet)}, {nameof(ComponentMenuVerdict.DesignerPlaced)} or {nameof(ComponentMenuVerdict.Internal)}.");

                if (!string.Equals(columns[3], packageId, StringComparison.Ordinal))
                    continue;

                string menu = columns[1];
                if (verdict == ComponentMenuVerdict.DesignerPlaced && string.IsNullOrWhiteSpace(menu))
                    throw new InvalidOperationException(
                        $"{Path} line {lineNumber}: '{columns[2]}' is DesignerPlaced with no menu path. Give it a path, or change the verdict to Internal.");

                if (verdict == ComponentMenuVerdict.Internal && !string.IsNullOrEmpty(menu))
                    throw new InvalidOperationException(
                        $"{Path} line {lineNumber}: '{columns[2]}' is Internal but carries the menu path '{menu}'. Clear the path, or change the verdict to DesignerPlaced.");

                rows.Add(new ComponentMenuProposalRow(verdict, menu, columns[2], columns[3], columns[4]));
            }

            if (rows.Count == 0)
                throw new InvalidOperationException(
                    $"{Path} holds no rows for package '{packageId}'. Check the package id against the file's package column.");

            return rows;
        }
    }
}
#endif
