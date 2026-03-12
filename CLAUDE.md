# CLAUDE.md — Maqui Project Context

> AI session context for `D:\ware\MaqUI\`. Read this first, always.
> Last updated to reflect: MaquiWindowManager, IUIService, UILayer, IWindowHandle, IMaquiAssetProvider.

---

## Project Overview

**Maqui** (`com.ware.maqui` v0.1.0) is a reactive MVVM UI framework for Unity.
It uses **R3** for reactive state, **VitalRouter** for command routing, and **UniTask** for async.
The primary consumer is **ORO** (an MMO), where Maqui serves as the UI layer on top of **HybridFrame**.

- **Package path**: `D:\ware\MaqUI\com.ware.maqui\`
- **Sandbox/host project**: `D:\ware\MaqUI\CharqUI\` (Unity 2022.3 LTS, URP 17.x)
- **Renamed from**: CharqUI → Maqui. All namespaces use `Maqui.*`.

---

## Workspace Layout

```
D:\ware\MaqUI\
├── com.ware.maqui\              ← THE PACKAGE (source of truth)
│   ├── package.json             v0.1.0
│   ├── Runtime\
│   │   ├── Maqui.Runtime.asmdef refs: UIFramework, EventFramework, UniTask, R3.Unity, TMP
│   │   └── Core\
│   │       ├── CoreBootstrap.cs         [RuntimeInitializeOnLoadMethod(BeforeSceneLoad)]
│   │       ├── Bridge\
│   │       │   ├── AnimationBridge.cs   FadeAsync, ScaleAsync, SceneTransitionAsync
│   │       │   ├── InputBridge.cs       IInputProvider, LegacyInputProvider
│   │       │   ├── RouterBridge.cs      Router.Default singleton MonoBehaviour
│   │       │   ├── ThemeProvider.cs     ReactiveProperty<ThemeData>, SetTheme()
│   │       │   ├── MaquiNavigator.cs    NavigateReactive<TView,TVM>() — Resources-based
│   │       │   ├── IMaquiAssetProvider.cs   contract for window loading
│   │       │   └── IInputProvider.cs
│   │       ├── Logic\
│   │       │   ├── ViewModel.cs         abstract base, CompositeDisposable, Initialize/Dispose
│   │       │   ├── ThemeData.cs         ScriptableObject, 13 color slots, GetColor()
│   │       │   └── ThemeColorType.cs    enum (13 values)
│   │       └── Presentation\
│   │           ├── ReactiveBaseView<T>.cs   base view, IFreezableView, OnBind/OnFreeze hooks
│   │           ├── MaquiWindowManager.cs    4-layer Canvas, freeze, modal mask, IUIService impl
│   │           ├── IUIService.cs            ShowWindowAsync, ShowPrefabAsync, CleanupPlugin
│   │           ├── IWindowHandle.cs         Show, Hide, Dispose, IsVisible, Layer, Root
│   │           ├── UILayer.cs               enum: Background=0, Default=100, Overlay=200, Modal=300
│   │           ├── ThemeSubscriber.cs
│   │           ├── ThemeImageSubscriber.cs
│   │           └── ThemeTextSubscriber.cs
│   ├── Editor\
│   │   └── Maqui.Editor.asmdef
│   ├── Tests\
│   │   ├── Runtime\   ViewModelTests.cs, ThemeDataTests.cs
│   │   └── Editor\    MaquiEditorTests.cs
│   └── Samples~\
│       ├── Welcome\       minimal routing + MVVM demo
│       ├── GrandTour\     multi-VM shared state + hybrid rendering
│       ├── SharkSuite\    UI Toolkit list + uGUI detail panel
│       └── Modernization\ procedural visuals + theme binding
│
└── CharqUI\                     ← SANDBOX (Unity project only)
    ├── Assets\
    │   ├── UI\OneUI\            UIFramework + EventFramework (sandbox deps, not packaged)
    │   ├── UI\Pack\             Modular Game UI Kit art pack
    │   ├── Plugins\             Le Tai, AwesomeAttributes, UIParticles, LokoSolo
    │   └── Packages\            NuGet DLLs: R3, VitalRouter, VitalRouter.R3
    ├── Packages\manifest.json   links com.ware.maqui via "file:../../com.ware.maqui"
    └── docs\
        ├── 01-architecture.md … 09-samples-reference.md
        ├── adaption-report.md   ORO gap analysis
        ├── oro-maqui-ui-rework.md  ORO UI design (Maqui edition)
        └── legacy\              30 pre-rename docs, unmodified
