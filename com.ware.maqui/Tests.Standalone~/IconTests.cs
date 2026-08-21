// SPDX-License-Identifier: MIT
// MaqUI v2 — Image / Icon (V2-PLAYER-UI VPU.2).
//
// Per the Slider/Dropdown lesson (recording ≠ rendering) these assert BOTH
// that the DrawImage op carries the right key + size AND that it materializes
// through the reconciler — the leaf-list omission that silently dropped
// earlier controls would fail the last test here.

using System.Linq;
using Maqui;
using Maqui.Components;
using Xunit;

namespace Maqui.Tests
{
    public class IconTests
    {
        [Fact]
        public void DrawImage_RecordsKeyAndPixelSize()
        {
            var gui = new Gui();
            gui.BeginFrame();
            gui.DrawImage("Apple", 40f, 24f);
            gui.EndFrame();

            var op = gui.Buffer.Ops.First(o => o.Kind == FrameOpKind.DrawImage);
            Assert.Equal("Apple", op.Text);
            Assert.Equal((float)SizeKind.Pixels, op.FloatA);  // width kind
            Assert.Equal(40f, op.FloatB, 3);                  // width value
            Assert.Equal((float)SizeKind.Pixels, op.FloatC);  // height kind
            Assert.Equal(24f, op.FloatD, 3);                  // height value
        }

        [Fact]
        public void Icon_IsSquare_AtRequestedSize()
        {
            var gui = new Gui();
            gui.BeginFrame();
            gui.Icon("Red_Potion", 32f);
            gui.EndFrame();

            var op = gui.Buffer.Ops.First(o => o.Kind == FrameOpKind.DrawImage);
            Assert.Equal("Red_Potion", op.Text);
            Assert.Equal(32f, op.FloatB, 3);   // width
            Assert.Equal(32f, op.FloatD, 3);   // height == width → square
        }

        [Fact]
        public void Icon_NullKey_StillRecordsAnOp()
        {
            // A missing key must not throw and must still emit a sized box so the
            // slot's layout is stable (the backend paints a placeholder / nothing).
            var gui = new Gui();
            gui.BeginFrame();
            gui.Icon(null, 32f);
            gui.EndFrame();

            var op = gui.Buffer.Ops.First(o => o.Kind == FrameOpKind.DrawImage);
            Assert.Equal(string.Empty, op.Text);   // DrawImage normalizes null → ""
            Assert.Equal(32f, op.FloatB, 3);
        }

        [Fact]
        public void Icon_IsMaterializedByTheReconciler()
        {
            var gui = new Gui();
            var backend = new TestBackend();
            gui.BeginFrame();
            gui.Icon("Apple", 32f);
            gui.EndFrame();
            gui.Render(backend);

            var created = backend.Events.Where(e => e.Kind == BackendEventKind.CreateElement).ToList();
            Assert.Contains(created, e => e.OpKind == FrameOpKind.DrawImage && e.Text == "Apple");
        }
    }
}
