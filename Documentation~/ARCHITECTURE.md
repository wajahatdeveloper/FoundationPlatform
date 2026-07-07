# FoundationPlatform Architecture

**Path:** `Packages/com.homam.foundationplatform/`
**294 files:** 190 runtime (`Runtime/`), 104 editor (`Editor/`)

Base platform layer used by all other HOMAM frameworks. No dependencies on other HOMAM packages — everything else depends on this, not the reverse. Provides: pub/sub event bus, structured logging (DebugX), coroutine lifecycle (CoroutineX), singleton/reactive/fragment-data patterns, animation data packaging, gizmo drawing, ~180 extension methods, and editor authoring tooling (Framework Inspector, UI Validation, Preset Automation).

---

## Namespace map

| Namespace | Folder | Notes |
|---|---|---|
| `FoundationPlatform` | scattered | root/misc types (`FragmentData`, etc.) |
| `FoundationPlatform.Animation` / `.Editor.Animation` | `Runtime/Animation/`, `Editor/Animation/` | includes `AnimGraph/` subtree |
| `FoundationPlatform.Attributes` | `Runtime/Attributes/` | `[Tag]`, `[Layer]`, `[TooltipIcon]`, `[RunFirst/Before/After/Last]` |
| `FoundationPlatform.Behaviours` | `Runtime/Behaviours/` | small reusable MonoBehaviours |
| `FoundationPlatform.DebugX` | `Runtime/DebugX/` | logging core + sinks |
| `FoundationPlatform.FrameworkInspector` | `Runtime/FrameworkInspector/` | runtime-visible attributes only |
| `FoundationPlatform.FrameworkInspector.Editor` | `Editor/FrameworkInspector/` | reflection-driven inspector engine (drawer, resolver, GUI kit) |
| `FoundationPlatform.Gizmos` | `Runtime/Gizmos/`, `Editor/Gizmos/` | scene-view gizmo drawing |
| `FoundationPlatform.Utilities.Menus` | `Runtime/Menus/` | `MenuPaths`, `MenuPriorities` — Runtime asm, so no `.Editor.` in the name |
| `FoundationPlatform.Editor.Utilities` | `Editor/Utilities/`, `Editor/Drawers/`, `Editor/Windows/`, `Editor/AnimGraph/` | general editor helpers |
| `FoundationPlatform.Editor.Utilities.Messaging` | `Editor/Messaging/EventBus/` | EventBus debug windows |
| `FoundationPlatform.Editor.Utilities.PresetAutomation` | `Editor/Tools/PresetAutomation/` | asset preset enforcement |
| `FoundationPlatform.Editor.Utilities.Debugging` | `Editor/Debugging/` | in-context entity debugger overlay |
| `FoundationPlatform.Editor.Utilities.Tools` | `Editor/Tools/` | `Weaver` (package→folder), misc one-off editor tools |
| `FoundationPlatform.Editor.Tools` | `Editor/Tools/PrefabLightmapGenerator/` | lightmap data baking |
| `FoundationPlatform.Editor.Utilities.Validation.UI` | `Editor/Validation/UI/` | UI convention enforcement |
| `DebugXLogging` / `.ConsoleView.Editor` | `Editor/DebugX/`, `Editor/Console/` | **stale** — Runtime DebugX moved to `FoundationPlatform.DebugX`, Editor console half wasn't. See Known Issues. |
| *(none — global)* | `Editor/CoroutineX/`, `Editor/Identity/` | pre-existing, `#if UNITY_EDITOR`-wrapped |

**Rule of thumb:** everything is `FoundationPlatform.*`, with `.Editor` inserted at the point the type becomes editor-only. Exception is the `DebugXLogging` leftover above.

---

## Assembly definitions

```
FoundationPlatform.Runtime.asmdef   (Runtime/)
  references: Unity.InputSystem, Unity.TextMeshPro, UniTask

FoundationPlatform.Editor.asmdef    (Editor/)
  references: FoundationPlatform.Runtime
  includePlatforms: [Editor]
```

No `Tests/` asmdef yet.

---

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
- `BeginDomainPublishGate()` / `EndDomainPublishGate()` — scoped gate; `DomainEvent`s only publish inside a gate (Commit phase of the action pipeline). Publishing a `DomainEvent` outside a gate is a bug, not a fallback case — it throws/asserts rather than silently no-op'ing.
- Subscribers invoked priority-sorted, higher first.

| Type | Role |
|---|---|
| `BaseGameEvent` | base for all events; stores timestamp + provenance |
| `DomainEvent` | gameplay state-change event; requires publish gate |
| `Identity` | `string`-backed value type; `Identity.Global = "__global__"`, `Identity.None` = invalid |
| `SubscriptionToken` | opaque handle for `Unsubscribe(token)` |

Debug UI (`EventBusWindow`, subscription/publish history windows) lives in `Editor/Messaging/EventBus/`.

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

