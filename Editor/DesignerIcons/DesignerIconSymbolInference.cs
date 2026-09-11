#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using AetherNexus.FoundationPlatform.Utilities.Menus;

namespace AetherNexus.FoundationPlatform.DesignerIcons.Editor
{
    /// <summary>
    /// Suggests a <see cref="DesignerSymbol"/> for a type from the words in its name, falling back
    /// to its menu path. Audit-time only: the renderer never calls this, so a stamped
    /// <c>[DesignerIcon]</c> stays the single source of truth and a suggestion can be overruled by
    /// hand without the icon changing back on the next run.
    /// </summary>
    internal static class DesignerIconSymbolInference
    {
        /// <summary>
        /// Types whose name carries no word worth matching, decided by hand during the icon audit.
        /// Checked before the vocabulary so the call stays a decision rather than a coincidence.
        /// </summary>
        private static readonly Dictionary<string, DesignerSymbol> Overrides = new Dictionary<string, DesignerSymbol>(StringComparer.Ordinal)
        {
            { "InspectorSeparator", DesignerSymbol.Widget },
            { "PhysicsMover", DesignerSymbol.Component },
            { "GroundTargetingController", DesignerSymbol.Ability },
            { "PartyFollowController", DesignerSymbol.Character },
            { "PartyProgression", DesignerSymbol.Character },
            { "PartyRoster", DesignerSymbol.Character },
            { "ThreatTracker", DesignerSymbol.Combat },
        };

