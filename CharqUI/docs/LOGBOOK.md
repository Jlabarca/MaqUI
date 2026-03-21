# Maqui — Session Logbook

> Append-only. Each session adds an entry at the bottom. Never edit previous entries.

---

## 2026-01-08 — Design Finalized

Architecture and roadmap files created. Decided on Reactive MVVM (R3 + VitalRouter + UniTask) over four alternatives (State-Store/Redux, UI Toolkit-First, Blueprint Flow, Procedural-only). Chose hybrid uGUI + UI Toolkit rendering.

## 2026-01-09 — Core Foundation Deployed

Implemented: `ViewModel`, `ReactiveBaseView<T>`, `InputBridge`, `CharqUINavigator` (later `MaquiNavigator`). Integrated R3 and VitalRouter. Built Sample 3 (SharkSuite) — hybrid inventory with UI Toolkit list + uGUI detail panel.

## 2026-01-10 — Bug Fixes + Grand Tour

Fixed VitalRouter 2.0 signature issues and Gradient class ambiguity. Upgraded UIFrameDemo to "Grand Tour" stress test (multi-VM shared state across 3 rendering paradigms).

## 2026-03-12 — v0.1.0 Assessment (Mea Culpa)

Honest review of v0.1.0. Identified 13 gaps. Core architecture validated (MVVM + R3 + VitalRouter is solid). Major issues: singletons, no pooling, no reactive collections, minimal tests, no error handling. Renamed project from CharqUI to Maqui.

## 2026-03-XX — v0.1.0 Hardening

Fixed 4 of 5 "must fix" gaps:
- Replaced all singletons with `MaquiServices` static service locator (interface-based, `Reset()` for test isolation)
- Added `ReactiveList<T>` granular reactive collection
- Added `WindowPool` with opt-in per-asset-key object pooling
- Added `WindowLoadFailed` error event + null-return error handling
- Expanded test coverage to 13 runtime + 1 editor test suites
- Added `MaquiBaseView` self-contained base class

## 2026-03-21 — Documentation Reorganization

Reorganized docs/ from flat numbered files + legacy/ dump into protocol-driven structure: reference/, guides/, design/, research/, tools/, archive/. Created CONTEXT.md as single source of truth, LOGBOOK.md for session history, DOCS-PROTOCOL.md for maintenance rules. Consolidated 6 legacy design comparison docs into one frozen decision record.

Wired documentation maintenance rules into CLAUDE.md (6 rules for doc updates). Added Documentation section to root README.md and package README.md. Fixed stale singleton references across both READMEs (MaquiWindowManager.Instance → MaquiServices.Get, [Subscribe] → [Route], missing MapTo in OnBind). Removed OneUI as core dependency claim (only samples reference it). Updated package.json description.

Fixed Router wiring anti-pattern in all 7 demo views — moved `this.MapTo(Router.Default)` from `Start()` to `OnBind()` with `.AddTo(Disposables)` for pool-safe lifecycle. Verified ShopViewModel.Catalogue and FrostedHUDViewModel.Notifications are static catalogs (ReactiveProperty<IReadOnlyList<T>> correct, no ReactiveList conversion needed). Updated CONTEXT.md demo section to reflect actual state.

Removed OneUI/UIFramework dependency from all 3 sample asmdefs (Welcome, SharkSuite, GrandTour). Rewrote WelcomeRouterInterceptor.cs to use only Maqui-native APIs (MaquiServices, IAnimationBridge) instead of OneUI's UIFramework.GetView and DisplayOptions.

Added navigation stack to MaquiWindowManager: per-layer Stack<IWindowHandle>, PopWindow(), GetStackDepth(), BackRequested event, SuppressBackNavigation flag. Added OnBackRequested() virtual hook to ReactiveBaseView<T> (return true to consume). Escape/Cancel detection in Update() via InputBridge with legacy Input fallback. Internal IBackRequestable interface. 8 new tests covering empty pop, topmost disposal, depth tracking, per-layer independence, back consumption, suppression, out-of-order disposal, and push-on-show.

Wrote reference/shared-state.md documenting the two blessed VM-to-VM communication patterns: shared ViewModel instance (Demo3 pattern) and commands + per-window VMs (Demo2 pattern). Includes decision matrix and 6 anti-patterns.

Added asset key validation: runtime null/whitespace rejection, backslash normalization with warning, component mismatch suggestions ("expected X but found Y"). Editor validator menu (Maqui/Validate Asset Keys) scans Resources/Views/ prefabs for CanvasGroup and MaquiBaseView. 5 new tests.

Added MaquiDebugWindow (Window > Maqui > Debug Window): shows service registration status, modal/freeze state, per-layer window stacks with foldouts, and "Pop Topmost" navigation button. Play-mode only with auto-repaint. Added InternalsVisibleTo for Maqui.Editor assembly access to internal DebugInfo struct.

All 13 original v0.1.0 gaps assessed: 10 fixed, 3 remaining (animation sequencing, theme inheritance, configurable bootstrap — all "nice to have").
