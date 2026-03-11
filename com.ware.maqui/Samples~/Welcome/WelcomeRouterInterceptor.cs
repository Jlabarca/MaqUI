using VitalRouter;
using DevsDaddy.Shared.UIFramework;
using DevsDaddy.OneUI.Views;
using DevsDaddy.Shared.UIFramework.Core;
using System.Threading.Tasks;

namespace Maqui.Samples.Welcome
{
    /// <summary>
    /// Router interceptor to handle navigation logic for the Welcome sample.
    /// </summary>
    public class WelcomeRouterInterceptor : ICommandInterceptor
    {
        public async ValueTask InvokeAsync<T>(T command, PublishContext context, PublishContinuation<T> next) where T : ICommand
        {
            if (command is NavigateToHomeCommand)
            {
                UnityEngine.Debug.Log("[Maqui] Intercepted NavigateToHomeCommand. Transitioning...");

                var welcomeView = UIFramework.GetView<WelcomeView>();
                var homeView = UIFramework.GetView<HomeView>();

                welcomeView?.HideView(new DisplayOptions { IsAnimated = true });
                homeView?.ShowView(new DisplayOptions { IsAnimated = true });

                return; // Stop propagation
            }

            await next(command, context);
        }
    }
}
