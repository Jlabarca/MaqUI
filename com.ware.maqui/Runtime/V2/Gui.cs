// SPDX-License-Identifier: MIT
// MaqUI v2 — Gui. Frame-context entry point. See docs/products/maqui-v2-spec.md.

using System;
using System.Collections.Generic;
using UnityEngine;

namespace Maqui.V2
{
    /// <summary>
    /// Frame-context entry point. One <c>Gui</c> instance per UI root. Callers
    /// wrap each frame with <see cref="BeginFrame"/> / <see cref="EndFrame"/>
    /// and call layout/draw/interaction/scope methods between them. Every call
    /// appends one <see cref="FrameOp"/> to the internal <see cref="FrameBuffer"/>;
    /// P1 does nothing else — the P2 reconciler will consume the buffer.
    ///
    /// <para><b>Threading:</b> not thread-safe. UI is main-thread only.</para>
    /// </summary>
    public sealed class Gui
    {
        // --- Internal state (private; tests reach the FrameBuffer via internal accessor) ---

        private readonly FrameBuffer _frameBuffer = new FrameBuffer();
        private readonly Stack<string> _scopeStack = new Stack<string>(capacity: 16);
        private int _nextNodeId;
        private bool _inFrame;

        // --- P4: animation + interaction state (lives across frames) ---

        private readonly AnimationStore _animations = new AnimationStore();
        private readonly InteractionState _interactions = new InteractionState();
        private readonly TextInputStore _textInputs = new TextInputStore();

        /// <summary>P4: spring-damper animations keyed by user-supplied string.
        /// Lives on Gui (not in FrameBuffer) so values survive reconcile.</summary>
        public AnimationStore Animations => _animations;

        /// <summary>P4: per-NodeId interaction flag table. Written by the
        /// Unity-side adapter; read by <see cref="Node"/> extension methods.
        /// Reset every <see cref="BeginFrame"/>.</summary>
        public InteractionState Interactions => _interactions;

        /// <summary>P8.6: per-key typed-text table. Written by the Unity-side
        /// TextField value-changed callback; read by
        /// <c>MaquiComponents.TextInput</c> to surface latest typed text.
        /// Persists across frames (unlike <see cref="Interactions"/>).</summary>
        public TextInputStore TextInputs => _textInputs;

        // --- Frame lifecycle ---

        /// <summary>Begin a frame. Clears the prior frame's buffer and resets node ids.</summary>
        public void BeginFrame()
        {
            if (_inFrame) throw new InvalidOperationException("Maqui.V2.Gui: BeginFrame called twice without EndFrame.");
            _frameBuffer.Clear();
            _scopeStack.Clear();
            _nextNodeId = 0;
            _inFrame = true;
            // P4: clear per-frame interaction flags so the Unity adapter (or
            // test driver) re-emits Hover/Active/Focus each frame and per-frame
            // flags like ClickedThisFrame don't leak forward.
            _interactions.Reset();
        }

        /// <summary>End the current frame. P2 will hand the FrameBuffer to the reconciler here.</summary>
        public void EndFrame()
        {
            if (!_inFrame) throw new InvalidOperationException("Maqui.V2.Gui: EndFrame called without BeginFrame.");
            if (_scopeStack.Count > 0)
                throw new InvalidOperationException(
                    $"Maqui.V2.Gui: EndFrame with {_scopeStack.Count} unclosed data scope(s). " +
                    "Did you forget to dispose a scope from EnterDataScope?");
            _inFrame = false;
        }

        // --- Reconciler (P2) ---

        private readonly Reconciler _reconciler = new();

        /// <summary>
        /// Drive <paramref name="backend"/> with this frame's recorded ops.
        /// Call after <see cref="EndFrame"/>. The driver pattern is:
        /// <c>BeginFrame() → user component → EndFrame() → Render(backend)</c>.
        ///
        /// <para>Keeping reconcile out of <see cref="EndFrame"/> means the
        /// buffer is inspectable between EndFrame and Render (tests rely on
        /// this), and a single <see cref="Gui"/> can drive multiple backends
        /// in the same frame if needed.</para>
        /// </summary>
        public void Render(IBackend backend)
        {
            if (_inFrame) throw new InvalidOperationException("Maqui.V2.Gui: Render called inside an open frame. Call EndFrame first.");
            if (backend == null) throw new ArgumentNullException(nameof(backend));
            _reconciler.Reconcile(_frameBuffer, backend);
        }

        // --- Internal accessors for the reconciler (P2) and tests (1.7) ---

        internal FrameBuffer Buffer => _frameBuffer;

