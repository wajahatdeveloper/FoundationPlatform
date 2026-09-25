using System;
using UnityEngine;

namespace AetherNexus.FoundationPlatform.Extensions
{
[DisallowMultipleComponent]
[AddComponentMenu("")]
[Obsolete("Persistence is declared, not implied. Register with PersistentObjects and a PersistenceScope instead.")]
public class DontDestroyOnLoad : MonoBehaviour
{
    private void Awake()
    {
        throw new InvalidOperationException(
            $"'{gameObject.name}' uses the retired DontDestroyOnLoad component. Persistence now goes through " +
            "PersistentObjects.Register with a declared PersistenceScope, which is what gives the object its " +
            "duplicate policy and its scene-boundary reset.");
    }
}}
