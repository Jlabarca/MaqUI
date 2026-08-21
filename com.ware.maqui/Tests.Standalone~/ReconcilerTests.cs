// SPDX-License-Identifier: MIT
// MaqUI v2 — Reconciler tests (xUnit, standalone).
//
// Validates the keyed-map diff algorithm against TestBackend.

using System.Linq;
using Maqui;
using UnityEngine;
using Xunit;

namespace Maqui.Tests
{
    public class ReconcilerTests
    {
        private readonly Gui _gui = new();
        private readonly TestBackend _backend = new();

        // --- Helpers ---

        private void RenderFrame(System.Action<Gui> uiCode)
        {
            _gui.BeginFrame();
            uiCode(_gui);
            _gui.EndFrame();
            _gui.Render(_backend);
        }

        private int CountEvents(BackendEventKind kind) =>
            _backend.Events.Count(e => e.Kind == kind);

        // ---------- Lifecycle ----------

        [Fact]
        public void Reconcile_EmptyFrame_OnlyEmitsBeginEnd()
        {
            RenderFrame(_ => { });
            Assert.Equal(2, _backend.Events.Count);
            Assert.Equal(BackendEventKind.BeginReconcile, _backend.Events[0].Kind);
            Assert.Equal(BackendEventKind.EndReconcile, _backend.Events[^1].Kind);
        }

        // ---------- Create / Update / Recycle ----------

        [Fact]
        public void Reconcile_SingleBox_CreatesAndParentsToRoot()
        {
            RenderFrame(g => g.Box());

            Assert.Equal(1, CountEvents(BackendEventKind.CreateElement));
            Assert.Equal(0, CountEvents(BackendEventKind.UpdateElement));
            Assert.Equal(0, CountEvents(BackendEventKind.Recycle));

            var setParent = _backend.Events.First(e => e.Kind == BackendEventKind.SetParent);
            Assert.Equal(_backend.Root, setParent.Parent);
            Assert.Equal(0, setParent.SiblingIndex);
        }

        [Fact]
        public void Reconcile_IdenticalFrameTwice_UpdatesNotCreatesOnSecondFrame()
        {
            RenderFrame(g => g.Box());
            _backend.Clear();

            RenderFrame(g => g.Box());

            Assert.Equal(0, CountEvents(BackendEventKind.CreateElement));
            Assert.Equal(1, CountEvents(BackendEventKind.UpdateElement));
            Assert.Equal(0, CountEvents(BackendEventKind.Recycle));
        }

        [Fact]
        public void Reconcile_ElementRemovedNextFrame_GetsRecycled()
        {
            RenderFrame(g => { g.Box(); g.Box(); });
            _backend.Clear();

            RenderFrame(g => g.Box());

            Assert.Equal(1, CountEvents(BackendEventKind.UpdateElement));
            Assert.Equal(1, CountEvents(BackendEventKind.Recycle));
        }

        [Fact]
        public void Reconcile_NewElementAddedNextFrame_GetsCreated()
        {
            RenderFrame(g => g.Box());
            _backend.Clear();

            RenderFrame(g => { g.Box(); g.Box(); });

            Assert.Equal(1, CountEvents(BackendEventKind.UpdateElement));
            Assert.Equal(1, CountEvents(BackendEventKind.CreateElement));
            Assert.Equal(0, CountEvents(BackendEventKind.Recycle));
        }

        // ---------- Nested containers ----------

        [Fact]
        public void Reconcile_NestedRowInColumn_SetsParentToContainerHandle()
        {
            int columnHandle = -1, rowHandle = -1, leafHandle = -1;
            RenderFrame(g =>
            {
                g.Column();
                g.Row();
                g.Box();
                g.EndRow();
                g.EndColumn();
            });

            // Three SetParent calls — Column under Root, Row under Column, Box under Row.
            var setParents = _backend.Events.Where(e => e.Kind == BackendEventKind.SetParent).ToList();
            Assert.Equal(3, setParents.Count);

            // First Create event ↔ Column; second ↔ Row; third ↔ Box.
            var creates = _backend.Events.Where(e => e.Kind == BackendEventKind.CreateElement).ToList();
            columnHandle = creates[0].Handle;
            rowHandle = creates[1].Handle;
            leafHandle = creates[2].Handle;

            // Column parented to Root.
            Assert.Equal(_backend.Root, setParents[0].Parent);
            Assert.Equal(columnHandle, setParents[0].Handle);
            // Row parented to Column.
            Assert.Equal(columnHandle, setParents[1].Parent);
            Assert.Equal(rowHandle, setParents[1].Handle);
            // Box parented to Row.
            Assert.Equal(rowHandle, setParents[2].Parent);
            Assert.Equal(leafHandle, setParents[2].Handle);
        }

        [Fact]
        public void Reconcile_RowEndPopsParentStack_SiblingAfterEndAttachesToRoot()
        {
            RenderFrame(g =>
            {
                g.Row();
                g.Box();        // child of Row
                g.EndRow();
                g.Box();        // child of Root (Row was popped)
            });

            var setParents = _backend.Events.Where(e => e.Kind == BackendEventKind.SetParent).ToList();
            // setParents: [Row→Root, Box→Row, Box→Root]
            Assert.Equal(3, setParents.Count);
            Assert.Equal(_backend.Root, setParents[0].Parent);
            // The second SetParent's parent should be the Row's handle (the first create).
            int rowHandle = _backend.Events.First(e => e.Kind == BackendEventKind.CreateElement).Handle;
            Assert.Equal(rowHandle, setParents[1].Parent);
            // Third SetParent → back to Root.
            Assert.Equal(_backend.Root, setParents[2].Parent);
        }

