using System;

public static class NullChecks
{
    public static void VerifyNotNull<T>(this T obj, string errorMsg)
        where T : class
    {
        if (obj == null) throw new ArgumentNullException(nameof(obj), errorMsg);
    }

    public static bool IsNull<T>(this T @this) where T : class
    {
        return @this == null;
    }

    public static bool IsNotNull<T>(this T @this) where T : class
    {
        return @this != null;
    }
}