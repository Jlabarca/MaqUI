// SPDX-License-Identifier: MIT
// MaqUI v2 — Controls.Toggle.

using UnityEngine;

namespace Maqui.V2.Components
{
    public static partial class MaquiComponents
    {
        // V2-UI-FEEL P5: shrunk from 80x32/knob24 — at the old size, stacked
        // settings rows (Sprite filtering, Monster HP bars, ...) had almost no
        // gap between pills and read as one fused blue bar. Smaller pill, same
        // proportions, leaves room for row spacing to actually separate them.
        private const float TogglePillWidth = 44f;
        private const float TogglePillHeight = 20f;
        private const float ToggleKnobSize = 16f;

        /// <summary>
        /// Pill-shaped on/off toggle. Returns the (possibly flipped) state
        /// after processing this frame's pointer interactions. When clicked,
        /// the state flips and the knob slides.
        ///
        /// <para><b>Container, not sibling-rect</b> — same structure (and same
        /// reason) as <see cref="Button"/>. The pill used to be a
        /// <see cref="Gui.Box"/> with the visible body + knob drawn as SIBLINGS.
        /// Box is leaf-only — it doesn't push a reconciler parent — so pointer
        /// events landing on the visible rects bubbled past the Box to the outer
        /// container and the pill's <c>OnClick()</c> never fired. The pill is now
        /// a real <see cref="Gui.Row"/> carrying the body color, with the knob
        /// nested inside it.</para>
        ///
        /// <para>Animation slot uses <paramref name="key"/> + suffix so multiple
        /// toggles on the same screen coexist.</para>
        /// </summary>
        public static bool Toggle(this Gui gui, string key, bool state)
        {
            // Knob position animates between t=0 (off) and t=1 (on). Ticked by
            // Gui.TickAnimations; the slide is realized as a leading Spacer whose
            // width is the interpolated offset (see ComputeKnobOffset).
            float t = gui.Animate(key + "-toggle-t", state ? 1f : 0f);

            int pillId = gui.PeekNextNodeId();
            var pill = gui.Row(
                Size.Pixels(TogglePillWidth),
                Size.Pixels(TogglePillHeight),
                background: ToggleTint(gui, pillId, state),
                alignItems: AlignItems.Center); // vertically centers the knob in the pill
            {
                gui.Spacer(Size.Pixels(ComputeKnobOffset(t)));
                gui.DrawRect(MaquiTheme.ToggleKnob, Size.Pixels(ToggleKnobSize), Size.Pixels(ToggleKnobSize));
            }
            gui.EndRow();

            if (pill.OnClick())
            {
                state = !state;
            }
            return state;
        }

        /// <summary>
        /// Pure helper: leading-spacer width that places the knob at animation
        /// position <paramref name="t"/> (0 = off/left, 1 = on/right). Travel is
        /// the pill's inner width — the backend eats
        /// <see cref="MaquiTheme.ContainerPadding"/> on each side of any
        /// background-carrying container — minus the knob. Clamped so an
        /// overshooting spring can't push the knob outside the pill.
        ///
        /// <para>Exposed as <c>public static</c> so xUnit can validate the
        /// travel math without spinning up a <see cref="Gui"/>.</para>
        /// </summary>
        public static float ComputeKnobOffset(float t)
        {
            float travel = TogglePillWidth - (2f * MaquiTheme.ContainerPadding) - ToggleKnobSize;
            if (travel <= 0f) return 0f;
            if (t < 0f) t = 0f;
            else if (t > 1f) t = 1f;
            return t * travel;
        }

        /// <summary>Pill body color for the on/off state, with a hover lift.</summary>
        private static Color32 ToggleTint(Gui gui, int nodeId, bool state)
        {
            bool hovered = (gui.Interactions.GetFlags(nodeId) & NodeInteractionFlags.Hover) != 0;
            if (state) return hovered ? MaquiTheme.ToggleOnHover : MaquiTheme.ToggleOn;
            return hovered ? MaquiTheme.ToggleOffHover : MaquiTheme.ToggleOff;
        }
    }
}
