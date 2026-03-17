using Maqui.Core.Logic;
using R3;

namespace MaquiDemos.GameUI
{
    /// <summary>
    /// Demo 2 — Always-visible HUD state (Overlay layer).
    /// Tracks player resources that react to game events.
    /// </summary>
    public sealed class HUDViewModel : ViewModel
    {
        public readonly ReactiveProperty<int>    Gold      = new(1_250);
        public readonly ReactiveProperty<float>  HPRatio   = new(0.78f);   // 0-1
        public readonly ReactiveProperty<float>  MPRatio   = new(0.55f);   // 0-1
        public readonly ReactiveProperty<string> StatusMsg = new(string.Empty);

        // Called by ItemPurchasedCommand handler
        public void SpendGold(int amount)
        {
            Gold.Value = System.Math.Max(0, Gold.Value - amount);
            StatusMsg.Value = $"Purchased! -{amount}g";
        }

        public override void Dispose()
        {
            Gold.Dispose();
            HPRatio.Dispose();
            MPRatio.Dispose();
            StatusMsg.Dispose();
            base.Dispose();
        }
    }
}
