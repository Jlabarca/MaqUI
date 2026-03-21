# Docs Evaluation — Maqui Framework

> Audit date: 2026-03-11 | Auditor: Claude Sonnet 4.6
> Scope: All 30 documents in `docs/` and `docs/design/` and `docs/plugins/`

---

## Summary Verdict

| Category | Count | Action |
|:---|:---:|:---|
| **Architecturally sound, needs rename/update** | 5 | Rewrite for Maqui |
| **Outdated — system replaced** | 8 | Move to legacy, do not port |
| **Pure research / decision logs** | 9 | Move to legacy, archive |
| **Plugin references — still accurate** | 6 | Move to legacy, extract key facts |
| **Actively harmful / misleading if followed** | 2 | Move to legacy with warning header |

---

## Per-Document Assessment

### ✅ Still Architecturally Relevant (Rewrite for Maqui)

#### `design/charqui-architecture.md`
**Verdict**: Rewrite as Maqui Architecture Overview.
**What holds**: System topology diagram, the 4 architectural pillars (Reactive, Router, Hybrid Rendering, Unified Input), re-conditioning model (OneUI base + R3 + VitalRouter).
**What's stale**: Still says "CharqUI", uses `CoreBootstrap` that references sample interceptors (fixed), diagram shows `CharqUI Core` as bridge label.

#### `View-Animation-States.md`
**Verdict**: Rewrite as Animation & Transitions guide.
**What holds**: The stateDiagram lifecycle (Hidden → Transition_In → Visible → Transition_Out), `DisplayOptions` contract concept, 250–400ms snap timing standard, ghost-input prevention via `blocksRaycasts`.
**What's stale**: References `AnimateView` coroutine — Maqui uses `AnimationBridge` (UniTask async). `AnimationType.Fade/Scale` enum names need verification against current codebase.

#### `Rendering-Optimization-Audit.md`
**Verdict**: Port as Performance Guide.
**What holds**: All three bottleneck categories (Canvas Rebuild, Draw Call Fragmentation, Overdraw) are timeless uGUI truths. Profiling table (UI Profiler / Frame Debugger / Material Audit) targets are still correct. `Rect Mask 2D` vs `Mask` advice is accurate.
**What's stale**: References `OneUI DX`. No mention of URP-specific batching. Targets predate URP 17.x.

#### `Input-Management-Layer.md`
**Verdict**: Rewrite as Input Integration guide.
**What holds**: `CanvasGroup` + `blocksRaycasts` input blocking mechanics, modal popup focus capture model, explicit Raycast Target discipline.
**What's stale**: `IInputProvider` abstraction is now in `Maqui.Core.Bridge` (implemented). The doc treats this as "recommended pattern" when it's now shipped code. Update to show actual `InputBridge.Instance` API.

#### `UI-Toolkit-Integration-Guide.md`
**Verdict**: Rewrite as Hybrid Rendering guide.
**What holds**: The decision matrix (when to use uGUI vs UI Toolkit), `UIDocument`-based bridge pattern, shared `PanelSettings` batching tip, lazy `Q<T>` binding inside `OnViewStart`.
**What's stale**: Describes `EventMessenger` as the communication layer — replaced by VitalRouter. The `GrandTour.GrandTourSettingsView` sample now exists and is the reference implementation.

---

### ❌ Outdated — System Replaced (Archive, Do Not Port)

#### `UI-System-Bootstrap-Flow.md`
**Why obsolete**: Entire doc describes `ObjectInstaller` + `SceneController.BindViews()` manual registration pattern. This is entirely replaced by `CoreBootstrap.cs` → `[RuntimeInitializeOnLoadMethod(BeforeSceneLoad)]`. No scene setup is required in Maqui.

#### `Payload-Contract-Library.md`
**Why obsolete**: Documents the `EventMessenger` Publish-Subscribe bus (`OnViewAdded`, `OnViewShown`, `OnViewNavigated`, etc.). `EventMessenger` is **deprecated** in Maqui. All inter-system communication now uses VitalRouter `ICommand` structs. The `ConfirmDialoguePayload` / `PromptPayload` / `SearchPayload` patterns need to be re-expressed as VitalRouter commands.

#### `Developer-Onboarding-Playbook.md`
**Why obsolete**: Every step references the old system — `EventMessenger.Main.Subscribe`, `ObjectInstaller`, `SceneController.BindViews()`, `UIFramework.GetView<T>()`. The "Step 1: Define the Contract (Payload)" workflow is entirely replaced by ViewModel reactive properties. Following this doc in a Maqui project would produce broken code.

#### `System-Upgrade-Roadmap.md`
**Why obsolete**: The roadmap targets are now shipped:
- Phase 1 (Performance): UniTask replaces coroutines (done).
- Phase 2 (Service Decoupling): VitalRouter replaces EventMessenger (done). No Zenject/VContainer (used VitalRouter DI instead).
- Phase 3 (UI Toolkit Adoption): `GrandTourSettingsView` is the reference implementation (done).
The "Vision" section is now the baseline, not a goal.

#### `future-docs-roadmap.md`
**Why obsolete**: Predates the current doc structure. All 10 recommended documents either now exist or have been superseded.

