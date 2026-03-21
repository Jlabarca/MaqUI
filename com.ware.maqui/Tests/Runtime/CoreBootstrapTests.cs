using System.Collections;
using NUnit.Framework;
using Maqui.Core;
using Maqui.Core.Bridge;
using Maqui.Core.Presentation;
using UnityEngine;
using UnityEngine.TestTools;

namespace Maqui.Tests
{
    public class CoreBootstrapTests
    {
        private GameObject _coreObj;

        [SetUp]
        public void SetUp()
        {
            MaquiServices.Reset();
            DestroyExistingCore();
        }

        [TearDown]
        public void TearDown()
        {
            MaquiServices.Reset();
            DestroyExistingCore();
        }

        private void DestroyExistingCore()
        {
            _coreObj = GameObject.Find("Maqui_Core");
            if (_coreObj != null)
                Object.DestroyImmediate(_coreObj);
        }

        [UnityTest]
        public IEnumerator Initialize_CreatesGameObjectWithCorrectName()
        {
            CoreBootstrap.Initialize(null);
            yield return null;

            _coreObj = GameObject.Find("Maqui_Core");
            Assert.IsNotNull(_coreObj, "Maqui_Core GameObject should exist");
        }

        [UnityTest]
        public IEnumerator Initialize_AttachesAllFiveComponents()
        {
            CoreBootstrap.Initialize(null);
            yield return null;

            _coreObj = GameObject.Find("Maqui_Core");

            Assert.IsNotNull(_coreObj.GetComponent<InputBridge>(), "InputBridge should be attached");
            Assert.IsNotNull(_coreObj.GetComponent<RouterBridge>(), "RouterBridge should be attached");
            Assert.IsNotNull(_coreObj.GetComponent<ThemeProvider>(), "ThemeProvider should be attached");
            Assert.IsNotNull(_coreObj.GetComponent<AnimationBridge>(), "AnimationBridge should be attached");
            Assert.IsNotNull(_coreObj.GetComponent<MaquiWindowManager>(), "MaquiWindowManager should be attached");
        }

        [UnityTest]
        public IEnumerator Initialize_AllServicesRegisteredInMaquiServices()
        {
            CoreBootstrap.Initialize(null);
            yield return null;

            Assert.IsNotNull(MaquiServices.Get<IInputBridge>(), "IInputBridge should be registered");
            Assert.IsNotNull(MaquiServices.Get<IRouterBridge>(), "IRouterBridge should be registered");
            Assert.IsNotNull(MaquiServices.Get<IThemeProvider>(), "IThemeProvider should be registered");
            Assert.IsNotNull(MaquiServices.Get<IAnimationBridge>(), "IAnimationBridge should be registered");
            Assert.IsNotNull(MaquiServices.Get<IUIService>(), "IUIService should be registered");
        }

        [UnityTest]
        public IEnumerator Initialize_MaquiWindowManager_HasLayerCanvases()
        {
            CoreBootstrap.Initialize(null);
            yield return null;

            var wm = MaquiServices.Get<IUIService>() as MaquiWindowManager;
            Assert.IsNotNull(wm);
            Assert.IsNotNull(wm.GetLayerCanvas(UILayer.Background));
            Assert.IsNotNull(wm.GetLayerCanvas(UILayer.Default));
            Assert.IsNotNull(wm.GetLayerCanvas(UILayer.Overlay));
            Assert.IsNotNull(wm.GetLayerCanvas(UILayer.Modal));
        }

        // --- MaquiConfig tests ---

        [UnityTest]
        public IEnumerator Initialize_NullConfig_CreatesAllBridges()
        {
            // Null config = no MaquiConfig asset found = backward-compatible default
            CoreBootstrap.Initialize(null);
            yield return null;

            _coreObj = GameObject.Find("Maqui_Core");
            Assert.IsNotNull(_coreObj, "Maqui_Core should exist");
            Assert.IsNotNull(MaquiServices.Get<IInputBridge>(), "IInputBridge should be registered");
            Assert.IsNotNull(MaquiServices.Get<IRouterBridge>(), "IRouterBridge should be registered");
            Assert.IsNotNull(MaquiServices.Get<IThemeProvider>(), "IThemeProvider should be registered");
            Assert.IsNotNull(MaquiServices.Get<IAnimationBridge>(), "IAnimationBridge should be registered");
            Assert.IsNotNull(MaquiServices.Get<IUIService>(), "IUIService should be registered");
        }

