#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using AetherNexus.FoundationPlatform.Utilities.Menus;

namespace AetherNexus.FoundationPlatform.DesignerIcons.Editor
{
    /// <summary>
    /// The 10x10 mark for each <see cref="DesignerSymbol"/>. Authored by hand on the same grid the
    /// icon is drawn at, with strokes two pixels wide wherever the shape allows, so nothing thins
    /// out to a grey smear at 16px. Rows read top-down; <c>#</c> is ink.
    /// </summary>
    internal static class DesignerIconSymbols
    {
        internal const int Size = 10;

        private static readonly Dictionary<DesignerSymbol, string[]> Masks = new Dictionary<DesignerSymbol, string[]>
        {
            // Flag on a pole.
            { DesignerSymbol.Spawn, new[]
            {
                "##########",
                "##......##",
                "##......##",
                "##########",
                "##........",
                "##........",
                "##........",
                "##........",
                "##........",
                "####......",
            } },

            // Move handle: bold crosshair.
            { DesignerSymbol.Gizmo, new[]
            {
                "....##....",
                "....##....",
                "..######..",
                "....##....",
                "##########",
                "##########",
                "....##....",
                "..######..",
                "....##....",
                "....##....",
            } },

            // Camera body with a lens.
            { DesignerSymbol.Camera, new[]
            {
                "..........",
                "..####....",
                ".##..##...",
                "##########",
                "##......##",
                "##.####.##",
                "##.#..#.##",
                "##.####.##",
                "##########",
                "..........",
            } },

            // Pointer.
            { DesignerSymbol.Input, new[]
            {
                "##........",
                "####......",
                "#####.....",
                "######....",
                "#######...",
                "########..",
                "#####.....",
                "##.###....",
                "....###...",
                ".....##...",
            } },

            // Speaker with waves.
            { DesignerSymbol.Audio, new[]
            {
                "...##.....",
                "..###.....",
                ".####..#..",
                "#####.#.#.",
                "#####.#.#.",
                "#####.#.#.",
                "#####.#.#.",
                ".####..#..",
                "..###.....",
                "...##.....",
            } },

            // Objective marker: exclamation.
            { DesignerSymbol.Quest, new[]
            {
                "...####...",
                "...####...",
                "...####...",
                "...####...",
                "...####...",
                "....##....",
                "..........",
                "...####...",
                "...####...",
                "..........",
            } },

            // Chest.
            { DesignerSymbol.Item, new[]
            {
                "..........",
                ".########.",
                ".#..##..#.",
                ".########.",
                ".##....##.",
                ".##....##.",
                ".##.##.##.",
                ".##....##.",
                ".########.",
                "..........",
            } },

            // Bolt.
            { DesignerSymbol.Ability, new[]
            {
                ".....###..",
                "....###...",
                "...###....",
                "..######..",
                ".....###..",
                "....###...",
                "...###....",
                "..###.....",
                ".###......",
                "##........",
            } },

            // Bar chart.
            { DesignerSymbol.Stat, new[]
            {
                "..........",
                ".......##.",
                ".......##.",
                "....##.##.",
                "....##.##.",
                ".##.##.##.",
                ".##.##.##.",
                ".##.##.##.",
                ".##.##.##.",
                "##########",
            } },

            // Chip with pins.
            { DesignerSymbol.Ai, new[]
            {
                "..##..##..",
                ".########.",
                ".##....##.",
                ".##.##.##.",
                ".##.##.##.",
                ".##.##.##.",
                ".##....##.",
                ".########.",
                "..##..##..",
                "..........",
            } },

            // Tile grid.
            { DesignerSymbol.Level, new[]
            {
                "##########",
                "##..##..##",
                "##..##..##",
                "##########",
                "##..##..##",
                "##..##..##",
                "##########",
                "##..##..##",
                "##..##..##",
                "##########",
            } },

            // Four nodes wired to a hub.
            { DesignerSymbol.Network, new[]
            {
                "###....###",
                "#.#....#.#",
                "###....###",
                "..#....#..",
                "..######..",
                "..######..",
                "..#....#..",
                "###....###",
                "#.#....#.#",
                "###....###",
            } },

            // Question mark.
            { DesignerSymbol.Tutorial, new[]
            {
                "..######..",
                ".##....##.",
                "##......##",
                "......###.",
                ".....###..",
                "....###...",
                "....##....",
                "..........",
                "....##....",
                "....##....",
            } },

            // Window with a title bar.
            { DesignerSymbol.Widget, new[]
            {
                "..........",
                "##########",
                "##########",
                "##......##",
                "##.####.##",
                "##......##",
                "##.####.##",
                "##......##",
                "##########",
                "..........",
            } },

            // Stacked cylinder.
            { DesignerSymbol.Database, new[]
            {
                ".########.",
                "##......##",
                ".########.",
                ".##....##.",
                ".########.",
                ".##....##.",
                ".##....##.",
                ".########.",
                "##......##",
                ".########.",
            } },

            // Page with lines.
            { DesignerSymbol.Document, new[]
            {
                ".########.",
                ".#......#.",
                ".#.####.#.",
                ".#......#.",
                ".#.####.#.",
                ".#......#.",
                ".#.####.#.",
                ".#......#.",
                ".########.",
                "..........",
            } },

            // Rounded block.
            { DesignerSymbol.Component, new[]
            {
                "..######..",
                ".##....##.",
                "##########",
                "##......##",
                "##......##",
                "##......##",
                "##......##",
                "##########",
                ".##....##.",
                "..######..",
            } },

            // Person.
            { DesignerSymbol.Character, new[]
            {
                "...####...",
                "...####...",
                "..######..",
                "##########",
                "##########",
                "...####...",
                "...####...",
                "..##..##..",
                "..##..##..",
                ".##....##.",
            } },

            // Shield.
            { DesignerSymbol.Combat, new[]
            {
                "##########",
                "##########",
                "##......##",
                "##......##",
                ".##....##.",
                ".##....##.",
                "..##..##..",
                "..######..",
                "...####...",
                "....##....",
            } },

            // Coin.
            { DesignerSymbol.Economy, new[]
            {
                "..######..",
                ".##....##.",
                "##..##..##",
                "##..##..##",
                "##..##..##",
                "##..##..##",
                "##..##..##",
                "##..##..##",
                ".##....##.",
                "..######..",
            } },

            // Keyframe on a track.
            { DesignerSymbol.Animation, new[]
            {
                "....##....",
                "...####...",
                "..######..",
                ".########.",
                "..######..",
                "...####...",
                "....##....",
                "..........",
                "##########",
                "##########",
            } },

            // Sparkle.
            { DesignerSymbol.Effect, new[]
            {
                "....##....",
                "....##....",
                "..######..",
                "##########",
                ".########.",
                "..######..",
                ".##.##.##.",
                "##..##..##",
                "....##....",
                "....##....",
            } },

            // Zone corners.
            { DesignerSymbol.Trigger, new[]
            {
                "####..####",
                "##......##",
                "#........#",
                "..........",
                "..........",
                "..........",
                "..........",
                "#........#",
                "##......##",
                "####..####",
            } },

            // Clock.
            { DesignerSymbol.Timer, new[]
            {
                "..######..",
                ".##....##.",
                "##..##..##",
                "##..##..##",
                "##..#####.",
                "##......##",
                "##......##",
                ".##....##.",
                "..######..",
                "..........",
            } },

            // Price tag, point to the left.
            { DesignerSymbol.Tag, new[]
            {
                "...#######",
                "..########",
                ".###....##",
                "###.....##",
                "##..##..##",
                "##..##..##",
                "###.....##",
                ".###....##",
                "..########",
                "...#######",
            } },
        };

        internal static string[] Mask(DesignerSymbol symbol)
        {
            if (!Masks.TryGetValue(symbol, out string[] rows))
                throw new InvalidOperationException(
                    $"Designer symbol '{symbol}' has no {Size}x{Size} mask. Add one to {nameof(DesignerIconSymbols)}.");

            return rows;
        }

        /// <summary>Every symbol that has a mask, for the audit report's legend.</summary>
        internal static IEnumerable<DesignerSymbol> All => Masks.Keys;
    }
}
#endif
