// SPDX-License-Identifier: MIT
// MaqUI v2 — floating, cursor-anchored tooltip overlay (V2-UI-PARITY.3.4).
//
// MaquiComponents.Tooltip (Components/MaquiComponents.ItemSlot.cs) is a
// *placed* panel occupying real layout space where the caller draws it — not
// a floating overlay, and its own doc comment says so explicitly. This is the
// deferred floating variant, modeled on ORO's ItemDragGhostV2 (an always-
// on-top UIDocument with PickingMode.Ignore, polling Input.mousePosition each
// frame) — the one absolute-positioning precedent in the consuming repo.
//
// Cannot copy that precedent verbatim: ItemDragGhostV2 hardcodes an ORO
// Resources path for its PanelSettings, which is correct for a file that
// lives in ORO but meaningless in this repo (MaqUI is repo-agnostic, `file:`-
// consumed, no notion of a host project's Resources folder). Init() takes an
// injected PanelSettings instead — same shape as UIToolkitBackend's own
// constructor already taking injected IImageLoader/ISpriteLoader. The host
// project wires the injection (see ORO's TooltipOverlayBootstrap.cs).
//
// References UnityEngine.UIElements + MonoBehaviour — cannot be headlessly
// tested (same ceiling as UIToolkitBackend/GuiDriver); excluded from
// Tests.Standalone~/Maqui.V2.Tests.csproj.

using UnityEngine;
using UnityEngine.UIElements;
using Object = UnityEngine.Object;

namespace Maqui.V2
{
    /// <summary>
    /// Static façade over a lazily-created, <c>DontDestroyOnLoad</c> singleton
    /// that renders a single floating tooltip label, cursor-anchored, above
    /// every other panel content sharing the injected <see cref="PanelSettings"/>.
    /// </summary>
    public static class TooltipOverlay
    {
        private static TooltipOverlayDriver _instance;
        private static PanelSettings _settings;
        private static int _sortingOrder = 9000;
        private static bool _warnedMissingInit;
        private static StyleSheet[] _styleSheets;
        private static string _rootClass;

        /// <summary>
        /// Wires the <see cref="PanelSettings"/> every tooltip renders into. Call once
        /// at startup (see ORO's <c>TooltipOverlayBootstrap</c>). <paramref name="sortingOrder"/>
        /// defaults to 9000 — below ItemDragGhostV2's 10000, so a drag ghost is never
        /// occluded by a stray tooltip.
        ///
        /// <para><paramref name="styleSheets"/> and <paramref name="rootClass"/> exist
        /// because this overlay lives in its OWN <see cref="UIDocument"/>, and UI Toolkit
        /// stylesheets do not cross panel boundaries. A host whose look is defined in a
        /// stylesheet loaded onto its window roots (ORO: <c>ro-theme.uss</c> on each
        /// <c>RoWindow.Root</c>) gets a completely UNSTYLED tooltip without this — the
        /// class name passed to <see cref="Show"/> resolves against nothing. Worse, a
        /// theme built on custom properties needs the theme CLASS on this root too, or
        /// every <c>var()</c> resolves empty even once the sheet is present. Both halves
        /// are required; passing one without the other still renders unstyled.</para>
        /// </summary>
        /// <param name="styleSheets">Sheets to attach to the overlay's root, or null.</param>
        /// <param name="rootClass">A class applied to the overlay root — typically the
        /// host's theme class, under which its custom properties are declared. Swap it
        /// later with <see cref="SetRootClass"/>.</param>
        public static void Init(PanelSettings settings, int sortingOrder = 9000,
            StyleSheet[] styleSheets = null, string rootClass = null)
        {
            _settings = settings;
            _sortingOrder = sortingOrder;
            _styleSheets = styleSheets;
            _rootClass = rootClass;
            // Re-apply to an already-created instance so a late Init (or a re-Init after
            // a scene change) is not silently ignored.
            if (_instance != null) _instance.ApplyRootStyling(_styleSheets, _rootClass);
        }

        /// <summary>Swap the class on the overlay root — for a live theme toggle, so the
        /// popover recolours with the windows instead of keeping the theme it booted
        /// with. No-op if unchanged; safe before the overlay exists.</summary>
        public static void SetRootClass(string rootClass)
        {
            if (_rootClass == rootClass) return;
            _rootClass = rootClass;
            if (_instance != null) _instance.ApplyRootStyling(null, _rootClass);
        }

