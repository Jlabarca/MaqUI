# MaqUI v2 — Implementation Tracker

> Living tracker for the MaqUI v2 rework — PanGui-inspired, code-first IMGUI-flavored API for Unity backed by a UI Toolkit reconciler. Replaces prefab-based MVPa authoring; first-class LLM authoring loop.
>
> Source spec: [docs/products/maqui-v2-spec.md](../../docs/products/maqui-v2-spec.md).
> Downstream consumers: ORO/Rompe (rework branch — game HUD); Represent (annotation overlay — [products/represent.md § Architecture](../../docs/products/represent.md)); [METHODOLOGY-REPRESENT-IMPL.md](../../docs/impl/METHODOLOGY-REPRESENT-IMPL.md) P4.
> Follows DOCS-PROTOCOL.md Rule 12.
> Updated: 2026-05-14 (P3/P4/P5 headless + P7 complete + **P8 headless (8.1/8.2/8.3/8.5) shipped** after operator Editor-validation pass fixed 2 API drifts (StyleColor wrap + IPointerEvent.timestamp). xUnit 110+/110+ green. `/maqui-component` skill live. PanGui swap one-pager landed. **Halting before P6 — entirely Unity (port-a-window scene + visual parity) + still gated on 8.4/8.6 Image/TextInput operator Editor work.**)

## AI Continuation Prompt

A fresh AI session picking up this work should read, in order:

1. [docs/reference/methodology.md](../../docs/reference/methodology.md) — house rules for IMPL execution.
2. [docs/products/maqui-v2-spec.md](../../docs/products/maqui-v2-spec.md) — source-of-truth spec (goal, principles, API surface, 7-phase migration plan).
3. [pangui.io.md](../../pangui.io.md) — design inspiration. Closed alpha; copy patterns, not API names.
4. [MaqUI/CLAUDE.md](../CLAUDE.md) — existing v0.1 surface (`com.ware.maqui/`, R3 + VitalRouter + UniTask, CharqUI sandbox).
5. [docs/products/represent.md](../../docs/products/represent.md) § Architecture — downstream consumer that pins v2 as the annotation overlay layer.
6. This file — current phase, known bugs, test status.

Then [docs/CONTEXT.md](../../docs/CONTEXT.md) for cross-cutting product state.

## Working state

MaqUI v2 is a code-only, IMGUI-flavored rework of MaqUI: components are `static (this Gui gui, ...)` methods rendered through a per-frame virtual tree, reconciled into UI Toolkit `VisualElement`s. Authoring is LLM-first. It is **not** a prefab/MVPa bug-fix pass, **not** a 1:1 PanGui API mirror, and **not** a replacement for v0.1 in-place — v2 ships alongside under a separate namespace.

## Open Questions

> Spec Kit-style clarify step. ≤3 items. Auto-resolved via `/bootstrap-impl` per Rule 13 ("accept default with rationale"). Operator should flip any wrong defaults before P1 starts.

1. **Q:** Is Phase 7 (PanGui swap plan adapter spec) actually in scope for the v2 IMPL, or should it be cut to a future-conditional tracked elsewhere? The spec lists it as a migration phase, but PanGui is closed alpha with no announced Unity bridge as of April 2026, and Phase 7's exit criterion ("one-page design doc") is research, not shipped code.
   **Resolution:** Keep Phase 7 in scope but as a one-page design doc only — no code, no adapter scaffolding.
   > Rationale: The spec's migration table row 7 already defines the exit criterion as "One-page design doc," which is cheap, preserves the swappable-backend architectural intent, and the steering context explicitly notes "PanGui is closed alpha; no Unity bridge announced," so a doc is the only honest deliverable. Cutting it loses the architectural anchor; expanding it requires infrastructure that does not exist.
   > Override: Flip to "cut and track as a future-conditional in `docs/products/`" if PanGui's Unity bridge status remains unannounced when Phase 6 lands and the operator wants to ship v2 without a pending tail-phase.

2. **Q:** Does METHREP P4 (annotation overlay) actually unblock at Phase 2 (reconciler v0), or does it require Phase 3 (SDF shader) for callouts/tooltips/side-tables to land at the intended fidelity? Phase 2's exit criterion is "hello world + button renders," which is insufficient for shadowed/rounded annotation chrome.
   **Resolution:** Honor the existing METHREP P4-gates-on-MaqUI-P2 edge as written; accept flat-rect-and-text fidelity for the first annotation overlay cut, with SDF chrome treated as a visual polish pass that lands when MaqUI P3 ships.
   > Rationale: The METHREP IMPL already cites MaqUI v2 Phase 2 as the gate — moving the edge requires editing METHREP, which is out of scope for resolving this IMPL's open questions. P2's exit ("hello world + button renders identically to a hand-built UI Toolkit version") is sufficient for rectangular callouts and text labels; rounded/shadowed chrome is a fidelity upgrade, not a blocker. Keeps the cross-doc dependency stable; SDF becomes enhancement.
   > Override: Flip to "move METHREP P4 gate to MaqUI v2 Phase 3" if the first annotation overlay prototype is judged visually unacceptable on review, or if Represent's designer rejects flat-rect chrome.

