using System.Threading;
using Cysharp.Threading.Tasks;
using Maqui.Core.Logic;
using R3;
using UnityEngine;

namespace Maqui.Core.Presentation
{
    using Maqui.Core;
    /// <summary>
    /// Base class for all Reactive Views in Maqui.
    /// Extends MaquiBaseView with typed ViewModel binding, R3 disposables,
    /// and MaquiWindowManager layer/freeze integration.
    /// </summary>
    public abstract class ReactiveBaseView<T> : MaquiBaseView, IFreezableView, IPoolResetable where T : ViewModel
    {
        protected T ViewModel { get; private set; }
        protected CompositeDisposable Disposables = new();

        // ── Registration ───────────────────────────────────────────────────────

        public override void OnViewAwake()
        {
            base.OnViewAwake();
            MaquiServices.Get<IUIService>()?.RegisterFreezable(this);
        }

        // ── Initialization ─────────────────────────────────────────────────────

        /// <summary>
        /// Initializes the view with a ViewModel.
        /// Calls ViewModel.Initialize() then OnBind().
        /// </summary>
        public virtual void Initialize(T viewModel)
        {
            ViewModel = viewModel;
            ViewModel.Initialize();
            OnBind();
        }

        // ── Lifecycle hooks ────────────────────────────────────────────────────

        /// <summary>
        /// Called by MaquiWindowManager after instantiation, before the view animates in.
        /// Override to perform async data fetching that must complete before the window
        /// becomes visible to the player.
        /// </summary>
        protected virtual UniTask OnPreShowAsync(CancellationToken ct) => UniTask.CompletedTask;

        /// <summary>
        /// Called immediately before the view begins its hide sequence.
        /// Override to trigger outgoing state transitions or analytics events.
        /// </summary>
        protected virtual void OnPreHide() { }

        /// <summary>
        /// Called when a Modal-layer window opens and freezes this view's input.
        /// The Default/Overlay GraphicRaycasters are already disabled by MaquiWindowManager.
        /// Override for additional visual/audio freeze feedback.
        /// </summary>
        protected virtual void OnFreeze() { }

        /// <summary>
        /// Called when the last Modal-layer window closes and input is restored.
        /// </summary>
        protected virtual void OnUnfreeze() { }

        /// <summary>
        /// Reactive binding hook. Called after the ViewModel is initialized.
        /// Set up all R3 subscriptions here and add them to Disposables.
        /// Poolable views should also wire up VitalRouter here (not in Start).
        /// </summary>
        protected abstract void OnBind();

        /// <summary>
        /// Called when a pooled view is being returned to the pool.
        /// Override to clear cached references, reset scroll positions, input fields,
        /// or any UI state that OnBind() does not explicitly set on next reuse.
        /// </summary>
        protected virtual void OnReset() { }

        // ── Destruction ────────────────────────────────────────────────────────

        public override void OnViewDestroy()
        {
            MaquiServices.Get<IUIService>()?.UnregisterFreezable(this);
            OnPreHide();
            base.OnViewDestroy();
            Disposables.Dispose();
            ViewModel?.Dispose();
        }

        // ── Internal surface (MaquiWindowManager only) ─────────────────────────

        internal UniTask InvokePreShowAsync(CancellationToken ct) => OnPreShowAsync(ct);

        /// <summary>
        /// Called by MaquiWindowManager when returning a pooled view to the pool.
        /// Disposes subscriptions and ViewModel, unregisters from freeze system.
        /// </summary>
        internal void ResetForPool()
        {
            MaquiServices.Get<IUIService>()?.UnregisterFreezable(this);
            OnPreHide();
            OnReset();
            Disposables.Dispose();
            ViewModel?.Dispose();
            ViewModel = default;
        }

        /// <summary>
        /// Called by MaquiWindowManager when reusing a pooled view with a new ViewModel.
        /// Creates fresh Disposables, re-registers for freeze, and re-binds.
        /// </summary>
        internal void PrepareForReuse(T viewModel)
        {
            // Disposables MUST be reset before OnBind — OnBind wires routing and
            // subscriptions via .AddTo(Disposables), which are disposed on the next
            // ResetForPool. If this line is removed or reordered, pooled windows
            // will accumulate duplicate subscriptions on each reuse.
            Disposables = new CompositeDisposable();
            MaquiServices.Get<IUIService>()?.RegisterFreezable(this);
            ViewModel = viewModel;
            ViewModel.Initialize();
            OnBind();
        }

        void IFreezableView.InvokeFreeze()   => OnFreeze();
        void IFreezableView.InvokeUnfreeze() => OnUnfreeze();
        void IPoolResetable.InvokeResetForPool() => ResetForPool();
    }
}
