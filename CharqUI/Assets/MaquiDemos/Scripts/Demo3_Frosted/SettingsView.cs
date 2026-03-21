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
    /// Demo 3 — Settings sheet (UILayer.Modal).
    /// Slides in from the bottom with a frosted glass card.
    /// Shares FrostedHUDViewModel so sliders reflect real state.
    ///
    /// Prefab layout (Assets/Resources/Views/Demo3_Settings.prefab):
    ///
    ///  SettingsView (root)
    ///  ├── CanvasGroup                                ← _canvasGroup
    ///  ├── RectTransform (anchored bottom-stretch, ~100% × 420)  ← _panelRect
    ///  │   ├── [TranslucentImage] background          ← frosted glass
    ///  │   ├── Handle (decorative pill)
    ///  │   ├── TMP_Text "Settings"
    ///  │   ├── Button "✕"                            ← _closeButton
    ///  │   ├── Row: "Master Volume"
    ///  │   │   ├── TMP_Text label
    ///  │   │   ├── Slider 0-1                        ← _masterSlider
    ///  │   │   └── TMP_Text "80%"                    ← _masterLabel
    ///  │   ├── Row: "Music Volume"
    ///  │   │   ├── TMP_Text label
    ///  │   │   ├── Slider 0-1                        ← _musicSlider
    ///  │   │   └── TMP_Text "60%"                    ← _musicLabel
    ///  │   ├── Row: "Fullscreen"
    ///  │   │   ├── TMP_Text label
    ///  │   │   └── Toggle                            ← _fullscreenToggle
    ///  │   └── Row: "Show FPS"
    ///  │       ├── TMP_Text label
    ///  │       └── Toggle                            ← _fpsToggle
    ///
    /// Use OneUI Fields prefabs (BlueSlider, BasicToggle) for the controls.
    /// </summary>
    [Routes]
    public partial class SettingsView : ReactiveBaseView<FrostedHUDViewModel>
    {
        [Header("Panel")]
        [SerializeField] private CanvasGroup   _canvasGroup;
        [SerializeField] private RectTransform _panelRect;

        [Header("Header")]
        [SerializeField] private Button _closeButton;

        [Header("Volume")]
        [SerializeField] private Slider   _masterSlider;
        [SerializeField] private TMP_Text _masterLabel;
        [SerializeField] private Slider   _musicSlider;
        [SerializeField] private TMP_Text _musicLabel;

        [Header("Toggles")]
        [SerializeField] private Toggle _fullscreenToggle;
        [SerializeField] private Toggle _fpsToggle;

        private IWindowHandle _ownHandle;
        private bool          _binding; // guard against slider feedback loop

        public void SetHandle(IWindowHandle handle) => _ownHandle = handle;

        protected override void OnBind()
        {
            this.MapTo(Router.Default).AddTo(Disposables);

            _closeButton.onClick.AddListener(() =>
                _ = Router.Default.PublishAsync(new CloseSettingsCommand()));

            // ViewModel → sliders (initial values + external changes)
            ViewModel.MasterVolume
                .Subscribe(v => { _binding = true; _masterSlider.value = v; UpdatePct(_masterLabel, v); _binding = false; })
                .AddTo(Disposables);

            ViewModel.MusicVolume
                .Subscribe(v => { _binding = true; _musicSlider.value = v; UpdatePct(_musicLabel, v); _binding = false; })
                .AddTo(Disposables);

            ViewModel.FullscreenMode
                .Subscribe(v => { _binding = true; _fullscreenToggle.isOn = v; _binding = false; })
                .AddTo(Disposables);

            ViewModel.ShowFPS
                .Subscribe(v => { _binding = true; _fpsToggle.isOn = v; _binding = false; })
                .AddTo(Disposables);

            // Sliders → publish commands (don't set VM directly; command flow keeps it clean)
            _masterSlider.onValueChanged.AddListener(v =>
            {
                if (_binding) return;
                UpdatePct(_masterLabel, v);
                _ = Router.Default.PublishAsync(new SettingChangedCommand("master", v));
            });

            _musicSlider.onValueChanged.AddListener(v =>
            {
                if (_binding) return;
                UpdatePct(_musicLabel, v);
                _ = Router.Default.PublishAsync(new SettingChangedCommand("music", v));
            });

            _fullscreenToggle.onValueChanged.AddListener(v =>
            {
                if (_binding) return;
                ViewModel.FullscreenMode.Value = v;
                Screen.fullScreen = v;
            });

            _fpsToggle.onValueChanged.AddListener(v =>
            {
                if (_binding) return;
                ViewModel.ShowFPS.Value = v;
            });
        }

        // Slides up from bottom
        protected override async UniTask OnPreShowAsync(CancellationToken ct)
        {
            float height = _panelRect.rect.height;
            if (height <= 0f) height = 420f;

            _canvasGroup.alpha = 0f;
            var start = _panelRect.anchoredPosition;
            _panelRect.anchoredPosition = new Vector2(start.x, start.y - height);

            await UniTask.WhenAll(
                MaquiServices.Get<IAnimationBridge>().FadeAsync(_canvasGroup, 1f, 0.22f, ct),
                SlideVerticalAsync(_panelRect, start, 0.30f, ct));
        }

        [Route]
        public void On(CloseSettingsCommand _)
        {
            _ownHandle?.Dispose();
            _ownHandle = null;
        }

        [Route]
        public void On(SettingChangedCommand cmd) => ViewModel.ApplySetting(cmd.Key, cmd.Value);

        private static void UpdatePct(TMP_Text label, float v) =>
            label.text = $"{Mathf.RoundToInt(v * 100)}%";

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
            _masterSlider.onValueChanged.RemoveAllListeners();
            _musicSlider.onValueChanged.RemoveAllListeners();
            _fullscreenToggle.onValueChanged.RemoveAllListeners();
            _fpsToggle.onValueChanged.RemoveAllListeners();
            base.OnViewDestroy();
        }
    }
}
