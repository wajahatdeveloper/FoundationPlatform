# FoundationPlatform agent tools

Bridge tools that let a coding agent discover and drive this project's designer features instead of
re-deriving them from source. Compiles only when `com.aibridge.unity` is installed
(`AETHER_AIBRIDGE` version define on `FoundationPlatform.AgentTools.Editor.asmdef`), so the package
ships bridge-free.

| Tool id | Wraps | Notes |
|---|---|---|
| `platform-capability-list` | `FeatureCatalogApi.List` over `FeatureCatalog` | The `[DesignerFeature]` index: menu path, intent-first title, blurb, designer keywords, kind, doc. Filter with `keyword`. |
| `platform-capability-invoke` | `FeatureCatalogApi.Invoke` → the entry's own action | Catalogued entries only; unknown keys fail with the closest matches. Returns everything Unity logged while it ran. |
| `platform-agenttools-export-params` | `AgentToolParamExporter.Export` | Regenerates `.claude/skills/unity-bridge/params/*.json` from the live registry. |

## Publishing a new tool to agents

The MCP server builds `tools/list` from the schema files alone, so a `[BridgeTool]` that has not been
exported does not exist as far as any agent is concerned.

1. Add the method with `[BridgeTool]` + `[Description]` on the method and every parameter.
2. Run **Tools ▸ Platform ▸ Agent Tools ▸ Export MCP Params** (or call the tool above).
3. Restart the MCP server — it reads the schema files at startup.

Ids use an area prefix (`core-`, `platform-`, `character-`, `gas-`, `liveops-`). The exporter throws
when a package tool takes a prefix the vendored bridge owns, because the registry is a single
dictionary and the winner would depend on assembly scan order.

## Curated schema text

`_generated.json` records what the exporter last wrote per tool and parameter. A description that
still matches its baseline is untouched generated text and gets overwritten; anything else was
hand-curated (payload shapes, ENUM hints on vendored bridge tools) and survives regeneration.
Unknown top-level keys such as `hints` are always preserved.
