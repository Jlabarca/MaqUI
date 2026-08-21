# Fixture: SliderWithTicks

A horizontal slider with discrete tick marks along the track. Drag the handle to set a value; the handle snaps to the nearest tick when released.

## Inputs

- `string key` — animation slot identity
- `float value` — current value in `[min, max]`
- `float min`, `float max` — value range
- `int tickCount` — number of evenly-spaced tick marks (≥ 2)

## Output

- `float newValue` — possibly-updated value after this frame's drag interaction

## Visual

- Track is ~240×6 px; rounded corners (radius 3); dim gray fill
- Tick marks: 2×8 px vertical bars at evenly-spaced positions along the track; mid-gray
- Handle: 20×20 px circle (or rounded-rect with radius 10); white fill, subtle drop-shadow
- Active fill (left of handle): brand-blue
- Hover on handle: handle scale to 1.08 via `Animate`

## Interaction

- `OnDrag` on the handle reads pointer X; map to value via `value = min + (pointerX - trackLeft) / trackWidth * (max - min)`
- On `Up` (drag end), snap to nearest tick: round `value` to nearest tick step
- Snap animation: store the snapped value in animation slot `key + "-handle-x"` so the snap is smooth, not jumpy

## Notes for the LLM

- Use `Row` for the track + handle layout
- Tick marks render via a loop calling `gui.DrawRect` at computed X positions
- No magic-number sizes — use `Size.Pixels(...)` consistently
