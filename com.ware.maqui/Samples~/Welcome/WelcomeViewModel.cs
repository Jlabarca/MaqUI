using Maqui.Core.Logic;
using R3;
using VitalRouter;

namespace Maqui.Samples.Welcome
{
    /// <summary>
    /// ViewModel for the Welcome Screen.
    /// </summary>
    public class WelcomeViewModel : ViewModel
    {
        public readonly ReactiveProperty<string> Title = new("Welcome to Maqui");
        public readonly ReactiveProperty<string> Description = new("The reactive MVVM UI framework for Unity.");

        /// <summary>
        /// Command to start the experience.
        /// </summary>
        public void StartExperience()
        {
            Router.Default.PublishAsync(new NavigateToHomeCommand());
        }
    }
}
