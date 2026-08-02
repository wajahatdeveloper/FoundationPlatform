using System;
using System.Collections.Generic;
using AetherNexus.FoundationPlatform.DebugX;
using AetherNexus.FoundationPlatform.Extensions;
using AetherNexus.FoundationPlatform.AetherInspector;
using UnityEngine;
#if UNITY_EDITOR
using UnityEditor;
#endif

namespace AetherNexus.FoundationPlatform.Behaviours
{
	/// <summary>
	///  Spawner script for spawning units within an adjustable area.
	///  Attached to spawn area GameObjects (one for player, one for enemy, etc.).
	///  Supports both deterministic (via IRandomProvider) and non-deterministic (Unity Random) spawning.
	/// </summary>
	public class AreaSpawner : MonoBehaviour
	{
		[System.Serializable]
		public sealed class SpawnTransformOverrideSettings
		{
			[Tooltip("Local-space offset added to each random spawn position.")]
			public Vector3 positionOffset;

			[Tooltip("Local-space euler rotation applied relative to this spawner.")]
			public Vector3 rotationEuler;

			[Tooltip("Local scale applied to spawned instances.")]
			public Vector3 scale = Vector3.one;
		}

		[Header("Spawn Configuration")]
		[SerializeField] private GameObject unitPrefab;

		[Tooltip("Number of units to spawn when Spawn On Enable is checked or SpawnUnits is called with this count.")]
		[MinValue(0)]
		[SerializeField] private int spawnCount = 1;

		[Tooltip("Spawn units on every OnEnable when checked. With a deterministic random provider, waits until gameplay RNG is initialized.")]
		[SerializeField] private bool spawnOnEnable;

		[FoldoutGroup("Spawn Transform Override")]
		[Tooltip("When enabled, apply the spawn transform overrides below. When disabled, the prefab local transform is preserved (only world position comes from the spawn area).")]
		[SerializeField] private bool overrideSpawnTransform = true;

		[FoldoutGroup("Spawn Transform Override")]
		[ShowIf("@overrideSpawnTransform")]
		[InlineProperty, HideLabel]
		[SerializeField] private SpawnTransformOverrideSettings spawnTransformOverride = new();

		[Tooltip("Minimum clearance radius around each spawn point. 0 disables overlap checks.")]
		[MinValue(0)]
		[SerializeField] private float overlapAvoidanceRadius;

		[ShowIf("@overlapAvoidanceRadius > 0")]
		[Tooltip("Layers checked when testing spawn overlap.")]
		[SerializeField] private LayerMask overlapLayerMask = Physics.AllLayers;

		[Header("Spawn Area")]
		[SerializeField] private BoxCollider spawnArea; // Optional - if not set, uses transform + size

		[SerializeField] private Vector3 spawnAreaSize = new(10, 0, 10);
		[SerializeField] private Vector3 spawnAreaCenter = Vector3.zero; // Offset from transform position

		[Header("Random Provider")]
		[Tooltip("Optional random provider for deterministic spawning. If null, uses Unity Random.")]
		[SerializeField] private MonoBehaviour randomProviderBehaviour; // Serialized as MonoBehaviour for inspector assignment

		#if UNITY_EDITOR
		private const string DeterministicProviderType =
			"GameEngineCore.Runtime.DeterministicRandomProviderBehaviour, GameEngineCore";

		private const string UnityRandomProviderType =
			"GameEngineCore.Runtime.UnityRandomProviderBehaviour, GameEngineCore";

		[ShowIf("@randomProviderBehaviour == null")]
		[ValueDropdown(nameof(GetProviderTypeOptions))]
		[Tooltip("Random provider component type to add when none is assigned.")]
		[SerializeField] private string selectedProviderType = DeterministicProviderType;

		[ShowIf("@randomProviderBehaviour == null")]
		[Button("Add Random Provider")]
		private void AddRandomProviderComponent()
		{
			var providerType = System.Type.GetType(selectedProviderType);
			if (providerType == null)
			{
				FoundationPlatform.DebugX.DebugX.Builder(LogChannels.Validation).WithContext(gameObject)
				      .Error("AreaSpawner on {GameObjectName}: Could not resolve provider type '{ProviderType}'.",
				             gameObject.name, selectedProviderType);
				return;
			}

			randomProviderBehaviour = (MonoBehaviour)gameObject.AddComponent(providerType);
#if UNITY_EDITOR
			EditorUtility.SetDirty(this);
#endif
		}

