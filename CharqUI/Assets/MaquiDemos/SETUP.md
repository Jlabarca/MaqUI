# Maqui Demos — Scene Assembly Guide

Three demo scenes, each self-contained.
**No scene setup for Maqui itself** — `CoreBootstrap` fires at `BeforeSceneLoad` and creates `Maqui_Core` automatically.

---

## Shared Prerequisites

### Resources folders (create these)
```
Assets/Resources/Views/          ← all demo prefabs go here (read by ResourcesAssetProvider)
Assets/Resources/Themes/         ← Theme_Dark.asset, Theme_Light.asset (for Demo 1 theme toggle)
Assets/Resources/Icons/          ← optional sprites named sword.png, potion.png … (for Demo 2 rows)
```

### asmdef wired up
`MaquiDemos.asmdef` is already configured with all references.
Unity will compile it on import — check Console for errors before building scenes.

---

## Demo 1 — OneUI Component Gallery
**Scene:** `Assets/MaquiDemos/Scenes/Demo1_Gallery.unity`
**Shows:** Tab switching via VitalRouter commands, theme swapping, fade+scale entrance.

### Scene objects
| GameObject | Components |
|:---|:---|
| `Main Camera` | Camera |
| `DemoStarter` | **Demo1Starter** |

### Prefab: `Assets/Resources/Views/Demo1_Gallery.prefab`

Build it from scratch or duplicate the OneUI `DemoScene` panel as a starting point:

1. Create a new UI panel (no Canvas — it will be parented to Layer_Default by IUIService).
2. Add a `CanvasGroup` to the root → assign to **_canvasGroup**.
3. Add a `RectTransform` to the root → assign to **_panelRect** (for the scale animation).
4. **Header strip** (top, ~80px tall):
   - `TMP_Text` subtitle (e.g. "Buttons & Icons") → **_subtitleText**
   - `Button` "☽ Dark" → **_themeButton** + `TMP_Text` child → **_themeLabel**
5. **Tab bar** (3 buttons side-by-side):
   - Each: `Button` → wire into **_tabButtons[0..2]**
   - Each: child `Image` (1px tall colored underline) → **_tabIndicators[0..2]**
6. **Content panels** (3 panels that swap visibility):
   - Panel_Buttons → drag in OneUI `Prefabs/Components/Buttons/` prefabs inside a `GridLayoutGroup`
   - Panel_Cards   → drag in OneUI `Prefabs/Components/Cards/` prefabs
   - Panel_Fields  → drag in OneUI `Prefabs/Components/Fields/` prefabs
   - Each: wrap in a `ScrollRect` for overflow
   - Assign all 3 panels to **_contentPanels[0..2]**
7. Add **GalleryView** component to root.
8. Wire all Inspector fields.
9. Save as `Assets/Resources/Views/Demo1_Gallery.prefab`.

### Theme assets (optional)
If you want the theme toggle to do something visible, create `ThemeData` ScriptableObjects at
`Assets/Resources/Themes/Theme_Dark.asset` and `Theme_Light.asset`.
If absent, the toggle just logs — no crash.

---

## Demo 2 — Pack Game UI with Modal Layers
**Scene:** `Assets/MaquiDemos/Scenes/Demo2_GameUI.unity`
**Shows:** Overlay HUD → Default Inventory panel → Modal Shop with freeze system.

### Scene objects
| GameObject | Components |
|:---|:---|
| `Main Camera` | Camera |
| `DemoStarter` | **Demo2Starter** |

### Prefabs to build

#### `Assets/Resources/Views/Demo2_HUD.prefab` — HUDView
Anchor to fill the screen (it lives on the Overlay canvas).

| Element | Source asset | Script field |
|:---|:---|:---|
| Gold text | Any TMP_Text | `_goldText` |
| HP Slider | OneUI BlueSlider or Pack Slider | `_hpSlider` |
| MP Slider | Pack Progress/Linear | `_mpSlider` |
| Status text (center) | TMP_Text | `_statusText` |
| Status CanvasGroup | parent of status text | `_statusGroup` |
| Bag Button (bottom-right) | OneUI MenuIconButton | `_inventoryButton` |

Add **HUDView** component to root.

#### `Assets/Resources/Views/Demo2_Inventory.prefab` — InventoryView
Size: ~600 × 700, anchored center.

