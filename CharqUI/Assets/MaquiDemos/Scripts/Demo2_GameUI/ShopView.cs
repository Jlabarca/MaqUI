using System.Threading;
using Cysharp.Threading.Tasks;
using Maqui.Core;
using Maqui.Core.Bridge;
using Maqui.Core.Presentation;
using R3;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using VitalRouter;

namespace MaquiDemos.GameUI
{
    /// <summary>
    /// Demo 2 — Shop modal (UILayer.Modal).
    ///
    /// Opening this window causes MaquiWindowManager to:
    ///   • activate the semi-transparent modal mask at sortOrder=290
    ///   • disable GraphicRaycasters on Default + Overlay layers
    ///   • call OnFreeze() on all registered ReactiveBaseViews (e.g. HUDView, InventoryView)
    ///
    /// Prefab layout (Assets/Resources/Views/Demo2_Shop.prefab):
    ///
    ///  ShopView (root)
    ///  ├── CanvasGroup                    ← _canvasGroup
    ///  ├── RectTransform                  ← _panelRect  (~700×600, anchored center)
    ///  ├── Header
    ///  │   ├── TMP_Text "Shop"
    ///  │   └── Button  "✕"               ← _closeButton
    ///  ├── Left — Catalogue
    ///  │   └── ScrollView
    ///  │       └── Content               ← _catalogueContainer
    ///  │           └── [ShopItemRow × N]
    ///  ├── Right — Detail
    ///  │   ├── TMP_Text item name        ← _detailName
    ///  │   ├── TMP_Text description      ← _detailDesc
    ///  │   ├── TMP_Text price            ← _detailPrice
    ///  │   └── Button "Buy"              ← _buyButton
    ///  └── TMP_Text feedback             ← _feedbackText
    /// </summary>
    [Routes]
    public partial class ShopView : ReactiveBaseView<ShopViewModel>
    {
        [Header("Header")]
        [SerializeField] private Button _closeButton;

        [Header("Catalogue")]
        [SerializeField] private Transform   _catalogueContainer;
        [SerializeField] private ShopItemRow _rowPrefab;

        [Header("Detail panel")]
        [SerializeField] private TMP_Text _detailName;
        [SerializeField] private TMP_Text _detailDesc;
        [SerializeField] private TMP_Text _detailPrice;
        [SerializeField] private Button   _buyButton;
        [SerializeField] private GameObject _detailRoot;   // hide when nothing selected

        [Header("Feedback")]
        [SerializeField] private TMP_Text    _feedbackText;
        [SerializeField] private CanvasGroup _feedbackGroup;

        [Header("Animation")]
        [SerializeField] private CanvasGroup   _canvasGroup;
        [SerializeField] private RectTransform _panelRect;

        private IWindowHandle _ownHandle;
        private int           _playerGold;   // set by Demo2Starter before Initialize

        private void Start() => this.MapTo(Router.Default).AddTo(destroyCancellationToken);

        public void SetHandle(IWindowHandle handle) => _ownHandle = handle;
        public void SetPlayerGold(int gold)          => _playerGold = gold;

        protected override void OnBind()
        {
            _closeButton.onClick.AddListener(() =>
                _ = Router.Default.PublishAsync(new CloseShopCommand()));

            _buyButton.onClick.AddListener(OnBuyClicked);

            ViewModel.Catalogue
                .Subscribe(BuildCatalogue)
                .AddTo(Disposables);

            ViewModel.Selected
                .Subscribe(UpdateDetail)
                .AddTo(Disposables);

            ViewModel.FeedbackMsg
                .Subscribe(ShowFeedback)
                .AddTo(Disposables);
        }

        // Scale-pop in (feels "shoppy")
        protected override async UniTask OnPreShowAsync(CancellationToken ct)
        {
            _canvasGroup.alpha = 0f;
            _panelRect.localScale = new Vector3(0.85f, 0.85f, 1f);

            var curve = new AnimationCurve(
                new Keyframe(0f, 0f),
                new Keyframe(0.7f, 1.05f),
                new Keyframe(1f,  1f));

            var anim = MaquiServices.Get<IAnimationBridge>();
            await UniTask.WhenAll(
                anim.FadeAsync(_canvasGroup, 1f, 0.2f, ct),
                anim.ScaleAsync(_panelRect, Vector3.one, 0.28f, curve, ct));
        }

        // ── VitalRouter ───────────────────────────────────────────────────────

        [Route]
        public void On(CloseShopCommand _)
        {
            _ownHandle?.Dispose();
            _ownHandle = null;
        }

        // ── Catalogue ─────────────────────────────────────────────────────────

        private void BuildCatalogue(System.Collections.Generic.IReadOnlyList<ShopItem> items)
        {
            foreach (Transform c in _catalogueContainer) Destroy(c.gameObject);
            foreach (var item in items)
            {
                var row = Instantiate(_rowPrefab, _catalogueContainer);
                row.Bind(item, () => ViewModel.Selected.Value = item);
            }
        }

        private void UpdateDetail(ShopItem item)
        {
            bool hasItem = item != null;
            _detailRoot.SetActive(hasItem);
            if (!hasItem) return;

            _detailName.text  = item.Name;
            _detailDesc.text  = item.Description;
            _detailPrice.text = $"{item.Price} g";
            _buyButton.interactable = true;
        }

        private void OnBuyClicked()
        {
            if (ViewModel.Selected.Value == null) return;
            ViewModel.Buy(ViewModel.Selected.Value, _playerGold);

            // Also publish so HUD gold counter updates
            var item = ViewModel.Selected.Value;
            _ = Router.Default.PublishAsync(new ItemPurchasedCommand(item.Name, item.Price));
        }

        private System.Threading.CancellationTokenSource _feedCts;

        private void ShowFeedback(string msg)
        {
            if (string.IsNullOrEmpty(msg)) return;
            _feedbackText.text = msg;

            _feedCts?.Cancel();
            _feedCts?.Dispose();
            _feedCts = new System.Threading.CancellationTokenSource();
            PulseFeedbackAsync(_feedCts.Token).Forget();
        }

        private async UniTask PulseFeedbackAsync(System.Threading.CancellationToken ct)
        {
            _feedbackGroup.alpha = 0f;
            var anim = MaquiServices.Get<IAnimationBridge>();
            await anim.FadeAsync(_feedbackGroup, 1f, 0.15f, ct);
            await UniTask.Delay(1200, cancellationToken: ct);
            await anim.FadeAsync(_feedbackGroup, 0f, 0.3f, ct);
        }

        public override void OnViewDestroy()
        {
            _closeButton.onClick.RemoveAllListeners();
            _buyButton.onClick.RemoveAllListeners();
            _feedCts?.Cancel();
            _feedCts?.Dispose();
            base.OnViewDestroy();
        }
    }
}
