using System.Collections;
using NUnit.Framework;
using Maqui.Core;
using Maqui.Core.Bridge;
using Maqui.Core.Logic;
using Maqui.Core.Presentation;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.TestTools;

namespace Maqui.Tests
{
    public class ThemeSubscriberTests
    {
        private GameObject _providerGo;
        private ThemeProvider _provider;
        private ThemeData _theme;

        [UnitySetUp]
        public IEnumerator SetUp()
        {
            MaquiServices.Reset();

            _providerGo = new GameObject("TestThemeProvider");
            _provider = _providerGo.AddComponent<ThemeProvider>();
            MaquiServices.Register<IThemeProvider>(_provider);
            yield return null;

            _theme = ScriptableObject.CreateInstance<ThemeData>();
            _theme.ThemeName = "TestTheme";
            _theme.PrimaryColor = Color.red;
            _theme.TextPrimary = Color.green;
        }

        [TearDown]
        public void TearDown()
        {
            if (_providerGo != null) Object.DestroyImmediate(_providerGo);
            if (_theme != null) Object.DestroyImmediate(_theme);
            MaquiServices.Reset();
        }

        // ── ThemeImageSubscriber ──────────────────────────────────────────────

        [UnityTest]
        public IEnumerator ImageSubscriber_AppliesColor_OnThemeChange()
        {
            var go = new GameObject("TestImageSub");
            var image = go.AddComponent<Image>();
            var sub = go.AddComponent<ThemeImageSubscriber>();
            sub.ColorType = ThemeColorType.Primary;
            yield return null; // Awake + Start

            _provider.SetTheme(_theme);
            yield return null;

            Assert.AreEqual(Color.red, image.color);
            Object.DestroyImmediate(go);
        }

        [UnityTest]
        public IEnumerator ImageSubscriber_ColorTypeNone_DoesNotApply()
        {
            var go = new GameObject("TestImageSub");
            var image = go.AddComponent<Image>();
            var originalColor = image.color;
            var sub = go.AddComponent<ThemeImageSubscriber>();
            sub.ColorType = ThemeColorType.None;
            yield return null;

            _provider.SetTheme(_theme);
            yield return null;

            Assert.AreEqual(originalColor, image.color);
            Object.DestroyImmediate(go);
        }

        [UnityTest]
        public IEnumerator ImageSubscriber_ReactsToMultipleThemeChanges()
        {
            var go = new GameObject("TestImageSub");
            var image = go.AddComponent<Image>();
            var sub = go.AddComponent<ThemeImageSubscriber>();
            sub.ColorType = ThemeColorType.Primary;
            yield return null;

            _provider.SetTheme(_theme);
            yield return null;
            Assert.AreEqual(Color.red, image.color);

            var theme2 = ScriptableObject.CreateInstance<ThemeData>();
            theme2.PrimaryColor = Color.blue;

            _provider.SetTheme(theme2);
            yield return null;
            Assert.AreEqual(Color.blue, image.color);

            Object.DestroyImmediate(go);
            Object.DestroyImmediate(theme2);
        }

        [UnityTest]
        public IEnumerator ImageSubscriber_Destroy_DisposesSubscription()
        {
            var go = new GameObject("TestImageSub");
            go.AddComponent<Image>();
            var sub = go.AddComponent<ThemeImageSubscriber>();
            sub.ColorType = ThemeColorType.Primary;
            yield return null;

            // Destroy the subscriber — should not throw when theme changes afterward
            Object.DestroyImmediate(go);

            var theme2 = ScriptableObject.CreateInstance<ThemeData>();
            theme2.PrimaryColor = Color.magenta;
            Assert.DoesNotThrow(() => _provider.SetTheme(theme2));
            Object.DestroyImmediate(theme2);
        }

        // ── ThemeTextSubscriber ───────────────────────────────────────────────

        [UnityTest]
        public IEnumerator TextSubscriber_AppliesColor_OnThemeChange()
        {
            var go = new GameObject("TestTextSub");
            var text = go.AddComponent<TMPro.TextMeshProUGUI>();
            var sub = go.AddComponent<ThemeTextSubscriber>();
            sub.ColorType = ThemeColorType.TextPrimary;
            yield return null;

            _provider.SetTheme(_theme);
            yield return null;

            Assert.AreEqual(Color.green, text.color);
            Object.DestroyImmediate(go);
        }

        [UnityTest]
        public IEnumerator TextSubscriber_ColorTypeNone_DoesNotApply()
        {
            var go = new GameObject("TestTextSub");
            var text = go.AddComponent<TMPro.TextMeshProUGUI>();
            var originalColor = text.color;
            var sub = go.AddComponent<ThemeTextSubscriber>();
            sub.ColorType = ThemeColorType.None;
            yield return null;

            _provider.SetTheme(_theme);
            yield return null;

            Assert.AreEqual(originalColor, text.color);
            Object.DestroyImmediate(go);
        }

        [UnityTest]
        public IEnumerator TextSubscriber_Destroy_DisposesSubscription()
        {
            var go = new GameObject("TestTextSub");
            go.AddComponent<TMPro.TextMeshProUGUI>();
            var sub = go.AddComponent<ThemeTextSubscriber>();
            sub.ColorType = ThemeColorType.TextPrimary;
            yield return null;

            Object.DestroyImmediate(go);

            var theme2 = ScriptableObject.CreateInstance<ThemeData>();
            Assert.DoesNotThrow(() => _provider.SetTheme(theme2));
            Object.DestroyImmediate(theme2);
        }
    }
}
