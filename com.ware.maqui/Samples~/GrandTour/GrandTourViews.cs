using UnityEngine;
using UnityEngine.UI;
using TMPro;
using R3;
using Maqui.Core.Presentation;

namespace Maqui.Samples.GrandTour
{
    /// <summary>
    /// A generic Screen View for the Grand Tour demo.
    /// Demonstrates reactive binding to shared state.
    /// </summary>
    public class GrandTourScreenView : ReactiveBaseView<DemoScreenViewModel>
    {
        [Header("UI Elements")]
        [SerializeField] private TMP_Text titleLabel;
        [SerializeField] private TMP_Text userLabel;
        [SerializeField] private TMP_Text levelLabel;
        [SerializeField] private Button actionButton;
        [SerializeField] private Slider progressSlider;

        protected override void OnBind()
        {
            // Bind Title
            if (titleLabel != null)
                titleLabel.text = ViewModel.ScreenTitle;

            // Bind Shared State (User Name)
            ViewModel.GlobalState.UserName
                .Subscribe(name => userLabel.text = $"User: {name}")
                .AddTo(Disposables);

            // Bind Shared State (Global Level)
            ViewModel.GlobalState.GlobalLevel
                .Subscribe(lv => levelLabel.text = $"Lv. {lv}")
                .AddTo(Disposables);

            // Bind Loading Progress
            if (progressSlider != null)
            {
                ViewModel.GlobalState.LoadingProgress
                    .Subscribe(p => progressSlider.value = p)
                    .AddTo(Disposables);
            }

            // Handle Interaction
            actionButton?.onClick.AsObservable()
                .Subscribe(_ => ViewModel.GlobalState.IncrementLevel())
                .AddTo(Disposables);
        }
    }

    /// <summary>
    /// A World Space label.
    /// Demonstrates how World UIs stay in sync with Screen UIs.
    /// </summary>
    public class GrandTourWorldView : ReactiveBaseView<DemoWorldViewModel>
    {
        [SerializeField] private TMP_Text floatingLabel;

        protected override void OnBind()
        {
            ViewModel.GlobalState.GlobalLevel
                .Subscribe(lv => floatingLabel.text = $"WORLD LV: {lv}")
                .AddTo(Disposables);

            ViewModel.FloatingPosition
                .Subscribe(pos => transform.localPosition = pos)
                .AddTo(Disposables);
        }
    }
}
