# DebugX — Architecture Audit

## Context

Scope: `Runtime/DebugX/` (logging core: pipeline, sinks, console store, parsing) and its editor
counterparts `Editor/DebugX/` (menu items), `Editor/Console/` (DebugX Console window — a bespoke
replacement for Unity's Console), and `Editor/Debugging/` (a separate, unrelated diagnostics
subsystem: `IEntityDebugSection`/`IWorldDebugSection` registries, `FrameworkDebuggerWindow<T>`,
`GameStateWindow`, `EntityDebuggerOverlay`, `DebugDrawKit`, `GizmoLayerSet`). 55 `.cs` files read in
full. Cross-checked against `docs/00-AgentGuide.md`, `docs/01-CorePrinciples.md`,
`docs/02-Libraries.md`, `docs/09-EditorHub.md`, `docs/13-AuthoringStandards.md`, and
`Documentation~/ARCHITECTURE.md`.

DebugX is correctly scoped as a Library/Service (per `docs/02-Libraries.md`): it never touches
authoritative simulation state, never writes gameplay facts, and does not sit on the action
pipeline. Most of the surface (pipeline, sinks, queue, console store) is solid, thread-safe,
fail-loud-on-sink-failure engineering. The findings below are concentrated in a few sharp spots:
a real player-build compile risk, a namespace/type naming collision that is already visibly costing
every consumer outside the `DebugX` namespace, a dead parallel logging implementation, and an
error-dedupe mechanism that can silently drop unrelated errors.

## Findings

### Execution Spine
No findings. DebugX is pure cross-cutting logging infrastructure; nothing in scope reads or writes
gameplay/simulation state, and nothing here participates in `intent → validate → execute → commit`.

### Data/Controller/View Boundary
No findings. DebugX is correctly a Service per `docs/01-CorePrinciples.md`'s role glossary
("cross-cutting utility... debug overlay. Must not bypass the pipeline for simulation writes").
`Editor/Debugging/`'s `IEntityDebugSection`/`IWorldDebugSection` registries match
`docs/09-EditorHub.md`'s documented seams exactly (`IEntityDebugSection` for per-object Scene View
state, `IWorldDebugSection` for `GameStateWindow`) — both are strictly read-only diagnostics.

### Ownership

- **Error** — `Runtime/DebugX/Logging/DebugLogger.cs`, `Runtime/DebugX/Logging/LoggerFactory.cs`,
  `Runtime/DebugX/Logging/IDebugLogger.cs`, `Runtime/DebugX/Logging/Configuration/LogConfig.cs`: a
  second, entirely dead logging path. `LoggerFactory.GetOrCreateLogger` / `DebugLogger` /
  `IDebugLogger` duplicate `DebugXBuilder`/`DebugXLogger`'s fluent API almost 1:1, but gate on a
  **separate** config object (`LogConfig.MinimumLevel` / `LogConfig._disabledChannels`) that is
  never touched by `LogPipeline.SetMinimumLevel` (what the DebugX Console settings page and
  `DebugXInitializer` actually configure). A project-wide grep of `Packages/` and `Assets/` shows
  `LoggerFactory`/`IDebugLogger`/`GetOrCreateLogger` referenced **only inside their own three
  files** — nothing in the codebase creates a logger through this path. `Documentation~/ARCHITECTURE.md`
  §"Logging (DebugX)" also only documents `DebugX.Logger`/`DebugX.Builder`, silently confirming this
  is unused. Why it matters: if anyone resurrects or copy-pastes from this path (it's `internal`, so
  discoverable only by reading the file), their minimum-level/channel toggles silently stop working
  because they're checking a config object nothing else writes to — a second, drifted source of
  truth for "is this log enabled."

