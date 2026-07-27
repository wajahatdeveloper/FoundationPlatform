using System.Collections;
using AetherNexus.FoundationPlatform.DebugX;
using AetherNexus.FoundationPlatform.Messaging;
using UnityEngine;
using UnityEngine.InputSystem;

namespace AetherNexus.FoundationPlatform.Samples
{
    using CoroutineX = AetherNexus.FoundationPlatform.CoroutineX.CoroutineX;
    using DebugX = AetherNexus.FoundationPlatform.DebugX.DebugX;

    /// <summary>
    /// Sample event for the Foundation Platform EventBus + CoroutineX demo.
    /// </summary>
    public sealed class SamplePingEvent : BaseGameEvent
    {
        public string Message { get; }
        public int Step { get; }

        public SamplePingEvent(string message, int step)
        {
            Message = message;
            Step = step;
        }
    }

    /// <summary>
    /// Play Mode demo: CoroutineX-owned ladder publishes <see cref="SamplePingEvent"/>;
    /// EventBus subscriber logs via DebugX and nudges a marker transform.
    /// Controls: Space = manual ping, R = Rerun, S = Stop.
    /// </summary>
    public sealed class EventBusCoroutineSample : MonoBehaviour
    {
        [SerializeField]
        [Tooltip("Moved each coroutine step and on ping. Auto-creates a child cube if empty.")]
        private Transform marker;

        [SerializeField]
        [Tooltip("Seconds between CoroutineX demo steps.")]
        private float stepDelay = 0.45f;

        [SerializeField]
        private bool autoStartOnPlay = true;

        [SerializeField]
        private int demoSteps = 4;

        private CoroutineX _routine;
        private int _manualPingCount;

        private void OnEnable()
        {
            EventBus.Subscribe<SamplePingEvent>(OnPing);
        }

        private void OnDisable()
        {
            EventBus.Unsubscribe<SamplePingEvent>(OnPing);
            _routine?.Stop();
            _routine = null;
        }

        private void Start()
        {
            EnsureMarker();
            if (autoStartOnPlay)
                StartDemo();
        }

        private void Update()
        {
            Keyboard keyboard = Keyboard.current;
            if (keyboard == null)
                return;

            if (keyboard.spaceKey.wasPressedThisFrame)
                PublishManualPing();

            if (keyboard.rKey.wasPressedThisFrame)
                StartDemo();

            if (keyboard.sKey.wasPressedThisFrame)
            {
                _routine?.Stop();
                DebugX.Logger(LogChannels.DevTools).Info("[Sample] CoroutineX Stop()");
            }
        }

        private void OnGUI()
        {
            const float pad = 12f;
            GUILayout.BeginArea(new Rect(pad, pad, 420f, 140f), GUI.skin.box);
            GUILayout.Label("Foundation Platform — EventBus + CoroutineX");
            GUILayout.Label("Auto: coroutine publishes SamplePingEvent each step.");
            GUILayout.Label("Space = manual ping · R = Rerun · S = Stop");
            GUILayout.Label("Open Window → DebugX Console... to see logs.");
            GUILayout.EndArea();
        }

        /// <summary>Start or restart the owned CoroutineX demo ladder.</summary>
        public void StartDemo()
        {
            EnsureMarker();
            _routine?.Stop();
            _routine = CoroutineX.Run(this, DemoLadder());
            DebugX.Logger(LogChannels.DevTools).Info("[Sample] CoroutineX Run (owned by {Owner})", name);
        }

        private IEnumerator DemoLadder()
        {
            Vector3 origin = marker.position;

            for (int step = 1; step <= demoSteps; step++)
            {
                yield return new WaitForSeconds(stepDelay);

                EventBus.Publish(new SamplePingEvent($"CoroutineX step {step}/{demoSteps}", step));

                marker.position = origin + new Vector3(step * 0.75f, Mathf.Sin(step) * 0.25f, 0f);
            }

            EventBus.Publish(new SamplePingEvent("CoroutineX ladder complete", demoSteps + 1));
            DebugX.Logger(LogChannels.DevTools).Info("[Sample] CoroutineX completed — call StartDemo() or press R to Rerun");
        }

        private void PublishManualPing()
        {
            _manualPingCount++;
            EventBus.Publish(new SamplePingEvent($"Manual ping #{_manualPingCount} (Space)", -_manualPingCount));
        }

        private void OnPing(SamplePingEvent evt)
        {
            DebugX.Logger(LogChannels.DevTools).Info(
                "[Sample] EventBus received step={Step} message={Message}",
                evt.Step,
                evt.Message);

            if (marker != null && evt.Step < 0)
                marker.Rotate(0f, 25f, 0f, Space.Self);
        }

        private void EnsureMarker()
        {
            if (marker != null)
                return;

            Transform existing = transform.Find("Marker");
            if (existing != null)
            {
                marker = existing;
                return;
            }

            GameObject cube = GameObject.CreatePrimitive(PrimitiveType.Cube);
            cube.name = "Marker";
            cube.transform.SetParent(transform, false);
            cube.transform.localPosition = Vector3.zero;
            cube.transform.localScale = Vector3.one * 0.6f;
            marker = cube.transform;
        }
    }
}
