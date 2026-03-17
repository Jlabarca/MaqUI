using System.Collections;
using NUnit.Framework;
using Maqui.Core;
using Maqui.Core.Bridge;
using UnityEngine;
using UnityEngine.TestTools;

namespace Maqui.Tests
{
    public class AnimationBridgeTests
    {
        private GameObject _go;
        private AnimationBridge _bridge;

        [UnitySetUp]
        public IEnumerator SetUp()
        {
            MaquiServices.Reset();

            _go = new GameObject("TestAnimationBridge");
            _bridge = _go.AddComponent<AnimationBridge>();
            MaquiServices.Register<IAnimationBridge>(_bridge);
            yield return null;
        }

        [TearDown]
        public void TearDown()
        {
            if (_go != null) Object.DestroyImmediate(_go);
            MaquiServices.Reset();
        }

        [UnityTest]
        public IEnumerator FadeAsync_NullCanvasGroup_DoesNotThrow()
        {
            yield return null;

            var task = _bridge.FadeAsync(null, 1f, 0.1f);
            Assert.IsTrue(task.Status == Cysharp.Threading.Tasks.UniTaskStatus.Succeeded);
        }

        [UnityTest]
        public IEnumerator FadeAsync_ZeroDuration_SetsTargetImmediately()
        {
            var targetGo = new GameObject("FadeTarget");
            var cg = targetGo.AddComponent<CanvasGroup>();
            cg.alpha = 0f;
            yield return null;

            var task = _bridge.FadeAsync(cg, 1f, 0f);
            Assert.AreEqual(1f, cg.alpha);

            Object.DestroyImmediate(targetGo);
        }

        [UnityTest]
        public IEnumerator FadeAsync_Cancellation_StopsEarly()
        {
            var targetGo = new GameObject("FadeTarget");
            var cg = targetGo.AddComponent<CanvasGroup>();
            cg.alpha = 0f;
            yield return null;

            var cts = new System.Threading.CancellationTokenSource();
            cts.Cancel(); // Cancel immediately

            var task = _bridge.FadeAsync(cg, 1f, 5f, cts.Token);
            Assert.AreNotEqual(1f, cg.alpha);

            cts.Dispose();
            Object.DestroyImmediate(targetGo);
        }

        [UnityTest]
        public IEnumerator ScaleAsync_NullTransform_DoesNotThrow()
        {
            yield return null;

            var task = _bridge.ScaleAsync(null, Vector3.one, 0.1f);
            Assert.IsTrue(task.Status == Cysharp.Threading.Tasks.UniTaskStatus.Succeeded);
        }

        [UnityTest]
        public IEnumerator ScaleAsync_ZeroDuration_SetsTargetImmediately()
        {
            var targetGo = new GameObject("ScaleTarget");
            var rt = targetGo.AddComponent<RectTransform>();
            rt.localScale = Vector3.zero;
            yield return null;

            var task = _bridge.ScaleAsync(rt, Vector3.one, 0f);
            Assert.AreEqual(Vector3.one, rt.localScale);

            Object.DestroyImmediate(targetGo);
        }

        [UnityTest]
        public IEnumerator ScaleAsync_Cancellation_StopsEarly()
        {
            var targetGo = new GameObject("ScaleTarget");
            var rt = targetGo.AddComponent<RectTransform>();
            rt.localScale = Vector3.zero;
            yield return null;

            var cts = new System.Threading.CancellationTokenSource();
            cts.Cancel();

            var task = _bridge.ScaleAsync(rt, Vector3.one * 5f, 5f, ct: cts.Token);
            Assert.AreNotEqual(Vector3.one * 5f, rt.localScale);

            cts.Dispose();
            Object.DestroyImmediate(targetGo);
        }

        [Test]
        public void MaquiServices_ReturnsRegisteredBridge()
        {
            var resolved = MaquiServices.Get<IAnimationBridge>();
            Assert.AreSame(_bridge, resolved);
        }
    }
}
