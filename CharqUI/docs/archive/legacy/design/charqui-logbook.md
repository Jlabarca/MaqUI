# CharqUI Implementation Logbook

This document tracks the phased implementation of the CharqUI (Hybrid Reactive) system.

---

## 📅 Implementation Roadmap

### Phase 1: Core Foundation (The "Heart")
*Goal: Re-condition OneUI into a Reactive framework.*

- [x] **Infrastructure**: Implement the `IInputProvider` and `InputBridge`.
- [x] **Reactive Core**: Integrate **R3** as the primary data-binding engine.
- [x] **Base Classes**: Create `ReactiveBaseView<T>` and `ViewModel` base classes.
- [x] **OneUI Refit**: Modify `UIFramework` to support Reactive initialization (via `CharqUINavigator`).
- [x] **Messaging**: Integrate **VitalRouter** for unidirectional command flow.

### Phase 2: Visual Engine (The "Skin")
*Goal: Re-condition Modular Kit into a Procedural Theme system.*

- [x] **Theme Provider**: Create the `ThemeProvider` ScriptableObject and R3 bridge.
- [/] **Modular Refit**: Update `Ricimi.Gradient` and others to be `ThemeSubscribers`.
- [x] **Animation Bridge**: Port and optimize Modular Kit's `Transition` and `Popup` logic (via `AnimationBridge`).

### Phase 3: Sample 1 - Legacy Refit
*Goal: Prove OneUI conversion.*

- [x] Port a standard OneUI window (e.g., WelcomeView) to `ReactiveBaseView`.
- [x] Replace static Payloads with Reactive Property bindings.
- [x] Verify Legacy Input still works via the `InputBridge`.

### Phase 4: Sample 2 - Visual Modernization
*Goal: Prove Modular Kit procedural power.*

- [x] Build a screen using zero textures (100% Procedural Gradients/Meshes).
- [x] Bind visual states (Colors/Shapes) to a ViewModel.
- [x] Verify low memory and draw-call count.

### Phase 5: Sample 3 - The "Shark" Suite
*Goal: The definitive hybrid showcase.*

- [x] **UI Toolkit List**: Build an Inventory list using UXML.
- [x] **uGUI Details**: Build the item details panel using uGUI + Modular Kit.
- [x] **Dual Input**: Full controller and mouse/keyboard navigation (via `InputBridge`).

---

## 🛠 Progress Log (Live)

| Date       | Milestone         | Status          | Notes                                                                |
| :--------- | :---------------- | :-------------- | :------------------------------------------------------------------- |
| 2026-01-08 | Design Finalized  | ✅ DEPLOYED      | Architecture and Roadmap files created.                              |
| 2026-01-09 | Sample 3: Shark   | ✅ DEPLOYED      | Hybrid Inventory (UI Toolkit + uGUI) implemented.                    |
| 2026-01-10 | bug Fixes         | ✅ DEPLOYED      | Fixed VitalRouter 2.0 signature and Gradient ambiguity.              |
| 2026-01-10 | Testing Evolution | [/] IN PROGRESS | Upgraded UIFrameDemo to "The Grand Tour" Stress Test.                |
| 2026-01-09 | Core Foundation   | ✅ DEPLOYED      | ViewModel, ReactiveBaseView, InputBridge, and Navigator implemented. |

---

## 📋 Technical Debt & Bottlenecks
- [x] **Reactive Library**: Deciding between UniRx (Heavy) vs R3 (Modern) vs Custom (Lightweight). Decision: **R3** (Modern, High Performance).
- [ ] **Input System**: Ensure New Input System and Legacy don't conflict (handled by `InputBridge`).
