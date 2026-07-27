// SPDX-License-Identifier: MIT
// MaqUI v2 — UnitFrame + Hotbar (V2-PRO-SKIN.4.3).
//
// Asserts bar composition, slot count / numbering, and reconciler
// materialization (recording is not rendering).

using System.Linq;
using Maqui.V2;
using Maqui.V2.Components;
using Xunit;

namespace Maqui.V2.Tests
{
    public class UnitFrameTests
    {
        [Fact]
        public void UnitFrame_DrawsNameAndSub()
        {
            var gui = new Gui();
            gui.BeginFrame();
            gui.UnitFrame("self", "face", "Adventurer", "Lv. 99", 1f, "5798 / 5798", 1f, "228 / 228");
            gui.EndFrame();

            var texts = gui.Buffer.Ops.Where(o => o.Kind == FrameOpKind.DrawText).Select(o => o.Text).ToList();
            Assert.Contains("Adventurer", texts);
            Assert.Contains("Lv. 99", texts);
        }

        [Fact]
        public void UnitFrame_ComposesTwoBars()
        {
            var gui = new Gui();
            gui.BeginFrame();
            gui.UnitFrame("self", "face", "Adventurer", "Lv. 99", 0.5f, "hp", 0.5f, "sp",
                barClassName: "ro-pro-bar", hpFillClassName: "hp", spFillClassName: "sp");
            gui.EndFrame();

            // Each Bar is a Row with a Box fill; two Bars → at least two fill Boxes
            // whose width is a Percentage.
            int pctBoxes = gui.Buffer.Ops.Count(o => o.Kind == FrameOpKind.Box
                && o.FloatA == (float)SizeKind.Percentage);
            Assert.True(pctBoxes >= 2, "HP + SP bar fills must both be present");
        }

        [Fact]
        public void UnitFrame_IsMaterializedByTheReconciler()
        {
            var gui = new Gui();
            var backend = new TestBackend();
            gui.BeginFrame();
            gui.UnitFrame("self", "face", "Adventurer", "Lv. 99", 1f, "hp", 1f, "sp",
                className: "ro-pro-unit", portraitClassName: "ro-pro-inset");
            gui.EndFrame();
            gui.Render(backend);

            int created = backend.Events.Count(e => e.Kind == BackendEventKind.CreateElement);
            Assert.True(created >= 5, "frame + portrait + name + two bars must materialize");
        }
    }

    public class HotbarTests
    {
        [Fact]
        public void Hotbar_DefaultLabels_AreRoQuickbar()
        {
            var gui = new Gui();
            gui.BeginFrame();
            gui.Hotbar("hb", 10, _ => null);
            gui.EndFrame();

            var texts = gui.Buffer.Ops.Where(o => o.Kind == FrameOpKind.DrawText).Select(o => o.Text).ToList();
            Assert.Equal("1", texts[0]);
            Assert.Equal("9", texts[8]);
            Assert.Equal("0", texts[9]);   // slot 10 → "0"
        }

        [Fact]
        public void Hotbar_RendersOneColumnPerSlot()
        {
            var gui = new Gui();
            gui.BeginFrame();
            gui.Hotbar("hb", 10, _ => null, slotClassName: "ro-slot");
            gui.EndFrame();

            // One ColumnBegin per slot (the outer container is a Row, not a Column).
            int slots = gui.Buffer.Ops.Count(o => o.Kind == FrameOpKind.ColumnBegin);
            Assert.Equal(10, slots);
        }

        [Fact]
        public void Hotbar_NoClick_ReturnsMinusOne()
        {
            var gui = new Gui();
            gui.BeginFrame();
            int clicked = gui.Hotbar("hb", 10, _ => null);
            gui.EndFrame();
            Assert.Equal(-1, clicked);
        }

        [Fact]
        public void Hotbar_DrawsIconWhenKeyProvided()
        {
            var gui = new Gui();
            gui.BeginFrame();
            gui.Hotbar("hb", 3, i => i == 0 ? "skill_bash" : null);
            gui.EndFrame();

            int icons = gui.Buffer.Ops.Count(o => o.Kind == FrameOpKind.DrawImage);
            Assert.Equal(1, icons);
        }

        [Fact]
        public void Hotbar_IsMaterializedByTheReconciler()
        {
            var gui = new Gui();
            var backend = new TestBackend();
            gui.BeginFrame();
            gui.Hotbar("hb", 10, _ => null, className: "ro-pro-hotbar", slotClassName: "ro-slot");
            gui.EndFrame();
            gui.Render(backend);

            int created = backend.Events.Count(e => e.Kind == BackendEventKind.CreateElement);
            Assert.True(created >= 11, "row + 10 slots must materialize");
        }
    }
}
