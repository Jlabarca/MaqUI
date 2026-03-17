# Maqui — Quick Start

> Target: senior Unity developer, first integration. Goal: running reactive view in **under 15 minutes**.

---

## Prerequisites

| Requirement | Version | Notes |
|:---|:---|:---|
| Unity | 2022.3.x LTS+ | URP project recommended |
| R3.Unity | latest | via Git UPM |
| UniTask | 2.x | via Git UPM |
| VitalRouter | 2.0.5+ | via NuGetForUnity |
| UIFramework (OneUI) | any | Must be in `Assets/` as `UIFramework` assembly |

---

## Step 1 — Install the Package

### Option A: Local path (development)
In `Packages/manifest.json`:
```json
{
  "dependencies": {
    "com.ware.maqui": "file:../../com.ware.maqui"
  }
}
```

### Option B: Git URL (distribution — when published)
```json
{
  "dependencies": {
    "com.ware.maqui": "https://github.com/ware/maqui.git"
  }
}
```

After the package resolves, Unity compiles `Maqui.Runtime`. No further setup is required — `CoreBootstrap` fires automatically on every scene load.

Verify the bootstrap ran:
```
[Maqui] Initializing Core Foundation...
[Maqui] VitalRouter Initialized.
[Maqui] Core Foundation Ready.
```

---

## Step 2 — Create Your First ViewModel

ViewModels live in your game's assembly (not in Maqui). They hold state as R3 `ReactiveProperty<T>` fields and expose commands as methods that publish to `Router.Default`.

```csharp
// MyGame/Runtime/UI/ProfileViewModel.cs
using Maqui.Core.Logic;
using R3;
using VitalRouter;

public class ProfileViewModel : ViewModel
{
    public readonly ReactiveProperty<string> DisplayName = new("Anonymous");
    public readonly ReactiveProperty<int> Level = new(1);
    public readonly ReactiveProperty<bool> IsLoading = new(false);

    public override void Initialize()
    {
        // Kick off any async data fetch here.
        // Do NOT await in Initialize — use fire-and-forget or expose a Task.
    }

    public void RequestLevelUp()
    {
        Router.Default.PublishAsync(new LevelUpCommand { PlayerId = "local" }).Forget();
    }
}

// Keep commands as structs — zero allocation on the hot path.
public struct LevelUpCommand : ICommand
{
    public string PlayerId;
}
```

**Rules**:
- ViewModel must be `sealed` or `abstract` — avoid deep inheritance chains.
- Never reference `UnityEngine` types inside a ViewModel. If you need a `Sprite`, store its resource path as a `string` and resolve it in the View.
- `Disposables` is managed by the base class. Subscribe to your own reactive chains inside `Initialize()` and `.AddTo(Disposables)`.

---

## Step 3 — Create Your View

```csharp
// MyGame/Runtime/UI/ProfileView.cs
using Maqui.Core.Presentation;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using R3;

public class ProfileView : ReactiveBaseView<ProfileViewModel>
{
    [SerializeField] private TMP_Text nameLabel;
    [SerializeField] private TMP_Text levelLabel;
    [SerializeField] private Button levelUpButton;
    [SerializeField] private GameObject loadingSpinner;

    protected override void OnBind()
    {
        ViewModel.DisplayName
            .Subscribe(n => nameLabel.text = n)
            .AddTo(Disposables);

        ViewModel.Level
            .Subscribe(lv => levelLabel.text = $"Level {lv}")
            .AddTo(Disposables);

        ViewModel.IsLoading
            .Subscribe(loading => loadingSpinner.SetActive(loading))
            .AddTo(Disposables);

        levelUpButton.onClick.AsObservable()
            .Subscribe(_ => ViewModel.RequestLevelUp())
            .AddTo(Disposables);
    }
}
```

**Rules**:
- `OnBind()` is the only place you write binding code. It is called once, immediately after the ViewModel is injected.
- Never call `ViewModel.SomeReactiveProperty.Value` directly to read current state for rendering — subscribe and let the push update handle it.
- `Disposables` is cleared automatically in `OnViewDestroy()`. Do not manually dispose in `OnDestroy()`.

---

## Step 4 — Create the Prefab

1. Create a UI `GameObject` in your scene hierarchy (Canvas → Panel).
2. Attach `ProfileView` to the root object.
3. Assign the serialized field references (`nameLabel`, `levelLabel`, etc.).
4. Add a `CanvasGroup` to the root — required for `BaseView` fade transitions.
5. Save as a Prefab in `Assets/Resources/Views/ProfileView.prefab`.

> The `Resources/` path is required for `MaquiNavigator.NavigateReactive` to load the view. Alternatively, use `UIFramework.BindView(myView)` for scene-placed instances.

---

## Step 5 — Navigate to the View

### Option A: MaquiNavigator (resource-based, creates new ViewModel)
```csharp
MaquiNavigator.NavigateReactive<ProfileView, ProfileViewModel>(
    resourcePath: "Views/ProfileView",
    onComplete: view => Debug.Log("Profile view ready")
);
```
This loads the prefab from `Resources/Views/ProfileView`, instantiates it, creates a `ProfileViewModel`, calls `Initialize()`, then calls `UIFramework.Navigate(view)`.

### Option B: Scene-placed view with manual injection
```csharp
// In a MonoBehaviour scene controller:
[SerializeField] private ProfileView profileView;

private ProfileViewModel _vm;

private void Start()
{
    _vm = new ProfileViewModel();
    profileView.Initialize(_vm);
    profileView.ShowView(new DisplayOptions { IsAnimated = true });
}

private void OnDestroy()
{
    _vm?.Dispose();
    // ProfileView.OnViewDestroy() handles Disposables automatically.
}
```

---

## Step 6 — Handle the Command (Interceptor)

```csharp
// MyGame/Runtime/UI/ProfileInterceptor.cs
using VitalRouter;
using Cysharp.Threading.Tasks;

public class ProfileInterceptor : ICommandInterceptor
{
    private readonly ProfileViewModel _vm;
    private readonly IPlayerService _playerService;

    public ProfileInterceptor(ProfileViewModel vm, IPlayerService playerService)
    {
        _vm = vm;
        _playerService = playerService;
    }

    public async ValueTask InvokeAsync<T>(T command, PublishContext ctx, PublishContinuation<T> next)
        where T : ICommand
    {
        if (command is LevelUpCommand levelUpCmd)
        {
            _vm.IsLoading.Value = true;
            try
            {
                var newLevel = await _playerService.RequestLevelUpAsync(levelUpCmd.PlayerId);
                _vm.Level.Value = newLevel;
            }
            finally
            {
                _vm.IsLoading.Value = false;
            }
            return; // swallow — do not forward to next
        }

        await next(command, ctx); // pass through all other commands
    }
}
```

Register the interceptor once, close to where the ViewModel is created:
```csharp
Router.Default.AddFilter(new ProfileInterceptor(_vm, serviceLocator.Get<IPlayerService>()));
```

> **Caution**: `Router.Default.AddFilter` appends to a global list. Call `Router.Default.UnfilterAsync<ProfileInterceptor>()` when the screen is destroyed to avoid memory leaks and phantom command handling.

---

## Checklist Before Going Further

- [ ] `CoreBootstrap` logs appear in the Console on Play.
- [ ] ViewModel has no `using UnityEngine` statements.
- [ ] `OnBind()` has all subscriptions `.AddTo(Disposables)`.
- [ ] Prefab root has a `CanvasGroup` component.
- [ ] Interceptor is unregistered `OnDestroy` or when the owning scene unloads.
- [ ] Commands are `struct`, not `class`.
