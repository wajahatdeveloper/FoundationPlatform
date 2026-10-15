using UnityEngine;

namespace AetherNexus.FoundationPlatform.Extensions
{
public static class Vector4Extensions
{
    /// <summary>
    /// Get vector from source to destination
    /// </summary>
    public static Vector4 To(this Vector4 source, Vector4 destination) =>
        destination - source;

    /// <summary>
    /// Raise each component of the source Vector3 to the specified power.
    /// </summary>
    public static Vector4 Pow(this Vector4 source, float exponent) =>
        new Vector4(Mathf.Pow(source.x, exponent),
            Mathf.Pow(source.y, exponent),
            Mathf.Pow(source.z, exponent),
            Mathf.Pow(source.w, exponent));
}}
