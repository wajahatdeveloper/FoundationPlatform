using UnityEngine;

namespace ProjectWindowX {
    /// <summary>Registered pass wrapping <see cref="ZebraRows"/>.</summary>
    public sealed class ZebraRowsPass : IProjectWindowXPass {
        public string Id => "projectwindowx.zebra-rows";
        public int Order => 0;

        public bool Enabled(ProjectWindowXSettings s) => s.zebraRows;

        public void Draw(ProjectWindowX.RowContext ctx, Rect rect, bool listMode, ref float rightInset) {
            if (!listMode)
                return;
            ZebraRows.Draw(rect, ProjectWindowXSettings.instance);
        }
    }
}
