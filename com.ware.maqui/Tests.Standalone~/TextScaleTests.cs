// SPDX-License-Identifier: MIT
// MaqUI v2 — TextScale.Ramp tests (V2-UI-PARITY.3.7, headless).

using Maqui;
using Xunit;

namespace Maqui.Tests
{
    public class TextScaleTests
    {
        [Fact]
        public void AtReferenceWidth_ReturnsBaseFontSize()
        {
            float result = TextScale.Ramp(400f, baseFontSize: 16f, referenceWidth: 400f);
            Assert.Equal(16f, result, precision: 3);
        }

        [Fact]
        public void BelowReferenceWidth_ScalesDown()
        {
            // 300/400 * 16 = 12, inside the default [10,24] clamp range — a value
            // that actually exercises the scaling math instead of the floor clamp.
            float result = TextScale.Ramp(300f, baseFontSize: 16f, referenceWidth: 400f);
            Assert.Equal(12f, result, precision: 3);
        }

        [Fact]
        public void FarBelowReferenceWidth_ClampsToMin()
        {
            float result = TextScale.Ramp(200f, baseFontSize: 16f, referenceWidth: 400f);
            Assert.Equal(10f, result, precision: 3); // 8 clamped to min 10
        }

        [Fact]
        public void AboveReferenceWidth_ScalesUp_UntilClamp()
        {
            float result = TextScale.Ramp(800f, baseFontSize: 16f, minFontSize: 10f, maxFontSize: 24f, referenceWidth: 400f);
            Assert.Equal(24f, result, precision: 3); // 32 clamped to max
        }

        [Fact]
        public void ClampsToMinFontSize()
        {
            float result = TextScale.Ramp(1f, baseFontSize: 16f, minFontSize: 10f, maxFontSize: 24f, referenceWidth: 400f);
            Assert.Equal(10f, result, precision: 3);
        }

        [Fact]
        public void ClampsToMaxFontSize()
        {
            float result = TextScale.Ramp(10000f, baseFontSize: 16f, minFontSize: 10f, maxFontSize: 24f, referenceWidth: 400f);
            Assert.Equal(24f, result, precision: 3);
        }

        [Fact]
        public void ZeroWidth_ClampsToMin()
        {
            float result = TextScale.Ramp(0f, baseFontSize: 16f, minFontSize: 10f, maxFontSize: 24f, referenceWidth: 400f);
            Assert.Equal(10f, result, precision: 3);
        }

        [Fact]
        public void NegativeWidth_TreatedAsZero_ClampsToMin()
        {
            float result = TextScale.Ramp(-50f, baseFontSize: 16f, minFontSize: 10f, maxFontSize: 24f, referenceWidth: 400f);
            Assert.Equal(10f, result, precision: 3);
        }

        [Fact]
        public void ZeroOrNegativeReferenceWidth_ClampsBaseFontSizeDirectly()
        {
            float result = TextScale.Ramp(300f, baseFontSize: 16f, minFontSize: 10f, maxFontSize: 24f, referenceWidth: 0f);
            Assert.Equal(16f, result, precision: 3);

            float clamped = TextScale.Ramp(300f, baseFontSize: 999f, minFontSize: 10f, maxFontSize: 24f, referenceWidth: -1f);
            Assert.Equal(24f, clamped, precision: 3);
        }
    }
}
