# Procedural Visuals Standards

To maintain a "Premium" look while keeping asset sizes low, the system utilizes procedural mesh and texture generation. These standards define how and when to use these tools.

---

## 1. Core Procedural Tools

### Gradient Mesh Effect (`Gradient.cs`)
Avoid using baked sprite gradients. Use the vertex-based `Gradient` component to apply dynamic coloring to standard UI elements.
- **Usage**: Apply to `Image` or `Text` components.
- **Standard**: Always use HSL-harmonious color pairs (e.g., Slim-Dark for backgrounds, Vibrant-Glow for highlights).

```mermaid
---
config:
  theme: dark
---
graph LR
    A[UI Element] --> B[Vertex Helper]
    B --> C[Gradient Script]
    C --> D[Modify Vertex Color]
    D --> E[Final Render: No Extra Draw Calls]
```

### Procedural Overlays (`Popup.cs`)
Popups must generate their own dimming textures at runtime.
- **Benefit**: No "Background.png" required in the project.
- **Logic**: Use `Texture2D.SetPixel` or `Graphic.CrossFadeAlpha` for smooth transitions.

---

## 2. Visual Standards (Aesthetics)

To achieve a "WOW" effect, every UI element should follow these rules:

| Element           | Standard                              | Tool                      |
| :---------------- | :------------------------------------ | :------------------------ |
| **Buttons**       | Subtle Scale on Hover (1.05x)         | `BaseView.Animate`        |
| **Borders**       | Semi-transparent with Rounded Corners | `RoundedMask` Shader      |
| **Glassmorphism** | 20-40% Alpha + Blur                   | `BackgroundBlur` Material |
| **Transitions**   | Ease-in-out Cubic curve               | `DisplayOptions.Duration` |

---

## 3. Optimization Guidelines

1. **Avoid `new Material()`**: Procedural effects often create material instances. Use a **Shared Material Pool** where possible to enable GPU batching.
2. **Vertex Limit**: Keep the number of `BaseMeshEffect` components per canvas under 50 to avoid significant CPU overhead during layout changes.
3. **Sprite Stripping**: If a procedural effect can replace a 256x256 texture, ALWAYS use the procedural effect.

---

## 4. Implementation Example: The "Glow" Button

```csharp
public class GlobalGlowButton : MonoBehaviour {
    // Standardizing the "Premium" look procedurally
    void Start() {
        var grad = gameObject.AddComponent<Ricimi.Gradient>();
        grad.Color1 = new Color(0.2f, 0.4f, 1f, 1f); // Vibrant Blue
        grad.Color2 = new Color(0.1f, 0.1f, 0.2f, 1f); // Deep Dark
    }
}
```

> [!IMPORTANT]
> All procedural effects MUST be preview-able in the Editor. Implement `OnValidate()` or inherit from `BaseMeshEffect` to ensure the "DX" remains high for designers.
