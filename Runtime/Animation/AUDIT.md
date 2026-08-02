# Animation (Runtime + AnimGraph) — Architecture Audit

## Context

Scope audited (every `.cs` file read in full, 23 files):

- `Packages/com.aethernexus.foundationplatform/Runtime/Animation/` (incl. `AnimGraph/` subfolder) — 19 files
- `Packages/com.aethernexus.foundationplatform/Editor/Animation/` — 4 files
- `Packages/com.aethernexus.foundationplatform/Editor/AnimGraph/` — 5 files

This is FoundationPlatform's "animation graph" library (per `docs/02-Libraries.md`): a self-contained,
Animancer-style Playables graph (`PlayableGraphBridge`, `PlayableLayer`, `ClipState`/`ControllerState`/`MixerState`
family, `PlayableStateEvents`) driven through a data layer of `AnimationSet` ScriptableObjects
(`AnimationSetEntry`, `AnimationSetLink`, `LocomotionBlendProfile`/`LocomotionBlendStanceDefinition`), wrapped by an
abstract `AnimatorBridgeBase : MonoBehaviour` that concrete character bridges (outside this package, e.g.
`GameFramework/CharacterSystem/.../CharacterAnimator.cs`) subclass. Editor support is a code generator
(`AnimationSetCodeGenerator`), a validator (`AnimationSetValidator`), custom inspectors/drawers
(`AnimationSetEditor`, `AnimationSetEntryPropertyDrawer`, `AnimationSetLinkPropertyDrawer`,
`PlayableGraphBridgeEditor`), a bespoke `AnimationTestBenchWindow`, and a second, apparently legacy generator
(`AnimatorConstantsGenerator`) for Mecanim `AnimatorController` param/state hashes.

Read for this audit: `docs/00-AgentGuide.md` §3, `docs/01-CorePrinciples.md`, `docs/02-Libraries.md`,
`docs/12-ConsumptionContracts.md`, `docs/11-Determinism.md`, and
`Packages/com.aethernexus.foundationplatform/Documentation~/ARCHITECTURE.md`.

## Findings

### Consumption Contract / Animator Boundary

**No findings within scope.** `AnimatorBridgeBase` never calls `Animator.GetCurrentAnimatorStateInfo`,
`GetAnimatorTransitionInfo`, or any other Mecanim state-polling API. Its `Animator` accessor
(`AnimatorBridgeBase.cs:53`) only exposes non-decisional passthrough properties (`Speed`, `ApplyRootMotion`,
`UpdateMode`, `CullingMode`, `RootPosition`, `RootRotation` — lines 85–135): gameplay-adjacent code sets values,
the Animator reacts, matching `docs/12-ConsumptionContracts.md`'s "Animator is output device" rule. All playback
decisions route through the custom `PlayableGraphBridge`/`PlayableLayer` graph, not through Mecanim state
machine queries.

