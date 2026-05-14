// SPDX-License-Identifier: MIT
// MaqUI v2 — Controls.TextInput.
//
// =====================================================================
// MAQUIV2.8 — BLIND DRAFT. Operator must compile in Unity to validate.
// =====================================================================
//
// References UnityEngine.UIElements.TextField + needs keyboard event
// routing through the UI Toolkit panel. Headless-impractical: text
// editing semantics (cursor, selection, IME) are owned by Unity's
// TextField widget. Excluded from the standalone csproj.

using UnityEngine;
using UnityEngine.UIElements;

namespace Maqui.V2.Components
{
    public static partial class MaquiComponents
    {
        /// <summary>
        /// Editable text input. The current <paramref name="text"/> value is
        /// rendered; the backend's TextInputAdapter (Unity-side, not shipped
        /// in this blind draft) routes keyboard input into the returned
        /// string value.
        ///
        /// <para>v0 path: the Unity adapter creates a real
        /// <see cref="TextField"/> as the underlying VisualElement (the
        /// backend's <c>CreateElement</c> recognizes a new FrameOpKind for
        /// text inputs — added as part of the operator's wire-up work
        /// alongside this file). The component method records the value
        /// query op; the adapter reports back the typed text on the next
        /// frame's <see cref="Gui.BeginFrame"/>.</para>
        ///
        /// <para><b>Status: blind draft.</b> Not validated outside Unity Editor.</para>
        /// </summary>
        public static string TextInput(this Gui gui, string key, string text)
        {
            // Placeholder: render the current text as a flat label inside a
            // bordered Box. The real implementation will swap this for a
            // TextField-backed element via a new FrameOpKind.
            var box = gui.Box(height: Size.Pixels(28f));
            gui.DrawRect(new Color32(40, 40, 50, 255), height: Size.Pixels(28f));
            gui.DrawText(text ?? string.Empty, new Color32(230, 230, 230, 255));

            // Until the adapter is wired, the returned value is the input
            // unchanged. Operator's TODO: read updated text from a side-
            // channel keyed by `key + "-text"` (analogous to ScrollView's
            // pending-delta slot).
            return text ?? string.Empty;
        }
    }
}