        /// <summary>Shows <paramref name="text"/> at the current cursor position, with
        /// an edge-flip if it would overflow the panel's right edge. A no-op (with a
        /// one-time warning) if <see cref="Init"/> was never called.</summary>
        public static void Show(string text, string className = null)
        {
            var d = EnsureInstance();
            if (d == null) return;
            d.ShowInternal(null, text, className, null, null);
        }

        /// <summary>
        /// Two-part variant: a title line over a body block, both inside one styled
        /// container. Prefer this for item/skill descriptions — a single Label cannot
        /// carry two text styles, and the host almost always wants the name emphasised
        /// against a dimmer body (ORO's `.ro-tooltip__title` / `.ro-tooltip__body`).
        ///
        /// <para><paramref name="containerClassName"/> is where the panel look belongs
        /// (background, border, padding, max-width). Text properties inherit in UI
        /// Toolkit, so a container that sets colour/font-size/white-space covers both
        /// labels without either needing a class of its own.</para>
        /// </summary>
        public static void Show(string title, string body, string containerClassName,
            string titleClassName = null, string bodyClassName = null)
        {
            var d = EnsureInstance();
            if (d == null) return;
            d.ShowInternal(title, body, containerClassName, titleClassName, bodyClassName);
        }

        /// <summary>Hides the tooltip. Safe to call even if nothing is showing.</summary>
        public static void Hide()
        {
            if (_instance != null) _instance.HideInternal();
        }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetStatics() => _instance = null;

        private static TooltipOverlayDriver EnsureInstance()
        {
            if (_instance != null) return _instance;

            if (_settings == null)
            {
                // A missing tooltip is easy to not notice for a while (unlike a missing
                // drag ghost, which is instantly obvious — no cursor icon during a drag).
                // One-time warning, not silent, per V2-UI-PARITY.3 Decision 4.
                if (!_warnedMissingInit)
                {
                    Debug.LogWarning("Maqui.V2.TooltipOverlay: Show() called before Init(PanelSettings) — no tooltip will render.");
                    _warnedMissingInit = true;
                }
                return null;
            }

            var go = new GameObject("[MaqUI:TooltipOverlay]");
            Object.DontDestroyOnLoad(go);
            var doc = go.AddComponent<UIDocument>();
            doc.panelSettings = _settings;
            doc.sortingOrder = _sortingOrder;

            var driver = go.AddComponent<TooltipOverlayDriver>();
            driver.Init(doc);
            driver.ApplyRootStyling(_styleSheets, _rootClass);
            _instance = driver;
            return _instance;
        }

        [DefaultExecutionOrder(9000)]
        private sealed class TooltipOverlayDriver : MonoBehaviour
        {
            private VisualElement _root;
            /// <summary>The positioned, styled panel. Was a bare Label until the
            /// two-part Show() landed; a Label cannot carry two text styles, and the
            /// container is also the natural owner of the background/border/max-width
            /// the caller's class describes.</summary>
            private VisualElement _panelBox;
            private Label _title;
            private Label _body;
            private string _appliedRootClass;

            public void Init(UIDocument doc)
            {
                _root = doc.rootVisualElement;
                _root.pickingMode = PickingMode.Ignore;
                _root.style.position = Position.Absolute;
                _root.style.left = 0; _root.style.top = 0; _root.style.right = 0; _root.style.bottom = 0;

                _panelBox = new VisualElement { name = "TooltipOverlay", pickingMode = PickingMode.Ignore };
                _panelBox.style.position = Position.Absolute;
                _panelBox.style.display = DisplayStyle.None;
                // Auto-sizes to content — the "measured auto-height" legacy's tooltip has
                // and MaquiComponents.Tooltip (a placed panel) cannot: an absolutely
                // positioned element with no explicit width/height just hugs its content.

                _title = new Label { name = "TooltipOverlayTitle", pickingMode = PickingMode.Ignore };
                _body = new Label { name = "TooltipOverlayBody", pickingMode = PickingMode.Ignore };
                _panelBox.Add(_title);
                _panelBox.Add(_body);
                _root.Add(_panelBox);
            }

