using UnityEngine;
using VitalRouter;

namespace Maqui.Samples.GrandTour
{
    /// <summary>
    /// Bootstraps the Grand Tour demo.
    /// Ties together the Shared State, Interceptors, and Views.
    /// </summary>
    public class GrandTourInitializer : MonoBehaviour
    {
        [Header("References")]
        [SerializeField] private GrandTourSettingsView settingsView;
        [SerializeField] private GrandTourScreenView screenView;
        [SerializeField] private GrandTourWorldView worldView;

        private GlobalAppStateViewModel _globalVM;

        private void Start()
        {
            Debug.Log("[Maqui] Initializing Grand Tour Showcase...");

            // 1. Create Global State
            _globalVM = new GlobalAppStateViewModel();

            // 2. Register Advanced Interceptor to the Router
            Router.Default.AddFilter(new GrandTourInterceptor(_globalVM));

            // 3. Setup UI Toolkit Settings (The Hybrid Bridge)
            if (settingsView != null)
                settingsView.Setup(_globalVM);

            // 4. Initialize uGUI Views (Screen + World)
            if (screenView != null)
                screenView.Initialize(new DemoScreenViewModel(_globalVM, "Main Terminal"));

            if (worldView != null)
                worldView.Initialize(new DemoWorldViewModel(_globalVM));

            Debug.Log("[Maqui] Grand Tour Initialized. Shared state is now LIVE.");
        }

        private void OnDestroy()
        {
            _globalVM?.Dispose();
        }
    }
}
