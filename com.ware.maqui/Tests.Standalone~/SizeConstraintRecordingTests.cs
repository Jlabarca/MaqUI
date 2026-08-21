// SPDX-License-Identifier: MIT
// MaqUI v2 — proves Gui.Row/Column/Box forward Size.Min/Max into the recorded
// FrameOp (V2-UI-PARITY.3.7, headless). This is the one part of 3.1 that IS
// reachable by dotnet test — the backend-application half needs the Unity
// Editor (UIToolkitBackend.cs is excluded from this project).

using Maqui;
using Xunit;

namespace Maqui.Tests
{
    public class SizeConstraintRecordingTests
    {
        private readonly Gui _gui;

        public SizeConstraintRecordingTests()
        {
            _gui = new Gui();
            _gui.BeginFrame();
        }

        [Fact]
        public void Row_ForwardsWidthMinMaxAndHeightMax()
        {
            _gui.Row(Size.Expand().WithMinMax(100f, 300f), Size.Fit().WithMax(80f));

            var op = _gui.Buffer.Ops[0];
            Assert.Equal(FrameOpKind.RowBegin, op.Kind);
            Assert.Equal(100f, op.WidthMin);
            Assert.Equal(300f, op.WidthMax);
            Assert.Equal(80f, op.MaxHeight);
        }

        [Fact]
        public void Row_ForwardsHeightMin()
        {
            _gui.Row(Size.Fit(), Size.Expand().WithMin(50f));

            var op = _gui.Buffer.Ops[0];
            Assert.Equal(50f, op.HeightMin);
        }

        [Fact]
        public void Column_ForwardsWidthMinMaxAndHeightMax()
        {
            _gui.Column(Size.Expand().WithMinMax(120f, 320f), Size.Fit().WithMax(90f));

            var op = _gui.Buffer.Ops[0];
            Assert.Equal(FrameOpKind.ColumnBegin, op.Kind);
            Assert.Equal(120f, op.WidthMin);
            Assert.Equal(320f, op.WidthMax);
            Assert.Equal(90f, op.MaxHeight);
        }

        [Fact]
        public void Box_ForwardsWidthMinMaxAndHeightMaxCarriesHeightMax()
        {
            var box = _gui.Box(Size.Pixels(64f).WithMin(20f), Size.Fit().WithMax(40f));

            var op = _gui.Buffer.Ops[0];
            Assert.Equal(FrameOpKind.Box, op.Kind);
            Assert.Equal(20f, op.WidthMin);
            Assert.Equal(40f, op.MaxHeight);
        }

        [Fact]
        public void Row_WithNoConstraints_RecordsZero()
        {
            _gui.Row(Size.Expand(), Size.Fit());

            var op = _gui.Buffer.Ops[0];
            Assert.Equal(0f, op.WidthMin);
            Assert.Equal(0f, op.WidthMax);
            Assert.Equal(0f, op.HeightMin);
            Assert.Equal(0f, op.MaxHeight);
        }

        [Fact]
        public void ColorOverload_AlsoForwardsConstraints()
        {
            _gui.Row(Size.Expand().WithMinMax(100f, 300f), Size.Fit(),
                new UnityEngine.Color32(255, 0, 0, 255));

            var op = _gui.Buffer.Ops[0];
            Assert.Equal(100f, op.WidthMin);
            Assert.Equal(300f, op.WidthMax);
        }

        [Fact]
        public void ScrollBox_ExplicitMaxHeight_WinsOverSizeMax()
        {
            // Decision 2: the explicit maxHeight param wins over a Size.Max set on
            // height if a caller (incorrectly) sets both.
            _gui.ScrollBox(Size.Expand(), Size.Fit().WithMax(999f),
                background: default, maxHeight: 250f);

            var op = _gui.Buffer.Ops[0];
            Assert.Equal(FrameOpKind.ScrollBegin, op.Kind);
            Assert.Equal(250f, op.MaxHeight);
        }

        [Fact]
        public void ScrollBox_NoExplicitMaxHeight_FallsBackToSizeMax()
        {
            _gui.ScrollBox(Size.Expand(), Size.Fit().WithMax(400f),
                background: default);

            var op = _gui.Buffer.Ops[0];
            Assert.Equal(400f, op.MaxHeight);
        }
    }
}
