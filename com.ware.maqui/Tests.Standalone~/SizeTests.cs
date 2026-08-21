// SPDX-License-Identifier: MIT
// MaqUI v2 — Size.WithMin/WithMax/WithMinMax tests (V2-UI-PARITY.3.7, headless).

using Maqui;
using Xunit;

namespace Maqui.Tests
{
    public class SizeTests
    {
        [Fact]
        public void Factories_DefaultMinMax_ToZero()
        {
            Assert.Equal(0f, Size.Fit().Min);
            Assert.Equal(0f, Size.Fit().Max);
            Assert.Equal(0f, Size.Expand().Min);
            Assert.Equal(0f, Size.Ratio(1.5f).Max);
            Assert.Equal(0f, Size.Percentage(0.5f).Min);
            Assert.Equal(0f, Size.Pixels(100f).Max);
        }

        [Fact]
        public void ImplicitConversion_DefaultsMinMax_ToZero()
        {
            Size s = 200f;
            Assert.Equal(0f, s.Min);
            Assert.Equal(0f, s.Max);
        }

        [Fact]
        public void WithMin_SetsMin_LeavesMaxZero()
        {
            var s = Size.Expand().WithMin(200f);
            Assert.Equal(200f, s.Min);
            Assert.Equal(0f, s.Max);
            Assert.Equal(SizeKind.Expand, s.Kind);
        }

        [Fact]
        public void WithMax_SetsMax_LeavesMinZero()
        {
            var s = Size.Fit().WithMax(500f);
            Assert.Equal(0f, s.Min);
            Assert.Equal(500f, s.Max);
        }

        [Fact]
        public void WithMinMax_SetsBoth()
        {
            var s = Size.Expand().WithMinMax(200f, 500f);
            Assert.Equal(200f, s.Min);
            Assert.Equal(500f, s.Max);
        }

        [Fact]
        public void WithMin_PreservesKindAndValue()
        {
            var s = Size.Pixels(64f).WithMin(10f);
            Assert.Equal(SizeKind.Pixels, s.Kind);
            Assert.Equal(64f, s.Value);
        }

        [Fact]
        public void Chaining_WithMin_ThenWithMax_KeepsBoth()
        {
            var s = Size.Fit().WithMin(100f).WithMax(300f);
            Assert.Equal(100f, s.Min);
            Assert.Equal(300f, s.Max);
        }
    }
}
