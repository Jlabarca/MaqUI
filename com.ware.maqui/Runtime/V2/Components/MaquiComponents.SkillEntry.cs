// SPDX-License-Identifier: MIT
// MaqUI v2 — Controls.SkillEntry (V2-PRO-SKIN.3.1).

namespace Maqui.V2.Components
{
    public static partial class MaquiComponents
    {
        /// <summary>
        /// A skill-tree cell: icon + name + "level / max" with a −/+
        /// <see cref="Stepper"/>, wrapped in a container that gains
        /// <paramref name="learnedClassName"/> (the green learned outline) once the
        /// skill has at least one point. Returns the stepper delta this frame — the
        /// caller decides whether +1 is spendable and fires the server command
        /// (skill-down is not server-wired, so callers pass
        /// <paramref name="canLevelUp"/> and ignore −1).
        ///
        /// <para>Pure composition of Icon + DrawText + Stepper — no new op. Reuses
        /// the P1 Stepper and the pro token classes.</para>
        /// </summary>
        /// <param name="showStepper">V2-UI-PARITY.5.2: false genuinely OMITS the stepper
        /// (not just disables it) — parity with legacy <c>SkillWindowEntry.UpdateLevelUpButton</c>'s
        /// <c>SetActive(false)</c> when there are no points to spend, the skill is maxed, or
        /// its prereqs aren't met. <see cref="Stepper"/> itself deliberately never omits its
        /// own buttons (see its doc comment), so the hide has to happen one level up, here.
        /// The level text stays — legacy hides only the button, never the "N / max" label.</param>
        public static int SkillEntry(this Gui gui, string key, string iconKey, string name,
            int level, int maxLevel, bool learned, bool canLevelUp = true, float iconSize = 30f,
            string className = null, string learnedClassName = null, string iconClassName = null,
            string nameClassName = null, string levelClassName = null, string buttonClassName = null,
            bool showStepper = true)
        {
            int delta = 0;
            string cls = learned ? (learnedClassName ?? className) : className;

            gui.Column(Size.Expand(), Size.Fit(), background: default, className: cls);
            {
                gui.Row(Size.Expand(), Size.Fit(), background: default, alignItems: AlignItems.Center);
                {
                    gui.Column(Size.Pixels(iconSize + 6f), Size.Pixels(iconSize + 6f), background: default,
                        alignItems: AlignItems.Center, className: iconClassName);
                    {
                        if (!string.IsNullOrEmpty(iconKey))
                            gui.Icon(iconKey, iconSize);
                    }
                    gui.EndColumn();

                    gui.Spacer(Size.Pixels(4f));

                    // Name spans the full width right of the icon (single-line +
                    // ellipsis is a styling concern of nameClassName); the level and
                    // the −/+ stepper share the row BELOW it, so the name is never
                    // starved into a char-by-char wrap.
                    gui.Column(Size.Expand(), Size.Fit(), background: default, alignItems: AlignItems.Start);
                    {
                        gui.DrawText(name ?? string.Empty, className: nameClassName);

                        gui.Row(Size.Expand(), Size.Fit(), background: default, alignItems: AlignItems.Center);
                        {
                            gui.DrawText($"{level} / {maxLevel}", className: levelClassName);
                            gui.Spacer(Size.Expand());
                            if (showStepper)
                            {
                                // Skill-down is not a server command, so the − side is
                                // disabled; + is gated on canLevelUp.
                                delta = gui.Stepper(key + "-lv", string.Empty,
                                    buttonClassName: buttonClassName,
                                    canDecrement: false, canIncrement: canLevelUp);
                            }
                        }
                        gui.EndRow();
                    }
                    gui.EndColumn();
                }
                gui.EndRow();
            }
            gui.EndColumn();

            return delta;
        }
    }
}
