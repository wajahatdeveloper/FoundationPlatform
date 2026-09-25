#if UNITY_EDITOR
using System.Collections.Generic;
using System.IO;
using System.Text;
using AetherNexus.FoundationPlatform.Utilities.Menus;
using UnityEditor;

namespace AetherNexus.FoundationPlatform.DesignerIcons.Editor
{

    /// <summary>
    /// Batch entry points for the designer icon pass: report what is covered, generate a package's
    /// icons, stamp the attributes. Split from the menu items so the same calls are reachable from
    /// an editor script or an agent tool without going through the menu.
    /// </summary>
    public static class DesignerIconPipeline
    {
        private const string ReportDirectory = "Temp/DesignerIcons";
        private const string ReportPath = ReportDirectory + "/icon-coverage.txt";
        private const string AuditPath = ReportDirectory + "/symbol-audit.txt";

        /// <summary>Writes the full designer-facing type table to <c>Temp/DesignerIcons</c> and logs a per-package summary. Mutates nothing.</summary>
        public static string ReportCoverage()
        {
            var entries = DesignerIconCatalog.Build();
            string report = BuildReport(entries);

            Directory.CreateDirectory(ReportDirectory);
            File.WriteAllText(ReportPath, report);

            int covered = 0;
            foreach (var entry in entries)
            {
                if (entry.HasIconAttribute) covered++;
            }

            DebugX.Info(
                "Designer icons: {Covered}/{Total} designer-facing types carry [Icon]. Full table: {Path}",
                covered, entries.Count, ReportPath);

            return report;
        }

        /// <summary>
        /// Writes the symbol table for every designer-facing type: what it already declares, what
        /// the name suggests, and which types nothing recognises. Mutates nothing — the suggestions
        /// are reviewed before <see cref="StampSymbols"/> turns them into source.
        /// </summary>
        public static string AuditSymbols()
        {
            var entries = DesignerIconCatalog.Build();
            var builder = new StringBuilder();
            int declared = 0;
            int suggested = 0;
            var unresolved = new List<DesignerIconEntry>();

            string currentPackage = null;
            foreach (var entry in entries)
            {
                if (entry.PackageId != currentPackage)
                {
                    currentPackage = entry.PackageId;
                    builder.AppendLine();
                    builder.AppendLine($"== {currentPackage}");
                }

                string state;
                if (entry.Symbol.HasValue)
                {
                    declared++;
                    DesignerSymbol? second = DesignerIconSymbolInference.Suggest(entry);

                    // A declared symbol wins, but flagging the disagreement is how a bad early
                    // guess gets caught after the vocabulary improves.
                    state = second.HasValue && second.Value != entry.Symbol.Value
                        ? $"differs  {entry.Symbol.Value} vs {second.Value}"
                        : $"declared {entry.Symbol.Value}";
                }
                else
                {
                    DesignerSymbol? guess = DesignerIconSymbolInference.Suggest(entry);
                    if (guess.HasValue)
                    {
                        suggested++;
                        state = $"suggest  {guess.Value}";
                    }
                    else
                    {
                        unresolved.Add(entry);
                        state = $"letter   {entry.Letter}";
                    }
                }

                builder.AppendLine(string.Join(" | ", new[]
                {
                    state.PadRight(22),
                    entry.IsAsset ? "asset" : "comp ",
                    entry.Domain.PadRight(24),
                    entry.Type.Name.PadRight(44),
                    string.IsNullOrEmpty(entry.MenuPath) ? "(no menu)" : entry.MenuPath,
                }));
            }

            var report = new StringBuilder();
            report.AppendLine(
                $"Designer icon symbols — {entries.Count} types: {declared} declared, {suggested} suggested, {unresolved.Count} falling back to a letter");
            report.AppendLine();
            report.AppendLine($"Symbols available: {string.Join(", ", DesignerIconSymbols.All)}");
            report.Append(builder);

            Directory.CreateDirectory(ReportDirectory);
            File.WriteAllText(AuditPath, report.ToString());

            DebugX.Info(
                "Designer icons: {Declared} declared, {Suggested} suggested, {Unresolved} letter-only. Full table: {Path}",
                declared, suggested, unresolved.Count, AuditPath);

            return report.ToString();
        }

