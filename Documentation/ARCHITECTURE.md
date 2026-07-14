# Foundation Platform — Architecture

Package id: `com.aethernexus.foundationplatform` (`Packages/com.aethernexus.foundationplatform/`).

Base platform layer for the AetherNexus ecosystem. No dependencies on other AetherNexus gameplay packages — they depend on this package, not the reverse. Provides: EventBus, DebugX, CoroutineX, TweenX, patterns, animation data packaging, gizmos, extensions, and editor authoring tooling (Framework Inspector, UI Validation, Preset Automation).

See also: [index.md](index.md) · [TweenX](TweenX.md) · [Framework Inspector](FrameworkInspector.md)

---

## Namespace map

Most types live under `AetherNexus.FoundationPlatform.*`. Several core messaging / coroutine APIs are **global** (no namespace) for ergonomic call sites.

| Namespace | Folder | Notes |
|---|---|---|
| *(global)* | `Runtime/Messaging/EventBus/`, `Runtime/CoroutineX/`, `Runtime/Identity/` (value type) | `EventBus`, `BaseGameEvent`, `Identity`, `CoroutineX`, tween extensions |
| `AetherNexus.FoundationPlatform` | `Runtime/Patterns/` | root types such as `FragmentData` |
| `AetherNexus.FoundationPlatform.Animation` / `.Editor.Animation` | `Runtime/Animation/`, `Editor/Animation/` | includes `AnimGraph/` |
| `AetherNexus.FoundationPlatform.Attributes` | `Runtime/Attributes/` | `[Tag]`, `[Layer]`, `[TooltipIcon]`, run-order attributes |
| `AetherNexus.FoundationPlatform.Behaviours` | `Runtime/Behaviours/` | small reusable MonoBehaviours |
| `AetherNexus.FoundationPlatform.DebugX` | `Runtime/DebugX/`, `Editor/DebugX/` | logging API + editor menu items |
| `AetherNexus.FoundationPlatform.DebugX.ConsoleView.Editor` | `Editor/Console/` | DebugX Console window |
| `AetherNexus.FoundationPlatform.FrameworkInspector` | `Runtime/FrameworkInspector/` | runtime-visible attributes |
| `AetherNexus.FoundationPlatform.FrameworkInspector.Editor` | `Editor/FrameworkInspector/` | inspector engine, `GuiKit` |
| `AetherNexus.FoundationPlatform.Gizmos` | `Runtime/Gizmos/`, `Editor/Gizmos/` | scene-view gizmo drawing |
| `AetherNexus.FoundationPlatform.TweenX` / `.TweenX.Feedbacks` / `.TweenX.EditorTools` | `Runtime/TweenX/`, `Editor/TweenX/` | tweens + Feedback player |
| `AetherNexus.FoundationPlatform.Utilities.Menus` | `Runtime/Menus/` | `MenuPaths`, `MenuPriorities` |
| `AetherNexus.FoundationPlatform.Editor.Utilities` (+ `.Messaging`, `.Debugging`, `.Tools`, `.Validation.UI`, …) | `Editor/Utilities/`, `Editor/Messaging/`, … | general editor helpers and windows |
| `AetherNexus.FoundationPlatform.Editor.Tools` | `Editor/Tools/PrefabLightmapGenerator/` | lightmap baking |

**Rule of thumb:** prefer `AetherNexus.FoundationPlatform.*`, with `.Editor` (or an `Editor` asm) where types are editor-only. Global APIs stay global by design.

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

Optional editor asmdefs (EditorEnhancerX, HierarchyX, ProjectWindowX, StaleComponentGuard) require scripting define `HOMAM_GEC` and stay inactive for standalone installs.

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
- `BeginDomainPublishGate()` / `EndDomainPublishGate()` — scoped gate; `DomainEvent`s only publish inside an open gate (typically during a committed action phase in higher-level AetherNexus products). Publishing a `DomainEvent` outside a gate fails loudly (assert/throw), not silently.
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

`FragmentData` is the recommended pattern for ScriptableObject-or-inline config across AetherNexus gameplay packages.

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
| Framework Inspector | `Editor/FrameworkInspector/` | Attribute-based inspector engine (groups, drawers, `GuiKit`) |
| DebugX Console | `Editor/Console/` | Structured log console — **Window → DebugX Console...** |
| Event Bus windows | `Editor/Messaging/EventBus/` | Debug hub — **Window → Event Bus...** |
| Tween Debugger | `Editor/TweenX/` | Live tweens — **Window → TweenX → Tween Debugger** |
| UI Validation | `Editor/Validation/UI/` | UI hierarchy/naming conventions (asset postprocessor) |
| Preset Automation | `Editor/Tools/PresetAutomation/` | Enforce asset presets on import |
| Entity Debugger Overlay | `Editor/Debugging/` | Selection-following Scene view overlay (`IEntityDebugSection`) |
| Scene Switcher | `Editor/Windows/SceneSwitcherWindow.cs` | Scene navigation |
| Weaver | `Editor/Tools/Weaver.cs` | Constant / package rebuild utilities |
| Prefab Lightmap Generator | `Editor/Tools/PrefabLightmapGenerator/` | Prefab lightmap baking |

### Framework Inspector IMGUI theme

Inspector chrome is centralized in `FrameworkInspectorTheme.cs`. `GuiKit` is the public facade for non-inspector editor windows.

- Default fields still draw through `EditorGUILayout.PropertyField`.
- Visual harness: **Tools → Diagnostics → Framework Inspector Demo**.
- Full attribute matrix: [FrameworkInspector.md](../DOCS/FrameworkInspector.md).

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