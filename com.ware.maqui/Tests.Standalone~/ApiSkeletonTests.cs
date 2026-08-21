// SPDX-License-Identifier: MIT
// MaqUI v2 — Phase 1 API skeleton tests (xUnit, standalone).
//
// Ports the original NUnit ApiSkeletonTests.cs to xUnit so it runs in
// `dotnet test` without Unity. The Unity NUnit harness will be reintroduced
// in P3/P4/P5/P6 for tests that genuinely need Unity (PlayMode, render,
// UnityEngine.Object lifecycle).
//
// TEST.P1 per docs/MAQUI-V2-IMPL.md: every primitive compiles, returns a
// Node, records the right FrameOp kind, scope tracking works, BUG.1 lint
// anchors hold.

using System;
using System.IO;
using System.Runtime.CompilerServices;
using Maqui;
using UnityEngine;
using Xunit;

namespace Maqui.Tests
{
    public class ApiSkeletonTests
    {
        private readonly Gui _gui;

        // xUnit constructs a fresh instance per [Fact] — equivalent to NUnit [SetUp].
        public ApiSkeletonTests()
        {
            _gui = new Gui();
            _gui.BeginFrame();
        }

        // ---------- Lifecycle ----------

        [Fact]
        public void BeginFrame_Twice_Throws()
        {
            Assert.Throws<InvalidOperationException>(() => _gui.BeginFrame());
        }

        [Fact]
        public void EndFrame_WithOpenScope_Throws()
        {
            _gui.EnterDataScope("popup");
            Assert.Throws<InvalidOperationException>(() => _gui.EndFrame());
        }

        // ---------- Layout primitives (1.3) ----------

        [Fact]
        public void Row_RecordsBeginAndReturnsNonNullNode()
        {
            var n = _gui.Row(width: Size.Expand(), height: Size.Pixels(40));
            Assert.False(n.IsNone);
            Assert.Equal(1, _gui.Buffer.Count);
            Assert.Equal(FrameOpKind.RowBegin, _gui.Buffer.Ops[0].Kind);
        }

        [Fact]
        public void Column_RecordsBeginAndReturnsNonNullNode()
        {
            var n = _gui.Column();
            Assert.False(n.IsNone);
            Assert.Equal(FrameOpKind.ColumnBegin, _gui.Buffer.Ops[0].Kind);
        }

        [Fact]
        public void Box_RecordsBox()
        {
            var n = _gui.Box(Size.Pixels(100), Size.Pixels(50));
            Assert.False(n.IsNone);
            Assert.Equal(FrameOpKind.Box, _gui.Buffer.Ops[0].Kind);
        }

        [Fact]
        public void Containers_DefaultToTransparentAndStretch()
        {
            // alpha 0 == "no background"; Stretch is flex's own default. Both must
            // stay the default so the no-arg overloads are behaviour-preserving.
            _gui.Row();
            _gui.Column();
            _gui.ScrollBox();

            foreach (var op in _gui.Buffer.Ops)
            {
                Assert.Equal(0, op.Color.a);
                Assert.Equal(AlignItems.Stretch, op.AlignItems);
            }
        }

        [Fact]
        public void Row_CarriesBackgroundAndAlignItems()
        {
            _gui.Row(Size.Pixels(300), Size.Pixels(36),
                new UnityEngine.Color32(10, 20, 30, 255), AlignItems.Center);

            var op = _gui.Buffer.Ops[0];
            Assert.Equal(FrameOpKind.RowBegin, op.Kind);
            Assert.Equal(new UnityEngine.Color32(10, 20, 30, 255), op.Color);
            Assert.Equal(AlignItems.Center, op.AlignItems);
        }

        [Fact]
        public void Column_CarriesBackgroundAndAlignItems()
        {
            _gui.Column(Size.Pixels(300), default,
                new UnityEngine.Color32(1, 2, 3, 255), AlignItems.End);

            var op = _gui.Buffer.Ops[0];
            Assert.Equal(FrameOpKind.ColumnBegin, op.Kind);
            Assert.Equal(new UnityEngine.Color32(1, 2, 3, 255), op.Color);
            Assert.Equal(AlignItems.End, op.AlignItems);
        }

        [Fact]
        public void ScrollBox_CarriesBackground()
        {
            _gui.ScrollBox(default, Size.Pixels(420), new UnityEngine.Color32(4, 5, 6, 255));

            var op = _gui.Buffer.Ops[0];
            Assert.Equal(FrameOpKind.ScrollBegin, op.Kind);
            Assert.Equal(new UnityEngine.Color32(4, 5, 6, 255), op.Color);
            Assert.Equal(SizeKind.Pixels, (SizeKind)(int)op.FloatC);
            Assert.Equal(420f, op.FloatD, 3);
        }

