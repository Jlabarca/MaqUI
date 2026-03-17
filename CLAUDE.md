# CLAUDE.md — Maqui Project Context

> AI session context for `D:\ware\MaqUI\`. Read this first, always.
> Last updated to reflect: Singletons removed, MaquiServices service locator added, MaquiBaseView added, ReactiveList\<T\> reactive collections, WindowLoadFailed error event, window object pooling, expanded test coverage.

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
│   │   ├── Maqui.Runtime.asmdef refs: UniTask, R3.Unity, TMP
│   │   └── Core\
│   │       ├── CoreBootstrap.cs         [RuntimeInitializeOnLoadMethod(BeforeSceneLoad)]
│   │       ├── MaquiServices.cs        static service locator (Register/Get/Reset)
│   │       ├── Bridge\
│   │       │   ├── AnimationBridge.cs   FadeAsync, ScaleAsync, SceneTransitionAsync
│   │       │   ├── IAnimationBridge.cs  interface for AnimationBridge
│   │       │   ├── InputBridge.cs       IInputProvider, LegacyInputProvider
│   │       │   ├── IInputBridge.cs      interface for InputBridge
│   │       │   ├── RouterBridge.cs      Router wrapper MonoBehaviour
│   │       │   ├── IRouterBridge.cs     interface for RouterBridge
│   │       │   ├── ThemeProvider.cs     ReactiveProperty<ThemeData>, SetTheme()
│   │       │   ├── IThemeProvider.cs    interface for ThemeProvider
│   │       │   ├── MaquiNavigator.cs    NavigateReactive<TView,TVM>() — Resources-based
│   │       │   ├── IMaquiAssetProvider.cs   contract for window loading
│   │       │   └── IInputProvider.cs
│   │       ├── Logic\
│   │       │   ├── ViewModel.cs         abstract base, CompositeDisposable, Initialize/Dispose
│   │       │   ├── ReactiveList.cs      granular add/remove/replace/reset reactive collection
│   │       │   ├── ThemeData.cs         ScriptableObject, 13 color slots, GetColor()
│   │       │   └── ThemeColorType.cs    enum (13 values)
│   │       └── Presentation\
│   │           ├── MaquiBaseView.cs         self-contained base (replaces OneUI BaseView)
│   │           ├── ReactiveBaseView<T>.cs   extends MaquiBaseView, typed VM, OnBind/OnFreeze
│   │           ├── MaquiWindowManager.cs    4-layer Canvas, freeze, modal mask, pooling, IUIService
│   │           ├── IUIService.cs            ShowWindowAsync, ShowPrefabAsync, WindowLoadFailed
│   │           ├── IWindowHandle.cs         Show, Hide, Dispose, IsVisible, Layer, Root
│   │           ├── UILayer.cs               enum: Background=0, Default=100, Overlay=200, Modal=300
│   │           ├── WindowOptions.cs         opt-in pooling config (Pooled, MaxPoolSize)
│   │           ├── WindowPool.cs            internal per-asset-key Stack<GO> pool
│   │           ├── ThemeSubscriber.cs
│   │           ├── ThemeImageSubscriber.cs
│   │           └── ThemeTextSubscriber.cs
│   ├── Editor\
│   │   └── Maqui.Editor.asmdef
│   ├── Tests\
│   │   ├── Runtime\   ViewModelTests, ThemeDataTests, MaquiBaseViewTests,
│   │   │              ReactiveBaseViewTests, MaquiWindowManagerTests,
│   │   │              ReactiveListTests
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
                 services           AnimationBridge, MaquiWindowManager
                                    All registered in MaquiServices via interfaces
Presentation     ReactiveBaseView<T> Extends MaquiBaseView, typed VM binding,
                                    layer-aware, freeze-aware
```

### Zero-Config Bootstrap

`CoreBootstrap.[RuntimeInitializeOnLoadMethod(BeforeSceneLoad)]` creates `Maqui_Core`
DontDestroyOnLoad and attaches (in order), registering each into `MaquiServices`:
1. `InputBridge` → `MaquiServices.Register<IInputBridge>(...)`
2. `RouterBridge` → `MaquiServices.Register<IRouterBridge>(...)`
3. `ThemeProvider` → `MaquiServices.Register<IThemeProvider>(...)`
4. `AnimationBridge` → `MaquiServices.Register<IAnimationBridge>(...)`
5. `MaquiWindowManager` → `MaquiServices.Register<IUIService>(...)` — immediately builds 4 layer canvases + modal mask

**No scene setup, no prefab, no Awake ordering required.**
**No singletons — all services accessed via `MaquiServices.Get<T>()`.**

---

## Namespaces & Assemblies