        public string CurrentScopePath =>
            _scopeStack.Count == 0 ? "/" : "/" + string.Join("/", ReverseStack(_scopeStack));

        // --- Layout primitives (1.3) ---

        public Node Row(Size width = default, Size height = default)
        {
            int id = NewNodeId();
            _frameBuffer.Record(new FrameOp(FrameOpKind.RowBegin, id, CurrentScopePath,
                a: (float)width.Kind, b: width.Value, c: (float)height.Kind, d: height.Value));
            return new Node(this, id, _frameBuffer.Count - 1);
        }

        public void EndRow()
        {
            _frameBuffer.Record(new FrameOp(FrameOpKind.RowEnd, 0, CurrentScopePath));
        }

        public Node Column(Size width = default, Size height = default)
        {
            int id = NewNodeId();
            _frameBuffer.Record(new FrameOp(FrameOpKind.ColumnBegin, id, CurrentScopePath,
                a: (float)width.Kind, b: width.Value, c: (float)height.Kind, d: height.Value));
            return new Node(this, id, _frameBuffer.Count - 1);
        }

        public void EndColumn()
        {
            _frameBuffer.Record(new FrameOp(FrameOpKind.ColumnEnd, 0, CurrentScopePath));
        }

        public Node Box(Size width = default, Size height = default)
        {
            int id = NewNodeId();
            _frameBuffer.Record(new FrameOp(FrameOpKind.Box, id, CurrentScopePath,
                a: (float)width.Kind, b: width.Value, c: (float)height.Kind, d: height.Value));
            return new Node(this, id, _frameBuffer.Count - 1);
        }

        /// <summary>P8.5: container Box with <c>overflow:Hidden</c> on the backend
        /// wrapper. Used by <c>MaquiComponents.ScrollView</c> to clip content.
        /// Pairs with <see cref="EndClipBox"/>.</summary>
        public Node ClipBox(Size width = default, Size height = default)
        {
            int id = NewNodeId();
            _frameBuffer.Record(new FrameOp(FrameOpKind.ClipBoxBegin, id, CurrentScopePath,
                a: (float)width.Kind, b: width.Value, c: (float)height.Kind, d: height.Value));
            return new Node(this, id, _frameBuffer.Count - 1);
        }

        public void EndClipBox()
        {
            _frameBuffer.Record(new FrameOp(FrameOpKind.ClipBoxEnd, 0, CurrentScopePath));
        }

        public Node Spacer(Size size = default)
        {
            int id = NewNodeId();
            _frameBuffer.Record(new FrameOp(FrameOpKind.Spacer, id, CurrentScopePath,
                a: (float)size.Kind, b: size.Value));
            return new Node(this, id, _frameBuffer.Count - 1);
        }

        // --- Draw primitives (1.4) ---

        public Node DrawRect(Color32 color, Size width = default, Size height = default)
        {
            int id = NewNodeId();
            _frameBuffer.Record(new FrameOp(FrameOpKind.DrawRect, id, CurrentScopePath,
                a: (float)width.Kind, b: width.Value, c: (float)height.Kind, d: height.Value,
                color: color));
            return new Node(this, id, _frameBuffer.Count - 1);
        }

        public Node DrawText(string text, Color32 color = default, float fontSize = 14f)
        {
            int id = NewNodeId();
            _frameBuffer.Record(new FrameOp(FrameOpKind.DrawText, id, CurrentScopePath,
                a: fontSize, color: color, text: text ?? string.Empty));
            return new Node(this, id, _frameBuffer.Count - 1);
        }

        public Node DrawLine(Color32 color, float thickness = 1f)
        {
            int id = NewNodeId();
            _frameBuffer.Record(new FrameOp(FrameOpKind.DrawLine, id, CurrentScopePath,
                a: thickness, color: color));
            return new Node(this, id, _frameBuffer.Count - 1);
        }

        public Node DrawCircle(Color32 color, float radius = 1f)
        {
            int id = NewNodeId();
            _frameBuffer.Record(new FrameOp(FrameOpKind.DrawCircle, id, CurrentScopePath,
                a: radius, color: color));
            return new Node(this, id, _frameBuffer.Count - 1);
        }

        /// <summary>P8.4: image leaf. Backend resolves <paramref name="textureKey"/>
        /// via its <c>IImageLoader</c> (Unity-side; null-safe → placeholder).</summary>
        public Node DrawImage(string textureKey, float width = 64f, float height = 64f)
        {
            int id = NewNodeId();
            _frameBuffer.Record(new FrameOp(FrameOpKind.DrawImage, id, CurrentScopePath,
                a: (float)SizeKind.Pixels, b: width,
                c: (float)SizeKind.Pixels, d: height,
                text: textureKey ?? string.Empty));
            return new Node(this, id, _frameBuffer.Count - 1);
        }

