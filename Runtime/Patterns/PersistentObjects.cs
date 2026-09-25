using System;
using System.Collections.Generic;
using AetherNexus.FoundationPlatform.Logging;
using UnityEngine;

namespace AetherNexus.FoundationPlatform
{
    /// <summary>
    ///     The one way an object becomes persistent. Callers declare a <see cref="PersistenceScope" /> and a
    ///     key instead of calling <c>DontDestroyOnLoad</c> themselves, which buys three things no scattered
    ///     call gives: a readable lifetime, one duplicate policy for the whole project (keep the session
    ///     survivor, destroy the newcomer, report the config the newcomer carried and the survivor lacks),
    ///     and a single place from which the engine broadcasts scene-boundary resets.
    /// </summary>
    public static class PersistentObjects
    {
        public readonly struct Entry
        {
            public Entry(string key, PersistenceScope scope, GameObject owner)
            {
                Key = key;
                Scope = scope;
                Owner = owner;
            }

            public string Key { get; }
            public PersistenceScope Scope { get; }
            public GameObject Owner { get; }
        }

        private static readonly List<Entry> entries = new();
        private static readonly Dictionary<string, int> indexByKey = new(StringComparer.Ordinal);

        // Domain reload being off means these statics survive Stop->Play pointing at destroyed objects,
        // which reads as "already registered" and suppresses the new session's copies.
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetOnPlayModeEnter()
        {
            entries.Clear();
            indexByKey.Clear();
        }

        /// <summary>Live registrations, in registration order. Diagnostics and editor validation.</summary>
        public static IReadOnlyList<Entry> Entries => entries;

        /// <summary>
        ///     Registers <paramref name="owner" /> as the persistent object for <paramref name="key" />.
        ///     Returns true when this instance is the survivor; returns false after destroying it as a
        ///     duplicate, in which case the caller must stop initializing.
        /// </summary>
        public static bool Register(GameObject owner, PersistenceScope scope, string key)
        {
            return Register(owner, scope, key, null);
        }

        /// <summary>
        ///     Registration with a config diff: when this instance loses as a duplicate,
        ///     <paramref name="describeConfigMissingFromSurvivor" /> is asked what the survivor does not
        ///     carry, and a non-empty answer is reported as an error — that config never runs this session.
        /// </summary>
        public static bool Register(
            GameObject owner,
            PersistenceScope scope,
            string key,
            Func<GameObject, IReadOnlyList<string>> describeConfigMissingFromSurvivor)
        {
            if (owner == null) { throw new ArgumentNullException(nameof(owner)); }
            if (string.IsNullOrEmpty(key)) { throw new ArgumentException("A persistence key is required.", nameof(key)); }

            if (TryGetSurvivor(key, out var survivor))
            {
                if (ReferenceEquals(survivor, owner)) { return true; }

                ReportDuplicate(owner, survivor, key, describeConfigMissingFromSurvivor);
                UnityEngine.Object.Destroy(owner);
                return false;
            }

            // DontDestroyOnLoad only applies to roots. A persistent object parented under another one
            // (player controllers under the session root) already survives through its parent.
            if (owner.transform.parent == null) { UnityEngine.Object.DontDestroyOnLoad(owner); }

            indexByKey[key] = entries.Count;
            entries.Add(new Entry(key, scope, owner));
            return true;
        }

        /// <summary>Drops the registration when <paramref name="owner" /> is the one holding the key.</summary>
        public static void Unregister(string key, GameObject owner)
        {
            if (!indexByKey.TryGetValue(key, out var index)) { return; }
            if (!ReferenceEquals(entries[index].Owner, owner)) { return; }

            RemoveAt(index);
        }

        /// <summary>The live object holding <paramref name="key" />, if any.</summary>
        public static bool TryGetSurvivor(string key, out GameObject survivor)
        {
            survivor = null;
            if (!indexByKey.TryGetValue(key, out var index)) { return false; }

            var entry = entries[index];
            if (entry.Owner == null)
            {
                RemoveAt(index);
                return false;
            }

            survivor = entry.Owner;
            return true;
        }

        /// <summary>
        ///     Broadcast after the outgoing scene tore down its world. Driven by the engine's scene
        ///     transition — never call this from a persistent object.
        /// </summary>
        public static void NotifySceneExit()
        {
            Broadcast(isEnter: false);
        }

        /// <summary>Broadcast on the incoming scene, before its world is built.</summary>
        public static void NotifySceneEnter()
        {
            Broadcast(isEnter: true);
        }

        private static void Broadcast(bool isEnter)
        {
            var visited = new HashSet<IScenePersistentReset>();

            for (var i = entries.Count - 1; i >= 0; i--)
            {
                var owner = entries[i].Owner;
                if (owner == null)
                {
                    RemoveAt(i);
                    continue;
                }

                var targets = owner.GetComponentsInChildren<IScenePersistentReset>(true);
                for (var t = 0; t < targets.Length; t++)
                {
                    var target = targets[t];
                    if (!visited.Add(target)) { continue; }

                    if (isEnter) { target.OnScenePersistentEnter(); }
                    else { target.OnScenePersistentExit(); }
                }
            }
        }

        private static void RemoveAt(int index)
        {
            entries.RemoveAt(index);
            indexByKey.Clear();
            for (var i = 0; i < entries.Count; i++)
            {
                indexByKey[entries[i].Key] = i;
            }
        }

        private static void ReportDuplicate(
            GameObject duplicate,
            GameObject survivor,
            string key,
            Func<GameObject, IReadOnlyList<string>> describeConfigMissingFromSurvivor)
        {
            var lost = describeConfigMissingFromSurvivor == null
                ? Array.Empty<string>()
                : describeConfigMissingFromSurvivor(survivor);

            if (lost == null || lost.Count == 0)
            {
                DebugX.Logger(LogChannels.DevTools).Info(
                    "PersistentObjects: '{Key}' already has a session survivor; destroying the copy from scene '{Scene}'.",
                    key, duplicate.scene.name);
                return;
            }

            DebugX.Logger(LogChannels.Engine).Error(
                "[Core:ERROR:Bootstrap] Persistent object '{Key}' in scene '{DuplicateScene}' is destroyed as a duplicate of the " +
                "session survivor '{Survivor}', but it carries config the survivor does not — that config never runs this session:\n{Lost}",
                key, duplicate.scene.name, survivor.name, string.Join("\n", lost));
        }
    }
}
