// SPDX-License-Identifier: MIT
// MaqUI v2 — hide-priority / breakpoint predicate (V2-UI-PARITY.3.3).
//
// No FrameOpKind/reconciler feature: the whole UI is redeclared every frame
// from C# (immediate-mode), so "drop secondary content at a narrow width"
// is just a caller-side branch around the content it would otherwise render.
// This predicate is that branch's condition — a window's BuildUI calls it
// and skips rendering a block when it returns false. Policy (which tier maps
// to which breakpoint) stays in the window, per the Architecture section's
// "responsive by policy, not pixels" framing — this file owns no numbers.
//
// Pure, no UnityEngine dependency — headlessly testable in full.

namespace Maqui.V2
{
    public static class HidePriority
    {
        /// <summary>
        /// True if content at <paramref name="priority"/> should render given
        /// <paramref name="availableWidth"/> and the caller's own
        /// <paramref name="breakpoints"/> table (one pixel width per priority tier
        /// above 0, indexed <c>breakpoints[priority - 1]</c>).
        ///
        /// <para>Priority 0 always renders (there is no tier to hide it behind —
        /// primary content). A priority with no matching breakpoint entry
        /// (<paramref name="priority"/> exceeds <paramref name="breakpoints"/>'s
        /// length) fails OPEN (renders), not closed: a caller adding a new tier
        /// without updating its breakpoint table should not silently lose
        /// content.</para>
        /// </summary>
        public static bool IsVisible(float availableWidth, int priority, params float[] breakpoints)
        {
            if (priority <= 0) return true;
            if (breakpoints == null || priority > breakpoints.Length) return true;

            return availableWidth >= breakpoints[priority - 1];
        }
    }
}
