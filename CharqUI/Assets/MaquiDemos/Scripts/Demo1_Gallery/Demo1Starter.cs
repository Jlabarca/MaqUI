using Cysharp.Threading.Tasks;
using Maqui.Core;
using Maqui.Core.Presentation;
using UnityEngine;

namespace MaquiDemos.Gallery
{
    /// <summary>
    /// Place this MonoBehaviour on any GameObject in Demo1_Gallery.unity.
    /// CoreBootstrap runs first (BeforeSceneLoad), so MaquiWindowManager is ready.
    /// </summary>
    public sealed class Demo1Starter : MonoBehaviour
    {
        private IWindowHandle _galleryHandle;

        private async void Start()
        {
            var vm = new GalleryViewModel();
            _galleryHandle = await MaquiServices.Get<IUIService>().ShowWindowAsync<GalleryView, GalleryViewModel>(
                "Views/Demo1_Gallery",
                UILayer.Default,
                vm,
                destroyCancellationToken);
        }

        private void OnDestroy()
        {
            _galleryHandle?.Dispose();
            _galleryHandle = null;
        }
    }
}
