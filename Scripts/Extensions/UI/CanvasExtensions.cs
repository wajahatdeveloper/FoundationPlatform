using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public static class CanvasExtensions
{
    /// <summary>
    /// Toggle CanvasGroup Alpha, Interactable and BlocksRaycasts settings
    /// </summary>
    public static void SetCanvasState(CanvasGroup canvas, bool setOn)
    {
        canvas.alpha = setOn ? 1 : 0;
        canvas.interactable = setOn;
        canvas.blocksRaycasts = setOn;
    }

    /// <summary>
    /// Toggle CanvasGroup Alpha, Interactable and BlocksRaycasts settings
    /// </summary>
    public static void SetState(this CanvasGroup canvas, bool isOn)
    {
        SetCanvasState(canvas, isOn);
    }

    /// <summary>
    /// Get scale factor which canvas scaler calculated when work in <see cref="CanvasScaler.ScaleMode.ScaleWithScreenSize"/> mode.
    /// </summary>
    /// <param name="scaler">The canvas scaler.</param>
    /// <returns>Calculated scale factor.</returns>
    public static float GetScaleFactor(this CanvasScaler scaler)
    {
        return Mathf.Lerp(Screen.width / scaler.referenceResolution.x, Screen.height / scaler.referenceResolution.y,
            scaler.matchWidthOrHeight);
    }
}