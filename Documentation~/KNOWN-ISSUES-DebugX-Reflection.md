# Known issue: DebugX caller-info reflection runs unconditionally in player builds

## What

`Runtime/DebugX/Logging/Core/CallerInfoHelper.cs`'s `GetCallerInfo()` / `FindOptimalSkipCount()` /
`IsInternalMethod()` / `GetCallingMethod()` use `System.Diagnostics.StackTrace` and
`System.Reflection.MethodBase` on **every single `DebugX.*` log call, in every build** — not just the
editor. Only the Unity-stack-extractor fallback path inside that file is `#if UNITY_EDITOR`-gated; the
core stack-walk/reflection path is not.

`Runtime/DebugX/Logging/Parsing/MessageTemplateParser.cs:113` (`type.GetMethod("ToString",
System.Type.EmptyTypes)`) does the same thing per logged non-primitive property value, also unguarded,
in the Runtime assembly.

## Why it's flagged

The project's stated policy (`AGENTS.md`, `docs/00-AgentGuide.md` §3) is "No runtime reflection (AOT
target) — reflection only in editor tools." `CallerInfoHelper` and this `MessageTemplateParser` path are
compiled into the Runtime assembly (`includePlatforms: []`) with no such guard, so this reflection runs
in real player builds, including IL2CPP/AOT targets, on every log call.

A sibling file in the same package, `Runtime/DebugX/Logging/Console/LogEntriesBridge.cs`, does the same
kind of reflection but correctly confines all of it behind `#if UNITY_EDITOR` — so the project already
has a documented, working pattern for this; `CallerInfoHelper` and `MessageTemplateParser` just don't
follow it.

## Why it's not a quick fix

Caller-info (file/line/member name) without this reflection generally requires threading
`[CallerMemberName]`/`[CallerFilePath]`/`[CallerLineNumber]` compiler-supplied parameters through every
`DebugX.*` logging entry point (`DebugX.Info/Warning/Error`, `DebugXLogger`'s per-level methods,
`DebugXBuilder`'s fluent chain) instead of walking the stack at the call site. That's an API-surface-wide
change, not a localized patch — every call site's signature and every internal forwarding call needs the
attributes threaded through correctly, and existing callers need to keep compiling. It also changes what
information is available (compiler-supplied caller info can't see through indirection the way a runtime
stack walk sometimes can, e.g. reporting the immediate caller of a shared logging helper rather than that
helper's own caller).

This is judged out of scope for a routine audit-remediation pass and needs its own dedicated,
Unity-compile-verified pass.

## Options for whoever picks this up

1. **Thread `[CallerMemberName]`/`[CallerFilePath]`/`[CallerLineNumber]`** through every `DebugX.*` entry
   point (the "right" long-term fix, largest surface-area change).
2. **Confine the existing stack-walk path to `#if UNITY_EDITOR || DEVELOPMENT_BUILD`** (or `#if
   UNITY_EDITOR` only, matching the project's stricter reading of the no-runtime-reflection rule) and
   drop caller info entirely in stripped release player builds — smaller change, but callers lose
   file/line/member info outside dev builds.
3. **Confirm this is an accepted, deliberate trade-off** (the reflection used is property/attribute
   lookups and `StackTrace`/`MethodBase`, not `Reflection.Emit`/dynamic codegen, and generally works under
   IL2CPP/AOT — just with a real per-call perf cost) and document that decision here and in
   `ARCHITECTURE.md`'s Logging (DebugX) section instead of changing code.

No code changes have been made for this issue as part of the audit-remediation pass this doc was written
during — it is deferred, not fixed.
