using System;
using System.Collections.Generic;
using DevsDaddy.Shared.UIFramework;
using DevsDaddy.Shared.UIFramework.Core;
using Maqui.Core.Logic;
using Maqui.Core.Presentation;
using UnityEngine;

namespace Maqui.Core.Bridge
{
    /// <summary>
    /// Advanced Navigator for Maqui.
    /// Handles the instantiation of Reactive Views and their corresponding ViewModels.
    /// </summary>
    public static class MaquiNavigator
    {
        private static readonly Dictionary<Type, ViewModel> _activeViewModels = new();

        /// <summary>
        /// Navigates to a Reactive View, automatically creating its ViewModel.
        /// </summary>
        public static void NavigateReactive<TView, TViewModel>(string resourcePath, Action<TView> onComplete = null)
            where TView : ReactiveBaseView<TViewModel>
            where TViewModel : ViewModel, new()
        {
            UIFramework.LoadViewFromResources<TView>(resourcePath, false, view =>
            {
                // Create or reuse ViewModel
                if (!_activeViewModels.TryGetValue(typeof(TViewModel), out var viewModel))
                {
                    viewModel = new TViewModel();
                    _activeViewModels.Add(typeof(TViewModel), viewModel);
                }

                // Initialize View with ViewModel
                view.Initialize((TViewModel)viewModel);

                // Perform OneUI Navigation
                UIFramework.Navigate(view);

                onComplete?.Invoke(view);
            }, error =>
            {
                Debug.LogError($"[Maqui] Navigation failed: {error}");
            });
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
