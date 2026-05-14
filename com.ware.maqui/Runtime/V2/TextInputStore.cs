// SPDX-License-Identifier: MIT
// MaqUI v2 — TextInputStore. String-keyed text values living on Gui.

using System.Collections.Generic;

namespace Maqui.V2
{
    /// <summary>
    /// Per-key text value table. Written by the Unity-side TextField
    /// callback when a <see cref="MaquiComponents.MaquiComponents.TextInput"/>
    /// fires a value-changed event; read by the component to surface the
    /// latest typed text back to the caller.
    ///
    /// <para>Lives on <see cref="Gui"/> so values survive reconcile (frame
    /// buffer clears each <see cref="Gui.BeginFrame"/>; TextInputStore
    /// doesn't).</para>
    /// </summary>
    public sealed class TextInputStore
    {
        private readonly Dictionary<string, string> _values = new(capacity: 8);

        /// <summary>Get the latest value for <paramref name="key"/>, or <paramref name="fallback"/> if unset.</summary>
        public string Get(string key, string fallback = "")
        {
            return _values.TryGetValue(key, out var v) ? v : (fallback ?? "");
        }

        /// <summary>Set/overwrite the value for <paramref name="key"/>.</summary>
        public void Set(string key, string value)
        {
            _values[key] = value ?? string.Empty;
        }

        public bool Has(string key) => _values.ContainsKey(key);
        public bool Remove(string key) => _values.Remove(key);
        public int Count => _values.Count;
        public void Clear() => _values.Clear();
    }
}
