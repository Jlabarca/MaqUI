// SPDX-License-Identifier: MIT
// MaqUI v2 — AnimationStoreHasActiveTests (NUnit, Maqui.Tests.Runtime).
//
// FRAME-BUDGET.2.6: AnimationStore.HasActive / Gui.HasActiveAnimations are a forced-dirty
// source for the opt-in dirty-rebuild optimisation — a caller must treat any unsettled tween
// as "cannot skip this frame". These tests pin the settle/unsettle transition directly against
// AnimationStore, without a scene or a Gui instance.

using NUnit.Framework;

namespace Maqui.Tests.Runtime
{
    public class AnimationStoreHasActiveTests
    {
        [Test]
        public void Empty_store_has_no_active_animation()
        {
            var store = new AnimationStore();
            Assert.IsFalse(store.HasActive);
        }

        [Test]
        public void Freshly_animated_value_at_target_is_not_active()
        {
            var store = new AnimationStore();
            // First call initializes Current == Target — no animation yet.
            store.Animate("k", 10f);
            Assert.IsFalse(store.HasActive);
        }

        [Test]
        public void Retargeted_value_is_active_until_ticked_to_settle()
        {
            var store = new AnimationStore();
            store.Animate("k", 0f);
            store.Animate("k", 100f); // retarget away from Current — now unsettled
            Assert.IsTrue(store.HasActive);

            for (var i = 0; i < 2000 && store.HasActive; i++)
                store.TickAll(1f / 60f);

            Assert.IsFalse(store.HasActive, "animation should have settled within 2000 ticks");
        }

        [Test]
        public void One_active_animation_among_several_settled_ones_still_reports_active()
        {
            var store = new AnimationStore();
            store.Animate("settled", 5f);
            store.Animate("moving", 0f);
            store.Animate("moving", 50f);
            Assert.IsTrue(store.HasActive);
        }

        [Test]
        public void TickAll_advances_every_tracked_value_across_repeated_frames()
        {
            // TickAll snapshots keys into a buffer it reuses between frames; this pins that the
            // reuse still visits every value, including ones added after the first tick.
            var store = new AnimationStore();
            for (var k = 0; k < 8; k++) { store.Animate("k" + k, 0f); store.Animate("k" + k, 100f); }
            store.TickAll(1f / 60f);
            store.Animate("late", 0f); store.Animate("late", 100f);
            for (var i = 0; i < 10; i++) store.TickAll(1f / 60f);
            for (var k = 0; k < 8; k++) Assert.Greater(store.Get("k" + k).Current, 0f);
            Assert.Greater(store.Get("late").Current, 0f);
        }
    }

}
