using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace MaquiDemos.GameUI
{
    /// <summary>
    /// Simple data-row for the inventory list.
    /// Assign to a prefab that uses Pack or OneUI list-item components.
    ///
    /// Row prefab layout:
    ///   InventoryItemRow
    ///   ├── Image  Icon       ← _icon   (optional — load from Resources/Icons/{item.Icon})
    ///   ├── TMP_Text Name     ← _nameText
    ///   ├── TMP_Text Qty      ← _qtyText   ("×5")
    ///   └── TMP_Text Value    ← _valueText ("500 g")
    /// </summary>
    public sealed class InventoryItemRow : MonoBehaviour
    {
        [SerializeField] private Image    _icon;
        [SerializeField] private TMP_Text _nameText;
        [SerializeField] private TMP_Text _qtyText;
        [SerializeField] private TMP_Text _valueText;

        public void Bind(InventoryItem item)
        {
            _nameText.text  = item.Name;
            _qtyText.text   = item.Quantity > 1 ? $"×{item.Quantity}" : string.Empty;
            _valueText.text = $"{item.Value} g";

            if (_icon != null)
            {
                var sprite = Resources.Load<Sprite>($"Icons/{item.Icon}");
                if (sprite != null) _icon.sprite = sprite;
            }
        }
    }
}
