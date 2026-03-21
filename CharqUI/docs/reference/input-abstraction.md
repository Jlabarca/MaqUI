# Dual-Input System Abstraction

To achieve "Ultimate DX" and maximum compatibility, the UI system must support both the **Legacy Input Manager** and the **New Unity Input System** without forcing the developer to write different code for each.

---

## 1. The "Input Bridge" Pattern

Instead of calling `Input.GetKeyDown` or `InputAction.triggered` directly in your Views, the framework uses an abstraction layer.

### The Abstraction (`IInputProvider`)

```csharp
public interface IInputProvider {
    bool GetButtonDown(string ActionName);
    Vector2 GetVector2(string ActionName);
    event Action<string> OnActionPerformed;
}
```

---

## 2. Implementation Strategies

### Case A: Legacy Input (Unity Input Manager)
Uses standard string-based lookups from the `Project Settings > Input Manager`.

```csharp
public class LegacyInputProvider : IInputProvider {
    public bool GetButtonDown(string name) => Input.GetButton(name);
    public Vector2 GetVector2(string name) => new Vector2(Input.GetAxis(name + "X"), Input.GetAxis(name + "Y"));
    // ...
}
```

### Case B: New Input System (`com.unity.inputsystem`)
Uses `InputActionAsset` to map hardware inputs to semantic names.

```csharp
public class NewInputProvider : IInputProvider {
    [SerializeField] private PlayerInput playerInput;
    
    public bool GetButtonDown(string name) => playerInput.actions[name].WasPressedThisFrame();
    public Vector2 GetVector2(string name) => playerInput.actions[name].ReadValue<Vector2>();
}
```

---

## 3. Auto-Detection & Switching

The framework can automatically decide which provider to use based on preprocessor directives or project settings.

```csharp
public static class InputBridge {
    public static IInputProvider Instance { get; private set; }

    static InputBridge() {
        #if ENABLE_INPUT_SYSTEM
            Instance = new NewInputProvider();
        #else
            Instance = new LegacyInputProvider();
        #endif
    }
}
```

---

## 4. How it looks in the View (Hybrid Reactive)

Developers can now write clean, system-agnostic code.

```csharp
public class MySettingsView : ReactiveBaseView<SettingsViewModel> {
    void Update() {
        // Works regardless of which input system is active in Project Settings
        if (InputBridge.Instance.GetButtonDown("Cancel")) {
            UIFramework.NavigateBack();
        }
    }
}
```

---

## 5. Decision: When to use which?

| Metric                       | Legacy Input Manager      | New Input Guide                    |
| :--------------------------- | :------------------------ | :--------------------------------- |
| **Ease of Setup**            | 🟢 High (Default)          | 🟡 Moderate (Needs Asset)           |
| **Multi-Device Support**     | 🔴 Low (Hard for gamepads) | 🟢 High (Native Rebinding)          |
| **Performance**              | 🟡 Moderate                | 🟢 High (Event-driven)              |
| **Framework Recommendation** | Use for small prototypes. | **Use for production games/apps.** |

---

> [!TIP]
> By using the `InputBridge`, you can start your project with Legacy input and upgrade to the New Input System later by simply swapping the provider—without touching a single line of your View code.
