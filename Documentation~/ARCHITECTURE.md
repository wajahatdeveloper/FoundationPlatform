# Foundation Platform — Architecture

Package id: `com.aethernexus.foundationplatform` (`Packages/com.aethernexus.foundationplatform/`).

Base platform layer for the AetherNexus ecosystem. No dependencies on other AetherNexus gameplay packages — they depend on this package, not the reverse. Provides: EventBus, DebugX, CoroutineX, TweenX, patterns, animation data packaging, gizmos, extensions, and editor authoring tooling (AetherInspector, ProjectWindowX, HierarchyX, UI Validation, Preset Automation).

See also: [index.md](index.md) · [TweenX](TweenX.md) · [AetherInspector](AetherInspector.md)

---

## Namespace map

Most types live under `AetherNexus.FoundationPlatform.*`. A few subsystems keep a bare (no-prefix) namespace instead — noted explicitly below rather than left to guesswork.

| Namespace | Folder | Notes |
|---|---|---|
| `AetherNexus.FoundationPlatform.Messaging` | `Runtime/Messaging/EventBus/`, `Runtime/Identity/Identity.cs` | `EventBus`, `BaseGameEvent`, `DomainEvent`, `Identity`, `IIdentity`, `SubscriptionToken` — **not** global despite ergonomic call sites. `Identity` value type lives on disk under `Runtime/Identity/`; `IIdentity` stays in EventBus |
| `AetherNexus.FoundationPlatform.CoroutineX` | `Runtime/CoroutineX/` | `CoroutineX`, `Routines`, `CoroutineXExecutor`, `CoroutineXOwner` — **not** global |
| `AetherNexus.FoundationPlatform` | `Runtime/Patterns/` | root types such as `FragmentData` |
| `AetherNexus.FoundationPlatform.Animation` / `.Editor.Animation` | `Runtime/Animation/`, `Editor/Animation/` | includes `AnimGraph/` |
| `AetherNexus.FoundationPlatform.Attributes` | `Runtime/Attributes/` | `[Tag]`, `[Layer]`, `[TooltipIcon]`, run-order attributes |
| `AetherNexus.FoundationPlatform.Behaviours` | `Runtime/Behaviours/` | small reusable MonoBehaviours |
| `AetherNexus.FoundationPlatform.DebugX` | `Runtime/DebugX/`, `Editor/DebugX/` | logging API + editor menu items. **Gotcha:** the class and its enclosing namespace share the name `DebugX` — see `docs/00-AgentGuide.md` §3 |
| `AetherNexus.FoundationPlatform.DebugX.ConsoleView.Editor` | `Editor/Console/` | DebugX Console window |
| `AetherNexus.FoundationPlatform.AetherInspector` | `Runtime/AetherInspector/` | runtime-visible attributes |
| `AetherNexus.FoundationPlatform.AetherInspector.Editor` | `Editor/AetherInspector/` | inspector engine, `GuiKit` |
| `AetherNexus.FoundationPlatform.Identity` | `Runtime/Identity/`, `Editor/Identity/` | `IdentityComponent`, `IdentityFieldAttribute` — consumers of the `Identity` value type. Same `Runtime/Identity/` folder also holds `Identity.cs` (Messaging namespace — see above) |
| `ProjectWindowX` (bare, no prefix) | `Editor/ProjectWindowX/` | Project-window row-decoration + hover-create pipeline. `HOMAM_GEC`-gated (see below) |
| `HierarchyX` (bare, no prefix) | `Editor/HierarchyX/` | Hierarchy-window row-decoration + docked-panel pipeline |
| `AetherNexus.FoundationPlatform.Gizmos` | `Runtime/Gizmos/`, `Editor/Gizmos/` | scene-view gizmo drawing |
| `AetherNexus.FoundationPlatform.TweenX` / `.TweenX.Feedbacks` / `.TweenX.EditorTools` | `Runtime/TweenX/`, `Editor/TweenX/` | tweens + Feedback player |
| `AetherNexus.FoundationPlatform.Utilities.Menus` | `Runtime/Menus/` | `MenuPaths`, `MenuPriorities` |
| `AetherNexus.FoundationPlatform.Editor.Utilities` (+ `.Messaging`, `.Debugging`, `.Tools`, `.Validation.UI`, …) | `Editor/Utilities/`, `Editor/Messaging/`, `Editor/StaleComponentGuard/`, `Editor/EditorEnhancerX/`, `Editor/AssetImport/`, … | general editor helpers and windows |
| `AetherNexus.FoundationPlatform.Editor.Tools` | `Editor/Tools/`, `Editor/Tools/PrefabLightmapGenerator/` | codegen/scaffolding tools, lightmap baking (the baked-data `MonoBehaviour` itself, `PrefabLightmapData`, lives in `AetherNexus.FoundationPlatform.Tools` under `Runtime/Tools/PrefabLightmapGenerator/` so it compiles into player builds) |

