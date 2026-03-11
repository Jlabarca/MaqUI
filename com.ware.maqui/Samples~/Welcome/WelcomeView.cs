using Maqui.Core.Presentation;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using R3;

namespace Maqui.Samples.Welcome
{
    /// <summary>
    /// Reactive implementation of the Welcome View.
    /// </summary>
    public class WelcomeView : ReactiveBaseView<WelcomeViewModel>
    {
        [Header("UI References")]
        [SerializeField] private TMP_Text TitleText;
        [SerializeField] private TMP_Text DescriptionText;
        [SerializeField] private Button StartButton;

        protected override void OnBind()
        {
            // Bind Title
            ViewModel.Title
                .Subscribe(text => TitleText.text = text)
                .AddTo(Disposables);

            // Bind Description
            ViewModel.Description
                .Subscribe(text => DescriptionText.text = text)
                .AddTo(Disposables);

            // Bind Button Click to ViewModel Command
            StartButton.onClick.AsObservable()
                .Subscribe(_ => ViewModel.StartExperience())
                .AddTo(Disposables);
        }
    }
}
