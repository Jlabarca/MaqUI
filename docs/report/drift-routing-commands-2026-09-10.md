# Drift report — `CharqUI/docs/reference/routing-commands.md`

**Date:** 2026-09-10
**Doc under audit:** `CharqUI/docs/reference/routing-commands.md` (223 lines)
**Code audit base:** `main` @ `680d9d2` (2026-08-26)
**Reference doc was NOT edited** (per prompt).

---

## Why this doc

Shortest doc in the tree that makes a **high density of concrete, falsifiable claims about named types** — `RouterBridge`, `CoreBootstrap`, `Router.Default.AddFilter`, `UnfilterAsync`, `ICommandInterceptor`, `PublishContext`, `PublishContinuation<T>`, `ICommand`. Every one of those is either present in the repo or not; nothing in the audit depends on my judgement. That makes it the cheapest doc to take to a verdict rather than a pile of hedged "may be stale" findings.

**Verdict: total drift.** The doc describes a subsystem that does not merely differ from the code — it was *deleted* from the repo on 2026-08-21 in commit `b6a8f03` (`feat(sunset): delete the V1 MVVM stack — the package is immediate-mode only`). Not one of the six named types exists anywhere in first-party code, and zero first-party source files reference `VitalRouter` at all. The doc is indexed in `CharqUI/docs/README.md:19` as the live reference for "VitalRouter, commands, interceptors, async pipeline", so it is not already filed as archived history.

### The baseline check, once, in full

```
$ cd D:/ware/MaqUI
$ grep -rn "VitalRouter\|RouterBridge\|CoreBootstrap\|AnimationBridge\|UnfilterAsync\|MaquiNavigator\|ICommandInterceptor" \
    --include=*.cs --include=*.asmdef --include=*.json CharqUI/Assets com.ware.maqui \
    2>/dev/null | grep -v "/Packages/" | grep -v "\.meta"
(no output)
### exit=1
```

The single in-tree hit for any sibling doc's V1 name is a *tombstone comment*, not code — `CharqUI/Assets/UI/Pack/Common/Scripts/Gradient.cs:19`:

```
// (ThemeColorType fields + an IThemeProvider subscription via MaquiServices) was V1-only.
```

It is a note in a comment recording that the V1 coupling was stripped. It is not a live symbol reference. This report treats it as proof of deletion, not of survival.

---

## Drift list

### D1 — `RouterBridge` does not exist. The whole § 7 type listing is invented history.

- **Doc claim** — `routing-commands.md:174-196`: "`RouterBridge` is the `MonoBehaviour` that owns the `Router` instance", presented under the code comment `// Maqui.Core.Bridge.RouterBridge` with a listing showing `public static RouterBridge Instance { get; private set; }` and a `private void Awake()` that runs `Router = new Router();`.
- **Code** — `com.ware.maqui/Runtime/Core/` does not exist:
  ```
  $ ls com.ware.maqui/Runtime/Core
  ls: cannot access 'com.ware.maqui/Runtime/Core': No such file or directory
  ```
  Deleted in `b6a8f03`:
  ```
  $ git show --stat b6a8f03 -- com.ware.maqui/Runtime/Core/Bridge/RouterBridge.cs com.ware.maqui/Runtime/Core/CoreBootstrap.cs
   com.ware.maqui/Runtime/Core/Bridge/RouterBridge.cs | 21 ------
   com.ware.maqui/Runtime/Core/CoreBootstrap.cs       | 76 ----------------------
   2 files changed, 97 deletions(-)
  ```
