using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Essential Physics extension methods for Unity development
/// </summary>
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
    public static RaycastHit Raycast(Vector3 origin, Vector3 direction, float maxDistance = Mathf.Infinity, 
        LayerMask layerMask = default, QueryTriggerInteraction queryTriggerInteraction = QueryTriggerInteraction.UseGlobal)
    {
        Physics.Raycast(origin, direction, out RaycastHit hit, maxDistance, layerMask, queryTriggerInteraction);
        return hit;
    }

    /// <summary>
    /// Performs a raycast from a position to a target position
    /// </summary>
    /// <param name="origin">Ray origin</param>
    /// <param name="target">Target position</param>
    /// <param name="layerMask">Layer mask</param>
    /// <param name="queryTriggerInteraction">Query trigger interaction</param>
    /// <returns>Raycast hit info</returns>
    public static RaycastHit RaycastTo(Vector3 origin, Vector3 target, LayerMask layerMask = default, 
        QueryTriggerInteraction queryTriggerInteraction = QueryTriggerInteraction.UseGlobal)
    {
        Vector3 direction = (target - origin).normalized;
        float distance = Vector3.Distance(origin, target);
        Physics.Raycast(origin, direction, out RaycastHit hit, distance, layerMask, queryTriggerInteraction);
        return hit;
    }

    /// <summary>
    /// Performs a raycast from a position in the forward direction
    /// </summary>
    /// <param name="origin">Ray origin</param>
    /// <param name="forward">Forward direction</param>
    /// <param name="maxDistance">Maximum ray distance</param>
    /// <param name="layerMask">Layer mask</param>
    /// <param name="queryTriggerInteraction">Query trigger interaction</param>
    /// <returns>Raycast hit info</returns>
    public static RaycastHit RaycastForward(Vector3 origin, Vector3 forward, float maxDistance = Mathf.Infinity, 
        LayerMask layerMask = default, QueryTriggerInteraction queryTriggerInteraction = QueryTriggerInteraction.UseGlobal)
    {
        Physics.Raycast(origin, forward, out RaycastHit hit, maxDistance, layerMask, queryTriggerInteraction);
        return hit;
    }

    /// <summary>
    /// Performs a raycast from a position downward (ground check)
    /// </summary>
    /// <param name="origin">Ray origin</param>
    /// <param name="maxDistance">Maximum ray distance</param>
    /// <param name="layerMask">Layer mask</param>
    /// <param name="queryTriggerInteraction">Query trigger interaction</param>
    /// <returns>Raycast hit info</returns>
    public static RaycastHit RaycastDown(Vector3 origin, float maxDistance = Mathf.Infinity, 
        LayerMask layerMask = default, QueryTriggerInteraction queryTriggerInteraction = QueryTriggerInteraction.UseGlobal)
    {
        Physics.Raycast(origin, Vector3.down, out RaycastHit hit, maxDistance, layerMask, queryTriggerInteraction);
        return hit;
    }

    /// <summary>
    /// Performs a raycast from a position upward
    /// </summary>
    /// <param name="origin">Ray origin</param>
    /// <param name="maxDistance">Maximum ray distance</param>
    /// <param name="layerMask">Layer mask</param>
    /// <param name="queryTriggerInteraction">Query trigger interaction</param>
    /// <returns>Raycast hit info</returns>
    public static RaycastHit RaycastUp(Vector3 origin, float maxDistance = Mathf.Infinity, 
        LayerMask layerMask = default, QueryTriggerInteraction queryTriggerInteraction = QueryTriggerInteraction.UseGlobal)
    {
        Physics.Raycast(origin, Vector3.up, out RaycastHit hit, maxDistance, layerMask, queryTriggerInteraction);
        return hit;
    }

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
    public static Collider[] OverlapSphere(Vector3 position, float radius, LayerMask layerMask = default, 
        QueryTriggerInteraction queryTriggerInteraction = QueryTriggerInteraction.UseGlobal)
    {
        return Physics.OverlapSphere(position, radius, layerMask, queryTriggerInteraction);
    }

    /// <summary>
    /// Performs a box overlap check
    /// </summary>
    /// <param name="center">Box center</param>
    /// <param name="halfExtents">Box half extents</param>
    /// <param name="orientation">Box orientation</param>
    /// <param name="layerMask">Layer mask</param>
    /// <param name="queryTriggerInteraction">Query trigger interaction</param>
    /// <returns>Array of colliders</returns>
    public static Collider[] OverlapBox(Vector3 center, Vector3 halfExtents, Quaternion orientation, LayerMask layerMask = default, 
        QueryTriggerInteraction queryTriggerInteraction = QueryTriggerInteraction.UseGlobal)
    {
        return Physics.OverlapBox(center, halfExtents, orientation, layerMask, queryTriggerInteraction);
    }

    /// <summary>
    /// Performs a capsule overlap check
    /// </summary>
    /// <param name="point0">Capsule start point</param>
    /// <param name="point1">Capsule end point</param>
    /// <param name="radius">Capsule radius</param>
    /// <param name="layerMask">Layer mask</param>
    /// <param name="queryTriggerInteraction">Query trigger interaction</param>
    /// <returns>Array of colliders</returns>
    public static Collider[] OverlapCapsule(Vector3 point0, Vector3 point1, float radius, LayerMask layerMask = default, 
        QueryTriggerInteraction queryTriggerInteraction = QueryTriggerInteraction.UseGlobal)
    {
        return Physics.OverlapCapsule(point0, point1, radius, layerMask, queryTriggerInteraction);
    }

    #endregion

    #region Collision Detection Helpers

    /// <summary>
    /// Checks if a position is clear (no colliders in the area)
    /// </summary>
    /// <param name="position">Position to check</param>
    /// <param name="radius">Check radius</param>
    /// <param name="layerMask">Layer mask</param>
    /// <returns>True if position is clear</returns>
    public static bool IsPositionClear(Vector3 position, float radius = 0.5f, LayerMask layerMask = default)
    {
        return Physics.OverlapSphere(position, radius, layerMask).Length == 0;
    }

    /// <summary>
    /// Finds the nearest clear position around a given position
    /// </summary>
    /// <param name="position">Center position</param>
    /// <param name="radius">Search radius</param>
    /// <param name="checkRadius">Clearance radius</param>
    /// <param name="layerMask">Layer mask</param>
    /// <returns>Nearest clear position, or original position if none found</returns>
    public static Vector3 FindNearestClearPosition(Vector3 position, float radius = 5f, float checkRadius = 0.5f, LayerMask layerMask = default)
    {
        if (IsPositionClear(position, checkRadius, layerMask))
            return position;

        int attempts = 20;
        float angleStep = 360f / attempts;
        
        for (int i = 0; i < attempts; i++)
        {
            float angle = i * angleStep * Mathf.Deg2Rad;
            Vector3 offset = new Vector3(Mathf.Cos(angle), 0, Mathf.Sin(angle)) * radius;
            Vector3 testPosition = position + offset;
            
            if (IsPositionClear(testPosition, checkRadius, layerMask))
                return testPosition;
        }
        
        return position;
    }

    /// <summary>
    /// Gets all colliders within a certain distance of a position
    /// </summary>
    /// <param name="position">Center position</param>
    /// <param name="maxDistance">Maximum distance</param>
    /// <param name="layerMask">Layer mask</param>
    /// <returns>List of colliders within distance</returns>
    public static List<Collider> GetCollidersInRange(Vector3 position, float maxDistance, LayerMask layerMask = default)
    {
        List<Collider> colliders = new List<Collider>();
        Collider[] allColliders = Physics.OverlapSphere(position, maxDistance, layerMask);
        
        foreach (Collider collider in allColliders)
        {
            if (Vector3.Distance(position, collider.transform.position) <= maxDistance)
            {
                colliders.Add(collider);
            }
        }
        
        return colliders;
    }

    /// <summary>
    /// Gets the closest collider to a position
    /// </summary>
    /// <param name="position">Center position</param>
    /// <param name="maxDistance">Maximum search distance</param>
    /// <param name="layerMask">Layer mask</param>
    /// <returns>Closest collider, or null if none found</returns>
    public static Collider GetClosestCollider(Vector3 position, float maxDistance = Mathf.Infinity, LayerMask layerMask = default)
    {
        Collider[] colliders = Physics.OverlapSphere(position, maxDistance, layerMask);
        Collider closest = null;
        float closestDistance = float.MaxValue;
        
        foreach (Collider collider in colliders)
        {
            float distance = Vector3.Distance(position, collider.transform.position);
            if (distance < closestDistance)
            {
                closestDistance = distance;
                closest = collider;
            }
        }
        
        return closest;
    }

    #endregion

    #region Ground Detection

    /// <summary>
    /// Checks if a position is on the ground
    /// </summary>
    /// <param name="position">Position to check</param>
    /// <param name="groundCheckDistance">Distance to check for ground</param>
    /// <param name="groundLayer">Ground layer mask</param>
    /// <returns>True if on ground</returns>
    public static bool IsOnGround(Vector3 position, float groundCheckDistance = 0.1f, LayerMask groundLayer = default)
    {
        return Physics.Raycast(position, Vector3.down, groundCheckDistance, groundLayer);
    }

    /// <summary>
    /// Gets the ground normal at a position
    /// </summary>
    /// <param name="position">Position to check</param>
    /// <param name="groundCheckDistance">Distance to check for ground</param>
    /// <param name="groundLayer">Ground layer mask</param>
    /// <returns>Ground normal, or Vector3.up if no ground found</returns>
    public static Vector3 GetGroundNormal(Vector3 position, float groundCheckDistance = 0.1f, LayerMask groundLayer = default)
    {
        if (Physics.Raycast(position, Vector3.down, out RaycastHit hit, groundCheckDistance, groundLayer))
        {
            return hit.normal;
        }
        return Vector3.up;
    }

    /// <summary>
    /// Gets the ground height at a position
    /// </summary>
    /// <param name="position">Position to check</param>
    /// <param name="groundCheckDistance">Distance to check for ground</param>
    /// <param name="groundLayer">Ground layer mask</param>
    /// <returns>Ground height, or original Y if no ground found</returns>
    public static float GetGroundHeight(Vector3 position, float groundCheckDistance = 0.1f, LayerMask groundLayer = default)
    {
        if (Physics.Raycast(position, Vector3.down, out RaycastHit hit, groundCheckDistance, groundLayer))
        {
            return hit.point.y;
        }
        return position.y;
    }

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
    public static RaycastHit2D Raycast2D(Vector2 origin, Vector2 direction, float distance = Mathf.Infinity, LayerMask layerMask = default)
    {
        return Physics2D.Raycast(origin, direction, distance, layerMask);
    }

    /// <summary>
    /// Performs a 2D raycast from a position to a target position
    /// </summary>
    /// <param name="origin">Ray origin</param>
    /// <param name="target">Target position</param>
    /// <param name="layerMask">Layer mask</param>
    /// <returns>Raycast hit info</returns>
    public static RaycastHit2D Raycast2DTo(Vector2 origin, Vector2 target, LayerMask layerMask = default)
    {
        Vector2 direction = (target - origin).normalized;
        float distance = Vector2.Distance(origin, target);
        return Physics2D.Raycast(origin, direction, distance, layerMask);
    }

    /// <summary>
    /// Performs a 2D raycast downward (ground check)
    /// </summary>
    /// <param name="origin">Ray origin</param>
    /// <param name="distance">Ray distance</param>
    /// <param name="layerMask">Layer mask</param>
    /// <returns>Raycast hit info</returns>
    public static RaycastHit2D Raycast2DDown(Vector2 origin, float distance = Mathf.Infinity, LayerMask layerMask = default)
    {
        return Physics2D.Raycast(origin, Vector2.down, distance, layerMask);
    }

    /// <summary>
    /// Performs a 2D raycast upward
    /// </summary>
    /// <param name="origin">Ray origin</param>
    /// <param name="distance">Ray distance</param>
    /// <param name="layerMask">Layer mask</param>
    /// <returns>Raycast hit info</returns>
    public static RaycastHit2D Raycast2DUp(Vector2 origin, float distance = Mathf.Infinity, LayerMask layerMask = default)
    {
        return Physics2D.Raycast(origin, Vector2.up, distance, layerMask);
    }

    #endregion

    #region Overlap Detection

    /// <summary>
    /// Performs a 2D circle overlap check
    /// </summary>
    /// <param name="point">Circle center</param>
    /// <param name="radius">Circle radius</param>
    /// <param name="layerMask">Layer mask</param>
    /// <returns>Array of colliders</returns>
    public static Collider2D[] OverlapCircle2D(Vector2 point, float radius, LayerMask layerMask = default)
    {
        return Physics2D.OverlapCircleAll(point, radius, layerMask);
    }

    /// <summary>
    /// Performs a 2D box overlap check
    /// </summary>
    /// <param name="point">Box center</param>
    /// <param name="size">Box size</param>
    /// <param name="angle">Box angle</param>
    /// <param name="layerMask">Layer mask</param>
    /// <returns>Array of colliders</returns>
    public static Collider2D[] OverlapBox2D(Vector2 point, Vector2 size, float angle = 0f, LayerMask layerMask = default)
    {
        return Physics2D.OverlapBoxAll(point, size, angle, layerMask);
    }

    /// <summary>
    /// Performs a 2D capsule overlap check
    /// </summary>
    /// <param name="point">Capsule center</param>
    /// <param name="size">Capsule size</param>
    /// <param name="direction">Capsule direction</param>
    /// <param name="angle">Capsule angle</param>
    /// <param name="layerMask">Layer mask</param>
    /// <returns>Array of colliders</returns>
    public static Collider2D[] OverlapCapsule2D(Vector2 point, Vector2 size, CapsuleDirection2D direction, float angle = 0f, LayerMask layerMask = default)
    {
        return Physics2D.OverlapCapsuleAll(point, size, direction, angle, layerMask);
    }

    #endregion

    #region Collision Detection Helpers

    /// <summary>
    /// Checks if a position is clear in 2D (no colliders in the area)
    /// </summary>
    /// <param name="position">Position to check</param>
    /// <param name="radius">Check radius</param>
    /// <param name="layerMask">Layer mask</param>
    /// <returns>True if position is clear</returns>
    public static bool IsPositionClear2D(Vector2 position, float radius = 0.5f, LayerMask layerMask = default)
    {
        return Physics2D.OverlapCircle(position, radius, layerMask) == null;
    }

    /// <summary>
    /// Gets all colliders within a certain distance of a position in 2D
    /// </summary>
    /// <param name="position">Center position</param>
    /// <param name="maxDistance">Maximum distance</param>
    /// <param name="layerMask">Layer mask</param>
    /// <returns>List of colliders within distance</returns>
    public static List<Collider2D> GetCollidersInRange2D(Vector2 position, float maxDistance, LayerMask layerMask = default)
    {
        List<Collider2D> colliders = new List<Collider2D>();
        Collider2D[] allColliders = Physics2D.OverlapCircleAll(position, maxDistance, layerMask);
        
        foreach (Collider2D collider in allColliders)
        {
            if (Vector2.Distance(position, collider.transform.position) <= maxDistance)
            {
                colliders.Add(collider);
            }
        }
        
        return colliders;
    }

    /// <summary>
    /// Gets the closest collider to a position in 2D
    /// </summary>
    /// <param name="position">Center position</param>
    /// <param name="maxDistance">Maximum search distance</param>
    /// <param name="layerMask">Layer mask</param>
    /// <returns>Closest collider, or null if none found</returns>
    public static Collider2D GetClosestCollider2D(Vector2 position, float maxDistance = Mathf.Infinity, LayerMask layerMask = default)
    {
        Collider2D[] colliders = Physics2D.OverlapCircleAll(position, maxDistance, layerMask);
        Collider2D closest = null;
        float closestDistance = float.MaxValue;
        
        foreach (Collider2D collider in colliders)
        {
            float distance = Vector2.Distance(position, collider.transform.position);
            if (distance < closestDistance)
            {
                closestDistance = distance;
                closest = collider;
            }
        }
        
        return closest;
    }

    #endregion

    #region Ground Detection

    /// <summary>
    /// Checks if a position is on the ground in 2D
    /// </summary>
    /// <param name="position">Position to check</param>
    /// <param name="groundCheckDistance">Distance to check for ground</param>
    /// <param name="groundLayer">Ground layer mask</param>
    /// <returns>True if on ground</returns>
    public static bool IsOnGround2D(Vector2 position, float groundCheckDistance = 0.1f, LayerMask groundLayer = default)
    {
        return Physics2D.Raycast(position, Vector2.down, groundCheckDistance, groundLayer);
    }

    /// <summary>
    /// Gets the ground normal at a position in 2D
    /// </summary>
    /// <param name="position">Position to check</param>
    /// <param name="groundCheckDistance">Distance to check for ground</param>
    /// <param name="groundLayer">Ground layer mask</param>
    /// <returns>Ground normal, or Vector2.up if no ground found</returns>
    public static Vector2 GetGroundNormal2D(Vector2 position, float groundCheckDistance = 0.1f, LayerMask groundLayer = default)
    {
        RaycastHit2D hit = Physics2D.Raycast(position, Vector2.down, groundCheckDistance, groundLayer);
        if (hit.collider != null)
        {
            return hit.normal;
        }
        return Vector2.up;
    }

    /// <summary>
    /// Gets the ground height at a position in 2D
    /// </summary>
    /// <param name="position">Position to check</param>
    /// <param name="groundCheckDistance">Distance to check for ground</param>
    /// <param name="groundLayer">Ground layer mask</param>
    /// <returns>Ground height, or original Y if no ground found</returns>
    public static float GetGroundHeight2D(Vector2 position, float groundCheckDistance = 0.1f, LayerMask groundLayer = default)
    {
        RaycastHit2D hit = Physics2D.Raycast(position, Vector2.down, groundCheckDistance, groundLayer);
        if (hit.collider != null)
        {
            return hit.point.y;
        }
        return position.y;
    }

    #endregion
}
