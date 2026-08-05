# DebugX — Architecture Audit (Re-Audit)

## Re-Audit Context

This is a follow-up pass over the original audit below, after fixes were applied. Method: read the
original findings, then verified each against current source (`git diff d714482 HEAD -- Runtime/DebugX/
Editor/Console/ Editor/Debugging/ ...` plus direct file reads of the current tree; some fixes turned
out to already be baked into `d714482` itself — the "Audit" commit that is this diff's baseline — so a
few items below are confirmed fixed even though they don't appear as changed lines between `d714482`
and `HEAD`).

Headline result: **5 of 6 concrete defects from the original audit are fixed** (dead code deleted,
`ExplicitErrorDedupe` string-match path removed, `using UnityEditor;` guarded, the `DebugX` class/
namespace collision now documented in `docs/00-AgentGuide.md` §3, and — beyond what was asked — the
"widespread optional parameters" Info finding is now resolved for every constructor originally cited).
The one substantive item still open is `DebugX.LogArray<T>` bypassing `LogPipeline.Emit`. Separately,
this diff range applied a repo-wide mechanical fix (converting `method(..., T x = default)` into
`method(..., T x)` + a new no-default overload, to satisfy AGENTS.md's "no optional parameters" rule)
across ~10 methods in this subsystem (`DebugX.DrawString`/`DrawArrowRay`, `LogQueue.Enqueue`,
`LogPipeline.ProcessLogEvent`, `LogMessageTruncation.TruncateFromBottom`, plus ~15 more files package-wide)
— spot-checked and correct (see "Mechanical fix" below).

## Original Context

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

## Mechanical fix (this diff range): optional-parameter cleanup — verified correct

`d714482..HEAD` converts every `Method(..., T param = default)` in this subsystem's files into
`Method(..., T param)` + a new `Method(...)` overload that calls the full method with the exact
default baked in. Confirmed correct in this subsystem:

- `Runtime/DebugX/DebugX.cs`: `DrawString(text, worldPos, colour=null)` → `DrawString(text, worldPos)`
  calls `DrawString(text, worldPos, null)`; `DrawArrowRay(pos, dir, headLength=0.25f, headAngle=20f)` →
  new zero-extra-arg overload calls `DrawArrowRay(pos, dir, 0.25f, 20.0f)` — both defaults preserved
  exactly.
- `Runtime/DebugX/Logging/Core/LogMessageTruncation.cs`: `TruncateFromBottom(value, maxLength)` new
  1-arg overload calls `TruncateFromBottom(value, MaxFixedStringLength)` — correct.
- `Runtime/DebugX/Logging/Core/LogQueue.cs`: `Enqueue(logEvent)` calls `Enqueue(logEvent, false)` —
  correct, and the queue's only other caller pattern (explicit `true`/`false`) still compiles.
- `Runtime/DebugX/Logging/Pipeline/LogPipeline.cs`: `ProcessLogEvent(logEvent)` calls
  `ProcessLogEvent(logEvent, false)`, and correctly re-applies `[UnityEngine.HideInCallstack]` to the
  new overload too (not just the original) — attribute wasn't dropped.
- Also verified the same pattern in `Editor/Debugging/DebugDrawKit.cs` (3 methods, incl. the
  two-defaulted-param `Bar(string,float,string,bool=false,Color?=null)` split into three overloads
  covering all three call arities) and `Editor/Debugging/GizmoLayerSet.cs`/`EventLogView.cs` — all
  correct.
- Cross-repo call-site check: grepped the whole `D:\UnityProjects\HOMAM` tree for every affected
  method's call sites (`DebugDrawKit.Bar(`, `DrawDirectionalLine(`, `DrawTinyArrow(`, etc., including
  in `com.aethernexus.gameplayabilitysystem`, `com.aethernexus.gameframework`, `Editor/AnimGraph/`,
  `Editor/TweenX/`). No call site anywhere supplies a partial arg count that would now fail to resolve
  — every call site either matches a still-existing full-arity overload or one of the newly added
  reduced-arity overloads. No compile breaks found.

Not individually itemized further per instructions; treat as verified correct across the ~25-file
diff for this pattern.

## Findings

### Execution Spine
No findings. Unchanged — still pure cross-cutting logging infrastructure.

### Data/Controller/View Boundary
No findings. Unchanged — correctly a Service per `docs/01-CorePrinciples.md`.

### Ownership

- **RESOLVED** — `Runtime/DebugX/Logging/DebugLogger.cs`, `LoggerFactory.cs`, `IDebugLogger.cs`,
  `Configuration/LogConfig.cs`: **deleted**. Confirmed via `git show d714482 --stat` — these four files
  (296 lines) were removed in the `d714482` "Audit" commit itself (the baseline this re-audit diffs
  against), and a repo-wide glob for `DebugLogger.cs`/`LoggerFactory.cs`/`IDebugLogger.cs`/`LogConfig.cs`
  under this package today returns nothing. The dead second logging path (separate `LogConfig` gate,
  never wired to `LogPipeline.SetMinimumLevel`) no longer exists to be resurrected or copy-pasted from.

