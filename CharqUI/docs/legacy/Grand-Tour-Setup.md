# Grand Tour Scene Setup Guide 🚀

Follow these steps to set up the comprehensive **CharqUI Grand Tour** showcase using the provided pre-built assets.

## 0. Pre-built Assets 📦
I've generated the following assets to speed up your setup:
- **Orchestrator Prefab**: `GrandTour_Orchestrator.prefab` (Logic & Driver)
- **Screen HUD Prefab**: `GrandTour_ScreenView.prefab` (The main uGUI interface)
- **World Label Prefab**: `GrandTour_WorldView.prefab` (Floating 3D reactive label)
- **UI Toolkit Layout**: `GrandTour_Layout.uxml`
- **UI Toolkit Styles**: `GrandTour_Styles.uss`

## 1. Scene Prerequisites
- Ensure **VitalRouter**, **R3**, and **UniTask** are correctly installed in your project.
- Ensure the **OneUI UIFramework** is present in the scene (or initialized via `CoreBootstrap`).

## 2. Hierarchy Setup

### A. The Orchestrator
1. Drag the **`GrandTour_Orchestrator.prefab`** into your scene.

### B. The Screen View (uGUI HUD)
1. Create a **Canvas** (Screen Space - Overlay).
2. Attach a **CanvasGroup** and **GraphicRaycaster**.
3. Drag the **`GrandTour_ScreenView.prefab`** as a child of the Canvas.
4. (Optional) Inside the prefab, you can wire your own TMP/Button/Slider components if you want to customize them.

### C. The World View (uGUI World Space)
1. Create a **Canvas** (World Space).
2. Position it somewhere in 3D space.
3. Drag the **`GrandTour_WorldView.prefab`** as a child of this World Canvas.

### D. The Settings View (UI Toolkit)
1. Create a GameObject named `SettingsUI`.
2. Attach a **UIDocument** component.
3. Attach the `GrandTourSettingsView` script.
4. **Important**: Assign your `GrandTour_Layout.uxml` file to the UIDocument.
    - Ensure the UXML contains elements with names: `UserNameField` (TextField), `GlobalLevelSlider` (Slider), and `StartLoadingBtn` (Button).

## 3. Final Wiring
1. Go back to the `[GrandTour_Orchestrator]`.
2. Drag your Screen View, World View, and Settings View game objects into their respective slots in the `GrandTourInitializer` component.

## 4. Run & Test
- Press **Play**.
- Type in the UI Toolkit field -> Watch the uGUI and World Space labels update instantly.
- Click the uGUI Button -> Watch the World Space level counter increment.
- Click "Start Loading" -> Watch the uGUI progress bar fill up via the reactive stream.

---
> [!TIP]
> This setup proves that CharqUI can bridge **three different UI paradigms** (uGUI Screen, uGUI World, and UI Toolkit) using a single, unified reactive state.
