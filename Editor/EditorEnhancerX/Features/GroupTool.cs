using System.Linq;
using AetherNexus.FoundationPlatform.Utilities.Menus;
using UnityEditor;
using UnityEngine;

namespace EditorEnhancerX {
    /// <summary>
    /// Group: wraps the selection in a new parent GameObject (placement per settings,
    /// optional name prompt), preserving sibling order. Ungroup: moves children of the
    /// selected group(s) up to the grandparent and deletes the empty parent. Full Undo.
    /// </summary>
    [InitializeOnLoad]
    internal static class GroupTool {

        static GroupTool() {
            KeyRouter.Register("group",
                () => EditorEnhancerXSettings.instance.groupKey,
                KeyScope.SceneView | KeyScope.Hierarchy,
                Group);
            KeyRouter.Register("ungroup",
                () => EditorEnhancerXSettings.instance.ungroupKey,
                KeyScope.SceneView | KeyScope.Hierarchy,
                Ungroup);
        }

        [MenuItem(MenuPaths.EditorEnhancer.GroupSelection, false, 0)]
        private static void GroupMenu() => Group();

        [MenuItem(MenuPaths.EditorEnhancer.GroupSelection, true)]
        private static bool GroupMenuValidate() => Selection.transforms.Length > 0;

        [MenuItem(MenuPaths.EditorEnhancer.Ungroup, false, 1)]
        private static void UngroupMenu() => Ungroup();

        [MenuItem(MenuPaths.EditorEnhancer.Ungroup, true)]
        private static bool UngroupMenuValidate()
            => Selection.transforms.Any(t => t.childCount > 0);

        private static bool Group() {
            var selection = Selection.transforms;
            if (selection.Length == 0)
                return false;

            var s = EditorEnhancerXSettings.instance.group;
            if (s.askForName) {
                RenameWindow.OpenPrompt("Group Name", s.defaultName, name => CreateGroup(name));
                return true;
            }
            CreateGroup(s.defaultName);
            return true;
        }

        private static void CreateGroup(string name) {
            var selection = Selection.transforms
                .OrderBy(t => t.GetSiblingIndex())
                .ToArray();
            if (selection.Length == 0)
                return;

            var s = EditorEnhancerXSettings.instance.group;
            var group = new GameObject(string.IsNullOrEmpty(name) ? "Group" : name);
            Undo.RegisterCreatedObjectUndo(group, "Group Selection");

            // Insert where the first selected object lives.
            var anchor = selection[0];
            group.transform.SetParent(anchor.parent, false);
            group.transform.SetSiblingIndex(anchor.GetSiblingIndex());

            switch (s.parentPlacement) {
                case EditorEnhancerXSettings.GroupOptions.ParentPlacement.SelectionCenter:
                    if (SelectionBoundsUtility.TryGetBounds(selection.Select(t => t.gameObject).ToArray(), out var bounds))
                        group.transform.position = bounds.center;
                    break;
                case EditorEnhancerXSettings.GroupOptions.ParentPlacement.FirstObjectPivot:
                    group.transform.position = anchor.position;
                    break;
                case EditorEnhancerXSettings.GroupOptions.ParentPlacement.WorldOrigin:
                    group.transform.position = Vector3.zero;
                    break;
            }

            foreach (var t in selection)
                Undo.SetTransformParent(t, group.transform, "Group Selection");

            Selection.activeGameObject = group;
        }

        private static bool Ungroup() {
            var groups = Selection.transforms.Where(t => t.childCount > 0).ToArray();
            if (groups.Length == 0)
                return false;

            foreach (var group in groups) {
                var parent = group.parent;
                var index = group.GetSiblingIndex();

                var children = new Transform[group.childCount];
                for (var i = 0; i < children.Length; i++)
                    children[i] = group.GetChild(i);

                foreach (var child in children) {
                    Undo.SetTransformParent(child, parent, "Ungroup");
                    child.SetSiblingIndex(index++);
                }

                Undo.DestroyObjectImmediate(group.gameObject);
            }
            return true;
        }
    }
}
