# FoundationPlatform agent tools

Bridge tools that let a coding agent discover and drive this project's designer features instead of
re-deriving them from source. Compiles only when `com.aethernexus.aibridge` is installed
(`AETHER_AIBRIDGE` version define on `FoundationPlatform.AgentTools.Editor.asmdef`), so the package
ships bridge-free.

| Tool id | Wraps | Notes |
|---|---|---|
| `platform-capability-list` | `FeatureCatalogApi.List` over `FeatureCatalog` | The `[DesignerFeature]` index: menu path, intent-first title, blurb, designer keywords, kind, doc. Filter with `keyword`. |
| `platform-capability-invoke` | `FeatureCatalogApi.Invoke` → the entry's own action | Catalogued entries only; unknown keys fail with the closest matches. Returns everything Unity logged while it ran. |
| `render-preview` | `PreviewRenderUtility` + `AnimationClip.SampleAnimation` | One PNG of a model, prefab or scene object at a chosen angle, optionally posed at a clip time. |
| `render-clip-strip` | same session, many samples | Labelled contact sheet across a clip (`frames` or explicit `times`), one row per `yaws` entry. |
| `render-compare` | two preview sessions + pixel diff | Two poses side by side under identical framing, plus a difference cell and `changedFraction`. |
| `anim-set-report` | `AnimationSet.GetResolvedEntries`, `AnimationSetValidator`, `AnimationSetValidationProfile.Validate` | Resolved entries with declaring set, clip facts, links, and every validation finding. |
| `anim-clip-report` | `AnimationUtility` curve and event bindings | Clip facts plus, with `target`, the animated paths that do not resolve on that rig. |

## Visual verification

`screenshot-capture` renders the Scene or Game camera only, so animation, pose and IK outcomes were
invisible to agents. The `render-*` tools close that gap without touching an open scene: they
instantiate into a `PreviewRenderUtility` scene with fixed lights and fit-to-bounds framing, so the
same inputs give the same pixels, and `framing` in the result is the exact camera to reproduce or
nudge.

Animation Rigging does **not** evaluate off-scene. To see an evaluated IK pose, target the live
object — `scene:Archetype_Player` — because cloning it copies the transforms the rig already wrote.
The clone is instantiated under an inactive holder and stripped of every `MonoBehaviour` before it
activates, so no gameplay `Awake`/`OnEnable` runs: cloning a live Play-mode character used to
register the ghost with runtime registries and leave dangling references once it was destroyed.

Editor chrome (Inspector rows, tool windows, overlays) is still outside the bridge; capture it with
`capture_editor.py` in the bridge package's `Tools~` folder.

## Publishing a new tool to agents

Add the method with `[BridgeTool]` + `[Description]` on the method and every parameter, then restart
the MCP server. The bridge re-exports `UnityBridge/params` after every compile. Ids use an area prefix
(`core-`, `platform-`, `character-`, `gas-`, `liveops-`, `render-`, `anim-`, `ik-`, `rig-`); schema
export and curation rules are in the bridge package's `Tools~/README.md`.
