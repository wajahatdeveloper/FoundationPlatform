#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using UnityEngine;

namespace AetherNexus.FoundationPlatform.DesignerIcons.Editor
{
    /// <summary>
    /// Domain bucket resolution and colour table for generated script icons. A type's bucket comes
    /// from the first segment of the menu path a designer already sees (<c>[CreateAssetMenu]</c> /
    /// <c>[AddComponentMenu]</c>), so the icon wall and the menu taxonomy stay the same vocabulary.
    /// <para>Aliases collapse the duplicate roots called out in the designer friction audit
    /// (<c>Core</c>/<c>GameEngineCore</c>, <c>Foundation</c>/<c>FoundationPlatform</c>).</para>
    /// </summary>
    internal static class DesignerIconPalette
    {
        /// <summary>Menu root (or package fallback key) to canonical domain.</summary>
        private static readonly Dictionary<string, string> Aliases = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
        {
            { "GameEngineCore", "Core" },
            { "Game Engine Core", "Core" },
            { "Game", "Core" },
            { "Player", "Core" },
            { "Faction", "Core" },
            { "Platform", "Foundation" },
            { "Project", "Foundation" },
            { "Rendering", "Toolkit" },
            { "Progression", "Toolkit" },
            { "Threat", "Combat" },
            { "FoundationPlatform", "Foundation" },
            { "Foundation Platform", "Foundation" },
            { "Item System", "Item" },
            { "ItemSystem", "Item" },
            { "GAS", "Gameplay Ability System" },
            { "Gameplay Tag System", "Gameplay Ability System" },
            { "Character System", "Character" },
            { "Quest System", "Quest" },
            { "Combat System", "Combat" },
            { "UI (Canvas)", "UI Widgets" },
            { "UIWidgets", "UI Widgets" },
            { "Tutorial Manager", "Tutorial" },
            { "Lightmap Generator", "Toolkit" },
            { "AI System", "AI" },

            // Package fallback keys, used when a type is [DesignerFeature]-tagged but carries no menu path.
            { "com.aethernexus.gameenginecore", "Core" },
            { "com.aethernexus.foundationplatform", "Foundation" },
            { "com.aethernexus.gameframework", "Core" },
            { "com.aethernexus.gameplayabilitysystem", "Gameplay Ability System" },
            { "com.aethernexus.liveops", "Tutorial" },
            { "com.aethernexus.tacticalfeatures", "Toolkit" },
        };

        private static readonly Dictionary<string, Color32> Colors = new Dictionary<string, Color32>(StringComparer.OrdinalIgnoreCase)
        {
            { "Core",                    new Color32(0x3E, 0x7C, 0xC4, 0xFF) },
            { "Foundation",              new Color32(0x6B, 0x77, 0x86, 0xFF) },
            { "Item",                    new Color32(0xC1, 0x86, 0x2C, 0xFF) },
            { "AI",                      new Color32(0x74, 0x4B, 0xB8, 0xFF) },
            { "Gameplay Ability System", new Color32(0xC4, 0x4A, 0x46, 0xFF) },
            { "Character",               new Color32(0x2E, 0x96, 0x68, 0xFF) },
            { "Quest",                   new Color32(0xB8, 0x95, 0x22, 0xFF) },
            { "Combat",                  new Color32(0x9C, 0x37, 0x2C, 0xFF) },
            { "Economy",                 new Color32(0x2A, 0x86, 0x86, 0xFF) },
            { "Shop",                    new Color32(0x3B, 0x84, 0x4A, 0xFF) },
            { "UI Widgets",              new Color32(0x48, 0x64, 0x92, 0xFF) },
            { "Tutorial",                new Color32(0x86, 0x63, 0x35, 0xFF) },
            { "Toolkit",                 new Color32(0x55, 0x5F, 0x6B, 0xFF) },
            { "Level",                   new Color32(0x4A, 0x7A, 0x55, 0xFF) },
            { "Input",                   new Color32(0x8A, 0x5A, 0x9E, 0xFF) },
            { "Audio",                   new Color32(0xA8, 0x5C, 0x8C, 0xFF) },
            { "Network",                 new Color32(0x35, 0x72, 0x9E, 0xFF) },
            { "Animation",               new Color32(0x9E, 0x6E, 0x3A, 0xFF) },
        };

        /// <summary>Collapses an alias to its canonical domain; unknown roots pass through unchanged.</summary>
        internal static string Canonicalize(string menuRoot)
        {
            return Aliases.TryGetValue(menuRoot, out string canonical) ? canonical : menuRoot;
        }

        /// <summary>Domain plate colour. Throws when the domain has no entry — an unbucketed type must be classified, not greyed out.</summary>
        internal static Color32 ColorOf(string domain, Type owner)
        {
            if (Colors.TryGetValue(domain, out Color32 color))
                return color;

            throw new InvalidOperationException(
                $"Designer icon domain '{domain}' (from {owner.FullName}) has no palette entry. " +
                $"Add it to {nameof(DesignerIconPalette)}.{nameof(Colors)}, or map its menu root to an existing domain in {nameof(Aliases)}.");
        }

        internal static bool HasColor(string domain) => Colors.ContainsKey(domain);
    }
}
#endif
