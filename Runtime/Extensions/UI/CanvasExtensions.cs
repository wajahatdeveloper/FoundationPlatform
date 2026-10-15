using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace AetherNexus.FoundationPlatform.Extensions
{
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
}}