| Namespace | Assembly | Contents |
|:---|:---|:---|
| `Maqui.Core` | `Maqui.Runtime` | `CoreBootstrap`, `MaquiServices` |
| `Maqui.Core.Bridge` | `Maqui.Runtime` | `AnimationBridge`/`IAnimationBridge`, `InputBridge`/`IInputBridge`, `RouterBridge`/`IRouterBridge`, `ThemeProvider`/`IThemeProvider`, `MaquiNavigator`, `IInputProvider`, `IMaquiAssetProvider`, `MaquiAssetProviderBridge`, `ResourcesAssetProvider` |
| `Maqui.Core.Logic` | `Maqui.Runtime` | `ViewModel`, `ThemeData`, `ThemeColorType` |
| `Maqui.Core.Presentation` | `Maqui.Runtime` | `ReactiveBaseView<T>`, `MaquiWindowManager`, `IUIService`, `IWindowHandle`, `UILayer`, `ThemeSubscriber`, `ThemeImageSubscriber`, `ThemeTextSubscriber` |
| `Maqui.Samples.*` | per-sample asmdef | 4 sample assemblies |

---

## Key APIs

### MaquiServices
```csharp
// Maqui.Core — static service locator, replaces all singletons
MaquiServices.Register<IInputBridge>(bridge);      // called by CoreBootstrap
MaquiServices.Get<IInputBridge>();                  // returns registered instance or null
MaquiServices.Get<IThemeProvider>()?.SetTheme(t);   // null-safe access pattern
MaquiServices.Reset();                             // test isolation — clears all registrations
```

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

### ReactiveList\<T\>
```csharp
// Maqui.Core.Logic — granular reactive collection (lightweight alternative to ObservableCollections)
public sealed class ReactiveList<T> : IReadOnlyList<T>, IDisposable
{
    // Construction
    public ReactiveList();
    public ReactiveList(IEnumerable<T> initial);

    // Observables — subscribe for granular change notifications
    public Observable<ListAddEvent<T>> ObserveAdd();
    public Observable<ListRemoveEvent<T>> ObserveRemove();
    public Observable<ListReplaceEvent<T>> ObserveReplace();
    public Observable<Unit> ObserveReset();                    // emits on Clear()
    public ReadOnlyReactiveProperty<int> ObserveCountChanged();

    // Mutations — each emits the corresponding observable
    public void Add(T item);
    public void Insert(int index, T item);
    public bool Remove(T item);
    public void RemoveAt(int index);
    public void Clear();
    public void AddRange(IEnumerable<T> items);
    public void Move(int oldIndex, int newIndex);  // emits Remove + Add
    public T this[int index] { get; set; }         // set emits Replace
}

// Event structs: ListAddEvent<T>(Index, Item), ListRemoveEvent<T>(Index, Item),
//                ListReplaceEvent<T>(Index, OldItem, NewItem)
```

**Use `ReactiveList<T>` instead of `ReactiveProperty<IReadOnlyList<T>>`** when the view needs
incremental updates (add/remove/replace rows) rather than full-list rebuilds.

### ReactiveBaseView\<T\>
```csharp
// Maqui.Core.Presentation — inherits MaquiBaseView (self-contained), implements IFreezableView
public abstract class ReactiveBaseView<T> : MaquiBaseView, IFreezableView where T : ViewModel
{
    protected T ViewModel { get; private set; }
    protected readonly CompositeDisposable Disposables = new();

    public virtual void Initialize(T viewModel);        // injects VM → Initialize() → OnBind()
    protected abstract void OnBind();                   // reactive subscriptions only here

    protected virtual UniTask OnPreShowAsync(CancellationToken ct);  // async pre-fetch before reveal
    protected virtual void OnPreHide();                              // before hide sequence
    protected virtual void OnFreeze();                               // Modal opened → disable input
    protected virtual void OnUnfreeze();                             // Modal closed → restore input
    protected virtual void OnReset();                                // pooled view returning to pool

    public override void OnViewDestroy();               // disposes Disposables + ViewModel
}
```
`OnViewAwake()` auto-registers with `MaquiServices.Get<IUIService>()` for freeze notifications.

### MaquiWindowManager (IUIService)
```csharp
// Maqui.Core.Presentation — MonoBehaviour, implements IUIService
// Created by CoreBootstrap, registered as MaquiServices.Register<IUIService>(...), do NOT add manually

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

// Fired when any Show*Async fails (missing asset, wrong component, provider exception, etc.)
event Action<WindowLoadFailedEvent> WindowLoadFailed;
```

#### Error handling
All `Show*Async` methods:
- Return `null` (not throw) on failure, with `[Maqui]`-prefixed `Debug.LogError`
- Fire `WindowLoadFailed` event with `assetKey`, `layer`, `reason`, and optional `Exception`
- Re-throw `OperationCanceledException` (cancellation is not a failure)
- Clean up partially-instantiated GameObjects on failure

