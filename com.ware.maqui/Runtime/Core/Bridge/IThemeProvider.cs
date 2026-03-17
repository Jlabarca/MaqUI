using R3;
using Maqui.Core.Logic;

namespace Maqui.Core.Bridge
{
    /// <summary>
    /// Contract for the global theme system.
    /// Provides a reactive stream of ThemeData updates.
    /// </summary>
    public interface IThemeProvider
    {
        ReadOnlyReactiveProperty<ThemeData> CurrentTheme { get; }
        void SetTheme(ThemeData newTheme);
    }
}
