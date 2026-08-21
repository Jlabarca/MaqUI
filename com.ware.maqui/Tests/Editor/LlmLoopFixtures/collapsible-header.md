# Fixture: CollapsibleHeader

A clickable header row that toggles a collapsed/expanded state. When expanded, an animated chevron icon rotates 90° and the content below is rendered; when collapsed, content is hidden.

## Inputs

- `string key` — identity for animation + state
- `string label` — header text
- `bool expanded` — current state
- `Action drawContent` — delegate to render the body when expanded

## Output

- `bool newExpanded` — toggled if header was clicked this frame

## Visual

- Header row: ~280×36 px; horizontal layout
- Chevron icon: 16×16 px on the left, rotated 0° (collapsed) or 90° (expanded)
- Label: aligned start, font 14
- Hover: light background tint behind the header row
- Content body: rendered via `drawContent()` inside a `Column` only when `expanded` is true; vertical slide-in via `Animate` on height

## Interaction

- Click anywhere on the header row toggles `expanded`
- Chevron rotation tweens via `gui.Animate(key + "-chevron-deg", expanded ? 90f : 0f)`
- Content panel height tweens via `gui.Animate(key + "-content-h", expanded ? 1f : 0f)` and is clamped

## Notes for the LLM

- Use `Row` for the header + child `Box` for the chevron + `DrawText` for the label
- Don't `Resources.Load` an arrow texture — use `DrawText("▶")` or compose from primitives
- Wrap content rendering in `using (gui.EnterDataScope(key))` so the body's own animations/state are namespaced
