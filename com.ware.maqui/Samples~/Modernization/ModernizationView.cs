using Maqui.Core;
using Maqui.Core.Bridge;
using Maqui.Core.Presentation;
using Maqui.Core.Logic;
using UnityEngine;
using R3;
using Ricimi;

namespace Maqui.Samples.Modernization
{
    /// <summary>
    /// Demonstrates a "Zero Texture" screen using procedural logic and theme binding.
    /// </summary>
    public class ModernizationView : ReactiveBaseView<ModernizationViewModel>
    {
        [Header("Procedural Components")]
        [SerializeField] private Ricimi.Gradient BackgroundGradient;
        [SerializeField] private ThemeImageSubscriber MainIcon;

        protected override void OnBind()
        {
            // Bind Theme Color to Gradient
            ViewModel.SelectedColorType
                .Subscribe(type => {
                    BackgroundGradient.Color1Type = type;
                    BackgroundGradient.SendMessage("OnThemeChanged", MaquiServices.Get<IThemeProvider>()?.CurrentTheme);
                })
                .AddTo(Disposables);

            // Bind Icon Color
            ViewModel.SelectedColorType
                .Subscribe(type => MainIcon.ColorType = type)
                .AddTo(Disposables);
        }
    }
}
