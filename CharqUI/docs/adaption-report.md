# Maqui × ORO — Adaption Report

**Scope:** Gap analysis between current Maqui (v0.1.0) and the requirements specified in the ORO UI rework design. For each requirement: current state, gap severity, verdict (**Modify Maqui** vs **Adapt Integration**), and concrete implementation spec.

**Context:**
- **ORO** is a plugin framework built on HybridFrame that turns a Unity client into a hot-loadable multi-game platform.
- The original UI design targeted **OxGFrame UIBase** — a heavyweight UI system with 12 lifecycle hooks, 8 rendering layers, UIFreezeManager, UIMaskManager, and async prefab loading.
- **Maqui** wraps **OneUI** (`DevsDaddy.Shared.UIFramework`) — a lightweight single-view navigator with 3 lifecycle hooks and synchronous `Resources.Load`.
- Goal: achieve a **simple, performant, HybridFrame-adapted UI system** including full plugin API support. Maqui is not frozen; it can and should be modified.

---

## Summary Table

| Requirement | Gap | Verdict | Effort |
|:---|:---:|:---:|:---:|
| Lifecycle hooks (12 → simplified) | Moderate | Modify Maqui | M |
| Window layer system (8 → 4 UILayers) | High | Modify Maqui | M |
| Async prefab loading (YooAsset) | High | Modify Maqui + Adapt Integration | S |
| UIFreezeManager | Moderate | Adapt Integration (VitalRouter) | S |
| Modal mask / UIMaskManager | Low | Modify Maqui | S |
| IUIService / IWindowHandle contracts | High | Modify Maqui | M |
| Plugin UI sandbox (IPluginAPI.CreateUIPanel) | High | Adapt Integration | S |
| IWindowBuilder (programmatic UI) | Low | Adapt Integration | S |
| VitalRouter [Subscribe] on windows | None | Adapt Integration (document) | XS |
| [Routes] attribute-based windows | None | Adapt Integration (document) | XS |
| ThemeSystem | None | Maqui already covers this | — |
| Reactive ViewModel binding | None | Maqui core strength | — |

**Effort legend:** XS = hours, S = 1-2 days, M = 3-5 days

---

## 1. Lifecycle Hooks

### OxGFrame target (12 hooks)

`OnCreate → OnInit → OnPreShow → OnShow → OnUpdate → OnPreHide → OnHide → OnClose → OnRelease → Freeze → UnFreeze → OnDestroy`

### Current Maqui (3 hooks via OneUI `BaseView`)

`OnViewAwake() → OnViewStart() → OnViewDestroy()`

Maqui adds: `OnBind()` (called by `ReactiveBaseView.Initialize`) which is functionally `OnShow`.

### Gap assessment

OxGFrame's 12 hooks map to 5 functional categories:
1. **Creation** — OnCreate/OnInit → `OnViewAwake()` ✓ (present)
2. **Pre-show** — OnPreShow → ❌ missing (needed for async data fetch before reveal)
3. **Visibility** — OnShow/OnHide → `OnBind()` / no hide hook → partial
4. **Input blocking** — Freeze/UnFreeze → ❌ missing
5. **Destruction** — OnClose/OnDestroy → `OnViewDestroy()` ✓ (present)

`OnUpdate()` is **not needed** — R3 reactive subscriptions handle per-value updates without a per-frame hook.

### Verdict: **Modify Maqui**

Add 4 virtual hooks to `ReactiveBaseView<T>`:

```csharp
// com.ware.maqui / Runtime/Core/Presentation/ReactiveBaseView.cs
public abstract class ReactiveBaseView<T> : BaseView where T : ViewModel
{
    // Existing: called once, before scene is shown
    protected virtual void OnPreInitialize() { }

    // NEW: called before show animation begins — use for async data pre-fetch
    protected virtual UniTask OnPreShowAsync(CancellationToken ct) => UniTask.CompletedTask;

    // Existing via OnBind: called after show animation — reactive bindings here
    protected abstract void OnBind();

    // NEW: called before hide animation — teardown that needs to run before visual disappears
    protected virtual void OnPreHide() { }

    // NEW: called when a Modal-layer window covers this view (input frozen)
    protected virtual void OnFreeze() { }

    // NEW: called when the covering modal is removed (input restored)
    protected virtual void OnUnfreeze() { }

    // Existing: OnViewDestroy() — dispose subscriptions, destroy ViewModel
}
```