- **NOT FIXED** — `Runtime/DebugX/DebugX.cs:194-230` (`LogArray<T>`, both overloads): still gate on
  `LogPipeline.ShouldEmit(...)` and then call `UnityEngine.Debug.Log(sb.ToString())` **directly**,
  bypassing `LogPipeline.Emit`/sinks. Verified unchanged in current source — same behavior as
  originally reported. Output still never reaches `FileSink`/`JsonFileSink`/`EditorConsoleSink`, and
  in-editor still surfaces as `Source=Unity` rather than `Source=DebugX`.

- **Info, unchanged** — Raw `UnityEngine.Debug.Log`/`Debug.LogError` calls in `Editor/Debugging/` still
  bypass DebugX. Re-verified present at the same sites: `IEntityDebugSection.cs:90`,
  `IWorldDebugSection.cs:82`, `FrameworkDebuggerWindow.cs:261`, `EntityDebuggerOverlay.cs:164`,
  `GameStateWindow.cs:209`. Still low severity (editor-tool UX feedback / bootstrap-error logging), not
  addressed, not blocking.

### Designer Surface Priority
No findings — unchanged, still good alignment.

### Redundancy/Simplification

- **RESOLVED** (see Ownership) — dead `DebugLogger`/`IDebugLogger`/`LoggerFactory`/`LogConfig` path
  deleted rather than left as a trap.
- **Info, unchanged** — `UnityConsoleSink.cs`'s "surviving half of the old ConsoleProSink" doc comment
  is still present; harmless, no other half-migrated leftover found beyond the now-deleted `LogConfig`
  path.
- **New (bonus, beyond original scope)** — the "widespread optional parameters" Codebase Gotchas
  finding below (constructors) is now also resolved; noted there rather than duplicated here.

### Determinism
No findings requiring action. Unchanged.

### Fail-Fast Compliance

- **RESOLVED** — `Runtime/DebugX/Logging/Core/ExplicitErrorDedupe.cs`: the unbounded, message-based
  dedup path is **removed**. Current file (25 lines) contains only the per-instance
  `exception.Data[ExplicitlyLoggedKey]` check via `ShouldSkipErrorLog`, walking the `InnerException`
  chain and comparing the marker — exactly the "sound" half the original audit said to keep. The
  `[ThreadStatic] HashSet<string> s_failureMessages` / `RegisterExplicitFailure` string-match path and
  its unbounded-memory-leak risk are gone entirely. The file's own doc comment now states this
  explicitly: *"Scoped strictly to the exact same exception instance/chain — not a message-text match,
  which would risk dropping unrelated future errors that merely share message text."* This directly
  implements Fix #3 from the original audit.
- **Info, unchanged** — Everywhere else, fail-fast is still handled well (queue overflow reporting,
  sink failure logging, exception preservation).

### Doc/Architecture Drift
- **RESOLVED (moot)** — `Documentation~/ARCHITECTURE.md` §"Logging (DebugX)" still doesn't mention
  `LoggerFactory`/`IDebugLogger`/`DebugLogger`/`LogConfig`, but this is no longer a drift since those
  types no longer exist in the codebase. No action needed.

### Codebase Gotchas

- **RESOLVED** — `Runtime/DebugX/DebugX.cs:1-7`: `using UnityEditor;` is now correctly guarded:
  ```csharp
  using System.Collections.Generic;
  using System.Diagnostics;
  using System.Text;
  using UnityEngine;
  #if UNITY_EDITOR
  using UnityEditor;
  #endif
  ```
  The stray empty `#if UNITY_EDITOR`/`#endif` block is also gone. Confirmed this was already the state
  at `d714482` (`git show d714482:Runtime/DebugX/DebugX.cs` shows the same guarded form), i.e. fixed at
  or before the baseline this re-audit compares against. The player-build compile risk (`CS0246` on an
  unresolvable `using`) is closed.

- **RESOLVED (documented)** — The `DebugX` class/namespace collision. `docs/00-AgentGuide.md` now has
  a dedicated subsection, `### \`DebugX\` class/namespace collision` (lines 99-109), spelling out
  exactly the trap the original audit flagged: unqualified `DebugX.Logger(...)` from a sibling
  namespace resolves to the namespace, not the class, forcing the double-qualified
  `DebugX.DebugX.Logger(...)` form, "[a]lready confirmed costing real code." This implements Fix #5
  from the original audit. Note: the collision itself still exists in code — `Runtime/Patterns/
  SingletonBehaviour.cs:67,103,162,199` still uses `DebugX.DebugX.Logger(LogChannels.DevTools)` — which
  is expected, since the fix requested was documentation of the gotcha, not a rename of the type (a
  rename would be a much larger, unrequested breaking change).

