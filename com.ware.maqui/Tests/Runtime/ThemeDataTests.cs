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

        [Test]
        public void ThemeData_GetColor_WithoutParent_ReturnsOwnColor()
        {
            var theme = ScriptableObject.CreateInstance<ThemeData>();
            theme.PrimaryColor = Color.magenta;

            var result = theme.GetColor(ThemeColorType.Primary);

            Assert.AreEqual(Color.magenta, result);
            Object.DestroyImmediate(theme);
        }

        [Test]
        public void ThemeData_GetColor_WithParent_ReturnsParentForNonOverridden()
        {
            var parent = ScriptableObject.CreateInstance<ThemeData>();
            parent.AccentColor = Color.green;

            var child = ScriptableObject.CreateInstance<ThemeData>();
            child.Parent = parent;
            child.AccentColor = Color.black; // local value exists but slot is not overridden
            child.ClearOverride(ThemeColorType.Accent);

            var result = child.GetColor(ThemeColorType.Accent);

            Assert.AreEqual(Color.green, result);
            Object.DestroyImmediate(child);
            Object.DestroyImmediate(parent);
        }

        [Test]
        public void ThemeData_GetColor_WithParent_ReturnsOwnForOverriddenSlot()
        {
            var parent = ScriptableObject.CreateInstance<ThemeData>();
            parent.PrimaryColor = Color.red;

            var child = ScriptableObject.CreateInstance<ThemeData>();
            child.Parent = parent;
            child.SetColorOverride(ThemeColorType.Primary, Color.blue);

            var result = child.GetColor(ThemeColorType.Primary);

            Assert.AreEqual(Color.blue, result);
            Object.DestroyImmediate(child);
            Object.DestroyImmediate(parent);
        }

        [Test]
        public void ThemeData_GetColor_GrandparentChain_Inherits()
        {
            var grandparent = ScriptableObject.CreateInstance<ThemeData>();
            grandparent.Success = Color.yellow;

            var parent = ScriptableObject.CreateInstance<ThemeData>();
            parent.Parent = grandparent;
            parent.ClearOverride(ThemeColorType.Success);

            var child = ScriptableObject.CreateInstance<ThemeData>();
            child.Parent = parent;
            child.ClearOverride(ThemeColorType.Success);

            var result = child.GetColor(ThemeColorType.Success);

            Assert.AreEqual(Color.yellow, result);
            Object.DestroyImmediate(child);
            Object.DestroyImmediate(parent);
            Object.DestroyImmediate(grandparent);
        }

        [Test]
        public void ThemeData_IsOverridden_DefaultsToTrue()
        {
            var theme = ScriptableObject.CreateInstance<ThemeData>();

            Assert.IsTrue(theme.IsOverridden(ThemeColorType.Primary));
            Assert.IsTrue(theme.IsOverridden(ThemeColorType.Accent));
            Assert.IsTrue(theme.IsOverridden(ThemeColorType.Info));
            Object.DestroyImmediate(theme);
        }

        [Test]
        public void ThemeData_IsOverridden_ReturnsFalseAfterClear()
        {
            var theme = ScriptableObject.CreateInstance<ThemeData>();
            theme.ClearOverride(ThemeColorType.Warning);

            Assert.IsFalse(theme.IsOverridden(ThemeColorType.Warning));
            Object.DestroyImmediate(theme);
        }

        [Test]
        public void ThemeData_SetColorOverride_SetsColorAndMarksOverridden()
        {
            var parent = ScriptableObject.CreateInstance<ThemeData>();
            parent.Danger = Color.white;

            var child = ScriptableObject.CreateInstance<ThemeData>();
            child.Parent = parent;
            child.ClearOverride(ThemeColorType.Danger);

            // Verify it inherits first
            Assert.AreEqual(Color.white, child.GetColor(ThemeColorType.Danger));

            // Now override
            child.SetColorOverride(ThemeColorType.Danger, Color.magenta);

            Assert.IsTrue(child.IsOverridden(ThemeColorType.Danger));
            Assert.AreEqual(Color.magenta, child.GetColor(ThemeColorType.Danger));

            Object.DestroyImmediate(child);
            Object.DestroyImmediate(parent);
        }
    }
}
