using UnityEngine;

// ReSharper disable once InconsistentNaming
namespace AetherNexus.FoundationPlatform.SupportTypes
{
public struct HSL
{
    public float H;
    public float S;
    public float L;

    public Color ToColor()
    {
        float r, g, b;

        if (S == 0)
        {
            r = g = b = L; // achromatic
        }
        else
        {
            float q = L < 0.5f ? L * (1f + S) : L + S - L * S;
            float p = 2f * L - q;
            r = Hue2Rgb(p, q, H + 1f / 3f);
            g = Hue2Rgb(p, q, H);
            b = Hue2Rgb(p, q, H - 1f / 3f);
        }

        return new Color(r, g, b, 1f);
    }

    public static HSL FromColor(Color color)
    {
        var max = Mathf.Max(color.r, color.g, color.b);
        var min = Mathf.Min(color.r, color.g, color.b);
        float h, s, l = (max + min) / 2f;

        if (max == min)
        {
            h = s = 0; // achromatic
        }
        else
        {
            var d = max - min;
            s = l > 0.5f ? d / (2f - max - min) : d / (max + min);
            if (max == color.r)
                h = (color.g - color.b) / d + (color.g < color.b ? 6f : 0f);
            else if (max == color.g)
                h = (color.b - color.r) / d + 2f;
            else
                h = (color.r - color.g) / d + 4f;

            h /= 6f;
        }

        return new HSL { H = h, S = s, L = l };
    }

    private float Hue2Rgb(float p, float q, float t)
    {
        if (t < 0f) t += 1f;
        if (t > 1f) t -= 1f;
        if (t < 1f / 6f) return p + (q - p) * 6f * t;
        if (t < 1f / 2f) return q;
        if (t < 2f / 3f) return p + (q - p) * (2f / 3f - t) * 6f;
        return p;
    }
}}
