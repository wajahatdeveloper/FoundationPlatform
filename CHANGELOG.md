# Changelog

All notable changes to this package are documented here. Format follows [Keep a Changelog](https://keepachangelog.com/en/1.0.0/); versioning follows [Semantic Versioning](https://semver.org/).

## [1.0.0] - 2026-07-07

### Changed
- Moved package out of `Assets/Frameworks/FoundationPlatform/` into embedded UPM package `Packages/com.homam.foundationplatform/`
- Renamed `Scripts/` → `Runtime/` to match standard UPM layout
- Unified namespaces under `FoundationPlatform.*` (was fragmented across `FoundationPlatform.*`, `Framework.Inspector.*`, and partially `DebugXLogging.*`)
- `UniTask` dependency moved from `Assets/Plugins/UniTask` to OpenUPM scoped registry package (`com.cysharp.unitask` 2.3.3)

### Added
- `README.md`, `CHANGELOG.md`, `LICENSE.md`, `Documentation~/ARCHITECTURE.md`

## [Unreleased] - pre-2026-07 (Assets/Frameworks era)

### Changed
- Assimilated 5 vendored third-party namespaces into `FoundationPlatform.*` (`SVerdeTools.FastGizmos`, `BayatGames.Utilities.Editor`, `OpenScripts.EventBus.Editor`, `Essentials.PresetAutomation`, `CodeStage.PackageToFolder`)
- Moved all editor-only code out of the Runtime asmdef into `Editor/`

### Removed
- 6 dead-code behaviours with zero prefab/scene/asset GUID references: `GenericSelection`, `ParentScaleOnly`, `MouseCentering`, `CountdownDisplay`, `TextTokenReplacer`, `ButtonClickSoundCustomizer`

### Known issues
- `Editor/Console/` and `Editor/DebugX/DebugXMenuItems.cs` still use `DebugXLogging.*` namespace while `Runtime/DebugX/` uses `FoundationPlatform.DebugX` — pending unification
- `Editor/Tools/PrefabLightmapGenerator/` still vendored under `OpenScripts` / `OpenScripts.Editor` namespace — not yet assimilated
- No test suite (`Tests/` asmdef) yet
