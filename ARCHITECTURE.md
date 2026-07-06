# FoundationPlatform Architecture

**Last updated:** 2026-07-06
**Path:** `Assets/Frameworks/FoundationPlatform/`
**295 files: 191 runtime (`Scripts/`), 104 editor (`Editor/`)**

---

## Overview

Base platform layer used by all other frameworks. Provides: pub/sub event bus with channel routing, structured logging system (DebugX), advanced coroutine lifecycle (CoroutineX), singleton patterns, observable/reactive types, animation data packaging, gizmo drawing, and ~180 extension methods.

2026-07 consolidation pass: assimilated 5 vendored third-party namespaces into `FoundationPlatform.*`, moved editor-only code out of the Runtime asmdef, removed 6 dead-code files (verified zero prefab/scene GUID references), unified orphan namespaces. See **Pending Cleanup** below for what's left.

---

## Namespace Map

| Namespace | Location | Notes |
|---|---|---|
| `FoundationPlatform` | scattered | root/misc types |
| `FoundationPlatform.Animation` / `.Editor.Animation` | `Scripts/Animation/`, `Editor/Animation/` | folded from former `Core.Animation` |
| `FoundationPlatform.Attributes` | `Scripts/Attributes/` | was no-namespace |
| `FoundationPlatform.Behaviours` | `Scripts/Behaviours/` | was mixed (no-namespace + `GS.Behaviours`) |
| `FoundationPlatform.Gizmos` | `Scripts/Gizmos/`, `Editor/Gizmos/` | assimilated from `SVerdeTools.FastGizmos` |
| `FoundationPlatform.Utilities.Menus` | `Scripts/Menus/` | was `FoundationPlatform.Editor.Menus` (wrong — file is Runtime asm) |
| `FoundationPlatform.Editor.Utilities` | `Editor/Utilities/` | absorbed `BayatGames.Utilities.Editor` (`DataFolderExemptionMarker.cs`) |
| `FoundationPlatform.Editor.Utilities.Messaging` | `Editor/Messaging/EventBus/` | assimilated from `OpenScripts.EventBus.Editor` (13 files); physically moved here from `Scripts/Messaging/EventBus/Editor/` |
| `FoundationPlatform.Editor.Utilities.PresetAutomation` | `Editor/Tools/PresetAutomation/` | assimilated from `Essentials.PresetAutomation` |
| `FoundationPlatform.Editor.Utilities.Debugging` | `Editor/Debugging/` | |
| `FoundationPlatform.Editor.Utilities.Tools` | `Editor/Tools/` | includes `Weaver.cs`, assimilated from `CodeStage.PackageToFolder` |
| `FoundationPlatform.Editor.Utilities.Validation.UI` | `Editor/Validation/UI/` | |
| `DebugXLogging` / `.ConsoleView` / `.ConsoleView.Editor` | `Scripts/DebugX/`, `Editor/DebugX/` | established in-house brand name, deliberately not renamed to `FoundationPlatform.*` |
| `Framework.Inspector` / `.Editor` | `Scripts/FrameworkInspector/`, `Editor/FrameworkInspector/` | **not yet assimilated**, see Pending Cleanup |
| `OpenScripts` / `.Editor` | `Editor/Tools/PrefabLightmapGenerator/` | **vendored, not yet assimilated**, see Pending Cleanup |
| *(none — global)* | `Editor/CoroutineX/`, `Editor/Identity/` | pre-existing, `#if UNITY_EDITOR`-wrapped, not a new issue |

---

## Pending Cleanup

1. **`Scripts/FrameworkInspector/`** (2 files: `FrameworkInspector.cs`, `FrameworkInspectorAttributes.cs`) duplicates intent with the real engine in `Editor/FrameworkInspector/` (10 files). Not yet merged — check whether the version constant in `FrameworkInspector.cs` is read at runtime before folding into the Editor asmdef.
2. **`Editor/Tools/PrefabLightmapGenerator/`** (3 files, namespace `OpenScripts`/`OpenScripts.Editor`) — vendored tool discovered after the initial third-party sweep; not yet assimilated into `FoundationPlatform.*`.

