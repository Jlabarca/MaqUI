// SPDX-License-Identifier: MIT
// MaqUI v2 - derived-string memo (ZERO-ALLOC follow-up).
//
// Button used to build `className + "__label"` on every call, and a handful of siblings did the same
// with `key + "-dec"`, `count.ToString()` and friends. The values never change between frames, so
// they are now derived once and handed back by reference. These tests pin BEHAVIOUR - the right
// string, the same instance on a repeat, the bound on the table - not GC bytes, because the byte
// counter reads a flat 0 on some runtimes and a test that can only fail on bytes would fail open.

using System.Linq;
using Maqui;
using Maqui.Components;
using Xunit;

namespace Maqui.Tests
{
    public class StringCacheTests
    {
        private static Gui NewGui()
        {
            var gui = new Gui();
            gui.BeginFrame();
            return gui;
        }

        private static string LabelClassOfLastButton(Gui gui) =>
            gui.Buffer.Ops.Last(o => o.Kind == FrameOpKind.DrawText).ClassName;

        // ---- Button: the stack in the allocation report --------------------------------------

        [Fact]
        public void Button_LabelClass_IsTheContainerClassPlusLabelSuffix()
        {
            var gui = NewGui();
            gui.Button("OK", key: "ok", className: "ro-btn");

            Assert.Equal("ro-btn__label", LabelClassOfLastButton(gui));
            Assert.Equal("ro-btn", gui.Buffer.Ops.First(o => o.Kind == FrameOpKind.ColumnBegin).ClassName);
        }

        [Fact]
        public void Button_LabelClass_IsTheSameStringInstanceOnARepeatCall_AcrossFramesAndGuis()
        {
            var first = NewGui();
            first.Button("OK", className: "ro-btn-repeat");
            var a = LabelClassOfLastButton(first);

            first.EndFrame();
            first.BeginFrame();
            first.Button("OK", className: "ro-btn-repeat");
            var b = LabelClassOfLastButton(first);

            var other = NewGui();
            other.Button("Different label text", className: "ro-btn-repeat");
            var c = LabelClassOfLastButton(other);

            Assert.Equal("ro-btn-repeat__label", a);
            Assert.Same(a, b);   // a rebuild of the same window
            Assert.Same(a, c);   // another window using the same class
        }

        [Fact]
        public void Button_DistinctClasses_GetDistinctCorrectLabelClasses()
        {
            var gui = NewGui();
            gui.Button("A", className: "ro-pro-btn-ghost");
            gui.Button("B", className: "ro-statstoggle");

            var labels = gui.Buffer.Ops.Where(o => o.Kind == FrameOpKind.DrawText).Select(o => o.ClassName).ToList();
            Assert.Equal(new[] { "ro-pro-btn-ghost__label", "ro-statstoggle__label" }, labels);
        }

        [Fact]
        public void Button_WithNoClassName_StillDerivesNoLabelClass()
        {
            var gui = NewGui();
            gui.Button("OK");

            Assert.Null(LabelClassOfLastButton(gui));
            Assert.Null(gui.Buffer.Ops.First(o => o.Kind == FrameOpKind.ColumnBegin).ClassName);
        }

        [Fact]
        public void Button_EmptyClassName_IsUnstyled_NotAnUnderscoreOnlyLabelClass()
        {
            var gui = NewGui();
            gui.Button("OK", className: "");

            Assert.Null(LabelClassOfLastButton(gui));
        }

        // ---- the siblings that derived the same way ------------------------------------------

        [Fact]
        public void ItemSlot_CountText_IsCorrect_AndSharedAcrossRebuilds()
        {
            var gui = NewGui();
            gui.ItemSlot("s0", "Apple", count: 12);
            gui.EndFrame();
            var a = gui.Buffer.Ops.Single(o => o.Kind == FrameOpKind.DrawText).Text;

            gui.BeginFrame();
            gui.ItemSlot("s0", "Apple", count: 12);
            var b = gui.Buffer.Ops.Single(o => o.Kind == FrameOpKind.DrawText).Text;

            Assert.Equal("12", a);
            Assert.Same(a, b);
        }

        [Fact]
        public void ItemSlot_LargeCount_FallsBackToAnOrdinaryString()
        {
            var gui = NewGui();
            gui.ItemSlot("s0", "Apple", count: 12345);

            Assert.Equal("12345", gui.Buffer.Ops.Single(o => o.Kind == FrameOpKind.DrawText).Text);
        }

        [Fact]
        public void SkillEntry_LevelText_IsCorrect_AndSharedAcrossRebuilds()
        {
            var gui = NewGui();
            gui.SkillEntry("bash", "skill_bash", "Bash", 3, 10, learned: true);
            gui.EndFrame();
            var a = gui.Buffer.Ops.Single(o => o.Kind == FrameOpKind.DrawText && o.Text == "3 / 10").Text;

            gui.BeginFrame();
            gui.SkillEntry("bash", "skill_bash", "Bash", 3, 10, learned: true);
            var b = gui.Buffer.Ops.Single(o => o.Kind == FrameOpKind.DrawText && o.Text == "3 / 10").Text;

            Assert.Same(a, b);
        }

