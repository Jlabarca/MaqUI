// SPDX-License-Identifier: MIT
// V2-UI-POLISH P2 — FlowLayout column math.

using Maqui.V2;
using Xunit;

namespace Maqui.V2.Tests
{
    public class FlowLayoutTests
    {
        [Fact]
        public void Columns_ExactFit_TakesAllOfThem()
        {
            // 6 × 44 = 264, no gap.
            Assert.Equal(6, FlowLayout.Columns(264f, 44f));
        }

        [Fact]
        public void Columns_OnePixelShort_DropsOne()
        {
            Assert.Equal(5, FlowLayout.Columns(263f, 44f));
        }

        [Fact]
        public void Columns_GapCountsBetweenCellsOnly_NotAfterTheLast()
        {
            // 4 cells of 40 with 3 gaps of 6 = 178. A trailing-gap formula would need
            // 184 and report 3 here — the off-by-one this test exists to pin, at exactly
            // the boundary width a user dragging the grip is most likely to land on.
            Assert.Equal(4, FlowLayout.Columns(178f, 40f, gap: 6f));
            Assert.Equal(3, FlowLayout.Columns(177f, 40f, gap: 6f));
        }

        [Fact]
        public void Columns_NarrowerThanOneCell_StillReturnsTheMinimum()
        {
            // Better to overflow one cell than to render zero columns and no content.
            Assert.Equal(1, FlowLayout.Columns(10f, 44f));
        }

        [Fact]
        public void Columns_UnknownWidth_UsesTheFallback_NotOne()
        {
            // First frame, before layout has run. Falling back to the caller's shape
            // avoids a visible snap from one column to the real grid on open.
            Assert.Equal(6, FlowLayout.Columns(float.NaN, 44f, fallbackColumns: 6));
            Assert.Equal(6, FlowLayout.Columns(0f, 44f, fallbackColumns: 6));
            Assert.Equal(6, FlowLayout.Columns(-50f, 44f, fallbackColumns: 6));
        }

        [Fact]
        public void Columns_RespectsMinAndMax()
        {
            Assert.Equal(3, FlowLayout.Columns(1000f, 44f, maxColumns: 3));
            Assert.Equal(4, FlowLayout.Columns(50f, 44f, minColumns: 4));
            // Max outranks a fallback too — an unknown width must not escape the cap.
            Assert.Equal(3, FlowLayout.Columns(float.NaN, 44f, maxColumns: 3, fallbackColumns: 8));
        }

        [Fact]
        public void Columns_DegenerateCellSize_DoesNotDivideByZero()
        {
            Assert.Equal(1, FlowLayout.Columns(300f, 0f));
            Assert.Equal(1, FlowLayout.Columns(300f, -5f));
            Assert.Equal(1, FlowLayout.Columns(300f, float.NaN));
        }

        [Fact]
        public void Columns_NegativeGapIsTreatedAsZero()
        {
            Assert.Equal(6, FlowLayout.Columns(264f, 44f, gap: -10f));
        }

        [Theory]
        [InlineData(0, 6, 0)]
        [InlineData(1, 6, 1)]
        [InlineData(6, 6, 1)]
        [InlineData(7, 6, 2)]
        [InlineData(12, 6, 2)]
        [InlineData(13, 6, 3)]
        public void Rows_PartialRowStillCounts(int items, int columns, int expected)
        {
            Assert.Equal(expected, FlowLayout.Rows(items, columns));
        }

        [Fact]
        public void Rows_ZeroColumnsDoesNotDivideByZero()
        {
            Assert.Equal(5, FlowLayout.Rows(5, 0));
        }

        [Fact]
        public void Columns_WidensMonotonically_AcrossARealResizeSweep()
        {
            // The property that matters live: dragging the grip wider must never REDUCE
            // the column count. Cheap to assert, and would catch a rounding regression
            // that a handful of point checks could miss.
            int prev = 0;
            for (float w = 60f; w <= 1200f; w += 1f)
            {
                int c = FlowLayout.Columns(w, 44f, gap: 2f);
                Assert.True(c >= prev, $"column count fell from {prev} to {c} at width {w}");
                prev = c;
            }
        }
    }
}
