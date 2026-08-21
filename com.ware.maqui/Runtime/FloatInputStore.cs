// SPDX-License-Identifier: MIT
// MaqUI v2 — FloatInputStore. String-keyed float values living on Gui.

using System.Collections.Generic;

namespace Maqui
{
    /// <summary>
    /// Per-key float value table — the numeric twin of <see cref="TextInputStore"/>.
    /// Written by the backend when a native control (Slider) fires a value-changed
    /// event; read by the component to surface the user's current value back to the
    /// caller.
    ///
    /// <para>Lives on <see cref="Gui"/> so values survive reconcile: the frame buffer
    /// clears every <see cref="Gui.BeginFrame"/>, this doesn't.</para>
    ///
    /// <para><b>Why a store and not the drawn geometry (WINDOW-LOOP WL.0):</b> the
    /// old hand-drawn Slider tried to derive its value from pointer position and
    /// ended up computing value from value — the pointer X was never actually read
    /// in Unity, so dragging did nothing. Letting the native control own the
    /// interaction and reporting its value here removes that whole class of bug.</para>
    /// </summary>
    public sealed class FloatInputStore
    {
        private readonly Dictionary<string, float> _values = new(capacity: 8);

        /// <summary>Latest value for <paramref name="key"/>, or <paramref name="fallback"/> if unset.</summary>
        public float Get(string key, float fallback = 0f)
        {
            return _values.TryGetValue(key, out var v) ? v : fallback;
        }

        public void Set(string key, float value) => _values[key] = value;

        public bool Has(string key) => _values.ContainsKey(key);
        public bool Remove(string key) => _values.Remove(key);
        public int Count => _values.Count;
        public void Clear() => _values.Clear();
    }
}