        /// <summary>
        /// Word to symbol, most specific first — order decides ties, so subject words ("ability",
        /// "character") come before the container words that wrap them ("panel", "registry",
        /// "config"). Matching runs on whole words, so "ai" never fires inside "chain".
        /// </summary>
        private static readonly (string Word, DesignerSymbol Symbol)[] Vocabulary =
        {
            ("spawn", DesignerSymbol.Spawn),
            ("spawner", DesignerSymbol.Spawn),
            ("spawnpoint", DesignerSymbol.Spawn),
            ("start", DesignerSymbol.Spawn),
            ("placement", DesignerSymbol.Spawn),
            ("waypoint", DesignerSymbol.Spawn),

            ("gizmo", DesignerSymbol.Gizmo),
            ("handle", DesignerSymbol.Gizmo),
            ("overlay", DesignerSymbol.Gizmo),
            ("debug", DesignerSymbol.Gizmo),

            ("camera", DesignerSymbol.Camera),
            ("cam", DesignerSymbol.Camera),
            ("cinemachine", DesignerSymbol.Camera),
            ("framing", DesignerSymbol.Camera),
            ("viewport", DesignerSymbol.Camera),

            ("ai", DesignerSymbol.Ai),
            ("brain", DesignerSymbol.Ai),
            ("behavior", DesignerSymbol.Ai),
            ("behaviour", DesignerSymbol.Ai),
            ("decision", DesignerSymbol.Ai),
            ("consideration", DesignerSymbol.Ai),
            ("utility", DesignerSymbol.Ai),
            ("commander", DesignerSymbol.Ai),
            ("sensor", DesignerSymbol.Ai),

            // Media words come before "player", which otherwise claims MusicPlayer and FeedbackPlayer.
            ("audio", DesignerSymbol.Audio),
            ("sound", DesignerSymbol.Audio),
            ("music", DesignerSymbol.Audio),
            ("sfx", DesignerSymbol.Audio),
            ("voice", DesignerSymbol.Audio),

            ("vfx", DesignerSymbol.Effect),
            ("particle", DesignerSymbol.Effect),
            ("feedback", DesignerSymbol.Effect),

            ("character", DesignerSymbol.Character),
            ("player", DesignerSymbol.Character),
            ("unit", DesignerSymbol.Character),
            ("npc", DesignerSymbol.Character),
            ("hero", DesignerSymbol.Character),
            ("enemy", DesignerSymbol.Character),
            ("actor", DesignerSymbol.Character),
            ("pawn", DesignerSymbol.Character),
            ("faction", DesignerSymbol.Character),
            ("ragdoll", DesignerSymbol.Character),
            ("locomotion", DesignerSymbol.Character),

            ("ability", DesignerSymbol.Ability),
            ("abilities", DesignerSymbol.Ability),
            ("skill", DesignerSymbol.Ability),
            ("spell", DesignerSymbol.Ability),
            ("cast", DesignerSymbol.Ability),
            ("gas", DesignerSymbol.Ability),

            ("combat", DesignerSymbol.Combat),
            ("damage", DesignerSymbol.Combat),
            ("weapon", DesignerSymbol.Combat),
            ("melee", DesignerSymbol.Combat),
            ("attack", DesignerSymbol.Combat),
            ("armor", DesignerSymbol.Combat),
            ("armour", DesignerSymbol.Combat),
            ("defence", DesignerSymbol.Combat),
            ("defense", DesignerSymbol.Combat),
            ("health", DesignerSymbol.Combat),

            ("quest", DesignerSymbol.Quest),
            ("objective", DesignerSymbol.Quest),
            ("mission", DesignerSymbol.Quest),

            ("item", DesignerSymbol.Item),
            ("inventory", DesignerSymbol.Item),
            ("loot", DesignerSymbol.Item),
            ("equipment", DesignerSymbol.Item),
            ("pickup", DesignerSymbol.Item),
            ("slot", DesignerSymbol.Item),

            ("stat", DesignerSymbol.Stat),
            ("attribute", DesignerSymbol.Stat),
            ("modifier", DesignerSymbol.Stat),
            ("curve", DesignerSymbol.Stat),
            ("analytics", DesignerSymbol.Stat),
            ("telemetry", DesignerSymbol.Stat),

            ("animation", DesignerSymbol.Animation),
            ("animator", DesignerSymbol.Animation),
            ("anim", DesignerSymbol.Animation),
            ("clip", DesignerSymbol.Animation),
            ("pose", DesignerSymbol.Animation),
            ("rig", DesignerSymbol.Animation),
            ("ik", DesignerSymbol.Animation),
            ("bone", DesignerSymbol.Animation),
            ("stance", DesignerSymbol.Animation),
            ("tween", DesignerSymbol.Animation),
            ("blend", DesignerSymbol.Animation),

            ("input", DesignerSymbol.Input),
            ("touch", DesignerSymbol.Input),
            ("pointer", DesignerSymbol.Input),
            ("joystick", DesignerSymbol.Input),
            ("gesture", DesignerSymbol.Input),
            ("binding", DesignerSymbol.Input),
            ("drag", DesignerSymbol.Input),
            ("drop", DesignerSymbol.Input),
            ("click", DesignerSymbol.Input),
            ("select", DesignerSymbol.Input),
            ("selection", DesignerSymbol.Input),
            ("selectable", DesignerSymbol.Input),
            ("navigation", DesignerSymbol.Input),

            ("level", DesignerSymbol.Level),
            ("map", DesignerSymbol.Level),
            ("terrain", DesignerSymbol.Level),
            ("tile", DesignerSymbol.Level),
            ("grid", DesignerSymbol.Level),
            ("chunk", DesignerSymbol.Level),
            ("biome", DesignerSymbol.Level),
            ("scene", DesignerSymbol.Level),

            ("network", DesignerSymbol.Network),
            ("networked", DesignerSymbol.Network),
            ("replication", DesignerSymbol.Network),
            ("session", DesignerSymbol.Network),
            ("lobby", DesignerSymbol.Network),
            ("sync", DesignerSymbol.Network),

            ("economy", DesignerSymbol.Economy),
            ("currency", DesignerSymbol.Economy),
            ("shop", DesignerSymbol.Economy),
            ("store", DesignerSymbol.Economy),
            ("price", DesignerSymbol.Economy),
            ("cost", DesignerSymbol.Economy),
            ("reward", DesignerSymbol.Economy),
            ("wallet", DesignerSymbol.Economy),

            ("tutorial", DesignerSymbol.Tutorial),
            ("hint", DesignerSymbol.Tutorial),
            ("onboarding", DesignerSymbol.Tutorial),
            ("tooltip", DesignerSymbol.Tutorial),
            ("help", DesignerSymbol.Tutorial),

            ("effect", DesignerSymbol.Effect),
            ("shader", DesignerSymbol.Effect),
            ("light", DesignerSymbol.Effect),
            ("lighting", DesignerSymbol.Effect),
            ("lightmap", DesignerSymbol.Effect),
            ("gradient", DesignerSymbol.Effect),
            ("shine", DesignerSymbol.Effect),
            ("glow", DesignerSymbol.Effect),
            ("fade", DesignerSymbol.Effect),
            ("fader", DesignerSymbol.Effect),

            ("timer", DesignerSymbol.Timer),
            ("cooldown", DesignerSymbol.Timer),
            ("duration", DesignerSymbol.Timer),
            ("schedule", DesignerSymbol.Timer),
            ("clock", DesignerSymbol.Timer),
            ("delay", DesignerSymbol.Timer),

            ("tag", DesignerSymbol.Tag),
            ("label", DesignerSymbol.Tag),
            ("identity", DesignerSymbol.Tag),
            ("identifier", DesignerSymbol.Tag),
            ("marker", DesignerSymbol.Tag),

            ("ui", DesignerSymbol.Widget),
            ("hud", DesignerSymbol.Widget),
            ("widget", DesignerSymbol.Widget),
            ("panel", DesignerSymbol.Widget),
            ("menu", DesignerSymbol.Widget),
            ("screen", DesignerSymbol.Widget),
            ("window", DesignerSymbol.Widget),
            ("popup", DesignerSymbol.Widget),
            ("dialog", DesignerSymbol.Widget),
            ("modal", DesignerSymbol.Widget),
            ("toast", DesignerSymbol.Widget),
            ("button", DesignerSymbol.Widget),
            ("slider", DesignerSymbol.Widget),
            ("stepper", DesignerSymbol.Widget),
            ("dropdown", DesignerSymbol.Widget),
            ("toggle", DesignerSymbol.Widget),
            ("tab", DesignerSymbol.Widget),
            ("card", DesignerSymbol.Widget),
            ("scroll", DesignerSymbol.Widget),
            ("layout", DesignerSymbol.Widget),
            ("canvas", DesignerSymbol.Widget),

            ("registry", DesignerSymbol.Database),
            ("catalog", DesignerSymbol.Database),
            ("catalogue", DesignerSymbol.Database),
            ("database", DesignerSymbol.Database),
            ("table", DesignerSymbol.Database),
            ("library", DesignerSymbol.Database),
            ("collection", DesignerSymbol.Database),

            ("trigger", DesignerSymbol.Trigger),
            ("zone", DesignerSymbol.Trigger),
            ("volume", DesignerSymbol.Trigger),
            ("area", DesignerSymbol.Trigger),
            ("region", DesignerSymbol.Trigger),
            ("bounds", DesignerSymbol.Trigger),

            ("config", DesignerSymbol.Document),
            ("configuration", DesignerSymbol.Document),
            ("settings", DesignerSymbol.Document),
            ("profile", DesignerSymbol.Document),
            ("definition", DesignerSymbol.Document),
            ("preset", DesignerSymbol.Document),
            ("template", DesignerSymbol.Document),
            ("recipe", DesignerSymbol.Document),
            ("manifest", DesignerSymbol.Document),
            ("comment", DesignerSymbol.Document),
            ("note", DesignerSymbol.Document),
            ("properties", DesignerSymbol.Document),

            ("bootstrap", DesignerSymbol.Component),
            ("hub", DesignerSymbol.Component),
            ("service", DesignerSymbol.Component),
            ("subsystem", DesignerSymbol.Component),
            ("bridge", DesignerSymbol.Component),
        };

