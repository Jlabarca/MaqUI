using Maqui.Core.Logic;
using R3;

namespace MaquiDemos.GameUI
{
    public sealed class InventoryItem
    {
        public string Name;
        public string Icon;     // sprite name in Resources/Icons/
        public int    Quantity;
        public int    Value;    // sell value in gold
    }

    /// <summary>
    /// Demo 2 — Inventory panel state (UILayer.Default).
    /// Uses ReactiveList for granular add/remove notifications.
    /// In a real game this would be populated by a game service.
    /// </summary>
    public sealed class InventoryViewModel : ViewModel
    {
        public readonly ReactiveList<InventoryItem> Items;
        public readonly ReactiveProperty<int> SelectedIndex = new(-1);

        public InventoryViewModel()
        {
            Items = new ReactiveList<InventoryItem>(new[]
            {
                new InventoryItem { Name = "Iron Sword",   Icon = "sword",   Quantity = 1,  Value = 180 },
                new InventoryItem { Name = "Health Potion",Icon = "potion",  Quantity = 5,  Value = 30  },
                new InventoryItem { Name = "Leather Armor",Icon = "armor",   Quantity = 1,  Value = 250 },
                new InventoryItem { Name = "Mana Crystal", Icon = "crystal", Quantity = 3,  Value = 75  },
                new InventoryItem { Name = "Gold Ingot",   Icon = "ingot",   Quantity = 2,  Value = 500 },
                new InventoryItem { Name = "Shadow Gem",   Icon = "gem",     Quantity = 1,  Value = 900 },
            });
        }

        public override void Dispose()
        {
            Items.Dispose();
            SelectedIndex.Dispose();
            base.Dispose();
        }
    }
}
