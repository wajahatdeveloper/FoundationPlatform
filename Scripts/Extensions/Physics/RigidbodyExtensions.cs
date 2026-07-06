using System;
using UnityEngine;

/// <summary>
/// Essential Rigidbody extension methods for Unity physics development
/// </summary>
public static class RigidbodyExtensions
{
    #region Force Application

    /// <summary>
    /// Applies force in the forward direction of the rigidbody
    /// </summary>
    /// <param name="rigidbody">The rigidbody</param>
    /// <param name="force">Force magnitude</param>
    /// <param name="mode">Force mode</param>
    public static void AddForwardForce(this Rigidbody rigidbody, float force, ForceMode mode = ForceMode.Force)
    {
        rigidbody.AddForce(rigidbody.transform.forward * force, mode);
    }

    /// <summary>
    /// Applies force in the right direction of the rigidbody
    /// </summary>
    /// <param name="rigidbody">The rigidbody</param>
    /// <param name="force">Force magnitude</param>
    /// <param name="mode">Force mode</param>
    public static void AddRightForce(this Rigidbody rigidbody, float force, ForceMode mode = ForceMode.Force)
    {
        rigidbody.AddForce(rigidbody.transform.right * force, mode);
    }

    /// <summary>
    /// Applies force in the up direction of the rigidbody
    /// </summary>
    /// <param name="rigidbody">The rigidbody</param>
    /// <param name="force">Force magnitude</param>
    /// <param name="mode">Force mode</param>
    public static void AddUpForce(this Rigidbody rigidbody, float force, ForceMode mode = ForceMode.Force)
    {
        rigidbody.AddForce(rigidbody.transform.up * force, mode);
    }

    /// <summary>
    /// Applies force towards a target position
    /// </summary>
    /// <param name="rigidbody">The rigidbody</param>
    /// <param name="targetPosition">Target position</param>
    /// <param name="force">Force magnitude</param>
    /// <param name="mode">Force mode</param>
    public static void AddForceTowards(this Rigidbody rigidbody, Vector3 targetPosition, float force, ForceMode mode = ForceMode.Force)
    {
        Vector3 direction = (targetPosition - rigidbody.position).normalized;
        rigidbody.AddForce(direction * force, mode);
    }

    /// <summary>
    /// Applies force away from a target position
    /// </summary>
    /// <param name="rigidbody">The rigidbody</param>
    /// <param name="targetPosition">Target position</param>
    /// <param name="force">Force magnitude</param>
    /// <param name="mode">Force mode</param>
    public static void AddForceAwayFrom(this Rigidbody rigidbody, Vector3 targetPosition, float force, ForceMode mode = ForceMode.Force)
    {
        Vector3 direction = (rigidbody.position - targetPosition).normalized;
        rigidbody.AddForce(direction * force, mode);
    }

    /// <summary>
    /// Applies force in a specific direction
    /// </summary>
    /// <param name="rigidbody">The rigidbody</param>
    /// <param name="direction">Force direction</param>
    /// <param name="force">Force magnitude</param>
    /// <param name="mode">Force mode</param>
    public static void AddForceInDirection(this Rigidbody rigidbody, Vector3 direction, float force, ForceMode mode = ForceMode.Force)
    {
        rigidbody.AddForce(direction.normalized * force, mode);
    }

    #endregion

    #region Velocity Manipulation

    /// <summary>
    /// Sets the velocity to zero
    /// </summary>
    /// <param name="rigidbody">The rigidbody</param>
    public static void StopVelocity(this Rigidbody rigidbody)
    {
        rigidbody.linearVelocity = Vector3.zero;
    }

    /// <summary>
    /// Sets the angular velocity to zero
    /// </summary>
    /// <param name="rigidbody">The rigidbody</param>
    public static void StopAngularVelocity(this Rigidbody rigidbody)
    {
        rigidbody.angularVelocity = Vector3.zero;
    }

    /// <summary>
    /// Stops all movement (velocity and angular velocity)
    /// </summary>
    /// <param name="rigidbody">The rigidbody</param>
    public static void StopAllMovement(this Rigidbody rigidbody)
    {
        rigidbody.linearVelocity = Vector3.zero;
        rigidbody.angularVelocity = Vector3.zero;
    }

    /// <summary>
    /// Sets the velocity in a specific direction
    /// </summary>
    /// <param name="rigidbody">The rigidbody</param>
    /// <param name="direction">Velocity direction</param>
    /// <param name="speed">Speed magnitude</param>
    public static void SetVelocityInDirection(this Rigidbody rigidbody, Vector3 direction, float speed)
    {
        rigidbody.linearVelocity = direction.normalized * speed;
    }

