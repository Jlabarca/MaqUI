using System;
using System.Threading;
using Cysharp.Threading.Tasks;
using Maqui.Core.Logic;
using UnityEngine;

namespace Maqui.Core.Presentation
{
    /// <summary>
    /// Fired when a window fails to load, instantiate, or initialize.
    /// Subscribe to <see cref="IUIService.WindowLoadFailed"/> to show fallback UI
    /// or retry logic.
    /// </summary>
    public readonly struct WindowLoadFailedEvent
    {
        public readonly string AssetKey;
        public readonly UILayer Layer;
        public readonly string Reason;
        public readonly Exception Exception;

        public WindowLoadFailedEvent(string assetKey, UILayer layer, string reason, Exception exception = null)
        {
            AssetKey  = assetKey;
            Layer     = layer;
            Reason    = reason;
            Exception = exception;
        }
    }

    /// <summary>
    /// Contract for Maqui's window management system.
    /// Register with HybridFrame's service locator (HF.Register&lt;IUIService&gt;) to expose
    /// window creation to the plugin sandbox.
    /// </summary>
    public interface IUIService
    {
        /// <summary>
        /// Loads and shows a typed ReactiveBaseView from an asset key.
        /// A new TViewModel is created via new() and configured before binding.
        /// </summary>
        UniTask<IWindowHandle> ShowWindowAsync<TView, TViewModel>(
            string assetKey,
            UILayer layer,
            Action<TViewModel> configure = null,
            CancellationToken ct = default)
            where TView : ReactiveBaseView<TViewModel>
            where TViewModel : ViewModel, new();

        /// <summary>
        /// Loads and shows a typed ReactiveBaseView with a pre-constructed ViewModel.
        /// Use this when the ViewModel requires constructor injection.
        /// </summary>
        UniTask<IWindowHandle> ShowWindowAsync<TView, TViewModel>(
            string assetKey,
            UILayer layer,
            TViewModel viewModel,
            CancellationToken ct = default)
            where TView : ReactiveBaseView<TViewModel>
            where TViewModel : ViewModel;

        /// <summary>
        /// Loads and shows a typed ReactiveBaseView with pooling support.
        /// When <see cref="WindowOptions.Pooled"/> is true, Dispose() returns the window
        /// to an internal pool instead of destroying it. Subsequent calls for the same
        /// assetKey reuse the pooled instance (skip Instantiate, re-bind with new VM).
        /// </summary>
        UniTask<IWindowHandle> ShowWindowAsync<TView, TViewModel>(
            string assetKey,
            UILayer layer,
            TViewModel viewModel,
            WindowOptions options,
            CancellationToken ct = default)
            where TView : ReactiveBaseView<TViewModel>
            where TViewModel : ViewModel;

        /// <summary>
        /// Loads and shows a typed ReactiveBaseView with pooling support.
        /// A new TViewModel is created via new() and configured before binding.
        /// </summary>
        UniTask<IWindowHandle> ShowWindowAsync<TView, TViewModel>(
            string assetKey,
            UILayer layer,
            Action<TViewModel> configure,
            WindowOptions options,
            CancellationToken ct = default)
            where TView : ReactiveBaseView<TViewModel>
            where TViewModel : ViewModel, new();

        /// <summary>
        /// Loads a raw prefab and parents it to the specified layer.
        /// Use for plugin-loaded prefabs without a typed ViewModel.
        /// pluginId enables cleanup-on-unload via CleanupPlugin().
        /// </summary>
        UniTask<IWindowHandle> ShowPrefabAsync(
            string assetKey,
            UILayer layer,
            string pluginId = null,
            CancellationToken ct = default);

        /// <summary>
        /// Destroys all windows owned by the given pluginId.
        /// Call from IPluginEntryPoint.OnUnload() via SandboxedPluginAPI.
        /// </summary>
        void CleanupPlugin(string pluginId);

        /// <summary>
        /// Returns the root Canvas for a given layer.
        /// Plugins can use this to parent their own manually-constructed UI.
        /// </summary>
        Canvas GetLayerCanvas(UILayer layer);

        /// <summary>
        /// Registers a view for freeze/unfreeze notifications when Modal windows are shown.
        /// Called by ReactiveBaseView.OnViewAwake().
        /// </summary>
        void RegisterFreezable(IFreezableView view);

        /// <summary>
        /// Unregisters a view from freeze notifications.
        /// Called by ReactiveBaseView.OnViewDestroy().
        /// </summary>
        void UnregisterFreezable(IFreezableView view);

        /// <summary>
        /// Fired when ShowWindowAsync or ShowPrefabAsync fails for any reason
        /// (missing asset, wrong component, asset provider exception, etc.).
        /// Subscribe to show fallback UI or trigger retry logic.
        /// </summary>
        event Action<WindowLoadFailedEvent> WindowLoadFailed;
    }
}
