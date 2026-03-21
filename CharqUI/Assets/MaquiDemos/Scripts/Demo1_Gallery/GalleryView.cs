using System.Threading;
using Cysharp.Threading.Tasks;
using Maqui.Core;
using Maqui.Core.Bridge;
using Maqui.Core.Logic;
using Maqui.Core.Presentation;
using R3;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using VitalRouter;

namespace MaquiDemos.Gallery
{
    /// <summary>
    /// Demo 1 — OneUI Component Gallery View.
    ///
    /// Prefab layout (build this in Unity):
    ///
    ///  GalleryView (root)
    ///  ├── CanvasGroup                      ← _canvasGroup  (for fade-in)
    ///  ├── Header
    ///  │   ├── TMP_Text "Maqui Gallery"     ← _titleText
    ///  │   ├── TMP_Text "[Tab Name]"        ← _subtitleText
    ///  │   └── Button "Toggle Theme"
    ///  │       └── TMP_Text                 ← _themeLabel
    ///  ├── TabBar
    ///  │   ├── Button "Buttons"             ← _tabButtons[0]
    ///  │   │   └── Image (underline)        ← _tabIndicators[0]
    ///  │   ├── Button "Cards"               ← _tabButtons[1]
    ///  │   │   └── Image (underline)        ← _tabIndicators[1]
    ///  │   └── Button "Fields"              ← _tabButtons[2]
    ///  │       └── Image (underline)        ← _tabIndicators[2]
    ///  └── Content
    ///      ├── Panel_Buttons (ScrollRect)   ← _contentPanels[0]  ← drag OneUI Button prefabs here
    ///      ├── Panel_Cards   (ScrollRect)   ← _contentPanels[1]  ← drag OneUI Card prefabs here
    ///      └── Panel_Fields  (ScrollRect)   ← _contentPanels[2]  ← drag OneUI Field prefabs here
    ///
    /// Prefab must be saved at: Assets/Resources/Views/Demo1_Gallery.prefab
    /// </summary>
    [Routes]
    public partial class GalleryView : ReactiveBaseView<GalleryViewModel>
    {
        [Header("Nav")]
        [SerializeField] private Button[] _tabButtons;       // 3 elements
        [SerializeField] private Image[] _tabIndicators;     // 3 underline images, one per tab

        [Header("Content Panels")]
        [SerializeField] private GameObject[] _contentPanels; // 3 elements: Buttons, Cards, Fields

        [Header("Header")]
        [SerializeField] private TMP_Text _subtitleText;
        [SerializeField] private Button _themeButton;
        [SerializeField] private TMP_Text _themeLabel;

        [Header("Animation")]
        [SerializeField] private CanvasGroup _canvasGroup;
        [SerializeField] private RectTransform _panelRect;

        // ── Lifecycle ──────────────────────────────────────────────────────────

        protected override void OnBind()
        {
            this.MapTo(Router.Default).AddTo(Disposables);
            // Tab buttons → publish commands
            for (int i = 0; i < _tabButtons.Length; i++)
            {
                int index = i;
                _tabButtons[i].onClick.AddListener(() =>
                    _ = Router.Default.PublishAsync(new SwitchTabCommand((GalleryTab)index)));
            }

            _themeButton.onClick.AddListener(() =>
                _ = Router.Default.PublishAsync(new ToggleThemeCommand()));

            // ActiveTab → update visuals (R3 emits current value immediately)
            ViewModel.ActiveTab
                .Subscribe(tab =>
                {
                    int i = (int)tab;
                    UpdateTabVisuals(i);
                    _subtitleText.text = GalleryViewModel.TabLabels[i];
                })
                .AddTo(Disposables);

            // IsDark → swap theme
            ViewModel.IsDark
                .Subscribe(isDark =>
                {
                    _themeLabel.text = isDark ? "☀  Light" : "☽  Dark";
                    SwapTheme(isDark);
                })
                .AddTo(Disposables);
        }

        // Fade + bounce-in on first show
        protected override async UniTask OnPreShowAsync(CancellationToken ct)
        {
            _canvasGroup.alpha = 0f;
            _panelRect.localScale = new Vector3(0.92f, 0.92f, 1f);

            var anim = MaquiServices.Get<IAnimationBridge>();
            var fade  = anim.FadeAsync(_canvasGroup, 1f, 0.28f, ct);
            var scale = anim.ScaleAsync(_panelRect, Vector3.one, 0.28f,
                AnimationCurve.EaseInOut(0f, 0f, 1f, 1f), ct);

            await UniTask.WhenAll(fade, scale);
        }

        // ── VitalRouter handlers ───────────────────────────────────────────────

        [Route]
        public void On(SwitchTabCommand cmd) => ViewModel.ActiveTab.Value = cmd.Tab;

        [Route]
        public void On(ToggleThemeCommand _) => ViewModel.IsDark.Value = !ViewModel.IsDark.Value;

        // ── Helpers ────────────────────────────────────────────────────────────

        private void UpdateTabVisuals(int activeIndex)
        {
            for (int i = 0; i < _contentPanels.Length; i++)
            {
                _contentPanels[i].SetActive(i == activeIndex);

                if (_tabIndicators != null && i < _tabIndicators.Length)
                {
                    // Indicator brightness: full for active, dim for inactive
                    var c = _tabIndicators[i].color;
                    _tabIndicators[i].color = new Color(c.r, c.g, c.b, i == activeIndex ? 1f : 0.25f);
                }

                // Scale tab label for selected feel
                var label = _tabButtons[i].GetComponentInChildren<TMP_Text>();
                if (label) label.fontStyle = i == activeIndex ? FontStyles.Bold : FontStyles.Normal;
            }
        }

        private static void SwapTheme(bool isDark)
        {
            var provider = MaquiServices.Get<IThemeProvider>();
            if (provider == null) return;
            var name = isDark ? "Theme_Dark" : "Theme_Light";
            var theme = Resources.Load<ThemeData>($"Themes/{name}");
            if (theme != null) provider.SetTheme(theme);
        }

        public override void OnViewDestroy()
        {
            foreach (var btn in _tabButtons) btn.onClick.RemoveAllListeners();
            _themeButton.onClick.RemoveAllListeners();
            base.OnViewDestroy();
        }
    }
}
