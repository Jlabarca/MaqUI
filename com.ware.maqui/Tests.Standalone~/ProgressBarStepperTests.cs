// SPDX-License-Identifier: MIT
// MaqUI v2 — ProgressBar + Stepper (V2-PLAYER-UI VPU.1).
//
// These assert the OP structure and — critically, per the Slider/Dropdown
// lesson — that the elements actually MATERIALIZE through the reconciler.
// Recording is not rendering.

using System.Linq;
using Maqui.V2;
using Maqui.V2.Components;
using Xunit;

namespace Maqui.V2.Tests
{
    public class ProgressBarTests
    {
        [Fact]
        public void ProgressBar_EmitsTrackRowWithFillChild()
        {
            var gui = new Gui();
            gui.BeginFrame();
            gui.ProgressBar("hp", 0.5f, className: "ro-bar", fillClassName: "ro-bar__fill");
            gui.EndFrame();

            var kinds = gui.Buffer.Ops.Select(o => o.Kind).ToList();
            Assert.Equal(FrameOpKind.RowBegin, kinds[0]);   // track
            Assert.Contains(FrameOpKind.Box, kinds);         // fill
            Assert.Equal(FrameOpKind.RowEnd, kinds[kinds.Count - 1]);
        }

        [Fact]
        public void ProgressBar_FillWidthIsPercentageOfValue()
        {
            var gui = new Gui();
            gui.BeginFrame();
            gui.ProgressBar("hp", 0.42f, fillClassName: "f");
            gui.EndFrame();

            var fill = gui.Buffer.Ops.First(o => o.Kind == FrameOpKind.Box);
            Assert.Equal((float)SizeKind.Percentage, fill.FloatA);   // width kind
            Assert.Equal(0.42f, fill.FloatB, 3);                     // width value
        }

        [Theory]
        [InlineData(-0.5f, 0f)]
        [InlineData(1.5f, 1f)]
        [InlineData(0.3f, 0.3f)]
        public void ProgressBar_ClampsValueToUnitRange(float input, float expected)
        {
            var gui = new Gui();
            gui.BeginFrame();
            gui.ProgressBar("x", input, fillClassName: "f");
            gui.EndFrame();

            var fill = gui.Buffer.Ops.First(o => o.Kind == FrameOpKind.Box);
            Assert.Equal(expected, fill.FloatB, 3);
        }

        [Fact]
        public void ProgressBar_IsMaterializedByTheReconciler()
        {
            var gui = new Gui();
            var backend = new TestBackend();
            gui.BeginFrame();
            gui.ProgressBar("hp", 0.5f, className: "ro-bar", fillClassName: "ro-bar__fill");
            gui.EndFrame();
            gui.Render(backend);

            int created = backend.Events.Count(e => e.Kind == BackendEventKind.CreateElement);
            Assert.True(created >= 2, "track + fill must both be created");
        }
    }

    public class StepperTests
    {
        [Fact]
        public void Stepper_NoClick_ReturnsZeroDelta()
        {
            var gui = new Gui();
            gui.BeginFrame();
            int delta = gui.Stepper("str", "12");
            gui.EndFrame();
            Assert.Equal(0, delta);
        }

        [Fact]
        public void Stepper_EmitsTwoButtonsAndALabel()
        {
            var gui = new Gui();
            gui.BeginFrame();
            gui.Stepper("str", "12", buttonClassName: "b", labelClassName: "l");
            gui.EndFrame();

            // Each Button is a Column+DrawText; the stepper adds its own value
            // DrawText between them. Two −/+ labels + one value label = 3 texts.
            int texts = gui.Buffer.Ops.Count(o => o.Kind == FrameOpKind.DrawText);
            Assert.Equal(3, texts);
        }
    }
}
