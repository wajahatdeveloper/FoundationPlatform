namespace AetherNexus.FoundationPlatform.Utilities.Menus
{
    /// <summary>
    /// The shape drawn on a generated script icon. Each value maps to one hand-authored 10x10
    /// pixel mark, so the icon wall reads as a consistent set instead of a wall of initials. Pick
    /// the value a designer would recognise from the type's job, not from its class name.
    /// </summary>
    public enum DesignerSymbol
    {
        /// <summary>Spawn point, player start, placement marker.</summary>
        Spawn,

        /// <summary>Editor gizmo, handle, scene-view overlay.</summary>
        Gizmo,

        /// <summary>Camera rig, view, framing.</summary>
        Camera,

        /// <summary>Input binding, control scheme, touch or pointer handling.</summary>
        Input,

        /// <summary>Audio source, mixer, music or SFX definition.</summary>
        Audio,

        /// <summary>Quest, objective, mission step.</summary>
        Quest,

        /// <summary>Item, inventory, loot, equipment.</summary>
        Item,

        /// <summary>Ability, skill, spell.</summary>
        Ability,

        /// <summary>Stat, attribute, modifier curve.</summary>
        Stat,

        /// <summary>AI brain, behaviour tree, decision logic.</summary>
        Ai,

        /// <summary>Level, map, tile or grid layout.</summary>
        Level,

        /// <summary>Networking, replication, session.</summary>
        Network,

        /// <summary>Tutorial, hint, onboarding step.</summary>
        Tutorial,

        /// <summary>UI widget, HUD, panel, menu.</summary>
        Widget,

        /// <summary>Registry, catalog, lookup table.</summary>
        Database,

        /// <summary>Generic authored document: config, settings, profile.</summary>
        Document,

        /// <summary>Generic runtime component with no better shape.</summary>
        Component,

        /// <summary>Character, unit, player, NPC.</summary>
        Character,

        /// <summary>Combat, damage, weapon, defence.</summary>
        Combat,

        /// <summary>Currency, price, shop, reward.</summary>
        Economy,

        /// <summary>Animation clip, pose, rig.</summary>
        Animation,

        /// <summary>VFX, particles, visual effect.</summary>
        Effect,

        /// <summary>Trigger volume, zone, area of effect.</summary>
        Trigger,

        /// <summary>Timer, cooldown, duration, schedule.</summary>
        Timer,

        /// <summary>Tag, label, identifier.</summary>
        Tag,
    }
}
