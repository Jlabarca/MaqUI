// SPDX-License-Identifier: MIT
// MaqUI v2 — Bar + StatRow (V2-PRO-SKIN.1.4).
//
// Same discipline as ProgressBarStepperTests: assert the OP structure AND that
// the elements MATERIALIZE through the reconciler (recording is not rendering).

using System.Linq;
using Maqui.V2;
using Maqui.V2.Components;
using Xunit;

namespace Maqui.V2.Tests
{
    public class BarTests
    {
        [Fact]
        public void Bar_EmitsTrackRowWithFillChild()
        {
            var gui = new Gui();
            gui.BeginFrame();
            gui.Bar("hp", 0.5f, "50 / 100", className: "ro-bar", fillClassName: "f");
            gui.EndFrame();

            var kinds = gui.Buffer.Ops.Select(o => o.Kind).ToList();
            Assert.Equal(FrameOpKind.RowBegin, kinds[0]);
            Assert.Contains(FrameOpKind.Box, kinds);          // fill
            Assert.Equal(FrameOpKind.RowEnd, kinds[kinds.Count - 1]);
        }

        [Fact]
        public void Bar_DrawsCenteredValueLabel()
        {
            var gui = new Gui();
            gui.BeginFrame();
            gui.Bar("hp", 0.5f, "5798 / 5798", labelClassName: "ro-bar__label");
            gui.EndFrame();

            var text = gui.Buffer.Ops.FirstOrDefault(o => o.Kind == FrameOpKind.DrawText);
            Assert.Equal("5798 / 5798", text.Text);
        }

        [Fact]
        public void Bar_NullLabel_DrawsNoText()
        {
            var gui = new Gui();
            gui.BeginFrame();
            gui.Bar("hp", 0.5f, null);
            gui.EndFrame();

            Assert.DoesNotContain(gui.Buffer.Ops, o => o.Kind == FrameOpKind.DrawText);
        }

        [Theory]
        [InlineData(-0.5f, 0f)]
        [InlineData(1.5f, 1f)]
        [InlineData(0.3f, 0.3f)]
        public void Bar_ClampsFillToUnitRange(float input, float expected)
        {
            var gui = new Gui();
            gui.BeginFrame();
            gui.Bar("x", input, null, fillClassName: "f");
            gui.EndFrame();

            var fill = gui.Buffer.Ops.First(o => o.Kind == FrameOpKind.Box);
            Assert.Equal((float)SizeKind.Percentage, fill.FloatA);
            Assert.Equal(expected, fill.FloatB, 3);
        }

        [Fact]
        public void Bar_IsMaterializedByTheReconciler()
        {
            var gui = new Gui();
            var backend = new TestBackend();
            gui.BeginFrame();
            gui.Bar("hp", 0.5f, "50 / 100", className: "ro-bar", fillClassName: "f", labelClassName: "l");
            gui.EndFrame();
            gui.Render(backend);

            int created = backend.Events.Count(e => e.Kind == BackendEventKind.CreateElement);
            Assert.True(created >= 3, "track + fill + label must all be created");
        }
    }

    public class StatRowTests
    {
        [Fact]
        public void StatRow_NoStepper_ReturnsZeroDelta()
        {
            var gui = new Gui();
            gui.BeginFrame();
            int delta = gui.StatRow("str", "STR", "50");
            gui.EndFrame();
            Assert.Equal(0, delta);
        }

        [Fact]
        public void StatRow_EmitsLabelAndValue()
        {
            var gui = new Gui();
            gui.BeginFrame();
            gui.StatRow("atk", "ATK", "110");
            gui.EndFrame();

            var texts = gui.Buffer.Ops.Where(o => o.Kind == FrameOpKind.DrawText).Select(o => o.Text).ToList();
            Assert.Contains("ATK", texts);
            Assert.Contains("110", texts);
        }

        [Fact]
        public void StatRow_WithBonus_DrawsBonusText()
        {
            var gui = new Gui();
            gui.BeginFrame();
            gui.StatRow("str", "STR", "50", bonus: "+2", bonusClassName: "ro-bonus");
            gui.EndFrame();

            var texts = gui.Buffer.Ops.Where(o => o.Kind == FrameOpKind.DrawText).Select(o => o.Text).ToList();
            Assert.Contains("+2", texts);
        }

        [Fact]
        public void StatRow_EmptyBonus_DrawsNoBonusText()
        {
            var gui = new Gui();
            gui.BeginFrame();
            gui.StatRow("str", "STR", "50", bonus: "");
            gui.EndFrame();

            // Only label + value — no third text node for an empty bonus.
            int texts = gui.Buffer.Ops.Count(o => o.Kind == FrameOpKind.DrawText);
            Assert.Equal(2, texts);
        }

        [Fact]
        public void StatRow_WithStepper_EmitsStepperButtons()
        {
            var gui = new Gui();
            gui.BeginFrame();
            gui.StatRow("str", "STR", "50", showStepper: true, buttonClassName: "b");
            gui.EndFrame();

            // label + value + the stepper (two −/+ button labels + its own empty
            // value label) = 5 texts.
            int texts = gui.Buffer.Ops.Count(o => o.Kind == FrameOpKind.DrawText);
            Assert.Equal(5, texts);
        }

        [Fact]
        public void StatRow_IsMaterializedByTheReconciler()
        {
            var gui = new Gui();
            var backend = new TestBackend();
            gui.BeginFrame();
            gui.StatRow("str", "STR", "50", bonus: "+2", showStepper: true);
            gui.EndFrame();
            gui.Render(backend);

            int created = backend.Events.Count(e => e.Kind == BackendEventKind.CreateElement);
            Assert.True(created >= 4, "row + label + value + bonus must materialize");
        }
    }
}
