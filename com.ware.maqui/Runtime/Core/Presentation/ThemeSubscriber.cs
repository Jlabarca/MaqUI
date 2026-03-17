using R3;
using Maqui.Core.Bridge;
using Maqui.Core.Logic;
using UnityEngine;

namespace Maqui.Core.Presentation
{
    using Maqui.Core;

    /// <summary>
    /// Base class for UI components that need to respond to global theme changes.
    /// </summary>
    public abstract class ThemeSubscriber : MonoBehaviour
    {
        protected readonly CompositeDisposable ThemeDisposables = new();

        protected virtual void Start()
        {
            // Subscribe to theme changes
            var themeProvider = MaquiServices.Get<IThemeProvider>();
            if (themeProvider == null) return;

            themeProvider.CurrentTheme
                .Where(t => t != null)
                .Subscribe(OnThemeChanged)
                .AddTo(ThemeDisposables);
        }

        /// <summary>
        /// Called whenever the global theme is updated.
        /// </summary>
        protected abstract void OnThemeChanged(ThemeData theme);

        protected virtual void OnDestroy()
        {
            ThemeDisposables.Dispose();
        }
    }
}
