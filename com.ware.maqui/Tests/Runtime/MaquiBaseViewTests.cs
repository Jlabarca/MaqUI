using System.Collections;
using NUnit.Framework;
using Maqui.Core.Presentation;
using UnityEngine;
using UnityEngine.TestTools;

namespace Maqui.Tests
{
    public class MaquiBaseViewTests
    {
        private GameObject _go;

        [TearDown]
        public void TearDown()
        {
            if (_go != null) Object.DestroyImmediate(_go);
        }

        [UnityTest]
        public IEnumerator BaseView_StartsVisible_WhenInitialVisibilityTrue()
        {
            _go = CreateBaseViewObject(initialVisibility: true);
            yield return null; // wait for Start()

            var view = _go.GetComponent<TestBaseView>();
            Assert.IsTrue(view.IsViewVisible);
            Assert.AreEqual(1f, view.ViewCanvasGroup.alpha);
            Assert.IsTrue(view.ViewCanvasGroup.blocksRaycasts);
            Assert.IsTrue(view.ViewCanvasGroup.interactable);
        }

        [UnityTest]
        public IEnumerator BaseView_StartsHidden_WhenInitialVisibilityFalse()
        {
            _go = CreateBaseViewObject(initialVisibility: false);
            yield return null;

            var view = _go.GetComponent<TestBaseView>();
            Assert.IsFalse(view.IsViewVisible);
            Assert.AreEqual(0f, view.ViewCanvasGroup.alpha);
            Assert.IsFalse(view.ViewCanvasGroup.blocksRaycasts);
            Assert.IsFalse(view.ViewCanvasGroup.interactable);
        }

        [UnityTest]
        public IEnumerator ShowView_MakesViewVisible()
        {
            _go = CreateBaseViewObject(initialVisibility: false);
            yield return null;

            var view = _go.GetComponent<TestBaseView>();
            view.ShowView();

            Assert.IsTrue(view.IsViewVisible);
            Assert.AreEqual(1f, view.ViewCanvasGroup.alpha);
        }

        [UnityTest]
        public IEnumerator HideView_MakesViewInvisible()
        {
            _go = CreateBaseViewObject(initialVisibility: true);
            yield return null;

            var view = _go.GetComponent<TestBaseView>();
            view.HideView();

            Assert.IsFalse(view.IsViewVisible);
            Assert.AreEqual(0f, view.ViewCanvasGroup.alpha);
        }

        [UnityTest]
        public IEnumerator ToggleView_SwitchesVisibility()
        {
            _go = CreateBaseViewObject(initialVisibility: true);
            yield return null;

            var view = _go.GetComponent<TestBaseView>();
            Assert.IsTrue(view.IsViewVisible);

            view.ToggleView();
            Assert.IsFalse(view.IsViewVisible);

            view.ToggleView();
            Assert.IsTrue(view.IsViewVisible);
        }

        [UnityTest]
        public IEnumerator LifecycleHooks_CalledInOrder()
        {
            _go = CreateBaseViewObject(initialVisibility: true);
            yield return null;

            var view = _go.GetComponent<TestBaseView>();
            Assert.IsTrue(view.AwakeCalled, "OnViewAwake should be called");
            Assert.IsTrue(view.StartCalled, "OnViewStart should be called");

            Object.DestroyImmediate(_go);
            _go = null;
            Assert.IsTrue(view.DestroyCalled, "OnViewDestroy should be called");
        }

        [UnityTest]
        public IEnumerator RequiredComponents_AutoAttached()
        {
            _go = CreateBaseViewObject(initialVisibility: true);
            yield return null;

            Assert.IsNotNull(_go.GetComponent<CanvasGroup>());
            Assert.IsNotNull(_go.GetComponent<RectTransform>());
        }

        private static GameObject CreateBaseViewObject(bool initialVisibility)
        {
            var go = new GameObject("TestBaseView");
            var view = go.AddComponent<TestBaseView>();
            view.InitialVisibility = initialVisibility;
            return go;
        }

        private class TestBaseView : MaquiBaseView
        {
            public bool AwakeCalled { get; private set; }
            public bool StartCalled { get; private set; }
            public bool DestroyCalled { get; private set; }

            public override void OnViewAwake()
            {
                base.OnViewAwake();
                AwakeCalled = true;
            }

            public override void OnViewStart()
            {
                base.OnViewStart();
                StartCalled = true;
            }

            public override void OnViewDestroy()
            {
                base.OnViewDestroy();
                DestroyCalled = true;
            }
        }
    }
}
