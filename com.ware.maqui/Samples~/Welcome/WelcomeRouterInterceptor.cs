using System.Threading;
using System.Threading.Tasks;
using Maqui.Core;
using Maqui.Core.Bridge;
using UnityEngine;
using VitalRouter;

namespace Maqui.Samples.Welcome
{
    /// <summary>
    /// Sample interceptor demonstrating cross-cutting navigation logic.
    /// Intercepts NavigateToHomeCommand and fades out the WelcomeView before
    /// allowing the command to propagate.
    ///
    /// Usage (in a MonoBehaviour):
    ///   private WelcomeRouterInterceptor _interceptor;
    ///   void Start()  { _interceptor = new(); Router.Default.AddFilter(_interceptor); }
    ///   void OnDestroy() { Router.Default.RemoveFilter(_interceptor); }
    /// </summary>
    public class WelcomeRouterInterceptor : ICommandInterceptor
    {
        public async ValueTask InvokeAsync<T>(T command, PublishContext context, PublishContinuation<T> next)
            where T : ICommand
        {
            if (command is NavigateToHomeCommand)
            {
                Debug.Log("[Maqui] Intercepted NavigateToHomeCommand. Fading out...");

                // Demonstrate using AnimationBridge from an interceptor for transition effects.
                var anim = MaquiServices.Get<IAnimationBridge>();
                var uiService = MaquiServices.Get<IUIService>();

                if (anim != null && uiService != null)
                {
                    // Fade out the Default layer canvas as a simple transition effect.
                    var canvas = uiService.GetLayerCanvas(Maqui.Core.Presentation.UILayer.Default);
                    if (canvas != null)
                    {
                        var cg = canvas.GetComponent<CanvasGroup>();
                        if (cg != null)
                        {
                            await anim.FadeAsync(cg, 0f, 0.3f, CancellationToken.None);
                        }
                    }
                }

                Debug.Log("[Maqui] Transition complete. Command handled.");
                return; // Stop propagation — interceptor fully handled the navigation
            }

            // All other commands pass through unmodified
            await next(command, context);
        }
    }
}
