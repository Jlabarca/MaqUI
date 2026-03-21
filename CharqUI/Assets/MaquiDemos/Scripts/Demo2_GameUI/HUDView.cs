using Cysharp.Threading.Tasks;
using Maqui.Core.Presentation;
using R3;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using VitalRouter;

namespace MaquiDemos.GameUI
{
    /// <summary>
    /// Demo 2 — HUD View (UILayer.Overlay).
    ///
    /// Prefab layout (Assets/Resources/Views/Demo2_HUD.prefab):
    ///
    ///  HUDView (root, anchored bottom-left + top-right stretch)
    ///  ├── TopBar
    ///  │   ├── Image GoldIcon
    ///  │   └── TMP_Text "1,250g"           ← _goldText
    ///  ├── BarsPanel (bottom-left corner)
    ///  │   ├── Slider HP (fillOnly)        ← _hpSlider
    ///  │   └── Slider MP (fillOnly)        ← _mpSlider
    ///  ├── TMP_Text StatusMsg              ← _statusText  (fades in/out)
    ///  └── ButtonsPanel (bottom-right)
    ///      ├── Button "Bag"                ← _inventoryButton
    ///      └── (add more as needed)
    ///
    /// Sits on UILayer.Overlay so it persists above everything except modals.
    /// Freezes its own buttons when a modal opens via OnFreeze/OnUnfreeze.
    /// </summary>
    [Routes]
    public partial class HUDView : ReactiveBaseView<HUDViewModel>
    {
        [Header("Resource display")]
        [SerializeField] private TMP_Text _goldText;
        [SerializeField] private Slider   _hpSlider;
        [SerializeField] private Slider   _mpSlider;

        [Header("Feedback")]
        [SerializeField] private TMP_Text    _statusText;
        [SerializeField] private CanvasGroup _statusGroup;

        [Header("Buttons")]
        [SerializeField] private Button _inventoryButton;

        protected override void OnBind()
        {
            this.MapTo(Router.Default).AddTo(Disposables);
            _inventoryButton.onClick.AddListener(() =>
                _ = Router.Default.PublishAsync(new OpenInventoryCommand()));

            ViewModel.Gold
                .Subscribe(g => _goldText.text = g.ToString("N0") + " g")
                .AddTo(Disposables);

            ViewModel.HPRatio
                .Subscribe(v => _hpSlider.value = v)
                .AddTo(Disposables);

            ViewModel.MPRatio
                .Subscribe(v => _mpSlider.value = v)
                .AddTo(Disposables);

            ViewModel.StatusMsg
                .Subscribe(msg => ShowStatus(msg))
                .AddTo(Disposables);
        }

        // ── Freeze feedback: dim buttons when modal opens ─────────────────────

        protected override void OnFreeze()
        {
            _inventoryButton.interactable = false;
        }

        protected override void OnUnfreeze()
        {
            _inventoryButton.interactable = true;
        }

        // ── VitalRouter ───────────────────────────────────────────────────────

        [Route]
        public void On(ItemPurchasedCommand cmd) => ViewModel.SpendGold(cmd.Cost);

        // ── Helpers ───────────────────────────────────────────────────────────

        private System.Threading.CancellationTokenSource _statusCts;

        private void ShowStatus(string msg)
        {
            if (string.IsNullOrEmpty(msg)) return;
            _statusText.text = msg;

            _statusCts?.Cancel();
            _statusCts?.Dispose();
            _statusCts = new System.Threading.CancellationTokenSource();
            PulseStatusAsync(_statusCts.Token).Forget();
        }

        private async Cysharp.Threading.Tasks.UniTask PulseStatusAsync(System.Threading.CancellationToken ct)
        {
            _statusGroup.alpha = 0f;
            var anim = Maqui.Core.MaquiServices.Get<Maqui.Core.Bridge.IAnimationBridge>();
            await anim.FadeAsync(_statusGroup, 1f, 0.2f, ct);
            await Cysharp.Threading.Tasks.UniTask.Delay(1500, cancellationToken: ct);
            await anim.FadeAsync(_statusGroup, 0f, 0.4f, ct);
        }

        public override void OnViewDestroy()
        {
            _inventoryButton.onClick.RemoveAllListeners();
            _statusCts?.Cancel();
            _statusCts?.Dispose();
            base.OnViewDestroy();
        }
    }
}
