// SPDX-License-Identifier: MIT
// FRAME-BUDGET.2.1 — interned scope paths in Gui.cs. Verifies CurrentScopePath
// stays byte-identical to the old string.Join/ReverseStack implementation and
// that the intern table is bounded (evicts entries untouched for ~600 frames).

using System.Reflection;
using NUnit.Framework;

namespace Maqui.Tests.Runtime
{
    public class ScopePathInternTests
    {
        private static int InternTableCount(Gui gui)
        {
            FieldInfo field = typeof(Gui).GetField("_pathIntern", BindingFlags.NonPublic | BindingFlags.Instance);
            Assert.IsNotNull(field, "Gui._pathIntern field not found — has it been renamed?");
            var table = field.GetValue(gui) as System.Collections.ICollection;
            Assert.IsNotNull(table);
            return table.Count;
        }

        [Test]
        public void CurrentScopePath_Root_IsSlash()
        {
            var gui = new Gui();
            gui.BeginFrame();
            Assert.AreEqual("/", gui.CurrentScopePath);
            gui.EndFrame();
        }

        [Test]
        public void CurrentScopePath_NestedScopes_MatchesJoinedPath()
        {
            var gui = new Gui();
            gui.BeginFrame();
            using (gui.EnterDataScope("inventory"))
            {
                Assert.AreEqual("/inventory", gui.CurrentScopePath);
                using (gui.EnterDataScope("slot_7"))
                {
                    Assert.AreEqual("/inventory/slot_7", gui.CurrentScopePath);
                }
                Assert.AreEqual("/inventory", gui.CurrentScopePath);
            }
            Assert.AreEqual("/", gui.CurrentScopePath);
            gui.EndFrame();
        }

        [Test]
        public void CurrentScopePath_SameKeysAcrossFrames_ReturnsEqualStrings()
        {
            var gui = new Gui();

            gui.BeginFrame();
            string pathFrame1;
            using (gui.EnterDataScope("panel"))
            using (gui.EnterDataScope("row_2"))
                pathFrame1 = gui.CurrentScopePath;
            gui.EndFrame();

            gui.BeginFrame();
            string pathFrame2;
            using (gui.EnterDataScope("panel"))
            using (gui.EnterDataScope("row_2"))
                pathFrame2 = gui.CurrentScopePath;
            gui.EndFrame();

            Assert.AreEqual(pathFrame1, pathFrame2);
            // Interning means the second frame should reuse the SAME string
            // instance for an identical (parent, key) pair, not just an equal
            // one — this is the point of FRAME-BUDGET.2.1.
            Assert.AreSame(pathFrame1, pathFrame2, "expected the interned path to be reused, not recomputed.");
        }

        [Test]
        public void ScopeStack_DisposeOutOfOrder_StillThrows()
        {
            var gui = new Gui();
            gui.BeginFrame();
            var outer = gui.EnterDataScope("outer");
            gui.EnterDataScope("inner"); // not disposed via using — deliberately leaked for this assertion
            Assert.Throws<System.InvalidOperationException>(() => outer.Dispose());
            gui.EndFrame();
        }

        [Test]
        public void InternTable_EvictsEntriesUntouchedFor600Frames()
        {
            var gui = new Gui();

            // Touch one scope once, then let it go cold for 700 frames while
            // touching nothing else — it must be evicted on the next sweep
            // (every 128 frames) instead of surviving forever.
            gui.BeginFrame();
            using (gui.EnterDataScope("stale_item_42")) { }
            gui.EndFrame();

            int countAfterTouch = InternTableCount(gui);
            Assert.GreaterOrEqual(countAfterTouch, 1);

            for (int i = 0; i < 700; i++)
            {
                gui.BeginFrame();
                gui.EndFrame();
            }

            int countAfterCold = InternTableCount(gui);
            Assert.Less(countAfterCold, countAfterTouch + 1);
            Assert.AreEqual(0, countAfterCold, "the only entry was cold for 700 frames (> the 600-frame threshold, past a 128-frame sweep boundary) and should have been evicted.");
        }

        [Test]
        public void InternTable_RepeatedTouch_NeverEvicted()
        {
            var gui = new Gui();

            for (int i = 0; i < 700; i++)
            {
                gui.BeginFrame();
                using (gui.EnterDataScope("hot_panel")) { }
                gui.EndFrame();
            }

            Assert.AreEqual(1, InternTableCount(gui), "a scope pushed every frame must never be evicted.");
        }
    }
}
