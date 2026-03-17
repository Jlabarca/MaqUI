using R3;
using Maqui.Core.Logic;
using UnityEngine;

namespace Maqui.Core.Bridge
{
    /// <summary>
    /// Global Theme Provider for Maqui.
    /// Manages the active ThemeData and provides a reactive stream for theme updates.
    /// Registered into MaquiServices as IThemeProvider by CoreBootstrap.
    /// </summary>
    public class ThemeProvider : MonoBehaviour, IThemeProvider
    {
        [SerializeField] private ThemeData initialTheme;

        private readonly ReactiveProperty<ThemeData> _currentTheme = new();
        public ReadOnlyReactiveProperty<ThemeData> CurrentTheme => _currentTheme;

        private void Awake()
        {
            if (initialTheme != null)
            {
                SetTheme(initialTheme);
            }
        }

        public void SetTheme(ThemeData newTheme)
        {
            if (newTheme == null) return;

            _currentTheme.Value = newTheme;
            Debug.Log($"[Maqui] Theme switched to: {newTheme.ThemeName}");
        }
    }
}
