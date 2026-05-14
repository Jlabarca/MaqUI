// SPDX-License-Identifier: MIT
// MaqUI v2 — xUnit tests for AnimationFloat + AnimationStore. P4.

using Xunit;

namespace Maqui.V2.Tests
{
    public class AnimationFloatTests
    {
        [Fact]
        public void Default_Constructor_StartsAtZero()
        {
            var anim = new AnimationFloat();
            Assert.Equal(0f, anim.Current);
            Assert.Equal(0f, anim.Target);
            Assert.Equal(0f, anim.Velocity);
        }

        [Fact]
        public void Tick_PullsCurrentTowardTarget()
        {
            var anim = new AnimationFloat(0f, 1f);
            float prev = anim.Current;
            for (int i = 0; i < 5; i++)
            {
                anim.Tick(1f / 60f);
                Assert.True(anim.Current > prev, $"frame {i}: expected current to advance, was {anim.Current} (prev {prev})");
                prev = anim.Current;
            }
        }

        [Fact]
        public void Tick_SettlesAtTarget_AfterEnoughFrames()
        {
            var anim = new AnimationFloat(0f, 1f);
            for (int i = 0; i < 240; i++) anim.Tick(1f / 60f); // 4 seconds
            Assert.True(anim.IsSettled(), $"expected settled after 4s, was current={anim.Current} vel={anim.Velocity}");
            Assert.InRange(anim.Current, 0.99f, 1.01f);
        }

        [Fact]
        public void Tick_ZeroDt_IsNoOp()
        {
            var anim = new AnimationFloat(0f, 1f);
            anim.Tick(0f);
            Assert.Equal(0f, anim.Current);
            Assert.Equal(0f, anim.Velocity);
        }

        [Fact]
        public void Tick_NegativeDt_IsNoOp()
        {
            var anim = new AnimationFloat(0f, 1f);
            anim.Tick(-0.5f);
            Assert.Equal(0f, anim.Current);
            Assert.Equal(0f, anim.Velocity);
        }

        [Fact]
        public void Tick_ClampsLargeDt_DoesNotBlowUp()
        {
            var anim = new AnimationFloat(0f, 1f);
            anim.Tick(10f); // wildly large
            // Should be clamped to MaxStableDt internally — current bounded.
            Assert.True(anim.Current < 10f, $"large dt should be clamped, got current={anim.Current}");
            Assert.True(float.IsFinite(anim.Current));
            Assert.True(float.IsFinite(anim.Velocity));
        }

        [Fact]
        public void SnapToTarget_ZeroesVelocity()
        {
            var anim = new AnimationFloat(0f, 5f);
            for (int i = 0; i < 3; i++) anim.Tick(1f / 60f);
            Assert.NotEqual(0f, anim.Velocity);

            anim.SnapToTarget();
            Assert.Equal(5f, anim.Current);
            Assert.Equal(0f, anim.Velocity);
        }

        [Fact]
        public void IsSettled_TrueWhenAtTargetAndZeroVel()
        {
            var anim = new AnimationFloat(1f, 1f);
            Assert.True(anim.IsSettled());
        }

        [Fact]
        public void IsSettled_FalseDuringMotion()
        {
            var anim = new AnimationFloat(0f, 1f);
            anim.Tick(1f / 60f);
            Assert.False(anim.IsSettled());
        }

        [Fact]
        public void Overdamped_Default_DoesNotOvershoot()
        {
            var anim = new AnimationFloat(0f, 1f);
            float maxSeen = 0f;
            for (int i = 0; i < 240; i++)
            {
                anim.Tick(1f / 60f);
                if (anim.Current > maxSeen) maxSeen = anim.Current;
            }
            // Default stiffness=200, damping=20 is overdamped — should not exceed target by much.
            Assert.True(maxSeen < 1.05f, $"expected no significant overshoot, saw max={maxSeen}");
        }
    }

