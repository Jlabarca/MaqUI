// SPDX-License-Identifier: MIT
// MaqUI v2 — xUnit tests for the Controls layer (P8 headless subset).

using Maqui.V2.Components;
using UnityEngine;
using Xunit;

namespace Maqui.V2.Tests
{
    public class ButtonTests
    {
        [Fact]
        public void Button_LabelIsChildOfTheClickableContainer_NotASibling()
        {
            // Regression guard for the "click did nothing" bug. Only Row/Column/
            // ClipBox/Scroll push a reconciler parent — Box is leaf-only. When the
            // label was drawn as a SIBLING of an invisible Box, pointer events on
            // the visible text bubbled past the Box to the outer container and the
            // Box's OnClick() never fired. The label must be nested INSIDE the
            // clickable container so bubbling is guaranteed to reach it.
            var gui = new Gui();
            gui.BeginFrame();
            gui.Button("OK");
            gui.EndFrame();

            var ops = gui.Buffer.Ops;
            int columnBegin = IndexOf(ops, FrameOpKind.ColumnBegin);
            int drawText = IndexOf(ops, FrameOpKind.DrawText);
            int columnEnd = IndexOf(ops, FrameOpKind.ColumnEnd);
            int onClick = IndexOf(ops, FrameOpKind.OnClick);

            Assert.True(columnBegin >= 0, "Button must open a Column (a real parent-pushing container)");
            Assert.True(drawText > columnBegin && drawText < columnEnd,
                "label must be recorded BETWEEN ColumnBegin/ColumnEnd — i.e. a child, not a sibling");
            Assert.True(onClick > columnBegin, "OnClick must target the container");

            // The clickable container is the node OnClick refers to.
            Assert.Equal(ops[columnBegin].NodeId, ops[onClick].NodeId);
        }

        [Fact]
        public void Button_ContainerCarriesTheBackground_NotASiblingRect()
        {
            // The background must be painted on the container itself. A sibling
            // DrawRect cannot sit *behind* a flex container's children.
            var gui = new Gui();
            gui.BeginFrame();
            gui.Button("OK");
            gui.EndFrame();

            var ops = gui.Buffer.Ops;
            int columnBegin = IndexOf(ops, FrameOpKind.ColumnBegin);
            Assert.True(ops[columnBegin].Color.a > 0, "container must carry an opaque background color");
            Assert.DoesNotContain(ops, o => o.Kind == FrameOpKind.DrawRect);
        }

        [Fact]
        public void PeekNextNodeId_MatchesIdOfTheNextCreatedNode()
        {
            var gui = new Gui();
            gui.BeginFrame();

            int peeked = gui.PeekNextNodeId();
            var actual = gui.Box();
            Assert.Equal(peeked, actual.Id);

            // Still correct further into the frame, not just at id 1.
            int peeked2 = gui.PeekNextNodeId();
            var actual2 = gui.Box();
            Assert.Equal(peeked2, actual2.Id);
            Assert.NotEqual(peeked, peeked2);

            gui.EndFrame();
        }

        [Fact]
        public void PeekNextNodeId_IsStableAcrossFramesForTheSameBuildOrder()
        {
            // The property the hover tint depends on: id N denotes the same
            // logical node every frame, so flags written by last frame's pointer
            // event are the right ones to read for this frame's node.
            var gui = new Gui();

            gui.BeginFrame();
            gui.Box();
            int peekFrame1 = gui.PeekNextNodeId();
            gui.EndFrame();

            gui.BeginFrame();
            gui.Box();
            int peekFrame2 = gui.PeekNextNodeId();
            gui.EndFrame();

            Assert.Equal(peekFrame1, peekFrame2);
        }

        [Fact]
        public void Button_Idle_UsesBaseTint()
        {
            var gui = new Gui();
            gui.BeginFrame();
            gui.Button("OK");
            gui.EndFrame();

            Assert.Equal(MaquiTheme.ButtonBase, ContainerColor(gui));
        }

        [Fact]
        public void Button_Hovered_UsesHoverTint()
        {
            var gui = new Gui();
            gui.BeginFrame();
            // nodeId 1 = the container Column the Button is about to create.
            gui.Interactions.SetFlags(1, NodeInteractionFlags.Hover);
            gui.Button("OK");
            gui.EndFrame();

            Assert.Equal(MaquiTheme.ButtonHover, ContainerColor(gui));
        }

        [Fact]
        public void Button_Pressed_UsesActiveTint_EvenWhileAlsoHovered()
        {
            var gui = new Gui();
            gui.BeginFrame();
            // A pointer held down is always also hovering — press must win.
            gui.Interactions.SetFlags(1, NodeInteractionFlags.Hover | NodeInteractionFlags.Active);
            gui.Button("OK");
            gui.EndFrame();

            Assert.Equal(MaquiTheme.ButtonActive, ContainerColor(gui));
        }

        [Fact]
        public void Button_HoverOnANeighbour_DoesNotTintThisButton()
        {
            var gui = new Gui();
            gui.BeginFrame();
            // Button = 2 nodes (container Column + label), so button #1 owns ids
            // 1-2 and button #2's container is id 3. Hover only the second.
            gui.Interactions.SetFlags(3, NodeInteractionFlags.Hover);
            gui.Button("first");
            gui.Button("second");
            gui.EndFrame();

            var columns = new System.Collections.Generic.List<FrameOp>();
            foreach (var op in gui.Buffer.Ops)
                if (op.Kind == FrameOpKind.ColumnBegin) columns.Add(op);

            Assert.Equal(2, columns.Count);
            Assert.Equal(MaquiTheme.ButtonBase, columns[0].Color);
            Assert.Equal(MaquiTheme.ButtonHover, columns[1].Color);
        }

