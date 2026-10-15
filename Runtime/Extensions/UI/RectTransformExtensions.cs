using System;
using System.Collections.Generic;
using UnityEngine;


/// <summary>
/// Provides extension methods for rect transform components.
/// </summary>
namespace AetherNexus.FoundationPlatform.Extensions
{
public static class RectTransformExtensions
{

    /// <summary>
    /// The RecTransform parent of the RectTransform.
    /// </summary>
    /// <returns>The RecTransform parent of the RectTransform.</returns>
    public static RectTransform GetParent(this RectTransform rt)
    {
        return rt.parent as RectTransform;
    }


    // Set[edgeName](float) is similar to setting the "Left" etc variables in the inspector.  Unlike the inspector, these
    // can be used regardless of anchor position.  Be warned, there's a reason the functionality
    // is hidden in the editor, as the behavior is unintuitive when adjusting the parent's rect.
    // If you're calling these every frame or otherwise updating frequently, shouldn't be a problem, though.
    //
    //Keep in mind that these functions all use standard directions when determining positioning; this means
    // that unlike the inspector, positive ALWAYS means to the right/top, and negative ALWAYS means to the left/
    // bottom.  If you want true inspector functionality, use Left() and so on, below.
    //
    //E.g., SetLeftEdge(-10) will turn
    /*
        .__________.
        |          |
        |          |
        |   [ ]    |
        |          |
        |__________|

            into
        .__________.
        |          |
        |          |
      [       ]    |
        |          |
        |__________|

      [ ] is the RectTransform, the bigger square is the parent
    */

    // Truly matches the functionality of the "Left" etc property in the inspector. This means that
    // Right(10) will actually move the right edge to 10 units from the LEFT of the parent's right edge.
    // In other words, all coordinates are "inside": they measure distance from the parent's edge to the inside of the parent.
    /// <summary>
    /// Moves the edge to a distance towards the center from the same edge of the parent.
    /// </summary>
    /// <param name="distance"></param>
    public static void Left(this RectTransform rt, float distance)
    {
        rt.SetLeft(distance);
    }

    /// <summary>
    /// Moves the edge to a distance towards the center from the same edge of the parent.
    /// </summary>
    /// <param name="distance"></param>
    public static void Right(this RectTransform rt, float distance)
    {
        rt.SetRight(-distance);
    }

    /// <summary>
    /// Moves the edge to a distance towards the center from the same edge of the parent.
    /// </summary>
    /// <param name="distance"></param>
    public static void Top(this RectTransform rt, float distance)
    {
        rt.SetTop(-distance);
    }

    /// <summary>
    /// Moves the edge to a distance towards the center from the same edge of the parent.
    /// </summary>
    /// <param name="distance"></param>
    public static void Bottom(this RectTransform rt, float distance)
    {
        rt.SetBottom(distance);
    }


    //E.g., MoveLeft(0) will look like this:
    /*
        .__________.
        |          |
        |          |
       [|]         |
        |          |
        |__________|
    */
    /// <summary>
    /// Sets the position of the RectTransform relative to the parent's Left side, regardless of anchor setting.
    /// </summary>
    /// <param name="left">Sets the position of the RectTransform relative to the parent's Left side.</param>
    public static void MoveLeft(this RectTransform rt, float left)
    {
        float xmin = rt.GetParent().rect.xMin;
        float center = rt.anchorMax.x - rt.anchorMin.x;
        float anchorFactor = rt.anchorMax.x * 2 - 1;
        rt.anchoredPosition = new Vector2(xmin + (xmin * anchorFactor) + left - (center * xmin), rt.anchoredPosition.y);
    }

    /// <summary>Sets the position of the RectTransform relative to the parent's Left side.</summary>
    public static void MoveLeft(this RectTransform rt) => MoveLeft(rt, 0);

    /// <summary>
    /// Sets the position of the RectTransform relative to the parent's Bottom side, regardless of anchor setting.
    /// </summary>
    /// <param name="bottom">Sets the position of the RectTransform relative to the parent's Bottom side.</param>
    public static void MoveBottom(this RectTransform rt, float bottom)
    {
        float ymin = rt.GetParent().rect.yMin;
        float center = rt.anchorMax.y - rt.anchorMin.y;
        float anchorFactor = rt.anchorMax.y * 2 - 1;
        rt.anchoredPosition =
            new Vector2(rt.anchoredPosition.x, ymin + (ymin * anchorFactor) + bottom - (center * ymin));
    }

    /// <summary>Sets the position of the RectTransform relative to the parent's Bottom side.</summary>
    public static void MoveBottom(this RectTransform rt) => MoveBottom(rt, 0);


    /// <summary>
    /// Moves the RectTransform to the given point in parent space, considering (0, 0) to be the parent's lower-left corner.
    /// </summary>
    /// <param name="x">X coordinate of the point to move the RectTransform to.</param>
    /// <param name="y">Y coordinate of the point to move the RectTransform to.</param>
    public static void Move(this RectTransform rt, float x, float y)
    {
        rt.MoveLeft(x);
        rt.MoveBottom(y);
    }

