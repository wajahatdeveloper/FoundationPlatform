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
    /// ??????????,????
    /// </summary>
    public static Texture2D ScaleTextureBilinear(this Texture2D @this, float scaleFactor)
    {
        Texture2D newTexture = new Texture2D(Mathf.CeilToInt(@this.width * scaleFactor),
            Mathf.CeilToInt(@this.height * scaleFactor));
        float scale = 1.0f / scaleFactor;
        int maxX = @this.width - 1;
        int maxY = @this.height - 1;
        for (int y = 0; y < newTexture.height; y++)
        {
            for (int x = 0; x < newTexture.width; x++)
            {
                float targetX = x * scale;
                float targetY = y * scale;
                int x1 = Mathf.Min(maxX, Mathf.FloorToInt(targetX));
                int y1 = Mathf.Min(maxY, Mathf.FloorToInt(targetY));
                int x2 = Mathf.Min(maxX, x1 + 1);
                int y2 = Mathf.Min(maxY, y1 + 1);

                float u = targetX - x1;
                float v = targetY - y1;
                float w1 = (1 - u) * (1 - v);
                float w2 = u * (1 - v);
                float w3 = (1 - u) * v;
                float w4 = u * v;
                Color color1 = @this.GetPixel(x1, y1);
                Color color2 = @this.GetPixel(x2, y1);
                Color color3 = @this.GetPixel(x1, y2);
                Color color4 = @this.GetPixel(x2, y2);
                Color color = new Color(Mathf.Clamp01(color1.r * w1 + color2.r * w2 + color3.r * w3 + color4.r * w4),
                    Mathf.Clamp01(color1.g * w1 + color2.g * w2 + color3.g * w3 + color4.g * w4),
                    Mathf.Clamp01(color1.b * w1 + color2.b * w2 + color3.b * w3 + color4.b * w4),
                    Mathf.Clamp01(color1.a * w1 + color2.a * w2 + color3.a * w3 + color4.a * w4)
                );
                newTexture.SetPixel(x, y, color);
            }
        }

        newTexture.Apply();
        return newTexture;
    }

    /// <summary>
    /// ???????????????
    /// </summary>
    public static Texture2D SizeTextureBilinear(this Texture2D @this, Vector2 size)
    {
        Texture2D newTexture = new Texture2D(Mathf.CeilToInt(size.x), Mathf.CeilToInt(size.y));
        float scaleX = @this.width / size.x;
        float scaleY = @this.height / size.y;
        int maxX = @this.width - 1;
        int maxY = @this.height - 1;
        for (int y = 0; y < newTexture.height; y++)
        {
            for (int x = 0; x < newTexture.width; x++)
            {
                float targetX = x * scaleX;
                float targetY = y * scaleY;
                int x1 = Mathf.Min(maxX, Mathf.FloorToInt(targetX));
                int y1 = Mathf.Min(maxY, Mathf.FloorToInt(targetY));
                int x2 = Mathf.Min(maxX, x1 + 1);
                int y2 = Mathf.Min(maxY, y1 + 1);

                float u = targetX - x1;
                float v = targetY - y1;
                float w1 = (1 - u) * (1 - v);
                float w2 = u * (1 - v);
                float w3 = (1 - u) * v;
                float w4 = u * v;
                Color color1 = @this.GetPixel(x1, y1);
                Color color2 = @this.GetPixel(x2, y1);
                Color color3 = @this.GetPixel(x1, y2);
                Color color4 = @this.GetPixel(x2, y2);
                Color color = new Color(Mathf.Clamp01(color1.r * w1 + color2.r * w2 + color3.r * w3 + color4.r * w4),
                    Mathf.Clamp01(color1.g * w1 + color2.g * w2 + color3.g * w3 + color4.g * w4),
                    Mathf.Clamp01(color1.b * w1 + color2.b * w2 + color3.b * w3 + color4.b * w4),
                    Mathf.Clamp01(color1.a * w1 + color2.a * w2 + color3.a * w3 + color4.a * w4)
                );
                newTexture.SetPixel(x, y, color);
            }
        }

        newTexture.Apply();
        return newTexture;
    }

    /// <summary>
    /// Texture??
    /// </summary>
    public static Texture2D RotateTexture(this Texture2D @this, float eulerAngles)
    {
        int x;
        int y;
        int i;
        int j;
        float phi = eulerAngles / (180 / Mathf.PI);
        float sn = Mathf.Sin(phi);
        float cs = Mathf.Cos(phi);
        Color32[] arr = @this.GetPixels32();
        Color32[] arr2 = new Color32[arr.Length];
        int W = @this.width;
        int H = @this.height;
        int xc = W / 2;
        int yc = H / 2;

        for (j = 0; j < H; j++)
        {
            for (i = 0; i < W; i++)
            {
                arr2[j * W + i] = new Color32(0, 0, 0, 0);

                x = (int)(cs * (i - xc) + sn * (j - yc) + xc);
                y = (int)(-sn * (i - xc) + cs * (j - yc) + yc);

                if ((x > -1) && (x < W) && (y > -1) && (y < H))
                {
                    arr2[j * W + i] = arr[y * W + x];
                }
            }
        }

        Texture2D newImg = new Texture2D(W, H);
        newImg.SetPixels32(arr2);
        newImg.Apply();

        return newImg;
    }

    /// <summary>
    ///??texture?texture2d;
    /// </summary>
    public static Texture2D ToTexture2D(this Texture @this)
    {
        return Texture2D.CreateExternalTexture(
            @this.width,
            @this.height,
            TextureFormat.RGB24,
            false, false,
            @this.GetNativeTexturePtr());
    }


    /// <summary>
    /// Create new sprite out of Texture
    /// </summary>
    public static Sprite AsSprite(this Texture2D texture)
    {
        var rect = new Rect(0, 0, texture.width, texture.height);
        var pivot = new Vector2(0.5f, 0.5f);
        return Sprite.Create(texture, rect, pivot);
    }

    /// <summary>
    /// Change texture size (and scale accordingly)
    /// </summary>
    public static Texture2D Resample(this Texture2D source, int targetWidth, int targetHeight)
    {
        int sourceWidth = source.width;
        int sourceHeight = source.height;
        float sourceAspect = (float)sourceWidth / sourceHeight;
        float targetAspect = (float)targetWidth / targetHeight;

        int xOffset = 0;
        int yOffset = 0;
        float factor;

        if (sourceAspect > targetAspect)
        {
            // crop width
            factor = (float)targetHeight / sourceHeight;
            xOffset = (int)((sourceWidth - sourceHeight * targetAspect) * 0.5f);
        }
        else
        {
            // crop height
            factor = (float)targetWidth / sourceWidth;
            yOffset = (int)((sourceHeight - sourceWidth / targetAspect) * 0.5f);
        }

        var data = source.GetPixels32();
        var data2 = new Color32[targetWidth * targetHeight];
        for (int y = 0; y < targetHeight; y++)
        {
            for (int x = 0; x < targetWidth; x++)
            {
                var p = new Vector2(Mathf.Clamp(xOffset + x / factor, 0, sourceWidth - 1),
                    Mathf.Clamp(yOffset + y / factor, 0, sourceHeight - 1));
                // bilinear filtering
                var c11 = data[Mathf.FloorToInt(p.x) + sourceWidth * (Mathf.FloorToInt(p.y))];
                var c12 = data[Mathf.FloorToInt(p.x) + sourceWidth * (Mathf.CeilToInt(p.y))];
                var c21 = data[Mathf.CeilToInt(p.x) + sourceWidth * (Mathf.FloorToInt(p.y))];
                var c22 = data[Mathf.CeilToInt(p.x) + sourceWidth * (Mathf.CeilToInt(p.y))];

                data2[x + y * targetWidth] = Color.Lerp(Color.Lerp(c11, c12, p.y), Color.Lerp(c21, c22, p.y), p.x);
            }
        }

        var tex = new Texture2D(targetWidth, targetHeight);
        tex.SetPixels32(data2);
        tex.Apply(true);
        return tex;
    }

    /// <summary>
    /// Will texture with solid color
    /// </summary>
    public static Texture2D WithSolidColor(this Texture2D original, Color color)
    {
        var target = new Texture2D(original.width, original.height);
        for (int i = 0; i < target.width; i++)
        {
            for (int j = 0; j < target.height; j++)
            {
                target.SetPixel(i, j, color);
            }
        }

        target.Apply();

        return target;
    }

    /// <summary>
    /// sets a 1 pixel border of the texture on all mipmap levels to the clear color
    /// </summary>
    /// <param name="texture"></param>
    /// <param name="clearColor"> </param>
    /// <param name="makeNoLongerReadable"> </param>
    public static void ClearMipMapBorders(this Texture2D texture, Color clearColor, bool makeNoLongerReadable)
    {
        var mipCount = texture.mipmapCount;

        // In general case, mip level size is mipWidth=max(1,width>>miplevel) and similarly for height.

        var width = texture.width;
        var height = texture.height;
        // tint each mip level
        for (var mip = 1; mip < mipCount; ++mip)
        {
            var mipWidth = Mathf.Max(1, width >> mip);
            var mipHeight = Mathf.Max(1, height >> mip);
            if (mipWidth <= 2) continue; //don't change mip levels below 2x2
            var xCols = new Color[mipWidth];
            var yCols = new Color[mipHeight];
            if (clearColor != default(Color)) //speedup.
            {
                for (var x = 0; x < xCols.Length; ++x)
                {
                    xCols[x] = clearColor;
                }

                for (var y = 0; y < yCols.Length; ++y)
                {
                    yCols[y] = clearColor;
                }
            }

            texture.SetPixels(0, 0, mipWidth, 1, xCols, mip); //set the top edge colors
            texture.SetPixels(0, 0, 1, mipHeight, yCols, mip); //set the left edge colors
            texture.SetPixels(0, mipHeight - 1, mipWidth, 1, xCols, mip); //set the bottom edge colors
            texture.SetPixels(mipWidth - 1, 0, 1, mipHeight, yCols, mip); //set the right edge colors
        }

        // actually apply all SetPixels, don't recalculate mip levels
        texture.Apply(false, makeNoLongerReadable);
    }

    /// <summary>Sets a 1 pixel border of the texture on all mipmap levels to the clear color, keeping it readable.</summary>
    public static void ClearMipMapBorders(this Texture2D texture, Color clearColor) => ClearMipMapBorders(texture, clearColor, false);

    /// <summary>
    /// sets a 1 pixel border of the texture on all mipmap levels to clear white
    /// </summary>
    /// <param name="texture"></param>
    /// <param name="makeNoLongerReadable"></param>
    public static void ClearMipMapBorders(this Texture2D texture, bool makeNoLongerReadable)
    {
        var clear = new Color(1, 1, 1, 0);
        ClearMipMapBorders(texture, clear, makeNoLongerReadable);
    }

    /// <summary>Sets a 1 pixel border of the texture on all mipmap levels to clear white, keeping it readable.</summary>
    public static void ClearMipMapBorders(this Texture2D texture) => ClearMipMapBorders(texture, false);

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

    /// <summary>
    /// Texture scale mode options.
    /// </summary>
    public enum TextureScaleMode
    {
        /// <summary>
        /// Nearest.
        /// </summary>
        Nearest = 0,

        /// <summary>
        /// Bilinear
        /// </summary>
        Bilinear = 1,

        /// <summary>
        /// Average
        /// </summary>
        Average = 2,
    }

    /// <summary>
    /// Scale Texture.
    /// </summary>
    /// <param name="source"></param>
    /// <param name="scale"></param>
    /// <param name="textureScaleMode"></param>
    /// <returns></returns>
    /// <exception cref="ArgumentOutOfRangeException"></exception>
    public static Texture2D ScaleTexture(this Texture2D source, float scale, TextureScaleMode textureScaleMode)
    {
        int i;

        // Get All the source pixels
        var aSourceColor = source.GetPixels(0);
        var vSourceSize = new Vector2(source.width, source.height);

        // Calculate New Size
        float xWidth = Mathf.RoundToInt(source.width * scale);
        float xHeight = Mathf.RoundToInt(source.height * scale);

        // Make New
        var oNewTex = new Texture2D((int)xWidth, (int)xHeight, TextureFormat.RGBA32, false);

        // Make destination array
        var xLength = (int)xWidth * (int)xHeight;
        var aColor = new Color[xLength];

        var vPixelSize = new Vector2(vSourceSize.x / xWidth, vSourceSize.y / xHeight);

        // Loop through destination pixels and process
        var vCenter = new Vector2();
        for (i = 0; i < xLength; i++)
        {
            // Figure out x&y
            var xX = i % xWidth;
            var xY = Mathf.Floor(i / xWidth);

            // Calculate Center
            vCenter.x = xX / xWidth * vSourceSize.x;
            vCenter.y = xY / xHeight * vSourceSize.y;

            // Do Based on mode
            // Nearest neighbour (testing)
            switch (textureScaleMode)
            {
                case TextureScaleMode.Nearest:
                {
                    // Nearest neighbour (testing)
                    vCenter.x = Mathf.Round(vCenter.x);
                    vCenter.y = Mathf.Round(vCenter.y);

                    // Calculate source index
                    var xSourceIndex = (int)(vCenter.y * vSourceSize.x + vCenter.x);

                    // Copy Pixel
                    aColor[i] = aSourceColor[xSourceIndex];
                    break;
                }

                case TextureScaleMode.Bilinear:
                {
                    // Get Ratios
                    var xRatioX = vCenter.x - Mathf.Floor(vCenter.x);
                    var xRatioY = vCenter.y - Mathf.Floor(vCenter.y);

                    // Get Pixel index's
                    var xIndexTl = (int)(Mathf.Floor(vCenter.y) * vSourceSize.x + Mathf.Floor(vCenter.x));
                    var xIndexTr = (int)(Mathf.Floor(vCenter.y) * vSourceSize.x + Mathf.Ceil(vCenter.x));
                    var xIndexBl = (int)(Mathf.Ceil(vCenter.y) * vSourceSize.x + Mathf.Floor(vCenter.x));
                    var xIndexBr = (int)(Mathf.Ceil(vCenter.y) * vSourceSize.x + Mathf.Ceil(vCenter.x));

                    // Calculate Color
                    aColor[i] = Color.Lerp(
                        Color.Lerp(aSourceColor[xIndexTl], aSourceColor[xIndexTr], xRatioX),
                        Color.Lerp(aSourceColor[xIndexBl], aSourceColor[xIndexBr], xRatioX),
                        xRatioY
                    );
                    break;
                }
                case TextureScaleMode.Average:
                {
                    // Calculate grid around point
                    var xXFrom = (int)Mathf.Max(Mathf.Floor(vCenter.x - vPixelSize.x * 0.5f), 0);
                    var xXTo = (int)Mathf.Min(Mathf.Ceil(vCenter.x + vPixelSize.x * 0.5f), vSourceSize.x);
                    var xYFrom = (int)Mathf.Max(Mathf.Floor(vCenter.y - vPixelSize.y * 0.5f), 0);
                    var xYTo = (int)Mathf.Min(Mathf.Ceil(vCenter.y + vPixelSize.y * 0.5f), vSourceSize.y);

                    // Loop and accumulate
                    var oColorTemp = new Color();
                    float xGridCount = 0;
                    for (var iy = xYFrom; iy < xYTo; iy++)
                    for (var ix = xXFrom; ix < xXTo; ix++)
                    {
                        // Get Color
                        oColorTemp += aSourceColor[(int)(iy * vSourceSize.x + ix)];

                        // Sum
                        xGridCount++;
                    }

                    // Average Color
                    aColor[i] = oColorTemp / xGridCount;
                    break;
                }
                default:
                    throw new ArgumentOutOfRangeException(nameof(textureScaleMode), textureScaleMode, null);
            }
        }

        oNewTex.SetPixels(aColor);
        oNewTex.Apply();
        return oNewTex;
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