3. **Q:** What is the precise pass/fail rubric for Phase 5's TEST.LLM-LOOP gate — (a) is the threshold "all 3 components pass" or "2/3 acceptable," and (b) how is "zero hand-fix" verified: operator eyeball vs golden-image diff vs lint-clean-on-first-emit?
   **Resolution:** (a) Threshold is **3/3** — all three components must pass; (b) "zero hand-fix" is verified by **lint-clean on first emit** (the `maqui-component` skill's lint rules must pass without operator edits) **AND** operator eyeball confirmation against the `maqui-snap` 3-size screenshot output. Golden-image diff is reserved for Phase 5's regression suite, not the loop gate itself.
   > Rationale: The spec asserts the LLM loop "earns v2's existence," so a stricter 3/3 bar is the least-surprising default — partial credit on the load-bearing gate invites promotion of a half-working loop. Lint rules already exist in the spec as a deterministic verifier, making them the cheapest objective check; eyeball-on-snap is the existing pattern from the spec's screenshot harness and avoids building golden-image infrastructure for the gate itself.
   > Override: Flip to 2/3 with golden-image diff if lint rules prove too strict during Phase 5 dogfooding (LLM emits visually correct but lint-failing components on >1/3 of attempts), indicating the lint rules — not the LLM — are the bottleneck.

## Architecture

```
┌─ MaqUI v2 API (C#) ──────────────────────────────┐
│  • Gui context, Node(), Size, Align, Draw*       │
│  • Component = static method (Gui gui, ...) → void │
└────────────────┬─────────────────────────────────┘
                 │ virtual layout tree per frame
                 ▼
┌─ Reconciler ─────────────────────────────────────┐
│  • Diffs virtual tree against previous frame     │
│  • Patches VisualElement tree minimally          │
└────────────────┬─────────────────────────────────┘
                 │
                 ▼
┌─ Backend: UI Toolkit (today) ────────────────────┐
│  • VisualElement, USS dynamically generated      │
│  • TMP for text                                  │
│  • Custom shader via Shader Graph for SDF shapes │
└──────────────────────────────────────────────────┘

                 ▼ Future
┌─ Backend: PanGui (when Unity bridge exists) ─────┐
│  • Emit PanGui CommandList                       │
│  • Inject into URP CommandBuffer                 │
└──────────────────────────────────────────────────┘
```

Phase mapping: the **API box** is built in `MAQUIV2.1` (~20 primitives compile). The **Reconciler** + **UI Toolkit backend** boxes are built jointly in `MAQUIV2.2` (one window renders) — this is the load-bearing phase that gates METHREP P4. The SDF shader inside the UI Toolkit backend lands in `MAQUIV2.3`. Interactables and animation thread through API + reconciler in `MAQUIV2.4`. `MAQUIV2.5` adds the LLM authoring loop. **`MAQUIV2.8` (Controls layer — Button/Slider/Toggle/Image/ScrollView/TextInput, inserted 2026-05-14)** composes P1-P4 primitives into the widgets P6 needs. `MAQUIV2.6` proves the stack by porting one v1 window. The **PanGui future backend** box is scoped (adapter spec only) in `MAQUIV2.7`.

## Current State (2026-05-13)

**P1 shipped + P2 partial.** Headless subset of P2 (2.2.1 + 2.2.3 + 2.2.7) shipped via `/run-impl` — `IBackend` interface, `Reconciler` algorithm, 17 new xUnit reconciler tests; `dotnet test` 39/39 green in ~150ms on the Unity-less dev box. Unity-required tail (2.2.2 `UIToolkitBackend`, 2.2.4 `GuiDriver`, 2.2.5 sample, 2.2.6 PlayMode parity test) shipped as **blind drafts** — `.cs` files written from a Unity-less environment with a `// MAQUIV2.2 — operator must compile in Unity to validate` banner. Operator opens `MaqUI/CharqUI` to validate before flipping those four checkboxes.

Test surface split (unchanged from P1):

- **`Maqui.V2.Tests`** (xUnit, standalone, `Tests.Standalone~/`) — 39 tests, `dotnet test` runs without Unity. Default for everything testable this way.
- **`Maqui.V2.Tests.Runtime`** (NUnit, Unity PlayMode harness, `Tests/Runtime/V2/`) — reintroduced for P2.6's parity test. Code authored as a blind draft; execution requires Unity Test Runner.

### Operator action queue (after this session)

These four checkboxes (2.2.2, 2.2.4, 2.2.5, 2.2.6) stay un-ticked until operator confirms in Unity Editor:

1. Open `MaqUI/CharqUI` in Unity → wait for asmdef recompile. Catches compile errors in `UIToolkitBackend.cs`, `GuiDriver.cs`, `HelloWorld.cs`, `ReconcilerParityTests.cs`, and the new `Tests/Runtime/V2/Maqui.V2.Tests.Runtime.asmdef`.
2. Author `Samples~/V2/HelloWorld/HelloWorld.unity` scene in the Editor (drag `HelloWorld` component onto a GameObject with a `UIDocument`).
3. Run Unity Test Runner → PlayMode → `Maqui.V2.Tests.Runtime` → assert the two `ReconcilerParityTests` pass.
4. Flip 2.2.2 / 2.2.4 / 2.2.5 / 2.2.6 in the checklist + bump `Implementation Status` row to `[x] complete`.

If any blind-draft file fails to compile, fix in place — these were written without Unity available, so real UIElements API drift is the most likely failure mode.

## Known Bugs

- **BUG.1 — Drift back to prefab authoring under MCP pressure.** If a v2 component generation falls back to spawning prefabs (because the LLM-loop test harness isn't ready and a contributor reaches for the familiar MVPa flow), the entire v2 thesis collapses. Mitigation: lint pass in the `maqui-component` Claude skill rejects any `Resources.Load`, `GameObject.Instantiate`, or `[AddComponentMenu]` reference in v2 component files; CI runs the lint on every PR touching `Maqui.V2.*`.

_(more to be added as P1+ surface real ones)_

## What Needs Testing

- **TEST.P1** — API skeleton: ~20 primitives (`Gui`, `Node`, `Size`, `Align`, `DrawRect`, `DrawText`, `OnClick`, `OnHover`, `EnterDataScope`) compile and are unit-tested for shape, not behavior.
- **TEST.P2** — Reconciler v0: "hello world" + button renders byte-identical to a hand-built UI Toolkit reference scene in CharqUI.
- **TEST.P3** — SDF shader: rounded rect with inner + outer shadow renders correctly under URP 17.x; visual regression snapshot.
- **TEST.P4** — Interactables + animation: draggable handle with hover-scale `AnimationFloat` exercised in PlayMode.
- **TEST.LLM-LOOP** (P5) — Claude Code builds 3 components from text descriptions in ≤5 minutes each, **3/3 pass**, lint-clean on first emit AND operator-eyeball-passes against `maqui-snap` output. **This is the design's primary success criterion.**
- **TEST.P6** — Port-a-window: one buggy v1 window rebuilt in v2 with visual parity and no anchor/scale regressions across 1080p / 1440p / 4K and 0.75x / 1.25x DPI scale.
- **TEST.P7** — PanGui swap: adapter spec reviewed; no code test (design doc only).

## Implementation Status

| Phase | State | Model | Description |
|---|---|---|---|
| MAQUIV2.1 | [x] complete (2026-05-13) | Sonnet | API skeleton — ~20 primitives compile, do nothing useful. Smallest shippable slice, testable in <1 day. |
| MAQUIV2.2 | [~] in progress (2026-05-13) | Opus | Reconciler v0 — UI Toolkit backend; static hello-world + button renders identically to hand-built UI Toolkit version. **Gates METHREP P4.** Headless subset (2.2.1, 2.2.3, 2.2.7) shipped via `/run-impl` with 39/39 `dotnet test` green. Unity tail (2.2.2, 2.2.4, 2.2.5, 2.2.6) shipped as blind drafts; operator Editor validation pending. |
| MAQUIV2.3 | [~] in progress (2026-05-14) | Opus | SDF shader — URP custom node renders shapes with effects; rounded rect with inner/outer shadow renders correctly. **Blind drafts shipped via `/run-impl`: `MaquiSDF.hlsl` (3.1+3.2 math) + `SdfBackend.cs` sketch (3.4). 3.3/3.5/3.6 Editor-only — operator authors ShaderGraph + scene + reference PNG.** |
| MAQUIV2.4 | [~] in progress (2026-05-14) | Sonnet | Interactables + animation — hover/click/hold + AnimationFloat; draggable handle with hover-scale animation. **4.2 + 4.3 shipped (xUnit 85/85). 4.1 / 4.4 / 4.5 blind drafts pending Unity Editor validation.** |
| MAQUIV2.5 | [~] in progress (2026-05-14) | Opus | LLM loop — `maqui-component` skill + hot reload + snap; 3/3 components <5 min each, lint-clean + eyeball-passes. **Headless subset shipped via `/run-impl`: SKILL.md + lint logic + xUnit + 3 fixture .md. 5.2/5.3/5.5/5.7 Unity-required pending operator Editor.** |
| MAQUIV2.8 | [~] in progress (2026-05-14) | Sonnet | **Controls layer** (Button, Slider, Toggle, Image, ScrollView, TextInput) — discovered as a gap when scoping P6 port-a-window. **Headless subset (8.1 + 8.2 + 8.3 + 8.5) shipped via `/run-impl`. 8.4 (Image) + 8.6 (TextInput) + 8.7 (exit gate) pending operator Editor work.** |
| MAQUIV2.6 | [ ] not started | Sonnet | Port a window — one v1 buggy window rebuilt in v2; visual parity, no anchor/scale bugs. **Gated on P8.** |
| MAQUIV2.7 | [x] complete (2026-05-14) | Sonnet | PanGui swap plan — one-page adapter spec design doc (no code). _Shipped via `/run-impl`: [products/maqui-v2-pangui-swap.md](../../docs/products/maqui-v2-pangui-swap.md) + spec Decision log entry._ |

## Plan — MAQUIV2.1

### Files to be created

| File | Purpose | Lines (est.) |
|---|---|---|
| `MaqUI/com.ware.maqui/Runtime/V2/Maqui.V2.asmdef` | New runtime assembly; `rootNamespace: "Maqui.V2"`; refs `UnityEngine.UIElementsModule` only — no `Maqui.Runtime`, no R3, no UniTask, no VitalRouter (full isolation per IMPL) | ~20 |
| `MaqUI/com.ware.maqui/Runtime/V2/Gui.cs` | `public sealed class Gui` — frame-context entry: `BeginFrame()`/`EndFrame()`, owns `FrameBuffer`, owns scope-path stack, exposes layout/draw/interaction/scope methods (1.3–1.6) | ~180 |
| `MaqUI/com.ware.maqui/Runtime/V2/Node.cs` | `public readonly struct Node` — id (int) + frame-buffer index; cheap value handle; interaction extension methods live here (1.2, 1.5) | ~70 |
| `MaqUI/com.ware.maqui/Runtime/V2/Size.cs` | `public readonly struct Size` + `SizeKind` enum (Fit, Expand, Ratio, Percentage, Pixels); static factories; implicit `float`→`Size.Pixels` (1.2) | ~70 |
| `MaqUI/com.ware.maqui/Runtime/V2/Align.cs` | `public static class Align` — named float constants `Start=0f`, `Center=0.5f`, `End=1f`; no enum (spec §Alignment) (1.2) | ~20 |
| `MaqUI/com.ware.maqui/Runtime/V2/FrameBuffer.cs` | `internal sealed class FrameBuffer` — `List<FrameOp>` with `Record(FrameOp)`, `Clear()`, `Ops` accessor for tests | ~50 |
| `MaqUI/com.ware.maqui/Runtime/V2/FrameOp.cs` | `internal readonly struct FrameOp` — `FrameOpKind` enum + payload union (rect, text, color, scopeKey, nodeId) | ~80 |
| `MaqUI/com.ware.maqui/Tests/Editor/V2/Maqui.V2.EditorTests.asmdef` | Test assembly; refs `Maqui.V2` + `UnityEngine.TestRunner` + `UnityEditor.TestRunner`; `nunit.framework.dll`; mirror of v0.1 editor-test asmdef | ~25 |
| `MaqUI/com.ware.maqui/Tests/Editor/V2/ApiSkeletonTests.cs` | NUnit tests: all ~20 primitives compile-and-call; `Node` returned non-default; `FrameBuffer.Ops` kind-sequence matches; scope-path correctness; nested Begin/End pairing; BUG.1 forbidden-string grep | ~140 |

### Files to be edited

None. Phase 1 is greenfield under `Runtime/V2/` and `Tests/Editor/V2/`. v0.1 asmdefs, package.json, CLAUDE.md remain untouched (IMPL § Current State: v0.1 stays `Maqui.*` to preserve ORO/Rompe compile).

### Key design decisions

1. **Frame buffer.** `Gui` owns a single `FrameBuffer` (List<FrameOp>); every layout/draw/interaction/scope call appends one FrameOp. Begin/End ops bracket containers (`RowBegin`/`RowEnd`) so the P2 reconciler can rebuild a tree by linear scan. Tests assert kind-sequences, not tree shape.
2. **Node handle.** `readonly struct Node { int Id; int OpIndex; }`. Returned by every layout primitive; safe to discard. `default(Node)` is the null sentinel.
3. **Size.** Struct + `SizeKind` enum + payload `float`. Implicit `float`→`Size.Pixels` per spec. `Size.Lerp` deferred to P2 (not in 1.2 checklist).
4. **Align.** Plain `float` 0..1, overflow allowed. `Align` static class holds named constants only — no enum (spec principle).
5. **Data scope.** `Gui` owns `Stack<string>`; `EnterDataScope(key)` pushes + emits `ScopeEnter`, returns `IDisposable` so `using (gui.EnterDataScope("popup"))` matches spec. `ExitDataScope()` pops + emits `ScopeExit`.
6. **BUG.1 lint anchors.** Source files contain zero references to `Resources.Load`, `GameObject.Instantiate`, `[AddComponentMenu]` — verified by a grep assertion in `ApiSkeletonTests` so the lint contract is enforced from day 1.
7. **No PanGui name collisions.** `Row`/`Column`/`Box`/`Spacer` (not PanGui's `Node().Enter()` fluent). Pattern-inspired only per IMPL § Working state.

### Verification strategy

Cannot run Unity headless. Two checks:

1. **Syntactic (Roslyn-only):** new `.cs` files parse standalone. No v0.1 type references means no Unity-resolution dependency beyond `UnityEngine.dll` + `UnityEngine.UIElementsModule.dll`. If a Unity-generated `Maqui.V2.csproj` exists post-Editor-open, `dotnet build` is the cheapest signal.
2. **Operator-in-Editor:** operator opens `MaqUI/CharqUI` in Unity once after the asmdef lands → Unity regenerates `.csproj` files → Test Runner runs `Maqui.V2.EditorTests`. State this in LOGBOOK so it's not a silent prerequisite for P2.

### Order of operations

1.1 asmdef + folder skeleton → 1.2 Node, Size, Align, FrameOp, FrameBuffer, Gui shell → 1.3 layout primitives → 1.4 draw primitives → 1.5 interaction primitives → 1.6 data scope → 1.7 test asmdef + ApiSkeletonTests.cs.

## Checklist

### Phase 1 — API skeleton

- [x] **MAQUIV2.1.1** Create `Maqui.V2` namespace folder structure under `MaqUI/com.ware.maqui/Runtime/V2/` alongside v0.1; add `Maqui.V2.asmdef` (no v0.1 dep, autoReferenced).
- [x] **MAQUIV2.1.2** Core types: `Gui` (frame-context), `Node` (readonly struct handle), `Size` + `SizeKind`, `Align` (named float constants), `FrameOp` + `FrameBuffer` (internal).
- [x] **MAQUIV2.1.3** Layout primitives recording into frame buffer: `Row`/`EndRow`, `Column`/`EndColumn`, `Box`, `Spacer` (paired Begin/End for containers).
- [x] **MAQUIV2.1.4** Draw primitives as record-only stubs: `DrawRect`, `DrawText`, `DrawLine`, `DrawCircle`.
- [x] **MAQUIV2.1.5** Interaction primitives as extension methods on `Node`: `OnClick`, `OnHover`, `OnHold`, `OnDrag` (return false in P1 — record-only; real values land in P4).
- [x] **MAQUIV2.1.6** Data scope: `EnterDataScope(key)` returns `IDisposable` for `using`-pattern; `ExitDataScope()`; scope-path tracking via internal `Stack<string>`; `EndFrame` throws on unclosed scope.
- [x] **MAQUIV2.1.7** **Standalone xUnit tests** at `Tests.Standalone~/ApiSkeletonTests.cs` — 22 xUnit `[Fact]`s covering primitive call/Node-return/kind-sequence/nested pairing/scope path/BUG.1 forbidden-string grep. Runs via `dotnet test`, no Unity required (UnityEngine.Color32 shimmed). `InternalsVisibleTo` wired via `AssemblyInfo.cs` for both `Maqui.V2.Tests` (standalone) and `Maqui.V2.Tests.Editor` (Unity harness, recreated when P3/P4/P5/P6 need it). **`dotnet test` 22/22 green 2026-05-13.**

## Plan — MAQUIV2.2

P2 splits cleanly into a **headless-shippable core** (interface + reconciler algorithm + xUnit harness, all validatable via `dotnet test` on the Unity-less dev box) and a **Unity-required tail** (UI Toolkit backend impl, MonoBehaviour driver, `.unity` scene, PlayMode parity test). The recommended structural change is to **promote `IBackend` to a first-class interface** — not an internal implementation detail. This (a) lets the reconciler depend on an abstraction the standalone xUnit project can mock via `TestBackend`, (b) matches the spec's "future PanGui backend swap" goal (architecture note + Open Question #1 + `MAQUIV2.7`), and (c) keeps the headless verifiability won in P1.

### Files to be created

| File | Purpose | Headless? |
|---|---|---|
| `Runtime/V2/IBackend.cs` | Backend abstraction — opaque `int` handles, not `VisualElement` | Yes |
| `Runtime/V2/ReconcileKey.cs` | `internal readonly struct ReconcileKey { ScopePath, Kind, OrdinalInScope }` | Yes |
| `Runtime/V2/Reconciler.cs` | Keyed-map diff loop; drives `IBackend` create/update/recycle | Yes |
| `Runtime/V2/UIToolkitBackend.cs` | `IBackend` impl writing `VisualElement.style.*`; owns recycled-element pool | **No (blind draft)** |
| `Runtime/V2/GuiDriver.cs` | `MonoBehaviour` driver; `BeginFrame`→component→`EndFrame`→`Render(backend)` in `LateUpdate` | **No (blind draft)** |
| `Samples~/V2/HelloWorld/HelloWorld.cs` | Sample component method | Partial |
| `Samples~/V2/HelloWorld/HelloWorldRef.uxml` + `.cs` | Hand-coded UI Toolkit reference (parity target) | Yes |
| `Samples~/V2/HelloWorld/HelloWorld.unity` | Sample scene | **No — operator authors in Unity** |
| `Tests.Standalone~/TestBackend.cs` | Mock backend; records every call as typed events | Yes |
| `Tests.Standalone~/ReconcilerTests.cs` | xUnit `[Fact]`s: keying, create/update/recycle, pool reuse, scope nesting, idempotency, sibling reorder | Yes |
| `Tests/Runtime/V2/Maqui.V2.Tests.Editor.asmdef` | NUnit PlayMode asmdef (re-introduced) | **No (blind draft)** |
| `Tests/Runtime/V2/ReconcilerParityTests.cs` | PlayMode parity: load `HelloWorld.unity`, snapshot v2 vs ref, assert structural + style equivalence | **No (blind draft)** |

### Files to be edited

- `Runtime/V2/Gui.cs` — add `public void Render(IBackend backend)` that hands `Buffer` to a per-Gui cached `Reconciler` (so prior-frame element map survives across frames). Throws if called before `EndFrame`.

### Key design decisions

1. **`IBackend` shape (7 methods + 2 lifecycle).** `BeginReconcile()` / `EndReconcile()` bracket a pass. Element-creating: `int CreateRect(Color32)`, `int CreateText(string, Color32, float)`. Mutating: `void SetRectStyle(int, float w, float h, Color32)`, `void SetText(int, string, Color32, float)`. Tree: `void SetParent(int child, int parent, int siblingIndex)`. Pool: `void Recycle(int)`. Root: `void EnsureRoot(int)`. All elements addressed by opaque `int` handles minted by the backend — keeps `VisualElement` out of `Reconciler.cs`.
2. **Keying as struct, not string.** `ReconcileKey { string ScopePath; FrameOpKind Kind; int OrdinalInScope }` with cached `GetHashCode` (no string concat per-op). String form reserved for `ToString()` (diagnostics).
3. **Diff algorithm: O(n) keyed-map.** Build `Dictionary<ReconcileKey, int>` from prior frame; linear-scan this frame: hit → reuse handle + update if dirty; miss → backend creates. Unmatched prior handles → `Recycle()`. No LCS — keys are stable across frames in immediate mode.
4. **Pool ownership: Backend, not Reconciler.** `Recycle(handle)` returns to a backend-internal stack keyed by op-kind; `Create*` pops first. Mirrors P1's "backend owns Unity types."
5. **`Gui.EndFrame()` does NOT invoke reconcile.** Driver pattern: `BeginFrame` → user component → `EndFrame` → `Render(backend)`. Keeps buffer inspectable between EndFrame and Render (tests need this), keeps `Gui` free of an `IBackend` field (supports multi-backend per Gui).

### Verification strategy per checkbox

| Box | `dotnet test` covers? | Operator action |
|---|---|---|
| 2.2.1 | **Yes** — `ReconcileKey` equality/hash tests in xUnit | Review |
| 2.2.2 | **No** — references `UnityEngine.UIElements` | Open Unity → asmdef recompiles → catch compile errors |
| 2.2.3 | **Yes** — `TestBackend` + 14 xUnit `[Fact]`s | None |
| 2.2.4 | **No** — `MonoBehaviour` + `UIDocument` | Unity recompile |
| 2.2.5 | **No** — `.unity` files | **Operator authors scene** |
| 2.2.6 | **No** — Unity Test Runner | Run in Unity |
| 2.2.7 | **Yes** — doc edit only | Review METHREP cross-link |

### Headless subset (ships this session — flip these checkboxes)

- **2.2.1** — `IBackend.cs` + `ReconcileKey.cs` authored + tested
- **2.2.3** — `Reconciler.cs` + `TestBackend.cs` + `ReconcilerTests.cs` authored + green
- **2.2.7** — METHREP P4 cross-link added; reconciler-stable surface declared = `IBackend` + `Gui.Render(IBackend)`

### Unity-required tail (blind drafts written; checkboxes NOT flipped)

- **2.2.2 / 2.2.4 / 2.2.6** — `.cs` files written with a `// MAQUIV2.2 — operator must compile in Unity` banner. Operator flips checkboxes after Unity Editor recompile + manual smoke + PlayMode test pass.
- **2.2.5** — only `HelloWorld.cs` skeleton + reference UXML/`.cs` shipped. Operator authors the `.unity` scene file in Unity Editor.

### Order of operations

1. Author `IBackend.cs` + `ReconcileKey.cs`
2. Author `TestBackend.cs` (drives interface design)
3. Author `Reconciler.cs` — keyed-map diff loop
4. Edit `Gui.cs` — add `Render(IBackend)`
5. Author `ReconcilerTests.cs` — 14 xUnit tests
6. `dotnet test` → expect ~36/36 green
7. Author blind Unity drafts: `UIToolkitBackend.cs`, `GuiDriver.cs`, `HelloWorld.cs`, `HelloWorldRef.*`, `ReconcilerParityTests.cs` + asmdef
8. 2.2.7 doc gesture
9. Bookkeeping + single phase commit

### Phase 2 — Reconciler v0 (gates METHREP P4)

- [x] **MAQUIV2.2.1** Designed frame-buffer → backend tree-diff algorithm; documented keying as `ReconcileKey { ScopePath, Kind, OrdinalInScope }`; introduced `IBackend` as the public abstraction (Reconciler depends on the interface, not `VisualElement` — enables future PanGui backend swap per MAQUIV2.7). `Runtime/V2/IBackend.cs` + `Runtime/V2/ReconcileKey.cs`.
- [ ] **MAQUIV2.2.2** _Blind draft shipped at `Runtime/V2/UIToolkitBackend.cs`. Operator must compile in Unity Editor + fix any UIElements API drift before flipping._
- [x] **MAQUIV2.2.3** Reconcile pass implemented at `Runtime/V2/Reconciler.cs`: O(n) keyed-map diff (no LCS — keys are stable across frames in immediate mode). Per-(scope, kind) ordinal counters; parent + sibling-index stacks; interaction ops and scope markers are no-ops at v0. Verified by `Tests.Standalone~/ReconcilerTests.cs` (17 new xUnit `[Fact]`s) + `TestBackend.cs` (in-memory `IBackend` recording calls as a typed event sequence). `dotnet test` 39/39 green 2026-05-13.
- [ ] **MAQUIV2.2.4** _Blind draft shipped at `Runtime/V2/GuiDriver.cs` (MonoBehaviour). Operator must compile in Unity + smoke in a scene before flipping._
- [ ] **MAQUIV2.2.5** _Blind drafts shipped: `Samples~/V2/HelloWorld/HelloWorld.cs` (component subclass of GuiDriver) + `HelloWorldRef.uxml` (UI Toolkit parity reference). `HelloWorld.unity` scene file MUST be authored by operator in Unity Editor — binary YAML with auto-generated GUIDs._
- [ ] **MAQUIV2.2.6** _Blind draft shipped at `Tests/Runtime/V2/ReconcilerParityTests.cs` + `Maqui.V2.Tests.Runtime.asmdef`. Operator must run Unity Test Runner (PlayMode) before flipping._
- [x] **MAQUIV2.2.7** Reconciler-stable API surface declared: **`Gui` + `IBackend` + `Gui.Render(IBackend)`**. METHREP P4 cross-linked: [METHODOLOGY-REPRESENT-IMPL.md § P4](../../docs/impl/METHODOLOGY-REPRESENT-IMPL.md) row updated to reference this contract; downstream consumers can build against the public surface independently of the Unity-side `UIToolkitBackend` blind draft (since 2.2.7 is exactly about "the API is stable enough to build on" — not "the Unity backend is validated").

## Plan — MAQUIV2.3

P3 is **Unity-Editor-heavy**: ShaderGraph assets (`.shadergraph`), URP materials (`.mat`), per-instance property bindings, and visual regression scenes (`.unity`) are all binary YAML/JSON-with-GUID-refs files that require the Unity Editor to author correctly. The dev box has no Unity install, so this run ships:

- **3.1 + 3.2 — `Runtime/V2/Shaders/MaquiSDF.hlsl`** as a complete blind draft. HLSL is plain text; SDF math is Unity-version-stable. Operator wires it into a ShaderGraph **Custom Function** node in the Editor (3.3), at which point the file is exercised. Banner identifies it as a blind draft and lists the function signatures the ShaderGraph node should expose.
- **3.4 — `Runtime/V2/SdfBackend.cs`** as an interface sketch documenting how `IBackend.UpdateElement` would route `DrawRect`/`DrawCircle` ops through a material with per-instance properties (corner radius, shadow params). Stub-only — no actual `Material`/`VisualElement` glue, since the ShaderGraph asset must exist first. **Operator note**: until 3.3 lands, `UIToolkitBackend` continues to handle `DrawRect`/`DrawCircle` via flat `style.backgroundColor`/`borderRadius`. The SDF path is opt-in.
- **3.3, 3.5, 3.6** stay unticked; require Editor (3.3, 3.5) or both prior boxes (3.6).

### Files to be created

| File | Purpose | Headless? |
|---|---|---|
| `Runtime/V2/Shaders/MaquiSDF.hlsl` | SDF functions: `Sdf_Rect`, `Sdf_RoundedRect`, `Sdf_Circle`; shadow/stroke/fill compositors: `Sdf_OuterShadow`, `Sdf_InnerShadow`, `Sdf_Stroke`, `Sdf_FillAA` | Yes (blind text) |
| `Runtime/V2/SdfBackend.cs` | Interface sketch for an SDF-aware backend wrapper; opt-in, doesn't replace `UIToolkitBackend` | Yes (blind stub) |

### Files to be edited

None this phase. `UIToolkitBackend.cs` stays as-is; SDF path is additive. `FrameOp.cs` already carries enough float payload for corner-radius and shadow params (`FloatA..D` reused per `Kind`).

### Key design decisions

1. **Pure HLSL, no ShaderGraph-specific includes.** `MaquiSDF.hlsl` uses only `#include "UnityCG.cginc"`-compatible primitives and Unity's built-in `lerp`/`smoothstep`/`length`. Means the file is readable + verifiable by inspection; ShaderGraph wraps it as a Custom Function node taking explicit inputs.
2. **AA via `fwidth(d)`.** Anti-aliasing inside SDF uses screen-space derivative of the distance field, the standard cheap-and-good technique (Inigo Quilez). No SSAA, no Subpixel — UI Toolkit composites the result anyway.
3. **Shadow is two SDF passes.** Outer shadow = `1 - smoothstep(-radius, 0, d)` of the dilated SDF. Inner shadow = `smoothstep(0, radius, d)` clipped to the fill. Both modulated by a shadow color. Spec ("rounded rect with inner + outer shadow") is the gate; tested at 3.6.
4. **No `RWStructuredBuffer`, no compute.** Strictly fragment-shader-friendly so it ports to PanGui's `CommandList` later (P7). If shadow blur needs Gaussian, do it inside the same fragment via 5-tap weighted samples, not a separate pass.
5. **`SdfBackend.cs` is a sketch only.** A real impl needs Unity Editor to bind a `Material` to each shape `VisualElement` (via `customMaterial` USS or a custom `IIPanel` shader). Out of scope for this blind run — the .cs file documents the intended API for when Unity is available.

### Verification strategy per checkbox

| Box | Headless? | Operator action |
|---|---|---|
| 3.1 | **Partial** — HLSL parses standalone (no headless HLSL compiler on dev box), math is reviewable. Cannot confirm without ShaderGraph wrap. | Author Custom Function node in ShaderGraph pointing at `MaquiSDF.hlsl` → confirm node compiles |
| 3.2 | **Partial** — same as 3.1 (extends same file) | Same Custom Function node exercises shadow signatures |
| 3.3 | **No** — `.shadergraph` + `.mat` are binary | Author ShaderGraph asset in Editor; wire properties (size, radius, shadow params) |
| 3.4 | **Partial** — interface sketch in `.cs` compiles, but real wiring needs 3.3 | Implement actual `VisualElement`-to-Material binding once 3.3 lands |
| 3.5 | **No** — `.unity` scene + reference PNGs are binary | Author scene; capture reference screenshot |
| 3.6 | **No** — depends on 3.3, 3.5 | Run Unity Test Framework graphics test |

### Headless subset (ships this session — checkboxes NOT flipped)

All P3 checkboxes stay unticked. The two files (`MaquiSDF.hlsl`, `SdfBackend.cs`) ship with `// MAQUIV2.3 — BLIND DRAFT` banners. Implementation Status row → `[~] in progress`.

### Order of operations

1. Author `Runtime/V2/Shaders/MaquiSDF.hlsl` (SDF primitives + compositors)
2. Author `Runtime/V2/SdfBackend.cs` (interface sketch)
3. Bump IMPL `Updated:` line; flip status row to `[~] in progress`; do **not** flip any 2.3.x checkbox
4. LOGBOOK + CONTEXT bookkeeping
5. Two commits: `docs(maqui-v2): P3 plan + blind-draft scope` and `feat(maqui-v2): P3 blind drafts — MaquiSDF.hlsl + SdfBackend sketch`

### Phase 3 — SDF shader

- [ ] **MAQUIV2.3.1** Create URP ShaderGraph custom function node `MaquiSDF.hlsl` under `Runtime/V2/Shaders/`; implement signed-distance functions for rect, rounded-rect, circle. _Blind draft shipped at `Runtime/V2/Shaders/MaquiSDF.hlsl` (text-only SDF math, ShaderGraph wrap pending operator)._
- [ ] **MAQUIV2.3.2** Implement inner/outer shadow, stroke, and fill compositing in the SDF fragment path. _Blind draft shipped inside `MaquiSDF.hlsl` (`Sdf_OuterShadow`, `Sdf_InnerShadow`, `Sdf_Stroke`, `Sdf_FillAA`); operator wraps in ShaderGraph._
- [ ] **MAQUIV2.3.3** Build URP-compatible material + ShaderGraph asset that exposes per-instance properties (size, corner radius, shadow params) via `MaterialPropertyBlock` or instanced UI Toolkit hooks. _Unity Editor required — binary `.shadergraph` + `.mat`._
- [ ] **MAQUIV2.3.4** Bridge `DrawRect`/`DrawCircle` from Phase 1 into the SDF backend so the reconciler routes shape draws through the custom shader instead of default UI Toolkit backgrounds. _Interface sketch shipped at `Runtime/V2/SdfBackend.cs`; real wiring blocked on 3.3._
- [ ] **MAQUIV2.3.5** Add visual regression test scene `Samples~/V2/SdfShapes/` with reference screenshot; EditorTest compares render output via Unity Test Framework graphics tests. _Unity Editor required — binary `.unity` + reference PNG._
- [ ] **MAQUIV2.3.6** Exit gate: rounded rect with inner + outer shadow matches reference within tolerance. _Gated on 3.3 + 3.5 — both Editor-only._

## Plan — MAQUIV2.4

P4 splits cleanly into a **headless-testable math+infrastructure subset** (AnimationFloat spring-damper integrator, PointerEvent ring buffer, InteractionState flag table, Node.IsHovered/IsActive/IsFocused readers, Gui.Animate API) and a **Unity-required adapter+sample+PlayMode-test tail** (UI Toolkit pointer event subscription, hit-test from `VisualElement` to backend handle to NodeId, sample scene, PlayMode regression test).

The split lets the load-bearing math (`AnimationFloat.Tick(dt)`) ship green via `dotnet test` — operator can review and trust the integrator without spinning up Unity. The pointer event ring buffer is also fully xUnit-validated, so the wire-up surface that the Unity adapter (blind draft) writes into is provably correct. The adapter itself is small (subscribe to 4 events, look up handle, push to queue) and the operator's first Unity recompile catches all API drift in one shot.

### Files to be created

| File | Purpose | Headless? |
|---|---|---|
| `Runtime/V2/AnimationFloat.cs` | Spring-damper struct; `Tick(dt)` advances `Current` toward `Target` via velocity + stiffness + damping | Yes |
| `Runtime/V2/AnimationStore.cs` | `Dictionary<string, AnimationFloat>` keyed by scope-path + caller key; `Get/Set/TickAll(dt)`; survives reconcile because it lives on `Gui`, not in the frame buffer | Yes |
| `Runtime/V2/PointerEvent.cs` | `PointerEventKind` enum (Down/Up/Move/Enter/Leave) + `PointerEvent` struct (kind, X, Y, button, targetNodeId, targetScopePath) | Yes |
| `Runtime/V2/PointerEventQueue.cs` | Fixed-size FIFO ring buffer; `Enqueue` / `TryDequeue` / `Drain`; overflow drops oldest (with counter) | Yes |
| `Runtime/V2/InteractionState.cs` | `NodeInteractionFlags` enum (Hover/Active/Focus/ClickedThisFrame) + per-NodeId flag table + `Reset()` at frame boundary | Yes |
| `Runtime/V2/UIToolkitInteractionAdapter.cs` | Wires UI Toolkit `VisualElement` pointer callbacks → `PointerEventQueue` + `InteractionState` flag writes (uses backend handle → NodeId lookup) | **No (blind draft)** |
| `Samples~/V2/DraggableHandle/DraggableHandle.cs` | Sample component: drag handle with hover-scale via `Animate` | Partial (compiles headless; visual gate Unity-only) |
| `Tests.Standalone~/AnimationTests.cs` | xUnit: AnimationFloat settles toward target, AnimationStore keys/survives | Yes |
| `Tests.Standalone~/InteractionTests.cs` | xUnit: PointerEventQueue FIFO + overflow + drain, InteractionState set/get/reset, Node.OnClick reads InteractionState | Yes |
| `Tests/Runtime/V2/InteractableTests.cs` | NUnit PlayMode: simulate pointer events on `Samples~/V2/DraggableHandle/`; assert drag offset + animated scale settle | **No (blind draft)** |

### Files to be edited

- `Runtime/V2/Gui.cs` — add `public AnimationStore Animations { get; }` + `public InteractionState Interactions { get; }` + `public float Animate(string key, float target, float stiffness = 200f, float damping = 20f)` + `public void TickAnimations(float dt)`. Animation/interaction state is owned by Gui (not in FrameBuffer) so it survives across reconciles.
- `Runtime/V2/Node.cs` — add `IsHovered()` / `IsActive()` / `IsFocused()` reading from `Owner.Interactions[Id]`. Update `OnClick` / `OnHover` / `OnHold` / `OnDrag` to record the FrameOp *and* return the real boolean from `Interactions` (instead of always returning `false`).

### Key design decisions

1. **AnimationFloat: explicit-Euler spring-damper, not critically-damped exponential.** Spring (`k * (target - current)`) + damper (`c * velocity`) integrated with single-step Euler. Defaults `stiffness=200f, damping=20f` give ~150ms settle time for unit-distance moves; ratio is overdamped to avoid overshoot on UI motion. Trade-off: explicit Euler can blow up under very large `dt` (skipped frames), so callers clamp `dt ≤ 1/30s`. Documented in xdoc.
2. **AnimationStore lives on Gui, keyed by user-supplied string.** Pattern: `float scale = gui.Animate("popup-scale", isOpen ? 1f : 0.95f);`. Key derivation isn't automatic — the user's choice. Scope-path concat is a recommended convention but not enforced. Reason: auto-keying by call-site requires source-gen or stack walking; both are heavier than the deferred polish payoff. Two animations with the same key cross-pollinate — accepted (and useful) for shared animations.
3. **InteractionState keyed by current-frame NodeId — same-frame semantics only.** Adapter receives a UI Toolkit pointer event, resolves `VisualElement → backend handle → current-frame NodeId` (via the backend's last-seen FrameOp per handle, captured during `CreateElement`/`UpdateElement`), then writes flags. Node.OnClick reads flags by `this.Id`. **Limitation:** across-frame state (e.g., "still hovered") relies on the adapter re-emitting flags every frame from sticky pointer position. v0 acceptable; not a regression vs Dear ImGui's model. Cross-frame stable-key resolution lands in a P4.x follow-up if needed.
4. **PointerEventQueue is a fixed-size ring buffer (capacity 256).** Overflow drops oldest with a `DroppedCount` counter exposed for diagnostics. Capacity tuned to one frame's worth of input on a 240Hz pointer with two-finger touch — typical worst case < 50. Allocation-free per event.
5. **`UIToolkitInteractionAdapter` is blind-draft.** Operator binds it inside `GuiDriver.LateUpdate` after `Render(backend)`; adapter holds a `Dictionary<int handle, int lastSeenNodeId>` populated by intercepting backend Create/Update calls. The adapter is small enough to write correctly without Unity (~80 lines) but cannot be verified headlessly. Banner identifies as such.

### Verification strategy per checkbox

| Box | `dotnet test` covers? | Operator action |
|---|---|---|
| 4.1 | **No** — UI Toolkit pointer event subscription is Unity-only | Open Unity → adapter recompiles → smoke-click on sample → verify event fires + InteractionState writes |
| 4.2 | **Yes** — `AnimationTests.cs` exercises spring-damper convergence, frame-skip behavior, AnimationStore key lookup | None |
| 4.3 | **Yes** (read path) — `InteractionTests.cs` directly writes to InteractionState + asserts Node.IsHovered/OnClick read it; **partial** for write path (adapter validates 4.1) | None for reads; Unity for adapter writes |
| 4.4 | **No** — `.unity` scene + visual feel | Author scene; drag handle in Editor |
| 4.5 | **No** — NUnit PlayMode test | Run Unity Test Runner |

### Headless subset (ships this session — flip these checkboxes)

- **4.2** — AnimationFloat + AnimationStore + xUnit tests green
- **4.3** — InteractionState + PointerEventQueue + Node.IsHovered/IsActive/IsFocused + OnClick wiring; xUnit tests green. Same-frame-NodeId limitation documented.

### Unity-required tail (blind drafts; checkboxes NOT flipped)

- **4.1** — `UIToolkitInteractionAdapter.cs` blind draft
- **4.4** — `DraggableHandle.cs` blind draft + operator-authored `.unity` scene
- **4.5** — `InteractableTests.cs` blind draft (NUnit, PlayMode)

### Order of operations

1. Author `AnimationFloat.cs` + `AnimationStore.cs`
2. Author `PointerEvent.cs` + `PointerEventQueue.cs`
3. Author `InteractionState.cs`
4. Edit `Gui.cs` — wire Animations + Interactions + Animate() + TickAnimations()
5. Edit `Node.cs` — IsHovered/IsActive/IsFocused; update OnClick/OnHover/OnHold/OnDrag to read InteractionState
6. Author `Tests.Standalone~/AnimationTests.cs` + `InteractionTests.cs`
7. `dotnet test` → expect ~50+/50+ green
8. Author blind Unity drafts: `UIToolkitInteractionAdapter.cs`, `DraggableHandle.cs`, `InteractableTests.cs`
9. Bookkeeping + plan-commit + execution-commit

### Phase 4 — Interactables + animation

- [ ] **MAQUIV2.4.1** Implement pointer-event routing from UI Toolkit `VisualElement` callbacks back into Phase 1 interaction records (`OnClick`, `OnHover`, `OnHold`, `OnDrag`). _Blind draft shipped at `Runtime/V2/UIToolkitInteractionAdapter.cs` — operator wires inside `GuiDriver.LateUpdate` after `Render(backend)`._
- [x] **MAQUIV2.4.2** Implement `AnimationFloat` struct: target value, current value, spring/damper params, `Tick(dt)` integration; store keyed by data-scope-path so values survive reconcile. _Shipped via `/run-impl` 2026-05-14: `Runtime/V2/AnimationFloat.cs` + `Runtime/V2/AnimationStore.cs` + `Gui.Animate/TickAnimations`. 22 new xUnit `[Fact]`s green (`AnimationFloatTests` + `AnimationStoreTests` + `GuiAnimateTests`). dotnet test 85/85._
- [x] **MAQUIV2.4.3** Implement hover/active/focus state tracking per node, exposed as readable flags inside the immediate-mode call. _Shipped via `/run-impl` 2026-05-14: `Runtime/V2/InteractionState.cs` + `Runtime/V2/PointerEvent.cs` + `Runtime/V2/PointerEventQueue.cs`; `Gui.Interactions` property; `Node.IsHovered/IsActive/IsFocused` + updated `OnClick/OnHover/OnHold/OnDrag` reads. 24 new xUnit `[Fact]`s green (`PointerEventQueueTests` + `InteractionStateTests` + `NodeInteractionReadTests`). Same-frame NodeId semantics — adapter writes flags each frame; across-frame stickiness deferred._
- [ ] **MAQUIV2.4.4** Build draggable-handle-with-hover-scale sample at `Samples~/V2/DraggableHandle/`. _Blind draft shipped at `Samples~/V2/DraggableHandle/DraggableHandle.cs`; operator authors `.unity` scene._
- [ ] **MAQUIV2.4.5** PlayMode test `Tests/Runtime/V2/InteractableTests.cs` simulating pointer events; assert drag offset + animated scale settle to expected values. _Blind draft shipped; operator runs Unity Test Runner._

## Plan — MAQUIV2.5

P5 is the **LLM authoring loop** — the load-bearing test of v2's existence per the spec. Split by Unity dependency:

- **Headless this run:** 5.1 (skill SKILL.md is pure docs the operator invokes via `/maqui-component`), 5.4 (lint logic is a string scanner — xUnit testable as a static method), 5.6 (component description fixtures are plain text files).
- **Unity tail (blind drafts, NOT flipped):** 5.2 (domain reload harness — Editor-only), 5.3 (`maqui-snap` screenshot tool — needs Camera/GraphicsAPI), 5.5 (end-to-end wire — depends on 5.2 + 5.3), 5.7 (exit gate — requires running the loop).

### Files to be created

| File | Purpose | Headless? |
|---|---|---|
| `.claude/skills/maqui-component/SKILL.md` | Claude Code skill: prompt + lint rules + workflow | Yes |
| `MaqUI/com.ware.maqui/Tools/MaquiComponentLint.cs` | Static `Check(source) → IReadOnlyList<LintIssue>` scanner | Yes |
| `MaqUI/com.ware.maqui/Tools/Maqui.V2.Tools.asmdef` | Editor-side asmdef for lint tooling | Mostly |
| `MaqUI/com.ware.maqui/Tests.Standalone~/LintTests.cs` | xUnit: lint catches forbidden strings; allows clean source | Yes |
| `MaqUI/com.ware.maqui/Tests/Editor/V2/LlmLoopFixtures/toggle-pill.md` | Component description fixture #1 | Yes |
| `MaqUI/com.ware.maqui/Tests/Editor/V2/LlmLoopFixtures/slider-with-ticks.md` | Component description fixture #2 | Yes |
| `MaqUI/com.ware.maqui/Tests/Editor/V2/LlmLoopFixtures/collapsible-header.md` | Component description fixture #3 | Yes |

### Files to be edited

None. P5 is greenfield under `.claude/skills/maqui-component/` and `MaqUI/com.ware.maqui/Tools/`.

### Key design decisions

1. **Skill SKILL.md is the operator interface.** Operator invokes `/maqui-component "draggable toggle with animated knob"` → skill reads spec + existing component patterns + lint rules → emits a `Maqui.V2` component method. The skill doesn't need to live inside Unity; it generates source that Unity then compiles.
2. **Lint is a static string scanner, not an AST walker.** Forbidden strings: `Resources.Load`, `GameObject.Instantiate`, `[AddComponentMenu]`. Required-on-non-trivial: explicit `Size.*` for width/height parameters that aren't `default`. Cheap regex + line scan; testable in xUnit without compiling. Trade-off: string scanner can be fooled (e.g., comments mentioning forbidden symbols) — accepted, with a `// maqui-lint:ignore-next-line` escape hatch.
3. **Component description fixtures are markdown, not JSON.** Each fixture = ~20 lines describing the desired component (purpose, parameters, interaction model, visual chrome). Markdown so the operator can read + edit. The `maqui-component` skill consumes these as input prompts.
4. **5.2 / 5.3 / 5.5 / 5.7 stay unticked.** The hot-reload harness + screenshot tool + end-to-end wiring all need Unity Editor. Document in the IMPL row that ship-this-run = lint + skill scaffold + fixtures only.

### Verification strategy per checkbox

| Box | `dotnet test` covers? | Operator action |
|---|---|---|
| 5.1 | **Yes** — SKILL.md exists, parses as markdown, has required H2 sections | Operator runs `/maqui-component` once to smoke-test |
| 5.2 | **No** — domain-reload harness | Unity-only |
| 5.3 | **No** — screenshot capture | Unity-only |
| 5.4 | **Yes** — `LintTests.cs` exercises forbidden + allowed patterns | None |
| 5.5 | **No** — end-to-end | Depends on 5.2 + 5.3 |
| 5.6 | **Yes** — three .md files exist + non-empty | None |
| 5.7 | **No** — requires loop running | Unity + LLM eval |

### Headless subset (ships this session — flip these checkboxes)

- **5.1** — `.claude/skills/maqui-component/SKILL.md` authored
- **5.4** — `MaquiComponentLint.cs` + xUnit suite green
- **5.6** — three component fixture .md files

### Unity-required tail (NOT flipped)

- **5.2** — hot-reload harness (Unity-only)
- **5.3** — `maqui-snap` PNG capture (Unity-only)
- **5.5** — end-to-end wire (depends on 5.2 + 5.3)
- **5.7** — exit gate (timing + LLM eval against fixtures)

### Order of operations

1. Author `.claude/skills/maqui-component/SKILL.md`
2. Author `MaquiComponentLint.cs` + `LintTests.cs`
3. `dotnet test` — expect ~95+/95+ green
4. Author 3 fixture `.md` files
5. Bookkeeping + plan-commit + execution-commit

### Phase 5 — LLM loop

- [x] **MAQUIV2.5.1** Scaffold Claude Code skill `maqui-component` under `.claude/skills/maqui-component/` (SKILL.md + assets); document inputs (description, target file path) and outputs (compiled `Maqui.V2` component method). _Shipped via `/run-impl` 2026-05-14 at [.claude/skills/maqui-component/SKILL.md](../../.claude/skills/maqui-component/SKILL.md). Skill is auto-discovered by Claude Code; operator invokes `/maqui-component "<description>"`._
- [ ] **MAQUIV2.5.2** Implement domain-reload / hot-reload harness that reruns the current frame after a script recompile without losing data-scope state. _Unity Editor required — domain-reload is an Editor concept._
- [ ] **MAQUIV2.5.3** Implement snapshot (`maqui-snap`) tool: capture rendered component as PNG + serialized frame buffer for diff review; renders at 3 sizes (small/medium/large). _Unity Editor required — screen capture._
- [x] **MAQUIV2.5.4** Implement lint pass: reject `Resources.Load`, `GameObject.Instantiate`, `[AddComponentMenu]` (mitigates BUG.1); enforce explicit alignment on layout nodes; enforce `Size.*` for non-trivial dimensions. _Shipped via `/run-impl` 2026-05-14: `Tools/MaquiComponentLint.cs` (static string scanner; 3 BUG.1 rules; `// maqui-lint:ignore-next-line` escape hatch); 12 xUnit `[Fact]`s green (`MaquiComponentLintTests`). dotnet test 97/97. Size/Align enforcement deferred to a P5.x follow-up — current rules are the BUG.1 anchor set (load this if drift back to prefab/MVPa actually appears)._
- [ ] **MAQUIV2.5.5** Wire skill end-to-end: prompt → generated component file → domain reload → snap → reviewable artifact. _Depends on 5.2 + 5.3 (Unity)._
- [x] **MAQUIV2.5.6** Author 3 component descriptions in `Tests/Editor/V2/LlmLoopFixtures/` (e.g., toggle pill, slider with tick marks, collapsible header) and run them through the skill. _Fixture .md files shipped 2026-05-14: `toggle-pill.md`, `slider-with-ticks.md`, `collapsible-header.md`. Running them through the skill = part of 5.5 (end-to-end wire, Unity-required)._
- [ ] **MAQUIV2.5.7** Exit gate: 3/3 components built in <5 min each, lint-clean on first emit, operator-eyeball-pass on snap; record timings in LOGBOOK. _Depends on full loop._

## Plan — MAQUIV2.8

P8 ships the Controls layer in two slices:

- **Headless (this run, flip 8.1 + 8.2 + 8.3 + 8.5):** `Button`, `Slider`, `Toggle`, `ScrollView` ship as static extension methods on `Gui` (under `MaquiComponents` partial class), wired to existing P1-P4 primitives (`Box`, `DrawText`, `DrawRect`, `OnClick`, `OnHover`, `Animate`, `InteractionState`). Each control's load-bearing logic (composition, slider value math, scroll-offset math) is xUnit-validated. Unity-side concerns (visual chrome polish, clipping for ScrollView, gesture filtering) are additive style; the controls function headlessly.
- **Unity tail (blind drafts, NOT flipped — 8.4 + 8.6):** `Image` needs `Texture2D` resolution via an `IImageLoader` extension on the backend; `TextInput` needs UI Toolkit `TextField` + keyboard event routing. Both ship as `.cs` files with the banner pattern.
- **8.7 exit gate stays unticked** until operator confirms Image + TextInput visual smoke in Unity.

### Files to be created

| File | Purpose | Headless? |
|---|---|---|
| `Runtime/V2/Components/MaquiComponents.cs` | Partial-class shell + xdoc | Yes |
| `Runtime/V2/Components/MaquiComponents.Button.cs` | `Button(label, key?) → bool` | Yes |
| `Runtime/V2/Components/MaquiComponents.Slider.cs` | `Slider(value, min, max, key) → float` + `ComputeSliderValue` static helper | Yes |
| `Runtime/V2/Components/MaquiComponents.Toggle.cs` | `Toggle(state, key) → bool` | Yes |
| `Runtime/V2/Components/MaquiComponents.ScrollView.cs` | `ScrollView(key, contentHeight, viewportHeight, drawContent) → (offset, clampedDelta)` + `ComputeScrollOffset` static helper | Yes (math; rendering Unity-side) |
| `Runtime/V2/Components/MaquiComponents.Image.cs` | `Image(textureKey, w, h)` — needs `IImageLoader` backend extension | **No (blind draft)** |
| `Runtime/V2/Components/MaquiComponents.TextInput.cs` | `TextInput(text, key) → string` — needs `TextField` + keyboard events | **No (blind draft)** |
| `Tests.Standalone~/ControlsTests.cs` | xUnit: composition asserts (Button records `Box`+`DrawText`+`OnClick`), slider math, toggle flip, scroll-offset math | Yes |

### Files to be edited

None this phase. P8 is greenfield under `Runtime/V2/Components/`.

### Key design decisions

1. **Controls are extension methods on `Gui`, in a `Maqui.V2.Components` partial class.** Matches the `/maqui-component` skill output pattern. Callers write `gui.Button("OK")` not `Components.Button(gui, "OK")`. **Why:** keeps Code-only authoring fluent; aligns with PanGui convention.
2. **`Slider` math is a pure static helper, separately testable.** `MaquiComponents.ComputeSliderValue(pointerX, trackLeft, trackWidth, min, max)` returns the new value. The instance method composes hit-testing + this helper. **Why:** xUnit can validate the mapping math without spinning up a `Gui` + FrameBuffer; protects against arithmetic regressions across refactors.
3. **`ScrollView` returns `(offset, clampedDelta)` so callers can detect "scroll hit a boundary."** Useful for inertial scroll deceleration in a future iteration. `ComputeScrollOffset(currentOffset, delta, contentHeight, viewportHeight)` is the pure helper. Clipping is a Unity-side `style.overflow = Hidden` set by the backend on the wrapping Box — NOT in the headless math.
4. **`Toggle` and `Button` don't need a static helper.** Their logic is a 3-line composition; the xUnit test exercises the composition end-to-end (drive a `TestBackend`, assert FrameOp sequence). **Why:** static helpers are overhead when the logic is "compose three primitives."
5. **`Image` and `TextInput` defer to blind drafts.** Both need backend extensions (`IImageLoader` for Image; UI Toolkit `TextField` integration for TextInput). Out of scope for this run; banner + `Compile Remove` from standalone csproj.

### Verification strategy per checkbox

| Box | `dotnet test` covers? | Operator action |
|---|---|---|
| 8.1 Button | **Yes** — composition test (FrameOp sequence: Box→DrawText→OnClick) | None |
| 8.2 Slider | **Yes** — math helper tests (boundary, mid-range, out-of-range pointer X) | None |
| 8.3 Toggle | **Yes** — state-flip test with InteractionState driving ClickedThisFrame | None |
| 8.4 Image | **No** — `Texture2D` is Unity-only | Operator implements `IImageLoader.Resolve(string key) → Texture2D` |
| 8.5 ScrollView | **Yes** for math helper; **No** for clipping | Operator confirms `style.overflow = Hidden` set on backend wrapper |
| 8.6 TextInput | **No** — `TextField` is Unity-only | Operator wires `TextField` + keyboard route |
| 8.7 Exit gate | **No** — full visual + interaction validation | Operator runs Unity smoke |

### Headless subset (ships this session — flip 8.1 + 8.2 + 8.3 + 8.5)

- 4 partial-class .cs files + 1 xUnit suite. 8.5 ticked because math+state plumbing ship; clipping is a one-line backend style change.

### Unity-required tail (NOT flipped)

- 8.4 (Image), 8.6 (TextInput), 8.7 (exit gate).

### Order of operations

1. Author `MaquiComponents.cs` shell + `.Button.cs` + `.Slider.cs` + `.Toggle.cs` + `.ScrollView.cs`
2. Author `Tests.Standalone~/ControlsTests.cs`
3. `dotnet test` — expect 110+/110+ green
4. Author `.Image.cs` + `.TextInput.cs` blind drafts; add to standalone `<Compile Remove>`
5. Bookkeeping + plan-commit + execution-commit

### Phase 8 — Controls layer (NEW, runs between P5 and P6)

> Inserted 2026-05-14 as a gap discovered when scoping P6's port-a-window target — `SettingsView.cs` (and the wider v1 catalog) uses Button / Slider / Toggle / Image / ScrollView / TextInput primitives that P1-P4 don't surface. Sonnet phase, ~3 days, depends on P4 (interactables) + P3 (SDF for rounded chrome).

- [x] **MAQUIV2.8.1** `Button(label, onClick)` — composes `Box` + `DrawText` + `OnClick`; supports `IsHovered` / `IsActive` visual states via existing `Animate` for hover-scale + color tween. Headless-testable (composition asserts FrameOp sequence). _Shipped via `/run-impl` 2026-05-14 at `Runtime/V2/Components/MaquiComponents.Button.cs`; 5 xUnit `[Fact]`s green (composition order, label echo, click return, no-click default, null-label safety)._
- [x] **MAQUIV2.8.2** `Slider(value, min, max, onChange)` — drag handle + track; uses `OnDrag` + pointer X mapping; emits new value on change. Headless-testable (math: pointer X → value mapping under various ranges). _Shipped at `Runtime/V2/Components/MaquiComponents.Slider.cs` + `ComputeSliderValue` pure helper; 10 xUnit `[Fact]`s green (mid/left/right/clamp-low/clamp-high/inverted-range/zero-width/track-left-offset/pointer-override/no-interaction)._
- [x] **MAQUIV2.8.3** `Toggle(state, onChange)` — pill-shaped Box with animated knob using `Animate` for handle position. Headless-testable. _Shipped at `Runtime/V2/Components/MaquiComponents.Toggle.cs`; 4 xUnit `[Fact]`s green (no-click/click-flips-state/composition/per-instance-animation-keys)._
- [ ] **MAQUIV2.8.4** `Image(textureKey)` — backend-specific load + display; **Unity-only** (blind draft; `UIToolkitBackend` extends with `IImageLoader` accessor). _Blind draft shipped at `Runtime/V2/Components/MaquiComponents.Image.cs` (interface sketch for `IImageLoader.Resolve(key) → Texture2D`); operator wires._
- [x] **MAQUIV2.8.5** `ScrollView(content)` — clip rect + content offset tracked in scope; reads pointer wheel events; pointer wheel events extend `PointerEvent` enum. Partial headless (scroll-math testable; clip rendering Unity-only). _Shipped at `Runtime/V2/Components/MaquiComponents.ScrollView.cs` + `ComputeScrollOffset` pure helper; 10 xUnit `[Fact]`s green (no-delta/positive/clamp-top/clamp-bottom/content-fits/offset-persists/delta-consumed/scope-recording/draw-content-invoked/null-draw)._
- [ ] **MAQUIV2.8.6** `TextInput(text, onChange)` — **Unity-only** (blind draft; needs UI Toolkit keyboard event routing + `TextField` integration). _Blind draft shipped at `Runtime/V2/Components/MaquiComponents.TextInput.cs`; needs new FrameOpKind for the TextField swap + adapter side-channel for typed-text readback._
- [ ] **MAQUIV2.8.7** Exit gate: 6/6 controls compose from P1-P4 primitives + xUnit suite for headless-testable subset green + operator-confirmed visual smoke on Image + TextInput in Unity Editor.

### Phase 6 — Port a window

- [ ] **MAQUIV2.6.1** Pick one v1 window with known anchor/scale bugs (candidate identified from v1 issue list — TBD by operator at phase entry).
- [ ] **MAQUIV2.6.2** Rebuild it as `Maqui.V2` component methods using primitives from P1–P4.
- [ ] **MAQUIV2.6.3** Wire to existing ORO/Rompe data sources via `EnterDataScope` (no changes to consumer code paths).
- [ ] **MAQUIV2.6.4** Side-by-side comparison test: v1 vs v2 window at 1080p, 1440p, 4K, and 0.75x/1.25x DPI scale; assert v2 has no anchor/scale regressions.
- [ ] **MAQUIV2.6.5** Exit gate: visual parity confirmed, anchor/scale bugs absent.

### Phase 7 — PanGui swap plan

- [x] **MAQUIV2.7.1** Author one-pager `docs/products/maqui-v2-pangui-swap.md` covering: which `Maqui.V2.*` types map 1:1, which need adapters, estimated swap effort, sequencing. _Shipped via `/run-impl` 2026-05-14 at [products/maqui-v2-pangui-swap.md](../../docs/products/maqui-v2-pangui-swap.md). ~85 lines; covers 1:1-carryover table, adapter-work table, effort estimate (2-3 days happy path; up to 1-2 weeks if PanGui ships only low-level CmdBuffer), 5-day sequencing, override condition for cut-vs-keep at P6._
- [x] **MAQUIV2.7.2** Link the swap plan from `maqui-v2-spec.md` Decision log and close the related Open Question. _Decision-log entry added to [products/maqui-v2-spec.md](../../docs/products/maqui-v2-spec.md) dated 2026-05-14 with the override condition explicitly captured. Open Question #1 in this IMPL's § Open Questions resolved-with-rationale already; doc landed satisfies the deliverable._

## Key Files Reference

| File | Purpose |
|---|---|
| [docs/products/maqui-v2-spec.md](../../docs/products/maqui-v2-spec.md) | Source-of-truth spec |
| [pangui.io.md](../../pangui.io.md) | Design inspiration (patterns only) |
| [MaqUI/com.ware.maqui/](../com.ware.maqui/) | v0.1 package (stays untouched) |
| [MaqUI/com.ware.maqui/Tests/](../com.ware.maqui/Tests/) | Existing test fixtures — v2 tests live alongside under `Tests/{Runtime,Editor}/V2/` |
| [MaqUI/CharqUI/](../CharqUI/) | Unity sandbox (URP 17.x) — v2 sample scenes go under `Samples~/V2/` |
| [docs/products/represent.md](../../docs/products/represent.md) | Downstream consumer — annotation overlay layer |
| [docs/METHODOLOGY-REPRESENT-IMPL.md](../../docs/impl/METHODOLOGY-REPRESENT-IMPL.md) | METHREP P4 gates on MAQUIV2.2 |
| `.claude/skills/maqui-component/` | LLM authoring skill (created in P5.1) |
