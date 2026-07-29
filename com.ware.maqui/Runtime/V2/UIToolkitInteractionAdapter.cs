// SPDX-License-Identifier: MIT
// MaqUI v2 — UIToolkitInteractionAdapter. Routes UI Toolkit pointer events
// into InteractionState + PointerEventQueue.
//
// =====================================================================
// MAQUIV2.4 — BLIND DRAFT. Operator must compile in Unity to validate.
// =====================================================================
//
// This file references UnityEngine.UIElements (VisualElement, PointerDownEvent,
// etc.) which the standalone xUnit csproj does NOT shim. It's excluded from
// the standalone <Compile Include> via <Compile Remove>. Compile-checked
// only inside Unity Editor.
//
// Usage (in operator-authored GuiDriver.LateUpdate, after Render(backend)):
//
//   if (_adapter == null) {
//       _adapter = new UIToolkitInteractionAdapter(_gui, _backend, root);
//   }
//   _adapter.UpdateHandleMap();   // observes most-recent FrameOps per handle
//
// The adapter subscribes once per VisualElement to UI Toolkit pointer events
// and writes flags into Gui.Interactions on the fly. NodeId is looked up
// from the backend's most-recently-seen FrameOp for each handle.

using System.Collections.Generic;
using UnityEngine.UIElements;

namespace Maqui.V2
{
    /// <summary>
    /// Wires UI Toolkit <see cref="VisualElement"/> pointer callbacks into a
    /// <see cref="Gui"/>'s <see cref="InteractionState"/> + a
    /// <see cref="PointerEventQueue"/>.
    ///
    /// <para><b>Status: blind draft.</b> Not validated outside Unity Editor.</para>
    /// </summary>
    public sealed class UIToolkitInteractionAdapter
    {
        private readonly Gui _gui;
        private readonly UIToolkitBackend _backend;
        private readonly PointerEventQueue _queue;
        private readonly HashSet<int> _subscribed = new();
        private readonly Dictionary<int, int> _handleToCurrentNodeId = new();
        private readonly Dictionary<int, string> _handleToCurrentScope = new();

        // Drag tracking (VPU.6): the node a press started on, and whether the
        // pointer has moved since — so we can flag DragStarted on the source and
        // DragEnded on whatever node the release lands on (the drop target).
        private int _dragSourceNodeId = -1;
        private bool _dragging;
        /// <summary>Latched by the direct target's Up so the rest of that release's
        /// bubble chain still receives DragEnded after `_dragging` is cleared. Reset by
        /// the next Down. See the Up case for why the chain needs it.</summary>
        private bool _dragEndBubbling;

        /// <summary>The node id a drag is currently sourced from, or -1. Lets a
        /// caller carry a payload for the drag in progress (which item is held).</summary>
        public int DragSourceNodeId => _dragSourceNodeId;

        public UIToolkitInteractionAdapter(Gui gui, UIToolkitBackend backend, PointerEventQueue queue = null)
        {
            _gui = gui ?? throw new System.ArgumentNullException(nameof(gui));
            _backend = backend ?? throw new System.ArgumentNullException(nameof(backend));
            _queue = queue ?? new PointerEventQueue(256);
        }

        public PointerEventQueue Queue => _queue;

        /// <summary>
        /// Record the (handle → current-frame NodeId + scope) mapping for one
        /// op. Called by the backend at <c>CreateElement</c> + <c>UpdateElement</c>
        /// time. The handle-side mapping is needed at pointer-event-resolution
        /// time, so this method must run for every reconciled op every frame.
        ///
        /// <para><i>Wire-up note for operator:</i> in <see cref="UIToolkitBackend"/>,
        /// add a hook (e.g. an action invoked after each <c>CreateElement</c>
        /// + <c>UpdateElement</c>) that calls this. The blind-draft pattern
        /// here is a separate <see cref="UpdateHandleMap"/> method the operator
        /// calls after <see cref="Gui.Render"/> — needs the backend to surface
        /// a (handle → last FrameOp) accessor.</i></para>
        /// </summary>
        public void NoteHandleFrameOp(int handle, in FrameOp op)
        {
            _handleToCurrentNodeId[handle] = op.NodeId;
            _handleToCurrentScope[handle] = op.ScopePath;
        }

