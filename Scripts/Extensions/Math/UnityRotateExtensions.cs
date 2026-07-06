using System;
using System.Linq;
using System.Collections.Generic;
using UnityEngine;

/* *****************************************************************************
 * File:    UnityRotateExtensions.cs
 * Author:  Philip Pierce - Wednesday, October 29, 2014
 * Description:
 *  Extensions for rotating in Unity
 *  
 * History:
 *  Wednesday, October 29, 2014 - Created
 * ****************************************************************************/

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
    /// Sets <paramref name="z"/> value to z axis.
    /// </summary>
    /// <param name="quaternion">Target quaternion.</param>
    /// <param name="z">Value to set.</param>
    /// <returns><inheritdoc cref="With(Quaternion, int, float)"/></returns>
    public static Quaternion WithZ(this Quaternion quaternion, float z) => With(quaternion, 2, z);

    /// <summary>
    /// Sets <paramref name="w"/> value to w axis.
    /// </summary>
    /// <param name="quaternion">Target quaternion.</param>
    /// <param name="w">Value to set.</param>
    /// <returns><inheritdoc cref="With(Quaternion, int, float)"/></returns>
    public static Quaternion WithW(this Quaternion quaternion, float w) => With(quaternion, 3, w);

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
    /// Sets <paramref name="x"/> and <paramref name="y"/> values to respective axes.
    /// </summary>
    /// <param name="quaternion">Target quaternion.</param>
    /// <param name="x">Value to set.</param>
    /// <param name="y">Value to set.</param>
    /// <returns><inheritdoc cref="With(Quaternion, int, float)"/></returns>
    public static Quaternion WithXY(this Quaternion quaternion, float x, float y) => With(quaternion, 0, x, 1, y);

    /// <summary>
    /// Sets <paramref name="values"/> components to specified axes.
    /// </summary>
    /// <param name="quaternion">Target quaternion.</param>
    /// <param name="values">Value to set.</param>
    /// <returns><inheritdoc cref="With(Quaternion, int, float)"/></returns>
    public static Quaternion WithXY(this Quaternion quaternion, Vector2 values) =>
        With(quaternion, 0, values.x, 1, values.y);

    /// <summary>
    /// Sets <paramref name="x"/> and <paramref name="z"/> values to respective axes.
    /// </summary>
    /// <param name="quaternion">Target quaternion.</param>
    /// <param name="x">Value to set.</param>
    /// <param name="z">Value to set.</param>
    /// <returns><inheritdoc cref="With(Quaternion, int, float)"/></returns>
    public static Quaternion WithXZ(this Quaternion quaternion, float x, float z) => With(quaternion, 0, x, 2, z);

    /// <summary>
    /// Sets <paramref name="values"/> components to specified axes.
    /// </summary>
    /// <param name="quaternion">Target quaternion.</param>
    /// <param name="values">Value to set.</param>
    /// <returns><inheritdoc cref="With(Quaternion, int, float)"/></returns>
    public static Quaternion WithXZ(this Quaternion quaternion, Vector2 values) =>
        With(quaternion, 0, values.x, 2, values.y);

    /// <summary>
    /// Sets <paramref name="y"/> and <paramref name="z"/> values to respective axes.
    /// </summary>
    /// <param name="quaternion">Target quaternion.</param>
    /// <param name="y">Value to set.</param>
    /// <param name="z">Value to set.</param>
    /// <returns><inheritdoc cref="With(Quaternion, int, float)"/></returns>
    public static Quaternion WithYZ(this Quaternion quaternion, float y, float z) => With(quaternion, 1, y, 2, z);

    /// <summary>
    /// Sets <paramref name="values"/> components to specified axes.
    /// </summary>
    /// <param name="quaternion">Target quaternion.</param>
    /// <param name="values">Value to set.</param>
    /// <returns><inheritdoc cref="With(Quaternion, int, float)"/></returns>
    public static Quaternion WithYZ(this Quaternion quaternion, Vector2 values) =>
        With(quaternion, 1, values.x, 2, values.y);

    /// <summary>
    /// Sets <paramref name="x"/> and <paramref name="w"/> values to respective axes.
    /// </summary>
    /// <param name="quaternion">Target quaternion.</param>
    /// <param name="x">Value to set.</param>
    /// <param name="w">Value to set.</param>
    /// <returns><inheritdoc cref="With(Quaternion, int, float)"/></returns>
    public static Quaternion WithXW(this Quaternion quaternion, float x, float w) => With(quaternion, 0, x, 3, w);

    /// <summary>
    /// Sets <paramref name="values"/> components to specified axes.
    /// </summary>
    /// <param name="quaternion">Target quaternion.</param>
    /// <param name="values">Value to set.</param>
    /// <returns><inheritdoc cref="With(Quaternion, int, float)"/></returns>
    public static Quaternion WithXW(this Quaternion quaternion, Vector2 values) =>
        With(quaternion, 0, values.x, 3, values.y);

    /// <summary>
    /// Sets <paramref name="y"/> and <paramref name="w"/> values to respective axes.
    /// </summary>
    /// <param name="quaternion">Target quaternion.</param>
    /// <param name="y">Value to set.</param>
    /// <param name="w">Value to set.</param>
    /// <returns><inheritdoc cref="With(Quaternion, int, float)"/></returns>
    public static Quaternion WithYW(this Quaternion quaternion, float y, float w) => With(quaternion, 1, y, 3, w);

    /// <summary>
    /// Sets <paramref name="values"/> components to specified axes.
    /// </summary>
    /// <param name="quaternion">Target quaternion.</param>
    /// <param name="values">Value to set.</param>
    /// <returns><inheritdoc cref="With(Quaternion, int, float)"/></returns>
    public static Quaternion WithYW(this Quaternion quaternion, Vector2 values) =>
        With(quaternion, 1, values.x, 3, values.y);

    /// <summary>
    /// Sets <paramref name="z"/> and <paramref name="w"/> values to respective axes.
    /// </summary>
    /// <param name="quaternion">Target quaternion.</param>
    /// <param name="z">Value to set.</param>
    /// <param name="w">Value to set.</param>
    /// <returns><inheritdoc cref="With(Quaternion, int, float)"/></returns>
    public static Quaternion WithZW(this Quaternion quaternion, float z, float w) => With(quaternion, 2, z, 3, w);

    /// <summary>
    /// Sets <paramref name="values"/> components to specified axes.
    /// </summary>
    /// <param name="quaternion">Target quaternion.</param>
    /// <param name="values">Value to set.</param>
    /// <returns><inheritdoc cref="With(Quaternion, int, float)"/></returns>
    public static Quaternion WithZW(this Quaternion quaternion, Vector2 values) =>
        With(quaternion, 2, values.x, 3, values.y);

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

    /// <summary>
    /// Sets <paramref name="x"/>, <paramref name="y"/> and <paramref name="z"/> values to respective axes.
    /// </summary>
    /// <param name="quaternion">Target quaternion.</param>
    /// <param name="x">Value to set.</param>
    /// <param name="y">Value to set.</param>
    /// <param name="z">Value to set.</param>
    /// <returns><inheritdoc cref="With(Quaternion, int, float)"/></returns>
    public static Quaternion WithXYZ(this Quaternion quaternion, float x, float y, float z) =>
        With(quaternion, 0, x, 1, y, 2, z);

    /// <summary>
    /// Sets <paramref name="values"/> components to specified axes.
    /// </summary>
    /// <param name="quaternion">Target quaternion.</param>
    /// <param name="values">Value to set.</param>
    /// <returns><inheritdoc cref="With(Quaternion, int, float)"/></returns>
    public static Quaternion WithXYZ(this Quaternion quaternion, Vector3 values) =>
        With(quaternion, 0, values.x, 1, values.y, 2, values.z);

    /// <summary>
    /// Sets <paramref name="x"/>, <paramref name="y"/> and <paramref name="w"/> values to respective axes.
    /// </summary>
    /// <param name="quaternion">Target quaternion.</param>
    /// <param name="x">Value to set.</param>
    /// <param name="y">Value to set.</param>
    /// <param name="w">Value to set.</param>
    /// <returns><inheritdoc cref="With(Quaternion, int, float)"/></returns>
    public static Quaternion WithXYW(this Quaternion quaternion, float x, float y, float w) =>
        With(quaternion, 0, x, 1, y, 3, w);

    /// <summary>
    /// Sets <paramref name="values"/> components to specified axes.
    /// </summary>
    /// <param name="quaternion">Target quaternion.</param>
    /// <param name="values">Value to set.</param>
    /// <returns><inheritdoc cref="With(Quaternion, int, float)"/></returns>
    public static Quaternion WithXYW(this Quaternion quaternion, Vector3 values) =>
        With(quaternion, 0, values.x, 1, values.y, 3, values.z);

    /// <summary>
    /// Sets <paramref name="x"/>, <paramref name="z"/> and <paramref name="w"/> values to respective axes.
    /// </summary>
    /// <param name="quaternion">Target quaternion.</param>
    /// <param name="x">Value to set.</param>
    /// <param name="z">Value to set.</param>
    /// <param name="w">Value to set.</param>
    /// <returns><inheritdoc cref="With(Quaternion, int, float)"/></returns>
    public static Quaternion WithXZW(this Quaternion quaternion, float x, float z, float w) =>
        With(quaternion, 0, x, 2, z, 3, w);

    /// <summary>
    /// Sets <paramref name="values"/> components to specified axes.
    /// </summary>
    /// <param name="quaternion">Target quaternion.</param>
    /// <param name="values">Value to set.</param>
    /// <returns><inheritdoc cref="With(Quaternion, int, float)"/></returns>
    public static Quaternion WithXZW(this Quaternion quaternion, Vector3 values) =>
        With(quaternion, 0, values.x, 2, values.y, 3, values.z);

    /// <summary>
    /// Sets <paramref name="y"/>, <paramref name="z"/> and <paramref name="w"/> values to respective axes.
    /// </summary>
    /// <param name="quaternion">Target quaternion.</param>
    /// <param name="y">Value to set.</param>
    /// <param name="z">Value to set.</param>
    /// <param name="w">Value to set.</param>
    /// <returns><inheritdoc cref="With(Quaternion, int, float)"/></returns>
    public static Quaternion WithYZW(this Quaternion quaternion, float y, float z, float w) =>
        With(quaternion, 1, y, 2, z, 3, w);

    /// <summary>
    /// Sets <paramref name="values"/> components to specified axes.
    /// </summary>
    /// <param name="quaternion">Target quaternion.</param>
    /// <param name="values">Value to set.</param>
    /// <returns><inheritdoc cref="With(Quaternion, int, float)"/></returns>
    public static Quaternion WithYZW(this Quaternion quaternion, Vector3 values) =>
        With(quaternion, 1, values.x, 2, values.y, 3, values.z);


    #region Rotate_DegreesPerSecond

    /// <summary>
    /// Rotates the game object around <paramref name="direction"/> every <paramref name="timeInSeconds"/>.
    /// Using 1 for <paramref name="timeInSeconds"/> will result in the object rotating <paramref name="direction"/> degress in one second.
    /// </summary>
    /// <param name="go"></param>
    /// <param name="direction"></param>
    /// <param name="timeInSeconds"></param>
    public static void Rotate_DegreesPerSecond(this GameObject go, Vector3 direction, float timeInSeconds)
    {
        Rotate_DegreesPerSecond(go.transform, direction, timeInSeconds);
    }

    /// <summary>
    /// Rotates the game object around <paramref name="direction"/> every <paramref name="timeInSeconds"/>.
    /// Using 1 for <paramref name="timeInSeconds"/> will result in the object rotating <paramref name="direction"/> degress in one second.
    /// </summary>
    /// <param name="goTrans"></param>
    /// <param name="direction"></param>
    /// <param name="timeInSeconds"></param>
    public static void Rotate_DegreesPerSecond(this Transform goTrans, Vector3 direction, float timeInSeconds)
    {
        goTrans.Rotate(direction * timeInSeconds * Time.deltaTime);
    }

    // Rotate_DegreesPerSecond

    #endregion

    #region RotateAroundAxis

    #region RotateAroundAxis_X

    /// <summary>
    /// Rotates the game object around the X axis, rotating <paramref name="degrees"/> every <paramref name="timeInSeconds"/>.
    /// Using 1 for <paramref name="timeInSeconds"/> will result in the object rotating <paramref name="degrees"/> in one second.
    /// </summary>
    /// <param name="go"></param>
    /// <param name="degrees"></param>
    /// <param name="timeInSeconds"></param>
    public static void RotateAroundAxis_X(this GameObject go, float degrees, float timeInSeconds)
    {
        RotateAroundAxis_X(go.transform, degrees, timeInSeconds);
    }

    /// <summary>
    /// Rotates the game object around the X axis, rotating <paramref name="degrees"/> every <paramref name="timeInSeconds"/>.
    /// Using 1 for <paramref name="timeInSeconds"/> will result in the object rotating <paramref name="degrees"/> in one second.
    /// </summary>
    /// <param name="goTrans"></param>
    /// <param name="degrees"></param>
    /// <param name="timeInSeconds"></param>
    public static void RotateAroundAxis_X(this Transform goTrans, float degrees, float timeInSeconds)
    {
        Rotate_DegreesPerSecond(goTrans, new Vector3(degrees, 0, 0), timeInSeconds);
    }

    // RotateAroundAxis_X

    #endregion

    #region RotateAroundAxis_Y

    /// <summary>
    /// Rotates the game object around the Y axis, rotating <paramref name="degrees"/> every <paramref name="timeInSeconds"/>.
    /// Using 1 for <paramref name="timeInSeconds"/> will result in the object rotating <paramref name="degrees"/> in one second.
    /// </summary>
    /// <param name="go"></param>
    /// <param name="degrees"></param>
    /// <param name="timeInSeconds"></param>
    public static void RotateAroundAxis_Y(this GameObject go, float degrees, float timeInSeconds)
    {
        RotateAroundAxis_Y(go.transform, degrees, timeInSeconds);
    }

    /// <summary>
    /// Rotates the game object around the Y axis, rotating <paramref name="degrees"/> every <paramref name="timeInSeconds"/>.
    /// Using 1 for <paramref name="timeInSeconds"/> will result in the object rotating <paramref name="degrees"/> in one second.
    /// </summary>
    /// <param name="goTrans"></param>
    /// <param name="degrees"></param>
    /// <param name="timeInSeconds"></param>
    public static void RotateAroundAxis_Y(this Transform goTrans, float degrees, float timeInSeconds)
    {
        Rotate_DegreesPerSecond(goTrans, new Vector3(0, degrees, 0), timeInSeconds);
    }

    // RotateAroundAxis_Y

    #endregion

    #region RotateAroundAxis_Z

    /// <summary>
    /// Rotates the game object around the Z axis, rotating <paramref name="degrees"/> every <paramref name="timeInSeconds"/>.
    /// Using 1 for <paramref name="timeInSeconds"/> will result in the object rotating <paramref name="degrees"/> in one second.
    /// </summary>
    /// <param name="go"></param>
    /// <param name="degrees"></param>
    /// <param name="timeInSeconds"></param>
    public static void RotateAroundAxis_Z(this GameObject go, float degrees, float timeInSeconds)
    {
        RotateAroundAxis_Z(go.transform, degrees, timeInSeconds);
    }

    /// <summary>
    /// Rotates the game object around the Z axis, rotating <paramref name="degrees"/> every <paramref name="timeInSeconds"/>.
    /// Using 1 for <paramref name="timeInSeconds"/> will result in the object rotating <paramref name="degrees"/> in one second.
    /// </summary>
    /// <param name="goTrans"></param>
    /// <param name="degrees"></param>
    /// <param name="timeInSeconds"></param>
    public static void RotateAroundAxis_Z(this Transform goTrans, float degrees, float timeInSeconds)
    {
        Rotate_DegreesPerSecond(goTrans, new Vector3(0, 0, degrees), timeInSeconds);
    }

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
}