- Info — `AnimatorBridgeBase.TryGetCurrentSetAndEntry` (`AnimatorBridgeBase.cs:570`) is a public API that reverse-looks-up
  the currently-playing clip's set/entry id by inspecting `layer.CurrentState`/`clipState.Weight`. This is a
  legitimate presentation/debug query (used by editor tooling), but because it is `public virtual` on a runtime
  base class, nothing stops a future caller from using it to *gate a gameplay decision* ("if current entry is X,
  do Y"), which would recreate the exact "poll animator state for gameplay" anti-pattern the consumption contract
  forbids — just against the custom graph instead of Mecanim. No misuse found in this scope; flagging as a watch
  item for callers.
- Info (cross-reference, **out of assigned scope**) — `Runtime/Extensions/Animations/AnimatorExtensions.cs:22,54,57,102,117`
  and `Runtime/Extensions/Animations/SyncAnimatorTime.cs:69,76` *do* call `Animator.GetCurrentAnimatorStateInfo`
  directly. These are generic Mecanim helper extensions, not part of the AnimGraph system audited here, but they
  sit in the same package and are exactly the shape of API the Consumption Contract warns about if a gameplay
  caller reaches for them to make a decision. Worth a follow-up look when `Runtime/Extensions/Animations/` is
  in scope for a future audit pass.

### Data/Controller/View Boundary

**No findings.** `AnimationSet`, `AnimationSetEntry`, `AnimationSetLink`, `LocomotionBlendProfile`,
`LocomotionBlendStanceDefinition`, `ClipTransitionData` are pure data (ScriptableObjects/serializable POCOs, no
rule logic beyond structural validation). `AnimatorBridgeBase`/`PlayableGraphBridge` are presentation controllers
— they never touch simulation state (no manager/action-pipeline references anywhere in the scope), consistent with
`docs/02-Libraries.md`'s "must not own authoritative simulation state" boundary for Libraries.

### Ownership

- **Warning** — Two animation-driving mechanisms coexist for the same concrete bridge classes. This package's
  `AnimatorBridgeBase` (`Runtime/Animation/AnimatorBridgeBase.cs`) + `PlayableGraphBridge`
  (`Runtime/Animation/PlayableGraphBridge.cs`) form a self-contained Playables graph that plays clips directly
  and does not use a Mecanim `AnimatorController` at all. Separately, `Editor/Animation/AnimatorConstantsGenerator.cs`
  (`GenerateConstants(AnimatorBridgeBase bridgeBase)`, lines 17–101) requires the same GameObject's `Animator` to
  carry a Mecanim `AnimatorController` (`animator.runtimeAnimatorController is AnimatorController`, line 31) and
  code-generates `ParamHashes`/`ParamNames`/`LayerHashes`/`LayerNames`/`StateHashes`/`StateNames` nested classes
  into the *concrete* bridge subclass via a marked source region
  (`// <auto-generated AnimatorConstants>` / `// </auto-generated AnimatorConstants>`).
  Confirmed in production: `Packages/com.aethernexus.gameframework/CharacterSystem/Runtime/CharacterAnimator/CharacterAnimator.cs:869`
  carries exactly that generated region — i.e. the shipping `CharacterAnimator` keeps a live Mecanim
  `AnimatorController` assigned *and* drives the Animancer-style Playables graph on top of it. `ARCHITECTURE.md`'s
  Animation section (see Doc/Architecture Drift below) describes only the Playables/AnimationSet system, not this
  second Mecanim-hash generator, so it's unclear whether `AnimatorConstantsGenerator` is (a) legacy tooling left
  over from a pre-AnimGraph implementation that should be retired, or (b) an intentionally-retained bridge for
  states/params some other system still reads. Either way this is worth a deliberate ownership decision rather
  than two systems quietly coexisting — recommend confirming with the team and either documenting the dual-system
  rationale or removing the generator + its generated regions.
- No findings otherwise — within `Runtime/Animation/` + `Editor/Animation/` + `Editor/AnimGraph/` there is exactly
  one `AnimationSet` implementation, one sequence utility (`AnimationSetSequenceUtility`), one graph bridge
  (`PlayableGraphBridge`), and one locomotion blend layer *interface* (`ILocomotionBlendLayer`, implemented outside
  this package in `com.aethernexus.gameframework/CharacterSystem/Runtime/Animation/LocomotionBlendLayer.cs` — a
  legitimate cross-package extension point, not a duplicate).

### Designer Surface Priority

**No findings — good compliance.** `AnimationSetEditor` (`CustomEditor(typeof(AnimationSet))`),
`PlayableGraphBridgeEditor` (`CustomEditor(typeof(PlayableGraphBridge))`), `AnimationSetEntryPropertyDrawer`, and
`AnimationSetLinkPropertyDrawer` all extend Inspector surfaces rather than inventing new windows, matching the
"prefer Project/Inspector/Scene-overlay" guidance. The one bespoke `EditorWindow`
(`Editor/AnimGraph/AnimationTestBenchWindow.cs`) is justified by its own doc comment (lines 14–24): it needs a
target-rig selection independent of any single asset's Inspector, drives `AnimationMode` clip sampling in edit
mode, and drives the live `PlayableGraphBridge` in play mode with scrubbing — none of which maps to a single
object's Inspector. This is a reasonable, narrowly-scoped use of an `EditorWindow`, not a competing/duplicate
surface.

### Redundancy/Simplification

- **Error** — Optional parameters, which this project explicitly forbids, appear repeatedly across the runtime
  public API surface:
  - `AnimatorBridgeBase.cs:397` — `PlayFromPlayableAnimationSetEntry(..., float startNormalizedTime = 0f)` (private)
  - `AnimatorBridgeBase.cs:597-598` — `public virtual IEnumerator CrossfadeAsync(AnimationClip clip, AnimationClipInfo clipInfo, AnimationMask mask, ActionData[] actions = null, int layerIndex = -1, bool transitionBack = true)`
  - `AnimatorBridgeBase.cs:629` — `public virtual void PlayLoopingAnimation(AnimationClip clip, AnimationMask mask, bool isActAsAnimatorOutput = false, float transitionIn = 0.1f)`
  - `PlayableGraphBridge.cs:37` — `public void InitializeGraph(Animator animator, int initialLayerCount = 3)`
  - `PlayableGraphBridge.cs:179` — `public ClipState Play(ClipTransitionData transition, float fadeDuration = -1f)`
  - `PlayableGraphBridge.cs:209` — `public ClipState Play(AnimationClip clip, float fadeDuration = 0.25f)`
  - `PlayableGraphBridge.cs:215` — `public PlayableState Play(PlayableState state, float fadeDuration = 0.25f)`
  - `MixerStates.cs:14,67,75,125` — `MixerState`/`ManualMixerState`/`LinearMixerState`/`DirectionalMixerState` constructors, `int childCount = 0`
  - `PlayableState.cs:64` — `public PlayableStateEvents Events(object owner = null)`

  Other parts of this same package correctly use the project's prescribed workaround (multiple explicit overloads
  instead of a default) — e.g. `AnimatorBridgeBase.PlayFromSetStrict(string,string,Action)` and
  `PlayFromSetStrict(string,string,Action,float)` (lines 489, 505) — so the pattern is known; these call sites
  just didn't apply it. Matters because callers can't tell from the call site which defaults are actually in play,
  and it's the exact style the codebase guide calls out as forbidden.

