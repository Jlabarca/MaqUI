// SPDX-License-Identifier: MIT
// MaqUI v2 — Tabs (V2-PLAYER-UI VPU.3).
//
// Asserts the strip STRUCTURE (a Row of one Button per label), the active-class
// swap, content-callback invocation with the selected index, and — per the
// recording≠rendering lesson — that the strip materializes through the reconciler.

using System.Linq;
using Maqui;
using Maqui.Components;
using Xunit;

namespace Maqui.Tests
{
    public class TabsTests
    {
        private static readonly string[] Three = { "A", "B", "C" };

        [Fact]
        public void Tabs_EmitsOneButtonPerLabel()
        {
            var gui = new Gui();
            gui.BeginFrame();
            gui.Tabs("t", 0, Three);
            gui.EndFrame();

            // Each Button is a Column + a DrawText label; 3 tabs → 3 label texts.
            int texts = gui.Buffer.Ops.Count(o => o.Kind == FrameOpKind.DrawText);
            Assert.Equal(3, texts);
            Assert.Equal(FrameOpKind.RowBegin, gui.Buffer.Ops[0].Kind);   // the strip
        }

        [Fact]
        public void Tabs_ActiveTabGetsActiveClass()
        {
            var gui = new Gui();
            gui.BeginFrame();
            gui.Tabs("t", 1, Three, tabClassName: "tab", activeTabClassName: "tab--on");
            gui.EndFrame();

            // Button className lands on its Column container. Exactly one column
            // carries the active class (index 1); the others carry the base class.
            var colClasses = gui.Buffer.Ops
                .Where(o => o.Kind == FrameOpKind.ColumnBegin)
                .Select(o => o.ClassName)
                .ToList();
            Assert.Single(colClasses, c => c == "tab--on");
            Assert.Equal(2, colClasses.Count(c => c == "tab"));
        }

        [Fact]
        public void Tabs_NoClick_ReturnsActiveIndexUnchanged()
        {
            var gui = new Gui();
            gui.BeginFrame();
            int result = gui.Tabs("t", 2, Three);
            gui.EndFrame();
            Assert.Equal(2, result);
        }

        [Fact]
        public void Tabs_RendersContentForSelectedIndex()
        {
            int rendered = -1;
            var gui = new Gui();
            gui.BeginFrame();
            gui.Tabs("t", 1, Three, renderContent: i => rendered = i);
            gui.EndFrame();
            Assert.Equal(1, rendered);   // body callback runs with the active index
        }

        [Fact]
        public void Tabs_OutOfRangeIndexClampsToFirst()
        {
            int rendered = -1;
            var gui = new Gui();
            gui.BeginFrame();
            int result = gui.Tabs("t", 99, Three, renderContent: i => rendered = i);
            gui.EndFrame();
            Assert.Equal(0, result);
            Assert.Equal(0, rendered);
        }

        [Fact]
        public void Tabs_IsMaterializedByTheReconciler()
        {
            var gui = new Gui();
            var backend = new TestBackend();
            gui.BeginFrame();
            gui.Tabs("t", 0, Three);
            gui.EndFrame();
            gui.Render(backend);

            int created = backend.Events.Count(e => e.Kind == BackendEventKind.CreateElement);
            Assert.True(created >= 4, "strip row + 3 tab buttons must all be created");
        }

        [Fact]
        public void Tabs_EmptyLabels_IsNoOp()
        {
            var gui = new Gui();
            gui.BeginFrame();
            int result = gui.Tabs("t", 0, new string[0]);
            gui.EndFrame();
            Assert.Equal(0, result);
            Assert.Empty(gui.Buffer.Ops);
        }
    }
}
