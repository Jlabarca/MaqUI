using VitalRouter;

namespace MaquiDemos.GameUI
{
    /// <summary>Sent from HUD to open the Inventory panel on the Default layer.</summary>
    public readonly record struct OpenInventoryCommand : ICommand;

    /// <summary>Sent from Inventory to close itself.</summary>
    public readonly record struct CloseInventoryCommand : ICommand;

    /// <summary>Sent from Inventory to open the Shop modal.</summary>
    public readonly record struct OpenShopCommand : ICommand;

    /// <summary>Sent from Shop to close itself.</summary>
    public readonly record struct CloseShopCommand : ICommand;

    /// <summary>Sent when the player purchases an item. Carries item name for HUD feedback.</summary>
    public readonly record struct ItemPurchasedCommand(string ItemName, int Cost) : ICommand;
}