        [UnityTest]
        public IEnumerator Initialize_DefaultConfig_CreatesAllBridges()
        {
            // Default config with all flags true = same as null config
            var config = ScriptableObject.CreateInstance<MaquiConfig>();
            CoreBootstrap.Initialize(config);
            yield return null;

            _coreObj = GameObject.Find("Maqui_Core");
            Assert.IsNotNull(_coreObj, "Maqui_Core should exist");
            Assert.IsNotNull(MaquiServices.Get<IInputBridge>(), "IInputBridge should be registered");
            Assert.IsNotNull(MaquiServices.Get<IRouterBridge>(), "IRouterBridge should be registered");
            Assert.IsNotNull(MaquiServices.Get<IThemeProvider>(), "IThemeProvider should be registered");
            Assert.IsNotNull(MaquiServices.Get<IAnimationBridge>(), "IAnimationBridge should be registered");
            Assert.IsNotNull(MaquiServices.Get<IUIService>(), "IUIService should be registered");

            Object.DestroyImmediate(config);
        }

        [UnityTest]
        public IEnumerator Initialize_HeadlessConfig_CreatesNoBridges()
        {
            var config = ScriptableObject.CreateInstance<MaquiConfig>();
            config.Headless = true;
            CoreBootstrap.Initialize(config);
            yield return null;

            _coreObj = GameObject.Find("Maqui_Core");
            Assert.IsNotNull(_coreObj, "Maqui_Core should still exist in headless mode");

            // No bridges should be attached
            Assert.IsNull(_coreObj.GetComponent<InputBridge>(), "InputBridge should NOT be attached");
            Assert.IsNull(_coreObj.GetComponent<RouterBridge>(), "RouterBridge should NOT be attached");
            Assert.IsNull(_coreObj.GetComponent<ThemeProvider>(), "ThemeProvider should NOT be attached");
            Assert.IsNull(_coreObj.GetComponent<AnimationBridge>(), "AnimationBridge should NOT be attached");
            Assert.IsNull(_coreObj.GetComponent<MaquiWindowManager>(), "MaquiWindowManager should NOT be attached");

            // No services should be registered
            Assert.IsNull(MaquiServices.Get<IInputBridge>(), "IInputBridge should NOT be registered");
            Assert.IsNull(MaquiServices.Get<IRouterBridge>(), "IRouterBridge should NOT be registered");
            Assert.IsNull(MaquiServices.Get<IThemeProvider>(), "IThemeProvider should NOT be registered");
            Assert.IsNull(MaquiServices.Get<IAnimationBridge>(), "IAnimationBridge should NOT be registered");
            Assert.IsNull(MaquiServices.Get<IUIService>(), "IUIService should NOT be registered");

            Object.DestroyImmediate(config);
        }

        [UnityTest]
        public IEnumerator Initialize_SelectiveConfig_SkipsDisabledBridge()
        {
            var config = ScriptableObject.CreateInstance<MaquiConfig>();
            config.EnableTheme = false;
            config.EnableAnimation = false;
            CoreBootstrap.Initialize(config);
            yield return null;

            _coreObj = GameObject.Find("Maqui_Core");

            // Enabled bridges should exist
            Assert.IsNotNull(MaquiServices.Get<IInputBridge>(), "IInputBridge should be registered");
            Assert.IsNotNull(MaquiServices.Get<IRouterBridge>(), "IRouterBridge should be registered");
            Assert.IsNotNull(MaquiServices.Get<IUIService>(), "IUIService should be registered");

            // Disabled bridges should not exist
            Assert.IsNull(_coreObj.GetComponent<ThemeProvider>(), "ThemeProvider should NOT be attached");
            Assert.IsNull(_coreObj.GetComponent<AnimationBridge>(), "AnimationBridge should NOT be attached");
            Assert.IsNull(MaquiServices.Get<IThemeProvider>(), "IThemeProvider should NOT be registered");
            Assert.IsNull(MaquiServices.Get<IAnimationBridge>(), "IAnimationBridge should NOT be registered");

            Object.DestroyImmediate(config);
        }

        [UnityTest]
        public IEnumerator Initialize_SelectiveConfig_DisableWindowManager()
        {
            var config = ScriptableObject.CreateInstance<MaquiConfig>();
            config.EnableWindowManager = false;
            CoreBootstrap.Initialize(config);
            yield return null;

            _coreObj = GameObject.Find("Maqui_Core");

            // All bridges except window manager should exist
            Assert.IsNotNull(MaquiServices.Get<IInputBridge>(), "IInputBridge should be registered");
            Assert.IsNotNull(MaquiServices.Get<IRouterBridge>(), "IRouterBridge should be registered");
            Assert.IsNotNull(MaquiServices.Get<IThemeProvider>(), "IThemeProvider should be registered");
            Assert.IsNotNull(MaquiServices.Get<IAnimationBridge>(), "IAnimationBridge should be registered");

            // Window manager should not exist
            Assert.IsNull(_coreObj.GetComponent<MaquiWindowManager>(), "MaquiWindowManager should NOT be attached");
            Assert.IsNull(MaquiServices.Get<IUIService>(), "IUIService should NOT be registered");

            Object.DestroyImmediate(config);
        }
    }
}
