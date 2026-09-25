# FoundationPlatform — agent guide

Leaf package: messaging, logging, extensions, inspector and editor utilities. No gameplay
authority, no dependency on GameEngineCore. Symbol detail:
[Documentation~/ARCHITECTURE.md](Documentation~/ARCHITECTURE.md).

## Entry points

| Need | Use |
|------|-----|
| Decoupled messages | `EventBus` (global namespace); event types derive from `DomainEvent`; typed channels via the event-channel generator |
| Logging | `DebugX.Logger(channel)` / `DebugX.Builder(channel)` in `AetherNexus.FoundationPlatform.Logging`; channels in `LogChannels` |
| Seeded random inside this package | `RandomX` (`Extensions/Random/`) or an `IRandomProvider` parameter; the game engine installs the provider |
| Coroutines / tweens | `CoroutineX`, `TweenX` (no DOTween) |
| Inspector attributes | AetherInspector (`[Required]`, `[ValidateInput]`, `[Button]`, …); demo: **Window > Diagnostics > AetherInspector Demo** |
| Unity-null-safe lookups | `GetComponentInSelfOrParents` / `GetComponentInSelfOrChildren` |
| Stable IDs | `Identity` (string-backed; its hash is not collision-free — never use it as a key surrogate) |
| Authoring validation | implement `IAuthoringValidator` (found via `TypeCache`, no registration); emit `AuthoringIssue` |
| Designer feature index | tag menu items with `[DesignerFeature]`; agents list them with `platform-capability-list` |

## Agent tools

`platform-capability-list`, `platform-capability-invoke`, `render-preview`, `render-clip-strip`,
`render-compare`, `anim-set-report`, `anim-clip-report`. Compiled only when
`com.aethernexus.aibridge` is installed. Detail: [Editor/AgentTools/README.md](Editor/AgentTools/README.md).

## Hard rules

- Cannot reference `GameEngineCore.*`. Determinism here goes through `RandomX` / `IRandomProvider`,
  never `UnityEngine.Random` and never saving/restoring its global state.
- `DebugX` caller-info reflection is a deliberate, documented carve-out
  ([KNOWN-ISSUES-DebugX-Reflection.md](Documentation~/KNOWN-ISSUES-DebugX-Reflection.md)); do not strip it without a replacement.
- No `??` on `UnityEngine.Object`; resolve refs in `Awake`/`OnEnable`, not by writing serialized fields in `OnValidate`.