- **Secondary point** — even the listing the doc *quotes* is not what `RouterBridge.cs` contained. The real deleted file (retrievable at `b6a8f03^`) had **no `Instance` static, no `DontDestroyOnLoad`, no singleton guard, and no `IRouterBridge`-free signature**. It was 21 lines:
  ```csharp
  public class RouterBridge : MonoBehaviour, IRouterBridge
  {
      public Router Router { get; private set; }
      private void Awake() { Router = new Router(); Debug.Log("[Maqui] VitalRouter Initialized."); }
  }
  ```
  So § 7 is drifted **twice over**: the class is gone, *and* the code block was never a faithful copy of it while it lived. Anyone who "restores" this file from the doc would reintroduce a singleton the original deliberately did not have.

### D2 — `CoreBootstrap` does not exist, so the § 2 readiness guarantee is vacuous.

- **Doc claim** — `:44`: "`RouterBridge` initializes `Router.Default` in its `Awake()`. Because `CoreBootstrap` runs `BeforeSceneLoad`, `Router.Default` is always ready by the time any scene `Awake()` fires."
- **Code** — `CoreBootstrap.cs` deleted in the same commit (stat above, 76 lines removed). No file in the repo declares `CoreBootstrap`.
- **Note on the mechanism** — the *shape* of the claim (a `[RuntimeInitializeOnLoadMethod(BeforeSceneLoad)]` initializer that warms `Router.Default`) survives, but it is owned by the **VitalRouter Unity package**, not by Maqui: `CharqUI/Library/PackageCache/jp.hadashikick.vitalrouter.unity@0739200fbb47/Runtime/Initializer.cs:6-8` — `public static class Initializer` / `[RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]`. The doc credits Maqui for a guarantee the third-party package provides.

### D3 — `Router.Default` is attributed a setter it does not have; `AddFilter` still exists.

- **Doc claim** — `:44` implies `RouterBridge`/`CoreBootstrap` *populate* `Router.Default`; `:198`: "`RouterBridge.Instance.Router` is the same instance — they are interchangeable."
- **Code** — reflected from the shipped assembly (`CharqUI/Assets/Packages/VitalRouter.2.0.5/lib/netstandard2.1/VitalRouter.dll`):
  ```
  Router.Default: static=True readonly=True type=VitalRouter.Router
  ```
  `Router.Default` is a `static readonly` field. It is initialized by the type's own static constructor and **cannot be assigned by any `Awake()`**. `RouterBridge` never initialised it, and the doc's claim that a Maqui `MonoBehaviour` did is not merely obsolete — it was never mechanically possible.
- **Independently confirmed correct:** `Router.Default.AddFilter(ICommandInterceptor)` (`:108`, `:208`) **does** exist on VitalRouter 2.0.5:
  ```
  $cd /tmp/vrprobe && dotnet run ... VitalRouter.dll
  -- VitalRouter.Router
     Void AddFilter(CommandOrdering ordering)
     Void AddFilter(ICommandInterceptor interceptor)
  ```
  So § 4's registration half and § 8's tracing snippet are API-accurate. The drift in this section is ownership, not method existence. Worth stating plainly so the drift list is not read as "everything here is wrong".

### D4 — `Router.Default.UnfilterAsync<T>()` does not exist on this version. No such method at all.

- **Doc claim** — `:111-114`:
  ```csharp
  private void OnDestroy() { Router.Default.UnfilterAsync<InventoryInterceptor>().Forget(); }
  ```
  and `:117`: "Failing to call `UnfilterAsync` will cause the interceptor to handle commands after its owning objects are destroyed, resulting in `MissingReferenceException`."