---

## CoroutineX

`CoroutineX` — `Runtime/CoroutineX/CoroutineX.cs` — coroutine lifecycle with explicit ownership.

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

---

## Patterns

| Class | File | Behavior |
|---|---|---|
| `SingletonBehaviour<T>` | `Runtime/Patterns/SingletonBehaviour.cs` | `FindFirstObjectByType` lazy-init; logs error on duplicate |
| `PersistentSingletonBehaviour<T>` | same file | `DontDestroyOnLoad` variant |
| `Singleton<T>` | same file | non-MonoBehaviour, thread-safe lazy-init |
| `FragmentData<TDefinition, TPayload>` | `Runtime/Patterns/FragmentData.cs` | SO-based config: `SharedConfig` (reference to `TDefinition` SO) OR `CustomConfig` (inline `TPayload`) — pick one, not both |

All singletons handle application-quit via an `isQuitting` flag (don't recreate instances during teardown).

`FragmentData` is used throughout gameplay frameworks for composition-over-inheritance SO config: `WeaponCombatFragment`, `ItemContainerDefinition`, `ShopOfferConfigData`, `AIBehaviorDefinition`, etc.

---

## Reactive / support types

| Class | Folder | Purpose |
|---|---|---|
| `Observable<T>` | `Runtime/Patterns/Observable.cs` | class (not struct — see Design Decisions); `Value` setter fires `OnValueChanged` / `OnValueChangedTo` / `OnValueChangedFromTo` |
| `ObservableList<T>` | `Runtime/Patterns/ObservableList.cs` | `ItemAdded`, `ItemRemoved`, `Cleared` events |
| `MaybeMonad<T>` | `Runtime/SupportTypes/MaybeMonad.cs` | nullable wrapper (`Some`/`None`), functional optional |
| `CustomState` | `Runtime/SupportTypes/CustomState.cs` | state-machine support type |
| `HSL` / `HSV` | `Runtime/SupportTypes/` | color-space value types |

---

## Identity

`Identity` (value type) — string-based entity/channel id. `Identity.Global` = `"__global__"`; `Identity.None` = default invalid; `IsValid` = non-empty string; implicit `string → Identity` conversion.

`IIdentity` — interface, `Identity Identity` property. `IdentityComponent` — MonoBehaviour, serialized string id, auto-generates a design-time id. Duplicate detection is design-time only (`IdentityDuplicationHandler` in `Editor/Identity/`) — there is no runtime registry, so don't rely on uniqueness being enforced at runtime.

---

## Animation

| Class | Purpose |
|---|---|
| `AnimationSet` (SO) | named animation states/clips for a character |
| `LocomotionBlendProfile` | blend-tree config: directional mix + stance definitions |
| `PlayableGraphBridge` | Animancer-adjacent playable-graph bridge — see the package's own invariants doc before touching |
| `AnimationSetSequenceUtility` | sequence playback utilities |

Editor tools (`Editor/Animation/`, `Editor/AnimGraph/`): `AnimationSetCodeGenerator` (strongly-typed animation state accessors), `AnimationSetValidator` (validates clip assignments), `AnimationTestBenchWindow`.

---

## Gizmos

`Runtime/Gizmos/` (drawing API) + `Editor/Gizmos/` (editor-side support). Performant scene-view gizmo drawing, originally assimilated from a vendored third-party tool.

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
| `Extensions/Storage/` | PlayerPrefs wrapper, persistent-data adapters |
| `Extensions/Utilities/` | Base64, file I/O, streams, reflection |
| `Extensions/Audio/`, `Extensions/Bitwise/`, `Extensions/Color/`, `Extensions/Random/`, `Extensions/Reflection/`, `Extensions/Scene/`, `Extensions/Time/`, `Extensions/Validation/` | one concern each, self-explanatory from folder name |

---

## Editor tooling

| Tool | Location | Purpose |
|---|---|---|
| Framework Inspector | `Editor/FrameworkInspector/` | reflection-driven inspector engine (attribute-based groups, expression resolver, dropdowns, list drawer) |
| UI Validation | `Editor/Validation/UI/` | enforces UI hierarchy/naming conventions, runs as asset postprocessor |
| Preset Automation | `Editor/Tools/PresetAutomation/` | enforces asset presets on import |
| DebugX Console | `Editor/Console/` | see Logging section above |
| Entity Debugger Overlay | `Editor/Debugging/` | selection-following SceneView overlay, unifies per-domain debug sections via `IEntityDebugSection` |
| Scene Switcher | `Editor/Windows/SceneSwitcherWindow.cs` | scene navigation dropdown |
| Weaver | `Editor/Tools/Weaver.cs` | package/folder extraction tool |
| Prefab Lightmap Generator | `Editor/Tools/PrefabLightmapGenerator/` | lightmap data baking for prefabs |

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