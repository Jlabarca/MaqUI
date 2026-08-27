// SPDX-License-Identifier: MIT
// MaqUI v2 — MaquiTheme. Default palette + corner radius for stock components,
// harvested from the V1 (UGUI) MaquiSettingsWindow prefab so V2 windows read as
// the same design language by default instead of flat placeholder colors.
//
// Source values (RebuildClient/Assets/Resources/UI/MaquiSettingsWindow.prefab):
//   panel background : Color(0.18, 0.18, 0.22, 1)
//   body text         : Color(0.75, 0.75, 0.75, 1)
//   header/accent text: Color(0.35, 0.65, 1, 1)

using UnityEngine;

namespace Maqui.Components
{
    /// <summary>Stock default colors + corner radius used by <see cref="MaquiComponents"/>.</summary>
    public static class MaquiTheme
    {
        public static readonly Color32 PanelBackground = new Color32(46, 46, 56, 255);
        public static readonly Color32 TextPrimary = new Color32(191, 191, 191, 255);
        public static readonly Color32 TextAccent = new Color32(89, 166, 255, 255);

        /// <summary>Recessed well for scrollable content areas — a step darker
        /// than <see cref="PanelBackground"/> so a scroll region reads as inset
        /// rather than as more panel.</summary>
        public static readonly Color32 PanelInset = new Color32(34, 35, 43, 255);

        /// <summary>Hairline separator (e.g. under a panel header).</summary>
        public static readonly Color32 Separator = new Color32(70, 72, 86, 255);

        public static readonly Color32 ButtonBase = new Color32(58, 60, 72, 255);
        public static readonly Color32 ButtonHover = new Color32(72, 75, 90, 255);
        public static readonly Color32 ButtonActive = new Color32(38, 40, 50, 255);

        // Toggle pill. "On" borrows the accent blue so an enabled toggle reads as
        // the same affordance as an accent header, rather than a stock green that
        // appears nowhere else in the palette.
        public static readonly Color32 ToggleOn = new Color32(89, 166, 255, 255);
        public static readonly Color32 ToggleOnHover = new Color32(122, 186, 255, 255);
        public static readonly Color32 ToggleOff = new Color32(58, 60, 72, 255);
        public static readonly Color32 ToggleOffHover = new Color32(72, 75, 90, 255);
        public static readonly Color32 ToggleKnob = new Color32(245, 245, 250, 255);

        // ---- Text input chrome (MaquiComponents.TextInput / Gui.TextInputField).
        // A stock UI Toolkit TextField paints white-on-black with a 12px font and
        // carries its own margins, so a caller-set fixed height clipped the glyphs
        // and left the box reading as mostly empty white. These tokens are what the
        // backend paints instead: an inset well matching the panel, a legible font,
        // and only as much padding as the text actually needs.
        public static readonly Color32 InputBackground = new Color32(28, 29, 36, 255);
        public static readonly Color32 InputText = new Color32(232, 234, 240, 255);
        public static readonly Color32 InputBorder = new Color32(70, 72, 86, 255);
        public static readonly Color32 InputBorderFocus = new Color32(89, 166, 255, 255);

        /// <summary>Default height of a text input. Sized off
        /// <see cref="InputFontSize"/> + <see cref="InputPaddingVertical"/> — a
        /// taller box only adds the blank space this replaced.</summary>
        public const float InputHeight = 30f;
        public const float InputFontSize = 15f;
        public const float InputPaddingHorizontal = 8f;
        public const float InputPaddingVertical = 2f;

        public const float CornerRadius = 5f;
        public const float PanelCornerRadius = 8f;

        /// <summary>Horizontal padding the backend applies to any container that
        /// carries a background. Lives here (not as a literal in the backend) so
        /// components can do inner-width math — e.g. the Toggle knob's travel —
        /// against the same number the backend actually applies.</summary>
        public const float ContainerPadding = 10f;
    }
}
