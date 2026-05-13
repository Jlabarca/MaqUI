// SPDX-License-Identifier: MIT
// MaqUI v2 — Phase 1 API skeleton tests.
// TEST.P1 per docs/MAQUI-V2-IMPL.md: every primitive compiles, returns a Node,
// records the right FrameOp kind, scope tracking works, BUG.1 lint anchors hold.

using System.IO;
using Maqui.V2;
using NUnit.Framework;
using UnityEngine;

namespace Maqui.V2.Tests.Editor
{
    public class ApiSkeletonTests
    {
        private Gui _gui;

        [SetUp]
        public void SetUp()
        {
            _gui = new Gui();
            _gui.BeginFrame();
        }

        // ---------- Lifecycle ----------

        [Test]
        public void BeginFrame_Twice_Throws()
        {
            // Already inside BeginFrame from SetUp.
            Assert.Throws<System.InvalidOperationException>(() => _gui.BeginFrame());
        }

        [Test]
        public void EndFrame_WithOpenScope_Throws()
        {
            _gui.EnterDataScope("popup");
            Assert.Throws<System.InvalidOperationException>(() => _gui.EndFrame());
        }

        // ---------- Layout primitives (1.3) ----------

        [Test]
        public void Row_RecordsBeginAndReturnsNonNullNode()
        {
            var n = _gui.Row(width: Size.Expand(), height: Size.Pixels(40));
            Assert.IsFalse(n.IsNone, "Row() must return a non-default Node");
            Assert.AreEqual(1, _gui.Buffer.Count);
            Assert.AreEqual(FrameOpKind.RowBegin, _gui.Buffer.Ops[0].Kind);
        }

        [Test]
        public void Column_RecordsBeginAndReturnsNonNullNode()
        {
            var n = _gui.Column();
            Assert.IsFalse(n.IsNone);
            Assert.AreEqual(FrameOpKind.ColumnBegin, _gui.Buffer.Ops[0].Kind);
        }

        [Test]
        public void Box_RecordsBox()
        {
            var n = _gui.Box(Size.Pixels(100), Size.Pixels(50));
            Assert.IsFalse(n.IsNone);
            Assert.AreEqual(FrameOpKind.Box, _gui.Buffer.Ops[0].Kind);
        }

        [Test]
        public void Spacer_RecordsSpacer()
        {
            var n = _gui.Spacer(Size.Expand());
            Assert.IsFalse(n.IsNone);
            Assert.AreEqual(FrameOpKind.Spacer, _gui.Buffer.Ops[0].Kind);
        }

        [Test]
        public void Row_Then_EndRow_EmitsPairedBeginEnd()
        {
            _gui.Row();
            _gui.EndRow();
            Assert.AreEqual(2, _gui.Buffer.Count);
            Assert.AreEqual(FrameOpKind.RowBegin, _gui.Buffer.Ops[0].Kind);
            Assert.AreEqual(FrameOpKind.RowEnd, _gui.Buffer.Ops[1].Kind);
        }

        [Test]
        public void NestedRowInColumn_EmitsNestedPairs()
        {
            _gui.Column();
            _gui.Row();
            _gui.EndRow();
            _gui.EndColumn();

            var kinds = new[]
            {
                _gui.Buffer.Ops[0].Kind, _gui.Buffer.Ops[1].Kind,
                _gui.Buffer.Ops[2].Kind, _gui.Buffer.Ops[3].Kind,
            };
            CollectionAssert.AreEqual(
                new[] { FrameOpKind.ColumnBegin, FrameOpKind.RowBegin, FrameOpKind.RowEnd, FrameOpKind.ColumnEnd },
                kinds);
        }

        // ---------- Draw primitives (1.4) ----------

        [Test]
        public void DrawRect_Records()
        {
            var n = _gui.DrawRect(new Color32(255, 0, 0, 255), Size.Pixels(10), Size.Pixels(10));
            Assert.IsFalse(n.IsNone);
            Assert.AreEqual(FrameOpKind.DrawRect, _gui.Buffer.Ops[0].Kind);
            Assert.AreEqual(255, _gui.Buffer.Ops[0].Color.r);
        }

