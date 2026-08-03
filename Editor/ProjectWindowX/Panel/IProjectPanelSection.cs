using System.Collections.Generic;
using UnityEngine;

namespace ProjectWindowX {

    public enum PanelChipStatus {
        Neutral = 0,
        Ok = 1,
        Warning = 2,
        Error = 3,
    }

    public struct PanelChip {
        public string label;
        public PanelChipStatus status;
        public string tooltip;

        public PanelChip(string label, PanelChipStatus status, string tooltip) {
            this.label = label;
            this.status = status;
            this.tooltip = tooltip;
        }

        public PanelChip(string label, PanelChipStatus status) : this(label, status, null) { }
    }

    public struct PanelAction {
        public string glyph;
        public string tooltip;
        public System.Action onClick;
        public string iconName;

        public PanelAction(string glyph, string tooltip, System.Action onClick) {
            this.glyph = glyph;
            this.tooltip = tooltip;
            this.onClick = onClick;
            this.iconName = null;
        }

        public PanelAction(string glyph, string tooltip, System.Action onClick, string iconName) {
            this.glyph = glyph;
            this.tooltip = tooltip;
            this.onClick = onClick;
            this.iconName = iconName;
        }
    }

    /// <summary>
    /// Contribute a section to the Project docked context panel. Auto-discovered via TypeCache
    /// when a public parameterless constructor exists.
    /// </summary>
    public interface IProjectPanelSection {
        string Id { get; }
        string Title { get; }
        int Order { get; }
        IEnumerable<PanelChip> GetHeaderChips();
        IEnumerable<PanelAction> GetToolbarActions();
        void OnBodyGUI();
    }
}
