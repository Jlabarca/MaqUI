# Maqui — Living Architecture Document

> THE single source of truth for project state, architecture decisions, and next steps.
> Updated: 2026-03-21

## What Is This Project?

**Maqui** (`com.ware.maqui` v0.1.0) is a reactive MVVM UI framework for Unity. It uses R3 for reactive state, VitalRouter for command routing, and UniTask for async. The primary consumer is **ORO** (an MMO), where Maqui serves as the UI layer on top of HybridFrame.

## Architecture Decisions (Locked)

| Decision | Choice | Rationale | Date |
|---|---|---|---|
| UI architecture pattern | Reactive MVVM (R3 + VitalRouter) | Eliminates 40-60% boilerplate vs OneUI manual binding; enables pure C# VM testing | Jan 2026 |
| Rendering strategy | Hybrid uGUI + UI Toolkit | uGUI for VFX/world-space, UI Toolkit for data-heavy lists; same VM drives both | Jan 2026 |
| Theming approach | Procedural (ThemeData SO, 13 color slots) | Eliminates sprite sheet bloat and draw call fragmentation | Jan 2026 |
| Service access pattern | MaquiServices static service locator (interfaces) | Replaced singletons; enables test isolation via Reset() | Mar 2026 |
| Reactive collections | Custom ReactiveList\<T\> | Lightweight alternative to ObservableCollections; granular add/remove/replace/reset | Mar 2026 |
| Window lifecycle | Object pooling (opt-in per asset key) | Reduces GC pressure for frequently opened/closed windows (inventory, tooltips) | Mar 2026 |
| Error handling | Null-return + WindowLoadFailed event | Show*Async returns null on failure, fires event for fallback UI | Mar 2026 |
| Asset loading | IMaquiAssetProvider abstraction | Default Resources.Load; swap for YooAsset/Addressables without touching views | Jan 2026 |
| Bootstrap | [RuntimeInitializeOnLoadMethod] zero-config | No scene setup, no prefab, no Awake ordering | Jan 2026 |

See [design/foundation-decisions.md](design/foundation-decisions.md) for full rationale on the original 5 decisions.

## What's Built (v0.1.0)

### Core Framework
- Pure C# `ViewModel` base with `CompositeDisposable` lifecycle
- `ReactiveList<T>` granular reactive collection (add/remove/replace/reset observables)
- `ReactiveBaseView<T>` with typed VM binding, `OnBind()`/`OnFreeze()`/`OnUnfreeze()`/`OnReset()` lifecycle
- `MaquiBaseView` self-contained base (no external framework dependency)
- `MaquiWindowManager` — 4-layer Canvas system (Background/Default/Overlay/Modal) with:
  - Automatic modal freeze/unfreeze coordination
  - `IUIService` interface for HybridFrame integration
  - `WindowLoadFailed` error event
  - Opt-in per-asset-key object pooling (`WindowPool`)
- `MaquiServices` static service locator (replaces all singletons)

### Bridges (zero-config, auto-registered)
- `InputBridge` — `IInputProvider` abstraction (Legacy + New Input System)
- `RouterBridge` — VitalRouter wrapper
- `ThemeProvider` — `ReactiveProperty<ThemeData>`, 13 color slots, runtime swap
- `AnimationBridge` — `FadeAsync`, `ScaleAsync`, `SceneTransitionAsync`

### Navigation Stack
- Per-layer `Stack<IWindowHandle>` (push on show, pop on dispose)
- `PopWindow(UILayer)` — dispose topmost window on a layer
- `GetStackDepth(UILayer)` — query stack depth
- `BackRequested` event — fired on Escape/Cancel input
- `OnBackRequested()` — view hook to consume back navigation
- `SuppressBackNavigation` — disable auto Escape→Pop during cutscenes/transitions

### Subscribers
- `ThemeSubscriber`, `ThemeImageSubscriber`, `ThemeTextSubscriber` — zero-code theme binding

### Samples (4)
- Welcome — minimal routing + MVVM demo
- GrandTour — multi-VM shared state + hybrid rendering
- SharkSuite — UI Toolkit list + uGUI detail panel
- Modernization — procedural visuals + theme binding

### Test Coverage
- Runtime: ViewModelTests, ThemeDataTests, MaquiBaseViewTests, ReactiveBaseViewTests, MaquiWindowManagerTests, InputBridgeTests, ThemeProviderTests, AnimationBridgeTests, ThemeSubscriberTests, CoreBootstrapTests, AssetProviderTests, ReactiveListTests, InterceptorTests
- Editor: MaquiEditorTests

## Known Gaps (from v0.1.0 assessment)

| # | Gap | Severity | Status |
|---|---|---|---|
| 1 | ~~Singletons everywhere~~ | Must fix | **Fixed** — MaquiServices service locator |
| 2 | ~~No object pooling~~ | Should fix | **Fixed** — WindowPool, opt-in per asset key |
| 3 | ~~Reactive collections missing~~ | Must fix | **Fixed** — ReactiveList\<T\> |
| 4 | ~~Minimal test coverage~~ | Must fix | **Fixed** — 13 runtime + 1 editor test suites |
| 5 | ~~No VM-to-VM communication pattern~~ | Should fix | **Fixed** — documented in `reference/shared-state.md` (shared VM + commands patterns) |
| 6 | ~~No error handling~~ | Must fix | **Fixed** — WindowLoadFailed event, null-return |
| 7 | Animation system too simple | Nice to have | Open — no sequencing, no easing library |
| 8 | No prefab validation / compile-time safety | Should fix | Open — string asset keys, no editor validator |
| 9 | Theme system lacks depth | Nice to have | Open — flat, no inheritance/cascading |
| 10 | No runtime debugging tools | Nice to have | Open |
| 11 | CoreBootstrap not configurable | Nice to have | Open — no headless mode |
| 12 | ~~No navigation stack / back-button~~ | Should fix | **Fixed** — per-layer Stack\<IWindowHandle\>, PopWindow, Escape/Cancel detection, OnBackRequested hook |
| 13 | No localization hook | Nice to have | Open |

## Demo Scenes

Three demo scenes with 7 prefab-based windows, all using IUIService layer system correctly:

- **Demo1 Gallery** — Tab switching, theme toggle, fade+scale entrance. 1 window (Default layer).
- **Demo2 Game HUD** — Three-layer UI (HUD→Inventory→Shop). Demonstrates modal freeze system, ReactiveList granular binding, shared gold state across windows. 3 windows (Overlay/Default/Modal).
- **Demo3 Frosted HUD** — Shared ViewModel across 3 windows, two-way settings binding, slide animations. Le Tai TranslucentImage blur effects. 3 windows (Overlay/Modal/Modal).

All views wire Router in `OnBind()` with `.AddTo(Disposables)` (pool-safe pattern). Prefabs at `Assets/Resources/Views/`.

**Future improvement**: The original scene UI (OneUI/Pack/Le Tai components) still exists alongside the Maqui prefabs. The intended end-state is to rewire existing scene elements to ViewModels rather than using separate overlay prefabs. This requires Unity Editor work (scene/prefab modifications).

## Next Steps

1. **Asset key validation** — editor tooling or code-gen to catch typos at compile time
2. **MaquiDebugWindow** — runtime overlay showing layer hierarchy, active handles, modal count, recent commands
3. **Demo scene rewire** — bind existing scene UI elements to ViewModels instead of separate prefab overlays (Unity Editor task)
4. **Sample OneUI removal from scenes** — Welcome/GrandTour/SharkSuite sample asmdefs no longer reference UIFramework; remove OneUI controllers from sample scenes (Unity Editor task)
