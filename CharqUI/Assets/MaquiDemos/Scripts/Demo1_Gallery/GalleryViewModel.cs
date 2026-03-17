using Maqui.Core.Logic;
using R3;

namespace MaquiDemos.Gallery
{
    /// <summary>
    /// Demo 1 — OneUI Component Gallery.
    /// Drives tab selection and dark/light theme toggle.
    /// Pure C# — no UnityEngine references.
    /// </summary>
    public sealed class GalleryViewModel : ViewModel
    {
        public readonly ReactiveProperty<GalleryTab> ActiveTab = new(GalleryTab.Buttons);
        public readonly ReactiveProperty<bool> IsDark = new(false);

        // Tab labels shown in the header subtitle
        public static readonly string[] TabLabels =
        {
            "Buttons & Icons",
            "Cards & Layouts",
            "Fields & Controls"
        };

        public override void Dispose()
        {
            ActiveTab.Dispose();
            IsDark.Dispose();
            base.Dispose();
        }
    }
}
