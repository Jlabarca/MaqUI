// SPDX-License-Identifier: MIT
// MaqUI v2 — Reconciler keying.

using System;

namespace Maqui.V2
{
    /// <summary>
    /// Stable identity for an element across frames. Two ops with the same
    /// <see cref="ReconcileKey"/> on consecutive frames are treated as the
    /// same element by <see cref="Reconciler"/> — its handle is reused and
    /// only the differing properties get an <c>UpdateElement</c> call.
    ///
    /// <para>Triple: <see cref="ScopePath"/> (data-scope hierarchy from
    /// <c>Gui.EnterDataScope</c>), <see cref="Kind"/> (the FrameOp's kind),
    /// <see cref="OrdinalInScope"/> (how many ops of this kind have already
    /// appeared in this scope this frame).</para>
    ///
    /// <para>Known v0 limitation: ordinal-within-scope is fragile when the
    /// user's component code conditionally inserts a new element — trailing
    /// siblings get re-keyed and re-created. Mitigation: wrap variable-count
    /// regions in named data scopes. The reconciler then matches on the
    /// scope key, not the ordinal.</para>
    /// </summary>
    internal readonly struct ReconcileKey : IEquatable<ReconcileKey>
    {
        public string ScopePath { get; }
        public FrameOpKind Kind { get; }
        public int OrdinalInScope { get; }

        public ReconcileKey(string scopePath, FrameOpKind kind, int ordinalInScope)
        {
            ScopePath = scopePath ?? "/";
            Kind = kind;
            OrdinalInScope = ordinalInScope;
        }

        public bool Equals(ReconcileKey other) =>
            Kind == other.Kind
            && OrdinalInScope == other.OrdinalInScope
            && string.Equals(ScopePath, other.ScopePath, StringComparison.Ordinal);

        public override bool Equals(object obj) => obj is ReconcileKey k && Equals(k);

        public override int GetHashCode()
        {
            unchecked
            {
                int h = StringComparer.Ordinal.GetHashCode(ScopePath ?? "/");
                h = (h * 397) ^ (int)Kind;
                h = (h * 397) ^ OrdinalInScope;
                return h;
            }
        }

        public override string ToString() =>
            $"{ScopePath}#{Kind}#{OrdinalInScope}";

        public static bool operator ==(ReconcileKey a, ReconcileKey b) => a.Equals(b);
        public static bool operator !=(ReconcileKey a, ReconcileKey b) => !a.Equals(b);
    }
}
