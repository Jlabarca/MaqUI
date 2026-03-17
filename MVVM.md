# Why Maqui Is MVVM

Maqui is a **Model-View-ViewModel** UI framework for Unity. This document explains how each MVVM layer maps to Maqui's architecture and why it matters.

---

## The Short Version

| MVVM Layer | Maqui Class | Responsibility |
|:--|:--|:--|
| **Model** | Your game data (C# POCOs, services, APIs) | Truth — inventory DB, player stats, server state |
| **ViewModel** | `ViewModel` + `ReactiveProperty<T>` | Exposes Model data as observable state, handles user intent |
| **View** | `ReactiveBaseView<T>` | Subscribes to ViewModel, renders UI, forwards input back |

The ViewModel never knows the View exists. The View never touches the Model directly. Data flows in one direction through reactive bindings.

```mermaid
---
config:
  theme: dark
---
graph LR
    M[Model<br/><i>game data, services</i>] -->|pushes data| VM[ViewModel<br/><i>ReactiveProperty, commands</i>]
    VM -->|observable binding| V[View<br/><i>ReactiveBaseView</i>]
    V -->|user actions| VM
    VM -->|mutations| M
    style M fill:#2d5a3d,stroke:#4a9,color:#fff
    style VM fill:#2d3d5a,stroke:#49a,color:#fff
    style V fill:#5a2d3d,stroke:#a49,color:#fff
```

---

## Layer by Layer

### Model — Your Data

The Model is whatever your game already has: database records, network responses, ScriptableObjects, save files. Maqui doesn't define or constrain the Model layer. It just expects the ViewModel to translate it.

```csharp
// This is your game code, not Maqui
public class InventoryItem
{
    public string Id { get; set; }
    public string Name { get; set; }
    public int PowerLevel { get; set; }
}
```

### ViewModel — Observable State

A ViewModel extends `Maqui.Core.Logic.ViewModel`. It holds **reactive properties** that the View will subscribe to, and **methods** that represent user intent.

Key rules:
- No `using UnityEngine` — pure C#, fully testable without Unity
- State lives in `ReactiveProperty<T>` (single values) or `ReactiveList<T>` (collections)
- Subscriptions are tracked in `Disposables` for automatic cleanup

```csharp
public class WelcomeViewModel : ViewModel
{
    // Reactive state — the View subscribes to these
    public readonly ReactiveProperty<string> Title = new("Welcome to Maqui");
    public readonly ReactiveProperty<string> Description = new("Reactive MVVM for Unity.");

    // User intent — the View calls this, the ViewModel decides what happens
    public void StartExperience()
    {
        _ = Router.Default.PublishAsync(new NavigateToHomeCommand());
    }
}
```

The ViewModel talks *up* to the Model (fetching data, sending mutations) and exposes *down* to the View via reactive properties. It never references a `MonoBehaviour`, a `GameObject`, or any Unity type.

### View — Reactive Rendering

A View extends `ReactiveBaseView<T>` where `T` is its ViewModel type. All binding happens in one method: `OnBind()`.

```csharp
public class WelcomeView : ReactiveBaseView<WelcomeViewModel>
{
    [SerializeField] private TMP_Text TitleText;
    [SerializeField] private TMP_Text DescriptionText;
    [SerializeField] private Button StartButton;

    protected override void OnBind()
    {
        // ViewModel → View (data binding)
        ViewModel.Title
            .Subscribe(text => TitleText.text = text)
            .AddTo(Disposables);

        ViewModel.Description
            .Subscribe(text => DescriptionText.text = text)
            .AddTo(Disposables);

        // View → ViewModel (user intent)
        StartButton.onClick.AsObservable()
            .Subscribe(_ => ViewModel.StartExperience())
            .AddTo(Disposables);
    }
}
```

The View knows *what* to display and *where* user input goes, but never *why* or *how* the data was produced.

---

## How Data Flows

```mermaid
---
config:
  theme: dark
---
sequenceDiagram
    participant M as Model
    participant VM as ViewModel
    participant V as View

    Note over VM: Initialize()
    M->>VM: Load items from DB/API
    VM->>VM: Items.Value = loadedItems

    Note over V: OnBind()
    VM-->>V: ReactiveProperty emits value
    V->>V: Update UI elements

    Note over V: User clicks button
    V->>VM: ViewModel.SelectItem(item)
    VM->>VM: SelectedItem.Value = item
    VM-->>V: SelectedItem emits new value
    V->>V: Highlight selected row
```

Every arrow is explicit. There's no hidden magic, no reflection-based binding, no string-keyed lookups. You write the subscription in `OnBind()`, and R3 handles the plumbing.

---

## Why MVVM (and not MVC, MVP, or raw MonoBehaviours)

### The problem with putting logic in MonoBehaviours

In a typical Unity project, a `MonoBehaviour` does everything: holds state, handles input, updates visuals, talks to the network. This works for prototypes. It falls apart when:

- You want to **unit test** UI logic without booting Unity
- Two screens need to **share state** (inventory count in the HUD *and* the inventory panel)
- You need to **swap the view** (uGUI today, UI Toolkit tomorrow) without rewriting logic
- A designer wants to **rearrange the UI** without breaking game logic

### What MVVM gives you

```mermaid
---
config:
  theme: dark
---
graph TB
    subgraph "Without MVVM"
        MB[MonoBehaviour<br/>state + input + rendering + networking]
    end

    subgraph "With MVVM"
        VM2[ViewModel<br/>state + logic]
        V2[View<br/>rendering + input forwarding]
        VM2 <-->|reactive binding| V2
    end

    style MB fill:#5a3333,stroke:#a66,color:#fff
    style VM2 fill:#2d3d5a,stroke:#49a,color:#fff
    style V2 fill:#5a2d3d,stroke:#a49,color:#fff
```

| Benefit | How Maqui delivers it |
|:--|:--|
| **Testable logic** | ViewModels are pure C# — test with NUnit, no `PlayMode` required |
| **Shared state** | Two Views subscribe to the same ViewModel's `ReactiveProperty` |
| **Swappable views** | Replace the View class; ViewModel stays identical |
| **Designer-safe** | Prefab layout changes don't touch ViewModel code |
| **Automatic cleanup** | `Disposables` + `CompositeDisposable` — no forgotten `OnDestroy` unsubscribes |

---

## Reactive Binding: The Glue

The connection between ViewModel and View is **R3** — a reactive extensions library. Instead of polling or callbacks, R3 lets the View *declare* what it cares about.

```csharp
// Traditional (imperative) — you must remember to call this everywhere
void UpdateUI() {
    titleText.text = _title;    // Who calls this? When? What if you forget?
}

// Maqui (reactive) — declared once, runs automatically
ViewModel.Title
    .Subscribe(text => titleText.text = text)
    .AddTo(Disposables);
// Any time Title.Value changes, the UI updates. No manual calls. No forgotten paths.
```

For collections, `ReactiveList<T>` provides granular events instead of rebuilding the entire list:

```csharp
// ViewModel
public readonly ReactiveList<InventoryItem> Items = new();

// View — OnBind()
ViewModel.Items.ObserveAdd()
    .Subscribe(e => InsertRow(e.Index, e.Item))
    .AddTo(Disposables);

ViewModel.Items.ObserveRemove()
    .Subscribe(e => RemoveRow(e.Index))
    .AddTo(Disposables);
```

---

## The Full Picture

```mermaid
---
config:
  theme: dark
---
graph TB
    subgraph Model["Model Layer"]
        DB[(Game Data)]
        API[Network API]
    end

    subgraph ViewModel["ViewModel Layer — Pure C#"]
        VM[ViewModel]
        RP["ReactiveProperty&lt;T&gt;"]
        RL["ReactiveList&lt;T&gt;"]
        VM --- RP
        VM --- RL
    end

    subgraph View["View Layer — MonoBehaviour"]
        RBV["ReactiveBaseView&lt;T&gt;"]
        UGUI[uGUI Elements]
        RBV --- UGUI
    end

    subgraph Bridge["Bridge Layer — Services"]
        WM[MaquiWindowManager]
        AB[AnimationBridge]
        TP[ThemeProvider]
        IB[InputBridge]
    end

    DB --> VM
    API --> VM
    RP -->|"Subscribe().AddTo(Disposables)"| RBV
    RL -->|"ObserveAdd/Remove/Replace"| RBV
    RBV -->|"ViewModel.DoSomething()"| VM
    WM -->|manages lifecycle| RBV
    AB -->|animations| RBV
    TP -->|theme changes| RBV

    style DB fill:#2d5a3d,stroke:#4a9,color:#fff
    style API fill:#2d5a3d,stroke:#4a9,color:#fff
    style VM fill:#2d3d5a,stroke:#49a,color:#fff
    style RP fill:#2d3d5a,stroke:#49a,color:#fff
    style RL fill:#2d3d5a,stroke:#49a,color:#fff
    style RBV fill:#5a2d3d,stroke:#a49,color:#fff
    style UGUI fill:#5a2d3d,stroke:#a49,color:#fff
    style WM fill:#3d3d5a,stroke:#99a,color:#fff
    style AB fill:#3d3d5a,stroke:#99a,color:#fff
    style TP fill:#3d3d5a,stroke:#99a,color:#fff
    style IB fill:#3d3d5a,stroke:#99a,color:#fff
```

The Bridge layer (services like `MaquiWindowManager`, `AnimationBridge`, `ThemeProvider`) sits alongside the View — it manages lifecycle, animations, and theming so that Views stay focused on binding and rendering.

---

## Lifecycle: Who Creates What

```mermaid
---
config:
  theme: dark
---
sequenceDiagram
    participant App as Your Code
    participant WM as MaquiWindowManager
    participant V as View (prefab)
    participant VM as ViewModel

    App->>VM: new InventoryViewModel()
    App->>WM: ShowWindowAsync<InventoryView, InventoryVM>("key", layer, vm)
    WM->>WM: Load prefab from asset provider
    WM->>V: Instantiate prefab into layer canvas
    WM->>V: OnPreShowAsync() — async data loading
    WM->>V: Initialize(vm)
    V->>VM: vm.Initialize()
    V->>V: OnBind() — wire subscriptions

    Note over V,VM: Window is now live.<br/>Reactive bindings keep UI in sync.

    App->>WM: handle.Dispose()
    WM->>V: OnViewDestroy()
    V->>V: Disposables.Dispose()
    V->>VM: vm.Dispose()
    WM->>V: Destroy GameObject
```

The ViewModel is created *before* the View. The View receives it through `Initialize()`, never through `GetComponent` or `FindObjectOfType`. This inversion is what makes the ViewModel testable in isolation.

---

## Summary

Maqui is MVVM because it enforces a clean separation:

1. **ViewModels are pure C#** — no Unity dependencies, no MonoBehaviour inheritance
2. **Views only bind and render** — they subscribe to reactive state in `OnBind()` and forward user actions back to the ViewModel
3. **Data flows through reactive properties** — R3's `Subscribe` replaces manual update calls, polling, and event spaghetti
4. **Lifecycle is managed externally** — `MaquiWindowManager` handles creation, layering, pooling, and destruction so Views don't manage their own existence

The result: UI logic you can test without Unity, views you can swap without rewriting logic, and state you can share across screens without coupling them together.
