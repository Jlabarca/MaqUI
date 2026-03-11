using UnityEngine;
using UnityEngine.UIElements;
using R3;

namespace Maqui.Samples.GrandTour
{
    /// <summary>
    /// A Settings View implemented in UI Toolkit.
    /// Demonstrates hybrid integration and global theme control.
    /// </summary>
    public class GrandTourSettingsView : MonoBehaviour
    {
        [SerializeField] private UIDocument document;

        private VisualElement _root;
        private TextField _nameField;
        private Slider _levelSlider;
        private Button _loadingBtn;

        public void Setup(GlobalAppStateViewModel viewModel)
        {
            _root = document.rootVisualElement;

            _nameField = _root.Q<TextField>("UserNameField");
            _levelSlider = _root.Q<Slider>("GlobalLevelSlider");
            _loadingBtn = _root.Q<Button>("StartLoadingBtn");

            _nameField?.RegisterValueChangedCallback(evt => viewModel.UserName.Value = evt.newValue);
            _levelSlider?.RegisterValueChangedCallback(evt => viewModel.GlobalLevel.Value = (int)evt.newValue);

            if (_loadingBtn != null)
            {
                _loadingBtn.clickable.clicked += () => viewModel.SimulateLoading();
            }

            viewModel.UserName.Subscribe(val => {
                if (_nameField != null) _nameField.value = val;
            });

            viewModel.GlobalLevel.Subscribe(val => {
                if (_levelSlider != null) _levelSlider.value = val;
            });
        }
    }
}
