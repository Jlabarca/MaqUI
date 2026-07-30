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

        /// <summary>
        /// Wires the <see cref="PanelSettings"/> every tooltip renders into. Call once
        /// at startup (see ORO's <c>TooltipOverlayBootstrap</c>). <paramref name="sortingOrder"/>
        /// defaults to 9000 — below ItemDragGhostV2's 10000, so a drag ghost is never
        /// occluded by a stray tooltip.
        /// </summary>
        public static void Init(PanelSettings settings, int sortingOrder = 9000)
        {
            _settings = settings;
            _sortingOrder = sortingOrder;
        }

        /// <summary>Shows <paramref name="text"/> at the current cursor position, with
        /// an edge-flip if it would overflow the panel's right edge. A no-op (with a
        /// one-time warning) if <see cref="Init"/> was never called.</summary>
        public static void Show(string text, string className = null)
        {
            var d = EnsureInstance();
            if (d == null) return;
            d.ShowInternal(text, className);
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
            _instance = driver;
            return _instance;
        }

        [DefaultExecutionOrder(9000)]
        private sealed class TooltipOverlayDriver : MonoBehaviour
        {
            private VisualElement _root;
            private Label _label;

            public void Init(UIDocument doc)
            {
                _root = doc.rootVisualElement;
                _root.pickingMode = PickingMode.Ignore;
                _root.style.position = Position.Absolute;
                _root.style.left = 0; _root.style.top = 0; _root.style.right = 0; _root.style.bottom = 0;

                _label = new Label { name = "TooltipOverlayLabel", pickingMode = PickingMode.Ignore };
                _label.style.position = Position.Absolute;
                _label.style.display = DisplayStyle.None;
                // Auto-sizes to content — the "measured auto-height" legacy's tooltip has
                // and MaquiComponents.Tooltip (a placed panel) cannot: an absolutely
                // positioned Label with no explicit width/height just hugs its text.
                _root.Add(_label);
            }

            public void ShowInternal(string text, string className)
            {
                _label.text = text ?? string.Empty;
                _label.ClearClassList();
                if (!string.IsNullOrEmpty(className)) _label.AddToClassList(className);
                _label.style.display = DisplayStyle.Flex;
                Reposition();
            }

            public void HideInternal()
            {
                if (_label != null) _label.style.display = DisplayStyle.None;
            }

            private void LateUpdate()
            {
                if (_label == null || _label.style.display == DisplayStyle.None) return;
                Reposition();
            }

            private void Reposition()
            {
                var panel = _label.panel;
                if (panel == null) return;

                // Same technique as ItemDragGhostV2.LateUpdate: UI Toolkit runtime panels
                // are top-left/y-down; Input.mousePosition is bottom-left/y-up.
                Vector2 screenPos = Input.mousePosition;
                screenPos.y = Screen.height - screenPos.y;
                Vector2 panelPos = RuntimePanelUtils.ScreenToPanel(panel, screenPos);

                float offsetX = 16f, offsetY = 12f;
                float x = panelPos.x + offsetX;
                float y = panelPos.y + offsetY;

                // Edge-flip: if the label would overflow the panel's right edge, anchor
                // its right edge to the cursor instead of its left. resolvedStyle can lag
                // one layout pass behind a same-frame style write, so a very fast mouse
                // move near the edge may show one frame un-flipped before self-correcting
                // — acceptable at human perception speed, a known limitation, not swept.
                float labelWidth = _label.resolvedStyle.width;
                float panelWidth = panel.visualTree.resolvedStyle.width;
                bool haveMeasurements = !float.IsNaN(labelWidth) && labelWidth > 0f && !float.IsNaN(panelWidth);
                if (haveMeasurements && x + labelWidth > panelWidth)
                {
                    x = panelPos.x - offsetX - labelWidth;
                }

                _label.style.left = x;
                _label.style.top = y;
            }
        }
    }
}
