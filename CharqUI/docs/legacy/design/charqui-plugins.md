# CharqUI Plugin Ecosystem

This document classifies and recommends plugins for the CharqUI ecosystem. To achieve the "Ultimate DX" and peak performance, we categorize these tools into Core Dependencies, Optional External Tools, and Deprecated/Redundant tools.

---

## 1. Core Ecosystem Matrix

These plugins are the "Engine" of CharqUI. They should be included as standard dependencies to ensure the framework functions as intended.

| Plugin                | Primary Role          | Why it is Core                                                                                                             |
| :-------------------- | :-------------------- | :------------------------------------------------------------------------------------------------------------------------- |
| **VitalRouter**       | Messaging / Event Bus | **Central Messaging Pillar**. Provides zero-allocation, unidirectional control flow to replace heavy legacy event systems. |
| **R3**                | Reactive Extensions   | **Data Binding Engine**. Powers the "Reactive" part of CharqUI, allowing automatic UI updates from ViewModels.             |
| **UniTask**           | Async Workflow        | **Concurrency Engine**. Eliminates Coroutine bloat for UI transitions and async loading.                                   |
| **AwesomeAttributes** | Inspector DX          | **Development Standard**. Standardizes how we configure UI components in the Inspector via declarative attributes.         |

---

## 2. Integration: VitalRouter + CharqUI

**VitalRouter** is the chosen messaging backbone for CharqUI. It replaces the legacy `EventMessenger` with a high-performance, attribute-driven routing system.

### The "Unidirectional" Pattern
Instead of Views talking directly to Models, they publish **Commands** to the Router.

```csharp
[Routes]
public partial class InventoryPresenter : ReactiveBaseView<InventoryViewModel>
{
    [Route]
    void On(EquipItemCommand cmd)
    {
        // One-way logic flow
        ViewModel.UpdateEquipment(cmd.ItemId);
    }
}
```

- **DX Advantage**: Decouples logic (Presenters/ViewModels) from the View implementation.
- **Performance**: Zero-allocation message passing via Roslyn SourceGenerators.

### Advanced Patterns

#### 1. Interceptor Pipelines (Global Logic)
Use Interceptors for cross-cutting concerns like logging or sound effects without bloating your Presenters.

```csharp
public class UILoggingInterceptor : ICommandInterceptor
{
    public async UniTask InvokeAsync<T>(
        T command, 
        PublishContext context, 
        CommandExecutionDelegate next) where T : ICommand
    {
        Debug.Log($"[CharqUI] Publishing: {command.GetType().Name}");
        await next(command, context);
    }
}
```

#### 2. Sequential Command Ordering
Prevent "Button Spam" by ensuring commands are executed one after the other.

```csharp
[Route(CommandOrdering.Sequential)]
async UniTask On(SubmitFormCommand cmd, CancellationToken ct)
{
    await APIService.Submit(cmd.Data);
    // UI remains responsive but this handler won't re-run until finished
}
```

#### 3. R3 Integration (Reactive Command Streams)
Bridge global events directly into your Reactive ViewModels.

```csharp
// In your ViewModel
public class ShopViewModel : ViewModel 
{
    public void Initialize(Router router) 
    {
        // Convert VitalRouter commands into an R3 Observable stream
        router.ToObservable<ItemPurchasedCommand>()
              .Subscribe(cmd => {
                  Balance.Value -= cmd.Price;
              })
              .AddTo(Disposables);
    }
}
```

---

## 3. Optional External Tools

These assets are highly recommended for specific use cases (Visuals, Tooling) but are not strictly required for the Core framework.

| Asset               | Category    | Recommendation                                                                                      |
| :------------------ | :---------- | :-------------------------------------------------------------------------------------------------- |
| **Le Tai's Assets** | Visuals     | **Elite Glass & Shadows**. Use for premium mobile aesthetics (Translucent Image / True Shadow).     |
| **Odin Inspector**  | Tooling     | **Elite Inspector DX**. If the budget allows, use for 10x faster data entry and custom drawers.     |
| **Shapes**          | Visuals     | **Vector Graphics**. Best-in-class performance for procedural 2D/3D shapes (HP bars, radial fills). |
| **UI Particles**    | Visuals     | **Canvas Integration**. Use only for high-impact VFX (e.g., Rewards / Level Up).                    |
| **LokoSolo**        | Interaction | **Mobile Gestures**. Essential for maps or zoomable inventories.                                    |
| **SRDebugger**      | Quality     | **On-Device Debugging**. Critical for live-testing UI states on target hardware.                    |

---

## 4. Tools to AVOID / Deprecate

To keep CharqUI lean, avoid using these older or redundant patterns:

*   **Legacy Unity UI Shadows**: Replaced by **True Shadow** or **Procedural SDF Quads** (Lower draw calls).
*   **OneUI `EventMessenger` (Static)**: Replaced by **VitalRouter** (Zero-allocation / Type-safe).
*   **Standard Coroutines**: Replaced by **UniTask** (Better performance / No GC).
*   **Manual `OnDataUpdated` Catching**: Replaced by **R3 Data-Binding** (Automatic sync).

---

> [!IMPORTANT]
> **CharqUI Architecture Constraint**: Every "Action" in the UI should go through **VitalRouter**. This ensures that the state of your application is always traceable and predictable.

> [!TIP]
> Use the **[Visual FX Optimization Guide](../design/charqui-visual-fx-optimization.md)** in conjunction with your choice of **Le Tai** or **Shapes** assets for the best results.
