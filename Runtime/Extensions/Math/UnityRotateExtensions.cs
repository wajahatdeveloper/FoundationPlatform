using System;
using System.Linq;
using System.Collections.Generic;
using UnityEngine;

namespace AetherNexus.FoundationPlatform.Extensions
{
/// <summary>
/// Extensions for rotating in Unity
/// </summary>
public static class UnityRotateExtensions
{
    /// <summary>
    /// Sets <paramref name="value"/> to passed <paramref name="axis"/>.
    /// </summary>
    /// <param name="quaternion">Target quaternion.</param>
    /// <param name="axis">Axis to set.</param>
    /// <param name="value">Value to set.</param>
    /// <returns>Changed copy of the <paramref name="quaternion"/>.</returns>
    public static Quaternion With(this Quaternion quaternion, int axis, float value)
    {
        quaternion[axis] = value;
        return quaternion;
    }

    /// <summary>
    /// Sets <paramref name="x"/> value to x axis.
    /// </summary>
    /// <param name="quaternion">Target quaternion.</param>
    /// <param name="x">Value to set.</param>
    /// <returns><inheritdoc cref="With(Quaternion, int, float)"/></returns>
    public static Quaternion WithX(this Quaternion quaternion, float x) => With(quaternion, 0, x);

    /// <summary>
    /// Sets <paramref name="y"/> value to y axis.
    /// </summary>
    /// <param name="quaternion">Target quaternion.</param>
    /// <param name="y">Value to set.</param>
    /// <returns><inheritdoc cref="With(Quaternion, int, float)"/></returns>
    public static Quaternion WithY(this Quaternion quaternion, float y) => With(quaternion, 1, y);

    /// <summary>
    /// Sets values to passed axes.
    /// </summary>
    /// <param name="quaternion">Target quaternion.</param>
    /// <param name="axis1">First axis to set.</param>
    /// <param name="value1">First value to set.</param>
    /// <param name="axis2">Second axis to set.</param>
    /// <param name="value2">Second value to set.</param>
    /// <returns>Changed copy of the <paramref name="quaternion"/>.</returns>
    public static Quaternion With(this Quaternion quaternion, int axis1, float value1, int axis2, float value2)
    {
        quaternion[axis1] = value1;
        quaternion[axis2] = value2;
        return quaternion;
    }

    /// <summary>
    /// Sets values to passed axes.
    /// </summary>
    /// <param name="quaternion">Target quaternion.</param>
    /// <param name="axis1">First axis to set.</param>
    /// <param name="value1">First value to set.</param>
    /// <param name="axis2">Second axis to set.</param>
    /// <param name="value2">Second value to set.</param>
    /// <param name="axis3">Third axis to set.</param>
    /// <param name="value3">Third value to set.</param>
    /// <returns>Changed copy of the <paramref name="quaternion"/>.</returns>
    public static Quaternion With(this Quaternion quaternion, int axis1, float value1, int axis2, float value2,
        int axis3, float value3)
    {
        quaternion[axis1] = value1;
        quaternion[axis2] = value2;
        quaternion[axis3] = value3;
        return quaternion;
    }

#region Rotate_DegreesPerSecond
#endregion
#region RotateAroundAxis
#region RotateAroundAxis_X
#endregion
#region RotateAroundAxis_Y
#endregion
#region RotateAroundAxis_Z

    // RotateAroundAxis_Z

    #endregion

    // RotateAroundAxis

    #endregion

    #region LookAt

    /// <summary>
    /// No matter where the <paramref name="targetGo"/> goes, rotate toward him, like a gun turret following a target.
    /// </summary>
    /// <param name="go"></param>
    /// <param name="targetGo">object to look at</param>
    public static void LookAt(this GameObject go, GameObject targetGo)
    {
        LookAt(go, targetGo.transform);
    }

    /// <summary>
    /// No matter where the <paramref name="targetTrans"/> goes, rotate toward him, like a gun turret following a target.
    /// </summary>
    /// <param name="go"></param>
    /// <param name="targetTrans">object to look at</param>
    public static void LookAt(this GameObject go, Transform targetTrans)
    {
        go.transform.LookAt(targetTrans);
    }

    /// <summary>
    /// No matter where the <paramref name="targetVector"/> goes, rotate toward him, like a gun turret following a target.
    /// </summary>
    /// <param name="go"></param>
    /// <param name="targetVector">object to look at</param>
    public static void LookAt(this GameObject go, Vector3 targetVector)
    {
        go.transform.LookAt(targetVector);
    }

    // LookAt

    #endregion

    /// <summary>
    /// Quaternion raised to a power.
    /// This is useful for smoothly multiplying a Quaternion by a given floating-point value.
    /// </summary>
    public static Quaternion Pow(this Quaternion quaternion, float power)
    {
        float magnitude = quaternion.Magnitude();
        Vector3 normalizedVector = new Vector3(quaternion.x, quaternion.y, quaternion.z).normalized;

        return new Quaternion(normalizedVector.x, normalizedVector.y, normalizedVector.z, 0)
            .ScalarMultiply(power * Mathf.Acos(quaternion.w / magnitude))
            .Exp()
            .ScalarMultiply(Mathf.Pow(magnitude, power));
    }

    /// <summary>
    /// Euler's number raised to quaternion.
    /// </summary>
    public static Quaternion Exp(this Quaternion quaternion)
    {
        Vector3 inputVector = new Vector3(quaternion.x, quaternion.y, quaternion.z);
        float inputScalar = quaternion.w;

        Vector3 outputVector = Mathf.Exp(inputScalar) * (inputVector.normalized * Mathf.Sin(inputVector.magnitude));
        float outputScalar = Mathf.Exp(inputScalar) * Mathf.Cos(inputVector.magnitude);

        return new Quaternion(outputVector.x, outputVector.y, outputVector.z, outputScalar);
    }

    public static float Magnitude(this Quaternion quaternion)
    {
        return Mathf.Sqrt(quaternion.x * quaternion.x +
                          quaternion.y * quaternion.y +
                          quaternion.z * quaternion.z +
                          quaternion.w * quaternion.w);
    }

    public static Quaternion ScalarMultiply(this Quaternion quaternion, float scalar)
    {
        return new Quaternion(
            quaternion.x * scalar,
            quaternion.y * scalar,
            quaternion.z * scalar,
            quaternion.w * scalar);
    }
}}
