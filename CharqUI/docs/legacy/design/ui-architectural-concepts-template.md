# UI Concept Re-conditioning & Template Guide

This guide explains how to harvest the best parts of **OneUI** (Architecture) and **Modular Game UI Kit** (Visuals) and adapt them into your new UI architecture.

---

## 1. Adapting Into: Concept 1 (Mediated Reactive)
**Recommended Case**: Building a robust, scalable game UI.

### OneUI Re-conditioning (The "Brain")
OneUI's `BaseView` handles visibility and lifecycle perfectly, but it relies on static payloads. To re-condition it into a Reactive View, we bridge the the static `OnDataUpdated` hook to a dynamic `ViewModel`.

**Complexity**: 🟢 Low
**How-To**: Modify `BaseView` to include a reference to a generic `ViewModel`.

```csharp
// RE-CONDITIONED ONEUI BASE VIEW
public abstract class ReactiveBaseView<TViewModel> : BaseView where TViewModel : ViewModel {
    protected TViewModel ViewModel;

    public void Bind(TViewModel vm) {
        ViewModel = vm;
        OnBind(); // Custom lifecycle hook
    }

    // Instead of using OneUI's OnDataUpdated directly
    protected abstract void OnBind();
}
```

### Modular Kit Re-conditioning (The "Body")
Modular Kit's `Gradient.cs` should be treated as a **Visual Data Consumer**.

```csharp
// RE-CONDITIONED MODULAR COMPONENT
public class ReactiveGradient : Ricimi.Gradient {
    public void UpdateColors(Color c1, Color c2) {
        this.Color1 = c1;
        this.Color2 = c2;
        // Trigger uGUI rebuild
    }
}
```

---

## 2. Adapting Into: Concept 2 (State-Store / Redux)
**Recommended Case**: Predictable menus with high undo/redo needs.

### OneUI Re-conditioning
OneUI's `UIFramework` becomes the "Action Dispatcher." Instead of showing a view directly, you dispatch an action to the store, and the store tells the `UIFramework` which view to enable.

**Complexity**: 🔴 High
**How-To**: You must wrap the `UIFramework.Navigate` calls inside Store Actions.

```csharp
// REDUX ADAPTATION
public void ShowSettingsAction() {
    Store.Dispatch(new NavigationAction { TargetView = typeof(SettingsView) });
}

// Global Listener
Store.Subscribe(state => {
    UIFramework.GetView(state.CurrentView).ShowView();
});
```

---

## 3. The "Base Template" Blueprint
Use this structure to start any new project using your harvested "Super Kit":

### Hierarchy Template
```text
Root_UI_Canvas
├── [SERVICE] MainThreadDispatcher (from OneUI)
├── [MANAGER] UIFramework (from OneUI)
├── [VIEW] MainView (ReactiveBaseView)
│   ├── [VISUAL] Header_Background (Modular Kit Gradient)
│   ├── [VISUAL] PopUp_Overlay (Modular Kit Procedural BG)
│   └── [WIDGET] HealthBar (Reactive Binding)
└── [SERVICE] UIStore / ViewModelProvider
```

### Script Directory Structure
```text
Assets/ProjectUI/
├── Core/ (Re-conditioned Base Classes)
├── ViewModels/ (Pure C# Logic)
├── Views/ (OneUI + Modular Kit Prefabs)
└── Styles/ (Procedural Themes & USS)
```

---

## 4. Decision: uGUI vs. UI Toolkit Templates

| Element              | Template Choice         | Rationale                                              |
| :------------------- | :---------------------- | :----------------------------------------------------- |
| **HUD / Health**     | **Classic uGUI**        | Fast updates, world-space binding needed.              |
| **Inventory / Shop** | **UI Toolkit**          | Scroll optimization and data item reuse.               |
| **Dialogue Popups**  | **Modular Kit + OneUI** | Best-looking procedural visuals and stable lifecycles. |
| **Main Menu**        | **UI Toolkit**          | Efficient layout for many buttons/options.             |

---

> [!TIP]
> **Pro-Tip**: When re-conditioning **Modular Kit** scripts, always check for `new Material()` calls and replace them with a `SharedMaterialManager` to keep draw calls low across your new architecture.