		private static IEnumerable<ValueDropdownItem<string>> GetProviderTypeOptions()
		{
			yield return new ValueDropdownItem<string>("Deterministic (Gameplay RNG)", DeterministicProviderType);
			yield return new ValueDropdownItem<string>("Unity Random (Non-deterministic)", UnityRandomProviderType);
		}
		#endif

		private IRandomProvider _randomProvider;
		private bool _usesDeterministicProvider;

		private const int MaxSpawnPlacementAttempts = 64;

		private void Awake()
		{
			if (randomProviderBehaviour != null)
			{
				_randomProvider = randomProviderBehaviour as IRandomProvider;
				if (_randomProvider == null)
				{
					FoundationPlatform.DebugX.DebugX.Builder(LogChannels.Validation).WithContext(gameObject)
					      .Error(
						      "AreaSpawner on {GameObjectName}: randomProviderBehaviour does not implement IRandomProvider.",
						      gameObject.name);
				}
			}

			_usesDeterministicProvider = _randomProvider != null;
		}

		private void OnEnable()
		{
			if (!spawnOnEnable || spawnCount <= 0)
				return;

			TrySpawnWhenReady();
		}

		private void OnDisable()
		{
			SceneSpawnReadyGate.OnReady -= SpawnConfiguredUnits;
		}

		private void TrySpawnWhenReady()
		{
			if (!_usesDeterministicProvider || SceneSpawnReadyGate.IsReady)
			{
				SpawnConfiguredUnits();
				return;
			}

			SceneSpawnReadyGate.OnReady -= SpawnConfiguredUnits;
			SceneSpawnReadyGate.OnReady += SpawnConfiguredUnits;
		}

		private void SpawnConfiguredUnits()
		{
			if (!spawnOnEnable || spawnCount <= 0 || !isActiveAndEnabled)
				return;

			SpawnUnits(spawnCount);
		}

		/// <summary>
		///  Set the random provider for deterministic spawning.
		/// </summary>
		public void SetRandomProvider(IRandomProvider provider)
		{
			_randomProvider = provider;
			_usesDeterministicProvider = provider != null;
		}

		/// <summary>
		///  Draw spawn area gizmo in editor for visualization.
		/// </summary>
		private void OnDrawGizmosSelected()
		{
			UnityEngine.Gizmos.color = Color.green;

			Bounds bounds;
			if (spawnArea != null)
			{
				bounds = spawnArea.bounds;
			}
			else
			{
				var center = transform.position + spawnAreaCenter;
				bounds = new Bounds(center, spawnAreaSize);
			}

			UnityEngine.Gizmos.DrawWireCube(bounds.center, bounds.size);
		}

		/// <summary>
		///  Spawn count units randomly within the spawn area.
		/// </summary>
		/// <param name="count">Number of units to spawn</param>
		/// <returns>List of spawned unit GameObjects</returns>
		public List<GameObject> SpawnUnits(int count)
		{
			if (unitPrefab == null)
			{
				FoundationPlatform.DebugX.DebugX.Builder(LogChannels.Validation).WithContext(gameObject)
				      .Error("AreaSpawner on {GameObjectName}: Unit prefab is not assigned!", gameObject.name);
				return new List<GameObject>();
			}

			var spawned = new List<GameObject>();
			var placedPositions = new List<Vector3>(count);

			for (var i = 0; i < count; i++)
			{
				if (!TryFindSpawnPosition(placedPositions, out var position))
				{
					FoundationPlatform.DebugX.DebugX.Builder(LogChannels.Validation).WithContext(gameObject)
					      .Error(
						      "AreaSpawner on {GameObjectName}: Could not find a clear spawn position for unit {Index} after {Attempts} attempts.",
						      gameObject.name, i + 1, MaxSpawnPlacementAttempts);
					continue;
				}

				GameObject instance;
				if (overrideSpawnTransform)
				{
					var rotation = transform.rotation * Quaternion.Euler(spawnTransformOverride.rotationEuler);
					instance = Instantiate(unitPrefab, position, rotation, transform);
					instance.transform.localScale = spawnTransformOverride.scale;
				}
				else
				{
					// Instantiate directly at the spawn position (not moved after Awake).
					// Components that cache their position in Awake (e.g. KinematicCharacterMotor)
					// would otherwise overwrite the post-Awake move on their first tick, snapping
					// every unit back to the prefab's baked position.
					instance = Instantiate(unitPrefab, position, unitPrefab.transform.rotation, transform);
				}

				placedPositions.Add(position);
				spawned.Add(instance);
			}

			FoundationPlatform.DebugX.DebugX.Builder(LogChannels.Default).WithContext(gameObject)
			      .Info("AreaSpawner on {GameObjectName}: Spawned {Count} units", gameObject.name, count);
			return spawned;
		}

