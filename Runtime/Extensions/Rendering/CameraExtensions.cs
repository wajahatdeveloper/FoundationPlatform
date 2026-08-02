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

    /// <summary>
    /// Sets the camera's field of view
    /// </summary>
    /// <param name="camera">Camera to modify</param>
    /// <param name="fov">Field of view value</param>
    public static void SetFieldOfView(this Camera camera, float fov)
    {
        camera.fieldOfView = Mathf.Clamp(fov, 1f, 179f);
    }

    /// <summary>
    /// Sets the camera's orthographic size
    /// </summary>
    /// <param name="camera">Camera to modify</param>
    /// <param name="size">Orthographic size value</param>
    public static void SetOrthographicSize(this Camera camera, float size)
    {
        camera.orthographicSize = Mathf.Max(0.1f, size);
    }

    /// <summary>
    /// Sets the camera's near clipping plane
    /// </summary>
    /// <param name="camera">Camera to modify</param>
    /// <param name="near">Near clipping plane value</param>
    public static void SetNearClipPlane(this Camera camera, float near)
    {
        camera.nearClipPlane = Mathf.Max(0.01f, near);
    }

    /// <summary>
    /// Sets the camera's far clipping plane
    /// </summary>
    /// <param name="camera">Camera to modify</param>
    /// <param name="far">Far clipping plane value</param>
    public static void SetFarClipPlane(this Camera camera, float far)
    {
        camera.farClipPlane = Mathf.Max(camera.nearClipPlane + 0.01f, far);
    }

    /// <summary>
    /// Sets the camera's aspect ratio
    /// </summary>
    /// <param name="camera">Camera to modify</param>
    /// <param name="aspect">Aspect ratio value</param>
    public static void SetAspectRatio(this Camera camera, float aspect)
    {
        camera.aspect = Mathf.Max(0.1f, aspect);
    }

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

    /// <summary>
    /// Smoothly rotates the camera towards a target rotation
    /// </summary>
    /// <param name="camera">Camera to rotate</param>
    /// <param name="target">Target rotation</param>
    /// <param name="speed">Rotation speed</param>
    /// <param name="smoothTime">Smooth time for rotation</param>
    /// <returns>Coroutine for smooth rotation</returns>
    public static IEnumerator RotateTo(this Camera camera, Quaternion target, float speed, float smoothTime)
    {
        Quaternion startRotation = camera.transform.rotation;
        float elapsed = 0f;
        float duration = smoothTime > 0f ? smoothTime : (speed > 0f ? Quaternion.Angle(startRotation, target) / (speed * 90f) : 0f);

        // Guard against zero/negative speed (or smoothTime) which would make duration
        // 0/Infinity/NaN and never terminate the loop. Snap to target immediately.
        if (!(duration > 0f) || float.IsInfinity(duration) || float.IsNaN(duration))
        {
            camera.transform.rotation = target;
            yield break;
        }

        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float t = elapsed / duration;
            camera.transform.rotation = Quaternion.Lerp(startRotation, target, t);
            yield return null;
        }

        camera.transform.rotation = target;
    }

    /// <summary>Smoothly rotates the camera towards a target rotation, using a speed of 1 and no fixed smooth time.</summary>
    public static IEnumerator RotateTo(this Camera camera, Quaternion target) => RotateTo(camera, target, 1f, 0f);

    /// <summary>
    /// Smoothly moves and rotates the camera to look at a target
    /// </summary>
    /// <param name="camera">Camera to move</param>
    /// <param name="targetPosition">Target position</param>
    /// <param name="targetLookAt">Position to look at</param>
    /// <param name="speed">Movement speed</param>
    /// <param name="smoothTime">Smooth time</param>
    /// <returns>Coroutine for smooth movement and rotation</returns>
    public static IEnumerator MoveAndLookAt(this Camera camera, Vector3 targetPosition, Vector3 targetLookAt, float speed, float smoothTime)
    {
        Vector3 startPosition = camera.transform.position;
        Quaternion startRotation = camera.transform.rotation;
        Quaternion targetRotation = Quaternion.LookRotation(targetLookAt - targetPosition);
        
        float elapsed = 0f;
        float duration = smoothTime > 0f ? smoothTime : (speed > 0f ? Vector3.Distance(startPosition, targetPosition) / speed : 0f);

        // Guard against zero/negative speed (or smoothTime) which would make duration
        // 0/Infinity/NaN and never terminate the loop. Snap to target immediately.
        if (!(duration > 0f) || float.IsInfinity(duration) || float.IsNaN(duration))
        {
            camera.transform.position = targetPosition;
            camera.transform.rotation = targetRotation;
            yield break;
        }

        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float t = elapsed / duration;
            camera.transform.position = Vector3.Lerp(startPosition, targetPosition, t);
            camera.transform.rotation = Quaternion.Lerp(startRotation, targetRotation, t);
            yield return null;
        }

        camera.transform.position = targetPosition;
        camera.transform.rotation = targetRotation;
    }

    /// <summary>Smoothly moves and rotates the camera to look at a target, using a speed of 1 and no fixed smooth time.</summary>
    public static IEnumerator MoveAndLookAt(this Camera camera, Vector3 targetPosition, Vector3 targetLookAt) => MoveAndLookAt(camera, targetPosition, targetLookAt, 1f, 0f);

    #endregion

    #region Field of View

    /// <summary>
    /// Smoothly changes the field of view
    /// </summary>
    /// <param name="camera">Camera to modify</param>
    /// <param name="targetFov">Target field of view</param>
    /// <param name="duration">Transition duration</param>
    /// <returns>Coroutine for smooth FOV change</returns>
    public static IEnumerator ChangeFieldOfView(this Camera camera, float targetFov, float duration)
    {
        float startFov = camera.fieldOfView;
        float elapsed = 0f;

        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float t = elapsed / duration;
            camera.fieldOfView = Mathf.Lerp(startFov, targetFov, t);
            yield return null;
        }

        camera.fieldOfView = targetFov;
    }

    /// <summary>
    /// Smoothly changes the orthographic size
    /// </summary>
    /// <param name="camera">Camera to modify</param>
    /// <param name="targetSize">Target orthographic size</param>
    /// <param name="duration">Transition duration</param>
    /// <returns>Coroutine for smooth size change</returns>
    public static IEnumerator ChangeOrthographicSize(this Camera camera, float targetSize, float duration)
    {
        float startSize = camera.orthographicSize;
        float elapsed = 0f;

        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float t = elapsed / duration;
            camera.orthographicSize = Mathf.Lerp(startSize, targetSize, t);
            yield return null;
        }

        camera.orthographicSize = targetSize;
    }

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

    /// <summary>
    /// Converts a screen position to world position at a specific plane
    /// </summary>
    /// <param name="camera">Camera to use for conversion</param>
    /// <param name="screenPosition">Screen position</param>
    /// <param name="plane">Plane to intersect with</param>
    /// <returns>World position on the plane</returns>
    public static Vector3 ScreenToWorldPointOnPlane(this Camera camera, Vector3 screenPosition, Plane plane)
    {
        Ray ray = camera.ScreenPointToRay(screenPosition);
        if (plane.Raycast(ray, out float distance))
        {
            return ray.GetPoint(distance);
        }
        return Vector3.zero;
    }

    /// <summary>
    /// Converts a screen position to world position on the ground plane (Y = 0)
    /// </summary>
    /// <param name="camera">Camera to use for conversion</param>
    /// <param name="screenPosition">Screen position</param>
    /// <returns>World position on the ground plane</returns>
    public static Vector3 ScreenToWorldPointOnGround(this Camera camera, Vector3 screenPosition)
    {
        Plane groundPlane = new Plane(Vector3.up, Vector3.zero);
        return camera.ScreenToWorldPointOnPlane(screenPosition, groundPlane);
    }

    #endregion

    #region Raycasting

    /// <summary>
    /// Creates a ray from the camera through the center of the screen
    /// </summary>
    /// <param name="camera">Camera to create ray from</param>
    /// <returns>Ray from camera through screen center</returns>
    public static Ray ScreenCenterToRay(this Camera camera)
    {
        return camera.ScreenPointToRay(new Vector3(Screen.width * 0.5f, Screen.height * 0.5f, 0f));
    }

    /// <summary>
    /// Performs a raycast from the camera through a screen point
    /// </summary>
    /// <param name="camera">Camera to raycast from</param>
    /// <param name="screenPosition">Screen position</param>
    /// <param name="maxDistance">Maximum raycast distance</param>
    /// <param name="layerMask">Layer mask</param>
    /// <returns>Raycast hit info</returns>
    public static RaycastHit RaycastFromScreen(this Camera camera, Vector3 screenPosition, float maxDistance, LayerMask layerMask)
    {
        Ray ray = camera.ScreenPointToRay(screenPosition);
        Physics.Raycast(ray, out RaycastHit hit, maxDistance, layerMask);
        return hit;
    }

    /// <summary>RaycastFromScreen using Unity's own defaults (infinite distance, all layers).</summary>
    public static RaycastHit RaycastFromScreen(this Camera camera, Vector3 screenPosition) =>
        RaycastFromScreen(camera, screenPosition, Mathf.Infinity, default);

    /// <summary>
    /// Performs a raycast from the camera through the center of the screen
    /// </summary>
    /// <param name="camera">Camera to raycast from</param>
    /// <param name="maxDistance">Maximum raycast distance</param>
    /// <param name="layerMask">Layer mask</param>
    /// <returns>Raycast hit info</returns>
    public static RaycastHit RaycastFromScreenCenter(this Camera camera, float maxDistance, LayerMask layerMask)
    {
        return camera.RaycastFromScreen(new Vector3(Screen.width * 0.5f, Screen.height * 0.5f, 0f), maxDistance, layerMask);
    }

    /// <summary>RaycastFromScreenCenter using Unity's own defaults (infinite distance, all layers).</summary>
    public static RaycastHit RaycastFromScreenCenter(this Camera camera) =>
        RaycastFromScreenCenter(camera, Mathf.Infinity, default);

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

    /// <summary>
    /// Checks if a world position is visible in the camera's viewport
    /// </summary>
    /// <param name="camera">Camera to check against</param>
    /// <param name="worldPosition">World position to check</param>
    /// <returns>True if visible in viewport</returns>
    public static bool IsInViewport(this Camera camera, Vector3 worldPosition)
    {
        Vector3 viewportPos = camera.WorldToViewportPoint(worldPosition);
        return viewportPos.x >= 0f && viewportPos.x <= 1f && 
               viewportPos.y >= 0f && viewportPos.y <= 1f && 
               viewportPos.z > 0f;
    }

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
    /// Gets the camera's right direction
    /// </summary>
    /// <param name="camera">Camera to get direction from</param>
    /// <returns>Right direction vector</returns>
    public static Vector3 GetRightDirection(this Camera camera)
    {
        return camera.transform.right;
    }

    /// <summary>
    /// Gets the camera's up direction
    /// </summary>
    /// <param name="camera">Camera to get direction from</param>
    /// <returns>Up direction vector</returns>
    public static Vector3 GetUpDirection(this Camera camera)
    {
        return camera.transform.up;
    }

    /// <summary>
    /// Gets the camera's viewport bounds in world space
    /// </summary>
    /// <param name="camera">Camera to get bounds from</param>
    /// <param name="distance">Distance from camera</param>
    /// <returns>Viewport bounds</returns>
    public static Bounds GetViewportBounds(this Camera camera, float distance)
    {
        Vector3 bottomLeft = camera.ViewportToWorldPoint(new Vector3(0f, 0f, distance));
        Vector3 topRight = camera.ViewportToWorldPoint(new Vector3(1f, 1f, distance));

        Vector3 center = (bottomLeft + topRight) * 0.5f;
        Vector3 size = topRight - bottomLeft;

        return new Bounds(center, size);
    }

    /// <summary>Gets the camera's viewport bounds in world space, using a distance of 10 from the camera.</summary>
    public static Bounds GetViewportBounds(this Camera camera) => GetViewportBounds(camera, 10f);

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

    /// <summary>
    /// Checks if a world point is in the camera's viewport
    /// </summary>
    /// <param name="camera">Camera to check against</param>
    /// <param name="point">World point to check</param>
    /// <returns>True if the point is in the viewport</returns>
    public static bool IsWorldPointInViewport(this Camera camera, Vector3 point)
    {
        var position = camera.WorldToViewportPoint(point);
        return position.x > 0 && position.y > 0;
    }

    /// <summary>
    /// Gets a point with the same screen point as the source point,
    /// but at the specified distance from camera.
    /// </summary>
    /// <param name="camera">Camera to use</param>
    /// <param name="source">Source world point</param>
    /// <param name="distanceFromCamera">Distance from camera</param>
    /// <param name="eye">Mono or stereoscopic eye</param>
    /// <returns>World point at the specified distance</returns>
    public static Vector3 WorldPointOffsetByDepth(this Camera camera,
        Vector3 source,
        float distanceFromCamera,
        Camera.MonoOrStereoscopicEye eye)
    {
        var screenPoint = camera.WorldToScreenPoint(source, eye);
        return camera.ScreenToWorldPoint(screenPoint.SetZ(distanceFromCamera),
            eye);
    }

    /// <summary>Offsets a world point by depth, using the mono eye.</summary>
    public static Vector3 WorldPointOffsetByDepth(this Camera camera, Vector3 source, float distanceFromCamera) =>
        WorldPointOffsetByDepth(camera, source, distanceFromCamera, Camera.MonoOrStereoscopicEye.Mono);

    #endregion
}}
