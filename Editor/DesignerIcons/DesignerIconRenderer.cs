#if UNITY_EDITOR
using System;
using UnityEngine;

namespace AetherNexus.FoundationPlatform.DesignerIcons.Editor
{
    /// <summary>
    /// Draws a script icon on a 16x16 authoring grid and emits it at an integer multiple of that
    /// grid. Unity draws these at 16px in the Inspector header and the Project rows, so every edge
    /// is authored on the pixel the designer actually sees; the emitted 64px PNG is a nearest-4x
    /// copy, which Unity's own box downsample turns back into the authored pixels exactly.
    /// <para>Layers, back to front: domain plate (component square / asset folded page), 1px rim,
    /// 1px halo behind the mark, then the mark itself — a symbol when the type declares one, the
    /// type's initial otherwise.</para>
    /// </summary>
    internal static class DesignerIconRenderer
    {
        internal const int DefaultSize = 64;
        internal const int Grid = 16;

        /// <summary>Plate bounds on the grid, inclusive: a 1px gutter on every side.</summary>
        private const int PlateMin = 1;
        private const int PlateMax = Grid - 2;
        private const int CornerRadius = 2;

        /// <summary>Width of the diagonal that cuts the top-right corner off an asset plate.</summary>
        private const int FoldSize = 4;

        private const int MarkOrigin = 3;

        private static readonly Color32 LightInk = new Color32(0xF6, 0xF8, 0xFA, 0xFF);
        private static readonly Color32 DarkInk = new Color32(0x14, 0x18, 0x1C, 0xFF);

        internal static Texture2D Render(DesignerIconEntry entry, int size)
        {
            if (size % Grid != 0)
                throw new ArgumentException(
                    $"Designer icons are authored on a {Grid}px grid, so the output size must be a multiple of {Grid}. Got {size}.",
                    nameof(size));

            Color32[] cells = Compose(entry);
            int scale = size / Grid;
            var pixels = new Color32[size * size];

            for (int y = 0; y < size; y++)
            {
                // Texture2D rows run bottom-up; the grid is authored top-down.
                int row = (size - 1 - y) * size;
                int cellRow = (y / scale) * Grid;

                for (int x = 0; x < size; x++)
                    pixels[row + x] = cells[cellRow + (x / scale)];
            }

            var texture = new Texture2D(size, size, TextureFormat.RGBA32, false, true);
            texture.SetPixels32(pixels);
            texture.Apply();
            return texture;
        }

        /// <summary>The authored 16x16 grid, row-major from the top.</summary>
        private static Color32[] Compose(DesignerIconEntry entry)
        {
            Color32 plate = DesignerIconPalette.ColorOf(entry.Domain, entry.Type);
            Color32 rim = Scale(plate, 0.52f);
            bool darkPlate = Luminance(plate) < 0.55f;
            Color32 ink = darkPlate ? LightInk : DarkInk;
            Color32 halo = darkPlate ? Scale(plate, 0.30f) : Lerp(plate, LightInk, 0.62f);

            bool[] solid = BuildPlate(entry.IsAsset);
            bool[] mark = BuildMark(entry);
            bool[] glow = BuildHalo(mark);

            var cells = new Color32[Grid * Grid];
            for (int i = 0; i < cells.Length; i++)
            {
                if (!solid[i])
                {
                    cells[i] = new Color32(0, 0, 0, 0);
                    continue;
                }

                if (mark[i])
                    cells[i] = ink;
                else if (glow[i])
                    cells[i] = halo;
                else
                    cells[i] = IsEdge(solid, i) ? rim : plate;
            }

            return cells;
        }

        private static bool[] BuildPlate(bool isAsset)
        {
            var solid = new bool[Grid * Grid];

            for (int y = PlateMin; y <= PlateMax; y++)
            {
                for (int x = PlateMin; x <= PlateMax; x++)
                {
                    if (!InsideCorners(x, y))
                        continue;

                    // The folded top-right corner is what separates an asset from a component at
                    // 16px, where the plate colour alone carries no shape information.
                    if (isAsset && x - y >= PlateMax - FoldSize - PlateMin + 1)
                        continue;

                    solid[y * Grid + x] = true;
                }
            }

            return solid;
        }