    /// <summary>
    /// Sets the velocity towards a target position
    /// </summary>
    /// <param name="rigidbody">The rigidbody</param>
    /// <param name="targetPosition">Target position</param>
    /// <param name="speed">Speed magnitude</param>
    public static void SetVelocityTowards(this Rigidbody rigidbody, Vector3 targetPosition, float speed)
    {
        Vector3 direction = (targetPosition - rigidbody.position).normalized;
        rigidbody.linearVelocity = direction * speed;
    }

    /// <summary>
    /// Clamps the velocity to a maximum speed
    /// </summary>
    /// <param name="rigidbody">The rigidbody</param>
    /// <param name="maxSpeed">Maximum speed</param>
    public static void ClampVelocity(this Rigidbody rigidbody, float maxSpeed)
    {
        if (rigidbody.linearVelocity.magnitude > maxSpeed)
        {
            rigidbody.linearVelocity = rigidbody.linearVelocity.normalized * maxSpeed;
        }
    }

    /// <summary>
    /// Clamps the angular velocity to a maximum speed
    /// </summary>
    /// <param name="rigidbody">The rigidbody</param>
    /// <param name="maxAngularSpeed">Maximum angular speed</param>
    public static void ClampAngularVelocity(this Rigidbody rigidbody, float maxAngularSpeed)
    {
        if (rigidbody.angularVelocity.magnitude > maxAngularSpeed)
        {
            rigidbody.angularVelocity = rigidbody.angularVelocity.normalized * maxAngularSpeed;
        }
    }

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

    /// <summary>
    /// Unfreezes the rigidbody (sets isKinematic to false)
    /// </summary>
    /// <param name="rigidbody">The rigidbody</param>
    public static void Unfreeze(this Rigidbody rigidbody)
    {
        rigidbody.isKinematic = false;
    }

    /// <summary>
    /// Toggles the kinematic state
    /// </summary>
    /// <param name="rigidbody">The rigidbody</param>
    public static void ToggleKinematic(this Rigidbody rigidbody)
    {
        rigidbody.isKinematic = !rigidbody.isKinematic;
    }

    /// <summary>
    /// Enables gravity
    /// </summary>
    /// <param name="rigidbody">The rigidbody</param>
    public static void EnableGravity(this Rigidbody rigidbody)
    {
        rigidbody.useGravity = true;
    }

    /// <summary>
    /// Disables gravity
    /// </summary>
    /// <param name="rigidbody">The rigidbody</param>
    public static void DisableGravity(this Rigidbody rigidbody)
    {
        rigidbody.useGravity = false;
    }

    /// <summary>
    /// Toggles gravity
    /// </summary>
    /// <param name="rigidbody">The rigidbody</param>
    public static void ToggleGravity(this Rigidbody rigidbody)
    {
        rigidbody.useGravity = !rigidbody.useGravity;
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
    public static bool IsGrounded(this Rigidbody rigidbody, float groundCheckDistance = 0.1f, LayerMask groundLayer = default)
    {
        return Physics.Raycast(rigidbody.position, Vector3.down, groundCheckDistance, groundLayer);
    }

    /// <summary>
    /// Checks if the rigidbody is grounded using a sphere cast
    /// </summary>
    /// <param name="rigidbody">The rigidbody</param>
    /// <param name="groundCheckDistance">Distance to check for ground</param>
    /// <param name="radius">Radius of the sphere cast</param>
    /// <param name="groundLayer">Layer mask for ground objects</param>
    /// <returns>True if grounded</returns>
    public static bool IsGroundedSphere(this Rigidbody rigidbody, float groundCheckDistance = 0.1f, float radius = 0.5f, LayerMask groundLayer = default)
    {
        return Physics.SphereCast(rigidbody.position, radius, Vector3.down, out _, groundCheckDistance, groundLayer);
    }

    /// <summary>
    /// Gets the ground normal using a raycast
    /// </summary>
    /// <param name="rigidbody">The rigidbody</param>
    /// <param name="groundCheckDistance">Distance to check for ground</param>
    /// <param name="groundLayer">Layer mask for ground objects</param>
    /// <returns>Ground normal vector</returns>
    public static Vector3 GetGroundNormal(this Rigidbody rigidbody, float groundCheckDistance = 0.1f, LayerMask groundLayer = default)
    {
        if (Physics.Raycast(rigidbody.position, Vector3.down, out RaycastHit hit, groundCheckDistance, groundLayer))
        {
            return hit.normal;
        }
        return Vector3.up;
    }

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
    /// Gets the angular speed of the rigidbody
    /// </summary>
    /// <param name="rigidbody">The rigidbody</param>
    /// <returns>Current angular speed</returns>
    public static float GetAngularSpeed(this Rigidbody rigidbody)
    {
        return rigidbody.angularVelocity.magnitude;
    }

    /// <summary>
    /// Checks if the rigidbody is moving
    /// </summary>
    /// <param name="rigidbody">The rigidbody</param>
    /// <param name="threshold">Speed threshold</param>
    /// <returns>True if moving</returns>
    public static bool IsMoving(this Rigidbody rigidbody, float threshold = 0.01f)
    {
        return rigidbody.linearVelocity.magnitude > threshold;
    }

    /// <summary>
    /// Checks if the rigidbody is rotating
    /// </summary>
    /// <param name="rigidbody">The rigidbody</param>
    /// <param name="threshold">Angular speed threshold</param>
    /// <returns>True if rotating</returns>
    public static bool IsRotating(this Rigidbody rigidbody, float threshold = 0.01f)
    {
        return rigidbody.angularVelocity.magnitude > threshold;
    }

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

    /// <summary>
    /// Swap Rigidbody IsKinematic and DetectCollisions
    /// </summary>
    /// <param name="body"></param>
    /// <param name="state"></param>
    public static void SetBodyState(this Rigidbody body, bool state)
    {
        body.isKinematic = !state;
        body.detectCollisions = state;
    }

    #endregion
}

/// <summary>
/// Essential Rigidbody2D extension methods for Unity 2D physics development
/// </summary>
public static class Rigidbody2DExtensions
{
    #region Force Application

