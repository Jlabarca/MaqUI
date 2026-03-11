using UnityEngine;
using System.Collections.Generic;

namespace Maqui.Core.Logic
{
    public enum ThemeColorType
    {
        None,
        Primary,
        Secondary,
        Accent,
        BackgroundMain,
        BackgroundOverlay,
        BackgroundContrast,
        TextPrimary,
        TextSecondary,
        TextInverse,
        Success,
        Warning,
        Danger,
        Info
    }

    [CreateAssetMenu(fileName = "NewTheme", menuName = "Maqui/Theme Data")]
    public class ThemeData : ScriptableObject
    {
        [Header("General Info")]
        public string ThemeName = "Default";

        [Header("Primary Palette")]
        public Color PrimaryColor = Color.cyan;
        public Color SecondaryColor = Color.blue;
        public Color AccentColor = Color.yellow;

        [Header("Backgrounds")]
        public Color BackgroundMain = new Color(0.1f, 0.1f, 0.1f, 1f);
        public Color BackgroundOverlay = new Color(0.15f, 0.15f, 0.15f, 0.8f);
        public Color BackgroundContrast = Color.white;

        [Header("Text")]
        public Color TextPrimary = Color.white;
        public Color TextSecondary = Color.gray;
        public Color TextInverse = Color.black;

        [Header("State Colors")]
        public Color Success = Color.green;
        public Color Warning = Color.orange;
        public Color Danger = Color.red;
        public Color Info = Color.blue;

        [Header("Procedural Settings")]
        public float DefaultRounding = 10f;
        public float DefaultOutlineWidth = 1f;
        public float BlurStrength = 20f;

        public Color GetColor(ThemeColorType type)
        {
            return type switch
            {
                ThemeColorType.Primary => PrimaryColor,
                ThemeColorType.Secondary => SecondaryColor,
                ThemeColorType.Accent => AccentColor,
                ThemeColorType.BackgroundMain => BackgroundMain,
                ThemeColorType.BackgroundOverlay => BackgroundOverlay,
                ThemeColorType.BackgroundContrast => BackgroundContrast,
                ThemeColorType.TextPrimary => TextPrimary,
                ThemeColorType.TextSecondary => TextSecondary,
                ThemeColorType.TextInverse => TextInverse,
                ThemeColorType.Success => Success,
                ThemeColorType.Warning => Warning,
                ThemeColorType.Danger => Danger,
                ThemeColorType.Info => Info,
                _ => Color.white
            };
        }
    }
}