- **Warning** — `Runtime/DebugX/DebugX.cs:196-232` (`LogArray<T>`): both overloads check
  `LogPipeline.ShouldEmit(...)` for the level gate, then call `UnityEngine.Debug.Log(sb.ToString())`
  **directly**, bypassing `LogPipeline.Emit`/sinks entirely. Every other method in `DebugX` (`Info`,
  `Warning`, `Error`, the `Logger`/`Builder` paths) constructs a `LogEvent` and routes it through the
  pipeline so it reaches `FileSink`/`JsonFileSink`/`EditorConsoleSink` consistently. `LogArray`
  quietly skips all of that — its output never lands in the structured JSON log or the log file, and
  in-editor it shows up in the DebugX Console tagged `Source=Unity` (via the `Application.logMessageReceivedThreaded`
  capture) rather than `Source=DebugX`, losing channel/property association. A second, inconsistent
  logging path inside the very API meant to be the single entry point.

- **Info** — A few raw `UnityEngine.Debug.Log`/`Debug.LogError` calls in `Editor/Debugging/` bypass
  DebugX: `Editor/Debugging/IEntityDebugSection.cs:90` and `IWorldDebugSection.cs:82` (section
  registry instantiation failure), `Editor/Debugging/FrameworkDebuggerWindow.cs:261`,
  `EntityDebuggerOverlay.cs:164`, `GameStateWindow.cs:209` (trivial "copied to clipboard"
  confirmations). Low severity — this is editor-tool UX feedback and bootstrap-error logging in a
  different diagnostics subsystem, not runtime/simulation logging — but per AGENTS.md's "Use DebugX"
  mandate these could route through `DebugX.Logger(LogChannels.Editor)` instead of raw `Debug.*`.
  (Internal DebugX-infrastructure files — `FileSink`, `JsonFileSink`, `LogQueue`, `FlushScheduler`,
  `SessionCounter`, `LogPipeline.EmitToSinks` — also call raw `UnityEngine.Debug.LogError` on their
  own failures; that is a deliberate and correct exception, since these ARE the sinks DebugX would
  otherwise recurse through.)

### Designer Surface Priority
No findings — good alignment. `Editor/Console/DebugXConsoleWindow.cs` is a full bespoke
`EditorWindow` (1941 lines), which is normally a red flag per `docs/13-AuthoringStandards.md`'s
"prefer Project/Hierarchy/Inspector over a new EditorWindow" — but this is explicitly the case the
task brief calls out as reasonable: it is a superset replacement for Unity's own Console window
(mirrors it, adds structured filtering/tabs/watch/export), which is exactly "Unity's own Console is
the natural home." `GameStateWindow`/`EntityDebuggerOverlay` likewise match the two documented
diagnostic surfaces in `docs/09-EditorHub.md` ("what's up with this object" → Scene View overlay,
"what's up with the game" → `Window ▸ Domain ▸ Game State`) rather than inventing a third. No
unique-inspector-rule violations observed (no asset-browser/path/name duplication).

### Redundancy/Simplification

- Covered above under Ownership: `DebugLogger`/`IDebugLogger`/`LoggerFactory`/`LogConfig` is dead
  code and should be deleted rather than left as a trap (see Fixes).
- **Info** — `Runtime/DebugX/Logging/Sinks/UnityConsoleSink.cs`'s doc comment: "the surviving half of
  the old ConsoleProSink, minus the CPAPI markers" — a legacy reference, harmless, but confirms this
  package has been through at least one prior consolidation; worth checking there's no other
  half-migrated leftover beyond the `LogConfig` path already flagged.

### Determinism
No findings requiring action. `LogEvent.FrameCount` and `ConsoleEntry.FrameCount` read
`UnityEngine.Time.frameCount`, and `ConsoleLogStore`/sinks stamp `DateTime.Now` — both are
diagnostic metadata only (log display/ordering), never read back into gameplay/validation logic, so
this does not violate `docs/11-Determinism.md`. No `UnityEngine.Random` usage anywhere in scope.

### Fail-Fast Compliance

