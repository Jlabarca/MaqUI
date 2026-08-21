// SPDX-License-Identifier: MIT
// MaqUI v2 — Alignment. See docs/products/maqui-v2-spec.md § Alignment.

namespace Maqui
{
    /// <summary>
    /// Named float constants for alignment. Per the spec, alignment is just a
    /// <c>float</c> in 0..1 (overflow allowed for "past the edge" effects) —
    /// no enum. This class holds discoverable names so call sites can write
    /// <c>.Align(Align.Center)</c> without magic numbers.
    /// </summary>
    public static class Align
    {
        public const float Start = 0f;
        public const float Center = 0.5f;
        public const float End = 1f;
    }
}
