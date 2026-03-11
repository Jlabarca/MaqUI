using NUnit.Framework;
using Maqui.Core.Logic;
using UnityEngine;

namespace Maqui.Tests
{
    public class ThemeDataTests
    {
        [Test]
        public void ThemeData_GetColor_ReturnsCorrectColor()
        {
            var theme = ScriptableObject.CreateInstance<ThemeData>();
            theme.PrimaryColor = Color.red;

            var result = theme.GetColor(ThemeColorType.Primary);

            Assert.AreEqual(Color.red, result);
            Object.DestroyImmediate(theme);
        }

        [Test]
        public void ThemeData_GetColor_None_ReturnsWhite()
        {
            var theme = ScriptableObject.CreateInstance<ThemeData>();

            var result = theme.GetColor(ThemeColorType.None);

            Assert.AreEqual(Color.white, result);
            Object.DestroyImmediate(theme);
        }
    }
}
