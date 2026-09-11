#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using AetherNexus.FoundationPlatform.DesignerSurfaces.Editor;
using UnityEditor;

namespace AetherNexus.FoundationPlatform.CreateMenus.Editor
{
    using DebugX = DebugX.DebugX;

    /// <summary>
    /// Report, review, stamp for <c>[CreateAssetMenu]</c> paths. The TSV is the source of truth for
    /// a rehome; the classifier only proposes.
    /// </summary>
    public static class CreateMenuPipeline
    {
        internal const string ProposalDirectory = "Temp/CreateMenus";
        internal const string ProposalPath = ProposalDirectory + "/proposal.tsv";
        private const string Header = "verdict\tcurrent\tproposed\ttype\tpackage\tscript\treason";

        public static string ReportCoverage()
        {
            var entries = CreateMenuCatalog.Build();
            WriteProposal(entries);

            int authored = 0;
            int rehome = 0;
            int authoring = 0;
            int unclassified = 0;
            foreach (var entry in entries)
            {
                switch (entry.Verdict)
                {
                    case CreateMenuVerdict.AlreadySet:
                        if (!string.Equals(entry.ExistingMenu, entry.ProposedMenu, StringComparison.Ordinal))
                            rehome++;
                        else
                            authored++;
                        break;
                    case CreateMenuVerdict.Authoring: authoring++; break;
                    case CreateMenuVerdict.Unclassified: unclassified++; break;
                }
            }

            DebugX.Info(
                "Create menus: {Total} ScriptableObjects — {Authored} already home, {Rehome} proposed rehomes, {Authoring} missing a create entry, {Unclassified} unclassified. Review {Path} before applying.",
                entries.Count, authored, rehome, authoring, unclassified, ProposalPath);

            return BuildTable(entries);
        }

        public static string PreviewPackage(string packageId)
        {
            var rows = ReadProposal(packageId);
            var builder = new StringBuilder();
            builder.AppendLine($"Create menu dry run — {packageId} ({rows.Count} reviewed rows)");
            builder.AppendLine();
            int pending = 0;
            foreach (var row in rows)
            {
                if (string.Equals(row.Current, row.Proposed, StringComparison.Ordinal))
                    continue;
                pending++;
                builder.AppendLine($"{row.TypeName,-46} {row.Current} -> {row.Proposed}");
            }

            builder.AppendLine();
            builder.AppendLine($"{pending} of {rows.Count} rows would be rewritten.");
            return builder.ToString();
        }

        public static string ApplyPackage(string packageId)
        {
            var rows = ReadProposal(packageId);
            var builder = new StringBuilder();
            int stamped = 0;
            int skipped = 0;

            foreach (var row in rows)
            {
                if (string.IsNullOrEmpty(row.Proposed) || string.Equals(row.Current, row.Proposed, StringComparison.Ordinal))
                {
                    skipped++;
                    continue;
                }

                bool changed = SourceAttributeWriter.ReplaceCreateAssetMenuName(row.ScriptPath, row.TypeName, row.Proposed);
                if (changed)
                    stamped++;
                else
                    skipped++;

                builder.AppendLine($"{(changed ? "stamped" : "already")}  {row.TypeName,-46} {row.Proposed}");
            }

            AssetDatabase.Refresh();
            DebugX.Info("Create menus: rewrote {Stamped} menuName values in {Package}, skipped {Skipped}.", stamped, packageId, skipped);
            return builder.ToString();
        }

        public static string ReportMissing()
        {
            var entries = CreateMenuCatalog.Build();
            var authoring = new List<CreateMenuEntry>();
            var unclassified = new List<CreateMenuEntry>();
            int authored = 0;
            int excluded = 0;

            foreach (var entry in entries)
            {
                switch (entry.Verdict)
                {
                    case CreateMenuVerdict.AlreadySet: authored++; break;
                    case CreateMenuVerdict.Authoring: authoring.Add(entry); break;
                    case CreateMenuVerdict.Unclassified: unclassified.Add(entry); break;
                    default: excluded++; break;
                }
            }

            if (authoring.Count == 0 && unclassified.Count == 0)
            {
                DebugX.Info(
                    "Create menus: every first-party authoring asset is reachable from Assets > Create ({Authored} with an entry, {Excluded} config sections or generator-owned singletons).",
                    authored, excluded);
                return string.Empty;
            }

            var builder = new StringBuilder();
            if (authoring.Count > 0)
            {
                builder.AppendLine("Authoring assets with no Assets > Create entry:");
                AppendMissing(builder, authoring);
            }

            if (unclassified.Count > 0)
            {
                builder.AppendLine();
                builder.AppendLine("Unclassified — decide whether these are authored by hand or owned by a tool:");
                AppendMissing(builder, unclassified);
            }

            string table = builder.ToString();
            DebugX.Warning(
                "Create menus: {Authoring} authoring assets and {Unclassified} unclassified types out of {Total} first-party ScriptableObjects have no [CreateAssetMenu].\n{Table}",
                authoring.Count, unclassified.Count, entries.Count, table);

            return table;
        }

