#if UNITY_EDITOR
using System.Collections.Generic;
using System.Text;
using UnityEditor;

namespace AetherNexus.FoundationPlatform.ComponentMenus.Editor
{
    using DebugX = DebugX.DebugX;

    /// <summary>
    /// Batch entry points for the component menu pass: classify and report, stamp one reviewed
    /// package, lint what is still unclassified. Split from the menu items so the same calls are
    /// reachable from an editor script or an agent tool without going through the menu.
    /// </summary>
    public static class ComponentMenuPipeline
    {
        /// <summary>Classifies every first-party component and writes the reviewable proposal. Mutates no source.</summary>
        public static string ReportCoverage()
        {
            var entries = ComponentMenuCatalog.Build();
            ComponentMenuProposalFile.Write(entries);

            int authored = 0;
            int designerPlaced = 0;
            int hidden = 0;
            foreach (var entry in entries)
            {
                switch (entry.Verdict)
                {
                    case ComponentMenuVerdict.AlreadySet: authored++; break;
                    case ComponentMenuVerdict.DesignerPlaced: designerPlaced++; break;
                    default: hidden++; break;
                }
            }

            DebugX.Info(
                "Component menus: {Total} first-party components — {Authored} already authored, {Placed} proposed as designer-placed, {Hidden} proposed as hidden. Review {Path} before applying.",
                entries.Count, authored, designerPlaced, hidden, ComponentMenuProposalFile.Path);

            return BuildTable(entries);
        }

        /// <summary>What <see cref="ApplyPackage"/> would stamp for one package, straight from the reviewed file.</summary>
        public static string PreviewPackage(string packageId)
        {
            var rows = ComponentMenuProposalFile.Read(packageId);
            var builder = new StringBuilder();
            builder.AppendLine($"Component menu dry run — {packageId} ({rows.Count} reviewed rows)");
            builder.AppendLine();

            int pending = 0;
            foreach (var row in rows)
            {
                if (row.Verdict == ComponentMenuVerdict.AlreadySet)
                    continue;

                pending++;
                builder.AppendLine($"{row.Verdict,-14} {row.TypeName,-46} {(row.Menu.Length == 0 ? "(hidden)" : row.Menu)}");
            }

            builder.AppendLine();
            builder.AppendLine($"{pending} of {rows.Count} rows would be stamped.");
            return builder.ToString();
        }

        /// <summary>Stamps one package exactly as the reviewed proposal file says.</summary>
        public static string ApplyPackage(string packageId)
        {
            var rows = ComponentMenuProposalFile.Read(packageId);
            var builder = new StringBuilder();
            int stamped = 0;
            int skipped = 0;

            foreach (var row in rows)
            {
                if (row.Verdict == ComponentMenuVerdict.AlreadySet)
                {
                    skipped++;
                    continue;
                }

                bool changed = ComponentMenuAttributeWriter.Stamp(row.ScriptPath, row.TypeName, row.Menu);
                if (changed)
                    stamped++;
                else
                    skipped++;

                builder.AppendLine($"{(changed ? "stamped" : "already")}  {row.TypeName,-46} {(row.Menu.Length == 0 ? "(hidden)" : row.Menu)}");
            }

            AssetDatabase.Refresh();

            DebugX.Info("Component menus: stamped {Stamped} components in {Package}, skipped {Skipped}.", stamped, packageId, skipped);
            return builder.ToString();
        }

        /// <summary>Components with no <c>[AddComponentMenu]</c> at all, for the linting menu.</summary>
        public static string ReportMissing()
        {
            var entries = ComponentMenuCatalog.Build();
            var missing = new List<ComponentMenuEntry>();
            foreach (var entry in entries)
            {
                if (!entry.HasAttribute)
                    missing.Add(entry);
            }

            if (missing.Count == 0)
            {
                DebugX.Info("Component menus: every first-party component declares [AddComponentMenu].");
                return string.Empty;
            }

            string table = BuildTable(missing);
            DebugX.Warning(
                "Component menus: {Missing} of {Total} first-party components land in Add Component > Scripts.\n{Table}",
                missing.Count, entries.Count, table);

            return table;
        }

        private static string BuildTable(List<ComponentMenuEntry> entries)
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

                builder.AppendLine(string.Join(" | ", new[]
                {
                    entry.Verdict.ToString().PadRight(14),
                    entry.Type.Name.PadRight(46),
                    (entry.ProposedMenu.Length == 0 ? "(hidden)" : entry.ProposedMenu).PadRight(52),
                    entry.Reason,
                }));
            }

            return builder.ToString();
        }
    }
}
#endif
