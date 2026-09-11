#if UNITY_EDITOR
using AetherNexus.FoundationPlatform.Utilities.Menus;
using UnityEditor;

namespace AetherNexus.FoundationPlatform.DesignerIcons.Editor
{
    using DebugX = DebugX.DebugX;

    /// <summary>
    /// Menu surface for the designer icon pass. These are project-wide batch operations, which is
    /// the one job <c>Tools/</c> keeps; the icons they produce are what removes the wall of
    /// identical script icons a designer sees in the Project window and in Add Component.
    /// </summary>
    internal static class DesignerIconMenus
    {
        private static readonly string[] Packages =
        {
            "com.aethernexus.gameplayabilitysystem",
            "com.aethernexus.gameframework",
            "com.aethernexus.gameenginecore",
            "com.aethernexus.foundationplatform",
            "com.aethernexus.liveops",
            "com.aethernexus.tacticalfeatures",
        };

        [MenuItem(MenuPaths.Platform.IconsReportCoverage, false, MenuPriorities.Platform + 2)]
        [DesignerFeature(
            "Report Designer Icon Coverage",
            "Lists every asset type and component a designer can create, and whether it has its own icon yet.",
            "icon icons coverage report script icon project window add component missing blank identical",
            DesignerFeatureKind.Validation,
            "docs/09-EditorHub.md")]
        private static void ReportCoverage()
        {
            DesignerIconPipeline.ReportCoverage();
        }

        [MenuItem(MenuPaths.Platform.IconsAuditSymbols, false, MenuPriorities.Platform + 3)]
        [DesignerFeature(
            "Audit Designer Icon Symbols",
            "Shows which shape each asset type and component draws on its icon, and which ones still fall back to a plain letter.",
            "icon icons symbol shape audit report letter fallback unreadable legibility monogram",
            DesignerFeatureKind.Validation,
            "docs/09-EditorHub.md")]
        private static void AuditSymbols()
        {
            DesignerIconPipeline.AuditSymbols();
        }

        [MenuItem(MenuPaths.Platform.IconsStampSymbols, false, MenuPriorities.Platform + 4)]
        [DesignerFeature(
            "Assign Designer Icon Symbols For A Package",
            "Writes the suggested shape onto each of a package's types so their icons stop being look-alike letters.",
            "icon icons symbol shape assign stamp package infer suggestion designer icon attribute",
            DesignerFeatureKind.Generator,
            "docs/09-EditorHub.md")]
        private static void StampSymbols()
        {
            PickPackage(packageId =>
            {
                DebugX.Info(DesignerIconPipeline.AuditSymbols());

                bool apply = EditorUtility.DisplayDialog(
                    "Designer Icons",
                    $"{packageId}\n\nThe symbol audit was written to the console. Stamp [DesignerIcon] on this package's types? Unity recompiles afterwards; generate the icons once it is done.",
                    "Stamp Symbols",
                    "Cancel");

                if (!apply)
                    return;

                DebugX.Info(DesignerIconPipeline.StampSymbols(packageId));
            });
        }

        [MenuItem(MenuPaths.Platform.IconsGenerateAndStamp, false, MenuPriorities.Platform + 5)]
        [DesignerFeature(
            "Generate Designer Icons For A Package",
            "Draws the monogram icons for one package's designer-facing types and wires each type to its icon.",
            "icon icons generate create script icon monogram package stamp wire assign",
            DesignerFeatureKind.Generator,
            "docs/09-EditorHub.md")]
        private static void GenerateAndStamp()
        {
            PickPackage(RunWithConfirmation);
        }

        private static void PickPackage(System.Action<string> run)
        {
            int choice = EditorUtility.DisplayDialogComplex(
                "Designer Icons",
                "Pick a package. Each run previews the affected types before writing anything.",
                Packages[0],
                "Cancel",
                "More Packages...");

            if (choice == 1)
                return;

            if (choice == 0)
            {
                run(Packages[0]);
                return;
            }

            var menu = new GenericMenu();
            foreach (string packageId in Packages)
            {
                string captured = packageId;
                menu.AddItem(new UnityEngine.GUIContent(captured), false, () => run(captured));
            }

            menu.ShowAsContext();
        }

        [MenuItem(MenuPaths.Linting.MissingDesignerIcons, false, MenuPriorities.Linting + 4)]
        [DesignerFeature(
            "Report Missing Designer Icons",
            "Flags designer-facing types that still draw as a plain C# script instead of their own icon.",
            "lint icon missing coverage script icon designer facing validation report",
            DesignerFeatureKind.Validation,
            "docs/09-EditorHub.md")]
        private static void ReportMissing()
        {
            DesignerIconPipeline.ReportMissing();
        }

        private static void RunWithConfirmation(string packageId)
        {
            string preview = DesignerIconPipeline.PreviewPackage(packageId);
            DebugX.Info(preview);

            bool apply = EditorUtility.DisplayDialog(
                "Designer Icons",
                $"{packageId}\n\nThe full list of affected types was written to the console. Generate the icons and stamp [Icon] on each type?",
                "Generate + Stamp",
                "Cancel");

            if (!apply)
                return;

            DebugX.Info(DesignerIconPipeline.ApplyPackage(packageId));
        }
    }
}
#endif
