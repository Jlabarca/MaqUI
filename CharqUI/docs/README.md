# Maqui Documentation

> Start here: **[CONTEXT.md](CONTEXT.md)** — project state, decisions, next steps.

## Root

| File | Purpose |
|---|---|
| [CONTEXT.md](CONTEXT.md) | Living doc — state, architecture decisions, gaps, next steps |
| [LOGBOOK.md](LOGBOOK.md) | Append-only session history |
| [DOCS-PROTOCOL.md](DOCS-PROTOCOL.md) | Rules for maintaining this docs folder |

## reference/ — How Things Work

| File | Topic |
|---|---|
| [architecture.md](reference/architecture.md) | 3-layer model, bootstrap, data flow, class hierarchy |
| [core-concepts.md](reference/core-concepts.md) | ViewModel, ReactiveBaseView, R3 patterns, disposables |
| [routing-commands.md](reference/routing-commands.md) | VitalRouter, commands, interceptors, async pipeline |
| [theming.md](reference/theming.md) | ThemeData, ThemeProvider, subscribers, runtime switching |
| [animation-transitions.md](reference/animation-transitions.md) | AnimationBridge, FadeAsync, ScaleAsync, transitions |
| [hybrid-rendering.md](reference/hybrid-rendering.md) | uGUI vs UI Toolkit decision matrix, hybrid patterns |
| [samples.md](reference/samples.md) | All 4 samples with architecture diagrams |
| [rendering-optimization.md](reference/rendering-optimization.md) | Canvas rebuilds, draw calls, fill rate, profiling |
| [procedural-visuals.md](reference/procedural-visuals.md) | Gradient meshes, procedural overlays, batching |
| [input-abstraction.md](reference/input-abstraction.md) | IInputProvider, dual input system support |

## guides/ — How-To

| File | Topic |
|---|---|
| [quick-start.md](guides/quick-start.md) | First screen in 15 minutes |
| [performance.md](guides/performance.md) | Performance budget, optimization checklist, URP notes |

## design/ — Frozen Decision Records

| File | Topic |
|---|---|
| [foundation-decisions.md](design/foundation-decisions.md) | Why MVVM, hybrid rendering, procedural themes, dep stack |
| [oro-adaptation.md](design/oro-adaptation.md) | ORO gap analysis — 14 requirements vs Maqui v0.1.0 |
| [oro-ui-rework.md](design/oro-ui-rework.md) | ORO UI system design using Maqui |

## research/

| File | Topic |
|---|---|
| [oneui-analysis.md](research/oneui-analysis.md) | OneUI technical analysis (predecessor framework) |
| [modular-ui-kit-analysis.md](research/modular-ui-kit-analysis.md) | Modular Game UI Kit analysis (visual techniques source) |

## tools/

| File | Topic |
|---|---|
| [awesome-attributes.md](tools/awesome-attributes.md) | AwesomeAttributes Inspector DX reference |

## archive/

Historical docs from the CharqUI era. See `archive/` for listing.
