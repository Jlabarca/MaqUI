// SPDX-License-Identifier: MIT
// MaqUI v2 — HidePriority.IsVisible tests (V2-UI-PARITY.3.7, headless).

using Maqui;
using Xunit;

namespace Maqui.Tests
{
    public class HidePriorityTests
    {
        [Fact]
        public void PriorityZero_AlwaysVisible()
        {
            Assert.True(HidePriority.IsVisible(availableWidth: 0f, priority: 0));
            Assert.True(HidePriority.IsVisible(availableWidth: 10000f, priority: 0));
        }

        [Fact]
        public void NegativePriority_AlwaysVisible()
        {
            Assert.True(HidePriority.IsVisible(availableWidth: 0f, priority: -1));
        }

        [Fact]
        public void AboveBreakpoint_Visible()
        {
            Assert.True(HidePriority.IsVisible(500f, priority: 1, breakpoints: new float[] { 400f }));
        }

        [Fact]
        public void AtBreakpoint_Visible()
        {
            Assert.True(HidePriority.IsVisible(400f, priority: 1, breakpoints: new float[] { 400f }));
        }

        [Fact]
        public void BelowBreakpoint_Hidden()
        {
            Assert.False(HidePriority.IsVisible(399f, priority: 1, breakpoints: new float[] { 400f }));
        }

        [Fact]
        public void MidTier_UsesCorrectBreakpointIndex()
        {
            var breakpoints = new float[] { 300f, 500f, 700f };
            // priority 2 -> breakpoints[1] == 500f
            Assert.True(HidePriority.IsVisible(500f, priority: 2, breakpoints: breakpoints));
            Assert.False(HidePriority.IsVisible(499f, priority: 2, breakpoints: breakpoints));
        }

        [Fact]
        public void PriorityBeyondBreakpointsLength_FailsOpen_Visible()
        {
            // A caller adding a new tier without updating its breakpoint table should
            // not silently lose content.
            Assert.True(HidePriority.IsVisible(0f, priority: 5, breakpoints: new float[] { 400f }));
        }

        [Fact]
        public void NullBreakpoints_PositivePriority_FailsOpen_Visible()
        {
            Assert.True(HidePriority.IsVisible(0f, priority: 1, breakpoints: null));
        }

        [Fact]
        public void EmptyBreakpoints_PositivePriority_FailsOpen_Visible()
        {
            Assert.True(HidePriority.IsVisible(0f, priority: 1));
        }
    }
}
