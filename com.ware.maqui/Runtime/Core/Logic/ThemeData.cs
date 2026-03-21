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
        private const int ColorSlotCount = 14; // one per ThemeColorType value

        [Header("General Info")]
        public string ThemeName = "Default";

        [Header("Inheritance")]
        [Tooltip("Optional parent theme. Non-overridden color slots fall through to the parent.")]
        [SerializeField] private ThemeData _parent;

        [Tooltip("Check to override this slot instead of inheriting from parent. Index matches ThemeColorType enum.")]
        [SerializeField] private bool[] _overrides = DefaultOverrides();

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

        /// <summary>Parent theme to inherit non-overridden color slots from.</summary>
        public ThemeData Parent
        {
            get => _parent;
            set => _parent = value;
        }

        public Color GetColor(ThemeColorType type)
        {
            // If this slot is overridden (or no parent), return the local color
            if (_parent == null || IsOverridden(type))
                return GetLocalColor(type);

            // Delegate to parent (recursive — supports grandparent chains)
            return _parent.GetColor(type);
        }

        /// <summary>Returns true if this color slot is explicitly set (not inherited from parent).</summary>
        public bool IsOverridden(ThemeColorType type)
        {
            int index = (int)type;
            if (_overrides == null || index < 0 || index >= _overrides.Length)
                return true; // safe fallback: treat as overridden
            return _overrides[index];
        }

        /// <summary>Mark a slot as overridden and set its color value.</summary>
        public void SetColorOverride(ThemeColorType type, Color color)
        {
            EnsureOverridesArray();
            int index = (int)type;
            if (index >= 0 && index < _overrides.Length)
                _overrides[index] = true;
            SetLocalColor(type, color);
        }

        /// <summary>Clear the override flag for a slot so it inherits from the parent.</summary>
        public void ClearOverride(ThemeColorType type)
        {
            EnsureOverridesArray();
            int index = (int)type;
            if (index >= 0 && index < _overrides.Length)
                _overrides[index] = false;
        }

        /// <summary>Returns the local color value without inheritance lookup.</summary>
        private Color GetLocalColor(ThemeColorType type)
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

        private void SetLocalColor(ThemeColorType type, Color color)
        {
            switch (type)
            {
                case ThemeColorType.Primary: PrimaryColor = color; break;
                case ThemeColorType.Secondary: SecondaryColor = color; break;
                case ThemeColorType.Accent: AccentColor = color; break;
                case ThemeColorType.BackgroundMain: BackgroundMain = color; break;
                case ThemeColorType.BackgroundOverlay: BackgroundOverlay = color; break;
                case ThemeColorType.BackgroundContrast: BackgroundContrast = color; break;
                case ThemeColorType.TextPrimary: TextPrimary = color; break;
                case ThemeColorType.TextSecondary: TextSecondary = color; break;
                case ThemeColorType.TextInverse: TextInverse = color; break;
                case ThemeColorType.Success: Success = color; break;
                case ThemeColorType.Warning: Warning = color; break;
                case ThemeColorType.Danger: Danger = color; break;
                case ThemeColorType.Info: Info = color; break;
            }
        }

        private void EnsureOverridesArray()
        {
            if (_overrides == null || _overrides.Length < ColorSlotCount)
                _overrides = DefaultOverrides();
        }

        /// <summary>
        /// All-true by default so existing ThemeData assets (without a parent) behave identically
        /// to before — every slot is treated as explicitly set.
        /// </summary>
        private static bool[] DefaultOverrides()
        {
            var arr = new bool[ColorSlotCount];
            for (int i = 0; i < ColorSlotCount; i++)
                arr[i] = true;
            return arr;
        }
    }
}