- **Not addressed** — Runtime reflection in `Runtime/DebugX/Logging/Core/CallerInfoHelper.cs` is still
  unconditional (not editor-gated): `GetCallingMethod()` (`StackTrace(skipFrames: 1, ...)`),
  `FindOptimalSkipCount` (`StackTrace(...)`), `IsInternalMethod`/`ExtractAsyncMethodName`
  (`MethodBase`) all still run on every DebugX log call in every build; only the Unity-stack-extractor
  fallback remains `#if UNITY_EDITOR`-gated. `Logging/Parsing/MessageTemplateParser.cs:113`
  (`type.GetMethod("ToString", ...)`) is likewise still unguarded. Still an open design-tension item,
  not a regression — unchanged from the original audit.

- **RESOLVED** — Widespread public-API constructors with default-valued parameters (originally flagged
  as tension with AGENTS.md's "no optional parameters"). All six constructors cited in the original
  finding now use the chained-overload pattern instead of default parameter values:
  - `LogEvent` (`Core/LogEvent.cs:27-37` full ctor, no defaults; `:54` convenience 4-arg overload
    chains to it with explicit `null`s).
  - `LogProperty` (`Core/LogProperty.cs:22` full ctor; `:29` 2-arg overload chains with
    `PropertyType.Scalar`).
  - `FileSink` (`Sinks/FileSink.cs:24-25` full ctor, no defaults; `:36` 2-arg overload chains with
    `maxFileSizeMB: 10, bufferThreshold: 50, flushIntervalSeconds: 1f`).
  - `JsonFileSink` (`Sinks/JsonFileSink.cs:27-28` full ctor; `:40` 2-arg overload chains with the same
    style of named-default arguments).
  - `UnityConsoleSink` (`Sinks/UnityConsoleSink.cs:21` full ctor; `:27` 1-arg overload chains with
    `LogLevel.Debug`).
  - `EditorConsoleSink` (`Console/EditorConsoleSink.cs:15` full ctor; `:20` 0-arg overload chains with
    `LogLevel.Verbose`).
  This is the same overload-split pattern used by the mechanical fix elsewhere in this diff, applied
  here to constructors too — verified each chained overload reproduces the exact default value that
  was previously inline.
- **No findings** for the other §3 gotchas checked — unchanged (no `??`/`??=` on `UnityEngine.Object`,
  no struct violations, no `OnValidate`/serialization callbacks in scope).

## Fixes

Status of the original priority list:

1. ~~Guard `using UnityEditor;`~~ — **DONE** (confirmed fixed at/before `d714482`).
2. ~~Delete `DebugLogger.cs`/`IDebugLogger.cs`/`LoggerFactory.cs`/`LogConfig.cs`~~ — **DONE** (deleted at
   `d714482`).
3. ~~Fix/remove the string-based half of `ExplicitErrorDedupe`~~ — **DONE**.
4. **Still open** — Route `DebugX.LogArray<T>` through `LogPipeline.Emit` like every other entry point,
   or document why it deliberately bypasses sinks. This is the one remaining actionable item from the
   original audit.
5. ~~Add the `DebugX` class/namespace collision to `docs/00-AgentGuide.md` §3~~ — **DONE**.

New, optional (not from original audit, not blocking): consider editor-gating the unconditional
`StackTrace`/`MethodBase` reflection in `CallerInfoHelper.cs`/`MessageTemplateParser.cs` per AGENTS.md's
no-runtime-reflection policy, and/or routing the five remaining raw `Debug.Log`/`Debug.LogError` calls
in `Editor/Debugging/` through `DebugX.Logger(LogChannels.Editor)`.

## Cross-references

- `D:\UnityProjects\HOMAM\docs\00-AgentGuide.md` §3 (`Debug` namespace collision gotcha; now also
  carries the `DebugX`/`DebugX` collision as its own subsection — confirmed present)
- `D:\UnityProjects\HOMAM\docs\01-CorePrinciples.md` (fail-fast errors — `ExplicitErrorDedupe` finding,
  now resolved)
- `D:\UnityProjects\HOMAM\docs\02-Libraries.md` (Library/Service role — DebugX correctly scoped)
- `D:\UnityProjects\HOMAM\docs\09-EditorHub.md` (diagnostic surfaces by scope)
- `D:\UnityProjects\HOMAM\docs\13-AuthoringStandards.md` (editor-window-as-last-resort)
- `D:\UnityProjects\HOMAM\Packages\com.aethernexus.foundationplatform\Documentation~\ARCHITECTURE.md`
  (Logging (DebugX) section — no longer references the now-deleted `LoggerFactory` path)
- Sibling audit for a different subsystem in this package, same pattern:
  `D:\UnityProjects\HOMAM\Packages\com.aethernexus.foundationplatform\Editor\ProjectWindowX\AUDIT.md`