    /// <summary>
    /// Applies force in the forward direction of the rigidbody2D
    /// </summary>
    /// <param name="rigidbody2D">The rigidbody2D</param>
    /// <param name="force">Force magnitude</param>
    /// <param name="mode">Force mode</param>
    public static void AddForwardForce(this Rigidbody2D rigidbody2D, float force, ForceMode2D mode = ForceMode2D.Force)
    {
        rigidbody2D.AddForce(rigidbody2D.transform.right * force, mode);
    }

    /// <summary>
    /// Applies force in the up direction of the rigidbody2D
    /// </summary>
    /// <param name="rigidbody2D">The rigidbody2D</param>
    /// <param name="force">Force magnitude</param>
    /// <param name="mode">Force mode</param>
    public static void AddUpForce(this Rigidbody2D rigidbody2D, float force, ForceMode2D mode = ForceMode2D.Force)
    {
        rigidbody2D.AddForce(rigidbody2D.transform.up * force, mode);
    }

    /// <summary>
    /// Applies force towards a target position
    /// </summary>
    /// <param name="rigidbody2D">The rigidbody2D</param>
    /// <param name="targetPosition">Target position</param>
    /// <param name="force">Force magnitude</param>
    /// <param name="mode">Force mode</param>
    public static void AddForceTowards(this Rigidbody2D rigidbody2D, Vector2 targetPosition, float force, ForceMode2D mode = ForceMode2D.Force)
    {
        Vector2 direction = (targetPosition - rigidbody2D.position).normalized;
        rigidbody2D.AddForce(direction * force, mode);
    }

    /// <summary>
    /// Applies force away from a target position
    /// </summary>
    /// <param name="rigidbody2D">The rigidbody2D</param>
    /// <param name="targetPosition">Target position</param>
    /// <param name="force">Force magnitude</param>
    /// <param name="mode">Force mode</param>
    public static void AddForceAwayFrom(this Rigidbody2D rigidbody2D, Vector2 targetPosition, float force, ForceMode2D mode = ForceMode2D.Force)
    {
        Vector2 direction = (rigidbody2D.position - targetPosition).normalized;
        rigidbody2D.AddForce(direction * force, mode);
    }

    #endregion

    #region Velocity Manipulation

    /// <summary>
    /// Sets the velocity to zero
    /// </summary>
    /// <param name="rigidbody2D">The rigidbody2D</param>
    public static void StopVelocity(this Rigidbody2D rigidbody2D)
    {
        rigidbody2D.linearVelocity = Vector2.zero;
    }

    /// <summary>
    /// Sets the angular velocity to zero
    /// </summary>
    /// <param name="rigidbody2D">The rigidbody2D</param>
    public static void StopAngularVelocity(this Rigidbody2D rigidbody2D)
    {
        rigidbody2D.angularVelocity = 0f;
    }

    /// <summary>
    /// Stops all movement (velocity and angular velocity)
    /// </summary>
    /// <param name="rigidbody2D">The rigidbody2D</param>
    public static void StopAllMovement(this Rigidbody2D rigidbody2D)
    {
        rigidbody2D.linearVelocity = Vector2.zero;
        rigidbody2D.angularVelocity = 0f;
    }

