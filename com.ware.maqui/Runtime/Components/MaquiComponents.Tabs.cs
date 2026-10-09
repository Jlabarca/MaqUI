// SPDX-License-Identifier: MIT
// MaqUI v2 — Controls.Tabs (V2-PLAYER-UI VPU.3).

namespace Maqui.Components
{
    public static partial class MaquiComponents
    {
        /// <summary>
        /// A horizontal tab strip: one Button per label, the active one styled with
        /// <paramref name="activeTabClassName"/>. Returns the index selected THIS
        /// frame (unchanged if no tab was clicked) — the caller owns the active
        /// index, exactly like <see cref="Stepper"/> owns its value and
        /// <see cref="ProgressBar"/> owns its fraction. Immediate-mode: there is no
        /// hidden per-tab state to fall out of sync.
        ///
        /// <para>When <paramref name="renderContent"/> is supplied it is invoked
        /// once, after the strip, with the freshly-selected index — so clicking a
        /// tab swaps the body on the SAME frame (no one-frame lag). Pass null to
        /// render the body yourself from the return value.</para>
        ///
        /// <para>Pure Button composition — no new framework op. The active/inactive
        /// split is a className swap so USS owns the look (WL.2 delegate-don't-tint).</para>
        ///
        /// <para><b>Body is scoped per tab index.</b> The reconciler keys containers by
        /// <c>(scopePath, kind, ordinal-within-scope)</c> — there is no caller-supplied
        /// identity for Row/Column. Without a per-tab scope, every tab body renders into
        /// the SAME scope, so "the Nth RowBegin" on one tab collides with "the Nth
        /// RowBegin" on a differently-shaped tab and the backend reuses the wrong
        /// element via <c>UpdateElement</c> (no style reset — that only runs on a real
        /// pool rent). Confirmed live: a plain footer Row (Size.Expand/Size.Fit,
        /// background: default) landed on a stale Toggle pill's inline 52x22 size and
        /// non-transparent background after switching tabs, squeezing its Button
        /// children to ~14px. <see cref="Gui.EnterDataScope"/> already exists for
        /// exactly this (repeated/keyed content) — Tabs just wasn't using it.</para>
        /// </summary>
        public static int Tabs(this Gui gui, string key, int activeIndex, string[] labels,
            System.Action<int> renderContent = null,
            string stripClassName = null, string tabClassName = null, string activeTabClassName = null)
        {
            if (labels == null || labels.Length == 0) return activeIndex;

            // Clamp to a valid tab so a stale/out-of-range index can't blank the body.
            int selected = (activeIndex < 0 || activeIndex >= labels.Length) ? 0 : activeIndex;

            gui.Row(Size.Expand(), Size.Fit(), background: default,
                alignItems: AlignItems.Stretch, className: stripClassName);
            {
                for (int i = 0; i < labels.Length; i++)
                {
                    bool isActive = i == selected;
                    string cls = isActive ? (activeTabClassName ?? tabClassName) : tabClassName;
                    // Keyed per index so each tab is a stable node with its own hover/press.
                    if (gui.Button(labels[i] ?? string.Empty, key: MaquiStrings.Indexed(key, "-tab-", i), className: cls))
                        selected = i;
                }
            }
            gui.EndRow();

            if (renderContent != null)
            {
                using (gui.EnterDataScope(MaquiStrings.Indexed(key, "-body-", selected)))
                {
                    renderContent(selected);
                }
            }
            return selected;
        }
    }
}
