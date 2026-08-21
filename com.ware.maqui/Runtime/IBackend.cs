// SPDX-License-Identifier: MIT
// MaqUI v2 — Reconciler backend abstraction.
//
// The Reconciler depends on this interface, not on UnityEngine.UIElements.
// Concrete implementations:
//   - UIToolkitBackend (Runtime/UIToolkitBackend.cs) — Unity, VisualElement-backed
//   - TestBackend      (Tests.Standalone~/TestBackend.cs) — in-memory, records calls for assertions
//   - (future)         — PanGui backend when their Unity bridge ships
//
// All elements are addressed by opaque <c>int</c> handles minted by the backend.
// The reconciler never touches VisualElement directly — that's what makes this
// architecture multi-backend.

namespace Maqui
{
    /// <summary>
    /// Backend abstraction consumed by <see cref="Reconciler"/>. One backend
    /// per UI root; lifetime spans many frames.
    /// </summary>
    public interface IBackend
    {
        /// <summary>
        /// Handle of the root element. Reconciler uses this as the parent for
        /// top-level frame ops. Backends guarantee this handle is stable across
        /// reconciles and not subject to <see cref="Recycle"/>.
        /// </summary>
        int Root { get; }

        /// <summary>Called once at the start of each reconcile pass.</summary>
        void BeginReconcile();

        /// <summary>
        /// Called once at the end of each reconcile pass. Backends may flush
        /// pending style/layout writes here.
        /// </summary>
        void EndReconcile();

        /// <summary>
        /// Mint a new element for this <paramref name="op"/>. Backend reads
        /// <c>op.Kind</c> to decide what kind of element to create (rect, text,
        /// circle, line, container). Returns a fresh handle.
        /// </summary>
        int CreateElement(in FrameOp op);

        /// <summary>
        /// Apply the new op's properties to the existing element behind
        /// <paramref name="handle"/>. Called on cache-hit during reconcile.
        /// </summary>
        void UpdateElement(int handle, in FrameOp op);

        /// <summary>
        /// Set <paramref name="child"/>'s parent + sibling index. Called for
        /// every reconciled element each frame (cheap for unchanged trees).
        /// </summary>
        void SetParent(int child, int parent, int siblingIndex);

        /// <summary>
        /// Return <paramref name="handle"/> to the backend's recycle pool.
        /// Subsequent <see cref="CreateElement"/> calls may reuse the same
        /// underlying element. Never called on <see cref="Root"/>.
        /// </summary>
        void Recycle(int handle);
    }
}
