# Maqui — Routing & Commands

> VitalRouter is the command bus. All inter-system communication flows through it.

---

## 1. Command Anatomy

Commands are `struct` types implementing `VitalRouter.ICommand`. They carry only data — no logic.

```csharp
// ✅ Correct: value type, POD data, descriptive name
public struct PlayerLevelUpCommand : ICommand
{
    public string PlayerId;
    public int CurrentXP;
}

// ✅ Correct: zero-data signal command
public struct CloseInventoryCommand : ICommand { }

// ❌ Wrong: class allocation, logic in command
public class LoadPlayerDataCommand : ICommand // should be struct
{
    public async Task Execute() { } // logic does not belong here
}
```

---

## 2. Publishing Commands

```csharp
// Fire-and-forget (navigation, UI signals)
Router.Default.PublishAsync(new CloseInventoryCommand()).Forget();

// Await completion (when you need the interceptor to finish before continuing)
await Router.Default.PublishAsync(new SaveGameCommand { SlotId = 0 });

// With cancellation
await Router.Default.PublishAsync(new FetchLeaderboardCommand(), cancellationToken: ct);
```

`RouterBridge` initializes `Router.Default` in its `Awake()`. Because `CoreBootstrap` runs `BeforeSceneLoad`, `Router.Default` is always ready by the time any scene `Awake()` fires.

---

## 3. Interceptor Pipeline

```mermaid
---
config:
  theme: dark
---
sequenceDiagram
    participant Publisher
    participant IC1 as Interceptor A (Logging)
    participant IC2 as Interceptor B (Auth Guard)
    participant IC3 as Interceptor C (Domain Handler)
    participant End as Pipeline End

    Publisher->>IC1: PublishAsync(cmd)
    IC1->>IC1: Log command name
    IC1->>IC2: await next(cmd, ctx)
    IC2->>IC2: Check auth token
    alt Not authenticated
        IC2-->>Publisher: return (swallows command)
    else Authenticated
        IC2->>IC3: await next(cmd, ctx)
        IC3->>IC3: Handle domain logic
        IC3->>End: await next(cmd, ctx)
    end
```

Each interceptor decides whether to:
- **Pass through**: `await next(command, context)` — forwards to the next interceptor.
- **Swallow**: `return` without calling `next` — terminates the pipeline for this command.
- **Transform**: modify data and `await next(...)` with the same or a different command.

```csharp
public class AuthGuardInterceptor : ICommandInterceptor
{
    private readonly IAuthService _auth;
    public AuthGuardInterceptor(IAuthService auth) => _auth = auth;

    public async ValueTask InvokeAsync<T>(T command, PublishContext ctx, PublishContinuation<T> next)
        where T : ICommand
    {
        // Only guard specific command types
        if (command is IRequiresAuthCommand && !_auth.IsLoggedIn)
        {
            Router.Default.PublishAsync(new ShowLoginCommand()).Forget();
            return; // swallow
        }

        await next(command, ctx);
    }
}
```

---

## 4. Registering & Unregistering Interceptors

```csharp
// Register — typically in Start() or Initialize()
var filter = new InventoryInterceptor(_vm, _inventoryService);
Router.Default.AddFilter(filter);

// Unregister — always in OnDestroy() to prevent phantom handling
private void OnDestroy()
{
    Router.Default.UnfilterAsync<InventoryInterceptor>().Forget();
}
```

> **Interceptor lifetime is your responsibility.** Maqui does not automatically remove interceptors when scenes unload. Failing to call `UnfilterAsync` will cause the interceptor to handle commands after its owning objects are destroyed, resulting in `MissingReferenceException` or silent logic bugs.

---

## 5. Async Navigation Pipeline

VitalRouter's interceptors are `async ValueTask`, making it trivial to build ordered async navigation:

```csharp
public class ScreenTransitionInterceptor : ICommandInterceptor
{
    private readonly AnimationBridge _anim;
    private readonly CanvasGroup _currentScreen;

    public ScreenTransitionInterceptor(AnimationBridge anim, CanvasGroup currentScreen)
    {
        _anim = anim;
        _currentScreen = currentScreen;
    }

    public async ValueTask InvokeAsync<T>(T command, PublishContext ctx, PublishContinuation<T> next)
        where T : ICommand
    {
        if (command is INavigationCommand)
        {
            // 1. Fade OUT current screen
            await _anim.FadeAsync(_currentScreen, targetAlpha: 0f, duration: 0.25f);

            // 2. Execute the navigation (next interceptor loads the new screen)
            await next(command, ctx);

            // 3. Fade IN happens in the next interceptor or in the new view's OnBind
            return;
        }

        await next(command, ctx);
    }
}
```

The pattern gives you a **guaranteed before/after hook** around any navigation event, with no coroutine juggling.

---

## 6. Command Naming Conventions

| Pattern | Example | When |
|:---|:---|:---|
| `Verb + Noun + Command` | `ShowProfileCommand` | User-initiated navigation |
| `Noun + Changed` (as command) | `UserDataChangedCommand` | System state notifications |
| `Request + Noun + Command` | `RequestLogoutCommand` | Requires confirmation before executing |
| `Noun + Failed + Command` | `NetworkRequestFailedCommand` | Error signaling |

Keep command names in the domain language, not the UI language. `RequestLogoutCommand` is correct; `ClickedLogoutButtonCommand` is not.

---

## 7. RouterBridge

`RouterBridge` is the `MonoBehaviour` that owns the `Router` instance:

```csharp
// Maqui.Core.Bridge.RouterBridge
public class RouterBridge : MonoBehaviour
{
    public static RouterBridge Instance { get; private set; }
    public Router Router { get; private set; }

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
            Router = new Router();
        }
        else Destroy(gameObject);
    }
}
```

`Router.Default` is VitalRouter's process-level default router. `RouterBridge.Instance.Router` is the same instance — they are interchangeable. Prefer `Router.Default` in userland code for brevity.

---

## 8. Command Tracing (Debug)

Register a logging interceptor first in the chain to trace all commands:

```csharp
#if UNITY_EDITOR || DEVELOPMENT_BUILD
Router.Default.AddFilter(new CommandLoggerInterceptor());
#endif

public class CommandLoggerInterceptor : ICommandInterceptor
{
    public async ValueTask InvokeAsync<T>(T command, PublishContext ctx, PublishContinuation<T> next)
        where T : ICommand
    {
        UnityEngine.Debug.Log($"[Router] → {typeof(T).Name}");
        await next(command, ctx);
    }
}
```

Add this as the first `AddFilter` call — interceptors are invoked in registration order.