        // ---------- Sibling index ----------

        [Fact]
        public void Reconcile_MultipleSiblings_IncrementSiblingIndex()
        {
            RenderFrame(g => { g.Box(); g.Box(); g.Box(); });

            var siblingIndices = _backend.Events
                .Where(e => e.Kind == BackendEventKind.SetParent)
                .Select(e => e.SiblingIndex)
                .ToArray();
            Assert.Equal(new[] { 0, 1, 2 }, siblingIndices);
        }

        // ---------- Scope-based keying ----------

        [Fact]
        public void Reconcile_SameKindInDifferentScopes_DoesNotCollide()
        {
            RenderFrame(g =>
            {
                using (g.EnterDataScope("a")) g.Box();
                using (g.EnterDataScope("b")) g.Box();
            });
            _backend.Clear();

            // Re-render identically; both boxes should Update, not Create.
            RenderFrame(g =>
            {
                using (g.EnterDataScope("a")) g.Box();
                using (g.EnterDataScope("b")) g.Box();
            });
            Assert.Equal(2, CountEvents(BackendEventKind.UpdateElement));
            Assert.Equal(0, CountEvents(BackendEventKind.CreateElement));
        }

        [Fact]
        public void Reconcile_TwoBoxesInSameScope_GetDistinctOrdinalsAndPersist()
        {
            RenderFrame(g => { g.Box(); g.Box(); });
            int firstHandle = _backend.Events.Where(e => e.Kind == BackendEventKind.CreateElement).First().Handle;
            int secondHandle = _backend.Events.Where(e => e.Kind == BackendEventKind.CreateElement).Skip(1).First().Handle;
            Assert.NotEqual(firstHandle, secondHandle);

            _backend.Clear();
            RenderFrame(g => { g.Box(); g.Box(); });

            // Both should Update on identical second frame.
            Assert.Equal(2, CountEvents(BackendEventKind.UpdateElement));
            Assert.Equal(0, CountEvents(BackendEventKind.CreateElement));
        }

        // ---------- Interaction + scope marker passthrough ----------

        [Fact]
        public void Reconcile_InteractionOps_DoNotTriggerBackendCalls()
        {
            RenderFrame(g =>
            {
                var box = g.Box();
                box.OnClick();
                box.OnHover();
                box.OnHold();
                box.OnDrag();
            });

            // Only one CreateElement (the Box). The four interaction ops are in the
            // buffer but the reconciler doesn't surface them.
            Assert.Equal(1, CountEvents(BackendEventKind.CreateElement));
            Assert.Equal(1, _backend.Events.Count(e => e.Kind == BackendEventKind.SetParent));
        }

        [Fact]
        public void Reconcile_ScopeEnterExit_DoNotTriggerBackendCalls()
        {
            RenderFrame(g =>
            {
                using (g.EnterDataScope("x")) { }
            });

            // No Create / Update / SetParent / Recycle — scope ops are buffer-only.
            Assert.Equal(0, CountEvents(BackendEventKind.CreateElement));
            Assert.Equal(0, CountEvents(BackendEventKind.UpdateElement));
            Assert.Equal(0, CountEvents(BackendEventKind.SetParent));
            Assert.Equal(0, CountEvents(BackendEventKind.Recycle));
        }

        // ---------- Mixed leaf kinds ----------

        [Fact]
        public void Reconcile_MixedLeafKinds_AllParentedToRoot()
        {
            RenderFrame(g =>
            {
                g.DrawRect(new Color32(255, 0, 0, 255));
                g.DrawText("hi", new Color32(255, 255, 255, 255));
                g.DrawLine(new Color32(0, 255, 0, 255));
                g.DrawCircle(new Color32(0, 0, 255, 255));
                g.Spacer();
            });

            Assert.Equal(5, CountEvents(BackendEventKind.CreateElement));
            Assert.All(
                _backend.Events.Where(e => e.Kind == BackendEventKind.SetParent),
                e => Assert.Equal(_backend.Root, e.Parent));
        }

        // ---------- Lifecycle guards ----------

        [Fact]
        public void Render_InsideOpenFrame_Throws()
        {
            _gui.BeginFrame();
            Assert.Throws<System.InvalidOperationException>(() => _gui.Render(_backend));
        }

        [Fact]
        public void Render_NullBackend_Throws()
        {
            _gui.BeginFrame();
            _gui.EndFrame();
            Assert.Throws<System.ArgumentNullException>(() => _gui.Render(null));
        }

        // ---------- ReconcileKey shape ----------

        [Fact]
        public void ReconcileKey_EqualWhenAllThreeMatch()
        {
            var a = new ReconcileKey("/popup", FrameOpKind.Box, 2);
            var b = new ReconcileKey("/popup", FrameOpKind.Box, 2);
            Assert.Equal(a, b);
            Assert.Equal(a.GetHashCode(), b.GetHashCode());
        }

        [Fact]
        public void ReconcileKey_DistinctWhenAnyFieldDiffers()
        {
            var baseKey = new ReconcileKey("/", FrameOpKind.Box, 0);
            Assert.NotEqual(baseKey, new ReconcileKey("/popup", FrameOpKind.Box, 0));
            Assert.NotEqual(baseKey, new ReconcileKey("/", FrameOpKind.DrawText, 0));
            Assert.NotEqual(baseKey, new ReconcileKey("/", FrameOpKind.Box, 1));
        }
    }
}
