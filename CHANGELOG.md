# Changelog

All notable changes to this package are documented here. Format follows [Keep a Changelog](https://keepachangelog.com/en/1.0.0/); versioning follows [Semantic Versioning](https://semver.org/).

## [Unreleased]

### Added

- **`RandomX` gains a `UnityEngine.Random`-shaped facade** (`Extensions/Random/RandomX.Unity.cs`): `value`, `Range`, `insideUnitCircle`, `insideUnitSphere`, `onUnitSphere`, `rotation`, `rotationUniform`, `ColorHSV`, plus `Stream(name)` and `CaptureState`/`RestoreState`. Routed through an installed `RandomX.Provider`; throws with a fix message when none is installed (no silent fallback to a non-deterministic source). Member names match Unity's exactly so adoption is a mechanical `Random.` → `RandomX.` substitution
- **`IRandomStreamSource : IRandomProvider`** (`Runtime/Behaviours/`) — adds independent named streams and opaque state capture/restore, so the package can expose those without knowing any particular RNG implementation
- **`IWorldDebugSection` + `GameStateWindow`** (`Editor/Debugging/`) — world-scope counterpart to the per-entity `IEntityDebugSection` overlay: a dockable shell with TypeCache auto-discovery, live repaint, `DebugDrawKit`-backed Copy Info, and a handoff button to the Scene View overlay. Ships no gameplay sections; installed gameplay packages contribute them

### Changed

- `RandomX` is now `partial` so the collection helpers and the new facade share one type name — gameplay code should never have to choose between two "random" types
- Editor windows (`EventPublishHistoryWindow`, `ActiveSubscriptionsWindow`, `SubscriptionHistoryWindow`, `AnimationTestBenchWindow`, `AutoBinderWindow`, `SceneSwitcherWindow`, `ScriptGeneratorWindow`, `StaleComponentWindow`, `PresetAutomationWindow`, `GameStateWindow`) retrofitted onto the `GuiKit` shared chrome instead of ad-hoc `HelpBox`/`Foldout`/`GUILayout.Toolbar` calls
- `PresetAutomationWindow` now actually uses its `_scroll` field — Filters and Folder Priorities are dynamic-length lists that were previously drawn with no scroll view, clipping content taller than the window with no way to reach it

## [1.0.0] - 2026-07-14

### Added

- First public Asset Store UPM release of Foundation Platform (`com.aethernexus.foundationplatform`)
- EventBus, DebugX, CoroutineX, TweenX / Feedbacks, patterns, Identity, animation tooling, extensions
- Framework Inspector and related Editor tooling
- Package Manager sample: **EventBus + CoroutineX**
- User docs: README, Documentation index, Architecture, TweenX, Framework Inspector guides

### Notes

- Requires Unity **6000.3.10f1+**; URP recommended; Input System + uGUI
- Embeds Cysharp **UniTask 2.5.11** (MIT) — see Third-Party Notices.txt; do not install UniTask separately
- Fast Enter Play Mode (Domain Reload off) is **not supported**
- Publisher: [AetherNexus](https://aethernexus.online) · Support: wajahatdeveloperqs@gmail.com