---

## Event Bus

**`EventBus`** (`Scripts/Messaging/EventBus/EventBus.cs`) — static pub/sub.

```csharp
EventBus.Subscribe<MyEvent>(handler, priority: 0)
EventBus.SubscribeWithToken<MyEvent>(handler)        // returns SubscriptionToken
EventBus.Unsubscribe<MyEvent>(handler)
EventBus.Publish<MyEvent>(myEvent)
```

**Routing:**
- Events carry an `Identity` channel; `Publish()` routes to that channel's subscribers
- `[BroadcastGlobal]` attribute — event delivered to all channels
- `BeginDomainPublishGate()` / `EndDomainPublishGate()` — scoped gate; `DomainEvent`s only publish inside a gate (Commit phase of action pipeline)
- Priority-sorted subscriber invocation (higher first)

**`BaseGameEvent`** — base for all events; stores timestamp + provenance
**`DomainEvent`** — gameplay state change events; requires publish gate
**`Identity`** — `string`-backed value type; `Identity.Global = "__global__"`, `Identity.None` (invalid)
**`SubscriptionToken`** — opaque handle for `Unsubscribe(token)`

Editor debug UI (`EventBusWindow` etc.) lives in `Editor/Messaging/EventBus/` — namespace `FoundationPlatform.Editor.Utilities.Messaging`.

---

## Logging (DebugX)

**`DebugX`** (`Scripts/DebugX/DebugX.cs`) — static logging API:

```csharp
DebugX.Logger(LogChannels.AI).Info("threat found: {target}", targetId)   // zero-alloc
DebugX.Builder(LogChannels.Combat).WithProperty("damage", 42).Error("...")  // allocating
```

**Log channels:** `Default`, `Engine`, `Simulation`, `DevTools`, `Framework`, `Validation`, `SceneTransition`, `GAS`, `Locomotion`, `Inventory`, `Economy`, `Quest`, `Combat`, `RuleSystem`, `AI`, `UI`, `Editor`

**Zero-alloc path:** `ShouldEmit(channel, level)` checked first; if filtered → no-op. Overloads for 1–5 typed args to avoid `params object[]`.

**`LogPipeline`** — sink routing + minimum level filtering
**`DebugXInitializer`** — `[RuntimeInitializeOnLoadMethod(BeforeSceneLoad)]` configures sinks per platform:
- Editor: EditorConsoleSink (feeds the DebugX Console window) + FileSink + JsonFileSink
- Android: UnityConsoleSink (+ FileSink in development builds)
- WebGL: UnityConsoleSink
- Standalone/other: UnityConsoleSink + FileSink + JsonFileSink

**Bootstrap:**
```
DebugXInitializer.Initialize()  [BeforeSceneLoad]
  └─ LogPipeline.Configure()
       ├─ Set minimum level + excluded channels
       ├─ Register sinks
       └─ Start LogQueue + MainThreadDispatcher
```

---

## CoroutineX

**`CoroutineX`** (`Scripts/CoroutineX/CoroutineX.cs`) — advanced coroutine lifecycle:

| Method | Purpose |
|---|---|
| `CoroutineX.Run(IEnumerator, owner)` | Start owned coroutine |
| `CoroutineX.Run(IEnumerator)` | Start unowned (executor singleton) |
| `Stop()` | Pause execution |
| `Reset()` | Back to initial state |
| `Rerun()` | `Reset().Run()` |
| `SetOwner(go)` / `MakeUnowned()` | Ownership transfer |

**Awaitable expectations:** `WaitForComplete()`, `WaitForStop()`, `WaitForRun()`, `WaitForReset()` — all return `YieldAwaiter`

**State events:** `Reseted`, `Running`, `Stopped`, `Completed`, `Destroyed`

