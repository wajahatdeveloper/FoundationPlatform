namespace AetherNexus.FoundationPlatform
{
    /// <summary>
    /// Marks an authoring-only component (notes, separators, gizmos, edit-mode helpers). The class
    /// still compiles in players so placed instances never become missing scripts, but the build's
    /// scene processor removes every instance from player scenes.
    /// </summary>
    public interface IEditorOnlyComponent
    {
    }
}
