# TweenX — Architecture Audit

## Context

Scope: `Runtime/TweenX/` (Core, Components, Extensions, Feedbacks) and `Editor/TweenX/` in
`com.aethernexus.foundationplatform` — the package's presentation-layer tween/juice engine
(DOTween-style ergonomics, no DOTween dependency). 14 `.cs` files read in full:

- Core: `Ease.cs`, `JuiceTween.cs`, `PathTween.cs`, `Sequence.cs`, `Tween.cs`, `TweenClock.cs`,
  `TweenHandle.cs`, `TweenInterpolators.cs`, `TweenManager.cs`
- Components: `TweenAnimator.cs`
- Extensions: `TweenExtensions.Juice.cs`, `.Misc.cs`, `.Path.cs`, `.Rendering.cs`, `.Transform.cs`, `.UI.cs`
- Feedbacks: `Feedback.cs`, `FeedbackPlayer.cs`, `Feedbacks.cs`
- Editor: `FeedbackPlayerEditor.cs`, `TweenAnimatorEditor.cs`, `TweenDebugSection.cs`,
  `TweenDebuggerWindow.cs`, `TweenStepDrawer.cs`

Docs read: `docs/00-AgentGuide.md`, `docs/01-CorePrinciples.md`, `docs/02-Libraries.md`,
`docs/11-Determinism.md`, `docs/13-AuthoringStandards.md`, and the package's own
`Documentation~/ARCHITECTURE.md` + `Documentation~/TweenX.md`.

Confirmed: TweenX is **not** in docs/00 §4's vendored/generated table (only CoroutineX,
RagdollHelper, IndicatorPackage/OffScreenIndicator, and `Assets/AssetPacks/**` are listed there) —
`TweenX.md` line 3 independently calls it out as "First-party ... no DOTween dependency." Treated
as first-party, hand-editable code for this audit, not a vendored blob.

## Findings

### Execution Spine

No findings of an actual gameplay-fact write inside TweenX. Every extension method in this
package writes to a purely visual/transform/audio/camera channel: `Transform` position/rotation/
scale (`TweenExtensions.Transform.cs`), `RectTransform`/`CanvasGroup`/`Graphic`
(`TweenExtensions.UI.cs`), `SpriteRenderer`/`Material`/`Light` (`TweenExtensions.Rendering.cs`),
`AudioSource`/`Camera` (`TweenExtensions.Misc.cs`), and `JuiceTween`'s punch/shake channels
(`Core/JuiceTween.cs`) which only ever touch `Transform.position/localPosition/localScale/
localEulerAngles`.

- **Info** — `TweenValue.Float/Vector3/Color` (`Runtime/TweenX/Extensions/TweenExtensions.Misc.cs:41-51`)
  and the sibling `MiscTweenExtensions` methods are a generic escape hatch: they accept an
  arbitrary caller-supplied `Func<T> getter` / `Action<T> setter`. Nothing in TweenX itself stops
  a caller from wiring the setter directly into a simulation-owned field instead of a presentation
  mirror of one — the same category of risk docs/12 calls out for Animator/IK output devices.
  Repo-wide grep found exactly one current caller:
  `Packages/com.aethernexus.gameplayabilitysystem/GameplayAttributeSystem/AttributeBarView.cs:106`,
  which tweens a UI `Slider.value` against itself (getter and setter both hit the same UI
  control) — presentation-only, no violation today. Flagging as a structural watch-item: the API
  surface provides no guardrail, so this is worth re-checking whenever a new caller adopts
  `TweenValue.*` (see Cross-references).

### Data/Controller/View Boundary

No findings. TweenX has no Data-layer definitions and no Controller/pipeline coupling — it is
pure View-layer output plumbing (`Tween`/`TweenHandle`/`TweenManager` are runtime-only pooled
presentation state, not domain records per docs/06). `TweenAnimator` and `FeedbackPlayer` are
authored/read entirely through the Inspector and never reach into simulation types; neither
package references any domain-manager or action-pipeline symbol (no such `using`/type appears
anywhere in the 14 files).

### Ownership

No findings — confirmed exactly one tweening utility in the project.

