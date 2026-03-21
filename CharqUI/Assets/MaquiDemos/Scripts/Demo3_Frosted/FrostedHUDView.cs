using Cysharp.Threading.Tasks;
using Maqui.Core.Presentation;
using R3;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using VitalRouter;

namespace MaquiDemos.Frosted
{
    /// <summary>
    /// Demo 3 — Frosted Glass HUD (UILayer.Overlay).
    ///
    /// Uses Le Tai TranslucentImage on the panel backgrounds for the blur/glass effect.
    /// Each panel that needs frosted glass needs:
    ///   Image component → swap to TranslucentImage component in Inspector.
    ///
    /// Prefab layout (Assets/Resources/Views/Demo3_FrostedHUD.prefab):
    ///
    ///  FrostedHUDView (root, full-screen anchor)
    ///  ├── TopBar (anchored top, height ~80)
    ///  │   ├── [TranslucentImage] background  ← frosted glass bar
    ///  │   ├── TMP_Text player name           ← _playerName
    ///  │   ├── TMP_Text zone                  ← _zoneName
    ///  │   └── Button 🔔 (badge)
    ///  │       ├── Image badge background      ← _notifBadge (set active when UnreadCount > 0)
    ///  │       └── TMP_Text count              ← _notifCount
    ///  ├── XPBar (anchored bottom-left, ~300 wide)
    ///  │   ├── [TranslucentImage] background  ← frosted glass
    ///  │   ├── Slider (fill-only)             ← _xpSlider
    ///  │   └── TMP_Text "62%"                 ← _xpLabel
    ///  └── BottomRight buttons
    ///      ├── Button "⚙ Settings"            ← _settingsButton
    ///      └── (add more as needed)
    ///
    /// Note: TranslucentImage requires a BlurredBackground source in the scene.
    /// The Demo3 scene should include a Camera with TranslucentImageSource component.
    /// </summary>
    [Routes]
    public partial class FrostedHUDView : ReactiveBaseView<FrostedHUDViewModel>
    {
        [Header("Top bar")]
        [SerializeField] private TMP_Text  _playerName;
        [SerializeField] private TMP_Text  _zoneName;
        [SerializeField] private Button    _notifButton;
        [SerializeField] private GameObject _notifBadge;
        [SerializeField] private TMP_Text  _notifCount;

        [Header("XP bar")]
        [SerializeField] private Slider   _xpSlider;
        [SerializeField] private TMP_Text _xpLabel;

        [Header("Buttons")]
        [SerializeField] private Button _settingsButton;

        protected override void OnBind()
        {
            this.MapTo(Router.Default).AddTo(Disposables);
            _notifButton.onClick.AddListener(() =>
                _ = Router.Default.PublishAsync(new OpenNotificationsCommand()));

            _settingsButton.onClick.AddListener(() =>
                _ = Router.Default.PublishAsync(new OpenSettingsCommand()));

            ViewModel.PlayerName
                .Subscribe(n => _playerName.text = n)
                .AddTo(Disposables);

            ViewModel.CurrentZone
                .Subscribe(z => _zoneName.text = z)
                .AddTo(Disposables);

            ViewModel.XPRatio
                .Subscribe(v =>
                {
                    _xpSlider.value = v;
                    _xpLabel.text   = $"{Mathf.RoundToInt(v * 100)}%";
                })
                .AddTo(Disposables);

            ViewModel.UnreadCount
                .Subscribe(n =>
                {
                    bool hasUnread = n > 0;
                    _notifBadge.SetActive(hasUnread);
                    _notifCount.text = n.ToString();
                })
                .AddTo(Disposables);
        }

        protected override void OnFreeze()
        {
            _notifButton.interactable   = false;
            _settingsButton.interactable = false;
        }

        protected override void OnUnfreeze()
        {
            _notifButton.interactable   = true;
            _settingsButton.interactable = true;
        }

        [Route]
        public void On(SettingChangedCommand cmd) => ViewModel.ApplySetting(cmd.Key, cmd.Value);

        public override void OnViewDestroy()
        {
            _notifButton.onClick.RemoveAllListeners();
            _settingsButton.onClick.RemoveAllListeners();
            base.OnViewDestroy();
        }
    }
}