**Wire-up:** `MaquiWindowManager` (new, see §2) calls `OnPreShowAsync` before animating in, and fires `OnFreeze`/`OnUnfreeze` when the Modal layer changes occupancy.

**What we drop from OxGFrame:** `OnInit`, `OnUpdate`, `OnRelease`, `OnPreHide` intermediate states. These were OxGFrame bookkeeping; Maqui's R3 disposable discipline makes them unnecessary.

---

## 2. Window Layer System

### OxGFrame target (8 NodeTypes)

`Background → Scene → HUD → MainUI → PopupUI → ClosedPopupUI → GlobalUI → SafeMode`

All are separate RectTransform containers parented under a master canvas hierarchy. UI manager sorts windows into NodeType buckets and controls show/hide per bucket.

### HybridFrame plugin API (4 UILayers)

```csharp
public enum UILayer { Background, Default, Overlay, Modal }
```

Defined in `IPluginAPI.CreateUIPanel(name, layer)`.

### Current Maqui

No layer system. OneUI's `UIFramework` has a flat `List<IBaseView>` and hides the previous view on every navigate call.

### Gap assessment

**Severity: High.** The single-view navigator pattern collapses when multiple independent views need to coexist (HUD + inventory panel + modal dialog). ORO's plugin system inherently requires concurrent windows at different layers.

### Verdict: **Modify Maqui**

Add `MaquiWindowManager` — a persistent MonoBehaviour (created by `CoreBootstrap`) that owns 4 child Canvases:

```
Maqui_Core [DontDestroyOnLoad]
├── WindowManager
│   ├── Layer_Background [Canvas sortOrder=0]
│   ├── Layer_Default    [Canvas sortOrder=100]
│   ├── Layer_Overlay    [Canvas sortOrder=200]
│   ├── Layer_ModalMask  [Canvas sortOrder=290] ← semi-transparent black (see §5)
│   └── Layer_Modal      [Canvas sortOrder=300]
```

```csharp
// com.ware.maqui / Runtime/Core/Presentation/MaquiWindowManager.cs
public class MaquiWindowManager : MonoBehaviour
{
    public static MaquiWindowManager Instance { get; private set; }

    private readonly Dictionary<UILayer, Canvas> _layers = new();
    private readonly Dictionary<UILayer, int> _occupancy = new();

    public Canvas GetLayer(UILayer layer) => _layers[layer];

    public IWindowHandle ShowWindow(GameObject prefab, UILayer layer, string pluginId = null)
    {
        var parent = _layers[layer].GetComponent<RectTransform>();
        var go = Instantiate(prefab, parent);
        _occupancy[layer]++;
        OnLayerChanged(layer);
        return new WindowHandle(go, layer, this);
    }

    public void HideWindow(WindowHandle handle)
    {
        Destroy(handle.Root);
        _occupancy[handle.Layer] = Mathf.Max(0, _occupancy[handle.Layer] - 1);
        OnLayerChanged(handle.Layer);
    }

    private void OnLayerChanged(UILayer layer)
    {
        if (layer == UILayer.Modal)
        {
            bool hasModal = _occupancy[UILayer.Modal] > 0;
            _layers.TryGetValue((UILayer)290, out var mask); // ModalMask pseudo-layer
            if (mask) mask.gameObject.SetActive(hasModal);
            // Notify freeze state
            HF.Publish(new UIFreezeChangedCommand { IsFrozen = hasModal });
        }
    }
}
```

**ORO layer mapping:**

| ORO/OxGFrame NodeType | Maqui UILayer | Sort Order |
|:---|:---|:---:|
| Background / Scene | Background | 0 |
| HUD | Overlay | 200 |
| MainUI / PopupUI | Default | 100 |
| GlobalUI / SafeMode | Overlay | 200 |
| Modal dialogs | Modal | 300 |

