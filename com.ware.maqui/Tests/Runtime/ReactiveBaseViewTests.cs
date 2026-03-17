using System.Collections;
using NUnit.Framework;
using Maqui.Core;
using Maqui.Core.Logic;
using Maqui.Core.Presentation;
using R3;
using UnityEngine;
using UnityEngine.TestTools;

namespace Maqui.Tests
{
    public class ReactiveBaseViewTests
    {
        private GameObject _go;

        [SetUp]
        public void SetUp()
        {
            MaquiServices.Reset();
        }

        [TearDown]
        public void TearDown()
        {
            if (_go != null) Object.DestroyImmediate(_go);
            MaquiServices.Reset();
        }

        [UnityTest]
        public IEnumerator Initialize_SetsViewModelAndCallsOnBind()
        {
            _go = new GameObject("TestView");
            var view = _go.AddComponent<ConcreteTestView>();
            yield return null;

            var vm = new CounterViewModel();
            view.Initialize(vm);

            Assert.IsTrue(vm.WasInitialized);
            Assert.IsTrue(view.OnBindCalled);
            Assert.AreSame(vm, view.ExposedViewModel);
        }

        [UnityTest]
        public IEnumerator OnBind_ReactiveSubscription_Works()
        {
            _go = new GameObject("TestView");
            var view = _go.AddComponent<ConcreteTestView>();
            yield return null;

            var vm = new CounterViewModel();
            view.Initialize(vm);

            // Initial value emitted on subscribe
            Assert.AreEqual(0, view.LastCount);

            vm.Count.Value = 42;
            Assert.AreEqual(42, view.LastCount);
        }

        [UnityTest]
        public IEnumerator OnViewDestroy_DisposesViewModelAndSubscriptions()
        {
            _go = new GameObject("TestView");
            var view = _go.AddComponent<ConcreteTestView>();
            yield return null;

            var vm = new CounterViewModel();
            view.Initialize(vm);

            Object.DestroyImmediate(_go);
            _go = null;

            Assert.IsTrue(vm.WasDisposed);

            // Subscription should not update after dispose
            vm.Count.Value = 99;
            Assert.AreNotEqual(99, view.LastCount);
        }

        [UnityTest]
        public IEnumerator Freeze_Unfreeze_CalledCorrectly()
        {
            _go = new GameObject("TestView");
            var view = _go.AddComponent<ConcreteTestView>();
            yield return null;

            var vm = new CounterViewModel();
            view.Initialize(vm);

            Assert.IsFalse(view.IsFrozen);

            ((IFreezableView)view).InvokeFreeze();
            Assert.IsTrue(view.IsFrozen);

            ((IFreezableView)view).InvokeUnfreeze();
            Assert.IsFalse(view.IsFrozen);
        }

        // ── Test doubles ──────────────────────────────────────────────────────

        private class CounterViewModel : ViewModel
        {
            public readonly ReactiveProperty<int> Count = new(0);
            public bool WasInitialized { get; private set; }
            public bool WasDisposed { get; private set; }

            public override void Initialize()
            {
                base.Initialize();
                WasInitialized = true;
            }

            public override void Dispose()
            {
                WasDisposed = true;
                Count.Dispose();
                base.Dispose();
            }
        }

        private class ConcreteTestView : ReactiveBaseView<CounterViewModel>
        {
            public bool OnBindCalled { get; private set; }
            public int LastCount { get; private set; }
            public bool IsFrozen { get; private set; }
            public CounterViewModel ExposedViewModel => ViewModel;

            protected override void OnBind()
            {
                OnBindCalled = true;
                ViewModel.Count
                    .Subscribe(c => LastCount = c)
                    .AddTo(Disposables);
            }

            protected override void OnFreeze() => IsFrozen = true;
            protected override void OnUnfreeze() => IsFrozen = false;
        }
    }
}
