// SPDX-License-Identifier: MIT
// MaqUI v2 — AnimationStore. String-keyed AnimationFloat table living on Gui.

using System.Collections.Generic;

namespace Maqui
{
    /// <summary>
    /// Map of caller-supplied key → <see cref="AnimationFloat"/>. Lives on
    /// <see cref="Gui"/> so values survive across <see cref="Gui.BeginFrame"/>
    /// boundaries (frame buffer clears; AnimationStore doesn't).
    ///
    /// <para>Keys are user-supplied strings — recommended convention is
    /// scope-path-then-name (<c>"/popup/scale"</c>), but unenforced. Two
    /// callers using the same key share state, which is sometimes useful.</para>
    /// </summary>
    public sealed class AnimationStore
    {
        private readonly Dictionary<string, AnimationFloat> _values = new(capacity: 32);
        // Reused by TickAll so advancing animations allocates nothing per frame.
        private readonly List<string> _tickKeys = new(capacity: 32);

        /// <summary>
        /// Get current value for <paramref name="key"/> animating toward
        /// <paramref name="target"/>. Creates a new entry on first call
        /// (initialized to <c>target</c>, no animation). Subsequent calls
        /// update the target only — the animator advances on
        /// <see cref="TickAll"/>.
        /// </summary>
        public float Animate(string key, float target, float stiffness = AnimationFloat.DefaultStiffness, float damping = AnimationFloat.DefaultDamping)
        {
            if (_values.TryGetValue(key, out var anim))
            {
                anim.Target = target;
                anim.Stiffness = stiffness;
                anim.Damping = damping;
                _values[key] = anim;
                return anim.Current;
            }
            // First sight — initialize at target so we don't animate from 0.
            var fresh = new AnimationFloat(target, target, stiffness, damping);
            _values[key] = fresh;
            return fresh.Current;
        }

        /// <summary>Direct accessor for tests + advanced callers.</summary>
        public AnimationFloat Get(string key)
        {
            return _values.TryGetValue(key, out var anim) ? anim : default;
        }

        /// <summary>Direct mutator (overwrites). Used by tests + snap-to-target.</summary>
        public void Set(string key, in AnimationFloat value)
        {
            _values[key] = value;
        }

        /// <summary>Remove a key. Returns true if it existed.</summary>
        public bool Remove(string key) => _values.Remove(key);

        /// <summary>Number of tracked animations.</summary>
        public int Count => _values.Count;

        /// <summary>
        /// FRAME-BUDGET.2.6 — true when at least one tracked animation has not settled
        /// (<see cref="AnimationFloat.IsSettled"/> false). A window with active tweens or scroll
        /// inertia must not be skipped by a dirty-rebuild optimisation, so this is one of the
        /// forced-dirty sources a caller reads before deciding to bail on a frame's rebuild.
        /// O(n) over tracked animations — called at most once per window per frame.
        /// </summary>
        public bool HasActive
        {
            get
            {
                foreach (var kv in _values)
                    if (!kv.Value.IsSettled()) return true;
                return false;
            }
        }

        /// <summary>
        /// Advance every tracked animation by <paramref name="dt"/> seconds.
        /// Called by <see cref="Gui.TickAnimations"/> once per frame.
        /// </summary>
        public void TickAll(float dt)
        {
            if (dt <= 0f) return;
            if (_values.Count == 0) return;
            // Snapshot keys to avoid dict-modified mid-enumeration; struct values
            // are copied so we write back. The snapshot buffer is reused across frames.
            var keys = _tickKeys;
            keys.Clear();
            foreach (var kv in _values) keys.Add(kv.Key);
            for (int j = 0; j < keys.Count; j++)
            {
                var anim = _values[keys[j]];
                anim.Tick(dt);
                _values[keys[j]] = anim;
            }
            keys.Clear();
        }

        /// <summary>Clear all tracked animations. Useful for tests.</summary>
        public void Clear() => _values.Clear();
    }
}
