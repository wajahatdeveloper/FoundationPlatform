using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public static class RenderTextureExtensions
{
    /// <summary>
    /// Create texture and write <paramref name="renderTexture"/> to it.
    /// </summary>
    /// <param name="renderTexture">The render texture.</param>
    /// <returns>Created texture.</returns>
    public static Texture2D ToTexture2D(this RenderTexture renderTexture, TextureFormat format)
    {
        var texture = new Texture2D(renderTexture.width, renderTexture.height, format, false);
        renderTexture.WriteToTexture2D(texture);

        return texture;
    }

    /// <summary>
    /// Write <paramref name="renderTexture"/> to <paramref name="texture"/>.
    /// </summary>
    /// <param name="renderTexture">The render texture.</param>
    /// <param name="texture">Texture to write render texture.</param>
    public static void WriteToTexture2D(this RenderTexture renderTexture, Texture2D texture)
    {
        var oldRenderTexture = RenderTexture.active;
        RenderTexture.active = renderTexture;

        texture.ReadPixels(new Rect(0, 0, renderTexture.width, renderTexture.height), 0, 0);
        texture.Apply();

        RenderTexture.active = oldRenderTexture;
    }

    /// <summary>
    /// Create sprite and write <paramref name="renderTexture"/> to it.
    /// </summary>
    /// <param name="renderTexture">The render texture.</param>
    /// <returns>Created sprite.</returns>
    public static Sprite ToSprite(this RenderTexture renderTexture, TextureFormat format)
    {
        var texture = renderTexture.ToTexture2D(format);
        return Sprite.Create(texture, new Rect(0f, 0f, texture.width, texture.height), new Vector2(0.5f, 0.5f));
    }

    /// <summary>
    /// Write <paramref name="renderTexture"/> to <paramref name="sprite"/>.
    /// </summary>
    /// <param name="renderTexture">The render texture.</param>
    /// <param name="sprite">Sprite to write render texture.</param>
    public static void WriteToSprite(this RenderTexture renderTexture, Sprite sprite)
    {
        // sprite.texture is the underlying source Texture2D. For an atlased sprite this is a
        // SHARED atlas: ReadPixels/Apply into it would corrupt neighbouring sprites, and it must
        // be CPU-readable or ReadPixels throws. There is no safe way to replace just one sprite's
        // pixels inside a shared atlas here (sprite.texture is read-only and cannot be reassigned),
        // so this operation is contractually limited to standalone sprites that own their entire,
        // readable texture. Fail fast otherwise instead of silently corrupting unrelated sprites.
        var texture = sprite.texture;
        var rect = sprite.textureRect;
        var occupiesWholeTexture =
            Mathf.RoundToInt(rect.x) == 0 && Mathf.RoundToInt(rect.y) == 0 &&
            Mathf.RoundToInt(rect.width) == texture.width && Mathf.RoundToInt(rect.height) == texture.height;
        if (!occupiesWholeTexture)
        {
            throw new System.InvalidOperationException(
                "WriteToSprite only supports standalone sprites that own their whole texture. Sprite '" +
                sprite.name + "' occupies textureRect " + rect + " of a " + texture.width + "x" +
                texture.height + " texture (atlased); writing would corrupt other sprites in the atlas.");
        }
        if (!texture.isReadable)
        {
            throw new System.InvalidOperationException(
                "WriteToSprite requires sprite '" + sprite.name + "' to use a CPU-readable texture " +
                "(enable Read/Write in the import settings).");
        }

        var oldRenderTexture = RenderTexture.active;
        RenderTexture.active = renderTexture;

        // Standalone sprite: textureRect origin is (0,0), so read the render texture straight into
        // the destination texture origin. Source rect is in render-texture pixel space.
        texture.ReadPixels(new Rect(0f, 0f, renderTexture.width, renderTexture.height), 0, 0);
        texture.Apply();

        RenderTexture.active = oldRenderTexture;
    }
}