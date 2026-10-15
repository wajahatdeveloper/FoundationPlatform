using System.Linq;
using UnityEngine;

namespace AetherNexus.FoundationPlatform.Extensions
{
public static class RectExtensions
{


    public static Rect Merge(this Rect src, Rect mergeWith)
    {
        return new Rect
        {
            xMin = Mathf.Min(src.xMin, mergeWith.xMin),
            xMax = Mathf.Max(src.xMax, mergeWith.xMax),
            yMin = Mathf.Min(src.yMin, mergeWith.yMin),
            yMax = Mathf.Max(src.yMax, mergeWith.yMax)
        };
    }

    public static Rect Merge(this Rect src, ref Rect mergeWith)
    {
        return new Rect
        {
            xMin = Mathf.Min(src.xMin, mergeWith.xMin),
            xMax = Mathf.Max(src.xMax, mergeWith.xMax),
            yMin = Mathf.Min(src.yMin, mergeWith.yMin),
            yMax = Mathf.Max(src.yMax, mergeWith.yMax)
        };
    }

    public static bool Contains(this Rect src, Rect rect)
    {
        return rect.xMin > src.xMin && rect.xMax < src.xMax && rect.yMin > src.yMin && rect.yMax < src.yMax;
    }

    public static bool Contains(this Rect src, ref Rect rect)
    {
        return rect.xMin > src.xMin && rect.xMax < src.xMax && rect.yMin > src.yMin && rect.yMax < src.yMax;
    }

    public static float Volume(this Rect src)
    {
        return src.width * src.height;
    }

    public static bool Contains(this Rect src, Vector2 point)
    {
        return src.xMax >= point.x && src.xMin <= point.x && src.yMax >= point.y && src.yMin <= point.y;
    }

    public static bool Contains(this Rect src, ref Vector2 point)
    {
        return src.xMax >= point.x && src.xMin <= point.x && src.yMax >= point.y && src.yMin <= point.y;
    }

    #region Position

    public static Rect WithX(this Rect rect, float x)
    {
        return new Rect(x, rect.y, rect.width, rect.height);
    }

    public static Rect WithY(this Rect rect, float y)
    {
        return new Rect(rect.x, y, rect.width, rect.height);
    }

    public static Rect Translate(this Rect rect, float x, float y)
    {
        return new Rect(rect.x + x, rect.y + y, rect.width, rect.height);
    }

    public static Rect Translate(this Rect rect, Vector2 translation)
    {
        return new Rect(rect.x + translation.x, rect.y + translation.y, rect.width, rect.height);
    }

    public static Rect Below(this Rect rect, Rect target)
    {
        return new Rect(rect.x, target.y + target.height, rect.width, rect.height);
    }

    public static Rect Above(this Rect rect, Rect target)
    {
        return new Rect(rect.x, target.y - rect.height, rect.width, rect.height);
    }

#endregion
#region Size

    public static Rect Pad(this Rect rect, float padding)
    {
        return new Rect(rect.x + padding, rect.y + padding, rect.width - padding * 2, rect.height - padding * 2);
    }

    public static Rect Pad(this Rect rect, float left, float top, float right, float bottom)
    {
        return new Rect(rect.x + left, rect.y + top, rect.width - right - left, rect.height - bottom - top);
    }

    public static Rect Clip(this Rect rect, Rect target)
    {
        float x = rect.x;
        if (x < target.x)
            x = target.x;
        if (x > target.xMax)
            x = target.xMax;

        float y = rect.y;
        if (y < target.y)
            y = target.y;
        if (y > target.yMax)
            y = target.yMax;

        float width = rect.width;
        if (x + width > target.xMax)
            width = target.xMax - rect.x;

        float height = rect.height;
        if (y + height > target.yMax)
            height = target.yMax - rect.y;

        return new Rect(x, y, width, height);
    }

    public static Rect Cover(this Rect rect, params Rect[] targets)
    {
        float x = targets.Min(t => t.x);
        float y = targets.Min(t => t.y);
        float width = targets.Max(t => t.xMax - x);
        float height = targets.Max(t => t.yMax - y);

        return new Rect(x, y, width, height);
    }

    #endregion
}}
