using UnityEngine;

namespace ProjectWindowX {
    /// <summary>Registered pass wrapping <see cref="ContextActions"/> (hover "+" button).</summary>
    public sealed class ContextActionsPass : IProjectWindowXPass {
        public string Id => "projectwindowx.context-actions";
        public int Order => 200;

        public bool Enabled(ProjectWindowXSettings s) => s.contextActions;

        public void Draw(ProjectWindowX.RowContext ctx, Rect rect, bool listMode, ref float rightInset) {
            if (!listMode)
                return;
            rightInset += ContextActions.Draw(ctx, rect);
        }
    }
}
