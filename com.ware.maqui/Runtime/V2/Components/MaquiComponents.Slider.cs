// SPDX-License-Identifier: MIT
// MaqUI v2 — Controls.Slider.

using UnityEngine;

namespace Maqui.V2.Components
{
    public static partial class MaquiComponents
    {
        /// <summary>
        /// Horizontal slider returning a new value in <c>[min, max]</c> based on
        /// pointer position over the track. While the handle isn't being
        /// dragged, the current <paramref name="value"/> is returned unchanged.
        ///
        /// <para>The drag-tracking happens via the standard
        /// <see cref="NodeInteractions.OnDrag"/> path — pointer X in local
        /// space is read from the queued pointer events; in this v0 the
        /// pointer position is supplied via <paramref name="pointerXOverride"/>
        /// for headless testability. Unity adapter will fill this from the
        /// queued <c>PointerEvent.X</c> when dragging.</para>
        /// </summary>
        public static float Slider(
            this Gui gui,
            string key,
            float value,
            float min,
            float max,
            float trackWidth = 240f,
            float pointerXOverride = float.NaN)
        {
            var track = gui.Box(
                width: Size.Pixels(trackWidth),
                height: Size.Pixels(6f));
            gui.DrawRect(new Color32(80, 80, 90, 255), Size.Pixels(trackWidth), Size.Pixels(6f));

            // Handle (visual only at v0; position is value-mapped client-side).
            float t = max > min ? (value - min) / (max - min) : 0f;
            t = t < 0f ? 0f : (t > 1f ? 1f : t);
            float animatedT = gui.Animate(key + "-handle-t", t);

            // Draw handle as a small box at the mapped position.
            // (Layout system doesn't support absolute positioning yet; the Unity
            // backend reads the handle's normalized t from gui.Animations to
            // place it correctly. v0 emits a marker DrawRect.)
            gui.DrawRect(new Color32(220, 220, 230, 255), Size.Pixels(20f), Size.Pixels(20f));

            // Apply drag if active OR if caller forced a pointer X (headless tests).
            bool active = track.IsActive() || !float.IsNaN(pointerXOverride);
            if (active)
            {
                float px = float.IsNaN(pointerXOverride)
                    ? animatedT * trackWidth
                    : pointerXOverride;
                value = ComputeSliderValue(px, 0f, trackWidth, min, max);
            }

            return value;
        }

        /// <summary>
        /// Pure helper: map a pointer X coordinate inside a track to a value in
        /// <c>[min, max]</c>. Clamps out-of-range pointer Xs to the endpoints.
        ///
        /// <para>Exposed as <c>public static</c> so xUnit can validate the
        /// mapping math without spinning up a <see cref="Gui"/>.</para>
        /// </summary>
        public static float ComputeSliderValue(float pointerX, float trackLeft, float trackWidth, float min, float max)
        {
            if (trackWidth <= 0f) return min;
            float t = (pointerX - trackLeft) / trackWidth;
            if (t < 0f) t = 0f;
            else if (t > 1f) t = 1f;
            return min + t * (max - min);
        }
    }
}