        private static void WriteProposal(List<CreateMenuEntry> entries)
        {
            var builder = new StringBuilder();
            builder.AppendLine("# Create menu proposal. Correct the proposed column, then run");
            builder.AppendLine("# Tools > Platform > Create Menus > Apply Package... for each package.");
            builder.AppendLine("#");
            builder.AppendLine(Header);

            foreach (var entry in entries)
            {
                builder.AppendLine(string.Join("\t", new[]
                {
                    entry.Verdict.ToString(),
                    entry.ExistingMenu ?? string.Empty,
                    entry.ProposedMenu ?? string.Empty,
                    entry.Type.Name,
                    entry.PackageId,
                    entry.ScriptPath,
                    entry.Reason,
                }));
            }

            Directory.CreateDirectory(ProposalDirectory);
            File.WriteAllText(ProposalPath, builder.ToString());
        }

        private static List<ProposalRow> ReadProposal(string packageId)
        {
            if (!File.Exists(ProposalPath))
                throw new InvalidOperationException(
                    $"No reviewed proposal at {ProposalPath}. Run Tools > Platform > Create Menus > Report Coverage first.");

            var rows = new List<ProposalRow>();
            int lineNumber = 0;
            foreach (string line in File.ReadAllLines(ProposalPath))
            {
                lineNumber++;
                if (line.Length == 0 || line[0] == '#' || string.Equals(line, Header, StringComparison.Ordinal))
                    continue;

                string[] columns = line.Split('\t');
                if (columns.Length < 6)
                    throw new InvalidOperationException(
                        $"{ProposalPath} line {lineNumber} has {columns.Length} columns, expected at least 6.");

                if (!string.Equals(columns[4], packageId, StringComparison.Ordinal))
                    continue;

                if (columns[0] != nameof(CreateMenuVerdict.AlreadySet))
                    continue;

                rows.Add(new ProposalRow(columns[1], columns[2], columns[3], columns[4], columns[5]));
            }

            if (rows.Count == 0)
                throw new InvalidOperationException(
                    $"{ProposalPath} holds no AlreadySet rows for package '{packageId}'.");

            return rows;
        }

        private static string BuildTable(List<CreateMenuEntry> entries)
        {
            var builder = new StringBuilder();
            string currentPackage = null;
            foreach (var entry in entries)
            {
                if (entry.PackageId != currentPackage)
                {
                    currentPackage = entry.PackageId;
                    builder.AppendLine();
                    builder.AppendLine($"== {currentPackage}");
                }

                string proposed = entry.ProposedMenu ?? string.Empty;
                string current = entry.ExistingMenu ?? "(none)";
                string arrow = string.Equals(current, proposed, StringComparison.Ordinal) ? current : $"{current} -> {proposed}";
                builder.AppendLine($"{entry.Verdict.ToString().PadRight(14)} {entry.Type.Name.PadRight(46)} {arrow}");
            }

            return builder.ToString();
        }

        private static void AppendMissing(StringBuilder builder, List<CreateMenuEntry> entries)
        {
            string currentPackage = null;
            foreach (var entry in entries)
            {
                if (entry.PackageId != currentPackage)
                {
                    currentPackage = entry.PackageId;
                    builder.AppendLine();
                    builder.AppendLine($"== {currentPackage}");
                }

                builder.AppendLine(string.Join(" | ", new[]
                {
                    entry.Verdict.ToString().PadRight(14),
                    entry.Type.Name.PadRight(46),
                    entry.Reason,
                }));
            }
        }

        private readonly struct ProposalRow
        {
            public ProposalRow(string current, string proposed, string typeName, string packageId, string scriptPath)
            {
                Current = current;
                Proposed = proposed;
                TypeName = typeName;
                PackageId = packageId;
                ScriptPath = scriptPath;
            }

            public string Current { get; }
            public string Proposed { get; }
            public string TypeName { get; }
            public string PackageId { get; }
            public string ScriptPath { get; }
        }
    }
}
#endif
