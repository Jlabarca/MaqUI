# Maqui — Foundation Decisions

> Frozen design record. Captures the architectural decisions that shaped Maqui.
> Consolidated from 6 pre-Maqui design documents (Jan 2026).
> These decisions are locked — this document explains WHY, not current state.

---

## Decision 1: Reactive MVVM over Manual Binding

The predecessor framework (DevsDaddy OneUI) used an event-driven payload model: data changes triggered a published payload, and every view manually caught that payload and updated its own text fields (`text.text = data`). This created three problems:

1. **Boilerplate explosion.** Every view needed explicit subscribe/unsubscribe logic and manual field-setting code. The MVVM reactive approach eliminates an estimated 40-60% of UI script boilerplate.
2. **Desync risk.** When multiple screens display the same data (e.g., a health bar on the HUD and the character panel), each view needed its own `OnDataUpdated` handler. Forgetting one caused silent desync. With reactive properties, all views observe the same source of truth — desync becomes structurally impossible.
3. **Testability.** OneUI relied on static classes and singletons, making unit testing and mocking impractical. Pure C# ViewModels with no `UnityEngine` references can be tested in isolation without a running Unity editor.

The "Mediated Reactive" (MVVM) pattern was selected over four alternatives that were formally evaluated:
- **State-Store (Redux)**: Too much boilerplate (Action + Reducer per change) for the expected screen count; undo/redo not needed.
- **UI Toolkit-First**: Rejected as primary due to limited 3D/VFX integration (needed for world-space UI and particle effects).
- **Blueprint Flow (Visual Graph)**: Required custom editor tooling and risked graph spaghetti; better suited to branching narratives than game HUDs.
- **Procedural Theme Engine (standalone)**: Adopted as a visual layer, but insufficient as an architectural backbone.

MVVM was judged the best balance of iteration speed, long-term maintainability, and logic portability — the ViewModel layer survives a complete rendering engine swap.

---

## Decision 2: Hybrid Rendering (uGUI + UI Toolkit)

Rather than choosing one rendering system, both coexist under a single decision rule:

| Use uGUI when... | Use UI Toolkit when... |
|:---|:---|
| The screen needs world-space placement | The screen is a data-heavy scrolling list (100+ items) |
| Complex shaders, particles, or 3D model integration | Web-like layout iteration (CSS Flexbox semantics) |
| Pixel-perfect drag-and-drop is required | Minimizing CPU "Canvas Rebuild" spikes is critical |

**Why not uGUI-only?** Classic uGUI's Canvas Rebuild system causes CPU spikes when animating complex layouts. For a 1,000-item inventory, UI Toolkit's retained-mode layout engine handles virtualization with near-zero CPU cost compared to uGUI's destroy-and-reinstantiate pattern.

**Why not UI Toolkit-only?** UI Toolkit in Unity 2022.3 LTS lacks mature support for world-space canvases, particle integration, and the deep shader/VFX pipeline that uGUI provides. HUD elements with glow effects, 3D model previews, and canvas-integrated particles require uGUI.

The architectural guarantee: because the logic layer (ViewModels) has zero Unity references, the same ViewModel can drive either a uGUI `ReactiveBaseView` or a UI Toolkit controller. Swapping rendering technology never touches business logic.

---

## Decision 3: Procedural Theming over Texture-Based

OneUI relied on sprite-based visuals — separate textures for every button state (blue, red, glowing, disabled). This caused:

- **Draw call fragmentation.** Each texture variant broke GPU batching.
- **Asset bloat.** Multiple sprite sheets per theme multiplied project size.
- **Inflexibility.** Changing the accent color meant re-exporting every asset.

The Modular Game UI Kit demonstrated that procedural mesh generation and gradient shaders could produce the same visual quality with a single material. The `ThemeData` ScriptableObject approach (13 named color slots) allows project-wide color changes by swapping one asset.

The trade-off is intentional: traditional 2D artists cannot contribute by dropping PSD exports into the project. All visual styling is code-driven or ScriptableObject-driven, requiring some technical art knowledge. For a one-person company, this is acceptable; the flexibility and performance gains outweigh the traditional-artist workflow loss.

---

## Decision 4: Dependency Stack

Four core dependencies were selected, each replacing a specific legacy pattern:

| Dependency | Replaces | Rationale |
|:---|:---|:---|
| **R3** (Reactive Extensions) | UniRx / manual `OnDataUpdated` | R3 is the official successor to UniRx by the same author (neuecc). Zero-allocation observable chains, native Unity lifecycle integration, `CompositeDisposable` for clean teardown. |
| **VitalRouter** | OneUI `EventMessenger` (static) | Zero-allocation message passing via Roslyn source generators. Type-safe commands replace stringly-typed payloads. Attribute-driven routing (`[Routes]`, `[Route]`) eliminates manual subscribe/unsubscribe boilerplate. Interceptor pipelines handle cross-cutting concerns. |
| **UniTask** | Standard coroutines | No GC allocation per async operation. Proper `CancellationToken` support for UI transitions and async loading. Eliminates coroutine lifecycle bugs. |
| **AwesomeAttributes** | Raw `[SerializeField]` | Inspector DX standardization (conditional fields, button attributes, read-only display). Development-only dependency. |

The key constraint: every "action" in the UI flows through VitalRouter. This makes application state traceable and predictable — any command can be logged, intercepted, or replayed without modifying the handler.

---

## Decision 5: OneUI Heritage — What Was Kept vs Replaced

Maqui (originally "CharqUI") was explicitly designed as "OneUI 2.0" — an evolution, not a rewrite from scratch.

### Kept from OneUI

| Element | How it was preserved |
|:---|:---|
| **View lifecycle** | `BaseView`'s lifecycle hooks (start, show, hide, destroy) carried forward into `ReactiveBaseView` and `MaquiBaseView`. |
| **Navigation/framework shell** | OneUI's `UIFramework` navigator concept evolved into `MaquiWindowManager` with layer-aware canvases. |
| **Decoupled architecture philosophy** | OneUI's core insight — UI logic should not live in MonoBehaviour callbacks — was the foundation Maqui built on. |

### Replaced from OneUI

| Element | Replacement | Why |
|:---|:---|:---|
| `EventMessenger` (static event bus) | VitalRouter command routing | Static singletons blocked testing; stringly-typed payloads caused runtime errors. |
| Manual `OnDataUpdated` binding | R3 reactive subscriptions in `OnBind()` | Eliminated 40-60% boilerplate; made desync impossible. |
| Payload classes | Typed command structs (`readonly record struct : ICommand`) | Payloads mixed data transport with event signaling. Commands are pure intent. |
| Singleton service access | `MaquiServices` service locator with interface registration | Enables test isolation via `Reset()` and interface-based mocking. |
| Sprite-based visuals | Procedural `ThemeData` + `ThemeSubscriber` | Draw call batching, asset size reduction, instant project-wide restyling. |
| Coroutine-based transitions | UniTask async methods (`FadeAsync`, `ScaleAsync`) | Cancellation support, zero GC, composable async chains. |

### Modular Game UI Kit Contributions

The Modular Kit was never adopted as a framework — only its visual techniques were extracted:
- `Gradient.cs` procedural backgrounds became `ThemeSubscriber` reactive theme components.
- `Popup` / `Transition` animation patterns were absorbed into `AnimationBridge`.
- The kit's component-based "drag and drop" architecture was explicitly rejected for anything beyond prototyping, as it caused logic fragmentation at scale.
