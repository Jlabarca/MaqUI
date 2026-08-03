// SPDX-License-Identifier: MIT
// MaqUI v2 — FlowLayout: derive a column count from measured available width
// (V2-UI-POLISH P2).
//
// The fourth responsiveness primitive, alongside Size.WithMin/Max, TextScale.Ramp
// and HidePriority. Those three let a window CONSTRAIN, SHRINK or DROP content as
// it narrows; none of them let content REARRANGE. That was the gap: every V2 grid
// hard-coded its column count (ORO's inventory: `Columns = 6`), so widening the
// window added empty space instead of items per row, and narrowing it clipped.
//
// Pure arithmetic — no Unity types — so it lives at Runtime/V2 root with the other
// pure primitives and is covered by the headless xUnit suite, unlike anything that
// touches UIElements.

namespace Maqui.V2
{
    /// <summary>
    /// Column-count math for a wrapping grid whose cells are a fixed size.
    /// </summary>
    public static class FlowLayout
    {
        /// <summary>
        /// How many fixed-width cells fit across <paramref name="availableWidth"/>.
        ///
        /// <para>Fits <c>n</c> cells when <c>n·cell + (n-1)·gap &lt;= available</c>, i.e.
        /// <c>n = floor((available + gap) / (cell + gap))</c>. The gap is BETWEEN cells
        /// only — a trailing gap after the last cell would under-count by one at exactly
        /// the boundary width, which is the width a user is most likely to drag to.</para>
        ///
        /// <para><b>Include per-cell margins in <paramref name="cellSize"/>.</b> Margins
        /// are outside the box in flex layout, so a 40px slot with a 2px margin occupies
        /// 44px; passing the bare 40 over-counts and the last cell wraps. This is a
        /// caller responsibility on purpose — the pure layer cannot see a stylesheet.</para>
        ///
        /// <para>Returns <paramref name="fallbackColumns"/> when the width is unknown
        /// (NaN or non-positive), which is the state on the first frame before layout
        /// has run. Falling back to a caller-chosen value rather than 1 keeps a window
        /// from visibly snapping from a single column to its real shape on open.</para>
        /// </summary>
        /// <param name="maxColumns">Upper bound, or 0 for none. Useful where more columns
        /// stop being readable long before they stop fitting.</param>
        public static int Columns(float availableWidth, float cellSize, float gap = 0f,
            int minColumns = 1, int maxColumns = 0, int fallbackColumns = 1)
        {
            if (minColumns < 1) minColumns = 1;

            if (cellSize <= 0f || float.IsNaN(cellSize)) return Clamp(minColumns, minColumns, maxColumns);
            if (float.IsNaN(availableWidth) || availableWidth <= 0f)
                return Clamp(fallbackColumns, minColumns, maxColumns);

            if (gap < 0f) gap = 0f;
            int fit = (int)((availableWidth + gap) / (cellSize + gap));
            return Clamp(fit, minColumns, maxColumns);
        }

        /// <summary>
        /// Rows needed to lay <paramref name="itemCount"/> items into
        /// <paramref name="columns"/>. Convenience for a caller that needs to reserve or
        /// measure height (e.g. a virtualizing scroll region).
        /// </summary>
        public static int Rows(int itemCount, int columns)
        {
            if (itemCount <= 0) return 0;
            if (columns < 1) columns = 1;
            return (itemCount + columns - 1) / columns;
        }

        private static int Clamp(int value, int min, int max)
        {
            if (value < min) value = min;
            if (max > 0 && value > max) value = max;
            return value;
        }
    }
}