        /// <summary>Stamps <c>[DesignerIcon]</c> on one package's types from the inferred suggestions. Types nothing recognises are left alone and listed.</summary>
        public static string StampSymbols(string packageId)
        {
            var entries = DesignerIconCatalog.BuildForPackage(packageId);
            var builder = new StringBuilder();
            int stamped = 0;
            int skipped = 0;

            foreach (var entry in entries)
            {
                if (entry.Symbol.HasValue)
                {
                    builder.AppendLine($"already   {entry.Type.Name}  ->  {entry.Symbol.Value}");
                    continue;
                }

                DesignerSymbol? guess = DesignerIconSymbolInference.Suggest(entry);
                if (!guess.HasValue)
                {
                    skipped++;
                    builder.AppendLine($"letter    {entry.Type.Name}  ->  '{entry.Letter}' (no symbol inferred)");
                    continue;
                }

                if (DesignerIconAttributeWriter.StampSymbol(entry, guess.Value))
                    stamped++;

                builder.AppendLine($"stamped   {entry.Type.Name}  ->  {guess.Value}");
            }

            DebugX.Info(
                "Designer icons: stamped [DesignerIcon] on {Stamped} of {Total} types in {Package}; {Skipped} keep a letter.",
                stamped, entries.Count, packageId, skipped);

            return builder.ToString();
        }

        /// <summary>Lists what <see cref="ApplyPackage"/> would write for one package, without touching a file.</summary>
        public static string PreviewPackage(string packageId)
        {
            var entries = DesignerIconCatalog.BuildForPackage(packageId);
            var builder = new StringBuilder();
            builder.AppendLine($"Designer icon dry run — {packageId} ({entries.Count} designer-facing types)");
            builder.AppendLine();
            AppendTable(builder, entries);

            DebugX.Info("Designer icons: dry run for {Package} covering {Count} types.", packageId, entries.Count);
            return builder.ToString();
        }

        /// <summary>Generates every icon for one package and stamps the matching <c>[Icon]</c> attributes.</summary>
        public static string ApplyPackage(string packageId)
        {
            GenerateIcons(packageId);
            return StampAttributes(packageId);
        }

        /// <summary>Draws and imports the icon PNGs for one package without touching any source file.</summary>
        public static string GenerateIcons(string packageId)
        {
            var entries = DesignerIconCatalog.BuildForPackage(packageId);

            foreach (var entry in entries)
                DesignerIconWriter.WriteFile(entry);

            AssetDatabase.Refresh();

            foreach (var entry in entries)
                DesignerIconWriter.ApplyImportSettings(entry.IconAssetPath);

            DebugX.Info("Designer icons: wrote {Count} icons for {Package}.", entries.Count, packageId);
            return $"{entries.Count} icons written to Packages/{packageId}/Editor/Icons.";
        }

        /// <summary>Stamps <c>[Icon]</c> on every designer-facing type in one package.</summary>
        public static string StampAttributes(string packageId)
        {
            var entries = DesignerIconCatalog.BuildForPackage(packageId);
            var builder = new StringBuilder();
            int stamped = 0;

            foreach (var entry in entries)
            {
                bool changed = DesignerIconAttributeWriter.Stamp(entry);
                if (changed) stamped++;
                builder.AppendLine($"{(changed ? "stamped" : "already")}  {entry.Type.Name}  ->  {entry.IconAssetPath}");
            }

            DebugX.Info("Designer icons: stamped {Stamped} of {Total} types in {Package}.", stamped, entries.Count, packageId);
            return builder.ToString();
        }

        /// <summary>Designer-facing types with no <c>[Icon]</c>, for the linting menu.</summary>
        public static string ReportMissing()
        {
            var entries = DesignerIconCatalog.Build();
            var missing = new List<DesignerIconEntry>();
            foreach (var entry in entries)
            {
                if (!entry.HasIconAttribute)
                    missing.Add(entry);
            }

            if (missing.Count == 0)
            {
                DebugX.Info("Designer icons: every designer-facing type carries [Icon].");
                return string.Empty;
            }

            var builder = new StringBuilder();
            builder.AppendLine($"Designer-facing types missing [Icon] — {missing.Count} of {entries.Count}");
            AppendTable(builder, missing);

            DebugX.Warning(
                "Designer icons: {Missing} of {Total} designer-facing types have no [Icon].\n{Table}",
                missing.Count, entries.Count, builder.ToString());

            return builder.ToString();
        }

        private static string BuildReport(List<DesignerIconEntry> entries)
        {
            var builder = new StringBuilder();
            builder.AppendLine($"Designer icon coverage — {entries.Count} designer-facing first-party types");
            builder.AppendLine();
            AppendTable(builder, entries);
            return builder.ToString();
        }

        private static void AppendTable(StringBuilder builder, List<DesignerIconEntry> entries)
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
                    entry.HasIconAttribute ? "icon" : "----",
                    entry.IsAsset ? "asset" : "comp ",
                    (entry.Symbol.HasValue ? entry.Symbol.Value.ToString() : $"'{entry.Letter}'").PadRight(10),
                    entry.Domain.PadRight(24),
                    entry.Type.Name.PadRight(44),
                    string.IsNullOrEmpty(entry.MenuPath) ? "(no menu)" : entry.MenuPath,
                }));
            }
        }
    }
}
#endif