        [Test]
        public void DrawText_RecordsTextPayload()
        {
            var n = _gui.DrawText("hello", new Color32(255, 255, 255, 255), fontSize: 20f);
            Assert.IsFalse(n.IsNone);
            Assert.AreEqual(FrameOpKind.DrawText, _gui.Buffer.Ops[0].Kind);
            Assert.AreEqual("hello", _gui.Buffer.Ops[0].Text);
            Assert.AreEqual(20f, _gui.Buffer.Ops[0].FloatA);
        }

        [Test]
        public void DrawLine_Records()
        {
            _gui.DrawLine(new Color32(0, 255, 0, 255), thickness: 2f);
            Assert.AreEqual(FrameOpKind.DrawLine, _gui.Buffer.Ops[0].Kind);
            Assert.AreEqual(2f, _gui.Buffer.Ops[0].FloatA);
        }

        [Test]
        public void DrawCircle_Records()
        {
            _gui.DrawCircle(new Color32(0, 0, 255, 255), radius: 8f);
            Assert.AreEqual(FrameOpKind.DrawCircle, _gui.Buffer.Ops[0].Kind);
            Assert.AreEqual(8f, _gui.Buffer.Ops[0].FloatA);
        }

        // ---------- Interaction primitives (1.5) ----------

        [Test]
        public void OnClick_OnHandle_RecordsInteractionOp_ReturnsFalseInP1()
        {
            var box = _gui.Box();
            bool clicked = box.OnClick();
            Assert.IsFalse(clicked, "P1 interaction primitives are record-only; must return false.");
            Assert.AreEqual(2, _gui.Buffer.Count);
            Assert.AreEqual(FrameOpKind.OnClick, _gui.Buffer.Ops[1].Kind);
            Assert.AreEqual(box.Id, _gui.Buffer.Ops[1].NodeId);
        }

        [Test]
        public void OnHover_OnHold_OnDrag_AllRecord()
        {
            var box = _gui.Box();
            box.OnHover();
            box.OnHold();
            box.OnDrag();
            Assert.AreEqual(FrameOpKind.OnHover, _gui.Buffer.Ops[1].Kind);
            Assert.AreEqual(FrameOpKind.OnHold, _gui.Buffer.Ops[2].Kind);
            Assert.AreEqual(FrameOpKind.OnDrag, _gui.Buffer.Ops[3].Kind);
        }

        [Test]
        public void Interactions_OnNone_AreNoOp()
        {
            // Discard the return — Node.None.OnClick() must not throw or record.
            Assert.IsFalse(Node.None.OnClick());
            Assert.IsFalse(Node.None.OnHover());
            Assert.AreEqual(0, _gui.Buffer.Count);
        }

        // ---------- Data scope (1.6) ----------

        [Test]
        public void EnterExitDataScope_RecordsAndUpdatesPath()
        {
            using (_gui.EnterDataScope("popup"))
            {
                _gui.Box();
                Assert.AreEqual("/popup", _gui.CurrentScopePath);
            }
            // After dispose:
            Assert.AreEqual("/", _gui.CurrentScopePath);

            // Ops: ScopeEnter, Box, ScopeExit.
            Assert.AreEqual(FrameOpKind.ScopeEnter, _gui.Buffer.Ops[0].Kind);
            Assert.AreEqual(FrameOpKind.Box, _gui.Buffer.Ops[1].Kind);
            Assert.AreEqual(FrameOpKind.ScopeExit, _gui.Buffer.Ops[2].Kind);
        }

        [Test]
        public void NestedDataScopes_BuildPath()
        {
            using (_gui.EnterDataScope("outer"))
            using (_gui.EnterDataScope("inner"))
            {
                Assert.AreEqual("/outer/inner", _gui.CurrentScopePath);
            }
            Assert.AreEqual("/", _gui.CurrentScopePath);
        }