- **Error** — `Runtime/DebugX/Logging/Core/ExplicitErrorDedupe.cs`: the message-based dedup can
  silently swallow unrelated future errors. `RegisterExplicitFailure` (called after every
  `DebugX.Error(messageTemplate, ...)` with **no** exception) extracts a message string from the
  logged properties and adds it to a `[ThreadStatic] HashSet<string> s_failureMessages` that is
  **never cleared** for the life of the thread/editor session. `ShouldSkipErrorLog` then checks any
  **later** exception's `.Message` (walking the `InnerException` chain) against that set — if it
  matches, the later `DebugX.Error(exception, ...)` call is dropped entirely (no sink sees it). The
  per-instance guard (`exception.Data[ExplicitlyLoggedKey]`) is sound — it only suppresses the exact
  same exception object being logged twice. But the string-based half is not scoped to "the same
  failure": two unrelated exceptions thrown minutes apart, in different subsystems, that merely
  happen to share message text (very plausible for generic messages like "not found" / "invalid
  state" / a repeated user-facing string) will cause the second, genuinely new error to be silently
  dropped from every sink — file, JSON, and console. This directly contradicts
  `docs/01-CorePrinciples.md`'s fail-fast rule ("do not silently substitute... stop with a clear
  error") applied to the logging framework's own error-reporting path. It is also an unbounded
  per-thread memory leak (the set only grows).
- **Info** — Everywhere else, fail-fast is handled well: `LogQueue`'s overflow drop-oldest is never
  silent (`ReportDrops` emits a synthetic warning with the drop count), sink failures are caught and
  reported via `UnityEngine.Debug.LogWarning`/`LogError` rather than swallowed
  (`LogPipeline.EmitToSinks`, `FileSink.Emit/FlushBuffer`, `JsonFileSink.Emit/FlushBuffer`), and
  `DebugX.Error(exception, message, ...)` overloads consistently preserve the full exception via
  `LogEvent.Exception` (message + inner-exception chain via `.ToString()` + caller context) rather
  than flattening it — good compliance with "preserve full diagnostic detail."

### Doc/Architecture Drift
- **Info** — `Documentation~/ARCHITECTURE.md` §"Logging (DebugX)" documents only `DebugX.Logger`/
  `DebugX.Builder` and the sink/pipeline architecture. It does not mention `LoggerFactory`/
  `IDebugLogger`/`DebugLogger`/`LogConfig` at all — consistent with (and indirect confirmation of)
  the dead-code finding above, not a contradiction. No drift found in the documented parts: the
  per-platform sink table, the `DebugXInitializer` bootstrap sequence, and the DebugX Console
  description all match the code as read.

### Codebase Gotchas

- **Error (build-risk, unverified — cannot run Unity to confirm)** — `Runtime/DebugX/DebugX.cs:1-7`:
  ```csharp
  using System.Collections.Generic;
  using System.Diagnostics;
  using System.Text;
  using UnityEditor;
  using UnityEngine;
  #if UNITY_EDITOR
  #endif
  ```
  `using UnityEditor;` is **not** guarded by `#if UNITY_EDITOR`, immediately followed by a stray,
  empty `#if UNITY_EDITOR` / `#endif` block (dead code — looks like a leftover from a refactor where
  the guard was moved but this using directive was left outside it). `Runtime/FoundationPlatform.Runtime.asmdef`
  has `"includePlatforms": []` (compiled for every platform, including player builds), and none of
  its `references` list `UnityEditor`. Every actual use of `UnityEditor` types in this file
  (`Handles`, `SceneView` in `DrawString`) is correctly wrapped in `#if UNITY_EDITOR` — but the
  `using` directive itself is not, and an unresolvable `using` is a compile error (`CS0246`)
  independent of whether any imported type is actually referenced. This compiles fine inside the
  Editor (where `UnityEditor.dll` is always available), which is exactly why it would go unnoticed
  during normal development — the failure only surfaces when Unity recompiles scripts for an actual
  player build target. This is the same class of trap as the documented `Debug`/`GameEngineCore.Debug`
  namespace collision in `docs/00-AgentGuide.md` §3: something that is silently correct in the editor
  and silently wrong at build time.

- **Warning** — The `DebugX` **class** shares its simple name with its enclosing **namespace**
  (`namespace AetherNexus.FoundationPlatform.DebugX { public static class DebugX { ... } }`,
  `Runtime/DebugX/DebugX.cs:9-11`). This is the same category of trap `docs/00-AgentGuide.md` §3
  already documents for `Debug`/`GameEngineCore.Debug` (a nested namespace shadowing an unqualified
  identifier), just not yet written down for this case. Confirmed already costing real code: from any
  namespace nested directly under `AetherNexus.FoundationPlatform` other than `.DebugX` itself —
  e.g. `AetherNexus.FoundationPlatform` (root) or `AetherNexus.FoundationPlatform.Behaviours` — an
  unqualified `DebugX.Logger(...)` / `DebugX.Builder(...)` resolves to the **namespace**, not the
  class, because C# namespace lookup finds the sibling/nested namespace via the enclosing scope
  before it considers the `using`-imported type of the same simple name. Consumers are forced into
  the awkward double-qualified `DebugX.DebugX.Logger(...)` to compile:
  - `Runtime/Patterns/SingletonBehaviour.cs:67,103,162,199` (namespace `AetherNexus.FoundationPlatform`) —
    `DebugX.DebugX.Logger(LogChannels.DevTools).Error(...)` / `.Info(...)`.
  - `Runtime/Behaviours/AreaSpawner.cs:86,115,197,209,236,264,320` (namespace
    `AetherNexus.FoundationPlatform.Behaviours`) — `FoundationPlatform.DebugX.DebugX.Builder(...)`.

  Every file living *inside* the `AetherNexus.FoundationPlatform.DebugX.*` namespace tree (all of
  `Editor/Console/`, `Editor/DebugX/`) is unaffected and uses the clean `DebugX.PrefKeyEditorMinLevel`
  / `DebugX.CaptureFullStackTraces` form — so the trap is invisible to anyone working inside the
  DebugX package itself, and only bites every *consumer* elsewhere in the codebase (i.e. everywhere
  "Use DebugX" actually applies). Worth documenting explicitly in `docs/00-AgentGuide.md` §3 next to
  the `Debug`/`GameEngineCore.Debug` entry, since it is the same failure mode.

- **Warning** — Runtime reflection used unconditionally (not editor-gated), in tension with
  AGENTS.md's "No runtime reflection (AOT target) — reflection only in editor tools":
  - `Runtime/DebugX/Logging/Core/CallerInfoHelper.cs`: `GetCallerInfo()` / `FindOptimalSkipCount` /
    `IsInternalMethod` / `GetCallingMethod` use `System.Diagnostics.StackTrace` and
    `System.Reflection.MethodBase` on **every single DebugX log call, in every build** (only the
    Unity-stack-extractor fallback at lines 177-190 and 264-304 is `#if UNITY_EDITOR`-gated; the core
    stack-walk/reflection path is not). Contrast with `Runtime/DebugX/Logging/Console/LogEntriesBridge.cs`
    in the same package, which does the same kind of reflection but correctly confines all of it
    behind `#if UNITY_EDITOR` (and documents exactly why: "Lives in the runtime assembly
    (editor-guarded)...").
  - `Runtime/DebugX/Logging/Parsing/MessageTemplateParser.cs:113`:
    `type.GetMethod("ToString", System.Type.EmptyTypes)` — a reflection call per logged non-primitive
    property value, also unguarded, in the Runtime assembly.
  This is a design tension worth a conscious decision (perf on AOT/IL2CPP targets, plus the stated
  project policy) rather than a silent default — not necessarily wrong (caller-info without
  `[CallerMemberName]` genuinely needs some form of this), but it should be a deliberate, documented
  trade-off rather than something a reviewer has to discover by reading the file.

- **Info** — Widespread public-API constructors with default-valued parameters, in tension with
  AGENTS.md's "No optional parameters": `LogEvent(...)` (`Core/LogEvent.cs:27-37`, 6 defaulted
  params), `LogProperty(...)` (`Core/LogProperty.cs:22`), `FileSink(...)` (`Sinks/FileSink.cs:24-25`,
  4 defaulted params), `JsonFileSink(...)` (`Sinks/JsonFileSink.cs:27-28`, 5 defaulted params),
  `UnityConsoleSink(...)` (`Sinks/UnityConsoleSink.cs:21`), `EditorConsoleSink(...)`
  (`Console/EditorConsoleSink.cs:15`). Consistent pattern across the whole public surface rather than
  an isolated slip, so likely a deliberate convenience choice for this package specifically — flagged
  for consistency with the stated project-wide rule, not as a functional defect.
- **No findings** for the other §3 gotchas checked: no `??`/`??=` on any `UnityEngine.Object` in
  scope (all `??` usages are on strings/collections/plain C# refs); no struct with an instance field
  initializer or an explicit constructor that skips a field (`LogChannel`, `CallerInfo`, `LogEvent`,
  `LogProperty`, `DebugXLogger`, `RowRef`, `QueuedLog`, `GizmoLayerSet.Layer` all check out); no
  `OnValidate`/`OnAfterDeserialize`/`OnBeforeSerialize` anywhere in this scope.

## Fixes

None applied — this audit is read-only per instructions (only this AUDIT.md was written). Suggested
priority order for a follow-up task:

1. Guard `using UnityEditor;` in `Runtime/DebugX/DebugX.cs` behind `#if UNITY_EDITOR` (move the
   `Handles`/`SceneView`-using methods' using requirement into the existing per-method `#if
   UNITY_EDITOR` blocks, or wrap the whole using line) and delete the adjacent stray empty `#if
   UNITY_EDITOR`/`#endif`. Verify with an actual player build, not just an editor recompile.
2. Delete `DebugLogger.cs`, `IDebugLogger.cs`, `LoggerFactory.cs`, and `LogConfig.cs` (confirmed
   unreferenced outside themselves), or if the intent was ever to expose this as a documented
   alternative API, wire it onto `LogPipeline` instead of the orphaned `LogConfig` gate.
3. Fix or remove the string-based half of `ExplicitErrorDedupe` (drop the `s_failureMessages`
   text-match path; keep only the per-instance `exception.Data` marker, which is sound), and/or scope
   it with a bound/expiry instead of an unbounded thread-static set.
4. Route `DebugX.LogArray<T>` through `LogPipeline.Emit` like every other entry point, or document
   why it deliberately bypasses sinks.
5. Add the `DebugX` class/namespace name collision to `docs/00-AgentGuide.md` §3 next to the existing
   `Debug`/`GameEngineCore.Debug` entry, since it is the same failure mode and already has two
   confirmed consumer workarounds in the codebase.

## Cross-references

- `D:\UnityProjects\HOMAM\docs\00-AgentGuide.md` §3 (`Debug` namespace collision gotcha — same
  failure mode as the `DebugX`/`DebugX` finding above; fail-fast rule; no-runtime-reflection rule)
- `D:\UnityProjects\HOMAM\docs\01-CorePrinciples.md` (fail-fast errors — `ExplicitErrorDedupe` finding)
- `D:\UnityProjects\HOMAM\docs\02-Libraries.md` (Library/Service role — DebugX correctly scoped)
- `D:\UnityProjects\HOMAM\docs\09-EditorHub.md` (diagnostic surfaces by scope —
  `IEntityDebugSection`/`IWorldDebugSection` alignment)
- `D:\UnityProjects\HOMAM\docs\13-AuthoringStandards.md` (editor-window-as-last-resort — DebugX
  Console justified as Unity Console's natural extension)
- `D:\UnityProjects\HOMAM\Packages\com.aethernexus.foundationplatform\Documentation~\ARCHITECTURE.md`
  (Logging (DebugX) section — matches code except for the undocumented dead `LoggerFactory` path)
- Sibling audit for a different subsystem in this package, same pattern:
  `D:\UnityProjects\HOMAM\Packages\com.aethernexus.foundationplatform\Editor\ProjectWindowX\AUDIT.md`
