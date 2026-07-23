using UnityEditor;

namespace ProjectWindowX {
    /// <summary>
    /// Implement to contribute items to the ProjectWindowX hover "+" create-actions menu.
    /// Concrete types with a public parameterless constructor are auto-discovered via
    /// <see cref="UnityEditor.TypeCache"/>; instances can also be registered manually with
    /// <see cref="ProjectWindowXContextMenuRegistry.Register"/>.
    /// </summary>
    public interface IProjectWindowXContextMenu {
        /// <summary>Stable identity for the contributor.</summary>
        string Id { get; }

        /// <summary>Contribution order; lower runs first.</summary>
        int Order { get; }

        /// <summary>
        /// Add menu items for the given row. <paramref name="targetFolder"/> is the folder
        /// create actions should target (same as built-in items).
        /// </summary>
        void Contribute(ProjectWindowX.RowContext ctx, string targetFolder, GenericMenu menu);
    }
}
