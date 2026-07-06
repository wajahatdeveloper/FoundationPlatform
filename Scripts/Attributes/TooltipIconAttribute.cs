using System;
using UnityEngine;

namespace FoundationPlatform.Attributes
{
    [AttributeUsage(AttributeTargets.Field | AttributeTargets.Property, AllowMultiple = false, Inherited = true)]
    public sealed class TooltipIconAttribute : PropertyAttribute
    {
        public string Tooltip { get; }

        public TooltipIconAttribute(string tooltip)
        {
            Tooltip = tooltip;
        }
    }
}