Subscribe to `WindowLoadFailed` for fallback UI or retry logic:
```csharp
MaquiServices.Get<IUIService>().WindowLoadFailed += e =>
    Debug.LogWarning($"Window failed: {e.AssetKey} — {e.Reason}");
```

#### Window pooling
Opt-in per-asset-key object pool. Pooled windows are deactivated (not destroyed) on Dispose,
and reused on the next ShowWindowAsync for the same key — skipping Instantiate and Awake.

```csharp
var opts = new WindowOptions { Pooled = true, MaxPoolSize = 2 };
var handle = await MaquiServices.Get<IUIService>()
    .ShowWindowAsync<MyView, MyVM>("key", UILayer.Default, vm, opts, ct);

// handle.Dispose() → ResetForPool → pool.Return (deactivate, don't destroy)
// Next ShowWindowAsync("key", ..., opts) → pool.TryGet → reuse → PrepareForReuse(newVM)
```

Pool lifecycle:
- **First show**: normal Instantiate → Awake → OnPreShowAsync → Initialize → OnBind
- **Dispose (pooled)**: OnPreHide → OnReset → Disposables.Dispose → VM.Dispose → UnregisterFreezable → SetActive(false)
- **Reuse**: SetActive(true) → OnPreShowAsync → new Disposables → RegisterFreezable → VM.Initialize → OnBind
- **Pool full on return**: falls through to Destroy (normal non-pooled cleanup)

Override `OnReset()` to clear UI state that `OnBind()` doesn't explicitly set (scroll positions, input fields, etc.).

**Important**: Poolable views must wire up VitalRouter in `OnBind()` + `Disposables`, not in `Start()`.

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

### ThemeProvider (IThemeProvider)
```csharp
MaquiServices.Get<IThemeProvider>()?.SetTheme(ThemeData theme);
MaquiServices.Get<IThemeProvider>()?.CurrentTheme  // ReadOnlyReactiveProperty<ThemeData>
```

### AnimationBridge (IAnimationBridge)
```csharp
var anim = MaquiServices.Get<IAnimationBridge>();
await anim.FadeAsync(CanvasGroup cg, float alpha, float duration, CancellationToken ct);
await anim.ScaleAsync(RectTransform rt, Vector3 target, float duration, CancellationToken ct);
await anim.SceneTransitionAsync(string sceneName, float duration, Color color, CancellationToken ct);
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
Router.Default.RemoveFilter(myInterceptor);        // always unregister in OnDestroy()
```

### Window subscriptions (direct intent)
```csharp
// [Routes] + [Route] — for windows reacting to their own open/close commands
[Routes]
public partial class InventoryWindow : ReactiveBaseView<InventoryViewModel>
{
    [Route]
    public async UniTask On(OpenInventoryCommand cmd, CancellationToken ct)
    {
        await MaquiServices.Get<IAnimationBridge>().FadeAsync(ViewCanvasGroup, 1f, 0.2f, ct);
    }

    [Route]
    public void On(CloseInventoryCommand cmd) => HideView();

    protected override void OnBind()
    {
        // Always wire routing in OnBind with Disposables — works for both pooled and non-pooled
        this.MapTo(Router.Default).AddTo(Disposables);
        ViewModel.Items.Subscribe(RefreshGrid).AddTo(Disposables);
    }
}
```
**Rule**: Use `ICommandInterceptor` for cross-cutting concerns (logging, auth). Use `[Route]` on windows for their own show/hide intent.

**Routing convention**: Always wire `this.MapTo(Router.Default).AddTo(Disposables)` in `OnBind()`, never in `Start()`. `Disposables` is disposed both on destroy (non-pooled) and on pool return (pooled), so this single pattern is always correct. Using `Start()` + `destroyCancellationToken` breaks pooled windows because the subscription survives pool return, causing duplicate handlers on reuse.

**Note on VitalRouter**: VitalRouter 2.x uses `[Route]` (not `[Subscribe]`) on handler methods. `PublishAsync` returns `ValueTask` — use `_ = Router.Default.PublishAsync(...)` for fire-and-forget, not `.Forget()`.

---

## HybridFrame / ORO Integration