**Rule of thumb:** prefer `AetherNexus.FoundationPlatform.*`, with `.Editor` (or an `Editor` asm) where types are editor-only. `ProjectWindowX` and `HierarchyX` are the only two subsystems that intentionally keep a bare namespace — not an oversight, but not yet reconciled with the "prefer prefixed" rule of thumb either; treat as a known inconsistency rather than a pattern to copy for new code.

---

## Assembly definitions

```
UniTask                          (Runtime/ThirdParty/UniTask/)   — embedded
FoundationPlatform.Runtime       (Runtime/)
  references: Unity.InputSystem, Unity.TextMeshPro, UniTask

UniTask.Editor                   (Editor/ThirdParty/UniTask/)
FoundationPlatform.Editor        (Editor/)
  references: FoundationPlatform.Runtime
  includePlatforms: [Editor]
```

`ProjectWindowX.Editor`, `HierarchyX.Editor` (folder `Editor/HierarchyX/`, despite the type-side asmdef name), `EditorEnhancerX.Editor`, and `FoundationPlatform.StaleComponentGuard.Editor` additionally carry `defineConstraints: ["HOMAM_GEC"]` + `versionDefines` on `com.aethernexus.gameenginecore` — they only compile when GameEngineCore is present as a UPM package. This is a deliberate Asset-Store publishing pattern (see `docs/Notes/FoundationPlatform-PUBLISHING.md`, `docs/Notes/GameEngineCore-PUBLISHING.md` §`HOMAM_GEC` define), not a bug: no assembly *reference* to GameEngineCore exists, so the top-line "no dependencies on other AetherNexus gameplay packages" claim still holds, but these four designer-facing subsystems silently don't exist at all in a build without GameEngineCore installed.

## Event Bus

`EventBus` — `Runtime/Messaging/EventBus/EventBus.cs` — static pub/sub.

```csharp
EventBus.Subscribe<MyEvent>(handler, priority: 0);
EventBus.SubscribeWithToken<MyEvent>(handler);   // returns SubscriptionToken
EventBus.Unsubscribe<MyEvent>(handler);
EventBus.Publish(myEvent);
```

- Events carry an `Identity` channel; `Publish()` routes to that channel's subscribers.
- `[BroadcastGlobal]` — event delivered to all channels regardless of `Identity`.
- `BeginDomainPublishGate()` / `EndDomainPublishGate()` — scoped gate; `DomainEvent`s only publish inside an open gate (typically during a committed action phase in higher-level AetherNexus products). Publishing a `DomainEvent` outside a gate fails loudly (assert/throw), not silently.
- Subscribers invoked priority-sorted, higher first.

| Type | Role |
|---|---|
| `BaseGameEvent` | base for all events; stores timestamp + provenance |
| `DomainEvent` | gameplay state-change event; requires publish gate |
| `Identity` | `string`-backed value type; `Identity.Global = "__global__"`, `Identity.None` = invalid |
| `SubscriptionToken` | opaque handle for `Unsubscribe(token)` |

