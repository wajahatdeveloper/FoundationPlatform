# PlayableGraphBridge invariants

Promoted from inline comments in `Runtime/Animation/PlayableGraphBridge.cs` — read this before touching
layer/state weight or lifecycle logic in that file.

## Layers boot connected at weight 1

Every `PlayableLayer` connects its state mixer to the owning layer mixer at weight 1 on construction
(`PlayableLayer` ctor, `PlayableGraphBridge.cs`). Overlay-layer visibility is primarily gated by **state**
weight, not layer weight — an empty/all-zero state mixer passes the lower layers through instead of
overriding with a bind pose, so an idle overlay layer sitting at layer-weight 1 contributes nothing.

**Do not boot overlay layers at weight 0.** `TransitionBackFromLayer` fades the overlay **layer** weight
to 0 after a one-shot ends (its last state stays at weight 1), so every play path on layers 1+ restores
`layer.Weight = 1` before playing — the `Weight` setter also cancels any pending fade.

## Only `ClipState`s are transient; `MixerState`s are long-lived and reused

When a state's weight reaches 0 and it's no longer the current state, only `ClipState` instances are
disconnected, destroyed, and have their port freed — a fresh `ClipState` is created per `Play()` call, so
reclaiming it is correct. `MixerState`s (stance/blend states) are intentionally **not** reclaimed here:
they stay connected at weight 0 and are re-targeted on the next `Play()`, since they're long-lived and
reused across plays rather than recreated.

If you add a new `PlayableState` subtype, decide up front which lifecycle model it follows — transient
(reclaim like `ClipState`) or long-lived (reuse like `MixerState`) — and make sure the reclaim check in
`PlayableLayer`'s update loop treats it consistently.
