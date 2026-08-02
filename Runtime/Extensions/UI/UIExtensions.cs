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

    /// <summary>
    /// Gets the main canvas in the scene
    /// </summary>
    /// <returns>Main canvas, or null if not found</returns>
    public static Canvas GetMainCanvas()
    {
        return GameObject.FindFirstObjectByType<Canvas>();
    }

    /// <summary>
    /// Gets the canvas component on this GameObject or its parents
    /// </summary>
    /// <param name="gameObject">GameObject to check</param>
    /// <returns>Canvas component, or null if not found</returns>
    public static Canvas GetCanvas(this GameObject gameObject)
    {
        return gameObject.GetComponentInParent<Canvas>();
    }

    /// <summary>
    /// Gets the canvas component on this Component or its parents
    /// </summary>
    /// <param name="component">Component to check</param>
    /// <returns>Canvas component, or null if not found</returns>
    public static Canvas GetCanvas(this Component component)
    {
        return component.GetComponentInParent<Canvas>();
    }

    /// <summary>
    /// Checks if this GameObject is part of a UI canvas
    /// </summary>
    /// <param name="gameObject">GameObject to check</param>
    /// <returns>True if part of UI canvas</returns>
    public static bool IsUIElement(this GameObject gameObject)
    {
        return gameObject.GetComponentInParent<Canvas>() != null;
    }

    #endregion

    #region Button Extensions

    /// <summary>
    /// Adds a click listener to a button
    /// </summary>
    /// <param name="button">Button to add listener to</param>
    /// <param name="action">Action to execute on click</param>
    public static void AddClickListener(this Button button, Action action)
    {
        button.onClick.AddListener(() => action?.Invoke());
    }

    /// <summary>
    /// Removes all click listeners from a button
    /// </summary>
    /// <param name="button">Button to clear</param>
    public static void ClearClickListeners(this Button button)
    {
        button.onClick.RemoveAllListeners();
    }

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

    /// <summary>
    /// Clears the text content
    /// </summary>
    /// <param name="text">Text component</param>
    public static void ClearText(this Text text)
    {
        text.text = string.Empty;
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

    /// <summary>
    /// Fades an Image component to a target alpha
    /// </summary>
    /// <param name="image">Image component</param>
    /// <param name="targetAlpha">Target alpha value</param>
    /// <param name="duration">Fade duration</param>
    /// <returns>Coroutine for the fade</returns>
    public static IEnumerator FadeToAlpha(this Image image, float targetAlpha, float duration)
    {
        if (duration <= 0f)
        {
            image.SetAlpha(targetAlpha);
            yield break;
        }

        float startAlpha = image.color.a;
        float elapsed = 0f;

        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float alpha = Mathf.Lerp(startAlpha, targetAlpha, elapsed / duration);
            image.SetAlpha(alpha);
            yield return null;
        }

        image.SetAlpha(targetAlpha);
    }

    #endregion

    #region RectTransform Extensions

    /// <summary>
    /// Sets the anchored position of a RectTransform
    /// </summary>
    /// <param name="rectTransform">RectTransform to modify</param>
    /// <param name="position">Anchored position</param>
    public static void SetAnchoredPosition(this RectTransform rectTransform, Vector2 position)
    {
        rectTransform.anchoredPosition = position;
    }

    /// <summary>
    /// Sets the size delta of a RectTransform
    /// </summary>
    /// <param name="rectTransform">RectTransform to modify</param>
    /// <param name="size">Size delta</param>
    public static void SetSizeDelta(this RectTransform rectTransform, Vector2 size)
    {
        rectTransform.sizeDelta = size;
    }

    // Note: SetWidth/SetHeight(this RectTransform, float) live in RectTransformExtensions.cs — kept
    // there as the single source (their SetSize-based implementation also accounts for stretched
    // anchors correctly) to avoid an ambiguous-call (CS0121) between two identical-signature overloads
    // in this same namespace.

    /// <summary>
    /// Centers a RectTransform within its parent
    /// </summary>
    /// <param name="rectTransform">RectTransform to center</param>
    public static void CenterInParent(this RectTransform rectTransform)
    {
        rectTransform.anchorMin = new Vector2(0.5f, 0.5f);
        rectTransform.anchorMax = new Vector2(0.5f, 0.5f);
        rectTransform.anchoredPosition = Vector2.zero;
    }

    /// <summary>
    /// Stretches a RectTransform to fill its parent
    /// </summary>
    /// <param name="rectTransform">RectTransform to stretch</param>
    public static void StretchToFillParent(this RectTransform rectTransform)
    {
        rectTransform.anchorMin = Vector2.zero;
        rectTransform.anchorMax = Vector2.one;
        rectTransform.offsetMin = Vector2.zero;
        rectTransform.offsetMax = Vector2.zero;
    }

    #endregion

    #region ScrollRect Extensions

    /// <summary>
    /// Scrolls to the top of a ScrollRect
    /// </summary>
    /// <param name="scrollRect">ScrollRect to scroll</param>
    public static void ScrollToTop(this ScrollRect scrollRect)
    {
        scrollRect.normalizedPosition = new Vector2(scrollRect.normalizedPosition.x, 1f);
    }

    /// <summary>
    /// Scrolls to the bottom of a ScrollRect
    /// </summary>
    /// <param name="scrollRect">ScrollRect to scroll</param>
    public static void ScrollToBottom(this ScrollRect scrollRect)
    {
        scrollRect.normalizedPosition = new Vector2(scrollRect.normalizedPosition.x, 0f);
    }

    /// <summary>
    /// Scrolls to the left of a ScrollRect
    /// </summary>
    /// <param name="scrollRect">ScrollRect to scroll</param>
    public static void ScrollToLeft(this ScrollRect scrollRect)
    {
        scrollRect.normalizedPosition = new Vector2(0f, scrollRect.normalizedPosition.y);
    }

    /// <summary>
    /// Scrolls to the right of a ScrollRect
    /// </summary>
    /// <param name="scrollRect">ScrollRect to scroll</param>
    public static void ScrollToRight(this ScrollRect scrollRect)
    {
        scrollRect.normalizedPosition = new Vector2(1f, scrollRect.normalizedPosition.y);
    }

    #endregion

    #region Toggle Extensions

    /// <summary>
    /// Sets the toggle state
    /// </summary>
    /// <param name="toggle">Toggle to modify</param>
    /// <param name="isOn">Toggle state</param>
    public static void SetToggleState(this Toggle toggle, bool isOn)
    {
        toggle.isOn = isOn;
    }

    /// <summary>
    /// Toggles the toggle state
    /// </summary>
    /// <param name="toggle">Toggle to toggle</param>
    public static void ToggleState(this Toggle toggle)
    {
        toggle.isOn = !toggle.isOn;
    }

    /// <summary>
    /// Adds a value changed listener to a toggle
    /// </summary>
    /// <param name="toggle">Toggle to add listener to</param>
    /// <param name="action">Action to execute on value change</param>
    public static void AddValueChangedListener(this Toggle toggle, Action<bool> action)
    {
        toggle.onValueChanged.AddListener(value => action?.Invoke(value));
    }

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

    /// <summary>
    /// Sets the slider value with clamping
    /// </summary>
    /// <param name="slider">Slider to modify</param>
    /// <param name="value">Value to set</param>
    public static void SetValueClamped(this Slider slider, float value)
    {
        slider.value = Mathf.Clamp(value, slider.minValue, slider.maxValue);
    }

    /// <summary>
    /// Adds a value changed listener to a slider
    /// </summary>
    /// <param name="slider">Slider to add listener to</param>
    /// <param name="action">Action to execute on value change</param>
    public static void AddValueChangedListener(this Slider slider, Action<float> action)
    {
        slider.onValueChanged.AddListener(value => action?.Invoke(value));
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

    /// <summary>
    /// Clears the input field text
    /// </summary>
    /// <param name="inputField">InputField to clear</param>
    public static void ClearText(this InputField inputField)
    {
        inputField.text = string.Empty;
    }

    /// <summary>
    /// Sets the placeholder text
    /// </summary>
    /// <param name="inputField">InputField to modify</param>
    /// <param name="placeholderText">Placeholder text</param>
    public static void SetPlaceholderText(this InputField inputField, string placeholderText)
    {
        if (inputField.placeholder != null)
        {
            inputField.placeholder.GetComponent<Text>().text = placeholderText;
        }
    }

    /// <summary>
    /// Adds a value changed listener to an input field
    /// </summary>
    /// <param name="inputField">InputField to add listener to</param>
    /// <param name="action">Action to execute on value change</param>
    public static void AddValueChangedListener(this InputField inputField, Action<string> action)
    {
        inputField.onValueChanged.AddListener(value => action?.Invoke(value));
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

    /// <summary>
    /// Gets the UI element under the mouse cursor
    /// </summary>
    /// <returns>UI element under mouse, or null if none</returns>
    public static GameObject GetUIElementUnderMouse()
    {
        if (EventSystem.current == null) return null;

        Vector2 mousePosition = Mouse.current != null ? Mouse.current.position.ReadValue() : Vector2.zero;
        PointerEventData pointerData = new PointerEventData(EventSystem.current)
        {
            position = mousePosition
        };

        var results = new List<RaycastResult>();
        EventSystem.current.RaycastAll(pointerData, results);

        return results.Count > 0 ? results[0].gameObject : null;
    }

    #endregion

    #region Layout Group Extensions

    /// <summary>
    /// Forces a layout group to rebuild its layout
    /// </summary>
    /// <param name="layoutGroup">Layout group to rebuild</param>
    public static void RebuildLayout(this LayoutGroup layoutGroup)
    {
        LayoutRebuilder.ForceRebuildLayoutImmediate(layoutGroup.GetComponent<RectTransform>());
    }

    /// <summary>
    /// Forces a content size fitter to rebuild its layout
    /// </summary>
    /// <param name="contentSizeFitter">Content size fitter to rebuild</param>
    public static void RebuildLayout(this ContentSizeFitter contentSizeFitter)
    {
        LayoutRebuilder.ForceRebuildLayoutImmediate(contentSizeFitter.GetComponent<RectTransform>());
    }

    #endregion

    #region Utility Methods

    /// <summary>
    /// Finds a UI element by name in the scene
    /// </summary>
    /// <param name="name">Name of the UI element</param>
    /// <returns>UI element, or null if not found</returns>
    public static GameObject FindUIElement(string name)
    {
        Canvas[] canvases = GameObject.FindObjectsByType<Canvas>(FindObjectsSortMode.None);
        foreach (Canvas canvas in canvases)
        {
            Transform found = canvas.transform.Find(name);
            if (found != null) return found.gameObject;
        }
        return null;
    }

    /// <summary>
    /// Gets all UI elements of a specific type
    /// </summary>
    /// <typeparam name="T">Type of UI element</typeparam>
    /// <returns>Array of UI elements</returns>
    public static T[] GetAllUIElements<T>() where T : Component
    {
        return GameObject.FindObjectsByType<T>(FindObjectsSortMode.None);
    }

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
