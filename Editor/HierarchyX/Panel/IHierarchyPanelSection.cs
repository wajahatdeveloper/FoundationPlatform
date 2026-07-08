using System.Collections.Generic;
using UnityEngine;

namespace HierarchyX {

    /// <summary>Traffic-light state for a <see cref="PanelChip"/>.</summary>
    public enum PanelChipStatus {
        /// <summary>Informational / not-applicable. Neutral grey.</summary>
        Neutral = 0,
        /// <summary>Set up correctly. Green.</summary>
        Ok = 1,
        /// <summary>Present but incomplete / needs attention. Amber.</summary>
        Warning = 2,
        /// <summary>Missing / broken. Red.</summary>
        Error = 3,
    }

    /// <summary>
    /// A small status pill shown in a section's collapsed header row — the "at a glance" summary
    /// (e.g. "GameBootstrap ✓", "Roster ⚠"). Sections return these from
    /// <see cref="IHierarchyPanelSection.GetHeaderChips"/>.
    /// </summary>
    public struct PanelChip {
        public string label;
        public PanelChipStatus status;
        public string tooltip;

        public PanelChip(string label, PanelChipStatus status, string tooltip = null) {
            this.label = label;
            this.status = status;
            this.tooltip = tooltip;
        }
    }

    /// <summary>
    /// A compact icon action a section contributes to the panel's top toolbar (beside the built-in
    /// settings/window icons). Keeps game-specific actions out of the engine-agnostic host.
    /// </summary>
    public struct PanelAction {
        public string glyph;
        public string tooltip;
        public System.Action onClick;

        public PanelAction(string glyph, string tooltip, System.Action onClick) {
            this.glyph = glyph;
            this.tooltip = tooltip;
            this.onClick = onClick;
        }
    }

    /// <summary>
    /// Implement to contribute a section to the Hierarchy docked setup panel without coupling
    /// HierarchyX to game code. Concrete implementations with a public parameterless constructor are
    /// auto-discovered via <see cref="UnityEditor.TypeCache"/> (same pattern as
    /// <see cref="IHierarchyRowDecorator"/>); you can also register instances manually with
    /// <see cref="HierarchyXPanelRegistry.Register"/>.
    ///
    /// <see cref="GetHeaderChips"/> must be cheap (called every repaint for the collapsed summary);
    /// do heavier scanning inside <see cref="OnBodyGUI"/> or behind a cache the section invalidates
    /// on scene/object change.
    /// </summary>
    public interface IHierarchyPanelSection {
        /// <summary>Stable identity used to persist the section's expanded/collapsed state.</summary>
        string Id { get; }

        /// <summary>Human title shown on the section's header row.</summary>
        string Title { get; }

        /// <summary>Display order; lower sorts first.</summary>
        int Order { get; }

        /// <summary>
        /// Status pills for the collapsed header summary. Return an empty sequence for none.
        /// Keep this cheap — it runs on the hot repaint path.
        /// </summary>
        IEnumerable<PanelChip> GetHeaderChips();

        /// <summary>
        /// Compact icon actions shown in the panel's top toolbar. Return an empty sequence for none.
        /// </summary>
        IEnumerable<PanelAction> GetToolbarActions();

        /// <summary>
        /// Draw the expanded section body using <c>GUILayout</c>/<c>EditorGUILayout</c>. Only called
        /// when the section is expanded and the panel is not collapsed.
        /// </summary>
        void OnBodyGUI();
    }
}