        private static Color32 ContainerColor(Gui gui)
        {
            var ops = gui.Buffer.Ops;
            return ops[IndexOf(ops, FrameOpKind.ColumnBegin)].Color;
        }

        private static int IndexOf(System.Collections.Generic.IReadOnlyList<FrameOp> ops, FrameOpKind kind)
        {
            for (int i = 0; i < ops.Count; i++)
                if (ops[i].Kind == kind) return i;
            return -1;
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
            // Pre-seed ClickedThisFrame for nodeId=1 (the pill Row).
            gui.Interactions.SetFlags(1, NodeInteractionFlags.ClickedThisFrame);
            bool s = gui.Toggle("dark-mode", state: false);
            gui.EndFrame();
            Assert.True(s);
        }

        [Fact]
        public void Toggle_KnobIsChildOfTheClickablePill_NotASibling()
        {
            // Same regression guard as Button: the pill was a leaf-only Box with
            // the visible body + knob as SIBLINGS, so every click bubbled past it
            // to the outer container and OnClick() never fired.
            var gui = new Gui();
            gui.BeginFrame();
            gui.Toggle("dm", state: true);
            gui.EndFrame();

            var ops = gui.Buffer.Ops;
            int rowBegin = IndexOf(ops, FrameOpKind.RowBegin);
            int knob = IndexOf(ops, FrameOpKind.DrawRect);
            int rowEnd = IndexOf(ops, FrameOpKind.RowEnd);
            int onClick = IndexOf(ops, FrameOpKind.OnClick);

            Assert.True(rowBegin >= 0, "pill must be a Row (a real parent-pushing container)");
            Assert.True(knob > rowBegin && knob < rowEnd, "knob must be a child of the pill, not a sibling");
            Assert.Equal(ops[rowBegin].NodeId, ops[onClick].NodeId);

            // The pill body color must ride on the container itself.
            Assert.True(ops[rowBegin].Color.a > 0);
            // ...and the knob must be centered rather than stretched to fill.
            Assert.Equal(AlignItems.Center, ops[rowBegin].AlignItems);
        }

        [Fact]
        public void Toggle_OnAndOffUseDistinctPillColors()
        {
            var gui = new Gui();

            gui.BeginFrame();
            gui.Toggle("dm", state: true);
            gui.EndFrame();
            var onColor = gui.Buffer.Ops[IndexOf(gui.Buffer.Ops, FrameOpKind.RowBegin)].Color;

            gui.BeginFrame();
            gui.Toggle("dm2", state: false);
            gui.EndFrame();
            var offColor = gui.Buffer.Ops[IndexOf(gui.Buffer.Ops, FrameOpKind.RowBegin)].Color;

            Assert.Equal(MaquiTheme.ToggleOn, onColor);
            Assert.Equal(MaquiTheme.ToggleOff, offColor);
        }

        [Fact]
        public void ComputeKnobOffset_OffIsFlushLeft()
        {
            Assert.Equal(0f, MaquiComponents.ComputeKnobOffset(0f), 3);
        }

        [Fact]
        public void ComputeKnobOffset_OnIsFlushRight_WithinThePillsInnerWidth()
        {
            // 80 wide - 2*10 padding - 24 knob = 36 of travel.
            Assert.Equal(36f, MaquiComponents.ComputeKnobOffset(1f), 3);
        }

        [Fact]
        public void ComputeKnobOffset_MidTravel_IsHalfway()
        {
            Assert.Equal(18f, MaquiComponents.ComputeKnobOffset(0.5f), 3);
        }

        [Fact]
        public void ComputeKnobOffset_SpringOvershoot_ClampedInsideThePill()
        {
            // The spring animator overshoots past its target; the knob must not
            // escape the pill (or invert) when it does.
            Assert.Equal(36f, MaquiComponents.ComputeKnobOffset(1.4f), 3);
            Assert.Equal(0f, MaquiComponents.ComputeKnobOffset(-0.3f), 3);
        }

        [Fact]
        public void Toggle_KnobActuallyMoves_WhenAnimationReachesTheOnState()
        {
            // Guards the bug where `t` was computed but never consumed — the knob
            // was drawn at a fixed position and the toggle never visibly moved.
            var gui = new Gui();

            gui.BeginFrame();
            gui.Toggle("dm", state: false);
            gui.EndFrame();
            float offAt = SpacerWidth(gui);

            // Settle the spring at the "on" target.
            gui.BeginFrame();
            gui.Toggle("dm", state: true);
            gui.EndFrame();
            for (int i = 0; i < 400; i++) gui.TickAnimations(0.016f);

            gui.BeginFrame();
            gui.Toggle("dm", state: true);
            gui.EndFrame();
            float onAt = SpacerWidth(gui);

            Assert.Equal(0f, offAt, 3);
            Assert.True(onAt > offAt, $"knob must slide right when toggled on (off={offAt}, on={onAt})");
            Assert.Equal(36f, onAt, 1);
        }

        private static float SpacerWidth(Gui gui)
        {
            var ops = gui.Buffer.Ops;
            return ops[IndexOf(ops, FrameOpKind.Spacer)].FloatB;
        }

        private static int IndexOf(System.Collections.Generic.IReadOnlyList<FrameOp> ops, FrameOpKind kind)
        {
            for (int i = 0; i < ops.Count; i++)
                if (ops[i].Kind == kind) return i;
            return -1;
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