        /// <summary>
        /// Wire pointer event listeners onto <paramref name="element"/> the
        /// first time we see this handle. Idempotent — subsequent calls no-op.
        /// </summary>
        public void SubscribeIfNew(int handle, VisualElement element)
        {
            if (element == null) return;
            if (!_subscribed.Add(handle)) return;

            element.RegisterCallback<PointerEnterEvent>(evt => OnEvent(handle, evt, PointerEventKind.Enter));
            element.RegisterCallback<PointerLeaveEvent>(evt => OnEvent(handle, evt, PointerEventKind.Leave));
            element.RegisterCallback<PointerDownEvent>(evt => OnEvent(handle, evt, PointerEventKind.Down));
            element.RegisterCallback<PointerUpEvent>(evt => OnEvent(handle, evt, PointerEventKind.Up));
            element.RegisterCallback<PointerMoveEvent>(evt => OnEvent(handle, evt, PointerEventKind.Move));
        }

        // Generic over the concrete event type because target/currentTarget live on
        // EventBase, not IPointerEvent — the bubbling check below needs both.
        private void OnEvent<T>(int handle, T evt, PointerEventKind kind)
            where T : EventBase, IPointerEvent
        {
            // UI Toolkit BUBBLES pointer events, so this callback also runs for every
            // ancestor of the element actually hit — each with its own handle. Per-node
            // flags (Hover/Active/Clicked) are fine with that: each element flags itself,
            // and the hit element still gets its own. The DRAG SOURCE is not, because it
            // is a single field: the innermost element wrote it first and every ancestor
            // then overwrote it, so it ended up holding the OUTERMOST container.
            //
            // Measured in ORO's inventory: pressing the first item slot (handle 5) left
            // _dragSourceNodeId == 1, which is the `ro-admin` root container. DragStarted
            // was therefore flagged on the container and the slot never saw it — the item
            // drag "not working" while clicks on the very same element worked fine.
            //
            // `target != currentTarget` is the standard test for "this is a bubbled copy,
            // not the real hit". Only the drag-source bookkeeping is gated on it, so the
            // flag semantics every other consumer relies on are untouched.
            bool isDirectTarget = ReferenceEquals(evt.target, evt.currentTarget);
            DispatchEvent(handle, kind, evt.position.x, evt.position.y, evt.button, isDirectTarget);
        }

