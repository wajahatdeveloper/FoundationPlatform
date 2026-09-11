#if UNITY_EDITOR
using AetherNexus.FoundationPlatform.Utilities.Menus;
using UnityEditor;

namespace AetherNexus.FoundationPlatform.CreateMenus.Editor
{
    /// <summary>Linting surface for the create-menu pass: one project-wide coverage report.</summary>
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
    }
}
#endif