```

---

## Core Architecture — Three Layers

```
Logic Layer      ViewModel          Pure C#, no UnityEngine refs, R3 reactive state
Bridge Layer     MonoBehaviour      InputBridge, RouterBridge, ThemeProvider,
                 singletons         AnimationBridge, MaquiWindowManager
Presentation     ReactiveBaseView<T> Extends OneUI BaseView, typed VM binding,
                                    layer-aware, freeze-aware
```

### Zero-Config Bootstrap

`CoreBootstrap.[RuntimeInitializeOnLoadMethod(BeforeSceneLoad)]` creates `Maqui_Core`
DontDestroyOnLoad and attaches (in order):
1. `InputBridge`
2. `RouterBridge`
3. `ThemeProvider`
4. `AnimationBridge`
5. `MaquiWindowManager` — immediately builds 4 layer canvases + modal mask

**No scene setup, no prefab, no Awake ordering required.**

---

## Namespaces & Assemblies

| Namespace | Assembly | Contents |
|:---|:---|:---|
| `Maqui.Core` | `Maqui.Runtime` | `CoreBootstrap` |
| `Maqui.Core.Bridge` | `Maqui.Runtime` | `AnimationBridge`, `InputBridge`, `RouterBridge`, `ThemeProvider`, `MaquiNavigator`, `IInputProvider`, `IMaquiAssetProvider`, `MaquiAssetProviderBridge`, `ResourcesAssetProvider` |
| `Maqui.Core.Logic` | `Maqui.Runtime` | `ViewModel`, `ThemeData`, `ThemeColorType` |
| `Maqui.Core.Presentation` | `Maqui.Runtime` | `ReactiveBaseView<T>`, `MaquiWindowManager`, `IUIService`, `IWindowHandle`, `UILayer`, `ThemeSubscriber`, `ThemeImageSubscriber`, `ThemeTextSubscriber` |
| `Maqui.Samples.*` | per-sample asmdef | 4 sample assemblies |

---

## Key APIs

### ViewModel
```csharp
// Maqui.Core.Logic
public abstract class ViewModel : IDisposable
{
    protected readonly CompositeDisposable Disposables = new();
    public virtual void Initialize() { }
    public virtual void Dispose() { Disposables.Dispose(); }
}
```

### ReactiveBaseView\<T\>
```csharp
// Maqui.Core.Presentation — inherits BaseView (OneUI), implements IFreezableView
public abstract class ReactiveBaseView<T> : BaseView, IFreezableView where T : ViewModel
{
    protected T ViewModel { get; private set; }
    protected readonly CompositeDisposable Disposables = new();

    public virtual void Initialize(T viewModel);        // injects VM → Initialize() → OnBind()
    protected abstract void OnBind();                   // reactive subscriptions only here

    protected virtual UniTask OnPreShowAsync(CancellationToken ct);  // async pre-fetch before reveal
    protected virtual void OnPreHide();                              // before hide sequence
    protected virtual void OnFreeze();                               // Modal opened → disable input
    protected virtual void OnUnfreeze();                             // Modal closed → restore input

    public override void OnViewDestroy();               // disposes Disposables + ViewModel
}
```
`OnViewAwake()` auto-registers with `MaquiWindowManager` for freeze notifications.

### MaquiWindowManager (IUIService)
```csharp
// Maqui.Core.Presentation — MonoBehaviour singleton, implements IUIService
// Created by CoreBootstrap, do NOT add manually