**`CoroutineXExecutor`** — `DontDestroyOnLoad` singleton; dispatches unowned coroutines
**`CoroutineXOwner`** — auto-added to owned GameObjects; stops coroutines on deactivate
**`CoroutineXOwnerEditor`** — `Editor/CoroutineX/` (global namespace, `#if UNITY_EDITOR`)

---

## Singleton Patterns

| Class | File | Behavior |
|---|---|---|
| `SingletonBehaviour<T>` | `Scripts/Patterns/SingletonBehaviour.cs` | `FindFirstObjectByType` lazy-init; logs error on duplicate |
| `PersistentSingletonBehaviour<T>` | same file | `DontDestroyOnLoad` variant |
| `Singleton<T>` | same file | Non-MonoBehaviour; thread-safe lazy-init |

All handle application-quit gracefully via `isQuitting` flag.

---

## Reactive / Observable Types

| Class | Purpose |
|---|---|
| `Observable<T>` | Class; `Value` setter fires `OnValueChanged`, `OnValueChangedTo`, `OnValueChangedFromTo`. Initialize fields with `= new Observable<T>()` |
| `ObservableList<T>` | `ItemAdded`, `ItemRemoved`, `Cleared` events; used by `ScrollableList` data binding |
| `MaybeMonad<T>` | Nullable wrapper (`Some/None`); functional optional |
| `OptionalValue<T>` / `OptionalReference<T>` | Serializable optional fields |
| `CustomState` | State machine support type |

---

## Identity System

**`Identity`** (value type) — string-based entity/channel ID:
- `Identity.Global` — `"__global__"` for global event channel
- `Identity.None` — default invalid
- `IsValid` — non-empty string
- Implicit `string → Identity` conversion

**`IIdentity`** — interface: `Identity Identity` property
**`IdentityComponent`** — MonoBehaviour; serialized string id, implements `IIdentity`; auto-generates design-time ID. Duplicate detection is design-time (`IdentityDuplicationHandler`, in `Editor/Identity/`); no runtime registry.

---

## Gizmos (FoundationPlatform.Gizmos)

Assimilated from vendored `SVerdeTools.FastGizmos`. Performant scene-view gizmo drawing:
- `Scripts/Gizmos/` — runtime drawing API
- `Editor/Gizmos/` — editor-side support

---

## Animation System

| Class | Purpose |
|---|---|
| `AnimationSet` (SO) | Stores named animation states/clips for a character |
| `LocomotionBlendProfile` | Blend tree config: directional mix + stance definitions |
| `AnimatorBridgeBase` | Base for Animancer bridges |
| `AnimationSetSequenceUtility` | Sequence playback utilities |

**Editor tools** (`Editor/Animation/`, `Editor/AnimGraph/`):
- `AnimationSetCodeGenerator` — strongly-typed animation state accessors
- `AnimationSetValidator` — validates clip assignments

---

## Fragment Data Pattern

Used throughout all framework systems for composition-over-inheritance SO configuration:

```csharp
// Author a reusable config as SO, or inline custom data:
FragmentData<TDefinition, TPayload>
  └─ SharedConfig (reference to TDefinition SO)  OR  CustomConfig (inline TPayload)

IFragmentConfig<TPayload>
  └─ Payload property → returns TPayload
```

Used by: `WeaponCombatFragment`, `ItemContainerDefinition`, `ShopOfferConfigData`, `AIBehaviorDefinition`, etc.

---

## Extensions (~180 methods)

| Namespace | Coverage |
|---|---|
| `Extensions/Core/` | GameObject, Component, MonoBehaviour, Type, String, null checks |
| `Extensions/Collections/` | Array, List, Dictionary, IEnumerable |
| `Extensions/Math/` | Vector2/3/4, Quaternion, Rect, int |
| `Extensions/Animations/` | Animator, AnimationClip, fade transitions |
| `Extensions/Physics/` | Collider, Rigidbody, Physics casts |
| `Extensions/Rendering/` | Camera, Renderer, Texture2D, GraphicExtensions |
| `Extensions/UI/` | RectTransform, Canvas, EventSystem, RichText |
| `Extensions/Storage/` | PlayerPrefs wrapper, persistent data adapters |
| `Extensions/Utilities/` | Base64, File I/O, streams, reflection |

