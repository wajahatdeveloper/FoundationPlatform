using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Essential Physics extension methods for Unity development
/// </summary>
namespace AetherNexus.FoundationPlatform.Extensions
{
public static class PhysicsExtensions
{
    #region Raycast Utilities

    /// <summary>
    /// Performs a raycast from a position in a direction with optional parameters
    /// </summary>
    /// <param name="origin">Ray origin</param>
    /// <param name="direction">Ray direction</param>
    /// <param name="maxDistance">Maximum ray distance</param>
    /// <param name="layerMask">Layer mask</param>
    /// <param name="queryTriggerInteraction">Query trigger interaction</param>
    /// <returns>Raycast hit info</returns>
    public static RaycastHit Raycast(Vector3 origin, Vector3 direction, float maxDistance,
        LayerMask layerMask, QueryTriggerInteraction queryTriggerInteraction)
    {
        Physics.Raycast(origin, direction, out RaycastHit hit, maxDistance, layerMask, queryTriggerInteraction);
        return hit;
    }

    /// <summary>Raycast using Unity's own defaults (infinite distance, all layers, global trigger-query setting).</summary>
    public static RaycastHit Raycast(Vector3 origin, Vector3 direction) =>
        Raycast(origin, direction, Mathf.Infinity, default, QueryTriggerInteraction.UseGlobal);

    /// <summary>
    /// Performs a raycast from a position in the forward direction
    /// </summary>
    /// <param name="origin">Ray origin</param>
    /// <param name="forward">Forward direction</param>
    /// <param name="maxDistance">Maximum ray distance</param>
    /// <param name="layerMask">Layer mask</param>
    /// <param name="queryTriggerInteraction">Query trigger interaction</param>
    /// <returns>Raycast hit info</returns>
    public static RaycastHit RaycastForward(Vector3 origin, Vector3 forward, float maxDistance,
        LayerMask layerMask, QueryTriggerInteraction queryTriggerInteraction)
    {
        Physics.Raycast(origin, forward, out RaycastHit hit, maxDistance, layerMask, queryTriggerInteraction);
        return hit;
    }

    /// <summary>RaycastForward using Unity's own defaults (infinite distance, all layers, global trigger-query setting).</summary>
    public static RaycastHit RaycastForward(Vector3 origin, Vector3 forward) =>
        RaycastForward(origin, forward, Mathf.Infinity, default, QueryTriggerInteraction.UseGlobal);

    #endregion

    #region Overlap Detection

    /// <summary>
    /// Performs a sphere overlap check
    /// </summary>
    /// <param name="position">Sphere center</param>
    /// <param name="radius">Sphere radius</param>
    /// <param name="layerMask">Layer mask</param>
    /// <param name="queryTriggerInteraction">Query trigger interaction</param>
    /// <returns>Array of colliders</returns>
    public static Collider[] OverlapSphere(Vector3 position, float radius, LayerMask layerMask,
        QueryTriggerInteraction queryTriggerInteraction)
    {
        return Physics.OverlapSphere(position, radius, layerMask, queryTriggerInteraction);
    }

    /// <summary>Sphere overlap check using Unity's own defaults (all layers, global trigger-query setting).</summary>
    public static Collider[] OverlapSphere(Vector3 position, float radius) =>
        OverlapSphere(position, radius, default, QueryTriggerInteraction.UseGlobal);

    /// <summary>
    /// Performs a box overlap check
    /// </summary>
    /// <param name="center">Box center</param>
    /// <param name="halfExtents">Box half extents</param>
    /// <param name="orientation">Box orientation</param>
    /// <param name="layerMask">Layer mask</param>
    /// <param name="queryTriggerInteraction">Query trigger interaction</param>
    /// <returns>Array of colliders</returns>
    public static Collider[] OverlapBox(Vector3 center, Vector3 halfExtents, Quaternion orientation, LayerMask layerMask,
        QueryTriggerInteraction queryTriggerInteraction)
    {
        return Physics.OverlapBox(center, halfExtents, orientation, layerMask, queryTriggerInteraction);
    }

    /// <summary>Box overlap check using Unity's own defaults (all layers, global trigger-query setting).</summary>
    public static Collider[] OverlapBox(Vector3 center, Vector3 halfExtents, Quaternion orientation) =>
        OverlapBox(center, halfExtents, orientation, default, QueryTriggerInteraction.UseGlobal);

    /// <summary>
    /// Performs a capsule overlap check
    /// </summary>
    /// <param name="point0">Capsule start point</param>
    /// <param name="point1">Capsule end point</param>
    /// <param name="radius">Capsule radius</param>
    /// <param name="layerMask">Layer mask</param>
    /// <param name="queryTriggerInteraction">Query trigger interaction</param>
    /// <returns>Array of colliders</returns>
    public static Collider[] OverlapCapsule(Vector3 point0, Vector3 point1, float radius, LayerMask layerMask,
        QueryTriggerInteraction queryTriggerInteraction)
    {
        return Physics.OverlapCapsule(point0, point1, radius, layerMask, queryTriggerInteraction);
    }

    /// <summary>Capsule overlap check using Unity's own defaults (all layers, global trigger-query setting).</summary>
    public static Collider[] OverlapCapsule(Vector3 point0, Vector3 point1, float radius) =>
        OverlapCapsule(point0, point1, radius, default, QueryTriggerInteraction.UseGlobal);

    #endregion

    #region Collision Detection Helpers

    /// <summary>
    /// Checks if a position is clear (no colliders in the area)
    /// </summary>
    /// <param name="position">Position to check</param>
    /// <param name="radius">Check radius</param>
    /// <param name="layerMask">Layer mask</param>
    /// <returns>True if position is clear</returns>
    public static bool IsPositionClear(Vector3 position, float radius, LayerMask layerMask)
    {
        return Physics.OverlapSphere(position, radius, layerMask).Length == 0;
    }

    /// <summary>Checks if a position is clear, using a 0.5 radius against all layers.</summary>
    public static bool IsPositionClear(Vector3 position) => IsPositionClear(position, 0.5f, default);

#endregion
#region Ground Detection

    #endregion
}

/// <summary>
/// Essential Physics2D extension methods for Unity 2D development
/// </summary>
public static class Physics2DExtensions
{
    #region Raycast Utilities

    /// <summary>
    /// Performs a 2D raycast from a position in a direction
    /// </summary>
    /// <param name="origin">Ray origin</param>
    /// <param name="direction">Ray direction</param>
    /// <param name="distance">Ray distance</param>
    /// <param name="layerMask">Layer mask</param>
    /// <returns>Raycast hit info</returns>
    public static RaycastHit2D Raycast2D(Vector2 origin, Vector2 direction, float distance, LayerMask layerMask)
    {
        return Physics2D.Raycast(origin, direction, distance, layerMask);
    }

    /// <summary>2D raycast using an infinite distance against all layers.</summary>
    public static RaycastHit2D Raycast2D(Vector2 origin, Vector2 direction) => Raycast2D(origin, direction, Mathf.Infinity, default);

#endregion
#region Overlap Detection
#endregion
#region Collision Detection Helpers
#endregion
#region Ground Detection

    #endregion
}
}
