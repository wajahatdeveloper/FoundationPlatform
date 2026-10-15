using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;

/// <summary>
/// Essential UI extension methods for Unity development
/// </summary>
namespace AetherNexus.FoundationPlatform.Extensions
{
public static class UIExtensions
{

#region Canvas Extensions
#endregion
#region Button Extensions

    /// <summary>
    /// Enables or disables a button
    /// </summary>
    /// <param name="button">Button to modify</param>
    /// <param name="enabled">Enable state</param>
    public static void SetEnabled(this Button button, bool enabled)
    {
        button.interactable = enabled;
    }

    /// <summary>
    /// Toggles the enabled state of a button
    /// </summary>
    /// <param name="button">Button to toggle</param>
    public static void ToggleEnabled(this Button button)
    {
        button.interactable = !button.interactable;
    }

    #endregion

    #region Text Extensions

    /// <summary>
    /// Sets the text content of a Text component
    /// </summary>
    /// <param name="text">Text component</param>
    /// <param name="content">Text content</param>
    public static void SetText(this Text text, string content)
    {
        text.text = content;
    }

    /// <summary>
    /// Sets the text content of a Text component with formatting
    /// </summary>
    /// <param name="text">Text component</param>
    /// <param name="format">Format string</param>
    /// <param name="args">Format arguments</param>
    public static void SetText(this Text text, string format, params object[] args)
    {
        text.text = string.Format(format, args);
    }

    /// <summary>
    /// Appends text to a Text component
    /// </summary>
    /// <param name="text">Text component</param>
    /// <param name="content">Text to append</param>
    public static void AppendText(this Text text, string content)
    {
        text.text += content;
    }

    #endregion

    #region Image Extensions

    /// <summary>
    /// Sets the color of an Image component
    /// </summary>
    /// <param name="image">Image component</param>
    /// <param name="color">Color to set</param>
    public static void SetColor(this Image image, Color color)
    {
        image.color = color;
    }

    /// <summary>
    /// Sets the alpha of an Image component
    /// </summary>
    /// <param name="image">Image component</param>
    /// <param name="alpha">Alpha value (0-1)</param>
    public static void SetAlpha(this Image image, float alpha)
    {
        Color color = image.color;
        color.a = Mathf.Clamp01(alpha);
        image.color = color;
    }

#endregion
#region RectTransform Extensions
#endregion
#region ScrollRect Extensions
#endregion
#region Toggle Extensions

    #endregion

    #region Slider Extensions

    /// <summary>
    /// Sets the slider value
    /// </summary>
    /// <param name="slider">Slider to modify</param>
    /// <param name="value">Value to set</param>
    public static void SetValue(this Slider slider, float value)
    {
        slider.value = value;
    }

    #endregion

    #region InputField Extensions

    /// <summary>
    /// Sets the input field text
    /// </summary>
    /// <param name="inputField">InputField to modify</param>
    /// <param name="text">Text to set</param>
    public static void SetText(this InputField inputField, string text)
    {
        inputField.text = text;
    }

    #endregion

    #region Event System Extensions

    /// <summary>
    /// Checks if the mouse is over a UI element
    /// </summary>
    /// <returns>True if mouse is over UI</returns>
    public static bool IsMouseOverUI()
    {
        return EventSystem.current != null && EventSystem.current.IsPointerOverGameObject();
    }

#endregion
#region Layout Group Extensions
#endregion
#region Utility Methods

    /// <summary>
    /// Shows a UI element (sets active to true)
    /// </summary>
    /// <param name="gameObject">UI element to show</param>
    public static void Show(this GameObject gameObject)
    {
        gameObject.SetActive(true);
    }

    /// <summary>
    /// Hides a UI element (sets active to false)
    /// </summary>
    /// <param name="gameObject">UI element to hide</param>
    public static void Hide(this GameObject gameObject)
    {
        gameObject.SetActive(false);
    }

    /// <summary>
    /// Toggles the visibility of a UI element
    /// </summary>
    /// <param name="gameObject">UI element to toggle</param>
    public static void ToggleVisibility(this GameObject gameObject)
    {
        gameObject.SetActive(!gameObject.activeInHierarchy);
    }

    #endregion
}
}
