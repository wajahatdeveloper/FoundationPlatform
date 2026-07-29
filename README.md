# Foundation Platform

Free Unity foundation layer for the **AetherNexus** toolkit (`com.aethernexus.foundationplatform`). Messaging, logging, coroutines, tweens, patterns, and editor tooling — with no dependency on GameEngineCore or other gameplay packages.

**Publisher:** [AetherNexus](https://aethernexus.online) · **Support:** wajahatdeveloperqs@gmail.com  
**Unity:** 6000.3.10f1+ · **URP** recommended · **License:** [MIT](LICENSE.md)  
**Third-party:** Cysharp UniTask (MIT) — see [Third-Party Notices.txt](Third-Party%20Notices.txt)

## What's inside

| Area | What you get |
|------|----------------|
| **EventBus** | Pub/sub with `Identity` channels and priority subscribers |
| **DebugX** | Structured logging + in-Editor console |
| **CoroutineX** | Owned coroutine lifecycle (`Stop` / `Reset` / `Rerun`) |
| **TweenX** | Tweens + Feedback player (no DOTween required) |
| **Patterns** | Singletons, `FragmentData`, reactive `Observable` types |
| **Identity** | String-backed entity / channel ids |
| **Animation** | `AnimationSet`, locomotion blend profiles, playable tooling |
| **RandomX** | `UnityEngine.Random`'s API over a pluggable provider — named streams, state save/load |
| **Extensions** | Broad GameObject / math / physics / UI helpers |
| **Editor** | Framework Inspector, DebugX Console, Event Bus window, Entity Debugger overlay, Game State window, validation & utilities |

Docs index: [Documentation~/index.md](Documentation~/index.md)

## Install (Asset Store UPM)

1. Package Manager → **My Assets** → **Foundation Platform** → Download / Import.
2. Project Settings → Player → **Active Input Handling** = Input System Package **or** Both (`com.unity.inputsystem`).
3. Confirm **uGUI** is present (`com.unity.ugui` — included in typical URP templates).
4. Optional: Package Manager → Samples → import **EventBus + CoroutineX**.

**Do not** install Cysharp UniTask separately. This package embeds UniTask **2.5.11**; a second UniTask package collides on the `UniTask` assembly name.

## Dependencies

| Dependency | How provided |
|------------|----------------|
| UniTask 2.5.11 (MIT) | Embedded under `Runtime/ThirdParty/UniTask` and `Editor/ThirdParty/UniTask` |
| `com.unity.inputsystem` | Declared in `package.json` |
| `com.unity.ugui` | Declared in `package.json` |

## Quick usage

```csharp
using AetherNexus.FoundationPlatform.DebugX;

EventBus.Subscribe<MyEvent>(OnMyEvent, priority: 0);
EventBus.Publish(new MyEvent(...));

DebugX.Logger(LogChannels.DevTools).Info("threat found: {target}", targetId);

var handle = CoroutineX.Run(MyRoutine(), owner: gameObject);
yield return handle.WaitForComplete();

transform.TweenMove(target, 1f).SetEase(Ease.OutBack);

float roll = RandomX.value;              // deterministic when a provider is installed
var loot = RandomX.Stream("loot");       // independent sequence
```

`EventBus`, `CoroutineX`, and tween extension methods are in the **global** namespace. Logging types live under `AetherNexus.FoundationPlatform.DebugX`; `RandomX` under `AetherNexus.FoundationPlatform.Extensions`.

### RandomX needs a provider

`RandomX` mirrors `UnityEngine.Random`'s API — `value`, `Range`, `insideUnitSphere`, `rotation`, `ColorHSV` — but routes through an installed `RandomX.Provider`, so a game can make the same calls deterministic without changing call sites. With no provider it **throws** rather than falling back to a non-deterministic source; that silence is the bug it exists to prevent.

Game Engine Core installs one during startup. Standalone, install your own:

```csharp
RandomX.Provider = myProvider;   // IRandomProvider, or IRandomStreamSource for streams + save/load
```

## Assemblies

| Assembly | Role |
|----------|------|
| `UniTask` | Embedded Cysharp UniTask |
| `FoundationPlatform.Runtime` | Runtime APIs |
| `FoundationPlatform.Editor` | Editor tooling |
| `UniTask.Editor` | UniTask Tracker window |

## Package Integration Manifest

`PackageIntegrationManifest.asset` registers this package with **GameEngineCore Central Authoring** when that product is installed. It is optional metadata for the wider AetherNexus hub — not required for EventBus, DebugX, CoroutineX, or TweenX.

## Compatibility

- **Unity** 6000.3.10f1+
- **URP** recommended (Unity 6 default)
- **Fast Enter Play Mode** (Domain Reload disabled): **not supported**. Keep Domain Reload enabled.
- Verified clean import on an empty URP **6000.3.10f1** project

## Samples

Import **EventBus + CoroutineX** from Package Manager Samples. Details: [SAMPLES.md](SAMPLES.md)

## Support

- Website: [aethernexus.online](https://aethernexus.online)
- Email: wajahatdeveloperqs@gmail.com
- Changes: [CHANGELOG.md](CHANGELOG.md)

## Version

**1.0.0** — public API; breaking changes bump MAJOR.