4 UILayers cover all practical ORO use cases. The ClosedPopupUI NodeType (OxGFrame-specific animation staging area) is not needed — Maqui's AnimationBridge handles in-place show/hide.

---

## 3. Async Prefab Loading

### OxGFrame target

`await UIManager.OpenAsync<InventoryWindow>()` — resolves the prefab address from a registration table, loads from YooAsset, instantiates, shows.

### Current Maqui

`MaquiNavigator.NavigateReactive<TView, TViewModel>` uses `Resources.Load<GameObject>` synchronously. No async path exists.

### Gap assessment

**Severity: High.** ORO plugins deliver assets via YooAsset encrypted bundles from CDN. Synchronous `Resources.Load` cannot reach these assets. Blocking the main thread for prefab loading is also unacceptable in production.

### Verdict: **Modify Maqui** (interface) **+ Adapt Integration** (ORO provides implementation)

Add an `IMaquiAssetProvider` interface to the package and register it from `CoreBootstrap`:

```csharp
// com.ware.maqui / Runtime/Core/Bridge/IMaquiAssetProvider.cs
public interface IMaquiAssetProvider
{
    UniTask<GameObject> LoadPrefabAsync(string key, CancellationToken ct = default);
    void ReleasePrefab(string key);
}

// Default: Resources folder (current sandbox behavior)
public class ResourcesAssetProvider : IMaquiAssetProvider
{
    public UniTask<GameObject> LoadPrefabAsync(string key, CancellationToken ct)
        => UniTask.FromResult(Resources.Load<GameObject>(key));

    public void ReleasePrefab(string key) { }
}
```

ORO registers its own provider during `HFLauncher` boot, before Maqui windows are opened:

```csharp
// In ORO's boot procedure (HotUpdate side):
HF.Get<IMaquiAssetProvider>(); // resolves to ORO's YooAssetProvider
// OR via Maqui's own registration:
MaquiAssetProviderBridge.SetProvider(new YooAssetMaquiProvider());
```

`MaquiWindowManager.ShowWindowAsync(key, layer)` calls `IMaquiAssetProvider.LoadPrefabAsync` instead of `Resources.Load`.

**Pros:**
- Maqui works out-of-the-box in CharqUI sandbox (ResourcesAssetProvider)
- ORO drops in YooAsset without touching Maqui package internals

**Cons:**
- Requires registration call during boot — one extra line in ORO's HFLauncher

---

## 4. UIFreezeManager

### OxGFrame target

`UIFreezeManager` is an imperative singleton: `UIFreezeManager.Freeze()` / `UIFreezeManager.Unfreeze()`. Called automatically when a UI node with `freeze: true` is shown. All non-modal input stops responding.

### Current Maqui

No freeze manager. AnimationBridge sets `CanvasGroup.blocksRaycasts = false` during transitions — this is per-view, not system-wide.

### Gap assessment

**Severity: Moderate.** ORO needs input suppression when the card game opens fullscreen or when a modal dialog is active. The missing freeze primitive is real.

### Verdict: **Adapt Integration (VitalRouter command approach — better than OxGFrame's pattern)**

The imperative freeze manager is an anti-pattern in a reactive system. Maqui's philosophy: state changes propagate via commands, subscribers react.

`MaquiWindowManager` publishes `UIFreezeChangedCommand` whenever the Modal layer occupancy changes (see §2 code above). Windows that need to suppress input subscribe:

```csharp
// Shared.Contracts (HybridFrame boundary):
public readonly record struct UIFreezeChangedCommand(bool IsFrozen) : ICommand;

// In a HUD window:
[Routes]
public partial class HUDWindow : ReactiveBaseView<HUDViewModel>
{
    [Subscribe]
    public void On(UIFreezeChangedCommand cmd)
    {
        GetComponent<CanvasGroup>().interactable = !cmd.IsFrozen;
        GetComponent<GraphicRaycaster>().enabled = !cmd.IsFrozen;
    }
}
```

**Pros over OxGFrame:**
- Fully decoupled — the modal knows nothing about what subscribes to freeze events
- Testable — inject a freeze command in tests without touching the UI hierarchy
- Extensible — custom freeze behavior per window, not a global boolean

