// SPDX-License-Identifier: MIT
// MaqUI v2 — FRAME-BUDGET.2.5. Opt-out levers for the frame-budget work, bound by ORO's
// `/fb` chat hook via reflection (this package is not yet the committed pin — see
// docs/FRAME-BUDGET-IMPL.md P2's commit note). Kept to plain public static bool fields
// so reflection binding is trivial and no new MaqUI public API surface is implied.

namespace Maqui
{
    /// <summary>
    /// Frame-budget opt-out flags. Every flag defaults to the FAST (new) behavior;
    /// flipping one off reverts to the pre-FRAME-BUDGET.2 code path for A/B measurement.
    /// Once each phase's AFTER row is recorded and parity holds, the OFF path is deleted
    /// and the flag itself removed (FRAME-BUDGET.2.9's plan).
    /// </summary>
    public static class FrameBudgetFlags
    {
        /// <summary>
        /// FRAME-BUDGET.2.5: when true (default), <see cref="UIToolkitBackend.ApplyProps"/>
        /// skips its style writes for an element whose <see cref="FrameOp"/> is unchanged
        /// from the last one applied (<see cref="FrameOp.PropsEqual"/>). Flip off to force
        /// every op through the full write path, matching pre-2.5 behavior.
        /// </summary>
        public static bool PropDiff = true;
    }
}
