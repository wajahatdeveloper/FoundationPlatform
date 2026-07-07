using System;
using UnityEngine;

public static class ReflectionExtensions
{
    /// <summary>
    /// Checks if the target object has a method with the specified name
    /// </summary>
    public static bool HasMethod(this object target, string methodName)
    {
        return target.GetType().GetMethod(methodName) != null;
    }

    /// <summary>
    /// Checks if the target object has a field with the specified name
    /// </summary>
    public static bool HasField(this object target, string fieldName)
    {
        return target.GetType().GetField(fieldName) != null;
    }

    /// <summary>
    /// Checks if the target object has a property with the specified name
    /// </summary>
    public static bool HasProperty(this object target, string propertyName)
    {
        return target.GetType().GetProperty(propertyName) != null;
    }
}

