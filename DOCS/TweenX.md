# TweenX — In-House Tween System

A first-party tween/animation engine for HOMAM. DOTween-style ergonomics, no third-party
dependency. Lives in `FoundationPlatform` (Runtime/TweenX + Editor/TweenX) alongside `CoroutineX`.

> **Status:** All three phases landed — Phase 1 (core + fluent API + designer component + scene
> debugging), Phase 2 (sequences, path tweens, juice primitives), Phase 3 (FEEL-style feedback player).

---

## Quick start (code)

Extension methods live in the **global namespace** (like `MathX`/`CoroutineX`) — no `using` needed:

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

Every call returns a `TweenHandle` — a safe value-type reference. Store it to control the tween
later (`.Pause()`, `.Play()`, `.Complete()`, `.Rewind()`, `.Kill()`). Calls through a handle whose
tween has finished are harmless no-ops (generation-guarded — no use-after-free).

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
`Restart` (jump to start), `Yoyo` (ping-pong), `Incremental` (each loop continues from the last;
numeric types only, falls back to Restart otherwise).

---

## Clocks, timescale & determinism

Every tween runs on one of three clocks (`SetClock`, default **Scaled**):

| Clock | Time source | Pauses with game? | Use for |
|-------|-------------|-------------------|---------|
| `Unscaled` | `Time.unscaledDeltaTime` | No | UI, menus, pause screens |
| `Scaled` | `Time.deltaTime` | Yes (via `Time.timeScale`) | Gameplay-facing visuals |
| `Deterministic` | `SimulationClock.DeltaTime` (fixed step) | Yes (with sim) | Reproducible, gameplay-affecting motion |

- **Global controls:** `TweenManager.GlobalTimeScale`, `TweenManager.PauseAll/ResumeAll` — affect
  presentation clocks only; deterministic tweens are untouched so replays stay bit-identical.
- **Determinism seam:** `FoundationPlatform` is a leaf assembly and can't see `SimulationClock`.
  The `Deterministic` slot is empty until `GameEngineCore.SimulationClockTweenBridge` registers it
  at play start (`AfterSceneLoad`) and pumps `TweenManager.TickDeterministic()` once per sim step.
  Request `Deterministic` before the bridge exists → transparent fallback to `Scaled` with a
  one-time warning.
- Ease functions are pure functions of normalized time, so deterministic tweens produce identical
  values across runs given the same sim ticks.

---

## Allocation contract

- **Zero allocation per frame** during ticking: pooled tween objects (per value type, mirroring
  GameEngineCore's `ActionPool<T>`), cached `static readonly` interpolators, no LINQ/closures in the
  tick loop, `switch`-based ease evaluation.
- Creating a tween may allocate its getter/setter closures the first time (same as DOTween); the
  tween object itself is reused from the pool on kill/complete.

---

## Designer component — `TweenAnimator`

Add **FoundationPlatform ▸ Tween Animator** to a GameObject. Author a list of steps (no code):

- **Property** — Move / LocalMove / Scale / Rotate / AnchorPos / Fade / Color (target components
  auto-resolved from the GameObject).
- Per step: destination, duration, delay, ease (enum or custom curve), loops + loop type, `From`,
  and **Join Previous** (start together with the prior step → parallel; otherwise sequential).
- Playback: `Play On Enable`, clock, snapping, plus **Play**/**Stop** buttons in the inspector.
- **Scene View:** positional steps show a movable destination handle with a dashed line from the
  transform — drag to author motion targets visually.

The inspector renders through the parity engine (FrameworkInspector), so conditionals and grouping
just work; nested steps use a 3-line `FrameworkReflectedDrawer`.

---

## Debugging

- **In-context (Scene View):** select a GameObject with live tweens in Play mode → the
  *Entity Debugger* overlay stacks a **Tweens** block (progress bars, clock, loop state, Pause/Kill
  per tween). Auto-discovered via `IEntityDebugSection` — no registration.
- **Whole-scene window:** **Window ▸ TweenX ▸ Tween Debugger** lists every live tween with progress,
  clock, loop/time readouts, per-row Pause/Kill, and global Pause-All / Kill-All.

---

## Bulk control

```csharp
TweenManager.KillAll();
TweenManager.KillById(42);              // tweens tagged via .SetId(42)
TweenManager.KillTweensOf(myTransform); // all tweens targeting an object
```

Tweens auto-kill when their target `UnityEngine.Object` is destroyed, or (with `.SetLink(go)`) when
a linked GameObject is destroyed/disabled.

---

## Sequences (Phase 2)

```csharp
Sequence.Create()
    .Append(transform.TweenMove(a, 1f).SetEase(Ease.OutQuad))
    .Join(transform.TweenScale(1.5f, 1f))   // parallel with the move
    .AppendInterval(0.25f)
    .AppendCallback(() => Fire())
    .SetLoops(-1)
    .Play();
```

`Append` / `Join` / `Insert(atTime, …)` / `AppendInterval` / `AppendCallback` / `InsertCallback`.
Child tweens are adopted onto the sequence's playhead — their own delay/loops/clock are ignored
(use `AppendInterval` and the sequence's `SetLoops`/`SetClock`). Sequence loops are Restart-only;
nesting sequences isn't supported.

## Path tweens (Phase 2)

```csharp
transform.TweenPath(new[]{ p1, p2, p3 }, 2f, PathType.CatmullRom);
```
Current position is the implicit start point. `PathType.Linear` or `CatmullRom`; `local: true` for
local-space.

## Juice (Phase 2)

```csharp
transform.TweenPunchScale(Vector3.one * 0.2f, 0.3f);   // squash-punch, returns to origin
transform.TweenShakePosition(0.3f, 0.5f);              // decaying jitter (pass seed for reproducible)
image.TweenFlash(Color.red, 0.2f, flashes: 2);         // color pulse
canvasGroup.TweenBlink(0.2f, 0.4f, blinks: 3);         // alpha pulse
```
Punch/shake resolve exactly back to the captured origin. Shake oscillation is a pure function of
time (no RNG state) → with a fixed seed it's reproducible, deterministic-clock included.

## FEEL feedback player (Phase 3)

Add **FoundationPlatform ▸ Feedback Player**, then build a burst of composable feedbacks (no code):

- Built-ins: Move, ScalePunch, PunchRotation, ShakePosition, Flash, Fade, Audio, CameraShake,
  TimeFreeze (hit-stop), Event (UnityEvent). Each has an `Active` toggle and a `Delay`.
- The **Add Feedback ▾** dropdown auto-lists every concrete `Feedback` subclass (TypeCache) — writing
  a new one (`[Serializable] class FeedbackX : Feedback { protected override void Execute(...) }`) makes
  it appear with no registration.
- `Play()` fires all active feedbacks together (each respecting its delay); `Stop()` cancels the burst.
  Trigger from code, a UnityEvent, or `Play On Enable`.

```csharp
GetComponent<FeedbackPlayer>().Play();   // e.g. on hit: shake + flash + punch + hit-stop + sfx
```

Feedbacks are built entirely on the tween core, so they inherit its clocks, pooling, and debugging.
