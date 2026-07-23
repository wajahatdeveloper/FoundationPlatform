using UnityEngine;

namespace ProjectWindowX {
    /// <summary>Registered pass wrapping <see cref="FileExtensionLabels"/>.</summary>
    public sealed class FileExtensionLabelsPass : IProjectWindowXPass {
        public string Id => "projectwindowx.file-extension-labels";
        public int Order => 300;

        public bool Enabled(ProjectWindowXSettings s) => s.extensionLabels;

        public void Draw(ProjectWindowX.RowContext ctx, Rect rect, bool listMode, ref float rightInset) {
            if (!listMode || ctx.isFolder)
                return;
            FileExtensionLabels.Draw(ctx, rect, rightInset);
        }
    }
}