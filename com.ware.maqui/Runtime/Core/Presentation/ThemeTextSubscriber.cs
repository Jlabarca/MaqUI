using UnityEngine;
using TMPro;
using Maqui.Core.Logic;

namespace Maqui.Core.Presentation
{
    /// <summary>
    /// Syncs TextMeshPro color with the global theme.
    /// </summary>
    [RequireComponent(typeof(TMP_Text))]
    public class ThemeTextSubscriber : ThemeSubscriber
    {
        public ThemeColorType ColorType = ThemeColorType.TextPrimary;

        private TMP_Text _text;

        private void Awake()
        {
            _text = GetComponent<TMP_Text>();
        }

        protected override void OnThemeChanged(ThemeData theme)
        {
            if (ColorType == ThemeColorType.None) return;
            _text.color = theme.GetColor(ColorType);
        }
    }
}