    /// <summary>
    /// Moves the RectTransform to the given point in parent space, considering (0, 0) to be the parent's lower-left corner.
    /// </summary>
    /// <param name="point">The point to move the RectTransform to.</param>
    public static void Move(this RectTransform rt, Vector2 point)
    {
        rt.MoveLeft(point.x);
        rt.MoveBottom(point.y);
    }

    /// <summary>
    /// Resets `anchorMin`, `anchorMax`, `offsetMin`, `offsetMax` to `Vector2.zero`.
    /// </summary>
    /// <param name="rectTransform">RectTransform to operate with.</param>
    public static void Reset(this RectTransform rectTransform)
    {
        rectTransform.anchorMin = Vector2.zero;
        rectTransform.anchorMax = Vector2.one;
        rectTransform.offsetMin = Vector2.zero;
        rectTransform.offsetMax = Vector2.zero;
    }

    /// <summary>
    /// Sets offset min x value of the rectTransform.
    /// </summary>
    /// <param name="rectTransform">Target rectTransform.</param>
    /// <param name="x">Value to set.</param>
    public static void SetOffsetMinX(this RectTransform rectTransform, float x) =>
        rectTransform.offsetMin = rectTransform.offsetMin.WithX(x);

    /// <summary>
    /// Sets offset from left. The same as <b>SetOffsetMinX</b>.
    /// </summary>
    /// <param name="rectTransform">Target rectTransform.</param>
    /// <param name="x">Offset value.</param>
    public static void SetLeft(this RectTransform rectTransform, float x) => SetOffsetMinX(rectTransform, x);

    /// <summary>
    /// Sets offset min y value of the rectTransform.
    /// </summary>
    /// <param name="rectTransform">Target rectTransform.</param>
    /// <param name="y">Value to set.</param>
    public static void SetOffsetMinY(this RectTransform rectTransform, float y) =>
        rectTransform.offsetMin = rectTransform.offsetMin.WithY(y);

    /// <summary>
    /// Sets offset from bottom. The same as <b>SetOffsetMinY</b>.
    /// </summary>
    /// <param name="rectTransform">Target rectTransform.</param>
    /// <param name="y">Offset value.</param>
    public static void SetBottom(this RectTransform rectTransform, float y) => SetOffsetMinY(rectTransform, y);

    /// <summary>
    /// Sets offset max x value of the rectTransform.
    /// </summary>
    /// <param name="rectTransform">Target rectTransform.</param>
    /// <param name="x">Value to set.</param>
    public static void SetOffsetMaxX(this RectTransform rectTransform, float x) =>
        rectTransform.offsetMax = rectTransform.offsetMax.WithX(x);

    /// <summary>
    /// Sets offset from right. The same as <b>SetOffsetMaxX</b>.
    /// </summary>
    /// <param name="rectTransform">Target rectTransform.</param>
    /// <param name="x">Offset value.</param>
    public static void SetRight(this RectTransform rectTransform, float x) => SetOffsetMaxX(rectTransform, x);

    /// <summary>
    /// Sets offset max y value of the rectTransform.
    /// </summary>
    /// <param name="rectTransform">Target rectTransform.</param>
    /// <param name="y">Value to set.</param>
    public static void SetOffsetMaxY(this RectTransform rectTransform, float y) =>
        rectTransform.offsetMax = rectTransform.offsetMax.WithY(y);

    /// <summary>
    /// Sets offset from top. The same as <b>SetOffsetMaxY</b>.
    /// </summary>
    /// <param name="rectTransform">Target rectTransform.</param>
    /// <param name="y">Offset value.</param>
    public static void SetTop(this RectTransform rectTransform, float y) => SetOffsetMaxY(rectTransform, y);

    /// <summary>
    ///   <para>Returns true if the <paramref name="other"/> RectTransform overlaps this one.</para>
    /// </summary>
    /// <param name="other">Other rectangle to test overlapping with.</param>
    public static bool Overlaps(this RectTransform rect, RectTransform other) =>
        rect.WorldRect().Overlaps(other.WorldRect());

    /// <summary>
    ///   <para>Returns true if the <paramref name="other"/> RectTransform overlaps this one. If <paramref name="allowInverse"/> is true, the widths and heights of the Rects are allowed to take negative values (ie, the min value is greater than the max), and the test will still work.</para>
    /// </summary>
    /// <param name="other">Other rectangle to test overlapping with.</param>
    /// <param name="allowInverse">Does the test allow the widths and heights of the Rects to be negative?</param>
    public static bool Overlaps(this RectTransform rect, RectTransform other, bool allowInverse) =>
        rect.WorldRect().Overlaps(other.WorldRect(), allowInverse);

    public static Rect WorldRect(this RectTransform rectTransform)
    {
        var sizeDelta = rectTransform.sizeDelta;
        var rectTransformWidth = sizeDelta.x * rectTransform.lossyScale.x;
        var rectTransformHeight = sizeDelta.y * rectTransform.lossyScale.y;

        var position = rectTransform.position;
        return new Rect(position.x - rectTransformWidth / 2f,
            position.y - rectTransformHeight / 2f,
            rectTransformWidth,
            rectTransformHeight);
    }
}
}
