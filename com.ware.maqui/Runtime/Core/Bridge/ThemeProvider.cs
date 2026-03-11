using R3;
using Maqui.Core.Logic;
using UnityEngine;

namespace Maqui.Core.Bridge
{
    /// <summary>
    /// Global Theme Provider for Maqui.
    /// Manages the active ThemeData and provides a reactive stream for theme updates.
    /// </summary>
    public class ThemeProvider : MonoBehaviour
    {
        public static ThemeProvider Instance { get; private set; }

        [SerializeField] private ThemeData initialTheme;

        private readonly ReactiveProperty<ThemeData> _currentTheme = new();
        public ReadOnlyReactiveProperty<ThemeData> CurrentTheme => _currentTheme;

        private void Awake()
        {
            if (Instance == null)
            {
                Instance = this;
                DontDestroyOnLoad(gameObject);

                if (initialTheme != null)
                {
                    SetTheme(initialTheme);
                }
            }
            else
            {
                Destroy(gameObject);
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
