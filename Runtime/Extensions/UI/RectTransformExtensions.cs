using System;
using System.Collections.Generic;
using UnityEngine;


/// <summary>
/// Provides extension methods for rect transform components.
/// </summary>
public static class RectTransformExtensions
{
    // RectTransform

    public static void SetPositionX(this RectTransform transform, float x)
    {
        transform.anchoredPosition = transform.anchoredPosition.SetX(x);
    }

    public static void SetPositionY(this RectTransform transform, float y)
    {
        transform.anchoredPosition = transform.anchoredPosition.SetY(y);
    }

    public static void OffsetPositionX(this RectTransform transform, float x)
    {
        transform.anchoredPosition = transform.anchoredPosition.OffsetX(x);
    }

    public static void OffsetPositionY(this RectTransform transform, float y)
    {
        transform.anchoredPosition = transform.anchoredPosition.OffsetY(y);
    }

    /// <summary>
    /// The normalized position in the parent RectTransform that the corners is anchored to.
    /// </summary>
    /// <returns>A MinMax01 defining the normalized position in the parent RectTransform that the corners is anchored to.</returns>
    public static MinMax01 GetAnchors(this RectTransform rt)
    {
        return new MinMax01(rt.anchorMin, rt.anchorMax);
    }


    /// <summary>
    /// Sets the normalized position in the parent RectTransform that the corners is anchored to.
    /// </summary>
    public static void SetAnchors(this RectTransform rt, MinMax01 anchors)
    {
        rt.anchorMin = anchors.min;
        rt.anchorMax = anchors.max;
    }

    /// <summary>
    /// The RecTransform parent of the RectTransform.
    /// </summary>
    /// <returns>The RecTransform parent of the RectTransform.</returns>
    public static RectTransform GetParent(this RectTransform rt)
    {
        return rt.parent as RectTransform;
    }

    /// <summary>
    /// Sets the width and the height of the RectTransform keeping the current anchors.
    /// </summary>
    /// <param name="width">The desired width of the RectTransform</param>
    /// <param name="height">The desired height of the RectTransform</param>
    public static void SetSize(this RectTransform rt, float width, float height)
    {
        rt.SetSizeWithCurrentAnchors(RectTransform.Axis.Horizontal, width);
        rt.SetSizeWithCurrentAnchors(RectTransform.Axis.Vertical, height);
    }

    /// <summary>
    /// The center of the left edge.
    /// </summary>
    /// <returns>The center of the left edge.</returns>
    public static Vector2 GetLeft(this RectTransform rt)
    {
        return new Vector2(rt.offsetMin.x, rt.anchoredPosition.y);
    }

    /// <summary>
    /// The center of the right edge.
    /// </summary>
    /// <returns>The center of the right edge.</returns>
    public static Vector2 GetRight(this RectTransform rt)
    {
        return new Vector2(rt.offsetMax.x, rt.anchoredPosition.y);
    }

    /// <summary>
    /// The center of the top edge.
    /// </summary>
    /// <returns>The center of the top edge.</returns>
    public static Vector2 GetTop(this RectTransform rt)
    {
        return new Vector2(rt.anchoredPosition.x, rt.offsetMax.y);
    }

