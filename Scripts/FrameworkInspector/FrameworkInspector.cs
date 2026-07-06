namespace Framework.Inspector
{
    /// <summary>
    /// The in-house inspector attribute set.
    /// This assembly holds the attribute definitions (Phase 1); the matching UI Toolkit
    /// drawer engine lives in <c>Framework.Inspector.Editor</c>. See
    /// the Framework Inspector demo window for the live attribute surface.
    ///
    /// Attributes are pure metadata: adding them here has no effect until the drawer
    /// engine reads them, so no inspector regresses before Phase 1 wires both together.
    /// </summary>
    public static class FrameworkInspector
    {
        /// <summary>Marks the scaffold as present. Bumped as coverage lands.</summary>
        public const string Version = "0.0.0-scaffold";
    }
}