#### `Grand-Tour-Setup.md`
**Why stale**: References pre-built prefab assets (`GrandTour_Orchestrator.prefab`, `GrandTour_Layout.uxml`, `GrandTour_Styles.uss`) that exist only as C# scripts in `Samples~/GrandTour/`. The setup guide needs to be rebuilt once scene assets exist.

#### `UI-Localization-Workflow.md`
**Why stale**: Describes a `LocalizedLabel` component that references `EventMessenger`. No localization system is implemented in the current codebase. This is aspirational. Mark as backlog.

#### `Procedural-Visuals-Standards.md`
**Why stale**: All references are to `Ricimi.Gradient` from the Modular Game UI Kit. Maqui's `ModernizationSample` uses it, but it's a sandbox dependency, not a package dependency. The gradient tooling doc belongs in CharqUI sandbox docs, not in the Maqui package docs.

---

### 📁 Research / Decision Logs (Archive)

| File | Reason to Archive |
|:---|:---|
| `design/charqui-architecture.md` | Source doc — rewritten above |
| `design/charqui-logbook.md` | Implementation log — internal. Not user-facing. |
| `design/charqui-plugins.md` | Superceded by actual `package.json` deps |
| `design/charqui-visual-fx-optimization.md` | Blur/shadow optimization — valid but belongs as a rendering sub-section |
| `design/dual-input-system-abstraction.md` | Design doc for `IInputProvider` — now shipped, archived |
| `design/oneui-vs-hybrid-recommendation.md` | Decision was made — archive |
| `design/ui-architectural-concepts-comparison.md` | Decision was made — archive |
| `design/ui-architectural-concepts-recommendation.md` | Decision was made — archive |
| `design/ui-architectural-concepts-template.md` | Decision was made — archive |
| `devsdaddy-oneui-analysis.md` | Analysis doc — archive |
| `modular-ui-kit-analysis.md` | Analysis doc — archive |
| `ui-systems-comparison.md` | Decision was made — archive |

---

### 🔌 Plugin Docs (Archive, Key Facts Extracted)

| File | Key Facts to Preserve |
|:---|:---|
| `plugins/le-tai-assets-architecture.md` | `LeTai.TranslucentImage` + `LeTai.TrueShadow` — critical for blur/glassmorphism in CharqUI sandbox. API: `TranslucentImage` component, `TrueShadow` component |
| `plugins/attributes-utility-architecture.md` | `ConditionalFieldAttribute` — lightweight, keeps Inspector clean |
| `plugins/awesome-attributes-architecture.md` | 17 attributes. Most useful: `[Button]`, `[ShowIf]`, `[ReadonlyIf]`, `[Required]`, `[Separator]`. Asmdef: `AwesomeAttributes.Attributes` |
| `plugins/lokosolo-architecture.md` | `PinchableScrollRect` — needed for mobile scroll. Asmdef: `LokoSolo.PinchableScrollRect` |
| `plugins/ui-particles-architecture.md` | `Coffee.UIParticle` — renders Shuriken particles inside uGUI Canvas |
| `plugins/better-folders-architecture.md` | Editor-only. No runtime impact. |

---

## What the New Docs Need to Cover

### Missing entirely from legacy docs:
1. **VitalRouter command patterns** — `ICommand` structs, `ICommandInterceptor`, async pipeline, `Router.Default.PublishAsync`
2. **R3 reactive binding patterns** — `ReactiveProperty<T>`, `CompositeDisposable`, `.Subscribe()`, `.AddTo()`, `.Where()`, `.Select()`
3. **ViewModel lifecycle** — `Initialize()`, `Dispose()`, sharing state across views
4. **ThemeProvider API** — how to create `ThemeData` assets, `SetTheme()`, `ThemeSubscriber` / `ThemeImageSubscriber` / `ThemeTextSubscriber`
5. **AnimationBridge API** — `FadeAsync()`, `ScaleAsync()`, `SceneTransitionAsync()` with `CancellationToken`
6. **MaquiNavigator** — `NavigateReactive<TView, TViewModel>()`, `CleanupViewModel<T>()`
7. **UPM package setup** — how to install `com.ware.maqui` as a local or Git dependency
8. **Sample walkthroughs** — technical breakdown of all 4 samples
9. **Testing guide** — running package tests from the CharqUI sandbox via testables

---

## Recommendation

Move all 30 legacy docs to `docs/legacy/` without modification. Create a fresh `docs/` structure targeting Unity Asset Store customers (Unity 2022.3 LTS+, URP 17.x, senior developers):

```
docs/
├── docs-evaluation.md          ← this file
├── 01-architecture.md          ← system topology, pillars, layer model
├── 02-quick-start.md           ← install, first screen, 10-minute path
├── 03-core-concepts.md         ← ViewModel, ReactiveBaseView, disposables
├── 04-routing-commands.md      ← VitalRouter integration, interceptors
├── 05-theming.md               ← ThemeData, ThemeProvider, ThemeSubscribers
├── 06-animation-transitions.md ← AnimationBridge, UniTask patterns, DisplayOptions
├── 07-hybrid-rendering.md      ← uGUI vs UI Toolkit decision matrix
├── 08-performance.md           ← Canvas optimization, profiling, URP 17.x notes
├── 09-samples-reference.md     ← all 4 samples, what each demonstrates
└── legacy/                     ← all original 30 docs, unmodified
```
