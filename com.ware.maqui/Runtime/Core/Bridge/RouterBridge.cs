using VitalRouter;
using UnityEngine;

namespace Maqui.Core.Bridge
{
    /// <summary>
    /// Global bridge for VitalRouter.
    /// Manages the default router instance for the Maqui ecosystem.
    /// </summary>
    public class RouterBridge : MonoBehaviour
    {
        public static RouterBridge Instance { get; private set; }

        public Router Router { get; private set; }

        private void Awake()
        {
            if (Instance == null)
            {
                Instance = this;
                DontDestroyOnLoad(gameObject);

                // Initialize the central router
                Router = new Router();
                Debug.Log("[Maqui] VitalRouter Initialized.");
            }
            else
            {
                Destroy(gameObject);
            }
        }
    }
}
