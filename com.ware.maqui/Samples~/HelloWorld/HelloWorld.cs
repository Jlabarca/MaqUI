// SPDX-License-Identifier: MIT
// MaqUI v2 — HelloWorld sample. Smallest possible v2 component (P2.5 target).
//
// =====================================================================
// MAQUIV2.2 — BLIND DRAFT. Operator must compile in Unity to validate
// AND must author HelloWorld.unity scene in Editor (binary YAML).
// =====================================================================
//
// Pair with HelloWorldRef.uxml/cs (hand-coded UI Toolkit equivalent) for the
// P2.6 parity test.

using UnityEngine;

namespace Maqui.Samples.HelloWorld
{
    /// <summary>
    /// Attach this to a GameObject with a <c>UIDocument</c>. v2 renders a
    /// red rectangle containing the text "Hello, world!" centered inside a
    /// column.
    ///
    /// <para><b>Status: blind draft.</b> Scene not authored.</para>
    /// </summary>
    public sealed class HelloWorld : GuiDriver
    {
        protected override void BuildUI(Gui gui)
        {
            gui.Column();
                gui.DrawRect(new Color32(180, 32, 32, 255), Size.Pixels(200), Size.Pixels(80));
                gui.DrawText("Hello, world!", new Color32(255, 255, 255, 255), fontSize: 20f);
            gui.EndColumn();
        }
    }
}