        [Fact]
        public void ScrollBox_MaxHeightIsCarriedAndLeavesHeightUnset()
        {
            // A max-height scroll region must NOT also pin a fixed height — that's the
            // whole point (hug content, cap at the max, scroll past it). If both were
            // set the well would reserve its full box around tiny content.
            _gui.ScrollBox(default, default, new UnityEngine.Color32(4, 5, 6, 255), maxHeight: 420f);

            var op = _gui.Buffer.Ops[0];
            Assert.Equal(420f, op.MaxHeight, 3);
            Assert.NotEqual(SizeKind.Pixels, (SizeKind)(int)op.FloatC);
        }

        [Fact]
        public void MaxHeight_DefaultsToUnset()
        {
            _gui.ScrollBox();
            _gui.Row();
            _gui.Column();
            foreach (var op in _gui.Buffer.Ops)
                Assert.Equal(0f, op.MaxHeight, 3);
        }

        [Fact]
        public void Spacer_RecordsSpacer()
        {
            var n = _gui.Spacer(Size.Expand());
            Assert.False(n.IsNone);
            Assert.Equal(FrameOpKind.Spacer, _gui.Buffer.Ops[0].Kind);
        }

        [Fact]
        public void Row_Then_EndRow_EmitsPairedBeginEnd()
        {
            _gui.Row();
            _gui.EndRow();
            Assert.Equal(2, _gui.Buffer.Count);
            Assert.Equal(FrameOpKind.RowBegin, _gui.Buffer.Ops[0].Kind);
            Assert.Equal(FrameOpKind.RowEnd, _gui.Buffer.Ops[1].Kind);
        }

        [Fact]
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
            Assert.Equal(
                new[] { FrameOpKind.ColumnBegin, FrameOpKind.RowBegin, FrameOpKind.RowEnd, FrameOpKind.ColumnEnd },
                kinds);
        }

        // ---------- Draw primitives (1.4) ----------

        [Fact]
        public void DrawRect_Records()
        {
            var n = _gui.DrawRect(new Color32(255, 0, 0, 255), Size.Pixels(10), Size.Pixels(10));
            Assert.False(n.IsNone);
            Assert.Equal(FrameOpKind.DrawRect, _gui.Buffer.Ops[0].Kind);
            Assert.Equal((byte)255, _gui.Buffer.Ops[0].Color.r);
        }

        [Fact]
        public void DrawText_RecordsTextPayload()
        {
            var n = _gui.DrawText("hello", new Color32(255, 255, 255, 255), fontSize: 20f);
            Assert.False(n.IsNone);
            Assert.Equal(FrameOpKind.DrawText, _gui.Buffer.Ops[0].Kind);
            Assert.Equal("hello", _gui.Buffer.Ops[0].Text);
            Assert.Equal(20f, _gui.Buffer.Ops[0].FloatA);
        }

        [Fact]
        public void DrawLine_Records()
        {
            _gui.DrawLine(new Color32(0, 255, 0, 255), thickness: 2f);
            Assert.Equal(FrameOpKind.DrawLine, _gui.Buffer.Ops[0].Kind);
            Assert.Equal(2f, _gui.Buffer.Ops[0].FloatA);
        }

        [Fact]
        public void DrawCircle_Records()
        {
            _gui.DrawCircle(new Color32(0, 0, 255, 255), radius: 8f);
            Assert.Equal(FrameOpKind.DrawCircle, _gui.Buffer.Ops[0].Kind);
            Assert.Equal(8f, _gui.Buffer.Ops[0].FloatA);
        }

        // ---------- Interaction primitives (1.5) ----------

        [Fact]
        public void OnClick_OnHandle_RecordsInteractionOp_ReturnsFalseInP1()
        {
            var box = _gui.Box();
            bool clicked = box.OnClick();
            Assert.False(clicked); // P1: record-only.
            Assert.Equal(2, _gui.Buffer.Count);
            Assert.Equal(FrameOpKind.OnClick, _gui.Buffer.Ops[1].Kind);
            Assert.Equal(box.Id, _gui.Buffer.Ops[1].NodeId);
        }

        [Fact]
        public void OnHover_OnHold_OnDrag_AllRecord()
        {
            var box = _gui.Box();
            box.OnHover();
            box.OnHold();
            box.OnDrag();
            Assert.Equal(FrameOpKind.OnHover, _gui.Buffer.Ops[1].Kind);
            Assert.Equal(FrameOpKind.OnHold, _gui.Buffer.Ops[2].Kind);
            Assert.Equal(FrameOpKind.OnDrag, _gui.Buffer.Ops[3].Kind);
        }

        [Fact]
        public void Interactions_OnNone_AreNoOp()
        {
            Assert.False(Node.None.OnClick());
            Assert.False(Node.None.OnHover());
            Assert.Equal(0, _gui.Buffer.Count);
        }

        // ---------- Data scope (1.6) ----------

