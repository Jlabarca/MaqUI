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
            CoreBootstrap.Initialize();
            yield return null;

            _coreObj = GameObject.Find("Maqui_Core");
            Assert.IsNotNull(_coreObj, "Maqui_Core GameObject should exist");
        }

        [UnityTest]
        public IEnumerator Initialize_AttachesAllFiveComponents()
        {
            CoreBootstrap.Initialize();
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
            CoreBootstrap.Initialize();
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
            CoreBootstrap.Initialize();
            yield return null;

            var wm = MaquiServices.Get<IUIService>() as MaquiWindowManager;
            Assert.IsNotNull(wm);
            Assert.IsNotNull(wm.GetLayerCanvas(UILayer.Background));
            Assert.IsNotNull(wm.GetLayerCanvas(UILayer.Default));
            Assert.IsNotNull(wm.GetLayerCanvas(UILayer.Overlay));
            Assert.IsNotNull(wm.GetLayerCanvas(UILayer.Modal));
        }
    }
}