    /// <summary>
    /// The center of the bottom edge.
    /// </summary>
    /// <returns>The center of the bottom edge.</returns>
    public static Vector2 GetBottom(this RectTransform rt)
    {
        return new Vector2(rt.anchoredPosition.x, rt.offsetMin.y);
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


    /// <summary>
    /// Repositions the requested edge relative to the passed anchor.
    /// </summary>
    /// <param name="anchor">The anchor to get the relative from.</param>
    /// <param name="distance">The distance to be moved to.</param>
    public static void SetLeftFrom(this RectTransform rt, MinMax01 anchor, float distance)
    {
        Vector2 origin = rt.AnchorToParentSpace(anchor.min - rt.anchorMin);

        rt.offsetMin = new Vector2(origin.x + distance, rt.offsetMin.y);
    }

    /// <summary>
    /// Repositions the requested edge relative to the passed anchor.
    /// </summary>
    /// <param name="anchor">The anchor to get the relative from.</param>
    /// <param name="distance">The distance to be moved to.</param>
    public static void SetRightFrom(this RectTransform rt, MinMax01 anchor, float distance)
    {
        Vector2 origin = rt.AnchorToParentSpace(anchor.max - rt.anchorMax);

        rt.offsetMax = new Vector2(origin.x + distance, rt.offsetMax.y);
    }

    /// <summary>
    /// Repositions the requested edge relative to the passed anchor.
    /// </summary>
    /// <param name="anchor">The anchor to get the relative from.</param>
    /// <param name="distance">The distance to be moved to.</param>
    public static void SetTopFrom(this RectTransform rt, MinMax01 anchor, float distance)
    {
        Vector2 origin = rt.AnchorToParentSpace(anchor.max - rt.anchorMax);

        rt.offsetMax = new Vector2(rt.offsetMax.x, origin.y + distance);
    }

    /// <summary>
    /// Repositions the requested edge relative to the passed anchor.
    /// </summary>
    /// <param name="anchor">The anchor to get the relative from.</param>
    /// <param name="distance">The distance to be moved to.</param>
    public static void SetBottomFrom(this RectTransform rt, MinMax01 anchor, float distance)
    {
        Vector2 origin = rt.AnchorToParentSpace(anchor.min - rt.anchorMin);

        rt.offsetMin = new Vector2(rt.offsetMin.x, origin.y + distance);
    }


    /// <summary>
    /// Moves the edge to the requested position relative to the current position.
    /// <para>Using these functions repeatedly will result in unintuitive behavior, since the anchored position is getting changed with each call.</para>
    /// </summary>
    /// <param name="rt"></param>
    /// <param name="distance">The distance you to displace the left edge.</param>
    public static void SetRelativeLeft(this RectTransform rt, float distance)
    {
        rt.offsetMin = new Vector2(rt.anchoredPosition.x + distance, rt.offsetMin.y);
    }

    /// <summary>
    /// Moves the edge to the requested position relative to the current position.
    /// <para>Using these functions repeatedly will result in unintuitive behavior, since the anchored position is getting changed with each call.</para>
    /// </summary>
    /// <param name="rt"></param>
    /// <param name="distance">The distance you to displace the right edge.</param>
    public static void SetRelativeRight(this RectTransform rt, float distance)
    {
        rt.offsetMax = new Vector2(rt.anchoredPosition.x + distance, rt.offsetMax.y);
    }

    /// <summary>
    /// Moves the edge to the requested position relative to the current position.
    /// <para>Using these functions repeatedly will result in unintuitive behavior, since the anchored position is getting changed with each call.</para>
    /// </summary>
    /// <param name="rt"></param>
    /// <param name="distance">The distance you to displace the top edge.</param>
    public static void SetRelativeTop(this RectTransform rt, float distance)
    {
        rt.offsetMax = new Vector2(rt.offsetMax.x, rt.anchoredPosition.y + distance);
    }

    /// <summary>
    /// Moves the edge to the requested position relative to the current position.
    /// <para>Using these functions repeatedly will result in unintuitive behavior, since the anchored position is getting changed with each call.</para>
    /// </summary>
    /// <param name="rt"></param>
    /// <param name="distance">The distance you to displace the bottom edge.</param>
    public static void SetRelativeBottom(this RectTransform rt, float distance)
    {
        rt.offsetMin = new Vector2(rt.offsetMin.x, rt.anchoredPosition.y + distance);
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
    public static void MoveLeft(this RectTransform rt, float left = 0)
    {
        float xmin = rt.GetParent().rect.xMin;
        float center = rt.anchorMax.x - rt.anchorMin.x;
        float anchorFactor = rt.anchorMax.x * 2 - 1;
        rt.anchoredPosition = new Vector2(xmin + (xmin * anchorFactor) + left - (center * xmin), rt.anchoredPosition.y);
    }

    /// <summary>
    /// Sets the position of the RectTransform relative to the parent's Right side, regardless of anchor setting.
    /// </summary>
    /// <param name="right">Sets the position of the RectTransform relative to the parent's Right side.</param>
    public static void MoveRight(this RectTransform rt, float right = 0)
    {
        float xmax = rt.GetParent().rect.xMax;
        float center = rt.anchorMax.x - rt.anchorMin.x;
        float anchorFactor = rt.anchorMax.x * 2 - 1;
        rt.anchoredPosition =
            new Vector2(xmax - (xmax * anchorFactor) - right + (center * xmax), rt.anchoredPosition.y);
    }

    /// <summary>
    /// Sets the position of the RectTransform relative to the parent's Top side, regardless of anchor setting.
    /// </summary>
    /// <param name="top">Sets the position of the RectTransform relative to the parent's Top side.</param>
    public static void MoveTop(this RectTransform rt, float top = 0)
    {
        float ymax = rt.GetParent().rect.yMax;
        float center = rt.anchorMax.y - rt.anchorMin.y;
        float anchorFactor = rt.anchorMax.y * 2 - 1;
        rt.anchoredPosition = new Vector2(rt.anchoredPosition.x, ymax - (ymax * anchorFactor) - top + (center * ymax));
    }

    /// <summary>
    /// Sets the position of the RectTransform relative to the parent's Bottom side, regardless of anchor setting.
    /// </summary>
    /// <param name="bottom">Sets the position of the RectTransform relative to the parent's Bottom side.</param>
    public static void MoveBottom(this RectTransform rt, float bottom = 0)
    {
        float ymin = rt.GetParent().rect.yMin;
        float center = rt.anchorMax.y - rt.anchorMin.y;
        float anchorFactor = rt.anchorMax.y * 2 - 1;
        rt.anchoredPosition =
            new Vector2(rt.anchoredPosition.x, ymin + (ymin * anchorFactor) + bottom - (center * ymin));
    }


    //Moves the RectTransform to align the child left edge with the parent left edge, etc.
    //E.g., MoveLeftInside(0) will look like this:
    /*
        .__________.
        |          |
        |          |
        [ ]        |
        |          |
        |__________|
    */
    /// <summary>
    /// Moves the RectTransform to align the child left edge with the parent left edge.
    /// </summary>
    /// <param name="distance">The distance to the parent left edge.</param>
    public static void MoveLeftInside(this RectTransform rt, float distance = 0)
    {
        rt.MoveLeft(distance + rt.GetWidth() / 2);
    }

    /// <summary>
    /// Moves the RectTransform to align the child left edge with the parent right edge.
    /// </summary>
    /// <param name="distance">The distance to the parent right edge.</param>
    public static void MoveRightInside(this RectTransform rt, float distance = 0)
    {
        rt.MoveRight(distance + rt.GetWidth() / 2);
    }

    /// <summary>
    /// Moves the RectTransform to align the child left edge with the parent left top.
    /// </summary>
    /// <param name="distance">The distance to the parent top edge.</param>
    public static void MoveTopInside(this RectTransform rt, float distance = 0)
    {
        rt.MoveTop(distance + rt.GetHeight() / 2);
    }

    /// <summary>
    /// Moves the RectTransform to align the child left edge with the parent bottom edge.
    /// </summary>
    /// <param name="distance">The distance to the parent bottom edge.</param>
    public static void MoveBottomInside(this RectTransform rt, float distance = 0)
    {
        rt.MoveBottom(distance + rt.GetHeight() / 2);
    }


    //Moves the RectTransform to align the child right edge with the parent left edge, etc
    //E.g., MoveLeftOutside(0) will look like this:
    /*
        .__________.
        |          |
        |          |
      [ ]          |
        |          |
        |__________|
    */
    /// <summary>
    /// Moves the RectTransform to align the right edge with the parent left edge.
    /// </summary>
    /// <param name="rt"></param>
    /// <param name="distance">The distance between the edges</param>
    public static void MoveLeftOutside(this RectTransform rt, float distance = 0)
    {
        rt.MoveLeft(distance - rt.GetWidth() / 2);
    }

    /// <summary>
    /// Moves the RectTransform to align the left edge with the parent right edge.
    /// </summary>
    /// <param name="distance">The distance between the edges</param>
    public static void MoveRightOutside(this RectTransform rt, float distance = 0)
    {
        rt.MoveRight(distance - rt.GetWidth() / 2);
    }

    /// <summary>
    /// Moves the RectTransform to align the bottom edge with the parent top edge.
    /// </summary>
    /// <param name="distance">The distance between the edges</param>
    public static void MoveTopOutside(this RectTransform rt, float distance = 0)
    {
        rt.MoveTop(distance - rt.GetHeight() / 2);
    }

    /// <summary>
    /// Moves the RectTransform to align the top edge with the parent bottom edge.
    /// </summary>
    /// <param name="distance">The distance between the edges</param>
    public static void MoveBottomOutside(this RectTransform rt, float distance = 0)
    {
        rt.MoveBottom(distance - rt.GetHeight() / 2);
    }


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
    /// Moves the RectTransform relative to the parent's lower-le1ft corner, respecting the RT's width and height.
    /// <para>See MoveLeftInside for more information.</para>
    /// </summary>
    /// <param name="x">X coordinate of the point to move the RectTransform to.</param>
    /// <param name="y">Y coordinate of the point to move the RectTransform to.</param>
    public static void MoveInside(this RectTransform rt, float x, float y)
    {
        rt.MoveLeftInside(x);
        rt.MoveBottomInside(y);
    }

    /// <summary>
    /// Moves the RectTransform relative to the parent's lower-le1ft corner, respecting the RT's width and height.
    /// <para>See MoveLeftInside for more information.</para>
    /// </summary>
    /// <param name="point">The point to move the RectTransform to.</param>
    public static void MoveInside(this RectTransform rt, Vector2 point)
    {
        rt.MoveLeftInside(point.x);
        rt.MoveBottomInside(point.y);
    }


    /// <summary>
    /// Moves the RectTransform relative to the parent's lower-left corner, respecting the RT's width and height.
    /// <para>See MoveLeftOutside for more information.</para>
    /// </summary>
    /// <param name="x">X coordinate of the point to move the RectTransform to.</param>
    /// <param name="y">Y coordinate of the point to move the RectTransform to.</param>
    public static void MoveOutside(this RectTransform rt, float x, float y)
    {
        rt.MoveLeftOutside(x);
        rt.MoveBottomOutside(y);
    }

    /// <summary>
    /// Moves the RectTransform relative to the parent's lower-left corner, respecting the RT's width and height.
    /// <para>See MoveLeftOutside for more information.</para>
    /// </summary>
    /// <param name="point">The point to move the RectTransform to.</param>
    public static void MoveOutside(this RectTransform rt, Vector2 point)
    {
        rt.MoveLeftOutside(point.x);
        rt.MoveBottomOutside(point.y);
    }


    /// <summary>
    /// Moves the RectTransform relative to an arbitrary anchor point.  This is effectively like setting the anchor, then moving, then setting it back, but does so without potentially getting in the way of anything else.
    /// </summary>
    public static void MoveFrom(this RectTransform rt, MinMax01 anchor, Vector2 point)
    {
        rt.MoveFrom(anchor, point.x, point.y);
    }

    /// <summary>
    /// Moves the RectTransform relative to an arbitrary anchor point.  This is effectively like setting the anchor, then moving, then setting it back, but does so without potentially getting in the way of anything else.
    /// </summary>
    /// <param name="rt"></param>
    public static void MoveFrom(this RectTransform rt, MinMax01 anchor, float x, float y)
    {
        Vector2 origin = rt.AnchorToParentSpace(AnchorOrigin(anchor) - rt.AnchorOrigin());
        rt.anchoredPosition = new Vector2(origin.x + x, origin.y + y);
    }


    /// <summary>
    /// Translates a point on the parent's frame of reference, with (0, 0) being the parent's lower-left hand corner, into the same point relative to the RectTransform's current anchor.
    /// </summary>
    /// <param name="point">The point to translate.</param>
    /// <returns>The translated point.</returns>
    public static Vector2 ParentToChildSpace(this RectTransform rt, Vector2 point)
    {
        return rt.ParentToChildSpace(point.x, point.y);
    }

    /// <summary>
    /// Translates a point on the parent's frame of reference, with (0, 0) being the parent's lower-left hand corner, into the same point relative to the RectTransform's current anchor.
    /// </summary>
    /// <param name="x">X coordinate of the point to translate.</param>
    /// <param name="y">Y coordinate of the point to translate.</param>
    /// <returns>The translated point.</returns>
    public static Vector2 ParentToChildSpace(this RectTransform rt, float x, float y)
    {
        float xmin = rt.GetParent().rect.xMin;
        float ymin = rt.GetParent().rect.yMin;
        float anchorFactorX = rt.anchorMin.x * 2 - 1;
        float anchorFactorY = rt.anchorMin.y * 2 - 1;
        return new Vector2(xmin + (xmin * anchorFactorX) + x, ymin + (ymin * anchorFactorY) + y);
    }


    /// <summary>
    /// Translates a point (presumably the RectTransform's anchoredPosition) into the same point on the parent's frame of reference, with (0, 0) being the parent's lower-left hand corner.
    /// </summary>
    /// <param name="x">X coordinate of the point to translate.</param>
    /// <param name="y">Y coordinate of the point to translate.</param>
    /// <returns>The translated point.</returns>
    public static Vector2 ChildToParentSpace(this RectTransform rt, float x, float y)
    {
        return rt.AnchorOriginParent() + new Vector2(x, y);
    }

    /// <summary>
    /// Translates a point (presumably the RectTransform's anchoredPosition) into the same point on the parent's frame of reference, with (0, 0) being the parent's lower-left hand corner.
    /// </summary>
    /// <param name="point">The point to translate.</param>
    /// <returns>The translated point.</returns>
    public static Vector2 ChildToParentSpace(this RectTransform rt, Vector2 point)
    {
        return rt.AnchorOriginParent() + point;
    }


    /// <summary>
    /// Normalizes a point associated with the parent object into "Anchor Space", which is to say, (0, 0) represents the parent's lower-left-hand corner, and (1, 1) represents the upper-right-hand.
    /// </summary>
    /// <param name="point">The point to normalize.</param>
    /// <returns>The normalized point.</returns>
    public static Vector2 ParentToAnchorSpace(this RectTransform rt, Vector2 point)
    {
        return rt.ParentToAnchorSpace(point.x, point.y);
    }

    /// <summary>
    /// Normalizes a point associated with the parent object into "Anchor Space", which is to say, (0, 0) represents the parent's lower-left-hand corner, and (1, 1) represents the upper-right-hand.
    /// </summary>
    /// <param name="x">X coordinate of the point to normalize.</param>
    /// <param name="y">Y coordinate of the point to normalize.</param>
    /// <returns>The normalized point.</returns>
    public static Vector2 ParentToAnchorSpace(this RectTransform rt, float x, float y)
    {
        Rect parent = rt.GetParent().rect;
        if (parent.width != 0)
            x /= parent.width;
        else
            x = 0;

        if (parent.height != 0)
            y /= parent.height;
        else
            y = 0;

        return new Vector2(x, y);
    }


    /// <summary>
    /// Translates a normalized "Anchor Space" coordinate into a real point on the parent's reference system.
    /// </summary>
    /// <param name="x">X coordinate of a normalized point set in "Anchor Space".</param>
    /// <param name="y">Y coordinate of a normalized point set in "Anchor Space".</param>
    /// <returns>The anchor space coordinate as a real point on the parent's reference</returns>
    public static Vector2 AnchorToParentSpace(this RectTransform rt, float x, float y)
    {
        return new Vector2(x * rt.GetParent().rect.width, y * rt.GetParent().rect.height);
    }

    /// <summary>
    /// Translates a normalized "Anchor Space" coordinate into a real point on the parent's reference system.
    /// </summary>
    /// <param name="point">A normalized "Anchor Space" point of a normalized point set in "Anchor Space".</param>
    /// <returns>The anchor space coordinate as a real point on the parent's reference</returns>
    public static Vector2 AnchorToParentSpace(this RectTransform rt, Vector2 point)
    {
        return new Vector2(point.x * rt.GetParent().rect.width, point.y * rt.GetParent().rect.height);
    }


    /// <summary>
    /// The center of the rectangle the two anchors represent, which is the origin that a RectTransform's anchoredPosition is an offset of.
    /// </summary>
    /// <returns>The center of the rectangle the two anchors represent.</returns>
    public static Vector2 AnchorOrigin(this RectTransform rt)
    {
        return AnchorOrigin(rt.GetAnchors());
    }

    /// <summary>
    /// The center of the rectangle the two anchors represent, which is the origin that a RectTransform's anchoredPosition is an offset of.
    /// </summary>
    /// <returns>The center of the rectangle the two anchors represent.</returns>
    public static Vector2 AnchorOrigin(MinMax01 anchor)
    {
        float x = anchor.min.x + (anchor.max.x - anchor.min.x) / 2;
        float y = anchor.min.y + (anchor.max.y - anchor.min.y) / 2;

        return new Vector2(x, y);
    }

    /// <summary>
    /// Translates a RectTransform's anchor origin into Parent space.
    /// </summary>
    /// <returns>The anchor origin in parent space.</returns>
    public static Vector2 AnchorOriginParent(this RectTransform rt)
    {
        return Vector2.Scale(rt.AnchorOrigin(), new Vector2(rt.GetParent().rect.width, rt.GetParent().rect.height));
    }

    /// <summary>
    /// Returns the top-most-level canvas that this RectTransform is a child of.
    /// </summary>
    /// <returns>The top-most-level canvas that this RectTransform is a child of, the root canvas.</returns>
    public static Canvas GetRootCanvas(this RectTransform rt)
    {
        Canvas rootCanvas = rt.GetComponentInParent<Canvas>();

        while (!rootCanvas.isRootCanvas)
            rootCanvas = rootCanvas.transform.parent.GetComponentInParent<Canvas>();

        return rootCanvas;
    }

    /// <summary>
    /// Converts the anchoredPosition of the first RectTransform to the second RectTransform,
    /// taking into consideration offset, anchors and pivot, and returns the new anchoredPosition
    /// </summary>
    public static Vector2 switchToRectTransform(this RectTransform from, RectTransform to)
    {
        Vector2 localPoint;
        Vector2 fromPivotDerivedOffset = new Vector2(from.rect.width * from.pivot.x + from.rect.xMin,
            from.rect.height * from.pivot.y + from.rect.yMin);
        Vector2 screenP = RectTransformUtility.WorldToScreenPoint(null, from.position);
        screenP += fromPivotDerivedOffset;
        RectTransformUtility.ScreenPointToLocalPointInRectangle(to, screenP, null, out localPoint);
        Vector2 pivotDerivedOffset = new Vector2(to.rect.width * to.pivot.x + to.rect.xMin,
            to.rect.height * to.pivot.y + to.rect.yMin);
        return to.anchoredPosition + localPoint - pivotDerivedOffset;
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
    /// Get's the screen rect of provided RectTransform.
    /// </summary>
    /// <param name="rectTransform">RectTransform to operate with.</param>
    /// <returns>Screen rect.</returns>
    public static Rect GetScreenRect(this RectTransform rectTransform)
    {
        var rtCorners = new Vector3[4];
        rectTransform.GetWorldCorners(rtCorners);
        var rtRect = new Rect(new Vector2(rtCorners[0].x, rtCorners[0].y),
            new Vector2(rtCorners[3].x - rtCorners[0].x, rtCorners[1].y - rtCorners[0].y));

        var canvas = rectTransform.GetComponentInParent<Canvas>();
        var canvasCorners = new Vector3[4];
        canvas.GetComponent<RectTransform>().GetWorldCorners(canvasCorners);
        var cRect = new Rect(new Vector2(canvasCorners[0].x, canvasCorners[0].y),
            new Vector2(canvasCorners[3].x - canvasCorners[0].x, canvasCorners[1].y - canvasCorners[0].y));

        var screenWidth = Screen.width;
        var screenHeight = Screen.height;

        var size = new Vector2(screenWidth / cRect.size.x * rtRect.size.x, screenHeight / cRect.size.y * rtRect.size.y);
        var rect = new Rect(screenWidth * ((rtRect.x - cRect.x) / cRect.size.x),
            screenHeight * ((-cRect.y + rtRect.y) / cRect.size.y), size.x, size.y);
        return rect;
    }

    /// <summary>
    /// Method to get Rect related to ScreenSpace, from given RectTransform.
    /// This will give the real position of this Rect on screen.
    /// </summary>
    /// <param name="transform">Original RectTransform of some object</param>
    /// <returns>New Rect instance.</returns>
    public static Rect RectTransformToScreenSpace(this RectTransform transform)
    {
        Vector2 size = Vector2.Scale(transform.rect.size, transform.lossyScale);
        Rect rect = new Rect(transform.position.x, Screen.height - transform.position.y, size.x, size.y);
        rect.x -= (transform.pivot.x * size.x);
        rect.y -= ((1.0f - transform.pivot.y) * size.y);
        return rect;
    }

    /// <summary>
    /// Sets size delta x value of the rectTransform.
    /// </summary>
    /// <param name="rectTransform">Target rectTransform.</param>
    /// <param name="x">Value to set.</param>
    public static void SetSizeDeltaX(this RectTransform rectTransform, float x) =>
        rectTransform.sizeDelta = rectTransform.sizeDelta.WithX(x);

    /// <summary>
    /// Sets size delta y value of the rectTransform.
    /// </summary>
    /// <param name="rectTransform">Target rectTransform.</param>
    /// <param name="y">Value to set.</param>
    public static void SetSizeDeltaY(this RectTransform rectTransform, float y) =>
        rectTransform.sizeDelta = rectTransform.sizeDelta.WithY(y);

    /// <summary>
    /// Sets anchor min x value of the rectTransform.
    /// </summary>
    /// <param name="rectTransform">Target rectTransform.</param>
    /// <param name="x">Value to set.</param>
    public static void SetAnchorMinX(this RectTransform rectTransform, float x) =>
        rectTransform.anchorMin = rectTransform.anchorMin.WithX(x);

    /// <summary>
    /// Sets anchor min y value of the rectTransform.
    /// </summary>
    /// <param name="rectTransform">Target rectTransform.</param>
    /// <param name="y">Value to set.</param>
    public static void SetAnchorMinY(this RectTransform rectTransform, float y) =>
        rectTransform.anchorMin = rectTransform.anchorMin.WithY(y);

    /// <summary>
    /// Sets anchor max x value of the rectTransform.
    /// </summary>
    /// <param name="rectTransform">Target rectTransform.</param>
    /// <param name="x">Value to set.</param>
    public static void SetAnchorMaxX(this RectTransform rectTransform, float x) =>
        rectTransform.anchorMax = rectTransform.anchorMax.WithX(x);

    /// <summary>
    /// Sets anchor max y value of the rectTransform.
    /// </summary>
    /// <param name="rectTransform">Target rectTransform.</param>
    /// <param name="y">Value to set.</param>
    public static void SetAnchorMaxY(this RectTransform rectTransform, float y) =>
        rectTransform.anchorMax = rectTransform.anchorMax.WithY(y);

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
    /// Sets anchor position x value of the rectTransform.
    /// </summary>
    /// <param name="rectTransform">Target rectTransform.</param>
    /// <param name="x">Value to set.</param>
    public static void SetAnchoredPositionX(this RectTransform rectTransform, float x) =>
        rectTransform.anchoredPosition = rectTransform.anchoredPosition.WithX(x);

    /// <summary>
    /// Sets anchor position y value of the rectTransform.
    /// </summary>
    /// <param name="rectTransform">Target rectTransform.</param>
    /// <param name="x">Value to set.</param>
    public static void SetAnchoredPositionY(this RectTransform rectTransform, float y) =>
        rectTransform.anchoredPosition = rectTransform.anchoredPosition.WithY(y);

    /// <summary>
    /// Sets anchor position 3d x value of the rectTransform.
    /// </summary>
    /// <param name="rectTransform">Target rectTransform.</param>
    /// <param name="x">Value to set.</param>
    public static void SetAnchoredPosition3DX(this RectTransform rectTransform, float x) =>
        rectTransform.anchoredPosition3D = rectTransform.anchoredPosition3D.WithX(x);

    /// <summary>
    /// Sets anchor position 3d y value of the rectTransform.
    /// </summary>
    /// <param name="rectTransform">Target rectTransform.</param>
    /// <param name="y">Value to set.</param>
    public static void SetAnchoredPosition3DY(this RectTransform rectTransform, float y) =>
        rectTransform.anchoredPosition3D = rectTransform.anchoredPosition3D.WithY(y);

    /// <summary>
    /// Sets anchor position 3d z value of the rectTransform.
    /// </summary>
    /// <param name="rectTransform">Target rectTransform.</param>
    /// <param name="z">Value to set.</param>
    public static void SetAnchoredPosition3DZ(this RectTransform rectTransform, float z) =>
        rectTransform.anchoredPosition3D = rectTransform.anchoredPosition3D.WithZ(z);

    /// <summary>
    /// Sets anchor position 3d x and y values of the rectTransform.
    /// </summary>
    /// <param name="rectTransform">Target rectTransform.</param>
    /// <param name="x">Value to set.</param>
    /// <param name="y">Value to set.</param>
    public static void SetAnchoredPosition3DXY(this RectTransform rectTransform, float x, float y) =>
        rectTransform.anchoredPosition3D = rectTransform.anchoredPosition3D.WithXY(x, y);

    /// <summary>
    /// Sets anchor position 3d x and y values of the rectTransform.
    /// </summary>
    /// <param name="rectTransform">Target rectTransform.</param>
    /// <param name="position">Position to set.</param>
    public static void SetAnchoredPosition3DXY(this RectTransform rectTransform, Vector2 position) =>
        rectTransform.anchoredPosition3D = rectTransform.anchoredPosition3D.WithXY(position.x, position.y);

    /// <summary>
    /// Sets anchor position 3d x and z values of the rectTransform.
    /// </summary>
    /// <param name="rectTransform">Target rectTransform.</param>
    /// <param name="x">Value to set.</param>
    /// <param name="z">Value to set.</param>
    public static void SetAnchoredPosition3DXZ(this RectTransform rectTransform, float x, float z) =>
        rectTransform.anchoredPosition3D = rectTransform.anchoredPosition3D.WithXZ(x, z);

    /// <summary>
    /// Sets anchor position 3d x and z values of the rectTransform.
    /// </summary>
    /// <param name="rectTransform">Target rectTransform.</param>
    /// <param name="position">Position to set.</param>
    public static void SetAnchoredPosition3DXZ(this RectTransform rectTransform, Vector2 position) =>
        rectTransform.anchoredPosition3D = rectTransform.anchoredPosition3D.WithXZ(position.x, position.y);

    /// <summary>
    /// Sets anchor position 3d y and z values of the rectTransform.
    /// </summary>
    /// <param name="rectTransform">Target rectTransform.</param>
    /// <param name="y">Value to set.</param>
    /// <param name="z">Value to set.</param>
    public static void SetAnchoredPosition3DYZ(this RectTransform rectTransform, float y, float z) =>
        rectTransform.anchoredPosition3D = rectTransform.anchoredPosition3D.WithYZ(y, z);

    /// <summary>
    /// Sets anchor position 3d y and z values of the rectTransform.
    /// </summary>
    /// <param name="rectTransform">Target rectTransform.</param>
    /// <param name="position">Position to set.</param>
    public static void SetAnchoredPosition3DYZ(this RectTransform rectTransform, Vector2 position) =>
        rectTransform.anchoredPosition3D = rectTransform.anchoredPosition3D.WithYZ(position.x, position.y);

    /// <summary>
    /// Sets pivot x value of the rectTransform.
    /// </summary>
    /// <param name="rectTransform">Target rectTransform.</param>
    /// <param name="x">Value to set.</param>
    public static void SetPivotX(this RectTransform rectTransform, float x) =>
        rectTransform.pivot = rectTransform.pivot.WithX(x);

    /// <summary>
    /// Sets pivot y value of the rectTransform.
    /// </summary>
    /// <param name="rectTransform">Target rectTransform.</param>
    /// <param name="x">Value to set.</param>
    public static void SetPivotY(this RectTransform rectTransform, float y) =>
        rectTransform.pivot = rectTransform.pivot.WithY(y);

    /// <summary>
    /// Sets pivot x value of the rectTransform without any changes min and max points.
    /// </summary>
    /// <param name="rectTransform">Target rectTransform.</param>
    /// <param name="x">Value to set.</param>
    public static void SetPivotOnlyX(this RectTransform rectTransform, float x)
    {
        var deltaPercent = rectTransform.pivot.x - x;
        rectTransform.SetPivotX(x);
        rectTransform.SetAnchoredPositionX(rectTransform.anchoredPosition.x - rectTransform.sizeDelta.x * deltaPercent);
    }

    /// <summary>
    /// Sets pivot y value of the rectTransform without any changes min and max points.
    /// </summary>
    /// <param name="rectTransform">Target rectTransform.</param>
    /// <param name="y">Value to set.</param>
    public static void SetPivotOnlyY(this RectTransform rectTransform, float y)
    {
        var deltaPercent = rectTransform.pivot.y - y;
        rectTransform.SetPivotY(y);
        rectTransform.SetAnchoredPositionY(rectTransform.anchoredPosition.y - rectTransform.sizeDelta.y * deltaPercent);
    }

    /// <summary>
    /// Sets pivot value of the rectTransform without any changes min and max points.
    /// </summary>
    /// <param name="rectTransform">Target rectTransform.</param>
    /// <param name="pivot">Value to set.</param>
    public static void SetPivotOnly(this RectTransform rectTransform, Vector2 pivot) =>
        SetPivotOnly(rectTransform, pivot.x, pivot.y);

    /// <summary>
    /// Sets pivot x and y values of the rectTransform without any changes min and max points.
    /// </summary>
    /// <param name="rectTransform">Target rectTransform.</param>
    /// <param name="x">Value to set.</param>
    /// <param name="y">Value to set.</param>
    public static void SetPivotOnly(this RectTransform rectTransform, float x, float y)
    {
        SetPivotOnlyX(rectTransform, x);
        SetPivotOnlyY(rectTransform, y);
    }


    public static void SetDefaultScale(this RectTransform trans)
    {
        trans.localScale = new Vector3(1, 1, 1);
    }

    public static void SetPivotAndAnchors(this RectTransform trans, Vector2 aVec)
    {
        trans.pivot = aVec;
        trans.anchorMin = aVec;
        trans.anchorMax = aVec;
    }

    public static Vector2 GetSize(this RectTransform trans)
    {
        return trans.rect.size;
    }

    public static float GetWidth(this RectTransform trans)
    {
        return trans.rect.width;
    }

    public static float GetHeight(this RectTransform trans)
    {
        return trans.rect.height;
    }

    public static void SetPositionOfPivot(this RectTransform trans, Vector2 newPos)
    {
        trans.localPosition = new Vector3(newPos.x, newPos.y, trans.localPosition.z);
    }

    public static void SetLeftBottomPosition(this RectTransform trans, Vector2 newPos)
    {
        trans.localPosition = new Vector3(newPos.x + (trans.pivot.x * trans.rect.width),
            newPos.y + (trans.pivot.y * trans.rect.height), trans.localPosition.z);
    }

    public static void SetLeftTopPosition(this RectTransform trans, Vector2 newPos)
    {
        trans.localPosition = new Vector3(newPos.x + (trans.pivot.x * trans.rect.width),
            newPos.y - ((1f - trans.pivot.y) * trans.rect.height), trans.localPosition.z);
    }

    public static void SetRightBottomPosition(this RectTransform trans, Vector2 newPos)
    {
        trans.localPosition = new Vector3(newPos.x - ((1f - trans.pivot.x) * trans.rect.width),
            newPos.y + (trans.pivot.y * trans.rect.height), trans.localPosition.z);
    }

    public static void SetRightTopPosition(this RectTransform trans, Vector2 newPos)
    {
        trans.localPosition = new Vector3(newPos.x - ((1f - trans.pivot.x) * trans.rect.width),
            newPos.y - ((1f - trans.pivot.y) * trans.rect.height), trans.localPosition.z);
    }

    public static void SetSize(this RectTransform trans, Vector2 newSize)
    {
        var oldSize = trans.rect.size;
        var deltaSize = newSize - oldSize;
        trans.offsetMin = trans.offsetMin - new Vector2(deltaSize.x * trans.pivot.x, deltaSize.y * trans.pivot.y);
        trans.offsetMax = trans.offsetMax +
                          new Vector2(deltaSize.x * (1f - trans.pivot.x), deltaSize.y * (1f - trans.pivot.y));
    }

    public static void SetWidth(this RectTransform trans, float newSize)
    {
        SetSize(trans, new Vector2(newSize, trans.rect.size.y));
    }

    public static void SetHeight(this RectTransform trans, float newSize)
    {
        SetSize(trans, new Vector2(trans.rect.size.x, newSize));
    }

    /// <summary>
    /// Represents values for a rect anchor setting.
    /// </summary>
    private struct RectSetting
    {
        /// <summary>
        /// The anchor's max values.
        /// </summary>
        public Vector2 anchorMax;

        /// <summary>
        /// The anchor's min values.
        /// </summary>
        public Vector2 anchorMin;

        /// <summary>
        /// The pivot values.
        /// </summary>
        public Vector2 pivot;

        /// <summary>
        /// Initializes the rectangle setting.
        /// </summary>
        /// <param name="xMin">The minimum x value.</param>
        /// <param name="xMax">The maximum x value.</param>
        /// <param name="yMin">The minimum y value.</param>
        /// <param name="yMax">The maximum y value.</param>
        /// <param name="xPivot">The pivot value on the x axis.</param>
        /// <param name="yPivot">The pivot value on the y axis.</param>
        public RectSetting(float xMin, float xMax, float yMin, float yMax, float xPivot, float yPivot)
        {
            anchorMax = new Vector2(xMax, yMax);
            anchorMin = new Vector2(xMin, yMin);
            pivot = new Vector2(xPivot, yPivot);
        }
    }

    /// <summary>
    /// Holds the preset values used for each anchor setting.
    /// </summary>
    private static readonly Dictionary<RectAnchor, RectSetting> _anchorPresets = new Dictionary<RectAnchor, RectSetting>
    {
        { RectAnchor.TOP_LEFT, new RectSetting(0f, 0f, 1f, 1f, 0f, 1f) },
        { RectAnchor.TOP_CENTER, new RectSetting(0.5f, 0.5f, 1f, 1f, 0.5f, 1f) },
        { RectAnchor.TOP_RIGHT, new RectSetting(1f, 1f, 1f, 1f, 1f, 1f) },
        { RectAnchor.MIDDLE_LEFT, new RectSetting(0f, 0f, 0.5f, 0.5f, 0f, 0.5f) },
        { RectAnchor.MIDDLE_CENTER, new RectSetting(0.5f, 0.5f, 0.5f, 0.5f, 0.5f, 0.5f) },
        { RectAnchor.MIDDLE_RIGHT, new RectSetting(1f, 1f, 0.5f, 0.5f, 1f, 0.5f) },
        { RectAnchor.BOTTOM_LEFT, new RectSetting(0f, 0f, 0f, 0f, 0f, 0f) },
        { RectAnchor.BOTTOM_CENTER, new RectSetting(0.5f, 0.5f, 0f, 0f, 0.5f, 0f) },
        { RectAnchor.BOTTOM_RIGHT, new RectSetting(1f, 1f, 0f, 0f, 1f, 0f) },
        { RectAnchor.STRETCH_TOP, new RectSetting(0f, 1f, 1f, 1f, 0.5f, 1f) },
        { RectAnchor.STRETCH_MIDDLE, new RectSetting(0f, 1f, 0.5f, 0.5f, 0.5f, 0.5f) },
        { RectAnchor.STRETCH_BOTTOM, new RectSetting(0f, 1f, 0f, 0f, 0.5f, 0f) },
        { RectAnchor.STRETCH_LEFT, new RectSetting(0f, 0f, 0f, 1f, 0f, 0.5f) },
        { RectAnchor.STRETCH_CENTER, new RectSetting(0.5f, 0.5f, 0f, 1f, 0.5f, 0.5f) },
        { RectAnchor.STRETCH_RIGHT, new RectSetting(1f, 1f, 0f, 1f, 1f, 0.5f) },
        { RectAnchor.STRETCH_FULL, new RectSetting(0f, 1f, 0f, 1f, 0.5f, 0.5f) },
    };

    /// <summary>
    /// Sets the anchor values.
    /// </summary>
    /// <param name="transform">The rectangle transform.</param>
    /// <param name="xMin">The minimum x value.</param>
    /// <param name="xMax">The maximum x value.</param>
    /// <param name="yMin">The minimum y value.</param>
    /// <param name="yMax">The maximum y value.</param>
    public static void SetAnchor(this RectTransform transform, float xMin, float xMax, float yMin, float yMax)
    {
        if (transform == null)
            throw new ArgumentNullException(nameof(transform));

        transform.anchorMin = new Vector2(xMin, yMin);
        transform.anchorMax = new Vector2(xMax, yMax);
    }

    /// <summary>
    /// Sets the anchor to a given setting.
    /// </summary>
    /// <param name="transform">The rectangle transform.</param>
    /// <param name="anchor">The anchor setting to use.</param>
    /// <param name="setPivot">Whether the pivot should also be set based on the new setting.</param>
    /// <param name="setPosition">Whether to set the position after the setting has been applied.</param>
    public static void SetAnchor(
        this RectTransform transform,
        RectAnchor anchor,
        bool setPivot = false,
        bool setPosition = false)
    {
        if (transform == null)
            throw new ArgumentNullException(nameof(transform));

        RectSetting setting = _anchorPresets[anchor];
        SetAnchor(transform, setting.anchorMin.x, setting.anchorMax.x, setting.anchorMin.y, setting.anchorMax.y);

        if (setPivot)
            transform.pivot = setting.pivot;

        if (setPosition)
            transform.anchoredPosition = Vector2.zero;
    }

    /// <summary>
    /// Returns the world rectangle of a rectangle transform.
    /// </summary>
    /// <param name="transform">The rectangle transform.</param>
    /// <returns>The world rectangle.</returns>
    public static Rect GetWorldRect(this RectTransform transform)
    {
        if (transform == null)
            throw new ArgumentNullException(nameof(transform));

        Vector3[] corners = new Vector3[4];
        transform.GetWorldCorners(corners);

        Vector3 bottomLeft = corners[0];

        Vector2 size = new Vector2(
            transform.lossyScale.x * transform.rect.size.x,
            transform.lossyScale.y * transform.rect.size.y);

        return new Rect(bottomLeft, size);
    }

    /// <summary>
    /// Returns the rectangle transform. Will return null if a normal transform is used.
    /// </summary>
    /// <param name="component">The component of which to get the rectangle transform.</param>
    /// <returns>The rectangle transform instance.</returns>
    public static RectTransform GetRectTransform(this Component component)
    {
        if (component == null)
            throw new ArgumentNullException(nameof(component));

        return component.transform as RectTransform;
    }

    /// <summary>
    /// Returns the rectangle transform. Will return null if a normal transform is used.
    /// </summary>
    /// <param name="gameObject">The game object of which to get the rectangle transform.</param>
    /// <returns>The rectangle transform instance.</returns>
    public static RectTransform GetRectTransform(this GameObject gameObject)
    {
        if (gameObject == null)
            throw new ArgumentNullException(nameof(gameObject));

        return gameObject.transform as RectTransform;
    }

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

    public static Rect
        ToScreenSpace(
            this RectTransform transform)
    {
        Vector2 size = Vector2.Scale(transform.rect.size, transform.lossyScale);
        Rect rect = new Rect(transform.position.x, Screen.height - transform.position.y, size.x, size.y);
        rect.x -= (transform.pivot.x * size.x);
        rect.y -= ((1.0f - transform.pivot.y) * size.y);
        return rect;
    }
}

public enum RectAnchor
{
    TOP_LEFT = 0,
    TOP_CENTER = 1,
    TOP_RIGHT = 2,
    MIDDLE_LEFT = 3,
    MIDDLE_CENTER = 4,
    MIDDLE_RIGHT = 5,
    BOTTOM_LEFT = 6,
    BOTTOM_CENTER = 7,
    BOTTOM_RIGHT = 8,
    STRETCH_TOP = 9,
    STRETCH_MIDDLE = 10,
    STRETCH_BOTTOM = 11,
    STRETCH_LEFT = 12,
    STRETCH_CENTER = 13,
    STRETCH_RIGHT = 14,
    STRETCH_FULL = 15
}

/// <summary>
/// Class containing a minimum and max Vectors with its components between 0 and 1
/// </summary>
public struct MinMax01
{
    public Vector2 min { get; private set; }
    public Vector2 max { get; private set; }

    public MinMax01(Vector2 min, Vector2 max)
    {
        this.min = new Vector2(Mathf.Clamp01(min.x), Mathf.Clamp01(min.y));
        this.max = new Vector2(Mathf.Clamp01(max.x), Mathf.Clamp01(max.y));
    }

    public MinMax01(float minx, float miny, float maxx, float maxy)
    {
        this.min = new Vector2(Mathf.Clamp01(minx), Mathf.Clamp01(miny));
        this.max = new Vector2(Mathf.Clamp01(maxx), Mathf.Clamp01(maxy));
    }
}