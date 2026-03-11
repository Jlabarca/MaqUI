using NUnit.Framework;
using Maqui.Core.Logic;
using R3;

namespace Maqui.Tests
{
    public class ViewModelTests
    {
        [Test]
        public void ViewModel_Dispose_DisposesCompositeDisposable()
        {
            var vm = new TestViewModel();
            bool disposed = false;

            vm.Disposables.Add(Disposable.Create(() => disposed = true));
            vm.Dispose();

            Assert.IsTrue(disposed);
        }

        [Test]
        public void ViewModel_Initialize_CallsVirtualMethod()
        {
            var vm = new TestViewModel();
            vm.Initialize();

            Assert.IsTrue(vm.WasInitialized);
        }

        private class TestViewModel : ViewModel
        {
            public bool WasInitialized { get; private set; }

            public new CompositeDisposable Disposables => base.Disposables;

            public override void Initialize()
            {
                base.Initialize();
                WasInitialized = true;
            }
        }
    }
}