// Load prefab from assetKey, instantiate in layer, run OnPreShowAsync, then Initialize(vm)
UniTask<IWindowHandle> ShowWindowAsync<TView, TVM>(string assetKey, UILayer layer, TVM viewModel, CancellationToken ct);
// Same but creates new TVM via new()
UniTask<IWindowHandle> ShowWindowAsync<TView, TVM>(string assetKey, UILayer layer, Action<TVM> configure, CancellationToken ct);
// Raw prefab load — for plugins without typed ViewModel
UniTask<IWindowHandle> ShowPrefabAsync(string assetKey, UILayer layer, string pluginId, CancellationToken ct);
// Destroys all windows registered under pluginId
void CleanupPlugin(string pluginId);
// Returns the root Canvas for a layer (for manual parenting)
Canvas GetLayerCanvas(UILayer layer);
```

### UILayer
```csharp
public enum UILayer
{
    Background = 0,    // skybox overlays, cutscene letterbox
    Default    = 100,  // main panels: inventory, character, map, NPC dialog
    Overlay    = 200,  // HUD: HP bars, minimap, hotbar, buffs
    Modal      = 300,  // blocking dialogs, fullscreen modal content
}
// Layer_ModalMask Canvas sits at sortOrder=290, auto-managed
```

### IWindowHandle
```csharp
public interface IWindowHandle : IDisposable
{
    bool IsVisible { get; }
    UILayer Layer { get; }
    GameObject Root { get; }
    void Show();    // SetActive(true)  — does NOT re-run lifecycle
    void Hide();    // SetActive(false) — does NOT destroy
    void Dispose(); // Destroys Root, notifies MaquiWindowManager, decrements modal count
}
```

### IMaquiAssetProvider / MaquiAssetProviderBridge
```csharp
// Default: ResourcesAssetProvider (synchronous Resources.Load)
// Replace at boot for YooAsset, Addressables, etc.
MaquiAssetProviderBridge.SetProvider(new YooAssetMaquiProvider());
MaquiAssetProviderBridge.Current  // → IMaquiAssetProvider
```

### ThemeProvider
```csharp
ThemeProvider.Instance.SetTheme(ThemeData theme);
ThemeProvider.Instance.CurrentTheme  // ReadOnlyReactiveProperty<ThemeData>
```

### AnimationBridge
```csharp
await AnimationBridge.Instance.FadeAsync(CanvasGroup cg, float alpha, float duration, CancellationToken ct);
await AnimationBridge.Instance.ScaleAsync(RectTransform rt, Vector3 target, float duration, CancellationToken ct);
await AnimationBridge.Instance.SceneTransitionAsync(string sceneName, float duration, Color color, CancellationToken ct);
```

### MaquiNavigator (legacy — Resources-based)
```csharp
// Simple navigation without layer system. Use IUIService for new windows.
MaquiNavigator.NavigateReactive<TView, TViewModel>(string resourcePath, Action<TView> onComplete = null);
MaquiNavigator.CleanupViewModel<TViewModel>();
```

---

## Modal Freeze System

When a `UILayer.Modal` window is shown:
1. `Layer_ModalMask` (sortOrder=290, semi-transparent black) activates
2. `GraphicRaycaster` on `Layer_Default` and `Layer_Overlay` is disabled
3. All registered `ReactiveBaseView` instances receive `OnFreeze()`

When the last Modal window is disposed:
1. Reverse all three steps
2. All views receive `OnUnfreeze()`

Implement `OnFreeze()`/`OnUnfreeze()` for custom per-view feedback (audio mute, dim, etc.).

---

## VitalRouter Integration

### Interceptors (cross-cutting concerns)
```csharp
// ICommandInterceptor — for logging, auth checks, analytics, async navigation gate
Router.Default.AddFilter(myInterceptor);           // register in Start()
await Router.Default.UnfilterAsync<T>();            // always unregister in OnDestroy()
```

### Window subscriptions (direct intent)
```csharp
// [Routes] + [Route] — for windows reacting to their own open/close commands
[Routes]
public partial class InventoryWindow : ReactiveBaseView<InventoryViewModel>
{
    private void Start() => this.MapTo(Router.Default).AddTo(destroyCancellationToken);