| Element | Source asset | Script field |
|:---|:---|:---|
| Title TMP_Text | TMP_Text | `_titleText` |
| Close button | OneUI CloseButton | `_closeButton` |
| Item container | ScrollRect → Content (VertLayoutGroup) | `_itemContainer` |
| Shop button (footer) | Pack Button-Primary | `_shopButton` |
| CanvasGroup (root) | CanvasGroup | `_canvasGroup` |
| RectTransform (root) | (implicit) | `_panelRect` |

**InventoryItemRow prefab** (nested):
Use Pack `Common/Prefabs/3-Layouts/Item-Locked` or any horizontal row.
Add **InventoryItemRow** component and wire: `_icon`, `_nameText`, `_qtyText`, `_valueText`.
Assign to **_rowPrefab** on the InventoryView.

Add **InventoryView** component to root.

#### `Assets/Resources/Views/Demo2_Shop.prefab` — ShopView
Size: ~700 × 600, anchored center.
Use a two-column layout: left = catalogue list, right = detail card.

| Element | Source | Field |
|:---|:---|:---|
| Close button | OneUI CloseButton | `_closeButton` |
| Catalogue container | ScrollRect → Content | `_catalogueContainer` |
| Detail name | TMP_Text (bold) | `_detailName` |
| Detail desc | TMP_Text (small) | `_detailDesc` |
| Detail price | TMP_Text | `_detailPrice` |
| Buy button | Pack Button-Primary | `_buyButton` |
| Detail root | parent of detail widgets | `_detailRoot` |
| Feedback text | TMP_Text | `_feedbackText` |
| Feedback CanvasGroup | parent of feedback | `_feedbackGroup` |
| CanvasGroup (root) | CanvasGroup | `_canvasGroup` |
| RectTransform (root) | (implicit) | `_panelRect` |

**ShopItemRow prefab** (nested):
Any horizontal row. Add **ShopItemRow** component.
Wire: `_icon`, `_nameText`, `_priceText`, `_selectButton`.
Assign to **_rowPrefab** on ShopView.

Add **ShopView** component to root.

### How the freeze demo works
1. Press **Bag** → Inventory slides in (Default layer).
2. Press **Shop** in Inventory → Shop pops in (Modal layer).
3. Watch: modal mask fades in, HUD buttons grey out (OnFreeze), Inventory input blocked.
4. Press **✕** on Shop → mask disappears, HUD restores (OnUnfreeze).

---

## Demo 3 — Frosted Glass HUD
**Scene:** `Assets/MaquiDemos/Scenes/Demo3_FrostedHUD.unity`
**Shows:** Shared ViewModel across windows, slide-in/out animations, TranslucentImage blur.

### TranslucentImage setup (required)
1. Add **TranslucentImageSource** component to `Main Camera`.
2. Set downsample and blur iterations to taste (4 iterations, σ=3 looks good at 60fps).
3. Any panel using frosted glass: add `TranslucentImage` component instead of a plain `Image`.
   Set its `Source` to the `TranslucentImageSource` on the camera.

### Scene objects
| GameObject | Components |
|:---|:---|
| `Main Camera` | Camera, **TranslucentImageSource** |
| `Background` | Image with gradient or a sprite (so the blur has something to blur) |
| `DemoStarter` | **Demo3Starter** |

### Prefabs to build

#### `Assets/Resources/Views/Demo3_FrostedHUD.prefab` — FrostedHUDView
Full-screen anchor (Overlay layer).

| Element | Field |
|:---|:---|
| Player name TMP_Text | `_playerName` |
| Zone TMP_Text | `_zoneName` |
| Notif Button (top-right) | `_notifButton` |
| Notif badge GameObject | `_notifBadge` |
| Notif count TMP_Text | `_notifCount` |
| XP Slider | `_xpSlider` |
| XP % TMP_Text | `_xpLabel` |
| Settings Button | `_settingsButton` |

On the top bar and XP bar: replace the background `Image` with **TranslucentImage**.

Add **FrostedHUDView** component to root.

#### `Assets/Resources/Views/Demo3_Notifications.prefab` — NotificationView
Anchored top-stretch, height ~340. Slides down from off-screen.

