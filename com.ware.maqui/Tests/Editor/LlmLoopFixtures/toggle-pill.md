# Fixture: TogglePill

A pill-shaped toggle (on/off switch). When clicked, an animated knob slides from one end of the pill to the other and the underlying boolean state flips.

## Inputs

- `string key` — identity for the animation slot (so multiple toggles on the same screen don't share knob position)
- `bool state` — current on/off state

## Output

- `bool newState` — the (possibly flipped) state after this frame's interaction is processed

## Visual

- Pill is ~80×32 px; rounded corners (radius 16)
- Off: background gray; knob on the left, knob fill light gray
- On: background brand-green; knob on the right, knob fill white
- Hover: subtle ~5% lighter background; cursor pointer
- Active (pointer down): slight scale-down on the knob (0.95)

## Interaction

- Click anywhere on the pill flips `state`
- Animated knob slide uses `gui.Animate(key + "-knob-x", targetX)` with default stiffness/damping
- Color tween between off-color and on-color uses `gui.Animate(key + "-color-t", state ? 1f : 0f)` then `Color32.Lerp` (composed externally — the component returns the lerped color)

## Notes for the LLM

- Use `Box` for the pill body, a child `Box` for the knob
- Don't load any textures — pure shape + color
- Animation keys must include the user-supplied `key` so multiple toggles coexist
