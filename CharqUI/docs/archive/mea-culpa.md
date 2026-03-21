# Maqui — Mea Culpa

**Status:** Honest assessment of where we are
**Date:** 2026-03-12
**Context:** Maqui v0.1.0, pre-ORO integration

---

## The Good News

The bones are solid. MVVM + R3 + VitalRouter is a strong stack, and the decisions that matter most are right:

- **Pure C# ViewModels** — no UnityEngine coupling, fully testable in isolation
- **R3 reactive binding** with proper `CompositeDisposable` cleanup everywhere
- **Layer-based window manager** with automatic modal freeze/unfreeze coordination
- **Zero-config bootstrap** via `RuntimeInitializeOnLoadMethod` — no scene setup, no prefab dragging
- **Interface-based asset loading** (`IMaquiAssetProvider`) — swap Resources for Addressables/YooAsset without touching views
- **VitalRouter command routing** with interceptor chains — clean cross-cutting concerns
- **Hybrid uGUI + UI Toolkit** support from the same ViewModel

For a small-to-mid project, this works today. For ORO — a big multiplayer game — it needs hardening.

---

## What We Got Wrong

### 1. Singletons Everywhere

`InputBridge.Instance`, `ThemeProvider.Instance`, `AnimationBridge.Instance`, `MaquiWindowManager.Instance` — all singletons. This makes unit testing views painful (can't mock dependencies), creates hidden coupling, and will bite us in integration tests.

**Fix:** Expose an `IMaquiServices` interface that views receive, or adopt a lightweight DI container (VContainer). At minimum, make the singleton access mockable behind interfaces.

### 2. No Object Pooling for Windows
devsda
`ShowWindowAsync` instantiates. `Dispose()` destroys. In ORO we'll open/close inventory, tooltips, and damage numbers hundreds of times per session. That's a lot of GC pressure and instantiation cost.

**Fix:** Add a `WindowPool` or at least a `reuse: bool` parameter to recycle frequently-used windows instead of destroy/recreate cycles.

### 3. Reactive Collections Are Missing

ViewModels use `ReactiveProperty<T>` for scalars, but there's no `ReactiveList<T>` or `ObservableCollection` pattern. Inventory, quest logs, chat, party frames — any list-based UI needs granular add/remove/move notifications, not full-list replacement via `ReactiveProperty<IReadOnlyList<T>>`.

R3 has `ObservableList`. We should surface it as a first-class pattern with documented usage.

**Fix:** Add `ObservableList<T>` examples and base patterns. Document when to use list-replacement vs granular notifications.

### 4. Test Coverage Is Minimal

Three test files: `ViewModelTests.cs`, `ThemeDataTests.cs`, `MaquiEditorTests.cs`. That covers disposal and color lookup. Missing:

- View lifecycle tests (bind, freeze, unfreeze, destroy sequence)
- Window manager layer logic (sort order, modal mask activation)
- Modal freeze/unfreeze cascading
- Navigation flows
- Command interceptor chains
- Asset provider error paths

For a big game, this is the first thing that slows you down when refactoring.

**Fix:** Write integration tests for `MaquiWindowManager` lifecycle. Add a test harness that can instantiate views without a full Unity scene.

### 5. No ViewModel-to-ViewModel Communication Pattern

Shared state is ad-hoc — GrandTour passes `GlobalAppStateViewModel` via constructor, the ORO design doc says "register as a service and `HF.Get<T>()`". That's two different approaches already, and neither is codified in the framework.

**Fix:** Document one blessed pattern for shared state. Likely: shared VMs registered in a service locator, per-window VMs created fresh. Make it a framework opinion, not a team decision.

### 6. No Error Handling or Fallback Strategy

`LoadPrefabAsync` can fail silently. No retry logic, no fallback UI, no error boundary concept. In a shipped game, asset loads fail — corrupted bundles, network timeouts for remote assets, missing keys.

**Fix:** Add an `OnWindowLoadFailed` hook or error view fallback. At minimum, log errors with `[Maqui]` prefix and return a null handle instead of throwing into the void.

### 7. Animation System Is Too Simple

`AnimationBridge` offers `FadeAsync`, `ScaleAsync`, `SceneTransitionAsync`. That's it. Real games need: sequenced/parallel animations, an easing curves library, interruptible transitions, animation cancellation that doesn't leave views in broken states.

**Fix:** Expose an `IAnimationSequence` builder behind the bridge interface. Consider integrating LitMotion or DOTween Pro as the backend, keeping the bridge as the public API.

### 8. No Prefab Validation or Compile-Time Safety

`ShowWindowAsync` takes a `string assetKey`. Typo = runtime crash with no useful error message. There's no editor tooling to validate that all referenced prefabs exist or that the view type on the prefab matches the generic parameter.

**Fix:** Add a code-generated enum or `[ViewAsset("path")]` attribute with an editor validator. At minimum, add runtime validation with a clear error message: "Asset key 'com.rompe.core/InvntoryWindow' not found. Did you mean 'InventoryWindow'?"

### 9. Theme System Lacks Depth

13 fixed color slots in `ThemeData` works for basic light/dark theming. Big games need: per-widget color overrides, theme inheritance (dark-base + "fire" accent for a dungeon), animated theme transitions (day/night cycle). The `ThemeData` ScriptableObject is flat.

**Fix:** Consider a hierarchical/cascading model where themes can inherit from a base and override specific slots. Not urgent — the current system works for ORO's initial needs.

### 10. No Runtime Debugging Tools

No way to see at runtime: active windows per layer, subscription counts, command history, theme state, freeze status. When something goes wrong in a 20-window game, you're reading console logs.

**Fix:** Build a `MaquiDebugWindow` — either a runtime overlay toggled by a key combo, or an Editor window that visualizes framework state. Show the layer hierarchy, active handles, modal count, and recent commands.

### 11. CoreBootstrap Is Not Configurable

It always creates all bridges in a fixed order. No way to run headless (server/tests without UI), skip specific bridges, or customize initialization order.

**Fix:** Make bootstrap data-driven via a `MaquiConfig` ScriptableObject, or at least add `[RuntimeInitializeOnLoadMethod]` guards that check for a "headless" flag.

### 12. No Navigation Stack or Back-Button Support

`MaquiNavigator` exists but there's no history stack. Console games need B-button/back support. Mobile needs swipe-back. Even PC games benefit from Escape closing the topmost panel.

**Fix:** Add `PushView`/`PopView` with a navigation stack and `OnBackRequested` hook. The window manager already tracks active handles per layer — build on that.

### 13. Missing Localization Hook

Views bind text directly to `ReactiveProperty<string>`. No integration point for localization. When we add Korean/Japanese/Portuguese to ORO, every text binding needs to change.

**Fix:** Add a `LocalizedReactiveProperty<string>` or a `Localize(key)` extension that auto-updates when locale changes. Or at minimum, document the pattern for wrapping localization systems with R3.

---

## Priority

| Tier | Items | Why |
|:---|:---|:---|
| **Must fix before scaling** | #1 (DI), #3 (reactive lists), #4 (tests), #6 (error handling) | Architectural pain that compounds at scale |
| **Should fix soon** | #2 (pooling), #5 (VM communication), #8 (asset key safety), #12 (nav stack) | DX friction that slows every developer down |
| **Nice to have** | #7 (animations), #9 (theme depth), #10 (debug tools), #11 (config), #13 (localization) | Quality-of-life, can wait for demand |

---

## Bottom Line

Maqui v0.1.0 is a good prototype that proves the architecture works. It is not yet a framework you'd hand to a team of 5+ developers and say "build a big game on this." The gap is mostly about robustness (error handling, testing, validation) and scale patterns (pooling, reactive collections, shared state conventions).

The path forward is clear. The foundation doesn't need to change — it needs to grow up.
