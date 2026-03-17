# Demos TODO — Rewire Existing Scene UI with Maqui

The current demos create new prefabs that overlay on top of the original scene UI.
The correct approach is to **rewire the existing UI elements** in each scene to work with Maqui's MVVM/reactive layer.

---

## Demo 1 — Gallery (OneUI)

**Scene**: `Assets/MaquiDemos/Scenes/Demo1_Gallery.unity`
**Source UI**: OneUI component showcase (buttons, cards, sliders, toggles, inputs)

### What should happen:
- Keep the existing OneUI UI hierarchy intact
- Remove only the OneUI `SceneController` / `ObjectInstaller` scripts
- Create a `GalleryViewModel` that exposes reactive state for the existing tabs/components
- Create a `GalleryView : ReactiveBaseView<GalleryViewModel>` that binds to the existing UI elements in the scene (not new prefabs)
- Wire the existing tab buttons to switch content panels via ViewModel state
- Wire the existing theme toggle to `ThemeProvider.Instance.SetTheme()`
- Add `ThemeSubscriber` components to existing UI elements for reactive theming

### What NOT to do:
- Do NOT create new prefab overlays
- Do NOT instantiate OneUI prefabs into new ScrollRects — they're already in the scene

---

## Demo 2 — Game HUD (Pack)

**Scene**: `Assets/MaquiDemos/Scenes/Demo2_GameUI.unity`
**Source UI**: Modular Game UI Kit — HUD with health bars, gold, inventory, shop panels

### What should happen:
- Keep ALL existing Pack UI hierarchy (HUD, inventory panel, shop panel)
- Disable only the Pack demo controller scripts
- Create ViewModels that model the existing UI state (HP, MP, gold, inventory items, shop items)
- Create Views that bind to the **existing** UI elements (Text, Slider, Button) already in the scene
- Wire existing "Bag" button → open existing inventory panel via VitalRouter command
- Wire existing shop button → open existing shop panel as Modal
- Use `MaquiWindowManager` layer system by re-parenting existing panels, or manage visibility directly

### What NOT to do:
- Do NOT create new HUD/Inventory/Shop prefabs — they already exist in the scene
- Do NOT overlay Maqui canvases on top of Pack canvases

---

## Demo 3 — Frosted HUD (TranslucentImage)

**Scene**: `Assets/MaquiDemos/Scenes/Demo3_FrostedHUD.unity`
**Source UI**: Le Tai TranslucentImage demo — frosted glass panels, blur effects

### What should happen:
- Keep the existing frosted glass UI elements
- Create ViewModels for character info, notifications, settings
- Create Views that bind to the **existing** frosted panels and their child Text/Image components
- Wire existing buttons to show/hide existing panels via VitalRouter commands
- Use TranslucentImage blur effects as-is — just drive them reactively

### What NOT to do:
- Do NOT recreate frosted panels from scratch
- Do NOT lose the TranslucentImage blur effects by replacing with plain panels

---

## General Approach for All Demos

1. Study each scene's existing hierarchy (names, components, layout)
2. Identify which scripts to remove (demo controllers) vs keep (visual components)
3. Create ViewModel with reactive properties matching the existing UI state
4. Create View that finds existing UI elements via `transform.Find()` or serialized references
5. Bind existing elements to ViewModel in `OnBind()`
6. Use VitalRouter commands for navigation between panels
7. Result: same visual look, but driven by Maqui MVVM

---

## Current State (v0.1 — overlay demos)

The current implementation creates 7 new prefabs at `Assets/Resources/Views/` that render on top of the scene UI via `MaquiWindowManager` (Screen Space Overlay). These work as a proof-of-concept but don't demonstrate the intended "rewire existing UI" pattern.

Files to revise when redoing:
- `Assets/MaquiDemos/Scripts/Demo1_Gallery/` — GalleryView, GalleryViewModel, Demo1Starter
- `Assets/MaquiDemos/Scripts/Demo2_GameUI/` — HUDView, HUDViewModel, InventoryView/VM, ShopView/VM, Demo2Starter, commands
- `Assets/MaquiDemos/Scripts/Demo3_Frosted/` — FrostedHUDView/VM, NotificationsView/VM, SettingsView/VM, Demo3Starter
- `Assets/MaquiDemos/Scripts/Editor/MaquiViewPrefabCreator.cs` — can be removed once views bind to scene objects
- `Assets/MaquiDemos/Scripts/Editor/MaquiDemoSetup.cs` — simplify setup (less teardown needed)
