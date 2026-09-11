#if UNITY_EDITOR
using System.Collections.Generic;
using System.Text;

namespace AetherNexus.FoundationPlatform.CreateMenus.Editor
{
    using DebugX = DebugX.DebugX;

    /// <summary>
    /// Report-only half of the create-menu pass: which authoring assets a designer cannot reach
    /// from <c>Assets ▸ Create</c>. Split from the menu item so an editor script or an agent tool
    /// can call it without going through the menu. Nothing here mutates source.
    /// </summary>
    public static class CreateMenuPipeline
    {
        /// <summary>Authoring types with no <c>[CreateAssetMenu]</c>, plus anything the classifier could not place.</summary>
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
                AppendTable(builder, authoring);
            }

            if (unclassified.Count > 0)
            {
                builder.AppendLine();
                builder.AppendLine("Unclassified — decide whether these are authored by hand or owned by a tool:");
                AppendTable(builder, unclassified);
            }

            string table = builder.ToString();
            DebugX.Warning(
                "Create menus: {Authoring} authoring assets and {Unclassified} unclassified types out of {Total} first-party ScriptableObjects have no [CreateAssetMenu].\n{Table}",
                authoring.Count, unclassified.Count, entries.Count, table);

            return table;
        }

        private static void AppendTable(StringBuilder builder, List<CreateMenuEntry> entries)
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
    }
}
#endif
