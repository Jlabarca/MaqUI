// SPDX-License-Identifier: MIT
// MaqUI v2 — Controls.Toggle.

using UnityEngine;

namespace Maqui.V2.Components
{
    public static partial class MaquiComponents
    {
        // Sizing history: 80x32/knob24 originally; V2-UI-FEEL P5 shrank it to
        // 44x20/knob16 because stacked settings rows read as one fused blue bar.
        // That fixed the fusing but broke the MOTION, which is the operator's
        // follow-up complaint: the backend forces ContainerPadding (10px) on each
        // side of any background-carrying container, so a 44px pill has only
        // 44 - 20 - 16 = **8px** of knob travel. The knob barely moved, which reads
        // as a broken control rather than a small one.
        //
        // 52x22 restores 52 - 20 - 16 = 16px of travel — double the slide — while
        // staying far closer to the P5 size than the original 80px, so the row
        // fusing P5 fixed does not come back (the 6px row spacer added alongside it
        // is what actually separates the rows).
        private const float TogglePillWidth = 52f;
        private const float TogglePillHeight = 22f;
        private const float ToggleKnobSize = 16f;

        // A pill wants a circular knob. DrawRect gets MaquiTheme.CornerRadius (5f),
        // which on a 16px square reads as a rounded box sliding in a slot; DrawCircle
        // sets width/height AND a full corner radius from one value, so the knob is
        // actually round. Radius, not diameter — hence the halving.
        private const float ToggleKnobRadius = ToggleKnobSize * 0.5f;

        // Snappier than the spring defaults: a toggle should arrive decisively rather
        // than drift. Higher stiffness with damping just under critical gives a quick
        // slide with a barely-perceptible settle instead of a visible wobble.
        private const float ToggleStiffness = 520f;
        private const float ToggleDamping = 34f;

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
            float t = gui.Animate(key + "-toggle-t", state ? 1f : 0f,
                ToggleStiffness, ToggleDamping);

            int pillId = gui.PeekNextNodeId();
            var pill = gui.Row(
                Size.Pixels(TogglePillWidth),
                Size.Pixels(TogglePillHeight),
                background: ToggleTint(gui, pillId, state),
                alignItems: AlignItems.Center); // vertically centers the knob in the pill
            {
                gui.Spacer(Size.Pixels(ComputeKnobOffset(t)));
                gui.DrawCircle(MaquiTheme.ToggleKnob, ToggleKnobRadius);
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
