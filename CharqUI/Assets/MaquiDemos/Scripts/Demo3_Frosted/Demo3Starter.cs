using Cysharp.Threading.Tasks;
using Maqui.Core;
using Maqui.Core.Presentation;
using UnityEngine;
using VitalRouter;

namespace MaquiDemos.Frosted
{
    /// <summary>
    /// Place this on any GameObject in Demo3_FrostedHUD.unity.
    ///
    /// One shared FrostedHUDViewModel is passed to all three windows so that
    /// the notification badge on the HUD and the notification list stay in sync,
    /// and settings changes are reflected immediately in the HUD state.
    /// </summary>
    [Routes]
    public partial class Demo3Starter : MonoBehaviour
    {
        private FrostedHUDViewModel _sharedVm;

        private IWindowHandle _hudHandle;
        private IWindowHandle _notifHandle;
        private IWindowHandle _settingsHandle;

        private async void Start()
        {
            this.MapTo(Router.Default).AddTo(destroyCancellationToken);

            _sharedVm = new FrostedHUDViewModel();

            _hudHandle = await MaquiServices.Get<IUIService>().ShowWindowAsync<FrostedHUDView, FrostedHUDViewModel>(
                "Views/Demo3_FrostedHUD",
                UILayer.Overlay,
                _sharedVm,
                destroyCancellationToken);
        }

        // ── Notifications ──────────────────────────────────────────────────────

        [Route]
        public async UniTask On(OpenNotificationsCommand _, System.Threading.CancellationToken ct)
        {
            if (_notifHandle != null) return;

            // Pass the *shared* VM so notification data and badge count stay in sync
            _notifHandle = await MaquiServices.Get<IUIService>().ShowWindowAsync<NotificationView, FrostedHUDViewModel>(
                "Views/Demo3_Notifications",
                UILayer.Modal,
                _sharedVm,
                ct);

            _notifHandle.Root.GetComponent<NotificationView>()?.SetHandle(_notifHandle);
        }

        [Route]
        public void On(CloseNotificationsCommand _)
        {
            _notifHandle?.Dispose();
            _notifHandle = null;
        }

        // ── Settings ───────────────────────────────────────────────────────────

        [Route]
        public async UniTask On(OpenSettingsCommand _, System.Threading.CancellationToken ct)
        {
            if (_settingsHandle != null) return;

            _settingsHandle = await MaquiServices.Get<IUIService>().ShowWindowAsync<SettingsView, FrostedHUDViewModel>(
                "Views/Demo3_Settings",
                UILayer.Modal,
                _sharedVm,
                ct);

            _settingsHandle.Root.GetComponent<SettingsView>()?.SetHandle(_settingsHandle);
        }

        [Route]
        public void On(CloseSettingsCommand _)
        {
            _settingsHandle?.Dispose();
            _settingsHandle = null;
        }

        private void OnDestroy()
        {
            _settingsHandle?.Dispose();
            _notifHandle?.Dispose();
            _hudHandle?.Dispose();
        }
    }
}
