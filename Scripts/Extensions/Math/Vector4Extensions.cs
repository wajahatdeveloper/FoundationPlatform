using UnityEngine;

public static class Vector4Extensions
{
    /// <summary>
    /// Get vector from source to destination
    /// </summary>
    public static Vector4 To(this Vector4 source, Vector4 destination) =>
        destination - source;

    /// <summary>
    /// Immutably returns the result of the source vector multiplied with
    /// another vector component-wise.
    /// </summary>
    public static Vector4 ScaleBy(this Vector4 source, Vector4 right) =>
        Vector4.Scale(source, right);

    /// <summary>
    /// Raise each component of the source Vector3 to the specified power.
    /// </summary>
    public static Vector4 Pow(this Vector4 source, float exponent) =>
        new Vector4(Mathf.Pow(source.x, exponent),
            Mathf.Pow(source.y, exponent),
            Mathf.Pow(source.z, exponent),
            Mathf.Pow(source.w, exponent));
}