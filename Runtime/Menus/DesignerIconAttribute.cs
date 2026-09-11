using System;

namespace AetherNexus.FoundationPlatform.Utilities.Menus
{
    /// <summary>
    /// Declares which symbol the generated script icon draws for this type. Authoring intent only:
    /// the icon generator reads it, and separately stamps Unity's <c>[Icon]</c> with the path of
    /// the PNG it wrote. A type without this attribute falls back to its initial, which reads far
    /// worse at 16px than a shape — so anything a designer meets often should declare one.
    /// </summary>
    [AttributeUsage(AttributeTargets.Class, AllowMultiple = false, Inherited = false)]
    public sealed class DesignerIconAttribute : Attribute
    {
        public DesignerIconAttribute(DesignerSymbol symbol)
        {
            Symbol = symbol;
        }

        public DesignerSymbol Symbol { get; }
    }
}
