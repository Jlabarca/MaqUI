using UnityEngine;
using UnityEngine.UI;
using Maqui.Core.Logic;

namespace Maqui.Core.Presentation
{
    /// <summary>
    /// Syncs a standard Unity Image color with the global theme.
    /// </summary>
    [RequireComponent(typeof(Image))]
    public class ThemeImageSubscriber : ThemeSubscriber
    {
        public ThemeColorType ColorType = ThemeColorType.Primary;

        private Image _image;

        private void Awake()
        {
            _image = GetComponent<Image>();
        }

        protected override void OnThemeChanged(ThemeData theme)
        {
            if (ColorType == ThemeColorType.None) return;
            _image.color = theme.GetColor(ColorType);
        }
    }
}
