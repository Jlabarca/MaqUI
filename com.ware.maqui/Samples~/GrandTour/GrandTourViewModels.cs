using R3;
using Maqui.Core.Logic;
using UnityEngine;

namespace Maqui.Samples.GrandTour
{
    /// <summary>
    /// Global state shared across all Demo views.
    /// Demonstrates how Maqui handles synchronized cross-view updates.
    /// </summary>
    public class GlobalAppStateViewModel : ViewModel
    {
        // Reactive Properties
        public readonly ReactiveProperty<string> UserName = new("Shark Hunter");
        public readonly ReactiveProperty<int> GlobalLevel = new(1);
        public readonly ReactiveProperty<float> LoadingProgress = new(0f);
        public readonly ReactiveProperty<bool> IsLoading = new(false);

        // Stats
        public readonly ReactiveProperty<int> ActiveViewCount = new(0);

        public void IncrementLevel() => GlobalLevel.Value++;

        public async void SimulateLoading()
        {
            if (IsLoading.Value) return;

            IsLoading.Value = true;
            LoadingProgress.Value = 0f;

            for (int i = 0; i <= 100; i += 10)
            {
                LoadingProgress.Value = i / 100f;
                await System.Threading.Tasks.Task.Delay(100);
            }

            IsLoading.Value = false;
        }
    }

    /// <summary>
    /// ViewModel for Screen Views (Demo 1-4)
    /// </summary>
    public class DemoScreenViewModel : ViewModel
    {
        public readonly GlobalAppStateViewModel GlobalState;
        public readonly string ScreenTitle;
        public readonly ReactiveProperty<string> LocalStatus = new("Idle");

        public DemoScreenViewModel(GlobalAppStateViewModel globalState, string title)
        {
            GlobalState = globalState;
            ScreenTitle = title;
        }

        public void OpenNextDemo()
        {
            LocalStatus.Value = "Navigating...";
            // Logic handled by Interceptor
        }
    }

    /// <summary>
    /// ViewModel for World Views (Demo 1-3)
    /// </summary>
    public class DemoWorldViewModel : ViewModel
    {
        public readonly GlobalAppStateViewModel GlobalState;
        public readonly ReactiveProperty<Vector3> FloatingPosition = new(Vector3.up);

        public DemoWorldViewModel(GlobalAppStateViewModel globalState)
        {
            GlobalState = globalState;
        }
    }
}
