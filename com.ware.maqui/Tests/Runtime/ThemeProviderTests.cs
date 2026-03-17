using System.Collections;
using System.Collections.Generic;
using NUnit.Framework;
using Maqui.Core;
using Maqui.Core.Bridge;
using Maqui.Core.Logic;
using R3;
using UnityEngine;
using UnityEngine.TestTools;

namespace Maqui.Tests
{
    public class ThemeProviderTests
    {
        private GameObject _go;
        private ThemeProvider _provider;

        [UnitySetUp]
        public IEnumerator SetUp()
        {
            MaquiServices.Reset();

            _go = new GameObject("TestThemeProvider");
            _provider = _go.AddComponent<ThemeProvider>();
            MaquiServices.Register<IThemeProvider>(_provider);
            yield return null;
        }

        [TearDown]
        public void TearDown()
        {
            if (_go != null) Object.DestroyImmediate(_go);
            MaquiServices.Reset();
        }

        [Test]
        public void SetTheme_NullTheme_DoesNothing()
        {
            _provider.SetTheme(null);
            Assert.IsNull(_provider.CurrentTheme.CurrentValue);
        }

        [Test]
        public void SetTheme_ValidTheme_UpdatesCurrentTheme()
        {
            var theme = ScriptableObject.CreateInstance<ThemeData>();
            theme.ThemeName = "TestDark";

            _provider.SetTheme(theme);

            Assert.AreSame(theme, _provider.CurrentTheme.CurrentValue);
            Object.DestroyImmediate(theme);
        }

        [Test]
        public void CurrentTheme_ReactiveEmission_NotifiesSubscribers()
        {
            var received = new List<ThemeData>();
            var disposable = _provider.CurrentTheme
                .Where(t => t != null)
                .Subscribe(t => received.Add(t));

            var theme1 = ScriptableObject.CreateInstance<ThemeData>();
            theme1.ThemeName = "Theme1";
            var theme2 = ScriptableObject.CreateInstance<ThemeData>();
            theme2.ThemeName = "Theme2";

            _provider.SetTheme(theme1);
            _provider.SetTheme(theme2);

            Assert.AreEqual(2, received.Count);
            Assert.AreSame(theme1, received[0]);
            Assert.AreSame(theme2, received[1]);

            disposable.Dispose();
            Object.DestroyImmediate(theme1);
            Object.DestroyImmediate(theme2);
        }

        [Test]
        public void MaquiServices_ReturnsRegisteredProvider()
        {
            var resolved = MaquiServices.Get<IThemeProvider>();
            Assert.AreSame(_provider, resolved);
        }
    }
}
