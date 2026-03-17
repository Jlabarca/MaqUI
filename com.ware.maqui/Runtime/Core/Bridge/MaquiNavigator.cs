using System;
using System.Collections.Generic;
using System.Threading;
using Cysharp.Threading.Tasks;
using Maqui.Core.Logic;
using Maqui.Core.Presentation;
using UnityEngine;

namespace Maqui.Core.Bridge
{
    /// <summary>
    /// Simple navigator for Maqui (legacy API — prefer IUIService for new code).
    /// Loads views via IMaquiAssetProvider and manages ViewModel lifecycle.
    /// </summary>
    public static class MaquiNavigator
    {
        private static readonly Dictionary<Type, ViewModel> _activeViewModels = new();
        private static MaquiBaseView _currentView;

        /// <summary>
        /// Navigates to a Reactive View, automatically creating its ViewModel.
        /// Uses the configured IMaquiAssetProvider for loading.
        /// </summary>
        public static void NavigateReactive<TView, TViewModel>(
            string resourcePath,
            Action<TView> onComplete = null,
            CancellationToken ct = default)
            where TView : ReactiveBaseView<TViewModel>
            where TViewModel : ViewModel, new()
        {
            NavigateReactiveAsync<TView, TViewModel>(resourcePath, onComplete, ct).Forget();
        }

        private static async UniTaskVoid NavigateReactiveAsync<TView, TViewModel>(
            string resourcePath,
            Action<TView> onComplete,
            CancellationToken ct)
            where TView : ReactiveBaseView<TViewModel>
            where TViewModel : ViewModel, new()
        {
            try
            {
                var prefab = await MaquiAssetProviderBridge.Current.LoadPrefabAsync(resourcePath, ct);
                if (prefab == null)
                {
                    Debug.LogError($"[Maqui] Navigation failed: could not load '{resourcePath}'");
                    return;
                }

                ct.ThrowIfCancellationRequested();

                var go = UnityEngine.Object.Instantiate(prefab);
                var view = go.GetComponent<TView>();
                if (view == null)
                {
                    Debug.LogError($"[Maqui] Navigation failed: prefab '{resourcePath}' does not have {typeof(TView).Name}");
                    UnityEngine.Object.Destroy(go);
                    return;
                }

                // Create or reuse ViewModel
                if (!_activeViewModels.TryGetValue(typeof(TViewModel), out var viewModel))
                {
                    viewModel = new TViewModel();
                    _activeViewModels.Add(typeof(TViewModel), viewModel);
                }

                view.Initialize((TViewModel)viewModel);

                // Hide previous, show new
                if (_currentView != null)
                    _currentView.HideView();

                _currentView = view;
                view.ShowView();

                onComplete?.Invoke(view);
            }
            catch (OperationCanceledException) { }
            catch (Exception ex)
            {
                Debug.LogError($"[Maqui] Navigation failed: {ex.Message}");
            }
        }

        /// <summary>
        /// Cleans up a ViewModel when its view is removed.
        /// </summary>
        public static void CleanupViewModel<TViewModel>() where TViewModel : ViewModel
        {
            if (_activeViewModels.TryGetValue(typeof(TViewModel), out var viewModel))
            {
                viewModel.Dispose();
                _activeViewModels.Remove(typeof(TViewModel));
            }
        }
    }
}
