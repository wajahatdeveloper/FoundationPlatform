using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Essential Camera extension methods for Unity development
/// </summary>
namespace AetherNexus.FoundationPlatform.Extensions
{
public static class CameraExtensions
{

#region Camera Control

    #endregion

    #region Position and Rotation

    /// <summary>
    /// Sets the camera's position
    /// </summary>
    /// <param name="camera">Camera to modify</param>
    /// <param name="position">Position to set</param>
    public static void SetPosition(this Camera camera, Vector3 position)
    {
        camera.transform.position = position;
    }

    /// <summary>
    /// Sets the camera's rotation
    /// </summary>
    /// <param name="camera">Camera to modify</param>
    /// <param name="rotation">Rotation to set</param>
    public static void SetRotation(this Camera camera, Quaternion rotation)
    {
        camera.transform.rotation = rotation;
    }

    /// <summary>
    /// Sets the camera's position and rotation
    /// </summary>
    /// <param name="camera">Camera to modify</param>
    /// <param name="position">Position to set</param>
    /// <param name="rotation">Rotation to set</param>
    public static void SetPositionAndRotation(this Camera camera, Vector3 position, Quaternion rotation)
    {
        camera.transform.SetPositionAndRotation(position, rotation);
    }

    /// <summary>
    /// Looks at a target position
    /// </summary>
    /// <param name="camera">Camera to modify</param>
    /// <param name="target">Target position to look at</param>
    /// <param name="up">Up vector (default: Vector3.up)</param>
    public static void LookAt(this Camera camera, Vector3 target, Vector3 up)
    {
        if (up == default) up = Vector3.up;
        camera.transform.LookAt(target, up);
    }

    /// <summary>Looks at a target position, using Vector3.up.</summary>
    public static void LookAt(this Camera camera, Vector3 target) => LookAt(camera, target, default);

    /// <summary>
    /// Looks at a target transform
    /// </summary>
    /// <param name="camera">Camera to modify</param>
    /// <param name="target">Target transform to look at</param>
    /// <param name="up">Up vector (default: Vector3.up)</param>
    public static void LookAt(this Camera camera, Transform target, Vector3 up)
    {
        if (target == null) return;
        camera.LookAt(target.position, up);
    }

    /// <summary>Looks at a target transform, using Vector3.up.</summary>
    public static void LookAt(this Camera camera, Transform target) => LookAt(camera, target, default);

    #endregion

    #region Movement

    /// <summary>
    /// Moves the camera towards a target position
    /// </summary>
    /// <param name="camera">Camera to move</param>
    /// <param name="target">Target position</param>
    /// <param name="speed">Movement speed</param>
    /// <param name="smoothTime">Smooth time for movement</param>
    /// <returns>Coroutine for smooth movement</returns>
    public static IEnumerator MoveTo(this Camera camera, Vector3 target, float speed, float smoothTime)
    {
        Vector3 startPosition = camera.transform.position;
        float elapsed = 0f;
        float duration = smoothTime > 0f ? smoothTime : (speed > 0f ? Vector3.Distance(startPosition, target) / speed : 0f);

        // Guard against zero/negative speed (or smoothTime) which would make duration
        // 0/Infinity/NaN and never terminate the loop. Snap to target immediately.
        if (!(duration > 0f) || float.IsInfinity(duration) || float.IsNaN(duration))
        {
            camera.transform.position = target;
            yield break;
        }

        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float t = elapsed / duration;
            camera.transform.position = Vector3.Lerp(startPosition, target, t);
            yield return null;
        }

        camera.transform.position = target;
    }

    /// <summary>Moves the camera towards a target position, using a speed of 1 and no fixed smooth time.</summary>
    public static IEnumerator MoveTo(this Camera camera, Vector3 target) => MoveTo(camera, target, 1f, 0f);

#endregion
#region Field of View

    #endregion

    #region Screen and World Conversion

    /// <summary>
    /// Converts a screen position to world position
    /// </summary>
    /// <param name="camera">Camera to use for conversion</param>
    /// <param name="screenPosition">Screen position</param>
    /// <param name="distance">Distance from camera</param>
    /// <returns>World position</returns>
    public static Vector3 ScreenToWorldPoint(this Camera camera, Vector3 screenPosition, float distance)
    {
        return camera.ScreenToWorldPoint(new Vector3(screenPosition.x, screenPosition.y, distance));
    }

    /// <summary>Converts a screen position to world position, using a distance of 10 from the camera.</summary>
    public static Vector3 ScreenToWorldPoint(this Camera camera, Vector3 screenPosition) => ScreenToWorldPoint(camera, screenPosition, 10f);

#endregion
#region Raycasting

    #endregion

    #region Viewport

    /// <summary>
    /// Converts a viewport position to world position
    /// </summary>
    /// <param name="camera">Camera to use for conversion</param>
    /// <param name="viewportPosition">Viewport position (0-1)</param>
    /// <param name="distance">Distance from camera</param>
    /// <returns>World position</returns>
    public static Vector3 ViewportToWorldPoint(this Camera camera, Vector3 viewportPosition, float distance)
    {
        return camera.ViewportToWorldPoint(new Vector3(viewportPosition.x, viewportPosition.y, distance));
    }

    /// <summary>Converts a viewport position to world position, using a distance of 10 from the camera.</summary>
    public static Vector3 ViewportToWorldPoint(this Camera camera, Vector3 viewportPosition) => ViewportToWorldPoint(camera, viewportPosition, 10f);

    #endregion

    #region Utility Methods

    /// <summary>
    /// Gets the camera's forward direction
    /// </summary>
    /// <param name="camera">Camera to get direction from</param>
    /// <returns>Forward direction vector</returns>
    public static Vector3 GetForwardDirection(this Camera camera)
    {
        return camera.transform.forward;
    }

    /// <summary>
    /// Resets the camera to its default state
    /// </summary>
    /// <param name="camera">Camera to reset</param>
    public static void Reset(this Camera camera)
    {
        camera.transform.position = Vector3.zero;
        camera.transform.rotation = Quaternion.identity;
        camera.fieldOfView = 60f;
        camera.orthographicSize = 5f;
        camera.nearClipPlane = 0.3f;
        camera.farClipPlane = 1000f;
        camera.aspect = 16f / 9f;
    }

    #endregion
}}
