// SPDX-License-Identifier: MIT
// MaqUI v2 — Controls.ScrollView.

using System;
using UnityEngine;

namespace Maqui.V2.Components
{
    public static partial class MaquiComponents
    {
        /// <summary>
        /// Vertically scrollable region. Content is drawn by the supplied
        /// <paramref name="drawContent"/> delegate inside a clipped Column.
        /// The scroll offset (in pixels, 0 = top) is stored on
        /// <see cref="Gui.Animations"/> keyed by <paramref name="key"/> so it
        /// survives reconcile.
        ///
        /// <para>Visual clipping (<c>style.overflow = Hidden</c>) is set by
        /// the Unity-side backend on the wrapping Box when it sees this
        /// component's data scope. The headless math (offset clamping +
        /// hit-at-boundary detection) is fully unit-testable; the visual
        /// clipping is a single-line backend style toggle the operator
        /// confirms in Editor.</para>
        ///
        /// <para>Wheel-event deltas are read from <see cref="Gui.Animations"/>
        /// slot <c>key + "-pending-delta"</c>, which the Unity adapter writes
        /// from queued <see cref="PointerEvent"/>s of kind Move when ctrl/wheel
        /// is held. v0 alpha: the adapter side is left as part of P8.5's
        /// Unity tail; headless callers can poke the slot directly.</para>
        /// </summary>
        public static float ScrollView(
            this Gui gui,
            string key,
            float contentHeight,
            float viewportHeight,
            Action drawContent)
        {
            // Read pending wheel delta from animations store (Unity adapter writes; v0 alpha).
            var pendingSlot = key + "-pending-delta";
            float delta = gui.Animations.Get(pendingSlot).Target;
            // Consume the delta — set target back to 0 so it doesn't accumulate next frame.
            if (delta != 0f)
            {
                gui.Animations.Set(pendingSlot, new AnimationFloat(0f, 0f));
            }

            // Read current offset (persistent across frames).
            var offsetSlot = key + "-offset";
            float current = gui.Animations.Get(offsetSlot).Target;
            float newOffset = ComputeScrollOffset(current, delta, contentHeight, viewportHeight);

            // Persist the new offset.
            gui.Animations.Set(offsetSlot, new AnimationFloat(newOffset, newOffset));

            using (gui.EnterDataScope(key))
            {
                var clipBox = gui.Box(
                    width: default,
                    height: Size.Pixels(viewportHeight));
                gui.DrawRect(new Color32(0, 0, 0, 0)); // transparent — clipping is what matters

                gui.Column();
                {
                    // Spacer to offset content by newOffset px (negative offset = scroll down = content moves up).
                    gui.Spacer(Size.Pixels(-newOffset));
                    drawContent?.Invoke();
                }
                gui.EndColumn();
            }

            return newOffset;
        }

        /// <summary>
        /// Pure helper: clamp <paramref name="current"/> + <paramref name="delta"/>
        /// to <c>[0, max(0, contentHeight - viewportHeight)]</c>. Returns the
        /// new offset. When content fits the viewport, offset stays at 0.
        ///
        /// <para>Exposed as <c>public static</c> so xUnit can validate without
        /// a <see cref="Gui"/>.</para>
        /// </summary>
        public static float ComputeScrollOffset(float current, float delta, float contentHeight, float viewportHeight)
        {
            float maxOffset = contentHeight - viewportHeight;
            if (maxOffset < 0f) maxOffset = 0f;
            float next = current + delta;
            if (next < 0f) next = 0f;
            else if (next > maxOffset) next = maxOffset;
            return next;
        }
    }
}
