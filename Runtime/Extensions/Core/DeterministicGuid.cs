using System;
using System.Security.Cryptography;
using System.Text;

/// <summary>
/// Generates stable, zero-allocation GUIDs from string keys using MD5.
/// AOT-safe and GC-friendly for use during gameplay.
/// ThreadStatic MD5 instance: one alloc per thread on first call, zero thereafter.
/// </summary>
namespace AetherNexus.FoundationPlatform.Extensions
{
public static class DeterministicGuid
{
    [ThreadStatic]
    private static MD5 _md5;

    private static MD5 GetMD5() => _md5 ??= MD5.Create();

    /// <summary>
    /// Generates a stable GUID from a string key. Zero-alloc after warmup per thread.
    /// </summary>
    public static Guid Create(string sourceKey)
    {
        if (string.IsNullOrEmpty(sourceKey))
            return Guid.Empty;

        int byteCount = Encoding.UTF8.GetByteCount(sourceKey);
        Span<byte> inputBuffer = stackalloc byte[byteCount];
        Encoding.UTF8.GetBytes(sourceKey, inputBuffer);

        Span<byte> hashBuffer = stackalloc byte[16];

        if (GetMD5().TryComputeHash(inputBuffer, hashBuffer, out _))
            return new Guid(hashBuffer);

        return Guid.Empty;
    }
}
}
