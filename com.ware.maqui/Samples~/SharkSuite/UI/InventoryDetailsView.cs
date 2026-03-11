using Maqui.Core.Presentation;
using Maqui.Samples.SharkSuite.Logic;
using TMPro;
using UnityEngine;
using R3;

namespace Maqui.Samples.SharkSuite.UI
{
    /// <summary>
    /// uGUI implementation of the Item Details panel.
    /// Inherits from ReactiveBaseView to leverage automatic binding.
    /// </summary>
    public class InventoryDetailsView : ReactiveBaseView<InventoryViewModel>
    {
        [Header("UI References")]
        [SerializeField] private TMP_Text NameText;
        [SerializeField] private TMP_Text DescriptionText;
        [SerializeField] private TMP_Text PowerLevelText;

        protected override void OnBind()
        {
            // Bind Details to SelectedItem
            ViewModel.SelectedItem
                .Where(item => item != null)
                .Subscribe(item => {
                    NameText.text = item.Name;
                    DescriptionText.text = item.Description;
                    PowerLevelText.text = $"Power Level: {item.PowerLevel}";
                })
                .AddTo(Disposables);

            // Handle Hidden state if nothing selected
            ViewModel.SelectedItem
                .Subscribe(item => {
                    if (item == null) HideView();
                    else ShowView();
                })
                .AddTo(Disposables);
        }
    }
}
