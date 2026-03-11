using R3;
using System.Collections.Generic;

namespace Maqui.Samples.SharkSuite.Logic
{
    public class InventoryItem
    {
        public string Id { get; set; }
        public string Name { get; set; }
        public string Description { get; set; }
        public int PowerLevel { get; set; }
        public string IconPath { get; set; }
    }

    /// <summary>
    /// ViewModel for the Hybrid Inventory (Shark Suite).
    /// </summary>
    public class InventoryViewModel : Maqui.Core.Logic.ViewModel
    {
        // Reactive Collection of Items
        public readonly ReactiveProperty<List<InventoryItem>> Items = new(new List<InventoryItem>());

        // Currently Selected Item
        public readonly ReactiveProperty<InventoryItem> SelectedItem = new();

        public override void Initialize()
        {
            base.Initialize();

            // Mock Data
            Items.Value = new List<InventoryItem>
            {
                new() { Id = "1", Name = "Great White", Description = "The apex predator of the ocean.", PowerLevel = 99 },
                new() { Id = "2", Name = "Hammerhead", Description = "Unique sensory organs for pinpoint accuracy.", PowerLevel = 85 },
                new() { Id = "3", Name = "Tiger Shark", Description = "A generalist feeder with a striped pattern.", PowerLevel = 92 },
                new() { Id = "4", Name = "Mako Shark", Description = "The fastest shark in the sea.", PowerLevel = 88 }
            };
        }

        public void SelectItem(InventoryItem item)
        {
            SelectedItem.Value = item;
        }
    }
}