            /// <summary>Attach host stylesheets and/or set the host theme class on the
            /// overlay ROOT. Null <paramref name="sheets"/> leaves existing sheets alone
            /// (so a theme-only swap doesn't re-add them); duplicate sheets are skipped.</summary>
            public void ApplyRootStyling(StyleSheet[] sheets, string rootClass)
            {
                if (_root == null) return;

                if (sheets != null)
                {
                    foreach (var sheet in sheets)
                    {
                        if (sheet == null || _root.styleSheets.Contains(sheet)) continue;
                        _root.styleSheets.Add(sheet);
                    }
                }

                if (_appliedRootClass == rootClass) return;
                if (!string.IsNullOrEmpty(_appliedRootClass)) _root.RemoveFromClassList(_appliedRootClass);
                if (!string.IsNullOrEmpty(rootClass)) _root.AddToClassList(rootClass);
                _appliedRootClass = rootClass;
            }

            public void ShowInternal(string title, string body, string containerClassName,
                string titleClassName, string bodyClassName)
            {
                ApplyLabel(_title, title, titleClassName);
                ApplyLabel(_body, body, bodyClassName);

                _panelBox.ClearClassList();
                if (!string.IsNullOrEmpty(containerClassName)) _panelBox.AddToClassList(containerClassName);
                _panelBox.style.display = DisplayStyle.Flex;
                Reposition();
            }

            /// <summary>Empty text collapses the label entirely rather than leaving a
            /// blank line — the single-string Show() passes no title, and an empty Label
            /// still contributes its line-height and padding to the box.</summary>
            private static void ApplyLabel(Label label, string text, string className)
            {
                bool has = !string.IsNullOrEmpty(text);
                label.style.display = has ? DisplayStyle.Flex : DisplayStyle.None;
                label.text = has ? text : string.Empty;
                label.ClearClassList();
                if (has && !string.IsNullOrEmpty(className)) label.AddToClassList(className);
            }

            public void HideInternal()
            {
                if (_panelBox != null) _panelBox.style.display = DisplayStyle.None;
            }

            private void LateUpdate()
            {
                if (_panelBox == null || _panelBox.style.display == DisplayStyle.None) return;
                Reposition();
            }

            private void Reposition()
            {
                var panel = _panelBox.panel;
                if (panel == null) return;

                // Same technique as ItemDragGhostV2.LateUpdate: UI Toolkit runtime panels
                // are top-left/y-down; Input.mousePosition is bottom-left/y-up.
                Vector2 screenPos = Input.mousePosition;
                screenPos.y = Screen.height - screenPos.y;
                Vector2 panelPos = RuntimePanelUtils.ScreenToPanel(panel, screenPos);

                float offsetX = 16f, offsetY = 12f;
                float x = panelPos.x + offsetX;
                float y = panelPos.y + offsetY;

                // Edge-flip: if the box would overflow the panel's right edge, anchor
                // its right edge to the cursor instead of its left. resolvedStyle can lag
                // one layout pass behind a same-frame style write, so a very fast mouse
                // move near the edge may show one frame un-flipped before self-correcting
                // — acceptable at human perception speed, a known limitation, not swept.
                float boxWidth = _panelBox.resolvedStyle.width;
                float boxHeight = _panelBox.resolvedStyle.height;
                float panelWidth = panel.visualTree.resolvedStyle.width;
                float panelHeight = panel.visualTree.resolvedStyle.height;

                if (!float.IsNaN(boxWidth) && boxWidth > 0f && !float.IsNaN(panelWidth)
                    && x + boxWidth > panelWidth)
                {
                    x = panelPos.x - offsetX - boxWidth;
                }

                // Vertical flip too. A one-line tooltip rarely needed it, but a two-part
                // item popover with a wrapped description is tall enough to run off the
                // bottom of the panel when the cursor is in the lower rows of a bag — and
                // unlike the horizontal case there is no scrollback to reach it.
                if (!float.IsNaN(boxHeight) && boxHeight > 0f && !float.IsNaN(panelHeight)
                    && y + boxHeight > panelHeight)
                {
                    y = panelPos.y - offsetY - boxHeight;
                }

                // Never let a flip push the box off the opposite edge (a cursor near the
                // left/top with a wide box): clamping loses the cursor offset, which is
                // strictly better than rendering off-panel.
                if (x < 0f) x = 0f;
                if (y < 0f) y = 0f;

                _panelBox.style.left = x;
                _panelBox.style.top = y;
            }
        }
    }
}
