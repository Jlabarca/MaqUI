// SPDX-License-Identifier: MIT
// MaqUI v2 — Size primitive. See docs/products/maqui-v2-spec.md § Size primitives.

namespace Maqui.V2
{
    /// <summary>
    /// How a <see cref="Size"/>'s payload should be interpreted by the layout solver.
    /// Pattern-inspired by PanGui but not name-mirrored.
    /// </summary>
    public enum SizeKind : byte
    {
        /// <summary>Hug content. Payload is unused (treated as 1.0 = 100%).</summary>
        Fit = 0,

        /// <summary>Fill remaining space with the given weight (default 1).</summary>
        Expand = 1,

        /// <summary>Other axis multiplied by payload (e.g. <c>Size.Ratio(1.5f)</c>).</summary>
        Ratio = 2,

        /// <summary>Fraction of parent (0..1 normally, overflow allowed).</summary>
        Percentage = 3,

        /// <summary>Absolute pixel count.</summary>
        Pixels = 4,
    }

    /// <summary>
    /// Composable size primitive. A struct (no allocs) carrying a <see cref="SizeKind"/>
    /// and a single <c>float</c> payload. Static factories cover the common shapes;
    /// implicit conversion from <c>float</c> produces <see cref="Pixels"/>.
    ///
    /// <para>P1 does not implement <c>Size.Lerp</c> — blendable sizes land in P2 with
    /// the reconciler. P1 only ensures every primitive accepts a Size argument shape.</para>
    /// </summary>
    public readonly struct Size
    {
        public SizeKind Kind { get; }
        public float Value { get; }

        /// <summary>Floor in pixels below which the resolved size may not shrink. 0 = unset.
        /// V2-UI-PARITY.3.1 — the "stop shrinking below min-content" primitive.</summary>
        public float Min { get; }

        /// <summary>Ceiling in pixels above which the resolved size may not grow. 0 = unset.
        /// V2-UI-PARITY.3.1.</summary>
        public float Max { get; }

        private Size(SizeKind kind, float value, float min = 0f, float max = 0f)
        {
            Kind = kind;
            Value = value;
            Min = min;
            Max = max;
        }

        public static Size Fit() => new Size(SizeKind.Fit, 1f);
        public static Size FitContent(float fraction) => new Size(SizeKind.Fit, fraction);
        public static Size Expand(float weight = 1f) => new Size(SizeKind.Expand, weight);
        public static Size Ratio(float ratio) => new Size(SizeKind.Ratio, ratio);
        public static Size Percentage(float fraction) => new Size(SizeKind.Percentage, fraction);
        public static Size Pixels(float pixels) => new Size(SizeKind.Pixels, pixels);

        /// <summary>A raw <c>float</c> is interpreted as pixels — matches PanGui's
        /// implicit conversion. Lets call sites write <c>.Width(200)</c>.</summary>
        public static implicit operator Size(float pixels) => Pixels(pixels);
        public static implicit operator Size(int pixels) => Pixels(pixels);

        /// <summary>Returns a copy with a pixel floor. V2-UI-PARITY.3.1.</summary>
        public Size WithMin(float min) => new Size(Kind, Value, min, Max);

        /// <summary>Returns a copy with a pixel ceiling. V2-UI-PARITY.3.1.</summary>
        public Size WithMax(float max) => new Size(Kind, Value, Min, max);

        /// <summary>Returns a copy with both a pixel floor and ceiling. V2-UI-PARITY.3.1.</summary>
        public Size WithMinMax(float min, float max) => new Size(Kind, Value, min, max);

        public override string ToString() => Kind switch
        {
            SizeKind.Fit => Value == 1f ? "Fit" : $"FitContent({Value})",
            SizeKind.Expand => $"Expand({Value})",
            SizeKind.Ratio => $"Ratio({Value})",
            SizeKind.Percentage => $"Percentage({Value})",
            SizeKind.Pixels => $"{Value}px",
            _ => $"Size({Kind}, {Value})",
        };
    }
}
