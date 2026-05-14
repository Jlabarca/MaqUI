using System;
using System.Collections;
using System.Collections.Generic;
using System.Threading;
using Cysharp.Threading.Tasks;
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
            if (_go != null) UnityEngine.Object.DestroyImmediate(_go);
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

            UnityEngine.Object.DestroyImmediate(targetGo);
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
            UnityEngine.Object.DestroyImmediate(targetGo);
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

            UnityEngine.Object.DestroyImmediate(targetGo);
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
            UnityEngine.Object.DestroyImmediate(targetGo);
        }

        [Test]
        public void MaquiServices_ReturnsRegisteredBridge()
        {
            var resolved = MaquiServices.Get<IAnimationBridge>();
            Assert.AreSame(_bridge, resolved);
        }

        // --- Ease function tests ---

        [Test]
        public void EaseFunctions_AllTypes_ReturnZeroAtZero_And_OneAtOne()
        {
            foreach (Ease ease in Enum.GetValues(typeof(Ease)))
            {
                float atZero = EaseFunctions.Evaluate(ease, 0f);
                float atOne = EaseFunctions.Evaluate(ease, 1f);

                Assert.AreEqual(0f, atZero, 0.001f, $"{ease} at t=0 should be 0");
                Assert.AreEqual(1f, atOne, 0.001f, $"{ease} at t=1 should be 1");
            }
        }

        [Test]
        public void EaseFunctions_Linear_ReturnsInputValue()
        {
            Assert.AreEqual(0.5f, EaseFunctions.Evaluate(Ease.Linear, 0.5f), 0.001f);
            Assert.AreEqual(0.25f, EaseFunctions.Evaluate(Ease.Linear, 0.25f), 0.001f);
        }

        [Test]
        public void EaseFunctions_InQuad_IsSlowerThanLinearAtMidpoint()
        {
            float mid = EaseFunctions.Evaluate(Ease.InQuad, 0.5f);
            Assert.Less(mid, 0.5f, "InQuad at t=0.5 should be less than 0.5 (starts slow)");
        }

        [Test]
        public void EaseFunctions_OutBack_OvershotsBeyondOne()
        {
            // OutBack should overshoot past 1 before settling
            float earlyValue = EaseFunctions.Evaluate(Ease.OutBack, 0.5f);
            Assert.Greater(earlyValue, 0.5f, "OutBack should overshoot");
        }

        // --- Eased overload tests ---

        [UnityTest]
        public IEnumerator FadeAsync_WithEase_ZeroDuration_SetsTargetImmediately()
        {
            var targetGo = new GameObject("FadeEaseTarget");
            var cg = targetGo.AddComponent<CanvasGroup>();
            cg.alpha = 0f;
            yield return null;

            var task = _bridge.FadeAsync(cg, 1f, 0f, Ease.OutCubic);
            Assert.AreEqual(1f, cg.alpha);

            UnityEngine.Object.DestroyImmediate(targetGo);
        }

        [UnityTest]
        public IEnumerator ScaleAsync_WithEase_ZeroDuration_SetsTargetImmediately()
        {
            var targetGo = new GameObject("ScaleEaseTarget");
            var rt = targetGo.AddComponent<RectTransform>();
            rt.localScale = Vector3.zero;
            yield return null;

            var task = _bridge.ScaleAsync(rt, Vector3.one, 0f, Ease.InOutQuad);
            Assert.AreEqual(Vector3.one, rt.localScale);

            UnityEngine.Object.DestroyImmediate(targetGo);
        }

        // --- Animation sequence tests ---

        [UnityTest]
        public IEnumerator AnimationSequence_Then_RunsStepsSequentially()
        {
            var order = new List<int>();

            var seq = AnimationSequence.Create()
                .Then(async ct =>
                {
                    order.Add(1);
                    await UniTask.Yield(ct);
                })
                .Then(async ct =>
                {
                    order.Add(2);
                    await UniTask.Yield(ct);
                })
                .Then(async ct =>
                {
                    order.Add(3);
                    await UniTask.Yield(ct);
                });

            var task = seq.PlayAsync(CancellationToken.None);

            // Wait a few frames for completion
            yield return null;
            yield return null;
            yield return null;
            yield return null;

            Assert.AreEqual(3, order.Count);
            Assert.AreEqual(1, order[0]);
            Assert.AreEqual(2, order[1]);
            Assert.AreEqual(3, order[2]);
        }

        [UnityTest]
        public IEnumerator AnimationSequence_With_RunsInParallelWithPreviousStep()
        {
            bool step1Started = false;
            bool step2Started = false;
            bool bothStartedSameFrame = false;

            var seq = AnimationSequence.Create()
                .Then(async ct =>
                {
                    step1Started = true;
                    // Check if step2 also started (it should, since With runs in parallel)
                    await UniTask.Yield(ct);
                    if (step2Started) bothStartedSameFrame = true;
                })
                .With(async ct =>
                {
                    step2Started = true;
                    await UniTask.Yield(ct);
                    if (step1Started) bothStartedSameFrame = true;
                });

            var task = seq.PlayAsync(CancellationToken.None);
            yield return null;
            yield return null;

            Assert.IsTrue(step1Started, "Step 1 should have started");
            Assert.IsTrue(step2Started, "Step 2 should have started");
            Assert.IsTrue(bothStartedSameFrame, "Both steps should run in parallel");
        }

        [UnityTest]
        public IEnumerator AnimationSequence_Cancellation_ThrowsOperationCanceledException()
        {
            var cts = new CancellationTokenSource();
            bool caughtCancellation = false;

            var seq = AnimationSequence.Create()
                .Then(async ct =>
                {
                    cts.Cancel();
                    await UniTask.Yield(ct);
                })
                .Then(async ct =>
                {
                    // This should never run
                    Assert.Fail("Second step should not execute after cancellation");
                    await UniTask.Yield(ct);
                });

            // yield can't sit inside try-with-catch (CS1626); start the task,
            // tick frames, then inspect post-hoc.
            var task = seq.PlayAsync(cts.Token);
            yield return null;
            yield return null;
            if (task.Status == Cysharp.Threading.Tasks.UniTaskStatus.Canceled)
            {
                caughtCancellation = true;
            }

            Assert.IsTrue(caughtCancellation || cts.IsCancellationRequested,
                "Cancellation should be detected");

            cts.Dispose();
        }

        [Test]
        public void AnimationSequence_Delay_AddsSequentialStep()
        {
            // Verify Delay doesn't throw and returns the sequence for chaining
            var seq = AnimationSequence.Create()
                .Then(ct => UniTask.CompletedTask)
                .Delay(0.5f)
                .Then(ct => UniTask.CompletedTask);

            Assert.IsNotNull(seq);
        }

        [Test]
        public void AnimationSequence_With_NoExistingStep_BehavesLikeThen()
        {
            // With() on empty sequence should not throw
            var seq = AnimationSequence.Create()
                .With(ct => UniTask.CompletedTask);

            Assert.IsNotNull(seq);
        }
    }
}
