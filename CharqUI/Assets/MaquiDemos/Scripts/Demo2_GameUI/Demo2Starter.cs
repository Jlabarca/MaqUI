using Cysharp.Threading.Tasks;
using Maqui.Core;
using Maqui.Core.Presentation;
using UnityEngine;
using VitalRouter;

namespace MaquiDemos.GameUI
{
    /// <summary>
    /// Place this on any GameObject in Demo2_GameUI.unity.
    /// Manages handle lifetimes and wires up multi-window flow:
    ///   HUD (Overlay) → Inventory (Default) → Shop (Modal)
    /// </summary>
    [Routes]
    public partial class Demo2Starter : MonoBehaviour
    {
        private HUDViewModel  _hudVm;
        private IWindowHandle _hudHandle;
        private IWindowHandle _inventoryHandle;
        private IWindowHandle _shopHandle;

        private async void Start()
        {
            this.MapTo(Router.Default).AddTo(destroyCancellationToken);

            _hudVm = new HUDViewModel();
            _hudHandle = await MaquiServices.Get<IUIService>().ShowWindowAsync<HUDView, HUDViewModel>(
                "Views/Demo2_HUD",
                UILayer.Overlay,
                _hudVm,
                destroyCancellationToken);
        }

        // ── Open / close Inventory ─────────────────────────────────────────────

        [Route]
        public async UniTask On(OpenInventoryCommand _, System.Threading.CancellationToken ct)
        {
            if (_inventoryHandle != null) return;  // already open

            var vm = new InventoryViewModel();
            _inventoryHandle = await MaquiServices.Get<IUIService>().ShowWindowAsync<InventoryView, InventoryViewModel>(
                "Views/Demo2_Inventory",
                UILayer.Default,
                vm,
                ct);

            // Pass the handle so the view can self-close
            var view = _inventoryHandle.Root.GetComponent<InventoryView>();
            view?.SetHandle(_inventoryHandle);
        }

        [Route]
        public void On(CloseInventoryCommand _)
        {
            _inventoryHandle?.Dispose();
            _inventoryHandle = null;
        }

        // ── Open / close Shop (Modal) ─────────────────────────────────────────

        [Route]
        public async UniTask On(OpenShopCommand _, System.Threading.CancellationToken ct)
        {
            if (_shopHandle != null) return;

            var vm = new ShopViewModel();
            _shopHandle = await MaquiServices.Get<IUIService>().ShowWindowAsync<ShopView, ShopViewModel>(
                "Views/Demo2_Shop",
                UILayer.Modal,
                vm,
                ct);

            // Give the shop access to the handle + current gold
            var view = _shopHandle.Root.GetComponent<ShopView>();
            if (view != null)
            {
                view.SetHandle(_shopHandle);
                view.SetPlayerGold(_hudVm.Gold.CurrentValue);
            }
        }

        [Route]
        public void On(CloseShopCommand _)
        {
            _shopHandle?.Dispose();
            _shopHandle = null;
        }

        private void OnDestroy()
        {
            _shopHandle?.Dispose();
            _shopHandle = null;
            _inventoryHandle?.Dispose();
            _inventoryHandle = null;
            _hudHandle?.Dispose();
            _hudHandle = null;
        }
    }
}
