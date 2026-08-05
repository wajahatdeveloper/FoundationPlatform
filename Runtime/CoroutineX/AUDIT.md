# CoroutineX — Architecture Audit (Re-Audit)

## Re-Audit Context

Follow-up pass after fixes were applied. Method: re-read the original findings below, diffed
`d714482..HEAD` for `Runtime/CoroutineX/`, and read the current file states directly (some fixes,
including the biggest one, predate `d714482` itself — the baseline "Audit" commit this diff range
starts from — so they don't show as changed lines in the diff but are confirmed present in `HEAD`).

**Headline result: the resolution path chosen was different from the original audit's default
recommendation, but it closes the same gap.** The original audit's primary finding was framed as "is
this vendored file being hand-patched in place" and recommended either reverting the DebugX coupling
or explicitly documenting it as a sanctioned deviation from upstream. What actually happened:
**CoroutineX has been reclassified as first-party, non-vendored code**, removed from
`docs/00-AgentGuide.md` §4's vendored-frameworks table, and `Documentation~/ARCHITECTURE.md` now states
explicitly: *"First-party code (not vendored — the `DebugX` logging calls woven into its control flow
are a deliberate first-party dependency, not an in-place patch of a third-party drop)."* This is a
valid resolution of the doc-drift finding (Fix #2/#4 from the original audit), just via reclassification
rather than a "vendored" annotation. The dead ~50-line commented-out alternate implementation is
deleted. **This audit is therefore no longer "vendored framework"-shaped** — see below for what that
means for future audits of this file.

## Original Context

`CoroutineX` was listed in `docs/00-AgentGuide.md` §4 ("Vendored & generated code — do not hand-edit")
as a **vendored framework**, alongside RagdollHelper and IndicatorPackage/OffScreenIndicator. Per that
doc: *"To 'fix' vendored code: revert to upstream and apply the change as configuration or a wrapper,
not an in-place patch."*

This shaped the original audit's approach — the primary question was not "is this code well-designed"
but "has this vendor file been hand-patched in place, and is that patching now indistinguishable from
the vendor baseline because nobody diffs against upstream?" No upstream source tree was available;
all "hand-patch" conclusions were inferred from internal evidence (style breaks, project-specific
coupling, leftover dead code), stated as a limitation.

Scope read in full (10 `.cs` files): `Runtime/CoroutineX/CoroutineX.cs`,
`Runtime/CoroutineX/Components/{CoroutineXExecutor,CoroutineXGroup,CoroutineXOwner,EnumerableEnumerator,Routines}.cs`,
`Runtime/CoroutineX/SubScripts/{CoroutineXExtensions,WaitForAll,WaitForAny,YieldAwaiter}.cs`,
`Editor/CoroutineX/CoroutineXOwnerEditor.cs`.

## Mechanical fix (this diff range): optional-parameter cleanup — verified correct

`d714482..HEAD` converts `CoroutineX.Run(bool rerunIfCompleted = true)`
(`Runtime/CoroutineX/CoroutineX.cs:540-582`) into `Run(bool rerunIfCompleted)` + a new
`Run() => Run(true)` overload (added at line 665-666). Default value (`true`) preserved exactly.
Cross-repo grep for `CoroutineX.Run(` and `.Run(` call sites (this package's `SceneManagerX.cs`, the
`Samples~/EventBusCoroutineDemo` sample, and the package's own `SubScripts/CoroutineXExtensions.cs`)
found no partial-arg call site that would fail to resolve against the two overloads. Consistent with
the same pattern applied ~25 times across this diff range in DebugX and elsewhere — correct here too.

## Findings

### Vendor Integrity

- **RESOLVED (via reclassification, not reversion)** — `CoroutineX.cs`'s project-specific
  structured-logging calls (`using AetherNexus.FoundationPlatform.DebugX;` and
  `DebugX.Logger(LogChannels.DevTools).Error(...)` calls inside `CurrentState`/`Run()`/`SetOwner()`)
  are **still present** — confirmed unchanged in current source. What changed is the framing: this is
  no longer flagged as an undocumented vendor-integrity risk, because the file is no longer classified
  as vendored at all. `docs/00-AgentGuide.md` §4's vendored-frameworks table now reads only
  `RagdollHelper, IndicatorPackage / OffScreenIndicator` — `CoroutineX` has been removed. This
  implements the "team decides deliberately and records it" half of the original Fix #2, just landing
  on "not vendored" rather than "vendored + documented seam."

- **RESOLVED** — The ~50-line dead, commented-out alternate `CoroutineX` implementation
  (`private static CoroutineOwner CoroutineOwner`, `StartCoroutine(this IEnumerator)`, `internal class
  CoroutineOwner : MonoBehaviour`) is **deleted**. Confirmed via `git show d714482 --stat` — 48 lines
  removed from `CoroutineX.cs` in the `d714482` "Audit" commit itself (the baseline this re-audit's
  diff starts from). Current file is 863 lines, ends cleanly after the `RunAsync`/`Run` region with no
  trailing commented block; `d714482`'s version was already 860 lines with the same clean ending, i.e.
  the deletion predates and is preserved through the diff range under review. Implements Fix #3.

- **Info, unchanged in substance, downgraded in significance** — `WaitForCompletionAsync`, `RunAsync<T>`,
  `RunAsync` (`CoroutineX.cs`, now ~lines 826-857 given the file shrank) still use raw
  `System.Threading.Tasks.Task`, still stylistically foreign to the rest of the file, still unrelated to
  `CoroutineX` instance state. Previously read as vendor-drift evidence; now that the file is first-party,
  this is just an ordinary "should this live here" design note, not integrity evidence. Not fixed, not
  urgent.

- **No findings** beyond the above for the other 9 files — unchanged, still internally consistent.

### Ownership
No findings — unchanged from original audit (no competing coroutine helper found; `global using`
availability confirmed in `QuestSystem`/`ItemSystem`/`CharacterSystem`/`RagdollHelper`, but this
folder-scoped re-audit still cannot see whether those packages call it — same limitation as before,
flagged for the queued GameFramework/GameEngineCore audits). `UniTask`/`CoroutineX` two-substrate
co-existence note also unchanged — still an open platform-level design question, not a defect.

### Determinism
No findings — unchanged. `Routines.Delay`/`FrameDelay` remain presentation-timing only within this
scope; the cross-package call-site verification remains a task for the queued GameFramework/
GameEngineCore/GameplayAbilitySystem/TacticalFeatures audits, same as originally noted.

### Redundancy/Simplification
No findings beyond what's covered under Vendor Integrity — unchanged. `Editor/CoroutineX/
CoroutineXOwnerEditor.cs` still a small, correctly `#if UNITY_EDITOR`-gated custom inspector.

### Doc/Architecture Drift

- **RESOLVED** — `Documentation~/ARCHITECTURE.md`'s `## CoroutineX` section (now lines 116-134) opens
  with the explicit non-vendored statement quoted above, closing the gap the original audit flagged
  ("no callout that it is vendored, no 'do not hand-edit' note"). Since the resolution path was
  reclassification rather than annotation-as-vendored, this note is phrased as "not vendored" rather
  than "vendored, see docs/00 §4" — but it achieves the same goal: nobody reading this doc is left
  thinking CoroutineX is a clean, unexamined vendor drop that shouldn't be touched.
- **Consistent** — `docs/03-Frameworks.md` (line 47/77 area) still lists CoroutineX alongside
  EventBus/DebugX/TweenX under the Foundation Platform pointer table, with no vendored annotation —
  now correctly consistent with the reclassification (previously this was flagged as part of the drift;
  now there's nothing to be inconsistent with).
- **Unverified / possibly moot** — The original audit also cited `Assets/Docs/ARCHITECTURE.md` line 29
  as a third doc layer repeating the gap. That file no longer exists at the path checked
  (`D:\UnityProjects\HOMAM\Assets\Docs\ARCHITECTURE.md` — not found), consistent with the broader docs
  reorg described in the root `CLAUDE.md` (docs now live under `docs/`). Not independently chased down
  further; flagging as likely resolved by the doc reorg rather than a targeted fix.

## Fixes

Status of the original priority list:

1. **Not done, and no longer applicable in its original form** — "Diff against real upstream before
   touching `CoroutineX.cs` further." Given the file is now officially classified as first-party
   (not vendored), this recommendation's premise no longer holds — there is no upstream to diff
   against for a file the project no longer treats as vendored. If this reclassification is itself
   wrong (i.e. there really is an upstream CoroutineX this diverged from and the team should know that),
   that's a decision outside this audit's scope to second-guess, but worth a sanity check by whoever
   made the reclassification call.
2. ~~Decide, deliberately, whether the DebugX coupling is a sanctioned integration seam~~ — **DONE**,
   resolved as "yes, and it's not even a vendor deviation because the file isn't vendored."
3. ~~Delete the dead commented-out block~~ — **DONE**.
4. ~~Add the vendored/do-not-hand-edit callout to `Documentation~/ARCHITECTURE.md`~~ — **DONE**, in the
   form of a "not vendored" callout instead, which resolves the same underlying gap (a reader now knows
   the status either way, rather than not knowing at all).
5. No action recommended for UniTask/CoroutineX co-existence or `RunAsync` additions — **unchanged**,
   still an open platform-level question, not urgent.

## Cross-references

- **GameFramework (queued audit)** — unchanged from original: verify whether `QuestSystem`/
  `ItemSystem`/`CharacterSystem`/`RagdollHelper` actually call `CoroutineX.Run`/`Routines.Delay` for a
  gameplay-affecting timer.
- **GameEngineCore / GameplayAbilitySystem / TacticalFeatures (queued audits)** — unchanged, same
  grep-and-check task carried forward.
- **RagdollHelper / IndicatorPackage-OffScreenIndicator** — these two remain in `docs/00-AgentGuide.md`
  §4's vendored table (`CoroutineX` was removed from it, not the other two). Worth checking, when those
  are audited, whether the same "hand-patched vendor file, undocumented" pattern found in the original
  CoroutineX audit recurs there — that finding's general shape is still valid even though CoroutineX
  itself turned out not to be the right example of it.
- **UniTask** (`Runtime/ThirdParty/UniTask/`) — unchanged, still a second async substrate co-resident
  with CoroutineX; not itself in the vendored-code table.