    [Route]
    public async UniTask On(OpenInventoryCommand cmd, CancellationToken ct)
    {
        await AnimationBridge.Instance.FadeAsync(GetComponent<CanvasGroup>(), 1f, 0.2f, ct);
    }

    [Route]
    public void On(CloseInventoryCommand cmd) => HideView();
}
```
**Rule**: Use `ICommandInterceptor` for cross-cutting concerns (logging, auth). Use `[Route]` on windows for their own show/hide intent.

**Note on VitalRouter attribute naming**: VitalRouter 2.x uses `[Route]` (not `[Subscribe]`) on handler methods. The wire-up call is `this.MapTo(Router.Default).AddTo(destroyCancellationToken)` (not `Router.Default.Subscribe(this)`). `PublishAsync` returns `ValueTask` — use `_ = Router.Default.PublishAsync(...)` for fire-and-forget, not `.Forget()`.

---

## HybridFrame / ORO Integration

HybridFrame (https://github.com/waremoto/hybridframe) uses a service locator: `HF.Get<T>()` / `HF.Register<T>()`.

`MaquiWindowManager` implements `IUIService`. Register it with HF during boot:
```csharp
// In ORO boot procedure (after CoreBootstrap has run):
HF.Register<IUIService>(MaquiWindowManager.Instance);
```

Plugin window creation goes through `IPluginAPI.CreateUIPanelAsync()` → `SandboxedPluginAPI` → `IUIService.ShowPrefabAsync()` with the plugin's `pluginId`.

On plugin unload: `HF.Get<IUIService>().CleanupPlugin(pluginId)` destroys all plugin-owned windows.

Asset loading in ORO uses YooAsset. Register the provider early:
```csharp
MaquiAssetProviderBridge.SetProvider(new YooAssetMaquiProvider());
```

---

## Dependencies

### Maqui.Runtime.asmdef references
| Dep | Source |
|:---|:---|
| `UIFramework` (DevsDaddy.OneUI) | `CharqUI/Assets/UI/OneUI/` — sandbox only, not packaged |
| `EventFramework` (DevsDaddy.OneUI) | same |
| `UniTask` | Git UPM `com.cysharp.unitask` |
| `R3.Unity` | Git UPM `com.cysharp.r3` |
| `Unity.TextMeshPro` | Built-in |
| `VitalRouter` (implicit) | NuGet `Assets/Packages/VitalRouter.2.0.5/` — DLLs replaced with 2.2.0 to match UPM `jp.hadashikick.vitalrouter.unity@2.2.0` |

### Sandbox-only (not in package)
Le Tai TranslucentImage, Le Tai TrueShadow, Coffee.UIParticle, AwesomeAttributes, LokoSolo.PinchableScrollRect, Modular Game UI Kit

---

## Patterns

### New window (layer-aware)
1. `sealed class MyViewModel : ViewModel` — no UnityEngine refs
2. `[Routes] public partial class MyView : ReactiveBaseView<MyViewModel>` — bind in `OnBind()` only
3. Prefab root needs `CanvasGroup`; no `Canvas` component needed (parented to layer canvas)
4. Open: `HF.Get<IUIService>().ShowWindowAsync<MyView, MyViewModel>("key", UILayer.Default, vm, ct)`
5. Close: `handle.Dispose()` or via `[Route]` on a close command

### New window (simple, Resources-based)
`MaquiNavigator.NavigateReactive<MyView, MyViewModel>("Views/MyView")`

### New command flow
```csharp
public readonly record struct OpenMyPanelCommand : ICommand;  // struct in Shared.Contracts
// Publish: _ = Router.Default.PublishAsync(new OpenMyPanelCommand());
// Subscribe in window: [Route] public void On(OpenMyPanelCommand cmd) { ... }
```

### Theme-aware component
- `ThemeImageSubscriber` / `ThemeTextSubscriber` — zero-code, assign slot in Inspector
- `ThemeSubscriber` — extend, implement `OnThemeChanged(ThemeData)`
- In `OnBind()`: `ThemeProvider.Instance.CurrentTheme.Subscribe(...).AddTo(Disposables)`

---

## Anti-Patterns

| Anti-pattern | Why | Fix |
|:---|:---|:---|
| `using UnityEngine` in ViewModel | Breaks testability | Pass primitives; resolve refs in View |
| Reading `.Value` in `OnBind()` | Unnecessary — R3 emits immediately on subscribe | Just subscribe |
| `FindObjectOfType<MyView>()` | O(n), brittle | Communicate via VitalRouter commands |
| `GetComponent<CanvasGroup>().alpha = 0` directly | Bypasses freeze state and lifecycle | Use `handle.Hide()` or `[Route]` + AnimationBridge |
| Registering interceptors in `CoreBootstrap` | Couples package to app code | Register in scene's `Start()`, unregister in `OnDestroy()` |
| Forgetting `.AddTo(Disposables)` | Subscription leak | Always chain |
| Forgetting `UnfilterAsync` in `OnDestroy` | Phantom interceptor | Always pair `AddFilter` → `UnfilterAsync` |
| Cross-plugin `window.Show()` directly | Violates sandbox | Publish a command instead |
| Disposing `IWindowHandle` more than once | Double-destroy | Guard with `_handle = null` after Dispose |

---

## Testing

- Run: Unity → Window → Test Runner → filter `Maqui.Tests`
- `CharqUI/Packages/manifest.json` has `"testables": ["com.ware.maqui"]`
- Tests: `Tests/Runtime/ViewModelTests.cs`, `Tests/Runtime/ThemeDataTests.cs`, `Tests/Editor/MaquiEditorTests.cs`

---

## Documentation

`CharqUI/docs/`:
- `01-architecture.md` — layer model, class hierarchy, bootstrap, data flow
- `02-quick-start.md` — first screen in 15 min
- `03-core-concepts.md` — ViewModel, ReactiveBaseView, R3 patterns, ThemeProvider
- `04-routing-commands.md` — VitalRouter, interceptors, `[Route]`, async pipeline
- `05-theming.md` — ThemeData, SetTheme, ThemeSubscriber
- `06-animation-transitions.md` — AnimationBridge, UniTask, DisplayOptions
- `07-hybrid-rendering.md` — uGUI vs UI Toolkit decision matrix
- `08-performance.md` — canvas optimization, draw calls, profiling
- `09-samples-reference.md` — all 4 samples with architecture diagrams
- `adaption-report.md` — ORO gap analysis (Maqui vs OxGFrame requirements)
- `oro-maqui-ui-rework.md` — ORO UI design document (Maqui edition)

---

## Git & Conventions

- Repo root: `D:\ware\MaqUI\` — tracks both `com.ware.maqui/` and `CharqUI/`
- Branch: `main`
- **Naming**: PascalCase classes, `_camelCase` private fields
- **Commands**: `struct` (or `readonly record struct`), `[Verb][Noun]Command`
- **Logs**: `[Maqui]` prefix inside the package
- **Resources path**: `Assets/Resources/Views/` (for MaquiNavigator legacy load)
- **Window asset keys**: `{PluginId}/{WindowName}` for IUIService (e.g., `com.rompe.core/InventoryWindow`)
- **Theme assets**: `Assets/Resources/Themes/Theme_Dark.asset`
