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

    #endregion

    #region LookAt Variations

    /// <summary>
    /// Creates a rotation that looks at a target position with optional up vector
    /// </summary>
    /// <param name="from">Source position</param>
    /// <param name="to">Target position</param>
    /// <param name="up">Up vector (default: Vector3.up)</param>
    /// <returns>Rotation looking at target</returns>
    public static Quaternion LookAt(Vector3 from, Vector3 to, Vector3 up)
    {
        if (up == default) up = Vector3.up;
        Vector3 direction = (to - from).normalized;
        return Quaternion.LookRotation(direction, up);
    }

    /// <summary>Creates a rotation that looks at a target position, using Vector3.up.</summary>
    public static Quaternion LookAt(Vector3 from, Vector3 to) => LookAt(from, to, default);

#endregion
#region Euler Angle Utilities
#endregion
#region Rotation Utilities

    /// <summary>
    /// Checks if two rotations are approximately equal
    /// </summary>
    /// <param name="rotation1">First rotation</param>
    /// <param name="rotation2">Second rotation</param>
    /// <param name="tolerance">Tolerance for comparison</param>
    /// <returns>True if approximately equal</returns>
    public static bool Approximately(this Quaternion rotation1, Quaternion rotation2, float tolerance)
    {
        return Quaternion.Angle(rotation1, rotation2) < tolerance;
    }

    /// <summary>Checks approximate equality using a 0.01 tolerance.</summary>
    public static bool Approximately(this Quaternion rotation1, Quaternion rotation2) => Approximately(rotation1, rotation2, 0.01f);

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

    #endregion
}
}