        /// <summary>
        /// Test seam + headless dispatch path. Drives the same flag-writing
        /// logic the real <c>OnEvent</c> uses, but without depending on
        /// <see cref="IPointerEvent"/> — so PlayMode tests that can't attach a
        /// PanelSettings-backed panel can still verify the adapter logic, and
        /// non-UI-Toolkit input sources (raw mouse/touch readers) can route
        /// events through the same code path.
        ///
        /// <para><paramref name="handle"/> must have been registered via
        /// <see cref="NoteHandleFrameOp"/> first; otherwise the call is a
        /// no-op (same semantics as <c>OnEvent</c>).</para>
        /// </summary>
        /// <param name="isDirectTarget">False when this call is a BUBBLED copy of an event
        /// whose real target was a descendant. Only the drag-source bookkeeping consults
        /// it; defaults to true so the headless/test path and non-UIT input sources keep
        /// their existing single-dispatch semantics.</param>
        public void DispatchEvent(int handle, PointerEventKind kind, float x, float y, int button = 0,
            bool isDirectTarget = true)
        {
            if (!_handleToCurrentNodeId.TryGetValue(handle, out int nodeId)) return;
            _handleToCurrentScope.TryGetValue(handle, out string scope);

            // Enqueue for downstream consumers that want full event history.
            // PointerEvent.Timestamp defaults to 0f — IPointerEvent only exposes
            // deltaTime (since last event with same pointerId), not a wall-clock
            // timestamp. Callers needing wall-clock should cast to EventBase and
            // read its `timestamp` long, then convert; not done here since v0
            // consumers (DragHandle sample, InteractableTests) don't need it.
            _queue.Enqueue(new PointerEvent(kind, nodeId, scope ?? "/", x, y, button));

            // Update flags in-place — same-frame semantics.
            var state = _gui.Interactions;
            switch (kind)
            {
                case PointerEventKind.Enter:
                    state.AddFlags(nodeId, NodeInteractionFlags.Hover);
                    break;
                case PointerEventKind.Leave:
                    state.ClearFlags(nodeId, NodeInteractionFlags.Hover | NodeInteractionFlags.Active);
                    break;
                case PointerEventKind.Down:
                    state.AddFlags(nodeId, NodeInteractionFlags.Active);
                    // Arm a potential drag from this node; not a drag until the
                    // pointer actually moves (a plain click must not flag a drag).
                    // Only the DIRECT target arms it — a bubbled copy from an ancestor
                    // would otherwise overwrite the real source with a container.
                    if (isDirectTarget)
                    {
                        _dragSourceNodeId = nodeId;
                        _dragging = false;
                        // A new gesture closes the previous release's bubble chain.
                        _dragEndBubbling = false;
                    }
                    break;
                case PointerEventKind.Up:
                    bool wasActive = state.Has(nodeId, NodeInteractionFlags.Active);
                    state.ClearFlags(nodeId, NodeInteractionFlags.Active);
                    if (wasActive && state.Has(nodeId, NodeInteractionFlags.Hover))
                    {
                        state.AddFlags(nodeId, NodeInteractionFlags.ClickedThisFrame);
                    }
                    // A release that ends an in-progress drag flags DragEnded on the
                    // node under the pointer — the drop target (may differ from the
                    // source; that's the whole point of a drag).
                    //
                    // Unlike the drag SOURCE, DragEnded must reach the whole bubble chain,
                    // not just the direct target. Drop zones are containers whose visible
                    // body is a child (ORO's Equip/Use zones are a Column wrapping a
                    // Label), so a release lands on the CHILD — flagging only the direct
                    // target would leave the zone itself without a drop and `OnDragEnd()`
                    // would never fire.
                    //
                    // Resetting on the direct target alone is what made this fail before:
                    // the innermost Up cleared _dragging, and every ancestor's bubbled copy
                    // then found it already false and skipped its flag. So the latch below
                    // keeps the chain flagging after the reset, and is armed only by the
                    // direct target so one gesture can't re-enter it.
                    if (isDirectTarget)
                    {
                        if (_dragging)
                        {
                            state.AddFlags(nodeId, NodeInteractionFlags.DragEndedThisFrame);
                            _dragEndBubbling = true;
                            _dragging = false;
                        }
                        // Disarm on EVERY release, not just one that was dragging. A plain
                        // click previously left the source armed, so the next stray Move —
                        // with no button held — promoted it to a phantom drag.
                        _dragSourceNodeId = -1;
                    }
                    else if (_dragEndBubbling)
                    {
                        state.AddFlags(nodeId, NodeInteractionFlags.DragEndedThisFrame);
                    }
                    break;
                case PointerEventKind.Move:
                    // First move after a Down promotes the armed node to a drag and
                    // flags DragStarted on the SOURCE (not the moved-over node).
                    if (_dragSourceNodeId >= 0 && !_dragging)
                    {
                        _dragging = true;
                        state.AddFlags(_dragSourceNodeId, NodeInteractionFlags.DragStartedThisFrame);
                    }
                    break;
            }
        }
    }
}
