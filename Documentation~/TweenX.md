# TweenX

First-party tween and Feedback system in Foundation Platform. DOTween-style ergonomics; no DOTween dependency. Lives alongside CoroutineX under `Runtime/TweenX` and `Editor/TweenX`.

Namespace: `AetherNexus.FoundationPlatform.TweenX` (Feedbacks under `.Feedbacks`). Extension methods such as `TweenMove` are **global** (no `using` required).

Related: [Architecture](ARCHITECTURE.md) · Debugger **Window → TweenX → Tween Debugger**

## Quick start

```csharp
// Move, ease, loop forever ping-pong
transform.TweenMove(target, 1f)
         .SetEase(Ease.OutBack)
         .SetLoops(-1, LoopType.Yoyo);

// Fade a UI group out, then disable it
canvasGroup.TweenFade(0f, 0.25f)
           .OnComplete(() => canvasGroup.gameObject.SetActive(false));

// Tween any float with a getter/setter
TweenValue.Float(() => score, v => score = v, 100f, 0.5f);
```

Every call returns a `TweenHandle` — a safe value-type reference. Store it to `.Pause()`, `.Play()`, `.Complete()`, `.Rewind()`, or `.Kill()`. Calls through a finished handle are no-ops (generation-guarded).

### Available targets

`Transform` (Move/MoveX/Y/Z, LocalMove, Scale, Rotate, RotateQuaternion, LocalRotate) ·
`RectTransform` (AnchorPos, SizeDelta) · `CanvasGroup` (Fade) · `Graphic` (Color, Fade) ·
`SpriteRenderer` (Color, Fade) · `Material` (Color, Float, color/float by property id) ·
`Light` (Intensity, Color) · `AudioSource` (Volume, Pitch) · `Camera` (FOV, OrthoSize) ·
`TweenValue.Float/Vector3/Color` (generic).

### Fluent setters

`SetEase(Ease | AnimationCurve)` · `SetLoops(count, LoopType)` · `SetDelay(s)` · `SetClock(clock)` ·
`SetTimeScale(x)` · `SetSnapping()` · `SetRelative()` · `From()` · `SetId(int)` · `SetLink(go)` ·
`OnStart/OnUpdate/OnStepComplete/OnComplete/OnKill(Action)`.

### Loop types

`Restart` · `Yoyo` · `Incremental` (numeric types; otherwise falls back to Restart).

## Clocks and timescale

Every tween runs on one of three clocks (`SetClock`, default **Scaled**):

| Clock | Time source | Pauses with game? | Use for |
|-------|-------------|-------------------|---------|
| `Unscaled` | `Time.unscaledDeltaTime` | No | UI, menus, pause screens |
| `Scaled` | `Time.deltaTime` | Yes (`Time.timeScale`) | Gameplay visuals |
| `Deterministic` | Fixed simulation step (when registered) | With sim | Reproducible motion |

- **Global controls:** `TweenManager.GlobalTimeScale`, `TweenManager.PauseAll` / `ResumeAll` affect presentation clocks only; deterministic tweens are untouched so replays stay consistent.
- Ease functions are pure functions of normalized time.

### Ecosystem integration (optional)

Foundation Platform cannot reference GameEngineCore. The **Deterministic** clock is empty until a higher-level product registers a bridge (for example GameEngineCore’s simulation clock bridge) and pumps `TweenManager.TickDeterministic()` each sim step. Requesting `Deterministic` before a bridge exists falls back to **Scaled** with a one-time warning. Standalone projects can ignore this clock entirely.

Tween pooling mirrors typical pooled gameplay allocators used in AetherNexus products; tick loops avoid LINQ/closures for per-frame work.

## Allocation contract

- **Zero allocation per frame** during ticking: pooled tween objects, cached interpolators, `switch`-based eases.
- Creating a tween may allocate getter/setter closures once (similar to DOTween); the tween object is reused from the pool on kill/complete.

## Designer component — Tween Animator

Add **Foundation Platform ▸ Tween Animator** to a GameObject. Author steps without code:

- **Property** — Move / LocalMove / Scale / Rotate / AnchorPos / Fade / Color
- Per step: destination, duration, delay, ease, loops, `From`, **Join Previous** (parallel)
- Playback: `Play On Enable`, clock, snapping; **Play** / **Stop** in the inspector
- Scene View: positional steps show a movable destination handle

## Debugging

- **Scene View:** select a GameObject with live tweens in Play Mode — Entity Debugger overlay can show a **Tweens** block (`IEntityDebugSection`).
- **Window → TweenX → Tween Debugger** — every live tween with Pause/Kill and global controls.

## Bulk control

```csharp
TweenManager.KillAll();
TweenManager.KillById(42);
TweenManager.KillTweensOf(myTransform);
```

Tweens auto-kill when their target is destroyed, or (with `.SetLink(go)`) when a linked GameObject is destroyed/disabled.

## Sequences

```csharp
Sequence.Create()
    .Append(transform.TweenMove(a, 1f).SetEase(Ease.OutQuad))
    .Join(transform.TweenScale(1.5f, 1f))
    .AppendInterval(0.25f)
    .AppendCallback(() => Fire())
    .SetLoops(-1)
    .Play();
```

`Append` / `Join` / `Insert` / `AppendInterval` / `AppendCallback` / `InsertCallback`. Child delay/loops/clock are ignored (use sequence APIs). Sequence loops are Restart-only; nesting sequences is not supported.

## Path tweens

```csharp
transform.TweenPath(new[]{ p1, p2, p3 }, 2f, PathType.CatmullRom);
```

Current position is the implicit start. `PathType.Linear` or `CatmullRom`; `local: true` for local space.

## Juice

```csharp
transform.TweenPunchScale(Vector3.one * 0.2f, 0.3f);
transform.TweenShakePosition(0.3f, 0.5f);
image.TweenFlash(Color.red, 0.2f, flashes: 2);
canvasGroup.TweenBlink(0.2f, 0.4f, blinks: 3);
```

Punch/shake return to the captured origin. With a fixed seed, shake is reproducible on the deterministic clock when that clock is registered.

## Feedback player

Add **Foundation Platform ▸ Feedback Player**, then build composable feedbacks (no code):

- Built-ins: Move, ScalePunch, PunchRotation, ShakePosition, Flash, Fade, Audio, CameraShake, TimeFreeze, Event (UnityEvent)
- **Add Feedback** lists every concrete `Feedback` subclass (TypeCache)
- `Play()` / `Stop()`; optional Play On Enable

```csharp
GetComponent<FeedbackPlayer>().Play();
```

Feedbacks use the tween core (clocks, pooling, debugging).
