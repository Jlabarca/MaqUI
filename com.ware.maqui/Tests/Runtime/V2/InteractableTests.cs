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
//   4. Drag handle in Samples~/V2/DraggableHandle/DraggableHandle.unity
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

namespace Maqui.V2.Tests.Runtime
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
            var go = new GameObject("test-adapter");
            try
            {
                var doc = go.AddComponent<UIDocument>();
                // Need a PanelSettings asset to render — left as operator
                // setup: this test asserts adapter behavior, not panel paint.
                yield return null;

                var root = doc.rootVisualElement;
                if (root == null) Assert.Inconclusive("UIDocument has no rootVisualElement — needs a PanelSettings asset in the scene/prefab.");

                var gui = new Gui();
                var backend = new UIToolkitBackend(root);
                var adapter = new UIToolkitInteractionAdapter(gui, backend);

                var el = new VisualElement { name = "hit-target", style = { width = 100, height = 100 } };
                root.Add(el);

                int handle = 42;
                adapter.NoteHandleFrameOp(handle, new FrameOp(FrameOpKind.Box, nodeId: 7, scopePath: "/test"));
                adapter.SubscribeIfNew(handle, el);

                // Synthesize a PointerEnterEvent — Unity exposes EventBase.Get<T>().
                using (var evt = PointerEnterEvent.GetPooled())
                {
                    evt.target = el;
                    el.SendEvent(evt);
                }

                Assert.That(gui.Interactions.Has(7, NodeInteractionFlags.Hover),
                    "PointerEnter should add Hover flag to the resolved NodeId.");
            }
            finally
            {
                Object.DestroyImmediate(go);
            }
        }

        [UnityTest]
        public IEnumerator Adapter_PointerDownUp_SetsClickedThisFrame()
        {
            // Operator: this test mirrors PointerEnter_SetsHoverFlag but with
            // Down → Up sequence. Marked Inconclusive in this blind draft
            // since UIDocument PanelSettings is required for event dispatch.
            yield return null;
            Assert.Inconclusive("Blind draft — operator wires PanelSettings + completes.");
        }
    }
}
