# MaqUI — Claude Code Instructions

**Platform:** Unity 2022.3 LTS+ · **Package:** `com.ware.maqui` v0.1.0
**Repo:** `D:\ware\MaqUI` — the package plus `CharqUI`, a demo Unity project that consumes it by
local path (`"com.ware.maqui": "file:../../com.ware.maqui"`).

> **Rewritten 2026-08-21 (MAQUI-V1-SUNSET).** Everything this file used to describe — `MaquiServices`,
> `IUIService`, `ViewModel`/`ReactiveProperty`, `ReactiveBaseView<T>`, `MaquiWindowManager`, the
> four-layer Canvas hierarchy, the modal freeze system, `ThemeData` — **is deleted code.** Maqui is now
> the immediate-mode framework formerly called "Maqui V2", and the `V2` suffix is gone from the
> namespace, the asmdef and the folder layout. If a note, comment or doc mentions `Maqui.V2` or
> `Runtime/V2/`, it predates that change.

## What Maqui is

An **immediate-mode UI framework on a retained reconciler.** You write a `void Build(Gui gui)` that
runs every frame; `Gui` records the calls into a `FrameBuffer` of `FrameOp`s; the `Reconciler` diffs
that against the live `VisualElement` tree and applies only the delta, reusing pooled elements.

The build function never touches Unity. That is the design's whole point: the interesting half —
layout maths, reconciliation, scroll offsets, text ramping, component state machines — is plain C#
and runs headless.

## Layout

```
com.ware.maqui/
├── Runtime/                 26 .cs — Gui, FrameBuffer, FrameOp, Node, Reconciler, ReconcileKey,
│   │                        Size, Align, TextScale, HidePriority, FlowLayout, InteractionState,
│   │                        PointerEvent(Queue), TextInputStore, FloatInputStore, AnimationStore,
│   │                        AnimationFloat, TooltipOverlay, IBackend, UIToolkitBackend, SdfBackend,
│   │                        UIToolkitInteractionAdapter, GuiDriver, ResourcesImageLoader
│   ├── Components/          19 .cs — MaquiComponents.* extension methods + MaquiTheme
│   ├── Shaders/
│   └── Maqui.asmdef         name: "Maqui", rootNamespace: "Maqui"
├── Tools/                   MaquiComponentLint
├── Tests/
│   ├── Runtime/             NUnit, in-Unity  (Maqui.Tests.Runtime)
│   └── Editor/              NUnit, Editor    (Maqui.Tests.Editor) + LlmLoopFixtures
├── Tests.Standalone~/       xUnit, headless  (Maqui.Tests.csproj) — 286 tests, the primary suite
└── Samples~/{HelloWorld,DraggableHandle}
```

**One runtime assembly**, `Maqui`, namespace `Maqui` (components in `Maqui.Components`).

## Critical rules

1. **Keys, not positions — `EnterDataScope` is mandatory for repeated or conditional content.**
   The reconciler matches by position within a scope. Render a list without
   `using (gui.EnterDataScope(uniqueKey))` per item and a container can be handed a pooled node that
   was a `Button` last frame, inheriting its inline height and background. This has bitten real
   windows at least three times (`MaquiComponents.Tabs` carries the note; ORO's Mod Browser rows
   collapsed to 32px with a button tint before per-row scopes were added). Same applies to any
   `if (x) A() else B()` where A and B occupy the same slot.

2. **Never author against Unity inside a build function.** `Resources.Load`, `GameObject.Instantiate`
   and `[AddComponentMenu]` are forbidden in `Runtime/` and enforced by
   `ApiSkeletonTests.RuntimeSources_DoNotReference_PrefabSpawningApis`. `ResourcesImageLoader.cs` is
   the single allowlisted exception (it *is* the adapter).

3. **Put new logic in the pure layer, then test it headless.** If a change can be expressed without
   `UnityEngine`, it belongs in a plain class with xUnit coverage in `Tests.Standalone~`. The
   Unity-facing files (`UIToolkitBackend`, `GuiDriver`, `SdfBackend`, `UIToolkitInteractionAdapter`,
   `ResourcesImageLoader`, `TooltipOverlay`) are excluded from that csproj by explicit
   `<Compile Remove>` — adding a new Unity-dependent file means adding it there too.

4. **`Tests.Standalone~/Maqui.Tests.csproj` globs source by path.** It pulls `..\Runtime\*.cs`,
   `..\Runtime\Components\*.cs` and `..\Tools\*.cs`. Moving or adding a source folder silently
   changes what is under test — check the globs.

5. **Baseline is 281/286.** Five `ToggleTests` failures are long-standing and unrelated; treat any
   sixth failure as yours. Run `dotnet test com.ware.maqui/Tests.Standalone~/Maqui.Tests.csproj`.

6. **`InternalsVisibleTo` lives in `Runtime/AssemblyInfo.cs`** and names `Maqui.Tests`,
   `Maqui.Tests.Editor`, `Maqui.Tests.Runtime`. Renaming a test assembly means editing it.

## Consumers

**ORO** (`G:\ro\RagnarokRebuildTcp`) is the real consumer and references the package by unpinned
`file:` path — it always builds against this repo's working tree, so **there is no version boundary
to stage a breaking change behind.** A rename here red-lines ORO's client until ORO is updated in the
same session. `CharqUI` consumes it the same way.

ORO builds its own window chrome (`RoWindow`, `RoWindowHost`) on top of Maqui; that code lives in ORO,
not here. Do not add game-specific chrome to this package.

## Commits

Group by topic, explicit pathspec, and end with the `Co-Authored-By` trailer. Multi-line messages via
`git commit -F <file>` from bash, or a PowerShell here-string — never `-m @'...'` in bash.
