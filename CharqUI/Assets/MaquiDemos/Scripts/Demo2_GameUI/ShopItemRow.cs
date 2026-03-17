using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace MaquiDemos.GameUI
{
    /// <summary>
    /// Single row in the shop catalogue list.
    ///
    /// Row prefab layout:
    ///   ShopItemRow
    ///   ├── Image  Icon       ← _icon
    ///   ├── TMP_Text Name     ← _nameText
    ///   ├── TMP_Text Price    ← _priceText   ("400 g")
    ///   └── Button (whole row as button, or separate)  ← _selectButton
    /// </summary>
    public sealed class ShopItemRow : MonoBehaviour
    {
        [SerializeField] private Image    _icon;
        [SerializeField] private TMP_Text _nameText;
        [SerializeField] private TMP_Text _priceText;
        [SerializeField] private Button   _selectButton;

        public void Bind(ShopItem item, Action onSelect)
        {
            _nameText.text  = item.Name;
            _priceText.text = $"{item.Price} g";

            if (_icon != null)
            {
                var sprite = Resources.Load<Sprite>($"Icons/{item.Icon}");
                if (sprite != null) _icon.sprite = sprite;
            }

            _selectButton.onClick.RemoveAllListeners();
            _selectButton.onClick.AddListener(() => onSelect?.Invoke());
        }

        private void OnDestroy() => _selectButton.onClick.RemoveAllListeners();
    }
}