        [Test]
        public void ExitDataScope_WhenEmpty_Throws()
        {
            Assert.Throws<System.InvalidOperationException>(() => _gui.ExitDataScope());
        }

        [Test]
        public void EnterDataScope_NullOrEmpty_Throws()
        {
            Assert.Throws<System.ArgumentException>(() => _gui.EnterDataScope(""));
            Assert.Throws<System.ArgumentException>(() => _gui.EnterDataScope(null));
        }

        // ---------- Size primitive shape ----------

        [Test]
        public void Size_Factories_RoundTrip()
        {
            Assert.AreEqual(SizeKind.Fit, Size.Fit().Kind);
            Assert.AreEqual(SizeKind.Expand, Size.Expand(2f).Kind);
            Assert.AreEqual(2f, Size.Expand(2f).Value);
            Assert.AreEqual(SizeKind.Ratio, Size.Ratio(1.5f).Kind);
            Assert.AreEqual(SizeKind.Percentage, Size.Percentage(0.5f).Kind);
            Assert.AreEqual(SizeKind.Pixels, Size.Pixels(200f).Kind);

            // Implicit float -> Size.Pixels:
            Size implicitFromFloat = 300f;
            Assert.AreEqual(SizeKind.Pixels, implicitFromFloat.Kind);
            Assert.AreEqual(300f, implicitFromFloat.Value);
        }

        // ---------- Align constants ----------

        [Test]
        public void Align_Constants_Are_0_Half_1()
        {
            Assert.AreEqual(0f, Align.Start);
            Assert.AreEqual(0.5f, Align.Center);
            Assert.AreEqual(1f, Align.End);
        }

        // ---------- BUG.1 mitigation: lint anchors ----------

        /// <summary>
        /// BUG.1 mitigation per MAQUI-V2-IMPL.md: any reference to prefab-spawning
        /// APIs in v2 source means the LLM-loop authoring guarantee leaks. Tests
        /// scan the V2 runtime files for forbidden strings and fail if any appear.
        /// </summary>
        [Test]
        public void V2Sources_DoNotReference_PrefabSpawningApis()
        {
            string runtimeDir = FindRuntimeV2Dir();
            string[] forbidden = { "Resources.Load", "GameObject.Instantiate", "[AddComponentMenu" };

            foreach (string file in Directory.GetFiles(runtimeDir, "*.cs", SearchOption.AllDirectories))
            {
                string src = File.ReadAllText(file);
                foreach (string needle in forbidden)
                {
                    Assert.IsFalse(
                        src.Contains(needle),
                        $"BUG.1: forbidden API '{needle}' found in {Path.GetFileName(file)}. " +
                        "v2 source files must never reach for prefab-spawning APIs (see MAQUI-V2-IMPL.md BUG.1).");
                }
            }
        }

        private static string FindRuntimeV2Dir()
        {
            // Walk up from the assembly location until we find com.ware.maqui/Runtime/V2.
            // In Unity Editor, Application.dataPath is .../CharqUI/Assets; package lives under
            // Library/PackageCache or directly via Packages/com.ware.maqui. Use the source
            // file's own path as the anchor — works regardless of layout.
            string thisFile = new System.Diagnostics.StackFrame(true).GetFileName();
            // We're at .../com.ware.maqui/Tests/Editor/V2/ApiSkeletonTests.cs;
            // go up to .../com.ware.maqui/, then into Runtime/V2.
            DirectoryInfo dir = new FileInfo(thisFile).Directory; // V2
            dir = dir.Parent; // Editor
            dir = dir.Parent; // Tests
            dir = dir.Parent; // com.ware.maqui
            string runtime = Path.Combine(dir.FullName, "Runtime", "V2");
            Assert.IsTrue(Directory.Exists(runtime), $"Could not locate Runtime/V2 dir from {thisFile}");
            return runtime;
        }
    }
}
