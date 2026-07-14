# Sample scenes (Foundation Platform)

## EventBus + CoroutineX

Package Manager sample path: `Samples~/EventBusCoroutineDemo`

Owned **CoroutineX** ladder publishes **SamplePingEvent**; **EventBus** subscriber logs with **DebugX** and moves a marker cube.

| | |
|--|--|
| Scene | `EventBusCoroutineDemo.unity` |
| Script | `EventBusCoroutineSample.cs` |
| Controls | Space = manual ping · R = Rerun · S = Stop |

## Setup

- **Input System:** Project Settings → Player → Active Input Handling = Input System Package **or** Both
- **uGUI:** present via `com.unity.ugui` (typical URP templates)
- **Domain Reload:** leave **enabled** (Fast Enter Play Mode is not supported)
- **URP** recommended
- Do **not** install a second UniTask package

## How to import

Package Manager → Foundation Platform → Samples → **Import** “EventBus + CoroutineX” → open the sample scene → Play.

Optional: **Window → DebugX Console...**

## Related

- [Sample README](Samples~/EventBusCoroutineDemo/README.md)
- [Documentation index](Documentation~/index.md)