- Repo-wide grep for `DOTween|LeanTween|iTween` outside TweenX only matched
  `Packages/com.aethernexus.foundationplatform/Runtime/ThirdParty/UniTask/External/DOTween/
  DOTweenAsyncExtensions.cs` — a vendored UniTask compatibility shim for interop *if* a project
  also has real DOTween installed, not a competing engine. Not part of TweenX; noted only to
  preempt confusion in a future ownership pass (see Cross-references).
- The `TweenClock.Deterministic` seam is correctly one-directional: TweenX defines
  `ITweenClock`/`TweenManager.RegisterClock` (`Core/TweenClock.cs`, `Core/TweenManager.cs:78-81`)
  without referencing GameEngineCore, and
  `Packages/com.aethernexus.gameenginecore/Runtime/Core/Lifecycle/SimulationClockTweenBridge.cs`
  is the sole registrant (`TweenManager.RegisterClock(TweenClock.Deterministic, bridge)`), which
  matches the documented assembly-boundary pattern from docs/00 §3 ("FoundationPlatform cannot
  reference GameEngineCore.Runtime").

### Designer Surface Priority

No findings. `TweenAnimatorEditor.cs` and `FeedbackPlayerEditor.cs` are Inspector customizations
(reorderable polymorphic list, conditional fields) plus a Scene View overlay
(`TweenAnimatorEditor.OnSceneGUI`, movable destination handles) — priorities #3/#4 in
docs/13-AuthoringStandards. `TweenDebugSection.cs` plugs into the existing
`EntityDebuggerOverlay`/`IEntityDebugSection` (Hierarchy/Scene-selection-driven), priority #2. The
one true `EditorWindow` (`TweenDebuggerWindow.cs`) is explicitly a whole-scene live-tween list with
pause/kill and global controls — the one designer-surface-approved use of an EditorWindow per
docs/13 priority #5 ("bulk validation and debug/power tools only"). No bespoke EditorWindow is used
for anything designers author.

### Redundancy/Simplification

No findings. No dead code, no legacy/back-compat shims, and no unnecessary abstraction layers were
found: the `Tween` → `Tween<T>`/`JuiceTween`/`PathTween`/`Sequence` hierarchy exists specifically so
`TweenManager` can tick a heterogeneous pooled list without knowing the value type (documented
rationale in `Core/Tween.cs`'s class comment), and the Juice/Flash/Blink extension wrappers are
thin, non-duplicated compositions over the core color/fade tweens.

### Determinism

- **Error** — `FeedbackTimeFreeze` (`Runtime/TweenX/Feedbacks/Feedbacks.cs:150-164`) mutates the
  global `Time.timeScale` and schedules the restore through
  `FeedbackContext.Schedule(FreezeDuration, () => Time.timeScale = prev, unscaled: true)`
  (`Feedback.cs:36-45`), which tracks a zero-duration, delayed tween and fires the restore from
  its `OnComplete` callback. But `FeedbackContext.Stop()` (`Feedback.cs:48-52`) — called from
  `FeedbackPlayer.OnDisable()`/`Play()` (`FeedbackPlayer.cs:31,36,43`) — kills every tracked
  handle, and `TweenManager.RemoveAt` (`Core/TweenManager.cs:225-237`) only invokes `OnKillCb`,
  never `OnCompleteCb` (confirmed: `Kill`/`KillResolved`, `Core/TweenManager.cs:314-324`, route
  through `RemoveAt(idx, killed: true)`). **Failure scenario:** a `FeedbackTimeFreeze` fires (sets
  `Time.timeScale` to e.g. `0.1`), then the owning GameObject/`FeedbackPlayer` is disabled,
  pooled, or the scene unloads before `FreezeDuration` elapses (very plausible — hit-stop is
  typically triggered on a hit that can itself kill/despawn the actor) — the restore action is
  silently dropped and `Time.timeScale` is left stuck at the frozen value for the rest of the
  process, permanently slowing every "Scaled"-clock tween and Unity's physics fixed-step cadence
  project-wide. This is a global engine-state corruption bug caused by a presentation component,
  not a local visual glitch — worth Error severity even though it is not a direct simulation-fact
  write.
- **Warning** — Same root cause, a second failure mode: two overlapping `FeedbackTimeFreeze`
  calls (two feedbacks on one player, or two different actors hit-stopping concurrently) each
  independently capture/restore `Time.timeScale` with no ref-count or stack. Whichever restore
  fires last wins, and if it fires with a `prev` captured *while already frozen* by the other
  call, the game is left in the wrong timescale after both freezes should have ended (classic
  hit-stop-stacking bug).
- **Info** — `JuiceTween`'s class comment (`Core/JuiceTween.cs:10-12`) states "a shake with a
  fixed seed is reproducible... including on the deterministic clock," and `TweenX.md:124` repeats
  "With a fixed seed, shake is reproducible on the deterministic clock." Both are accurate only
  when the caller passes an explicit `seed >= 0`; the default (`seed = -1`) resolves through
  `JuiceTweenExtensions.ResolveSeed` (`Extensions/TweenExtensions.Juice.cs:73`) to a shared,
  call-order-dependent static counter `_shakeSeed` (line 13), which is not saved/restored and not
  reproducible across sessions or call order. Not an architecture violation (shake is
  presentation-only, so non-determinism here is explicitly allowed by docs/01), but the two docs
  make a reproducibility claim that only holds for the non-default path — a footgun for anyone
  wiring a "reproducible" shake onto `TweenClock.Deterministic` without reading closely.
- Confirmed **no `UnityEngine.Random` usage anywhere in TweenX** (repo grep of
  `Runtime/TweenX` + `Editor/TweenX` returned zero hits). Shake jitter uses a deterministic
  sine-hash (`JuiceTween.NoiseSin`, `Core/JuiceTween.cs:84-88`) instead — correct, and stronger
  than needing `RandomX.PresentationRange`, since it needs no random-provider dependency at all.

### Doc/Architecture Drift

No findings. `Documentation~/TweenX.md` and the `TweenX` row in `Documentation~/ARCHITECTURE.md`
(lines 5, 7, 17, 27, 223) accurately describe the current namespaces, clock table, fluent-setter
list, Sequence/Path/Juice APIs, and the Feedback catalogue — every `Feedback*` type documented in
`TweenX.md:130` (Move, ScalePunch, PunchRotation, ShakePosition, Flash, Fade, Audio, CameraShake,
TimeFreeze, Event) matches a real class in `Feedbacks.cs`, and the "Ecosystem integration
(optional)" section correctly describes the `SimulationClockTweenBridge` seam verified above under
Ownership.

### Codebase Gotchas

- **Warning** — Pervasive use of optional parameters across TweenX's public API, which
  contradicts the project's stated "No optional parameters" rule (`.cursor/AGENTS.md:37`,
  docs/00 §2 and the pre-flight checklist). This is a deliberate, consistently-applied DOTween-style
  ergonomic choice (documented as such throughout `TweenX.md`), not an accidental slip, but it is a
  direct, wide textual violation of the rule as written. Representative instances:
  - `TweenPunchPosition/LocalPosition/Scale/Rotation(..., float vibrato = 10f)` and
    `TweenShakePosition/Rotation/Scale(..., float vibrato = 10f, int seed = -1)`
    (`Extensions/TweenExtensions.Juice.cs:17-41`)
  - `TweenFlash(..., int flashes = 1)` / `TweenBlink(..., int blinks = 1)`
    (`Extensions/TweenExtensions.Juice.cs:45-60`)
  - `TweenValue.Float/Vector3/Color(..., UnityEngine.Object link = null)`
    (`Extensions/TweenExtensions.Misc.cs:41-51`)
  - `TweenHandle.SetLoops(int count, LoopType type = LoopType.Restart)`,
    `SetSnapping(bool snapping = true)`, `SetRelative(bool relative = true)`
    (`Core/TweenHandle.cs:47,51-52`)
  - `TweenPath(..., PathType type = PathType.CatmullRom, bool local = false)`
    (`Extensions/TweenExtensions.Path.cs:13-14`)
- No findings for `??` on `UnityEngine.Object`. The only two `??` usages in scope —
  `_waypoints?.Length ?? 0` (`Core/PathTween.cs:44`) and `Resolve(h)?.Progress ?? 0f`
  (`Core/TweenManager.cs:249`) — operate on a plain `Vector3[]` and the plain C# class `Tween`
  respectively; neither derives from `UnityEngine.Object`.
- No findings for `OnValidate`/`OnAfterDeserialize`/`ISerializationCallbackReceiver`. None exist
  anywhere in the 14 files (confirmed by grep); `TweenAnimator`/`FeedbackPlayer` resolve
  everything at runtime (`OnEnable`/`Play()`) or leave it to manual Inspector authoring, so the
  docs/00 §3 "unconditional serialized write dirties the scene" trap does not apply here.
- No findings for struct-initializer / constructor-field rules. The only custom struct is the
  `readonly struct TweenHandle` (`Core/TweenHandle.cs:26`), which has exactly one explicit
  constructor assigning both fields (`Id`, `Generation`) and no instance field initializers —
  compliant with the C# 9 struct rules from docs/00 §3.
- No findings for the `Debug` namespace collision trap. TweenX's namespace is
  `AetherNexus.FoundationPlatform.TweenX` (not `GameEngineCore.*`), so the plain `Debug.LogWarning`
  calls (`Core/TweenManager.cs:265`, `Core/Sequence.cs:57`) correctly resolve to
  `UnityEngine.Debug` via the file's `using UnityEngine;` — no ambiguity risk in this namespace.

## Fixes

None applied — this audit is read-only per instructions (only `AUDIT.md` was written). Recommended
follow-ups, in priority order:

1. **`FeedbackTimeFreeze` restore-loss (Error).** Make the `Time.timeScale` restore
   unconditional, not contingent on the scheduling tween surviving to `OnComplete`. Options: (a)
   give `FeedbackContext` a small "must-run on stop" callback list that `Stop()` invokes before
   killing tracked handles, so a pending freeze always restores; or (b) track outstanding freezes
   with a static ref-count/stack scoped to `FeedbackTimeFreeze` so `Stop()`/domain-shutdown
   unconditionally restores the pre-freeze value, and nested/overlapping freezes compose instead
   of stomping each other.
2. **Optional parameters (Warning).** Either record an explicit, documented exception for TweenX's
   fluent API against the "No optional parameters" rule (one line at the top of `TweenX.md`,
   mirroring how `AGENTS.md` documents other rule scopes), or convert the defaulted parameters into
   explicit overloads (e.g. a `TweenShakePosition(strength, duration)` overload that forwards to
   the full-parameter version with `vibrato: 10f, seed: -1`).
3. **Shake-determinism doc footgun (Info).** Tighten `JuiceTween`'s class comment and
   `TweenX.md:124` to state plainly that reproducibility on `TweenClock.Deterministic` requires an
   explicit `seed >= 0`; the auto-seed default is not reproducible across sessions or call order.
4. **Generic escape hatch (Info).** Add a one-line caveat to `TweenValue`'s XML summary
   (`Extensions/TweenExtensions.Misc.cs:33-37`) warning that getter/setter pairs must target
   presentation-only state, mirroring the Animator/IK guidance in docs/12 — no code change needed,
   just closes the documentation gap for future callers.

## Cross-references

- `Packages/com.aethernexus.gameenginecore/Runtime/Core/Lifecycle/SimulationClockTweenBridge.cs`
  — the `TweenClock.Deterministic` registrant; out of this audit's package scope but directly
  relevant to TweenX's clock seam (see Ownership).
- `Packages/com.aethernexus.gameplayabilitysystem/GameplayAttributeSystem/AttributeBarView.cs:106`
  — the sole current caller of the generic `TweenValue.Float` escape hatch (see Execution Spine);
  confirmed presentation-only today, recommend re-checking during the GameplayAbilitySystem
  package audit as that file evolves.
- `Packages/com.aethernexus.foundationplatform/Runtime/ThirdParty/UniTask/External/DOTween/
  DOTweenAsyncExtensions.cs` — vendored UniTask/DOTween interop shim, unrelated to TweenX; noted
  only to preempt a false "second tweening engine" flag in a broader package audit.