    /// <summary>
    /// Clamps the velocity to a maximum speed
    /// </summary>
    /// <param name="rigidbody2D">The rigidbody2D</param>
    /// <param name="maxSpeed">Maximum speed</param>
    public static void ClampVelocity(this Rigidbody2D rigidbody2D, float maxSpeed)
    {
        if (rigidbody2D.linearVelocity.magnitude > maxSpeed)
        {
            rigidbody2D.linearVelocity = rigidbody2D.linearVelocity.normalized * maxSpeed;
        }
    }

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

    /// <summary>
    /// Unfreezes the rigidbody2D (sets bodyType to Dynamic)
    /// </summary>
    /// <param name="rigidbody2D">The rigidbody2D</param>
    public static void Unfreeze(this Rigidbody2D rigidbody2D)
    {
        rigidbody2D.bodyType = RigidbodyType2D.Dynamic;
    }

    /// <summary>
    /// Toggles between Kinematic and Dynamic bodyType
    /// </summary>
    /// <param name="rigidbody2D">The rigidbody2D</param>
    public static void ToggleKinematic(this Rigidbody2D rigidbody2D)
    {
        rigidbody2D.bodyType = rigidbody2D.bodyType == RigidbodyType2D.Kinematic
            ? RigidbodyType2D.Dynamic
            : RigidbodyType2D.Kinematic;
    }

    /// <summary>
    /// Enables gravity
    /// </summary>
    /// <param name="rigidbody2D">The rigidbody2D</param>
    public static void EnableGravity(this Rigidbody2D rigidbody2D)
    {
        rigidbody2D.gravityScale = 1f;
    }

    /// <summary>
    /// Disables gravity
    /// </summary>
    /// <param name="rigidbody2D">The rigidbody2D</param>
    public static void DisableGravity(this Rigidbody2D rigidbody2D)
    {
        rigidbody2D.gravityScale = 0f;
    }

    /// <summary>
    /// Toggles gravity
    /// </summary>
    /// <param name="rigidbody2D">The rigidbody2D</param>
    public static void ToggleGravity(this Rigidbody2D rigidbody2D)
    {
        rigidbody2D.gravityScale = rigidbody2D.gravityScale > 0f ? 0f : 1f;
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
    public static bool IsGrounded(this Rigidbody2D rigidbody2D, float groundCheckDistance = 0.1f, LayerMask groundLayer = default)
    {
        return Physics2D.Raycast(rigidbody2D.position, Vector2.down, groundCheckDistance, groundLayer);
    }

    /// <summary>
    /// Checks if the rigidbody2D is grounded using a circle cast
    /// </summary>
    /// <param name="rigidbody2D">The rigidbody2D</param>
    /// <param name="groundCheckDistance">Distance to check for ground</param>
    /// <param name="radius">Radius of the circle cast</param>
    /// <param name="groundLayer">Layer mask for ground objects</param>
    /// <returns>True if grounded</returns>
    public static bool IsGroundedCircle(this Rigidbody2D rigidbody2D, float groundCheckDistance = 0.1f, float radius = 0.5f, LayerMask groundLayer = default)
    {
        return Physics2D.CircleCast(rigidbody2D.position, radius, Vector2.down, groundCheckDistance, groundLayer);
    }

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
    /// Gets the angular speed of the rigidbody2D
    /// </summary>
    /// <param name="rigidbody2D">The rigidbody2D</param>
    /// <returns>Current angular speed</returns>
    public static float GetAngularSpeed(this Rigidbody2D rigidbody2D)
    {
        return Mathf.Abs(rigidbody2D.angularVelocity);
    }

    /// <summary>
    /// Checks if the rigidbody2D is moving
    /// </summary>
    /// <param name="rigidbody2D">The rigidbody2D</param>
    /// <param name="threshold">Speed threshold</param>
    /// <returns>True if moving</returns>
    public static bool IsMoving(this Rigidbody2D rigidbody2D, float threshold = 0.01f)
    {
        return rigidbody2D.linearVelocity.magnitude > threshold;
    }

    /// <summary>
    /// Checks if the rigidbody2D is rotating
    /// </summary>
    /// <param name="rigidbody2D">The rigidbody2D</param>
    /// <param name="threshold">Angular speed threshold</param>
    /// <returns>True if rotating</returns>
    public static bool IsRotating(this Rigidbody2D rigidbody2D, float threshold = 0.01f)
    {
        return Mathf.Abs(rigidbody2D.angularVelocity) > threshold;
    }

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
