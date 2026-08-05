using UnityEngine;

namespace ProjectWindowX {
    /// <summary>Registered pass for custom folder icons (resolve API lives on <see cref="FolderIcons"/>).</summary>
    public sealed class FolderIconsPass : IProjectWindowXPass {
        public string Id => "projectwindowx.folder-icons";
        public int Order => 100;

        public bool Enabled(ProjectWindowXSettings s) => s.folderIcons;

        public void Draw(ProjectWindowX.RowContext ctx, Rect rect, bool listMode, ref float rightInset) {
            if (!ctx.isFolder)
                return;
            FolderIcons.Draw(ctx, rect, listMode, ProjectWindowXSettings.instance);
        }
    }
}