        [Fact]
        public void Tabs_BodyScopeAndTabKeys_KeepTheirExistingShape()
        {
            // The body scope is the data-scope path; a changed shape would silently re-key every tab body.
            var gui = NewGui();
            gui.Tabs("inv", 1, new[] { "A", "B" }, renderContent: _ => gui.DrawText("body"));

            var scope = gui.Buffer.Ops.First(o => o.Kind == FrameOpKind.ScopeEnter);
            Assert.Equal("inv-body-1", scope.Text);
        }

        // ---- the memo itself -----------------------------------------------------------------

        [Fact]
        public void Suffixed_MatchesConcatenation_IncludingNullSides()
        {
            var m = new DerivedStrings();
            Assert.Equal("hp-dec", m.Suffixed("hp", "-dec"));
            Assert.Equal("-dec", m.Suffixed(null, "-dec"));
            Assert.Equal("hp", m.Suffixed("hp", null));
            Assert.Equal("", m.Suffixed(null, null));
        }

        [Fact]
        public void Suffixed_ReturnsTheSameInstanceForTheSameInputs()
        {
            var m = new DerivedStrings();
            var a = m.Suffixed("hp", "-dec");
            Assert.Same(a, m.Suffixed("hp", "-dec"));
            Assert.NotSame(a, m.Suffixed("sp", "-dec"));
            Assert.NotSame(a, m.Suffixed("hp", "-inc"));
        }

        [Fact]
        public void Scoped_MatchesTheOldStoreKeyShape()
        {
            var m = new DerivedStrings();
            Assert.Equal("/win/text", m.Scoped("/win", "text"));
            Assert.Same(m.Scoped("/win", "text"), m.Scoped("/win", "text"));
            Assert.Equal("/", m.Scoped(null, null));
        }

        [Fact]
        public void Indexed_MatchesTheOldInterpolation()
        {
            var m = new DerivedStrings();
            Assert.Equal("inv-tab-3", m.Indexed("inv", "-tab-", 3));
            Assert.Equal("-tab-3", m.Indexed(null, "-tab-", 3));
            Assert.Same(m.Indexed("inv", "-tab-", 3), m.Indexed("inv", "-tab-", 3));
            Assert.NotSame(m.Indexed("inv", "-tab-", 3), m.Indexed("inv", "-tab-", 4));
        }

        [Fact]
        public void Pair_MatchesTheOldInterpolation()
        {
            var m = new DerivedStrings();
            Assert.Equal("3 / 10", m.Pair(3, 10));
            Assert.Equal("0 / 0", m.Pair(0, 0));
            Assert.Same(m.Pair(3, 10), m.Pair(3, 10));
        }

        [Theory]
        [InlineData(0, "0")]
        [InlineData(7, "7")]
        [InlineData(999, "999")]
        [InlineData(1000, "1000")]
        [InlineData(65535, "65535")]
        [InlineData(-4, "-4")]
        public void Int_MatchesToString(int n, string expected)
        {
            var m = new DerivedStrings();
            Assert.Equal(expected, m.Int(n));
            Assert.Equal(expected, m.Int(n));
        }

        [Fact]
        public void Int_SmallValuesAreShared_LargeAndNegativeAreNotRemembered()
        {
            var m = new DerivedStrings();
            Assert.Same(m.Int(42), m.Int(42));
            Assert.NotSame(m.Int(5000), m.Int(5000));   // outside the flat array: an ordinary ToString, never stored
        }

        [Fact]
        public void TheTable_IsBounded_AndAnInputPastTheBoundStillGetsTheRightString()
        {
            var m = new DerivedStrings(cap: 8);
            for (var i = 0; i < 100; i++)
                Assert.Equal($"k{i}-dec", m.Suffixed($"k{i}", "-dec"));

            Assert.True(m.Count <= 8, $"the memo grew past its cap: {m.Count}");

            // The first eight were remembered; one that arrived after the table filled is correct but fresh.
            Assert.Same(m.Suffixed("k0", "-dec"), m.Suffixed("k0", "-dec"));
            Assert.Equal("k99-dec", m.Suffixed("k99", "-dec"));
        }

        [Fact]
        public void Shared_SurvivesParallelUse()
        {
            // xunit runs fixtures in parallel against MaquiStrings.Shared; a torn entry would throw or loop.
            var results = new string[64];
            System.Threading.Tasks.Parallel.For(0, results.Length, i =>
            {
                for (var n = 0; n < 200; n++)
                    results[i] = MaquiStrings.Suffixed("par-" + (n % 7), "-x");
            });

            Assert.All(results, r => Assert.StartsWith("par-", r));
        }
    }
}
