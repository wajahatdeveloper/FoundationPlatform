using UnityEngine;

namespace ProjectWindowX {
    /// <summary>Registered pass wrapping <see cref="FileExtensionLabels"/>.</summary>
    public sealed class FileExtensionLabelsPass : IProjectWindowXPass {
        public string Id => "projectwindowx.file-extension-labels";
        // Runs before the badge passes (250/260) so the extension label claims the
        // rightmost slot and badges/chips are pushed to its left, not the other way round.
        public int Order => 210;

        public bool Enabled(ProjectWindowXSettings s) => s.extensionLabels;

        public void Draw(ProjectWindowX.RowContext ctx, Rect rect, bool listMode, ref float rightInset) {
            if (!listMode || ctx.isFolder)
                return;
            FileExtensionLabels.Draw(ctx, rect, ref rightInset);
        }
    }
}