    public class AnimationStoreTests
    {
        [Fact]
        public void Animate_FirstCall_InitializesAtTarget_NoMotion()
        {
            var store = new AnimationStore();
            float v = store.Animate("k", 5f);
            Assert.Equal(5f, v);
            Assert.Equal(0f, store.Get("k").Velocity);
        }

        [Fact]
        public void Animate_SecondCall_UpdatesTarget()
        {
            var store = new AnimationStore();
            store.Animate("k", 0f);
            store.Animate("k", 10f);
            Assert.Equal(10f, store.Get("k").Target);
            // Current still at original (0) until TickAll fires.
            Assert.Equal(0f, store.Get("k").Current);
        }

        [Fact]
        public void TickAll_AdvancesAllTrackedAnimations()
        {
            var store = new AnimationStore();
            store.Animate("a", 0f); store.Animate("a", 1f);
            store.Animate("b", 0f); store.Animate("b", 2f);

            for (int i = 0; i < 120; i++) store.TickAll(1f / 60f);

            Assert.InRange(store.Get("a").Current, 0.95f, 1.05f);
            Assert.InRange(store.Get("b").Current, 1.9f, 2.1f);
        }

        [Fact]
        public void TickAll_ZeroDt_IsNoOp()
        {
            var store = new AnimationStore();
            store.Animate("k", 0f); store.Animate("k", 1f);
            store.TickAll(0f);
            Assert.Equal(0f, store.Get("k").Current);
        }

        [Fact]
        public void Remove_ExistingKey_ReturnsTrue()
        {
            var store = new AnimationStore();
            store.Animate("k", 1f);
            Assert.True(store.Remove("k"));
            Assert.Equal(0, store.Count);
        }

        [Fact]
        public void Remove_MissingKey_ReturnsFalse()
        {
            var store = new AnimationStore();
            Assert.False(store.Remove("missing"));
        }

        [Fact]
        public void Get_MissingKey_ReturnsDefault()
        {
            var store = new AnimationStore();
            var anim = store.Get("nope");
            Assert.Equal(0f, anim.Current);
            Assert.Equal(0f, anim.Target);
        }

        [Fact]
        public void Set_OverwritesValue()
        {
            var store = new AnimationStore();
            store.Set("k", new AnimationFloat(3f, 4f));
            Assert.Equal(3f, store.Get("k").Current);
            Assert.Equal(4f, store.Get("k").Target);
        }
    }

    public class GuiAnimateTests
    {
        [Fact]
        public void Animate_OnGui_ProxiesToStore()
        {
            var gui = new Gui();
            float v = gui.Animate("scale", 1f);
            Assert.Equal(1f, v);
            Assert.Equal(1f, gui.Animations.Get("scale").Current);
        }

        [Fact]
        public void TickAnimations_AdvancesTowardTarget()
        {
            var gui = new Gui();
            gui.Animate("scale", 0f);
            gui.Animate("scale", 1f);
            for (int i = 0; i < 120; i++) gui.TickAnimations(1f / 60f);
            Assert.InRange(gui.Animations.Get("scale").Current, 0.95f, 1.05f);
        }

        [Fact]
        public void Animations_SurviveAcrossFrames()
        {
            var gui = new Gui();
            // Use a slower spring so we land squarely mid-flight after a few ticks.
            gui.Animate("scale", 0f, stiffness: 20f, damping: 8f);
            gui.Animate("scale", 1f, stiffness: 20f, damping: 8f);
            for (int i = 0; i < 5; i++) gui.TickAnimations(1f / 60f);
            float midFrame = gui.Animations.Get("scale").Current;
            Assert.True(midFrame > 0f && midFrame < 1f, $"expected mid-flight value, got {midFrame}");

            // Start a new frame — animation state should persist.
            gui.BeginFrame();
            gui.EndFrame();

            Assert.Equal(midFrame, gui.Animations.Get("scale").Current);
        }
    }
}