- **Warning** — `PlayableState.Events(object owner = null)` (`PlayableState.cs:64`) never reads `owner` in its
  body (`if (_events == null) _events = new PlayableStateEvents(this); return _events;`). Every call site passes
  `this` (`AnimatorBridgeBase.cs:366,383,435,440,618`) as if it registers ownership, but nothing is done with it —
  a dead parameter compounding the optional-parameter issue above. Either wire it into `PlayableStateEvents` (e.g.
  for future per-owner diagnostics) or drop it.

- **Info** — `FindEntryById(AnimationSet set, string entryId) => set.FindEntry(entryId)` is implemented twice as a
  private one-line forwarder: `AnimatorBridgeBase.cs:197-201` and `AnimationSetSequenceUtility.cs:181-185`
  (as `FindEntryById`, private static). Trivial, but two names for the same call is unnecessary — `AnimationSet.FindEntry`
  could be called directly at both sites, or the sequence utility could reuse the bridge's version if a shared
  internal helper type existed.

### Determinism

**No findings requiring a fix** — both non-deterministic-timing usages found are legitimate presentation timing,
consistent with `docs/11-Determinism.md`'s "visuals and I/O may use async tasks" / presentation-timing carve-out:

- `PlayableGraphBridge.cs:76` — `Update()` uses `Time.deltaTime` to advance the Playables graph's own layer/state
  fade weights. This only affects animation blending, never simulation state.
- `AnimatorBridgeBase.cs:389` — `UnpauseSequenceStateAfterDelay` uses `yield return new WaitForSeconds(delay)` to
  resume a paused clip after a link-hold. This only pauses/resumes animation playback speed.

- Info (boundary risk, not a defect here) — Both of the above ultimately fire an `onComplete`/`OnEnd` callback
  (`AnimatorBridgeBase.cs:342-362`, `393`). If a *caller* outside this package wires that callback to a gameplay
  mutation (e.g. "grant reward when the animation ends"), the effective gameplay timing becomes wall-clock-driven
  via `WaitForSeconds`/`Time.deltaTime` rather than the deterministic scheduler
  (`SimulationLoop.Schedule`, per `docs/00-AgentGuide.md` §3). That would be a violation, but it would live in the
  *caller*, not in this file — flagging as a boundary to watch when auditing consumers (e.g. `GameFramework`'s
  character/ability code) rather than a finding against this package.

### Doc/Architecture Drift

- **Warning** — `Documentation~/ARCHITECTURE.md` lines 163–172 ("## Animation") lists only `AnimationSet`,
  `LocomotionBlendProfile`, `PlayableGraphBridge`, `AnimationSetSequenceUtility`, plus three editor tools
  (`AnimationSetCodeGenerator`, `AnimationSetValidator`, `AnimationTestBenchWindow`). It omits
  `AnimatorBridgeBase` (the central abstract base every character bridge subclasses), the entire `PlayableState`
  family (`ClipState`, `ControllerState`, `MixerState`/`ManualMixerState`/`LinearMixerState`/`DirectionalMixerState`,
  `PlayableStateEvents`), `ILocomotionBlendLayer`, `LocomotionBlendParams`/`LocomotionBlendStanceDefinition`/
  `LocomotionBlendTemplateUtility`, `AnimationSetValidationProfile`, `CrossfadeSourceMode`, `AnimationSetLink`,
  `AnimationEventCatalog`/`CoreAnimationEvents`, and the remaining `Editor/AnimGraph/` tools
  (`PlayableGraphBridgeEditor`, `AnimationSetLinkPropertyDrawer`, `AnimationPreviewHelper`) and
  `Editor/Animation/AnimatorConstantsGenerator.cs`. Given how much of the actual surface area this section misses,
  it reads as a stub rather than a current map of the system.
