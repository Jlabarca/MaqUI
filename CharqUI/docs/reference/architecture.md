# Maqui — Architecture Reference

> **Package**: `com.ware.maqui` v0.1.0
> **Unity**: 2022.3 LTS+ | **Render Pipeline**: URP 17.x
> **Runtime deps**: R3, VitalRouter, UniTask, UIFramework (OneUI)

---

## 1. Design Philosophy

Maqui is a **reactive MVVM bridge layer** that sits between your game logic and Unity's rendering systems. It makes three hard guarantees:

1. **ViewModels have zero Unity dependencies** — they contain only C#, R3 observables, and VitalRouter commands. They can be unit-tested without entering Play Mode.
2. **The rendering layer is swappable** — your business logic never directly touches `GameObject`, `Canvas`, or `UIDocument`. Swapping uGUI for UI Toolkit requires changes only in `Presentation/`.
3. **Bootstrap is zero-config** — `CoreBootstrap` fires via `[RuntimeInitializeOnLoadMethod]` before any scene loads. You do not need a bootstrap scene, a prefab in your hierarchy, or a manually ordered Awake sequence.

---

## 2. Layer Model

```mermaid
---
config:
  theme: dark
---
graph TB
    subgraph Game["Game / Application Layer"]
        GS[Game Systems<br/>ScriptableObjects, Services]
    end

    subgraph Maqui["Maqui Package — com.ware.maqui"]
        subgraph Logic["Logic Layer (no UnityEngine refs)"]
            VM[ViewModel<br/>R3 ReactiveProperty&lt;T&gt;]
            CMD[ICommand Structs<br/>VitalRouter]
        end

        subgraph Bridge["Bridge Layer (MonoBehaviour singletons)"]
            RB[RouterBridge<br/>Router instance]
            TP[ThemeProvider<br/>ReactiveProperty&lt;ThemeData&gt;]
            AB[AnimationBridge<br/>UniTask animations]
            IB[InputBridge<br/>IInputProvider]
            MN[MaquiNavigator<br/>View factory]
        end

        subgraph Presentation["Presentation Layer (Unity View)"]
            RBV[ReactiveBaseView&lt;T&gt;<br/>inherits BaseView]
            TS[ThemeSubscriber<br/>ThemeImageSubscriber<br/>ThemeTextSubscriber]
        end
    end

    subgraph Deps["Dependencies (in CharqUI sandbox Assets)"]
        UF[UIFramework<br/>OneUI navigator + registry]
        EF[EventFramework<br/>EventMessenger bus]
        OUI[OneUI Components<br/>prefabs + shaders]
    end

    GS -->|publish commands| CMD
    CMD -->|Router.Default.PublishAsync| RB
    RB -->|intercept| VM
    VM -->|ReactiveProperty streams| RBV
    TP -->|CurrentTheme stream| TS
    AB -->|FadeAsync / ScaleAsync| RBV
    IB -->|GetButtonDown| RBV
    MN -->|NavigateReactive| UF
    UF -->|BaseView lifecycle| RBV
    RBV --> OUI
```

---

## 3. Architectural Pillars

| Pillar | Implementation | Guarantee |
|:---|:---|:---|
| **Reactive Logic** | R3 `ReactiveProperty<T>` + `CompositeDisposable` | Zero-allocation push-based data flow. No polling. |
| **Command Bus** | VitalRouter `ICommand` + `ICommandInterceptor` | Unidirectional control flow. Async-safe navigation pipeline. |
| **Hybrid Rendering** | uGUI (visual fidelity) + UI Toolkit (data-dense lists) | Each renderer used where it excels. No forced migration. |
| **Input Abstraction** | `IInputProvider` → `InputBridge` | New Input System and Legacy Input coexist behind a single API. |
| **Zero-Config Boot** | `[RuntimeInitializeOnLoadMethod(BeforeSceneLoad)]` | No bootstrap scene. No manually ordered GameObjects. |
| **Theme System** | `ThemeData` ScriptableObject + reactive stream | Runtime theme switching with zero manual `Refresh()` calls. |

---

## 4. Class Hierarchy

```mermaid
---
config:
  theme: dark
---
classDiagram
    class ViewModel {
        <<abstract>>
        #CompositeDisposable Disposables
        +Initialize() void
        +Dispose() void
    }

    class ReactiveBaseView~T~ {
        <<abstract>>
        #T ViewModel
        #CompositeDisposable Disposables
        +Initialize(T viewModel) void
        #OnBind()* void
        +OnViewDestroy() void
    }

    class BaseView {
        <<OneUI>>
        +ShowView(DisplayOptions) void
        +HideView(DisplayOptions) void
        +OnViewDestroy() void
    }

    class ThemeSubscriber {
        <<abstract>>
        #CompositeDisposable ThemeDisposables
        #OnThemeChanged(ThemeData)* void
    }

    class ThemeImageSubscriber {
        +ThemeColorType ColorType
    }

    class ThemeTextSubscriber {
        +ThemeColorType ColorType
    }

    class RouterBridge {
        +Router Router
        +Instance$ RouterBridge
    }

    class ThemeProvider {
        +ReadOnlyReactiveProperty~ThemeData~ CurrentTheme
        +SetTheme(ThemeData) void
        +Instance$ ThemeProvider
    }

    class AnimationBridge {
        +FadeAsync(CanvasGroup, float, float, CancellationToken) UniTask
        +ScaleAsync(RectTransform, Vector3, float, AnimationCurve, CancellationToken) UniTask
        +SceneTransitionAsync(string, float, Color, CancellationToken) UniTask
        +Instance$ AnimationBridge
    }

    class MaquiNavigator {
        +NavigateReactive~TView, TViewModel~(string, Action~TView~)$ void
        +CleanupViewModel~TViewModel~()$ void
    }

    class InputBridge {
        +SetProvider(IInputProvider) void
        +GetAxis(string) float
        +GetButtonDown(string) bool
        +GetPointerPosition() Vector2
        +Instance$ InputBridge
    }

    BaseView <|-- ReactiveBaseView~T~
    MonoBehaviour <|-- ThemeSubscriber
    ThemeSubscriber <|-- ThemeImageSubscriber
    ThemeSubscriber <|-- ThemeTextSubscriber
    ViewModel <-- ReactiveBaseView~T~
```

