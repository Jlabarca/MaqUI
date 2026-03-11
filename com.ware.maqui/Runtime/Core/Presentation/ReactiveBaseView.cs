using System.Threading;
using Cysharp.Threading.Tasks;
using DevsDaddy.Shared.UIFramework.Core;
using Maqui.Core.Logic;
using R3;
using UnityEngine;

namespace Maqui.Core.Presentation
{
    /// <summary>
    /// Base class for all Reactive Views in Maqui.
    /// Bridges OneUI's BaseView lifecycle with R3-powered ViewModels and the
    /// MaquiWindowManager layer system.
    /// </summary>
    public abstract class ReactiveBaseView<T> : BaseView, IFreezableView where T : ViewModel
    {
        protected T ViewModel { get; private set; }
        protected readonly CompositeDisposable Disposables = new();

        // ── Registration ───────────────────────────────────────────────────────

        public override void OnViewAwake()
        {
            base.OnViewAwake();
            MaquiWindowManager.Instance?.RegisterFreezable(this);
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
        /// </summary>
        protected abstract void OnBind();

        // ── Destruction ────────────────────────────────────────────────────────

        public override void OnViewDestroy()
        {
            MaquiWindowManager.Instance?.UnregisterFreezable(this);
            OnPreHide();
            base.OnViewDestroy();
            Disposables.Dispose();
            ViewModel?.Dispose();
        }

        // ── Internal surface (MaquiWindowManager only) ─────────────────────────

        internal UniTask InvokePreShowAsync(CancellationToken ct) => OnPreShowAsync(ct);

        void IFreezableView.InvokeFreeze()   => OnFreeze();
        void IFreezableView.InvokeUnfreeze() => OnUnfreeze();
    }
}