- **Warning** — `ARCHITECTURE.md:169` says of `PlayableGraphBridge`: "see the package's own invariants doc before
  touching" — no such invariants doc exists anywhere under `Documentation~/` (only `ARCHITECTURE.md`,
  `AetherInspector.md`, `index.md`, `TweenX.md` are present). The actual invariants this line presumably refers to
  (e.g. "every layer boots connected at weight 1", "only `ClipState`s are transient and reclaimed, `MixerState`s
  are long-lived and reused" — see inline comments at `PlayableGraphBridge.cs:128-134` and `:312-316`) currently
  live only as code comments. Either write the referenced doc or fix the pointer.

### Codebase Gotchas

- No findings — no `??` usage on any `UnityEngine.Object` in scope; all such checks use explicit `== null`/`!= null`
  (e.g. `AnimatorBridgeBase.cs:67-77`).
- No findings — no struct violates the C# 9 rules: `LocomotionBlendParams`, `LocomotionClipCategoryValidation`,
  `EquipmentOverlaySpec`, `AnimationSetLinkHoldPlayback` all either have no instance field initializers, or (for
  `EquipmentOverlaySpec`'s explicit constructor) assign every field.
- No findings — no `OnValidate`/`OnAfterDeserialize` in scope performs an unconditional serialized-field write.
  `AnimationSet.OnValidate` (`AnimationSet.cs:124-145`) only logs warnings; no `ISerializationCallbackReceiver`
  implementations exist in this scope at all.
- Info — `AnimatorBridgeBase.Awake()` (`AnimatorBridgeBase.cs:65-78`) is inconsistent about the ref-caching pattern
  the codebase guide recommends: `animancer` is guarded (`if (animancer == null) { animancer = GetComponent<PlayableGraphBridge>(); }`,
  line 67) but `animator` is reassigned unconditionally (`animator = GetComponent<Animator>();`, line 68). Low
  impact since `Awake` writes don't dirty the editor scene, but it will silently clobber a manually-assigned
  `animator` reference (e.g. pointed at a rig on a different child) every time the object is instantiated/enters
  Play mode, and it's inconsistent with the very next line's guarded pattern.

## Fixes

No files were modified — per task instructions this audit is read-only; the AUDIT.md above is the only file
written. Recommended remediation order for a follow-up task:

1. Replace the optional parameters listed under Redundancy/Simplification with explicit overloads (the pattern
   `AnimatorBridgeBase.PlayFromSetStrict` already uses), starting with the `public`/`public virtual` members
   (`CrossfadeAsync`, `PlayLoopingAnimation`, `InitializeGraph`, the three `PlayableLayer.Play` overloads) since
   those are the widest-reaching call sites.
2. Decide and document the `AnimatorConstantsGenerator` vs. Playables-graph ownership question; update
   `ARCHITECTURE.md` accordingly either way.
3. Refresh `ARCHITECTURE.md`'s Animation section to list the full current type surface, and either write the
   referenced "invariants doc" for `PlayableGraphBridge` or drop the dangling pointer.
4. Drop or wire up the unused `owner` parameter on `PlayableState.Events`.

## Cross-references

- `Runtime/Extensions/Animations/AnimatorExtensions.cs` and `Runtime/Extensions/Animations/SyncAnimatorTime.cs`
  (out of this audit's assigned scope) call `Animator.GetCurrentAnimatorStateInfo` directly — worth checking under
  a future audit of `Runtime/Extensions/` for Consumption Contract compliance at the call sites that use them.
- `Packages/com.aethernexus.gameframework/CharacterSystem/Runtime/Animation/LocomotionBlendLayer.cs` implements
  `ILocomotionBlendLayer` (defined in this package) — relevant to the GameFramework package audit (task queued
  separately) for whether its `GetDominantEntryId()`/`ResolveTurnClipId()` are used for presentation only.
- `Packages/com.aethernexus.gameframework/CharacterSystem/Runtime/CharacterAnimator/CharacterAnimator.cs:869`
  carries the `AnimatorConstantsGenerator`-generated region referenced in the Ownership finding above — relevant
  evidence for the GameFramework package audit.
- `docs/02-Libraries.md`, `docs/12-ConsumptionContracts.md`, `docs/11-Determinism.md`,
  `docs/00-AgentGuide.md` §3 — source of the rules applied throughout this audit.
