using UnityEngine;

namespace AetherNexus.FoundationPlatform.Extensions
{
public static class RendererExtensions
{

    /// <summary>
    /// Sets the alpha channel for the SpriteRenderer's color
    /// </summary>
    /// <param name="spriteRenderer">SpriteRenderer to operate with.</param>
    /// <param name="value">Alpha channel value.</param>
    public static void SetAlpha(this SpriteRenderer spriteRenderer, float value)
    {
        spriteRenderer.color = spriteRenderer.color.WithA(value);
    }

    /// <summary>
    /// Set's alpha channel for the Material `_Color` property
    /// </summary>
    /// <param name="material">Material to operate with.</param>
    /// <param name="value">Alpha channel value.</param>
    public static void SetAlpha(this Material material, float value)
    {
        if (material.HasProperty("_Color"))
        {
            var color = material.color;
            color.a = value;
            material.color = color;
        }
    }
}
}

