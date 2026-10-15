using System;
using UnityEngine;

namespace AetherNexus.FoundationPlatform.Extensions
{
public static class Texture2DExtensions
{
    public static Texture2D Clone(this Texture2D @this)
    {
        var newTex = new Texture2D(@this.width, @this.height);
        Color[] colors = @this.GetPixels(0, 0, @this.width, @this.height);
        newTex.SetPixels(colors);
        newTex.Apply();
        return newTex;
    }

    /// <summary>
    /// Rotates <see cref="Texture2D"/> pixels to a specified angle.
    /// </summary>
    /// <param name="tex">Source texture to rotate.</param>
    /// <param name="angle">Rotate angle.</param>
    public static Texture2D Rotate(this Texture2D tex, float angle)
    {
        var rotImage = new Texture2D(tex.width, tex.height);
        int x;

        var w = tex.width;
        var h = tex.height;
        var x0 = rot_x(angle, -w / 2.0f, -h / 2.0f) + w / 2.0f;
        var y0 = rot_y(angle, -w / 2.0f, -h / 2.0f) + h / 2.0f;

        var dxX = rot_x(angle, 1.0f, 0.0f);
        var dxY = rot_y(angle, 1.0f, 0.0f);
        var dyX = rot_x(angle, 0.0f, 1.0f);
        var dyY = rot_y(angle, 0.0f, 1.0f);

        var x1 = x0;
        var y1 = y0;

        for (x = 0; x < tex.width; x++)
        {
            var x2 = x1;
            var y2 = y1;
            int y;
            for (y = 0; y < tex.height; y++)
            {
                x2 += dxX;
                y2 += dxY;
                rotImage.SetPixel((int)Mathf.Floor(x), (int)Mathf.Floor(y), GetPixel(tex, x2, y2));
            }

            x1 += dyX;
            y1 += dyY;
        }

        rotImage.Apply();
        return rotImage;
    }

    /// <summary>
    /// Resize Texture2D.
    /// </summary>
    /// <param name="source">Source texture to resize.</param>
    /// <param name="newWidth">New texture width.</param>
    /// <param name="newHeight">New texture height. </param>
    /// <param name="filterMode">The filtering mode to use during resize.</param>
    /// <returns></returns>
    public static Texture2D Resize(this Texture2D source, int newWidth, int newHeight, FilterMode filterMode)
    {
        source.filterMode = filterMode;
        var rt = RenderTexture.GetTemporary(newWidth, newHeight);
        rt.filterMode = FilterMode.Point;
        RenderTexture.active = rt;
        Graphics.Blit(source, rt);
        var nTex = new Texture2D(newWidth, newHeight);
        nTex.ReadPixels(new Rect(0, 0, newWidth, newHeight), 0, 0);
        nTex.Apply();
        RenderTexture.active = null;
        return nTex;
    }

    static Color GetPixel(Texture2D tex, float x, float y)
    {
        Color pix;
        var x1 = (int)Mathf.Floor(x);
        var y1 = (int)Mathf.Floor(y);

        if (x1 >= tex.width || x1 < 0 ||
            y1 >= tex.height || y1 < 0)
            pix = Color.clear;
        else
            pix = tex.GetPixel(x1, y1);

        return pix;
    }

    static float rot_x(float angle, float x, float y)
    {
        var cos = Mathf.Cos(angle / 180.0f * Mathf.PI);
        var sin = Mathf.Sin(angle / 180.0f * Mathf.PI);
        return x * cos + y * -sin;
    }

    static float rot_y(float angle, float x, float y)
    {
        var cos = Mathf.Cos(angle / 180.0f * Mathf.PI);
        var sin = Mathf.Sin(angle / 180.0f * Mathf.PI);
        return x * sin + y * cos;
    }
}}
