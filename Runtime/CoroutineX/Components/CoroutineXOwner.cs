using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using UnityEngine;

/// <summary>
/// Coroutines owner. Automatically added to game objects that are owners.<br/>
/// Partially controls the behavior of Coroutines, such as stopping all associated Coroutines if the game object is disabled.
/// </summary>
namespace AetherNexus.FoundationPlatform.CoroutineX
{
public sealed class CoroutineXOwner : MonoBehaviour
{
    [SerializeReference]
    private List<CoroutineX> _Coroutines = new();

    /// <summary>
    /// All owned Coroutines. Not contains destroyed Coroutines.
    /// </summary>
    public ReadOnlyCollection<CoroutineX> Coroutines => _Coroutines.AsReadOnly();

    private void OnDisable() => Deactivate();

    private void OnDestroy() => Deactivate();

    private void Deactivate()
    {
        foreach (var coroutine in _Coroutines.ToArray())
            coroutine.OnOwnerDeactivate();
    }

    internal void Add(CoroutineX coroutineX) => _Coroutines.Add(coroutineX);

    internal void Remove(CoroutineX coroutineX) => _Coroutines.Remove(coroutineX);

    internal void TryDestroy()
    {
        if (_Coroutines.Count == 0)
            Destroy(this);
    }
}}
