# EventBus + CoroutineX demo

Play Mode sample for Foundation Platform: owned CoroutineX steps publish `SamplePingEvent`; EventBus + DebugX handle the results.

## Requires

- Unity **6000.3.10f1+** (URP recommended)
- **Input System** active (Active Input Handling = Input System Package or Both)
- Domain Reload **enabled**
- Do not install a second UniTask package

## How to run

1. Package Manager → Foundation Platform → Samples → **Import** “EventBus + CoroutineX”
2. Open `EventBusCoroutineDemo.unity`
3. Enter Play Mode
4. Optional: **Window → DebugX Console...**

## What it shows

| Feature | Behavior |
|---------|----------|
| **CoroutineX** | `CoroutineX.Run(this, DemoLadder())` with `WaitForSeconds` |
| **EventBus** | Publish / subscribe `SamplePingEvent` (clean unsubscribe on disable) |
| **DebugX** | Logs on channel `DevTools` |

## Controls

| Input | Action |
|-------|--------|
| *(auto)* | Ladder starts on Play when Auto Start On Play is enabled |
| **Space** | Manual ping (marker rotates) |
| **R** | Rerun ladder |
| **S** | `CoroutineX.Stop()` |

## Files

- `EventBusCoroutineSample.cs` — event type + demo behaviour
- `EventBusCoroutineDemo.unity` — camera, light, sample host