        /// <summary>
        /// Best guess for <paramref name="entry"/>, or null when nothing is recognised — those are
        /// the types to bring to a human rather than guess at. The type's own name is searched
        /// first: a menu root like "Item System" would otherwise label every type under it.
        /// </summary>
        internal static DesignerSymbol? Suggest(DesignerIconEntry entry)
        {
            if (Overrides.TryGetValue(entry.Type.Name, out DesignerSymbol pinned))
                return pinned;

            DesignerSymbol? fromName = Match(NameWords(entry.Type.Name));
            if (fromName.HasValue)
                return fromName;

            return Match(MenuWords(entry.MenuPath));
        }

        private static DesignerSymbol? Match(HashSet<string> words)
        {
            foreach ((string word, DesignerSymbol symbol) in Vocabulary)
            {
                if (words.Contains(word))
                    return symbol;
            }

            return null;
        }

        private static HashSet<string> NameWords(string typeName)
        {
            var words = new HashSet<string>(StringComparer.Ordinal);
            foreach (string word in DesignerIconCatalog.SplitWords(typeName))
                Add(words, word);

            return words;
        }

        private static HashSet<string> MenuWords(string menuPath)
        {
            var words = new HashSet<string>(StringComparer.Ordinal);
            if (string.IsNullOrEmpty(menuPath))
                return words;

            foreach (string segment in menuPath.Split('/'))
            {
                foreach (string piece in segment.Split(' '))
                {
                    foreach (string word in DesignerIconCatalog.SplitWords(piece))
                        Add(words, word);
                }
            }

            return words;
        }

        /// <summary>Adds the word and its singular, so the table carries one entry per concept.</summary>
        private static void Add(HashSet<string> words, string word)
        {
            string lower = word.ToLowerInvariant();
            words.Add(lower);

            if (lower.Length > 3 && lower.EndsWith("s", StringComparison.Ordinal))
                words.Add(lower.Substring(0, lower.Length - 1));
        }
    }
}
#endif
