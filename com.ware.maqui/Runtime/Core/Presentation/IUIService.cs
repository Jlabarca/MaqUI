using System;
using System.Threading;
using Cysharp.Threading.Tasks;
using Maqui.Core.Logic;
using UnityEngine;

namespace Maqui.Core.Presentation
{
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
    }
}
