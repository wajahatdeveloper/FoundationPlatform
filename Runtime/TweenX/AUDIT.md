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

- **Error (resolved — removed)** — `FeedbackTimeFreeze` previously mutated global `Time.timeScale`
  from a presentation feedback; restore could be dropped on `Stop()`/`OnDisable`, and overlapping
  freezes could stomp each other. **Fix applied:** class deleted. Timescale / hit-stop belongs in
  GameEngineCore (GameManager), not Foundation TweenX. Related `FeedbackContext` must-run cleanup
  APIs (only consumer) also removed.
- **Info (resolved — docs)** — `JuiceTween` / `TweenX.md` now state plainly that reproducibility on
  `TweenClock.Deterministic` requires explicit `seed >= 0`; default auto-seed is not session/
  call-order stable.
- Confirmed **no `UnityEngine.Random` usage anywhere in TweenX** (repo grep of
  `Runtime/TweenX` + `Editor/TweenX` returned zero hits). Shake jitter uses a deterministic
  sine-hash (`JuiceTween.NoiseSin`, `Core/JuiceTween.cs:84-88`) instead — correct, and stronger
  than needing `RandomX.PresentationRange`, since it needs no random-provider dependency at all.

### Doc/Architecture Drift

No findings. `Documentation~/TweenX.md` and the `TweenX` row in `Documentation~/ARCHITECTURE.md`
(lines 5, 7, 17, 27, 223) accurately describe the current namespaces, clock table, fluent-setter
list, Sequence/Path/Juice APIs, and the Feedback catalogue — every `Feedback*` type documented in
`TweenX.md` Feedback section (Move, ScalePunch, PunchRotation, ShakePosition, Flash, Fade, Audio,
CameraShake, Event) matches a real class in `Feedbacks.cs`, and the "Ecosystem integration
(optional)" section correctly describes the `SimulationClockTweenBridge` seam verified above under
Ownership. (`TimeFreeze` removed from catalogue and code.)

### Codebase Gotchas

- **Warning (resolved — documented exception)** — Pervasive use of optional parameters across
  TweenX's public API. Documented as an approved exception at the top of `Documentation~/TweenX.md`
  (DOTween-style fluent surface; overload conversion declined). Representative instances remain:
  - `TweenPunchPosition/LocalPosition/Scale/Rotation(..., float vibrato = 10f)` and
    `TweenShakePosition/Rotation/Scale(..., float vibrato = 10f, int seed = -1)`
    (`Extensions/TweenExtensions.Juice.cs:17-41`)
  - `TweenFlash(..., int flashes = 1)` / `TweenBlink(..., int blinks = 1)`
    (`Extensions/TweenExtensions.Juice.cs:45-60`)
  - `TweenValue.Float/Vector3/Color(..., UnityEngine.Object link = null)`
    (`Extensions/TweenExtensions.Misc.cs`)
  - `TweenHandle.SetLoops(int count, LoopType type = LoopType.Restart)`,
    `SetSnapping(bool snapping = true)`, `SetRelative(bool relative = true)`
    (`Core/TweenHandle.cs:47,51-52`)
  - `TweenPath(..., PathType type = PathType.CatmullRom, bool local = false)`
    (`Extensions/TweenExtensions.Path.cs:13-14`)
- **Info (resolved — docs)** — `TweenValue` class summary now warns getter/setter pairs must target
  presentation-only state.
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

All follow-ups applied:

1. **`FeedbackTimeFreeze` (Error) — removed.** Class deleted; timescale ownership stays in
   GameEngineCore GameManager. `FeedbackContext` cleanup APIs that only served TimeFreeze removed.
2. **Optional parameters (Warning) — documented exception** at top of `TweenX.md` (kept; no overload conversion).
3. **Shake-determinism doc footgun (Info) — tightened** in `JuiceTween` class comment and `TweenX.md` Juice section.
4. **Generic escape hatch (Info) — caveat** added to `TweenValue` class summary.

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
