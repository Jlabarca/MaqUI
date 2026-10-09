// SPDX-License-Identifier: MIT
// MaqUI v2 - MaquiStrings. Bounded memo for the strings the component layer derives on every
// call (ZERO-ALLOC follow-up).
//
// Why this exists: immediate-mode components run every time their window rebuilds, which for a
// hovered window is every frame. Several of them DERIVE a string from their arguments on each call -
// `className + "__label"` in Button, `key + "-dec"` in Stepper, `count.ToString()` in ItemSlot - and
// each derivation allocates a fresh string that is identical to the one the previous frame built.
// The inputs are a closed set of literals (USS class names, element keys, small counts), so each
// derived string is built ONCE per distinct input and handed back by reference afterwards.
//
// Bounded on purpose: a caller that feeds it a key per item id or per chat line would otherwise
// grow the table without limit. Once a table is full, a NEW input is derived the old way (a plain
// allocation, correct, just not remembered), so the memo can only ever make things cheaper.
//
// Single-threaded in practice (a Gui is main-thread), but the standalone test suite runs fixtures in
// parallel against the shared instance, so every table access takes a lock; uncontended it is a few
// nanoseconds and it can never hand back a torn entry.

using System.Collections.Generic;

namespace Maqui
{
    /// <summary>The memo itself, as an instance so a test can exercise the bound with a tiny cap
    /// instead of filling (and perturbing) the shared tables.</summary>
    internal sealed class DerivedStrings
    {
        /// <summary>Entries per table before new inputs stop being remembered.</summary>
        internal const int DefaultCap = 1024;

        /// <summary>Counts below this are served from a flat array (stack sizes, hotbar numbers).</summary>
        internal const int SmallIntCount = 1000;

        private readonly int _cap;
        private readonly object _lock = new object();
        private readonly Dictionary<(string, string), string> _suffixed = new Dictionary<(string, string), string>();
        private readonly Dictionary<(string, string), string> _scoped = new Dictionary<(string, string), string>();
        private readonly Dictionary<(string, string, int), string> _indexed = new Dictionary<(string, string, int), string>();
        private readonly Dictionary<(int, int), string> _pairs = new Dictionary<(int, int), string>();
        private readonly string[] _smallInts = new string[SmallIntCount];

        internal DerivedStrings(int cap = DefaultCap) { _cap = cap; }

        /// <summary>Entries currently remembered across the keyed tables (the bound under test).</summary>
        internal int Count
        {
            get { lock (_lock) return _suffixed.Count + _scoped.Count + _indexed.Count + _pairs.Count; }
        }

        /// <summary><c>a + b</c>. A null side concatenates as empty, exactly like the operator.</summary>
        internal string Suffixed(string a, string b)
        {
            lock (_lock)
            {
                if (_suffixed.TryGetValue((a, b), out var hit)) return hit;
                var made = a + b;
                if (_suffixed.Count < _cap) _suffixed[(a, b)] = made;
                return made;
            }
        }

        /// <summary><c>scope + "/" + key</c> - the store key every value-emitting control addresses
        /// itself by.</summary>
        internal string Scoped(string scope, string key)
        {
            lock (_lock)
            {
                if (_scoped.TryGetValue((scope, key), out var hit)) return hit;
                var made = scope + "/" + key;
                if (_scoped.Count < _cap) _scoped[(scope, key)] = made;
                return made;
            }
        }

        /// <summary><c>key + mid + index</c>, i.e. the interpolation <c>$"{key}-tab-{i}"</c>.</summary>
        internal string Indexed(string key, string mid, int index)
        {
            lock (_lock)
            {
                if (_indexed.TryGetValue((key, mid, index), out var hit)) return hit;
                var made = key + mid + index.ToString();
                if (_indexed.Count < _cap) _indexed[(key, mid, index)] = made;
                return made;
            }
        }

        /// <summary><c>$"{a} / {b}"</c> - a level readout such as "3 / 10".</summary>
        internal string Pair(int a, int b)
        {
            lock (_lock)
            {
                if (_pairs.TryGetValue((a, b), out var hit)) return hit;
                var made = a.ToString() + " / " + b.ToString();
                if (_pairs.Count < _cap) _pairs[(a, b)] = made;
                return made;
            }
        }

        /// <summary><c>n.ToString()</c>, remembered for 0..<see cref="SmallIntCount"/>-1.</summary>
        internal string Int(int n)
        {
            if ((uint)n >= (uint)SmallIntCount) return n.ToString();
            lock (_lock)
            {
                return _smallInts[n] ?? (_smallInts[n] = n.ToString());
            }
        }
    }

    /// <summary>The shared memo the components use. Thin statics so a call site reads as the
    /// expression it replaces.</summary>
    internal static class MaquiStrings
    {
        internal static readonly DerivedStrings Shared = new DerivedStrings();

        /// <summary>The label class Button derives from its container class: <c>{className}__label</c>.</summary>
        internal static string LabelClass(string className) => Shared.Suffixed(className, "__label");

        internal static string Suffixed(string a, string b) => Shared.Suffixed(a, b);
        internal static string Scoped(string scope, string key) => Shared.Scoped(scope, key);
        internal static string Indexed(string key, string mid, int index) => Shared.Indexed(key, mid, index);
        internal static string Pair(int a, int b) => Shared.Pair(a, b);
        internal static string Int(int n) => Shared.Int(n);
    }
}
