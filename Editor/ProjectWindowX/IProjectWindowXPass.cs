using UnityEngine;

namespace ProjectWindowX {
    /// <summary>
    /// Implement to draw a Project window row overlay. Concrete types with a public
    /// parameterless constructor are auto-discovered via <see cref="UnityEditor.TypeCache"/>;
    /// instances can also be registered manually with <see cref="ProjectWindowXPassRegistry.Register"/>.
    /// </summary>
    public interface IProjectWindowXPass {
        /// <summary>Stable identity for the pass.</summary>
        string Id { get; }

        /// <summary>Display/draw order; lower runs first.</summary>
        int Order { get; }

        /// <summary>Whether this pass should run for the current settings.</summary>
        bool Enabled(ProjectWindowXSettings s);

        /// <summary>
        /// Draw into the Project window row. Mutate <paramref name="rightInset"/> when consuming
        /// space from the right edge so later passes can avoid overlap.
        /// </summary>
        void Draw(ProjectWindowX.RowContext ctx, Rect rect, bool listMode, ref float rightInset);
    }
}
