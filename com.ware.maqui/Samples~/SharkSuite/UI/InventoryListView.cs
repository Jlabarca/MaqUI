using UnityEngine;
using UnityEngine.UIElements;
using Maqui.Samples.SharkSuite.Logic;
using R3;

namespace Maqui.Samples.SharkSuite.UI
{
    /// <summary>
    /// UI Toolkit implementation of the Inventory List.
    /// </summary>
    public class InventoryListView : MonoBehaviour
    {
        [SerializeField] private UIDocument document;
        private InventoryViewModel _viewModel;
        private ListView _listView;

        public void Initialize(InventoryViewModel viewModel)
        {
            _viewModel = viewModel;
            var root = document.rootVisualElement;
            _listView = root.Q<ListView>("inventory-list");

            if (_listView == null) return;

            // Bind Collection to ListView
            _viewModel.Items
                .Subscribe(items => {
                    _listView.itemsSource = items;
                    _listView.Rebuild();
                });

            // Handle Selection
            _listView.onSelectionChange += objects => {
                if (objects != null)
                {
                    _viewModel.SelectItem((InventoryItem)_listView.selectedItem);
                }
            };
        }
    }
}