**Cons:**
- Requires `[Routes]` on windows that want freeze awareness (explicit, not implicit)
- Windows that forget to subscribe don't freeze — opt-in vs opt-out

**Mitigation:** `ReactiveBaseView`'s new `OnFreeze()`/`OnUnfreeze()` hooks (see §1) bridge this. `MaquiWindowManager` directly calls these hooks on all registered views in the Default/Overlay layers when Modal occupancy changes. No subscription required.

---

## 5. Modal Mask / UIMaskManager

### OxGFrame target

`UIMaskManager` creates a semi-transparent overlay behind the top modal window. It tracks the mask's opacity and input-blocking state separately from the window stack.

### Current Maqui

No mask. No overlay.

### Gap assessment

**Severity: Low.** The mask is a visual affordance (darken/blur background) and an input blocker. Both are achievable with a Canvas layer.

### Verdict: **Modify Maqui (trivial)**

Add a `ModalMask` prefab to `MaquiWindowManager`. It's a full-screen Canvas at sort order 290 (between Overlay and Modal), with a `CanvasGroup` (alpha = 0.6) and `Image` (color = black). Enabled/disabled automatically when Modal layer occupancy changes (already wired in §2).

Optional: replace the `Image` with a `TranslucentImage` (Le Tai's) for glassmorphism modal masks — wired via `ThemeData.BlurStrength`.

---

## 6. IUIService / IWindowHandle Contracts

### OxGFrame / ORO target

Plugins receive a scoped `IPluginAPI` proxy. Through it:
```csharp
RectTransform panel = api.CreateUIPanel("CardGame", UILayer.Modal);
```

The panel's lifetime is tracked per-plugin for cleanup on unload.

### Current Maqui

`MaquiNavigator` is a static class. No interface, no contract. Cannot be registered as an HF service. Cannot be given to a plugin proxy.

### Gap assessment

**Severity: High.** Without an `IUIService` interface, Maqui cannot participate in HybridFrame's service locator or ORO's `SandboxedPluginAPI`.

### Verdict: **Modify Maqui**

Add `IUIService` and `IWindowHandle` to the Maqui package's contract surface:

```csharp
// com.ware.maqui / Runtime/Core/Presentation/IUIService.cs
public interface IUIService
{
    // Show a typed Maqui window (preferred for first-party code)
    UniTask<IWindowHandle> ShowWindowAsync<TView, TViewModel>(
        UILayer layer,
        Action<TViewModel> configure = null,
        CancellationToken ct = default)
        where TView : ReactiveBaseView<TViewModel>
        where TViewModel : ViewModel, new();

    // Show a raw prefab (for plugin-loaded assets)
    UniTask<IWindowHandle> ShowPrefabAsync(
        string assetKey,
        UILayer layer,
        string pluginId = null,
        CancellationToken ct = default);

    // Cleanup all windows owned by a plugin (called on plugin unload)
    void CleanupPlugin(string pluginId);

    Canvas GetLayerCanvas(UILayer layer);
}

public interface IWindowHandle : IDisposable
{
    bool IsVisible { get; }
    UILayer Layer { get; }
    void Show();
    void Hide();
}
```

`MaquiWindowManager` implements `IUIService`. `CoreBootstrap` registers it:

```csharp
// In CoreBootstrap.cs:
var windowManager = maquiCore.AddComponent<MaquiWindowManager>();
HF.Register<IUIService>(windowManager); // registered to HF service locator
```

**ORO's `SandboxedPluginAPI`** then wraps it:

```csharp
// In ORO's SandboxedPluginAPI.cs:
public async UniTask<RectTransform> CreateUIPanel(string name, UILayer layer)
{
    CheckPermission("ui.create_panel");
    var handle = await HF.Get<IUIService>().ShowPrefabAsync(name, layer, _pluginId);
    _panelHandles.Add(handle); // tracked for cleanup
    return handle.Root.GetComponent<RectTransform>();
}

// Called by PluginServiceScope on plugin unload:
public void Cleanup() => HF.Get<IUIService>().CleanupPlugin(_pluginId);
```

---

## 7. Plugin UI Sandbox

### Target

`api.CreateUIPanel(name, UILayer.Modal)` — plugin creates a panel scoped to its bundle, sandboxed by `SandboxedPluginAPI`.

### Current Maqui

No plugin-aware surface. Windows have no owner tracking.

### Gap assessment

**Severity: High in context.** Without plugin ownership tracking, unloading a plugin leaves orphaned panels.

### Verdict: **Adapt Integration (ORO implements on top of Maqui's IUIService)**

With `IUIService.ShowPrefabAsync(key, layer, pluginId)` in place (§6), ORO's `SandboxedPluginAPI` is the correct place to:
1. Check the `"ui.create_panel"` permission
2. Scope the asset key to the plugin's YooAsset bundle group
3. Track the `IWindowHandle` for cleanup
4. Call `IUIService.CleanupPlugin(pluginId)` on `OnUnload()`

No additional Maqui changes needed. The contract (`IUIService`) is Maqui's concern. The enforcement is ORO's concern.

---

## 8. IWindowBuilder (Programmatic UI)

### Target

Plugins need to create UI programmatically at runtime — not from a prefab. A card game might generate card slot layouts dynamically.

### Current Maqui

No builder API. All views are prefab-based.

### Gap assessment

**Severity: Low.** OxGFrame's `IWindowBuilder` is rarely needed when plugins ship prefabs (which they should). It's an escape hatch.

### Verdict: **Adapt Integration (ORO responsibility, not Maqui)**

For dynamic UI:
- Plugins that use **UI Toolkit** create `VisualElement` trees — no builder needed, it's just code.
- Plugins that use **uGUI** ship prefabs in their YooAsset bundle and load them via `api.LoadAssetAsync<GameObject>`.

If a plugin truly needs fully programmatic uGUI: `api.CreateUIPanel` returns a `RectTransform` — the plugin can `AddComponent<>` and build the hierarchy manually. This is rare enough to not warrant a builder abstraction.

Maqui will **not** add a builder. Document the pattern in ORO's plugin guide.

---

## 9. VitalRouter [Subscribe] / [Routes] on Windows

### Target

Game windows subscribe to commands directly (intent-based routing):

```csharp
[Routes]
public partial class InventoryWindow : ReactiveBaseView<InventoryViewModel>
{
    [Subscribe]
    public void On(OpenInventoryCommand cmd) => Show();

    [Subscribe]
    public void On(CloseInventoryCommand cmd) => Hide();
}
```

### Current Maqui

Maqui uses `ICommandInterceptor` for cross-cutting concerns. Windows are passive — they are told to show/hide from outside.

### Gap assessment

**Severity: None / Design decision.** VitalRouter's `[Routes]`/`[Subscribe]` attributes work on any `partial class`. There is no technical blocker. This is purely a documentation and convention gap.

### Verdict: **Adapt Integration (document the pattern, no code changes)**

**Two complementary patterns, both valid:**

| Pattern | Use For |
|:---|:---|
| `ICommandInterceptor` | Cross-cutting: auth gates, analytics, freeze, error handling |
| `[Subscribe]` on window | Intent: "this window handles OpenInventory" |

`ReactiveBaseView<T>` windows become `partial` and add `[Routes]`:

```csharp
// ORO pattern:
[Routes(CommandOrdering.Sequential)]
public partial class InventoryWindow : ReactiveBaseView<InventoryViewModel>
{
    private void Start()
    {
        // Register with VitalRouter (lifetime tied to this MonoBehaviour)
        Router.Default.Subscribe(this).AddTo(this);
    }

    protected override void OnBind()
    {
        ViewModel.Items
            .Subscribe(RefreshList)
            .AddTo(Disposables);
    }

    [Subscribe]
    public async UniTask On(OpenInventoryCommand cmd, CancellationToken ct)
    {
        await AnimationBridge.Instance.ScaleAsync(rectTransform, Vector3.one, 0.25f, ct: ct);
    }
}
```

Document this in Maqui's routing docs. No package changes required.

---

## 10. Requirements Already Covered by Maqui

| Feature | Maqui component | Notes |
|:---|:---|:---|
| Reactive data binding | `ReactiveBaseView<T>` + R3 | Core strength — exceeds OxGFrame's basic binding |
| Theme system | `ThemeProvider`, `ThemeData`, `ThemeSubscriber` | Full color slot system, runtime switching |
| Async transitions | `AnimationBridge` (UniTask) | CancellationToken-safe, better than OxGFrame coroutines |
| DisplayOptions | `UIFramework.Navigate()` + `DisplayOptions` | Fade/Scale/None, duration, completion callback |
| Subscription cleanup | `CompositeDisposable` + `.AddTo(Disposables)` | Stronger than OxGFrame's manual cleanup |
| HUD rendering | Overlay UILayer canvas | Maps to OxGFrame's HUD NodeType |

---

## Recommended Maqui Changes (Priority Order)

### Must-do before ORO M1

1. **Add `MaquiWindowManager`** with 4-layer Canvas hierarchy — replaces OneUI's single-view navigator for multi-window scenarios. OneUI `UIFramework` remains for simple single-screen navigation in non-ORO projects.

2. **Add `IUIService` / `IWindowHandle` interfaces** — enables HF service registration and `SandboxedPluginAPI` integration.

3. **Add `IMaquiAssetProvider` interface** — enables YooAsset backend in ORO without modifying the Maqui package post-install.

### High-value additions (do in parallel with ORO M1)

4. **Extended lifecycle hooks** (`OnPreShowAsync`, `OnPreHide`, `OnFreeze`, `OnUnfreeze`) — completes the window lifecycle story. `MaquiWindowManager` calls these hooks.

5. **Modal mask** — 3 lines in `MaquiWindowManager`, activated on Modal layer occupancy.

### Not needed

- UIFreezeManager singleton — replaced by `UIFreezeChangedCommand` + `OnFreeze`/`OnUnfreeze` hooks
- OxGFrame-style 12-hook lifecycle — 7 hooks cover all functional needs
- 8 NodeType layers — 4 UILayers match ORO's `IPluginAPI` design exactly
- IWindowBuilder — out of scope for Maqui; ORO plugin guide covers the patterns

---

## Pros and Cons: Maqui vs Staying on OxGFrame

### If we adapt Maqui (recommended)

**Pros:**
- Single reactive paradigm: R3 + VitalRouter throughout, no event system mixing
- `UIFreezeChangedCommand` is testable and decoupled — OxGFrame freeze manager is a hidden global
- 4 UILayers match ORO's own API definition — no impedance mismatch
- Theme system (ThemeData/ThemeProvider) is built-in and reactive — OxGFrame has no equivalent
- Maqui is owned, can be modified — OxGFrame is a third-party dependency
- `ReactiveBaseView<T>` gives every view a typed ViewModel with R3 — OxGFrame UIBase is generic C# with no reactive model

**Cons:**
- Maqui requires 3 new systems (MaquiWindowManager, IUIService, IMaquiAssetProvider) — ~1 week dev work
- OneUI is still in the dependency chain — need to evaluate if it can be removed as Maqui matures
- Less battle-tested than OxGFrame at scale (OxGFrame ships in hundreds of commercial MMO titles)

### If we keep OxGFrame

**Pros:**
- Proven in production MMO at scale
- 12 lifecycle hooks, freeze manager, mask manager all built-in
- UIManager.OpenAsync<T>() just works for prefab loading

**Cons:**
- OxGFrame uses coroutines, not UniTask — async coordination between UI and game logic is awkward
- No reactive binding — every update is imperative `GetView<InventoryWindow>().Refresh(data)`
- No theme system — roll your own or use a third-party
- Not behind an interface — cannot register as HF service, cannot be mocked in tests
- OxGFrame license ties ORO to a third-party release cycle
- EventMessenger (OxGFrame) + VitalRouter (HF) running side-by-side creates two event spines — commands flow through both, debugging is harder
- ORO's `IPluginAPI.CreateUIPanel` already assumes a layer enum — doesn't map cleanly to OxGFrame NodeTypes
