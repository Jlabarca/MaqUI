// SPDX-License-Identifier: MIT
// MaqUI v2 — Controls.Button.

using UnityEngine;

namespace Maqui.V2.Components
{
    public static partial class MaquiComponents
    {
        /// <summary>
        /// Click-to-trigger button. Returns <c>true</c> on the frame the
        /// button is released (via <see cref="NodeInteractions.OnClick"/>).
        ///
        /// <para><b>Container, not sibling-rect.</b> The button's own
        /// <see cref="Gui.Column(Size,Size,Color32)"/> carries the background
        /// (via the Column-background feature) and the label is a real CHILD
        /// of it — not a separate sibling <see cref="Gui.DrawRect"/>. This
        /// matters for hit-testing: only <c>Row</c>/<c>Column</c>/<c>ClipBox</c>
        /// push a new parent in the reconciler (<c>Box</c> is leaf-only), so a
        /// sibling label's pointer events bubble straight past an invisible
        /// Box to the outer container — the Box's <c>OnClick()</c> then never
        /// fires for a real click landing on the visible text. Confirmed live:
        /// Down/Up always fired on the label's handle and the root's handle,
        /// never on the Box's. Nesting the label inside the clickable
        /// container guarantees bubbling reaches it.</para>
        ///
        /// <para>Hover/press tint works despite the background color being an
        /// argument to the very call that mints the container's Node: the flags
        /// are read up-front via <see cref="Gui.PeekNextNodeId"/>. See that
        /// method for why peeking is sound.</para>
        ///
        /// <para>Optional <paramref name="key"/> is the animation slot identity
        /// for future hover-scale tween work; unused at v0.</para>
        /// </summary>
        public static bool Button(this Gui gui, string label, string key = null)
        {
            var container = gui.Column(default, Size.Pixels(32f),
                background: ButtonTint(gui, gui.PeekNextNodeId()));
            gui.DrawText(label ?? string.Empty, MaquiTheme.TextPrimary);
            gui.EndColumn();
            return container.OnClick();
        }

        /// <summary>Base/hover/press background for a button-like container.
        /// Press wins over hover — a pointer held down is always also hovering.</summary>
        internal static Color32 ButtonTint(Gui gui, int nodeId)
        {
            var flags = gui.Interactions.GetFlags(nodeId);
            if ((flags & NodeInteractionFlags.Active) != 0) return MaquiTheme.ButtonActive;
            if ((flags & NodeInteractionFlags.Hover) != 0) return MaquiTheme.ButtonHover;
            return MaquiTheme.ButtonBase;
        }
    }
}
