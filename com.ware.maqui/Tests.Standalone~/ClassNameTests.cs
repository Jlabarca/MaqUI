// SPDX-License-Identifier: MIT
// MaqUI v2 — className pass-through (WINDOW-LOOP WL.2.2).
//
// Why this exists: V2's backend used to mint plain, class-less VisualElements,
// so USS had nothing to select on and appearance could only come from draw-op
// colour arguments in C#. FrameOp.ClassName is the seam that hands theming back
// to the host framework (docs/research/ui-window-loop-plan.md §4.3 — delegate,
// don't reimplement).
//
// Scope note: these cover the OP layer (Gui records the class, every op still
// defaults to null). The backend half — AddToClassList / stale-class removal on
// pooled elements — lives in UIToolkitBackend.cs, which the csproj excludes from
// this suite as a Unity-only blind draft. That boundary is exactly the one the
// 2026-07-16 audit flagged; WL.3's WindowProbe is what covers the other side.

using Maqui;
using UnityEngine;
using Xunit;

namespace Maqui.Tests
{
    public class ClassNameTests
    {
        private readonly Gui _gui;

        public ClassNameTests()
        {
            _gui = new Gui();
            _gui.BeginFrame();
        }

        [Fact]
        public void Row_RecordsClassName()
        {
            _gui.Row(className: "ro-row");
            Assert.Equal("ro-row", _gui.Buffer.Ops[0].ClassName);
        }

        [Fact]
        public void Column_RecordsClassName()
        {
            _gui.Column(className: "ro-col");
            Assert.Equal("ro-col", _gui.Buffer.Ops[0].ClassName);
        }

        [Fact]
        public void Box_RecordsClassName()
        {
            _gui.Box(className: "ro-box");
            Assert.Equal("ro-box", _gui.Buffer.Ops[0].ClassName);
        }

        [Fact]
        public void ScrollBox_RecordsClassName()
        {
            _gui.ScrollBox(className: "ro-scroll");
            Assert.Equal("ro-scroll", _gui.Buffer.Ops[0].ClassName);
        }

        [Fact]
        public void DrawText_RecordsClassName()
        {
            _gui.DrawText("hello", className: "ro-label");
            Assert.Equal("ro-label", _gui.Buffer.Ops[0].ClassName);
        }

        /// <summary>The background/alignItems overloads must carry the class too —
        /// they're the ones real windows call (a themed panel sets both).</summary>
        [Fact]
        public void Row_BackgroundOverload_RecordsClassName()
        {
            _gui.Row(Size.Pixels(10f), Size.Pixels(10f), new Color32(1, 2, 3, 255),
                AlignItems.Center, className: "ro-row-bg");

            var op = _gui.Buffer.Ops[0];
            Assert.Equal("ro-row-bg", op.ClassName);
            Assert.Equal(AlignItems.Center, op.AlignItems);   // guard: class didn't displace an arg
        }

        [Fact]
        public void Column_BackgroundOverload_RecordsClassName()
        {
            _gui.Column(Size.Pixels(10f), Size.Pixels(10f), new Color32(1, 2, 3, 255),
                AlignItems.End, className: "ro-col-bg");

            var op = _gui.Buffer.Ops[0];
            Assert.Equal("ro-col-bg", op.ClassName);
            Assert.Equal(AlignItems.End, op.AlignItems);
        }

        /// <summary>ScrollBox's maxHeight must survive alongside the class — the two
        /// sit next to each other in the signature, which is where a mis-wire lands.</summary>
        [Fact]
        public void ScrollBox_KeepsMaxHeightAlongsideClassName()
        {
            _gui.ScrollBox(default, default, new Color32(9, 9, 9, 255),
                maxHeight: 420f, className: "ro-well");

            var op = _gui.Buffer.Ops[0];
            Assert.Equal("ro-well", op.ClassName);
            Assert.Equal(420f, op.MaxHeight);
        }

        /// <summary>The whole feature is opt-in: every existing call site must keep
        /// recording no class at all, so nothing starts matching a stylesheet rule
        /// by accident.</summary>
        [Fact]
        public void OmittedClassName_IsNull_ForEveryOpKind()
        {
            _gui.Row();
            _gui.EndRow();
            _gui.Column();
            _gui.EndColumn();
            _gui.Box();
            _gui.ScrollBox();
            _gui.EndScrollBox();
            _gui.Spacer(Size.Pixels(4f));
            _gui.DrawText("x");
            _gui.DrawRect(new Color32(1, 1, 1, 255));
            _gui.DrawLine(new Color32(1, 1, 1, 255));

            foreach (var op in _gui.Buffer.Ops)
                Assert.Null(op.ClassName);
        }
    }
}
