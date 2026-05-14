// SPDX-License-Identifier: MIT
// MaqUI v2 — xUnit tests for the Controls layer (P8 headless subset).

using Maqui.V2.Components;
using Xunit;

namespace Maqui.V2.Tests
{
    public class ButtonTests
    {
        [Fact]
        public void Button_RecordsBoxDrawRectDrawTextOnClickInOrder()
        {
            var gui = new Gui();
            gui.BeginFrame();
            gui.Button("OK");
            gui.EndFrame();

            // Should have at least: Box + DrawRect + DrawText + OnClick.
            var ops = gui.Buffer.Ops;
            Assert.Contains(ops, o => o.Kind == FrameOpKind.Box);
            Assert.Contains(ops, o => o.Kind == FrameOpKind.DrawRect);
            Assert.Contains(ops, o => o.Kind == FrameOpKind.DrawText);
            Assert.Contains(ops, o => o.Kind == FrameOpKind.OnClick);

            // Order: Box first, OnClick last.
            int boxIdx = -1, clickIdx = -1;
            for (int i = 0; i < ops.Count; i++)
            {
                if (boxIdx < 0 && ops[i].Kind == FrameOpKind.Box) boxIdx = i;
                if (ops[i].Kind == FrameOpKind.OnClick) clickIdx = i;
            }
            Assert.True(boxIdx >= 0 && clickIdx > boxIdx, "Box must precede OnClick");
        }

        [Fact]
        public void Button_LabelTextRecordedInDrawText()
        {
            var gui = new Gui();
            gui.BeginFrame();
            gui.Button("Save changes");
            gui.EndFrame();

            bool foundLabel = false;
            foreach (var op in gui.Buffer.Ops)
            {
                if (op.Kind == FrameOpKind.DrawText && op.Text == "Save changes")
                {
                    foundLabel = true;
                    break;
                }
            }
            Assert.True(foundLabel, "Button label must appear in a DrawText FrameOp");
        }

        [Fact]
        public void Button_ReturnsTrueWhenClickedFlagSet()
        {
            var gui = new Gui();
            gui.BeginFrame();
            // Pre-seed the click flag for the next Node id (1, since BeginFrame resets).
            gui.Interactions.SetFlags(1, NodeInteractionFlags.ClickedThisFrame);
            bool clicked = gui.Button("OK");
            gui.EndFrame();
            Assert.True(clicked);
        }

        [Fact]
        public void Button_ReturnsFalseWhenNotClicked()
        {
            var gui = new Gui();
            gui.BeginFrame();
            bool clicked = gui.Button("OK");
            gui.EndFrame();
            Assert.False(clicked);
        }

        [Fact]
        public void Button_NullLabel_DoesNotCrash()
        {
            var gui = new Gui();
            gui.BeginFrame();
            gui.Button(null);
            gui.EndFrame();
            // No assertion — just shouldn't throw.
        }
    }

    public class SliderTests
    {
        [Fact]
        public void ComputeSliderValue_MidPointer_HalfRange()
        {
            float v = MaquiComponents.ComputeSliderValue(50f, 0f, 100f, 0f, 10f);
            Assert.Equal(5f, v, 3);
        }

        [Fact]
        public void ComputeSliderValue_LeftEdge_Min()
        {
            float v = MaquiComponents.ComputeSliderValue(0f, 0f, 100f, 0f, 10f);
            Assert.Equal(0f, v, 3);
        }

        [Fact]
        public void ComputeSliderValue_RightEdge_Max()
        {
            float v = MaquiComponents.ComputeSliderValue(100f, 0f, 100f, 0f, 10f);
            Assert.Equal(10f, v, 3);
        }

        [Fact]
        public void ComputeSliderValue_PointerLeftOfTrack_ClampedToMin()
        {
            float v = MaquiComponents.ComputeSliderValue(-50f, 0f, 100f, 5f, 15f);
            Assert.Equal(5f, v, 3);
        }

        [Fact]
        public void ComputeSliderValue_PointerRightOfTrack_ClampedToMax()
        {
            float v = MaquiComponents.ComputeSliderValue(500f, 0f, 100f, 5f, 15f);
            Assert.Equal(15f, v, 3);
        }

        [Fact]
        public void ComputeSliderValue_NegativeRange_HandlesInvertedMapping()
        {
            // min > max is unusual but should map smoothly (no crash).
            float v = MaquiComponents.ComputeSliderValue(50f, 0f, 100f, 10f, 0f);
            Assert.Equal(5f, v, 3); // halfway between 10 and 0 = 5
        }

        [Fact]
        public void ComputeSliderValue_ZeroWidth_ReturnsMin()
        {
            float v = MaquiComponents.ComputeSliderValue(50f, 0f, 0f, 3f, 7f);
            Assert.Equal(3f, v, 3);
        }

        [Fact]
        public void ComputeSliderValue_NonZeroTrackLeft_OffsetsCorrectly()
        {
            float v = MaquiComponents.ComputeSliderValue(150f, 100f, 100f, 0f, 1f);
            Assert.Equal(0.5f, v, 3);
        }

        [Fact]
        public void Slider_PointerOverride_UpdatesValueWithoutDrag()
        {
            var gui = new Gui();
            gui.BeginFrame();
            float v = gui.Slider("vol", value: 0f, min: 0f, max: 100f, trackWidth: 200f, pointerXOverride: 150f);
            gui.EndFrame();
            Assert.Equal(75f, v, 1);
        }

        [Fact]
        public void Slider_NoInteraction_ReturnsValueUnchanged()
        {
            var gui = new Gui();
            gui.BeginFrame();
            float v = gui.Slider("vol", value: 42f, min: 0f, max: 100f);
            gui.EndFrame();
            Assert.Equal(42f, v, 3);
        }
    }