        /// <summary>P8.6: editable text field. Backend creates a UI Toolkit
        /// TextField; value-changed callback routes through
        /// <see cref="TextInputs"/> keyed by ScopePath + <paramref name="key"/>.</summary>
        public Node TextInputField(string key, string initialValue, float height = 28f)
        {
            int id = NewNodeId();
            string keyPayload = string.IsNullOrEmpty(key)
                ? (CurrentScopePath + "/text")
                : (CurrentScopePath + "/" + key);
            _frameBuffer.Record(new FrameOp(FrameOpKind.TextInputField, id, CurrentScopePath,
                a: (float)SizeKind.Pixels, b: 0f,
                c: (float)SizeKind.Pixels, d: height,
                text: keyPayload + "|" + (initialValue ?? string.Empty)));
            return new Node(this, id, _frameBuffer.Count - 1);
        }

        // --- Interaction recording — called by Node extension methods (1.5) ---

        internal void RecordInteraction(FrameOpKind kind, Node target)
        {
            _frameBuffer.Record(new FrameOp(kind, target.Id, CurrentScopePath));
        }

        // --- P4: animation + tick helpers ---

        /// <summary>
        /// P4. Get/update an animated value keyed by <paramref name="key"/>.
        /// First call initializes at <paramref name="target"/> (no animation);
        /// subsequent calls update the target and the animator pulls toward it
        /// on each <see cref="TickAnimations"/>. Returns the current value.
        /// </summary>
        public float Animate(string key, float target,
            float stiffness = AnimationFloat.DefaultStiffness,
            float damping = AnimationFloat.DefaultDamping)
        {
            return _animations.Animate(key, target, stiffness, damping);
        }

        /// <summary>
        /// P4. Advance every animation by <paramref name="dt"/> seconds. Call
        /// once per frame, typically from the Unity-side driver
        /// (<c>GuiDriver.LateUpdate</c>) before <see cref="BeginFrame"/>.
        /// </summary>
        public void TickAnimations(float dt)
        {
            _animations.TickAll(dt);
        }

        // --- Data scope (1.6) ---

        /// <summary>
        /// Enter a data scope keyed by <paramref name="key"/>. Returns an
        /// <see cref="IDisposable"/> so callers can write
        /// <c>using (gui.EnterDataScope("popup")) { ... }</c>.
        /// </summary>
        public IDisposable EnterDataScope(string key)
        {
            if (string.IsNullOrEmpty(key))
                throw new ArgumentException("Maqui.V2.Gui: data scope key must be non-empty.", nameof(key));
            _scopeStack.Push(key);
            _frameBuffer.Record(new FrameOp(FrameOpKind.ScopeEnter, 0, CurrentScopePath, text: key));
            return new ScopeHandle(this, _scopeStack.Count);
        }

        /// <summary>Manual exit. Prefer the <c>using</c>-pattern via the IDisposable returned by EnterDataScope.</summary>
        public void ExitDataScope()
        {
            if (_scopeStack.Count == 0)
                throw new InvalidOperationException("Maqui.V2.Gui: ExitDataScope with no scope on stack.");
            string popped = _scopeStack.Pop();
            _frameBuffer.Record(new FrameOp(FrameOpKind.ScopeExit, 0, CurrentScopePath, text: popped));
        }

        private sealed class ScopeHandle : IDisposable
        {
            private readonly Gui _gui;
            private readonly int _expectedDepth;
            private bool _disposed;

            public ScopeHandle(Gui gui, int expectedDepth)
            {
                _gui = gui;
                _expectedDepth = expectedDepth;
            }

            public void Dispose()
            {
                if (_disposed) return;
                _disposed = true;
                if (_gui._scopeStack.Count != _expectedDepth)
                    throw new InvalidOperationException(
                        $"Maqui.V2.Gui: data scope disposed out of order (expected depth {_expectedDepth}, got {_gui._scopeStack.Count}). " +
                        "Did you nest using-blocks correctly?");
                _gui.ExitDataScope();
            }
        }

        // --- Helpers ---

        private int NewNodeId() => ++_nextNodeId;

        private static IEnumerable<string> ReverseStack(Stack<string> stack)
        {
            // Stack iterates top-to-bottom; we want root-to-leaf for the path.
            var arr = stack.ToArray(); // top..bottom
            for (int i = arr.Length - 1; i >= 0; i--) yield return arr[i];
        }
    }
}
