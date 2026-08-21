// SPDX-License-Identifier: MIT
// MaqUI v2 — EquipSlot (V2-PRO-SKIN.2.2).
//
// Asserts the OP structure, the empty-state "—", and that the tile materializes
// through the reconciler (recording is not rendering).

using System.Linq;
using Maqui;
using Maqui.Components;
using Xunit;

namespace Maqui.Tests
{
    public class EquipSlotTests
    {
        [Fact]
        public void EquipSlot_DrawsSlotLabelAndItemName()
        {
            var gui = new Gui();
            gui.BeginFrame();
            gui.EquipSlot("head", "HEAD TOP", "icon_hat", "Hat");
            gui.EndFrame();

            var texts = gui.Buffer.Ops.Where(o => o.Kind == FrameOpKind.DrawText).Select(o => o.Text).ToList();
            Assert.Contains("HEAD TOP", texts);
            Assert.Contains("Hat", texts);
        }

        [Fact]
        public void EquipSlot_EmptyItem_RendersDash()
        {
            var gui = new Gui();
            gui.BeginFrame();
            gui.EquipSlot("acc", "ACCESSORY", null, null);
            gui.EndFrame();

            var texts = gui.Buffer.Ops.Where(o => o.Kind == FrameOpKind.DrawText).Select(o => o.Text).ToList();
            Assert.Contains("—", texts);
        }

        [Fact]
        public void EquipSlot_EmptyItem_DrawsNoIcon()
        {
            var gui = new Gui();
            gui.BeginFrame();
            gui.EquipSlot("acc", "ACCESSORY", null, null);
            gui.EndFrame();

            Assert.DoesNotContain(gui.Buffer.Ops, o => o.Kind == FrameOpKind.DrawImage);
        }

        [Fact]
        public void EquipSlot_WithIcon_DrawsIcon()
        {
            var gui = new Gui();
            gui.BeginFrame();
            gui.EquipSlot("head", "HEAD TOP", "icon_hat", "Hat");
            gui.EndFrame();

            Assert.Contains(gui.Buffer.Ops, o => o.Kind == FrameOpKind.DrawImage);
        }

        [Fact]
        public void EquipSlot_IsMaterializedByTheReconciler()
        {
            var gui = new Gui();
            var backend = new TestBackend();
            gui.BeginFrame();
            gui.EquipSlot("head", "HEAD TOP", "icon_hat", "Hat",
                className: "ro-pro-equipslot", iconClassName: "ro-pro-inset");
            gui.EndFrame();
            gui.Render(backend);

            int created = backend.Events.Count(e => e.Kind == BackendEventKind.CreateElement);
            Assert.True(created >= 4, "row + icon tile + label + name must materialize");
        }
    }
}
