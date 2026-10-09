// SPDX-License-Identifier: MIT
// MaqUI v2 — Controls.TextInput.
//
// Records a TextInputField FrameOp; UIToolkitBackend swaps in a
// UnityEngine.UIElements.TextField + routes value-changed callbacks to
// gui.TextInputs keyed by ScopePath+key. The component reads from the
// store to surface the latest typed value back to the caller. Pure C#
// (no Unity types in this file); headlessly testable.

namespace Maqui.Components
{
    public static partial class MaquiComponents
    {
        /// <summary>
        /// Editable text input. Returns the latest typed value (from
        /// <see cref="Gui.TextInputs"/>, populated by the UI Toolkit
        /// TextField the backend creates). First-time callers get
        /// <paramref name="text"/> echoed back; subsequent calls reflect
        /// keystrokes the user has typed.
        /// </summary>
        public static string TextInput(this Gui gui, string key, string text,
            float height = MaquiTheme.InputHeight)
        {
            // Records a TextInputField FrameOp; UIToolkitBackend swaps in a
            // UnityEngine.UIElements.TextField + routes value changes to
            // gui.TextInputs keyed by ScopePath+key. We surface the latest
            // typed-text value back to the caller via TextInputs.Get.
            gui.TextInputField(key, text, height);
            string storeKey = MaquiStrings.Scoped(gui.CurrentScopePath ?? "/", key ?? "text");
            return gui.TextInputs.Get(storeKey, text ?? string.Empty);
        }
    }
}
