# Changelog

All notable changes to this package are documented here. Format follows [Keep a Changelog](https://keepachangelog.com/en/1.0.0/); versioning follows [Semantic Versioning](https://semver.org/).

## [Unreleased]

### Added

- **`[ContentHome]`** (`AetherNexus.FoundationPlatform.Attributes.ContentHomeAttribute`): declares where an authored type's assets belong — folder patterns relative to `Assets/Content` (`*`, `**`, leading `***`), inherited by subclasses. It replaces `PackageIntegrationManifest` type-mapping rows as the source of truth. `ContentHomes` (editor) is the one resolver. `AnimationSet`, `AnimationSetValidationProfile`, `LocomotionBlendProfile` and `LightmapConfiguration` carry their former manifest patterns.
- **`ProjectContentConfig`** (editor): the single per-project content layout asset — content areas, combined areas, Shared/Global subfolders, required roots, exempt folders, auto-move-on-import flag.

### Removed

- `DataFolderMappingConfig`, `DataFolderExemptionMarker` (exempt folders are now a `ProjectContentConfig` list), `DataFolderMappingExemptionResolver`, `HierarchyPathPolicy.TryClassify` and the `HierarchyRoot` / `HierarchyBucket` / `DataFolderMappingPathClassification` enums. **Breaking**: move any exemption-marker folders into `ProjectContentConfig ▸ Exempt Folders`.

### Changed

- **Logging namespace renamed** `AetherNexus.FoundationPlatform.DebugX` -> `AetherNexus.FoundationPlatform.Logging` (and `.DebugX.ConsoleView.Editor` -> `.Logging.ConsoleView.Editor`); the `DebugX` class keeps its name. The old namespace shared the class's simple name, so from sibling namespaces `DebugX.Logger(...)` resolved to the namespace and needed `DebugX.DebugX.Logger(...)` or a local alias. **Breaking for consumers**: replace `using AetherNexus.FoundationPlatform.DebugX;` with `using AetherNexus.FoundationPlatform.Logging;` and drop any `DebugX.DebugX.` qualification or `using DebugX = DebugX.DebugX;` alias.
- Agent tools: `platform-agenttools-export-params` and `AgentToolParamExporter` moved into the bridge package (`com.aethernexus.aibridge`, tool `editor-export-mcp-params`, runs after every compile). The agent-tools assembly is now gated on `com.aethernexus.aibridge`.

### Removed

- `Tools/Platform/Agent Tools/Export MCP Params` menu (now **Tools > AI Bridge > Export MCP Params**)
- Publisher-only editor tooling moved to the monorepo package `com.aethernexus.publishertools` (not shipped): `Editor/DesignerIcons/`, `Editor/ComponentMenus/`, `Editor/CreateMenus/`, `Editor/DesignerSurfaces/` and their **Tools > Platform > Icons / Component Menus / Create Menus** and **Tools > Linting > Report Missing …** menus. `[DesignerIcon]` and the icon PNGs stay here.

- **Cysharp UniTask is no longer embedded** (`Runtime/ThirdParty/UniTask/`, `Editor/ThirdParty/UniTask/`, the `UniTask` and `UniTask.Editor` assemblies, and the UniTask Tracker window). Async code across the AetherNexus packages now targets `UnityEngine.Awaitable`. **Breaking for consumers** that referenced the `UniTask` assembly or typed against `UniTask` / `UniTask<T>` in overrides of package APIs — change those signatures to `Awaitable` / `Awaitable<T>` and drop the assembly reference. Fire-and-forget `UniTaskVoid` + `.Forget()` becomes `async void` with an explicit `try`/`catch`, since `Awaitable` has no unobserved-exception routing
- Legacy `UnityEngine.Input` fallbacks in shipped components: every polling site now goes through the Input System package, which the package already required

### Added

- **Agent tools** (`Editor/AgentTools/`) — the designer feature index made drivable by coding agents through the Unity AI Bridge: `platform-capability-list` (every `[DesignerFeature]` / owned `[MenuItem]` with intent-first title, blurb and keywords), `platform-capability-invoke` (runs a catalogued entry through its own action and returns what Unity logged), and `platform-agenttools-export-params` (regenerates the MCP schema files from the live tool registry, preserving hand-curated text via a `_generated.json` baseline). New public `FeatureCatalogApi` over the previously internal `FeatureCatalog`. Own assembly gated on `com.aibridge.unity`, so the package ships bridge-free. See [Editor/AgentTools/README.md](Editor/AgentTools/README.md)
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
