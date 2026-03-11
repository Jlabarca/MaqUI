using UnityEngine;
using Maqui.Samples.SharkSuite.Logic;
using Maqui.Samples.SharkSuite.UI;

namespace Maqui.Samples.SharkSuite
{
    /// <summary>
    /// Initializer for the Shark Suite demo.
    /// Manages the lifecycle of the hybrid inventory sample.
    /// </summary>
    public class SharkSuiteInitializer : MonoBehaviour
    {
        [Header("UI Toolkit References")]
        [SerializeField] private InventoryListView ListView;

        [Header("uGUI References")]
        [SerializeField] private InventoryDetailsView DetailsView;

        private InventoryViewModel _viewModel;

        private void Start()
        {
            Debug.Log("[Maqui] Initializing Shark Suite Sample...");

            // 1. Create ViewModel
            _viewModel = new InventoryViewModel();
            _viewModel.Initialize();

            // 2. Initialize UI Toolkit List
            if (ListView != null)
                ListView.Initialize(_viewModel);

            // 3. Initialize uGUI Details View
            if (DetailsView != null)
                DetailsView.Initialize(_viewModel);

            Debug.Log("[Maqui] Shark Suite Ready.");
        }

        private void OnDestroy()
        {
            _viewModel?.Dispose();
        }
    }
}