| Element | Field |
|:---|:---|
| CanvasGroup (root) | `_canvasGroup` |
| RectTransform (root) | `_panelRect` |
| Close Button | `_closeButton` |
| "Mark all read" Button | `_clearButton` |
| List container (VertLayoutGroup) | `_container` |

**NotifRow prefab** (nested):
Horizontal row: icon TMP_Text, title TMP_Text, body TMP_Text, time TMP_Text.
Add **NotifRow** component. Assign to **_rowPrefab** on NotificationView.

Add **NotificationView** component to root.
Replace background Image with **TranslucentImage**.

#### `Assets/Resources/Views/Demo3_Settings.prefab` — SettingsView
Anchored bottom-stretch, height ~420. Slides up from off-screen.

| Element | Source | Field |
|:---|:---|:---|
| CanvasGroup (root) | | `_canvasGroup` |
| RectTransform (root) | | `_panelRect` |
| Close Button | OneUI CloseButton | `_closeButton` |
| Master Slider | OneUI BlueSlider | `_masterSlider` |
| Master % TMP_Text | TMP_Text | `_masterLabel` |
| Music Slider | OneUI GreenSlider | `_musicSlider` |
| Music % TMP_Text | TMP_Text | `_musicLabel` |
| Fullscreen Toggle | OneUI BasicToggle | `_fullscreenToggle` |
| FPS Toggle | OneUI ToggleVariant | `_fpsToggle` |

Replace background Image with **TranslucentImage**.
Add **SettingsView** component to root.

### Shared ViewModel demo
`Demo3Starter` creates one `FrostedHUDViewModel` and passes it to all three windows.
This means:
- Dismissing all notifications (`_clearButton`) → `UnreadCount.Value = 0` → HUD badge disappears immediately, even though the HUD and the notification tray are separate GameObjects on different canvases.
- Moving the Master Volume slider in Settings → the VM value updates → if you subscribe to it in HUD, HUD reflects it without any event bus.

---

## File Tree (final)

```
Assets/MaquiDemos/
├── Scripts/
│   ├── MaquiDemos.asmdef
│   ├── Demo1_Gallery/
│   │   ├── GalleryCommands.cs
│   │   ├── GalleryViewModel.cs
│   │   ├── GalleryView.cs
│   │   └── Demo1Starter.cs
│   ├── Demo2_GameUI/
│   │   ├── GameUICommands.cs
│   │   ├── HUDViewModel.cs
│   │   ├── HUDView.cs
│   │   ├── InventoryViewModel.cs
│   │   ├── InventoryView.cs
│   │   ├── InventoryItemRow.cs
│   │   ├── ShopViewModel.cs
│   │   ├── ShopView.cs
│   │   ├── ShopItemRow.cs
│   │   └── Demo2Starter.cs
│   └── Demo3_Frosted/
│       ├── FrostedCommands.cs
│       ├── FrostedHUDViewModel.cs
│       ├── FrostedHUDView.cs
│       ├── NotificationView.cs
│       ├── NotifRow.cs
│       ├── SettingsView.cs
│       └── Demo3Starter.cs
├── Scenes/            ← create empty Unity scenes here
│   ├── Demo1_Gallery.unity
│   ├── Demo2_GameUI.unity
│   └── Demo3_FrostedHUD.unity
└── SETUP.md           ← this file

Assets/Resources/
├── Views/             ← save prefabs here (ResourcesAssetProvider loads from here)
├── Themes/            ← Theme_Dark.asset, Theme_Light.asset  (optional for Demo 1)
└── Icons/             ← item sprites for Demo 2 rows  (optional — rows still show without them)
```

---

## Common mistakes

| Issue | Fix |
|:---|:---|
| "Could not load prefab: 'Views/…'" | Prefab not in `Assets/Resources/Views/` |
| Scripts don't appear in Add Component | asmdef compile error — check Console |
| `[Routes]` partial class error | VitalRouter.dll must be in precompiledReferences of MaquiDemos.asmdef |
| TranslucentImage shows nothing blurred | TranslucentImageSource not on the Camera |
| Modal mask doesn't appear | MaquiWindowManager not initialised — check Maqui_Core exists in Hierarchy at runtime |
| `destroyCancellationToken` compile error | Requires Unity 2022+; use `this.GetCancellationTokenOnDestroy()` on older versions |
