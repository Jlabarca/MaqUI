// SPDX-License-Identifier: MIT
// MaqUI v2 — Controls.UnitFrame (V2-PRO-SKIN.4.1).

namespace Maqui.V2.Components
{
    public static partial class MaquiComponents
    {
        /// <summary>
        /// A compact unit HUD frame: portrait tile + name + level/job line, over
        /// stacked HP and SP <see cref="Bar"/>s. The self/party/target frame the
        /// mockup floats under the character. Composes the P1 <see cref="Bar"/> —
        /// no new op.
        /// </summary>
        public static void UnitFrame(this Gui gui, string key, string portraitKey,
            string name, string subText, float hp01, string hpText, float sp01, string spText,
            float portraitSize = 40f, string className = null, string portraitClassName = null,
            string nameClassName = null, string subClassName = null, string barClassName = null,
            string hpFillClassName = null, string spFillClassName = null, string barLabelClassName = null)
        {
            gui.Row(Size.Expand(), Size.Fit(), background: default,
                alignItems: AlignItems.Center, className: className);
            {
                gui.Column(Size.Pixels(portraitSize + 6f), Size.Pixels(portraitSize + 6f),
                    background: default, alignItems: AlignItems.Center, className: portraitClassName);
                {
                    if (!string.IsNullOrEmpty(portraitKey))
                        gui.Icon(portraitKey, portraitSize);
                }
                gui.EndColumn();

                gui.Spacer(Size.Pixels(6f));

                gui.Column(Size.Expand(), Size.Fit());
                {
                    gui.Row(Size.Expand(), Size.Fit(), background: default, alignItems: AlignItems.Center);
                    {
                        gui.DrawText(name ?? string.Empty, className: nameClassName);
                        gui.Spacer(Size.Expand());
                        gui.DrawText(subText ?? string.Empty, className: subClassName);
                    }
                    gui.EndRow();

                    gui.Bar(key + "-hp", hp01, hpText, className: barClassName,
                        fillClassName: hpFillClassName, labelClassName: barLabelClassName, height: 14f);
                    gui.Bar(key + "-sp", sp01, spText, className: barClassName,
                        fillClassName: spFillClassName, labelClassName: barLabelClassName, height: 12f);
                }
                gui.EndColumn();
            }
            gui.EndRow();
        }
    }
}
