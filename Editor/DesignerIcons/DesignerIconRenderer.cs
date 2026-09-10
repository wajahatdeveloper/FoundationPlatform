#if UNITY_EDITOR
using UnityEngine;

namespace AetherNexus.FoundationPlatform.DesignerIcons.Editor
{
    /// <summary>
    /// Draws a script icon from a catalog entry: domain-coloured plate, silhouette that separates
    /// assets (folded page) from components (rounded square), and the type's monogram in 5x7
    /// glyphs. Rendered at 4x and box-filtered down so edges are smooth at 16px inspector size.
    /// </summary>
    internal static class DesignerIconRenderer
    {
        internal const int DefaultSize = 64;
        private const int Supersample = 4;

        private const float Margin = 0.07f;
        private const float CornerRadius = 0.16f;
        private const float BorderWidth = 0.035f;
        private const float FoldSize = 0.30f;

        private static readonly Color32 GlyphColor = new Color32(0xF4, 0xF6, 0xF8, 0xFF);

        internal static Texture2D Render(DesignerIconEntry entry, int size)
        {
            Color plate = DesignerIconPalette.ColorOf(entry.Domain, entry.Type);
            Color rim = new Color(plate.r * 0.55f, plate.g * 0.55f, plate.b * 0.55f, 1f);

            int hi = size * Supersample;
            var accum = new Color[size * size];
            string monogram = entry.Monogram;

            for (int py = 0; py < hi; py++)
            {
                float v = (py + 0.5f) / hi;
                for (int px = 0; px < hi; px++)
                {
                    float u = (px + 0.5f) / hi;
                    Color sample = SamplePixel(u, v, entry.IsAsset, plate, rim, monogram);

                    int dst = (py / Supersample) * size + (px / Supersample);
                    accum[dst].r += sample.r * sample.a;
                    accum[dst].g += sample.g * sample.a;
                    accum[dst].b += sample.b * sample.a;
                    accum[dst].a += sample.a;
                }
            }

            float samplesPerPixel = Supersample * Supersample;
            var pixels = new Color32[size * size];
            for (int i = 0; i < accum.Length; i++)
            {
                float alpha = accum[i].a / samplesPerPixel;
                Color color = alpha > 0f
                    ? new Color(accum[i].r / accum[i].a, accum[i].g / accum[i].a, accum[i].b / accum[i].a, alpha)
                    : Color.clear;

                // Texture2D rows run bottom-up; the sampler walks top-down.
                int x = i % size;
                int y = i / size;
                pixels[(size - 1 - y) * size + x] = color;
            }

            var texture = new Texture2D(size, size, TextureFormat.RGBA32, false, true);
            texture.SetPixels32(pixels);
            texture.Apply();
            return texture;
        }

        /// <summary>Colour at normalized (<paramref name="u"/>, <paramref name="v"/>), v measured from the top.</summary>
        private static Color SamplePixel(float u, float v, bool isAsset, Color plate, Color rim, string monogram)
        {
            float distance = PlateDistance(u, v, isAsset);
            if (distance > 0f)
                return Color.clear;

            if (InsideMonogram(u, v, monogram))
                return GlyphColor;

            return distance > -BorderWidth ? rim : plate;
        }

        /// <summary>Signed distance to the plate silhouette: negative inside, positive outside.</summary>
        private static float PlateDistance(float u, float v, bool isAsset)
        {
            float min = Margin;
            float max = 1f - Margin;

            float dx = Mathf.Max(min + CornerRadius - u, u - (max - CornerRadius), 0f);
            float dy = Mathf.Max(min + CornerRadius - v, v - (max - CornerRadius), 0f);
            float rounded = Mathf.Sqrt(dx * dx + dy * dy) - CornerRadius;

            float outside = Mathf.Max(Mathf.Max(min - u, u - max), Mathf.Max(min - v, v - max));
            float distance = Mathf.Max(rounded, outside);

            if (!isAsset)
                return distance;

            // Page fold: clip the top-right corner along a diagonal so assets read differently
            // from components even at 16px.
            float foldX = u - (max - FoldSize);
            float foldY = (min + FoldSize) - v;
            float fold = (foldX + foldY - FoldSize) * 0.70710678f;
            return Mathf.Max(distance, fold);
        }

        private static bool InsideMonogram(float u, float v, string monogram)
        {
            int columns = monogram.Length * DesignerIconGlyphs.Width + (monogram.Length - 1);
            float plateSpan = 1f - 2f * Margin;
            float unit = Mathf.Min(plateSpan * 0.68f / columns, plateSpan * 0.52f / DesignerIconGlyphs.Height);

            float blockWidth = columns * unit;
            float blockHeight = DesignerIconGlyphs.Height * unit;
            float originX = 0.5f - blockWidth * 0.5f;
            float originY = 0.5f - blockHeight * 0.5f;

            int cellX = Mathf.FloorToInt((u - originX) / unit);
            int cellY = Mathf.FloorToInt((v - originY) / unit);
            if (cellX < 0 || cellX >= columns || cellY < 0 || cellY >= DesignerIconGlyphs.Height)
                return false;

            int stride = DesignerIconGlyphs.Width + 1;
            int glyphIndex = cellX / stride;
            int glyphX = cellX - glyphIndex * stride;
            if (glyphX >= DesignerIconGlyphs.Width)
                return false;

            return DesignerIconGlyphs.Pixel(monogram[glyphIndex], glyphX, cellY);
        }
    }
}
#endif
