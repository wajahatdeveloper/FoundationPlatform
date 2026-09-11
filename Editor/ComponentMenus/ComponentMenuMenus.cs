#if UNITY_EDITOR
using AetherNexus.FoundationPlatform.Utilities.Menus;
using UnityEditor;
using UnityEngine;

namespace AetherNexus.FoundationPlatform.ComponentMenus.Editor
{
    using DebugX = DebugX.DebugX;

    /// <summary>
    /// Menu surface for the component menu pass. Project-wide batch operations, which is the one
    /// job <c>Tools/</c> keeps; what they produce is an <c>Add Component ▸ Scripts</c> with no
    /// first-party component left in it.
    /// </summary>
    internal static class ComponentMenuMenus
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

        [MenuItem(MenuPaths.Platform.ComponentMenusReportCoverage, false, MenuPriorities.Platform + 4)]
        [DesignerFeature(
            "Report Component Menu Coverage",
            "Classifies every component as designer-placed or internal and writes the reviewable Add Component proposal.",
            "add component menu scripts coverage report classify proposal unclassified",
            DesignerFeatureKind.Validation,
            "docs/09-EditorHub.md")]
        private static void ReportCoverage()
        {
            DebugX.Info(ComponentMenuPipeline.ReportCoverage());
        }

        [MenuItem(MenuPaths.Platform.ComponentMenusApplyPackage, false, MenuPriorities.Platform + 5)]
        [DesignerFeature(
            "Apply Component Menus For A Package",
            "Stamps [AddComponentMenu] on one package's components exactly as the reviewed proposal file says.",
            "add component menu apply stamp package hide internal scripts submenu",
            DesignerFeatureKind.Generator,
            "docs/09-EditorHub.md")]
        private static void ApplyPackage()
        {
            var menu = new GenericMenu();
            foreach (string packageId in Packages)
            {
                string captured = packageId;
                menu.AddItem(new GUIContent(captured), false, () => RunWithConfirmation(captured));
            }

            menu.ShowAsContext();
        }

        [MenuItem(MenuPaths.Linting.MissingComponentMenus, false, MenuPriorities.Linting + 5)]
        [DesignerFeature(
            "Report Missing Component Menus",
            "Flags first-party components that still land in Add Component ▸ Scripts instead of a named category.",
            "lint add component menu missing scripts submenu unclassified validation report",
            DesignerFeatureKind.Validation,
            "docs/09-EditorHub.md")]
        private static void ReportMissing()
        {
            ComponentMenuPipeline.ReportMissing();
        }

        private static void RunWithConfirmation(string packageId)
        {
            string preview = ComponentMenuPipeline.PreviewPackage(packageId);
            DebugX.Info(preview);

            bool apply = EditorUtility.DisplayDialog(
                "Component Menus",
                $"{packageId}\n\nThe rows this would stamp were written to the console. Stamp [AddComponentMenu] from the reviewed proposal file?",
                "Stamp",
                "Cancel");

            if (!apply)
                return;

            DebugX.Info(ComponentMenuPipeline.ApplyPackage(packageId));
        }
    }
}
#endif
