/// <summary>
/// Interface for state objects that store entity spawn indices for deterministic ID generation.
/// Replaces IEntityIdState for string-based deterministic identities.
/// </summary>
public interface IEntitySpawnState
{
	int NextUnitSpawnIndex { get; set; }
	int NextCitySpawnIndex { get; set; }
}
