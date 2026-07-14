using System;
using UnityEngine;

/// <summary>
/// Essential Quaternion extension methods for Unity development
/// </summary>
namespace AetherNexus.FoundationPlatform.Extensions
{
public static class QuaternionExtensions
{
    #region Smooth Rotation

    /// <summary>
    /// Smoothly rotates towards a target rotation using Slerp
    /// </summary>
    /// <param name="current">Current rotation</param>
    /// <param name="target">Target rotation</param>
    /// <param name="speed">Rotation speed</param>
    /// <returns>Smoothed rotation</returns>
    public static Quaternion SmoothRotateTowards(this Quaternion current, Quaternion target, float speed)
    {
        return Quaternion.Slerp(current, target, speed * Time.deltaTime);
    }

    /// <summary>
    /// Smoothly rotates towards a target rotation using Lerp
    /// </summary>
    /// <param name="current">Current rotation</param>
    /// <param name="target">Target rotation</param>
    /// <param name="speed">Rotation speed</param>
    /// <returns>Smoothed rotation</returns>
    public static Quaternion SmoothRotateTowardsLerp(this Quaternion current, Quaternion target, float speed)
    {
        return Quaternion.Lerp(current, target, speed * Time.deltaTime);
    }

    /// <summary>
    /// Smoothly rotates towards a target rotation with damping
    /// </summary>
    /// <param name="current">Current rotation</param>
    /// <param name="target">Target rotation</param>
    /// <param name="velocity">Reference velocity for damping</param>
    /// <param name="smoothTime">Smooth time</param>
    /// <returns>Smoothed rotation</returns>
    public static Quaternion SmoothRotateTowardsDamped(this Quaternion current, Quaternion target, ref Vector3 velocity, float smoothTime)
    {
        Vector3 currentEuler = current.eulerAngles;
        Vector3 targetEuler = target.eulerAngles;
        
        return Quaternion.Euler(
            Mathf.SmoothDampAngle(currentEuler.x, targetEuler.x, ref velocity.x, smoothTime),
            Mathf.SmoothDampAngle(currentEuler.y, targetEuler.y, ref velocity.y, smoothTime),
            Mathf.SmoothDampAngle(currentEuler.z, targetEuler.z, ref velocity.z, smoothTime)
        );
    }

    #endregion

    #region LookAt Variations

    /// <summary>
    /// Creates a rotation that looks at a target position with optional up vector
    /// </summary>
    /// <param name="from">Source position</param>
    /// <param name="to">Target position</param>
    /// <param name="up">Up vector (default: Vector3.up)</param>
    /// <returns>Rotation looking at target</returns>
    public static Quaternion LookAt(Vector3 from, Vector3 to, Vector3 up = default)
    {
        if (up == default) up = Vector3.up;
        Vector3 direction = (to - from).normalized;
        return Quaternion.LookRotation(direction, up);
    }

    /// <summary>
    /// Creates a rotation that looks at a target position, ignoring Y axis
    /// </summary>
    /// <param name="from">Source position</param>
    /// <param name="to">Target position</param>
    /// <returns>Rotation looking at target (Y-axis ignored)</returns>
    public static Quaternion LookAt2D(Vector3 from, Vector3 to)
    {
        Vector3 direction = (to - from).normalized;
        direction.y = 0; // Ignore Y axis
        if (direction == Vector3.zero) return Quaternion.identity;
        return Quaternion.LookRotation(direction);
    }

    /// <summary>
    /// Creates a rotation that looks at a target position, only considering Y axis
    /// </summary>
    /// <param name="from">Source position</param>
    /// <param name="to">Target position</param>
    /// <returns>Rotation looking at target (only Y-axis)</returns>
    public static Quaternion LookAtY(Vector3 from, Vector3 to)
    {
        Vector3 direction = (to - from).normalized;
        direction.x = 0; // Ignore X axis
        direction.z = 0; // Ignore Z axis
        if (direction == Vector3.zero) return Quaternion.identity;
        return Quaternion.LookRotation(direction);
    }

    #endregion

    #region Euler Angle Utilities

    /// <summary>
    /// Sets the X component of the euler angles
    /// </summary>
    /// <param name="rotation">Current rotation</param>
    /// <param name="x">X euler angle</param>
    /// <returns>New rotation with X set</returns>
    public static Quaternion SetEulerX(this Quaternion rotation, float x)
    {
        Vector3 euler = rotation.eulerAngles;
        euler.x = x;
        return Quaternion.Euler(euler);
    }

    /// <summary>
    /// Sets the Y component of the euler angles
    /// </summary>
    /// <param name="rotation">Current rotation</param>
    /// <param name="y">Y euler angle</param>
    /// <returns>New rotation with Y set</returns>
    public static Quaternion SetEulerY(this Quaternion rotation, float y)
    {
        Vector3 euler = rotation.eulerAngles;
        euler.y = y;
        return Quaternion.Euler(euler);
    }

    /// <summary>
    /// Sets the Z component of the euler angles
    /// </summary>
    /// <param name="rotation">Current rotation</param>
    /// <param name="z">Z euler angle</param>
    /// <returns>New rotation with Z set</returns>
    public static Quaternion SetEulerZ(this Quaternion rotation, float z)
    {
        Vector3 euler = rotation.eulerAngles;
        euler.z = z;
        return Quaternion.Euler(euler);
    }

    /// <summary>
    /// Adds to the X component of the euler angles
    /// </summary>
    /// <param name="rotation">Current rotation</param>
    /// <param name="x">X euler angle to add</param>
    /// <returns>New rotation with X added</returns>
    public static Quaternion AddEulerX(this Quaternion rotation, float x)
    {
        Vector3 euler = rotation.eulerAngles;
        euler.x += x;
        return Quaternion.Euler(euler);
    }