- **Code** — exhaustive assembly-wide reflection scan for the identifier:
  ```
  Methods containing 'Unfilter': NONE (0)
  Removal API: UnsubscribeAll() | RemoveFilter(ICommandInterceptor) | RemoveFilter(Func`2) | RemoveAllFilters()
  ```
  There is no `UnfilterAsync` on VitalRouter 2.0.5 — not on `Router`, not anywhere in the assembly. `Routing-commands.md` was written against VitalRouter **1.x** naming. This is a hard compile break, not a style drift: a reader following § 4 verbatim gets a red file. The correct 2.x call is `Router.Default.RemoveFilter(filter)` / `RemoveAllFilters()` / `UnsubscribeAll()`.
- **The trailing `.Forget()`** on that line is a UniTask extension; it compounds the error by implying an awaitable that does not exist either.

### D5 — `IRequiresAuthCommand` is a phantom marker interface that never existed in this repo.

- **Doc claim** — `:90`: `if (command is IRequiresAuthCommand && !_auth.IsLoggedIn)`.
- **Code** — no declaration and no implementor anywhere:
  ```
  $ grep -rn ": ICommand\|ICommand\b" --include=*.cs CharqUI/Assets com.ware.maqui | grep -v "/Packages/"
  (no output)
  ```
  `IRequiresAuthCommand` appears **zero** times in the repo. There are also **zero first-party `ICommand` implementors** — so the doc's § 1 (`PlayerLevelUpCommand`, `CloseInventoryCommand`), § 5 (`NavigateToDemoCommand`), and § 6 naming table describe command types that nothing in this repo defines or consumes.
- **Notable contrast:** `AddFilter` and `ICommandInterceptor` *did* have real, working first-party users before the delete — `com.ware.maqui/Samples~/GrandTour/GrandTourInterceptor.cs:19` (`class GrandTourInterceptor : ICommandInterceptor`) and `:28` (`InvokeAsync<T>(T command, PublishContext context, PublishContinuation<T> next) where T : ICommand`), registered at `GrandTourInitializer.cs:27`. That is why the doc's signatures look right: they were copied from working V1 sample code, and only the *surrounding framework* was invented. `B6a8f03` deleted the samples and the framework together.

### D6 — § 3's "Transform" bullet describes an API shape the interceptor contract does not permit.

- **Doc claim** — `:78`: "**Transform**: modify data and `await next(...)` with the same or a different command."
- **Code** — the reflected continuation signature is closed over `T`:
  ```
  VitalRouter.PublishContinuation`1
     ValueTask Invoke(T cmd, PublishContext ctx)
  ```
  and the interface is `ICommandInterceptor.InvokeAsync<T>(T command, PublishContext context, PublishContinuation<T> next)`. `next` accepts **only** `T`, so an interceptor for command `T` can never forward `U`. The doc's own § 3 example on `:96` (`await next(command, ctx)`) is correct; the prose bullet above it is the drifted one.
- **Honest caveat:** the doc's mermaid `:78` prose is the only place this is claimed, and the distinction is subtle enough that it may be loose writing rather than a claim about a version. I report it as drift because a reader would act on it and fail to compile. Lower confidence than D1–D5; flagged as such.

### D7 — `PublishContext.CancellationToken` is documented as a settable interceptor output; it is an init-only-style property with no public setter path.

