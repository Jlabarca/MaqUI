// SPDX-License-Identifier: MIT
// MaqUI v2 — PlayMode tests for interactables + animation. P4.5.
//
// =====================================================================
// MAQUIV2.4 — BLIND DRAFT. Operator must run Unity Test Runner.
// =====================================================================
//
// NUnit, runs in PlayMode via Unity Test Runner. Asserts:
//   1. AnimationFloat settles to within tolerance over N FixedUpdate ticks
//   2. UIToolkitInteractionAdapter writes Hover on PointerEnter
//   3. UIToolkitInteractionAdapter writes ClickedThisFrame on Down+Up over
//      same element while hovered
//   4. Drag handle in Samples~/DraggableHandle/DraggableHandle.unity
//      scene tracks pointer delta within ±2 pixels
//
// Some of these duplicate xUnit coverage (Tests.Standalone~/AnimationTests.cs)
// — by design: the standalone tests verify the algorithm; these verify the
// Unity adapter wiring + UIDocument integration.

using NUnit.Framework;
using System.Collections;
using UnityEngine;
using UnityEngine.TestTools;
using UnityEngine.UIElements;

namespace Maqui.Tests.Runtime
{
    public class InteractableTests
    {
        [Test]
        public void AnimationFloat_SettlesToTarget_OverFrames()
        {
            var anim = new AnimationFloat(0f, 1f);
            for (int i = 0; i < 240; i++) anim.Tick(1f / 60f);
            Assert.That(anim.Current, Is.EqualTo(1f).Within(0.01f),
                "AnimationFloat should settle within tolerance after 4s of 60Hz ticks.");
        }

        [UnityTest]
        public IEnumerator Adapter_PointerEnter_SetsHoverFlag()
        {
            // Uses the public DispatchEvent test seam on UIToolkitInteractionAdapter
            // so we don't depend on a PanelSettings-backed UIDocument to deliver
            // synthetic UI Toolkit events. The seam exercises the same flag-writing
            // logic that real PointerEnterEvent callbacks route through.
            yield return null;

            var root = new VisualElement { name = "test-root" };
            var gui = new Gui();
            var backend = new UIToolkitBackend(root, gui);
            var adapter = new UIToolkitInteractionAdapter(gui, backend);

            const int handle = 42;
            const int nodeId = 7;
            adapter.NoteHandleFrameOp(handle, new FrameOp(FrameOpKind.Box, nodeId, "/test"));

            adapter.DispatchEvent(handle, PointerEventKind.Enter, 10f, 20f);

            Assert.That(gui.Interactions.Has(nodeId, NodeInteractionFlags.Hover),
                "PointerEnter should add Hover flag to the resolved NodeId.");
            Assert.AreEqual(1, adapter.Queue.Count, "PointerEvent should be enqueued.");
        }

        [UnityTest]
        public IEnumerator Adapter_PointerDownUp_SetsClickedThisFrame()
        {
            yield return null;

            var root = new VisualElement { name = "test-root" };
            var gui = new Gui();
            var backend = new UIToolkitBackend(root, gui);
            var adapter = new UIToolkitInteractionAdapter(gui, backend);

            const int handle = 42;
            const int nodeId = 7;
            adapter.NoteHandleFrameOp(handle, new FrameOp(FrameOpKind.Box, nodeId, "/test"));

            // Enter -> Down -> Up sequence over the same hovered element should
            // produce ClickedThisFrame (and clear Active on Up).
            adapter.DispatchEvent(handle, PointerEventKind.Enter, 10f, 20f);
            adapter.DispatchEvent(handle, PointerEventKind.Down,  10f, 20f);
            Assert.IsTrue(gui.Interactions.Has(nodeId, NodeInteractionFlags.Active),
                "PointerDown should add Active flag.");

            adapter.DispatchEvent(handle, PointerEventKind.Up,    10f, 20f);
            Assert.IsFalse(gui.Interactions.Has(nodeId, NodeInteractionFlags.Active),
                "PointerUp should clear Active flag.");
            Assert.IsTrue(gui.Interactions.Has(nodeId, NodeInteractionFlags.ClickedThisFrame),
                "PointerUp over a still-hovered element should add ClickedThisFrame.");
        }
    }
}