    /// <summary>
    /// Adds to the Y component of the euler angles
    /// </summary>
    /// <param name="rotation">Current rotation</param>
    /// <param name="y">Y euler angle to add</param>
    /// <returns>New rotation with Y added</returns>
    public static Quaternion AddEulerY(this Quaternion rotation, float y)
    {
        Vector3 euler = rotation.eulerAngles;
        euler.y += y;
        return Quaternion.Euler(euler);
    }

    /// <summary>
    /// Adds to the Z component of the euler angles
    /// </summary>
    /// <param name="rotation">Current rotation</param>
    /// <param name="z">Z euler angle to add</param>
    /// <returns>New rotation with Z added</returns>
    public static Quaternion AddEulerZ(this Quaternion rotation, float z)
    {
        Vector3 euler = rotation.eulerAngles;
        euler.z += z;
        return Quaternion.Euler(euler);
    }

    #endregion

    #region Rotation Utilities

    /// <summary>
    /// Clamps the euler angles to specified ranges
    /// </summary>
    /// <param name="rotation">Current rotation</param>
    /// <param name="xMin">Minimum X angle</param>
    /// <param name="xMax">Maximum X angle</param>
    /// <param name="yMin">Minimum Y angle</param>
    /// <param name="yMax">Maximum Y angle</param>
    /// <param name="zMin">Minimum Z angle</param>
    /// <param name="zMax">Maximum Z angle</param>
    /// <returns>Clamped rotation</returns>
    public static Quaternion ClampEuler(this Quaternion rotation, float xMin, float xMax, float yMin, float yMax, float zMin, float zMax)
    {
        Vector3 euler = rotation.eulerAngles;
        euler.x = Mathf.Clamp(euler.x, xMin, xMax);
        euler.y = Mathf.Clamp(euler.y, yMin, yMax);
        euler.z = Mathf.Clamp(euler.z, zMin, zMax);
        return Quaternion.Euler(euler);
    }

    /// <summary>
    /// Gets the angle between two rotations
    /// </summary>
    /// <param name="rotation1">First rotation</param>
    /// <param name="rotation2">Second rotation</param>
    /// <returns>Angle in degrees</returns>
    public static float AngleTo(this Quaternion rotation1, Quaternion rotation2)
    {
        return Quaternion.Angle(rotation1, rotation2);
    }

    /// <summary>
    /// Checks if two rotations are approximately equal
    /// </summary>
    /// <param name="rotation1">First rotation</param>
    /// <param name="rotation2">Second rotation</param>
    /// <param name="tolerance">Tolerance for comparison</param>
    /// <returns>True if approximately equal</returns>
    public static bool Approximately(this Quaternion rotation1, Quaternion rotation2, float tolerance = 0.01f)
    {
        return Quaternion.Angle(rotation1, rotation2) < tolerance;
    }

    /// <summary>
    /// Rotates around a specific axis
    /// </summary>
    /// <param name="rotation">Current rotation</param>
    /// <param name="axis">Axis to rotate around</param>
    /// <param name="angle">Angle in degrees</param>
    /// <returns>New rotation</returns>
    public static Quaternion RotateAround(this Quaternion rotation, Vector3 axis, float angle)
    {
        return rotation * Quaternion.AngleAxis(angle, axis);
    }

    /// <summary>
    /// Gets the forward direction of the rotation
    /// </summary>
    /// <param name="rotation">Current rotation</param>
    /// <returns>Forward direction vector</returns>
    public static Vector3 Forward(this Quaternion rotation)
    {
        return rotation * Vector3.forward;
    }

    /// <summary>
    /// Gets the right direction of the rotation
    /// </summary>
    /// <param name="rotation">Current rotation</param>
    /// <returns>Right direction vector</returns>
    public static Vector3 Right(this Quaternion rotation)
    {
        return rotation * Vector3.right;
    }

    /// <summary>
    /// Gets the up direction of the rotation
    /// </summary>
    /// <param name="rotation">Current rotation</param>
    /// <returns>Up direction vector</returns>
    public static Vector3 Up(this Quaternion rotation)
    {
        return rotation * Vector3.up;
    }

    #endregion

    #region Interpolation Helpers

    /// <summary>
    /// Interpolates between two rotations using Slerp with a t value
    /// </summary>
    /// <param name="from">From rotation</param>
    /// <param name="to">To rotation</param>
    /// <param name="t">Interpolation value (0-1)</param>
    /// <returns>Interpolated rotation</returns>
    public static Quaternion SlerpTo(this Quaternion from, Quaternion to, float t)
    {
        return Quaternion.Slerp(from, to, t);
    }

    /// <summary>
    /// Interpolates between two rotations using Lerp with a t value
    /// </summary>
    /// <param name="from">From rotation</param>
    /// <param name="to">To rotation</param>
    /// <param name="t">Interpolation value (0-1)</param>
    /// <returns>Interpolated rotation</returns>
    public static Quaternion LerpTo(this Quaternion from, Quaternion to, float t)
    {
        return Quaternion.Lerp(from, to, t);
    }

    /// <summary>
    /// Interpolates between two rotations using SlerpUnclamped with a t value
    /// </summary>
    /// <param name="from">From rotation</param>
    /// <param name="to">To rotation</param>
    /// <param name="t">Interpolation value (can be outside 0-1)</param>
    /// <returns>Interpolated rotation</returns>
    public static Quaternion SlerpUnclampedTo(this Quaternion from, Quaternion to, float t)
    {
        return Quaternion.SlerpUnclamped(from, to, t);
    }

    #endregion
}
}
