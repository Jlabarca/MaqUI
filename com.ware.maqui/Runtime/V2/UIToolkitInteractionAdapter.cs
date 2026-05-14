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

        private void OnEvent(int handle, IPointerEvent evt, PointerEventKind kind)
        {
            if (!_handleToCurrentNodeId.TryGetValue(handle, out int nodeId)) return;
            _handleToCurrentScope.TryGetValue(handle, out string scope);

            // Enqueue for downstream consumers that want full event history.
            // PointerEvent.Timestamp defaults to 0f — IPointerEvent only exposes
            // deltaTime (since last event with same pointerId), not a wall-clock
            // timestamp. Callers needing wall-clock should cast to EventBase and
            // read its `timestamp` long, then convert; not done here since v0
            // consumers (DragHandle sample, InteractableTests) don't need it.
            _queue.Enqueue(new PointerEvent(
                kind,
                nodeId,
                scope ?? "/",
                evt.position.x,
                evt.position.y,
                evt.button));

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
                    break;
                case PointerEventKind.Up:
                    bool wasActive = state.Has(nodeId, NodeInteractionFlags.Active);
                    state.ClearFlags(nodeId, NodeInteractionFlags.Active);
                    if (wasActive && state.Has(nodeId, NodeInteractionFlags.Hover))
                    {
                        state.AddFlags(nodeId, NodeInteractionFlags.ClickedThisFrame);
                    }
                    break;
                case PointerEventKind.Move:
                    // No flag changes; Move events are useful only via the queue
                    // (for callers tracking drag deltas).
                    break;
            }
        }
    }
}
