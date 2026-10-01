using System;
using AetherNexus.FoundationPlatform.CoroutineX;
using AetherNexus.FoundationPlatform.Logging;
using AetherNexus.FoundationPlatform.TweenX;
using UnityEngine;
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

        // Cleared at SubsystemRegistration; subscribers re-subscribe at BeforeSceneLoad or later.
        internal static event Action<bool> FocusChanged;
        internal static event Action<bool> PauseChanged;

        /// <summary>Raised once from <c>OnApplicationQuit</c>, after pending main-thread log actions ran.</summary>
        internal static event Action Quitting;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetEvents()
        {
            FocusChanged = null;
            PauseChanged = null;
            Quitting = null;
        }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void InstallPlayerLoop()
        {
            PlayerLoopInstaller.InsertAfter(typeof(PlatformHost), typeof(Update.ScriptRunBehaviourUpdate), Tick);
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
    }
}