    public class ToggleTests
    {
        [Fact]
        public void Toggle_NoClick_StateUnchanged()
        {
            var gui = new Gui();
            gui.BeginFrame();
            bool s = gui.Toggle("dark-mode", state: true);
            gui.EndFrame();
            Assert.True(s);
        }

        [Fact]
        public void Toggle_ClickFlagSet_FlipsState()
        {
            var gui = new Gui();
            gui.BeginFrame();
            // Pre-seed ClickedThisFrame for nodeId=1 (the pill Box).
            gui.Interactions.SetFlags(1, NodeInteractionFlags.ClickedThisFrame);
            bool s = gui.Toggle("dark-mode", state: false);
            gui.EndFrame();
            Assert.True(s);
        }

        [Fact]
        public void Toggle_RecordsBoxAndOnClick()
        {
            var gui = new Gui();
            gui.BeginFrame();
            gui.Toggle("dm", state: true);
            gui.EndFrame();
            Assert.Contains(gui.Buffer.Ops, o => o.Kind == FrameOpKind.Box);
            Assert.Contains(gui.Buffer.Ops, o => o.Kind == FrameOpKind.OnClick);
        }

        [Fact]
        public void Toggle_AnimationKeyIncludesUserKey_NotSharedAcrossInstances()
        {
            var gui = new Gui();
            gui.BeginFrame();
            gui.Toggle("a", state: true);
            gui.Toggle("b", state: false);
            gui.EndFrame();

            Assert.Equal(1f, gui.Animations.Get("a-toggle-t").Target, 3);
            Assert.Equal(0f, gui.Animations.Get("b-toggle-t").Target, 3);
        }
    }

    public class ScrollViewTests
    {
        [Fact]
        public void ComputeScrollOffset_NoDelta_Unchanged()
        {
            float o = MaquiComponents.ComputeScrollOffset(current: 30f, delta: 0f, contentHeight: 500f, viewportHeight: 200f);
            Assert.Equal(30f, o, 3);
        }

        [Fact]
        public void ComputeScrollOffset_PositiveDelta_AdvancesDown()
        {
            float o = MaquiComponents.ComputeScrollOffset(current: 30f, delta: 50f, contentHeight: 500f, viewportHeight: 200f);
            Assert.Equal(80f, o, 3);
        }

        [Fact]
        public void ComputeScrollOffset_ClampsToTop()
        {
            float o = MaquiComponents.ComputeScrollOffset(current: 10f, delta: -100f, contentHeight: 500f, viewportHeight: 200f);
            Assert.Equal(0f, o, 3);
        }

        [Fact]
        public void ComputeScrollOffset_ClampsToBottom()
        {
            // max = contentHeight - viewportHeight = 500 - 200 = 300
            float o = MaquiComponents.ComputeScrollOffset(current: 280f, delta: 100f, contentHeight: 500f, viewportHeight: 200f);
            Assert.Equal(300f, o, 3);
        }

        [Fact]
        public void ComputeScrollOffset_ContentFitsInViewport_StaysAtZero()
        {
            float o = MaquiComponents.ComputeScrollOffset(current: 0f, delta: 100f, contentHeight: 100f, viewportHeight: 200f);
            Assert.Equal(0f, o, 3);
        }

        [Fact]
        public void ScrollView_OffsetPersistsAcrossFrames()
        {
            var gui = new Gui();
            // First frame — seed a delta in the pending slot.
            gui.Animations.Set("list-pending-delta", new AnimationFloat(0f, 80f));
            gui.BeginFrame();
            float offset1 = gui.ScrollView("list", contentHeight: 500f, viewportHeight: 200f, drawContent: () => { });
            gui.EndFrame();
            Assert.Equal(80f, offset1, 3);

            // Second frame — no new delta; offset stays.
            gui.BeginFrame();
            float offset2 = gui.ScrollView("list", contentHeight: 500f, viewportHeight: 200f, drawContent: () => { });
            gui.EndFrame();
            Assert.Equal(80f, offset2, 3);
        }

        [Fact]
        public void ScrollView_DeltaConsumedAfterFrame()
        {
            var gui = new Gui();
            gui.Animations.Set("list-pending-delta", new AnimationFloat(0f, 50f));
            gui.BeginFrame();
            gui.ScrollView("list", contentHeight: 500f, viewportHeight: 200f, drawContent: () => { });
            gui.EndFrame();
            // Delta should be reset.
            Assert.Equal(0f, gui.Animations.Get("list-pending-delta").Target, 3);
        }

        [Fact]
        public void ScrollView_RecordsScopeEnterAndExit()
        {
            var gui = new Gui();
            gui.BeginFrame();
            gui.ScrollView("list", contentHeight: 500f, viewportHeight: 200f, drawContent: () => { });
            gui.EndFrame();
            Assert.Contains(gui.Buffer.Ops, o => o.Kind == FrameOpKind.ScopeEnter && o.Text == "list");
            Assert.Contains(gui.Buffer.Ops, o => o.Kind == FrameOpKind.ScopeExit && o.Text == "list");
        }

        [Fact]
        public void ScrollView_InvokesDrawContent()
        {
            var gui = new Gui();
            int callCount = 0;
            gui.BeginFrame();
            gui.ScrollView("list", contentHeight: 500f, viewportHeight: 200f, drawContent: () => callCount++);
            gui.EndFrame();
            Assert.Equal(1, callCount);
        }

        [Fact]
        public void ScrollView_NullDrawContent_DoesNotCrash()
        {
            var gui = new Gui();
            gui.BeginFrame();
            gui.ScrollView("list", contentHeight: 500f, viewportHeight: 200f, drawContent: null);
            gui.EndFrame();
            // No assertion; shouldn't throw.
        }
    }
}
