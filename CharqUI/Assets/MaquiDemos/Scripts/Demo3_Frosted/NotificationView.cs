using System.Collections.Generic;
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

namespace MaquiDemos.Frosted
{
    /// <summary>
    /// Demo 3 — Notification tray (UILayer.Modal).
    /// Slides in from the top with a frosted glass background.
    ///
    /// Prefab layout (Assets/Resources/Views/Demo3_Notifications.prefab):
    ///
    ///  NotificationView (root)
    ///  ├── CanvasGroup                          ← _canvasGroup
    ///  ├── RectTransform (anchored top-stretch, ~100% wide × 340 tall)  ← _panelRect
    ///  │   ├── [TranslucentImage] background    ← frosted glass
    ///  │   ├── Handle bar (decorative)
    ///  │   ├── Header row
    ///  │   │   ├── TMP_Text "Notifications"
    ///  │   │   ├── Button "Mark all read"       ← _clearButton
    ///  │   │   └── Button "✕"                  ← _closeButton
    ///  │   └── ScrollView
    ///  │       └── Content (VerticalLayoutGroup) ← _container
    ///  │           └── [NotifRow × N]
    ///
    /// The panel starts off-screen above (anchoredPosition.y = +panelHeight)
    /// and slides down to y=0 using SmoothStep.
    /// </summary>
    [Routes]
    public partial class NotificationView : ReactiveBaseView<FrostedHUDViewModel>
    {
        [Header("Panel")]
        [SerializeField] private CanvasGroup   _canvasGroup;
        [SerializeField] private RectTransform _panelRect;

        [Header("Header")]
        [SerializeField] private Button _closeButton;
        [SerializeField] private Button _clearButton;

        [Header("List")]
        [SerializeField] private Transform    _container;
        [SerializeField] private NotifRow     _rowPrefab;

        private IWindowHandle _ownHandle;

        public void SetHandle(IWindowHandle handle) => _ownHandle = handle;

        protected override void OnBind()
        {
            this.MapTo(Router.Default).AddTo(Disposables);

            _closeButton.onClick.AddListener(() =>
                _ = Router.Default.PublishAsync(new CloseNotificationsCommand()));

            _clearButton.onClick.AddListener(() =>
            {
                ViewModel.UnreadCount.Value = 0;
                _ = Router.Default.PublishAsync(new CloseNotificationsCommand());
            });

            ViewModel.Notifications
                .Subscribe(BuildList)
                .AddTo(Disposables);
        }

        // Slide in from top
        protected override async UniTask OnPreShowAsync(CancellationToken ct)
        {
            float height = _panelRect.rect.height;
            if (height <= 0f) height = 340f; // fallback before layout pass

            _canvasGroup.alpha = 0f;
            var start = _panelRect.anchoredPosition;
            _panelRect.anchoredPosition = new Vector2(start.x, start.y + height);

            await UniTask.WhenAll(
                MaquiServices.Get<IAnimationBridge>().FadeAsync(_canvasGroup, 1f, 0.22f, ct),
                SlideVerticalAsync(_panelRect, start, 0.30f, ct));
        }

        [Route]
        public void On(CloseNotificationsCommand _)
        {
            _ownHandle?.Dispose();
            _ownHandle = null;
        }

        private void BuildList(IReadOnlyList<NotificationEntry> entries)
        {
            foreach (Transform c in _container) Destroy(c.gameObject);
            foreach (var e in entries)
            {
                var row = Instantiate(_rowPrefab, _container);
                row.Bind(e);
            }
        }

        private static async UniTask SlideVerticalAsync(RectTransform rt, Vector2 target, float duration, CancellationToken ct)
        {
            var start   = rt.anchoredPosition;
            var elapsed = 0f;
            while (elapsed < duration)
            {
                if (ct.IsCancellationRequested) break;
                elapsed += Time.deltaTime;
                rt.anchoredPosition = Vector2.LerpUnclamped(start, target, Mathf.SmoothStep(0f, 1f, elapsed / duration));
                await UniTask.Yield(PlayerLoopTiming.Update, ct);
            }
            rt.anchoredPosition = target;
        }

        public override void OnViewDestroy()
        {
            _closeButton.onClick.RemoveAllListeners();
            _clearButton.onClick.RemoveAllListeners();
            base.OnViewDestroy();
        }
    }
}
