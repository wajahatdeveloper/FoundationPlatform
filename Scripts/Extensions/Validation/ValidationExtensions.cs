using System;
using UnityEngine;

public static class ValidationExtensions
{
    /// <summary>
    /// Throws an ArgumentNullException if the argument is null. Returns the value otherwise.
    /// </summary>
    /// <param name="argument">The argument to check for null.</param>
    /// <param name="argumentName">The name of the argument in case it is null.</param>
    /// <returns>Returns the argument.</returns>
    public static T ThrowIfNull<T>(this T argument, string argumentName)
    {
        if (argument == null)
        {
            throw new ArgumentNullException(argumentName);
        }

        return argument;
    }

    /// <summary>
    /// Throws an ArgumentNullException if the argument is null. Returns the value otherwise.
    /// </summary>
    /// <param name="argument">The argument to check for null.</param>
    /// <param name="argumentName">The name of the argument in case it is null.</param>
    /// <returns>Returns the argument.</returns>
    public static object ThrowIfNull(this object argument, string argumentName)
    {
        if (argument is null)
        {
            throw new ArgumentNullException(argumentName);
        }

        return argument;
    }

    /// <summary>
    /// Throws an ArgumentNullException if the argument is null. Returns the value otherwise.
    /// </summary>
    /// <param name="argument">The argument to check for null.</param>
    /// <param name="argumentName">The name of the argument in case it is null.</param>
    /// <returns>Returns the argument.</returns>
    public static Type ThrowIfNull(this System.Type argument, string argumentName)
    {
        if (argument is null)
        {
            throw new ArgumentNullException(argumentName);
        }

        return argument;
    }
}

