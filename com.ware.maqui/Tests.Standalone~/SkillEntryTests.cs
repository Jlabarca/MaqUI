// SPDX-License-Identifier: MIT
// MaqUI v2 — SkillEntry (V2-PRO-SKIN.3.2).
//
// Asserts the learned-outline class toggle, the level/max text, the stepper
// wiring, and reconciler materialization (recording is not rendering).

using System.Linq;
using Maqui;
using Maqui.Components;
using Xunit;

namespace Maqui.Tests
{
    public class SkillEntryTests
    {
        [Fact]
        public void SkillEntry_DrawsNameAndLevel()
        {
            var gui = new Gui();
            gui.BeginFrame();
            gui.SkillEntry("bash", "skill_bash", "Bash", 10, 10, learned: true);
            gui.EndFrame();

            var texts = gui.Buffer.Ops.Where(o => o.Kind == FrameOpKind.DrawText).Select(o => o.Text).ToList();
            Assert.Contains("Bash", texts);
            Assert.Contains("10 / 10", texts);
        }

        [Fact]
        public void SkillEntry_Learned_AppliesLearnedClassToContainer()
        {
            var gui = new Gui();
            gui.BeginFrame();
            gui.SkillEntry("bash", "skill_bash", "Bash", 5, 10, learned: true,
                className: "ro-skill", learnedClassName: "ro-skill--learned");
            gui.EndFrame();

            // The outermost container is the first ColumnBegin.
            var container = gui.Buffer.Ops.First(o => o.Kind == FrameOpKind.ColumnBegin);
            Assert.Equal("ro-skill--learned", container.ClassName);
        }

        [Fact]
        public void SkillEntry_NotLearned_UsesBaseClass()
        {
            var gui = new Gui();
            gui.BeginFrame();
            gui.SkillEntry("bash", "skill_bash", "Bash", 0, 10, learned: false,
                className: "ro-skill", learnedClassName: "ro-skill--learned");
            gui.EndFrame();

            var container = gui.Buffer.Ops.First(o => o.Kind == FrameOpKind.ColumnBegin);
            Assert.Equal("ro-skill", container.ClassName);
        }

        [Fact]
        public void SkillEntry_NoClick_ReturnsZeroDelta()
        {
            var gui = new Gui();
            gui.BeginFrame();
            int delta = gui.SkillEntry("bash", "skill_bash", "Bash", 5, 10, learned: true);
            gui.EndFrame();
            Assert.Equal(0, delta);
        }

        [Fact]
        public void SkillEntry_ShowStepperFalse_OmitsStepperButtons()
        {
            var gui = new Gui();
            gui.BeginFrame();
            gui.SkillEntry("bash", "skill_bash", "Bash", 5, 10, learned: true, showStepper: false);
            gui.EndFrame();

            // Stepper renders two OnClick-bearing Column buttons; with showStepper
            // false, zero of those should exist — only the icon/name/level containers.
            int clickable = gui.Buffer.Ops.Count(o => o.Kind == FrameOpKind.OnClick);
            Assert.Equal(0, clickable);
        }

        [Fact]
        public void SkillEntry_ShowStepperFalse_StillDrawsLevelText()
        {
            var gui = new Gui();
            gui.BeginFrame();
            gui.SkillEntry("bash", "skill_bash", "Bash", 3, 10, learned: true, showStepper: false);
            gui.EndFrame();

            // V2-UI-PARITY.5.2: legacy hides only the button, never the "N / max" label.
            var texts = gui.Buffer.Ops.Where(o => o.Kind == FrameOpKind.DrawText).Select(o => o.Text).ToList();
            Assert.Contains("3 / 10", texts);
        }

        [Fact]
        public void SkillEntry_ShowStepperDefaultsTrue_ExistingCallersUnaffected()
        {
            var gui = new Gui();
            gui.BeginFrame();
            gui.SkillEntry("bash", "skill_bash", "Bash", 5, 10, learned: true);
            gui.EndFrame();

            int clickable = gui.Buffer.Ops.Count(o => o.Kind == FrameOpKind.OnClick);
            Assert.True(clickable > 0, "default showStepper=true must still render the stepper buttons");
        }

        [Fact]
        public void SkillEntry_IsMaterializedByTheReconciler()
        {
            var gui = new Gui();
            var backend = new TestBackend();
            gui.BeginFrame();
            gui.SkillEntry("bash", "skill_bash", "Bash", 5, 10, learned: true,
                className: "ro-skill", learnedClassName: "ro-skill--learned");
            gui.EndFrame();
            gui.Render(backend);

            int created = backend.Events.Count(e => e.Kind == BackendEventKind.CreateElement);
            Assert.True(created >= 4, "container + icon + name + level must materialize");
        }
    }
}
