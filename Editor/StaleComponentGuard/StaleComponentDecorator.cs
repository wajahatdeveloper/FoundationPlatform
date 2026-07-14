#if UNITY_EDITOR
using HierarchyX;
using UnityEngine;

namespace FoundationPlatform.StaleComponentGuard.Editor
{
    /// <summary>
    /// Paints a red row + <b>STALE</b> chip on any GameObject that owns a component whose script no longer
    /// declares fields the scene still serializes (see <see cref="StaleComponentCache"/>). Auto-discovered by
    /// <c>HierarchyXRegistry</c> (public parameterless ctor).
    /// </summary>
    public sealed class StaleComponentDecorator : IHierarchyRowDecorator
    {
        private static readonly Color Red = new Color(0.90f, 0.25f, 0.20f);

        // Above Domain (40), below CharacterRig (100): a rig row keeps its identity, but on a plain
        // GameObject the stale-red must win over the domain-teal — drift is more urgent than category.
        public int Order => 60;

        public bool TryDecorate(GameObject go, ref HierarchyRowDecoration decoration)
        {
            if (!StaleComponentCache.IsStale(go))
                return false;

            var tint = Red; tint.a = 0.16f;
            var accent = Red; accent.a = 0.95f;
            var badge = Red; badge.a = 1f;

            decoration.rowTint = tint;
            decoration.tintMode = TintMode.GradientLeftToRight;
            decoration.accent = accent;
            decoration.accentFilled = true;
            decoration.badgeText = "STALE";
            decoration.badgeColor = badge;
            decoration.tooltip = "Stale component: the scene serializes fields this script no longer defines. " +
                                 "Select it to see which, then re-author or Strip.";
            return true;
        }
    }
}
#endif
