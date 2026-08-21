// SPDX-License-Identifier: MIT
// MaqUI v2 — Controls.Slider (WINDOW-LOOP WL.0: native-control swap).

namespace Maqui.Components
{
    public static partial class MaquiComponents
    {
        /// <summary>
        /// Horizontal slider. Returns the user's current value in <c>[min, max]</c>.
        ///
        /// <para><b>Backed by the host framework's own slider.</b> The backend mints a
        /// real UI Toolkit <c>Slider</c> and routes its value into
        /// <see cref="Gui.FloatInputs"/>; this component only records the desired
        /// value and reads back what the user did.</para>
        ///
        /// <para><b>Why it was rewritten:</b> the previous version drew a track plus a
        /// loose marker rect and tried to derive its value from pointer position. In
        /// Unity the pointer X was never actually read — the drag branch fell back to
        /// <c>animatedT * trackWidth</c>, i.e. it computed the value from the value it
        /// already had — so dragging could not move it, and the handle had nowhere to
        /// sit because the layout has no absolute positioning. Both problems vanish by
        /// delegating to the control the platform already ships, which is the standing
        /// rule for this framework: close gaps by forwarding, never by reimplementing.
        /// </para>
        /// </summary>
        /// <param name="key">Identity for the value slot; scoped by the current data scope.</param>
        public static float Slider(this Gui gui, string key, float value, float min, float max)
        {
            gui.SliderField(key, value, min, max);
            return gui.FloatInputs.Get(gui.InputKey(key, "slider"), value);
        }

        /// <summary>
        /// Pure helper: map a pointer X inside a track to a value in <c>[min, max]</c>,
        /// clamping out-of-range Xs to the endpoints.
        ///
        /// <para>Retained after the native swap because it is genuinely useful for
        /// custom value widgets and is covered by existing tests; the shipped
        /// <see cref="Slider"/> no longer needs it.</para>
        /// </summary>
        public static float ComputeSliderValue(float pointerX, float trackLeft, float trackWidth, float min, float max)
        {
            if (trackWidth <= 0f) return min;
            float t = (pointerX - trackLeft) / trackWidth;
            if (t < 0f) t = 0f;
            else if (t > 1f) t = 1f;
            return min + t * (max - min);
        }

        /// <summary>
        /// Dropdown / option picker. Returns the currently selected option.
        /// Native <c>DropdownField</c> under the hood — the platform owns the popup,
        /// keyboard navigation and focus, none of which Maqui should reimplement.
        /// </summary>
        public static string Dropdown(this Gui gui, string key, string selected, params string[] options)
        {
            gui.DropdownField(key, selected, options);
            return gui.TextInputs.Get(gui.InputKey(key, "dropdown"), selected ?? string.Empty);
        }
    }
}
