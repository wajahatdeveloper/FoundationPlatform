using System;
using UnityEngine;

/// <summary>
/// Essential Rigidbody extension methods for Unity physics development
/// </summary>
namespace AetherNexus.FoundationPlatform.Extensions
{
public static class RigidbodyExtensions
{

#region Force Application
#endregion
#region Velocity Manipulation

    #endregion

    #region Physics State Management

    /// <summary>
    /// Freezes the rigidbody (sets isKinematic to true)
    /// </summary>
    /// <param name="rigidbody">The rigidbody</param>
    public static void Freeze(this Rigidbody rigidbody)
    {
        rigidbody.isKinematic = true;
    }

    #endregion

    #region Collision Detection

    /// <summary>
    /// Checks if the rigidbody is grounded using a raycast
    /// </summary>
    /// <param name="rigidbody">The rigidbody</param>
    /// <param name="groundCheckDistance">Distance to check for ground</param>
    /// <param name="groundLayer">Layer mask for ground objects</param>
    /// <returns>True if grounded</returns>
    public static bool IsGrounded(this Rigidbody rigidbody, float groundCheckDistance, LayerMask groundLayer)
    {
        return Physics.Raycast(rigidbody.position, Vector3.down, groundCheckDistance, groundLayer);
    }

    /// <summary>Checks if the rigidbody is grounded using a raycast, using a 0.1 check distance against all layers.</summary>
    public static bool IsGrounded(this Rigidbody rigidbody) => IsGrounded(rigidbody, 0.1f, default);

    #endregion

    #region Utility Methods

    /// <summary>
    /// Gets the speed of the rigidbody
    /// </summary>
    /// <param name="rigidbody">The rigidbody</param>
    /// <returns>Current speed</returns>
    public static float GetSpeed(this Rigidbody rigidbody)
    {
        return rigidbody.linearVelocity.magnitude;
    }

    /// <summary>
    /// Checks if the rigidbody is moving
    /// </summary>
    /// <param name="rigidbody">The rigidbody</param>
    /// <param name="threshold">Speed threshold</param>
    /// <returns>True if moving</returns>
    public static bool IsMoving(this Rigidbody rigidbody, float threshold)
    {
        return rigidbody.linearVelocity.magnitude > threshold;
    }

    /// <summary>Checks if the rigidbody is moving, using a 0.01 speed threshold.</summary>
    public static bool IsMoving(this Rigidbody rigidbody) => IsMoving(rigidbody, 0.01f);

    /// <summary>
    /// Resets the rigidbody to its initial state
    /// </summary>
    /// <param name="rigidbody">The rigidbody</param>
    public static void Reset(this Rigidbody rigidbody)
    {
        rigidbody.linearVelocity = Vector3.zero;
        rigidbody.angularVelocity = Vector3.zero;
        rigidbody.isKinematic = false;
        rigidbody.useGravity = true;
    }

    #endregion
}

/// <summary>
/// Essential Rigidbody2D extension methods for Unity 2D physics development
/// </summary>
public static class Rigidbody2DExtensions
{

#region Force Application
#endregion
#region Velocity Manipulation

    #endregion

    #region Physics State Management

    /// <summary>
    /// Freezes the rigidbody2D (sets bodyType to Kinematic)
    /// </summary>
    /// <param name="rigidbody2D">The rigidbody2D</param>
    public static void Freeze(this Rigidbody2D rigidbody2D)
    {
        rigidbody2D.bodyType = RigidbodyType2D.Kinematic;
    }

    #endregion

    #region Collision Detection

    /// <summary>
    /// Checks if the rigidbody2D is grounded using a raycast
    /// </summary>
    /// <param name="rigidbody2D">The rigidbody2D</param>
    /// <param name="groundCheckDistance">Distance to check for ground</param>
    /// <param name="groundLayer">Layer mask for ground objects</param>
    /// <returns>True if grounded</returns>
    public static bool IsGrounded(this Rigidbody2D rigidbody2D, float groundCheckDistance, LayerMask groundLayer)
    {
        return Physics2D.Raycast(rigidbody2D.position, Vector2.down, groundCheckDistance, groundLayer);
    }

    /// <summary>Checks if the rigidbody2D is grounded using a raycast, using a 0.1 check distance against all layers.</summary>
    public static bool IsGrounded(this Rigidbody2D rigidbody2D) => IsGrounded(rigidbody2D, 0.1f, default);

    #endregion

    #region Utility Methods

    /// <summary>
    /// Gets the speed of the rigidbody2D
    /// </summary>
    /// <param name="rigidbody2D">The rigidbody2D</param>
    /// <returns>Current speed</returns>
    public static float GetSpeed(this Rigidbody2D rigidbody2D)
    {
        return rigidbody2D.linearVelocity.magnitude;
    }

    /// <summary>
    /// Checks if the rigidbody2D is moving
    /// </summary>
    /// <param name="rigidbody2D">The rigidbody2D</param>
    /// <param name="threshold">Speed threshold</param>
    /// <returns>True if moving</returns>
    public static bool IsMoving(this Rigidbody2D rigidbody2D, float threshold)
    {
        return rigidbody2D.linearVelocity.magnitude > threshold;
    }

    /// <summary>Checks if the rigidbody2D is moving, using a 0.01 speed threshold.</summary>
    public static bool IsMoving(this Rigidbody2D rigidbody2D) => IsMoving(rigidbody2D, 0.01f);

    /// <summary>
    /// Resets the rigidbody2D to its initial state
    /// </summary>
    /// <param name="rigidbody2D">The rigidbody2D</param>
    public static void Reset(this Rigidbody2D rigidbody2D)
    {
        rigidbody2D.linearVelocity = Vector2.zero;
        rigidbody2D.angularVelocity = 0f;
        rigidbody2D.bodyType = RigidbodyType2D.Dynamic;
        rigidbody2D.gravityScale = 1f;
    }

    #endregion
}
}
