/// <summary>
/// Single source of truth for the simulation seed used across all seeded random systems.
/// Change this value to replay with a different seed; keep it constant for deterministic builds.
/// </summary>
namespace AetherNexus.FoundationPlatform.Extensions
{
public static class GlobalSimulationSeed
{
    public const int Value = 1234556;
}
}
