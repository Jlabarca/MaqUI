# Maqui — Shared State Patterns

> How ViewModels communicate across windows. This is the blessed approach.

---

## The Two Patterns

Maqui windows are isolated by design: each `ReactiveBaseView<T>` owns a typed ViewModel and disposes it on close. Cross-window communication uses one of two patterns depending on whether the windows share *identity* (same domain) or merely *react to each other* (different domains).

---

### Pattern 1: Shared ViewModel Instance

Pass the **same ViewModel instance** to multiple windows. All views bind to the same reactive properties, so changes propagate automatically with zero boilerplate.

**When to use**: Windows that display different facets of the same data. A HUD badge and its detail panel. A settings modal that immediately reflects changes in the HUD. Any case where two views are "the same state, different lenses."

**How it works**: The orchestrator (a `MonoBehaviour` starter) creates one ViewModel and passes it to every `ShowWindowAsync` call.

```csharp
// Demo3Starter.cs — one VM, three windows
_sharedVm = new FrostedHUDViewModel();

// HUD (Overlay) — shows player name, XP, unread badge
_hudHandle = await MaquiServices.Get<IUIService>().ShowWindowAsync<FrostedHUDView, FrostedHUDViewModel>(
    "Views/Demo3_FrostedHUD", UILayer.Overlay, _sharedVm, ct);

// Notification list (Modal) — same VM, binds to Notifications + UnreadCount
_notifHandle = await MaquiServices.Get<IUIService>().ShowWindowAsync<NotificationView, FrostedHUDViewModel>(
    "Views/Demo3_Notifications", UILayer.Modal, _sharedVm, ct);

// Settings (Modal) — same VM, binds to MasterVolume, MusicVolume, etc.
_settingsHandle = await MaquiServices.Get<IUIService>().ShowWindowAsync<SettingsView, FrostedHUDViewModel>(
    "Views/Demo3_Settings", UILayer.Modal, _sharedVm, ct);
```

The ViewModel itself is a standard `ViewModel` subclass with reactive properties covering all shared state:

```csharp
public sealed class FrostedHUDViewModel : ViewModel
{
    public readonly ReactiveProperty<int>   UnreadCount  = new(3);
    public readonly ReactiveProperty<float> MasterVolume = new(0.8f);
    public readonly ReactiveProperty<IReadOnlyList<NotificationEntry>> Notifications;
    // ...each view subscribes to whichever properties it cares about
}
```

Each view's `OnBind()` subscribes to the subset of properties it needs. When SettingsView writes `ViewModel.MasterVolume.Value = 0.5f`, the HUD view's subscription fires immediately.

**Lifetime rule**: The orchestrator owns the ViewModel. Individual windows must **not** dispose it in `OnViewDestroy()` — only the orchestrator disposes it (typically in `OnDestroy()`). Since `ReactiveBaseView<T>.OnViewDestroy()` calls `ViewModel.Dispose()` by default, override it in shared-VM views to skip VM disposal, or structure the orchestrator so the VM outlives all its windows.

---

### Pattern 2: Commands + Per-Window ViewModels

Each window gets its **own ViewModel**. Cross-window effects travel through VitalRouter commands. The orchestrator listens for commands and brokers the interaction.

**When to use**: Windows with independent domains that occasionally trigger side effects in each other. A shop that deducts gold from the HUD. An inventory that opens a shop modal. Any case where the windows have different data models and only share *events*, not *state*.

**How it works**: Each window has a dedicated ViewModel. The orchestrator routes commands and bridges data between VMs manually.

```csharp
// Demo2Starter.cs — separate VMs, command-based coordination
_hudVm = new HUDViewModel();    // Gold, HP, MP, StatusMsg
_hudHandle = await MaquiServices.Get<IUIService>().ShowWindowAsync<HUDView, HUDViewModel>(
    "Views/Demo2_HUD", UILayer.Overlay, _hudVm, ct);

// Inventory gets its own VM
[Route]
public async UniTask On(OpenInventoryCommand _, CancellationToken ct)
{
    var vm = new InventoryViewModel();
    _inventoryHandle = await MaquiServices.Get<IUIService>()
        .ShowWindowAsync<InventoryView, InventoryViewModel>("Views/Demo2_Inventory", UILayer.Default, vm, ct);
}

// Shop gets its own VM — orchestrator passes gold snapshot at open time
[Route]
public async UniTask On(OpenShopCommand _, CancellationToken ct)
{
    var vm = new ShopViewModel();
    _shopHandle = await MaquiServices.Get<IUIService>()
        .ShowWindowAsync<ShopView, ShopViewModel>("Views/Demo2_Shop", UILayer.Modal, vm, ct);

    _shopHandle.Root.GetComponent<ShopView>()?.SetPlayerGold(_hudVm.Gold.CurrentValue);
}
```

Commands are zero-data signal structs or carry minimal payload:

```csharp
public readonly record struct OpenInventoryCommand  : ICommand;
public readonly record struct CloseInventoryCommand : ICommand;
public readonly record struct ItemPurchasedCommand(string ItemName, int Cost) : ICommand;
```

The orchestrator handles cross-domain side effects (e.g., `ItemPurchasedCommand` calls `_hudVm.SpendGold(cost)`). Each ViewModel stays focused on its own domain.

---

## Decision Matrix

| Signal | Pattern | Why |
|:---|:---|:---|
| HUD badge reflects notification count | Shared VM | Same data, different rendering |
| Settings slider updates HUD state | Shared VM | Same domain (app config) |
| Shop purchase deducts HUD gold | Commands | Different domains (commerce vs. player stats) |
| Inventory button opens shop modal | Commands | Navigation intent, no shared state |
| Chat window echoes combat log | Commands | Separate systems, event-driven |
| Party frame + party detail panel | Shared VM | Same party data, two projections |

**Rule of thumb**: if you would model the data in one class, use a shared VM. If you would model it in two classes that happen to interact, use commands.

---

## Anti-Patterns

| Anti-pattern | Why it fails | Fix |
|:---|:---|:---|
| `FindObjectOfType<OtherView>()` to read its VM | O(n), fragile, breaks when view is pooled or destroyed | Use shared VM or publish a command |
| Static / global ViewModel singletons | Untestable, unclear lifetime, survives scene loads | Orchestrator owns the VM, passes it explicitly |
| View A directly calling View B's methods | Couples presentation layers, breaks if B is not open | Publish a command; let the orchestrator broker |
| Shared VM where each view only touches disjoint fields | Signals "these are really separate VMs" | Split into per-window VMs + commands |
| Disposing a shared VM from a closing window | Kills reactive state for all other windows still open | Only the orchestrator (owner) disposes shared VMs |
| Passing VM references through static fields | Race conditions, unclear ownership | Pass through `ShowWindowAsync` or `SetHandle` |
