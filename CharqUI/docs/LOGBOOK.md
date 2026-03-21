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
