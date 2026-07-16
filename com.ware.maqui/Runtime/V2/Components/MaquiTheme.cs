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

namespace Maqui.V2.Components
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

        public const float CornerRadius = 5f;
        public const float PanelCornerRadius = 8f;

        /// <summary>Horizontal padding the backend applies to any container that
        /// carries a background. Lives here (not as a literal in the backend) so
        /// components can do inner-width math — e.g. the Toggle knob's travel —
        /// against the same number the backend actually applies.</summary>
        public const float ContainerPadding = 10f;
    }
}
