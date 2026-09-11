#if UNITY_EDITOR
using System;

namespace AetherNexus.FoundationPlatform.CreateMenus.Editor
{
    /// <summary>
    /// Maps an authored <c>[CreateAssetMenu]</c> path onto the native-or-domain-root law in
    /// docs/NAMING.md section 4. Longest prefix wins. Unknown roots pass through so a new domain
    /// is a docs + palette change, not a silent rename.
    /// </summary>
    internal static class CreateMenuTaxonomy
    {
        private static readonly (string From, string To)[] Prefixes =
        {
            ("Foundation/Animation/", "Animation/"),
            ("FoundationPlatform/", "Project/"),
            ("Foundation/", "Project/"),
            ("GameEngineCore/Network/", "Network/"),
            ("GameEngineCore/Touch Layout Preset", "Player/Touch Layout Preset"),
            ("GameEngineCore/Package Integration Manifest", "Project/Package Integration Manifest"),
            ("GameEngineCore/Central Authoring Project Config", "Project/Central Authoring Project Config"),
            ("GameEngineCore/", "Game/"),
            ("Core/Input/", "Input/"),
            ("Core/Level ", "Level/"),
            ("Core/Level", "Level/"),
            ("Core/Player Faction ", "Faction/Player "),
            ("Core/Faction ", "Faction/"),
            ("Core/Faction", "Faction/"),
            ("Core/", "Game/"),
            ("Quest System/", "Quest/"),
            ("Character System/", "Character/"),
            ("Tutorial Manager/", "Tutorial/"),
            ("Lightmap Generator/", "Rendering/"),
            ("GameFeatures/Threat ", "Combat/Threat "),
            ("GameFeatures/Progression ", "Progression/"),
            ("GameFeatures/", "Progression/"),
        };

        internal static string Rehome(string menuName)
        {
            if (string.IsNullOrEmpty(menuName))
                return menuName;

            foreach (var pair in Prefixes)
            {
                if (menuName.StartsWith(pair.From, StringComparison.Ordinal))
                    return pair.To + menuName.Substring(pair.From.Length);
            }

            return menuName;
        }
    }
}
#endif
