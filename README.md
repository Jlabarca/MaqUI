# Maqui

**Reactive MVVM UI framework for Unity.**
R3-powered ViewModels · VitalRouter command routing · Layer-based window management · HybridFrame-ready plugin architecture.

> Requires Unity 2022.3 LTS or newer.

---

## Features

- **MVVM without the ceremony** — `ViewModel` (pure C#) + `ReactiveBaseView<T>` (Unity). No code-gen, no DI container required.
- **R3 reactive state** — `ReactiveProperty<T>`, `CompositeDisposable`, first-class `async/await` via UniTask.
- **VitalRouter command bus** — `[Subscribe]` attributes on windows for clean show/hide intent. `ICommandInterceptor` for cross-cutting concerns.
- **Four-layer Canvas hierarchy** — `Background / Default / Overlay / Modal` managed automatically by `MaquiWindowManager`.
- **Modal freeze system** — Opening a `Modal` window auto-activates the mask overlay and calls `OnFreeze()` on every active view. Closing restores everything.
- **Async asset loading** — Swappable `IMaquiAssetProvider`. Ships with `ResourcesAssetProvider` (synchronous). Drop in a YooAsset or Addressables adapter with one call.
- **Runtime theming** — `ThemeData` ScriptableObject with 13 color slots. `ThemeImageSubscriber` / `ThemeTextSubscriber` components require zero code.
- **Zero-config bootstrap** — `[RuntimeInitializeOnLoadMethod]` fires before any scene loads. No prefab, no scene setup, no manual Awake ordering.
- **Plugin-safe** — `IUIService` / `IWindowHandle` contracts integrate with [HybridFrame](https://github.com/waremoto/hybridframe)'s `SandboxedPluginAPI`. `CleanupPlugin()` destroys all windows on plugin unload.

---

## Requirements

| Dependency | Version | How to install |
|:---|:---|:---|
| **Unity** | 2022.3 LTS+ | — |
| **R3** | latest | Git UPM (see below) |
| **UniTask** | latest | Git UPM (see below) |
| **VitalRouter** | 2.0.5+ | NuGetForUnity |
| **VitalRouter.R3** | 2.0.5+ | NuGetForUnity |
| **OneUI** (UIFramework + EventFramework) | — | Manual — see note below |

> **OneUI note**: `UIFramework` and `EventFramework` (DevsDaddy) must be present in your project's `Assets/` folder as `Maqui.Runtime.asmdef` references them by name. They are not bundled with this package.

---

## Installation

### Option A — Local path (same repository)

Add to your project's `Packages/manifest.json`:

```json
{
  "dependencies": {
    "com.ware.maqui": "file:../../com.ware.maqui",
    "com.cysharp.r3": "https://github.com/Cysharp/R3.git?path=src/R3.Unity/Assets/R3.Unity",
    "com.cysharp.unitask": "https://github.com/Cysharp/UniTask.git?path=src/UniTask/Assets/Plugins/UniTask"
  },
  "testables": ["com.ware.maqui"]
}
```

### Option B — Git URL

```json
{
  "dependencies": {
    "com.ware.maqui": "https://github.com/waremoto/maqui.git",
    "com.cysharp.r3": "https://github.com/Cysharp/R3.git?path=src/R3.Unity/Assets/R3.Unity",
    "com.cysharp.unitask": "https://github.com/Cysharp/UniTask.git?path=src/UniTask/Assets/Plugins/UniTask"
  }
}
```

### VitalRouter via NuGetForUnity

1. Install [NuGetForUnity](https://github.com/GlitchEnzo/NuGetForUnity) in your project.
2. Install `VitalRouter` and `VitalRouter.R3` from the NuGet window.

---

## Quick Start

### 1. Create a ViewModel

```csharp
using Maqui.Core.Logic;
using R3;

// Pure C# — no UnityEngine references
public class GreetingViewModel : ViewModel
{
    public readonly ReactiveProperty<string> Message = new("Hello, Maqui!");
    public readonly ReactiveProperty<int> ClickCount = new(0);

    public void OnButtonClicked() => ClickCount.Value++;
}
```

### 2. Create a View

```csharp
using Maqui.Core.Presentation;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class GreetingView : ReactiveBaseView<GreetingViewModel>
{
    [SerializeField] private TMP_Text messageLabel;
    [SerializeField] private TMP_Text countLabel;
    [SerializeField] private Button clickButton;

    protected override void OnBind()
    {
        ViewModel.Message
            .Subscribe(msg => messageLabel.text = msg)
            .AddTo(Disposables);

        ViewModel.ClickCount
            .Subscribe(n => countLabel.text = $"Clicks: {n}")
            .AddTo(Disposables);

        clickButton.onClick
            .AsObservable()
            .Subscribe(_ => ViewModel.OnButtonClicked())
            .AddTo(Disposables);
    }
}
```

### 3. Create a Prefab

- Create a UI prefab. The root needs a `CanvasGroup` component.
- Add the `GreetingView` MonoBehaviour and wire up the serialized fields.
- **Do not add a `Canvas` component** — Maqui parents it to the appropriate layer canvas.

### 4. Open the Window

```csharp
using Maqui.Core.Presentation;
using Cysharp.Threading.Tasks;

// Via IUIService (recommended — layer-aware, async, plugin-safe):
var vm = new GreetingViewModel();
var handle = await MaquiWindowManager.Instance.ShowWindowAsync<GreetingView, GreetingViewModel>(
    "Views/GreetingView",   // asset key (Resources path by default)
    UILayer.Default,
    vm,
    destroyCancellationToken
);

// Close:
handle.Dispose();
```

Or use `MaquiNavigator` for simple projects (no layer system):

```csharp
using Maqui.Core.Bridge;
MaquiNavigator.NavigateReactive<GreetingView, GreetingViewModel>("Views/GreetingView");
```

---

## Window Layers

`MaquiWindowManager` creates four persistent Canvases under `Maqui_Core` on startup:

| Layer | Sort Order | Typical Use |
|:---|:---:|:---|
| `UILayer.Background` | 0 | Skybox overlays, cutscene letterbox |
| `UILayer.Default` | 100 | Main panels: inventory, character, map, NPC dialog |
| `UILayer.Overlay` | 200 | HUD: HP bars, minimap, hotbar, buff icons |
| `UILayer.Modal` | 300 | Blocking dialogs, fullscreen content, plugin store |

A `ModalMask` Canvas (sortOrder 290, semi-transparent black) activates automatically when any Modal window opens and deactivates when the last one closes.

---

## VitalRouter Command Routing

### Window subscribes to its own open/close commands

```csharp
using VitalRouter;
using Cysharp.Threading.Tasks;
using System.Threading;

public readonly record struct OpenInventoryCommand : ICommand;
public readonly record struct CloseInventoryCommand : ICommand;

[Routes(CommandOrdering.Sequential)]
public partial class InventoryWindow : ReactiveBaseView<InventoryViewModel>
{
    private void Start() => Router.Default.Subscribe(this).AddTo(this);

    [Subscribe]
    public async UniTask On(OpenInventoryCommand cmd, CancellationToken ct)
    {
        await AnimationBridge.Instance.FadeAsync(GetComponent<CanvasGroup>(), 1f, 0.2f, ct);
    }

    [Subscribe]
    public void On(CloseInventoryCommand cmd) => HideView();

    protected override void OnBind()
    {
        ViewModel.Items
            .Subscribe(RefreshGrid)
            .AddTo(Disposables);
    }
}
```

### Cross-cutting interceptors

```csharp
public class AuthInterceptor : ICommandInterceptor
{
    public async UniTask InvokeAsync<T>(T cmd, CancellationToken ct, Func<T, CancellationToken, UniTask> next)
        where T : ICommand
    {
        if (!PlayerSession.IsAuthenticated)
        {
            Router.Default.PublishAsync(new ShowLoginCommand()).Forget();
            return;
        }
        await next(cmd, ct);
    }
}

// In a MonoBehaviour:
private AuthInterceptor _auth;
private void Start() { _auth = new AuthInterceptor(); Router.Default.AddFilter(_auth); }
private async void OnDestroy() { await Router.Default.UnfilterAsync<AuthInterceptor>(); }
```

---

## Freeze-Aware Windows

Windows that need custom behaviour when a Modal opens override `OnFreeze` / `OnUnfreeze`:

```csharp
public class HUDWindow : ReactiveBaseView<HUDViewModel>
{
    [SerializeField] private CanvasGroup inputGroup;

    protected override void OnFreeze()   => inputGroup.interactable = false;
    protected override void OnUnfreeze() => inputGroup.interactable = true;

    protected override void OnBind()
    {
        ViewModel.CurrentHP
            .Subscribe(hp => hpBar.value = hp)
            .AddTo(Disposables);
    }
}
```

---

## Async Pre-fetch

`OnPreShowAsync` runs after the prefab is instantiated but before `OnBind()` and any show animation. Use it to fetch data that must be ready before the window is visible:

```csharp
protected override async UniTask OnPreShowAsync(CancellationToken ct)
{
    await ViewModel.LoadCharacterDataAsync(ct);
}
```

---

## Runtime Theming

### No-code (Inspector)

Add `ThemeImageSubscriber` or `ThemeTextSubscriber` to any UI component, pick a `ColorType` slot — done.

### In code

```csharp
// Switch theme at runtime:
var darkTheme = Resources.Load<ThemeData>("Themes/Theme_Dark");
ThemeProvider.Instance.SetTheme(darkTheme);

// React in a View:
ThemeProvider.Instance.CurrentTheme
    .Subscribe(t => background.color = t.GetColor(ThemeColorType.BackgroundPrimary))
    .AddTo(Disposables);
```

---

## Custom Asset Provider

The default `ResourcesAssetProvider` uses synchronous `Resources.Load`. To use YooAsset, Addressables, or any other system, implement `IMaquiAssetProvider` and register it early in your boot sequence — before any window opens:

```csharp
public class YooAssetProvider : IMaquiAssetProvider
{
    public async UniTask<GameObject> LoadPrefabAsync(string key, CancellationToken ct)
    {
        var handle = YooAssets.LoadAssetAsync<GameObject>(key);
        await handle.ToUniTask(cancellationToken: ct);
        return handle.AssetObject as GameObject;
    }

    public void ReleasePrefab(string key) => YooAssets.UnloadUnusedAssets();
}

// In boot:
MaquiAssetProviderBridge.SetProvider(new YooAssetProvider());
```

---

## HybridFrame Integration

Maqui is designed to run as the UI layer of a [HybridFrame](https://github.com/waremoto/hybridframe) application. `MaquiWindowManager` implements `IUIService`, which you register with HF's service locator:

```csharp
// After CoreBootstrap has run (it fires BeforeSceneLoad, so MaquiWindowManager exists):
HF.Register<IUIService>(MaquiWindowManager.Instance);
```

Plugin UI creation flows through `SandboxedPluginAPI → IUIService.ShowPrefabAsync(pluginId: ...)`. On plugin unload:

```csharp
public void OnUnload() => HF.Get<IUIService>().CleanupPlugin(Manifest.Id);
```

This destroys all windows registered under that plugin ID without requiring the plugin to track them individually.

---

## Samples

Import samples via **Package Manager → Maqui → Samples**.

| Sample | What it shows |
|:---|:---|
| **Welcome** | Minimal routing flow: publish a command, intercept it, navigate to a new view |
| **Grand Tour** | Multi-screen app with shared `GlobalAppStateViewModel`, theming, and hybrid uGUI + UI Toolkit |
| **Shark Suite** | Reactive inventory: UI Toolkit list + uGUI detail panel driven by one ViewModel |
| **Modernization** | Before/after: migrating a procedural view to MVVM with ThemeSubscriber |

---

## Project Structure

```
com.ware.maqui/
├── Runtime/Core/
│   ├── CoreBootstrap.cs            Zero-config startup
│   ├── Bridge/                     AnimationBridge, ThemeProvider, RouterBridge,
│   │                               InputBridge, MaquiNavigator, IMaquiAssetProvider
│   ├── Logic/                      ViewModel, ThemeData, ThemeColorType
│   └── Presentation/               ReactiveBaseView<T>, MaquiWindowManager,
│                                   IUIService, IWindowHandle, UILayer,
│                                   ThemeSubscriber, ThemeImageSubscriber, ThemeTextSubscriber
├── Editor/
├── Tests/
│   ├── Runtime/                    ViewModelTests, ThemeDataTests
│   └── Editor/                     MaquiEditorTests
└── Samples~/
    ├── Welcome/
    ├── GrandTour/
    ├── SharkSuite/
    └── Modernization/
```

---

## License

MIT — see [LICENSE](LICENSE).

Built on top of [R3](https://github.com/Cysharp/R3), [UniTask](https://github.com/Cysharp/UniTask), [VitalRouter](https://github.com/hadashiA/VitalRouter), and [DevsDaddy OneUI](https://assetstore.unity.com/).

Designed for use with [HybridFrame](https://github.com/waremoto/hybridframe).
