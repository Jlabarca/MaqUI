// SPDX-License-Identifier: MIT
// MaqUI v2 — PropDiffTests (NUnit, Maqui.Tests.Runtime).
//
// FRAME-BUDGET.2.5: UIToolkitBackend.ApplyProps skips its style writes when the
// incoming FrameOp is field-for-field identical to the last one applied to that
// element (Maqui.FrameBudgetFlags.PropDiff, default on). These tests drive the
// backend directly (CreateElement/UpdateElement/Recycle), bypassing Gui/the
// reconciler, so the op sequence per element is exact and reproducible — no
// dependence on how the reconciler happens to key nodes across frames.

using NUnit.Framework;
using UnityEngine;
using UnityEngine.UIElements;

namespace Maqui.Tests.Runtime
{
    public class PropDiffTests
    {
        private VisualElement _root;
        private UIToolkitBackend _backend;
        private bool _savedPropDiff;

        [SetUp]
        public void SetUp()
        {
            _root = new VisualElement { name = "test-root" };
            _backend = new UIToolkitBackend(_root);
            _savedPropDiff = FrameBudgetFlags.PropDiff;
        }

        [TearDown]
        public void TearDown()
        {
            // The flag is a package-wide static; never leak a test's override into the
            // next test (or into a real play-mode session sharing the same domain).
            FrameBudgetFlags.PropDiff = _savedPropDiff;
            _root?.RemoveFromHierarchy();
        }

        [Test]
        public void IdenticalRepeatedOp_SkipsWrite_WhenPropDiffOn()
        {
            FrameBudgetFlags.PropDiff = true;

            var op = new FrameOp(FrameOpKind.DrawText, nodeId: 1, scopePath: "/a",
                a: 20f, color: new Color32(255, 255, 255, 255), text: "Hello");
            int handle = _backend.CreateElement(in op);
            _backend.SetParent(handle, _backend.Root, 0);

            var label = (Label)_root[0];
            // A sentinel value the real op never writes (white != green) — if the next,
            // identical UpdateElement call actually re-runs the write path, this gets
            // stomped back to white.
            label.style.color = new StyleColor(Color.green);

            _backend.UpdateElement(handle, in op); // field-for-field identical op

            Assert.AreEqual(Color.green, (Color)label.style.color.value,
                "identical op should have skipped the color write, leaving the sentinel in place");
        }

        [Test]
        public void IdenticalRepeatedOp_StillWrites_WhenPropDiffOff()
        {
            FrameBudgetFlags.PropDiff = false;

            var op = new FrameOp(FrameOpKind.DrawText, nodeId: 1, scopePath: "/a",
                a: 20f, color: new Color32(255, 255, 255, 255), text: "Hello");
            int handle = _backend.CreateElement(in op);
            _backend.SetParent(handle, _backend.Root, 0);

            var label = (Label)_root[0];
            label.style.color = new StyleColor(Color.green);

            _backend.UpdateElement(handle, in op); // field-for-field identical op

            Assert.AreEqual(Color.white, (Color)label.style.color.value,
                "with the lever off every op re-runs the full write path regardless of diff");
        }

        [Test]
        public void ChangedProp_WritesTheNewValue()
        {
            FrameBudgetFlags.PropDiff = true;

            var opWhite = new FrameOp(FrameOpKind.DrawText, nodeId: 1, scopePath: "/a",
                a: 20f, color: new Color32(255, 255, 255, 255), text: "Hello");
            int handle = _backend.CreateElement(in opWhite);
            _backend.SetParent(handle, _backend.Root, 0);

            var opBlue = new FrameOp(FrameOpKind.DrawText, nodeId: 1, scopePath: "/a",
                a: 20f, color: new Color32(0, 0, 255, 255), text: "Hello");
            _backend.UpdateElement(handle, in opBlue);

            var label = (Label)_root[0];
            Assert.AreEqual(Color.blue, (Color)label.style.color.value,
                "a genuinely changed prop must still be written");
        }

        [Test]
        public void RerentedElement_StartsClean_NotStaleFromPreviousTenant()
        {
            FrameBudgetFlags.PropDiff = true;

            // Box ops pool back into the FrameOpKind.Box bucket on Recycle (unlike
            // Label/TextField/ScrollView, which drop on the floor).
            var opRed = new FrameOp(FrameOpKind.Box, nodeId: 1, scopePath: "/a",
                color: new Color32(255, 0, 0, 255));
            int handleA = _backend.CreateElement(in opRed);
            _backend.SetParent(handleA, _backend.Root, 0);

            var rented = _root[0];
            Assert.AreEqual(Color.red, (Color)rented.style.backgroundColor.value);

            _backend.Recycle(handleA);

            var opBlue = new FrameOp(FrameOpKind.Box, nodeId: 2, scopePath: "/b",
                color: new Color32(0, 0, 255, 255));
            int handleB = _backend.CreateElement(in opBlue);
            _backend.SetParent(handleB, _backend.Root, 0);

            var rerented = _root[0];
            Assert.AreSame(rented, rerented,
                "sanity check: the pool should have handed back the same instance");
            Assert.AreEqual(Color.blue, (Color)rerented.style.backgroundColor.value,
                "a re-rented element must apply its first op in full, never skip because " +
                "of a stale last-tenant's FrameOp");
        }
    }
}