        [Fact]
        public void EnterExitDataScope_RecordsAndUpdatesPath()
        {
            using (_gui.EnterDataScope("popup"))
            {
                _gui.Box();
                Assert.Equal("/popup", _gui.CurrentScopePath);
            }
            Assert.Equal("/", _gui.CurrentScopePath);

            Assert.Equal(FrameOpKind.ScopeEnter, _gui.Buffer.Ops[0].Kind);
            Assert.Equal(FrameOpKind.Box, _gui.Buffer.Ops[1].Kind);
            Assert.Equal(FrameOpKind.ScopeExit, _gui.Buffer.Ops[2].Kind);
        }

        [Fact]
        public void NestedDataScopes_BuildPath()
        {
            using (_gui.EnterDataScope("outer"))
            using (_gui.EnterDataScope("inner"))
            {
                Assert.Equal("/outer/inner", _gui.CurrentScopePath);
            }
            Assert.Equal("/", _gui.CurrentScopePath);
        }

        [Fact]
        public void ExitDataScope_WhenEmpty_Throws()
        {
            Assert.Throws<InvalidOperationException>(() => _gui.ExitDataScope());
        }

        [Fact]
        public void EnterDataScope_NullOrEmpty_Throws()
        {
            Assert.Throws<ArgumentException>(() => _gui.EnterDataScope(""));
            Assert.Throws<ArgumentException>(() => _gui.EnterDataScope(null));
        }

        // ---------- Size primitive shape ----------

        [Fact]
        public void Size_Factories_RoundTrip()
        {
            Assert.Equal(SizeKind.Fit, Size.Fit().Kind);
            Assert.Equal(SizeKind.Expand, Size.Expand(2f).Kind);
            Assert.Equal(2f, Size.Expand(2f).Value);
            Assert.Equal(SizeKind.Ratio, Size.Ratio(1.5f).Kind);
            Assert.Equal(SizeKind.Percentage, Size.Percentage(0.5f).Kind);
            Assert.Equal(SizeKind.Pixels, Size.Pixels(200f).Kind);

            Size implicitFromFloat = 300f;
            Assert.Equal(SizeKind.Pixels, implicitFromFloat.Kind);
            Assert.Equal(300f, implicitFromFloat.Value);
        }

        // ---------- Align constants ----------

        [Fact]
        public void Align_Constants_Are_0_Half_1()
        {
            Assert.Equal(0f, Align.Start);
            Assert.Equal(0.5f, Align.Center);
            Assert.Equal(1f, Align.End);
        }

        // ---------- BUG.1 mitigation: lint anchors ----------

        /// <summary>
        /// BUG.1 mitigation per MAQUI-V2-IMPL.md: any reference to prefab-spawning
        /// APIs in v2 source means the LLM-loop authoring guarantee leaks. The
        /// test scans every v2 runtime .cs file for forbidden strings and fails
        /// if any appear.
        /// </summary>
        [Fact]
        public void RuntimeSources_DoNotReference_PrefabSpawningApis()
        {
            string runtimeDir = FindRuntimeDir();
            string[] forbidden = { "Resources.Load", "GameObject.Instantiate", "[AddComponentMenu" };
            // Allowlist: files that legitimately use prefab-spawning APIs as the
            // backend's adapter surface (not as component-authoring code).
            string[] allowlist = { "ResourcesImageLoader.cs" };

            foreach (string file in Directory.GetFiles(runtimeDir, "*.cs", SearchOption.AllDirectories))
            {
                string fileName = Path.GetFileName(file);
                if (System.Array.IndexOf(allowlist, fileName) >= 0) continue;
                string src = File.ReadAllText(file);
                foreach (string needle in forbidden)
                {
                    Assert.False(
                        src.Contains(needle),
                        $"BUG.1: forbidden API '{needle}' found in {Path.GetFileName(file)}. " +
                        "Maqui runtime sources must never reach for prefab-spawning APIs (see MAQUI-V2-IMPL.md BUG.1).");
                }
            }
        }

        private static string FindRuntimeDir([CallerFilePath] string thisFile = null)
        {
            // This file is at  .../com.ware.maqui/Tests.Standalone~/ApiSkeletonTests.cs
            // Runtime    is at  .../com.ware.maqui/Runtime
            //
            // MAQUI-V1-SUNSET MVS.6: this used to point at Runtime/V2. The V2 folder was
            // collapsed into Runtime when V1 was deleted, and this hardcoded path is exactly
            // the silent-breakage class the rename was expected to produce -- the suite caught
            // it, which is why the path lives in one helper rather than inline per test.
            var thisDir = new FileInfo(thisFile).Directory;            // Tests.Standalone~
            var pkgDir  = thisDir.Parent;                              // com.ware.maqui
            string runtime = Path.Combine(pkgDir.FullName, "Runtime");
            Assert.True(Directory.Exists(runtime), $"Could not locate Runtime dir from {thisFile}");
            return runtime;
        }
    }
}
