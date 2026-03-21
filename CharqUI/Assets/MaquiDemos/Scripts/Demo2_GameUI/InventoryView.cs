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
    /// Demo 2 — Inventory panel (UILayer.Default).
    ///
    /// Prefab layout (Assets/Resources/Views/Demo2_Inventory.prefab):
    ///
    ///  InventoryView (root)
    ///  ├── CanvasGroup                      ← _canvasGroup
    ///  ├── RectTransform                    ← _panelRect  (anchored center, ~600×700)
    ///  ├── Header
    ///  │   ├── TMP_Text "Inventory (6)"     ← _titleText
    ///  │   └── Button  "✕"                 ← _closeButton
    ///  ├── ScrollView
    ///  │   └── Content (VerticalLayoutGroup)← _itemContainer
    ///  │       └── [prefab slot] InventoryItemRow × N  (each has Name+Qty+Value labels + icon)
    ///  └── Footer
    ///      ├── TMP_Text "Weight: 0/50"      ← (optional)
    ///      └── Button "Shop"                ← _shopButton
    ///
    /// Use Pack/Common/Prefabs components for the item rows and scroll area.
    /// </summary>
    [Routes]
    public partial class InventoryView : ReactiveBaseView<InventoryViewModel>
    {
        [Header("Header")]
        [SerializeField] private TMP_Text _titleText;
        [SerializeField] private Button   _closeButton;

        [Header("Content")]
        [SerializeField] private Transform          _itemContainer;
        [SerializeField] private InventoryItemRow   _rowPrefab;     // assign in Inspector

        [Header("Footer")]
        [SerializeField] private Button _shopButton;

        [Header("Animation")]
        [SerializeField] private CanvasGroup  _canvasGroup;
        [SerializeField] private RectTransform _panelRect;

        private IWindowHandle _ownHandle; // set by Demo2Starter so we can self-close

        // Called by Demo2Starter immediately after ShowWindowAsync
        public void SetHandle(IWindowHandle handle) => _ownHandle = handle;

        protected override void OnBind()
        {
            this.MapTo(Router.Default).AddTo(Disposables);

            _closeButton.onClick.AddListener(() =>
                _ = Router.Default.PublishAsync(new CloseInventoryCommand()));

            _shopButton.onClick.AddListener(() =>
                _ = Router.Default.PublishAsync(new OpenShopCommand()));

            // Build initial rows from existing items
            foreach (var item in ViewModel.Items)
                AddRow(item);

            // Granular list subscriptions
            ViewModel.Items.ObserveAdd()
                .Subscribe(e => InsertRow(e.Index, e.Item))
                .AddTo(Disposables);

            ViewModel.Items.ObserveRemove()
                .Subscribe(e => RemoveRow(e.Index))
                .AddTo(Disposables);

            ViewModel.Items.ObserveReset()
                .Subscribe(_ => ClearRows())
                .AddTo(Disposables);

            ViewModel.Items.ObserveCountChanged()
                .Subscribe(n => _titleText.text = $"Inventory  ({n})")
                .AddTo(Disposables);
        }

        // Slide in from the right
        protected override async UniTask OnPreShowAsync(CancellationToken ct)
        {
            _canvasGroup.alpha = 0f;
            var startPos = _panelRect.anchoredPosition;
            _panelRect.anchoredPosition = startPos + new Vector2(80f, 0f);

            // Fade + slide simultaneously using DOTween-free approach
            var fadeCt  = ct;
            var slideCt = ct;

            var fade  = MaquiServices.Get<IAnimationBridge>().FadeAsync(_canvasGroup, 1f, 0.25f, fadeCt);
            var slide = SlideToAsync(_panelRect, startPos, 0.25f, slideCt);
            await UniTask.WhenAll(fade, slide);
        }

        [Route]
        public void On(CloseInventoryCommand _)
        {
            _ownHandle?.Dispose();
            _ownHandle = null;
        }

        [Route]
        public async UniTask On(OpenShopCommand _, CancellationToken ct)
        {
            // Shop is opened by Demo2Starter which holds the handles — publish is enough
            // (Demo2Starter subscribes to OpenShopCommand)
        }

        // ── Row building ──────────────────────────────────────────────────────

        private void AddRow(InventoryItem item)
        {
            var row = Instantiate(_rowPrefab, _itemContainer);
            row.Bind(item);
        }

        private void InsertRow(int index, InventoryItem item)
        {
            var row = Instantiate(_rowPrefab, _itemContainer);
            row.Bind(item);
            row.transform.SetSiblingIndex(index);
        }

        private void RemoveRow(int index)
        {
            if (index < _itemContainer.childCount)
                Destroy(_itemContainer.GetChild(index).gameObject);
        }

        private void ClearRows()
        {
            foreach (Transform child in _itemContainer)
                Destroy(child.gameObject);
        }

        // ── Simple slide helper ───────────────────────────────────────────────

        private static async UniTask SlideToAsync(RectTransform rt, Vector2 target, float duration, CancellationToken ct)
        {
            var start   = rt.anchoredPosition;
            var elapsed = 0f;
            while (elapsed < duration)
            {
                if (ct.IsCancellationRequested) break;
                elapsed += Time.deltaTime;
                float t = Mathf.SmoothStep(0f, 1f, elapsed / duration);
                rt.anchoredPosition = Vector2.LerpUnclamped(start, target, t);
                await UniTask.Yield(PlayerLoopTiming.Update, ct);
            }
            rt.anchoredPosition = target;
        }

        public override void OnViewDestroy()
        {
            _closeButton.onClick.RemoveAllListeners();
            _shopButton.onClick.RemoveAllListeners();
            base.OnViewDestroy();
        }
    }
}
