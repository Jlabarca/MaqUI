// SPDX-License-Identifier: MIT
// MaqUI v2 — PointerEventQueue. Fixed-capacity FIFO ring buffer.

namespace Maqui.V2
{
    /// <summary>
    /// Allocation-free FIFO ring buffer for <see cref="PointerEvent"/>. The
    /// Unity-side adapter enqueues events as UI Toolkit pointer callbacks fire;
    /// the Gui drains them at <see cref="Gui.BeginFrame"/>.
    ///
    /// <para>Default capacity 256 — tuned for one frame's worth of input on a
    /// 240Hz pointer with two-finger touch (typical worst case &lt; 50).
    /// Overflow drops the oldest event and increments
    /// <see cref="DroppedCount"/>.</para>
    /// </summary>
    public sealed class PointerEventQueue
    {
        private readonly PointerEvent[] _buf;
        private int _head;  // next slot to read
        private int _tail;  // next slot to write
        private int _count;

        /// <summary>Number of events dropped due to overflow since construction.</summary>
        public int DroppedCount { get; private set; }

        public int Capacity => _buf.Length;
        public int Count => _count;
        public bool IsEmpty => _count == 0;
        public bool IsFull => _count == _buf.Length;

        public PointerEventQueue(int capacity = 256)
        {
            if (capacity < 1) capacity = 1;
            _buf = new PointerEvent[capacity];
            _head = 0;
            _tail = 0;
            _count = 0;
        }

        public void Enqueue(in PointerEvent e)
        {
            if (_count == _buf.Length)
            {
                // Overflow: advance head to drop the oldest, then overwrite tail slot.
                _head = (_head + 1) % _buf.Length;
                _count--;
                DroppedCount++;
            }
            _buf[_tail] = e;
            _tail = (_tail + 1) % _buf.Length;
            _count++;
        }

        public bool TryDequeue(out PointerEvent e)
        {
            if (_count == 0)
            {
                e = default;
                return false;
            }
            e = _buf[_head];
            _head = (_head + 1) % _buf.Length;
            _count--;
            return true;
        }

        /// <summary>
        /// Drain all queued events into <paramref name="sink"/> in FIFO order,
        /// then clear. Sink receives them via the supplied action.
        /// </summary>
        public void Drain(System.Action<PointerEvent> sink)
        {
            if (sink == null) return;
            while (TryDequeue(out var e)) sink(e);
        }

        public void Clear()
        {
            _head = 0;
            _tail = 0;
            _count = 0;
        }
    }
}
