using System;
using AetherNexus.FoundationPlatform.CoroutineX;
using AetherNexus.FoundationPlatform.Logging;
using AetherNexus.FoundationPlatform.TweenX;
using UnityEngine;
using UnityEngine.LowLevel;
using UnityEngine.PlayerLoop;

namespace AetherNexus.FoundationPlatform
{
    /// <summary>
    /// The one hidden application-lifetime object FoundationPlatform creates. Runs unowned
    /// <see cref="CoroutineX.CoroutineX"/> routines and raises focus / pause / quit for static services.
    /// Per-frame service work (presentation tweens, main-thread log actions) runs from a player-loop
    /// entry after <c>ScriptRunBehaviourUpdate</c>, not from a MonoBehaviour <c>Update</c>.
    /// </summary>
    [AddComponentMenu("")]
    internal sealed class PlatformHost : MonoBehaviour
    {
        private const string PersistenceKey = "FoundationPlatform.Host";

        internal static PlatformHost Instance { get; private set; }
        internal static bool HasInstance => Instance != null;

        internal CoroutineXOwner Owner { get; private set; }

        // Static events survive Stop->Play without a domain reload; subscribers unsubscribe before
        // re-subscribing so they are never registered twice.
        internal static event Action<bool> FocusChanged;
        internal static event Action<bool> PauseChanged;

        /// <summary>Raised once from <c>OnApplicationQuit</c>, after pending main-thread log actions ran.</summary>
        internal static event Action Quitting;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void InstallPlayerLoop()
        {
            var root = PlayerLoop.GetCurrentPlayerLoop();
            RemoveTick(ref root);
            if (!TryInsertAfter(ref root, typeof(Update.ScriptRunBehaviourUpdate)))
            {
                throw new InvalidOperationException(
                    "[FoundationPlatform] PlatformHost could not find Update.ScriptRunBehaviourUpdate in the player loop.");
            }
            PlayerLoop.SetPlayerLoop(root);
        }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        private static void CreateInstance()
        {
            if (Instance != null)
            {
                return;
            }

            var go = new GameObject("[FoundationPlatform]") { hideFlags = HideFlags.HideInHierarchy };
            Instance = go.AddComponent<PlatformHost>();
            Instance.Owner = go.AddComponent<CoroutineXOwner>();
            PersistentObjects.Register(go, PersistenceScope.Application, PersistenceKey);
        }

        private void OnDestroy()
        {
            if (Instance == this)
            {
                Instance = null;
            }
        }

        private void OnApplicationFocus(bool hasFocus) => FocusChanged?.Invoke(hasFocus);

        private void OnApplicationPause(bool isPaused) => PauseChanged?.Invoke(isPaused);

        private void OnApplicationQuit()
        {
            LogQueue.ProcessMainThreadActions();
            Quitting?.Invoke();
        }

        private static void Tick()
        {
            if (!Application.isPlaying)
            {
                return;
            }

            TweenManager.TickPresentation();
            LogQueue.ProcessMainThreadActions();
        }

        private static PlayerLoopSystem CreateTickSystem() => new()
        {
            type = typeof(PlatformHost),
            updateDelegate = Tick,
        };

        private static bool TryInsertAfter(ref PlayerLoopSystem system, Type afterType)
        {
            var list = system.subSystemList;
            if (list == null)
            {
                return false;
            }

            for (int i = 0; i < list.Length; i++)
            {
                if (list[i].type == afterType)
                {
                    var newList = new PlayerLoopSystem[list.Length + 1];
                    Array.Copy(list, 0, newList, 0, i + 1);
                    newList[i + 1] = CreateTickSystem();
                    Array.Copy(list, i + 1, newList, i + 2, list.Length - i - 1);
                    system.subSystemList = newList;
                    return true;
                }

                if (TryInsertAfter(ref list[i], afterType))
                {
                    return true;
                }
            }

            return false;
        }

        // Without a domain reload the previous play session's entry is still in the current loop.
        private static void RemoveTick(ref PlayerLoopSystem system)
        {
            var list = system.subSystemList;
            if (list == null)
            {
                return;
            }

            int index = Array.FindIndex(list, s => s.type == typeof(PlatformHost));
            if (index >= 0)
            {
                var newList = new PlayerLoopSystem[list.Length - 1];
                Array.Copy(list, 0, newList, 0, index);
                Array.Copy(list, index + 1, newList, index, list.Length - index - 1);
                system.subSystemList = newList;
                list = newList;
            }

            for (int i = 0; i < list.Length; i++)
            {
                RemoveTick(ref list[i]);
            }
        }
    }
}