---

## 5. Bootstrap Sequence

`CoreBootstrap.Initialize()` runs automatically via `[RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]`. It creates a single `DontDestroyOnLoad` `GameObject` named `Maqui_Core` and attaches the four bridge components in order:

```mermaid
---
config:
  theme: dark
---
sequenceDiagram
    participant Unity as Unity Engine
    participant CB as CoreBootstrap
    participant GO as Maqui_Core GameObject
    participant Bridges as Bridge Components
    participant Scene as Scene Load

    Unity->>CB: RuntimeInitializeOnLoadMethod (BeforeSceneLoad)
    CB->>GO: new GameObject("Maqui_Core")
    CB->>GO: DontDestroyOnLoad
    CB->>Bridges: AddComponent InputBridge
    CB->>Bridges: AddComponent RouterBridge
    CB->>Bridges: AddComponent ThemeProvider
    CB->>Bridges: AddComponent AnimationBridge
    CB-->>Scene: Scene loads
    Note over Bridges,Scene: All bridge singletons available<br/>before Awake/Start run in any scene
```

**Dependency order matters**:
- `InputBridge` must exist before any `ReactiveBaseView` that reads input in `Update`.
- `RouterBridge` must exist before any interceptor registration (samples register their own).
- `ThemeProvider` must exist before any `ThemeSubscriber` calls `Start`.

> **Do not** register sample interceptors in `CoreBootstrap`. Each sample owns its own `Initialize()` → `Router.Default.AddFilter(...)` call. The package core is decoupled from its samples.

---

## 6. Data Flow: One Reactive Cycle

The canonical Maqui data cycle from user interaction to screen update:

```mermaid
---
config:
  theme: dark
---
sequenceDiagram
    participant User
    participant View as ReactiveBaseView&lt;T&gt;
    participant VM as ViewModel
    participant Router as VitalRouter Router
    participant IC as ICommandInterceptor
    participant VM2 as Target ViewModel

    User->>View: UI Event (Button.onClick)
    View->>Router: Router.Default.PublishAsync(new MyCommand())
    Router->>IC: InvokeAsync (interceptor chain)
    IC->>IC: Async work (await UniTask.Delay / API call)
    IC->>VM2: Update ReactiveProperty
    VM2-->>View: .Subscribe stream pushes new value
    View->>View: Update label / spinner / panel
```

---

## 7. Dependency Graph

```mermaid
---
config:
  theme: dark
---
graph LR
    subgraph pkg["com.ware.maqui"]
        MR[Maqui.Runtime]
        ME[Maqui.Editor]
    end

    subgraph sandbox["CharqUI Sandbox Assets/"]
        UF[UIFramework]
        EF[EventFramework]
    end

    subgraph nuget["NuGet (via NuGetForUnity)"]
        VR[VitalRouter.dll]
        VRR3[VitalRouter.R3.dll]
    end

    subgraph git["Git UPM"]
        R3[R3]
        UT[UniTask]
    end

    MR --> UF
    MR --> EF
    MR --> VR
    MR --> VRR3
    MR --> R3
    MR --> UT
    ME --> MR
```

> **Note on UIFramework**: `UIFramework` and `EventFramework` are in the CharqUI sandbox `Assets/` folder, not packaged separately. `Maqui.Runtime` references them by assembly name. If you are distributing Maqui as a standalone product, these assemblies must be extracted into their own UPM package (`com.ware.oneui`) first.

---

## 8. Assembly Definitions

| Assembly | Path | `autoReferenced` | Purpose |
|:---|:---|:---:|:---|
| `Maqui.Runtime` | `Runtime/` | `true` | All runtime Core classes |
| `Maqui.Editor` | `Editor/` | `true` | Editor tooling (currently placeholder) |
| `Maqui.Tests.Runtime` | `Tests/Runtime/` | `false` | NUnit play-mode + edit-mode tests |
| `Maqui.Tests.Editor` | `Tests/Editor/` | `false` | Editor-only tests |
| `Maqui.Samples.*` | `Samples~/*/` | `false` | Per-sample, imported on demand |

Tests are gated by `UNITY_INCLUDE_TESTS` to prevent shipping test assemblies in production builds.

---

## 9. Threading Model

All Maqui operations are **main-thread safe by default**:

- R3's `ReactiveProperty` subscribers always fire on the thread that called `.Value = x`. In Unity, that is the main thread.
- `AnimationBridge` uses `await UniTask.Yield(PlayerLoopTiming.Update, ct)`, ensuring all interpolation runs in the main Unity player loop.
- VitalRouter's `Router.Default.PublishAsync` is designed to be awaited. Fire-and-forget with `.Forget()` is acceptable for navigation commands where you do not need the result.

If you publish commands from background threads (e.g., a `Task.Run` callback), use `await UniTask.SwitchToMainThread()` before calling `PublishAsync`.
