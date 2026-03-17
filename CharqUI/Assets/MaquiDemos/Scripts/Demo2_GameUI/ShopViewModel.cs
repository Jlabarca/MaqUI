using System.Collections.Generic;
using Maqui.Core.Logic;
using R3;

namespace MaquiDemos.GameUI
{
    public sealed class ShopItem
    {
        public string Name;
        public string Icon;
        public int    Price;
        public string Description;
    }

    /// <summary>
    /// Demo 2 — Shop modal state (UILayer.Modal).
    /// Opening this window triggers the freeze system on Default/Overlay.
    /// </summary>
    public sealed class ShopViewModel : ViewModel
    {
        public readonly ReactiveProperty<IReadOnlyList<ShopItem>> Catalogue;
        public readonly ReactiveProperty<ShopItem> Selected = new(null);
        public readonly ReactiveProperty<string>   FeedbackMsg = new(string.Empty);

        public ShopViewModel()
        {
            Catalogue = new ReactiveProperty<IReadOnlyList<ShopItem>>(new List<ShopItem>
            {
                new() { Name = "Elixir of Might",   Icon = "elixir",  Price = 400,  Description = "+20 ATK for 5 min" },
                new() { Name = "Amulet of Ward",     Icon = "amulet",  Price = 750,  Description = "+15% magic resist"  },
                new() { Name = "Swift Boots",        Icon = "boots",   Price = 320,  Description = "+10 movement speed" },
                new() { Name = "Dragon Scale",       Icon = "scale",   Price = 1200, Description = "Rare crafting mat." },
                new() { Name = "Scroll of Recall",   Icon = "scroll",  Price = 50,   Description = "Teleport to town"   },
                new() { Name = "Phoenix Feather",    Icon = "feather", Price = 2500, Description = "Auto-revive once"   },
            });
        }

        public void Buy(ShopItem item, int playerGold)
        {
            if (playerGold < item.Price)
            {
                FeedbackMsg.Value = "Not enough gold!";
                return;
            }
            FeedbackMsg.Value = $"Bought {item.Name}!";
        }

        public override void Dispose()
        {
            Catalogue.Dispose();
            Selected.Dispose();
            FeedbackMsg.Dispose();
            base.Dispose();
        }
    }
}
