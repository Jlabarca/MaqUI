// SPDX-License-Identifier: MIT
// MaqUI v2 — MaquiComponents. Static partial class extending Gui with the
// Controls layer (Button, Slider, Toggle, Image, ScrollView, TextInput).

namespace Maqui.V2.Components
{
    /// <summary>
    /// Controls layer. Each method is a <c>static</c> extension on <see cref="Gui"/>;
    /// callers write fluent code like <c>gui.Button("OK")</c>. Each control composes
    /// from P1-P4 primitives only — no Unity types — except <c>Image</c> and
    /// <c>TextInput</c> which need backend extensions and ship as blind drafts.
    ///
    /// <para>The `/maqui-component` skill (P5.1) generates additional components
    /// into this same partial class. New component methods follow the same shape:
    /// extension on <c>Gui</c>, optional <c>string key</c> for animations,
    /// composition over primitives, return type matches the control's output
    /// (bool for click, float for slider, etc.).</para>
    /// </summary>
    public static partial class MaquiComponents
    {
    }
}
