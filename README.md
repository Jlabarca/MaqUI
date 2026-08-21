# Maqui

**Immediate-mode UI framework for Unity, on a retained reconciler.**
Declarative per-frame widget code · pooled UI Toolkit backend · headless-testable pure layer.

> Requires Unity 2022.3 LTS or newer.

---

## What it is

You write UI as a function of state that runs every frame:

```csharp
private static void BuildUI(Gui gui)
{
    gui.Column(default, default, className: "panel");
    {
        gui.DrawText($"HP {_hp}/{_max}", className: "title");
        gui.Bar("hp", _hp / (float)_max);

        if (gui.Button("Heal", key: "heal"))
            _hp = _max;
    }
    gui.EndColumn();
}
```

There is no view class to keep in sync, no binding to wire, and no prefab to author. The call
sequence *is* the UI. Between frames a reconciler diffs the recorded frame against the live
`VisualElement` tree and applies only what changed, reusing pooled elements — so you get
immediate-mode ergonomics at retained-mode cost.

## Why it is built this way

- **A frame is a value.** `Gui` records into a `FrameBuffer` of `FrameOp`s. Nothing touches Unity
  during your build function, which is what makes the interesting half testable without an Editor.
- **The pure layer is the tested layer.** Layout maths, the reconciler, scroll offsets, text
  ramping, flow-column counts and every component's state machine live in plain C# and run under
  `dotnet test` in milliseconds — see `com.ware.maqui/Tests.Standalone~` (286 tests).
- **The backend is swappable.** `IBackend` is the seam; `UIToolkitBackend` is the shipping
  implementation. Unity-facing code is deliberately thin, because that is where the bugs live.
- **Keys, not positions.** Repeated or conditional content must be wrapped in
  `gui.EnterDataScope(key)`. Without it the reconciler matches by position and a container can be
  handed a pooled node that was something else last frame, inheriting its inline styles.

## Components

Built on the primitives (`Row`, `Column`, `Box`, `ScrollBox`, `DrawText`, `DrawRect`, `DrawImage`,
`DrawLine`, `DrawCircle`, `Spacer`, `ClipBox`):

`Bar` · `Button` · `Dropdown` · `EquipSlot` · `FlowGrid` · `Grid` · `Hotbar` · `Icon` · `Image` ·
`ItemSlot` · `ProgressBar` · `SkillEntry` · `Slider` · `StatRow` · `Stepper` · `Tabs` · `TextInput` ·
`Toggle` · `TogglePill` · `Tooltip` · `UnitFrame`

Responsiveness primitives: `Size.WithMin/Max`, `TextScale.Ramp`, `HidePriority`, `FlowLayout.Columns`,
and a floating `TooltipOverlay`.

## Requirements

| Dependency | Version | How to install |
|:---|:---|:---|
| **Unity** | 2022.3 LTS+ | — |
| **UniTask** | latest | Git UPM |
| **R3** | latest | Git UPM |

## Samples

| Sample | What it shows |
|:---|:---|
| **Hello World** | The `Gui` loop, a window, a few widgets. |
| **Draggable Handle** | Pointer interaction through the interaction adapter. |

## Testing

```bash
dotnet test com.ware.maqui/Tests.Standalone~/Maqui.Tests.csproj
```

The Unity-side assemblies (`UIToolkitBackend`, `GuiDriver`, `SdfBackend`,
`UIToolkitInteractionAdapter`, `ResourcesImageLoader`, `TooltipOverlay`) are excluded from that
project by design — they are exercised in-Editor instead.

---

## History

Maqui shipped a reactive **MVVM** framework first — `ViewModel` + `ReactiveProperty<T>`,
`ReactiveBaseView<T>`, VitalRouter command routing, a four-layer Canvas hierarchy and a modal freeze
system. That stack was **removed in full on 2026-08-21**; the immediate-mode framework described
above replaced it, and "Maqui" now means what was previously called "Maqui V2".

If you are reading old notes: `Maqui.V2` is now `Maqui`, `Runtime/V2/` is now `Runtime/`, and
anything referring to `MaquiServices`, `IUIService`, `MaquiBaseView`, `ReactiveBaseView`,
`MaquiWindowManager` or `ThemeData` describes deleted code. Rationale and the full removal record:
`docs/MAQUI-V2-IMPL.md` here, and `docs/MAQUI-V1-SUNSET-IMPL.md` in the ORO repo.
