# Foundation Platform documentation

User-facing guides for `com.aethernexus.foundationplatform`.

| Doc | Audience |
|-----|----------|
| [../README.md](../README.md) | Install, overview, quick start |
| [ARCHITECTURE.md](ARCHITECTURE.md) | Systems, namespaces, assemblies |
| [TweenX.md](TweenX.md) | Tweens and Feedback player |
| [FrameworkInspector.md](FrameworkInspector.md) | Attribute inspector engine |
| [../SAMPLES.md](../SAMPLES.md) | Package Manager samples |
| [../CHANGELOG.md](../CHANGELOG.md) | Version history |
| [../LICENSE.md](../LICENSE.md) | MIT license |
| [../Third-Party Notices.txt](../Third-Party%20Notices.txt) | UniTask and notices |

## Useful Editor menus

| Menu | Purpose |
|------|---------|
| **Window → DebugX Console...** | Structured log console |
| **Window → Event Bus...** | EventBus debug hub |
| **Window → TweenX → Tween Debugger** | Live tween list |
| **Window → Domain → Game State...** | World-scope live state; sections come from whichever gameplay package is installed |
| **Tools → Diagnostics → Framework Inspector Demo** | Attribute surface harness |

## Debug section seams

Two registries, both auto-discovered via `TypeCache` — implement the interface anywhere and the surface picks it up, no registration call:

| Interface | Surface | Scope |
|---|---|---|
| `IEntityDebugSection` | Scene View **Entity Debugger** overlay | the selected GameObject |
| `IWorldDebugSection` | **Game State** window | state that belongs to no GameObject |

Both are shells here — FoundationPlatform ships no gameplay sections. `DebugDrawKit` mirrors every draw call into `ActiveRecorder` when set, which is how both surfaces implement Copy Info with no separate serialization path.