---

## Utility Behaviours

`Scripts/Behaviours/`: `AreaSpawner`, `CameraLookConstraint`, `CooldownTracker`, `Drag2DHandler`, `Drop2DHandler`, `InspectorSeparator`, `SceneSpawnReadyGate`. (`IRandomProvider` — interface, not a behaviour.)

Removed 2026-07 as dead code (zero prefab/scene/asset GUID references): `GenericSelection`, `ParentScaleOnly`, `MouseCentering`, `CountdownDisplay`, `TextTokenReplacer`, `ButtonClickSoundCustomizer`.

---

## Editor Tooling

| Tool | Purpose |
|---|---|
| `AutoBinderWindow` | UI binding automation |
| `SceneSwitcherWindow` | Scene navigation |
| `UIValidationEngine` | UI convention enforcement (`Editor/Validation/UI/`) |
| `PresetAutomation/` | Asset preset enforcement (assimilated, was `Essentials.PresetAutomation`) |
| `EditorGUIX`, `EditorX` | Editor GUI helpers |
| `Gizmos/` (Editor half) | Scene gizmo drawing support (assimilated, was `SVerdeTools.FastGizmos`) |
| `Weaver` | Package/folder extraction tool (assimilated, was `CodeStage.PackageToFolder`) |
| `PrefabLightmapGenerator/` | Lightmap data baking for prefabs — **vendored, still under `OpenScripts` namespace, not yet assimilated** |

---

## Bootstrap

```
[RuntimeInitializeOnLoadMethod(BeforeSceneLoad)]
  ├─ DebugXInitializer.Initialize()   ← logging pipeline
  └─ CoroutineXExecutor.CreateInstance()  ← global coroutine executor

EventBus — stateless; ready immediately (all static)
```

---

## Assembly Definitions

```
FoundationPlatform.Runtime.asmdef (Scripts/)
  References: Unity.InputSystem, Unity.TextMeshPro, UniTask

FoundationPlatform.Editor.asmdef (Editor/)
  References: FoundationPlatform.Runtime
```

All editor-only code lives under `Editor/` as of the 2026-07 pass (previously `Scripts/Messaging/EventBus/Editor/`, `Scripts/DebugX/Editor/`, `Scripts/CoroutineX/Editor/`, `Scripts/Identity/Editor/` were compiling into the Runtime asmdef).

---

## Key Design Decisions

- **Service locator (static) over DI** — `EventBus`, `DebugX`, `CoroutineXExecutor` are static; zero setup cost; acceptable for singletons that span entire app lifetime.
- **Struct-based zero-alloc logging** — `DebugXLogger` is a struct; `ShouldEmit()` pre-check before string building; 1–5 typed overloads avoid `params object[]` boxing.
- **Channel-based event routing** — `Identity`-tagged events; global fallback via `Identity.Global`; enables per-entity event scoping without subscriber proliferation.
- **DomainEvent gate** — `DomainEvent`s blocked outside Commit phase; prevents state mutations from validation or other forbidden phases.
- **Fragment data pattern** — All SO-based configs use `FragmentData<TDefinition, TPayload>` allowing shared SO definitions OR inline custom overrides; no duplication.
- **Observable<T>** — reference type (class); callbacks fire only on actual value change. It is a class, not a struct: a mutable struct with delegate fields silently drops subscribers when copied (return-by-value, pass-by-value), so it must be a reference type to be exposed safely via properties. Declare with an initializer, e.g. `= new Observable<T>()`.
- **`DebugXLogging` namespace kept as-is** — established in-house brand across the whole project (DebugX Console, log channels, etc); not folded into `FoundationPlatform.*` during the 2026-07 namespace consolidation.