Debug UI (`EventBusWindow`, subscription/publish history windows) lives in `Editor/Messaging/EventBus/`. GameEngineCore-specific debug-display filtering (recognizing GameAction/RuleSystem infrastructure so it doesn't clutter publisher/subscriber names) is *not* hardcoded here — it's an optional `IRuleSystemDebugClassifier` GameEngineCore registers via `EventBus.RegisterRuleSystemClassifier(...)`, mirroring the `IEventDebugSignalEmitter` seam used for telemetry.

**Player debug reflection (opt-in):** EventBus reflection-based debug metadata (subscriber naming, history enrichment) always compiles in the Editor. Development Builds include it only when Project Settings → EventBus Debug → **Include Reflection In Development Builds** is on, which adds scripting define `EVENTBUS_DEBUG_REFLECTION`. A Development Build with that option enabled shows a Continue/Cancel warning dialog at preprocess (never silent). Release builds never include that path.

---

## Logging (DebugX)

`DebugX` — `Runtime/DebugX/DebugX.cs` — static logging API:

```csharp
DebugX.Logger(LogChannels.AI).Info("threat found: {target}", targetId);          // zero-alloc path
DebugX.Builder(LogChannels.Combat).WithProperty("damage", 42).Error("hit failed"); // allocating, richer
```

**Channels:** `Default, Engine, Simulation, DevTools, Framework, Validation, SceneTransition, GAS, Locomotion, Inventory, Economy, Quest, Combat, RuleSystem, AI, UI, Editor`

**Zero-alloc path:** `ShouldEmit(channel, level)` is checked before any string building; overloads exist for 1–5 typed args to avoid `params object[]` boxing.

```
DebugXInitializer.Initialize()  [RuntimeInitializeOnLoadMethod(BeforeSceneLoad)]
  └─ LogPipeline.Configure()
       ├─ set minimum level + excluded channels
       ├─ register sinks (per platform, below)
       └─ start LogQueue + MainThreadDispatcher
```

| Platform | Sinks |
|---|---|
| Editor | `EditorConsoleSink` (feeds the DebugX Console window) + `FileSink` + `JsonFileSink` |
| Android | `UnityConsoleSink` (+ `FileSink` in development builds) |
| WebGL | `UnityConsoleSink` |
| Standalone / other | `UnityConsoleSink` + `FileSink` + `JsonFileSink` |

**DebugX Console window** — `Editor/Console/DebugXConsoleWindow.cs` — reads structured `LogEvent`s from `ConsoleLogStore` (ring buffer fed by `EditorConsoleSink`). Supports filters, tabs, watch expressions, snippets, compile-error surfacing, export.

**Deliberate carve-out — caller-info reflection:** `CallerInfoHelper` and `MessageTemplateParser` use cached `StackTrace`/`MethodBase` reflection on every log call in all builds so file/line/member attribution reaches sinks. This is an intentional trade-off (not a silent violation of the no-runtime-reflection rule). The Unity-stack-extractor fallback remains editor-gated. See [KNOWN-ISSUES-DebugX-Reflection.md](KNOWN-ISSUES-DebugX-Reflection.md) for rationale and future options if attribution is ever threaded via `[CallerMemberName]` instead.

---

## CoroutineX

`CoroutineX` — `Runtime/CoroutineX/CoroutineX.cs` — coroutine lifecycle with explicit ownership. First-party code (not vendored — the `DebugX` logging calls woven into its control flow are a deliberate first-party dependency, not an in-place patch of a third-party drop).

| Member | Purpose |
|---|---|
| `CoroutineX.Run(IEnumerator, owner)` | start owned coroutine |
| `CoroutineX.Run(IEnumerator)` | start unowned (via executor singleton) |
| `.Stop()` | pause execution |
| `.Reset()` | back to initial state |
| `.Rerun()` | `Reset().Run()` |
| `.SetOwner(go)` / `.MakeUnowned()` | ownership transfer |
| `.WaitForComplete()` / `WaitForStop()` / `WaitForRun()` / `WaitForReset()` | awaitable, return `YieldAwaiter` |

State events: `Reseted, Running, Stopped, Completed, Destroyed`.

`CoroutineXExecutor` — `DontDestroyOnLoad` singleton, dispatches unowned coroutines. `CoroutineXOwner` — auto-added to owned GameObjects, stops coroutines on deactivate.

**`RunAsync` / `WaitForCompletionAsync`:** first-party `Task` helpers collocated in `CoroutineX.cs` for bridge scenarios — not vendor drift.

**UniTask coexistence:** `Runtime/ThirdParty/UniTask/` and CoroutineX are both present by design. Pick the substrate that fits the call site (yield/coroutine lifecycle vs async/await); neither is a defect in the other.

---

## Patterns

| Class | File | Behavior |
|---|---|---|
| `SingletonBehaviour<T>` | `Runtime/Patterns/SingletonBehaviour.cs` | `FindFirstObjectByType` lazy-init; logs **Info** (not Error) when a second copy is found in a newly-loaded scene — the session survivor is kept, the duplicate destroyed |
| `PersistentSingletonBehaviour<T>` | same file | `DontDestroyOnLoad` variant |
| `Singleton<T>` | same file | non-MonoBehaviour, thread-safe lazy-init |
| `FragmentData<TDefinition, TPayload>` | `Runtime/Patterns/FragmentData.cs` | SO-based config: `SharedConfig` (reference to `TDefinition` SO) OR `CustomConfig` (inline `TPayload`) — pick one, not both |

All singletons handle application-quit via an `isQuitting` flag (don't recreate instances during teardown).

`FragmentData` is the recommended pattern for ScriptableObject-or-inline config across AetherNexus gameplay packages.

---

## Reactive / support types

| Class | Folder | Purpose |
|---|---|---|
| `Observable<T>` | `Runtime/Patterns/Observable.cs` | class (not struct — see Design Decisions); `Value` setter fires `OnValueChanged` / `OnValueChangedTo` / `OnValueChangedFromTo` |
| `ObservableList<T>` | `Runtime/Patterns/ObservableList.cs` | `ItemAdded`, `ItemRemoved`, `Cleared` events |
| `MaybeMonad` | `Runtime/SupportTypes/MaybeMonad.cs` | **non-generic** static class of LINQ-chain extension methods (`With`, `Return`, `If`, `Unless`, `Do`, `IfNotNull`) — not a generic `Some`/`None` optional-value wrapper |
| `CustomState` | `Runtime/SupportTypes/CustomState.cs` | a bare `MonoBehaviour` wrapping `Dictionary<string,string> keyValuePairs`, used by `MonoBehaviourExtensions.RunOnce`/`RunOncePersistent` as a per-object "have I already fired this once" flag store — no state-machine semantics |
| `HSL` / `HSV` | `Runtime/SupportTypes/` | color-space value types |

---

## Identity

`Identity` (value type, `Runtime/Identity/Identity.cs`, namespace `AetherNexus.FoundationPlatform.Messaging`) — string-based entity/channel id. `Identity.Global` = `"__global__"`; `Identity.None` = default invalid; `IsValid` = non-empty string; implicit `string → Identity` conversion.

`IIdentity` — interface, `Identity Identity` property (also `Runtime/Messaging/EventBus/`). `IdentityComponent` (`Runtime/Identity/`, namespace `AetherNexus.FoundationPlatform.Identity`) — MonoBehaviour, serialized string id, auto-generates a design-time id via the shared `IdentityComponent.NewDesignTimeId()` helper (also used by `Editor/Identity/IdentityFieldDrawer`'s "New" button). Duplicate detection is design-time only (`IdentityDuplicationHandler` in `Editor/Identity/`) — there is no runtime registry, so don't rely on uniqueness being enforced at runtime.

---

## Animation

| Class | Purpose |
|---|---|
| `AnimationSet` (SO) | named animation states/clips for a character |
| `AnimationSetEntry` / `AnimationSetLink` | individual clip entry / cross-set link within an `AnimationSet` |
| `LocomotionBlendProfile` / `LocomotionBlendStanceDefinition` | blend-tree config: directional mix + stance definitions |
| `ILocomotionBlendLayer` | interface for a locomotion blend layer contract; implemented outside this package (e.g. GameFramework's `CharacterSystem`) |
| `AnimatorBridgeBase` | abstract `MonoBehaviour` base every character animator bridge subclasses; owns the Animator's non-decisional passthrough properties (`Speed`, `ApplyRootMotion`, `UpdateMode`, etc. — the Animator is an output device, never polled for gameplay decisions here) |
| `PlayableGraphBridge` | Animancer-adjacent Playables graph bridge — see [PlayableGraphBridge-Invariants.md](PlayableGraphBridge-Invariants.md) before touching layer/state weight or lifecycle logic |
| `PlayableLayer` | one layer of the Playables graph, owns a state mixer |
| `PlayableState` family — `ClipState`, `ControllerState`, `MixerState` / `ManualMixerState` / `LinearMixerState` / `DirectionalMixerState`, `PlayableStateEvents` | the playable-state hierarchy `PlayableGraphBridge` ticks; `ClipState`s are transient (recreated per `Play()`), `MixerState`s are long-lived and reused |
| `AnimationSetSequenceUtility` | sequence playback utilities |
| `AnimationSetValidationProfile` | validation-rule config consumed by `AnimationSetValidator` |
| `AnimationEventCatalog` / `CoreAnimationEvents` | named animation-event catalog |
| `CrossfadeSourceMode` | enum controlling crossfade source resolution |

Editor tools (`Editor/Animation/`, `Editor/AnimGraph/`): `AnimationSetCodeGenerator` (strongly-typed animation state accessors), `AnimationSetValidator` (validates clip assignments), `AnimationTestBenchWindow`, `PlayableGraphBridgeEditor`, `AnimationSetLinkPropertyDrawer`, `AnimationPreviewHelper`, `AnimatorConstantsGenerator` (see dual-mechanism note below).

**Intentional dual mechanism — Mecanim hash tooling + Playables runtime:** `Editor/Animation/AnimatorConstantsGenerator.cs`
code-generates Mecanim `AnimatorController` param/state hash constants into a concrete bridge subclass.
This is **intentional** editor tooling for Mecanim hash constants — not legacy to retire from this package.
Runtime playback is owned by the Playables AnimGraph (`PlayableGraphBridge`, `AnimationSet` entries).
GameFramework's `CharacterAnimator.cs` uses both: an assigned `AnimatorController` (for generated hash constants and Mecanim wiring) and the Playables graph for actual clip playback. That dual use is expected.

**Consumer watch items:**
- `TryGetCurrentSetAndEntry` is an ungated reverse-lookup helper — do not use it to drive gameplay decisions.
- `onComplete` / `PlayableStateEvents.OnEnd` callbacks are presentation boundaries — must not write authoritative simulation state.

---

## Gizmos

`Runtime/Gizmos/` (drawing API) + `Editor/Gizmos/` (editor-side support, including all `[CustomEditor]` classes — `GizmosEditor`, `GizmosHandleTextEditor`, `ColliderGizmoEditor`). Performant scene-view gizmo drawing, originally assimilated from a vendored third-party tool.

---

## Extensions (~180 methods)

| Folder | Coverage |
|---|---|
| `Extensions/Core/` | GameObject, Component, MonoBehaviour, Type, String, null checks |
| `Extensions/Collections/` | Array, List, Dictionary, IEnumerable |
| `Extensions/Math/` | Vector2/3/4, Quaternion, Rect, int |
| `Extensions/Animations/` | Animator, AnimationClip, fade transitions |
| `Extensions/Physics/` | Collider, Rigidbody, Physics casts |
| `Extensions/Rendering/` | Camera, Renderer, Texture2D, graphics |
| `Extensions/UI/` | RectTransform, Canvas, EventSystem, rich text |
| `Extensions/Storage/` | `PersistentDataHandler` (JSON-backed, canonical for new code) + `PlayerPrefsX` (legacy binary codec, kept for existing callers) |
| `Extensions/Utilities/` | Base64, file I/O, streams |
| `Extensions/Reflection/` | `ReflectionExtensions` (`HasMethod`/`HasField`/`HasProperty`) — editor-only (`#if UNITY_EDITOR`), no runtime callers |
| `Extensions/Audio/`, `Extensions/Bitwise/`, `Extensions/Color/`, `Extensions/Random/`, `Extensions/Scene/`, `Extensions/Time/`, `Extensions/Validation/` | one concern each, self-explanatory from folder name |

### RandomX — pluggable random with `UnityEngine.Random`'s shape

`Extensions/Random/RandomX.cs` holds the collection helpers (`Shuffle`, `NextItem`, `NextWeightedInd`, …) that take an `IRandomProvider`. `RandomX.Unity.cs` adds a static facade mirroring `UnityEngine.Random` member for member — `value`, `Range`, `insideUnitCircle`, `insideUnitSphere`, `onUnitSphere`, `rotation`, `rotationUniform`, `ColorHSV` — routed through an installed `RandomX.Provider`.

Lowercase member names are deliberate: they match Unity's exactly, so adopting the facade in a file is a mechanical `Random.` → `RandomX.` substitution and nothing else. The point is that a deterministic sim can use the full surface rather than only the two methods a narrower facade offered — the missing members were why `UnityEngine.Random` kept appearing in simulation paths.

**No provider, no fallback:** every call throws and names the fix. A game engine installs one at bootstrap (GameEngineCore does this during SystemBoot). Falling back to `UnityEngine.Random` would make early rolls silently non-deterministic.

Two seams, both in `Runtime/Behaviours/`:

| Interface | Contract |
|---|---|
| `IRandomProvider` | `Range(float,float)`, `Range(int,int)` — all most callers need |
| `IRandomStreamSource : IRandomProvider` | adds `Stream(name)` for independent named sequences, plus `CaptureState()` / `RestoreState()` with an opaque provider-defined payload |

The state payload is an opaque string so this package stays ignorant of any particular RNG implementation. `RandomX.Stream` / `CaptureState` / `RestoreState` throw a naming error when the installed provider only implements the smaller interface.

`CollectionExtensions.GetRandom`/`Shuffle` (in `Extensions/Collections/`) and `MathExtensions.GetDirectionFromSpread` are explicitly documented as PRESENTATION-ONLY (bare `UnityEngine.Random`) — use the `IRandomProvider`-based path above for anything simulation-affecting.

---

## ProjectWindowX

`Editor/ProjectWindowX/` (namespace `ProjectWindowX`, bare — see Namespace map) — a *mechanism-only* Project-window row-decoration + hover-create layer, `HOMAM_GEC`-gated (see Assembly definitions). Owns the single `EditorApplication.projectWindowItemOnGUI` subscription project-wide.

Two `TypeCache`-discovered extension points:

| Interface | Purpose |
|---|---|
| `IProjectWindowXPass` | contributes a row-decoration pass (zebra rows, folder icons, file-extension labels, and three more ship built-in) |
| `IProjectWindowXContextMenu` | contributes an entry to the hover "+" create-menu |

It does **not** itself implement domain/mapped-type authoring (create-domain, create-mapped-types, out-of-sync badges) — those are contributed by GameEngineCore (`Editor/ProjectWindowX/AuthoringProjectWindowXConsumers.cs`, `DomainFolderColorPass.cs`, `LevelOwnershipBadgePass.cs`) through these two registries, matching docs/09's extension pattern. Settings live under **Project Settings ▸ ProjectWindowX**. Packages append extra settings blocks via `ProjectWindowXSettingsExtras.Register` (same pattern as HierarchyX's `HierarchyXSettingsExtras`) without ProjectWindowX referencing those packages.

---

## Hierarchy tooling (HierarchyX)

`Editor/HierarchyX/` (namespace `HierarchyX`, bare — see Namespace map) — the generic, engine-agnostic Hierarchy-window enhancement layer: one draw pipeline (`HierarchyX.OnItemGUI`) layering row tint, tree lines, best-component icon, missing-script badge, tag/layer/sorting-layer mini labels, a decorator-supplied chip, hover-only row controls, an accent spine, and a row separator — plus a docked/fallback setup panel (`Panel/`) hosting accordion sections.

Two `TypeCache`-discovered extension points:

| Interface | Purpose |
|---|---|
| `IHierarchyRowDecorator` | contributes row tint/accent/chip (e.g. GameEngineCore's `DomainHierarchyDecorator`/`SessionHierarchyDecorator` for engine-concept chips) |
| `IHierarchyPanelSection` | contributes a docked-panel accordion section (e.g. GameEngineCore's `SceneSetupPanelSection`/`GameSessionPanelSection`/`DomainsPanelSection`) |

Settings persist to `ProjectSettings/HierarchyXSettings.asset` (per-project, versionable — not per-user `EditorPrefs`). Packages append extra settings blocks via `HierarchyXSettingsExtras.Register` (e.g. Stale Component Guard, GameEngineCore engine-concept colours) without HierarchyX referencing those packages. `HierarchyXRowControls.soloButtons` (hover visibility/pickability toggles) defaults **off**: Unity's own stock Hierarchy already shows equivalent hover icons via `SceneVisibilityManager` at the same row position; only `rowActiveToggle` (genuinely new) defaults on.

---

## Editor tooling

| Tool | Location | Purpose |
|---|---|---|
| AetherInspector | `Editor/AetherInspector/` | Attribute-based inspector engine (groups, drawers, `GuiKit`) |
| ProjectWindowX | `Editor/ProjectWindowX/` | Project-window row decoration + hover-create pipeline (see section above) |
| HierarchyX | `Editor/HierarchyX/` | Hierarchy-window row decoration + docked panel (see section above) |
| StaleComponentGuard | `Editor/StaleComponentGuard/` | Detects components whose serialized YAML still carries fields the current script no longer declares (renamed/removed without `[FormerlySerializedAs]`); Hierarchy row decorator + inspector badge + Project Settings panel, one sweep `EditorWindow` as last resort |
| EditorEnhancerX | `Editor/EditorEnhancerX/` | Scene View / Hierarchy power tools: Scene View overlays, native `EditorTool` rail (duplicate-array, pivot-rotation/move tools), `[MainToolbarElement]` timescale slider, Project Settings provider |
| AssetImport | `Editor/AssetImport/` | Asset-import plugin pipeline |
| DebugX Console | `Editor/Console/` | Structured log console — **Window → DebugX Console...** |
| Event Bus windows | `Editor/Messaging/EventBus/` | Debug hub — **Window → Event Bus...** |
| Tween Debugger | `Editor/TweenX/` | Live tweens — **Window → TweenX → Tween Debugger** |
| UI Validation | `Editor/Validation/UI/` | UI hierarchy/naming conventions (asset postprocessor) |
| Preset Automation | `Editor/Tools/PresetAutomation/` | Enforce asset presets on import |
| Entity Debugger Overlay | `Editor/Debugging/` | Selection-following Scene view overlay (`IEntityDebugSection`) |
| Game State window | `Editor/Debugging/` | World-scope live state (`IWorldDebugSection`) — **Window → Domain → Game State...** |
| Scene Switcher | `Editor/Windows/SceneSwitcherWindow.cs` | Scene navigation |
| Weaver | `Editor/Tools/Weaver.cs` | Constant / package rebuild utilities (plain `static class`, not an `EditorWindow`) |
| Prefab Lightmap Generator | `Runtime/Tools/PrefabLightmapGenerator/` (baked-data component) + `Editor/Tools/PrefabLightmapGenerator/` (baking pipeline, inspector) | Prefab lightmap baking — the `PrefabLightmapData` `MonoBehaviour` compiles into player builds; only the `Lightmapping.Bake()`/`PrefabUtility`-dependent baking pipeline (`PrefabLightmapBaker`) and its custom inspector are editor-only |

### AetherInspector IMGUI theme

Inspector chrome is centralized in `AetherInspectorTheme.cs`. `GuiKit` is the public facade for non-inspector editor windows.

- Default fields still draw through `EditorGUILayout.PropertyField`.
- Visual harness: **Window → Diagnostics → AetherInspector Demo**.
- Full attribute matrix: [AetherInspector.md](AetherInspector.md).
- Reflection/IMGUI empty-catch sites and `ObjectSelectorPopupX` scope: see [AetherInspector.md](AetherInspector.md) § Implementation notes.

---

## Bootstrap

```
[RuntimeInitializeOnLoadMethod(BeforeSceneLoad)]
  ├─ DebugXInitializer.Initialize()      ← logging pipeline
  └─ CoroutineXExecutor.CreateInstance() ← global coroutine executor

EventBus — stateless, ready immediately (all static)
```

---

## Key design decisions

- **Static service locator over DI** — `EventBus`, `DebugX`, `CoroutineXExecutor` are static. Zero setup cost; acceptable because these are true app-lifetime singletons, not swappable services.
- **Struct-based zero-alloc logging** — `DebugXLogger` is a struct; `ShouldEmit()` gates before any string building; 1–5 typed-arg overloads avoid `params object[]` boxing.
- **Channel-based event routing** — `Identity`-tagged events, with `Identity.Global` as explicit fallback. Enables per-entity event scoping without subscriber-side filtering.
- **`DomainEvent` publish gate** — blocked outside the Commit phase to prevent state mutation from validation or other read-only phases. Fails loud (assert/throw), not silently.
- **`FragmentData<TDefinition, TPayload>`** — every SO-based config picks shared-SO-reference OR inline-custom-payload, never both, so there's one source of truth per fragment.
- **`Observable<T>` is a class, not a struct** — a mutable struct with delegate fields silently drops subscribers when copied (return-by-value, pass-by-value). Must stay a reference type to be exposed safely via properties. Always initialize explicitly: `= new Observable<T>()`.
