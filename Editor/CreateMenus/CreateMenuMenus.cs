#if UNITY_EDITOR
using AetherNexus.FoundationPlatform.Utilities.Menus;
using UnityEditor;
using UnityEngine;

namespace AetherNexus.FoundationPlatform.CreateMenus.Editor
{

    /// <summary>Linting and review-gate surface for the create-menu pass.</summary>
    internal static class CreateMenuMenus
    {
        [MenuItem(MenuPaths.Linting.MissingCreateMenus, false, MenuPriorities.Linting + 6)]
        [DesignerFeature(
            "Report Missing Create Menus",
            "Flags authoring assets a designer cannot create from Assets > Create, and any ScriptableObject the classifier could not place.",
            "lint create asset menu missing assets create authoring scriptableobject coverage report validation",
            DesignerFeatureKind.Validation,
            "docs/09-EditorHub.md")]
        private static void ReportMissing()
        {
            CreateMenuPipeline.ReportMissing();
        }

        [MenuItem(MenuPaths.Platform.CreateMenusReportCoverage, false, MenuPriorities.Platform + 8)]
        [DesignerFeature(
            "Report Create Menu Coverage",
            "Writes the reviewable Assets > Create rehome proposal for every first-party ScriptableObject.",
            "create asset menu coverage report rehome taxonomy proposal",
            DesignerFeatureKind.Validation,
            "docs/09-EditorHub.md")]
        private static void ReportCoverage()
        {
            DebugX.Info(CreateMenuPipeline.ReportCoverage());
        }

        [MenuItem(MenuPaths.Platform.CreateMenusApplyPackage, false, MenuPriorities.Platform + 9)]
        [DesignerFeature(
            "Apply Create Menus For A Package",
            "Rewrites [CreateAssetMenu] menuName values on one package exactly as the reviewed proposal file says.",
            "create asset menu apply stamp package rehome",
            DesignerFeatureKind.Generator,
            "docs/09-EditorHub.md")]
        private static void ApplyPackage()
        {
            var menu = new GenericMenu();
            foreach (string packageId in Packages)
            {
                string captured = packageId;
                menu.AddItem(new GUIContent(captured), false, () =>
                {
                    string preview = CreateMenuPipeline.PreviewPackage(captured);
                    DebugX.Info(preview);
                    if (!EditorUtility.DisplayDialog(
                            "Create Menus",
                            $"{captured}\n\nThe rows this would rewrite were written to the console. Apply the reviewed proposal?",
                            "Stamp",
                            "Cancel"))
                        return;
                    DebugX.Info(CreateMenuPipeline.ApplyPackage(captured));
                });
            }

            menu.ShowAsContext();
        }

        private static readonly string[] Packages =
        {
            "com.aethernexus.gameplayabilitysystem",
            "com.aethernexus.gameframework",
            "com.aethernexus.gameenginecore",
            "com.aethernexus.foundationplatform",
            "com.aethernexus.liveops",
            "com.aethernexus.tacticalfeatures",
        };
    }
}
#endif