		private bool TryFindSpawnPosition(List<Vector3> placedPositions, out Vector3 position)
		{
			for (var attempt = 0; attempt < MaxSpawnPlacementAttempts; attempt++)
			{
				var candidate = GetFinalSpawnPosition(GetRandomSpawnPosition());
				if (IsSpawnPositionClear(candidate, placedPositions))
				{
					position = candidate;
					return true;
				}
			}

			position = default;
			return false;
		}

		private Vector3 GetFinalSpawnPosition(Vector3 areaPosition)
		{
			if (!overrideSpawnTransform)
				return areaPosition;

			if (spawnTransformOverride == null)
			{
				FoundationPlatform.DebugX.DebugX.Builder(LogChannels.Validation).WithContext(gameObject)
				      .Error("AreaSpawner on {GameObjectName}: spawnTransformOverride is not assigned.", gameObject.name);
				return areaPosition;
			}

			return areaPosition + transform.TransformVector(spawnTransformOverride.positionOffset);
		}

		private bool IsSpawnPositionClear(Vector3 position, List<Vector3> placedPositions)
		{
			if (overlapAvoidanceRadius <= 0f)
				return true;

			var minSeparationSqr = overlapAvoidanceRadius * overlapAvoidanceRadius;
			for (var i = 0; i < placedPositions.Count; i++)
			{
				if ((position - placedPositions[i]).sqrMagnitude < minSeparationSqr)
					return false;
			}

			return PhysicsExtensions.IsPositionClear(position, overlapAvoidanceRadius, overlapLayerMask);
		}

		/// <summary>
		///  Get a random position within the spawn area bounds.
		/// </summary>
		private Vector3 GetRandomSpawnPosition()
		{
			Bounds bounds;

			if (spawnArea != null)
			{
				// Use BoxCollider bounds
				bounds = spawnArea.bounds;
			}
			else
			{
				// Use transform position + size
				var center = transform.position + spawnAreaCenter;
				bounds = new Bounds(center, spawnAreaSize);
			}

			// Generate random position within bounds using injected provider or Unity Random
			float x, z;
			if (_randomProvider != null)
			{
				x = _randomProvider.Range(bounds.min.x, bounds.max.x);
				z = _randomProvider.Range(bounds.min.z, bounds.max.z);
			}
			else
			{
				// Spawn positions are simulation-affecting facts. If the scene relies on deterministic
				// startup but no random provider was wired, fail-fast instead of silently using
				// UnityEngine.Random (which would make spawns non-deterministic with no diagnostic).
				if (SceneSpawnReadyGate.UsesDeterministicStartup)
				{
					FoundationPlatform.DebugX.DebugX.Builder(LogChannels.Validation).WithContext(gameObject)
					      .Error(
						      "AreaSpawner on {GameObjectName}: deterministic startup is in use but no IRandomProvider is assigned. Assign a deterministic random provider; refusing to fall back to UnityEngine.Random for simulation-affecting spawn positions.",
						      gameObject.name);
					throw new InvalidOperationException(
						$"AreaSpawner on {gameObject.name}: deterministic startup is in use but no IRandomProvider is assigned.");
				}

				x = UnityEngine.Random.Range(bounds.min.x, bounds.max.x);
				z = UnityEngine.Random.Range(bounds.min.z, bounds.max.z);
			}

			var y = bounds.center.y; // Use center Y to spawn on ground level
			return new Vector3(x, y, z);
		}
	}
}
