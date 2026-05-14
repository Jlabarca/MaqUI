// SPDX-License-Identifier: MIT
// MaqUI v2 — Sample: draggable handle with hover-scale animation.
//
// =====================================================================
// MAQUIV2.4 — BLIND DRAFT. Operator must compile in Unity to validate.
// =====================================================================
//
// Subclasses GuiDriver (the MonoBehaviour author of MAQUIV2.2.4 blind draft).
// Renders a 200x200 box that scales up on hover and follows the pointer
// while held + dragged. Exercises:
//   - gui.Animate(...) for the hover scale tween
//   - node.IsHovered() / node.IsActive() / node.OnDrag()
//   - data scope so position state survives reconcile
//
// Operator authors a Samples~/V2/DraggableHandle/DraggableHandle.unity scene
// with a UIDocument + this component on a GameObject. The .unity file must
// be authored in the Editor (binary YAML with GUIDs).

using UnityEngine;

namespace Maqui.V2.Samples
{
    /// <summary>
    /// <para><b>Status: blind draft.</b> Not validated outside Unity Editor.</para>
    /// </summary>
    public sealed class DraggableHandle : GuiDriver
    {
        private float _x;
        private float _y;

        protected override void BuildFrame(Gui gui)
        {
            // Hover-scale animation: target 1.1 when hovered, 1.0 otherwise.
            float targetScale = _hoverFlag ? 1.1f : 1.0f;
            float scale = gui.Animate("handle-scale", targetScale);

            using (gui.EnterDataScope("handle"))
            {
                var handle = gui.Box(
                    width: Size.Pixels(200f * scale),
                    height: Size.Pixels(200f * scale));

                // Read interaction flags ON THIS frame.
                _hoverFlag = handle.IsHovered();

                if (handle.OnDrag())
                {
                    // The adapter writes Active+ClickedThisFrame; we read
                    // queue Move events from the Gui's PointerEventQueue for
                    // delta — left as an operator exercise since the queue
                    // wiring on GuiDriver is itself a blind draft.
                }

                // Draw a square at (_x, _y) — actual transform would need a
                // proper layout hook; sample is a sketch only.
                gui.DrawRect(new Color32(80, 140, 220, 255),
                    Size.Pixels(200f * scale),
                    Size.Pixels(200f * scale));

                gui.DrawText($"({_x:0},{_y:0}) scale={scale:0.00} hover={handle.IsHovered()}");
            }
        }

        private bool _hoverFlag;

        private void Update()
        {
            // Tick animations once per Unity frame (separate from BuildFrame
            // which runs inside GuiDriver.LateUpdate).
            if (Gui != null) Gui.TickAnimations(Time.deltaTime);
        }
    }
}