HybridFrame (https://github.com/waremoto/hybridframe) uses a service locator: `HF.Get<T>()` / `HF.Register<T>()`.

`MaquiWindowManager` implements `IUIService`. Register it with HF during boot:
```csharp
// In ORO boot procedure (after CoreBootstrap has run):
HF.Register<IUIService>(MaquiServices.Get<IUIService>());
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
| `UniTask` | Git UPM `com.cysharp.unitask` |
| `R3.Unity` | Git UPM `com.cysharp.r3` |
| `Unity.TextMeshPro` | Built-in |
| `VitalRouter` (implicit) | NuGet `Assets/Packages/VitalRouter.2.0.5/` — DLLs replaced with 2.2.0 to match UPM `jp.hadashikick.vitalrouter.unity@2.2.0` |

### Sandbox-only (not in package)
DevsDaddy OneUI (UIFramework + EventFramework — no longer referenced by Maqui.Runtime), Le Tai TranslucentImage, Le Tai TrueShadow, Coffee.UIParticle, AwesomeAttributes, LokoSolo.PinchableScrollRect, Modular Game UI Kit

---

## Patterns

### New window (layer-aware)
1. `sealed class MyViewModel : ViewModel` — no UnityEngine refs
2. `[Routes] public partial class MyView : ReactiveBaseView<MyViewModel>` — bind in `OnBind()` only
3. Prefab root needs `CanvasGroup`; **must NOT have a `Canvas` component** (parented to layer canvas which owns the GraphicRaycaster — a nested Canvas without its own GraphicRaycaster silently blocks all raycasts)
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

### Reactive list binding (granular updates)
```csharp
// ViewModel — use ReactiveList instead of ReactiveProperty<IReadOnlyList<T>>
public readonly ReactiveList<MyItem> Items = new();

// View — OnBind(): build initial rows, then subscribe to granular events
foreach (var item in ViewModel.Items) AddRow(item);

ViewModel.Items.ObserveAdd()
    .Subscribe(e => InsertRow(e.Index, e.Item)).AddTo(Disposables);
ViewModel.Items.ObserveRemove()
    .Subscribe(e => RemoveRow(e.Index)).AddTo(Disposables);
ViewModel.Items.ObserveReset()
    .Subscribe(_ => ClearRows()).AddTo(Disposables);
ViewModel.Items.ObserveCountChanged()
    .Subscribe(n => _countLabel.text = $"({n})").AddTo(Disposables);
```

### Poolable window
```csharp
// Open with pooling enabled (inventory opens/closes many times per session)
var opts = new WindowOptions { Pooled = true, MaxPoolSize = 2 };
_handle = await MaquiServices.Get<IUIService>()
    .ShowWindowAsync<InventoryView, InventoryVM>("Views/Inventory", UILayer.Default, vm, opts, ct);

// OnBind + Disposables already handles pool correctly — no special wiring needed.
// Override OnReset() only if you have UI state that OnBind doesn't explicitly set.
protected override void OnReset()
{
    _scrollRect.verticalNormalizedPosition = 1f;
}
```

### Theme-aware component
- `ThemeImageSubscriber` / `ThemeTextSubscriber` — zero-code, assign slot in Inspector
- `ThemeSubscriber` — extend, implement `OnThemeChanged(ThemeData)`
- In `OnBind()`: `MaquiServices.Get<IThemeProvider>()?.CurrentTheme.Subscribe(...).AddTo(Disposables)`

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
| Forgetting `RemoveFilter` in `OnDestroy` | Phantom interceptor | Always pair `AddFilter` → `RemoveFilter` |
| Cross-plugin `window.Show()` directly | Violates sandbox | Publish a command instead |
| Disposing `IWindowHandle` more than once | Double-destroy | Guard with `_handle = null` after Dispose |
| `ReactiveProperty<IReadOnlyList<T>>` for mutable lists | Full-list rebuild on every change | Use `ReactiveList<T>` with granular ObserveAdd/Remove |
| `this.MapTo(Router.Default)` in `Start()` | Breaks pooled windows — subscription survives pool return, duplicate handlers on reuse | Always wire in `OnBind()` with `.AddTo(Disposables)` |
| `Canvas` component on view prefab root | Creates nested sub-canvas without GraphicRaycaster — silently blocks ALL raycasts (clicks, drags, scrolls). No errors logged. | Remove the Canvas from the prefab. Views are parented to layer canvases which already have GraphicRaycaster. Only `CanvasGroup` + `RectTransform` needed on root. |

---

## Testing

- Run: Unity → Window → Test Runner → filter `Maqui.Tests`
- `CharqUI/Packages/manifest.json` has `"testables": ["com.ware.maqui"]`
- Tests: `Tests/Runtime/` — ViewModelTests, ThemeDataTests, MaquiBaseViewTests, ReactiveBaseViewTests, MaquiWindowManagerTests, InputBridgeTests, ThemeProviderTests, AnimationBridgeTests, ThemeSubscriberTests, CoreBootstrapTests, AssetProviderTests, ReactiveListTests, InterceptorTests; `Tests/Editor/MaquiEditorTests.cs`
- All tests use `MaquiServices.Reset()` in setup/teardown for clean isolation

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
