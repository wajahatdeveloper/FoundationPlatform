# PlayableGraphBridge invariants

Promoted from inline comments in `Runtime/Animation/PlayableGraphBridge.cs` — read this before touching
layer/state weight or lifecycle logic in that file.

## Layers boot connected at weight 1

Every `PlayableLayer` connects its state mixer to the owning layer mixer at weight 1 on construction
(`PlayableLayer` ctor, `PlayableGraphBridge.cs`). Overlay-layer visibility is primarily gated by **state**
weight, not layer weight — an empty/all-zero state mixer passes the lower layers through instead of
overriding with a bind pose, so an idle overlay layer sitting at layer-weight 1 contributes nothing.

**Do not boot overlay layers at weight 0.** `TransitionBackFromLayer` fades the overlay **layer** weight
to 0 after a one-shot ends (its last state stays at weight 1), so every play path on layers 1+ calls
`layer.FadeInForPlay(fade)` before playing. It eases the layer up from its current weight over the same
fade the new state crosses in on; snapping `Weight = 1` would pop a state that is mid fade-out (or the
stale last state) to full strength. Only zero-fade paths (preview, timeline resync) snap.

## Crossfades are eased and keep state weights summing to 1

`Play` / `Stop` start every active state on one shared fade clock (`ActiveState.BeginFade`). Each state
eases from its own start weight to its target with smoothstep on that clock, so the weights still sum
to 1 every frame (a sag lets a humanoid drift toward its default pose) while each blend starts and ends
at zero velocity. Layer-weight fades (`StartFade`) use the same easing. Anything that retargets a state
fade, including editor tooling, goes through `BeginFade`.

## Only `ClipState`s are transient; `MixerState`s are long-lived and reused

When a state's weight reaches 0 and it's no longer the current state, only `ClipState` instances are
disconnected, destroyed, and have their port freed — a fresh `ClipState` is created per `Play()` call, so
reclaiming it is correct. `MixerState`s (stance/blend states) are intentionally **not** reclaimed here:
they stay connected at weight 0 and are re-targeted on the next `Play()`, since they're long-lived and
reused across plays rather than recreated.

If you add a new `PlayableState` subtype, decide up front which lifecycle model it follows — transient
(reclaim like `ClipState`) or long-lived (reuse like `MixerState`) — and make sure the reclaim check in
`PlayableLayer`'s update loop treats it consistently.
