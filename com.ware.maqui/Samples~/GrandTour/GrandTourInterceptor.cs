using VitalRouter;
using Cysharp.Threading.Tasks;
using System;

namespace Maqui.Samples.GrandTour
{
    /// <summary>
    /// Commands for the Grand Tour demo.
    /// </summary>
    public struct NavigateToDemoCommand : ICommand
    {
        public int DemoId;
    }

    /// <summary>
    /// Interceptor that handles the "Cinematic" transitions for the Grand Tour.
    /// Demonstrates the Async Navigation Pipeline.
    /// </summary>
    public class GrandTourInterceptor : ICommandInterceptor
    {
        private readonly GlobalAppStateViewModel _globalState;

        public GrandTourInterceptor(GlobalAppStateViewModel globalState)
        {
            _globalState = globalState;
        }

        public async ValueTask InvokeAsync<T>(T command, PublishContext context, PublishContinuation<T> next) where T : ICommand
        {
            if (command is NavigateToDemoCommand navCmd)
            {
                UnityEngine.Debug.Log($"[GrandTour] Navigating to Demo {navCmd.DemoId}...");

                // 1. Trigger Loading State
                _globalState.SimulateLoading();

                // 2. Perform Async Transition
                await UniTask.Delay(TimeSpan.FromSeconds(1.5f));

                // 3. Update active view count
                _globalState.ActiveViewCount.Value = navCmd.DemoId;

                UnityEngine.Debug.Log($"[GrandTour] Successfully transitioned to Demo {navCmd.DemoId}.");
                return;
            }

            await next(command, context);
        }
    }
}
