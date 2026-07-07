using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;

public static class EventSystemExtensions
{
    #region IsPointerOnUIElement

    private static int _uiLayer = -2;

    private static int UiLayer
    {
        get
        {
            if (_uiLayer == -2)
                _uiLayer = LayerMask.NameToLayer("UI");
            return _uiLayer;
        }
    }

    public static bool IsPointerOverUIElement(this EventSystem eventSystem, GameObject gameObject)
    {
        return IsPointerOverUIElement(GetEventSystemRaycastResults(eventSystem), gameObject);
    }

    private static bool IsPointerOverUIElement(List<RaycastResult> eventSystemRaysastResults, GameObject gameObject)
    {
        int uiLayer = UiLayer;
        for (int index = 0; index < eventSystemRaysastResults.Count; index++)
        {
            RaycastResult curRaysastResult = eventSystemRaysastResults[index];
            if (curRaysastResult.gameObject.layer == uiLayer &&
                curRaysastResult.gameObject == gameObject)
                return true;
        }

        return false;
    }

    private static List<RaycastResult> GetEventSystemRaycastResults(EventSystem eventSystem)
    {
        PointerEventData eventData = new PointerEventData(eventSystem);
        eventData.position = Mouse.current != null ? Mouse.current.position.ReadValue() : Vector2.zero;
        List<RaycastResult> raysastResults = new List<RaycastResult>();
        eventSystem.RaycastAll(eventData, raysastResults);
        return raysastResults;
    }

    #endregion
}