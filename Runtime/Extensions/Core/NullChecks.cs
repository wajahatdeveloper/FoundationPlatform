using System;

namespace AetherNexus.FoundationPlatform.Extensions
{
public static class NullChecks
{

    public static bool IsNull<T>(this T @this) where T : class
    {
        return @this == null;
    }
}}
