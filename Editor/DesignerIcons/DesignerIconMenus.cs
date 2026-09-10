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

        [MenuItem(MenuPaths.Platform.IconsGenerateAndStamp, false, MenuPriorities.Platform + 3)]
        [DesignerFeature(
            "Generate Designer Icons For A Package",
            "Draws the monogram icons for one package's designer-facing types and wires each type to its icon.",
            "icon icons generate create script icon monogram package stamp wire assign",
            DesignerFeatureKind.Generator,
            "docs/09-EditorHub.md")]
        private static void GenerateAndStamp()
        {
            int choice = EditorUtility.DisplayDialogComplex(
                "Designer Icons",
                "Pick a package to generate icons for. Each run previews the affected types before writing anything.",
                Packages[0],
                "Cancel",
                "More Packages...");

            if (choice == 1)
                return;

            if (choice == 0)
            {
                RunWithConfirmation(Packages[0]);
                return;
            }

            var menu = new GenericMenu();
            foreach (string packageId in Packages)
            {
                string captured = packageId;
                menu.AddItem(new UnityEngine.GUIContent(captured), false, () => RunWithConfirmation(captured));
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
