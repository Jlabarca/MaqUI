using VitalRouter;
using UnityEngine;

namespace Maqui.Core.Bridge
{
    /// <summary>
    /// Global bridge for VitalRouter.
    /// Manages the default router instance for the Maqui ecosystem.
    /// Registered into MaquiServices as IRouterBridge by CoreBootstrap.
    /// </summary>
    public class RouterBridge : MonoBehaviour, IRouterBridge
    {
        public Router Router { get; private set; }

        private void Awake()
        {
            Router = new Router();
            Debug.Log("[Maqui] VitalRouter Initialized.");
        }
    }
}
