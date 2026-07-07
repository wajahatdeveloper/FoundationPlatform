# Foundation Platform

Core foundation layer for the HOMAM project. Leaf dependency for all gameplay frameworks — no dependencies on other HOMAM packages.

## What's inside

- **EventBus** — pub/sub messaging with channel routing (`Identity`), priority-sorted subscribers, domain-event publish gate
- **DebugX** — structured, zero-alloc-path logging with channels, sinks (Editor console / Unity console / file / JSON), and an in-editor console window
- **CoroutineX** — coroutine lifecycle with ownership transfer, `Stop`/`Reset`/`Rerun`, and async-awaitable wait points
- **Patterns** — `SingletonBehaviour<T>`, `PersistentSingletonBehaviour<T>`, `Singleton<T>`, `FragmentData<TDefinition, TPayload>` (SO-or-inline config)
- **Reactive types** — `Observable<T>`, `ObservableList<T>`, `MaybeMonad<T>`, `OptionalValue<T>` / `OptionalReference<T>`
- **Identity** — string-backed entity/channel id (`Identity`, `IIdentity`, `IdentityComponent`)
- **Animation** — `AnimationSet`, `LocomotionBlendProfile`, `PlayableGraphBridge` (Animancer-adjacent), animation-set editor tooling
- **Gizmos** — performant scene-view gizmo drawing API
- **Extensions** — ~180 extension methods (GameObject/Component, collections, math, physics, rendering, UI, storage)
- **Editor tooling** — Framework Inspector (reflection-driven inspector engine), UI Validation, Preset Automation, DebugX Console window, Scene Switcher

See [Documentation~/ARCHITECTURE.md](Documentation~/ARCHITECTURE.md) for namespace map, class tables, and design rationale.

## Install

Embedded package — already present at `Packages/com.homam.foundationplatform/` in this repo. To reuse elsewhere, copy the folder into another project's `Packages/` directory or reference it as a local/git UPM dependency.

## Dependencies

- `com.cysharp.unitask` 2.3.3
- `com.unity.inputsystem` 1.18.0
- `com.unity.ugui` 2.0.0

## Quick usage

```csharp
// Event bus
EventBus.Subscribe<MyEvent>(OnMyEvent, priority: 0);
EventBus.Publish(new MyEvent(...));

// Logging (zero-alloc path)
DebugX.Logger(LogChannels.AI).Info("threat found: {target}", targetId);

// Coroutine lifecycle
var handle = CoroutineX.Run(MyRoutine(), owner: gameObject);
await handle.WaitForComplete();
```

## Assemblies

| Assembly | Folder | Notes |
|---|---|---|
| `FoundationPlatform.Runtime` | `Runtime/` | references Unity.InputSystem, Unity.TextMeshPro, UniTask |
| `FoundationPlatform.Editor` | `Editor/` | editor-only, references `FoundationPlatform.Runtime` |

## Status

Active development — no test suite yet, API may shift. See [CHANGELOG.md](CHANGELOG.md).