        private static bool InsideCorners(int x, int y)
        {
            float cx = x + 0.5f;
            float cy = y + 0.5f;

            float nearX = cx < PlateMin + CornerRadius
                ? PlateMin + CornerRadius
                : (cx > PlateMax + 1 - CornerRadius ? PlateMax + 1 - CornerRadius : cx);
            float nearY = cy < PlateMin + CornerRadius
                ? PlateMin + CornerRadius
                : (cy > PlateMax + 1 - CornerRadius ? PlateMax + 1 - CornerRadius : cy);

            float dx = cx - nearX;
            float dy = cy - nearY;
            return dx * dx + dy * dy <= CornerRadius * CornerRadius;
        }

        private static bool[] BuildMark(DesignerIconEntry entry)
        {
            // The fold eats the top-right of an asset plate, so its mark sits one row lower.
            int originY = entry.IsAsset ? MarkOrigin + 1 : MarkOrigin;

            if (entry.Symbol.HasValue)
                return Stamp(DesignerIconSymbols.Mask(entry.Symbol.Value), DesignerIconSymbols.Size, DesignerIconSymbols.Size, MarkOrigin, originY);

            return Stamp(
                DesignerIconLetters.Rows(entry.Letter),
                DesignerIconLetters.Width,
                DesignerIconLetters.Height,
                MarkOrigin + (DesignerIconSymbols.Size - DesignerIconLetters.Width) / 2,
                originY);
        }

        private static bool[] Stamp(string[] rows, int width, int height, int originX, int originY)
        {
            var mark = new bool[Grid * Grid];

            for (int y = 0; y < height; y++)
            {
                string row = rows[y];
                for (int x = 0; x < width; x++)
                {
                    if (row[x] != '#')
                        continue;

                    mark[(originY + y) * Grid + (originX + x)] = true;
                }
            }

            return mark;
        }

        /// <summary>Cells touching the mark, so it keeps its contrast on light plates too.</summary>
        private static bool[] BuildHalo(bool[] mark)
        {
            var glow = new bool[Grid * Grid];

            for (int y = 0; y < Grid; y++)
            {
                for (int x = 0; x < Grid; x++)
                {
                    if (mark[y * Grid + x] || !TouchesMark(mark, x, y))
                        continue;

                    glow[y * Grid + x] = true;
                }
            }

            return glow;
        }

        private static bool TouchesMark(bool[] mark, int x, int y)
        {
            for (int dy = -1; dy <= 1; dy++)
            {
                for (int dx = -1; dx <= 1; dx++)
                {
                    int nx = x + dx;
                    int ny = y + dy;
                    if (nx < 0 || nx >= Grid || ny < 0 || ny >= Grid)
                        continue;

                    if (mark[ny * Grid + nx])
                        return true;
                }
            }

            return false;
        }

        private static bool IsEdge(bool[] solid, int index)
        {
            int x = index % Grid;
            int y = index / Grid;

            return !Filled(solid, x - 1, y) || !Filled(solid, x + 1, y) ||
                   !Filled(solid, x, y - 1) || !Filled(solid, x, y + 1) ||
                   !Filled(solid, x - 1, y - 1) || !Filled(solid, x + 1, y - 1) ||
                   !Filled(solid, x - 1, y + 1) || !Filled(solid, x + 1, y + 1);
        }

        private static bool Filled(bool[] solid, int x, int y)
        {
            if (x < 0 || x >= Grid || y < 0 || y >= Grid)
                return false;

            return solid[y * Grid + x];
        }

        private static float Luminance(Color32 color)
        {
            return (0.2126f * color.r + 0.7152f * color.g + 0.0722f * color.b) / 255f;
        }

        private static Color32 Scale(Color32 color, float factor)
        {
            return new Color32(
                (byte)Mathf.RoundToInt(color.r * factor),
                (byte)Mathf.RoundToInt(color.g * factor),
                (byte)Mathf.RoundToInt(color.b * factor),
                255);
        }

        private static Color32 Lerp(Color32 from, Color32 to, float t)
        {
            return new Color32(
                (byte)Mathf.RoundToInt(Mathf.Lerp(from.r, to.r, t)),
                (byte)Mathf.RoundToInt(Mathf.Lerp(from.g, to.g, t)),
                (byte)Mathf.RoundToInt(Mathf.Lerp(from.b, to.b, t)),
                255);
        }
    }
}
#endif