- **Doc claim** — implied throughout § 3/§ 5: the interceptor receives `PublishContext ctx` and downstream code reads `ctx.CancellationToken` (`animation-transitions.md` does exactly this). Reading is fine.
- **Code** — reflected:
  ```
  -- VitalRouter.PublishContext
     prop CancellationToken CancellationToken
     prop IDictionary`2 Extensions
     Void set_CancellationToken(CancellationToken value)     <-- setter IS public
  ```
  Correction on my own first pass: the setter **is** public, so this is **not** drift. I am recording it as an explicitly-cleared item rather than silently dropping it, because it is the one claim in the doc I initially expected to fail and it did not. The extension point the doc gestures at is real.

### D8 — Non-drift, recorded so the list is not read as total: § 5's async pipeline and § 6's naming table are structurally sound.

- `ICommandInterceptor.InvokeAsync<T>(T, PublishContext, PublishContinuation<T>)` → `ValueTask` — **exists, matches `:86` exactly**.
- Interceptors being invoked in registration order (`:222`) — consistent with `AddFilter` appending; no counter-evidence found.
- § 6's naming conventions are a style guide with no code coupling; nothing to drift against.
- § 1's "struct implementing `ICommand`" guidance is consistent with `ICommand` being an empty marker interface (`-- VitalRouter.ICommand [interface]` with no members).

---

## Why this drifted, and what it means

The doc is a **pre-delete V1 artifact**. `CharqUI/docs/DOCS-PROTOCOL.md:28` states Rule 2 plainly: "Files in `reference/` explain HOW systems work. No TODOs, no milestone percentages, no progress tracking." A reference doc is a description of a *live* system. `routing-commands.md` describes a system that was deleted six weeks after the doc's last content commit (`4332762`, 2026-03-21) — so it has been describing deleted code since `b6a8f03` (2026-08-21), and was already partly fictional by D1/D2 before that.

Two structural findings worth more than the individual lines:

1. **The doc is still indexed as live.** `CharqUI/docs/README.md:19` lists it with no archive marker. `DOCS-PROTOCOL.md:20` reserves `archive/` for exactly this. Nothing in the protocol's four-step update point (`DOCS-PROTOCOL.md:35`: "update the relevant reference doc ONLY if the system description itself changed") was executed when the system was deleted — a delete is the limiting case of "the system description changed".

2. **The repo's own guardrails do not cover docs.** `com.ware.maqui/Tests.Standalone~/ApiSkeletonTests.cs` asserts the *source* contains no `Resources.Load`/`GameObject.Instantiate`/`[AddComponentMenu]` (BUG.1 lint), and `MaquiComponentLint` enforces it for generated components. There is **no equivalent check that any markdown file's named types still exist**. A ~20-line grep test over `docs/reference/*.md` for `[A-Z][A-Za-z]+(Class|Bridge|View|Service|Provider)` against the compiled assembly surface would have caught D1–D5 on the day of the delete. That is a cheap, non-blocking suggestion, not part of this audit's scope.

## Scope of this audit — what I did and did not check

**Checked:** every named type in the doc (`RouterBridge`, `CoreBootstrap`, `Router`, `Router.Default`, `ICommand`, `ICommandInterceptor`, `PublishContext`, `PublishContinuation<T>`, `AddFilter`, `UnfilterAsync`, `IRequiresAuthCommand`, `ICommandSubscriber`) against the shipped `VitalRouter.dll` **and** against first-party source.

**Not checked, and why:**
- `animation-transitions.md` and `shared-state.md` were spot-read and are **at least as drifted** (`AnimationBridge`, `Maqui.Core.Bridge`, `DisplayOptions`, `UIFramework.Navigate`, `MaquiNavigator`, `ReactiveBaseView<T>`, `MaquiServices.Get<IUIService>()`, `UILayer.Overlay`, all deleted). They are **out of scope** for this report — the prompt asks for one doc, and folding them in would dilute the evidence per claim.
- Runtime behaviour of the VitalRouter 2.x replacement calls. I confirmed they *exist* by reflection; I did not execute them, so I make no claim about their semantics.
- Whether `VitalRouter` *should* remain a dependency. `com.ware.maqui/package.json:12-15` still declares `com.cysharp.r3`, `com.cysharp.unitask`, `com.unity.ugui`, `com.unity.inputsystem`, while `com.ware.maqui/Runtime/Maqui.asmdef` declares `"references": []` and `"precompiledReferences": []`, and **zero first-party `.cs` files reference any of them**. `b6a8f03`'s own commit message flags this as deliberately deferred: *"Dependency pruning (com.cysharp.r3, com.unity.ugui) is deliberately NOT done here per OQ-1: it is a separately reversible edit whose blast radius was never measured."* I am reporting the observation, not recommending the prune — it is a separate decision with its own measurement, and I have not made it.

**Every command in this report was run in this session; the outputs above are pasted verbatim.** The one place I corrected myself mid-audit (D7, which I expected to be drift and is not) is left in the list rather than deleted, so the false-positive rate is visible.
